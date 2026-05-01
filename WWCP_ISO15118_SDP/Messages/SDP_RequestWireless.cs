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
    /// SDP_RequestWireless – the EVCC asks "is there a SECC on this link?" for wireless charging / ACDP.
    /// 
    /// Wire format (V2GTP payload, 62 bytes – fixed):
    ///
    ///   octet  0    : Security           (0x00 = TLS, 0x10 = no-TLS)
    ///   octet  1    : TransportProtocol  (0x00 = TCP)
    ///   octets 2-3  : P2PS/PPD           (2-byte bitfield, see ISO 15118-20 Table 24)
    ///   octet  4    : CouplingType       (1 byte, see ISO 15118-20 Table 23)
    ///   octets 5-24 : EVID               (20 bytes ASCII, temporary EV identifier; "ZZ00000" if unknown)
    ///   octets25-61 : EVSEID             (37 bytes ASCII, optional/pre-filled by EV if known)
    ///
    /// Only used in ISO 15118-20.
    /// </summary>
    public sealed record SDP_RequestWireless(SDP_Security           Security,
                                            SDP_TransportProtocol  TransportProtocol,
                                            UInt16                P2psPpd,          // 2 bytes bit-coded
                                            Byte                  CouplingType,     // 1 byte
                                            String                Evid,             // exactly 20 bytes ASCII (padded?)
                                            String                Evseid,           // exactly 37 bytes ASCII (padded?)
                                            SDP_Version            Version = SDP_Version.ISO_15118_20)

        : ISDP_Message

    {

        public const Int32 PayloadSize = 62;

        public V2GTP_PayloadType PayloadType
            => V2GTP_PayloadType.SdpRequestWireless;

        public Byte[] EncodePayload()
        {

            var buf = new Byte[PayloadSize];
            buf[0] = (Byte) Security;
            buf[1] = (Byte) TransportProtocol;

            // P2PS/PPD (big-endian uint16)
            buf[2] = (Byte) (P2psPpd >> 8);
            buf[3] = (Byte)  P2psPpd;

            buf[4] = CouplingType;

            // EVID (20 bytes ASCII)
            var evidBytes    = System.Text.Encoding.ASCII.GetBytes(Evid.  PadRight(20, '\0'));
            Array.Copy(evidBytes,   0, buf,  5, 20);

            // EVSEID (37 bytes ASCII)
            var evseidBytes  = System.Text.Encoding.ASCII.GetBytes(Evseid.PadRight(37, '\0'));
            Array.Copy(evseidBytes, 0, buf, 25, 37);

            return buf;

        }

        public static SDP_RequestWireless Decode(ReadOnlySpan<Byte>  Payload,
                                                SDP_Version          Version = SDP_Version.ISO_15118_20)
        {

            if (Payload.Length != PayloadSize)
                throw new ArgumentException($"SDP_RequestWireless payload must be exactly {PayloadSize} bytes, got {Payload.Length}.");

            return new SDP_RequestWireless(
                       Security:           (SDP_Security)          Payload[0],
                       TransportProtocol:  (SDP_TransportProtocol) Payload[1],
                       P2psPpd:            (UInt16) ((Payload[2] << 8) | Payload[3]),
                       CouplingType:       Payload[4],
                       Evid:               System.Text.Encoding.ASCII.GetString(Payload.Slice(5,  20)).TrimEnd('\0'),
                       Evseid:             System.Text.Encoding.ASCII.GetString(Payload.Slice(25, 37)).TrimEnd('\0'),
                       Version:            Version
                   );

        }

        /// <summary>
        /// Lenient decoder for pentest / debugging (accepts ≥ 62 bytes, ignores trailing data).
        /// </summary>
        public static SDP_RequestWireless DecodeLenient(ReadOnlySpan<Byte>  Payload,
                                                       SDP_Version          Version = SDP_Version.ISO_15118_20)
        {

            if (Payload.Length < PayloadSize)
                throw new ArgumentException($"SDP_RequestWireless payload must be at least {PayloadSize} bytes, got {Payload.Length}.");

            return Decode(
                       Payload[..PayloadSize],
                       Version
                   );

        }

    }

}
