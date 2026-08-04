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

namespace Vanaheimr.V2G.Simulation.Transport
{
    /// <summary>
    /// The cipher suites each ISO 15118 protocol's TLS profile pins, per <c>docs/pki-model.md</c>. Constants
    /// only — a caller still states its profile explicitly on <see cref="TlsOptions"/>, because the guide's
    /// core lesson is that a library default is exactly what lets the wrong profile through unnoticed.
    /// <para>
    /// <c>BcV2GTls.CipherSuites</c> derives its pinned pair from <see cref="Iso20CipherSuites"/> by casting to
    /// the IANA code points, so the .NET and BouncyCastle backends cannot drift apart. What
    /// <see cref="TlsPlatform"/> checks is a different thing: that a caller asking for a pin the BouncyCastle
    /// fallback cannot honour is refused rather than silently served with different suites.
    /// </para>
    /// </summary>
    public static class TlsProfiles
    {
        /// <summary>ISO 15118-2: TLS 1.2 with AES-128-CBC-SHA256, static or ephemeral ECDH over secp256r1.</summary>
        public static readonly TlsCipherSuite[] Iso2CipherSuites =
        {
            TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256,
            TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA256,
        };

        /// <summary>ISO 15118-20: TLS 1.3 with the two AEAD suites.</summary>
        public static readonly TlsCipherSuite[] Iso20CipherSuites =
        {
            TlsCipherSuite.TLS_AES_256_GCM_SHA384,
            TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256,
        };
    }
}
