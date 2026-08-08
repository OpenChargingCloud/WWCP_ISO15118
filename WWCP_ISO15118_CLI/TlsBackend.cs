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

namespace Vanaheimr.V2G.Simulation.Cli
{
    /// <summary>Which TLS stack to use: .NET's <c>SslStream</c> (Schannel/OpenSSL) or BouncyCastle.</summary>
    /// <remarks>
    /// Not <see cref="Transport.TlsBackend"/>, which it shadows inside this namespace. This one is the CLI's
    /// <i>setup</i> choice and includes <see cref="None"/>; picking <see cref="BouncyCastle"/> here means
    /// configuring <c>BcTlsOptions</c> straight from <c>--pki-dir</c>, bypassing <c>TlsOptions</c>. The
    /// transport enum answers a narrower question — which implementation carries an already-configured
    /// <c>TlsOptions</c> session — and is how a <see cref="Dotnet"/> session reaches the managed stack.
    /// </remarks>
    public enum TlsBackend
    {
        /// <summary>No TLS — plain TCP.</summary>
        None,

        /// <summary>.NET <c>SslStream</c>: fast, platform-native; a self-signed P-256 dev server cert.</summary>
        Dotnet,

        /// <summary>BouncyCastle: the -20-faithful profile (TLS 1.3, secp521r1, mutual TLS) — needs <c>--pki-dir</c>.</summary>
        BouncyCastle,
    }
}
