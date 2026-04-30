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
    // CM_SET_KEY.REQ        Host → PLC chip   (unicast, addressed to the chip)
    //
    //   Standard HomePlug AV management message (NOT SLAC-specific). After a
    //   successful SLAC match the host CPU uses this MME to program the
    //   negotiated NMK + NID into its local PLC chip (e.g. qca7000), so that
    //   the chip leaves its initial AVLN and joins the EV-EVSE AVLN.
    //
    //   Wire layout (HomePlug AV 2.1 §11.5.4):
    //     KeyType         (1 byte)   — 0x01 = NMK
    //     MyNonce         (4 bytes)  — sender's nonce (random)
    //     YourNonce       (4 bytes)  — peer's nonce (0 on first set)
    //     PID             (1 byte)   — Protocol ID, 0x04 = HLE Protocol
    //     PRN             (2 bytes)  — Protocol Run Number, little-endian
    //     PMN             (1 byte)   — Protocol Message Number
    //     CCoCapability   (1 byte)   — 0..3 (none / level-0..2)
    //     NID             (7 bytes)
    //     NewEKS          (1 byte)   — Encryption Key Select, 0x01 = NMK
    //     NewKey          (16 bytes) — the NMK
    // =========================================================================
    public sealed record SetKeyReq(
        byte    KeyType,
        uint    MyNonce,
        uint    YourNonce,
        byte    Pid,
        ushort  Prn,
        byte    Pmn,
        byte    CCoCapability,
        byte[]  Nid,
        byte    NewEks,
        byte[]  NewKey) : ISlacMessage
    {
        public ManagementMessageType MmType => ManagementMessageType.CM_SET_KEY_REQ;

        /// <summary>Convenience: build a typical "set NMK" REQ for a fresh AVLN join.</summary>
        public static SetKeyReq ForNmk(byte[] nid, byte[] nmk, byte ccoCapability = 0x00)
        {
            if (nid.Length != SLACConstants.NidLength)
                throw new ArgumentException($"NID must be {SLACConstants.NidLength} bytes.", nameof(nid));
            if (nmk.Length != SLACConstants.NmkLength)
                throw new ArgumentException($"NMK must be {SLACConstants.NmkLength} bytes.", nameof(nmk));

            Span<byte> nonceBytes = stackalloc byte[4];
            System.Security.Cryptography.RandomNumberGenerator.Fill(nonceBytes);

            return new SetKeyReq(
                KeyType       : 0x01,                                       // NMK
                MyNonce       : System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(nonceBytes),
                YourNonce     : 0,                                          // first set
                Pid           : 0x04,                                       // HLE Protocol
                Prn           : 0x0000,
                Pmn           : 0x00,
                CCoCapability : ccoCapability,
                Nid           : (byte[]) nid.Clone(),
                NewEks        : 0x01,                                       // NMK
                NewKey        : (byte[]) nmk.Clone());
        }

        public Byte[] Encode()
        {
            var buf = new Byte[1 + 1 + 1 + 4 + 4 + 1 + 2 + 1 + 1 + 7 + 1 + 16];
            var span = buf.AsSpan();
            var off = 0;

            span[off++] = SLACConstants.ApplicationType_PevEvseMatching;
            span[off++] = SLACConstants.SecurityType_None;

            span[off++] = KeyType;

            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(off, 4), MyNonce);
            off += 4;
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(off, 4), YourNonce);
            off += 4;

            span[off++] = Pid;
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(off, 2), Prn);
            off += 2;
            span[off++] = Pmn;
            span[off++] = CCoCapability;

            Nid.AsSpan().CopyTo(span.Slice(off, 7));     off += 7;
            span[off++] = NewEks;
            NewKey.AsSpan().CopyTo(span.Slice(off, 16));

            return buf;
        }

        public static SetKeyReq Decode(ReadOnlySpan<Byte> body)
        {
            if (body.Length < 39) throw new InvalidDataException("CM_SET_KEY.REQ truncated.");

            return new SetKeyReq(
                KeyType       : body[2],
                MyNonce       : System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(body.Slice(3, 4)),
                YourNonce     : System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(body.Slice(7, 4)),
                Pid           : body[11],
                Prn           : System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(body.Slice(12, 2)),
                Pmn           : body[14],
                CCoCapability : body[15],
                Nid           : body.Slice(16, 7).ToArray(),
                NewEks        : body[23],
                NewKey        : body.Slice(24, 16).ToArray());
        }
    }

}
