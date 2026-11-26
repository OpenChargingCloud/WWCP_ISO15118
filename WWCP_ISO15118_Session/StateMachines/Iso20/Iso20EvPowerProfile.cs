/*
 * Copyright (c) 2021-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP ISO/IEC 15118 <https://github.com/OpenChargingCloud/WWCP_ISO15118>
 *
 * Licensed under the Affero GPL license, Version 3.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.gnu.org/licenses/agpl.html
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Generic;
using System.Linq;

using cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated;

namespace cloud.charging.open.protocols.ISO15118.StateMachines.Iso20
{

    /// <summary>
    /// The <c>EVPowerProfile</c> an EVCC sends in <c>PowerDeliveryReq(Start)</c>: when it starts, and what
    /// power it projects in each slot of the schedule the station offered.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this exists.</b> Until 2026-11-27 the profile was a constant: <c>TimeAnchor</c> 0 and one
    /// 3600 s entry at 10 kW. Zero is 1970-01-01 in SECC time, so the profile was active an hour in 1970
    /// and nowhere near the offered schedule. EVerest 2026.10.0 is the first station here that checks a
    /// Scheduled profile against the offer, and it refused every Scheduled <c>-20</c> session with
    /// <c>FAILED_EVPowerProfileInvalid</c>
    /// (<c>docs/interop-runs/2026-11-26-everest-2026.10.0-forward/</c> in ISO15118ConformanceTests).
    /// The station was right:
    /// </para>
    /// <list type="bullet">
    ///   <item><c>[V2G20-1861]</c> — <c>TimeAnchor</c> is when the first entry becomes active, and Table 103
    ///         encodes it in <b>microseconds</b> of SECC time (the <c>PowerSchedule</c>'s own anchor,
    ///         Table 112, is in <b>milliseconds</b>).</item>
    ///   <item><c>[V2G20-1529]</c>, <c>[V2G20-1536]</c> — SECC time is the station's: the <c>TimeStamp</c>
    ///         of its message headers, starting with <c>SessionSetupRes</c>.</item>
    ///   <item><c>[V2G20-1559]</c> — no entry may exceed the selected tuple's schedule at any time, and
    ///         <c>[V2G20-1811]</c> makes the schedule zero before its first and after its last entry, so a
    ///         profile outside the offer can only be compliant at zero power.</item>
    /// </list>
    /// <para>
    /// <b>What it does now.</b> The anchor is the <c>TimeStamp</c> of the station's latest response, in
    /// whole seconds — the station's own clock, so no clock of ours and no synchronisation is involved,
    /// and a recorded session stays a pure function of its responses. The entries follow the selected
    /// tuple's charging schedule from that moment: one entry per remaining slot, each at the car's target
    /// power or the slot's, whichever is lower; a zero entry for any time before the schedule starts; and
    /// nothing past its end. A schedule that has already ended leaves one zero entry, because that is all
    /// <c>[V2G20-1811]</c> allows.
    /// </para>
    /// <para>
    /// <b>Discharge is not checked here, and need not be.</b> This car only charges, so every entry is at
    /// or above zero, and a discharging schedule — a lower bound — cannot be undercut by it.
    /// </para>
    /// <para>
    /// <b>Dynamic mode</b> has no tuple to follow, so its profile stays the car's own curve, one hour at
    /// its target power — but anchored in SECC time like the Scheduled one.
    /// </para>
    /// </remarks>
    public static class Iso20EvPowerProfile
    {

        /// <summary>The car's target power when nothing caps it: 10 kW (10 × 10³ W).</summary>
        public static readonly RationalNumberType TargetPower = new(3, 10);

        /// <summary>The horizon of a profile that has no schedule to follow.</summary>
        public const uint UnboundedDuration = 3600;

        private const ulong MicrosecondsPerSecond = 1_000_000;
        private const ulong MillisecondsPerSecond = 1_000;

        private static readonly RationalNumberType Zero = new(0, 0);


        /// <summary>
        /// Builds the profile for <c>PowerDeliveryReq(Start)</c>.
        /// </summary>
        /// <param name="scheduleRes">The station's last <c>ScheduleExchangeRes</c>.</param>
        /// <param name="seccTimeSeconds">SECC time now, in seconds: the <c>TimeStamp</c> of the station's
        /// latest response.</param>
        /// <param name="dynamic">Whether this session runs in Dynamic control mode.</param>
        public static EVPowerProfileType Build(ScheduleExchangeRes scheduleRes, ulong seccTimeSeconds, bool dynamic)
        {

            var tuple = dynamic ? null : scheduleRes.Scheduled_SEResControlMode?.ScheduleTuple.FirstOrDefault();

            return new EVPowerProfileType(
                TimeAnchor: seccTimeSeconds * MicrosecondsPerSecond,
                Dynamic_EVPPTControlMode: dynamic ? new Dynamic_EVPPTControlModeType() : null,
                // PowerToleranceAcceptance is schema-optional but Josev's model requires it (its SECC rejects
                // an absent one); a live run needed it set. PowerToleranceConfirmed = the EV accepts the tolerance.
                Scheduled_EVPPTControlMode: dynamic
                                                ? null
                                                : new Scheduled_EVPPTControlModeType(tuple?.ScheduleTupleID ?? 1,
                                                                                     PowerToleranceAcceptance.PowerToleranceConfirmed),
                EVPowerProfileEntries: new EVPowerProfileEntryListType(
                    tuple is null
                        ? new[] { new PowerScheduleEntryType(UnboundedDuration, TargetPower, null, null) }
                        : Follow(tuple.ChargingSchedule.PowerSchedule, seccTimeSeconds)));

        }


        /// <summary>The entries that follow <paramref name="schedule"/> from <paramref name="nowSeconds"/> on.</summary>
        public static IReadOnlyList<PowerScheduleEntryType> Follow(PowerScheduleType schedule, ulong nowSeconds)
        {

            var entries   = new List<PowerScheduleEntryType>();
            var slotStart = schedule.TimeAnchor / MillisecondsPerSecond;
            var cursor    = nowSeconds;

            // [V2G20-1811]: before the schedule's first entry its power is zero.
            if (cursor < slotStart)
            {
                entries.Add(new PowerScheduleEntryType(checked((uint) (slotStart - cursor)), Zero, null, null));
                cursor = slotStart;
            }

            foreach (var slot in schedule.PowerScheduleEntries.PowerScheduleEntry)
            {
                var slotEnd = slotStart + slot.Duration;
                if (slotEnd > cursor)
                {
                    entries.Add(new PowerScheduleEntryType((uint) (slotEnd - cursor), Capped(slot), null, null));
                    cursor = slotEnd;
                }
                slotStart = slotEnd;
            }

            // [V2G20-1811] again: the schedule has ended, and after its last entry its power is zero.
            if (entries.Count == 0)
                entries.Add(new PowerScheduleEntryType(UnboundedDuration, Zero, null, null));

            return entries;

        }


        /// <summary>
        /// The car's target power, capped at what <paramref name="slot"/> allows.
        /// </summary>
        /// <remarks>
        /// The entry carries a total and no per-phase values. In a single-phase session only the base
        /// <c>Power</c> counts (<c>[V2G20-1819]</c>), and in a three-phase one a bare total is spread evenly
        /// over the phases (<c>[V2G20-1817]</c>) — so a slot with per-phase values caps the total at three
        /// times its weakest phase as well, and an absent phase counts as zero. Either reading then holds.
        /// </remarks>
        public static RationalNumberType Capped(PowerScheduleEntryType slot)
        {

            var limit = slot.Power;

            if (slot.Power_L2 is not null || slot.Power_L3 is not null)
            {
                var weakest = new[] { slot.Power, slot.Power_L2 ?? Zero, slot.Power_L3 ?? Zero }.Min(Watts);
                if (3 * weakest < Watts(limit))
                    limit = FromWatts(3 * weakest);
            }

            return Watts(TargetPower) <= Watts(limit) ? TargetPower : limit;

        }


        public static double Watts(RationalNumberType value)
            => value.Value * System.Math.Pow(10, value.Exponent);

        /// <summary>The largest representable value that is not above <paramref name="watts"/>, so a cap
        /// can only round towards compliance.</summary>
        public static RationalNumberType FromWatts(double watts)
        {
            sbyte exponent = 0;
            while (System.Math.Abs(watts / System.Math.Pow(10, exponent)) > short.MaxValue)
                exponent++;
            return new RationalNumberType(exponent, (short) System.Math.Floor(watts / System.Math.Pow(10, exponent)));
        }

    }

}
