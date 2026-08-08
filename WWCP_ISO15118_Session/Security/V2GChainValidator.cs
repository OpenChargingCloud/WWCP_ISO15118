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

using System.Security.Cryptography.X509Certificates;

namespace cloud.charging.open.protocols.ISO15118.Security
{

    /// <summary>What validating one chain came to.</summary>
    /// <param name="Ok">Whether the leaf chains to one of the configured roots and every certificate on the
    /// way was acceptable.</param>
    /// <param name="Reason">Why not, when <paramref name="Ok"/> is false — the platform's own chain-status
    /// text, which names the actual defect (expired, untrusted root, bad signature) rather than just failing.</param>
    /// <param name="Anchor">The subject of the root it chained to, when it did. Useful when several roots are
    /// configured and the interesting question is *which* one accepted this peer.</param>
    public sealed record ChainResult(bool Ok, string Reason, string? Anchor)
    {
        public static ChainResult Trusted(string anchor) => new(true, "ok", anchor);
        public static ChainResult Rejected(string reason) => new(false, reason, null);

        /// <summary>No roots were configured, so nothing was checked. Distinct from a failure on purpose:
        /// "not checked" and "checked and bad" must never read the same in a conformance run.</summary>
        public static readonly ChainResult NotConfigured = new(false, "no trust roots configured — chain not validated", null);
    }

    /// <summary>
    /// Validates an ISO 15118 certificate chain against a set of V2G root certificates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The chains this sees arrive as DER blobs inside V2G messages — a leaf plus its <c>SubCertificates</c>
    /// in issuer order — or as a TLS peer's chain. Either way the question is the same: does this reach one
    /// of the roots we were told to trust, and was every hop valid on the way.
    /// </para>
    /// <para>
    /// <b>Custom trust, never the machine's.</b> A V2G root is not in any operating system's store and must
    /// not be: accepting the platform's anchors here would mean any public CA could mint a contract
    /// certificate. So the chain is built with <see cref="X509ChainTrustMode.CustomRootTrust"/> over exactly
    /// the roots handed in, and <see cref="X509VerificationFlags.AllowUnknownCertificateAuthority"/> is
    /// deliberately <i>not</i> set — an unknown root is the failure, not a warning.
    /// </para>
    /// <para>
    /// <b>Revocation is off, and that is a limitation rather than a decision.</b> A V2G PKI distributes
    /// revocation through OCSP responders that a test hierarchy does not have, so asking would fail every
    /// chain for the wrong reason. <see cref="ChainResult.Reason"/> therefore says nothing about revocation,
    /// and a run that needs to claim a certificate is un-revoked cannot get that claim from here.
    /// </para>
    /// </remarks>
    public sealed class V2GChainValidator
    {

        private readonly X509Certificate2Collection roots;

        /// <summary>The subjects of the configured roots, for a run that wants to print what it trusts.</summary>
        public IReadOnlyList<string> RootSubjects { get; }

        public V2GChainValidator(X509Certificate2Collection roots)
        {
            ArgumentNullException.ThrowIfNull(roots);
            if (roots.Count == 0)
                throw new ArgumentException("A chain validator needs at least one root certificate.", nameof(roots));

            this.roots   = roots;
            RootSubjects = roots.Select(r => r.Subject).ToArray();
        }

        /// <summary>Validates a leaf and its intermediates, all DER-encoded, leaf-issuer first.</summary>
        public ChainResult Validate(byte[] leaf, IReadOnlyList<byte[]>? intermediates = null)
        {
            ArgumentNullException.ThrowIfNull(leaf);

            X509Certificate2 certificate;
            try { certificate = X509CertificateLoader.LoadCertificate(leaf); }
            catch (Exception ex) { return ChainResult.Rejected($"leaf is not a readable certificate: {ex.Message}"); }

            using (certificate)
                return Validate(certificate, intermediates);
        }

        /// <summary>Validates an already-parsed leaf against the configured roots.</summary>
        public ChainResult Validate(X509Certificate2 leaf, IReadOnlyList<byte[]>? intermediates = null)
        {
            ArgumentNullException.ThrowIfNull(leaf);

            using var chain = new X509Chain();
            chain.ChainPolicy.TrustMode         = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.RevocationMode    = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            chain.ChainPolicy.CustomTrustStore.AddRange(roots);

            if (intermediates is not null)
                foreach (var der in intermediates)
                {
                    try { chain.ChainPolicy.ExtraStore.Add(X509CertificateLoader.LoadCertificate(der)); }
                    catch (Exception ex) { return ChainResult.Rejected($"a SubCertificate is not readable: {ex.Message}"); }
                }

            if (!chain.Build(leaf))
            {
                // The platform's own status text names the defect. Distinct statuses are joined rather than
                // reduced to the first, because "expired AND untrusted root" is a different story from either.
                var why = chain.ChainStatus.Length > 0
                              ? string.Join("; ", chain.ChainStatus.Select(s => s.StatusInformation.Trim()).Distinct())
                              : "chain could not be built";
                return ChainResult.Rejected(why);
            }

            // Build() succeeded, so the last element is one of our roots.
            var anchor = chain.ChainElements[^1].Certificate.Subject;
            return ChainResult.Trusted(anchor);
        }

    }

}
