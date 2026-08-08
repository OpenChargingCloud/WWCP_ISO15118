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

namespace cloud.charging.open.protocols.ISO15118.SharedCC
{
    /// <summary>Which TLS stack a run uses: none, .NET's <c>SslStream</c>, or BouncyCastle.</summary>
    /// <remarks>
    /// <para>
    /// Not <see cref="Transport.TlsBackend"/>. This one is the *setup* choice a command line makes and
    /// includes <see cref="None"/>; picking <see cref="BouncyCastle"/> here means configuring
    /// <c>BcTlsOptions</c> directly, bypassing <c>TlsOptions</c>. The transport enum answers a narrower
    /// question — which implementation carries an already-configured <c>TlsOptions</c> session — and is
    /// how a <see cref="Dotnet"/> session reaches the managed stack.
    /// </para>
    /// <para>
    /// <b>When each is the right answer.</b> <see cref="Dotnet"/> is fast and native and fine for most
    /// runs. <see cref="BouncyCastle"/> exists because two platforms cannot serve the ISO 15118-20 TLS
    /// profile through <c>SslStream</c> at all: macOS has no TLS 1.3 there (Apple's SecureTransport never
    /// gained it), and Windows Schannel does TLS 1.3 but cannot use secp521r1 certificates, cannot pin
    /// cipher suites per connection, and refuses to present a client chain whose root the machine does not
    /// trust. All three are what -20 asks for. See <c>Transport/TlsPlatform.cs</c> for the measurements.
    /// </para>
    /// </remarks>
    public enum TlsStack
    {
        /// <summary>No TLS — plain TCP.</summary>
        None,

        /// <summary>.NET <c>SslStream</c>: fast, platform-native, and on Windows and macOS unable to carry
        /// the -20 profile — see the remarks above.</summary>
        Dotnet,

        /// <summary>BouncyCastle: managed, cross-platform, and the only one here that runs the -20-faithful
        /// profile (TLS 1.3, secp521r1 or Ed448, pinned suites, mutual).</summary>
        BouncyCastle,
    }
}
