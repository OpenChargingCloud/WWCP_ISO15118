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

using System.Linq;

using NUnit.Framework;

using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Emit;
using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Grammar;
using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Xsd;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{

    /// <summary>
    /// <b>The one place where the wire format forks: how the document grammar numbers global elements.</b>
    ///
    /// <para>
    /// Every encoded message opens with a "document element selector" — an index into the schema's
    /// global elements. cbexigen and a specification-built EXI processor disagree about that index
    /// whenever two global elements share one named type: cbexigen groups them together behind the
    /// alphabetically-first element of the type, while plain lexicographic order leaves whatever sorts
    /// between them in place.
    /// </para>
    ///
    /// <para>
    /// In ISO 15118 this happens exactly once — <c>ACDP_DisconnectReq</c>/<c>Res</c> have no types of
    /// their own (ISO commented the declarations out) and reuse <c>ACDP_ConnectReqType</c>/<c>ResType</c>
    /// — so ACDP is the only message set the two modes number differently. The tests below use a
    /// synthetic schema of the same shape rather than ISO's, so they say what the rule is instead of
    /// restating one schema's output.
    /// </para>
    ///
    /// <para>
    /// Found on 2026-08-07 by round-tripping the -20 corpus through EXIficient, which read our
    /// <c>ACDP_DisconnectReq</c> as an <c>ACDP_ConnectRes</c> and ran out of bits, and read our
    /// <c>ACDP_ConnectRes</c> as an <c>ACDP_DisconnectReq</c> — decoding it cleanly, as the wrong
    /// message, which is the worse of the two outcomes because nothing reports it. The default stays
    /// cbexigen-compatible; see <see cref="DocumentElementOrder"/> for why that is a decision and not
    /// an oversight.
    /// </para>
    /// </summary>
    [TestFixture]
    public class GeneratorDocumentOrderTests
    {

        /// <summary>
        /// The ACDP shape, minimised. <c>Alpha</c> and <c>Charlie</c> share one type while <c>Bravo</c>
        /// sorts <i>between</i> them — the condition for the two orders to disagree at all, and exactly
        /// ACDP's: sorted by name, <c>ACDP_ConnectRes</c> falls between <c>ACDP_ConnectReq</c> and
        /// <c>ACDP_DisconnectReq</c>, which are the pair sharing <c>ACDP_ConnectReqType</c>.
        /// </summary>
        private const string SharedTypeSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                   xmlns="urn:test:order" targetNamespace="urn:test:order">
          <xs:element name="Alpha"   type="SharedType"/>
          <xs:element name="Bravo"   type="BravoType"/>
          <xs:element name="Charlie" type="SharedType"/>
          <xs:complexType name="SharedType">
            <xs:sequence><xs:element name="A" type="xs:unsignedInt"/></xs:sequence>
          </xs:complexType>
          <xs:complexType name="BravoType">
            <xs:sequence><xs:element name="B" type="xs:unsignedInt"/></xs:sequence>
          </xs:complexType>
        </xs:schema>
        """;

        /// <summary>The same three elements, each with a type of its own. Nothing to group.</summary>
        private const string DistinctTypeSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                   xmlns="urn:test:order" targetNamespace="urn:test:order">
          <xs:element name="Alpha"   type="AlphaType"/>
          <xs:element name="Bravo"   type="BravoType"/>
          <xs:element name="Charlie" type="CharlieType"/>
          <xs:complexType name="AlphaType">
            <xs:sequence><xs:element name="A" type="xs:unsignedInt"/></xs:sequence>
          </xs:complexType>
          <xs:complexType name="BravoType">
            <xs:sequence><xs:element name="B" type="xs:unsignedInt"/></xs:sequence>
          </xs:complexType>
          <xs:complexType name="CharlieType">
            <xs:sequence><xs:element name="C" type="xs:unsignedInt"/></xs:sequence>
          </xs:complexType>
        </xs:schema>
        """;


        private static string[] Order(string xsd, DocumentElementOrder order)
            => GrammarBuilder.OrderDocumentElements(XsdReader.ParseSet(new[] { xsd }), order)
                             .Select(x => x.Name)
                             .ToArray();


        /// <summary>
        /// The default, and what the checked-in vector corpus is: the shared-type pair is pulled
        /// together and <c>Bravo</c> is pushed past it — so <c>Charlie</c> is index 1, not 2.
        /// </summary>
        [Test]
        public void CbV2GCompatible_GroupsElementsThatShareAType()
            => Assert.That(Order(SharedTypeSchema, DocumentElementOrder.CbV2GCompatible),
                           Is.EqualTo(new[] { "Alpha", "Charlie", "Bravo" }));


        /// <summary>Plain lexicographic order, which is what EXIficient expects.</summary>
        [Test]
        public void ExiSorted_LeavesThemWhereTheNamesPutThem()
            => Assert.That(Order(SharedTypeSchema, DocumentElementOrder.ExiSorted),
                           Is.EqualTo(new[] { "Alpha", "Bravo", "Charlie" }));


        /// <summary>
        /// The fork is real: for a schema with a shared type the two modes assign different indices,
        /// and an index is a message identity. Stated as its own test because it is the whole point.
        /// </summary>
        [Test]
        public void TheTwoModesDisagree_WhenAndOnlyWhenATypeIsShared()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Order(SharedTypeSchema, DocumentElementOrder.CbV2GCompatible),
                            Is.Not.EqualTo(Order(SharedTypeSchema, DocumentElementOrder.ExiSorted)),
                            "a shared type is what makes the orders differ");

                Assert.That(Order(DistinctTypeSchema, DocumentElementOrder.CbV2GCompatible),
                            Is.EqualTo(Order(DistinctTypeSchema, DocumentElementOrder.ExiSorted)),
                            "without one they must agree — which is why every ISO set but ACDP is unaffected");
            });
        }


        /// <summary>
        /// Unset means cbexigen-compatible. If this ever flips, every checked-in vector and every
        /// cbexigen-derived peer disagrees with us at once, so it is worth failing loudly here.
        /// </summary>
        [Test]
        public void TheDefaultIsCbV2GCompatible()
            => Assert.That(default(DocumentElementOrder), Is.EqualTo(DocumentElementOrder.CbV2GCompatible));

    }


    /// <summary>
    /// <b>The second wire-format fork: an optional repeating element followed by an optional one.</b>
    ///
    /// <para>
    /// ISO 15118 has this only in WPT — <c>VendorSpecificDataContainer</c> (<c>maxOccurs="16"</c>)
    /// followed by an optional <c>WPT_LF_DataPackageList</c>. cbexigen unrolls it into three states and
    /// runs out after two items, and hides the following particle from the zero-item state; the schema
    /// describes a plain loop with the particle reachable throughout. Verified against cbV2G at
    /// <c>03350be048b3</c>, <c>encode_iso20_wpt_WPT_FinePositioningReqType</c> grammar ids 178/179/180.
    /// </para>
    ///
    /// <para>
    /// The visible consequence is one event code: with no items and no suffix, cbexigen's end-element is
    /// <b>1</b> and the schema grammar's is <b>2</b>. That is why EXIficient read our WPT frames as a
    /// start-element and reported <c>Premature EOS</c> — see
    /// <c>ISO15118ConformanceTests/docs/interop-runs/2026-08-07-exificient-iso20/</c>.
    /// </para>
    /// </summary>
    [TestFixture]
    public class GeneratorParticleGrammarTests
    {

        /// <summary>The WPT shape: an optional bounded list, then one optional element, then nothing.</summary>
        private const string MidListSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                   xmlns="urn:test:particles" targetNamespace="urn:test:particles">
          <xs:element name="Msg" type="MsgType"/>
          <xs:complexType name="MsgType">
            <xs:sequence>
              <xs:element name="Container" type="xs:base64Binary" minOccurs="0" maxOccurs="16"/>
              <xs:element name="Trailer"   type="TrailerType"     minOccurs="0"/>
            </xs:sequence>
          </xs:complexType>
          <xs:complexType name="TrailerType">
            <xs:sequence><xs:element name="T" type="xs:unsignedInt"/></xs:sequence>
          </xs:complexType>
        </xs:schema>
        """;

        private static string Emit(ParticleGrammar grammar)
        {
            var plan = GrammarBuilder.Build(XsdReader.ParseSet(new[] { MidListSchema }),
                                            System.Array.Empty<string>(),
                                            DocumentElementOrder.CbV2GCompatible, grammar);
            // Every generated file, concatenated: the assertions below name text specific enough
            // that picking one file would only add a way for this helper to break.
            return string.Concat(CSharpCodecEmitter.Instance.Emit(plan, "T", "TCodec")
                                                  .Select(f => f.Source));
        }


        /// <summary>The default reproduces cbexigen, ceiling and all.</summary>
        [Test]
        public void CbV2GCompatible_CapsTheListAndHidesTheSuffix()
        {
            var src = Emit(ParticleGrammar.CbV2GCompatible);
            Assert.Multiple(() =>
            {
                Assert.That(src, Does.Contain("Count > 2"), "cbexigen cannot represent a third item");
                Assert.That(src, Does.Contain("cannot be encoded while"),
                            "and cannot reach the suffix from the zero-item state");
            });
        }


        /// <summary>
        /// The schema's own grammar: loops to maxOccurs, suffix reachable throughout — and the empty
        /// case ends with event code 2, the one byte EXIficient was waiting for.
        /// </summary>
        [Test]
        public void SchemaConformant_LoopsToMaxOccursAndKeepsTheSuffixReachable()
        {
            var src = Emit(ParticleGrammar.SchemaConformant);
            Assert.Multiple(() =>
            {
                Assert.That(src, Does.Contain("Count > 16"), "the schema's maxOccurs, not cbexigen's two");
                Assert.That(src, Does.Contain("foreach"), "a loop, not unrolled states");
                Assert.That(src, Does.Not.Contain("cannot be encoded while"),
                            "the suffix is reachable with an empty list");
                Assert.That(src, Does.Contain("w.WriteBits(2, 2);   // element EE"),
                            "the empty case ends with code 2 — cbexigen writes 1 there");
            });
        }


        /// <summary>Unset means cbexigen-compatible, for the same reason as the document order.</summary>
        [Test]
        public void TheDefaultIsCbV2GCompatible()
            => Assert.That(default(ParticleGrammar), Is.EqualTo(ParticleGrammar.CbV2GCompatible));

    }

}
