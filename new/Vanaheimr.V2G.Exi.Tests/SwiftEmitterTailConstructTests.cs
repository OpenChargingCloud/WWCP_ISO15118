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
    /// The smaller constructs of the ISO 15118-2 set: <c>xs:choice</c> (1 type),
    /// <c>xs:simpleContent</c> (4), and a substitution group in an *optional* position, which
    /// unlike the required one widens the run it sits in.
    /// </summary>
    [TestFixture]
    public class SwiftEmitterTailConstructTests
    {
        private static (string, string)[] Schema(string body) => [("test.xsd", $$"""
            <?xml version="1.0" encoding="UTF-8"?>
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns:t="urn:test" targetNamespace="urn:test"
                       elementFormDefault="qualified">
            {{body}}
            </xs:schema>
            """)];

        private const string ChoiceSchema = """
              <xs:complexType name="PickType">
                <xs:choice>
                  <xs:element name="Alpha" type="xs:unsignedInt"/>
                  <xs:element name="Bravo" type="xs:string"/>
                </xs:choice>
              </xs:complexType>
              <xs:element name="doc">
                <xs:complexType><xs:sequence>
                  <xs:element name="Pick" type="t:PickType"/>
                </xs:sequence></xs:complexType>
              </xs:element>
            """;

        private const string SimpleContentSchema = """
              <xs:complexType name="TaggedType">
                <xs:simpleContent>
                  <xs:extension base="xs:string">
                    <xs:attribute name="Id" type="xs:string" use="required"/>
                  </xs:extension>
                </xs:simpleContent>
              </xs:complexType>
              <xs:element name="doc">
                <xs:complexType><xs:sequence>
                  <xs:element name="Tagged" type="t:TaggedType"/>
                </xs:sequence></xs:complexType>
              </xs:element>
            """;

        /// <summary>A substitution group referenced from an optional position.</summary>
        private const string OptionalSubstitutionSchema = """
              <xs:complexType name="HeadType" abstract="true">
                <xs:sequence><xs:element name="Common" type="xs:unsignedInt"/></xs:sequence>
              </xs:complexType>
              <xs:complexType name="AlphaType">
                <xs:complexContent><xs:extension base="t:HeadType">
                  <xs:sequence><xs:element name="A" type="xs:unsignedInt"/></xs:sequence>
                </xs:extension></xs:complexContent>
              </xs:complexType>
              <xs:complexType name="BravoType">
                <xs:complexContent><xs:extension base="t:HeadType">
                  <xs:sequence><xs:element name="B" type="xs:unsignedInt"/></xs:sequence>
                </xs:extension></xs:complexContent>
              </xs:complexType>
              <xs:element name="Head" type="t:HeadType" abstract="true"/>
              <xs:element name="Alpha" type="t:AlphaType" substitutionGroup="t:Head"/>
              <xs:element name="Bravo" type="t:BravoType" substitutionGroup="t:Head"/>
              <xs:element name="doc">
                <xs:complexType><xs:sequence>
                  <xs:element name="Lead" type="xs:unsignedInt"/>
                  <xs:element ref="t:Head" minOccurs="0"/>
                </xs:sequence></xs:complexType>
              </xs:element>
            """;

        [TestCase(ChoiceSchema,               TestName = "xs:choice")]
        [TestCase(SimpleContentSchema,        TestName = "xs:simpleContent with a required attribute")]
        [TestCase(OptionalSubstitutionSchema, TestName = "substitution group in an optional position")]
        public void SwiftAndCSharpAgreeOperationForOperation(string body)
        {
            var swift  = CrossEmitterComparison.Operations(EmitterHarness.EmitSwift("test", "TestCodec", Schema(body)));
            var csharp = CrossEmitterComparison.Operations(EmitterHarness.EmitCSharp("Test", "TestCodec", Schema(body)));

            Assert.That(swift, Is.Not.Empty);

            var problems = CrossEmitterComparison.Diff(swift, "swift", csharp, "csharp");
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void AChoiceSelectsOneAlternativeAndRefusesNone()
        {
            var pick = EmitterHarness.EmitSwift("test", "TestCodec", Schema(ChoiceSchema))
                                     .Single(f => f.FileName == "PickType.swift").Source;

            Assert.Multiple(() =>
            {
                Assert.That(pick, Does.Contain("if let v = msg.alpha {"));
                Assert.That(pick, Does.Contain("} else if let v = msg.bravo {"));
                Assert.That(pick, Does.Contain("preconditionFailure(\"no choice alternative set"));
            });
        }

        [Test]
        public void SimpleContentIsOneContentEventAndABareValue()
        {
            var tagged = EmitterHarness.EmitSwift("test", "TestCodec", Schema(SimpleContentSchema))
                                       .Single(f => f.FileName == "TaggedType.swift").Source;

            Assert.Multiple(() =>
            {
                Assert.That(tagged, Does.Contain("w.writeBits(0, 1)   // AT(required attribute)"));
                Assert.That(tagged, Does.Contain("w.writeBits(0, 1)   // CONTENT event"));
                Assert.That(tagged, Does.Contain("public var value: String"));
            });
        }

        /// <summary>
        /// The width test: a substitution group contributes one production per member, so an
        /// optional one widens the selector of the run it sits in beyond what a plain optional
        /// would need. Getting that wrong shifts every following bit.
        /// </summary>
        [Test]
        public void AnOptionalSubstitutionWidensTheRunSelector()
        {
            var doc = EmitterHarness.EmitSwift("test", "TestCodec", Schema(OptionalSubstitutionSchema))
                                    .Single(f => f.FileName == "Doc.swift").Source;

            // The group has three slots, not two: the abstract head reserves a code without being
            // encodable. Three + the element EE + the non-strict phantom = 5 productions → 3 bits,
            // where a plain optional particle would have needed 1. The element EE lands on 3, the
            // slot the head reserved being skipped.
            Assert.Multiple(() =>
            {
                Assert.That(doc, Does.Contain("as? AlphaType"));
                Assert.That(doc, Does.Contain("w.writeBits(0, 3)   // Alpha"));
                Assert.That(doc, Does.Contain("w.writeBits(1, 3)   // Bravo"));
                Assert.That(doc, Does.Contain("w.writeBits(3, 3)   // element EE"));
            });
        }
    }
}
