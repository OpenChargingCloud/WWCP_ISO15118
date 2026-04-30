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

using System.Buffers.Binary;
using cloud.charging.open.protocols.ISO15118.SLAC.Messages;
using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

namespace cloud.charging.open.protocols.ISO15118.SLAC
{

    /// <summary>
    /// HomePlug AV Management Message Entry (MME) framing. The wire format is:
    ///
    ///   +-------------------+--------+-----------+
    ///   | Ethernet header   | MM hdr | Body      |
    ///   |  DST(6) SRC(6)    |  MMV   | (per      |
    ///   |  ET=0x88E1        |  MMTYPE| message)  |
    ///   +-------------------+--------+-----------+
    ///
    /// MMV is 1 byte (0x01). MMTYPE is 2 bytes, little-endian.
    /// (HPAV-1.1+ also has FMI/FMSN bytes, but they are not used by the
    /// SLAC matching profile for these single-fragment messages.)
    /// </summary>
    public static class ManagementMessageEntry
    {
        /// <summary>Total length of the Ethernet header (DST + SRC + EtherType).</summary>
        public const int EthernetHeaderLength = 6 + 6 + 2;

        /// <summary>Length of the HomePlug AV management header (MMV + MMTYPE).</summary>
        public const int MmHeaderLength = 1 + 2;

        /// <summary>Encode a complete on-the-wire frame including Ethernet header.</summary>
        public static byte[] Encode(MACAddress destination, MACAddress source, ISlacMessage message)
        {
            var body = message.Encode();
            var buf  = new Byte[EthernetHeaderLength + MmHeaderLength + body.Length];
            var span = buf.AsSpan();

            // Ethernet header
            destination.CopyTo(span.Slice(0, 6));
            source.CopyTo(span.Slice(6, 6));
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(12, 2), SLACConstants.HomePlugAvEtherType);

            // HomePlug AV MM header
            span[14] = SLACConstants.Mmv;
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(15, 2), (ushort) message.MmType);

            // Body
            body.AsSpan().CopyTo(span.Slice(17, body.Length));

            return buf;
        }

        /// <summary>
        /// Decode a frame received from the wire. Returns the parsed message together with the
        /// Ethernet source / destination MAC addresses. Returns null if the frame is not a
        /// recognised SLAC message (wrong EtherType, unknown MMTYPE, truncated, …).
        /// </summary>
        public static DecodedFrame? TryDecode(ReadOnlySpan<Byte> frame)
        {
            if (frame.Length < EthernetHeaderLength + MmHeaderLength)
                return null;

            var dst       = MACAddress.From(frame.Slice(0, 6));
            var src       = MACAddress.From(frame.Slice(6, 6));
            var etherType = BinaryPrimitives.ReadUInt16BigEndian(frame.Slice(12, 2));

            if (etherType != SLACConstants.HomePlugAvEtherType)
                return null;

            var mmv = frame[14];
            if (mmv != SLACConstants.Mmv)
                return null;

            var mmType = (ManagementMessageType) BinaryPrimitives.ReadUInt16LittleEndian(frame.Slice(15, 2));
            var body   = frame.Slice(17);

            ISlacMessage? msg;
            try
            {
                msg = mmType switch
                {
                    ManagementMessageType.CM_SLAC_PARM_REQ        => SLACParamReq.Decode(body),
                    ManagementMessageType.CM_SLAC_PARM_CNF        => SLACParmCnf.Decode(body),
                    ManagementMessageType.CM_START_ATTEN_CHAR_IND => StartAttenCharInd.Decode(body),
                    ManagementMessageType.CM_MNBC_SOUND_IND       => MnbcSoundInd.Decode(body),
                    ManagementMessageType.CM_ATTEN_CHAR_IND       => AttenCharInd.Decode(body),
                    ManagementMessageType.CM_ATTEN_CHAR_RSP       => AttenCharRsp.Decode(body),
                    ManagementMessageType.CM_VALIDATE_REQ         => ValidateReq.Decode(body),
                    ManagementMessageType.CM_VALIDATE_CNF         => ValidateCnf.Decode(body),
                    ManagementMessageType.CM_SLAC_MATCH_REQ       => SlacMatchReq.Decode(body),
                    ManagementMessageType.CM_SLAC_MATCH_CNF       => SlacMatchCnf.Decode(body),
                    ManagementMessageType.CM_SET_KEY_REQ          => SetKeyReq.Decode(body),
                    ManagementMessageType.CM_SET_KEY_CNF          => SetKeyCnf.Decode(body),
                    _                              => null
                };
            }
            catch (InvalidDataException)
            {
                return null;
            }

            return msg is null ? null : new DecodedFrame(dst, src, msg);
        }
    }

}
