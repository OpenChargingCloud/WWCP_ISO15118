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

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>
    /// The out-of-Roslyn driver's file handling, exercised end to end through
    /// <see cref="Codegen.Program.Main"/> against real files in a temporary directory.
    /// </summary>
    /// <remarks>
    /// The part that earns a test is the stale-output removal. It is the only place in this
    /// repository that deletes files the developer did not ask it to, and the only thing standing
    /// between a hand-written source and deletion is one line of its own text. Both directions are
    /// checked here: what must go, and what must stay.
    /// </remarks>
    [TestFixture]
    public class CodegenDriverTests
    {
        private const string Schema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                   xmlns="urn:test:drv" targetNamespace="urn:test:drv">
          <xs:element name="Root" type="RootType"/>
          <xs:complexType name="RootType">
            <xs:sequence>
              <xs:element name="Count" type="xs:unsignedInt"/>
            </xs:sequence>
          </xs:complexType>
        </xs:schema>
        """;

        /// <summary>The same schema with RootType renamed — the case that strands a file.</summary>
        private const string RenamedSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                   xmlns="urn:test:drv" targetNamespace="urn:test:drv">
          <xs:element name="Root" type="RenamedType"/>
          <xs:complexType name="RenamedType">
            <xs:sequence>
              <xs:element name="Count" type="xs:unsignedInt"/>
            </xs:sequence>
          </xs:complexType>
        </xs:schema>
        """;

        private string _dir = null!;
        private string _xsd = null!;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "exi-codegen-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _xsd = Path.Combine(_dir, "drv.xsd");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, recursive: true);
        }

        private int RunKotlin(string schema, string output)
        {
            File.WriteAllText(_xsd, schema);
            return Codegen.Program.Main(
                ["--xsd", _xsd, "--out", output,
                 "--lang", "kotlin", "--namespace", "test.drv", "--codec", "DrvCodec"]);
        }

        private string Out => Path.Combine(_dir, "out");

        private string[] Emitted =>
            Directory.Exists(Out)
                ? Directory.GetFiles(Out, "*.kt").Select(p => Path.GetFileName(p)!).Order().ToArray()
                : [];

        // ---- directory output --------------------------------------------------------------

        [Test]
        public void Kotlin_WritesOneFilePerType_IntoTheDirectory()
        {
            Assert.That(RunKotlin(Schema, Out), Is.Zero);
            Assert.That(Emitted, Is.EquivalentTo(new[] { "DrvCodec.kt", "Root.kt", "RootType.kt",
                                                "DrvCodecJson.Json.kt", "Root.Json.kt",
                                                "RootType.Json.kt" }));
        }

        [Test]
        public void Kotlin_CreatesTheOutputDirectory()
        {
            var nested = Path.Combine(_dir, "does", "not", "exist");

            Assert.That(RunKotlin(Schema, nested), Is.Zero);
            Assert.That(Directory.GetFiles(nested, "*.kt"), Is.Not.Empty);
        }

        [Test]
        public void Kotlin_RefusesAFilePathBecauseItSplitsItsOutput()
        {
            // The pre-split invocation. Treating it as a directory would silently create one called
            // "DrvCodec.kt" holding every type — the shape of the old command, none of its meaning.
            var asFile = Path.Combine(_dir, "DrvCodec.kt");

            Assert.That(RunKotlin(Schema, asFile), Is.EqualTo(2), "usage errors exit 2");
            Assert.That(Directory.Exists(asFile), Is.False);
            Assert.That(File.Exists(asFile), Is.False);
        }

        [Test]
        public void CSharp_AlsoWritesOneFilePerTypeIntoTheDirectory()
        {
            File.WriteAllText(_xsd, Schema);
            var target = Path.Combine(_dir, "cs");

            var exit = Codegen.Program.Main(
                ["--xsd", _xsd, "--out", target,
                 "--lang", "csharp", "--namespace", "Test.Drv", "--codec", "DrvCodec"]);

            Assert.That(exit, Is.Zero);
            Assert.That(Directory.GetFiles(target, "*.g.cs").Select(Path.GetFileName),
                        Is.EquivalentTo(new[] { "DrvCodec.g.cs", "Root.g.cs", "RootType.g.cs",
                                                "DrvCodecJson.Json.g.cs", "Root.Json.g.cs",
                                                "RootType.Json.g.cs" }));
            Assert.That(File.ReadAllText(Path.Combine(target, "RootType.g.cs")),
                        Does.Contain("namespace Test.Drv"));
        }

        [Test]
        public void CSharp_RefusesAFilePathToo()
        {
            File.WriteAllText(_xsd, Schema);
            var asFile = Path.Combine(_dir, "Codec.g.cs");

            var exit = Codegen.Program.Main(
                ["--xsd", _xsd, "--out", asFile,
                 "--lang", "csharp", "--namespace", "Test.Drv", "--codec", "DrvCodec"]);

            Assert.That(exit, Is.EqualTo(2), "usage errors exit 2");
            Assert.That(File.Exists(asFile), Is.False);
        }

        // ---- stale-output removal ------------------------------------------------------------

        [Test]
        public void RegeneratingIsIdempotent()
        {
            Assert.That(RunKotlin(Schema, Out), Is.Zero);
            var first = Emitted.ToDictionary(n => n, n => File.ReadAllBytes(Path.Combine(Out, n)));

            Assert.That(RunKotlin(Schema, Out), Is.Zero);

            Assert.That(Emitted, Is.EquivalentTo(first.Keys));
            foreach (var (name, bytes) in first)
                Assert.That(File.ReadAllBytes(Path.Combine(Out, name)), Is.EqualTo(bytes), name);
        }

        [Test]
        public void ARenamedTypeDoesNotLeaveItsOldFileBehind()
        {
            Assert.That(RunKotlin(Schema, Out), Is.Zero);
            Assert.That(Emitted, Does.Contain("RootType.kt"));

            Assert.That(RunKotlin(RenamedSchema, Out), Is.Zero);

            Assert.That(Emitted, Does.Contain("RenamedType.kt"));
            Assert.That(Emitted, Does.Not.Contain("RootType.kt"),
                        "a stale declaration beside its replacement is a duplicate-declaration error");
        }

        [Test]
        public void AHandWrittenFileInTheSameDirectorySurvives()
        {
            Assert.That(RunKotlin(Schema, Out), Is.Zero);

            // V2GSignature.kt is exactly this case: hand-written, in the generated package.
            var handWritten = Path.Combine(Out, "V2GSignature.kt");
            const string content = "package test.drv\n\nobject V2GSignature\n";
            File.WriteAllText(handWritten, content);

            Assert.That(RunKotlin(RenamedSchema, Out), Is.Zero);

            Assert.That(File.Exists(handWritten), Is.True, "a file without the generator banner is not ours to delete");
            Assert.That(File.ReadAllText(handWritten), Is.EqualTo(content));
        }

        [Test]
        public void AFileOfAnotherKindIsLeftAlone()
        {
            Assert.That(RunKotlin(Schema, Out), Is.Zero);

            var readme = Path.Combine(Out, "notes.md");
            File.WriteAllText(readme, "// <auto-generated/>\nnot a Kotlin file\n");

            Assert.That(RunKotlin(RenamedSchema, Out), Is.Zero);

            Assert.That(File.Exists(readme), Is.True,
                        "only files with the back end's own extension are candidates");
        }
    }
}
