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

namespace cloud.charging.open.protocols.ISO15118.Transport.BouncyCastle
{
    /// <summary>
    /// Configuration for the BouncyCastle TLS backend (<see cref="BcTlsTransport"/>) — the managed,
    /// platform-independent alternative to .NET's <c>SslStream</c>, used for the ISO 15118-20-faithful
    /// TLS profile (TLS 1.3, secp521r1 / Ed448) that Windows Schannel cannot do. See <c>EVSimulatorApp/docs/pki-model.md</c>.
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

        /// <summary>
        /// Validates the peer's whole certificate chain (leaf first, DER-encoded) rather than pinning one
        /// certificate. Runs after <see cref="ValidatePeerLeaf"/>, and both must pass when both are set.
        /// </summary>
        /// <remarks>
        /// Pinning and chaining answer different questions. <see cref="ValidatePeerLeaf"/> asks "is this the
        /// exact certificate I was told to expect", which is what a dev loopback can check and a stranger
        /// cannot satisfy. This asks "does this reach a root I trust", which is the question a real V2G
        /// deployment asks and the only one that works against a peer whose material we did not mint.
        /// </remarks>
        public Func<byte[][], bool>? ValidatePeerChain { get; init; }

        /// <summary>SECC side only: require a client certificate from the EVCC (mutual TLS). Ignored on the client.</summary>
        public bool RequireClientCertificate { get; init; }

        /// <summary>SECC side only: the signature schemes the server will accept for the EVCC's <i>client</i>
        /// certificate, i.e. what goes into the TLS 1.3 <c>CertificateRequest</c>. Null (default): ISO 15118-20's
        /// strict pair (<c>ecdsa_secp521r1_sha512</c>, <c>ed448</c>).
        /// <para>
        /// Setting this <b>deviates from the -20 TLS profile</b> and exists for the macOS TLS 1.3 fallback
        /// (<see cref="TlsPlatform"/>), which carries the .NET backend's deliberately off-profile P-256 test
        /// certificates (see <c>EVSimulatorApp/docs/pki-model.md</c>). Never set it on a -20 conformance path.
        /// </para></summary>
        public int[]? AcceptedClientSignatureSchemes { get; init; }

        /// <summary>
        /// Run ISO 15118-<b>2</b>'s transport instead of -20's: TLS <b>1.2</b> and the two `-2` cipher
        /// suites, rather than TLS 1.3 and the `-20` pair. Both sides must agree.
        /// </summary>
        /// <remarks>
        /// The BouncyCastle backend existed for the `-20` profile Schannel cannot do, so it was TLS
        /// 1.3-only. `-2` needs it too since 2026-08-16, for one reason: <see cref="TrustedCaKeys"/> is an
        /// RFC 6066 extension of the TLS 1.2 era, and <c>SslStream</c> cannot send arbitrary client
        /// extensions on any platform.
        /// </remarks>
        public bool Iso2Profile { get; init; }

        /// <summary>
        /// EVCC side: the V2G <b>root</b> certificates (DER) this car holds, sent as RFC 6066's
        /// <c>trusted_ca_keys</c> in the ClientHello. Null or empty omits the extension.
        /// </summary>
        /// <remarks>
        /// <para>
        /// `[V2G2-651]` makes this unconditional for every `-2` EV — *"shall send a list of V2G root
        /// certificates it possesses"* — and it is what `[V2G2-871]`'s duty on the station is answerable
        /// to: serve a chain up to one of the roots the car named. With a single root the two behaviours
        /// are indistinguishable, which is why the requirement bites exactly where an operator holds two.
        /// </para>
        /// <para>
        /// Until 2026-08-16 nothing in this stack could send it — <c>grep -rn trusted_ca_keys</c> matched
        /// nothing — so this project could file
        /// <c>docs/reports/everest-isomux.md</c> §4 about a station that disables it and never measure the
        /// failing case. A gap of ours that was also the instrument for one of theirs.
        /// </para>
        /// </remarks>
        public byte[][]? TrustedCaKeys { get; init; }

        /// <summary>SECC side: called with the <c>cert_sha1_hash</c> entries of a client's
        /// <c>trusted_ca_keys</c>, and how many entries of the other identifier types were skipped.
        /// Observation only — this station serves the chain it was configured with either way.</summary>
        /// <remarks>An extension a station receives and does not act on is invisible from both ends
        /// otherwise, which is the same reason <c>Secc20Base.MeterInfoRequestedByEv</c> exists.</remarks>
        public Action<IReadOnlyList<byte[]>, int>? OnTrustedCaKeys { get; init; }

        /// <summary><b>EXPERIMENTAL</b> (see <c>Vanaheimr.V2G.Experiments.Pqc</c>): override the TLS
        /// named groups this side offers/accepts for the key exchange — e.g.
        /// <c>Org.BouncyCastle.Tls.NamedGroup.MLKEM1024</c> for a post-quantum ML-KEM (FIPS 203) key
        /// exchange. Setting this deviates from ISO 15118-20's TLS profile (which pins classical
        /// groups) — loopback experiments only, never wire-conformant. Null (default): BouncyCastle's
        /// standard list, i.e. the production behaviour, unchanged.</summary>
        public int[]? ExperimentalNamedGroups { get; init; }
    }
}
