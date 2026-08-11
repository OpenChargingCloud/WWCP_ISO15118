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

using cloud.charging.open.protocols.ISO15118_20.AC_DER_IEC.Generated;

namespace cloud.charging.open.protocols.ISO15118_20.AC_DER_IEC
{
    /// <summary>
    /// XMLDSig signing/verification for ISO 15118-20 AC_DER_IEC (Amendment 1 DER) — identical suite and
    /// shape to <see cref="cloud.charging.open.protocols.ISO15118_20.CommonMessages.V2GSignature"/> (SHA-512 digests,
    /// ECDSA-P521 or Ed448 signatures), against this set's own duplicated
    /// <c>SignedInfoType</c>/codec.
    /// </summary>
    /// <remarks>
    /// The signable element stays AC's <c>AC_ChargeParameterDiscoveryRes</c> — the DER schemas keep
    /// AC's message roots and only add substitution members — but the helper cannot be AC's. The
    /// fragment grammar's element selector is sized by the whole set, and the DER members move it:
    /// SignedInfo is event code 217 at 9 bits here, against 135 at 8 bits in plain AC and 230 at 9 bits
    /// in CommonMessages. The same logical SignedInfo signs different octets, so borrowing AC's
    /// helper would sign the wrong bytes.
    /// </remarks>
    public static class V2GSignature
    {
        public const string CanonicalizationExi = "http://www.w3.org/TR/canonical-exi/";
        public const string EcdsaSha512 = "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha512";

        /// <summary>Ed448 (pure EdDSA, RFC 8032) signature method — RFC 9231 §2.3.12.</summary>
        public const string EddsaEd448 = "http://www.w3.org/2021/04/xmldsig-more#eddsa-ed448";

        public const string Sha512 = "http://www.w3.org/2001/04/xmlenc#sha512";

        public static byte[] Digest(ReadOnlySpan<byte> fragmentBytes) => SHA512.HashData(fragmentBytes);

        public static SignedInfoType BuildSignedInfo(
            string referenceId, byte[] digest, string signatureMethodAlgorithm = EcdsaSha512) =>
            new(
                Id: null,
                CanonicalizationMethod: new CanonicalizationMethodType(Algorithm: CanonicalizationExi, ANY: null),
                SignatureMethod: new SignatureMethodType(Algorithm: signatureMethodAlgorithm, HMACOutputLength: null, ANY: null),
                Reference: new[]
                {
                    new ReferenceType(
                        Id: null, Type: null, URI: "#" + referenceId, Transforms: null,
                        DigestMethod: new DigestMethodType(Algorithm: Sha512, ANY: null),
                        DigestValue: digest),
                });

        public static SignatureType BuildSignature(SignedInfoType signedInfo, byte[] signatureValue) =>
            new(Id: null,
                SignedInfo: signedInfo,
                SignatureValue: new SignatureValueType(Id: null, Value: signatureValue),
                KeyInfo: null,
                Object: null);

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
                    if (AcDerIecCodec.EncodeFragment_SignedInfo(signedInfo, buf, out int n))
                        return buf.AsSpan(0, n).ToArray();
                }
                catch (IndexOutOfRangeException)
                { /* the buffer was too small; the only way the encoder says so */ }

                if (buf.Length >= 1 << 20)
                    throw new InvalidOperationException("SignedInfo fragment: encode failed even at 1 MiB.");
                buf = new byte[buf.Length * 2];
            }
        }

        public static byte[] Sign(SignedInfoType signedInfo, ECDsa privateKey) =>
            privateKey.SignData(SignedInfoFragment(signedInfo), HashAlgorithmName.SHA512,
                                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        public static bool Verify(SignedInfoType signedInfo, byte[] signatureValue, ECDsa publicKey) =>
            publicKey.VerifyData(SignedInfoFragment(signedInfo), signatureValue, HashAlgorithmName.SHA512,
                                 DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        /// <summary>Signs a <see cref="SignedInfoType"/> with Ed448 (RFC 8032) — a "pure" EdDSA scheme,
        /// so this signs the SignedInfo fragment octets directly (no separate pre-hash like
        /// <see cref="Sign"/>'s SHA-512). Returns the raw 114-byte signature.</summary>
        public static byte[] SignEd448(SignedInfoType signedInfo, Ed448PrivateKeyParameters privateKey)
        {
            var signer = new Ed448Signer(Array.Empty<byte>());
            signer.Init(forSigning: true, privateKey);
            var message = SignedInfoFragment(signedInfo);
            signer.BlockUpdate(message, 0, message.Length);
            return signer.GenerateSignature();
        }

        /// <summary>Verifies a raw 114-byte Ed448 <c>SignatureValue</c>.</summary>
        public static bool VerifyEd448(SignedInfoType signedInfo, byte[] signatureValue, Ed448PublicKeyParameters publicKey)
        {
            var signer = new Ed448Signer(Array.Empty<byte>());
            signer.Init(forSigning: false, publicKey);
            var message = SignedInfoFragment(signedInfo);
            signer.BlockUpdate(message, 0, message.Length);
            return signer.VerifySignature(signatureValue);
        }

        public static bool VerifyReference(ReferenceType reference, ReadOnlySpan<byte> signedElementFragment) =>
            CryptographicOperations.FixedTimeEquals(reference.DigestValue, Digest(signedElementFragment));
    }
}
