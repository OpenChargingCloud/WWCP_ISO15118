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
    /// The Swift back end's handling of <c>xs:extension</c>, on a mini-XSD that exercises nothing
    /// else. Inheritance is the largest single construct in the real sets — 47 of the 94 ISO
    /// 15118-2 types extend a base — so it is worth isolating from the rest.
    /// </summary>
    /// <remarks>
    /// The grammar flattens a derived type's children, so inheritance carries no wire meaning of
    /// its own; what these tests protect is that the *declarations* stay coherent while the encoder
    /// keeps walking the same particles in the same order as the C# back end.
    /// </remarks>
    [TestFixture]
    public class SwiftEmitterInheritanceTests
    {
        private const string Xsd = """
            <?xml version="1.0" encoding="UTF-8"?>
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns:t="urn:test" targetNamespace="urn:test"
                       elementFormDefault="qualified">

              <xs:complexType name="BaseType" abstract="true">
                <xs:sequence>
                  <xs:element name="Common" type="xs:unsignedInt"/>
                </xs:sequence>
              </xs:complexType>

              <xs:complexType name="DerivedType">
                <xs:complexContent>
                  <xs:extension base="t:BaseType">
                    <xs:sequence>
                      <xs:element name="Extra" type="xs:unsignedInt"/>
                    </xs:sequence>
                  </xs:extension>
                </xs:complexContent>
              </xs:complexType>

              <xs:element name="doc">
                <xs:complexType>
                  <xs:sequence>
                    <xs:element name="Body" type="t:DerivedType"/>
                  </xs:sequence>
                </xs:complexType>
              </xs:element>
            </xs:schema>
            """;

        private static (string, string)[] Schema() => [("test.xsd", Xsd)];

        [Test]
        public void ADerivedTypeIsAClassExtendingItsBase()
        {
            var files = EmitterHarness.EmitSwift("test", "TestCodec", Schema());
            var derived = files.Single(f => f.FileName == "DerivedType.swift").Source;

            Assert.Multiple(() =>
            {
                Assert.That(derived, Does.Contain("public final class DerivedType: BaseType"));
                // The base's child is inherited, so it must not be re-declared as a stored property.
                Assert.That(derived, Does.Not.Contain("public var common"));
                Assert.That(derived, Does.Contain("public var extra"));
                // …but the initialiser still takes every child, flattened, base first.
                Assert.That(derived, Does.Contain("public init(common: UInt32, extra: UInt32)"));
                Assert.That(derived, Does.Contain("super.init(common: common)"));
            });
        }

        [Test]
        public void AnAbstractBaseIsSubclassableAndCarriesNoCodec()
        {
            var files = EmitterHarness.EmitSwift("test", "TestCodec", Schema());
            var baseFile = files.Single(f => f.FileName == "BaseType.swift").Source;

            Assert.Multiple(() =>
            {
                // Not `final`: it has to be subclassable.
                Assert.That(baseFile, Does.Contain("public class BaseType"));
                Assert.That(baseFile, Does.Not.Contain("final"));
                // Abstract types are never encoded directly — only their members are.
                Assert.That(baseFile, Does.Not.Contain("func encodeBaseType"));
                Assert.That(baseFile, Does.Not.Contain("func decodeBaseType"));
            });
        }

        /// <summary>
        /// The point of the whole exercise: whatever the declarations look like, the derived type's
        /// encoder must perform the base's particles and its own in the same order as C#.
        /// </summary>
        [Test]
        public void SwiftAndCSharpAgreeOnTheFlattenedEncoding()
        {
            var swift  = CrossEmitterComparison.Operations(EmitterHarness.EmitSwift("test", "TestCodec", Schema()));
            var csharp = CrossEmitterComparison.Operations(EmitterHarness.EmitCSharp("Test", "TestCodec", Schema()));

            Assert.That(swift.ContainsKey("encode:derivedtype"), Is.True, "no encoder for the derived type");

            var problems = CrossEmitterComparison.Diff(swift, "swift", csharp, "csharp");
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }
    }
}
