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

namespace cloud.charging.open.protocols.ISO15118.SLAC.Messages
{


    // =========================================================================
    // CM_SET_KEY.CNF        PLC chip → Host
    //   Confirmation that the chip accepted the new key. Result=0x01 means
    //   success; the chip will now use the new NMK for AVLN traffic.
    // =========================================================================
    public sealed record SetKeyCnf(
        byte    Result,             // 0x00 = failure, 0x01 = success
        uint    MyNonce,
        uint    YourNonce,
        byte    Pid,
        ushort  Prn,
        byte    Pmn,
        byte    CCoCapability) : ISlacMessage
    {
        public ManagementMessageType MmType => ManagementMessageType.CM_SET_KEY_CNF;

        /// <summary>True iff the chip accepted the new key.</summary>
        public bool IsSuccess => Result == 0x01;

        public Byte[] Encode()
        {
            var buf = new Byte[1 + 1 + 1 + 4 + 4 + 1 + 2 + 1 + 1];
            var span = buf.AsSpan();
            var off = 0;

            span[off++] = SLACConstants.ApplicationType_PevEvseMatching;
            span[off++] = SLACConstants.SecurityType_None;
            span[off++] = Result;

            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(off, 4), MyNonce);
            off += 4;
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(off, 4), YourNonce);
            off += 4;

            span[off++] = Pid;
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(off, 2), Prn);
            off += 2;
            span[off++] = Pmn;
            span[off]   = CCoCapability;

            return buf;
        }

        public static SetKeyCnf Decode(ReadOnlySpan<Byte> body)
        {
            if (body.Length < 15) throw new InvalidDataException("CM_SET_KEY.CNF truncated.");

            return new SetKeyCnf(
                Result        : body[2],
                MyNonce       : System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(body.Slice(3, 4)),
                YourNonce     : System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(body.Slice(7, 4)),
                Pid           : body[11],
                Prn           : System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(body.Slice(12, 2)),
                Pmn           : body[14],
                CCoCapability : body.Length > 15 ? body[15] : (byte) 0);
        }

    }

}
