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
    /// XSD attributes in the Swift back end, on mini-XSDs that exercise nothing else. 18 of the 94
    /// ISO 15118-2 types carry them, in the two shapes the grammar models: a lone required
    /// attribute written before the content, and optional ones riding as leading particles of the
    /// content run.
    /// </summary>
    [TestFixture]
    public class SwiftEmitterAttributeTests
    {
        private static string Xsd(string attributes) => $$"""
            <?xml version="1.0" encoding="UTF-8"?>
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns:t="urn:test" targetNamespace="urn:test"
                       elementFormDefault="qualified">
              <xs:element name="doc">
                <xs:complexType>
                  <xs:sequence>
                    <xs:element name="Body" type="xs:unsignedInt"/>
                  </xs:sequence>
                  {{attributes}}
                </xs:complexType>
              </xs:element>
            </xs:schema>
            """;

        private const string Required = """<xs:attribute name="Id" type="xs:string" use="required"/>""";
        private const string Optional = """<xs:attribute name="Id" type="xs:string"/>""";

        private static (string, string)[] Schema(string attrs) => [("test.xsd", Xsd(attrs))];

        [Test]
        public void ARequiredAttributeIsANonOptionalFieldWrittenBeforeTheContent()
        {
            var doc = EmitterHarness.EmitSwift("test", "TestCodec", Schema(Required))
                                    .Single(f => f.FileName == "Doc.swift").Source;

            Assert.Multiple(() =>
            {
                Assert.That(doc, Does.Contain("public var id: String"));
                Assert.That(doc, Does.Not.Contain("public var id: String?"));
                // AT event, then a bare value: no value-start bit, because the AT event *is* it.
                Assert.That(doc, Does.Contain("w.writeBits(0, 1)   // AT(required attribute)"));
                Assert.That(doc, Does.Contain("ExiPrimitives.writeStringValue(w, msg.id)"));
                // The attribute precedes the content in the initialiser, as in the other back ends.
                Assert.That(doc, Does.Contain("public init(id: String, body: UInt32)"));
            });
        }

        [Test]
        public void AnOptionalAttributeBecomesAnOptionalFieldInTheContentRun()
        {
            var doc = EmitterHarness.EmitSwift("test", "TestCodec", Schema(Optional))
                                    .Single(f => f.FileName == "Doc.swift").Source;

            Assert.Multiple(() =>
            {
                Assert.That(doc, Does.Contain("public var id: String?"));
                Assert.That(doc, Does.Not.Contain("AT(required attribute)"));
                // It rides the optional-run state machine, so it is selected by an event code.
                Assert.That(doc, Does.Contain("if let v = msg.id"));
            });
        }

        [TestCase(Required, TestName = "required attribute")]
        [TestCase(Optional, TestName = "optional attribute")]
        public void SwiftAndCSharpAgreeOperationForOperation(string attrs)
        {
            var swift  = CrossEmitterComparison.Operations(EmitterHarness.EmitSwift("test", "TestCodec", Schema(attrs)));
            var csharp = CrossEmitterComparison.Operations(EmitterHarness.EmitCSharp("Test", "TestCodec", Schema(attrs)));

            Assert.That(swift, Is.Not.Empty);

            var problems = CrossEmitterComparison.Diff(swift, "swift", csharp, "csharp");
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        /// <summary>
        /// The shapes the grammar allows but this back end does not model must still fail loudly.
        /// A required attribute alongside optional ones would need both mechanisms at once.
        /// </summary>
        [Test]
        public void RefusesARequiredAttributeAlongsideOthers()
        {
            var mixed = Required + "\n" + """<xs:attribute name="Other" type="xs:string"/>""";

            var ex = Assert.Throws<NotSupportedException>(
                         () => EmitterHarness.EmitSwift("test", "TestCodec", Schema(mixed)));

            Assert.That(ex!.Message, Does.Contain("required attribute alongside others"));
        }

        [Test]
        public void RefusesANonStringAttribute()
        {
            var ex = Assert.Throws<NotSupportedException>(
                         () => EmitterHarness.EmitSwift("test", "TestCodec",
                                   Schema("""<xs:attribute name="Count" type="xs:unsignedInt"/>""")));

            Assert.That(ex!.Message, Does.Contain("non-string attribute"));
        }
    }
}
