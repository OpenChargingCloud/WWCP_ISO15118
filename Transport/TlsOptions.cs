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

using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Vanaheimr.V2G.Simulation.Transport
{
    /// <summary>
    /// TLS knobs for <see cref="TcpV2GListener"/>/<see cref="TcpV2GClient"/>. Supports both server-side TLS
    /// (ISO 15118-2) and <b>mutual TLS</b> (ISO 15118-20): the SECC presents its server certificate and,
    /// when <see cref="RequireClientCertificate"/> is set, requires and validates the EVCC's TLS client
    /// certificate (the CharIN "Vehicle" certificate) — see <c>docs/pki-model.md</c>.
    /// <para>
    /// <b>Known gap:</b> ISO 15118-20's TLS profile pins specific cipher suites/curves (TLS 1.3,
    /// <c>TLS_AES_256_GCM_SHA384</c>/<c>TLS_CHACHA20_POLY1305_SHA256</c>, secp521r1); this uses
    /// <see cref="SslProtocols.Tls13"/> with whatever cipher suites .NET/Schannel negotiate by default
    /// rather than enforcing the spec's exact list.
    /// </para>
    /// </summary>
    public sealed record TlsOptions
    {
        /// <summary>SECC side: the certificate to present. Required when TLS is enabled on the listener.</summary>
        public X509Certificate2? ServerCertificate { get; init; }

        /// <summary>SECC side: the intermediate CA certificates to send alongside <see cref="ServerCertificate"/> so
        /// the EVCC can build the chain to its trust anchor (e.g. a Josev EVCC validating our SECC leaf against the
        /// V2G root needs the CPO Sub-CAs). Without these, <c>SslStream</c> sends only the leaf. Null = leaf only.</summary>
        public X509Certificate2Collection? ServerCertificateChain { get; init; }

        /// <summary>EVCC side: how to validate the SECC's certificate. Defaults to the platform's normal chain validation if not set.</summary>
        public RemoteCertificateValidationCallback? ServerCertificateValidation { get; init; }

        /// <summary>EVCC side: the TLS client certificate to present for mutual TLS (the Vehicle certificate). Null = no client cert.</summary>
        public X509Certificate2? ClientCertificate { get; init; }

        /// <summary>EVCC side: the intermediate CA certificates to send alongside <see cref="ClientCertificate"/> so the
        /// SECC can build the chain to its trust anchor. Without these, <c>SslStream</c> sends only the leaf and a peer
        /// that holds only the root (e.g. Josev, which loads just the OEM root) can't verify the client. Null = leaf only.</summary>
        public X509Certificate2Collection? ClientCertificateChain { get; init; }

        /// <summary>SECC side: require a TLS client certificate from the EVCC (mutual TLS). Off for plain server-side TLS.</summary>
        public bool RequireClientCertificate { get; init; }

        /// <summary>SECC side: how to validate the EVCC's client certificate. Defaults to the platform's normal chain validation if not set.</summary>
        public RemoteCertificateValidationCallback? ClientCertificateValidation { get; init; }

        /// <summary>The TLS version(s) this session may negotiate. <b>Required, deliberately without a default:</b>
        /// the version belongs to the protocol's profile — <c>docs/pki-model.md</c> pins the mapping strictly to
        /// -2 ↔ TLS 1.2 and -20 ↔ TLS 1.3 — so no single value can be right for both, and a library default is
        /// exactly what the guide warns against. This used to default to <see cref="SslProtocols.Tls13"/>, which
        /// silently ran ISO 15118-2 sessions over TLS 1.3, the one combination the document rules out.
        /// <para>
        /// A permissive set (e.g. <c>Tls12 | Tls13</c>) is a deliberate interop choice, not a fallback: on a
        /// platform whose <c>SslStream</c> caps out below the top of the range it simply negotiates the lower
        /// version, which for -20 means a non-conformant session. Verify what was negotiated rather than
        /// assuming (<c>E2E/TlsAssert.cs</c>).
        /// </para></summary>
        public required SslProtocols EnabledSslProtocols { get; init; }

        /// <summary>The cipher suites this session may negotiate — the other half of the protocol's TLS profile
        /// (<c>docs/pki-model.md</c>: version, suites, signature algorithms and curve are one coupled unit, and
        /// letting the stack auto-select is the documented failure mode). Null: whatever the platform prefers,
        /// which is <i>not</i> profile-conformant — measured on macOS, an unpinned ISO 15118-2 session at
        /// TLS 1.2 negotiates <c>TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384</c> instead of the profile's AES-128-CBC.
        /// <para>
        /// Applied via <see cref="CipherSuitesPolicy"/>, which .NET supports on macOS/Linux but <b>not</b> on
        /// Windows/Schannel (suites are configured system-wide there). Where it cannot be applied the transport
        /// leaves the suites unpinned rather than throwing — see
        /// <see cref="TlsPlatform.SupportsCipherSuitePinning"/>.
        /// </para></summary>
        public IReadOnlyList<TlsCipherSuite>? CipherSuites { get; init; }
    }
}
