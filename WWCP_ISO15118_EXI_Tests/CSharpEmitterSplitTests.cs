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

using System.Text.RegularExpressions;
using NUnit.Framework;
using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Emit;
using cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>
    /// The C# back end emits one file per type, the same layout as the Kotlin one — but reached
    /// differently: the codec class becomes <c>partial</c> and its parts are spread over the type
    /// files, so nothing moves out of it and no member changes accessibility.
    /// </summary>
    /// <remarks>
    /// That difference is the reason these are separate tests rather than a shared fixture: what
    /// has to hold is the same, what could go wrong is not. Here the risk is a part that never gets
    /// its <c>partial class</c> wrapper, or a private method that ends up in a different class.
    /// Both are compile errors, and this fixture compiles what it emits.
    /// </remarks>
    [TestFixture]
    public class CSharpEmitterSplitTests
    {
        /// <summary>
        /// Two complex types, an enum, and a global element whose body is a type of its own — so
        /// one type's encoder calls another's, across files and across parts of the same class.
        /// </summary>
        private const string SplitSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                   xmlns="urn:test:split" targetNamespace="urn:test:split">
          <xs:element name="Root" type="RootType"/>
          <xs:complexType name="RootType">
            <xs:sequence>
              <xs:element name="Mode" type="modeType"/>
              <xs:element name="Detail" type="DetailType" minOccurs="0"/>
            </xs:sequence>
          </xs:complexType>
          <xs:complexType name="DetailType">
            <xs:sequence>
              <xs:element name="Count" type="xs:unsignedInt"/>
            </xs:sequence>
          </xs:complexType>
          <xs:simpleType name="modeType">
            <xs:restriction base="xs:string">
              <xs:enumeration value="Fast"/>
              <xs:enumeration value="Slow"/>
            </xs:restriction>
          </xs:simpleType>
        </xs:schema>
        """;

        private static IReadOnlyList<GeneratedFile> Split() =>
            EmitterHarness.EmitCSharp("Test.Split", "SplitCodec", ("split.xsd", SplitSchema));

        private static GeneratedFile File(IReadOnlyList<GeneratedFile> files, string name) =>
            files.SingleOrDefault(f => f.FileName == name)
            ?? throw new AssertionException(
                   $"no file '{name}'; got: {string.Join(", ", files.Select(f => f.FileName))}");

        [Test]
        public void EachTypeGetsItsOwnFileNamedAfterIt()
        {
            Assert.That(Split().Select(f => f.FileName),
                        Is.EquivalentTo(new[]
                        {
                            "Mode.g.cs", "Root.g.cs", "RootType.g.cs", "DetailType.g.cs", "SplitCodec.g.cs",

                            // The JSON-LD pass, split the same way: one part per type, one for the
                            // dispatchers. Enums and opaque placeholders have no JSON part of their
                            // own — an enum is a string wherever it appears.
                            "Root.Json.g.cs", "RootType.Json.g.cs", "DetailType.Json.g.cs",
                            "SplitCodecJson.Json.g.cs",
                        }));
        }

        [Test]
        public void ATypeAndItsCodecShareAFileThroughAPartialClass()
        {
            var detail = File(Split(), "DetailType.g.cs").Source;

            Assert.That(detail, Does.Contain("public sealed record DetailType("));
            Assert.That(detail, Does.Contain("public static partial class SplitCodec"));
            // Still private: parts of one partial class see each other's private members, so the
            // split costs no accessibility — unlike Kotlin, where these had to become `internal`.
            Assert.That(detail, Does.Contain("private static void Encode_DetailType"));
            Assert.That(detail, Does.Contain("private static DetailType Decode_DetailType"));
        }

        [Test]
        public void TheCodecClassFileHoldsOnlyTheEntryPoints()
        {
            var codec = File(Split(), "SplitCodec.g.cs").Source;

            Assert.That(codec, Does.Contain("public static partial class SplitCodec"));
            Assert.That(codec, Does.Contain("public static object DecodeAny("));
            Assert.That(codec, Does.Contain("public const byte ExiHeader = 0x80;"));

            Assert.That(codec, Does.Not.Contain("Encode_DetailType(ref BitWriter"));
            Assert.That(codec, Does.Not.Contain("Decode_DetailType(ref BitReader"));
        }

        [Test]
        public void EveryFileIsItsOwnCompilationUnit()
        {
            foreach (var f in Split())
            {
                var lines = f.Source.Replace("\r\n", "\n").Split('\n');

                Assert.That(lines[0], Is.EqualTo("// <auto-generated/>"),
                            $"{f.FileName} must carry the banner — the driver deletes stale output by it");
                Assert.That(lines.Count(l => l.StartsWith("namespace ")), Is.EqualTo(1), f.FileName);
                Assert.That(lines.Any(l => l == "#nullable enable"), Is.True, f.FileName);
                // Each file repeats the usings, because each is compiled on its own.
                Assert.That(lines.Any(l => l == "using cloud.charging.open.protocols.ISO15118.EXI;"), Is.True, f.FileName);
            }
        }

        [Test]
        public void NoTypeIsDeclaredTwice()
        {
            var declaration = new Regex(
                @"^\s*public (?:sealed |abstract )?(?:record|enum) (?<name>\w+)", RegexOptions.Multiline);

            var seen = new Dictionary<string, string>();
            foreach (var f in Split())
                foreach (Match m in declaration.Matches(f.Source))
                {
                    var name = m.Groups["name"].Value;
                    Assert.That(seen.ContainsKey(name), Is.False,
                                $"{name} is declared in both {seen.GetValueOrDefault(name)} and {f.FileName}");
                    seen[name] = f.FileName;
                }
        }

        /// <summary>
        /// The one that would catch a missing <c>partial class</c> wrapper or a method emitted into
        /// the wrong part: cross-part calls to private members only resolve if the parts really are
        /// one class.
        /// </summary>
        [Test]
        public void TheSplitFilesCompileTogether()
        {
            var errors = GeneratorHarness.CompileErrors(
                Split().Select(f => f.Source), typeof(cloud.charging.open.protocols.ISO15118.EXI.ExiPrimitives));

            Assert.That(errors, Is.Empty, string.Join("\n", errors.Select(e => e.ToString())));
        }

        [Test]
        public void FullIso2SchemaSet_SplitsIntoManyBoundedFiles()
        {
            var files = EmitterHarness.EmitCSharp(
                "cloud.charging.open.protocols.ISO15118_2.Generated", "Iso2Codec",
                EmitterHarness.RealSchemaSet("WWCP_ISO15118_2"));

            // The size that motivated the split: the set used to be one ~8,600-line file.
            Assert.That(files.Count, Is.GreaterThan(50));
            Assert.That(files.Max(f => f.Source.Length), Is.LessThan(100_000));

            // Compiling the whole real set is SchemaSetIntegrationTests' job — it now compiles every
            // one of these files as its own tree.
        }
    }
}
