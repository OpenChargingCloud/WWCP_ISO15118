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

using cloud.charging.open.protocols.ISO15118_2;
using cloud.charging.open.protocols.ISO15118_2.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.ChargingSimulation
{
    /// <summary>
    /// The vehicle (EVCC) — it drives the session. Each step below is one request/response exchange over
    /// EXI (see the sequence diagram). It carries the SECC-assigned SessionID in every header and applies
    /// a per-message timeout budget (the ISO 15118-2 performance timeout, simplified).
    /// </summary>
    public sealed class Evcc(Wire wire, ChargingMode mode, bool breakSequence = false)
    {
        // ISO 15118-2 uses ~2 s for most performance timeouts and a longer sequence budget; simplified here.
        private static readonly TimeSpan MsgTimeout = TimeSpan.FromSeconds(2);

        private byte[] _sid = new byte[8];   // 0 until SessionSetupRes assigns one

        public void Run()
        {
            // ── SETUP ──────────────────────────────────────────────────────────
            Send<SessionSetupResType>(new SessionSetupReqType(EVCCID: new byte[] { 0xAB, 0xCD, 0xEF, 0x01, 0x02, 0x03 }));
            Send<ServiceDiscoveryResType>(new ServiceDiscoveryReqType(ServiceScope: null, ServiceCategory: null));
            Send<PaymentServiceSelectionResType>(new PaymentServiceSelectionReqType(PaymentOption.ExternalPayment,
                new SelectedServiceListType(new[] { new SelectedServiceType(ServiceID: 1, ParameterSetID: null) })));

            if (breakSequence)
            {
                // Deliberately skip authorisation and jump straight to charging — the SECC must reject it.
                Console.WriteLine("  [EV sends PowerDeliveryReq out of order to trip the sequence guard]");
                Send<PowerDeliveryResType>(PowerDelivery(ChargeProgress.Start));
                return;
            }

            // ── AUTH (loop until authorised) ───────────────────────────────────
            while (Send<AuthorizationResType>(new AuthorizationReqType(Id: null, GenChallenge: null))
                       .EVSEProcessing != EVSEProcessing.Finished)
                Thread.Sleep(50);

            // ── CHARGE PARAMETERS (+ DC cable check / pre-charge) ──────────────
            while (Send<ChargeParameterDiscoveryResType>(ChargeParameterDiscovery())
                       .EVSEProcessing != EVSEProcessing.Finished)
                Thread.Sleep(50);

            if (mode == ChargingMode.Dc)
            {
                while (Send<CableCheckResType>(new CableCheckReqType(EvStatus()))
                           .EVSEProcessing != EVSEProcessing.Finished)
                    Thread.Sleep(50);
                Send<PreChargeResType>(new PreChargeReqType(EvStatus(),
                    EVTargetVoltage: Volt(400), EVTargetCurrent: Amp(2)));
            }

            // ── CHARGE ─────────────────────────────────────────────────────────
            Send<PowerDeliveryResType>(PowerDelivery(ChargeProgress.Start));

            for (int cycle = 0; cycle < 3; cycle++)                    // a few charging-loop iterations
            {
                if (mode == ChargingMode.Dc)
                    Send<CurrentDemandResType>(CurrentDemand());
                else
                    Send<ChargingStatusResType>(new ChargingStatusReqType());
                Thread.Sleep(50);
            }

            Send<PowerDeliveryResType>(PowerDelivery(ChargeProgress.Stop));

            // ── STOP ───────────────────────────────────────────────────────────
            if (mode == ChargingMode.Dc)
                Send<WeldingDetectionResType>(new WeldingDetectionReqType(EvStatus()));
            Send<SessionStopResType>(new SessionStopReqType(ChargingSession.Terminate));
        }

        private T Send<T>(BodyBaseType requestBody) where T : BodyBaseType
        {
            var header = new MessageHeaderType(_sid, Notification: null, Signature: null);
            var reply = wire.Exchange(new V2G_Message(header, new BodyType(requestBody)), MsgTimeout);
            _sid = reply.Header.SessionID;                            // adopt the SECC session id
            return (T)reply.Body.BodyElement!;
        }

        // ── request builders ──────────────────────────────────────────────────
        private static PowerDeliveryReqType PowerDelivery(ChargeProgress progress) =>
            new(progress, SAScheduleTupleID: 1, ChargingProfile: null, EVPowerDeliveryParameter: null);

        private ChargeParameterDiscoveryReqType ChargeParameterDiscovery() =>
            mode == ChargingMode.Dc
                ? new ChargeParameterDiscoveryReqType(MaxEntriesSAScheduleTuple: null, EnergyTransferMode.DC_extended,
                    new DC_EVChargeParameterType(DepartureTime: null, EvStatus(),
                        EVMaximumCurrentLimit: Amp(200), EVMaximumPowerLimit: null, EVMaximumVoltageLimit: Volt(500),
                        EVEnergyCapacity: null, EVEnergyRequest: null, FullSOC: 100, BulkSOC: 80))
                : new ChargeParameterDiscoveryReqType(MaxEntriesSAScheduleTuple: null, EnergyTransferMode.AC_three_phase_core,
                    new AC_EVChargeParameterType(DepartureTime: null,
                        EAmount: PhysicalValue.Of(22_000, UnitSymbol.Wh), EVMaxVoltage: Volt(400),
                        EVMaxCurrent: Amp(32), EVMinCurrent: Amp(6)));

        private CurrentDemandReqType CurrentDemand() =>
            new(EvStatus(), EVTargetCurrent: Amp(120),
                EVMaximumVoltageLimit: null, EVMaximumCurrentLimit: null, EVMaximumPowerLimit: null,
                BulkChargingComplete: null, ChargingComplete: false,
                RemainingTimeToFullSoC: null, RemainingTimeToBulkSoC: null,
                EVTargetVoltage: Volt(400));

        private static DC_EVStatusType EvStatus() => new(EVReady: true, DC_EVErrorCode.NO_ERROR, EVRESSSOC: 50);
        private static PhysicalValueType Volt(short v) => new(0, UnitSymbol.V, v);
        private static PhysicalValueType Amp(short a)  => new(0, UnitSymbol.A, a);
    }
}
