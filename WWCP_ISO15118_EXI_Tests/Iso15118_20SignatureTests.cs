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

using System.Security.Cryptography;

using NUnit.Framework;

using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

using cloud.charging.open.protocols.ISO15118_20.CommonMessages;
using cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>
    /// ISO 15118-20 CommonMessages XMLDSig signing/verification: SHA-512 over an element's EXI fragment
    /// feeds a SignedInfo Reference; the SignedInfo fragment is ECDSA-P521 (secp521r1) signed, with the
    /// SignatureValue as raw r‖s (132 bytes). The fragment octets themselves are pinned to cbV2G in
    /// <see cref="Iso15118_20FragmentTests"/>; these exercise the crypto on top.
    /// </summary>
    [TestFixture]
    public class Iso15118_20SignatureTests
    {
        private static MessageHeaderType Header() =>
            new(SessionID: new byte[8], TimeStamp: 1_700_000_000UL, Signature: null);

        // The generator gives the global element "MeteringConfirmationReq" its own record (used for
        // document TryEncode/DecodeAny), distinct from the structurally-identical MeteringConfirmationReqType
        // (used for the fragment codec) — the two are unrelated C# types, so both are built from this
        // one shared SignedMeteringData.
        private static SignedMeteringDataType SignedMeteringData() =>
            new(Id: "ID1", SessionID: new byte[8],
                MeterInfo: new MeterInfoType(
                    MeterID: "M1", ChargedEnergyReadingWh: 5000,
                    BPT_DischargedEnergyReadingWh: null, CapacitiveEnergyReadingVARh: null,
                    BPT_InductiveEnergyReadingVARh: null, MeterSignature: null,
                    MeterStatus: null, MeterTimestamp: null),
                Receipt: null,
                Dynamic_SMDTControlMode: null,
                Scheduled_SMDTControlMode: new Scheduled_SMDTControlModeType(SelectedScheduleTupleID: 1));

        private static MeteringConfirmationReqType SignableBody() => new(Header(), SignedMeteringData());

        private static ReferenceType SignedElementReference(out byte[] fragment)
        {
            var content = SignableBody();
            var buf = new byte[512];
            Assert.That(CommonMessagesCodec.EncodeFragment_MeteringConfirmationReq(content, buf, out int n), Is.True);
            fragment = buf.AsSpan(0, n).ToArray();
            var digest = V2GSignature.Digest(fragment);
            return new ReferenceType(Id: null, Type: null, URI: "#ID1", Transforms: null,
                DigestMethod: new DigestMethodType(Algorithm: V2GSignature.Sha512, ANY: null),
                DigestValue: digest);
        }

        [Test]
        public void SignThenVerify_RoundTrips()
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP521);
            var signedInfo = V2GSignature.BuildSignedInfo("ID1", SignedElementReference(out _).DigestValue);

            var signatureValue = V2GSignature.Sign(signedInfo, key);

            Assert.That(signatureValue.Length, Is.EqualTo(132), "P-521 r‖s is 66+66 bytes");
            Assert.That(V2GSignature.Verify(signedInfo, signatureValue, key), Is.True);
        }

        [Test]
        public void Verify_FailsForTamperedSignature()
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP521);
            var signedInfo = V2GSignature.BuildSignedInfo("ID1", SignedElementReference(out _).DigestValue);
            var signatureValue = V2GSignature.Sign(signedInfo, key);

            signatureValue[0] ^= 0xFF;   // flip a byte of r

            Assert.That(V2GSignature.Verify(signedInfo, signatureValue, key), Is.False);
        }

        [Test]
        public void Verify_FailsForWrongKey()
        {
            using var signer   = ECDsa.Create(ECCurve.NamedCurves.nistP521);
            using var attacker = ECDsa.Create(ECCurve.NamedCurves.nistP521);
            var signedInfo = V2GSignature.BuildSignedInfo("ID1", SignedElementReference(out _).DigestValue);
            var signatureValue = V2GSignature.Sign(signedInfo, signer);

            Assert.That(V2GSignature.Verify(signedInfo, signatureValue, attacker), Is.False);
        }

        [Test]
        public void VerifyReference_MatchesSignedElementDigest()
        {
            var reference = SignedElementReference(out var fragment);

            Assert.That(V2GSignature.VerifyReference(reference, fragment), Is.True);

            var tampered = (byte[])fragment.Clone();
            tampered[^1] ^= 0x01;
            Assert.That(V2GSignature.VerifyReference(reference, tampered), Is.False);
        }

        [Test]
        public void EndToEnd_SignEncodeDecodeVerify()
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP521);

            // 1. Sign the MeteringConfirmationReq body (digest its fragment, sign the SignedInfo).
            var body = SignableBody();
            var fragBuf = new byte[512];
            Assert.That(CommonMessagesCodec.EncodeFragment_MeteringConfirmationReq(body, fragBuf, out int fn), Is.True);
            var bodyFragment = fragBuf.AsSpan(0, fn).ToArray();

            var signedInfo = V2GSignature.BuildSignedInfo("ID1", V2GSignature.Digest(bodyFragment));
            var signatureValue = V2GSignature.Sign(signedInfo, key);

            // 2. Assemble the signed message — the document-level MeteringConfirmationReq (not the
            //    fragment-only MeteringConfirmationReqType), header carrying the signature — and encode it.
            var message = new MeteringConfirmationReq(
                Header() with { Signature = V2GSignature.BuildSignature(signedInfo, signatureValue) },
                SignedMeteringData());
            var buf = new byte[1024];
            Assert.That(message.TryEncode(buf, out int n), Is.True);

            // 3. Decode it and verify the signature end to end.
            var decoded = (MeteringConfirmationReq)CommonMessagesCodec.DecodeAny(buf.AsSpan(0, n), out _);
            var sig = decoded.Header.Signature;
            Assert.That(sig, Is.Not.Null);

            Assert.That(V2GSignature.Verify(sig!.SignedInfo, sig.SignatureValue.Value, key), Is.True,
                "ECDSA over the decoded SignedInfo must verify");

            // Verification reconstructs the pre-signature fragment: -20 folds the header (including the
            // Signature slot) into the signed element itself, so the receiver must zero that slot back out
            // before re-fragment-encoding — exactly what the sender digested before it had a signature to put
            // there. (-2 avoids this by keeping header and signable body as separate types altogether.)
            var decodedForFragment = new MeteringConfirmationReqType(
                decoded.Header with { Signature = null }, decoded.SignedMeteringData);
            Assert.That(CommonMessagesCodec.EncodeFragment_MeteringConfirmationReq(decodedForFragment, fragBuf, out int fn2), Is.True);
            Assert.That(V2GSignature.VerifyReference(sig.SignedInfo.Reference[0], fragBuf.AsSpan(0, fn2)), Is.True,
                "the reference digest must match the signed element");
        }

        // ── Ed448 (RFC 8032) — the -20 signature suite's other option, via BouncyCastle ──────────
        //
        // The three below are self-referential by construction: sign-then-verify, tampered
        // signature, wrong key. They say the implementation agrees with itself and nothing more.
        // Ed448RfcVectorTests is what holds it to the standard — RFC 8032 §7.4's published
        // signatures, byte for byte, which is possible here and not for P-521 because Ed448 is
        // deterministic. Keep both: these cover the failure paths, those cover the happy path
        // against an oracle.

        [Test]
        public void Ed448_SignThenVerify_RoundTrips()
        {
            var key = new Ed448PrivateKeyParameters(new SecureRandom());
            var signedInfo = V2GSignature.BuildSignedInfo(
                "ID1", SignedElementReference(out _).DigestValue, V2GSignature.EddsaEd448);

            var signatureValue = V2GSignature.SignEd448(signedInfo, key);

            Assert.That(signatureValue.Length, Is.EqualTo(114), "Ed448 signatures are always 114 bytes");
            Assert.That(V2GSignature.VerifyEd448(signedInfo, signatureValue, key.GeneratePublicKey()), Is.True);
        }

        [Test]
        public void Ed448_Verify_FailsForTamperedSignature()
        {
            var key = new Ed448PrivateKeyParameters(new SecureRandom());
            var signedInfo = V2GSignature.BuildSignedInfo(
                "ID1", SignedElementReference(out _).DigestValue, V2GSignature.EddsaEd448);
            var signatureValue = V2GSignature.SignEd448(signedInfo, key);

            signatureValue[0] ^= 0xFF;

            Assert.That(V2GSignature.VerifyEd448(signedInfo, signatureValue, key.GeneratePublicKey()), Is.False);
        }

        [Test]
        public void Ed448_Verify_FailsForWrongKey()
        {
            var signer   = new Ed448PrivateKeyParameters(new SecureRandom());
            var attacker = new Ed448PrivateKeyParameters(new SecureRandom());
            var signedInfo = V2GSignature.BuildSignedInfo(
                "ID1", SignedElementReference(out _).DigestValue, V2GSignature.EddsaEd448);
            var signatureValue = V2GSignature.SignEd448(signedInfo, signer);

            Assert.That(V2GSignature.VerifyEd448(signedInfo, signatureValue, attacker.GeneratePublicKey()), Is.False);
        }
    }
}
