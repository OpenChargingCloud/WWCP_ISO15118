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

namespace Vanaheimr.V2G.Simulation.Transport
{
    /// <summary>
    /// Which TLS implementation carries a <see cref="TlsOptions"/>-configured session. The two backends
    /// are not interchangeable — they differ in what they can be held to and in what they demand of the
    /// certificate material — so the choice is stated, not guessed (<c>docs/pki-model.md</c>).
    /// </summary>
    public enum TlsBackend
    {

        /// <summary>
        /// Let <see cref="TlsPlatform.ResolveBackend"/> pick per platform capability: <see cref="SslStream"/>
        /// everywhere except where its TLS 1.3 is missing (macOS), which routes TLS-1.3-only sessions to
        /// <see cref="BouncyCastle"/> rather than letting them downgrade to a non-conformant 1.2.
        /// <para>
        /// Capability only — <b>not</b> profile fidelity. Where <c>SslStream</c> merely negotiates something
        /// weaker or refuses a certificate the standard allows, Auto still picks it; say
        /// <see cref="BouncyCastle"/> to get the -20-faithful stack.
        /// </para>
        /// </summary>
        Auto,

        /// <summary>
        /// .NET's <c>SslStream</c> — platform-native (Schannel on Windows, OpenSSL on Linux,
        /// SecureTransport on macOS), and the only backend that can use a private key it never sees
        /// (Windows certificate store, HSM). Its limits are the platform's: no per-connection suite
        /// pinning on Windows, no secp521r1 on Schannel, no TLS 1.3 on macOS, and client certificates
        /// whose chain the OS trust store does not accept are refused before they reach the wire.
        /// </summary>
        SslStream,

        /// <summary>
        /// The managed BouncyCastle stack (<c>Transport/BouncyCastle/</c>) — TLS 1.3 and ISO 15118-20's
        /// cipher suites by construction, secp521r1/Ed448 certificates, and no OS trust store anywhere
        /// in the path, so a test-PKI chain is presented and judged on its own merits.
        /// <para>
        /// It signs in managed code, so it needs the leaf's private key in process: import PKCS#12 with
        /// <c>X509KeyStorageFlags.Exportable</c>. A non-exportable key, or an RSA/Ed448
        /// <c>X509Certificate2</c>, fails loudly in <c>BcCredentialBridge</c> rather than degrading.
        /// </para>
        /// </summary>
        BouncyCastle

    }
}
