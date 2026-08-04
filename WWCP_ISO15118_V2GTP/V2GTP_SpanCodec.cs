/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

using cloud.charging.open.protocols.ISO15118.V2GTP;

namespace cloud.charging.open.protocols.ISO15118.EXI.Dispatch
{
    /// <summary>
    /// The V2GTP header as flat spans and <c>Try…</c> returns, for the EXI codec path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A second face on <see cref="V2GTP_Header"/>, not a second implementation. The record struct
    /// next door is the model — it parses, it validates, it round-trips, and SDP uses it that way.
    /// This is the same header for callers reading frames off a socket into a rented buffer, where
    /// allocating a header object per frame and catching an exception per malformed one is the
    /// wrong shape.
    /// </para>
    /// <para>
    /// The two used to be separate implementations in separate projects, and they disagreed: this
    /// one had the ISO 15118-20 payload ids right, the other had them shifted by one — no -20
    /// mainstream id at all, and WPT sitting on ACDP's value. Every number now comes from
    /// <see cref="V2GTP_PayloadType"/> and nowhere else, so they cannot drift apart again.
    /// </para>
    /// </remarks>
    public static class V2GTP
    {
        public const byte ProtocolVersion        = V2GTP_ProtocolVersion.Current;
        public const byte InverseProtocolVersion = V2GTP_ProtocolVersion.Inverse;

        // The SupportedAppProtocol handshake uses the SAP/EXI-encoded payload id 0x8001 — the SAME id the
        // DIN/-2 messages use (ISO 15118-20 §A / libcbv2g V2GTP20_SAP_PAYLOAD_ID / Josev's ISOV20PayloadTypes.SAP).
        // SAP and -2 frames are told apart by session phase (SAP is always first), NOT by payload type, so this
        // is not a distinct wire value. It is kept as a named alias for the SAP framing path (which decodes SAP
        // explicitly rather than through the payload-type dispatcher). A live interop run against Josev caught
        // the earlier 0x8000 here as a wire-conformance bug — see docs/interop-runs/2026-07-21-iso20-dc-pnc-tcp/.
        public const ushort PayloadType_AppProtocol      = (ushort) V2GTP_PayloadType.ExiSupportedAppProtocol;
        public const ushort PayloadType_DinIso2Main      = (ushort) V2GTP_PayloadType.ExiMainstream;
        public const ushort PayloadType_Iso20Main        = (ushort) V2GTP_PayloadType.ExiIso20Mainstream;
        public const ushort PayloadType_Iso20AC          = (ushort) V2GTP_PayloadType.ExiAC;
        public const ushort PayloadType_Iso20DC          = (ushort) V2GTP_PayloadType.ExiDC;
        public const ushort PayloadType_Iso20ACDP        = (ushort) V2GTP_PayloadType.ExiACDP;
        public const ushort PayloadType_Iso20WPT         = (ushort) V2GTP_PayloadType.ExiWPT;

        public const int HeaderSize = V2GTP_Header.Size;

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

            V2GTP_Header.Standard((V2GTP_PayloadType) payloadType, payloadLength).WriteTo(dest);
            return HeaderSize;
        }

        public static bool TryReadHeader(
            ReadOnlySpan<byte> src, out ushort payloadType, out uint payloadLength)
        {
            payloadType = 0; payloadLength = 0;

            // TryParseRaw skips the version-complement check so pentest tooling can inspect a
            // deliberately malformed header; this path wants it enforced, as the old free function did.
            if (!V2GTP_Header.TryParseRaw(src, out var header) || !header.IsVersionValid)
                return false;

            payloadType   = (ushort) header.PayloadType;
            payloadLength = header.PayloadLength;
            return true;
        }
    }
}
