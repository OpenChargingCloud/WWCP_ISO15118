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

namespace cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Xsd
{
    /// <summary>
    /// In-memory model of the XSD subset the generator understands.
    /// Deliberately minimal: only the constructs needed for V2G_CI_AppProtocol.xsd.
    /// </summary>
    internal sealed class XsdSchema
    {
        public string TargetNamespace { get; set; } = "";
        public List<XsdElement> GlobalElements { get; } = new();
        public Dictionary<string, XsdSimpleType> SimpleTypes { get; } = new();
        public Dictionary<string, XsdComplexType> ComplexTypes { get; } = new();

        /// <summary>
        /// Local names of global elements declared in an <em>opaque</em> namespace — one whose
        /// full grammar the generator deliberately does not model yet (currently only XMLDSig).
        /// Types from such a namespace are never built; a reference to one of these elements
        /// becomes an opaque, encode-absent/round-trip-only child (see the Signature reference
        /// in the ISO 15118-2 message header). Full fidelity is deferred to Phase 3.
        /// </summary>
        public HashSet<string> OpaqueElementNames { get; } = new();

        /// <summary>
        /// Every element declaration of the collected set (global AND local, all namespaces incl.
        /// XMLDSig), as (localName, namespace) pairs. This is the production set of the EXI fragment
        /// grammar (§8.5.3): sorted by name then namespace, each gets an event code (used to encode a
        /// signable element as a standalone fragment for XMLDSig).
        /// </summary>
        public HashSet<(string Name, string Namespace)> AllElementDeclarations { get; } = new();

        /// <summary>Maps an element declaration (name, namespace) to its <c>type</c> reference, so a
        /// signable fragment element (which may be local, e.g. SalesTariff) can be tied to its record.</summary>
        public Dictionary<(string Name, string Namespace), string> ElementTypeRefs { get; } = new();
    }
}
