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
    /// Turns a target-language-agnostic <see cref="SchemaPlan"/> into source text for one
    /// language. This is the seam between the shared front end (<c>Xsd/</c> → <c>Grammar/</c>,
    /// which parses the schema set and derives the EXI grammar) and the per-language back end.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Everything upstream of this interface is language-neutral: the plan names types via
    /// <see cref="TypeRef"/> / <see cref="PrimitiveKind"/> and never spells a language's syntax.
    /// An emitter owns that mapping — see <c>CSharpSyntax</c> for the C# one.
    /// </para>
    /// <para>
    /// Only the C# emitter can run inside the Roslyn incremental generator, which by definition
    /// contributes C# to the current compilation. Other languages are emitted by a separate
    /// driver that reuses the same front end and writes files to disk; the interface is
    /// deliberately free of Roslyn types so that driver needs no Roslyn dependency.
    /// </para>
    /// </remarks>
    internal interface ICodecEmitter
    {
        /// <summary>Short identifier of the target language, e.g. <c>"csharp"</c>.</summary>
        string Language { get; }

        /// <summary>
        /// Extension for the generated artefacts, including the leading dot — e.g. <c>".g.cs"</c>.
        /// </summary>
        string FileExtension { get; }

        /// <summary>
        /// Emit the codec source for <paramref name="plan"/>, as one or more files.
        /// </summary>
        /// <remarks>
        /// How many files, and what they are called, is the emitter's decision — a language's
        /// conventions and its compiler's limits differ. C# puts everything in one file; Kotlin
        /// emits one per type, because a single ~1 MB file exhausts the Kotlin compiler's heap
        /// and lands every method of a message set in one class file.
        /// </remarks>
        /// <param name="plan">The language-neutral grammar plan for one schema set.</param>
        /// <param name="targetNamespace">
        /// Namespace / package the generated types live in. Emitters map this onto their own
        /// notion of one (C# <c>namespace</c>, Kotlin <c>package</c>, a Swift enum, …).
        /// </param>
        /// <param name="codecClassName">Name of the generated codec type.</param>
        IReadOnlyList<GeneratedFile> Emit(SchemaPlan plan, string targetNamespace, string codecClassName);
    }
}
