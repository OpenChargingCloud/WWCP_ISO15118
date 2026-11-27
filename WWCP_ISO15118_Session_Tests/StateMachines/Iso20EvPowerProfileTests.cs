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

using cloud.charging.open.protocols.ISO15118.StateMachines.Iso20;
using cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated;

using NUnit.Framework;

namespace cloud.charging.open.protocols.ISO15118.Session.Tests.StateMachines
{

    /// <summary>
    /// The EVCC's <c>EVPowerProfile</c> against the rules a station checks it by — <c>[V2G20-1861]</c>,
    /// <c>[V2G20-1559]</c>, <c>[V2G20-1811]</c> — and against the anchor that made EVerest 2026.10.0 refuse
    /// every Scheduled session: <c>TimeAnchor</c> 0, an hour in 1970.
    /// </summary>
    /// <remarks>
    /// <see cref="Compliant"/> is the station's side of the rule written out independently: every profile
    /// entry, over the whole time it is active, at or below every schedule slot it overlaps, and zero where
    /// no slot covers it. It is a plain reading of the requirement, not a port of anyone's checker, so the
    /// builder is not graded by its own logic.
    /// </remarks>
    [TestFixture]
    public class Iso20EvPowerProfileTests
    {

        /// <summary>2026-01-01T00:00:00Z, the station's clock in these tests.</summary>
        private const UInt64 Now = 1_767_225_600;

        private static RationalNumberType KW(Int16 kw) => new(3, kw);

        private static PowerScheduleEntryType Slot(UInt32 seconds, RationalNumberType power,
                                                   RationalNumberType? l2 = null, RationalNumberType? l3 = null)
            => new(seconds, power, l2, l3);

        /// <summary>A Scheduled offer with one tuple whose charging schedule starts at
        /// <paramref name="anchorSeconds"/> (encoded in ms, Table 112).</summary>
        private static ScheduleExchangeRes Offer(UInt64 anchorSeconds, params PowerScheduleEntryType[] slots)
            => new(Header:                     new MessageHeaderType(new Byte[8], Now, null),
                   ResponseCode:               ResponseCode.OK,
                   EVSEProcessing:             Processing.Finished,
                   GoToPause:                  false,
                   Scheduled_SEResControlMode: new Scheduled_SEResControlModeType(new[]
                   {
                       new ScheduleTupleType(7,
                           new ChargingScheduleType(
                               new PowerScheduleType(anchorSeconds * 1000, null, null, new PowerScheduleEntryListType(slots)),
                               AbsolutePriceSchedule: null, PriceLevelSchedule: null),
                           DischargingSchedule: null),
                   }),
                   Dynamic_SEResControlMode:   null);

        private static PowerScheduleType ScheduleOf(ScheduleExchangeRes offer)
            => offer.Scheduled_SEResControlMode!.ScheduleTuple[0].ChargingSchedule.PowerSchedule;

        /// <summary>The station's reading: is the profile, everywhere it is active, within the schedule?</summary>
        private static Boolean Compliant(EVPowerProfileType profile, PowerScheduleType schedule)
        {
            var entryStart = profile.TimeAnchor / 1_000_000;
            foreach (var entry in profile.EVPowerProfileEntries.EVPowerProfileEntry)
            {
                var entryEnd = entryStart + entry.Duration;
                var demand   = Iso20EvPowerProfile.Watts(entry.Power);

                var slotStart = schedule.TimeAnchor / 1000;
                foreach (var slot in schedule.PowerScheduleEntries.PowerScheduleEntry)
                {
                    var slotEnd = slotStart + slot.Duration;
                    if (entryStart < slotEnd && slotStart < entryEnd && demand > Iso20EvPowerProfile.Watts(slot.Power))
                        return false;
                    slotStart = slotEnd;
                }

                var scheduleStart = schedule.TimeAnchor / 1000;
                var scheduleEnd   = slotStart;
                if ((entryStart < scheduleStart || entryEnd > scheduleEnd) && demand > 0)
                    return false;                                        // [V2G20-1811]: zero there

                entryStart = entryEnd;
            }
            return true;
        }


        [Test]
        public void TheAnchorIsTheStationsTimeStamp_InMicroseconds()
        {
            var profile = Iso20EvPowerProfile.Build(Offer(Now - 60, Slot(3600, KW(22))), Now, dynamic: false);

            Assert.That(profile.TimeAnchor, Is.EqualTo(Now * 1_000_000), "Table 103: microseconds of SECC time");
            Assert.That(profile.Scheduled_EVPPTControlMode!.SelectedScheduleTupleID, Is.EqualTo(7));
        }

        [Test]
        public void TheOldConstantProfile_IsWhatTheStationRefused()
        {
            // TimeAnchor 0 and one hour at 10 kW: active in 1970, where [V2G20-1811] makes the schedule zero.
            var old = new EVPowerProfileType(0, null, new Scheduled_EVPPTControlModeType(7, null),
                                             new EVPowerProfileEntryListType(new[] { Slot(3600, KW(10)) }));

            Assert.That(Compliant(old, ScheduleOf(Offer(Now - 60, Slot(3600, KW(22))))), Is.False);
        }

        [Test]
        public void ItFollowsTheSlotsFromNow_CappedAtEach()
        {
            var offer   = Offer(Now - 600, Slot(3600, KW(22)), Slot(3600, KW(5)));
            var profile = Iso20EvPowerProfile.Build(offer, Now, dynamic: false);
            var entries = profile.EVPowerProfileEntries.EVPowerProfileEntry;

            Assert.That(entries.Select(e => e.Duration), Is.EqualTo(new UInt32[] { 3000, 3600 }));
            Assert.That(entries[0].Power, Is.EqualTo(Iso20EvPowerProfile.TargetPower), "22 kW allowed, the car wants 10");
            Assert.That(entries[1].Power, Is.EqualTo(KW(5)),                           "5 kW allowed, so 5");
            Assert.That(Compliant(profile, ScheduleOf(offer)), Is.True);
        }

        [Test]
        public void BeforeTheScheduleStarts_ItProjectsZero()
        {
            var offer   = Offer(Now + 30, Slot(600, KW(22)));
            var profile = Iso20EvPowerProfile.Build(offer, Now, dynamic: false);
            var entries = profile.EVPowerProfileEntries.EVPowerProfileEntry;

            Assert.That(entries.Select(e => e.Duration), Is.EqualTo(new UInt32[] { 30, 600 }));
            Assert.That(Iso20EvPowerProfile.Watts(entries[0].Power), Is.Zero);
            Assert.That(Compliant(profile, ScheduleOf(offer)), Is.True);
        }

        [Test]
        public void ASchedulethatHasEnded_LeavesOneZeroEntry()
        {
            // Our own SECC's offer until 2026-11-27: anchored at 0, so it ended in 1970.
            var offer   = Offer(0, Slot(3600, new RationalNumberType(0, 100)));
            var profile = Iso20EvPowerProfile.Build(offer, Now, dynamic: false);
            var entries = profile.EVPowerProfileEntries.EVPowerProfileEntry;

            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(Iso20EvPowerProfile.Watts(entries[0].Power), Is.Zero);
            Assert.That(Compliant(profile, ScheduleOf(offer)), Is.True);
        }

        [Test]
        public void APerPhaseSlot_CapsTheTotalAtThreeTimesItsWeakestPhase()
        {
            // 11 kW on L1 and L3 but 1 kW on L2: spread evenly, a 10 kW total would put 3,3 kW on L2.
            var capped = Iso20EvPowerProfile.Capped(Slot(3600, KW(11), l2: KW(1), l3: KW(11)));

            Assert.That(Iso20EvPowerProfile.Watts(capped), Is.EqualTo(3_000));
        }

        [Test]
        public void AnAbsentPhase_CountsAsZero()
        {
            var capped = Iso20EvPowerProfile.Capped(Slot(3600, KW(11), l2: KW(11), l3: null));

            Assert.That(Iso20EvPowerProfile.Watts(capped), Is.Zero);
        }

        [Test]
        public void DynamicMode_KeepsTheCarsOwnCurve_ButAnchoredInSeccTime()
        {
            var profile = Iso20EvPowerProfile.Build(Offer(Now, Slot(60, KW(1))), Now, dynamic: true);

            Assert.That(profile.TimeAnchor,                 Is.EqualTo(Now * 1_000_000));
            Assert.That(profile.Scheduled_EVPPTControlMode, Is.Null);
            Assert.That(profile.Dynamic_EVPPTControlMode,   Is.Not.Null);
            Assert.That(profile.EVPowerProfileEntries.EVPowerProfileEntry.Single().Power,
                        Is.EqualTo(Iso20EvPowerProfile.TargetPower), "Dynamic has no schedule to follow");
        }

        [Test]
        public void FromWatts_NeverRoundsUp()
        {
            var value = Iso20EvPowerProfile.FromWatts(123_456.9);

            Assert.That(Iso20EvPowerProfile.Watts(value), Is.LessThanOrEqualTo(123_456.9));
            Assert.That(Iso20EvPowerProfile.Watts(value), Is.GreaterThan(123_000));
        }

    }

}
