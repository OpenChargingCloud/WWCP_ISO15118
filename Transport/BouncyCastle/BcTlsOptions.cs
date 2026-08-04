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

namespace Vanaheimr.V2G.Simulation.Transport.BouncyCastle
{
    /// <summary>
    /// Configuration for the BouncyCastle TLS backend (<see cref="BcTlsTransport"/>) — the managed,
    /// platform-independent alternative to .NET's <c>SslStream</c>, used for the ISO 15118-20-faithful
    /// TLS profile (TLS 1.3, secp521r1 / Ed448) that Windows Schannel cannot do. See <c>docs/pki-model.md</c>.
    /// </summary>
    public sealed record BcTlsOptions
    {
        /// <summary>The certificate + key this side presents (SECC server cert, or EVCC Vehicle client cert).
        /// <b>Required on the SECC/server side.</b> May be null on the EVCC/client side for unilateral TLS —
        /// the ISO 15118-2 shape, where only the SECC authenticates; the client then declines the
        /// <c>CertificateRequest</c> (and a SECC with <see cref="RequireClientCertificate"/> rejects it).</summary>
        public BcTlsCredentials? OwnCredentials { get; init; }

        /// <summary>Validate the peer's leaf certificate (DER). Return false to abort the handshake. Null = accept any.</summary>
        public Func<byte[], bool>? ValidatePeerLeaf { get; init; }

        /// <summary>SECC side only: require a client certificate from the EVCC (mutual TLS). Ignored on the client.</summary>
        public bool RequireClientCertificate { get; init; }

        /// <summary>SECC side only: the signature schemes the server will accept for the EVCC's <i>client</i>
        /// certificate, i.e. what goes into the TLS 1.3 <c>CertificateRequest</c>. Null (default): ISO 15118-20's
        /// strict pair (<c>ecdsa_secp521r1_sha512</c>, <c>ed448</c>).
        /// <para>
        /// Setting this <b>deviates from the -20 TLS profile</b> and exists for the macOS TLS 1.3 fallback
        /// (<see cref="TlsPlatform"/>), which carries the .NET backend's deliberately off-profile P-256 test
        /// certificates (see <c>docs/pki-model.md</c>). Never set it on a -20 conformance path.
        /// </para></summary>
        public int[]? AcceptedClientSignatureSchemes { get; init; }

        /// <summary><b>EXPERIMENTAL</b> (see <c>Vanaheimr.V2G.Experiments.Pqc</c>): override the TLS
        /// named groups this side offers/accepts for the key exchange — e.g.
        /// <c>Org.BouncyCastle.Tls.NamedGroup.MLKEM1024</c> for a post-quantum ML-KEM (FIPS 203) key
        /// exchange. Setting this deviates from ISO 15118-20's TLS profile (which pins classical
        /// groups) — loopback experiments only, never wire-conformant. Null (default): BouncyCastle's
        /// standard list, i.e. the production behaviour, unchanged.</summary>
        public int[]? ExperimentalNamedGroups { get; init; }
    }
}
