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
using System.Runtime.Versioning;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

using Org.BouncyCastle.Tls;

using Vanaheimr.V2G.Simulation.Transport.BouncyCastle;

namespace Vanaheimr.V2G.Simulation.Transport
{
    /// <summary>
    /// Picks the TLS backend for a <see cref="TlsOptions"/>-configured session, and translates those
    /// options for the managed one.
    /// <para>
    /// <b>What the platform forces.</b> .NET's macOS TLS layer sits on Apple's deprecated SecureTransport
    /// API, which never gained TLS 1.3, so <c>SslStream</c> throws <see cref="PlatformNotSupportedException"/>
    /// for <see cref="SslProtocols.Tls13"/> there (measured: <c>Tls13</c> alone throws, <c>Tls12</c> and
    /// <c>Tls12|Tls13</c> are accepted but negotiate 1.2). Since a TLS-1.2 "-20 session" would be silently
    /// non-conformant, <see cref="TlsBackend.Auto"/> routes TLS-1.3-only sessions through the managed
    /// <see cref="BcTlsTransport"/> instead.
    /// </para>
    /// <para>
    /// <b>What the caller chooses.</b> Auto answers "can this platform serve the session at all", not "is
    /// this the -20-faithful stack" — Windows/Schannel, for one, does TLS 1.3 and so passes the capability
    /// test while still being unable to pin the suites, use secp521r1, or present a client chain rooted
    /// outside its trust store. Those sessions ask for <see cref="TlsBackend.BouncyCastle"/> explicitly, via
    /// <see cref="TlsOptions.Backend"/> or the <see cref="BackendEnvironmentVariable"/> knob. Auto is
    /// deliberately not widened to make that choice on the caller's behalf: the managed backend needs the
    /// leaf's private key in process, so an <c>SslStream</c> session running on a store-held or RSA key —
    /// which works today — would break if it were silently rerouted.
    /// </para>
    /// <para>
    /// Translation onto the managed backend is a <b>test/dev accommodation, not the -20-faithful path</b>:
    /// it carries the .NET backend's P-256 certificates onto a stack that otherwise pins secp521r1/Ed448, so
    /// it widens the accepted client-certificate signature schemes accordingly. For real -20 conformance use
    /// <see cref="BcTlsOptions"/> directly with the PKI builder's strict-20 chains (see
    /// <c>docs/pki-model.md</c>).
    /// </para>
    /// </summary>
    public static class TlsPlatform
    {
        /// <summary>Whether this platform's <c>SslStream</c> can do TLS 1.3 at all.</summary>
        public static bool SslStreamSupportsTls13 => !OperatingSystem.IsMacOS();

        /// <summary>
        /// Whether <see cref="CipherSuitesPolicy"/> can pin the cipher suites of a single connection. Windows
        /// is the exception: Schannel takes its suite list from system-wide policy, and .NET throws
        /// <see cref="PlatformNotSupportedException"/> for a per-connection policy there. On such a platform
        /// <see cref="TlsOptions.CipherSuites"/> cannot be enforced and the deviation has to be recorded
        /// instead of pinned (<c>docs/pki-model.md</c> asks for exactly that).
        /// </summary>
        // The guard attribute lets the platform-compatibility analyser (CA1416) see that a true value rules
        // Windows out, so callers may touch CipherSuitesPolicy behind this check without a suppression.
        [UnsupportedOSPlatformGuard("windows")]
        public static bool SupportsCipherSuitePinning => !OperatingSystem.IsWindows();

        /// <summary>
        /// The <see cref="CipherSuitesPolicy"/> for these options, or null when nothing is pinned or the
        /// platform cannot honour it.
        /// </summary>
        internal static CipherSuitesPolicy? CipherSuitesPolicyFor(TlsOptions tls)
            => tls.CipherSuites is { Count: > 0 } suites && SupportsCipherSuitePinning
                   ? new CipherSuitesPolicy(suites)
                   : null;

        /// <summary>
        /// The environment variable that selects a backend for sessions which left
        /// <see cref="TlsOptions.Backend"/> at <see cref="TlsBackend.Auto"/> — the knob for an interop run
        /// driving a binary whose <see cref="TlsOptions"/> it cannot edit. Values are the
        /// <see cref="TlsBackend"/> names, case-insensitively; anything else throws rather than being
        /// ignored, because a typo that silently left the session on the platform stack is exactly the
        /// failure this exists to avoid.
        /// </summary>
        public const string BackendEnvironmentVariable = "V2G_TLS_BACKEND";

        /// <summary>The backend named by <see cref="BackendEnvironmentVariable"/>, or null when unset.</summary>
        public static TlsBackend? BackendFromEnvironment()
            => ParseBackend(Environment.GetEnvironmentVariable(BackendEnvironmentVariable));

        /// <summary>
        /// Parses a <see cref="TlsBackend"/> name (case-insensitive, surrounding whitespace ignored) — for the
        /// environment knob and for a CLI flag. Null or blank yields null, meaning "nothing requested"; an
        /// unrecognised name throws <see cref="ArgumentException"/>.
        /// </summary>
        public static TlsBackend? ParseBackend(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (Enum.TryParse<TlsBackend>(value.Trim(), ignoreCase: true, out var backend) &&
                Enum.IsDefined(backend))
                return backend;

            throw new ArgumentException(
                $"'{value}' is not a TLS backend. Expected one of " +
                $"{string.Join(", ", Enum.GetNames<TlsBackend>())}.", nameof(value));
        }

        /// <summary>
        /// The backend this session runs on — never <see cref="TlsBackend.Auto"/>. An explicit
        /// <see cref="TlsOptions.Backend"/> wins over <see cref="BackendEnvironmentVariable"/>, which wins over
        /// the platform default; a request the backend cannot honour throws here rather than at handshake time.
        /// </summary>
        public static TlsBackend ResolveBackend(TlsOptions tls)
        {
            ArgumentNullException.ThrowIfNull(tls);

            var requested = tls.Backend != TlsBackend.Auto
                                ? tls.Backend
                                : BackendFromEnvironment() ?? TlsBackend.Auto;

            switch (requested)
            {

                // BcV2GTlsClient/Server offer TLS 1.3 and nothing else, so a protocol set without it would be
                // answered with a version the caller did not ask for — refuse instead. Tls12|Tls13 is fine:
                // 1.3 is inside the requested set, which is what a permissive set means.
                case TlsBackend.BouncyCastle when (tls.EnabledSslProtocols & SslProtocols.Tls13) == 0:
                    throw new NotSupportedException(
                        $"TlsOptions.EnabledSslProtocols is {tls.EnabledSslProtocols}, but the BouncyCastle " +
                        "backend speaks TLS 1.3 only. Include Tls13, or use the SslStream backend.");

                case TlsBackend.BouncyCastle:
                    return TlsBackend.BouncyCastle;

                case TlsBackend.SslStream when !SslStreamSupportsTls13 && tls.EnabledSslProtocols == SslProtocols.Tls13:
                    throw new PlatformNotSupportedException(
                        "The SslStream backend was requested for a TLS-1.3-only session, but this platform's " +
                        "SslStream cannot do TLS 1.3 (macOS/SecureTransport). Use the BouncyCastle backend, or " +
                        "leave TlsOptions.Backend at Auto, which selects it here.");

                case TlsBackend.SslStream:
                    return TlsBackend.SslStream;

                default:
                    return !SslStreamSupportsTls13 && tls.EnabledSslProtocols == SslProtocols.Tls13
                               ? TlsBackend.BouncyCastle
                               : TlsBackend.SslStream;

            }
        }

        // The fallback carries whatever curve the .NET-side test certificates use, so the SECC must be
        // willing to request more than the -20 pair — otherwise a P-256 Vehicle certificate cannot be
        // offered at all. Explicitly a deviation, confined to this path.
        private static readonly int[] FallbackClientSignatureSchemes =
        {
            SignatureScheme.ecdsa_secp521r1_sha512,
            SignatureScheme.ed448,
            SignatureScheme.ecdsa_secp384r1_sha384,
            SignatureScheme.ecdsa_secp256r1_sha256,
        };

        // The BouncyCastle backend pins its own cipher suites (BcV2GTls.CipherSuites) and has no equivalent of
        // CipherSuitesPolicy, so a TlsOptions.CipherSuites list cannot be carried across. Silently dropping it
        // would mean the fallback negotiates suites the caller did not ask for, so anything other than the -20
        // pair is refused outright rather than quietly ignored.
        private static void EnsureSuitesMatchBackend(TlsOptions tls)
        {
            if (tls.CipherSuites is not { Count: > 0 } requested)
                return;

            if (!requested.Order().SequenceEqual(TlsProfiles.Iso20CipherSuites.Order()))
                throw new NotSupportedException(
                    $"TlsOptions.CipherSuites requests [{string.Join(", ", requested)}], but this session runs on " +
                    "the BouncyCastle backend, which pins ISO 15118-20's " +
                    $"[{string.Join(", ", TlsProfiles.Iso20CipherSuites)}] and cannot negotiate anything else. " +
                    "Request the -20 suites, or drop the pin.");
        }

        /// <summary>EVCC side: translate client-side <see cref="TlsOptions"/> into <see cref="BcTlsOptions"/>.</summary>
        public static BcTlsOptions ToBcClientOptions(TlsOptions tls)
        {
            EnsureSuitesMatchBackend(tls);

            // No client certificate = unilateral TLS; BouncyCastle then declines the CertificateRequest.
            return new BcTlsOptions
            {
                OwnCredentials   = tls.ClientCertificate is { } leaf
                                       ? BcCredentialBridge.FromX509(leaf, tls.ClientCertificateChain)
                                       : null,
                ValidatePeerLeaf = Adapt(tls.ServerCertificateValidation),
            };
        }

        /// <summary>SECC side: translate server-side <see cref="TlsOptions"/> into <see cref="BcTlsOptions"/>.</summary>
        public static BcTlsOptions ToBcServerOptions(TlsOptions tls)
        {
            EnsureSuitesMatchBackend(tls);

            if (tls.ServerCertificate is null)
                throw new ArgumentException("TlsOptions.ServerCertificate is required for the SECC/listener side.", nameof(tls));

            return new BcTlsOptions
            {
                OwnCredentials                 = BcCredentialBridge.FromX509(tls.ServerCertificate, tls.ServerCertificateChain),
                ValidatePeerLeaf               = Adapt(tls.ClientCertificateValidation),
                RequireClientCertificate       = tls.RequireClientCertificate,
                AcceptedClientSignatureSchemes = FallbackClientSignatureSchemes,
            };
        }

        // BouncyCastle hands us the peer's leaf as DER and performs no platform chain build, so the
        // callback is invoked with no X509Chain and SslPolicyErrors.None — it must do its own checking
        // (the loopback tests compare thumbprints, which is exactly that). A callback that relies on the
        // platform having pre-validated the chain would be weaker here than on the SslStream path.
        private static Func<byte[], bool>? Adapt(RemoteCertificateValidationCallback? validate)
            => validate is null
                   ? null
                   : der => validate(sender: null!, X509CertificateLoader.LoadCertificate(der), chain: null, SslPolicyErrors.None);
    }
}
