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

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Emit;
using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Grammar;
using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Xsd;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure
{
    /// <summary>
    /// Drives a back end over synthetic or real XSDs, and returns the files it produces.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="GeneratorHarness"/> reaches the C# back end the way production does, through
    /// Roslyn, but it sees only source text. This one goes at the emitters directly, so a test can
    /// ask what the *files* are — and it is the only route to a port back end at all, whose sole
    /// production caller is the Codegen driver.
    /// </para>
    /// <para>
    /// This file is source-linked into the app's codegen test project, which is where the Kotlin,
    /// Swift and TypeScript back ends live. That is why the back end is a parameter here rather
    /// than a default: a default of Kotlin would tie this file to an emitter that is no longer in
    /// this repository. Only the C# back end, which every port is compared against, has a
    /// convenience method.
    /// </para>
    /// </remarks>
    internal static class EmitterHarness
    {
        /// <param name="files">(file name, xsd content) pairs forming ONE schema set.</param>
        public static IReadOnlyList<GeneratedFile> Emit(
            ICodecEmitter emitter, string target, string codec,
            params (string Name, string Xsd)[] files) =>
            Emit(emitter, target, codec, [], files);

        /// <summary>The C# back end — the reference every port back end is diffed against.</summary>
        public static IReadOnlyList<GeneratedFile> EmitCSharp(
            string targetNamespace, string codecClass, params (string Name, string Xsd)[] files) =>
            Emit(CSharpCodecEmitter.Instance, targetNamespace, codecClass, [], files);

        public static IReadOnlyList<GeneratedFile> Emit(
            ICodecEmitter emitter, string target, string codec, string[] fragments,
            params (string Name, string Xsd)[] files)
        {
            var schema = XsdReader.ParseSet(files.Select(f => f.Xsd));
            var plan   = GrammarBuilder.Build(schema, fragments);
            return emitter.Emit(plan, target, codec);
        }

        /// <summary>Every <c>.xsd</c> of a schema set that ships with one of the sibling projects.</summary>
        /// <remarks>
        /// Anchored on this file's own compile-time path, not on a walk up from the test binary.
        /// Source-linked into the app's test project, that walk would climb past the app's root
        /// without ever passing the schema sets, which are inside a submodule two levels down.
        /// </remarks>
        public static (string Name, string Xsd)[] RealSchemaSet(string projectName)
        {
            // …/Vanaheimr.V2G.Exi.Tests/Infrastructure/EmitterHarness.cs → the directory holding
            // every codec project.
            var root    = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(ThisFile())!, "..", ".."));
            var schemas = Path.Combine(root, projectName, "Schemas");

            if (!Directory.Exists(schemas))
                throw new DirectoryNotFoundException(
                    $"{projectName} has no Schemas/ under {root} (anchored on {ThisFile()})");

            return Directory.GetFiles(schemas, "*.xsd")
                            .Select(f => (Path.GetFileName(f), File.ReadAllText(f)))
                            .ToArray();
        }

        /// <summary>
        /// This source file's own path. <see cref="CallerFilePathAttribute"/> is filled in at the
        /// *call site*, so it has to be read from a call that lives in this file — asking for it on
        /// <see cref="RealSchemaSet"/> directly yields whichever test file called it, and those sit
        /// at two different depths even before this harness is linked into another repository.
        /// </summary>
        private static string ThisFile([CallerFilePath] string path = "") => path;

        // ---- shapes the tests assert against ------------------------------------------------

        /// <summary>A top-level declaration: `class Foo`, `enum class Bar`, `internal fun encodeFoo`, …</summary>
        public static readonly Regex TopLevelDeclaration =
            new(@"^(?<modifier>internal |private |public )?"
              + @"(?<keyword>enum class|data class|abstract class|open class|sealed class|class|object|fun)"
              + @" (?<name>[A-Za-z_][A-Za-z0-9_]*)",
                RegexOptions.Compiled);

        /// <summary>A call to a generated per-type codec, as the codec object and nested types make it.</summary>
        public static readonly Regex CodecCall =
            new(@"(?<![A-Za-z0-9_.])(?<name>(?:encode|decode)[A-Za-z_][A-Za-z0-9_]*)\s*\(",
                RegexOptions.Compiled);

        public static IEnumerable<string> Lines(GeneratedFile file) =>
            file.Source.Replace("\r\n", "\n").Split('\n');

        /// <summary>Declarations at column 0, i.e. everything the file contributes to the package.</summary>
        public static IEnumerable<Match> TopLevelDeclarations(GeneratedFile file) =>
            Lines(file).Select(l => TopLevelDeclaration.Match(l)).Where(m => m.Success);
    }
}
