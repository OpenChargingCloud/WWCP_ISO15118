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

using NUnit.Framework;

using cloud.charging.open.protocols.ISO15118_2;
using cloud.charging.open.protocols.ISO15118_2.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>
    /// ISO 15118-2 XMLDSig signing/verification (§7.9): SHA-256 over an element's EXI fragment feeds a
    /// SignedInfo Reference; the SignedInfo fragment is ECDSA-P256 signed, with the SignatureValue as raw
    /// r‖s. These exercise the crypto on top of the byte-exact fragment codecs; the fragment octets
    /// themselves are pinned to cbV2G in <see cref="Iso15118_2FragmentTests"/>.
    /// </summary>
    [TestFixture]
    public class V2GSignatureTests
    {
        private static ReferenceType SignedElementReference(out byte[] fragment)
        {
            // A signable AuthorizationReq (Id="ID1"), digested over its EXI fragment.
            var content = new AuthorizationReqType(Id: "ID1",
                GenChallenge: new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });
            var buf = new byte[512];
            Assert.That(Iso2Codec.EncodeFragment_AuthorizationReq(content, buf, out int n), Is.True);
            fragment = buf.AsSpan(0, n).ToArray();
            var digest = V2GSignature.Digest(fragment);
            return new ReferenceType(Id: null, Type: null, URI: "#ID1", Transforms: null,
                DigestMethod: new DigestMethodType(Algorithm: V2GSignature.Sha256, ANY: null),
                DigestValue: digest);
        }

        [Test]
        public void SignThenVerify_RoundTrips()
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var signedInfo = V2GSignature.BuildSignedInfo("ID1", SignedElementReference(out _).DigestValue);

            var signatureValue = V2GSignature.Sign(signedInfo, key);

            Assert.That(signatureValue.Length, Is.EqualTo(64), "P-256 r‖s is 32+32 bytes");
            Assert.That(V2GSignature.Verify(signedInfo, signatureValue, key), Is.True);
        }

        [Test]
        public void Verify_FailsForTamperedSignature()
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var signedInfo = V2GSignature.BuildSignedInfo("ID1", SignedElementReference(out _).DigestValue);
            var signatureValue = V2GSignature.Sign(signedInfo, key);

            signatureValue[0] ^= 0xFF;   // flip a byte of r

            Assert.That(V2GSignature.Verify(signedInfo, signatureValue, key), Is.False);
        }

        [Test]
        public void Verify_FailsForWrongKey()
        {
            using var signer   = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var attacker = ECDsa.Create(ECCurve.NamedCurves.nistP256);
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
        public void SignedInfoDigest_IsStableForSameContent()
        {
            var a = V2GSignature.SignedInfoFragment(V2GSignature.BuildSignedInfo("ID1", SignedElementReference(out _).DigestValue));
            var b = V2GSignature.SignedInfoFragment(V2GSignature.BuildSignedInfo("ID1", SignedElementReference(out _).DigestValue));
            Assert.That(a, Is.EqualTo(b), "identical SignedInfo content must yield identical fragment octets");
        }

        [Test]
        public void EndToEnd_SignEncodeDecodeVerify()
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            // 1. Sign the AuthorizationReq body (digest its fragment, sign the SignedInfo).
            var body = new AuthorizationReqType(Id: "ID1",
                GenChallenge: new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });
            var fragBuf = new byte[512];
            Assert.That(Iso2Codec.EncodeFragment_AuthorizationReq(body, fragBuf, out int fn), Is.True);
            var bodyFragment = fragBuf.AsSpan(0, fn).ToArray();

            var signedInfo = V2GSignature.BuildSignedInfo("ID1", V2GSignature.Digest(bodyFragment));
            var signatureValue = V2GSignature.Sign(signedInfo, key);

            // 2. Assemble the signed V2G_Message and encode it.
            var message = new V2G_Message(
                new MessageHeaderType(SessionID: new byte[8], Notification: null,
                                      Signature: V2GSignature.BuildSignature(signedInfo, signatureValue)),
                new BodyType(body));
            var buf = new byte[1024];
            Assert.That(message.TryEncode(buf, out int n), Is.True);

            // 3. Decode it and verify the signature end to end.
            var decoded = (V2G_Message)Iso2Codec.DecodeAny(buf.AsSpan(0, n), out _);
            var sig = decoded.Header.Signature;
            Assert.That(sig, Is.Not.Null);

            Assert.That(V2GSignature.Verify(sig!.SignedInfo, sig.SignatureValue.Value, key), Is.True,
                "ECDSA over the decoded SignedInfo must verify");

            // The decoded body re-encodes to the same fragment, and its digest matches the reference.
            Assert.That(Iso2Codec.EncodeFragment_AuthorizationReq((AuthorizationReqType)decoded.Body.BodyElement!, fragBuf, out int fn2), Is.True);
            Assert.That(V2GSignature.VerifyReference(sig.SignedInfo.Reference[0], fragBuf.AsSpan(0, fn2)), Is.True,
                "the reference digest must match the signed element");
        }
    }
}
