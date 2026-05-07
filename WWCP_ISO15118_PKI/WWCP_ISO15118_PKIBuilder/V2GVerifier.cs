/*
 * Copyright (c) 2014-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

#region Usings

using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Pkix;
using Org.BouncyCastle.Utilities.Collections;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Store;

#endregion

namespace cloud.charging.open.protocols.ISO15118.PKI
{

    /// <summary>
    /// Cross-checks that the good hierarchy validates and the evil variants fail
    /// the way we expect — useful as a smoke test in CI.
    /// </summary>
    public static class V2GVerifier
    {

        public sealed record VerificationResult(String   Slug,
                                                Boolean  Ok,
                                                String?  Error);

        public static List<VerificationResult> VerifyGood(V2GHierarchy h)
        {

            var trust    = new HashSet<TrustAnchor> {
                               new (h.Root.Certificate, null)
                           };

            var results  = new List<VerificationResult> {
                               VerifyChain("secc",         h, trust, h.SeccLeaf,       h.CpoSubCa2, h.CpoSubCa1),
                               VerifyChain("contract",     h, trust, h.ContractLeaf,   h.MoSubCa2,  h.MoSubCa1),
                               VerifyChain("oem_prov",     h, trust, h.OemProvLeaf,    h.OemSubCa2, h.OemSubCa1),
                               VerifyChain("cps_signing",  h, trust, h.CpsSigningLeaf, h.CpsSubCa)
                           };

            return results;

        }

        private static VerificationResult VerifyChain(String                slug,
                                                      V2GHierarchy          hierarchy,
                                                      HashSet<TrustAnchor>  trust,
                                                      V2GIssued             leaf,
                                                      params V2GIssued[]    intermediates)
        {
            try
            {

                var allCerts   = new List<X509Certificate> { leaf.Certificate };
                allCerts.AddRange(intermediates.Select(i => i.Certificate));

                var holder     = CollectionUtilities.CreateStore(allCerts);
                var selector   = new X509CertStoreSelector { Certificate = leaf.Certificate };

                var pkixParams = new PkixBuilderParameters(trust, selector) {
                    IsRevocationEnabled = false,
                };
                pkixParams.AddStoreCert(holder);

                var builder = new PkixCertPathBuilder();
                builder.Build(pkixParams);

                VerifyRoleProfile(
                    hierarchy,
                    leaf,
                    intermediates
                );

                return new VerificationResult(
                           slug,
                           true,
                           null
                       );

            }
            catch (Exception ex)
            {
                return new VerificationResult(
                           slug,
                           false,
                           ex.Message
                       );
            }
        }

        private static void VerifyRoleProfile(V2GHierarchy        hierarchy,
                                              V2GIssued           leaf,
                                              params V2GIssued[]  intermediates)
        {

            if (hierarchy.Options.IsStrict15118_2 && hierarchy.Algorithm != V2GAlgorithm.EcdsaP256)
                throw new InvalidOperationException("strict ISO 15118-2 requires ECDSA P-256");

            if (hierarchy.Options.Flavor == V2GProfileFlavor.Strict15118_20 && !V2GAlgorithms.IsStrict15118_20(hierarchy.Algorithm))
                throw new InvalidOperationException("strict ISO 15118-20 requires ECDSA P-521 or Ed448");

            if (hierarchy.Options.Flavor is V2GProfileFlavor.Strict15118_2 or V2GProfileFlavor.Strict15118_20 && V2GAlgorithms.IsPqc(hierarchy.Algorithm))
                throw new InvalidOperationException("PQC algorithms are experimental and not part of the strict ISO 15118 profiles");

            foreach (var intermediate in intermediates)
                VerifyCa(intermediate);

            VerifyLeafKeyUsage         (hierarchy, leaf);
            VerifyLeafExtendedKeyUsage (hierarchy, leaf);
            VerifyPolicies             (leaf);

        }

        private static void VerifyCa(V2GIssued issued)
        {

            var cert = issued.Certificate;
            if (cert.GetBasicConstraints() < 0)
                throw new InvalidOperationException($"{issued.Profile.Role} is not marked as a CA");

            RequireKeyUsage(issued, KeyUsageBit.KeyCertSign, "keyCertSign");
            RequireKeyUsage(issued, KeyUsageBit.CrlSign,     "cRLSign");

        }

        private static void VerifyLeafKeyUsage(V2GHierarchy  hierarchy,
                                               V2GIssued     leaf)
        {

            if (leaf.Certificate.GetBasicConstraints() >= 0)
                throw new InvalidOperationException($"{leaf.Profile.Role} leaf is marked as a CA");

            RequireKeyUsage(
                leaf,
                KeyUsageBit.DigitalSignature,
                "digitalSignature"
            );

            var hasKeyAgreement = HasKeyUsage(leaf.Certificate, KeyUsageBit.KeyAgreement);
            if (V2GAlgorithms.IsSignatureOnly(hierarchy.Algorithm) && hasKeyAgreement)
                throw new InvalidOperationException($"{leaf.Profile.Role} {hierarchy.Algorithm} certificate must not carry keyAgreement");

            if (!V2GAlgorithms.IsSignatureOnly(hierarchy.Algorithm) && !hasKeyAgreement && leaf.Profile.Role is V2GRole.SECCLeaf or V2GRole.ContractCertLeaf or V2GRole.OEMProvCertLeaf)
                throw new InvalidOperationException($"{leaf.Profile.Role} ECDSA certificate is missing keyAgreement");

        }

        private static void VerifyLeafExtendedKeyUsage(V2GHierarchy  hierarchy,
                                                       V2GIssued     leaf)
        {

            var eku = leaf.Certificate.GetExtendedKeyUsage()?.
                           OfType<DerObjectIdentifier>().
                           Select(oid => oid.Id).
                           ToHashSet() ?? [];

            switch (leaf.Profile.Role)
            {

                case V2GRole.SECCLeaf:
                    RequireEku(eku, KeyPurposeID.id_kp_serverAuth, "serverAuth", leaf);
                    if (!hierarchy.Options.IsLab && eku.Contains(KeyPurposeID.id_kp_clientAuth.Id))
                        throw new InvalidOperationException("strict SECC leaf must not carry clientAuth EKU");
                    break;

                case V2GRole.ContractCertLeaf:
                case V2GRole.OEMProvCertLeaf:
                    if (hierarchy.Options.Flavor != V2GProfileFlavor.Strict15118_2)
                        RequireEku(eku, KeyPurposeID.id_kp_clientAuth, "clientAuth", leaf);
                    if (eku.Contains(KeyPurposeID.id_kp_serverAuth.Id))
                        throw new InvalidOperationException($"{leaf.Profile.Role} must not carry serverAuth EKU");
                    break;

            }

        }

        private static void VerifyPolicies(V2GIssued issued)
        {

            if (issued.Profile.PolicyOids.Length == 0)
                return;

            var actual = GetCertificatePolicies(issued.Certificate);
            foreach (var expected in issued.Profile.PolicyOids)
            {
                if (!actual.Contains(expected))
                    throw new InvalidOperationException($"{issued.Profile.Role} is missing certificate policy {expected}");
            }

        }

        private static HashSet<String> GetCertificatePolicies(X509Certificate cert)
        {

            var extension = cert.GetExtensionValue(X509Extensions.CertificatePolicies);
            if (extension is null)
                return [];

            var policies = CertificatePolicies.GetInstance(Asn1Object.FromByteArray(extension.GetOctets()));

            return [.. policies.GetPolicyInformation().Select(policy => policy.PolicyIdentifier.Id)];

        }

        private static void RequireEku(HashSet<String> actual, KeyPurposeID expected, String name, V2GIssued issued)
        {
            if (!actual.Contains(expected.Id))
                throw new InvalidOperationException($"{issued.Profile.Role} is missing {name} EKU");
        }

        private static void RequireKeyUsage(V2GIssued issued, KeyUsageBit bit, String name)
        {
            if (!HasKeyUsage(issued.Certificate, bit))
                throw new InvalidOperationException($"{issued.Profile.Role} is missing {name} key usage");
        }

        private static bool HasKeyUsage(X509Certificate cert, KeyUsageBit bit)
        {
            var usage = cert.GetKeyUsage();
            var index = (int) bit;
            return usage is not null && usage.Length > index && usage[index];
        }

        private enum KeyUsageBit
        {
            DigitalSignature = 0,
            KeyCertSign      = 5,
            CrlSign          = 6,
            KeyAgreement     = 4
        }

    }

}
