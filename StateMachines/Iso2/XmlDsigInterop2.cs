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

using I2 = cloud.charging.open.protocols.ISO15118_2.Generated;
using X = cloud.charging.open.protocols.ISO15118_20.XMLDSig.Generated;

namespace Vanaheimr.V2G.Simulation.StateMachines.Iso2
{
    /// <summary>
    /// ISO 15118-<b>2</b> flavour of the Josev signature interop (see the -20 counterparts
    /// <c>Iso20.XmlDsigInteropVerify</c>/<c>Iso20.XmlDsigInteropSign</c> for the grammar story): Josev's
    /// <c>create_signature</c>/<c>verify_signature</c> are protocol-agnostic — for -2 too, the
    /// <c>SignedInfo</c> octets are EXI-encoded over the <b>standalone xmldsig grammar</b> (not the combined
    /// <c>V2G_CI_MsgDef</c> fragment grammar our production codec and cbV2G use), with hardcoded SHA-256 and
    /// a mandatory <c>Transforms</c> (EXI C14N) in each Reference. The xmldsig schema is byte-identical
    /// across -2/-20, so the same <c>WWCP_ISO15118_XMLDSig</c> codec serves both; only the CLR record
    /// families differ, hence this separate structural mapping.
    /// </summary>
    public static class XmlDsigInterop2
    {
        private const string CanonicalizationExi = "http://www.w3.org/TR/canonical-exi/";
        private const string EcdsaSha256         = "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha256";
        private const string Sha256Uri           = "http://www.w3.org/2001/04/xmlenc#sha256";

        /// <summary>Builds the header <see cref="I2.SignatureType"/> for one signed body element in Josev's
        /// exact -2 form: SHA-256 digest of <paramref name="signedElementFragment"/>, <c>Transforms</c> =
        /// [EXI C14N], SignedInfo signed ECDSA-P256/SHA-256 over its standalone-xmldsig encoding, raw
        /// <c>r‖s</c> (64 bytes).</summary>
        public static I2.SignatureType Sign(string referenceId, ReadOnlySpan<byte> signedElementFragment, ECDsa contractKey)
        {
            var signedInfo = new I2.SignedInfoType(
                Id: null,
                CanonicalizationMethod: new I2.CanonicalizationMethodType(Algorithm: CanonicalizationExi, ANY: null),
                SignatureMethod: new I2.SignatureMethodType(Algorithm: EcdsaSha256, HMACOutputLength: null, ANY: null),
                Reference: new[]
                {
                    new I2.ReferenceType(
                        Id: null, Type: null, URI: "#" + referenceId,
                        Transforms: new I2.TransformsType(new[] { new I2.TransformType(CanonicalizationExi, XPath: null, ANY: null) }),
                        DigestMethod: new I2.DigestMethodType(Algorithm: Sha256Uri, ANY: null),
                        DigestValue: SHA256.HashData(signedElementFragment)),
                });

            var octets = EncodeStandalone(signedInfo)
                ?? throw new InvalidOperationException("standalone-xmldsig SignedInfo encode failed.");
            var rawSignature = contractKey.SignData(octets, HashAlgorithmName.SHA256,
                                                    DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

            return new I2.SignatureType(
                Id: null,
                SignedInfo: signedInfo,
                SignatureValue: new I2.SignatureValueType(Id: null, Value: rawSignature),
                KeyInfo: null,
                Object: null);
        }

        /// <summary>Re-encodes a received -2 <c>SignedInfo</c> under the standalone xmldsig grammar and
        /// ECDSA-P256/SHA-256-verifies <paramref name="signatureValue"/> against it (the Josev form).
        /// <c>false</c> (never throws) on encode failure or mismatch.</summary>
        public static bool VerifyStandaloneXmldsig(I2.SignedInfoType signedInfo, byte[] signatureValue, ECDsa publicKey)
        {
            var octets = EncodeStandalone(signedInfo);
            return octets is not null
                && publicKey.VerifyData(octets, signatureValue, HashAlgorithmName.SHA256,
                                        DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }

        private static byte[]? EncodeStandalone(I2.SignedInfoType signedInfo)
        {
            var mapped = Map(signedInfo);
            var buf = new byte[512];
            while (!X.XmlDsigCodec.EncodeFragment_SignedInfo(mapped, buf, out _))
            {
                if (buf.Length >= 1 << 20) return null; // guard; a SignedInfo never approaches 1 MiB
                buf = new byte[buf.Length * 2];
            }
            X.XmlDsigCodec.EncodeFragment_SignedInfo(mapped, buf, out int n);
            return buf.AsSpan(0, n).ToArray();
        }

        // Same xmldsig-core-schema.xsd on both sides — a straight structural copy (-2 record → XmlDsig record).
        private static X.SignedInfoType Map(I2.SignedInfoType s) =>
            new(s.Id,
                new X.CanonicalizationMethodType(s.CanonicalizationMethod.Algorithm, s.CanonicalizationMethod.ANY),
                new X.SignatureMethodType(s.SignatureMethod.Algorithm, s.SignatureMethod.HMACOutputLength, s.SignatureMethod.ANY),
                s.Reference.Select(Map).ToArray());

        private static X.ReferenceType Map(I2.ReferenceType r) =>
            new(r.Id, r.Type, r.URI,
                r.Transforms is null ? null : new X.TransformsType(r.Transforms.Transform.Select(Map).ToArray()),
                new X.DigestMethodType(r.DigestMethod.Algorithm, r.DigestMethod.ANY),
                r.DigestValue);

        private static X.TransformType Map(I2.TransformType t) =>
            new(t.Algorithm, t.XPath, t.ANY);
    }
}
