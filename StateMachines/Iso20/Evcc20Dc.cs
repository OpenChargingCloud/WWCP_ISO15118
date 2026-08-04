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

using Vanaheimr.V2G.Simulation.Session;
using Vanaheimr.V2G.Simulation.Timing;
using cloud.charging.open.protocols.ISO15118.EXI.Dispatch;

using Dc20         = cloud.charging.open.protocols.ISO15118_20.DC.Generated;
using Dc20Rational = cloud.charging.open.protocols.ISO15118_20.DC.RationalNumber;

namespace Vanaheimr.V2G.Simulation.StateMachines.Iso20
{
    /// <summary>EVCC-side DC hooks: charge-parameter discovery, CableCheck+PreCharge, one DC charge-loop iteration, WeldingDetection.</summary>
    public class Evcc20Dc(Stream stream, TimeProvider clock, IAsyncDelay pollDelay, TimeSpan perMessageTimeout)
        : Evcc20Base(stream, clock, pollDelay, perMessageTimeout)
    {
        // NOTE: uses the base class's PollDelay accessor, not the pollDelay parameter above directly,
        // to avoid capturing it twice (once here, once in the base primary constructor).
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);

        protected override PowerMode EnergyMode => PowerMode.Dc;

        protected override async Task RunChargeParameterDiscoveryAsync(CancellationToken ct)
        {
            var req = new Dc20.DC_ChargeParameterDiscoveryReq(SessionCtx.ToDcHeader(),
                new Dc20.DC_CPDReqEnergyTransferModeType(
                    EVMaximumChargePower: Rat(5_000, 1), EVMinimumChargePower: Rat(0),
                    EVMaximumChargeCurrent: Rat(200), EVMinimumChargeCurrent: Rat(0),
                    EVMaximumVoltage: Rat(500), EVMinimumVoltage: Rat(50), TargetSOC: 80));

            var (set, message) = await ExchangeRaw(MessageSet.Iso20DC,
                dest => Dc20.DcCodec.TryEncode(req, dest, out int n) ? n : throw EncodeFailed(), ct);
            Expect<Dc20.DC_ChargeParameterDiscoveryRes>(set, message, MessageSet.Iso20DC);
        }

        protected override async Task RunPreChargeSequenceAsync(CancellationToken ct)
        {
            var cableGuard = Ongoing("DC_CableCheck");
            while (true)
            {
                var (set, message) = await ExchangeRaw(MessageSet.Iso20DC,
                    dest => Dc20.DcCodec.TryEncode(new Dc20.DC_CableCheckReq(SessionCtx.ToDcHeader()), dest, out int n) ? n : throw EncodeFailed(), ct);
                var res = Expect<Dc20.DC_CableCheckRes>(set, message, MessageSet.Iso20DC);
                if (res.EVSEProcessing == Dc20.Processing.Finished) break;
                cableGuard.Tick();
                await PollDelay.Wait(PollInterval, ct);
            }

            var preChargeReq = new Dc20.DC_PreChargeReq(SessionCtx.ToDcHeader(), Dc20.Processing.Finished,
                EVPresentVoltage: Rat(0), EVTargetVoltage: Rat(400));
            var (preSet, preMessage) = await ExchangeRaw(MessageSet.Iso20DC,
                dest => Dc20.DcCodec.TryEncode(preChargeReq, dest, out int n) ? n : throw EncodeFailed(), ct);
            Expect<Dc20.DC_PreChargeRes>(preSet, preMessage, MessageSet.Iso20DC);
        }

        protected override async Task RunChargeLoopIterationAsync(CancellationToken ct)
        {
            // Asking in kind, the mirror of [V2G20-1600]: the request's control mode must be the one the
            // session negotiated. Dynamic states what the battery needs and what the car can take, and lets
            // the station choose the setpoint; Scheduled names the setpoint itself.
            Dc20.CLReqControlModeType controlMode = PreferDynamicControlMode
                ? new Dc20.Dynamic_DC_CLReqControlModeType(
                      DepartureTime:          DepartureTime,
                      EVTargetEnergyRequest:  Rat(30, 3),    // 30 kWh
                      EVMaximumEnergyRequest: Rat(60, 3),    // 60 kWh
                      EVMinimumEnergyRequest: Rat(10, 3),    // 10 kWh
                      EVMaximumChargePower:   Rat(50, 3),    // 50 kW
                      EVMinimumChargePower:   Rat(1,  3),    //  1 kW
                      EVMaximumChargeCurrent: Rat(125),
                      EVMaximumVoltage:       Rat(500),
                      EVMinimumVoltage:       Rat(200))
                : new Dc20.Scheduled_DC_CLReqControlModeType(
                      null, null, null, EVTargetCurrent: Rat(120), EVTargetVoltage: Rat(400),
                      null, null, null, null, null);

            var req = new Dc20.DC_ChargeLoopReq(SessionCtx.ToDcHeader(), DisplayParameters: null, MeterInfoRequested: false,
                EVPresentVoltage: Rat(400), CLReqControlMode: controlMode);

            var (set, message) = await ExchangeRaw(MessageSet.Iso20DC,
                dest => Dc20.DcCodec.TryEncode(req, dest, out int n) ? n : throw EncodeFailed(), ct);
            var response = Expect<Dc20.DC_ChargeLoopRes>(set, message, MessageSet.Iso20DC);

            // The EV's own voltage — it sent EVPresentVoltage above, and a DC vehicle really does
            // measure that at its own inlet — times the current the station reports. Half-borrowed on
            // purpose: -20 DC gives the vehicle no field for a current it measured itself, and
            // inventing one from EVTargetCurrent would be a *request* rather than a measurement, and
            // would not exist at all in Dynamic mode.
            Meter.Sample((double) Dc20Rational.ToDecimal(req.EVPresentVoltage),
                         (double) Dc20Rational.ToDecimal(response.EVSEPresentCurrent));
        }

        protected override async Task RunPostChargeSequenceAsync(CancellationToken ct)
        {
            var req = new Dc20.DC_WeldingDetectionReq(SessionCtx.ToDcHeader(), Dc20.Processing.Finished);
            var (set, message) = await ExchangeRaw(MessageSet.Iso20DC,
                dest => Dc20.DcCodec.TryEncode(req, dest, out int n) ? n : throw EncodeFailed(), ct);
            Expect<Dc20.DC_WeldingDetectionRes>(set, message, MessageSet.Iso20DC);
        }

        private static T Expect<T>(MessageSet actualSet, object message, MessageSet expectedSet)
        {
            if (actualSet != expectedSet || message is not T typed)
                throw new SessionAborted($"expected a {typeof(T).Name} on {expectedSet}, got {message.GetType().Name} on {actualSet}.");
            return typed;
        }

        private static Dc20.RationalNumberType Rat(short value, sbyte exponent = 0) => new(exponent, value);
        private static InvalidOperationException EncodeFailed() => new("EXI encode failed (buffer too small?).");
    }
}
