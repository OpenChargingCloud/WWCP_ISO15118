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
    /// A complex type with element content. <see cref="Sequence"/> holds the type's OWN
    /// particles; if <see cref="BaseTypeRef"/> is set (xs:complexContent/xs:extension), the
    /// grammar builder prepends the (recursively flattened) base particles.
    /// </summary>
    internal sealed record XsdComplexType(
        string                    Name,
        IReadOnlyList<XsdElement>  Sequence,
        string?                   BaseTypeRef = null,   // extension base (may carry a prefix)
        bool                      IsAbstract  = false,
        IReadOnlyList<XsdAttribute>? Attributes = null,
        IReadOnlyList<XsdElement>? Choice = null,        // xs:choice content (mutually exclusive with Sequence)
        string?                   SimpleContentBase = null); // xs:simpleContent/xs:extension base (a value + attributes)
}
