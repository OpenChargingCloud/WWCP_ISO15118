/*
 * Copyright (c) 2014-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

#region Usings

using cloud.charging.open.protocols.ISO15118.V2GTP;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SDP.Messages
{

    /// <summary>
    /// SDP_Request – the EVCC asks "is there a SECC on this link?".
    ///
    /// Wire format (V2GTP payload, 2 bytes):
    ///
    ///   octet 0: Security           (0x00 = TLS, 0x10 = no-TLS)
    ///   octet 1: TransportProtocol  (0x00 = TCP)
    ///
    /// 15118-2 and 15118-20 share this exact format and use V2GTP payload type 0x9000.
    /// </summary>
    public sealed record SDP_Request(SDP_Security           Security,
                                     SDP_TransportProtocol  TransportProtocol,
                                     SDP_Version            Version = SDP_Version.ISO_15118_2)

        : ISDP_Message

    {

        public const Int32 PayloadSize = 2;

        public V2GTP_PayloadType PayloadType
            => V2GTP_PayloadType.SdpRequest;

        public Byte[] EncodePayload()
        {
            var buf = new Byte[PayloadSize];
            buf[0] = (Byte) Security;
            buf[1] = (Byte) TransportProtocol;
            return buf;
        }

        public static SDP_Request Decode(ReadOnlySpan<Byte>  Payload,
                                         SDP_Version         Version = SDP_Version.ISO_15118_2)
        {

            if (Payload.Length != PayloadSize)
                throw new ArgumentException(
                    $"SDP_Request payload must be exactly {PayloadSize} bytes, got {Payload.Length}.",
                    nameof(Payload));

            return new SDP_Request(
                       Security:          (SDP_Security)          Payload[0],
                       TransportProtocol: (SDP_TransportProtocol) Payload[1],
                       Version:           Version
                   );

        }

        /// <summary>
        /// Lenient decoder: accepts any payload length ≥ 2, ignores trailing
        /// bytes. For pentest tooling that needs to dissect malformed frames.
        /// </summary>
        public static SDP_Request DecodeLenient(ReadOnlySpan<Byte>  Payload,
                                                SDP_Version         Version = SDP_Version.ISO_15118_2)
        {

            if (Payload.Length < PayloadSize)
                throw new ArgumentException(
                    $"SDP_Request payload must be at least {PayloadSize} bytes, got {Payload.Length}.",
                    nameof(Payload));

            return new SDP_Request(
                       Security:          (SDP_Security)          Payload[0],
                       TransportProtocol: (SDP_TransportProtocol) Payload[1],
                       Version:           Version
                   );

        }

    }

}
