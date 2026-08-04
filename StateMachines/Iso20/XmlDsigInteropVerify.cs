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
using X = cloud.charging.open.protocols.ISO15118_20.XMLDSig.Generated;

namespace Vanaheimr.V2G.Simulation.StateMachines.Iso20
{
    /// <summary>
    /// Interop-only verification of an ISO 15118-20 <c>SignedInfo</c> signature that was produced over the
    /// <b>standalone <c>xmldsig-core-schema.xsd</c> grammar</b> rather than the combined
    /// <c>V2G_CI_CommonMessages</c> fragment grammar our production codec (and cbV2G, our reference) uses.
    /// <para>
    /// Josev's stack signs the <c>SignedInfo</c> this way — <c>to_exi(signed_info, Namespace.XML_DSIG)</c> maps
    /// the XMLDSig namespace to its <c>XMLDSIG_Core_Schema_Grammar</c> (a grammar built from
    /// xmldsig-core-schema.xsd alone), which gives the EXI <i>Fragment</i> top-level element event code one fewer
    /// bit and shifts the whole bitstream, yielding a 209-byte form vs our/cbV2G 210-byte one. Our own generator
    /// reproduces those exact bytes from the same schema (see the <c>WWCP_ISO15118_XMLDSig</c> project and
    /// <c>XmlDsigStandaloneGrammarReproducesJosev</c>), so we can <b>verify</b> such signatures — and, for the
    /// EVCC-side interop path, <b>sign</b> in the same form (see <see cref="XmlDsigInteropSign"/>): a Josev SECC
    /// re-encodes a received SignedInfo with its own standalone grammar before checking the ECDSA signature, so
    /// only this form verifies over there. Our production/default signing (<c>V2GSignature</c>) stays
    /// cbV2G-byte-exact per the project ground rule.
    /// </para>
    /// See <c>docs/interop-runs/2026-07-21-iso20-dc-pnc-tls/notes.md</c>.
    /// </summary>
    // Public to match XmlDsigInteropSign and the -2 XmlDsigInterop2, both of which already are. The
    // asymmetry was accidental: verifying is exactly as much a caller's business as signing, and the
    // trace corpus needs it to check what a replayed session actually signed. EncodeStandalone below
    // stays internal — that one really is a detail of this pair.
    public static class XmlDsigInteropVerify
    {
        /// <summary>
        /// Re-encodes <paramref name="signedInfo"/> under the standalone xmldsig grammar and ECDSA-verifies
        /// <paramref name="signatureValue"/> (raw <c>r‖s</c>) against it with <paramref name="publicKey"/>.
        /// Returns <c>false</c> (never throws) if the grammar encode fails or verification does not match.
        /// </summary>
        public static bool VerifyStandaloneXmldsig(
            C.SignedInfoType signedInfo, byte[] signatureValue, ECDsa publicKey, HashAlgorithmName hashName)
        {
            var octets = EncodeStandalone(signedInfo);
            return octets is not null
                && publicKey.VerifyData(octets, signatureValue, hashName,
                                        DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }

        /// <summary>Encodes a CommonMessages <c>SignedInfo</c> under the standalone xmldsig grammar — the exact
        /// octets Josev's stack signs/verifies. <c>null</c> if the grammar encode fails.</summary>
        internal static byte[]? EncodeStandalone(C.SignedInfoType signedInfo)
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

        // The two SignedInfo type families are generated from the same xmldsig-core-schema.xsd, so this is a
        // straight structural copy (CommonMessages record -> XmlDsig record).
        private static X.SignedInfoType Map(C.SignedInfoType s) =>
            new(s.Id,
                new X.CanonicalizationMethodType(s.CanonicalizationMethod.Algorithm, s.CanonicalizationMethod.ANY),
                new X.SignatureMethodType(s.SignatureMethod.Algorithm, s.SignatureMethod.HMACOutputLength, s.SignatureMethod.ANY),
                s.Reference.Select(Map).ToArray());

        private static X.ReferenceType Map(C.ReferenceType r) =>
            new(r.Id, r.Type, r.URI,
                r.Transforms is null ? null : new X.TransformsType(r.Transforms.Transform.Select(Map).ToArray()),
                new X.DigestMethodType(r.DigestMethod.Algorithm, r.DigestMethod.ANY),
                r.DigestValue);

        private static X.TransformType Map(C.TransformType t) =>
            new(t.Algorithm, t.XPath, t.ANY);
    }
}
