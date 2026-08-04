/*
 * Copyright (c) 2021-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP ISO/IEC 15118 <https://github.com/OpenChargingCloud/WWCP_ISO15118>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Security.Cryptography;

using cloud.charging.open.protocols.ISO15118_2;
using cloud.charging.open.protocols.ISO15118_2.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Simulation
{
    /// <summary>
    /// The charge point (SECC) — a <b>sequence-guarded</b> responder. It advances through the ISO
    /// 15118-2 charging state machine and only accepts the request expected next; anything out of order
    /// is rejected (a real SECC answers with <c>ResponseCode.FAILED_SequenceError</c> and closes — here
    /// we raise <see cref="SessionAborted"/>). It also enforces the SECC <i>sequence timeout</i>: if the
    /// EV goes quiet mid-session for too long, the SECC tears the session down.
    /// </summary>
    public sealed class Secc(ChargingMode mode, TimeSpan sequenceTimeout, TimeSpan authDelay = default)
    {
        private enum Phase
        {
            SessionSetup, ServiceDiscovery, PaymentSelection, Authorization, ChargeParams,
            CableCheck, PreCharge, PowerOn, Charging, WeldingDetection, SessionStop, Done,
        }

        private Phase _phase = Phase.SessionSetup;
        private byte[] _sessionId = new byte[8];
        private DateTimeOffset _lastSeen = DateTimeOffset.UtcNow;

        public V2G_Message Handle(V2G_Message request)
        {
            var now = DateTimeOffset.UtcNow;
            if (_phase is not Phase.SessionSetup && now - _lastSeen > sequenceTimeout)
                throw new SessionAborted($"SECC sequence timeout: EV silent for > {sequenceTimeout.TotalSeconds:0}s");
            _lastSeen = now;

            // Simulate a slow authorisation backend to exercise the EV-side message timeout.
            if (authDelay > TimeSpan.Zero && request.Body.BodyElement is AuthorizationReqType)
                Thread.Sleep(authDelay);

            var (body, next) = Dispatch(request.Body.BodyElement!);
            _phase = next;
            return new V2G_Message(new MessageHeaderType(_sessionId, Notification: null, Signature: null), new BodyType(body));
        }

        // The guard: only the (phase, request) pairs below are legal. The wildcard arm rejects the rest.
        private (BodyBaseType Body, Phase Next) Dispatch(BodyBaseType req) => (_phase, req) switch
        {
            (Phase.SessionSetup, SessionSetupReqType) =>
                (NewSession(), Phase.ServiceDiscovery),

            (Phase.ServiceDiscovery, ServiceDiscoveryReqType) =>
                (Discovery(), Phase.PaymentSelection),

            (Phase.PaymentSelection, PaymentServiceSelectionReqType) =>
                (new PaymentServiceSelectionResType(ResponseCode.OK), Phase.Authorization),

            (Phase.Authorization, AuthorizationReqType) =>
                (new AuthorizationResType(ResponseCode.OK, EVSEProcessing.Finished), Phase.ChargeParams),

            (Phase.ChargeParams, ChargeParameterDiscoveryReqType) =>
                (ChargeParams(), mode == ChargingMode.Dc ? Phase.CableCheck : Phase.PowerOn),

            // ── DC-only pre-charge sequence ──
            (Phase.CableCheck, CableCheckReqType) =>
                (new CableCheckResType(ResponseCode.OK, DcEvseStatus(), EVSEProcessing.Finished), Phase.PreCharge),
            (Phase.PreCharge, PreChargeReqType) =>
                (new PreChargeResType(ResponseCode.OK, DcEvseStatus(), Volt(390)), Phase.PowerOn),

            (Phase.PowerOn, PowerDeliveryReqType { ChargeProgress: ChargeProgress.Start }) =>
                (PowerOn(), Phase.Charging),

            // ── charging loop (mode-specific request) ──
            (Phase.Charging, CurrentDemandReqType)  when mode == ChargingMode.Dc =>
                (CurrentDemand(), Phase.Charging),
            (Phase.Charging, ChargingStatusReqType) when mode == ChargingMode.Ac =>
                (ChargingStatus(), Phase.Charging),
            (Phase.Charging, PowerDeliveryReqType { ChargeProgress: ChargeProgress.Stop }) =>
                (PowerOff(), mode == ChargingMode.Dc ? Phase.WeldingDetection : Phase.SessionStop),

            (Phase.WeldingDetection, WeldingDetectionReqType) =>
                (new WeldingDetectionResType(ResponseCode.OK, DcEvseStatus(), Volt(5)), Phase.SessionStop),

            (Phase.SessionStop, SessionStopReqType) =>
                (new SessionStopResType(ResponseCode.OK), Phase.Done),

            _ => throw new SessionAborted(
                $"SECC sequence guard: {req.GetType().Name.Replace("Type", "")} not allowed in phase {_phase} " +
                "(would be ResponseCode.FAILED_SequenceError)"),
        };

        // ── response builders ─────────────────────────────────────────────────
        private BodyBaseType NewSession()
        {
            _sessionId = RandomNumberGenerator.GetBytes(8);
            return new SessionSetupResType(ResponseCode.OK_NewSessionEstablished, "DE*ABC*E1", 1_600_000_000L);
        }

        private BodyBaseType Discovery() =>
            new ServiceDiscoveryResType(ResponseCode.OK,
                new PaymentOptionListType(new[] { PaymentOption.ExternalPayment }),
                new ChargeServiceType(ServiceID: 1, ServiceName: mode == ChargingMode.Dc ? "DC" : "AC",
                    ServiceCategory.EVCharging, ServiceScope: null, FreeService: true,
                    new SupportedEnergyTransferModeType(new[]
                    {
                        mode == ChargingMode.Dc ? EnergyTransferMode.DC_extended : EnergyTransferMode.AC_three_phase_core,
                    })),
                ServiceList: null);

        private BodyBaseType ChargeParams() =>
            mode == ChargingMode.Dc
                ? new ChargeParameterDiscoveryResType(ResponseCode.OK, EVSEProcessing.Finished, SASchedules: null,
                    new DC_EVSEChargeParameterType(DcEvseStatus(),
                        EVSEMaximumCurrentLimit: Amp(200), EVSEMaximumPowerLimit: Watt(150_000),
                        EVSEMaximumVoltageLimit: Volt(500), EVSEMinimumCurrentLimit: Amp(0),
                        EVSEMinimumVoltageLimit: Volt(200), EVSECurrentRegulationTolerance: null,
                        EVSEPeakCurrentRipple: Amp(1), EVSEEnergyToBeDelivered: null))
                : new ChargeParameterDiscoveryResType(ResponseCode.OK, EVSEProcessing.Finished, SASchedules: null,
                    new AC_EVSEChargeParameterType(AcEvseStatus(),
                        EVSENominalVoltage: Volt(230), EVSEMaxCurrent: Amp(32)));

        private BodyBaseType PowerOn() =>
            mode == ChargingMode.Dc
                ? new PowerDeliveryResType(ResponseCode.OK, DcEvseStatus())
                : new PowerDeliveryResType(ResponseCode.OK, AcEvseStatus());

        private BodyBaseType PowerOff() => PowerOn();

        private BodyBaseType CurrentDemand() =>
            new CurrentDemandResType(ResponseCode.OK, DcEvseStatus(),
                EVSEPresentVoltage: Volt(400), EVSEPresentCurrent: Amp(120),
                EVSECurrentLimitAchieved: false, EVSEVoltageLimitAchieved: false, EVSEPowerLimitAchieved: false,
                EVSEMaximumVoltageLimit: null, EVSEMaximumCurrentLimit: null, EVSEMaximumPowerLimit: null,
                EVSEID: "DE*ABC*E1", SAScheduleTupleID: 1, MeterInfo: null, ReceiptRequired: null);

        private BodyBaseType ChargingStatus() =>
            new ChargingStatusResType(ResponseCode.OK, "DE*ABC*E1", SAScheduleTupleID: 1,
                EVSEMaxCurrent: null, MeterInfo: null, ReceiptRequired: null, AcEvseStatus());

        private static DC_EVSEStatusType DcEvseStatus() =>
            new(NotificationMaxDelay: 0, EVSENotification.None, EVSEIsolationStatus: null, DC_EVSEStatusCode.EVSE_Ready);
        private static AC_EVSEStatusType AcEvseStatus() =>
            new(NotificationMaxDelay: 0, EVSENotification.None, RCD: false);

        private static PhysicalValueType Volt(short v) => new(0, UnitSymbol.V, v);
        private static PhysicalValueType Amp(short a)  => new(0, UnitSymbol.A, a);
        private static PhysicalValueType Watt(int w)   => PhysicalValue.Of(w, UnitSymbol.W);
    }
}
