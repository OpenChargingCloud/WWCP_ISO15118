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

using cloud.charging.open.protocols.ISO15118.StateMachines.Iso2;
using cloud.charging.open.protocols.ISO15118.StateMachines.Iso20;

using Ac       = cloud.charging.open.protocols.ISO15118_20.AC;
using AcDerIec = cloud.charging.open.protocols.ISO15118_20.AC_DER_IEC;
using AcDerSae = cloud.charging.open.protocols.ISO15118_20.AC_DER_SAE;
using Common   = cloud.charging.open.protocols.ISO15118_20.CommonMessages;
using Dc       = cloud.charging.open.protocols.ISO15118_20.DC;
using Iso2     = cloud.charging.open.protocols.ISO15118_2;


using NUnit.Framework;

namespace cloud.charging.open.protocols.ISO15118.Session.Tests.StateMachines
{

    /// <summary>
    /// <c>SignedInfoFragment</c> has to grow its buffer, in all six copies of it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each copy opened with 512 bytes and a loop that doubled on <c>false</c> — but a generated
    /// <c>EncodeFragment_*</c> reports a full buffer by letting <c>BitWriter</c> throw
    /// <see cref="IndexOutOfRangeException"/>, and returns <c>false</c> only when the destination cannot
    /// hold even the EXI header byte. The doubling was therefore unreachable and 512 was a hard limit that
    /// threw.
    /// </para>
    /// <para>
    /// It went unnoticed because every signed message in this stack had a single <c>Reference</c> until ISO
    /// 15118-2 contract provisioning arrived with four (§7.9.2.4.2). These tests use four everywhere, which
    /// for the -20 sets is a shape they do not produce today — the point is not the count but the contract:
    /// the helper must survive a fragment that does not fit at the first attempt, whatever made it large.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class SignedInfoFragmentTests
    {

        /// <summary>Four references with full-length SHA-512 digests and the EXI transform — comfortably
        /// past 512 bytes, which is the whole point.</summary>
        private static IReadOnlyList<(String, Byte[])> FourReferences()
            => Enumerable.Range(1, 4)
                         .Select(i => ($"id{i}", SHA512.HashData(new[] { (Byte) i })))
                         .ToList();

        [Test]
        public void Iso2_grows_past_its_starting_buffer()
        {

            var signedInfo = Iso2.V2GSignature.BuildSignedInfo(FourReferences(), includeExiTransform: true);
            var fragment   = Iso2.V2GSignature.SignedInfoFragment(signedInfo);

            Assert.That(fragment, Has.Length.GreaterThan(512),
                        "precondition: this is a fragment the old 512-byte buffer could not hold");

        }

        [Test]
        public void The_five_ISO_15118_20_copies_grow_too()
        {

            // Built by hand rather than through each set's BuildSignedInfo, because the -20 helpers take a
            // single reference — the very assumption that hid this.
            Assert.Multiple(() => {

                Assert.That(Common.V2GSignature.SignedInfoFragment(CommonSignedInfo()), Has.Length.GreaterThan(512));
                Assert.That(Ac.V2GSignature.SignedInfoFragment(AcSignedInfo()),               Has.Length.GreaterThan(512));
                Assert.That(Dc.V2GSignature.SignedInfoFragment(DcSignedInfo()),               Has.Length.GreaterThan(512));
                Assert.That(AcDerIec.V2GSignature.SignedInfoFragment(AcDerIecSignedInfo()),   Has.Length.GreaterThan(512));
                Assert.That(AcDerSae.V2GSignature.SignedInfoFragment(AcDerSaeSignedInfo()),   Has.Length.GreaterThan(512));

            });

        }

        [Test]
        public void The_standalone_xmldsig_encoders_grow_as_well()
        {

            // The fallback grammar, and the one that mattered most: a -2 CertificateInstallationRes whose
            // signature does not verify under the combined grammar is retried here, with all four
            // references. It threw instead of growing, on a path this stack actually takes.
            //
            // Asserted through the public verify rather than the internal encoder, because "returns false"
            // is what a caller needs and "throws" is what it used to do. A wrong answer would be a bug in
            // something else; an exception is this one.
            using var key    = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var       stub   = new Byte[64];
            var       iso2   = Iso2.V2GSignature.BuildSignedInfo(FourReferences(), includeExiTransform: true);
            var       common = CommonSignedInfo();

            Assert.Multiple(() => {

                Assert.That(() => XmlDsigInterop2.VerifyStandaloneXmldsig(iso2, stub, key),
                            Throws.Nothing);
                Assert.That(XmlDsigInterop2.VerifyStandaloneXmldsig(iso2, stub, key), Is.False,
                            "a stub signature must not verify — the point is only that asking does not throw");

                Assert.That(() => XmlDsigInteropVerify.VerifyStandaloneXmldsig(common, stub, key, HashAlgorithmName.SHA512),
                            Throws.Nothing);

            });

        }

        #region Per-set SignedInfo builders

        private static Common.Generated.SignedInfoType CommonSignedInfo()
            => new (Id: null,
                    new Common.Generated.CanonicalizationMethodType(Common.V2GSignature.CanonicalizationExi, ANY: null),
                    new Common.Generated.SignatureMethodType(Common.V2GSignature.EcdsaSha512, HMACOutputLength: null, ANY: null),
                    FourReferences().Select(r => new Common.Generated.ReferenceType(
                        Id: null, Type: null, URI: "#" + r.Item1,
                        Transforms: new Common.Generated.TransformsType(new[]
                        {
                            new Common.Generated.TransformType(Common.V2GSignature.CanonicalizationExi, XPath: null, ANY: null),
                        }),
                        DigestMethod: new Common.Generated.DigestMethodType(Common.V2GSignature.Sha512, ANY: null),
                        DigestValue: r.Item2)).ToArray());

        private static Ac.Generated.SignedInfoType AcSignedInfo()
            => new (Id: null,
                    new Ac.Generated.CanonicalizationMethodType(Common.V2GSignature.CanonicalizationExi, ANY: null),
                    new Ac.Generated.SignatureMethodType(Common.V2GSignature.EcdsaSha512, HMACOutputLength: null, ANY: null),
                    FourReferences().Select(r => new Ac.Generated.ReferenceType(
                        Id: null, Type: null, URI: "#" + r.Item1,
                        Transforms: new Ac.Generated.TransformsType(new[]
                        {
                            new Ac.Generated.TransformType(Common.V2GSignature.CanonicalizationExi, XPath: null, ANY: null),
                        }),
                        DigestMethod: new Ac.Generated.DigestMethodType(Common.V2GSignature.Sha512, ANY: null),
                        DigestValue: r.Item2)).ToArray());

        private static Dc.Generated.SignedInfoType DcSignedInfo()
            => new (Id: null,
                    new Dc.Generated.CanonicalizationMethodType(Common.V2GSignature.CanonicalizationExi, ANY: null),
                    new Dc.Generated.SignatureMethodType(Common.V2GSignature.EcdsaSha512, HMACOutputLength: null, ANY: null),
                    FourReferences().Select(r => new Dc.Generated.ReferenceType(
                        Id: null, Type: null, URI: "#" + r.Item1,
                        Transforms: new Dc.Generated.TransformsType(new[]
                        {
                            new Dc.Generated.TransformType(Common.V2GSignature.CanonicalizationExi, XPath: null, ANY: null),
                        }),
                        DigestMethod: new Dc.Generated.DigestMethodType(Common.V2GSignature.Sha512, ANY: null),
                        DigestValue: r.Item2)).ToArray());

        private static AcDerIec.Generated.SignedInfoType AcDerIecSignedInfo()
            => new (Id: null,
                    new AcDerIec.Generated.CanonicalizationMethodType(Common.V2GSignature.CanonicalizationExi, ANY: null),
                    new AcDerIec.Generated.SignatureMethodType(Common.V2GSignature.EcdsaSha512, HMACOutputLength: null, ANY: null),
                    FourReferences().Select(r => new AcDerIec.Generated.ReferenceType(
                        Id: null, Type: null, URI: "#" + r.Item1,
                        Transforms: new AcDerIec.Generated.TransformsType(new[]
                        {
                            new AcDerIec.Generated.TransformType(Common.V2GSignature.CanonicalizationExi, XPath: null, ANY: null),
                        }),
                        DigestMethod: new AcDerIec.Generated.DigestMethodType(Common.V2GSignature.Sha512, ANY: null),
                        DigestValue: r.Item2)).ToArray());

        private static AcDerSae.Generated.SignedInfoType AcDerSaeSignedInfo()
            => new (Id: null,
                    new AcDerSae.Generated.CanonicalizationMethodType(Common.V2GSignature.CanonicalizationExi, ANY: null),
                    new AcDerSae.Generated.SignatureMethodType(Common.V2GSignature.EcdsaSha512, HMACOutputLength: null, ANY: null),
                    FourReferences().Select(r => new AcDerSae.Generated.ReferenceType(
                        Id: null, Type: null, URI: "#" + r.Item1,
                        Transforms: new AcDerSae.Generated.TransformsType(new[]
                        {
                            new AcDerSae.Generated.TransformType(Common.V2GSignature.CanonicalizationExi, XPath: null, ANY: null),
                        }),
                        DigestMethod: new AcDerSae.Generated.DigestMethodType(Common.V2GSignature.Sha512, ANY: null),
                        DigestValue: r.Item2)).ToArray());

        #endregion

    }

}
