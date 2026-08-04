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

using AcNs  = cloud.charging.open.protocols.ISO15118_20.AC.Generated;
using IecNs = cloud.charging.open.protocols.ISO15118_20.AC_DER_IEC.Generated;
using SaeNs = cloud.charging.open.protocols.ISO15118_20.AC_DER_SAE.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>
    /// ISO 15118-20 <b>Amendment 1 AC DER</b> (distributed energy resources) coverage.
    /// <para>
    /// AC DER is <b>not a new message set</b>. <c>V2G_CI_AC_DER_{IEC,SAE}.xsd</c> import the base AC
    /// schema, leave the message roots commented out, and contribute six substitution-group members
    /// (<c>DER_*</c> energy-transfer and Scheduled/Dynamic control modes) that extend AC's own types
    /// via <c>xs:extension</c> — structurally the same pattern AC already uses for its <c>BPT_*</c>
    /// variants, which is why the generator needed no changes. The messages on the wire stay
    /// <c>AC_ChargeParameterDiscoveryReq/Res</c> and <c>AC_ChargeLoopReq/Res</c>.
    /// </para>
    /// <para>
    /// Because the generator inlines substitution-group members as grammar productions, adding the
    /// DER members changes the grammar of those AC messages — so the AC_DER_* assemblies are separate
    /// from the plain AC assembly by design. <see cref="Iec_PlainAcMessage_EncodesDifferentlyUnderTheDerGrammar"/>
    /// pins that consequence explicitly rather than leaving it as an assumption.
    /// </para>
    /// <para>
    /// <b>The byte oracle is partial.</b> cbexigen does not generate the amendment schemas, so
    /// nothing that uses a DER member can be checked against a reference encoder. What can be, and
    /// now is, are the plain-AC messages the DER grammar happens to encode identically: six of the
    /// ten AC vectors, carried into <c>Vectors/Iso15118_20.AC_DER_{IEC,SAE}.vectors.json</c> with
    /// cbV2G's own bytes. The other four shift (see
    /// <see cref="PlainAcMessage_IsByteIdenticalUnderBothGrammars"/>), and everything DER is this
    /// project's own output. EXIficient is schema-generic and remains the candidate spec oracle for
    /// the rest; see <c>docs/roadmap.md</c>.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Iso15118_20AcDerTests
    {
        private static IecNs.MessageHeaderType IecHeader() =>
            new(SessionID: new byte[8], TimeStamp: 1_700_000_000UL, Signature: null);

        private static SaeNs.MessageHeaderType SaeHeader() =>
            new(SessionID: new byte[8], TimeStamp: 1_700_000_000UL, Signature: null);

        private static AcNs.MessageHeaderType AcHeader() =>
            new(SessionID: new byte[8], TimeStamp: 1_700_000_000UL, Signature: null);

        /// <summary>A plain (non-DER) energy transfer mode, expressed in each assembly's own types.</summary>
        private static IecNs.AC_CPDReqEnergyTransferModeType IecPlainMode() =>
            new(new IecNs.RationalNumberType(0, 11000), null, null,
                new IecNs.RationalNumberType(0, 100),   null, null);

        private static SaeNs.AC_CPDReqEnergyTransferModeType SaePlainMode() =>
            new(new SaeNs.RationalNumberType(0, 11000), null, null,
                new SaeNs.RationalNumberType(0, 100),   null, null);

        private static AcNs.AC_CPDReqEnergyTransferModeType AcPlainMode() =>
            new(new AcNs.RationalNumberType(0, 11000), null, null,
                new AcNs.RationalNumberType(0, 100),   null, null);

        [Test]
        public void Iec_DerEnergyTransferMode_Roundtrips()
        {
            // The DER substitution member in place of the plain AC one: it inherits AC's charge-power
            // fields and adds the DER-only discharge powers, session discharge energy and reactive
            // power limits.
            var message = new IecNs.AC_ChargeParameterDiscoveryReq(
                IecHeader(),
                new IecNs.DER_AC_CPDReqEnergyTransferModeType(
                    EVMaximumChargePower:                   new IecNs.RationalNumberType(0, 11000),
                    EVMaximumChargePower_L2:                null,
                    EVMaximumChargePower_L3:                null,
                    EVMinimumChargePower:                   new IecNs.RationalNumberType(0, 100),
                    EVMinimumChargePower_L2:                null,
                    EVMinimumChargePower_L3:                null,
                    EVProcessing:                           IecNs.Processing.Finished,
                    EVMaximumDischargePower:                new IecNs.RationalNumberType(0, 5000),
                    EVMaximumDischargePower_L2:             null,
                    EVMaximumDischargePower_L3:             null,
                    EVMinimumDischargePower:                new IecNs.RationalNumberType(0, 50),
                    EVMinimumDischargePower_L2:             null,
                    EVMinimumDischargePower_L3:             null,
                    EVSessionTotalDischargeEnergyAvailable: new IecNs.RationalNumberType(0, 20000),
                    EVReactivePowerLimits:                  null));

            var buf1 = new byte[1024];
            Assert.That(IecNs.AcDerIecCodec.TryEncode(message, buf1, out int n1), Is.True, "encode failed");

            // Printed so the exact bytes fed to the EXIficient cross-check can be regenerated from
            // this test (see tools/exificient-ref/fixtures/iso20-ac-der-iec-cpdreq.hex).
            TestContext.Out.WriteLine($"AC+DER CPDReq: {Convert.ToHexString(buf1.AsSpan(0, n1))}");

            var decoded = (IecNs.AC_ChargeParameterDiscoveryReq)IecNs.AcDerIecCodec.DecodeAny(buf1.AsSpan(0, n1), out int consumed);
            Assert.That(consumed, Is.EqualTo(n1), "decoder did not consume all encoded bytes");

            // Compare re-encoded bytes rather than the records: the generated records carry byte[]
            // members (SessionID), and record equality is reference equality for arrays.
            var buf2 = new byte[1024];
            Assert.That(IecNs.AcDerIecCodec.TryEncode(decoded, buf2, out int n2), Is.True, "re-encode failed");

            Assert.Multiple(() =>
            {
                Assert.That(buf2.AsSpan(0, n2).ToArray(), Is.EqualTo(buf1.AsSpan(0, n1).ToArray()),
                            "decode∘encode is not the identity on the wire");
                Assert.That(decoded.AC_CPDReqEnergyTransferMode,
                            Is.InstanceOf<IecNs.DER_AC_CPDReqEnergyTransferModeType>(),
                            "the DER substitution member did not survive the roundtrip as a DER type");
            });
        }

        [Test]
        public void Iec_PlainAcEnergyTransferMode_StillRoundtrips_AndStaysDistinctFromDer()
        {
            // Adding members to a substitution group must not lose the ones already there, and the
            // grammar has to keep them distinguishable on decode.
            var message = new IecNs.AC_ChargeParameterDiscoveryReq(IecHeader(), IecPlainMode());

            var buf1 = new byte[1024];
            Assert.That(IecNs.AcDerIecCodec.TryEncode(message, buf1, out int n1), Is.True, "encode failed");

            var decoded = (IecNs.AC_ChargeParameterDiscoveryReq)IecNs.AcDerIecCodec.DecodeAny(buf1.AsSpan(0, n1), out int consumed);
            Assert.That(consumed, Is.EqualTo(n1), "decoder did not consume all encoded bytes");

            var buf2 = new byte[1024];
            Assert.That(IecNs.AcDerIecCodec.TryEncode(decoded, buf2, out int n2), Is.True, "re-encode failed");

            Assert.Multiple(() =>
            {
                Assert.That(buf2.AsSpan(0, n2).ToArray(), Is.EqualTo(buf1.AsSpan(0, n1).ToArray()),
                            "decode∘encode is not the identity on the wire");
                Assert.That(decoded.AC_CPDReqEnergyTransferMode,
                            Is.Not.InstanceOf<IecNs.DER_AC_CPDReqEnergyTransferModeType>(),
                            "a plain AC energy transfer mode decoded as the DER extension");
            });
        }

        [Test]
        public void Sae_PlainAcEnergyTransferMode_Roundtrips()
        {
            // SAE is the richer flavour (364 elements vs IEC's 166) and its DER_* member requires four
            // mandatory limit structures (apparent power, reactive power, excitation, inverter details,
            // ~12-24 fields each) plus IEEE 1547 categories. Constructing one is only worthwhile once
            // there is an oracle to check it against — until then this pins that the SAE grammar
            // variant generates, compiles and round-trips the messages it shares with plain AC.
            var message = new SaeNs.AC_ChargeParameterDiscoveryReq(SaeHeader(), SaePlainMode());

            var buf1 = new byte[1024];
            Assert.That(SaeNs.AcDerSaeCodec.TryEncode(message, buf1, out int n1), Is.True, "encode failed");

            var decoded = (SaeNs.AC_ChargeParameterDiscoveryReq)SaeNs.AcDerSaeCodec.DecodeAny(buf1.AsSpan(0, n1), out int consumed);
            Assert.That(consumed, Is.EqualTo(n1), "decoder did not consume all encoded bytes");

            var buf2 = new byte[1024];
            Assert.That(SaeNs.AcDerSaeCodec.TryEncode(decoded, buf2, out int n2), Is.True, "re-encode failed");

            Assert.That(buf2.AsSpan(0, n2).ToArray(), Is.EqualTo(buf1.AsSpan(0, n1).ToArray()),
                        "decode∘encode is not the identity on the wire");
        }

        [Test]
        public void PlainAcMessage_IsByteIdenticalUnderBothGrammars()
        {
            // MEASURED, not assumed. The same logical message — a plain, non-DER
            // AC_ChargeParameterDiscoveryReq — encoded by the plain AC codec and by the AC+DER codec
            // comes out BYTE-IDENTICAL.
            //
            // The initial expectation was the opposite (adding substitution-group members adds
            // productions, so the event code should widen). It does not happen here: the DER member is
            // appended after the existing members, and the production count at that choice point stays
            // within the same n-bit code width, so the existing members keep their codes.
            //
            // Do NOT generalise this to "plain AC traffic is unchanged under the DER grammar". It is
            // not: running all ten AC vectors through both DER codecs shows four of them shifting —
            // the ones that select Scheduled_ or Dynamic_ control modes. Those members sort AFTER
            // "DER_" alphabetically, so inserting the DER members pushes their event codes along,
            // while this message's group takes its DER member at the end and keeps every existing
            // code. The full picture is in Vectors/Iso15118_20.AC_DER_{IEC,SAE}.vectors.json, and
            // AcDerCorpusTests pins which vectors fall on which side.
            //
            // Compatibility also ends as soon as a DER member is actually used; that boundary is
            // pinned by DerMessage_IsNotDecodableByThePlainAcCodec.
            //
            // This is fragile in principle: a future amendment adding more members to the same group
            // could push the width over a power-of-two boundary and silently change these bytes. That
            // is exactly what this test would catch.
            var plain = new AcNs.AC_ChargeParameterDiscoveryReq(AcHeader(),   AcPlainMode());
            var der   = new IecNs.AC_ChargeParameterDiscoveryReq(IecHeader(), IecPlainMode());

            var plainBuf = new byte[1024];
            var derBuf   = new byte[1024];
            Assert.That(AcNs.AcCodec.TryEncode(plain, plainBuf, out int plainLen), Is.True, "plain AC encode failed");
            Assert.That(IecNs.AcDerIecCodec.TryEncode(der, derBuf, out int derLen), Is.True, "AC+DER encode failed");

            var plainBytes = plainBuf.AsSpan(0, plainLen).ToArray();
            var derBytes   = derBuf.AsSpan(0, derLen).ToArray();

            TestContext.Out.WriteLine($"plain AC : {Convert.ToHexString(plainBytes)}");
            TestContext.Out.WriteLine($"AC + DER : {Convert.ToHexString(derBytes)}");

            Assert.That(derBytes, Is.EqualTo(plainBytes),
                "the DER grammar no longer encodes a plain AC message identically — the substitution " +
                "group's event-code width has shifted, which breaks backward compatibility with plain " +
                "AC peers and needs a deliberate decision, not a silent change");
        }

        [Test]
        public void DerMessage_IsNotDecodableByThePlainAcCodec()
        {
            // Where the compatibility above ends: a message that actually carries a DER substitution
            // member selects a production the plain AC grammar does not have. The plain codec must not
            // silently mis-decode it — it either fails or produces something that is not a faithful
            // AC_ChargeParameterDiscoveryReq. This is the reason the variant has to be negotiated.
            var der = new IecNs.AC_ChargeParameterDiscoveryReq(
                IecHeader(),
                new IecNs.DER_AC_CPDReqEnergyTransferModeType(
                    new IecNs.RationalNumberType(0, 11000), null, null,
                    new IecNs.RationalNumberType(0, 100),   null, null,
                    IecNs.Processing.Finished,
                    new IecNs.RationalNumberType(0, 5000),  null, null,
                    new IecNs.RationalNumberType(0, 50),    null, null,
                    new IecNs.RationalNumberType(0, 20000),
                    null));

            var buf = new byte[1024];
            Assert.That(IecNs.AcDerIecCodec.TryEncode(der, buf, out int n), Is.True, "AC+DER encode failed");

            object? decoded = null;
            try
            {
                decoded = AcNs.AcCodec.DecodeAny(buf.AsSpan(0, n), out _);
            }
            catch (Exception e)
            {
                // Rejecting it outright is the ideal outcome.
                TestContext.Out.WriteLine($"plain AC codec rejected the DER message: {e.GetType().Name}");
                return;
            }

            // Either it threw (decoded stays null) or it produced something that cannot be the same
            // message; a plain AC codec has no DER type to produce.
            if (decoded is AcNs.AC_ChargeParameterDiscoveryReq roundtripped)
            {
                var reBuf = new byte[1024];
                Assert.That(AcNs.AcCodec.TryEncode(roundtripped, reBuf, out int reLen) &&
                            reBuf.AsSpan(0, reLen).SequenceEqual(buf.AsSpan(0, n)),
                            Is.False,
                            "the plain AC codec silently round-tripped a DER-carrying message — that " +
                            "would mean DER content is indistinguishable from plain AC on the wire");
            }
        }
    }
}
