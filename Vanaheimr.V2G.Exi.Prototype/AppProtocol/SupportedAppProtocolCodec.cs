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

using cloud.charging.open.protocols.ISO15118.EXI;

namespace cloud.charging.open.protocols.ISO15118.AppProtocol
{
    /// <summary>
    /// Hand-written codec for <c>SupportedAppProtocolReq</c> / <c>SupportedAppProtocolRes</c>.
    /// In production this would be emitted by an <c>IIncrementalGenerator</c> consuming
    /// <c>V2G_CI_AppProtocol.xsd</c> — the generated codec mirrors this shape byte-for-byte.
    ///
    /// <para>
    /// <b>Wire model: non-strict schema-informed EXI grammar.</b> This is the grammar the
    /// ISO 15118 ecosystem actually uses on the wire, as produced by EVerest's cbexigen /
    /// libcbv2g. It is NOT the fully-optimised "strict" grammar (which would collapse
    /// single-production states to 0-bit event codes). Every structural transition carries
    /// an explicit event-code, because the non-strict grammar reserves an additional
    /// production slot (for the generic/EE alternative) at each state:
    /// </para>
    /// <list type="bullet">
    ///   <item>Document element selector: 2 bits (Req = 0, Res = 1).</item>
    ///   <item>Each child element: SE event (1 bit = 0), a value-start event (1 bit = 0),
    ///         the value itself, then an EE event (1 bit = 0).</item>
    ///   <item>The first item of a required list: 1-bit SE (= 0). Every following item and
    ///         the list terminator: 2-bit event code (item = 0, EE = 1).</item>
    ///   <item>An optional element: 2-bit event code (present = 0, EE = 1).</item>
    /// </list>
    ///
    /// <para>Value codings (as cbexigen emits them):</para>
    /// <list type="bullet">
    ///   <item><c>ProtocolNamespace</c> (anyURI) — <c>UInt(len+2)</c> then one octet per
    ///         character. ASCII only; cbV2G rejects code points &gt; U+007F.</item>
    ///   <item><c>VersionNumber*</c> (xs:unsignedInt) — EXI Unsigned Integer (7-bit chunks).</item>
    ///   <item><c>SchemaID</c> (xs:unsignedByte) — 8-bit n-bit unsigned.</item>
    ///   <item><c>Priority</c> (range [1..20]) — 5-bit n-bit unsigned, encoded as value-1.</item>
    ///   <item><c>ResponseCode</c> — 2-bit n-bit unsigned; index is the enumeration's
    ///         <b>declaration order</b> in the XSD (OK = 0, OK-minor = 1, Failed = 2),
    ///         NOT a lexicographic sort.</item>
    /// </list>
    ///
    /// <para><b>Conformance.</b> The byte layout is validated against libcbv2g (a pinned
    /// commit) via the vector suite; see <c>Vectors/AppProtocol.vectors.json</c> and
    /// <c>tools/cbv2g-ref</c>.</para>
    /// </summary>
    public static class SupportedAppProtocolCodec
    {
        /// <summary>EXI header byte: distinguishing bits 10, no options, format version 0.</summary>
        public const byte ExiHeader = 0x80;

        /// <summary>Schema <c>maxOccurs</c> for the AppProtocol list.</summary>
        private const int MaxAppProtocols = 20;

        // ---------------------------------------------------------------------
        //  Encode
        // ---------------------------------------------------------------------

        public static bool TryEncodeRequest(
            SupportedAppProtocolReq msg, Span<byte> dest, out int bytesWritten)
        {
            bytesWritten = 0;

            if (msg.AppProtocols.Count is < 1 or > MaxAppProtocols) return false;
            if (dest.Length < 1) return false;

            dest[0] = ExiHeader;
            var w = new BitWriter(dest[1..]);

            // Document: SE(supportedAppProtocolReq) — 2-bit event code 0.
            w.WriteBits(0, 2);

            for (int i = 0; i < msg.AppProtocols.Count; i++)
            {
                // First entry: 1-bit SE(AppProtocol) = 0.
                // Subsequent entries: 2-bit loop event code = 0.
                w.WriteBits(0, i == 0 ? 1 : 2);
                EncodeAppProtocol(ref w, msg.AppProtocols[i]);
            }

            // List terminator: 2-bit EE = 1 (the supportedAppProtocolReq element's EE).
            w.WriteBits(1, 2);

            w.AlignToByte();
            bytesWritten = 1 + w.BytesWritten;
            return true;
        }

        private static void EncodeAppProtocol(ref BitWriter w, AppProtocolEntry e)
        {
            if (e.Priority is < 1 or > 20)
                throw new ArgumentOutOfRangeException(nameof(e), "Priority must be in [1..20].");

            // ProtocolNamespace (anyURI): SE, value-start, UInt(len+2)+octets, EE.
            w.WriteBits(0, 1); w.WriteBits(0, 1);
            ExiPrimitives.WriteStringValue(ref w, e.ProtocolNamespace);
            w.WriteBits(0, 1);

            // VersionNumberMajor (unsignedInt): SE, value-start, Unsigned Integer, EE.
            w.WriteBits(0, 1); w.WriteBits(0, 1);
            ExiPrimitives.WriteUnsignedInteger(ref w, e.VersionNumberMajor);
            w.WriteBits(0, 1);

            // VersionNumberMinor (unsignedInt).
            w.WriteBits(0, 1); w.WriteBits(0, 1);
            ExiPrimitives.WriteUnsignedInteger(ref w, e.VersionNumberMinor);
            w.WriteBits(0, 1);

            // SchemaID (unsignedByte): SE, value-start, 8-bit, EE.
            w.WriteBits(0, 1); w.WriteBits(0, 1);
            w.WriteBits(e.SchemaID, 8);
            w.WriteBits(0, 1);

            // Priority (range [1..20]): SE, value-start, 5-bit (value-1), EE.
            w.WriteBits(0, 1); w.WriteBits(0, 1);
            w.WriteBits((uint)(e.Priority - 1), 5);
            w.WriteBits(0, 1);

            // EE of the AppProtocol element.
            w.WriteBits(0, 1);
        }

        public static bool TryEncodeResponse(
            SupportedAppProtocolRes msg, Span<byte> dest, out int bytesWritten)
        {
            bytesWritten = 0;
            if (dest.Length < 1) return false;

            dest[0] = ExiHeader;
            var w = new BitWriter(dest[1..]);

            // Document: SE(supportedAppProtocolRes) — 2-bit event code 1.
            w.WriteBits(1, 2);

            // ResponseCode: SE, value-start, 2-bit enum (declaration index), EE.
            w.WriteBits(0, 1); w.WriteBits(0, 1);
            w.WriteBits((uint)msg.Code, 2);   // enum values already match XSD declaration order
            w.WriteBits(0, 1);

            // Optional SchemaID: 2-bit event code (present = 0, EE = 1).
            if (msg.SchemaID is byte schemaId)
            {
                w.WriteBits(0, 2);            // SE(SchemaID)
                w.WriteBits(0, 1);           // value-start
                w.WriteBits(schemaId, 8);
                w.WriteBits(0, 1);           // EE of SchemaID element
                w.WriteBits(0, 1);           // EE of supportedAppProtocolRes element
            }
            else
            {
                w.WriteBits(1, 2);           // EE of supportedAppProtocolRes element
            }

            w.AlignToByte();
            bytesWritten = 1 + w.BytesWritten;
            return true;
        }

        // ---------------------------------------------------------------------
        //  Decode
        // ---------------------------------------------------------------------

        /// <summary>
        /// Decode either a request or response. Caller dispatches on the result type.
        /// </summary>
        public static object DecodeAny(ReadOnlySpan<byte> src, out int bytesConsumed)
        {
            if (src.Length < 2 || src[0] != ExiHeader)
                throw new InvalidDataException("Invalid EXI header for AppProtocol stream.");

            var r = new BitReader(src[1..]);
            uint sel = r.ReadBits(2); // document element selector: 0 = Req, 1 = Res

            object result = sel switch
            {
                0 => DecodeRequestBody(ref r),
                1 => DecodeResponseBody(ref r),
                _ => throw new InvalidDataException($"Unknown document element index {sel}."),
            };
            bytesConsumed = 1 + r.BytesConsumed;
            return result;
        }

        private static SupportedAppProtocolReq DecodeRequestBody(ref BitReader r)
        {
            var entries = new List<AppProtocolEntry>(capacity: 4);

            // First entry mandatory: 1-bit SE.
            r.ReadBits(1);
            entries.Add(DecodeAppProtocol(ref r));

            // Following entries / terminator: 2-bit loop event code.
            while (true)
            {
                uint ec = r.ReadBits(2);
                if (ec == 1) break;          // EE
                if (ec != 0)
                    throw new InvalidDataException($"Unexpected AppProtocol list event code {ec}.");
                if (entries.Count >= MaxAppProtocols)
                    throw new InvalidDataException("AppProtocol list exceeds maxOccurs (20).");
                entries.Add(DecodeAppProtocol(ref r));
            }

            return new SupportedAppProtocolReq(entries);
        }

        private static AppProtocolEntry DecodeAppProtocol(ref BitReader r)
        {
            r.ReadBits(1); r.ReadBits(1);                             // SE, value-start
            var ns = ExiPrimitives.ReadStringValue(ref r, "ProtocolNamespace");
            r.ReadBits(1);                                            // EE

            r.ReadBits(1); r.ReadBits(1);
            var verMaj = checked((uint)ExiPrimitives.ReadUnsignedInteger(ref r));
            r.ReadBits(1);

            r.ReadBits(1); r.ReadBits(1);
            var verMin = checked((uint)ExiPrimitives.ReadUnsignedInteger(ref r));
            r.ReadBits(1);

            r.ReadBits(1); r.ReadBits(1);
            var schemaId = (byte)r.ReadBits(8);
            r.ReadBits(1);

            r.ReadBits(1); r.ReadBits(1);
            var priority = (byte)(r.ReadBits(5) + 1);
            r.ReadBits(1);

            r.ReadBits(1);                                           // EE of the AppProtocol element
            return new AppProtocolEntry(ns, verMaj, verMin, schemaId, priority);
        }

        private static SupportedAppProtocolRes DecodeResponseBody(ref BitReader r)
        {
            r.ReadBits(1); r.ReadBits(1);                            // SE(ResponseCode), value-start
            uint idx = r.ReadBits(2);
            if (idx > 2)
                throw new InvalidDataException($"Reserved/invalid ResponseCode index {idx}.");
            var code = (ResponseCode)idx;
            r.ReadBits(1);                                          // EE(ResponseCode)

            byte? schemaId = null;
            uint ec = r.ReadBits(2);                               // present = 0, EE = 1
            if (ec == 0)
            {
                r.ReadBits(1);                                     // value-start
                schemaId = (byte)r.ReadBits(8);
                r.ReadBits(1);                                     // EE of SchemaID element
                r.ReadBits(1);                                     // EE of supportedAppProtocolRes
            }
            else if (ec != 1)
            {
                throw new InvalidDataException($"Unexpected SchemaID event code {ec}.");
            }

            return new SupportedAppProtocolRes(code, schemaId);
        }
    }
}
