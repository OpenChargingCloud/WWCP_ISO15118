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

using cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated;
using cloud.charging.open.protocols.ISO15118.EXI.Dispatch;

using Ac20 = cloud.charging.open.protocols.ISO15118_20.AC.Generated;

namespace Vanaheimr.V2G.Simulation.StateMachines.Iso20
{
    /// <summary>
    /// -20 AC's diverging middle: AC-specific charge-parameter discovery and one AC charge-loop
    /// iteration. No CableCheck/PreCharge/WeldingDetection (DC-only). <c>AC.Generated</c> is aliased
    /// (<c>Ac20</c>) for the same reason as <see cref="Secc20Dc"/> — it redeclares
    /// <c>ResponseCode</c>/<c>Processing</c>/<c>RationalNumberType</c>/etc. as distinct CLR types.
    /// <para>
    /// Unsealed to match <see cref="Secc20Dc"/>, which never was: the difference was an accident, and a test
    /// that needs to watch what a station received (<c>Evcc20DynamicModeTests</c>) should not be able to do
    /// that for DC and not for AC.
    /// </para>
    /// </summary>
    public class Secc20Ac(TimeSpan sequenceTimeout, TimeProvider clock) : Secc20Base(sequenceTimeout, clock)
    {
        protected override bool HasPreChargeSequence => false;
        protected override bool HasPostChargeSequence => false;
        protected override IReadOnlyList<ushort> EnergyServiceIds => new ushort[] { 1, 5 };   // AC + AC_BPT

        protected override (MessageSet Set, object Response) HandleChargeParameterDiscovery(object request)
        {
            var req = (Ac20.AC_ChargeParameterDiscoveryReq)request;
            // Bidirectional (BPT) EV → advertise discharge power too; else the charge-only mode.
            Ac20.AC_CPDResEnergyTransferModeType mode = req.AC_CPDReqEnergyTransferMode is Ac20.BPT_AC_CPDReqEnergyTransferModeType
                ? new Ac20.BPT_AC_CPDResEnergyTransferModeType(
                    EVSEMaximumChargePower: Rat(2_200, exponent: 1), EVSEMaximumChargePower_L2: null, EVSEMaximumChargePower_L3: null,
                    EVSEMinimumChargePower: Rat(0), EVSEMinimumChargePower_L2: null, EVSEMinimumChargePower_L3: null,
                    EVSENominalFrequency: Rat(50), MaximumPowerAsymmetry: null, EVSEPowerRampLimitation: null,
                    EVSEPresentActivePower: null, EVSEPresentActivePower_L2: null, EVSEPresentActivePower_L3: null,
                    EVSEMaximumDischargePower: Rat(2_200, exponent: 1), EVSEMaximumDischargePower_L2: null, EVSEMaximumDischargePower_L3: null,
                    EVSEMinimumDischargePower: Rat(0), EVSEMinimumDischargePower_L2: null, EVSEMinimumDischargePower_L3: null)
                : new Ac20.AC_CPDResEnergyTransferModeType(
                    EVSEMaximumChargePower: Rat(2_200, exponent: 1), EVSEMaximumChargePower_L2: null, EVSEMaximumChargePower_L3: null,
                    EVSEMinimumChargePower: Rat(0), EVSEMinimumChargePower_L2: null, EVSEMinimumChargePower_L3: null,
                    EVSENominalFrequency: Rat(50), MaximumPowerAsymmetry: null, EVSEPowerRampLimitation: null,
                    EVSEPresentActivePower: null, EVSEPresentActivePower_L2: null, EVSEPresentActivePower_L3: null);
            return (MessageSet.Iso20AC, new Ac20.AC_ChargeParameterDiscoveryRes(SessionCtx.ToAcHeader(), Ac20.ResponseCode.OK, mode));
        }

        protected override (MessageSet Set, object Response) HandleChargeLoop(object request)
        {
            var req = (Ac20.AC_ChargeLoopReq)request;
            // Answer strictly in kind: the res control mode must be the same variant (Scheduled/Dynamic,
            // BPT or not) as the req's ([V2G20-1600]). Dynamic res carries a *mandatory* EVSETargetActivePower
            // (in dynamic mode the SECC dictates the operating point). BPT_Dynamic derives from Dynamic —
            // match the BPT subtypes first.
            Ac20.CLResControlModeType clRes = req.CLReqControlMode switch
            {
                Ac20.BPT_Dynamic_AC_CLReqControlModeType => new Ac20.BPT_Dynamic_AC_CLResControlModeType(
                    DepartureTime: null, MinimumSOC: null, TargetSOC: null, AckMaxDelay: null,
                    EVSETargetActivePower: Rat(2_200, exponent: 1), EVSETargetActivePower_L2: null, EVSETargetActivePower_L3: null,
                    EVSETargetReactivePower: null, EVSETargetReactivePower_L2: null, EVSETargetReactivePower_L3: null,
                    EVSEPresentActivePower: null, EVSEPresentActivePower_L2: null, EVSEPresentActivePower_L3: null),
                Ac20.Dynamic_AC_CLReqControlModeType => new Ac20.Dynamic_AC_CLResControlModeType(
                    DepartureTime: null, MinimumSOC: null, TargetSOC: null, AckMaxDelay: null,
                    EVSETargetActivePower: Rat(2_200, exponent: 1), EVSETargetActivePower_L2: null, EVSETargetActivePower_L3: null,
                    EVSETargetReactivePower: null, EVSETargetReactivePower_L2: null, EVSETargetReactivePower_L3: null,
                    EVSEPresentActivePower: null, EVSEPresentActivePower_L2: null, EVSEPresentActivePower_L3: null),
                Ac20.BPT_Scheduled_AC_CLReqControlModeType => new Ac20.BPT_Scheduled_AC_CLResControlModeType(null, null, null, null, null, null, null, null, null),
                _ => new Ac20.Scheduled_AC_CLResControlModeType(null, null, null, null, null, null, null, null, null),
            };
            // 22 kW: the EVSETargetActivePower announced below, and the EVPresentActivePower the
            // vehicle reports — AC is the one case where both sides name the same figure outright.
            Deliver(22_000);

            var res = new Ac20.AC_ChargeLoopRes(SessionCtx.ToAcHeader(), Ac20.ResponseCode.OK,
                // Service renegotiation is requested via the (otherwise absent) EVSEStatus ([V2G20-1477]).
                EVSEStatus: SignalRenegotiationOnce()
                    ? new Ac20.EVSEStatusType(NotificationMaxDelay: 0, Ac20.EvseNotification.ServiceRenegotiation)
                    : null,
                MeterInfo: MeterReading() is { } m
                    ? new Ac20.MeterInfoType(m.Id, m.Wh, null, null, null, m.Signature, null, m.Timestamp)
                    : null, Receipt: null, EVSETargetFrequency: null,
                CLResControlMode: clRes);
            return (MessageSet.Iso20AC, res);
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
                case Ac20.AC_ChargeParameterDiscoveryRes m:  ok = Ac20.AcCodec.TryEncode(m, dest, out n); break;
                case Ac20.AC_ChargeLoopRes m:                ok = Ac20.AcCodec.TryEncode(m, dest, out n); break;
                default: throw new InvalidOperationException($"no encoder for {message.GetType().Name}");
            }
            if (!ok) throw new InvalidOperationException("EXI encode failed (buffer too small?).");
            return n;
        }

        private static Ac20.RationalNumberType Rat(short value, sbyte exponent = 0) => new(exponent, value);
    }
}
