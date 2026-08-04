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

using cloud.charging.open.protocols.ISO15118_20.DC.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure
{
    /// <summary>The fixed ISO 15118-20 DC messages shared by the cbV2G byte-diff tests
    /// (<c>Vectors/Iso15118_20.DC.vectors.json</c>, <c>main_iso20.c</c>'s <c>do_dc</c>).</summary>
    public static class Iso15118_20DcFixtures
    {
        private static MessageHeaderType Header() => new(SessionID: new byte[8], TimeStamp: 1_700_000_000UL, Signature: null);
        private static RationalNumberType Rational(sbyte exponent, short value) => new(exponent, value);

        public static bool TryEncode(string vectorName, byte[] dest, out int bytesWritten)
        {
            bytesWritten = 0;
            switch (vectorName)
            {
                case "DC_CableCheckReq":
                    return new DC_CableCheckReq(Header()).TryEncode(dest, out bytesWritten);

                case "DC_CableCheckRes":
                    return new DC_CableCheckRes(Header(), ResponseCode.OK, Processing.Finished)
                        .TryEncode(dest, out bytesWritten);

                case "DC_ChargeParameterDiscoveryReq":
                    return new DC_ChargeParameterDiscoveryReq(Header(),
                            new DC_CPDReqEnergyTransferModeType(
                                EVMaximumChargePower: Rational(0, 20000), EVMinimumChargePower: Rational(0, 100),
                                EVMaximumChargeCurrent: Rational(0, 200), EVMinimumChargeCurrent: Rational(0, 1),
                                EVMaximumVoltage: Rational(0, 500), EVMinimumVoltage: Rational(0, 200),
                                TargetSOC: null))
                        .TryEncode(dest, out bytesWritten);

                case "DC_ChargeParameterDiscoveryRes":
                    return new DC_ChargeParameterDiscoveryRes(Header(), ResponseCode.OK,
                            new DC_CPDResEnergyTransferModeType(
                                EVSEMaximumChargePower: Rational(1, 15000), EVSEMinimumChargePower: Rational(0, 100),
                                EVSEMaximumChargeCurrent: Rational(0, 200), EVSEMinimumChargeCurrent: Rational(0, 1),
                                EVSEMaximumVoltage: Rational(0, 500), EVSEMinimumVoltage: Rational(0, 200),
                                EVSEPowerRampLimitation: null))
                        .TryEncode(dest, out bytesWritten);

                case "DC_PreChargeReq":
                    return new DC_PreChargeReq(Header(), Processing.Finished, Rational(0, 390), Rational(0, 400))
                        .TryEncode(dest, out bytesWritten);

                case "DC_PreChargeRes":
                    return new DC_PreChargeRes(Header(), ResponseCode.OK, Rational(0, 395))
                        .TryEncode(dest, out bytesWritten);

                case "DC_ChargeLoopReq":
                    // Exercises transitive substitution's concrete, non-BPT member
                    // (Scheduled_DC_CLReqControlMode) for the CLReqControlMode field.
                    return new DC_ChargeLoopReq(Header(), DisplayParameters: null, MeterInfoRequested: false,
                            Rational(0, 400),
                            new Scheduled_DC_CLReqControlModeType(
                                EVTargetEnergyRequest: null, EVMaximumEnergyRequest: null, EVMinimumEnergyRequest: null,
                                EVTargetCurrent: Rational(0, 120), EVTargetVoltage: Rational(0, 400),
                                EVMaximumChargePower: null, EVMinimumChargePower: null, EVMaximumChargeCurrent: null,
                                EVMaximumVoltage: null, EVMinimumVoltage: null))
                        .TryEncode(dest, out bytesWritten);

                case "DC_ChargeLoopRes":
                    return new DC_ChargeLoopRes(Header(), ResponseCode.OK,
                            EVSEStatus: null, MeterInfo: null, Receipt: null,
                            EVSEPresentCurrent: Rational(0, 118), EVSEPresentVoltage: Rational(0, 398),
                            EVSEPowerLimitAchieved: false, EVSECurrentLimitAchieved: false, EVSEVoltageLimitAchieved: false,
                            new Scheduled_DC_CLResControlModeType(
                                EVSEMaximumChargePower: null, EVSEMinimumChargePower: null,
                                EVSEMaximumChargeCurrent: null, EVSEMaximumVoltage: null))
                        .TryEncode(dest, out bytesWritten);

                case "DC_ChargeLoopReq_Dynamic":
                    // Exercises the untested Dynamic_DC_CLReqControlMode branch (different
                    // event code / bit width than the Scheduled branch above).
                    return new DC_ChargeLoopReq(Header(), DisplayParameters: null, MeterInfoRequested: false,
                            Rational(0, 400),
                            new Dynamic_DC_CLReqControlModeType(
                                DepartureTime: null,
                                EVTargetEnergyRequest: Rational(1, 4000), EVMaximumEnergyRequest: Rational(1, 6000),
                                EVMinimumEnergyRequest: Rational(0, 0),
                                EVMaximumChargePower: Rational(0, 20000), EVMinimumChargePower: Rational(0, 100),
                                EVMaximumChargeCurrent: Rational(0, 200),
                                EVMaximumVoltage: Rational(0, 500), EVMinimumVoltage: Rational(0, 200)))
                        .TryEncode(dest, out bytesWritten);

                case "DC_ChargeLoopRes_Dynamic":
                    return new DC_ChargeLoopRes(Header(), ResponseCode.OK,
                            EVSEStatus: null, MeterInfo: null, Receipt: null,
                            EVSEPresentCurrent: Rational(0, 118), EVSEPresentVoltage: Rational(0, 398),
                            EVSEPowerLimitAchieved: false, EVSECurrentLimitAchieved: false, EVSEVoltageLimitAchieved: false,
                            new Dynamic_DC_CLResControlModeType(
                                DepartureTime: null, MinimumSOC: null, TargetSOC: null, AckMaxDelay: null,
                                EVSEMaximumChargePower: Rational(0, 19500), EVSEMinimumChargePower: Rational(0, 100),
                                EVSEMaximumChargeCurrent: Rational(0, 195), EVSEMaximumVoltage: Rational(0, 500)))
                        .TryEncode(dest, out bytesWritten);

                case "DC_ChargeLoopReq_BPTScheduled":
                    // Exercises the untested BPT_Scheduled_DC_CLReqControlMode branch
                    // (adds the discharge-power fields on top of Scheduled_DC_).
                    return new DC_ChargeLoopReq(Header(), DisplayParameters: null, MeterInfoRequested: false,
                            Rational(0, 400),
                            new BPT_Scheduled_DC_CLReqControlModeType(
                                EVTargetEnergyRequest: null, EVMaximumEnergyRequest: null, EVMinimumEnergyRequest: null,
                                EVTargetCurrent: Rational(0, 120), EVTargetVoltage: Rational(0, 400),
                                EVMaximumChargePower: null, EVMinimumChargePower: null, EVMaximumChargeCurrent: null,
                                EVMaximumVoltage: null, EVMinimumVoltage: null,
                                EVMaximumDischargePower: Rational(0, 11000), EVMinimumDischargePower: Rational(0, 100),
                                EVMaximumDischargeCurrent: Rational(0, 110)))
                        .TryEncode(dest, out bytesWritten);

                case "DC_ChargeLoopRes_BPTScheduled":
                    return new DC_ChargeLoopRes(Header(), ResponseCode.OK,
                            EVSEStatus: null, MeterInfo: null, Receipt: null,
                            EVSEPresentCurrent: Rational(0, 118), EVSEPresentVoltage: Rational(0, 398),
                            EVSEPowerLimitAchieved: false, EVSECurrentLimitAchieved: false, EVSEVoltageLimitAchieved: false,
                            new BPT_Scheduled_DC_CLResControlModeType(
                                EVSEMaximumChargePower: null, EVSEMinimumChargePower: null,
                                EVSEMaximumChargeCurrent: null, EVSEMaximumVoltage: null,
                                EVSEMaximumDischargePower: Rational(0, 10500), EVSEMinimumDischargePower: Rational(0, 100),
                                EVSEMaximumDischargeCurrent: Rational(0, 105), EVSEMinimumVoltage: null))
                        .TryEncode(dest, out bytesWritten);

                case "DC_ChargeLoopReq_BPTDynamic":
                    // Exercises the untested BPT_Dynamic_DC_CLReqControlMode branch.
                    return new DC_ChargeLoopReq(Header(), DisplayParameters: null, MeterInfoRequested: false,
                            Rational(0, 400),
                            new BPT_Dynamic_DC_CLReqControlModeType(
                                DepartureTime: null,
                                EVTargetEnergyRequest: Rational(1, 4000), EVMaximumEnergyRequest: Rational(1, 6000),
                                EVMinimumEnergyRequest: Rational(0, 0),
                                EVMaximumChargePower: Rational(0, 20000), EVMinimumChargePower: Rational(0, 100),
                                EVMaximumChargeCurrent: Rational(0, 200),
                                EVMaximumVoltage: Rational(0, 500), EVMinimumVoltage: Rational(0, 200),
                                EVMaximumDischargePower: Rational(0, 11000), EVMinimumDischargePower: Rational(0, 100),
                                EVMaximumDischargeCurrent: Rational(0, 110),
                                EVMaximumV2XEnergyRequest: null, EVMinimumV2XEnergyRequest: null))
                        .TryEncode(dest, out bytesWritten);

                case "DC_ChargeLoopRes_BPTDynamic":
                    return new DC_ChargeLoopRes(Header(), ResponseCode.OK,
                            EVSEStatus: null, MeterInfo: null, Receipt: null,
                            EVSEPresentCurrent: Rational(0, 118), EVSEPresentVoltage: Rational(0, 398),
                            EVSEPowerLimitAchieved: false, EVSECurrentLimitAchieved: false, EVSEVoltageLimitAchieved: false,
                            new BPT_Dynamic_DC_CLResControlModeType(
                                DepartureTime: null, MinimumSOC: null, TargetSOC: null, AckMaxDelay: null,
                                EVSEMaximumChargePower: Rational(0, 19500), EVSEMinimumChargePower: Rational(0, 100),
                                EVSEMaximumChargeCurrent: Rational(0, 195), EVSEMaximumVoltage: Rational(0, 500),
                                EVSEMaximumDischargePower: Rational(0, 10500), EVSEMinimumDischargePower: Rational(0, 100),
                                EVSEMaximumDischargeCurrent: Rational(0, 105), EVSEMinimumVoltage: Rational(0, 200)))
                        .TryEncode(dest, out bytesWritten);

                case "DC_WeldingDetectionReq":
                    return new DC_WeldingDetectionReq(Header(), Processing.Finished).TryEncode(dest, out bytesWritten);

                case "DC_WeldingDetectionRes":
                    return new DC_WeldingDetectionRes(Header(), ResponseCode.OK, Rational(0, 5))
                        .TryEncode(dest, out bytesWritten);

                default:
                    throw new ArgumentException($"no DC fixture for vector '{vectorName}'");
            }
        }

        /// <summary>Decodes a DC wire message and re-encodes it, so callers can assert decode∘encode is
        /// the identity without referencing the generated types themselves.</summary>
        public static byte[] DecodeReEncode(byte[] wireBytes)
        {
            var decoded = DcCodec.DecodeAny(wireBytes, out int consumed);
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
                DC_CableCheckReq m => m.TryEncode(dest, out bytesWritten),
                DC_CableCheckRes m => m.TryEncode(dest, out bytesWritten),
                DC_ChargeParameterDiscoveryReq m => m.TryEncode(dest, out bytesWritten),
                DC_ChargeParameterDiscoveryRes m => m.TryEncode(dest, out bytesWritten),
                DC_PreChargeReq m => m.TryEncode(dest, out bytesWritten),
                DC_PreChargeRes m => m.TryEncode(dest, out bytesWritten),
                DC_ChargeLoopReq m => m.TryEncode(dest, out bytesWritten),
                DC_ChargeLoopRes m => m.TryEncode(dest, out bytesWritten),
                DC_WeldingDetectionReq m => m.TryEncode(dest, out bytesWritten),
                DC_WeldingDetectionRes m => m.TryEncode(dest, out bytesWritten),
                _ => throw new ArgumentException($"unexpected decoded DC type {message.GetType()}"),
            };
        }
    }
}
