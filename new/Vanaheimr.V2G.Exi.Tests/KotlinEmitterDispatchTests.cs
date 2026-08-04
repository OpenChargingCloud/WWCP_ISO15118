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
    /// Substitution dispatch in the Kotlin back end: which branch tests a type and which is the
    /// fallthrough. Both forms are emitted, and picking the wrong one is either a compiler warning
    /// or a lost error path.
    /// </summary>
    [TestFixture]
    public class KotlinEmitterDispatchTests
    {
        /// <summary>
        /// A concrete head with one derived member — the ISO 15118-20 shape
        /// (<c>AC_CPDReqEnergyTransferMode</c> + its BPT variant). The derived element is named
        /// <c>ZMode</c> on purpose: wire codes are alphabetical, so it sorts *after* the head while
        /// still being emitted *before* it, which is what makes the ordering test say something.
        /// </summary>
        private const string ConcreteHeadSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                   xmlns="urn:test:sub" targetNamespace="urn:test:sub">
          <xs:element name="Root" type="RootType"/>
          <xs:complexType name="RootType">
            <xs:sequence>
              <xs:element ref="Mode"/>
            </xs:sequence>
          </xs:complexType>
          <xs:element name="Mode" type="ModeType"/>
          <xs:element name="ZMode" type="ZModeType" substitutionGroup="Mode"/>
          <xs:complexType name="ModeType">
            <xs:sequence>
              <xs:element name="Power" type="xs:unsignedInt"/>
            </xs:sequence>
          </xs:complexType>
          <xs:complexType name="ZModeType">
            <xs:complexContent>
              <xs:extension base="ModeType">
                <xs:sequence>
                  <xs:element name="Discharge" type="xs:unsignedInt"/>
                </xs:sequence>
              </xs:extension>
            </xs:complexContent>
          </xs:complexType>
        </xs:schema>
        """;

        /// <summary>An abstract head — the ISO 15118-2 shape (<c>TimeInterval</c>).</summary>
        private const string AbstractHeadSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                   xmlns="urn:test:sub" targetNamespace="urn:test:sub">
          <xs:element name="Root" type="RootType"/>
          <xs:complexType name="RootType">
            <xs:sequence>
              <xs:element ref="Interval"/>
            </xs:sequence>
          </xs:complexType>
          <xs:element name="Interval" type="IntervalType" abstract="true"/>
          <xs:element name="RelativeInterval" type="RelativeIntervalType" substitutionGroup="Interval"/>
          <xs:complexType name="IntervalType" abstract="true"/>
          <xs:complexType name="RelativeIntervalType">
            <xs:complexContent>
              <xs:extension base="IntervalType">
                <xs:sequence>
                  <xs:element name="Start" type="xs:unsignedInt"/>
                </xs:sequence>
              </xs:extension>
            </xs:complexContent>
          </xs:complexType>
        </xs:schema>
        """;

        private static string Encoder(string schema, string typeName)
        {
            var files = EmitterHarness.Emit("test.sub", "SubCodec", ("sub.xsd", schema));
            return files.Single(f => f.FileName == typeName + ".kt").Source;
        }

        [Test]
        public void AConcreteHeadIsTheFallthroughBranch()
        {
            var src = Encoder(ConcreteHeadSchema, "RootType");

            // Most-derived first, because Kotlin's `is` matches subtypes too.
            Assert.That(src, Does.Contain("is ZModeType -> {"));

            // The head branch would test the property against its own declared type: always true,
            // which Kotlin reports and which makes the `else -> throw` behind it unreachable.
            Assert.That(src, Does.Not.Contain("is ModeType -> {"));
            Assert.That(src, Does.Contain("else -> {"));
            Assert.That(src, Does.Contain("encodeModeType(w, v)"));
            Assert.That(src, Does.Not.Contain("unsupported substitution member"),
                        "there is nothing left for it to reject");
        }

        [Test]
        public void AnAbstractHeadKeepsTheRejectingElse()
        {
            var src = Encoder(AbstractHeadSchema, "RootType");

            // The head is not a member here, so the last branch is a real check and a value that
            // matches none of them is a genuine error — the throw must survive.
            Assert.That(src, Does.Contain("is RelativeIntervalType -> {"));
            Assert.That(src, Does.Contain("else -> throw IllegalArgumentException(\"unsupported substitution member for Interval\")"));
        }

        [Test]
        public void TheWireCodesAreUnaffectedByBranchOrder()
        {
            var src = Encoder(ConcreteHeadSchema, "RootType");

            // Branches are ordered most-derived-first, but each keeps its own alphabetical position
            // on the wire — the head is 0 even though it is emitted last, and ZMode is 1 even
            // though its branch comes first.
            Assert.That(src, Does.Contain("w.writeBits(0u, 2)   // Mode"));
            Assert.That(src, Does.Contain("w.writeBits(1u, 2)   // ZMode"));
        }

        [Test]
        public void TheDecoderStillDispatchesOnTheEventCode()
        {
            // Only the encoder's branch shape changed; decoding reads the code and cannot collapse.
            var src = Encoder(ConcreteHeadSchema, "RootType");

            Assert.That(src, Does.Contain("0u -> decodeModeType(r)"));
            Assert.That(src, Does.Contain("1u -> decodeZModeType(r)"));
        }
    }
}
