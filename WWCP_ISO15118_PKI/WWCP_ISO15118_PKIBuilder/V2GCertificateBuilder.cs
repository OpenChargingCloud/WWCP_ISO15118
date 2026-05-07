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
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;

#endregion

namespace cloud.charging.open.protocols.ISO15118.PKI
{

    /// <summary>
    /// Builds V2G certificates and Sub-CA hierarchies with BouncyCastle.
    /// </summary>
    public static class V2GCertificateBuilder
    {

        /// <summary>
        /// Generate a fresh keypair for the given algorithm.
        /// </summary>
        public static AsymmetricCipherKeyPair GenerateKeyPair(V2GAlgorithm  algorithm,
                                                              SecureRandom  random)
        {
            switch (algorithm)
            {

                case V2GAlgorithm.EcdsaP256:
                case V2GAlgorithm.EcdsaP384:
                case V2GAlgorithm.EcdsaP521:
                {
                    var curveOid = algorithm switch {
                        V2GAlgorithm.EcdsaP256 => SecObjectIdentifiers.SecP256r1,
                        V2GAlgorithm.EcdsaP384 => SecObjectIdentifiers.SecP384r1,
                        V2GAlgorithm.EcdsaP521 => SecObjectIdentifiers.SecP521r1,
                        _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
                    };
                    var domainParams = new ECKeyGenerationParameters(curveOid, random);
                    var gen = new ECKeyPairGenerator("ECDSA");
                    gen.Init(domainParams);
                    return gen.GenerateKeyPair();
                }

                case V2GAlgorithm.Ed25519:
                {
                    var gen = new Ed25519KeyPairGenerator();
                    gen.Init(new Ed25519KeyGenerationParameters(random));
                    return gen.GenerateKeyPair();
                }

                case V2GAlgorithm.Ed448:
                {
                    var gen = new Ed448KeyPairGenerator();
                    gen.Init(new Ed448KeyGenerationParameters(random));
                    return gen.GenerateKeyPair();
                }

                case V2GAlgorithm.MLDsa44:
                case V2GAlgorithm.MLDsa65:
                case V2GAlgorithm.MLDsa87:
                {
                    var gen = new MLDsaKeyPairGenerator();
                    gen.Init(new MLDsaKeyGenerationParameters(random, MLDsaParameters(algorithm)));
                    return gen.GenerateKeyPair();
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(algorithm));

            }
        }


        /// <summary>
        /// Signature algorithm string for BouncyCastle's signer factory.
        /// </summary>
        public static string SignatureAlgorithmName(V2GAlgorithm algorithm)

            => algorithm switch {
                   V2GAlgorithm.EcdsaP256  => "SHA256withECDSA",
                   V2GAlgorithm.EcdsaP384  => "SHA384withECDSA",
                   V2GAlgorithm.EcdsaP521  => "SHA512withECDSA",
                   V2GAlgorithm.Ed25519    => "Ed25519",
                   V2GAlgorithm.Ed448      => "Ed448",
                   V2GAlgorithm.MLDsa44    => "ML-DSA-44",
                   V2GAlgorithm.MLDsa65    => "ML-DSA-65",
                   V2GAlgorithm.MLDsa87    => "ML-DSA-87",
                   _                       => throw new ArgumentOutOfRangeException(nameof(algorithm))
               };


        private static MLDsaParameters MLDsaParameters(V2GAlgorithm algorithm)

            => algorithm switch {
                   V2GAlgorithm.MLDsa44  => Org.BouncyCastle.Crypto.Parameters.MLDsaParameters.ml_dsa_44,
                   V2GAlgorithm.MLDsa65  => Org.BouncyCastle.Crypto.Parameters.MLDsaParameters.ml_dsa_65,
                   V2GAlgorithm.MLDsa87  => Org.BouncyCastle.Crypto.Parameters.MLDsaParameters.ml_dsa_87,
                   _                     => throw new ArgumentOutOfRangeException(nameof(algorithm))
               };


        /// <summary>
        /// Issue a certificate. If <paramref name="issuer"/> is null this is a self-signed root.
        /// </summary>
        public static V2GIssued Issue(V2GCertProfile                       profile,
                                      V2GAlgorithm                         algorithm,
                                      SecureRandom                         random,
                                      V2GIssued?                           issuer,
                                      DateTime?                            notBefore                     = null,
                                      AsymmetricCipherKeyPair?             subjectKeyPairOverride        = null,
                                      Action<X509V3CertificateGenerator>?  customizer                    = null,
                                      V2GRevocationInfo?                   revocation                    = null,
                                      Boolean                              omitDefaultBasicConstraints   = false,
                                      Boolean                              omitDefaultKeyUsage           = false)

        {

            var subjectKeyPair  = subjectKeyPairOverride ?? GenerateKeyPair(algorithm, random);

            var subjectDn       = BuildName(profile);
            var issuerDn        = issuer is null
                                      ? subjectDn
                                      : issuer.Certificate.SubjectDN;

            var nb              = notBefore ?? DateTime.UtcNow.AddMinutes(-5);
            var na              = nb.Add(profile.Validity);

            var gen             = new X509V3CertificateGenerator();

            gen.SetSerialNumber (new BigInteger(159, random).Abs().Add(BigInteger.One));
            gen.SetIssuerDN     (issuerDn);
            gen.SetSubjectDN    (subjectDn);
            gen.SetNotBefore    (nb);
            gen.SetNotAfter     (na);
            gen.SetPublicKey    (subjectKeyPair.Public);



            // -- Extensions ------------------------------------------------------

            if (!omitDefaultBasicConstraints)
            {
                gen.AddExtension(X509Extensions.BasicConstraints, critical: true,
                    profile.IsCa
                        ? (profile.PathLenConstraint is int pl
                            ? new BasicConstraints(pl)
                            : new BasicConstraints(cA: true))
                        : new BasicConstraints(cA: false));
            }

            if (!omitDefaultKeyUsage)
            {
                gen.AddExtension(X509Extensions.KeyUsage, critical: true,
                    new KeyUsage(profile.KeyUsageBits));
            }

            if (profile.ExtendedKeyUsages.Length > 0)
            {
                gen.AddExtension(X509Extensions.ExtendedKeyUsage, critical: false,
                    new ExtendedKeyUsage(profile.ExtendedKeyUsages));
            }



            // SKI / AKI
            var subjectPubInfo = SubjectPublicKeyInfoFactory
                .CreateSubjectPublicKeyInfo(subjectKeyPair.Public);
            var ski = X509ExtensionUtilities.CreateSubjectKeyIdentifier(subjectPubInfo);
            gen.AddExtension(X509Extensions.SubjectKeyIdentifier, critical: false, ski);

            var issuerPub = issuer?.KeyPair.Public ?? subjectKeyPair.Public;
            var issuerPubInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(issuerPub);
            gen.AddExtension(X509Extensions.AuthorityKeyIdentifier, critical: false,
                X509ExtensionUtilities.CreateAuthorityKeyIdentifier(issuerPubInfo));



            // Certificate Policies
            if (profile.PolicyOids.Length > 0)
            {
                var policies = profile.PolicyOids
                    .Select(o => new PolicyInformation(new DerObjectIdentifier(o)))
                    .ToArray();
                gen.AddExtension(X509Extensions.CertificatePolicies, critical: false,
                    new CertificatePolicies(policies));
            }



            // SAN (DNS)
            if (profile.SubjectAltDnsNames.Length > 0)
            {
                var names = profile.SubjectAltDnsNames
                    .Select(d => new GeneralName(GeneralName.DnsName, d))
                    .ToArray();
                gen.AddExtension(X509Extensions.SubjectAlternativeName, critical: false,
                    new GeneralNames(names));
            }

            revocation ??= V2GRevocationInfo.None;
            if (!string.IsNullOrWhiteSpace(revocation.CrlDistributionPointUri))
            {
                var distributionPointName = new DistributionPointName(
                    new GeneralNames(new GeneralName(
                        GeneralName.UniformResourceIdentifier,
                        revocation.CrlDistributionPointUri)));
                gen.AddExtension(X509Extensions.CrlDistributionPoints, critical: false,
                    new CrlDistPoint([new DistributionPoint(distributionPointName, null, null)]));
            }

            var aia = new List<AccessDescription>();
            if (!string.IsNullOrWhiteSpace(revocation.OcspUri))
            {
                aia.Add(new AccessDescription(
                    AccessDescription.IdADOcsp,
                    new GeneralName(GeneralName.UniformResourceIdentifier, revocation.OcspUri)));
            }
            if (!string.IsNullOrWhiteSpace(revocation.CaIssuersUri))
            {
                aia.Add(new AccessDescription(
                    AccessDescription.IdADCAIssuers,
                    new GeneralName(GeneralName.UniformResourceIdentifier, revocation.CaIssuersUri)));
            }
            if (aia.Count > 0)
            {
                gen.AddExtension(X509Extensions.AuthorityInfoAccess, critical: false,
                    new AuthorityInformationAccess(aia.ToArray()));
            }



            // Hook for "evil" variants to mangle extensions
            customizer?.Invoke(gen);



            // -- Sign -----------------------------------------------------------
            var signingKey   = issuer?.KeyPair.Private ?? subjectKeyPair.Private;
            var signingAlgo  = issuer is null
                                   ? SignatureAlgorithmName(algorithm)
                                   : SignatureAlgorithmName(issuer.Algorithm);

            var sigFactory   = new Asn1SignatureFactory(
                                   signingAlgo,
                                   signingKey,
                                   random
                               );

            var cert         = gen.Generate(sigFactory);


            return new V2GIssued(
                       profile,
                       algorithm,
                       subjectKeyPair,
                       cert
                   );


        }

        private static X509Name BuildName(V2GCertProfile profile)
        {

            var oids    = new List<DerObjectIdentifier>();
            var values  = new List<String>();

            if (profile.Country is { } c)
            {
                oids.  Add(X509Name.C);
                values.Add(c);
            }

            if (profile.Organization is { } o)
            {
                oids.  Add(X509Name.O);
                values.Add(o);
            }
            foreach (var dc in profile.DomainComponents)
            {
                oids.  Add(X509Name.DC);
                values.Add(dc);
            }

            oids.  Add(X509Name.CN);
            values.Add(profile.CommonName);

            return new X509Name(
                       oids,
                       values
                   );

        }

    }

}
