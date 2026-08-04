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

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using NUnit.Framework;

using cloud.charging.open.protocols.ISO15118.EXI.Tests.Infrastructure;
using cloud.charging.open.protocols.ISO15118_20.CommonMessages;
using cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests.Interop
{
    /// <summary>
    /// Root-causes the Plug &amp; Charge <c>SignedInfo</c> signature-verification finding from the live -20 PnC
    /// interop run (Josev EVCC → our SECC, <c>docs/interop-runs/2026-07-21-iso20-dc-pnc-tls/</c>). The earlier
    /// "Josev signs a non-reproducible <c>SignedInfo</c> form" conclusion is now <b>refuted</b>: Josev's exact
    /// signing octets were reproduced with Josev's own <c>EXICodec.jar</c> and its captured signature verifies
    /// against them.
    /// <para>Two independent facts pin the divergence down (both asserted below):</para>
    /// <list type="bullet">
    ///   <item><b>Reference digest is byte-exact.</b> SHA-256 of our re-encoded <c>PnC_AReqAuthorizationMode</c>
    ///     fragment equals Josev's <c>DigestValue</c> byte-for-byte — the signed <i>element</i> is encoded the
    ///     same on both sides (both use the CommonMessages grammar for it).</item>
    ///   <item><b>The <c>SignedInfo</c> grammar differs.</b> Josev encodes the <c>SignedInfo</c> under the
    ///     <b>standalone <c>xmldsig-core-schema.xsd</c> grammar</b> (its <c>BuiltInSchema.XSDCore</c> /
    ///     <c>XMLDSIG_Core_Schema_Grammar</c>, selected because <c>to_exi(signed_info, Namespace.XML_DSIG)</c>
    ///     passes the xmldsig namespace), whereas our codec — like cbV2G, our authoritative reference — encodes
    ///     the <c>SignedInfo</c> as a fragment of the full <c>V2G_CI_CommonMessages</c> schema set (which
    ///     <c>&lt;xs:import&gt;</c>s xmldsig alongside dozens of V2G global elements).</item>
    /// </list>
    /// Because the EXI <i>Fragment</i> grammar's leading element event-code width is set by the number of global
    /// elements in the loaded schema, the standalone-xmldsig grammar gives <c>SignedInfo</c> a one-bit-narrower
    /// top-level code; that shifts the whole bitstream, so Josev's form is <b>209 bytes</b> and bit-different
    /// from byte 1 onward vs. our/cbV2G <b>210-byte</b> form, even though both decode to the identical
    /// <c>SignedInfo</c>. This is a Josev-specific grammar choice, not a defect in our byte-exact
    /// (cbV2G-matched) codec — which per the project ground rule stays as-is. The 209-byte reference below was
    /// produced by Josev's own codec:
    /// <c>docker run iso15118-secc /venv/bin/python</c> →
    /// <c>EXI().to_exi(SignedInfo(...), Namespace.XML_DSIG)</c>.
    /// </summary>
    [TestFixture]
    public class JosevPnCSignatureDiag
    {
        /// <summary>
        /// Josev's exact <c>SignedInfo</c> signing octets for the captured PnC <c>AuthorizationReq</c>, produced
        /// by Josev's own <c>EXICodec.jar</c> over the standalone <c>XMLDSIG_Core_Schema_Grammar</c>
        /// (xmldsig-core-schema.xsd alone). Josev's captured 64-byte ECDSA-P256 signature verifies (SHA-256)
        /// against these 209 bytes — see <see cref="JosevSignsSignedInfoOverStandaloneXmldsigGrammar"/>.
        /// </summary>
        internal const string JosevStandaloneXmldsigSignedInfoHex =
            "808112b43a3a381d1797bbbbbb973b999737b93397aa2917b1b0b737b734b1b0b616b2bc3497a1ab43a3a381d17" +
            "97bbbbbb973b999737b933979918181897981a17bc36b63239b4b396b6b7b93291b2b1b239b096b9b430991a9b2" +
            "20623696431025687474703a2f2f7777772e77332e6f72672f54522f63616e6f6e6963616c2d6578692f4852d0e" +
            "8e8e0745e5eeeeeee5cee665cdee4ce5e646060625e60685ef0dad8cadcc646e6d0c2646a6c841736754d94353f2" +
            "86ff4e3565175269c61b7e98a60150b97ee93d16414d6e8e1a370";

        /// <summary>
        /// The signed <b>element</b> is encoded identically on both sides: SHA-256 of our re-encoded
        /// <c>PnC_AReqAuthorizationMode</c> fragment equals Josev's <c>DigestValue</c> byte-for-byte. And our
        /// (cbV2G-matched) CommonMessages-grammar <c>SignedInfo</c> fragment does <b>not</b> verify Josev's
        /// signature — because Josev signs the <c>SignedInfo</c> over a different grammar (see the class summary
        /// and <see cref="JosevSignsSignedInfoOverStandaloneXmldsigGrammar"/>).
        /// </summary>
        [Test]
        public void ReferenceDigestIsByteExact_ButCommonMessagesGrammarDoesNotVerifyJosevSignedInfo()
        {
            var bytes = HexUtil.Parse(JosevCapturedFrames20Tests.SignedAuthorizationReqHex);
            var req = (AuthorizationReq)CommonMessagesCodec.DecodeAny(bytes, out _);
            var pnc = req.PnC_AReqAuthorizationMode!;
            var sig = req.Header.Signature!;
            var reference = sig.SignedInfo.Reference[0];
            var hashName = sig.SignedInfo.SignatureMethod.Algorithm.Contains("sha256") ? HashAlgorithmName.SHA256 : HashAlgorithmName.SHA512;

            // 1. Reference digest over our re-encoded signed-element fragment matches Josev's byte-for-byte —
            //    proving our fragment codec is byte-exact (this is the strong conformance result).
            var frag = new byte[8192];
            Assert.That(CommonMessagesCodec.EncodeFragment_PnC_AReqAuthorizationMode(pnc, frag, out int fn), Is.True);
            var digest = reference.DigestMethod.Algorithm.Contains("sha256") ? SHA256.HashData(frag.AsSpan(0, fn)) : SHA512.HashData(frag.AsSpan(0, fn));
            Assert.That(digest.AsSpan().SequenceEqual(reference.DigestValue), Is.True,
                "reference digest must match — our signed-element fragment codec is byte-exact vs Josev/EXIficient");

            // Crypto is well-formed: P-256 contract leaf, 64-byte r‖s signature.
            using var contract = X509CertificateLoader.LoadCertificate(pnc.ContractCertificateChain.Certificate);
            Assert.That(contract.GetECDsaPublicKey()!.KeySize, Is.EqualTo(256));
            Assert.That(sig.SignatureValue.Value.Length, Is.EqualTo(64));

            // 2. Our (cbV2G-matched) CommonMessages-grammar SignedInfo fragment does NOT verify Josev's
            //    signature — Josev signs the SignedInfo over the standalone xmldsig grammar instead.
            using var ecdsa = contract.GetECDsaPublicKey()!;
            bool sigOk = ecdsa.VerifyData(V2GSignature.SignedInfoFragment(sig.SignedInfo), sig.SignatureValue.Value,
                hashName, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            Assert.That(sigOk, Is.False, "our combined-schema grammar differs from Josev's standalone-xmldsig grammar");
        }

        /// <summary>
        /// Root cause, reproduced: Josev's captured ECDSA-P256 signature verifies (SHA-256) against the
        /// <c>SignedInfo</c> octets Josev's own codec emits over the <b>standalone xmldsig grammar</b>
        /// (<see cref="JosevStandaloneXmldsigSignedInfoHex"/>, 209 B). Also asserts the structural relationship
        /// to our 210-byte CommonMessages-grammar form: same length class, but a whole-stream bit-shift from a
        /// one-bit-narrower top-level element event code (differs from byte 1 onward).
        /// </summary>
        [Test]
        public void JosevSignsSignedInfoOverStandaloneXmldsigGrammar()
        {
            var bytes = HexUtil.Parse(JosevCapturedFrames20Tests.SignedAuthorizationReqHex);
            var req = (AuthorizationReq)CommonMessagesCodec.DecodeAny(bytes, out _);
            var sig = req.Header.Signature!;
            using var contract = X509CertificateLoader.LoadCertificate(req.PnC_AReqAuthorizationMode!.ContractCertificateChain.Certificate);
            using var ecdsa = contract.GetECDsaPublicKey()!;

            var josev = Convert.FromHexString(JosevStandaloneXmldsigSignedInfoHex);
            bool ok = ecdsa.VerifyData(josev, sig.SignatureValue.Value, HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            Assert.That(ok, Is.True,
                "Josev signs the SignedInfo over the standalone XMLDSIG_Core_Schema_Grammar (xmldsig-core-schema.xsd alone)");
            Assert.That(josev.Length, Is.EqualTo(209));

            // Structural relationship to our (cbV2G-matched) CommonMessages-grammar form: same content, whole
            // bitstream shifted by the differing top-level element event-code width → 210 B, diff from byte 1 on.
            var ours = V2GSignature.SignedInfoFragment(sig.SignedInfo);
            Assert.That(ours.Length, Is.EqualTo(210));
            Assert.That(ours[0], Is.EqualTo(josev[0]));                 // EXI header byte identical
            Assert.That(ours[1], Is.Not.EqualTo(josev[1]));             // diverges at the first content bits
        }
    }
}
