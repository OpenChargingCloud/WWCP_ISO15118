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

using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace Vanaheimr.V2G.Simulation.Transport.BouncyCastle
{
    /// <summary>
    /// EVCC-side BouncyCastle TLS client: TLS 1.3 with the -20 cipher suites, validates the SECC server
    /// certificate, and presents the Vehicle client certificate when the SECC requests one (mutual TLS).
    /// </summary>
    internal sealed class BcV2GTlsClient : DefaultTlsClient
    {
        private readonly BcTlsCrypto _crypto;
        private readonly BcTlsOptions _options;

        public BcV2GTlsClient(BcTlsCrypto crypto, BcTlsOptions options) : base(crypto)
        {
            _crypto  = crypto;
            _options = options;
        }

        public override ProtocolVersion[] GetProtocolVersions() => BcV2GTls.Tls13Only;

        protected override int[] GetSupportedCipherSuites() => BcV2GTls.CipherSuites;

        // EXPERIMENTAL seam (BcTlsOptions.ExperimentalNamedGroups): offer exactly the configured
        // key-exchange groups (e.g. ML-KEM) instead of BouncyCastle's default list, and send the
        // TLS 1.3 initial key_share for the first of them (avoids a HelloRetryRequest round-trip).
        protected override IList<int> GetSupportedGroups(IList<int> namedGroupRoles)
            => _options.ExperimentalNamedGroups?.ToList() ?? base.GetSupportedGroups(namedGroupRoles);

        public override IList<int> GetEarlyKeyShareGroups()
            => _options.ExperimentalNamedGroups is { Length: > 0 } groups
                   ? new List<int> { groups[0] }
                   : base.GetEarlyKeyShareGroups();

        public override TlsAuthentication GetAuthentication()
            => new V2GAuthentication(_crypto, _options, () => m_context);

        private sealed class V2GAuthentication : TlsAuthentication
        {
            private readonly BcTlsCrypto _crypto;
            private readonly BcTlsOptions _options;
            private readonly Func<TlsContext> _context;

            public V2GAuthentication(BcTlsCrypto crypto, BcTlsOptions options, Func<TlsContext> context)
            {
                _crypto  = crypto;
                _options = options;
                _context = context;
            }

            public void NotifyServerCertificate(TlsServerCertificate serverCertificate)
                => BcV2GTls.ValidatePeer(serverCertificate?.Certificate, _options.ValidatePeerLeaf, AlertDescription.bad_certificate);

            // Null credentials decline client authentication — the unilateral-TLS case. BouncyCastle then
            // sends an empty Certificate message, which a SECC requiring one rejects.
            public TlsCredentials GetClientCredentials(CertificateRequest certificateRequest)
                => _options.OwnCredentials is { } own
                       ? BcV2GTls.BuildSigner(_crypto, own, _context())
                       : null!;
        }
    }
}
