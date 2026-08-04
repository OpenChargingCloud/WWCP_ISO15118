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

using System.Text.RegularExpressions;
using NUnit.Framework;
using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Emit;
using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Grammar;
using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Xsd;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure
{
    /// <summary>
    /// Drives a back end over synthetic or real XSDs, and returns the files it produces.
    /// </summary>
    /// <remarks>
    /// <see cref="GeneratorHarness"/> reaches the C# back end the way production does, through
    /// Roslyn, but it sees only source text. This one goes at the emitters directly, so a test can
    /// ask what the *files* are — and it is the only route to the Kotlin back end at all, whose
    /// sole production caller is the Codegen driver.
    /// </remarks>
    internal static class EmitterHarness
    {
        /// <param name="files">(file name, xsd content) pairs forming ONE schema set.</param>
        public static IReadOnlyList<GeneratedFile> Emit(
            string targetPackage, string codecObject, params (string Name, string Xsd)[] files) =>
            Emit(targetPackage, codecObject, [], files);

        public static IReadOnlyList<GeneratedFile> Emit(
            string targetPackage, string codecObject, string[] fragments,
            params (string Name, string Xsd)[] files) =>
            Emit(KotlinCodecEmitter.Instance, targetPackage, codecObject, fragments, files);

        /// <summary>The same, through the C# back end.</summary>
        public static IReadOnlyList<GeneratedFile> EmitCSharp(
            string targetNamespace, string codecClass, params (string Name, string Xsd)[] files) =>
            Emit(CSharpCodecEmitter.Instance, targetNamespace, codecClass, [], files);

        /// <summary>The same, through the Swift back end.</summary>
        public static IReadOnlyList<GeneratedFile> EmitSwift(
            string targetModule, string codecEnum, params (string Name, string Xsd)[] files) =>
            Emit(SwiftCodecEmitter.Instance, targetModule, codecEnum, [], files);

        private static IReadOnlyList<GeneratedFile> Emit(
            ICodecEmitter emitter, string target, string codec, string[] fragments,
            (string Name, string Xsd)[] files)
        {
            var schema = XsdReader.ParseSet(files.Select(f => f.Xsd));
            var plan   = GrammarBuilder.Build(schema, fragments);
            return emitter.Emit(plan, target, codec);
        }

        /// <summary>Every <c>.xsd</c> of a schema set that ships with one of the sibling projects.</summary>
        public static (string Name, string Xsd)[] RealSchemaSet(string projectName)
        {
            var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (root is not null && !Directory.Exists(Path.Combine(root.FullName, projectName)))
                root = root.Parent;
            if (root is null)
                throw new DirectoryNotFoundException($"{projectName} not found above the test directory");

            return Directory.GetFiles(Path.Combine(root.FullName, projectName, "Schemas"), "*.xsd")
                            .Select(f => (Path.GetFileName(f), File.ReadAllText(f)))
                            .ToArray();
        }

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
