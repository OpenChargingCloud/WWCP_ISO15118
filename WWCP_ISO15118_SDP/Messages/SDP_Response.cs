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

using System.Net;
using System.Buffers.Binary;

using cloud.charging.open.protocols.ISO15118.V2GTP;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SDP.Messages
{

    /// <summary>
    /// SDP_Response – the SECC announces its TCP/TLS endpoint.
    ///
    /// Wire format (V2GTP payload, 20 bytes):
    ///
    ///   octets  0..15: SECC IPv6 address       (16 bytes, link-local fe80::/10)
    ///   octets 16..17: SECC TCP port           (uint16, big-endian)
    ///   octet     18 : Security                (0x00 = TLS, 0x10 = no-TLS)
    ///   octet     19 : TransportProtocol       (0x00 = TCP)
    ///
    /// 15118-2 and 15118-20 share this exact format and use V2GTP payload type
    /// 0x9001. The IPv6 address must be link-local; the EVCC pairs it with the
    /// scope-id of the interface the SDP_Response arrived on.
    /// </summary>
    public sealed record SDP_Response(IPAddress             SeccIPAddress,
                                     UInt16                SeccPort,
                                     SDP_Security           Security,
                                     SDP_TransportProtocol  TransportProtocol,
                                     SDP_Version            Version = SDP_Version.ISO_15118_2)

        : ISDP_Message

    {

        public const Int32 PayloadSize = 20;

        public V2GTP_PayloadType PayloadType => V2GTP_PayloadType.SdpResponse;

        public Byte[] EncodePayload()
        {

            var buf  = new Byte[PayloadSize];
            var addr = SeccIPAddress.GetAddressBytes();
            if (addr.Length != 16)
                throw new InvalidOperationException(
                    $"SDP_Response.SeccIPAddress must be IPv6 (16 bytes), got {addr.Length}.");

            addr.CopyTo(buf.AsSpan(0, 16));
            BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(16, 2), SeccPort);
            buf[18] = (Byte) Security;
            buf[19] = (Byte) TransportProtocol;

            return buf;

        }

        public static SDP_Response Decode(ReadOnlySpan<Byte>  Payload,
                                         SDP_Version          Version = SDP_Version.ISO_15118_2)
        {

            if (Payload.Length != PayloadSize)
                throw new ArgumentException(
                          $"SDP_Response payload must be exactly {PayloadSize} bytes, got {Payload.Length}.",
                          nameof(Payload)
                      );

            return new SDP_Response(
                       SeccIPAddress:      new IPAddress(Payload[..16]),
                       SeccPort:           BinaryPrimitives.ReadUInt16BigEndian(Payload.Slice(16, 2)),
                       Security:           (SDP_Security)          Payload[18],
                       TransportProtocol:  (SDP_TransportProtocol) Payload[19],
                       Version:            Version
                   );

        }

        /// <summary>
        /// Lenient decoder for pentest tooling – accepts payload length ≥ 20, ignores trailing bytes.
        /// </summary>
        public static SDP_Response DecodeLenient(ReadOnlySpan<Byte>  Payload,
                                                SDP_Version          Version = SDP_Version.ISO_15118_2)
        {

            if (Payload.Length < PayloadSize)
                throw new ArgumentException(
                    $"SDP_Response payload must be at least {PayloadSize} bytes, got {Payload.Length}.",
                    nameof(Payload));

            return new SDP_Response(
                       SeccIPAddress:      new IPAddress(Payload[..16]),
                       SeccPort:           BinaryPrimitives.ReadUInt16BigEndian(Payload.Slice(16, 2)),
                       Security:           (SDP_Security)          Payload[18],
                       TransportProtocol:  (SDP_TransportProtocol) Payload[19],
                       Version:            Version
                   );

        }

        public override String ToString()
            => $"SDP_Response [{SeccIPAddress}]:{SeccPort} sec={Security} transport={TransportProtocol} v={Version}";

    }

}
