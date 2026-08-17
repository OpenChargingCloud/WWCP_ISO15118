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

using cloud.charging.open.protocols.ISO15118.Session;
using cloud.charging.open.protocols.ISO15118.Timing;
using cloud.charging.open.protocols.ISO15118.EXI.Dispatch;

using Ac20         = cloud.charging.open.protocols.ISO15118_20.AC.Generated;
using Ac20Rational = cloud.charging.open.protocols.ISO15118_20.AC.RationalNumber;

namespace cloud.charging.open.protocols.ISO15118.StateMachines.Iso20
{
    /// <summary>EVCC-side AC hooks: charge-parameter discovery and one AC charge-loop iteration. No pre-/post-charge sequence (DC-only).</summary>
    public sealed class Evcc20Ac(Stream stream, TimeProvider clock, IAsyncDelay pollDelay, TimeSpan perMessageTimeout)
        : Evcc20Base(stream, clock, pollDelay, perMessageTimeout)
    {
        protected override PowerMode EnergyMode => PowerMode.Ac;

        protected override async Task RunChargeParameterDiscoveryAsync(CancellationToken ct)
        {
            // Asking in kind on the direction axis — see Evcc20Dc for the same split and why. AC's BPT
            // subtype is the charge-only one plus a discharge power per phase; this EV is single-phase and
            // symmetric, so L2/L3 stay null on both halves and the discharge envelope mirrors the charge one.
            Ac20.AC_CPDReqEnergyTransferModeType transferMode = BidirectionalService
                ? new Ac20.BPT_AC_CPDReqEnergyTransferModeType(
                      EVMaximumChargePower: Rat(2_200, 1), EVMaximumChargePower_L2: null, EVMaximumChargePower_L3: null,
                      EVMinimumChargePower: Rat(0), EVMinimumChargePower_L2: null, EVMinimumChargePower_L3: null,
                      EVMaximumDischargePower: Rat(2_200, 1), EVMaximumDischargePower_L2: null, EVMaximumDischargePower_L3: null,
                      EVMinimumDischargePower: Rat(0), EVMinimumDischargePower_L2: null, EVMinimumDischargePower_L3: null)
                : new Ac20.AC_CPDReqEnergyTransferModeType(
                      EVMaximumChargePower: Rat(2_200, 1), EVMaximumChargePower_L2: null, EVMaximumChargePower_L3: null,
                      EVMinimumChargePower: Rat(0), EVMinimumChargePower_L2: null, EVMinimumChargePower_L3: null);

            var req = new Ac20.AC_ChargeParameterDiscoveryReq(SessionCtx.ToAcHeader(), transferMode);

            var (set, message) = await ExchangeRaw(MessageSet.Iso20AC,
                dest => Ac20.AcCodec.TryEncode(req, dest, out int n) ? n : throw EncodeFailed(), ct);
            Expect<Ac20.AC_ChargeParameterDiscoveryRes>(set, message, MessageSet.Iso20AC);
        }

        protected override Task RunPreChargeSequenceAsync(CancellationToken ct) => Task.CompletedTask;   // AC: not applicable

        /// <summary>What this vehicle's inlet can take, and the ceiling every figure below is held inside.
        /// Hardware: it is what the discovery envelope above declares, and it does not move because the
        /// driver asked for less today.</summary>
        private const double MaxActivePowerW = 22_000;

        /// <summary>
        /// The EV's present active power — what it is drawing right now, and the one figure it puts on an
        /// AC wire. <c>--power</c> lands here, scaled by the battery's taper, held inside
        /// <see cref="MaxActivePowerW"/>; 22 kW when no battery named a power, which is what this was
        /// before there were batteries and what every recorded run was taken at.
        /// </summary>
        /// <remarks>
        /// This is the honest place for it, and it is also the whole of AC's control model: the station
        /// offers an envelope and the <em>car</em> decides what to draw inside it, which is why AC needs no
        /// setpoint field for the EV to be steerable. <see cref="Secc20Ac"/> meters what this says.
        /// </remarks>
        private Ac20.RationalNumberType PresentActivePower
            => Battery is { RequestedPowerW: > 0 } b
                   ? Watts(Math.Min(MaxActivePowerW, b.RequestedPowerW * b.PowerFactor))
                   : Rat(2_200, 1);   // 22 kW, and this exact encoding — the corpus recorded it

        /// <summary>The same figure at the meter, read back off the rational rather than computed a second
        /// time: a vehicle whose counter and whose wire disagreed by a rounding step would be reporting one
        /// number and metering another, and the station meters the wire.</summary>
        private double PresentActivePowerW => (double) Ac20Rational.ToDecimal(PresentActivePower);

        /// <summary>
        /// The charge-power ceiling the car states inside the loop: the ask itself, tapered — separate from
        /// the discovery envelope on purpose, the same split as <see cref="Evcc20Dc.LoopMaxPower"/>.
        /// 11 kW when nothing asked, which is what the Dynamic arms always stated.
        /// </summary>
        private Ac20.RationalNumberType LoopMaxChargePower
            => Battery is { RequestedPowerW: > 0 } b
                   ? Watts(Math.Min(MaxActivePowerW, b.RequestedPowerW * b.PowerFactor))
                   : Rat(11, 3);

        /// <summary>The same for Scheduled, where it was never stated at all: optional there, so it stays
        /// absent unless <c>--power</c> gives it something to say.</summary>
        private Ac20.RationalNumberType? ScheduledMaxChargePower
            => Battery is { RequestedPowerW: > 0 } ? LoopMaxChargePower : null;

        /// <summary>
        /// What the car asks for as energy: how much it wants, how much it can still take, how much it
        /// needs. All three shrink as the pack fills, because they are what is left; the 30 / 60 / 10 kWh
        /// is what every recorded run carries and what a car without a pack still sends.
        /// </summary>
        private Ac20.RationalNumberType LoopTargetEnergy
            => EnergyRequestWh is { } e ? WattHours(e.Target)  : Rat(30, 3);
        private Ac20.RationalNumberType LoopMaximumEnergy
            => EnergyRequestWh is { } e ? WattHours(e.Maximum) : Rat(60, 3);
        private Ac20.RationalNumberType LoopMinimumEnergy
            => EnergyRequestWh is { } e ? WattHours(e.Minimum) : Rat(10, 3);

        /// <summary>The same three where Scheduled leaves them optional: absent unless a pack has
        /// something to say, which is where they stood before there were packs.</summary>
        private Ac20.RationalNumberType? ScheduledTargetEnergy  => Battery is null ? null : LoopTargetEnergy;
        private Ac20.RationalNumberType? ScheduledMaximumEnergy => Battery is null ? null : LoopMaximumEnergy;
        private Ac20.RationalNumberType? ScheduledMinimumEnergy => Battery is null ? null : LoopMinimumEnergy;

        protected override async Task RunChargeLoopIterationAsync(CancellationToken ct)
        {
            // Asking in kind, the mirror of [V2G20-1600], on both axes — see Evcc20Dc for the same split
            // and why.
            Ac20.CLReqControlModeType controlMode = (PreferDynamicControlMode, BidirectionalService) switch
            {
                // The Dynamic discharge pair is *mandatory* in the BPT subtype: a station steering a
                // bidirectional session needs to know how far either way.
                (true, true) => new Ac20.BPT_Dynamic_AC_CLReqControlModeType(
                      DepartureTime:            DepartureTime,
                      EVTargetEnergyRequest:    LoopTargetEnergy,
                      EVMaximumEnergyRequest:   LoopMaximumEnergy,
                      EVMinimumEnergyRequest:   LoopMinimumEnergy,
                      EVMaximumChargePower:     LoopMaxChargePower,
                      EVMaximumChargePower_L2:  null, EVMaximumChargePower_L3: null,
                      EVMinimumChargePower:     Rat(1,  3),
                      EVMinimumChargePower_L2:  null, EVMinimumChargePower_L3: null,
                      EVPresentActivePower:     PresentActivePower,
                      EVPresentActivePower_L2:  null, EVPresentActivePower_L3: null,
                      EVPresentReactivePower:   Rat(0),
                      EVPresentReactivePower_L2: null, EVPresentReactivePower_L3: null,
                      EVMaximumDischargePower:  LoopMaxChargePower,   // the charge request mirrored
                      EVMaximumDischargePower_L2: null, EVMaximumDischargePower_L3: null,
                      EVMinimumDischargePower:  Rat(1,  3),
                      EVMinimumDischargePower_L2: null, EVMinimumDischargePower_L3: null,
                      EVMaximumV2XEnergyRequest: null, EVMinimumV2XEnergyRequest: null),

                (true, false) => new Ac20.Dynamic_AC_CLReqControlModeType(
                      DepartureTime:            DepartureTime,
                      EVTargetEnergyRequest:    LoopTargetEnergy,
                      EVMaximumEnergyRequest:   LoopMaximumEnergy,
                      EVMinimumEnergyRequest:   LoopMinimumEnergy,
                      EVMaximumChargePower:     LoopMaxChargePower,
                      EVMaximumChargePower_L2:  null, EVMaximumChargePower_L3: null,
                      EVMinimumChargePower:     Rat(1,  3),
                      EVMinimumChargePower_L2:  null, EVMinimumChargePower_L3: null,
                      EVPresentActivePower:     PresentActivePower,
                      EVPresentActivePower_L2:  null, EVPresentActivePower_L3: null,
                      EVPresentReactivePower:   Rat(0),
                      EVPresentReactivePower_L2: null, EVPresentReactivePower_L3: null),

                // Scheduled's limits are all optional, so these two arms differ only in the discharge
                // envelope — which is the whole of what makes the request bidirectional, hence stated
                // rather than left null.
                (false, true) => new Ac20.BPT_Scheduled_AC_CLReqControlModeType(
                      EVTargetEnergyRequest:  ScheduledTargetEnergy,
                      EVMaximumEnergyRequest: ScheduledMaximumEnergy,
                      EVMinimumEnergyRequest: ScheduledMinimumEnergy,
                      EVMaximumChargePower: ScheduledMaxChargePower, null, null, null, null, null,
                      EVPresentActivePower: PresentActivePower, null, null, null, null, null,
                      EVMaximumDischargePower: Rat(2_200, 1),   // 22 kW: what the car can give back, hardware
                      EVMaximumDischargePower_L2: null, EVMaximumDischargePower_L3: null,
                      EVMinimumDischargePower: Rat(0),
                      EVMinimumDischargePower_L2: null, EVMinimumDischargePower_L3: null),

                _ => new Ac20.Scheduled_AC_CLReqControlModeType(
                      EVTargetEnergyRequest:  ScheduledTargetEnergy,
                      EVMaximumEnergyRequest: ScheduledMaximumEnergy,
                      EVMinimumEnergyRequest: ScheduledMinimumEnergy,
                      EVMaximumChargePower: ScheduledMaxChargePower, null, null, null, null, null,
                      EVPresentActivePower: PresentActivePower, null, null, null, null, null),
            };

            var req = new Ac20.AC_ChargeLoopReq(SessionCtx.ToAcHeader(), DisplayParameters: null,
                MeterInfoRequested: RequestMeterInfo, CLReqControlMode: controlMode);

            // Table 216 gives AC_ChargeLoopReq 0,5 s where the ordinary message gets 2 s — see
            // Evcc20Dc.ChargeLoopAsync for the DC twin and Evcc20Base.ChargeLoopMsgTimeout for the rule.
            var (set, message) = await ExchangeRaw(MessageSet.Iso20AC,
                dest => Ac20.AcCodec.TryEncode(req, dest, out int n) ? n : throw EncodeFailed(), ct,
                ChargeLoopMsgTimeout);
            var response = Expect<Ac20.AC_ChargeLoopRes>(set, message, MessageSet.Iso20AC);

            NoteMeterInfo(response.MeterInfo is not null);

            // [V2G20-1477]: the station asks for a service renegotiation through the otherwise
            // absent EVSEStatus. The base class acts on it once this iteration is finished and the
            // contactor is open — it cannot see this type, which is why the loop reports it.
            NoteRenegotiationRequest(response.EVSEStatus?.EVSENotification == Ac20.EvseNotification.ServiceRenegotiation);

            // The one place in this project where the EV's own inlet power is a field on the wire:
            // -20 AC has EVPresentActivePower in the request, so the vehicle's view needs no deriving
            // and nothing borrowed from the station. Read back off the rational it just sent, so the
            // counter counts exactly what the wire carried.
            Meter.Sample(PresentActivePowerW);
        }

        protected override Task RunPostChargeSequenceAsync(CancellationToken ct) => Task.CompletedTask;   // AC: not applicable

        private static T Expect<T>(MessageSet actualSet, object message, MessageSet expectedSet)
        {
            if (actualSet != expectedSet || message is not T typed)
                throw new SessionAborted($"expected a {typeof(T).Name} on {expectedSet}, got {message.GetType().Name} on {actualSet}.");
            return typed;
        }

        private static Ac20.RationalNumberType Rat(short value, sbyte exponent = 0) => new(exponent, value);

        /// <summary>Watts as an AC rational; the arithmetic (and its saturation) is
        /// <see cref="Evcc20Base.ScaledRational"/>, shared with the DC side.</summary>
        private static Ac20.RationalNumberType Watts(double watts)
        {
            var (value, exponent) = ScaledRational(watts);
            return Rat(value, exponent);
        }

        /// <summary>And watt-hours. Same arithmetic — the rational carries no unit, the field does — and
        /// named apart so a call site says which of the two it is sending.</summary>
        private static Ac20.RationalNumberType WattHours(double wattHours)
        {
            var (value, exponent) = ScaledRational(wattHours);
            return Rat(value, exponent);
        }

        private static InvalidOperationException EncodeFailed() => new("EXI encode failed (buffer too small?).");
    }
}
