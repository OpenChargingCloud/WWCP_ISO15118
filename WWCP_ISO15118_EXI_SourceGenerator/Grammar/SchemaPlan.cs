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
    /// The full plan for one schema, ready for emission.
    /// </summary>
    internal sealed record SchemaPlan(
        string                          TargetNamespace,
        IReadOnlyList<GlobalElementPlan> GlobalElements,
        IReadOnlyDictionary<string, SequencePlan> ComplexTypes,
        IReadOnlyList<EnumPlan>         Enums,
        IReadOnlyList<string>           OpaqueTypes,   // empty placeholder records for opaque refs
        int                             DocumentSelectorBits, // width of the document element selector
        int                             FragmentSelectorBits, // width of the EXI fragment element selector
        int                             FragmentEndCode,      // "End Fragment" (ED) event code
        IReadOnlyList<FragmentPlan>     Fragments,     // signable elements to emit fragment codecs for
        ParticleGrammar                 ParticleGrammar = ParticleGrammar.CbV2GCompatible);
}
