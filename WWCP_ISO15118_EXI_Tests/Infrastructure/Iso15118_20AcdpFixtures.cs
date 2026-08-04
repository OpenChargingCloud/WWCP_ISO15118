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

using cloud.charging.open.protocols.ISO15118_20.ACDP.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure
{
    /// <summary>The fixed ISO 15118-20 ACDP messages shared by the cbV2G byte-diff tests
    /// (<c>Vectors/Iso15118_20.ACDP.vectors.json</c>, <c>main_iso20.c</c>'s <c>do_acdp</c>).</summary>
    public static class Iso15118_20AcdpFixtures
    {
        private static MessageHeaderType Header() => new(SessionID: new byte[8], TimeStamp: 1_700_000_000UL, Signature: null);

        public static bool TryEncode(string vectorName, byte[] dest, out int bytesWritten)
        {
            bytesWritten = 0;
            switch (vectorName)
            {
                case "ACDP_VehiclePositioningReq":
                    return new ACDP_VehiclePositioningReq(Header(), EVMobilityStatus: true, EVPositioningSupport: true)
                        .TryEncode(dest, out bytesWritten);

                case "ACDP_VehiclePositioningRes":
                    return new ACDP_VehiclePositioningRes(Header(), ResponseCode.OK, Processing.Finished,
                            EVSEPositioningSupport: true,
                            EVRelativeXDeviation: 10, EVRelativeYDeviation: -5,
                            ContactWindowXc: 100, ContactWindowYc: 50,
                            EVInChargePosition: false)
                        .TryEncode(dest, out bytesWritten);

                case "ACDP_ConnectReq":
                    return new ACDP_ConnectReq(Header(), ElectricalChargingDeviceStatus.State_B)
                        .TryEncode(dest, out bytesWritten);

                case "ACDP_ConnectRes":
                    return new ACDP_ConnectRes(Header(), ResponseCode.OK, Processing.Finished,
                            ElectricalChargingDeviceStatus.State_C, MechanicalChargingDeviceStatus.EndPosition)
                        .TryEncode(dest, out bytesWritten);

                case "ACDP_DisconnectReq":
                    return new ACDP_DisconnectReq(Header(), ElectricalChargingDeviceStatus.State_A)
                        .TryEncode(dest, out bytesWritten);

                case "ACDP_DisconnectRes":
                    return new ACDP_DisconnectRes(Header(), ResponseCode.OK, Processing.Finished,
                            ElectricalChargingDeviceStatus.State_A, MechanicalChargingDeviceStatus.Home)
                        .TryEncode(dest, out bytesWritten);

                case "ACDP_SystemStatusReq":
                    return new ACDP_SystemStatusReq(Header(),
                            new EVTechnicalStatusType(
                                EVReadyToCharge: true, EVImmobilizationRequest: false,
                                EVImmobilized: null, EVWLANStrength: null, EVCPStatus: null,
                                EVSOC: null, EVErrorCode: null, EVTimeout: null))
                        .TryEncode(dest, out bytesWritten);

                case "ACDP_SystemStatusRes":
                    return new ACDP_SystemStatusRes(Header(), ResponseCode.OK,
                            MechanicalChargingDeviceStatus.EndPosition, EVSEReadyToCharge: true,
                            IsolationStatus.Safe, EVSEDisabled: false, EVSEUtilityInterruptEvent: false,
                            EVSEEmergencyShutdown: false, EVSEMalfunction: false,
                            EVInChargePosition: true, EVAssociationStatus: true)
                        .TryEncode(dest, out bytesWritten);

                default:
                    throw new ArgumentException($"no ACDP fixture for vector '{vectorName}'");
            }
        }

        /// <summary>Decodes an ACDP wire message and re-encodes it, so callers can assert decode∘encode is
        /// the identity without referencing the generated types themselves.</summary>
        public static byte[] DecodeReEncode(byte[] wireBytes)
        {
            var decoded = AcdpCodec.DecodeAny(wireBytes, out int consumed);
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
                ACDP_VehiclePositioningReq m => m.TryEncode(dest, out bytesWritten),
                ACDP_VehiclePositioningRes m => m.TryEncode(dest, out bytesWritten),
                ACDP_ConnectReq m => m.TryEncode(dest, out bytesWritten),
                ACDP_ConnectRes m => m.TryEncode(dest, out bytesWritten),
                ACDP_DisconnectReq m => m.TryEncode(dest, out bytesWritten),
                ACDP_DisconnectRes m => m.TryEncode(dest, out bytesWritten),
                ACDP_SystemStatusReq m => m.TryEncode(dest, out bytesWritten),
                ACDP_SystemStatusRes m => m.TryEncode(dest, out bytesWritten),
                _ => throw new ArgumentException($"unexpected decoded ACDP type {message.GetType()}"),
            };
        }
    }
}
