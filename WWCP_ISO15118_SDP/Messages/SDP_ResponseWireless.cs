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
    /// SDP_ResponseWireless – the SECC announces its TCP/TLS endpoint + diagnostic info for wireless pairing.
    ///
    /// Wire format (V2GTP payload, 59 bytes – fixed):
    ///
    ///   octets  0-15 : SECC IPv6 address     (16 bytes, link-local)
    ///   octets 16-17 : SECC TCP port         (uint16, big-endian)
    ///   octet     18 : Security              (0x00 = TLS, 0x10 = no-TLS)
    ///   octet     19 : TransportProtocol     (0x00 = TCP)
    ///   octet     20 : DiagStatus            (0x00=ongoing, 0x01=finished with EVSEID, 0x02=finished without EVSEID, 0x10=error)
    ///   octet     21 : CouplingType          (1 byte, mirrors request)
    ///   octets 22-58 : EVSEID                (37 bytes ASCII)
    ///
    /// Only used in ISO 15118-20.
    /// </summary>
    public sealed record SDP_ResponseWireless(Byte[]                SeccIpAddress,     // exactly 16 bytes
                                             UInt16                SeccTcpPort,
                                             SDP_Security           Security,
                                             SDP_TransportProtocol  TransportProtocol,
                                             Byte                  DiagStatus,        // 0x00 / 0x01 / 0x02 / 0x10
                                             Byte                  CouplingType,
                                             String                Evseid,            // exactly 37 bytes ASCII
                                             SDP_Version            Version = SDP_Version.ISO_15118_20)

        : ISDP_Message

    {

        public const Int32 PayloadSize = 59;

        public V2GTP_PayloadType PayloadType
            => V2GTP_PayloadType.SdpResponseWireless;

        public Byte[] EncodePayload()
        {

            if (SeccIpAddress.Length != 16)
                throw new ArgumentException("SECC IPv6 address must be exactly 16 bytes.");

            var buf = new Byte[PayloadSize];
            Array.Copy(SeccIpAddress, 0, buf, 0, 16);

            buf[16] = (Byte) (SeccTcpPort >> 8);
            buf[17] = (Byte) SeccTcpPort;

            buf[18] = (Byte) Security;
            buf[19] = (Byte) TransportProtocol;
            buf[20] = DiagStatus;
            buf[21] = CouplingType;

            var evseidBytes = System.Text.Encoding.ASCII.GetBytes(Evseid.PadRight(37, '\0'));
            Array.Copy(evseidBytes, 0, buf, 22, 37);

            return buf;

        }

        public static SDP_ResponseWireless Decode(ReadOnlySpan<Byte>  Payload,
                                                 SDP_Version          Version = SDP_Version.ISO_15118_20)
        {

            if (Payload.Length != PayloadSize)
                throw new ArgumentException($"SDP_ResponseWireless payload must be exactly {PayloadSize} bytes, got {Payload.Length}.");

            var ip = Payload[..16].ToArray();

            return new SDP_ResponseWireless(
                       SeccIpAddress:     ip,
                       SeccTcpPort:       (UInt16) ((Payload[16] << 8) | Payload[17]),
                       Security:          (SDP_Security)          Payload[18],
                       TransportProtocol: (SDP_TransportProtocol) Payload[19],
                       DiagStatus:        Payload[20],
                       CouplingType:      Payload[21],
                       Evseid:            System.Text.Encoding.ASCII.GetString(Payload.Slice(22, 37)).TrimEnd('\0'),
                       Version:           Version
                   );

        }

        public static SDP_ResponseWireless DecodeLenient(ReadOnlySpan<Byte>  Payload,
                                                        SDP_Version          Version = SDP_Version.ISO_15118_20)
        {

            if (Payload.Length < PayloadSize)
                throw new ArgumentException($"SDP_ResponseWireless payload must be at least {PayloadSize} bytes, got {Payload.Length}.");

            return Decode(
                       Payload[..PayloadSize],
                       Version
                   );

        }

    }

}
