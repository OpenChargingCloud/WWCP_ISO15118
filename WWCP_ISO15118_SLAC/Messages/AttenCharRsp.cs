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
    // CM_ATTEN_CHAR.RSP     EV → EVSE   (unicast)
    // =========================================================================
    public sealed record AttenCharRsp(
        MACAddress SourceAddress,
        RunId      RunId,
        byte[]     SourceId,
        byte[]     ResponseId,
        byte       Result) : ISlacMessage
    {
        public ManagementMessageType MmType => ManagementMessageType.CM_ATTEN_CHAR_RSP;

        public Byte[] Encode()
        {
            var buf = new Byte[1 + 1 + 6 + 8 + 17 + 17 + 1];
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

            span[off] = Result;
            return buf;
        }

        public static AttenCharRsp Decode(ReadOnlySpan<Byte> body)
        {
            if (body.Length < 51) throw new InvalidDataException("CM_ATTEN_CHAR.RSP truncated.");

            return new AttenCharRsp(
                SourceAddress: MACAddress.From(body.Slice(2, 6)),
                RunId        : new RunId(body.Slice(8, 8)),
                SourceId     : body.Slice(16, 17).ToArray(),
                ResponseId   : body.Slice(33, 17).ToArray(),
                Result       : body[50]
            );
        }

        private static void EnsureLength(byte[] data, int expected, string name)
        {
            if (data is null || data.Length != expected)
                throw new ArgumentException($"{name} must be exactly {expected} bytes long.", name);
        }
    }


}
