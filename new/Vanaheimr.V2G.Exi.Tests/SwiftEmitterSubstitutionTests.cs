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
    /// Substitution groups in the Swift back end. The dispatch is the one place where the choice of
    /// classes over enums has a cost, so the tests here are about that cost being paid: branches
    /// ordered so a subclass cannot steal an ancestor's arm, and a guard where a consumer could
    /// introduce one.
    /// </summary>
    [TestFixture]
    public class SwiftEmitterSubstitutionTests
    {
        /// <param name="headAbstract">
        /// An abstract head is not a member of its own group; a concrete one is, and sorts last.
        /// </param>
        private static string Xsd(bool headAbstract) => $$"""
            <?xml version="1.0" encoding="UTF-8"?>
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns:t="urn:test" targetNamespace="urn:test"
                       elementFormDefault="qualified">

              <xs:complexType name="HeadType"{{(headAbstract ? " abstract=\"true\"" : "")}}>
                <xs:sequence>
                  <xs:element name="Common" type="xs:unsignedInt"/>
                </xs:sequence>
              </xs:complexType>

              <xs:complexType name="AlphaType">
                <xs:complexContent>
                  <xs:extension base="t:HeadType">
                    <xs:sequence><xs:element name="A" type="xs:unsignedInt"/></xs:sequence>
                  </xs:extension>
                </xs:complexContent>
              </xs:complexType>

              <xs:complexType name="BravoType">
                <xs:complexContent>
                  <xs:extension base="t:HeadType">
                    <xs:sequence><xs:element name="B" type="xs:unsignedInt"/></xs:sequence>
                  </xs:extension>
                </xs:complexContent>
              </xs:complexType>

              <xs:element name="Head" type="t:HeadType"{{(headAbstract ? " abstract=\"true\"" : "")}}/>
              <xs:element name="Alpha" type="t:AlphaType" substitutionGroup="t:Head"/>
              <xs:element name="Bravo" type="t:BravoType" substitutionGroup="t:Head"/>

              <xs:element name="doc">
                <xs:complexType>
                  <xs:sequence>
                    <xs:element ref="t:Head"/>
                  </xs:sequence>
                </xs:complexType>
              </xs:element>
            </xs:schema>
            """;

        private static (string, string)[] Schema(bool headAbstract) => [("test.xsd", Xsd(headAbstract))];

        private static string Doc(bool headAbstract) =>
            EmitterHarness.EmitSwift("test", "TestCodec", Schema(headAbstract))
                          .Single(f => f.FileName == "Doc.swift").Source;

        [Test]
        public void DispatchesOnTheRuntimeTypeWithoutALeadingStartEvent()
        {
            var doc = Doc(headAbstract: true);

            Assert.Multiple(() =>
            {
                Assert.That(doc, Does.Contain("case let v as AlphaType:"));
                Assert.That(doc, Does.Contain("case let v as BravoType:"));
                Assert.That(doc, Does.Contain("encodeAlphaType(w, v)"));
                // The member's event code *is* the start event; a separate SE would be a bit
                // nothing reads back.
                Assert.That(doc, Does.Not.Contain("w.writeBits(0, 1)   // SE"));
            });
        }

        /// <summary>
        /// The guard belongs on members a consumer could subclass. A leaf is emitted `final`, so
        /// `as` there already means "exactly this type" and the check would be dead code.
        /// </summary>
        [Test]
        public void GuardsOnlyExtensibleMembers()
        {
            var doc = Doc(headAbstract: false);

            // Alpha and Bravo are leaves: final, so no guard.
            Assert.That(doc, Does.Not.Contain("type(of: v) == AlphaType.self"));
            // The concrete head is extended by both, so it stays subclassable — and is guarded.
            Assert.That(doc, Does.Contain("type(of: v) == HeadType.self"));
        }

        /// <summary>
        /// With a concrete head the last branch would test the value against its own declared type,
        /// which always succeeds and leaves the default unreachable — so it is emitted as the
        /// default instead, exactly as the Kotlin back end does.
        /// </summary>
        [Test]
        public void CollapsesTheLastBranchWhenTheHeadIsConcrete()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Doc(headAbstract: false), Does.Not.Contain("case let v as HeadType:"));
                Assert.That(Doc(headAbstract: false), Does.Not.Contain("preconditionFailure"));

                // With an abstract head the head is not a member, so the default is reachable and
                // the failure stays.
                Assert.That(Doc(headAbstract: true), Does.Contain("preconditionFailure"));
            });
        }

        [TestCase(true,  TestName = "abstract head")]
        [TestCase(false, TestName = "concrete head")]
        public void SwiftAndCSharpAgreeOperationForOperation(bool headAbstract)
        {
            var swift  = CrossEmitterComparison.Operations(
                             EmitterHarness.EmitSwift("test", "TestCodec", Schema(headAbstract)));
            var csharp = CrossEmitterComparison.Operations(
                             EmitterHarness.EmitCSharp("Test", "TestCodec", Schema(headAbstract)));

            Assert.That(swift, Is.Not.Empty);

            var problems = CrossEmitterComparison.Diff(swift, "swift", csharp, "csharp");
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }
    }
}
