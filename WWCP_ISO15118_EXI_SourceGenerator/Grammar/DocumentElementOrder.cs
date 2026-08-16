/*
 * Copyright (c) 2021-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

namespace cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Grammar
{

    /// <summary>
    /// How the document grammar numbers a schema's global elements — the "document element selector"
    /// every encoded message opens with.
    ///
    /// <para>
    /// The two modes differ for exactly one schema in ISO 15118: <b>ACDP</b>. Everywhere else they
    /// produce the identical numbering, because the difference only appears when two global elements
    /// share one named type, and ACDP is the only set where that happens —
    /// <c>ACDP_DisconnectReq</c>/<c>Res</c> have no types of their own (ISO commented the declarations
    /// out) and reuse <c>ACDP_ConnectReqType</c>/<c>ResType</c>.
    /// </para>
    ///
    /// <para>
    /// This is a real fork in the wire format, not a stylistic one, and it was found by pointing an
    /// independent codec at frames nobody else had ever read: see
    /// <c>ISO15118ConformanceTests/docs/interop-runs/2026-08-07-exificient-iso20/</c>. Two of the
    /// eight ACDP messages encode differently under the two modes, and one of them is decoded as a
    /// <i>different message</i> by a peer that disagrees with the encoder — which is worse than a
    /// decode error, because nothing reports it.
    /// </para>
    /// </summary>
    internal enum DocumentElementOrder
    {

        /// <summary>
        /// <b>Default.</b> What cbexigen does, and therefore what libcbv2g, EVerest and tux-evse do:
        /// elements sharing a named type are grouped immediately after the alphabetically-first element
        /// of that type, ahead of whatever would otherwise sort between them. Verified against cbV2G's
        /// <c>encode_iso20_acdp_exiDocument</c>: ConnectReq=0, DisconnectReq=1, ConnectRes=2,
        /// DisconnectRes=3.
        ///
        /// <para>
        /// Keeping this the default is deliberate. The checked-in vector corpus is cbV2G's output and is
        /// the project's authoritative oracle; changing the default would invalidate it and would put us
        /// out of byte-agreement with every cbexigen-derived stack in the field.
        /// </para>
        /// </summary>
        CbV2GCompatible,

        /// <summary>
        /// Plain lexicographic order over the element qname — name first, then namespace — which is what
        /// EXIficient implements and what a schema-informed EXI processor built from the specification
        /// rather than from cbexigen will expect: ConnectReq=0, ConnectRes=1, DisconnectReq=2,
        /// DisconnectRes=3.
        ///
        /// <para>
        /// Opt in with <c>&lt;ExiDocumentElementOrder&gt;ExiSorted&lt;/ExiDocumentElementOrder&gt;</c>.
        /// <b>This is what everything here builds with since 2026-08-08</b> —
        /// <c>Directory.Build.props</c> sets it for the C# codecs, and the three language ports pass
        /// <c>--doc-order ExiSorted</c> to <c>EVSimulatorApp.Codegen</c>. The default below stays
        /// cbexigen-compatible because it is the library's answer for a caller who has not decided;
        /// this repository has.
        /// </para>
        /// </summary>
        ExiSorted

    }

}
