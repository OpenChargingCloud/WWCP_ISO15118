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

using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace Vanaheimr.V2G.Simulation.Transport.BouncyCastle
{
    /// <summary>
    /// Shared building blocks for the BouncyCastle V2G TLS client/server: the ISO 15118-20 signature
    /// schemes, credentialed-signer construction, and peer-certificate validation.
    /// </summary>
    internal static class BcV2GTls
    {
        /// <summary>ISO 15118-20's two TLS signature schemes: ECDSA-secp521r1-SHA512 and Ed448.</summary>
        internal static readonly IList<SignatureAndHashAlgorithm> AcceptedSignatureAlgorithms =
            new List<SignatureAndHashAlgorithm>
            {
                SignatureScheme.GetSignatureAndHashAlgorithm(SignatureScheme.ecdsa_secp521r1_sha512),
                SignatureScheme.GetSignatureAndHashAlgorithm(SignatureScheme.ed448),
            };

        /// <summary>
        /// The -20 TLS 1.3 cipher suites — derived from <see cref="TlsProfiles.Iso20CipherSuites"/> rather than
        /// listed again, so the .NET and BouncyCastle backends cannot drift apart. Both enumerations carry the
        /// IANA code points, so the cast is exact (asserted by <c>TlsProfilesTests</c>).
        /// </summary>
        internal static readonly int[] CipherSuites =
            TlsProfiles.Iso20CipherSuites.Select(suite => (int) suite).ToArray();

        internal static readonly ProtocolVersion[] Tls13Only = { ProtocolVersion.TLSv13 };

        /// <summary>
        /// The signature algorithms to put in the <c>CertificateRequest</c>: the -20 pair by default, or the
        /// caller's explicit list (<see cref="BcTlsOptions.AcceptedClientSignatureSchemes"/>) — a documented
        /// deviation used by the macOS TLS 1.3 fallback for its P-256 certificates.
        /// </summary>
        internal static IList<SignatureAndHashAlgorithm> AcceptedClientSignatureAlgorithms(BcTlsOptions options)
            => options.AcceptedClientSignatureSchemes is { Length: > 0 } schemes
                   ? schemes.Select(SignatureScheme.GetSignatureAndHashAlgorithm).ToList()
                   : AcceptedSignatureAlgorithms;

        internal static TlsCredentials BuildSigner(BcTlsCrypto crypto, BcTlsCredentials creds, TlsContext context)
        {
            // TLS 1.3 uses the CertificateEntry form with an (empty) certificate_request_context — the
            // legacy Certificate(TlsCertificate[]) ctor is a 1.2-only structure and trips an internal_error.
            var entries = creds.CertificateChain
                               .Select(der => new CertificateEntry(new BcTlsCertificate(crypto, der),
                                                                    (IDictionary<int, byte[]>?) null))
                               .ToArray();

            return new BcDefaultTlsCredentialedSigner(
                       new TlsCryptoParameters(context),
                       crypto,
                       creds.PrivateKey,
                       new Certificate(TlsUtilities.EmptyBytes, entries),
                       SignatureScheme.GetSignatureAndHashAlgorithm(creds.SignatureScheme));
        }

        internal static void ValidatePeer(Certificate? peer, Func<byte[], bool>? validate, short missingAlert)
        {
            if (peer is null || peer.IsEmpty)
                throw new TlsFatalAlert(missingAlert);

            if (validate is null)
                return;

            if (!validate(peer.GetCertificateAt(0).GetEncoded()))
                throw new TlsFatalAlert(AlertDescription.bad_certificate);
        }
    }
}
