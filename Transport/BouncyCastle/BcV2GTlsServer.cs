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

namespace Vanaheimr.V2G.Simulation.Transport.BouncyCastle
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

        public override ProtocolVersion[] GetProtocolVersions() => BcV2GTls.Tls13Only;

        protected override int[] GetSupportedCipherSuites() => BcV2GTls.CipherSuites;

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
                                    m_context);

        public override CertificateRequest GetCertificateRequest()
            => _options.RequireClientCertificate
                   ? new CertificateRequest(TlsUtilities.EmptyBytes, BcV2GTls.AcceptedClientSignatureAlgorithms(_options), null, null)
                   : null!;

        public override void NotifyClientCertificate(Certificate clientCertificate)
            => BcV2GTls.ValidatePeer(clientCertificate, _options.ValidatePeerLeaf, AlertDescription.certificate_required);
    }
}
