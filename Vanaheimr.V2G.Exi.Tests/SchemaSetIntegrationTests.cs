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
using cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>
    /// Phase 2 integration gate: run the generator over the full ISO 15118-2 schema set and require
    /// zero diagnostics AND that the generated codec compiles (Phase 2 Definition of Done #1). The
    /// per-message byte conformance against cbV2G is a separate step (differential vectors).
    /// </summary>
    [TestFixture]
    public class SchemaSetIntegrationTests
    {
        private static GeneratorHarness.Result GenerateFullSet()
        {
            var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (root is not null && !Directory.Exists(Path.Combine(root.FullName, "Vanaheimr.V2G.Exi.Iso15118_2")))
                root = root.Parent;
            Assert.That(root, Is.Not.Null, "repo root not found");

            var schemaDir = Path.Combine(root!.FullName, "Vanaheimr.V2G.Exi.Iso15118_2", "Schemas");
            var files = new List<(string, string)>();
            foreach (var f in Directory.GetFiles(schemaDir, "*.xsd"))
                files.Add((Path.GetFileName(f), File.ReadAllText(f)));

            return GeneratorHarness.Run(files.ToArray());
        }

        [Test]
        public void FullIso2SchemaSet_GeneratesWithoutDiagnostics()
        {
            var diagnostics = GenerateFullSet().Diagnostics;

            var msg = $"{diagnostics.Length} diagnostics:\n";
            foreach (var d in diagnostics)
                msg += "  " + d.GetMessage() + "\n";
            Assert.That(diagnostics.Length, Is.Zero, msg);
        }

        [Test]
        public void FullIso2SchemaSet_GeneratedCodecCompiles()
        {
            var r = GenerateFullSet();
            var diagnostics = r.Diagnostics;
            Assert.That(diagnostics.Length, Is.Zero, "generation must be diagnostic-free first");

            var errors = GeneratorHarness.CompileErrors(r, typeof(cloud.charging.open.protocols.ISO15118.EXI.ExiPrimitives));
            Assert.That(errors, Is.Empty,
                string.Join("\n", errors.Select(e => e.ToString())));
        }

        [Test]
        public void FullIso2SchemaSet_DocumentSelectorMatchesCbV2G()
        {
            var r = GenerateFullSet();
            Assert.That(r.Diagnostics.Length, Is.Zero);
            var source = r.GeneratedSource;

            // The document grammar enumerates all 80 global elements of the set; V2G_Message is index 76
            // at a 7-bit selector (cbV2G iso2_exiDocument: nbit(7, 76)).
            Assert.That(source, Does.Contain("w.WriteBits(76, 7);   // document element selector"));
            Assert.That(source, Does.Contain("uint sel = r.ReadBits(7);"));
            Assert.That(source, Does.Contain("76u => Decode_V2G_Message(ref r)"));
        }
    }
}
