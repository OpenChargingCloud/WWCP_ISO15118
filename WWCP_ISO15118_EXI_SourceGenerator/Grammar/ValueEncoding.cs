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

using System.Collections.Generic;

namespace cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Grammar
{
    /// <summary>
    /// Per-element value encoding plan: which EXI primitive codec applies and with
    /// what parameters.
    /// </summary>
    internal abstract record ValueEncoding
    {
        public sealed record UnsignedInt : ValueEncoding;
        public sealed record SignedInt : ValueEncoding;            // xs:byte/short/int/long → EXI Integer
        public sealed record Binary : ValueEncoding;               // xs:hexBinary / xs:base64Binary → byte[]
        public sealed record StringValue : ValueEncoding;

        /// <summary>
        /// An attribute (AT) value carried inside an optional run: unlike an element, it is a bare
        /// string with no SE / value-start / child-EE wrapper — only the run's event code precedes it.
        /// </summary>
        public sealed record AttributeValue : ValueEncoding;
        public sealed record NBitUnsigned(int BitWidth, long Bias) : ValueEncoding;
        public sealed record EnumIndex(string EnumName, int BitWidth, IReadOnlyList<string> Members) : ValueEncoding;
        public sealed record ComplexRef(string TypeName) : ValueEncoding;

        /// <summary>
        /// A reference to an element in an opaque namespace (XMLDSig). Its grammar is not modelled;
        /// the child is only ever encoded/decoded as <em>absent</em>. Encoding or decoding a present
        /// instance fails loud — full fidelity is deferred to Phase 3. <see cref="TypeName"/> is the
        /// generated empty placeholder record.
        /// </summary>
        public sealed record OpaqueElement(string TypeName) : ValueEncoding;

        /// <summary>
        /// A reference to a substitution-group head: the value is one of several concrete member
        /// types, selected by an n-bit event code. Members are sorted by element name and include
        /// the abstract head element itself (cbexigen assigns it a production slot too).
        /// </summary>
        public sealed record SubstitutionChoice(int BitWidth, IReadOnlyList<SubstMember> Members) : ValueEncoding;

        /// <summary>
        /// An <c>xs:choice</c> nested inside a sequence (ISO 15118-20), flattened into the enclosing
        /// state exactly like a substitution reference — but unlike substitution, each branch is its
        /// OWN independent field in the record, not one polymorphic field (cbexigen models an inline
        /// choice as N sibling <c>_isUsed</c>-flagged fields, verified against
        /// <c>iso20_AuthorizationSetupResType</c>). <see cref="BitWidth"/> is only used for the
        /// standalone dispatch (no surrounding optional run); the run machine sizes the shared state
        /// itself via <c>ProductionCount</c>. Members keep XSD document order (not alphabetical —
        /// verified against <c>SignedInstallationDataType</c>'s 3-member choice).
        /// </summary>
        public sealed record InlineChoice(int BitWidth, IReadOnlyList<InlineChoiceMember> Members) : ValueEncoding;
    }
}
