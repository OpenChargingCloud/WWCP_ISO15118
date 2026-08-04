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

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure
{
    /// <summary>
    /// Drives <see cref="ExiCodecGenerator"/> over synthetic mini-XSDs so the grammar/emit
    /// pipeline can be unit-tested construct by construct: feed one or more <c>.xsd</c>
    /// documents, inspect the generator's diagnostics and the generated C# source.
    /// </summary>
    public static class GeneratorHarness
    {
        /// <param name="GeneratedSource">
        /// Every generated file, concatenated — for text assertions only. The generator emits one
        /// file per type, so this is not a compilation unit: each part carries its own using
        /// directives and namespace block. Compile <see cref="Sources"/>, not this.
        /// </param>
        /// <param name="Sources">The generated files, one entry per compilation unit.</param>
        public sealed record Result(ImmutableArray<Diagnostic> Diagnostics,
                                    string GeneratedSource,
                                    ImmutableArray<string> Sources)
        {
            public bool HasErrors => Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error ||
                                                          d.Severity == DiagnosticSeverity.Warning);
        }

        /// <param name="files">(fileName ending in .xsd, xsd content) pairs — one schema set.</param>
        public static Result Run(params (string name, string xsd)[] files)
        {
            var compilation = CSharpCompilation.Create(
                "GrammarTestAsm",
                syntaxTrees: null,
                references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var additional = files
                .Select(f => (AdditionalText)new InMemoryAdditionalText(f.name, f.xsd))
                .ToImmutableArray();

            GeneratorDriver driver = CSharpGeneratorDriver
                .Create(new ExiCodecGenerator().AsSourceGenerator())
                .AddAdditionalTexts(additional);

            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult();

            var perGenerator = runResult.Results.Single();
            var sources = runResult.GeneratedTrees.Select(t => t.ToString()).ToImmutableArray();
            return new Result(perGenerator.Diagnostics, string.Concat(sources), sources);
        }

        /// <summary>
        /// Compiles generated codec source against the runtime + a set of extra assemblies (the
        /// Prototype, for <c>BitWriter</c>/<c>ExiPrimitives</c>), returning only the compile errors.
        /// Lets grammar tests assert that generated C# for a construct actually builds, not just that
        /// its text matches — important for paths the checked-in vector projects don't yet exercise.
        /// </summary>
        public static ImmutableArray<Diagnostic> CompileErrors(Result result, params Type[] extraReferenceTypes) =>
            CompileErrors(result.Sources, extraReferenceTypes);

        public static ImmutableArray<Diagnostic> CompileErrors(IEnumerable<string> sources, params Type[] extraReferenceTypes)
        {
            var tpa = ((string)System.AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(System.IO.Path.PathSeparator);
            var refs = tpa.Where(p => p.EndsWith(".dll", System.StringComparison.OrdinalIgnoreCase))
                          .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
                          .ToList();
            foreach (var t in extraReferenceTypes)
                refs.Add(MetadataReference.CreateFromFile(t.Assembly.Location));

            var trees = sources.Select(s => CSharpSyntaxTree.ParseText(
                                            s, new CSharpParseOptions(LanguageVersion.Preview)));
            var comp = CSharpCompilation.Create("GenCompileAsm", trees, refs,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return comp.GetDiagnostics()
                       .Where(d => d.Severity == DiagnosticSeverity.Error)
                       .ToImmutableArray();
        }

        private sealed class InMemoryAdditionalText : AdditionalText
        {
            private readonly string _text;
            public InMemoryAdditionalText(string path, string text) { Path = path; _text = text; }
            public override string Path { get; }
            public override SourceText GetText(System.Threading.CancellationToken cancellationToken = default)
                => SourceText.From(_text, System.Text.Encoding.UTF8);
        }
    }
}
