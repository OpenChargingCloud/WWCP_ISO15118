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
    // CM_ATTEN_CHAR.IND     EVSE → EV   (broadcast)
    // =========================================================================
    public sealed record AttenCharInd(
        MACAddress SourceAddress,    // EV MAC
        RunId      RunId,
        byte[]     SourceId,          // 17 bytes
        byte[]     ResponseId,        // 17 bytes
        byte       NumSounds,
        byte[]     AttenProfile) : ISlacMessage   // NumGroups bytes (typ. 58)
    {
        public ManagementMessageType MmType => ManagementMessageType.CM_ATTEN_CHAR_IND;

        public Byte[] Encode()
        {
            var numGroups = (byte) AttenProfile.Length;
            var buf = new Byte[1 + 1 + 6 + 8 + 17 + 17 + 1 + 1 + numGroups];
            var span = buf.AsSpan();
            var off = 0;

            span[off++] = SLACConstants.ApplicationType_PevEvseMatching;
            span[off++] = SLACConstants.SecurityType_None;

            SourceAddress.CopyTo(span.Slice(off, 6)); off += 6;
            RunId.CopyTo(span.Slice(off, 8));         off += 8;

            EnsureLength(SourceId,   17, nameof(SourceId));
            SourceId.AsSpan().CopyTo(span.Slice(off, 17)); off += 17;

            EnsureLength(ResponseId, 17, nameof(ResponseId));
            ResponseId.AsSpan().CopyTo(span.Slice(off, 17)); off += 17;

            span[off++] = NumSounds;
            span[off++] = numGroups;

            AttenProfile.AsSpan().CopyTo(span.Slice(off, numGroups));

            return buf;
        }

        public static AttenCharInd Decode(ReadOnlySpan<Byte> body)
        {
            if (body.Length < 52) throw new InvalidDataException("CM_ATTEN_CHAR.IND truncated.");

            var src         = MACAddress.From(body.Slice(2, 6));
            var runId       = new RunId(body.Slice(8, 8));
            var sourceId    = body.Slice(16, 17).ToArray();
            var responseId  = body.Slice(33, 17).ToArray();
            var numSounds   = body[50];
            var numGroups   = body[51];

            if (body.Length < 52 + numGroups)
                throw new InvalidDataException("CM_ATTEN_CHAR.IND attenuation profile truncated.");

            var profile = body.Slice(52, numGroups).ToArray();

            return new AttenCharInd(src, runId, sourceId, responseId, numSounds, profile);
        }

        private static void EnsureLength(byte[] data, int expected, string name)
        {
            if (data is null || data.Length != expected)
                throw new ArgumentException($"{name} must be exactly {expected} bytes long.", name);
        }
    }

}
