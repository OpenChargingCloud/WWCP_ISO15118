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

using cloud.charging.open.protocols.ISO15118.StateMachines.Iso20;

using NUnit.Framework;

namespace cloud.charging.open.protocols.ISO15118.Session.Tests.StateMachines
{

    /// <summary>
    /// What an ISO 15118-20 header signature from our car declares, and that it signs what it declares.
    /// </summary>
    /// <remarks>
    /// Until 2026-11-27 <see cref="XmlDsigInteropSign"/> declared <c>ecdsa-sha256</c> whatever it was
    /// asked to sign, because Josev, the first station it met, accepts nothing else. EVerest 2026.10.0 holds
    /// the car to [V2G20-2473] and refused every AuthorizationReq with <c>FAILED_SignatureError</c>. The
    /// suite is now a choice, and these tests pin both choices.
    /// </remarks>
    [TestFixture]
    public class XmlDsigInteropSignTests
    {

        private static readonly Byte[] Fragment = "an EXI fragment stands in here"u8.ToArray();

        [Test]
        public void TheIso20SuiteDeclaresEcdsaSha512AndASha512Digest()
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP521);
            var signature = XmlDsigInteropSign.Sign("id1", Fragment, key, XmlDsigSuite.Iso20);
            var reference = signature.SignedInfo.Reference[0];

            Assert.Multiple(() =>
            {
                Assert.That(signature.SignedInfo.SignatureMethod.Algorithm,
                            Is.EqualTo("http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha512"), "[V2G20-2473]");
                Assert.That(reference.DigestMethod.Algorithm,
                            Is.EqualTo("http://www.w3.org/2001/04/xmlenc#sha512"), "[V2G20-2475]");
                Assert.That(reference.DigestValue, Is.EqualTo(SHA512.HashData(Fragment)));
                Assert.That(reference.URI, Is.EqualTo("#id1"));
                Assert.That(reference.Transforms!.Transform, Has.Count.EqualTo(1), "[V2G20-767]");

                // Raw r‖s at the field size: 2 × 66 bytes for secp521r1.
                Assert.That(signature.SignatureValue.Value, Has.Length.EqualTo(132));
                Assert.That(XmlDsigInteropVerify.VerifyStandaloneXmldsig(signature.SignedInfo,
                                signature.SignatureValue.Value, key, HashAlgorithmName.SHA512),
                            Is.True, "signed over SHA-512, as declared");
                Assert.That(XmlDsigInteropVerify.VerifyStandaloneXmldsig(signature.SignedInfo,
                                signature.SignatureValue.Value, key, HashAlgorithmName.SHA256),
                            Is.False, "and not over the hash the old form used");
            });
        }

        [Test]
        public void TheJosevSuiteStillDeclaresEcdsaSha256()
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var signature = XmlDsigInteropSign.Sign("id1", Fragment, key, XmlDsigSuite.JosevSha256);
            var reference = signature.SignedInfo.Reference[0];

            Assert.Multiple(() =>
            {
                Assert.That(signature.SignedInfo.SignatureMethod.Algorithm,
                            Is.EqualTo("http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha256"));
                Assert.That(reference.DigestMethod.Algorithm, Is.EqualTo("http://www.w3.org/2001/04/xmlenc#sha256"));
                Assert.That(reference.DigestValue, Is.EqualTo(SHA256.HashData(Fragment)));
                Assert.That(signature.SignatureValue.Value, Has.Length.EqualTo(64));
                Assert.That(XmlDsigInteropVerify.VerifyStandaloneXmldsig(signature.SignedInfo,
                                signature.SignatureValue.Value, key, HashAlgorithmName.SHA256),
                            Is.True);
            });
        }

        /// <summary>The default a caller gets without asking is -20's own, not Josev's.</summary>
        [Test]
        public void TheOptionsDefaultToTheIso20Suite()
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP521);
            using var agreement = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP521);

            Assert.Multiple(() =>
            {
                Assert.That(new PncEvccOptions([], [], key).Suite, Is.EqualTo(XmlDsigSuite.Iso20));
                Assert.That(new CertInstallEvccOptions([], [], key, agreement).Suite, Is.EqualTo(XmlDsigSuite.Iso20));
            });
        }

    }

}
