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

namespace Vanaheimr.V2G.Simulation.Cli
{
    /// <summary>Which TLS stack to use: .NET's <c>SslStream</c> (Schannel/OpenSSL) or BouncyCastle.</summary>
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
