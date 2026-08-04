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
    /// Platform gate for the <b>macOS TLS 1.3 fallback</b>: .NET's macOS TLS layer sits on Apple's
    /// deprecated SecureTransport API, which never gained TLS 1.3, so <c>SslStream</c> throws
    /// <see cref="PlatformNotSupportedException"/> for <see cref="SslProtocols.Tls13"/> there
    /// (measured: <c>Tls13</c> alone throws, <c>Tls12</c> and <c>Tls12|Tls13</c> are accepted but
    /// negotiate 1.2). Since a TLS-1.2 "-20 session" would be silently non-conformant, the transport
    /// routes TLS-1.3-only sessions through the managed <see cref="BcTlsTransport"/> instead.
    /// <para>
    /// This translation is a <b>test/dev accommodation, not the -20-faithful path</b>: it carries the
    /// .NET backend's P-256 certificates onto a stack that otherwise pins secp521r1/Ed448, so it widens
    /// the accepted client-certificate signature schemes accordingly. For real -20 conformance use
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

        /// <summary>True when the requested protocol set is TLS-1.3-only and <c>SslStream</c> cannot serve it.</summary>
        public static bool NeedsBouncyCastleFallback(TlsOptions tls)
            => !SslStreamSupportsTls13 && tls.EnabledSslProtocols == SslProtocols.Tls13;

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
                    $"TlsOptions.CipherSuites requests [{string.Join(", ", requested)}], but a TLS 1.3 session on " +
                    "this platform runs on the BouncyCastle backend, which pins ISO 15118-20's " +
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
