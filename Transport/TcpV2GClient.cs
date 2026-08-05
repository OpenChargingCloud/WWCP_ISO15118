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

using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

using Vanaheimr.V2G.Simulation.Transport.BouncyCastle;

namespace Vanaheimr.V2G.Simulation.Transport
{
    /// <summary>
    /// EVCC-side TCP connect to a fixed host:port (no SDP discovery — out of scope for this slice).
    /// Returns a <see cref="Stream"/> — plain, or TLS-authenticated against <paramref name="host"/> if
    /// <paramref name="tls"/> is given. When <see cref="TlsOptions.ClientCertificate"/> is set, the EVCC
    /// presents it for mutual TLS (the Vehicle certificate).
    /// </summary>
    public static class TcpV2GClient
    {
        public static async Task<Stream> ConnectAsync(string host, int port, TlsOptions? tls = null, CancellationToken ct = default)
        {
            var client = new TcpClient();
            await client.ConnectAsync(host, port, ct).ConfigureAwait(false);
            Stream stream = client.GetStream(); // owns the underlying socket; disposing the stream closes it

            if (tls is null) return stream;

            // Either the caller asked for the managed stack (TlsOptions.Backend / V2G_TLS_BACKEND — the way a
            // -20 mutual-TLS client presents a test-PKI chain on Windows, which Schannel refuses to do), or
            // this platform's SslStream cannot do TLS 1.3 at all and a 1.3-only session would otherwise be
            // silently downgraded to a non-conformant 1.2 (macOS). See TlsPlatform.
            if (TlsPlatform.ResolveBackend(tls) == TlsBackend.BouncyCastle)
                return await BcTlsTransport.AuthenticateClientAsync(stream, TlsPlatform.ToBcClientOptions(tls), ct).ConfigureAwait(false);

            var ssl = new SslStream(stream, leaveInnerStreamOpen: false, tls.ServerCertificateValidation);
            var options = new SslClientAuthenticationOptions
            {
                TargetHost = host,
                EnabledSslProtocols = tls.EnabledSslProtocols,
                CipherSuitesPolicy  = TlsPlatform.CipherSuitesPolicyFor(tls),
            };
            if (tls.ClientCertificate is { } clientCert)
            {
                if (tls.ClientCertificateChain is { Count: > 0 } chain)
                    // A chain was supplied: use a certificate context so SslStream actually transmits those
                    // intermediates, letting a root-only SECC build the path to its trust anchor.
                    options.ClientCertificateContext = SslStreamCertificateContext.Create(clientCert, chain);
                else
                    // No chain to send, so don't ask for a context. Create() builds a chain over the leaf
                    // internally against the *platform* trust store (it has no custom-trust hook), which
                    // fails for any certificate whose issuer the machine does not know — a self-signed test
                    // CA, or an EV whose OEM root is not installed locally. ClientCertificates just presents
                    // the leaf and leaves path building to the peer, which is what the -2/-20 model expects.
                    options.ClientCertificates = [clientCert];
            }

            await ssl.AuthenticateAsClientAsync(options, ct).ConfigureAwait(false);
            return ssl;
        }

        /// <summary>
        /// EVCC-side connect using the <b>BouncyCastle</b> TLS backend (the -20-faithful P-521 / Ed448
        /// profile Schannel can't do). Overload selected by passing <see cref="BcTlsOptions"/>.
        /// </summary>
        public static async Task<Stream> ConnectAsync(string host, int port, BcTlsOptions bcTls, CancellationToken ct = default)
        {
            var client = new TcpClient();
            await client.ConnectAsync(host, port, ct).ConfigureAwait(false);
            return await BcTlsTransport.AuthenticateClientAsync(client.GetStream(), bcTls, ct).ConfigureAwait(false);
        }
    }
}
