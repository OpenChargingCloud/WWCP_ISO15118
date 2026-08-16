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
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace cloud.charging.open.protocols.ISO15118.Transport.BouncyCastle
{
    /// <summary>
    /// SECC-side BouncyCastle TLS server: TLS 1.3 with the -20 cipher suites, presents its SECC certificate,
    /// and (for mutual TLS) requires + validates the EVCC's Vehicle client certificate.
    /// </summary>
    internal sealed class BcV2GTlsServer : DefaultTlsServer
    {
        private readonly BcTlsCrypto _crypto;
        private readonly BcTlsOptions _options;

        public BcV2GTlsServer(BcTlsCrypto crypto, BcTlsOptions options) : base(crypto)
        {
            _crypto  = crypto;
            _options = options;
        }

        public override ProtocolVersion[] GetProtocolVersions()
            => _options.Iso2Profile ? BcV2GTls.Tls12Only : BcV2GTls.Tls13Only;

        protected override int[] GetSupportedCipherSuites()
            => _options.Iso2Profile ? BcV2GTls.Iso2CipherSuites : BcV2GTls.CipherSuites;

        /// <summary>Records the client's <c>trusted_ca_keys</c> if it sent one — `[V2G2-651]` on the car's
        /// side, and the input `[V2G2-871]`'s selection duty is answerable to.</summary>
        /// <remarks>
        /// This station does not select on it: it serves the chain it was configured with, which is what
        /// makes a one-root deployment indistinguishable from a station that ignores the extension
        /// entirely — the very confusion that made <c>everest-isomux</c> §4 worth filing. Recorded rather
        /// than acted on, and said out loud here so nobody reads the callback as compliance.
        /// </remarks>
        public override void ProcessClientExtensions(IDictionary<int, byte[]> clientExtensions)
        {

            base.ProcessClientExtensions(clientExtensions);

            if (_options.OnTrustedCaKeys is { } observe &&
                clientExtensions is not null &&
                clientExtensions.TryGetValue(ExtensionType.trusted_ca_keys, out var raw) &&
                raw is not null)
            {
                var hashes = BcV2GTls.ParseTrustedCaKeys(raw, out var otherTypes);
                observe(hashes, otherTypes);
            }

        }

        // EXPERIMENTAL seam (BcTlsOptions.ExperimentalNamedGroups): accept exactly the configured
        // key-exchange groups (e.g. ML-KEM) — a client offering none of them fails the handshake.
        public override int[] GetSupportedGroups()
            => _options.ExperimentalNamedGroups ?? base.GetSupportedGroups();

        public override TlsCredentials GetCredentials()
            => BcV2GTls.BuildSigner(_crypto,
                                    _options.OwnCredentials
                                        ?? throw new InvalidOperationException(
                                               "BcTlsOptions.OwnCredentials is required on the SECC/server side — " +
                                               "the server must present a certificate."),
                                    m_context,
                                    _options.Iso2Profile);

        public override CertificateRequest GetCertificateRequest()
            => _options.RequireClientCertificate
                   ? new CertificateRequest(TlsUtilities.EmptyBytes, BcV2GTls.AcceptedClientSignatureAlgorithms(_options), null, null)
                   : null!;

        public override void NotifyClientCertificate(Certificate clientCertificate)
            => BcV2GTls.ValidatePeer(clientCertificate, _options.ValidatePeerLeaf, AlertDescription.certificate_required, _options.ValidatePeerChain);
    }
}
