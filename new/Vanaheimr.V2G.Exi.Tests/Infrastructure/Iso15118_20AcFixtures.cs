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

using cloud.charging.open.protocols.ISO15118_20.AC.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure
{
    /// <summary>The fixed ISO 15118-20 AC messages shared by the cbV2G byte-diff tests
    /// (<c>Vectors/Iso15118_20.AC.vectors.json</c>, <c>main_iso20.c</c>'s <c>do_ac</c>).</summary>
    public static class Iso15118_20AcFixtures
    {
        private static MessageHeaderType Header() => new(SessionID: new byte[8], TimeStamp: 1_700_000_000UL, Signature: null);
        private static RationalNumberType Rational(sbyte exponent, short value) => new(exponent, value);

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

                default:
                    throw new ArgumentException($"no AC fixture for vector '{vectorName}'");
            }
        }

        /// <summary>Decodes an AC wire message and re-encodes it, so callers can assert decode∘encode is
        /// the identity without referencing the generated types themselves.</summary>
        public static byte[] DecodeReEncode(byte[] wireBytes)
        {
            var decoded = AcCodec.DecodeAny(wireBytes, out int consumed);
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
                _ => throw new ArgumentException($"unexpected decoded AC type {message.GetType()}"),
            };
        }
    }
}
