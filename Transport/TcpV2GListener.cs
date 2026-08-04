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

using System.Net;
using System.Net.Security;
using System.Net.Sockets;

using Vanaheimr.V2G.Simulation.Transport.BouncyCastle;

namespace Vanaheimr.V2G.Simulation.Transport
{
    /// <summary>
    /// SECC-side TCP listener: binds a fixed endpoint (no SDP discovery — out of scope for this slice)
    /// and hands back a <see cref="Stream"/> per accepted connection — a plain <see cref="NetworkStream"/>,
    /// or an authenticated <see cref="SslStream"/> if <paramref name="tls"/> is given. Either way,
    /// <see cref="Framing.V2GTPStream"/> only ever sees a <see cref="Stream"/>, so TLS is transparent to
    /// framing by construction.
    /// </summary>
    public sealed class TcpV2GListener : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly TlsOptions? _tls;
        private readonly BcTlsOptions? _bcTls;

        /// <param name="endpoint">Pass port 0 to let the OS assign a free port — read it back via <see cref="LocalEndpoint"/>.</param>
        /// <param name="tls">When given, <see cref="AcceptAsync"/> authenticates as a TLS server before returning.</param>
        public TcpV2GListener(IPEndPoint endpoint, TlsOptions? tls = null)
        {
            if (tls is { ServerCertificate: null })
                throw new ArgumentException("TlsOptions.ServerCertificate is required for the SECC/listener side.", nameof(tls));

            _listener = new TcpListener(endpoint);
            EnableDualStack(_listener, endpoint);
            _tls = tls;
            _listener.Start();
        }

        /// <summary>SECC listener using the <b>BouncyCastle</b> TLS backend (the -20-faithful P-521 / Ed448 profile).</summary>
        public TcpV2GListener(IPEndPoint endpoint, BcTlsOptions bcTls)
        {
            _listener = new TcpListener(endpoint);
            EnableDualStack(_listener, endpoint);
            _bcTls = bcTls;
            _listener.Start();
        }

        // Binding [::] as dual-stack lets the SECC accept both IPv4 (loopback tests) and IPv6 connections —
        // the latter is what a real ISO 15118 EVCC (e.g. Josev over an fe80::…%eth0 link-local) uses.
        private static void EnableDualStack(TcpListener listener, IPEndPoint endpoint)
        {
            if (endpoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 &&
                endpoint.Address.Equals(IPAddress.IPv6Any))
                listener.Server.DualMode = true;
        }

        public IPEndPoint LocalEndpoint => (IPEndPoint)_listener.LocalEndpoint;

        /// <summary>Accepts the next incoming connection and returns its stream (plain, or TLS-authenticated if configured).</summary>
        /// <summary>
        /// An optional admission gate, consulted with the peer's address <b>before</b> anything else
        /// happens on the connection. Returning false closes it immediately.
        /// </summary>
        /// <remarks>
        /// Null by default, so nothing changes for existing callers. It exists for the pairing
        /// check (EVSimulatorApp <c>docs/CONCEPT.md</c> §4.6 Tier 1), which gates the accept rather
        /// than the session: that slot is where SLAC sits in a real deployment, it works identically
        /// for -2 and -20, and it needs no schema deviation. Checking here rather than after the
        /// handshake also means an unadmitted peer cannot make the station do public-key work — the
        /// cheapest denial of service a gated port otherwise offers.
        /// <para>
        /// The policy itself deliberately lives with the caller. This class knows about sockets; who
        /// is allowed to open one is not its business.
        /// </para>
        /// </remarks>
        public Func<IPEndPoint, bool>? Admit { get; init; }

        public async Task<Stream> AcceptAsync(CancellationToken ct = default)
        {
            TcpClient client;
            while (true)
            {
                client = await _listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                if (Admit is null || client.Client.RemoteEndPoint is not IPEndPoint peer || Admit(peer))
                    break;
                client.Dispose();   // refused: no handshake, no session, no reply
            }

            Stream stream = client.GetStream(); // owns the underlying socket; disposing the stream closes it

            if (_bcTls is not null)
                return await BcTlsTransport.AuthenticateServerAsync(stream, _bcTls, ct).ConfigureAwait(false);

            if (_tls is null) return stream;

            // macOS: see the matching note in TcpV2GClient — TLS-1.3-only sessions run on BouncyCastle.
            if (TlsPlatform.NeedsBouncyCastleFallback(_tls))
                return await BcTlsTransport.AuthenticateServerAsync(stream, TlsPlatform.ToBcServerOptions(_tls), ct).ConfigureAwait(false);

            var ssl = new SslStream(stream, leaveInnerStreamOpen: false,
                userCertificateValidationCallback: _tls.ClientCertificateValidation);
            var serverOptions = new SslServerAuthenticationOptions
            {
                EnabledSslProtocols = _tls.EnabledSslProtocols,
                CipherSuitesPolicy  = TlsPlatform.CipherSuitesPolicyFor(_tls),
                ClientCertificateRequired = _tls.RequireClientCertificate, // mutual TLS for ISO 15118-20
            };
            // Use a certificate context (not just ServerCertificate) so SslStream sends the intermediate chain,
            // letting a root-only EVCC build the path to its trust anchor — mirrors the client-side fix.
            if (_tls.ServerCertificateChain is not null)
                serverOptions.ServerCertificateContext = SslStreamCertificateContext.Create(_tls.ServerCertificate!, _tls.ServerCertificateChain);
            else
                serverOptions.ServerCertificate = _tls.ServerCertificate;
            await ssl.AuthenticateAsServerAsync(serverOptions, ct).ConfigureAwait(false);
            return ssl;
        }

        public void Dispose() => _listener.Stop();
    }
}
