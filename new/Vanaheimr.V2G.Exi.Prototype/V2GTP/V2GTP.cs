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

namespace cloud.charging.open.protocols.ISO15118.EXI.Dispatch
{
    /// <summary>
    /// V2G Transfer Protocol frame (8-byte header wrapping an EXI payload).
    /// </summary>
    public static class V2GTP
    {
        public const byte ProtocolVersion        = 0x01;
        public const byte InverseProtocolVersion = 0xFE;

        // The SupportedAppProtocol handshake uses the SAP/EXI-encoded payload id 0x8001 — the SAME id the
        // DIN/-2 messages use (ISO 15118-20 §A / libcbv2g V2GTP20_SAP_PAYLOAD_ID / Josev's ISOV20PayloadTypes.SAP).
        // SAP and -2 frames are told apart by session phase (SAP is always first), NOT by payload type, so this
        // is not a distinct wire value. It is kept as a named alias for the SAP framing path (which decodes SAP
        // explicitly rather than through the payload-type dispatcher). A live interop run against Josev caught
        // the earlier 0x8000 here as a wire-conformance bug — see docs/interop-runs/2026-07-21-iso20-dc-pnc-tcp/.
        public const ushort PayloadType_AppProtocol      = 0x8001;
        public const ushort PayloadType_DinIso2Main      = 0x8001;
        public const ushort PayloadType_Iso20Main        = 0x8002;
        public const ushort PayloadType_Iso20AC          = 0x8003;
        public const ushort PayloadType_Iso20DC          = 0x8004;
        public const ushort PayloadType_Iso20ACDP        = 0x8005;
        public const ushort PayloadType_Iso20WPT         = 0x8006;

        public const int HeaderSize = 8;

        /// <summary>
        /// The largest payload a reader will allocate for. A <b>receive</b> limit, not a wire change.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The header's length is a 32-bit count supplied by the peer, so believing it means letting
        /// whoever answered the socket choose an allocation of up to 4 GiB — out of one 8-byte frame
        /// that carries no payload at all. On a phone that is an out-of-memory kill; the frame that
        /// asked for it costs the sender nothing.
        /// </para>
        /// <para>
        /// Nothing legitimate comes close. The largest frame in any recorded session is a -20
        /// <c>AuthorizationReq</c> carrying a contract chain, at <b>921 bytes</b>; the largest EXI
        /// vector in the corpus is 266. A megabyte is three orders of magnitude of headroom, so this
        /// refuses only what no encoder here can produce.
        /// </para>
        /// <para>
        /// Encoding is unaffected: a payload too large for its length field already fails at
        /// <see cref="V2GTPDispatcher"/>. This is about what a reader will believe.
        /// </para>
        /// </remarks>
        public const int MaximumPayloadBytes = 1 << 20;

        public static int WriteHeader(Span<byte> dest, ushort payloadType, uint payloadLength)
        {
            if (dest.Length < HeaderSize)
                throw new ArgumentException("Need at least 8 bytes for V2GTP header.", nameof(dest));

            dest[0] = ProtocolVersion;
            dest[1] = InverseProtocolVersion;
            BinaryPrimitives.WriteUInt16BigEndian(dest[2..4], payloadType);
            BinaryPrimitives.WriteUInt32BigEndian(dest[4..8], payloadLength);
            return HeaderSize;
        }

        public static bool TryReadHeader(
            ReadOnlySpan<byte> src, out ushort payloadType, out uint payloadLength)
        {
            payloadType = 0; payloadLength = 0;
            if (src.Length < HeaderSize) return false;
            if (src[0] != ProtocolVersion || src[1] != InverseProtocolVersion) return false;

            payloadType   = BinaryPrimitives.ReadUInt16BigEndian(src[2..4]);
            payloadLength = BinaryPrimitives.ReadUInt32BigEndian(src[4..8]);
            return true;
        }
    }
}
