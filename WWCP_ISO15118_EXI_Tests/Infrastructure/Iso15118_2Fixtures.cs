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

using System.Linq;

using cloud.charging.open.protocols.ISO15118_2;
using cloud.charging.open.protocols.ISO15118_2.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure
{
    /// <summary>
    /// The fixed ISO 15118-2 messages shared by the round-trip and the cbV2G byte-diff tests. Each is
    /// keyed by the same name used in <c>Vectors/Iso15118_2.vectors.json</c> and in the reference tool
    /// <c>tools/cbv2g-ref/main_iso2.c</c>, so all three stay in lock-step. Every message uses an
    /// all-zero 8-byte SessionID and an otherwise empty header.
    /// </summary>
    public static class Iso15118_2Fixtures
    {
        private static MessageHeaderType Header() =>
            new(SessionID: new byte[8], Notification: null, Signature: null);

        private static byte[] Ramp(int n) => Enumerable.Range(1, n).Select(i => (byte)i).ToArray();
        private static byte[] Ramp(int start, int n) => Enumerable.Range(start, n).Select(i => (byte)i).ToArray();

        // ---- certificate message helpers (mirror tools/cbv2g-ref/main_iso2.c) ----
        private static CertificateChainType Chain(bool withSub) =>
            new(Id: null, Certificate: new byte[] { 0x30, 0x82, 0x01, 0x02 },
                SubCertificates: withSub
                    ? new SubCertificatesType(new[] { new byte[] { 0x30, 0x82, 0x03, 0x04 } })
                    : null);

        private static ListOfRootCertificateIDsType RootCerts() =>
            new(new[] { new X509IssuerSerialType("CN=Root CA", 12345) });

        /// <summary>A header carrying a full signature: SignedInfo over a 32-byte digest plus a 64-byte
        /// (r‖s) SignatureValue, KeyInfo/Object absent — the ISO 15118-2 signed-message shape.</summary>
        private static MessageHeaderType SignedHeader() =>
            new(SessionID: new byte[8], Notification: null,
                Signature: new SignatureType(
                    Id: null,
                    SignedInfo: V2GSignature.BuildSignedInfo("ID1", Ramp(32)),
                    SignatureValue: new SignatureValueType(Id: null, Value: Ramp(64)),
                    KeyInfo: null,
                    Object: null));

        public static readonly IReadOnlyDictionary<string, V2G_Message> ByName = new Dictionary<string, V2G_Message>
        {
            ["SessionSetupReq"] = new V2G_Message(Header(),
                new BodyType(new SessionSetupReqType(EVCCID: new byte[] { 0xAB, 0xCD, 0xEF, 0x01, 0x02, 0x03 }))),

            ["SessionSetupRes_ts"] = new V2G_Message(Header(),
                new BodyType(new SessionSetupResType(ResponseCode.OK_NewSessionEstablished, "DE*ABC*E12345*1", 1_600_000_000L))),

            ["SessionSetupRes_nots"] = new V2G_Message(Header(),
                new BodyType(new SessionSetupResType(ResponseCode.OK, "EVSE1", EVSETimeStamp: null))),

            ["ServiceDiscoveryReq_absent"] = new V2G_Message(Header(),
                new BodyType(new ServiceDiscoveryReqType(ServiceScope: null, ServiceCategory: null))),

            ["ServiceDiscoveryReq_present"] = new V2G_Message(Header(),
                new BodyType(new ServiceDiscoveryReqType("urn:scope:test", ServiceCategory.EVCharging))),

            ["ServiceDiscoveryRes"] = new V2G_Message(Header(),
                new BodyType(new ServiceDiscoveryResType(
                    ResponseCode.OK,
                    new PaymentOptionListType(new[] { PaymentOption.Contract, PaymentOption.ExternalPayment }),
                    new ChargeServiceType(ServiceID: 1, ServiceName: "AC", ServiceCategory.EVCharging,
                        ServiceScope: null, FreeService: true,
                        new SupportedEnergyTransferModeType(new[] { EnergyTransferMode.AC_single_phase_core, EnergyTransferMode.AC_three_phase_core })),
                    ServiceList: null))),

            ["AuthorizationReq_Signed"] = new V2G_Message(SignedHeader(),
                new BodyType(new AuthorizationReqType(Id: null, GenChallenge: Ramp(16)))),

            ["CertificateInstallationReq"] = new V2G_Message(Header(),
                new BodyType(new CertificateInstallationReqType(
                    Id: "ID1",
                    OEMProvisioningCert: new byte[] { 0x30, 0x82, 0x02, 0x03 },
                    ListOfRootCertificateIDs: RootCerts()))),

            ["CertificateInstallationRes"] = new V2G_Message(Header(),
                new BodyType(new CertificateInstallationResType(
                    ResponseCode.OK,
                    SAProvisioningCertificateChain: Chain(withSub: false),
                    ContractSignatureCertChain: Chain(withSub: true),
                    ContractSignatureEncryptedPrivateKey: new ContractSignatureEncryptedPrivateKeyType("ID2", Ramp(0xA0, 16)),
                    DHpublickey: new DiffieHellmanPublickeyType("ID3", Ramp(0xB0, 8)),
                    EMAID: new EMAIDType("ID4", "DEAAA0001234567")))),

            ["CertificateUpdateReq"] = new V2G_Message(Header(),
                new BodyType(new CertificateUpdateReqType(
                    Id: "ID1",
                    ContractSignatureCertChain: Chain(withSub: false),
                    EMAID: "DEAAA0001234567",
                    ListOfRootCertificateIDs: RootCerts()))),

            ["CertificateUpdateRes"] = new V2G_Message(Header(),
                new BodyType(new CertificateUpdateResType(
                    ResponseCode.OK,
                    SAProvisioningCertificateChain: Chain(withSub: false),
                    ContractSignatureCertChain: Chain(withSub: true),
                    ContractSignatureEncryptedPrivateKey: new ContractSignatureEncryptedPrivateKeyType("ID2", Ramp(0xA0, 16)),
                    DHpublickey: new DiffieHellmanPublickeyType("ID3", Ramp(0xB0, 8)),
                    EMAID: new EMAIDType("ID4", "DEAAA0001234567"),
                    RetryCounter: 3))),

            ["SessionStopReq"] = new V2G_Message(Header(),
                new BodyType(new SessionStopReqType(ChargingSession.Terminate))),

            ["SessionStopRes"] = new V2G_Message(Header(),
                new BodyType(new SessionStopResType(ResponseCode.OK))),

            ["CableCheckReq"] = new V2G_Message(Header(),
                new BodyType(new CableCheckReqType(DcEvStatus()))),

            ["CableCheckRes"] = new V2G_Message(Header(),
                new BodyType(new CableCheckResType(ResponseCode.OK, DcEvseStatus(), EVSEProcessing.Ongoing))),

            ["PreChargeReq"] = new V2G_Message(Header(),
                new BodyType(new PreChargeReqType(DcEvStatus(),
                    new PhysicalValueType(0, UnitSymbol.V, 400),
                    new PhysicalValueType(0, UnitSymbol.A, 10)))),

            ["PreChargeRes"] = new V2G_Message(Header(),
                new BodyType(new PreChargeResType(ResponseCode.OK, DcEvseStatus(),
                    new PhysicalValueType(0, UnitSymbol.V, 395)))),

            ["WeldingDetectionReq"] = new V2G_Message(Header(),
                new BodyType(new WeldingDetectionReqType(DcEvStatus()))),

            ["WeldingDetectionRes"] = new V2G_Message(Header(),
                new BodyType(new WeldingDetectionResType(ResponseCode.OK, DcEvseStatus(),
                    new PhysicalValueType(0, UnitSymbol.V, 400)))),

            ["PowerDeliveryReq"] = new V2G_Message(Header(),
                new BodyType(new PowerDeliveryReqType(ChargeProgress.Start, SAScheduleTupleID: 1,
                    ChargingProfile: null, EVPowerDeliveryParameter: null))),

            ["PowerDeliveryRes"] = new V2G_Message(Header(),
                new BodyType(new PowerDeliveryResType(ResponseCode.OK, DcEvseStatus()))),

            ["ChargingStatusRes"] = new V2G_Message(Header(),
                new BodyType(new ChargingStatusResType(ResponseCode.OK, "EVSE1", SAScheduleTupleID: 1,
                    EVSEMaxCurrent: null, MeterInfo: null, ReceiptRequired: null,
                    new AC_EVSEStatusType(NotificationMaxDelay: 0, EVSENotification.None, RCD: false)))),

            ["CurrentDemandReq"] = new V2G_Message(Header(),
                new BodyType(new CurrentDemandReqType(DcEvStatus(),
                    EVTargetCurrent: new PhysicalValueType(0, UnitSymbol.A, 10),
                    EVMaximumVoltageLimit: null, EVMaximumCurrentLimit: null, EVMaximumPowerLimit: null,
                    BulkChargingComplete: null, ChargingComplete: false,
                    RemainingTimeToFullSoC: null, RemainingTimeToBulkSoC: null,
                    EVTargetVoltage: new PhysicalValueType(0, UnitSymbol.V, 400)))),

            ["CurrentDemandRes"] = new V2G_Message(Header(),
                new BodyType(new CurrentDemandResType(ResponseCode.OK, DcEvseStatus(),
                    EVSEPresentVoltage: new PhysicalValueType(0, UnitSymbol.V, 395),
                    EVSEPresentCurrent: new PhysicalValueType(0, UnitSymbol.A, 10),
                    EVSECurrentLimitAchieved: false, EVSEVoltageLimitAchieved: false, EVSEPowerLimitAchieved: false,
                    EVSEMaximumVoltageLimit: null, EVSEMaximumCurrentLimit: null, EVSEMaximumPowerLimit: null,
                    EVSEID: "EVSE1", SAScheduleTupleID: 1, MeterInfo: null, ReceiptRequired: null))),

            ["ChargeParameterDiscoveryReq"] = new V2G_Message(Header(),
                new BodyType(new ChargeParameterDiscoveryReqType(
                    MaxEntriesSAScheduleTuple: null,
                    EnergyTransferMode.AC_single_phase_core,
                    new AC_EVChargeParameterType(DepartureTime: null,
                        EAmount:      new PhysicalValueType(0, UnitSymbol.Wh, 1000),
                        EVMaxVoltage: new PhysicalValueType(0, UnitSymbol.V, 400),
                        EVMaxCurrent: new PhysicalValueType(0, UnitSymbol.A, 16),
                        EVMinCurrent: new PhysicalValueType(0, UnitSymbol.A, 2))))),

            ["ChargeParameterDiscoveryRes"] = new V2G_Message(Header(),
                new BodyType(new ChargeParameterDiscoveryResType(
                    ResponseCode.OK, EVSEProcessing.Finished, SASchedules: null,
                    new AC_EVSEChargeParameterType(
                        new AC_EVSEStatusType(NotificationMaxDelay: 0, EVSENotification.None, RCD: false),
                        EVSENominalVoltage: new PhysicalValueType(0, UnitSymbol.V, 230),
                        EVSEMaxCurrent:     new PhysicalValueType(0, UnitSymbol.A, 32))))),

            ["ChargeParameterDiscoveryReq_DC"] = new V2G_Message(Header(),
                new BodyType(new ChargeParameterDiscoveryReqType(
                    MaxEntriesSAScheduleTuple: null, EnergyTransferMode.DC_extended,
                    new DC_EVChargeParameterType(DepartureTime: null, DcEvStatus(),
                        EVMaximumCurrentLimit: new PhysicalValueType(0, UnitSymbol.A, 200),
                        EVMaximumPowerLimit: null,
                        EVMaximumVoltageLimit: new PhysicalValueType(0, UnitSymbol.V, 500),
                        EVEnergyCapacity: null, EVEnergyRequest: null,
                        FullSOC: 100, BulkSOC: 80)))),

            ["ChargeParameterDiscoveryRes_DC"] = new V2G_Message(Header(),
                new BodyType(new ChargeParameterDiscoveryResType(
                    ResponseCode.OK, EVSEProcessing.Finished, SASchedules: null,
                    new DC_EVSEChargeParameterType(DcEvseStatus(),
                        EVSEMaximumCurrentLimit: new PhysicalValueType(0, UnitSymbol.A, 200),
                        EVSEMaximumPowerLimit:   new PhysicalValueType(1, UnitSymbol.W, 15000),
                        EVSEMaximumVoltageLimit: new PhysicalValueType(0, UnitSymbol.V, 500),
                        EVSEMinimumCurrentLimit: new PhysicalValueType(0, UnitSymbol.A, 0),
                        EVSEMinimumVoltageLimit: new PhysicalValueType(0, UnitSymbol.V, 200),
                        EVSECurrentRegulationTolerance: null,
                        EVSEPeakCurrentRipple: new PhysicalValueType(0, UnitSymbol.A, 1),
                        EVSEEnergyToBeDelivered: null)))),

            ["ChargingStatusReq"] = new V2G_Message(Header(),
                new BodyType(new ChargingStatusReqType())),

            ["ServiceDetailReq"] = new V2G_Message(Header(),
                new BodyType(new ServiceDetailReqType(ServiceID: 2))),

            ["ServiceDetailRes"] = new V2G_Message(Header(),
                new BodyType(new ServiceDetailResType(ResponseCode.OK, ServiceID: 2, ServiceParameterList: null))),

            ["PaymentServiceSelectionReq"] = new V2G_Message(Header(),
                new BodyType(new PaymentServiceSelectionReqType(PaymentOption.Contract,
                    new SelectedServiceListType(new[] { new SelectedServiceType(ServiceID: 1, ParameterSetID: null) })))),

            ["PaymentServiceSelectionRes"] = new V2G_Message(Header(),
                new BodyType(new PaymentServiceSelectionResType(ResponseCode.OK))),

            ["PaymentDetailsReq"] = new V2G_Message(Header(),
                new BodyType(new PaymentDetailsReqType(EMAID: "DEAAA0001234567",
                    new CertificateChainType(Id: null,
                        Certificate: new byte[] { 0x30, 0x82, 0x01, 0x02 },
                        SubCertificates: null)))),

            ["PaymentDetailsRes"] = new V2G_Message(Header(),
                new BodyType(new PaymentDetailsResType(ResponseCode.OK,
                    GenChallenge: new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
                    EVSETimeStamp: 1_600_000_000L))),

            // The bodies of the signed requests encode independently of the (absent) header signature.
            ["AuthorizationReq"] = new V2G_Message(Header(),
                new BodyType(new AuthorizationReqType(Id: null,
                    GenChallenge: new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 }))),

            ["AuthorizationRes"] = new V2G_Message(Header(),
                new BodyType(new AuthorizationResType(ResponseCode.OK, EVSEProcessing.Finished))),

            ["MeteringReceiptReq"] = new V2G_Message(Header(),
                new BodyType(new MeteringReceiptReqType(Id: null, SessionID: new byte[8], SAScheduleTupleID: 1,
                    new MeterInfoType(MeterID: "M1", MeterReading: null, SigMeterReading: null,
                        MeterStatus: null, TMeter: null)))),

            ["MeteringReceiptRes"] = new V2G_Message(Header(),
                new BodyType(new MeteringReceiptResType(ResponseCode.OK, DcEvseStatus()))),
        };

        private static DC_EVStatusType DcEvStatus() =>
            new(EVReady: true, DC_EVErrorCode.NO_ERROR, EVRESSSOC: 50);

        private static DC_EVSEStatusType DcEvseStatus() =>
            new(NotificationMaxDelay: 0, EVSENotification.None, EVSEIsolationStatus: null, DC_EVSEStatusCode.EVSE_Ready);
    }
}
