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

#region Usings

using System.Buffers.Binary;

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SLAC.Messages
{

    // =========================================================================
    // CM_SLAC_MATCH.CNF     EVSE → EV   (unicast)
    // =========================================================================
    public sealed record SlacMatchCnf(
        byte[]     PevId,
        MACAddress PevMac,
        byte[]     EvseId,
        MACAddress EvseMac,
        RunId      RunId,
        byte[]     Nid,            // 7 bytes
        byte[]     Nmk) : ISlacMessage   // 16 bytes
    {
        public ManagementMessageType MmType => ManagementMessageType.CM_SLAC_MATCH_CNF;

        /// <summary>Length of the variable-format field. Per ISO 15118-3 / HPGP: 0x0056 (86).</summary>
        public const ushort MatchVarFieldLength = 0x0056;

        public Byte[] Encode()
        {
            var buf = new Byte[1 + 1 + 2 + 17 + 6 + 17 + 6 + 8 + 8 + 7 + 1 + 16];
            var span = buf.AsSpan();
            var off = 0;

            span[off++] = SLACConstants.ApplicationType_PevEvseMatching;
            span[off++] = SLACConstants.SecurityType_None;

            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(off, 2), MatchVarFieldLength);
            off += 2;

            EnsureLength(PevId,  17, nameof(PevId));
            PevId.AsSpan().CopyTo(span.Slice(off, 17));  off += 17;

            PevMac.CopyTo(span.Slice(off, 6));           off += 6;

            EnsureLength(EvseId, 17, nameof(EvseId));
            EvseId.AsSpan().CopyTo(span.Slice(off, 17)); off += 17;

            EvseMac.CopyTo(span.Slice(off, 6));          off += 6;

            RunId.CopyTo(span.Slice(off, 8));            off += 8;

            // 8 reserved bytes
            off += 8;

            EnsureLength(Nid, SLACConstants.NidLength, nameof(Nid));
            Nid.AsSpan().CopyTo(span.Slice(off, SLACConstants.NidLength));
            off += SLACConstants.NidLength;

            // 1 reserved byte
            off += 1;

            EnsureLength(Nmk, SLACConstants.NmkLength, nameof(Nmk));
            Nmk.AsSpan().CopyTo(span.Slice(off, SLACConstants.NmkLength));

            return buf;
        }

        public static SlacMatchCnf Decode(ReadOnlySpan<Byte> body)
        {
            if (body.Length < 90) throw new InvalidDataException("CM_SLAC_MATCH.CNF truncated.");

            return new SlacMatchCnf(
                PevId  : body.Slice(4, 17).ToArray(),
                PevMac : MACAddress.From(body.Slice(21, 6)),
                EvseId : body.Slice(27, 17).ToArray(),
                EvseMac: MACAddress.From(body.Slice(44, 6)),
                RunId  : new RunId(body.Slice(50, 8)),
                // body.Slice(58, 8) reserved
                Nid    : body.Slice(66, 7).ToArray(),
                // body[73] reserved
                Nmk    : body.Slice(74, 16).ToArray()
            );
        }

        private static void EnsureLength(byte[] data, int expected, string name)
        {
            if (data is null || data.Length != expected)
                throw new ArgumentException($"{name} must be exactly {expected} bytes long.", name);
        }
    }

}
