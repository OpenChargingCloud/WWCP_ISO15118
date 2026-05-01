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

using System.Buffers.Binary;

using org.GraphDefined.Vanaheimr.Hermod.Ethernet;

#endregion

namespace cloud.charging.open.protocols.ISO15118.SLAC.Messages
{

    // =========================================================================
    // CM_MNBC_SOUND.IND     EV → EVSE   (broadcast, sent NUM_SOUNDS times)
    // =========================================================================
    public sealed record MnbcSoundInd(
        byte[] SenderId,        // 17 bytes, typically zero
        byte   Cnt,             // remaining-sound countdown
        RunId  RunId,
        byte[] Random16) : ISlacMessage   // 16 random bytes
    {
        public ManagementMessageType MmType => ManagementMessageType.CM_MNBC_SOUND_IND;

        public Byte[] Encode()
        {
            var buf = new Byte[1 + 1 + 17 + 1 + 8 + 8 + 16];
            var span = buf.AsSpan();
            var off = 0;

            span[off++] = SLACConstants.ApplicationType_PevEvseMatching;
            span[off++] = SLACConstants.SecurityType_None;

            EnsureLength(SenderId, 17, nameof(SenderId));
            SenderId.AsSpan().CopyTo(span.Slice(off, 17));
            off += 17;

            span[off++] = Cnt;

            RunId.CopyTo(span.Slice(off, 8));
            off += 8;

            // _Reserved (8 bytes) left as zero
            off += 8;

            EnsureLength(Random16, 16, nameof(Random16));
            Random16.AsSpan().CopyTo(span.Slice(off, 16));

            return buf;
        }

        public static MnbcSoundInd Decode(ReadOnlySpan<Byte> body)
        {
            if (body.Length < 52) throw new InvalidDataException("CM_MNBC_SOUND.IND truncated.");

            return new MnbcSoundInd(
                SenderId : body.Slice(2, 17).ToArray(),
                Cnt      : body[19],
                RunId    : new RunId(body.Slice(20, 8)),
                // body.Slice(28, 8) reserved
                Random16 : body.Slice(36, 16).ToArray()
            );
        }

        private static void EnsureLength(byte[] data, int expected, string name)
        {
            if (data is null || data.Length != expected)
                throw new ArgumentException($"{name} must be exactly {expected} bytes long.", name);
        }
    }

}
