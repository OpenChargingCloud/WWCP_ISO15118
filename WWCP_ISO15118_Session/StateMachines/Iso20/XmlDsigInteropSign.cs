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

using C = cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated;

namespace cloud.charging.open.protocols.ISO15118.StateMachines.Iso20
{
    /// <summary>
    /// Which algorithm pair a header signature declares in its <c>SignedInfo</c>, and therefore uses.
    /// </summary>
    public enum XmlDsigSuite
    {
        /// <summary>
        /// What ISO 15118-20 prescribes: <c>ecdsa-sha512</c> as <c>SignatureMethod</c> ([V2G20-2473]) and
        /// <c>sha512</c> as <c>DigestMethod</c> ([V2G20-2475]). The default. The same rules ask for a
        /// secp521r1 key ([V2G20-2674]); the suite does not enforce that, because whether the curve fits is
        /// the verifier's profile check (EVerest's verdict on a P-256 contract signed this way is
        /// <c>WARNING_CertificateValidationError</c>, [V2G20-2215]), and a car is allowed to be told so.
        /// </summary>
        Iso20,

        /// <summary>
        /// Josev's form: <c>ecdsa-sha256</c> and <c>sha256</c>, which Josev's SECC hard-codes when it
        /// verifies. Not what -20 asks for; kept, opt-in, for live runs against Josev.
        /// </summary>
        JosevSha256,
    }

    /// <summary>
    /// EVCC-side header signing for ISO 15118-20 Plug &amp; Charge and contract provisioning.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The grammar.</b> The <c>SignedInfo</c> is signed as encoded over the <b>standalone xmldsig</b>
    /// grammar (the 209-byte form), the XML signature schema as the entry point that [V2G20-1449] names.
    /// Two independent verifiers accept exactly these octets: Josev's <c>verify_signature</c>, which
    /// re-encodes with its <c>XMLDSIG_Core_Schema_Grammar</c>, and EVerest 2026.10.0's
    /// <c>Evse15118D20</c>, which re-encodes with cbV2G's <c>iso20_xmldsigFragment</c>
    /// (<c>ISO15118ConformanceTests/docs/interop-runs/2026-11-26-everest-2026.10.0-forward/</c>).
    /// <see cref="XmlDsigInteropVerify"/> tells the grammar story.
    /// </para>
    /// <para>
    /// <b>The algorithms</b> are the <see cref="XmlDsigSuite"/>'s. Until 2026-11-27 this class signed
    /// Josev's <c>ecdsa-sha256</c> unconditionally. Josev accepts nothing else, and nothing else was ever
    /// tried. EVerest 2026.10.0 then refused it with <c>FAILED_SignatureError</c>, as [V2G20-2473] lets it.
    /// </para>
    /// <para>
    /// The structure mirrors Josev's <c>create_signature</c> field for field. The Reference is
    /// <c>URI="#id"</c> with a <c>Transforms</c> list holding the single EXI-C14N transform
    /// ([V2G20-766], [V2G20-767]), and its digest is over the signed element's EXI fragment. The signature
    /// is raw <c>r‖s</c> at the key's field size: 64 bytes for P-256, 132 for P-521.
    /// </para>
    /// <para>
    /// Our own SECC verifies both suites in both grammars and reports which one matched
    /// (<c>SignatureGrammar == "xmldsig-standalone"</c> for this class).
    /// </para>
    /// </remarks>
    public static class XmlDsigInteropSign
    {
        private const string CanonicalizationExi = "http://www.w3.org/TR/canonical-exi/";
        private const string EcdsaSha256         = "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha256";
        private const string EcdsaSha512         = "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha512";
        private const string Sha256Uri           = "http://www.w3.org/2001/04/xmlenc#sha256";
        private const string Sha512Uri           = "http://www.w3.org/2001/04/xmlenc#sha512";

        /// <summary>Builds the header <see cref="C.SignatureType"/> for one signed element: digests
        /// <paramref name="signedElementFragment"/> (the element's EXI fragment octets), assembles the
        /// <c>SignedInfo</c> for <paramref name="suite"/>, and ECDSA-signs its standalone-xmldsig encoding
        /// with <paramref name="signingKey"/>.</summary>
        public static C.SignatureType Sign(string referenceId, ReadOnlySpan<byte> signedElementFragment,
                                           ECDsa signingKey, XmlDsigSuite suite)
        {
            var (signatureMethod, digestMethod, hash, digest) = suite switch
            {
                XmlDsigSuite.Iso20       => (EcdsaSha512, Sha512Uri, HashAlgorithmName.SHA512,
                                             SHA512.HashData(signedElementFragment)),
                XmlDsigSuite.JosevSha256 => (EcdsaSha256, Sha256Uri, HashAlgorithmName.SHA256,
                                             SHA256.HashData(signedElementFragment)),
                _ => throw new ArgumentOutOfRangeException(nameof(suite), suite, null),
            };

            var signedInfo = new C.SignedInfoType(
                Id: null,
                CanonicalizationMethod: new C.CanonicalizationMethodType(Algorithm: CanonicalizationExi, ANY: null),
                SignatureMethod: new C.SignatureMethodType(Algorithm: signatureMethod, HMACOutputLength: null, ANY: null),
                Reference: new[]
                {
                    new C.ReferenceType(
                        Id: null, Type: null, URI: "#" + referenceId,
                        Transforms: new C.TransformsType(new[] { new C.TransformType(CanonicalizationExi, XPath: null, ANY: null) }),
                        DigestMethod: new C.DigestMethodType(Algorithm: digestMethod, ANY: null),
                        DigestValue: digest),
                });

            var octets = XmlDsigInteropVerify.EncodeStandalone(signedInfo)
                ?? throw new InvalidOperationException("standalone-xmldsig SignedInfo encode failed.");
            var rawSignature = signingKey.SignData(octets, hash, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

            return new C.SignatureType(
                Id: null,
                SignedInfo: signedInfo,
                SignatureValue: new C.SignatureValueType(Id: null, Value: rawSignature),
                KeyInfo: null,
                Object: null);
        }
    }
}
