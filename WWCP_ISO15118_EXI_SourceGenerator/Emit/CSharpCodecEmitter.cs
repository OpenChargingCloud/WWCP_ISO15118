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
using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Grammar;

namespace cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Emit
{
    /// <summary>
    /// The C# back end: the <see cref="ICodecEmitter"/> face of <see cref="CodecEmitter"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="CodecEmitter"/> stays a self-contained, single-use string builder (it carries
    /// per-emission state), so the seam is a thin stateless adapter over it rather than an
    /// interface bolted onto that class.
    /// </remarks>
    internal sealed class CSharpCodecEmitter : ICodecEmitter
    {
        public static readonly CSharpCodecEmitter Instance = new();

        public string Language      => "csharp";
        public string FileExtension => ".g.cs";

        /// <summary>
        /// One file per type, plus one for the codec class — the same layout as the Kotlin back
        /// end, reached by making the codec class <c>partial</c> rather than by moving anything out
        /// of it — and one more for the JSON-LD (de)serializer.
        /// </summary>
        /// <remarks>
        /// The JSON-LD pass is part of this emitter rather than an emitter of its own, and that is
        /// docs/CONCEPT.md §4.4's actual requirement: "wire codec and JSON-LD codec come from the
        /// same type graph in the same generator pass, so they cannot drift". Two emitters would
        /// leave a seam where someone could regenerate one and not the other; there is no such seam
        /// here.
        /// </remarks>
        public IReadOnlyList<GeneratedFile> Emit(SchemaPlan plan, string targetNamespace, string codecClassName) =>
        [
            .. CodecEmitter.Emit    (plan, targetNamespace, codecClassName),
            .. CSharpJsonEmitter.Emit(plan, targetNamespace, codecClassName),
        ];
    }
}
