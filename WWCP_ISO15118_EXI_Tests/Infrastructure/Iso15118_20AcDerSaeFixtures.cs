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

using cloud.charging.open.protocols.ISO15118_20.AC_DER_SAE.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure
{
    /// <summary>
    /// The ISO 15118-20 AC fixtures again, field for field, but built from the AC_DER_SAE assembly's
    /// types and encoded by its codec.
    /// </summary>
    /// <remarks>
    /// These messages use no DER member at all. Adding the DER substitution members appends
    /// productions to groups the plain members already sit in, and whether that shifts their event
    /// codes is a question about alphabetical position, not something to assume — so these run
    /// against the cbV2G AC corpus and let the bytes answer it. Where a vector's bytes do move, the
    /// DER corpus records the DER encoding and says why.
    /// </remarks>
    public static class Iso15118_20AcDerSaeFixtures
    {
        private static MessageHeaderType Header() => new(SessionID: new byte[8], TimeStamp: 1_700_000_000UL, Signature: null);
        private static RationalNumberType Rational(sbyte exponent, short value) => new(exponent, value);

        // ---- building blocks for the SAE DER tree -------------------------------------------
        //
        // SAE's DER members are far deeper than IEC's: DERControlCPDRes alone pulls in trip curves,
        // volt/var and watt/var support and their data-point lists. The values are arbitrary — what
        // matters is that every required branch is populated, so the encoding these vectors pin
        // covers the whole tree rather than its shallow edges.

        private static CurveDataPointsListType Curve2() =>
            new(new[] { new DataTupleType(Rational(0, 1), Rational(0, 2)),
                        new DataTupleType(Rational(0, 3), Rational(0, 4)) });

        private static DERCurveType DerCurve() =>
            new(Enable: true, Priority: null, XUnit: DerUnit.V, YUnit: DerUnit.V,
                CurveDataPoints: Curve2(), CurveDataPoints_L2: null, CurveDataPoints_L3: null);

        private static VoltageTripType VoltageTrip() =>
            new(OverVoltageMustTripCurve: DerCurve(), UnderVoltageMustTripCurve: DerCurve(),
                OverVoltageMomentaryCessationTripCurve: null, UnderVoltageMomentaryCessationTripCurve: null,
                OverVoltageMayTripCurve: null, UnderVoltageMayTripCurve: null);

        private static FrequencyTripType FrequencyTrip() =>
            new(OverFrequencyMustTripCurve: DerCurve(), UnderFrequencyMustTripCurve: DerCurve(),
                OverFrequencyMayTripCurve: null, UnderFrequencyMayTripCurve: null);

        private static ReactivePowerSupportCPDResType ReactiveSupport() =>
            new(ConstantPowerFactor: new ConstantPowerFactorType(
                    Enable: true, Priority: null,
                    PowerFactorValue: Rational(0, 1), PowerFactorValue_L2: null, PowerFactorValue_L3: null,
                    PowerFactorExcitation: PowerFactorExcitation.OverExcited,
                    PowerFactorExcitation_L2: null, PowerFactorExcitation_L3: null),
                VoltVar: new VoltVarType(
                    Enable: true, Priority: null, XUnit: DerUnit.V, YUnit: DerUnit.V,
                    CurveDataPoints: Curve2(), CurveDataPoints_L2: null, CurveDataPoints_L3: null,
                    OpenLoopResponseTime: Rational(0, 5), TimeConstantPT1: null,
                    ReferenceVoltage: Rational(0, 230),
                    AutonomousReferenceVoltageAdjustmentEnable: false,
                    ReferenceVoltageAdjustmentTimeConstant: 0),
                WattVar: new WattVarType(
                    Enable: true, Priority: null, XUnit: DerUnit.V, YUnit: DerUnit.V,
                    CurveDataPoints: Curve2(), CurveDataPoints_L2: null, CurveDataPoints_L3: null,
                    OpenLoopResponseTime: null, TimeConstantPT1: null),
                ConstantVar: new ConstantVarType(
                    Enable: true, Priority: null,
                    VarSetpoint: Rational(0, 0), VarSetpoint_L2: null, VarSetpoint_L3: null,
                    Unit: DerUnit.V));

        private static ActivePowerSupportCPDResType ActiveSupport() =>
            new(FrequencyDroop: new FrequencyDroopType(
                    Enable: true, Priority: null, OverFrequencyDroop: null, UnderFrequencyDroop: null),
                VoltWatt: new VoltWattType(
                    Enable: true, Priority: null, XUnit: DerUnit.V, YUnit: DerUnit.V,
                    CurveDataPoints: Curve2(), CurveDataPoints_L2: null, CurveDataPoints_L3: null,
                    OpenLoopResponseTime: Rational(0, 5), TimeConstantPT1: null),
                ConstantWatt: new ConstantWattType(
                    Enable: true, Priority: null,
                    WattSetpoint: Rational(0, 1000), WattSetpoint_L2: null, WattSetpoint_L3: null,
                    Unit: DerUnit.V),
                LimitMaxDischargePower: new LimitMaxDischargePowerType(
                    Enable: true, Priority: null,
                    PercentageValue: 80, PercentageValue_L2: null, PercentageValue_L3: null,
                    OpenLoopResponseTime: null));

        private static DERControlCLResType DerControlClRes() =>
            new(VoltageTrip: null, FrequencyTrip: null,
                EnterServiceCLRes: new EnterServiceCLResType(
                    PermitService: true,
                    EnterServiceVoltageHigh: null, EnterServiceVoltageLow: null,
                    EnterServiceFrequencyHigh: null, EnterServiceFrequencyLow: null,
                    EnterServiceDelay: null, EnterServiceRandomizedDelay: null, EnterServiceRampTime: null),
                ReactivePowerSupportCLRes: null, ActivePowerSupportCLRes: null);

        public static bool TryEncode(string vectorName, byte[] dest, out int bytesWritten)
        {
            bytesWritten = 0;
            switch (vectorName)
            {
                case "AC_ChargeParameterDiscoveryReq":
                    // Exercises the concrete (non-abstract-element) substitution head
                    // AC_CPDReqEnergyTransferMode, choosing the base (non-BPT) member.
                    return new AC_ChargeParameterDiscoveryReq(
                            Header(),
                            new AC_CPDReqEnergyTransferModeType(
                                EVMaximumChargePower: Rational(0, 11000),
                                EVMaximumChargePower_L2: null,
                                EVMaximumChargePower_L3: null,
                                EVMinimumChargePower: Rational(0, 100),
                                EVMinimumChargePower_L2: null,
                                EVMinimumChargePower_L3: null))
                        .TryEncode(dest, out bytesWritten);

                case "AC_ChargeParameterDiscoveryRes":
                    return new AC_ChargeParameterDiscoveryRes(
                            Header(), ResponseCode.OK,
                            new AC_CPDResEnergyTransferModeType(
                                EVSEMaximumChargePower: Rational(0, 22000),
                                EVSEMaximumChargePower_L2: null,
                                EVSEMaximumChargePower_L3: null,
                                EVSEMinimumChargePower: Rational(0, 100),
                                EVSEMinimumChargePower_L2: null,
                                EVSEMinimumChargePower_L3: null,
                                EVSENominalFrequency: Rational(0, 50),
                                MaximumPowerAsymmetry: null,
                                EVSEPowerRampLimitation: null,
                                EVSEPresentActivePower: null,
                                EVSEPresentActivePower_L2: null,
                                EVSEPresentActivePower_L3: null))
                        .TryEncode(dest, out bytesWritten);

                case "AC_ChargeLoopReq":
                    // Exercises the transitive substitution's concrete, non-BPT member
                    // (Scheduled_AC_CLReqControlMode) for the CLReqControlMode field.
                    return new AC_ChargeLoopReq(
                            Header(), DisplayParameters: null, MeterInfoRequested: false,
                            new Scheduled_AC_CLReqControlModeType(
                                EVTargetEnergyRequest: null, EVMaximumEnergyRequest: null, EVMinimumEnergyRequest: null,
                                EVMaximumChargePower: null, EVMaximumChargePower_L2: null, EVMaximumChargePower_L3: null,
                                EVMinimumChargePower: null, EVMinimumChargePower_L2: null, EVMinimumChargePower_L3: null,
                                EVPresentActivePower: Rational(0, 4000), EVPresentActivePower_L2: null, EVPresentActivePower_L3: null,
                                EVPresentReactivePower: null, EVPresentReactivePower_L2: null, EVPresentReactivePower_L3: null))
                        .TryEncode(dest, out bytesWritten);

                case "AC_ChargeLoopRes":
                    return new AC_ChargeLoopRes(
                            Header(), ResponseCode.OK,
                            EVSEStatus: null, MeterInfo: null, Receipt: null, EVSETargetFrequency: null,
                            new Scheduled_AC_CLResControlModeType(
                                EVSETargetActivePower: null, EVSETargetActivePower_L2: null, EVSETargetActivePower_L3: null,
                                EVSETargetReactivePower: null, EVSETargetReactivePower_L2: null, EVSETargetReactivePower_L3: null,
                                EVSEPresentActivePower: null, EVSEPresentActivePower_L2: null, EVSEPresentActivePower_L3: null))
                        .TryEncode(dest, out bytesWritten);

                case "AC_ChargeLoopReq_BPTScheduled":
                    // Exercises the untested BPT_Scheduled_AC_CLReqControlMode branch
                    // (adds the discharge-power fields on top of Scheduled_AC_).
                    return new AC_ChargeLoopReq(
                            Header(), DisplayParameters: null, MeterInfoRequested: false,
                            new BPT_Scheduled_AC_CLReqControlModeType(
                                EVTargetEnergyRequest: null, EVMaximumEnergyRequest: null, EVMinimumEnergyRequest: null,
                                EVMaximumChargePower: null, EVMaximumChargePower_L2: null, EVMaximumChargePower_L3: null,
                                EVMinimumChargePower: null, EVMinimumChargePower_L2: null, EVMinimumChargePower_L3: null,
                                EVPresentActivePower: Rational(0, 4000), EVPresentActivePower_L2: null, EVPresentActivePower_L3: null,
                                EVPresentReactivePower: null, EVPresentReactivePower_L2: null, EVPresentReactivePower_L3: null,
                                EVMaximumDischargePower: Rational(0, 3700), EVMaximumDischargePower_L2: null, EVMaximumDischargePower_L3: null,
                                EVMinimumDischargePower: Rational(0, 100), EVMinimumDischargePower_L2: null, EVMinimumDischargePower_L3: null))
                        .TryEncode(dest, out bytesWritten);

                case "AC_ChargeLoopRes_BPTScheduled":
                    return new AC_ChargeLoopRes(
                            Header(), ResponseCode.OK,
                            EVSEStatus: null, MeterInfo: null, Receipt: null, EVSETargetFrequency: null,
                            new BPT_Scheduled_AC_CLResControlModeType(
                                EVSETargetActivePower: Rational(0, 3700), EVSETargetActivePower_L2: null, EVSETargetActivePower_L3: null,
                                EVSETargetReactivePower: null, EVSETargetReactivePower_L2: null, EVSETargetReactivePower_L3: null,
                                EVSEPresentActivePower: null, EVSEPresentActivePower_L2: null, EVSEPresentActivePower_L3: null))
                        .TryEncode(dest, out bytesWritten);

                case "AC_ChargeLoopReq_Dynamic":
                    // Exercises the untested Dynamic_AC_CLReqControlMode branch.
                    return new AC_ChargeLoopReq(
                            Header(), DisplayParameters: null, MeterInfoRequested: false,
                            new Dynamic_AC_CLReqControlModeType(
                                DepartureTime: null,
                                EVTargetEnergyRequest: Rational(1, 4000), EVMaximumEnergyRequest: Rational(1, 6000),
                                EVMinimumEnergyRequest: Rational(0, 0),
                                EVMaximumChargePower: Rational(0, 11000), EVMaximumChargePower_L2: null, EVMaximumChargePower_L3: null,
                                EVMinimumChargePower: Rational(0, 100), EVMinimumChargePower_L2: null, EVMinimumChargePower_L3: null,
                                EVPresentActivePower: Rational(0, 4000), EVPresentActivePower_L2: null, EVPresentActivePower_L3: null,
                                EVPresentReactivePower: Rational(0, 0), EVPresentReactivePower_L2: null, EVPresentReactivePower_L3: null))
                        .TryEncode(dest, out bytesWritten);

                case "AC_ChargeLoopRes_Dynamic":
                    return new AC_ChargeLoopRes(
                            Header(), ResponseCode.OK,
                            EVSEStatus: null, MeterInfo: null, Receipt: null, EVSETargetFrequency: null,
                            new Dynamic_AC_CLResControlModeType(
                                DepartureTime: null, MinimumSOC: null, TargetSOC: null, AckMaxDelay: null,
                                EVSETargetActivePower: Rational(0, 3700), EVSETargetActivePower_L2: null, EVSETargetActivePower_L3: null,
                                EVSETargetReactivePower: null, EVSETargetReactivePower_L2: null, EVSETargetReactivePower_L3: null,
                                EVSEPresentActivePower: null, EVSEPresentActivePower_L2: null, EVSEPresentActivePower_L3: null))
                        .TryEncode(dest, out bytesWritten);

                case "AC_ChargeLoopReq_BPTDynamic":
                    // Exercises the untested BPT_Dynamic_AC_CLReqControlMode branch.
                    return new AC_ChargeLoopReq(
                            Header(), DisplayParameters: null, MeterInfoRequested: false,
                            new BPT_Dynamic_AC_CLReqControlModeType(
                                DepartureTime: null,
                                EVTargetEnergyRequest: Rational(1, 4000), EVMaximumEnergyRequest: Rational(1, 6000),
                                EVMinimumEnergyRequest: Rational(0, 0),
                                EVMaximumChargePower: Rational(0, 11000), EVMaximumChargePower_L2: null, EVMaximumChargePower_L3: null,
                                EVMinimumChargePower: Rational(0, 100), EVMinimumChargePower_L2: null, EVMinimumChargePower_L3: null,
                                EVPresentActivePower: Rational(0, 4000), EVPresentActivePower_L2: null, EVPresentActivePower_L3: null,
                                EVPresentReactivePower: Rational(0, 0), EVPresentReactivePower_L2: null, EVPresentReactivePower_L3: null,
                                EVMaximumDischargePower: Rational(0, 3700), EVMaximumDischargePower_L2: null, EVMaximumDischargePower_L3: null,
                                EVMinimumDischargePower: Rational(0, 100), EVMinimumDischargePower_L2: null, EVMinimumDischargePower_L3: null,
                                EVMaximumV2XEnergyRequest: null, EVMinimumV2XEnergyRequest: null))
                        .TryEncode(dest, out bytesWritten);

                case "AC_ChargeLoopRes_BPTDynamic":
                    return new AC_ChargeLoopRes(
                            Header(), ResponseCode.OK,
                            EVSEStatus: null, MeterInfo: null, Receipt: null, EVSETargetFrequency: null,
                            new BPT_Dynamic_AC_CLResControlModeType(
                                DepartureTime: null, MinimumSOC: null, TargetSOC: null, AckMaxDelay: null,
                                EVSETargetActivePower: Rational(0, 3700), EVSETargetActivePower_L2: null, EVSETargetActivePower_L3: null,
                                EVSETargetReactivePower: null, EVSETargetReactivePower_L2: null, EVSETargetReactivePower_L3: null,
                                EVSEPresentActivePower: null, EVSEPresentActivePower_L2: null, EVSEPresentActivePower_L3: null))
                        .TryEncode(dest, out bytesWritten);

                // ---- the DER members themselves ------------------------------------------------
                //
                // No external oracle exists for these: cbexigen does not generate the amendment
                // schemas, so the bytes in the corpus are this project's own output. They pin the
                // encoding against drift and let the Kotlin port be checked against the C# one;
                // they are not evidence of wire conformance.

                case "AC_ChargeParameterDiscoveryReq_DER":
                    return new AC_ChargeParameterDiscoveryReq(
                            Header(),
                            new DER_AC_CPDReqEnergyTransferModeType(
                                EVMaximumChargePower: Rational(0, 11000),
                                EVMaximumChargePower_L2: null, EVMaximumChargePower_L3: null,
                                EVMinimumChargePower: Rational(0, 100),
                                EVMinimumChargePower_L2: null, EVMinimumChargePower_L3: null,
                                EVProcessing: Processing.Finished,
                                EVMaximumDischargePower: Rational(0, 5000),
                                EVMaximumDischargePower_L2: null, EVMaximumDischargePower_L3: null,
                                EVMinimumDischargePower: null,
                                EVMinimumDischargePower_L2: null, EVMinimumDischargePower_L3: null,
                                EVSessionTotalDischargeEnergyAvailable: null,
                                EVApparentPowerLimits: new EVApparentPowerLimitsType(
                                    Rational(0, 11000), null, null, Rational(0, 11000), null, null,
                                    Rational(0, 5000), null, null, Rational(0, 5000), null, null),
                                EVReactivePowerLimits: new EVReactivePowerLimitsType(
                                    Rational(0, 3000), null, null, null, null, null,
                                    Rational(0, 3000), null, null, null, null, null,
                                    Rational(0, 2000), null, null, null, null, null,
                                    Rational(0, 2000), null, null, null, null, null,
                                    Rational(0, 1), null, null),
                                EVExcitationLimits: new EVExcitationLimitsType(
                                    Rational(0, 1), null, null, Rational(0, 5000), null, null,
                                    Rational(0, 1), null, null, Rational(0, 5000), null, null),
                                EVInverterDetails: new EVInverterDetailsType(
                                    EVInverterSwVersion: "1.0", EVInverterHwVersion: null,
                                    EVInverterManufacturer: "ACME", EVInverterModel: "X1",
                                    EVInverterSerialNumber: "SN1"),
                                IEEE1547NormalCategory: Ieee1547NormalCategory.CategoryA,
                                IEEE1547AbnormalCategory: Ieee1547AbnormalCategory.CategoryI,
                                EVNominalVoltage: Rational(0, 230),
                                EVMaximumVoltage: Rational(0, 250),
                                EVMinimumVoltage: Rational(0, 200),
                                EVNominalVoltageOffset: Rational(0, 0),
                                J3072Certified: true,
                                J3072CertificationDate: 1_700_000_000UL,
                                EVUseableWattHours: 50_000,
                                EVUpdateTime: 1_700_000_000UL,
                                SupportedModes: 1,
                                EnabledModes: 1))
                        .TryEncode(dest, out bytesWritten);

                case "AC_ChargeParameterDiscoveryRes_DER":
                    return new AC_ChargeParameterDiscoveryRes(
                            Header(), ResponseCode.OK,
                            new DER_AC_CPDResEnergyTransferModeType(
                                EVSEMaximumChargePower: Rational(0, 22000),
                                EVSEMaximumChargePower_L2: null, EVSEMaximumChargePower_L3: null,
                                EVSEMinimumChargePower: Rational(0, 100),
                                EVSEMinimumChargePower_L2: null, EVSEMinimumChargePower_L3: null,
                                EVSENominalFrequency: Rational(0, 50),
                                MaximumPowerAsymmetry: null, EVSEPowerRampLimitation: null,
                                EVSEPresentActivePower: null,
                                EVSEPresentActivePower_L2: null, EVSEPresentActivePower_L3: null,
                                EVSEProcessing: Processing.Finished,
                                EVSEStatus: null,
                                DERControlCPDRes: new DERControlCPDResType(
                                    VoltageTrip: VoltageTrip(),
                                    FrequencyTrip: FrequencyTrip(),
                                    EnterServiceCPDRes: new EnterServiceCPDResType(
                                        PermitService: true,
                                        EnterServiceVoltageHigh: Rational(0, 250),
                                        EnterServiceVoltageLow: Rational(0, 200),
                                        EnterServiceFrequencyHigh: Rational(0, 51),
                                        EnterServiceFrequencyLow: Rational(0, 49),
                                        EnterServiceDelay: null, EnterServiceRandomizedDelay: null,
                                        EnterServiceRampTime: null),
                                    ReactivePowerSupportCPDRes: ReactiveSupport(),
                                    ActivePowerSupportCPDRes: ActiveSupport()),
                                EVSENominalChargePower: null,
                                EVSENominalChargePower_L2: null, EVSENominalChargePower_L3: null,
                                EVSENominalDischargePower: null,
                                EVSENominalDischargePower_L2: null, EVSENominalDischargePower_L3: null,
                                EVSEMaximumDischargePower: Rational(0, 7000),
                                EVSEMaximumDischargePower_L2: null, EVSEMaximumDischargePower_L3: null,
                                EVSEReactivePowerLimits: new EVSEReactivePowerLimitsType(
                                    Rational(0, 3000), null, null, Rational(0, 3000), null, null,
                                    Rational(0, 2000), null, null, Rational(0, 2000), null, null),
                                GridLimits: new GridLimitsType(
                                    GridNominalFrequency: Rational(0, 50),
                                    GridNominalVoltage: Rational(0, 230),
                                    GridNominalVoltageOffset: Rational(0, 0),
                                    GridMinFrequency: null, GridMaxFrequency: null,
                                    GridMaximumVoltage: Rational(0, 250),
                                    GridMinimumVoltage: Rational(0, 200)),
                                RequiredDEROperatingMode: RequiredDEROperatingMode.GridFollowing,
                                GridConnectionMode: GridConnectionMode.GridConnected,
                                EVSEUpdateTime: 1_700_000_000UL))
                        .TryEncode(dest, out bytesWritten);

                case "AC_ChargeLoopReq_DERScheduled":
                    return new AC_ChargeLoopReq(
                            Header(), DisplayParameters: null, MeterInfoRequested: false,
                            new DER_Scheduled_AC_CLReqControlModeType(
                                EVTargetEnergyRequest: null, EVMaximumEnergyRequest: null, EVMinimumEnergyRequest: null,
                                EVMaximumChargePower: null, EVMaximumChargePower_L2: null, EVMaximumChargePower_L3: null,
                                EVMinimumChargePower: null, EVMinimumChargePower_L2: null, EVMinimumChargePower_L3: null,
                                EVPresentActivePower: Rational(0, 4000),
                                EVPresentActivePower_L2: null, EVPresentActivePower_L3: null,
                                EVPresentReactivePower: null,
                                EVPresentReactivePower_L2: null, EVPresentReactivePower_L3: null,
                                EVPresentVoltage: Rational(0, 230),
                                EVPresentFrequency: Rational(0, 50),
                                EVMaximumDischargePower: null,
                                EVMaximumDischargePower_L2: null, EVMaximumDischargePower_L3: null,
                                EVMinimumDischargePower: null,
                                EVMinimumDischargePower_L2: null, EVMinimumDischargePower_L3: null,
                                DEROperationalState: DerOperationalState.On,
                                DERConnectionStatus: DerConnectionStatus.Disconnected,
                                EVApparentPower: null, EVReactivePower: null, EVExcitation: null,
                                EVUpdateTime: 1_700_000_000UL,
                                EVMinimumChargingDuration: null,
                                EVDurationMaximumChargeRate: null,
                                EVDurationMaximumDischargeRate: null,
                                DERAlarmStatus: 0,
                                EnabledModes: 1))
                        .TryEncode(dest, out bytesWritten);

                case "AC_ChargeLoopRes_DERScheduled":
                    return new AC_ChargeLoopRes(
                            Header(), ResponseCode.OK,
                            EVSEStatus: null, MeterInfo: null, Receipt: null, EVSETargetFrequency: null,
                            new DER_Scheduled_AC_CLResControlModeType(
                                EVSETargetActivePower: Rational(0, 3700),
                                EVSETargetActivePower_L2: null, EVSETargetActivePower_L3: null,
                                EVSETargetReactivePower: null,
                                EVSETargetReactivePower_L2: null, EVSETargetReactivePower_L3: null,
                                EVSEPresentActivePower: null,
                                EVSEPresentActivePower_L2: null, EVSEPresentActivePower_L3: null,
                                DERControlCLRes: DerControlClRes(),
                                EVSEMaximumChargePower: null,
                                EVSEMaximumChargePower_L2: null, EVSEMaximumChargePower_L3: null,
                                EVSEMaximumDischargePower: null,
                                EVSEMaximumDischargePower_L2: null, EVSEMaximumDischargePower_L3: null,
                                RequiredDEROperatingMode: null,
                                GridConnectionMode: null))
                        .TryEncode(dest, out bytesWritten);

                case "AC_ChargeLoopReq_DERDynamic":
                    return new AC_ChargeLoopReq(
                            Header(), DisplayParameters: null, MeterInfoRequested: false,
                            new DER_Dynamic_AC_CLReqControlModeType(
                                DepartureTime: null,
                                EVTargetEnergyRequest: Rational(1, 4000),
                                EVMaximumEnergyRequest: Rational(1, 6000),
                                EVMinimumEnergyRequest: Rational(0, 0),
                                EVMaximumChargePower: Rational(0, 11000),
                                EVMaximumChargePower_L2: null, EVMaximumChargePower_L3: null,
                                EVMinimumChargePower: Rational(0, 100),
                                EVMinimumChargePower_L2: null, EVMinimumChargePower_L3: null,
                                EVPresentActivePower: Rational(0, 4000),
                                EVPresentActivePower_L2: null, EVPresentActivePower_L3: null,
                                EVPresentReactivePower: Rational(0, 0),
                                EVPresentReactivePower_L2: null, EVPresentReactivePower_L3: null,
                                EVMaximumDischargePower: Rational(0, 5000),
                                EVMaximumDischargePower_L2: null, EVMaximumDischargePower_L3: null,
                                EVMinimumDischargePower: Rational(0, 50),
                                EVMinimumDischargePower_L2: null, EVMinimumDischargePower_L3: null,
                                EVPresentVoltage: Rational(0, 230),
                                EVPresentFrequency: Rational(0, 50),
                                EVSessionTotalDischargeEnergyAvailable: null,
                                EVApparentPower: null, EVReactivePower: null, EVExcitation: null,
                                EVMaximumV2XEnergyRequest: null, EVMinimumV2XEnergyRequest: null,
                                DEROperationalState: DerOperationalState.On,
                                DERConnectionStatus: DerConnectionStatus.Disconnected,
                                EVUpdateTime: 1_700_000_000UL,
                                EVMinimumChargingDuration: 600,
                                EVDurationMaximumChargeRate: 1200,
                                EVDurationMaximumDischargeRate: 900,
                                DERAlarmStatus: 0,
                                EnabledModes: 1))
                        .TryEncode(dest, out bytesWritten);

                case "AC_ChargeLoopRes_DERDynamic":
                    return new AC_ChargeLoopRes(
                            Header(), ResponseCode.OK,
                            EVSEStatus: null, MeterInfo: null, Receipt: null, EVSETargetFrequency: null,
                            new DER_Dynamic_AC_CLResControlModeType(
                                DepartureTime: null, MinimumSOC: null, TargetSOC: null, AckMaxDelay: null,
                                EVSETargetActivePower: Rational(0, 3700),
                                EVSETargetActivePower_L2: null, EVSETargetActivePower_L3: null,
                                EVSETargetReactivePower: null,
                                EVSETargetReactivePower_L2: null, EVSETargetReactivePower_L3: null,
                                EVSEPresentActivePower: null,
                                EVSEPresentActivePower_L2: null, EVSEPresentActivePower_L3: null,
                                DERControlCLRes: DerControlClRes(),
                                EVSEMaximumChargePower: null,
                                EVSEMaximumChargePower_L2: null, EVSEMaximumChargePower_L3: null,
                                EVSEMaximumDischargePower: null,
                                EVSEMaximumDischargePower_L2: null, EVSEMaximumDischargePower_L3: null,
                                RequiredDEROperatingMode: null,
                                GridConnectionMode: null))
                        .TryEncode(dest, out bytesWritten);

                default:
                    throw new ArgumentException($"no AC_DER_SAE fixture for vector '{vectorName}'");
            }
        }

        /// <summary>Decodes a wire message under this grammar and re-encodes it, so callers can
        /// assert decode∘encode is the identity without referencing the generated types.</summary>
        public static byte[] DecodeReEncode(byte[] wireBytes)
        {
            var decoded = AcDerSaeCodec.DecodeAny(wireBytes, out int consumed);
            if (consumed != wireBytes.Length)
                throw new InvalidDataException($"decoder consumed {consumed} of {wireBytes.Length} bytes");

            var buf = new byte[512];
            if (!TryReEncode(decoded, buf, out int n))
                throw new InvalidDataException("re-encode failed");
            return buf.AsSpan(0, n).ToArray();
        }

        private static bool TryReEncode(object message, byte[] dest, out int bytesWritten)
        {
            bytesWritten = 0;
            return message switch
            {
                AC_ChargeParameterDiscoveryReq m => m.TryEncode(dest, out bytesWritten),
                AC_ChargeParameterDiscoveryRes m => m.TryEncode(dest, out bytesWritten),
                AC_ChargeLoopReq m => m.TryEncode(dest, out bytesWritten),
                AC_ChargeLoopRes m => m.TryEncode(dest, out bytesWritten),
                _ => throw new ArgumentException($"unexpected decoded AC_DER_SAE type {message.GetType()}"),
            };
        }
    }
}
