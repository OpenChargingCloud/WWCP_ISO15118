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
    /// Construct-by-construct grammar/emit tests driven through the source generator on
    /// synthetic mini-XSDs (see <see cref="GeneratorHarness"/>). Each XSD construct gets a
    /// focused test before it is used against the real ISO 15118-2 schema set.
    /// </summary>
    [TestFixture]
    public class GeneratorGrammarTests
    {
        // ---- baseline: the single-file path still works -----------------------

        private const string SingleSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                   xmlns="urn:test:a" targetNamespace="urn:test:a">
          <xs:element name="root" type="RootType"/>
          <xs:complexType name="RootType">
            <xs:sequence>
              <xs:element name="Count" type="xs:unsignedInt"/>
            </xs:sequence>
          </xs:complexType>
        </xs:schema>
        """;

        [Test]
        public void SingleFile_Generates_WithoutDiagnostics()
        {
            var r = GeneratorHarness.Run(("a.xsd", SingleSchema));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            Assert.That(r.GeneratedSource, Does.Contain("record Root"));
            // Non-strict document grammar: a single global element still uses a >=1-bit selector.
            Assert.That(r.GeneratedSource, Does.Contain("Encode_Root"));
        }

        // ---- construct #1: multi-file import, cross-namespace type reference ---

        private const string Importer = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                   xmlns:b="urn:test:b" targetNamespace="urn:test:a">
          <xs:import namespace="urn:test:b" schemaLocation="b.xsd"/>
          <xs:element name="root" type="b:FooType"/>
        </xs:schema>
        """;

        private const string Imported = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                   xmlns="urn:test:b" targetNamespace="urn:test:b">
          <xs:complexType name="FooType">
            <xs:sequence>
              <xs:element name="X" type="xs:unsignedInt"/>
              <xs:element name="Y" type="xs:unsignedInt"/>
            </xs:sequence>
          </xs:complexType>
        </xs:schema>
        """;

        [Test]
        public void MultiFile_Import_ResolvesCrossNamespaceTypeRef()
        {
            // The importer's global element references a complexType in the imported file via a
            // prefix (b:FooType); the collected set must resolve it and emit the record + codec.
            var r = GeneratorHarness.Run(("a.xsd", Importer), ("b.xsd", Imported));

            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            Assert.That(r.GeneratedSource, Does.Contain("record Foo"));
            Assert.That(r.GeneratedSource, Does.Contain("Encode_Foo"));
        }

        [Test]
        public void ImportOrder_Independent()
        {
            // Same set, imported file first: merging must not depend on file order.
            var r = GeneratorHarness.Run(("b.xsd", Imported), ("a.xsd", Importer));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            Assert.That(r.GeneratedSource, Does.Contain("Encode_Foo"));
        }

        // ---- construct #2: complexContent / extension -------------------------

        [Test]
        public void Extension_MergesBaseThenDerivedParticles()
        {
            const string xsd = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns="urn:test:e" targetNamespace="urn:test:e">
              <xs:element name="root" type="DerivedType"/>
              <xs:complexType name="BaseType">
                <xs:sequence><xs:element name="BaseField" type="xs:unsignedInt"/></xs:sequence>
              </xs:complexType>
              <xs:complexType name="DerivedType">
                <xs:complexContent>
                  <xs:extension base="BaseType">
                    <xs:sequence><xs:element name="DerivedField" type="xs:unsignedInt"/></xs:sequence>
                  </xs:extension>
                </xs:complexContent>
              </xs:complexType>
            </xs:schema>
            """;
            var r = GeneratorHarness.Run(("e.xsd", xsd));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);

            // The record carries both fields, and the base field is encoded first.
            Assert.That(r.GeneratedSource, Does.Contain("BaseField"));
            Assert.That(r.GeneratedSource, Does.Contain("DerivedField"));
            int enc = r.GeneratedSource.IndexOf("Encode_Derived", StringComparison.Ordinal);
            int baseAt = r.GeneratedSource.IndexOf("msg.BaseField", enc, StringComparison.Ordinal);
            int derivedAt = r.GeneratedSource.IndexOf("msg.DerivedField", enc, StringComparison.Ordinal);
            Assert.That(baseAt, Is.GreaterThan(-1).And.LessThan(derivedAt),
                "base particle must be encoded before the derived particle");
        }

        [Test]
        public void Extension_OfEmptyAbstractBase_YieldsOwnParticlesOnly()
        {
            // Mirrors the ISO 15118-2 shape: an abstract empty BodyBaseType extended by a body.
            const string xsd = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns="urn:test:e" targetNamespace="urn:test:e">
              <xs:element name="root" type="MsgType"/>
              <xs:complexType name="BaseType" abstract="true"/>
              <xs:complexType name="MsgType">
                <xs:complexContent>
                  <xs:extension base="BaseType">
                    <xs:sequence><xs:element name="Only" type="xs:unsignedInt"/></xs:sequence>
                  </xs:extension>
                </xs:complexContent>
              </xs:complexType>
            </xs:schema>
            """;
            var r = GeneratorHarness.Run(("e.xsd", xsd));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            Assert.That(r.GeneratedSource, Does.Contain("Only"));
        }

        // ---- construct #3: substitutionGroup + abstract head + element ref ----

        private const string SubstSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                   xmlns="urn:test:s" targetNamespace="urn:test:s">
          <xs:element name="root" type="ContainerType"/>
          <xs:complexType name="ContainerType">
            <xs:sequence><xs:element ref="Head"/></xs:sequence>
          </xs:complexType>

          <xs:element name="Head" type="HeadBaseType" abstract="true"/>
          <xs:complexType name="HeadBaseType" abstract="true"/>

          <xs:element name="Alpha" type="AlphaType" substitutionGroup="Head"/>
          <xs:complexType name="AlphaType">
            <xs:complexContent><xs:extension base="HeadBaseType">
              <xs:sequence><xs:element name="A" type="xs:unsignedInt"/></xs:sequence>
            </xs:extension></xs:complexContent>
          </xs:complexType>

          <xs:element name="Beta" type="BetaType" substitutionGroup="Head"/>
          <xs:complexType name="BetaType">
            <xs:complexContent><xs:extension base="HeadBaseType">
              <xs:sequence><xs:element name="B" type="xs:unsignedInt"/></xs:sequence>
            </xs:extension></xs:complexContent>
          </xs:complexType>
        </xs:schema>
        """;

        [Test]
        public void SubstitutionGroup_EmitsAbstractBase_AndPolymorphicDispatch()
        {
            var r = GeneratorHarness.Run(("s.xsd", SubstSchema));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            // Abstract base record + members inheriting from it.
            Assert.That(src, Does.Contain("abstract record HeadBaseType"));
            Assert.That(src, Does.Contain(": HeadBaseType"));
            // Polymorphic encode: a case per concrete member (not the abstract head).
            Assert.That(src, Does.Contain("case AlphaType v:"));
            Assert.That(src, Does.Contain("case BetaType v:"));
        }

        [Test]
        public void SubstitutionGroup_IncludesAbstractHead_InEventCodeWidth()
        {
            // Members sorted by element name: Alpha(0), Beta(1), Head(2). Including the abstract
            // head makes 3 productions -> 2-bit event code (2 members alone would be 1 bit).
            var r = GeneratorHarness.Run(("s.xsd", SubstSchema));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            Assert.That(src, Does.Contain("w.WriteBits(0, 2)"), "Alpha at index 0, width 2");
            Assert.That(src, Does.Contain("w.WriteBits(1, 2)"), "Beta at index 1, width 2");
            // Decode reads the same 2-bit selector and rejects the abstract head slot (index 2).
            Assert.That(src, Does.Contain("r.ReadBits(2)"));
            Assert.That(src, Does.Contain("abstract substitution head cannot be decoded"));
        }

        // ---- construct: additional built-in datatypes -------------------------

        [Test]
        public void Builtins_BinaryAndSigned_MapToPrimitives()
        {
            const string xsd = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns="urn:test:b" targetNamespace="urn:test:b">
              <xs:element name="root" type="T"/>
              <xs:complexType name="T">
                <xs:sequence>
                  <xs:element name="Bin"   type="xs:hexBinary"/>
                  <xs:element name="Key"   type="xs:base64Binary"/>
                  <xs:element name="Stamp" type="xs:long"/>
                  <xs:element name="Delta" type="xs:int"/>
                </xs:sequence>
              </xs:complexType>
            </xs:schema>
            """;
            var r = GeneratorHarness.Run(("b.xsd", xsd));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            Assert.That(src, Does.Contain("byte[] Bin"));
            Assert.That(src, Does.Contain("byte[] Key"));
            Assert.That(src, Does.Contain("long Stamp"));
            Assert.That(src, Does.Contain("int Delta"));
            Assert.That(src, Does.Contain("ExiPrimitives.WriteBinary"));
            Assert.That(src, Does.Contain("ExiPrimitives.WriteSignedInteger"));
            Assert.That(src, Does.Contain("ExiPrimitives.ReadBinary"));
        }

        // ---- construct #4: optional attribute (AT event) ---------------------

        [Test]
        public void OptionalAttribute_EmittedAsMergedInitialState()
        {
            // Mirrors CertificateChainType: optional Id attribute + a required first content
            // element + a trailing optional element.
            const string xsd = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns="urn:test:a" targetNamespace="urn:test:a">
              <xs:element name="root" type="ChainType"/>
              <xs:complexType name="ChainType">
                <xs:sequence>
                  <xs:element name="Certificate" type="xs:base64Binary"/>
                  <xs:element name="Extra" type="xs:unsignedInt" minOccurs="0"/>
                </xs:sequence>
                <xs:attribute name="Id" type="xs:ID"/>
              </xs:complexType>
            </xs:schema>
            """;
            var r = GeneratorHarness.Run(("a.xsd", xsd));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            // Nullable attribute parameter, encoded as a string value with a 2-bit AT/SE selector.
            Assert.That(src, Does.Contain("string? Id"));
            Assert.That(src, Does.Contain("if (msg.Id is not null)"));
            Assert.That(src, Does.Contain("w.WriteBits(0, 2)"), "AT(Id) event code");
            Assert.That(src, Does.Contain("w.WriteBits(1, 2)"), "SE(first content) when attribute absent");
            // Decode reads the same 2-bit selector.
            Assert.That(src, Does.Contain("r.ReadBits(2)"));
            Assert.That(src, Does.Contain("_Id = ExiPrimitives.ReadStringValue"));
        }

        // ---- construct: repeating element within a sequence -------------------

        [Test]
        public void RepeatingElement_AsLastChild_EmittedAsList()
        {
            // Mirrors ParameterSetType: a leading scalar followed by a bounded-repeating element.
            const string xsd = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns="urn:test:r" targetNamespace="urn:test:r">
              <xs:element name="root" type="SetType"/>
              <xs:complexType name="SetType">
                <xs:sequence>
                  <xs:element name="SetID" type="xs:short"/>
                  <xs:element name="Item"  type="ItemType" maxOccurs="16"/>
                </xs:sequence>
              </xs:complexType>
              <xs:complexType name="ItemType">
                <xs:sequence><xs:element name="V" type="xs:unsignedInt"/></xs:sequence>
              </xs:complexType>
            </xs:schema>
            """;
            var r = GeneratorHarness.Run(("r.xsd", xsd));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            Assert.That(src, Does.Contain("short SetID"));
            Assert.That(src, Does.Contain("IReadOnlyList<ItemType> Item"));
            // The scalar is encoded before the list, and the list uses the 1-bit-first / 2-bit-loop
            // event codes with a 2-bit terminator.
            Assert.That(src, Does.Contain("w.WriteBits(0, i == 0 ? 1 : 2)"));
            Assert.That(src, Does.Contain("w.WriteBits(1, 2)"));
            int enc = src.IndexOf("Encode_SetType", StringComparison.Ordinal);
            int setId = src.IndexOf("msg.SetID", enc, StringComparison.Ordinal);
            int loop  = src.IndexOf("Item_list", enc, StringComparison.Ordinal);
            Assert.That(setId, Is.GreaterThan(-1).And.LessThan(loop));
        }

        // ---- construct #6: xs:choice + required attribute ---------------------

        [Test]
        public void ChoiceWithRequiredAttribute_EmitsSelectorAndPrefix()
        {
            // Mirrors ParameterType: a required Name attribute followed by a choice of typed values.
            const string xsd = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns="urn:test:c" targetNamespace="urn:test:c">
              <xs:element name="root" type="ParamType"/>
              <xs:complexType name="ParamType">
                <xs:choice>
                  <xs:element name="boolValue"   type="xs:boolean"/>
                  <xs:element name="intValue"    type="xs:int"/>
                  <xs:element name="stringValue" type="xs:string"/>
                </xs:choice>
                <xs:attribute name="Name" type="xs:string" use="required"/>
              </xs:complexType>
            </xs:schema>
            """;
            var r = GeneratorHarness.Run(("c.xsd", xsd));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            // Record: required attr (non-nullable) + mutually-exclusive nullable alternatives (field
            // names are PascalCased by the generator).
            Assert.That(src, Does.Contain("string Name"));
            Assert.That(src, Does.Contain("bool? BoolValue"));
            Assert.That(src, Does.Contain("int? IntValue"));
            Assert.That(src, Does.Contain("string? StringValue"));
            // Required-attribute prefix (1-bit AT) then a 2-bit choice selector (3 alts -> 2 bits).
            Assert.That(src, Does.Contain("w.WriteBits(0, 1);   // AT(required attribute)"));
            Assert.That(src, Does.Contain("if (msg.BoolValue is not null)"));
            Assert.That(src, Does.Contain("w.WriteBits(1, 2)"), "IntValue at choice index 1");
            Assert.That(src, Does.Contain("switch (r.ReadBits(2))"));
        }

        // ---- construct #7: xs:simpleContent extension -------------------------

        [Test]
        public void SimpleContent_ValuePlusRequiredAttribute()
        {
            // Mirrors ContractSignatureEncryptedPrivateKeyType: a base64 value with a required Id.
            const string xsd = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns="urn:test:sc" targetNamespace="urn:test:sc">
              <xs:element name="root" type="EncKeyType"/>
              <xs:complexType name="EncKeyType">
                <xs:simpleContent>
                  <xs:extension base="keyType">
                    <xs:attribute name="Id" type="xs:ID" use="required"/>
                  </xs:extension>
                </xs:simpleContent>
              </xs:complexType>
              <xs:simpleType name="keyType">
                <xs:restriction base="xs:base64Binary"><xs:maxLength value="48"/></xs:restriction>
              </xs:simpleType>
            </xs:schema>
            """;
            var r = GeneratorHarness.Run(("sc.xsd", xsd));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            Assert.That(src, Does.Contain("string Id"));
            Assert.That(src, Does.Contain("byte[] Value"));
            Assert.That(src, Does.Contain("w.WriteBits(0, 1);   // AT(required attribute)"));
            Assert.That(src, Does.Contain("w.WriteBits(0, 1);   // CONTENT event"));
            Assert.That(src, Does.Contain("ExiPrimitives.WriteBinary(ref w, msg.Value)"));
            Assert.That(src, Does.Contain("var _Value = ExiPrimitives.ReadBinary(ref r)"));
        }

        // ---- construct #8: opaque XMLDSig reference + runs of trailing optionals ----

        private const string DsigSchema = """
        <schema xmlns="http://www.w3.org/2001/XMLSchema" xmlns:ds="http://www.w3.org/2000/09/xmldsig#"
                targetNamespace="http://www.w3.org/2000/09/xmldsig#" elementFormDefault="qualified">
          <!-- Opaque signature subtree: xs:any / refs, never modelled. KeyInfo (unlike the now
               modelled SignedInfo subtree) stays opaque. -->
          <element name="KeyInfo" type="ds:KeyInfoType"/>
          <complexType name="KeyInfoType">
            <sequence><any processContents="lax"/></sequence>
            <attribute name="Id" type="ID"/>
          </complexType>
          <!-- A self-contained data type genuinely referenced by the main schema (like
               X509IssuerSerialType): unprefixed built-in field types resolve via the default
               XSD namespace. -->
          <complexType name="X509IssuerSerialType">
            <sequence>
              <element name="X509IssuerName" type="string"/>
              <element name="X509SerialNumber" type="integer"/>
            </sequence>
          </complexType>
        </schema>
        """;

        private const string HeaderSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:ds="http://www.w3.org/2000/09/xmldsig#"
                   xmlns="urn:test:h" targetNamespace="urn:test:h" elementFormDefault="qualified">
          <xs:import namespace="http://www.w3.org/2000/09/xmldsig#" schemaLocation="dsig.xsd"/>
          <xs:element name="root" type="HeaderType"/>
          <xs:complexType name="HeaderType">
            <xs:sequence>
              <xs:element name="SessionID" type="xs:hexBinary"/>
              <xs:element name="Note" type="xs:unsignedInt" minOccurs="0"/>
              <xs:element ref="ds:KeyInfo" minOccurs="0"/>
              <xs:element name="CertId" type="ds:X509IssuerSerialType" minOccurs="0"/>
            </xs:sequence>
          </xs:complexType>
        </xs:schema>
        """;

        [Test]
        public void OpaqueReference_ModelledAsAbsentPlaceholder()
        {
            var r = GeneratorHarness.Run(("h.xsd", HeaderSchema), ("dsig.xsd", DsigSchema));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            // The opaque KeyInfo element becomes an empty placeholder record and a nullable field;
            // encoding/decoding a present instance fails loud (deferred to Phase 3).
            Assert.That(src, Does.Contain("public sealed record KeyInfo();"));
            Assert.That(src, Does.Contain("KeyInfo? KeyInfo"));
            Assert.That(src, Does.Contain("(XMLDSig) is deferred to Phase 3"));
            // The self-contained data type from the opaque namespace IS modelled (unprefixed built-ins
            // resolved via the default XSD namespace: string -> string, integer -> long/EXI Integer).
            Assert.That(src, Does.Contain("record X509IssuerSerialType"));
            Assert.That(src, Does.Contain("string X509IssuerName"));
            Assert.That(src, Does.Contain("long X509SerialNumber"));
            Assert.That(src, Does.Contain("ExiPrimitives.WriteSignedInteger"));
        }

        [Test]
        public void TrailingOptionalRun_UsesCbV2GEventCodeWidths()
        {
            // SessionID (required) + a run of trailing optionals (Note, Signature, CertId) ending in
            // the element EE — the ISO 15118-2 message-header shape. cbexigen widths each state at
            // ceil(log2(productions+1)): 3 optionals + EE = 4 productions -> 3 bits, and the terminating
            // EE for the all-absent path takes the highest event code at that width.
            var r = GeneratorHarness.Run(("h.xsd", HeaderSchema), ("dsig.xsd", DsigSchema));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            // State 0 (Note, Signature, CertId, EE): 4 productions -> 3-bit codes; all-absent EE = code 3.
            Assert.That(src, Does.Contain("w.WriteBits(0, 3);   // Note"));
            Assert.That(src, Does.Contain("w.WriteBits(3, 3);   // element EE"));
            // A later state (Signature, CertId, EE): 3 productions -> 2-bit codes.
            Assert.That(src, Does.Contain("w.WriteBits(2, 2);   // element EE"));
            // Final state after the last optional (CertId) present: 1-bit EE.
            Assert.That(src, Does.Contain("w.WriteBits(0, 1);   // element EE"));
            // Decode reads the same widths.
            Assert.That(src, Does.Contain("r.ReadBits(3)"));
            Assert.That(src, Does.Contain("r.ReadBits(2)"));
        }

        [Test]
        public void OptionalRunAndOpaque_GeneratedCodeCompiles()
        {
            // The multi-optional-run, opaque-placeholder, and complex-terminator paths are not
            // exercised by the checked-in AppProtocol codec — compile the generated source directly
            // (against the Prototype's BitWriter/ExiPrimitives) to prove it builds.
            var r = GeneratorHarness.Run(("h.xsd", HeaderSchema), ("dsig.xsd", DsigSchema));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);

            var errors = GeneratorHarness.CompileErrors(r, typeof(cloud.charging.open.protocols.ISO15118.EXI.ExiPrimitives));
            Assert.That(errors, Is.Empty,
                r.GeneratedSource + "\n\n" + string.Join("\n", errors.Select(e => e.ToString())));
        }

        [Test]
        public void OptionalRunBeforeRequired_FoldsTerminatorSeIntoEventCode()
        {
            // A run of optionals terminated by a required element (CurrentDemandResType shape): the
            // required element's SE is folded into the run's event codes. 2 optionals + the required
            // terminator + EE-phantom = width ceil(log2(3+1)) = 2 bits at the first state; the
            // terminator takes the highest code when all optionals are absent.
            const string xsd = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns="urn:test:o" targetNamespace="urn:test:o">
              <xs:element name="root" type="T"/>
              <xs:complexType name="T">
                <xs:sequence>
                  <xs:element name="A" type="xs:unsignedInt"/>
                  <xs:element name="Opt1" type="xs:unsignedInt" minOccurs="0"/>
                  <xs:element name="Opt2" type="xs:unsignedInt" minOccurs="0"/>
                  <xs:element name="Req"  type="xs:unsignedInt"/>
                </xs:sequence>
              </xs:complexType>
            </xs:schema>
            """;
            var r = GeneratorHarness.Run(("o.xsd", xsd));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            // State 0 (Opt1, Opt2, Req): 3 productions -> 2 bits; Req (all optionals absent) = code 2.
            Assert.That(src, Does.Contain("w.WriteBits(0, 2);   // Opt1"));
            Assert.That(src, Does.Contain("w.WriteBits(2, 2);   // SE(Req)"));
            // Reached via the last optional present, Req is at its own 1-bit SE state.
            Assert.That(src, Does.Contain("w.WriteBits(0, 1);   // SE(Req)"));
            // The required terminator's content is emitted (not skipped) and the element still ends
            // with its own EE afterwards.
            Assert.That(src, Does.Contain("w.WriteBits(0, 1);   // element EE"));
        }

        // ---- construct #9: optional attribute + optional content ----

        private const string AuthReqSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                   xmlns="urn:test:auth" targetNamespace="urn:test:auth">
          <xs:element name="root" type="AuthReqType"/>
          <xs:complexType name="AuthReqType">
            <xs:sequence>
              <xs:element name="GenChallenge" type="xs:base64Binary" minOccurs="0"/>
            </xs:sequence>
            <xs:attribute name="Id" type="xs:ID"/>
          </xs:complexType>
        </xs:schema>
        """;

        [Test]
        public void OptionalAttributeWithOptionalContent_FoldsAtIntoContentRun()
        {
            // AuthorizationReqType shape: optional Id attribute + optional GenChallenge element. cbV2G
            // grammar 222/223: the AT event is the first production of the content's initial state, so
            // {Id, GenChallenge, EE} is a 3-production (2-bit) state — the attribute is just the leading
            // optional of the run. This used to fail loud ("first content child must be required").
            var r = GeneratorHarness.Run(("auth.xsd", AuthReqSchema));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            // Record: attribute first (nullable), then the optional element.
            Assert.That(src, Does.Contain("string? Id"));
            Assert.That(src, Does.Contain("byte[]? GenChallenge"));
            // State 0 {Id, GenChallenge, EE}: Id at code 0, all-absent EE at code 2, both 2-bit.
            Assert.That(src, Does.Contain("w.WriteBits(0, 2);   // Id"));
            Assert.That(src, Does.Contain("w.WriteBits(2, 2);   // element EE"));
            // The AT value is a bare string — no value-start bit, unlike an element value.
            Assert.That(src, Does.Contain("ExiPrimitives.WriteStringValue(ref w, msg.Id!);"));
            Assert.That(src, Does.Contain("_Id = ExiPrimitives.ReadStringValue(ref r, \"Id\");"));
        }

        [Test]
        public void OptionalAttributeWithOptionalContent_GeneratedCodeCompiles()
        {
            var r = GeneratorHarness.Run(("auth.xsd", AuthReqSchema));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var errors = GeneratorHarness.CompileErrors(r, typeof(cloud.charging.open.protocols.ISO15118.EXI.ExiPrimitives));
            Assert.That(errors, Is.Empty,
                r.GeneratedSource + "\n\n" + string.Join("\n", errors.Select(e => e.ToString())));
        }

        // ---- construct #10: substitution references flattened into optional runs ----

        private const string OptionalSubstSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                   xmlns="urn:test:pd" targetNamespace="urn:test:pd">
          <xs:element name="root" type="PdType"/>
          <xs:complexType name="PdType">
            <xs:sequence>
              <xs:element name="Req1" type="xs:unsignedInt"/>
              <xs:element name="Opt1" type="xs:unsignedInt" minOccurs="0"/>
              <xs:element ref="EVParam" minOccurs="0"/>
            </xs:sequence>
          </xs:complexType>
          <xs:element name="EVParam" type="EVParamBase" abstract="true"/>
          <xs:complexType name="EVParamBase" abstract="true"/>
          <xs:element name="DC_EVParam" type="DCEVParamType" substitutionGroup="EVParam"/>
          <xs:complexType name="DCEVParamType">
            <xs:complexContent><xs:extension base="EVParamBase">
              <xs:sequence><xs:element name="X" type="xs:unsignedInt"/></xs:sequence>
            </xs:extension></xs:complexContent>
          </xs:complexType>
        </xs:schema>
        """;

        [Test]
        public void OptionalSubstitutionInRun_FlattensMembersAsProductions()
        {
            // PowerDeliveryReqType shape: an optional element (Opt1) then an optional substitution
            // reference (EVParam, member DC_EVParam + abstract head), ending in EE. cbV2G grammar
            // 199/200: the members are individual productions in the run's grammar state alongside the
            // sibling optional and the EE — {Opt1, DC_EVParam, head, EE} is one 3-bit state.
            var r = GeneratorHarness.Run(("pd.xsd", OptionalSubstSchema));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            // State 0 {Opt1, DC_EVParam, head, EE} = 4 productions -> 3 bits.
            Assert.That(src, Does.Contain("w.WriteBits(0, 3);   // Opt1"));
            Assert.That(src, Does.Contain("w.WriteBits(1, 3);   // DC_EVParam"));
            Assert.That(src, Does.Contain("w.WriteBits(3, 3);   // element EE"));
            // State 1 (Opt1 consumed) {DC_EVParam, head, EE} = 3 productions -> 2 bits.
            Assert.That(src, Does.Contain("w.WriteBits(0, 2);   // DC_EVParam"));
            Assert.That(src, Does.Contain("w.WriteBits(2, 2);   // element EE"));
            // Dispatch is by runtime type; the abstract head reserves its slot but has no branch.
            Assert.That(src, Does.Contain("msg.EVParam is DCEVParamType"));
        }

        private const string RequiredSubstTerminatorSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                   xmlns="urn:test:cpd" targetNamespace="urn:test:cpd">
          <xs:element name="root" type="CpdType"/>
          <xs:complexType name="CpdType">
            <xs:sequence>
              <xs:element name="Req1" type="xs:unsignedInt"/>
              <xs:element ref="SASch" minOccurs="0"/>
              <xs:element ref="EVSEParam"/>
            </xs:sequence>
          </xs:complexType>
          <xs:element name="SASch" type="SASchBase" abstract="true"/>
          <xs:complexType name="SASchBase" abstract="true"/>
          <xs:element name="SAList" type="SAListType" substitutionGroup="SASch"/>
          <xs:complexType name="SAListType"><xs:complexContent><xs:extension base="SASchBase">
            <xs:sequence><xs:element name="Y" type="xs:unsignedInt"/></xs:sequence></xs:extension></xs:complexContent></xs:complexType>
          <xs:element name="EVSEParam" type="EVSEParamBase" abstract="true"/>
          <xs:complexType name="EVSEParamBase" abstract="true"/>
          <xs:element name="AC_EVSEParam" type="ACEVSEParamType" substitutionGroup="EVSEParam"/>
          <xs:complexType name="ACEVSEParamType"><xs:complexContent><xs:extension base="EVSEParamBase">
            <xs:sequence><xs:element name="A" type="xs:unsignedInt"/></xs:sequence></xs:extension></xs:complexContent></xs:complexType>
          <xs:element name="DC_EVSEParam" type="DCEVSEParamType" substitutionGroup="EVSEParam"/>
          <xs:complexType name="DCEVSEParamType"><xs:complexContent><xs:extension base="EVSEParamBase">
            <xs:sequence><xs:element name="D" type="xs:unsignedInt"/></xs:sequence></xs:extension></xs:complexContent></xs:complexType>
        </xs:schema>
        """;

        [Test]
        public void RequiredSubstitutionTerminatesRun_FoldsMembersIntoState()
        {
            // ChargeParameterDiscoveryResType shape: an optional substitution reference (SASch) followed
            // by a required substitution reference (EVSEParam). cbV2G grammar 284/285: both expansions
            // share the state — {SAList, SASch-head, AC, DC, EVSEParam-head} = 5 productions -> 3 bits;
            // once SASch is consumed, only the required terminator's members remain (3 -> 2 bits).
            var r = GeneratorHarness.Run(("cpd.xsd", RequiredSubstTerminatorSchema));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            // State 0: SAList(0), SASch-head(1, reserved), AC(2), DC(3), EVSEParam-head(4, reserved).
            Assert.That(src, Does.Contain("w.WriteBits(0, 3);   // SAList"));
            Assert.That(src, Does.Contain("w.WriteBits(2, 3);   // AC_EVSEParam"));
            Assert.That(src, Does.Contain("w.WriteBits(3, 3);   // DC_EVSEParam"));
            // State 1 (SASch consumed): the required terminator's members AC(0), DC(1) at 2 bits.
            Assert.That(src, Does.Contain("w.WriteBits(0, 2);   // AC_EVSEParam"));
            Assert.That(src, Does.Contain("w.WriteBits(1, 2);   // DC_EVSEParam"));
        }

        [Test]
        public void SubstitutionInRuns_GeneratedCodeCompiles()
        {
            foreach (var (name, xsd) in new[] { ("pd.xsd", OptionalSubstSchema), ("cpd.xsd", RequiredSubstTerminatorSchema) })
            {
                var r = GeneratorHarness.Run((name, xsd));
                Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
                var errors = GeneratorHarness.CompileErrors(r, typeof(cloud.charging.open.protocols.ISO15118.EXI.ExiPrimitives));
                Assert.That(errors, Is.Empty,
                    r.GeneratedSource + "\n\n" + string.Join("\n", errors.Select(e => e.ToString())));
            }
        }

        // ---- construct #11: an optional bounded-repeating element inside an optional run ----

        private const string OptionalRepeatingSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                   xmlns="urn:test:st" targetNamespace="urn:test:st">
          <xs:element name="root" type="EntryT"/>
          <xs:complexType name="EntryT">
            <xs:sequence>
              <xs:element name="Req1" type="xs:unsignedInt"/>
              <xs:element name="EPrice" type="xs:unsignedByte" minOccurs="0"/>
              <xs:element name="Cost" type="CostT" minOccurs="0" maxOccurs="3"/>
            </xs:sequence>
          </xs:complexType>
          <xs:complexType name="CostT">
            <xs:sequence><xs:element name="V" type="xs:unsignedInt"/></xs:sequence>
          </xs:complexType>
        </xs:schema>
        """;

        [Test]
        public void OptionalRepeatingInRun_FirstItemIsAStateProduction_RestLoop()
        {
            // SalesTariffEntryType shape: an optional element (EPrice) then an optional bounded-repeating
            // element (Cost, maxOccurs=3), ending in EE. cbV2G grammar 39-42: the FIRST Cost item is a
            // production of the run's grammar state {EPrice, Cost, EE}; further items and the terminating
            // EE use the 2-bit loop {item=0, EE=1}. The bound is enforced by the array, not the grammar.
            var r = GeneratorHarness.Run(("st.xsd", OptionalRepeatingSchema));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            Assert.That(src, Does.Contain("IReadOnlyList<CostT> Cost"));
            // State 0 {EPrice(0), Cost-first(1), EE(2)} = 3 productions -> 2 bits.
            Assert.That(src, Does.Contain("w.WriteBits(0, 2);   // EPrice"));
            Assert.That(src, Does.Contain("w.WriteBits(1, 2);   // Cost"));
            Assert.That(src, Does.Contain("w.WriteBits(2, 2);   // element EE"));
            // The loop: further items at code 0, the list-terminating EE at code 1 (both 2-bit).
            Assert.That(src, Does.Contain("w.WriteBits(1, 2);   // element EE (list end)"));
            Assert.That(src, Does.Contain("for (int ci = 1; ci < msg.Cost.Count; ci++)"));
            // Decode reads the first item then loops until the EE.
            Assert.That(src, Does.Contain("if (lc == 1) break;   // element EE (list end)"));
        }

        [Test]
        public void OptionalRepeatingInRun_GeneratedCodeCompiles()
        {
            var r = GeneratorHarness.Run(("st.xsd", OptionalRepeatingSchema));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var errors = GeneratorHarness.CompileErrors(r, typeof(cloud.charging.open.protocols.ISO15118.EXI.ExiPrimitives));
            Assert.That(errors, Is.Empty,
                r.GeneratedSource + "\n\n" + string.Join("\n", errors.Select(e => e.ToString())));
        }

        // ---- construct #12: optional run terminated by a required repeating element ----

        private const string RequiredRepeatingTerminatorSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                   xmlns="urn:test:tar" targetNamespace="urn:test:tar">
          <xs:element name="root" type="TarT"/>
          <xs:complexType name="TarT">
            <xs:sequence>
              <xs:element name="Req1" type="xs:unsignedInt"/>
              <xs:element name="Desc" type="xs:string" minOccurs="0"/>
              <xs:element name="Num" type="xs:unsignedByte" minOccurs="0"/>
              <xs:element name="Entry" type="EntryT" maxOccurs="4"/>
            </xs:sequence>
            <xs:attribute name="Id" type="xs:ID"/>
          </xs:complexType>
          <xs:complexType name="EntryT">
            <xs:sequence><xs:element name="V" type="xs:unsignedInt"/></xs:sequence>
          </xs:complexType>
        </xs:schema>
        """;

        [Test]
        public void RequiredRepeatingTerminatesRun_FirstItemIsAStateProduction()
        {
            // SalesTariffType shape: an optional run {Desc, Num} terminated by a REQUIRED repeating
            // element (Entry, minOccurs=1). cbV2G grammar 58-63: since Entry is required there is no EE
            // production in the run; its first item is the highest-code production of each state
            // ({Desc, Num, Entry} = 2 bits; then {Num, Entry}; then {Entry} = 1 bit), and further items
            // and the terminating EE use the 2-bit loop.
            var r = GeneratorHarness.Run(("tar.xsd", RequiredRepeatingTerminatorSchema));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            Assert.That(src, Does.Contain("IReadOnlyList<EntryT> Entry"));
            // State {Desc(0), Num(1), Entry(2)}: Entry first item at code 2, 2 bits (all optionals absent).
            Assert.That(src, Does.Contain("w.WriteBits(2, 2);   // Entry"));
            // Final state {Entry} = 1 production -> 1 bit.
            Assert.That(src, Does.Contain("w.WriteBits(0, 1);   // Entry"));
            // Further items and the list-terminating EE at the 2-bit loop code.
            Assert.That(src, Does.Contain("w.WriteBits(1, 2);   // element EE (list end)"));
        }

        [Test]
        public void RequiredRepeatingTerminator_GeneratedCodeCompiles()
        {
            var r = GeneratorHarness.Run(("tar.xsd", RequiredRepeatingTerminatorSchema));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var errors = GeneratorHarness.CompileErrors(r, typeof(cloud.charging.open.protocols.ISO15118.EXI.ExiPrimitives));
            Assert.That(errors, Is.Empty,
                r.GeneratedSource + "\n\n" + string.Join("\n", errors.Select(e => e.ToString())));
        }

        // ---- construct #13: xs:any wildcard -> optional base64 ANY (XMLDSig subtree) ----

        [Test]
        public void XsAny_BecomesOptionalBase64AnyElement()
        {
            // CanonicalizationMethodType shape: a required Algorithm attribute over an xs:any wildcard.
            // cbexigen models the wildcard as a single optional base64 "ANY" element; mixed is ignored.
            const string xsd = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns="urn:test:any" targetNamespace="urn:test:any">
              <xs:element name="root" type="MethodType"/>
              <xs:complexType name="MethodType" mixed="true">
                <xs:sequence>
                  <xs:any namespace="##any" minOccurs="0" maxOccurs="unbounded" processContents="lax"/>
                </xs:sequence>
                <xs:attribute name="Algorithm" type="xs:anyURI" use="required"/>
              </xs:complexType>
            </xs:schema>
            """;
            var r = GeneratorHarness.Run(("any.xsd", xsd));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            Assert.That(src, Does.Contain("string Algorithm"));
            Assert.That(src, Does.Contain("byte[]? ANY"));
            var errors = GeneratorHarness.CompileErrors(r, typeof(cloud.charging.open.protocols.ISO15118.EXI.ExiPrimitives));
            Assert.That(errors, Is.Empty, string.Join("\n", errors.Select(e => e.ToString())));
        }

        // ---- construct #13 (ISO 15118-20): xs:choice nested inside a sequence ----
        //
        // Distinct from a root-level "whole content is a choice" complexType (already supported,
        // ParameterType-style) and from a substitution-group reference: cbexigen models each branch of
        // an inline choice as its OWN independent (always-nullable) field — N sibling _isUsed-flagged
        // fields, not one polymorphic field — verified against cbV2G's iso20_AuthorizationSetupResType
        // (state 272: 2 required branches, 2-bit width, no absence production) and
        // iso20_SignedInstallationDataType (3 branches — 2 simple base64Binary, 1 complex — in
        // DOCUMENT order, not alphabetical: SECP521=0, X448=1, TPM=2).

        [Test]
        public void StandaloneRequiredInlineChoice_TwoComplexBranches_NoPrecedingOptionals()
        {
            // AuthorizationSetupResType shape: two required leading elements, then a required trailing
            // choice with no preceding optionals to flatten into — hits the standalone dispatch path
            // directly (case ChildShape.RequiredSingle), not the optional-run machine.
            const string xsd = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns="urn:test:ic1" targetNamespace="urn:test:ic1">
              <xs:element name="root" type="T"/>
              <xs:complexType name="T">
                <xs:sequence>
                  <xs:element name="A" type="xs:unsignedInt"/>
                  <xs:element name="B" type="xs:boolean"/>
                  <xs:choice>
                    <xs:element name="EIM_Mode" type="EimType"/>
                    <xs:element name="PnC_Mode" type="PncType"/>
                  </xs:choice>
                </xs:sequence>
              </xs:complexType>
              <xs:complexType name="EimType">
                <xs:sequence><xs:element name="X" type="xs:unsignedInt"/></xs:sequence>
              </xs:complexType>
              <xs:complexType name="PncType">
                <xs:sequence><xs:element name="Y" type="xs:unsignedInt"/></xs:sequence>
              </xs:complexType>
            </xs:schema>
            """;
            var r = GeneratorHarness.Run(("ic1.xsd", xsd));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            // Each branch is its own nullable field on T — not one polymorphic field.
            Assert.That(src, Does.Contain("EimType? EIM_Mode"));
            Assert.That(src, Does.Contain("PncType? PnC_Mode"));
            // 2 branches + phantom = ceil(log2(3)) = 2 bits; no absence production (required choice).
            Assert.That(src, Does.Contain("if (msg.EIM_Mode is not null)"));
            Assert.That(src, Does.Contain("w.WriteBits(0, 2);   // EIM_Mode"));
            Assert.That(src, Does.Contain("w.WriteBits(1, 2);   // PnC_Mode"));
            Assert.That(src, Does.Contain("else throw new ArgumentException(\"no choice alternative set\");"));

            var errors = GeneratorHarness.CompileErrors(r, typeof(cloud.charging.open.protocols.ISO15118.EXI.ExiPrimitives));
            Assert.That(errors, Is.Empty, src + "\n\n" + string.Join("\n", errors.Select(e => e.ToString())));
        }

        [Test]
        public void OptionalInlineChoice_MixedSimpleAndComplexBranches_DocumentOrder()
        {
            // Dynamic_SEResControlModeType / SignedInstallationDataType shape combined: an optional
            // trailing choice with 3 branches (2 simple base64Binary-like, 1 complex), preceded by other
            // OPTIONAL siblings so it flattens into the SAME run/state as them. Event codes must follow
            // DOCUMENT order (Alpha, Charlie, Bravo — deliberately non-alphabetical), matching cbV2G.
            const string xsd = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns="urn:test:ic2" targetNamespace="urn:test:ic2">
              <xs:element name="root" type="T"/>
              <xs:complexType name="T">
                <xs:sequence>
                  <xs:element name="Opt1" type="xs:unsignedInt" minOccurs="0"/>
                  <xs:choice minOccurs="0">
                    <xs:element name="Alpha" type="xs:base64Binary"/>
                    <xs:element name="Charlie" type="InnerType"/>
                    <xs:element name="Bravo" type="xs:base64Binary"/>
                  </xs:choice>
                </xs:sequence>
              </xs:complexType>
              <xs:complexType name="InnerType">
                <xs:sequence><xs:element name="Z" type="xs:unsignedInt"/></xs:sequence>
              </xs:complexType>
            </xs:schema>
            """;
            var r = GeneratorHarness.Run(("ic2.xsd", xsd));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            // 3 branches, each its own nullable field; simple branches use byte[], complex use the record.
            Assert.That(src, Does.Contain("byte[]? Alpha"));
            Assert.That(src, Does.Contain("InnerType? Charlie"));
            Assert.That(src, Does.Contain("byte[]? Bravo"));
            // State 0 (Opt1, Alpha, Charlie, Bravo, EE): 5 productions -> ceil(log2(6)) = 3 bits.
            Assert.That(src, Does.Contain("w.WriteBits(0, 3);   // Opt1"));
            // Reached after Opt1 present: state 1 (Alpha, Charlie, Bravo, EE): 4 -> ceil(log2(5)) = 3 bits.
            // Document order (not alphabetical Bravo<Charlie): Alpha=0, Charlie=1, Bravo=2.
            Assert.That(src, Does.Contain("w.WriteBits(0, 3);   // Alpha"));
            Assert.That(src, Does.Contain("w.WriteBits(1, 3);   // Charlie"));
            Assert.That(src, Does.Contain("w.WriteBits(2, 3);   // Bravo"));

            var errors = GeneratorHarness.CompileErrors(r, typeof(cloud.charging.open.protocols.ISO15118.EXI.ExiPrimitives));
            Assert.That(errors, Is.Empty, src + "\n\n" + string.Join("\n", errors.Select(e => e.ToString())));
        }

        [Test]
        public void InlineChoice_MoreThanOneChoiceInSequence_IsRejected()
        {
            const string xsd = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns="urn:test:ic3" targetNamespace="urn:test:ic3">
              <xs:element name="root" type="T"/>
              <xs:complexType name="T">
                <xs:sequence>
                  <xs:choice><xs:element name="A" type="xs:unsignedInt"/><xs:element name="B" type="xs:unsignedInt"/></xs:choice>
                  <xs:choice><xs:element name="C" type="xs:unsignedInt"/><xs:element name="D" type="xs:unsignedInt"/></xs:choice>
                </xs:sequence>
              </xs:complexType>
            </xs:schema>
            """;
            var r = GeneratorHarness.Run(("ic3.xsd", xsd));
            Assert.That(r.Diagnostics, Is.Not.Empty);
        }

        [Test]
        public void InlineChoice_NotLastParticle_IsSupported()
        {
            // EVPowerProfileType shape: a required choice in the MIDDLE of the sequence, followed by
            // more required content (a bounded list) — the standalone dispatch path does not assume the
            // choice ends the element, so this must generate and compile cleanly, in document order.
            const string xsd = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns="urn:test:ic4" targetNamespace="urn:test:ic4">
              <xs:element name="root" type="T"/>
              <xs:complexType name="T">
                <xs:sequence>
                  <xs:element name="TimeAnchor" type="xs:unsignedLong"/>
                  <xs:choice>
                    <xs:element name="A" type="xs:unsignedInt"/>
                    <xs:element name="B" type="xs:unsignedInt"/>
                  </xs:choice>
                  <xs:element name="Entries" type="xs:unsignedInt" maxOccurs="4"/>
                </xs:sequence>
              </xs:complexType>
            </xs:schema>
            """;
            var r = GeneratorHarness.Run(("ic4.xsd", xsd));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            Assert.That(src, Does.Contain("ulong TimeAnchor"));
            Assert.That(src, Does.Contain("uint? A"));
            Assert.That(src, Does.Contain("uint? B"));
            Assert.That(src, Does.Contain("IReadOnlyList<uint> Entries"));
            // TimeAnchor (required, plain) is emitted first, then the standalone choice dispatch, then
            // the required repeating Entries list — in that document order.
            int iTime = src.IndexOf("record T(", System.StringComparison.Ordinal);
            int iChoice = src.IndexOf("if (msg.A is not null)", System.StringComparison.Ordinal);
            int iEntries = src.IndexOf("Entries_list", System.StringComparison.Ordinal);
            Assert.That(iTime, Is.GreaterThan(-1));
            Assert.That(iChoice, Is.GreaterThan(iTime));
            Assert.That(iEntries, Is.GreaterThan(iChoice));

            var errors = GeneratorHarness.CompileErrors(r, typeof(cloud.charging.open.protocols.ISO15118.EXI.ExiPrimitives));
            Assert.That(errors, Is.Empty, src + "\n\n" + string.Join("\n", errors.Select(e => e.ToString())));
        }

        // ---- construct #14 (ISO 15118-20): a required bounded-repeating list (maxOccurs=2) followed
        // by more content, not the sequence's last particle ----

        [Test]
        public void RequiredRepeatingList_MaxOccurs2_FollowedByMoreContent()
        {
            // AuthorizationSetupResType shape: AuthorizationServices (required, maxOccurs=2) directly
            // followed by CertificateInstallationService (required bool) and the EIM/PnC choice.
            // cbV2G's grammar folds ONLY the immediate next particle into the list's own "continue vs
            // move on" event codes; the choice after that is dispatched independently.
            const string xsd = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns="urn:test:rt1" targetNamespace="urn:test:rt1">
              <xs:element name="root" type="T"/>
              <xs:complexType name="T">
                <xs:sequence>
                  <xs:element name="Services" type="xs:unsignedByte" maxOccurs="2"/>
                  <xs:element name="Flag" type="xs:boolean"/>
                  <xs:choice>
                    <xs:element name="A" type="xs:unsignedInt"/>
                    <xs:element name="B" type="xs:unsignedInt"/>
                  </xs:choice>
                </xs:sequence>
              </xs:complexType>
            </xs:schema>
            """;
            var r = GeneratorHarness.Run(("rt1.xsd", xsd));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            Assert.That(src, Does.Contain("IReadOnlyList<byte> Services"));
            Assert.That(src, Does.Contain("bool Flag"));
            // First item: unconditional 1-bit SE.
            Assert.That(src, Does.Contain("w.WriteBits(0, 1);   // SE(Services)"));
            // Mid-state (1 item collected): {continue=0, Flag-SE=1}, 2 bits.
            Assert.That(src, Does.Contain("w.WriteBits(0, 2);   // Services (loop)"));
            Assert.That(src, Does.Contain("w.WriteBits(1, 2);   // Flag"));
            // At-max state (2 items collected): {Flag-SE=0}, 1 bit, unconditional (no presence check —
            // Flag is a required, non-nullable bool; `is not null` would not even compile for it).
            Assert.That(src, Does.Contain("w.WriteBits(0, 1);   // Flag"));
            Assert.That(src, Does.Not.Contain("msg.Flag is not null"));
            // The choice after Flag is dispatched independently (its own 2-bit width), not folded in.
            Assert.That(src, Does.Contain("if (msg.A is not null)"));

            var errors = GeneratorHarness.CompileErrors(r, typeof(cloud.charging.open.protocols.ISO15118.EXI.ExiPrimitives));
            Assert.That(errors, Is.Empty, src + "\n\n" + string.Join("\n", errors.Select(e => e.ToString())));
        }

        // ---- construct #16 (ISO 15118-20 WPT): required repeating list, maxOccurs>2, true self-loop ----

        [Test]
        public void RequiredRepeatingList_MaxOccurs3_RequiredTail_SelfLoops()
        {
            // maxOccurs>2 can no longer be rejected: WPT_LF_TransmitterDataType needs a true self-loop
            // (TxSpecData, minOccurs=2/maxOccurs=255, -> TxPackageSpecData?). This case (required tail)
            // mirrors AuthorizationSetupResType's shape but past the old maxOccurs=2 unroll limit.
            const string xsd = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns="urn:test:rt2" targetNamespace="urn:test:rt2">
              <xs:element name="root" type="T"/>
              <xs:complexType name="T">
                <xs:sequence>
                  <xs:element name="Services" type="xs:unsignedByte" maxOccurs="3"/>
                  <xs:element name="Flag" type="xs:boolean"/>
                </xs:sequence>
              </xs:complexType>
            </xs:schema>
            """;
            var r = GeneratorHarness.Run(("rt2.xsd", xsd));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            // First item: unconditional 1-bit SE, same as the maxOccurs=2 shape.
            Assert.That(src, Does.Contain("w.WriteBits(0, 1);   // SE(Services)"));
            // True self-loop (a for-loop over the rest, not an unrolled per-position state): {loop=0, Flag=1}.
            Assert.That(src, Does.Contain("for (int ci = 1; ci <"));
            Assert.That(src, Does.Contain("w.WriteBits(0, 2);   // Services (loop)"));

            var errors = GeneratorHarness.CompileErrors(r, typeof(cloud.charging.open.protocols.ISO15118.EXI.ExiPrimitives));
            Assert.That(errors, Is.Empty, src + "\n\n" + string.Join("\n", errors.Select(e => e.ToString())));
        }

        [Test]
        public void RequiredRepeatingList_LargeMaxOccurs_OptionalTail_SelfLoopsWithEEAlternative()
        {
            // WPT_LF_TransmitterDataType's actual shape: TxSpecData (min=2,max=255) -> TxPackageSpecData?
            // (optional tail, the sequence's last particle). No working cbV2G reference exists for this —
            // cbexigen's own generated encoder for this exact type fails at runtime with
            // EXI_ERROR__UNKNOWN_EVENT_CODE even at minOccurs=2 (verified empirically against a standalone
            // build of libcbv2g) — so this is an independent, spec-following design: every loop iteration
            // offers [loop, tail-start, element EE].
            const string xsd = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns="urn:test:rt3" targetNamespace="urn:test:rt3">
              <xs:element name="root" type="T"/>
              <xs:complexType name="T">
                <xs:sequence>
                  <xs:element name="Items" type="xs:unsignedByte" minOccurs="2" maxOccurs="255"/>
                  <xs:element name="Tail" type="xs:unsignedByte" minOccurs="0"/>
                </xs:sequence>
              </xs:complexType>
            </xs:schema>
            """;
            var r = GeneratorHarness.Run(("rt3.xsd", xsd));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            Assert.That(src, Does.Contain("w.WriteBits(0, 1);   // SE(Items) first"));
            // Loop state offers 3 productions (loop=0, Tail=1, EE=2) -> 2 bits.
            Assert.That(src, Does.Contain("w.WriteBits(0, 2);   // Items (loop)"));
            Assert.That(src, Does.Contain("w.WriteBits(1, 2);   // Tail"));
            Assert.That(src, Does.Contain("w.WriteBits(2, 2);   // element EE"));
            // Choosing the tail still needs the outer element's own closing EE afterwards.
            Assert.That(src, Does.Contain("w.WriteBits(0, 1);   // element EE"));

            var errors = GeneratorHarness.CompileErrors(r, typeof(cloud.charging.open.protocols.ISO15118.EXI.ExiPrimitives));
            Assert.That(errors, Is.Empty, src + "\n\n" + string.Join("\n", errors.Select(e => e.ToString())));
        }

        // ---- construct #17 (ISO 15118-20 WPT): optional bounded-repeating list mid-run, capped at 2 ----

        [Test]
        public void OptionalBoundedList_MidRun_CappedAtTwoItems()
        {
            // WPT_FinePositioningReqType's actual shape: VendorSpecificDataContainer{0,16} ->
            // WPT_LF_DataPackageList? (both optional, list not last). cbV2G's own generated grammar for
            // this (iso20_WPT_Encoder.c states 178-180) hard-caps the list at 2 items regardless of its
            // schema maxOccurs (16) and makes the suffix particle unreachable unless >=1 list item was
            // written first — both verified byte-for-byte against that generated C source.
            const string xsd = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns="urn:test:ml1" targetNamespace="urn:test:ml1">
              <xs:element name="root" type="T"/>
              <xs:complexType name="T">
                <xs:sequence>
                  <xs:element name="Items" type="xs:unsignedByte" minOccurs="0" maxOccurs="16"/>
                  <xs:element name="Tail" type="xs:unsignedByte" minOccurs="0"/>
                </xs:sequence>
              </xs:complexType>
            </xs:schema>
            """;
            var r = GeneratorHarness.Run(("ml1.xsd", xsd));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            Assert.That(src, Does.Contain("cbV2G's grammar for this position caps this list at 2 items"));
            // State 0 (no items yet): {start item 0 = 0, EE = 1}, 2 bits — Tail is unreachable here.
            Assert.That(src, Does.Contain("w.WriteBits(0, 2);   // Items"));
            Assert.That(src, Does.Contain("w.WriteBits(1, 2);   // element EE"));
            // State 1 (1 item written): {loop=0, Tail=1, EE=2}.
            Assert.That(src, Does.Contain("w.WriteBits(1, 2);   // Tail"));

            var errors = GeneratorHarness.CompileErrors(r, typeof(cloud.charging.open.protocols.ISO15118.EXI.ExiPrimitives));
            Assert.That(errors, Is.Empty, src + "\n\n" + string.Join("\n", errors.Select(e => e.ToString())));
        }

        // ---- construct #18 (ISO 15118-20 ACDP): two global elements sharing the same named type ----

        [Test]
        public void TwoGlobalElementsSharingATypeAreGroupedInTheDocumentGrammar()
        {
            // ACDP_DisconnectReq/Res deliberately reuse ACDP_ConnectReq/ResType. cbV2G's document
            // grammar groups elements that share a type immediately after the alphabetically-first
            // element of that type — NOT plain alphabetical-by-element-name, which would put "BReq"
            // (a distinct type) between "AReq" and "ARes" here. Verified against
            // encode_iso20_acdp_exiDocument: ConnectReq=0, DisconnectReq=1, ConnectRes=2, DisconnectRes=3.
            const string xsd = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns="urn:test:st1" targetNamespace="urn:test:st1">
              <xs:element name="AReq" type="TReq"/>
              <xs:element name="ARes" type="TRes"/>
              <xs:element name="BReq" type="TReq"/>
              <xs:element name="BRes" type="TRes"/>
              <xs:complexType name="TReq">
                <xs:sequence>
                  <xs:element name="Value" type="xs:unsignedByte"/>
                </xs:sequence>
              </xs:complexType>
              <xs:complexType name="TRes">
                <xs:sequence>
                  <xs:element name="Value" type="xs:unsignedByte"/>
                </xs:sequence>
              </xs:complexType>
            </xs:schema>
            """;
            var r = GeneratorHarness.Run(("st1.xsd", xsd));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            // DecodeAny's switch literally encodes the assigned document index per element.
            Assert.That(src, Does.Contain("0u => Decode_AReq(ref r),"));
            Assert.That(src, Does.Contain("1u => Decode_BReq(ref r),"));
            Assert.That(src, Does.Contain("2u => Decode_ARes(ref r),"));
            Assert.That(src, Does.Contain("3u => Decode_BRes(ref r),"));

            var errors = GeneratorHarness.CompileErrors(r, typeof(cloud.charging.open.protocols.ISO15118.EXI.ExiPrimitives));
            Assert.That(errors, Is.Empty, src + "\n\n" + string.Join("\n", errors.Select(e => e.ToString())));
        }

        // ---- construct #15 (ISO 15118-20): transitive substitution groups + concrete (non-abstract-
        // element) heads whose members can extend EACH OTHER ----

        [Test]
        public void TransitiveSubstitutionGroup_ConcreteHeadElement_MostDerivedFirst()
        {
            // Mirrors DC's CLReqControlMode <- Scheduled_DC_CLReqControlMode <-
            // BPT_Scheduled_DC_CLReqControlMode chain: Head's ELEMENT is not abstract="true" (only its
            // TYPE is), and Gamma substitutes for Beta (a substitution-group member, not the root head) —
            // both members and their inheritance chain must be discovered transitively.
            const string xsd = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns="urn:test:ts1" targetNamespace="urn:test:ts1">
              <xs:element name="root" type="RootType"/>
              <xs:complexType name="RootType">
                <xs:sequence>
                  <xs:element ref="Head"/>
                </xs:sequence>
              </xs:complexType>
              <xs:element name="Head" type="HeadType"/>
              <xs:complexType name="HeadType" abstract="true"/>
              <xs:element name="Alpha" type="AlphaType" substitutionGroup="Head"/>
              <xs:complexType name="AlphaType">
                <xs:complexContent><xs:extension base="HeadType">
                  <xs:sequence><xs:element name="X" type="xs:unsignedInt"/></xs:sequence>
                </xs:extension></xs:complexContent>
              </xs:complexType>
              <xs:element name="Beta" type="BetaType" substitutionGroup="Head"/>
              <xs:complexType name="BetaType">
                <xs:complexContent><xs:extension base="HeadType">
                  <xs:sequence><xs:element name="Y" type="xs:unsignedInt"/></xs:sequence>
                </xs:extension></xs:complexContent>
              </xs:complexType>
              <xs:element name="Gamma" type="GammaType" substitutionGroup="Beta"/>
              <xs:complexType name="GammaType">
                <xs:complexContent><xs:extension base="BetaType">
                  <xs:sequence><xs:element name="Z" type="xs:unsignedInt"/></xs:sequence>
                </xs:extension></xs:complexContent>
              </xs:complexType>
            </xs:schema>
            """;
            var r = GeneratorHarness.Run(("ts1.xsd", xsd));
            Assert.That(r.Diagnostics, Is.Empty, r.GeneratedSource);
            var src = r.GeneratedSource;

            // Alphabetical wire order: Alpha=0, Beta=1, Gamma=2, Head(abstract, skipped)=3. 4 productions
            // (incl. the abstract head's reserved slot) -> ceil(log2(4+1)) = 3 bits.
            Assert.That(src, Does.Contain("w.WriteBits(0, 3);   // Alpha"));
            Assert.That(src, Does.Contain("w.WriteBits(1, 3);   // Beta"));
            Assert.That(src, Does.Contain("w.WriteBits(2, 3);   // Gamma"));
            Assert.That(src, Does.Not.Contain("// Head"));   // abstract head never gets a runtime case

            // Gamma extends Beta — its case must come BEFORE Beta's, or Beta's `case BetaType v` would
            // shadow it (CS8120: a base-type pattern also matches derived instances).
            int iGamma = src.IndexOf("case GammaType v:", System.StringComparison.Ordinal);
            int iBeta = src.IndexOf("case BetaType v:", System.StringComparison.Ordinal);
            Assert.That(iGamma, Is.GreaterThan(-1));
            Assert.That(iBeta, Is.GreaterThan(-1));
            Assert.That(iGamma, Is.LessThan(iBeta));

            var errors = GeneratorHarness.CompileErrors(r, typeof(cloud.charging.open.protocols.ISO15118.EXI.ExiPrimitives));
            Assert.That(errors, Is.Empty, src + "\n\n" + string.Join("\n", errors.Select(e => e.ToString())));
        }

        // ---- fail-loud: an unknown construct must still raise a diagnostic ----

        [Test]
        public void UnknownConstruct_RaisesDiagnostic()
        {
            const string withAll = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                       xmlns="urn:test:c" targetNamespace="urn:test:c">
              <xs:element name="root" type="RootType"/>
              <xs:complexType name="RootType">
                <xs:all>
                  <xs:element name="A" type="xs:unsignedInt"/>
                  <xs:element name="B" type="xs:unsignedInt"/>
                </xs:all>
              </xs:complexType>
            </xs:schema>
            """;
            var r = GeneratorHarness.Run(("c.xsd", withAll));
            // xs:all is not implemented — the generator must fail loud, not silently skip.
            Assert.That(r.Diagnostics, Is.Not.Empty);
        }
    }
}
