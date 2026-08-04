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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Emit;
using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Grammar;
using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Xsd;

namespace cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator
{
    /// <summary>
    /// Roslyn incremental source generator that produces an EXI codec from an XSD
    /// schema supplied as <c>&lt;AdditionalFiles&gt;</c>.
    ///
    /// <para>Hook-up in a consumer project:</para>
    /// <code>
    ///   &lt;ItemGroup&gt;
    ///     &lt;AdditionalFiles Include="Schemas\V2G_CI_AppProtocol.xsd" /&gt;
    ///     &lt;ProjectReference Include="..\WWCP_ISO15118_EXI_SourceGenerator\WWCP_ISO15118_EXI_SourceGenerator.csproj"
    ///                       OutputItemType="Analyzer"
    ///                       ReferenceOutputAssembly="false" /&gt;
    ///   &lt;/ItemGroup&gt;
    /// </code>
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class ExiCodecGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Collect ALL .xsd AdditionalFiles of the compilation as one schema set. A set may
            // span several files and namespaces linked by <xs:import>; types are resolved across
            // the whole set. (A project with a single XSD — e.g. AppProtocol — is a set of one.)
            var xsdFiles = context.AdditionalTextsProvider
                .Where(f => Path.GetExtension(f.Path).Equals(".xsd", StringComparison.OrdinalIgnoreCase))
                .Select((f, ct) => (Path: f.Path, Content: f.GetText(ct)?.ToString() ?? ""))
                .Collect();

            // The generated C# namespace and codec class name are configurable so that several codecs
            // (AppProtocol, ISO 15118-2, …) can coexist in one solution without colliding. Defaults keep
            // the AppProtocol prototype working unchanged.
            var config = context.AnalyzerConfigOptionsProvider.Select((p, _) =>
            (
                Ns:    p.GlobalOptions.TryGetValue("build_property.ExiGeneratedNamespace", out var ns) && ns.Length > 0
                           ? ns : "cloud.charging.open.protocols.ISO15118.AppProtocol.Generated",
                Codec: p.GlobalOptions.TryGetValue("build_property.ExiCodecClassName", out var cc) && cc.Length > 0
                           ? cc : "SupportedAppProtocolCodec",
                // Comma/space-separated global element names to emit EXI fragment codecs for (XMLDSig).
                Fragments: p.GlobalOptions.TryGetValue("build_property.ExiFragmentElements", out var fe) && fe.Length > 0
                           ? fe.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                           : System.Array.Empty<string>()
            ));

            context.RegisterSourceOutput(xsdFiles.Combine(config),
                (spc, pair) => Generate(spc, pair.Left, pair.Right.Ns, pair.Right.Codec, pair.Right.Fragments));
        }

        /// <summary>
        /// The back end this generator drives. Roslyn contributes C# to the compilation it runs in,
        /// so inside the generator this is fixed; other languages reuse the same front end
        /// (<c>Xsd/</c> → <c>Grammar/</c>) through <see cref="ICodecEmitter"/> from a separate,
        /// Roslyn-free driver.
        /// </summary>
        private static readonly ICodecEmitter Emitter = CSharpCodecEmitter.Instance;

        private static void Generate(SourceProductionContext spc, ImmutableArray<(string Path, string Content)> files,
                                     string generatedNamespace, string codecClass, string[] fragmentElements)
        {
            if (files.IsDefaultOrEmpty) return;

            var label = Path.GetFileNameWithoutExtension(files[0].Path);

            XsdSchema schema;
            try
            {
                schema = XsdReader.ParseSet(files.Select(f => f.Content));
            }
            catch (XsdReaderException ex)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.UnsupportedConstruct, Location.None, label, ex.Message));
                return;
            }
            catch (Exception ex)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.XsdParseError, Location.None, label, ex.Message));
                return;
            }

            SchemaPlan plan;
            try
            {
                plan = GrammarBuilder.Build(schema, fragmentElements);
            }
            catch (NotSupportedException ex)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.UnsupportedConstruct, Location.None, label, ex.Message));
                return;
            }
            catch (Exception ex)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.InternalError, Location.None, label, ex.Message));
                return;
            }

            IReadOnlyList<GeneratedFile> generated;
            try
            {
                generated = Emitter.Emit(plan, generatedNamespace, codecClass);
            }
            catch (Exception ex)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.InternalError, Location.None, label, ex.Message));
                return;
            }

            foreach (var file in generated)
                spc.AddSource(file.FileName, SourceText.From(file.Source, System.Text.Encoding.UTF8));
        }
    }
}
