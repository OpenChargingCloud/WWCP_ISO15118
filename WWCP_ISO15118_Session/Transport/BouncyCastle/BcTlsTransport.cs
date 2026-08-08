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

using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace Vanaheimr.V2G.Simulation.Transport.BouncyCastle
{
    /// <summary>
    /// Wraps a byte <see cref="Stream"/> (e.g. a <c>NetworkStream</c>) in a BouncyCastle TLS session and
    /// returns the authenticated application-data <see cref="Stream"/> — the managed, cross-platform
    /// counterpart to <c>SslStream</c>. The BouncyCastle handshake is synchronous, so it is run on a
    /// worker thread. <see cref="Framing.V2GTPStream"/> only ever sees a <see cref="Stream"/>, so this
    /// backend is transparent to framing exactly like the .NET path.
    /// </summary>
    public static class BcTlsTransport
    {
        public static async Task<Stream> AuthenticateServerAsync(Stream inner, BcTlsOptions options, CancellationToken ct = default)
        {
            var protocol = new TlsServerProtocol(inner);
            var server   = new BcV2GTlsServer(new BcTlsCrypto(new SecureRandom()), options);
            await Task.Run(() => protocol.Accept(server), ct).ConfigureAwait(false);
            return protocol.Stream;
        }

        public static async Task<Stream> AuthenticateClientAsync(Stream inner, BcTlsOptions options, CancellationToken ct = default)
        {
            var protocol = new TlsClientProtocol(inner);
            var client   = new BcV2GTlsClient(new BcTlsCrypto(new SecureRandom()), options);
            await Task.Run(() => protocol.Connect(client), ct).ConfigureAwait(false);
            return protocol.Stream;
        }
    }
}
