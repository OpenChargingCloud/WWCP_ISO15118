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
    /// How one narrow construct is given a grammar: an <b>optional repeating element followed by further
    /// optional elements</b>, all ending the sequence. In ISO 15118 this occurs only in WPT —
    /// <c>WPT_FinePositioning{,Setup}Req/ResType</c>, where <c>VendorSpecificDataContainer</c>
    /// (<c>minOccurs="0" maxOccurs="16"</c>) is followed by an optional <c>WPT_LF_DataPackageList</c>.
    ///
    /// <para>
    /// cbexigen and the schema disagree about it, and the disagreement is not a numbering detail like
    /// <see cref="DocumentElementOrder"/> — the two grammars accept <i>different sets of documents</i>:
    /// </para>
    ///
    /// <code>
    ///   cbexigen  state A (no items yet):  SE(list)=0            EE=1
    ///             state B (one item):      LOOP=0  SE(suffix)=1  EE=2
    ///             state C (two items):             SE(suffix)=0  EE=1     &lt;- no third item, ever
    ///
    ///   schema    state A (no items yet):  SE(list)=0  SE(suffix)=1  EE=2
    ///             state B (n items):       LOOP=0      SE(suffix)=1  EE=2  &lt;- loops to maxOccurs
    /// </code>
    ///
    /// <para>
    /// Two consequences, both verified against cbV2G at <c>03350be048b3</c>
    /// (<c>encode_iso20_wpt_WPT_FinePositioningReqType</c>, grammar ids 178/179/180): a third
    /// <c>VendorSpecificDataContainer</c> cannot be represented at all even though the struct's array is
    /// sized 16, and <c>WPT_LF_DataPackageList</c> is unreachable unless at least one container precedes
    /// it. Both are valid documents per ISO's schema.
    /// </para>
    ///
    /// <para>
    /// It also changes the bits for the ordinary empty case: with no items and no LF list, cbexigen's
    /// end-element is code <b>1</b> and the schema grammar's is <b>2</b>. That single code is why
    /// EXIficient could not read any of the four WPT frames on 2026-08-07 — it read our 1 as
    /// <c>SE(WPT_LF_DataPackageList)</c>, went looking for content that was not there, and reported
    /// <c>Premature EOS</c>. See <c>docs/interop-runs/2026-08-07-exificient-iso20/</c> in the conformance
    /// repository.
    /// </para>
    /// </summary>
    internal enum ParticleGrammar
    {

        /// <summary>
        /// <b>Default.</b> cbexigen's shape, exactly — including the two-item ceiling and the
        /// unreachable suffix. The checked-in WPT vectors are cbV2G's output and are this project's
        /// authoritative oracle; changing the default would invalidate them and put us out of byte
        /// agreement with every cbexigen-derived stack.
        ///
        /// <para>
        /// The generated encoder throws rather than silently truncating when a caller asks for something
        /// this grammar cannot express — a third container, or an LF list with no container.
        /// </para>
        /// </summary>
        CbV2GCompatible,

        /// <summary>
        /// The grammar ISO's schema actually describes: the list loops to its <c>maxOccurs</c>, and each
        /// following optional particle is reachable whether or not the list is empty. Opt in with
        /// <c>&lt;ExiParticleGrammar&gt;SchemaConformant&lt;/ExiParticleGrammar&gt;</c>.
        ///
        /// <para>
        /// Nothing in this repository builds with it yet. It exists so the alternative can be produced
        /// and measured rather than argued about — and because, unlike the document-element ordering,
        /// this one is not a matter of interpreting the EXI specification: the schema says
        /// <c>maxOccurs="16"</c> and makes the suffix independently optional, so a grammar that caps at
        /// two and gates the suffix is generating something its own input does not say.
        /// </para>
        /// </summary>
        SchemaConformant

    }

}
