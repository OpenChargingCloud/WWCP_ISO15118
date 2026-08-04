/*
 * Copyright (c) 2021-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of EVSimulatorApp
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

using Vanaheimr.V2G.Simulation.Session;
using Vanaheimr.V2G.Simulation.Timing;
using cloud.charging.open.protocols.ISO15118.EXI.Dispatch;

using Ac20 = cloud.charging.open.protocols.ISO15118_20.AC.Generated;

namespace Vanaheimr.V2G.Simulation.StateMachines.Iso20
{
    /// <summary>EVCC-side AC hooks: charge-parameter discovery and one AC charge-loop iteration. No pre-/post-charge sequence (DC-only).</summary>
    public sealed class Evcc20Ac(Stream stream, TimeProvider clock, IAsyncDelay pollDelay, TimeSpan perMessageTimeout)
        : Evcc20Base(stream, clock, pollDelay, perMessageTimeout)
    {
        protected override PowerMode EnergyMode => PowerMode.Ac;

        protected override async Task RunChargeParameterDiscoveryAsync(CancellationToken ct)
        {
            var req = new Ac20.AC_ChargeParameterDiscoveryReq(SessionCtx.ToAcHeader(),
                new Ac20.AC_CPDReqEnergyTransferModeType(
                    EVMaximumChargePower: Rat(2_200, 1), EVMaximumChargePower_L2: null, EVMaximumChargePower_L3: null,
                    EVMinimumChargePower: Rat(0), EVMinimumChargePower_L2: null, EVMinimumChargePower_L3: null));

            var (set, message) = await ExchangeRaw(MessageSet.Iso20AC,
                dest => Ac20.AcCodec.TryEncode(req, dest, out int n) ? n : throw EncodeFailed(), ct);
            Expect<Ac20.AC_ChargeParameterDiscoveryRes>(set, message, MessageSet.Iso20AC);
        }

        protected override Task RunPreChargeSequenceAsync(CancellationToken ct) => Task.CompletedTask;   // AC: not applicable

        /// <summary>The EV's present active power, 22 kW. One constant rather than the same literal in
        /// two control-mode branches and again at the meter, because those three drifting apart would
        /// mean the vehicle's counter no longer counted what the vehicle said.</summary>
        private static readonly Ac20.RationalNumberType PresentActivePower = Rat(2_200, 1);
        private const double PresentActivePowerW = 22_000;

        protected override async Task RunChargeLoopIterationAsync(CancellationToken ct)
        {
            // Asking in kind, the mirror of [V2G20-1600] — see Evcc20Dc for the same split and why.
            Ac20.CLReqControlModeType controlMode = PreferDynamicControlMode
                ? new Ac20.Dynamic_AC_CLReqControlModeType(
                      DepartureTime:            DepartureTime,
                      EVTargetEnergyRequest:    Rat(30, 3),   // 30 kWh
                      EVMaximumEnergyRequest:   Rat(60, 3),   // 60 kWh
                      EVMinimumEnergyRequest:   Rat(10, 3),   // 10 kWh
                      EVMaximumChargePower:     Rat(11, 3),   // 11 kW
                      EVMaximumChargePower_L2:  null, EVMaximumChargePower_L3: null,
                      EVMinimumChargePower:     Rat(1,  3),
                      EVMinimumChargePower_L2:  null, EVMinimumChargePower_L3: null,
                      EVPresentActivePower:     PresentActivePower,
                      EVPresentActivePower_L2:  null, EVPresentActivePower_L3: null,
                      EVPresentReactivePower:   Rat(0),
                      EVPresentReactivePower_L2: null, EVPresentReactivePower_L3: null)
                : new Ac20.Scheduled_AC_CLReqControlModeType(
                      null, null, null, null, null, null, null, null, null,
                      EVPresentActivePower: PresentActivePower, null, null, null, null, null);

            var req = new Ac20.AC_ChargeLoopReq(SessionCtx.ToAcHeader(), DisplayParameters: null,
                MeterInfoRequested: false, CLReqControlMode: controlMode);

            var (set, message) = await ExchangeRaw(MessageSet.Iso20AC,
                dest => Ac20.AcCodec.TryEncode(req, dest, out int n) ? n : throw EncodeFailed(), ct);
            Expect<Ac20.AC_ChargeLoopRes>(set, message, MessageSet.Iso20AC);

            // The one place in this project where the EV's own inlet power is a field on the wire:
            // -20 AC has EVPresentActivePower in the request, so the vehicle's view needs no deriving
            // and nothing borrowed from the station.
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
        private static InvalidOperationException EncodeFailed() => new("EXI encode failed (buffer too small?).");
    }
}
