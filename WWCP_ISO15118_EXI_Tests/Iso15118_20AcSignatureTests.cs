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

using cloud.charging.open.protocols.ISO15118_20.AC;
using cloud.charging.open.protocols.ISO15118_20.AC.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests
{
    /// <summary>
    /// ISO 15118-20 AC XMLDSig signing/verification: same ECDSA-P521/SHA-512 suite as CommonMessages'
    /// (<see cref="Iso15118_20SignatureTests"/>), against AC's own <c>SignedInfo</c>/<c>V2GSignature</c>.
    /// The fragment octets are pinned to cbV2G in <see cref="Iso15118_20AcFragmentTests"/>.
    /// </summary>
    [TestFixture]
    public class Iso15118_20AcSignatureTests
    {
        private static ReferenceType SignedElementReference(out byte[] fragment)
        {
            var content = new AC_ChargeParameterDiscoveryResType(
                Header: new MessageHeaderType(SessionID: new byte[8], TimeStamp: 1_700_000_000UL, Signature: null),
                ResponseCode: ResponseCode.OK,
                AC_CPDResEnergyTransferMode: new AC_CPDResEnergyTransferModeType(
                    EVSEMaximumChargePower: new RationalNumberType(0, 22000),
                    EVSEMaximumChargePower_L2: null,
                    EVSEMaximumChargePower_L3: null,
                    EVSEMinimumChargePower: new RationalNumberType(0, 100),
                    EVSEMinimumChargePower_L2: null,
                    EVSEMinimumChargePower_L3: null,
                    EVSENominalFrequency: new RationalNumberType(0, 50),
                    MaximumPowerAsymmetry: null,
                    EVSEPowerRampLimitation: null,
                    EVSEPresentActivePower: null,
                    EVSEPresentActivePower_L2: null,
                    EVSEPresentActivePower_L3: null));
            var buf = new byte[512];
            Assert.That(AcCodec.EncodeFragment_AC_ChargeParameterDiscoveryRes(content, buf, out int n), Is.True);
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

            signatureValue[0] ^= 0xFF;

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

        // ── Ed448 (RFC 8032) — the -20 signature suite's other option, via BouncyCastle ──────────

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
