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

using cloud.charging.open.protocols.ISO15118_2.Generated;

namespace cloud.charging.open.protocols.ISO15118_2
{
    /// <summary>
    /// XMLDSig signing/verification for ISO 15118-2 (§7.9 / Annex J). A V2G signature is an XML digital
    /// signature computed over EXI <em>fragments</em>, with two levels of digest:
    ///
    /// <list type="number">
    ///   <item>each signed element is encoded as an EXI fragment and SHA-256 digested; the digest goes
    ///   into a <see cref="ReferenceType"/> inside <see cref="SignedInfoType"/>;</item>
    ///   <item>the <c>SignedInfo</c> is itself encoded as an EXI fragment, SHA-256 digested and signed
    ///   with ECDSA over the NIST P-256 curve.</item>
    /// </list>
    ///
    /// The <c>SignatureValue</c> on the wire is the raw <c>r‖s</c> pair (32 + 32 bytes, IEEE P1363),
    /// not the ASN.1/DER encoding — ISO 15118-2 fixes the plain concatenation.
    ///
    /// <para>All fragment bytes come from the generated, cbV2G-byte-exact fragment codecs, so the digests
    /// match what a conforming peer computes.</para>
    /// </summary>
    public static class V2GSignature
    {
        /// <summary>EXI canonicalization (the only C14N ISO 15118-2 uses).</summary>
        public const string CanonicalizationExi = "http://www.w3.org/TR/canonical-exi/";

        /// <summary>ECDSA-SHA256 signature method.</summary>
        public const string EcdsaSha256 = "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha256";

        /// <summary>SHA-256 digest method.</summary>
        public const string Sha256 = "http://www.w3.org/2001/04/xmlenc#sha256";

        /// <summary>SHA-256 of an element's EXI fragment — the value that goes into its
        /// <see cref="ReferenceType.DigestValue"/>.</summary>
        public static byte[] Digest(ReadOnlySpan<byte> fragmentBytes) => SHA256.HashData(fragmentBytes);

        /// <summary>Builds a single-reference <see cref="SignedInfoType"/> over one already-computed
        /// element digest, with the fixed EXI-C14N / ECDSA-SHA256 / SHA-256 algorithm URIs. The reference
        /// URI is <c>"#" + <paramref name="referenceId"/></c> — the <c>Id</c> attribute of the signed
        /// element.</summary>
        public static SignedInfoType BuildSignedInfo(string referenceId, byte[] digest) =>
            new(
                Id: null,
                CanonicalizationMethod: new CanonicalizationMethodType(Algorithm: CanonicalizationExi, ANY: null),
                SignatureMethod: new SignatureMethodType(Algorithm: EcdsaSha256, HMACOutputLength: null, ANY: null),
                Reference: new[]
                {
                    new ReferenceType(
                        Id: null, Type: null, URI: "#" + referenceId, Transforms: null,
                        DigestMethod: new DigestMethodType(Algorithm: Sha256, ANY: null),
                        DigestValue: digest),
                });

        /// <summary>Builds a multi-reference <see cref="SignedInfoType"/> — one reference per signed
        /// element, all under ONE header signature (e.g. every signed <c>SalesTariff</c> of an
        /// SAScheduleList offer, §7.9.2.5). Reference URIs are <c>"#" + ReferenceId</c>.
        /// <paramref name="includeExiTransform"/> adds the (schema-optional) <c>Transforms</c> list with
        /// the single EXI-C14N transform — some peers (Josev's pydantic models) treat it as mandatory and
        /// fail message validation on a Reference without it (seen live in -2 ChargeParameterDiscovery,
        /// same bug class as its -20 CertificateInstallation counterpart).</summary>
        public static SignedInfoType BuildSignedInfo(IReadOnlyList<(string ReferenceId, byte[] Digest)> references,
                                                     bool includeExiTransform = false) =>
            new(
                Id: null,
                CanonicalizationMethod: new CanonicalizationMethodType(Algorithm: CanonicalizationExi, ANY: null),
                SignatureMethod: new SignatureMethodType(Algorithm: EcdsaSha256, HMACOutputLength: null, ANY: null),
                Reference: references
                    .Select(r => new ReferenceType(
                        Id: null, Type: null, URI: "#" + r.ReferenceId,
                        Transforms: includeExiTransform
                            ? new TransformsType(new[] { new TransformType(CanonicalizationExi, XPath: null, ANY: null) })
                            : null,
                        DigestMethod: new DigestMethodType(Algorithm: Sha256, ANY: null),
                        DigestValue: r.Digest))
                    .ToArray());

        /// <summary>Assembles the header <see cref="SignatureType"/> from a signed <c>SignedInfo</c> and
        /// its raw <c>r‖s</c> <c>SignatureValue</c> (KeyInfo/Object absent, as ISO 15118-2 uses).</summary>
        public static SignatureType BuildSignature(SignedInfoType signedInfo, byte[] signatureValue) =>
            new(Id: null,
                SignedInfo: signedInfo,
                SignatureValue: new SignatureValueType(Id: null, Value: signatureValue),
                KeyInfo: null,
                Object: null);

        /// <summary>Encodes a <see cref="SignedInfoType"/> as its EXI fragment — the exact octets that are
        /// SHA-256'd and signed (or verified).</summary>
        public static byte[] SignedInfoFragment(SignedInfoType signedInfo)
        {
            var buf = new byte[512];
            while (true)
            {
                if (Iso2Codec.EncodeFragment_SignedInfo(signedInfo, buf, out int n))
                    return buf.AsSpan(0, n).ToArray();
                buf = new byte[buf.Length * 2];
            }
        }

        /// <summary>Signs a <see cref="SignedInfoType"/>: SHA-256 over its EXI fragment, ECDSA-P256,
        /// returning the raw <c>r‖s</c> (64-byte) <c>SignatureValue</c>.</summary>
        public static byte[] Sign(SignedInfoType signedInfo, ECDsa privateKey) =>
            privateKey.SignData(SignedInfoFragment(signedInfo), HashAlgorithmName.SHA256,
                                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        /// <summary>Verifies a raw <c>r‖s</c> <c>SignatureValue</c> against a <see cref="SignedInfoType"/>
        /// and public key. Only checks the ECDSA signature over the SignedInfo fragment; the caller is
        /// responsible for confirming each reference digest matches the signed element (see
        /// <see cref="VerifyReference"/>).</summary>
        public static bool Verify(SignedInfoType signedInfo, byte[] signatureValue, ECDsa publicKey) =>
            publicKey.VerifyData(SignedInfoFragment(signedInfo), signatureValue, HashAlgorithmName.SHA256,
                                 DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        /// <summary>Confirms that a reference's <see cref="ReferenceType.DigestValue"/> equals the SHA-256
        /// of the given signed-element fragment — the second half of verification.</summary>
        public static bool VerifyReference(ReferenceType reference, ReadOnlySpan<byte> signedElementFragment) =>
            CryptographicOperations.FixedTimeEquals(reference.DigestValue, Digest(signedElementFragment));
    }
}
