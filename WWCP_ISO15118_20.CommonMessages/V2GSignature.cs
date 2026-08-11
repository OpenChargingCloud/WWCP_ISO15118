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

using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

using cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated;

namespace cloud.charging.open.protocols.ISO15118_20.CommonMessages
{
    /// <summary>
    /// XMLDSig signing/verification for ISO 15118-20 CommonMessages. Same two-level EXI-fragment digest
    /// scheme as -2's <c>V2GSignature</c> (see there for the general shape), but with -20's stronger
    /// suite: SHA-512 digests, and either of the -20 signature-suite's two asymmetric algorithms —
    /// ECDSA over NIST P-521 (secp521r1), raw <c>r‖s</c> <c>SignatureValue</c> (132 bytes: 66 + 66, IEEE
    /// P1363), via <see cref="Sign"/>/<see cref="Verify"/>; or Ed448 (RFC 8032), raw 114-byte
    /// <c>SignatureValue</c>, via <see cref="SignEd448"/>/<see cref="VerifyEd448"/> — .NET's
    /// <c>System.Security.Cryptography</c> has no Ed448 support, so that path uses BouncyCastle
    /// (<c>BouncyCastle.Cryptography</c>) instead.
    /// </summary>
    public static class V2GSignature
    {
        /// <summary>EXI canonicalization (the only C14N ISO 15118-20 uses).</summary>
        public const string CanonicalizationExi = "http://www.w3.org/TR/canonical-exi/";

        /// <summary>ECDSA-SHA512 signature method.</summary>
        public const string EcdsaSha512 = "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha512";

        /// <summary>Ed448 (pure EdDSA, RFC 8032) signature method — RFC 9231 §2.3.12.</summary>
        public const string EddsaEd448 = "http://www.w3.org/2021/04/xmldsig-more#eddsa-ed448";

        /// <summary>SHA-512 digest method.</summary>
        public const string Sha512 = "http://www.w3.org/2001/04/xmlenc#sha512";

        /// <summary>SHA-512 of an element's EXI fragment — the value that goes into its
        /// <see cref="ReferenceType.DigestValue"/>.</summary>
        public static byte[] Digest(ReadOnlySpan<byte> fragmentBytes) => SHA512.HashData(fragmentBytes);

        /// <summary>Builds a single-reference <see cref="SignedInfoType"/> over one already-computed
        /// element digest, with the fixed EXI-C14N / SHA-512 algorithm URIs and the given signature
        /// method (<see cref="EcdsaSha512"/> by default, or <see cref="EddsaEd448"/>). The reference URI
        /// is <c>"#" + <paramref name="referenceId"/></c> — the <c>Id</c> attribute of the signed
        /// element. <paramref name="includeExiTransform"/> adds the (schema-optional) <c>Transforms</c>
        /// list with the single EXI-C14N transform — some peers (Josev's pydantic models) treat it as
        /// mandatory and reject a Reference without it.</summary>
        public static SignedInfoType BuildSignedInfo(
            string referenceId, byte[] digest, string signatureMethodAlgorithm = EcdsaSha512,
            bool includeExiTransform = false) =>
            new(
                Id: null,
                CanonicalizationMethod: new CanonicalizationMethodType(Algorithm: CanonicalizationExi, ANY: null),
                SignatureMethod: new SignatureMethodType(Algorithm: signatureMethodAlgorithm, HMACOutputLength: null, ANY: null),
                Reference: new[]
                {
                    new ReferenceType(
                        Id: null, Type: null, URI: "#" + referenceId,
                        Transforms: includeExiTransform
                            ? new TransformsType(new[] { new TransformType(CanonicalizationExi, XPath: null, ANY: null) })
                            : null,
                        DigestMethod: new DigestMethodType(Algorithm: Sha512, ANY: null),
                        DigestValue: digest),
                });

        /// <summary>Assembles the header <see cref="SignatureType"/> from a signed <c>SignedInfo</c> and
        /// its raw <c>r‖s</c> <c>SignatureValue</c> (KeyInfo/Object absent, as -20 uses).</summary>
        public static SignatureType BuildSignature(SignedInfoType signedInfo, byte[] signatureValue) =>
            new(Id: null,
                SignedInfo: signedInfo,
                SignatureValue: new SignatureValueType(Id: null, Value: signatureValue),
                KeyInfo: null,
                Object: null);

        /// <summary>Encodes a <see cref="SignedInfoType"/> as its EXI fragment — the exact octets that are
        /// SHA-512'd and signed (or verified).</summary>
        /// <remarks>
        /// The growth loop catches rather than tests. A generated <c>EncodeFragment_*</c> signals a full
        /// buffer by letting <c>BitWriter</c> throw <see cref="IndexOutOfRangeException"/>, and returns
        /// <c>false</c> only for a destination too small to hold even the EXI header byte — so the <c>if</c>
        /// this loop span on never came back false, the doubling was unreachable, and 512 bytes was in truth
        /// a hard limit that threw. Unhit here while every signed -20 message has a single Reference; found
        /// in the -2 copy by the first four-reference SignedInfo (a CertificateInstallationRes, §7.9.2.4.2)
        /// and repaired across all six copies together.
        /// </remarks>
        public static byte[] SignedInfoFragment(SignedInfoType signedInfo)
        {
            var buf = new byte[1024];
            while (true)
            {
                try
                {
                    if (CommonMessagesCodec.EncodeFragment_SignedInfo(signedInfo, buf, out int n))
                        return buf.AsSpan(0, n).ToArray();
                }
                catch (IndexOutOfRangeException)
                { /* the buffer was too small; the only way the encoder says so */ }

                if (buf.Length >= 1 << 20)
                    throw new InvalidOperationException("SignedInfo fragment: encode failed even at 1 MiB.");
                buf = new byte[buf.Length * 2];
            }
        }

        /// <summary>Signs a <see cref="SignedInfoType"/>: SHA-512 over its EXI fragment, ECDSA-P521,
        /// returning the raw <c>r‖s</c> (132-byte) <c>SignatureValue</c>.</summary>
        public static byte[] Sign(SignedInfoType signedInfo, ECDsa privateKey) =>
            privateKey.SignData(SignedInfoFragment(signedInfo), HashAlgorithmName.SHA512,
                                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        /// <summary>Verifies a raw <c>r‖s</c> <c>SignatureValue</c> against a <see cref="SignedInfoType"/>
        /// and public key. Only checks the ECDSA signature over the SignedInfo fragment; the caller is
        /// responsible for confirming each reference digest matches the signed element (see
        /// <see cref="VerifyReference"/>).</summary>
        public static bool Verify(SignedInfoType signedInfo, byte[] signatureValue, ECDsa publicKey) =>
            publicKey.VerifyData(SignedInfoFragment(signedInfo), signatureValue, HashAlgorithmName.SHA512,
                                 DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        /// <summary>Signs a <see cref="SignedInfoType"/> with Ed448 (RFC 8032) — a "pure" EdDSA scheme,
        /// so this signs the SignedInfo fragment octets directly (the algorithm's internal SHAKE256
        /// takes the place of an external pre-hash; there is no separate digest step like
        /// <see cref="Sign"/>'s SHA-512). Returns the raw 114-byte signature.</summary>
        public static byte[] SignEd448(SignedInfoType signedInfo, Ed448PrivateKeyParameters privateKey)
        {
            var signer = new Ed448Signer(Array.Empty<byte>());
            signer.Init(forSigning: true, privateKey);
            var message = SignedInfoFragment(signedInfo);
            signer.BlockUpdate(message, 0, message.Length);
            return signer.GenerateSignature();
        }

        /// <summary>Verifies a raw 114-byte Ed448 <c>SignatureValue</c> against a <see cref="SignedInfoType"/>
        /// and public key. Only checks the Ed448 signature over the SignedInfo fragment; the caller is
        /// responsible for confirming each reference digest matches the signed element (see
        /// <see cref="VerifyReference"/>).</summary>
        public static bool VerifyEd448(SignedInfoType signedInfo, byte[] signatureValue, Ed448PublicKeyParameters publicKey)
        {
            var signer = new Ed448Signer(Array.Empty<byte>());
            signer.Init(forSigning: false, publicKey);
            var message = SignedInfoFragment(signedInfo);
            signer.BlockUpdate(message, 0, message.Length);
            return signer.VerifySignature(signatureValue);
        }

        /// <summary>Confirms that a reference's <see cref="ReferenceType.DigestValue"/> equals the SHA-512
        /// of the given signed-element fragment — the second half of verification.</summary>
        public static bool VerifyReference(ReferenceType reference, ReadOnlySpan<byte> signedElementFragment) =>
            CryptographicOperations.FixedTimeEquals(reference.DigestValue, Digest(signedElementFragment));
    }
}
