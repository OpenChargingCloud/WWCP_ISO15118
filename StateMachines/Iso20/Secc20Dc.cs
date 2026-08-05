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

using cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated;
using cloud.charging.open.protocols.ISO15118.EXI.Dispatch;

using Dc20 = cloud.charging.open.protocols.ISO15118_20.DC.Generated;

namespace Vanaheimr.V2G.Simulation.StateMachines.Iso20
{
    /// <summary>
    /// -20 DC's diverging middle: DC-specific charge-parameter discovery, the CableCheck/PreCharge
    /// sequence, one DC charge-loop iteration, and WeldingDetection. <c>DC.Generated</c> is aliased
    /// (<c>Dc20</c>) rather than imported unqualified because it redeclares <c>ResponseCode</c>/
    /// <c>Processing</c>/<c>RationalNumberType</c>/<c>MessageHeaderType</c>/<c>EVSEStatusType</c> as
    /// distinct CLR types from <c>CommonMessages.Generated</c> — both would otherwise be ambiguous.
    /// </summary>
    public class Secc20Dc(TimeSpan sequenceTimeout, TimeProvider clock) : Secc20Base(sequenceTimeout, clock)
    {
        protected override bool HasPreChargeSequence => true;
        protected override bool HasPostChargeSequence => true;
        protected override IReadOnlyList<ushort> EnergyServiceIds => new ushort[] { 2, 6 };   // DC + DC_BPT

        /// <summary>Classifies the DC poll requests so the base can self-loop CableCheck/PreCharge/WeldingDetection
        /// until the EV sends the next-phase message (the first DC_PreChargeReq, PowerDeliveryReq, SessionStopReq).</summary>
        protected override bool IsPollFor(Phase20 phase, object request) => phase switch
        {
            Phase20.CableCheck       => request is Dc20.DC_CableCheckReq,
            Phase20.PreCharge        => request is Dc20.DC_PreChargeReq,
            Phase20.WeldingDetection => request is Dc20.DC_WeldingDetectionReq,
            _                        => base.IsPollFor(phase, request),   // PowerOn (PowerDeliveryReq start) handled by the base
        };

        /// <summary>EVSE charge/discharge power limit. Virtual so the MCS profile can raise it to
        /// megawatt scale without duplicating the whole discovery handler (see <see cref="Secc20Mcs"/>).</summary>
        protected virtual Dc20.RationalNumberType MaxPower   => Rat(5_000, exponent: 1);   //  50 kW
        protected virtual Dc20.RationalNumberType MaxCurrent => Rat(200);                  // 200 A
        protected virtual Dc20.RationalNumberType MaxVoltage => Rat(500);                  // 500 V
        protected virtual Dc20.RationalNumberType MinVoltage => Rat(50);

        /// <summary>
        /// Answers DC charge-parameter discovery in kind — a BPT request gets discharge limits back, a
        /// charge-only one does not — after checking that the direction the EV just declared is the one the
        /// service it selected actually carries.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The check is the newer half, and it came from being on the other end of it.</b> Answering in
        /// kind alone is not enough: it makes the EV's charge-parameter message self-consistent and says
        /// nothing about whether it agrees with ServiceSelection two exchanges earlier. So a session could
        /// negotiate DC_BPT or MCS_BPT and then charge one way under it, and this station would serve it to
        /// completion. everest-core 2026.02.1 refuses exactly that session with
        /// <c>FAILED_WrongChargeParameter</c> — it refused ours
        /// (<c>docs/interop-runs/2026-08-05-everest-mcs-bpt/</c>) — and it is right: ISO 15118-20 carries the
        /// direction in the polymorphic type, so the selected service binds every message that follows.
        /// </para>
        /// <para>
        /// The refusal still carries a charge-parameter mode because the schema makes it mandatory, and it is
        /// built <i>in kind with the request</i> — which is what their station does too (its refusal frame is
        /// the plain type, shorter than a BPT one, in answer to our plain request). On a refusal the two
        /// readings of "in kind" point different ways; matching the request is the one an implementation that
        /// has actually sent this response chose, and the EV aborts on the code without reading the mode.
        /// </para>
        /// <para>
        /// Scoped to charge-parameter discovery, which is where the coupling was witnessed being enforced.
        /// The charge loop's control mode is checked only for the Scheduled/Dynamic axis
        /// (see <see cref="HandleChargeLoop"/>), and a session whose loop control mode contradicted its
        /// charge parameters would get past this — deliberately not guessed at without a counterparty
        /// that says so.
        /// </para>
        /// </remarks>
        protected override (MessageSet Set, object Response) HandleChargeParameterDiscovery(object request)
        {
            var req = (Dc20.DC_ChargeParameterDiscoveryReq)request;
            bool bidirectionalRequest = req.DC_CPDReqEnergyTransferMode is Dc20.BPT_DC_CPDReqEnergyTransferModeType;

            // Bidirectional (BPT) EV → respond with discharge limits too; else the charge-only mode.
            Dc20.DC_CPDResEnergyTransferModeType mode = bidirectionalRequest
                ? new Dc20.BPT_DC_CPDResEnergyTransferModeType(
                    EVSEMaximumChargePower: MaxPower, EVSEMinimumChargePower: Rat(0),
                    EVSEMaximumChargeCurrent: MaxCurrent, EVSEMinimumChargeCurrent: Rat(0),
                    EVSEMaximumVoltage: MaxVoltage, EVSEMinimumVoltage: MinVoltage, EVSEPowerRampLimitation: null,
                    EVSEMaximumDischargePower: MaxPower, EVSEMinimumDischargePower: Rat(0),
                    EVSEMaximumDischargeCurrent: MaxCurrent, EVSEMinimumDischargeCurrent: Rat(0))
                : new Dc20.DC_CPDResEnergyTransferModeType(
                    EVSEMaximumChargePower: MaxPower, EVSEMinimumChargePower: Rat(0),
                    EVSEMaximumChargeCurrent: MaxCurrent, EVSEMinimumChargeCurrent: Rat(0),
                    EVSEMaximumVoltage: MaxVoltage, EVSEMinimumVoltage: MinVoltage, EVSEPowerRampLimitation: null);

            var responseCode = bidirectionalRequest == BidirectionalServiceSelected
                                   ? Dc20.ResponseCode.OK
                                   : Dc20.ResponseCode.FAILED_WrongChargeParameter;

            return (MessageSet.Iso20DC, new Dc20.DC_ChargeParameterDiscoveryRes(SessionCtx.ToDcHeader(), responseCode, mode));
        }

        protected override (MessageSet Set, object Response) HandleCableCheck(object request)
        {
            var req = (Dc20.DC_CableCheckReq)request;
            return (MessageSet.Iso20DC, new Dc20.DC_CableCheckRes(SessionCtx.ToDcHeader(), Dc20.ResponseCode.OK, Dc20.Processing.Finished));
        }

        protected override (MessageSet Set, object Response) HandlePreCharge(object request)
        {
            var req = (Dc20.DC_PreChargeReq)request;
            return (MessageSet.Iso20DC, new Dc20.DC_PreChargeRes(SessionCtx.ToDcHeader(), Dc20.ResponseCode.OK, Rat(390)));
        }

        protected override (MessageSet Set, object Response) HandleChargeLoop(object request)
        {
            var req = (Dc20.DC_ChargeLoopReq)request;
            // Answer strictly in kind: the res control mode must be the same variant (Scheduled/Dynamic,
            // BPT or not) as the req's ([V2G20-1600]). The Dynamic res types carry *mandatory* EVSE limits
            // (in dynamic mode the SECC dictates the operating point); Scheduled res fields are all optional.
            // BPT_Dynamic derives from Dynamic — match the BPT subtypes first.
            Dc20.CLResControlModeType clRes = req.CLReqControlMode switch
            {
                Dc20.BPT_Dynamic_DC_CLReqControlModeType => new Dc20.BPT_Dynamic_DC_CLResControlModeType(
                    DepartureTime: null, MinimumSOC: null, TargetSOC: null, AckMaxDelay: null,
                    EVSEMaximumChargePower: Rat(5_000, exponent: 1), EVSEMinimumChargePower: Rat(0),
                    EVSEMaximumChargeCurrent: Rat(200), EVSEMaximumVoltage: Rat(500),
                    EVSEMaximumDischargePower: Rat(5_000, exponent: 1), EVSEMinimumDischargePower: Rat(0),
                    EVSEMaximumDischargeCurrent: Rat(200), EVSEMinimumVoltage: Rat(50)),
                Dc20.Dynamic_DC_CLReqControlModeType => new Dc20.Dynamic_DC_CLResControlModeType(
                    DepartureTime: null, MinimumSOC: null, TargetSOC: null, AckMaxDelay: null,
                    EVSEMaximumChargePower: Rat(5_000, exponent: 1), EVSEMinimumChargePower: Rat(0),
                    EVSEMaximumChargeCurrent: Rat(200), EVSEMaximumVoltage: Rat(500)),
                Dc20.BPT_Scheduled_DC_CLReqControlModeType => new Dc20.BPT_Scheduled_DC_CLResControlModeType(null, null, null, null, null, null, null, null),
                _ => new Dc20.Scheduled_DC_CLResControlModeType(null, null, null, null),
            };
            Deliver(400d * 120d);   // the EVSEPresentVoltage x EVSEPresentCurrent announced below

            var res = new Dc20.DC_ChargeLoopRes(SessionCtx.ToDcHeader(), Dc20.ResponseCode.OK,
                // Service renegotiation is requested via the (otherwise absent) EVSEStatus ([V2G20-1477]);
                // a Josev EVCC reacts with PowerDelivery(Stop) + SessionStopReq(ServiceRenegotiation).
                EVSEStatus: SignalRenegotiationOnce()
                    ? new Dc20.EVSEStatusType(NotificationMaxDelay: 0, Dc20.EvseNotification.ServiceRenegotiation)
                    : null,
                MeterInfo: MeterReading() is { } m
                    ? new Dc20.MeterInfoType(m.Id, m.Wh, null, null, null, m.Signature, null, m.Timestamp)
                    : null, Receipt: null,
                EVSEPresentCurrent: Rat(120), EVSEPresentVoltage: Rat(400),
                EVSEPowerLimitAchieved: false, EVSECurrentLimitAchieved: false, EVSEVoltageLimitAchieved: false,
                CLResControlMode: clRes);
            return (MessageSet.Iso20DC, res);
        }

        protected override (MessageSet Set, object Response) HandleWeldingDetection(object request)
        {
            var req = (Dc20.DC_WeldingDetectionReq)request;
            return (MessageSet.Iso20DC, new Dc20.DC_WeldingDetectionRes(SessionCtx.ToDcHeader(), Dc20.ResponseCode.OK, Rat(5)));
        }

        protected override int EncodeAny(MessageSet set, object message, byte[] dest)
        {
            bool ok; int n;
            switch (message)
            {
                case SessionSetupRes m:                    ok = m.TryEncode(dest, out n); break;
                case AuthorizationSetupRes m:               ok = m.TryEncode(dest, out n); break;
                case AuthorizationRes m:                    ok = m.TryEncode(dest, out n); break;
                case CertificateInstallationRes m:          ok = m.TryEncode(dest, out n); break;
                case ServiceDiscoveryRes m:                 ok = m.TryEncode(dest, out n); break;
                case ServiceDetailRes m:                    ok = m.TryEncode(dest, out n); break;
                case ServiceSelectionRes m:                 ok = m.TryEncode(dest, out n); break;
                case ScheduleExchangeRes m:                 ok = m.TryEncode(dest, out n); break;
                case PowerDeliveryRes m:                    ok = m.TryEncode(dest, out n); break;
                case SessionStopRes m:                      ok = m.TryEncode(dest, out n); break;
                case Dc20.DC_ChargeParameterDiscoveryRes m:  ok = Dc20.DcCodec.TryEncode(m, dest, out n); break;
                case Dc20.DC_CableCheckRes m:                ok = Dc20.DcCodec.TryEncode(m, dest, out n); break;
                case Dc20.DC_PreChargeRes m:                 ok = Dc20.DcCodec.TryEncode(m, dest, out n); break;
                case Dc20.DC_ChargeLoopRes m:                ok = Dc20.DcCodec.TryEncode(m, dest, out n); break;
                case Dc20.DC_WeldingDetectionRes m:          ok = Dc20.DcCodec.TryEncode(m, dest, out n); break;
                default: throw new InvalidOperationException($"no encoder for {message.GetType().Name}");
            }
            if (!ok) throw new InvalidOperationException("EXI encode failed (buffer too small?).");
            return n;
        }

        private static Dc20.RationalNumberType Rat(short value, sbyte exponent = 0) => new(exponent, value);
    }
}
