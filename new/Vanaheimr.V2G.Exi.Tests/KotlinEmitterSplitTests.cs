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

using NUnit.Framework;
using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Emit;
using cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>
    /// The Kotlin back end emits one file per type. These tests pin that layout — the file split,
    /// the visibility it forces, and above all that the pieces still refer to one another.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Everything else about this back end is checked outside the .NET suite: the bytes against
    /// cbV2G vectors, and well-formedness by compiling the result with kotlinc. Neither runs here,
    /// so these tests deliberately assert structure rather than behaviour — what they can catch is
    /// a split that drops, duplicates or orphans a declaration, which is exactly what a
    /// file-splitting change can get wrong without the byte-level gates noticing.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class KotlinEmitterSplitTests
    {
        /// <summary>
        /// Two complex types, an enum, and a global element whose body is a type of its own —
        /// enough for a cross-file call (Root's encoder calls DetailType's).
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
            EmitterHarness.Emit("test.split", "SplitCodec", ("split.xsd", SplitSchema));

        private static GeneratedFile File(IReadOnlyList<GeneratedFile> files, string name) =>
            files.SingleOrDefault(f => f.FileName == name)
            ?? throw new AssertionException(
                   $"no file '{name}'; got: {string.Join(", ", files.Select(f => f.FileName))}");

        // ---- the split itself -----------------------------------------------------------------

        [Test]
        public void EachTypeGetsItsOwnFileNamedAfterIt()
        {
            var files = Split();

            Assert.That(files.Select(f => f.FileName),
                        Is.EquivalentTo(new[]
                        {
                            "Mode.kt", "Root.kt", "RootType.kt", "DetailType.kt", "SplitCodec.kt",

                            // The JSON-LD pass, split the same way: one part per type, one for the
                            // dispatchers. An enum has no JSON part of its own.
                            "Root.Json.kt", "RootType.Json.kt", "DetailType.Json.kt",
                            "SplitCodecJson.Json.kt",
                        }));
        }

        [Test]
        public void FileNamesAreBareKotlinFileNames()
        {
            foreach (var f in Split())
            {
                Assert.That(f.FileName, Does.EndWith(".kt"));
                Assert.That(f.FileName, Does.Not.Contain("/").And.Not.Contain("\\"),
                            "a file name carries no directory part — the caller decides where files go");
            }
        }

        [Test]
        public void ATypeAndItsCodecShareAFile()
        {
            var detail = File(Split(), "DetailType.kt").Source;

            Assert.That(detail, Does.Contain("data class DetailType("));
            Assert.That(detail, Does.Contain("internal fun encodeDetailType(w: BitWriter, msg: DetailType) {"));
            Assert.That(detail, Does.Contain("internal fun decodeDetailType(r: BitReader): DetailType {"));
        }

        [Test]
        public void TheCodecObjectHoldsOnlyTheEntryPoints()
        {
            var codec = File(Split(), "SplitCodec.kt").Source;

            Assert.That(codec, Does.Contain("object SplitCodec {"));
            Assert.That(codec, Does.Contain("fun encode(msg: Root): ByteArray {"));
            Assert.That(codec, Does.Contain("fun decodeAny(src: ByteArray): Any {"));

            // The whole point of the split: per-type codecs are no longer members of the object.
            Assert.That(codec, Does.Not.Contain("fun encodeDetailType"));
            Assert.That(codec, Does.Not.Contain("fun decodeDetailType"));
        }

        [Test]
        public void FragmentCodecsStayWithTheCodecObject()
        {
            var files = EmitterHarness.Emit(
                "test.split", "SplitCodec", ["Root"], ("split.xsd", SplitSchema));
            var codec = File(files, "SplitCodec.kt").Source;

            Assert.That(codec, Does.Contain("fun encodeFragment_Root(content: RootType): ByteArray {"));
            Assert.That(codec, Does.Contain("fun decodeFragment_Root(src: ByteArray): RootType {"));
        }

        // ---- what the split must not break ------------------------------------------------------

        [Test]
        public void TypeCodecsAreInternalSoTheCodecObjectCanReachThem()
        {
            // A top-level `private fun` is file-private in Kotlin: the split would compile each file
            // and then fail to link the object to any of them.
            foreach (var f in Split())
                foreach (var line in EmitterHarness.Lines(f))
                    Assert.That(line, Does.Not.StartWith("private fun"),
                                $"{f.FileName} declares a top-level private function");
        }

        [Test]
        public void EveryCodecCallResolvesToADeclaredFunction()
        {
            AssertCallsResolve(Split());
        }

        [Test]
        public void NoDeclarationIsEmittedTwice()
        {
            AssertNoDuplicateDeclarations(Split());
        }

        [Test]
        public void EveryFileImportsExactlyWhatItUses()
        {
            AssertImportsMatchUse(Split());
        }

        [Test]
        public void EveryFileIsAWellFormedCompilationUnit()
        {
            AssertWellFormed(Split(), "test.split");
        }

        // ---- the same invariants over the real ISO 15118-2 set ----------------------------------

        [Test]
        public void FullIso2SchemaSet_SplitsIntoManyFilesAndStillResolves()
        {
            var files = EmitterHarness.Emit(
                "cloud.charging.v2g.iso2", "Iso15118_2Codec",
                ["AuthorizationReq", "MeteringReceiptReq", "SalesTariff", "SignedInfo"],
                EmitterHarness.RealSchemaSet("Vanaheimr.V2G.Exi.Iso15118_2"));

            // The size that motivated the split: one file per type over a real set is ~100 files.
            Assert.That(files.Count, Is.GreaterThan(50));
            Assert.That(files.Max(f => f.Source.Length), Is.LessThan(100_000),
                        "no single file may grow back towards the ~1 MB that exhausted the Kotlin compiler");

            AssertCallsResolve(files);
            AssertNoDuplicateDeclarations(files);
            AssertImportsMatchUse(files);
            AssertWellFormed(files, "cloud.charging.v2g.iso2");
        }

        /// <summary>
        /// Every string value read has to name its own EXI value-slot, or the decoder resolves
        /// value-table hits against the wrong partition. The vectors cannot catch this — cbV2G is
        /// miss-only, so no checked-in stream ever exercises a hit — which is why it is asserted on
        /// the emitted text instead. The C# side is pinned the same way in GeneratorGrammarTests.
        /// </summary>
        [Test]
        public void EveryStringReadNamesItsValueSlot()
        {
            var files = EmitterHarness.Emit(
                "cloud.charging.v2g.iso2", "Iso15118_2Codec", [],
                EmitterHarness.RealSchemaSet("Vanaheimr.V2G.Exi.Iso15118_2"));

            var reads = 0;
            foreach (var f in files)
                foreach (var line in EmitterHarness.Lines(f))
                {
                    var at = line.IndexOf("readStringValue(", StringComparison.Ordinal);
                    if (at < 0) continue;
                    reads++;
                    Assert.That(line[at..], Does.Match(@"readStringValue\(r, ""[^""]+""\)"),
                                $"{f.FileName}: a string read without a slot name — {line.Trim()}");
                }

            Assert.That(reads, Is.GreaterThan(0), "the check found nothing to verify");
        }

        // ---- shared assertions ------------------------------------------------------------------

        /// <summary>
        /// A poor man's linker: every <c>encodeX(…)</c> / <c>decodeX(…)</c> the generated code calls
        /// must be declared somewhere in the same package. Splitting a file cannot be verified by a
        /// byte diff — this is what catches a codec that was emitted into no file at all.
        /// </summary>
        private static void AssertCallsResolve(IReadOnlyList<GeneratedFile> files)
        {
            var declared = new HashSet<string>(
                files.SelectMany(EmitterHarness.TopLevelDeclarations)
                     .Where(m => m.Groups["keyword"].Value == "fun")
                     .Select(m => m.Groups["name"].Value));

            // Members of the codec object are indented, so they are not top-level matches.
            foreach (var f in files)
                foreach (var line in EmitterHarness.Lines(f))
                    if (line.TrimStart().StartsWith("fun ") || line.TrimStart().StartsWith("internal fun "))
                        declared.Add(line.TrimStart().Split("fun ")[^1].Split('(')[0].Trim());

            var calls = 0;
            foreach (var f in files)
                foreach (var line in EmitterHarness.Lines(f))
                {
                    if (line.Contains("fun ")) continue;   // a declaration, not a call
                    foreach (System.Text.RegularExpressions.Match m in EmitterHarness.CodecCall.Matches(line))
                    {
                        var name = m.Groups["name"].Value;
                        calls++;
                        Assert.That(declared, Does.Contain(name),
                                    $"{f.FileName} calls {name}(), which no file declares");
                    }
                }

            Assert.That(calls, Is.GreaterThan(0), "the check found nothing to verify");
        }

        private static void AssertNoDuplicateDeclarations(IReadOnlyList<GeneratedFile> files)
        {
            var seen = new Dictionary<string, string>();

            foreach (var f in files)
                foreach (var m in EmitterHarness.TopLevelDeclarations(f))
                {
                    var key = m.Groups["keyword"].Value + " " + m.Groups["name"].Value;
                    Assert.That(seen.ContainsKey(key), Is.False,
                                $"{key} is declared in both {seen.GetValueOrDefault(key)} and {f.FileName}");
                    seen[key] = f.FileName;
                }
        }

        /// <summary>
        /// Imports are filtered per file, so a data class does not import a <c>BitReader</c> it never
        /// mentions. Both directions matter: a missing import does not compile, an unused one warns.
        /// </summary>
        private static void AssertImportsMatchUse(IReadOnlyList<GeneratedFile> files)
        {
            foreach (var f in files)
            {
                var lines = EmitterHarness.Lines(f).ToList();
                var body  = string.Join("\n", lines.Where(l => !l.StartsWith("import ")));

                foreach (var type in new[] { "BitReader", "BitWriter", "ExiPrimitives" })
                {
                    var imported = lines.Contains("import cloud.charging.v2g.exi." + type);
                    Assert.That(imported, Is.EqualTo(body.Contains(type)),
                                $"{f.FileName}: import of {type} is {(imported ? "present" : "absent")} " +
                                $"but the file {(body.Contains(type) ? "uses" : "does not use")} it");
                }
            }
        }

        private static void AssertWellFormed(IReadOnlyList<GeneratedFile> files, string expectedPackage)
        {
            foreach (var f in files)
            {
                var lines = EmitterHarness.Lines(f).ToList();

                Assert.That(lines[0], Is.EqualTo("// <auto-generated/>"),
                            $"{f.FileName} must carry the banner — the driver deletes stale output by it");
                Assert.That(lines.Count(l => l.StartsWith("package ")), Is.EqualTo(1), f.FileName);
                Assert.That(lines.First(l => l.StartsWith("package ")),
                            Is.EqualTo("package " + expectedPackage), f.FileName);

                var firstDeclaration = lines.FindIndex(l => EmitterHarness.TopLevelDeclaration.IsMatch(l));
                Assert.That(firstDeclaration, Is.GreaterThan(0), $"{f.FileName} declares nothing");
                Assert.That(lines.FindLastIndex(l => l.StartsWith("import ")), Is.LessThan(firstDeclaration),
                            $"{f.FileName} has an import after its first declaration");

                var depth = f.Source.Count(c => c == '{') - f.Source.Count(c => c == '}');
                Assert.That(depth, Is.Zero, $"{f.FileName} has unbalanced braces");

                Assert.That(f.Source, Does.EndWith("\n"), f.FileName);
                Assert.That(f.Source.TrimEnd('\r', '\n').Length, Is.EqualTo(f.Source.Length - 1).Or
                                                                   .EqualTo(f.Source.Length - 2),
                            $"{f.FileName} ends with more than one blank line");
            }
        }
    }
}
