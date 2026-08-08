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

using cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{

    /// <summary>
    /// <b>A repeating particle's first <c>minOccurs</c> occurrences are forced, and a forced occurrence
    /// costs one bit.</b>
    ///
    /// <para>
    /// EXI unrolls a bounded particle into one grammar state per occurrence. Below <c>minOccurs</c> the
    /// state has a single production — <c>SE(item)</c>, because ending the element there would be
    /// invalid — so its event code is one bit; at <c>minOccurs</c> and above the state also offers the
    /// end-element and the code widens to two. The generator used to give every occurrence after the
    /// first the wide code, which is right for <c>minOccurs≤1</c> and one bit too many for anything
    /// else.
    /// </para>
    ///
    /// <para>
    /// ISO 15118 has five particles with <c>minOccurs="2"</c>: <c>CurveDataPoint</c> in both DER
    /// amendments, and <c>TxSpecData</c>, <c>RxSpecData</c> and <c>PulseSequenceOrder</c> in WPT. Every
    /// one of them is in a message set no reference encoder covers — cbexigen cannot generate the DER
    /// schemas at all, and the cbV2G WPT corpus leaves <c>LF_SystemSetupData</c> absent — so the vectors
    /// that exercise them are this project's own output, checked only against itself. That is why a bug
    /// this basic survived: the corpus can only catch what something else also encoded.
    /// </para>
    ///
    /// <para>
    /// Found on 2026-08-07, when EXIficient became the first codec other than ours to read
    /// <c>AC_ChargeParameterDiscoveryRes_DER</c> and gave up in the middle of the second
    /// <c>CurveDataPoint</c>, one bit out of step. Confirmed independently of ISO's schemas by asking
    /// EXIficient to <i>encode</i> the synthetic schema below: for <c>minOccurs="2"</c> the second
    /// occurrence's start-element is one bit and the third's is two, and a list filled to
    /// <c>maxOccurs</c> ends with a one-bit end-element. See
    /// <c>docs/interop-runs/2026-08-07-exificient-iso20/</c> in the conformance repository.
    /// </para>
    ///
    /// <para>
    /// Unlike the two other findings from that run (see <see cref="GeneratorDocumentOrderTests"/> and
    /// <see cref="GeneratorParticleGrammarTests"/>) this is not a fork between two defensible readings
    /// and has no switch: no reference encoder has ever written these bytes, so there is nothing to stay
    /// byte-exact with, and the schema and the EXI specification agree.
    /// </para>
    /// </summary>
    [TestFixture]
    public class GeneratorForcedOccurrenceTests
    {

        /// <summary><c>CurveDataPointsListType</c>, minimised: a sole child, <c>minOccurs="2"</c>,
        /// <c>maxOccurs="10"</c>.</summary>
        private const string SoleListSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                   xmlns="urn:test:forced" targetNamespace="urn:test:forced">
          <xs:element name="Points" type="PointsType"/>
          <xs:complexType name="PointsType">
            <xs:sequence>
              <xs:element name="Point" type="xs:unsignedShort" minOccurs="2" maxOccurs="10"/>
            </xs:sequence>
          </xs:complexType>
        </xs:schema>
        """;

        /// <summary>The same list with the default <c>minOccurs</c>, which is the shape almost every
        /// ISO list has — and whose generated codec must not change.</summary>
        private const string SoleListSchemaMinOne = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                   xmlns="urn:test:forced1" targetNamespace="urn:test:forced1">
          <xs:element name="Points" type="PointsType"/>
          <xs:complexType name="PointsType">
            <xs:sequence>
              <xs:element name="Point" type="xs:unsignedShort" maxOccurs="10"/>
            </xs:sequence>
          </xs:complexType>
        </xs:schema>
        """;


        private static string Emit(string name, string xsd)
        {
            var result = GeneratorHarness.Run((name + ".xsd", xsd));
            Assert.That(result.Diagnostics, Is.Empty, result.GeneratedSource);
            return result.GeneratedSource;
        }


        /// <summary>
        /// The encoder narrows the start-element code for the forced prefix. Written as a width
        /// <i>expression</i> rather than an unrolled sequence because the loop bound is a runtime count
        /// and the forced bound is a schema constant.
        /// </summary>
        [Test]
        public void MinOccursTwo_ForcesTheSecondOccurrenceToOneBit()
            => Assert.That(Emit("forced", SoleListSchema),
                           Does.Contain("w.WriteBits(0, i < 2 ? 1 : 2);"),
                           "the second Point is not a choice, so it cannot cost a choice's two bits");


        /// <summary>The decoder has to read exactly what the encoder wrote, or the corpus round-trips
        /// happily while no one else can read a byte of it — which is precisely how this survived.</summary>
        [Test]
        public void MinOccursTwo_DecoderReadsTheForcedOccurrenceAtOneBit()
        {
            var src = Emit("forced", SoleListSchema);
            Assert.Multiple(() =>
            {
                Assert.That(src, Does.Contain("r.ReadBits(1);   // SE(item) first"));
                Assert.That(src, Does.Contain("r.ReadBits(1);   // SE(item): forced by minOccurs=2"));
            });
        }


        /// <summary>
        /// The default shape is untouched, character for character. Stated as its own test because the
        /// fix would otherwise be free to rewrite every generated codec in the solution, and the whole
        /// value of the vector corpus rests on those bytes not moving.
        /// </summary>
        [Test]
        public void MinOccursOne_IsUnchanged()
            => Assert.That(Emit("forced1", SoleListSchemaMinOne),
                           Does.Contain("w.WriteBits(0, i == 0 ? 1 : 2);   // SE(item): 1-bit first, 2-bit loop"));


        /// <summary>
        /// At <c>maxOccurs</c> the state has only the end-element left, so the terminator is one bit —
        /// for any maximum, not only the <c>maxOccurs=2</c> case the generator used to special-case.
        /// Same EXIficient run: a ten-item list against a <c>maxOccurs="10"</c> schema ends with a
        /// single zero bit.
        /// </summary>
        [Test]
        public void ListAtMaxOccurs_EndsWithTheOneBitEndElement()
        {
            var src = Emit("forced", SoleListSchema);
            Assert.Multiple(() =>
            {
                Assert.That(src, Does.Contain("if (list.Count >= 10) w.WriteBits(0, 1);   // element EE (list at max)"));
                Assert.That(src, Does.Contain("else w.WriteBits(1, 2);   // element EE"));
                Assert.That(src, Does.Contain("if (list.Count >= 10) { r.ReadBits(1); break; }   // element EE (list at max)"));
            });
        }


        /// <summary>Whatever the widths, the emitted codec still has to compile.</summary>
        [Test]
        public void TheGeneratedCodecCompiles()
        {
            var result = GeneratorHarness.Run(("forced.xsd", SoleListSchema));
            var errors = GeneratorHarness.CompileErrors(result, typeof(ExiPrimitives));
            Assert.That(errors, Is.Empty,
                        result.GeneratedSource + "\n\n" + string.Join("\n", errors.Select(e => e.ToString())));
        }

    }

}
