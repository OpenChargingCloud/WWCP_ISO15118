/*
 * Copyright (c) 2021-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of EVSimulatorApp
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

namespace Vanaheimr.V2G.Simulation.StateMachines.Iso20
{
    /// <summary>
    /// EVCC-side Plug &amp; Charge signing in <b>Josev's exact form</b>, so a Josev SECC verifies it
    /// (its <c>verify_signature</c> re-encodes the received <c>SignedInfo</c> over the standalone xmldsig
    /// grammar and checks a hardcoded SHA-256 ECDSA — see <see cref="XmlDsigInteropVerify"/> for the grammar
    /// story). Mirrors Josev's <c>create_signature</c> field-for-field:
    /// <list type="bullet">
    ///   <item>Reference: <c>URI="#id"</c>, a <c>Transforms</c> list with the single EXI-C14N transform,
    ///         <c>DigestMethod</c> SHA-256, <c>DigestValue</c> = SHA-256 of the signed element's EXI fragment.</item>
    ///   <item>SignedInfo: EXI C14N + <c>ecdsa-sha256</c>, EXI-encoded over the <b>standalone xmldsig</b>
    ///         grammar (209-byte form), then ECDSA-P256/SHA-256 signed, raw <c>r‖s</c> (64 bytes).</item>
    /// </list>
    /// Our own SECC accepts this form too, via its standalone-xmldsig verify fallback
    /// (<c>SignatureGrammar == "xmldsig-standalone"</c>). The -20-nominal secp521r1/SHA-512 signing over the
    /// combined CommonMessages grammar stays in <c>V2GSignature</c>; this class exists purely for live interop
    /// with EXIficient-based stacks.
    /// </summary>
    public static class XmlDsigInteropSign
    {
        private const string CanonicalizationExi = "http://www.w3.org/TR/canonical-exi/";
        private const string EcdsaSha256         = "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha256";
        private const string Sha256Uri           = "http://www.w3.org/2001/04/xmlenc#sha256";

        /// <summary>Builds the header <see cref="C.SignatureType"/> for one signed element: digests
        /// <paramref name="signedElementFragment"/> (the element's EXI fragment octets), assembles the
        /// Josev-form <c>SignedInfo</c>, and ECDSA-signs its standalone-xmldsig encoding with
        /// <paramref name="contractKey"/> (must be P-256 — the raw <c>r‖s</c> form is 64 bytes).</summary>
        public static C.SignatureType Sign(string referenceId, ReadOnlySpan<byte> signedElementFragment, ECDsa contractKey)
        {
            var signedInfo = new C.SignedInfoType(
                Id: null,
                CanonicalizationMethod: new C.CanonicalizationMethodType(Algorithm: CanonicalizationExi, ANY: null),
                SignatureMethod: new C.SignatureMethodType(Algorithm: EcdsaSha256, HMACOutputLength: null, ANY: null),
                Reference: new[]
                {
                    new C.ReferenceType(
                        Id: null, Type: null, URI: "#" + referenceId,
                        Transforms: new C.TransformsType(new[] { new C.TransformType(CanonicalizationExi, XPath: null, ANY: null) }),
                        DigestMethod: new C.DigestMethodType(Algorithm: Sha256Uri, ANY: null),
                        DigestValue: SHA256.HashData(signedElementFragment)),
                });

            var octets = XmlDsigInteropVerify.EncodeStandalone(signedInfo)
                ?? throw new InvalidOperationException("standalone-xmldsig SignedInfo encode failed.");
            var rawSignature = contractKey.SignData(octets, HashAlgorithmName.SHA256,
                                                    DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

            return new C.SignatureType(
                Id: null,
                SignedInfo: signedInfo,
                SignatureValue: new C.SignatureValueType(Id: null, Value: rawSignature),
                KeyInfo: null,
                Object: null);
        }
    }
}
