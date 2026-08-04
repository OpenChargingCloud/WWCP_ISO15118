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

using System.Collections.Generic;

namespace cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Xsd
{
    /// <summary>
    /// Simple type derived by restriction from a single base. Value-space facets cover
    /// only what AppProtocol uses: integer min/max bounds, string maxLength, and enumeration.
    /// </summary>
    internal sealed class XsdSimpleType
    {
        public string Name { get; set; } = "";
        public string Base { get; set; } = "";   // e.g. "xs:unsignedByte" or "xs:string"

        public long?  MinInclusive { get; set; }
        public long?  MaxInclusive { get; set; }
        public int?   MaxLength    { get; set; }

        /// <summary>Lexicographically sorted (per EXI canonical ordering).</summary>
        public List<string>? Enumeration { get; set; }
    }
}
