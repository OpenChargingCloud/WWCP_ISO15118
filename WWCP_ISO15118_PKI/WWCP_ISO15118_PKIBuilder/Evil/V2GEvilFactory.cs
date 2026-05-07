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

using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Generators;

#endregion

namespace cloud.charging.open.protocols.ISO15118.PKI.Evil;


// Deliberately broken / out-of-spec certificates for pentesting V2G
// implementations. Every variant tests a specific implementation flaw class.

public static class V2GEvilFactory
{

    /// <summary>
    /// Build a comprehensive set of malformed certs against a "good" hierarchy.
    /// These reuse the good Sub-CAs as issuers wherever it makes sense, so a
    /// EVCC/SECC checking only the chain's signature math will still accept
    /// them — the bug lies in policy/EKU/timing/algorithm checks.
    /// </summary>
    public static List<EvilVariant> Build(V2GHierarchy  GoodV2GHierarchy,
                                          SecureRandom  SecureRandom)
    {

        var variants    = new List<EvilVariant> {

            // 1. Expired SECC leaf — NotAfter in the past.
            new (
                "expired_secc",
                "SECC leaf with NotAfter 30 days in the past",
                V2GCertificateBuilder.Issue(
                    Profile(
                        GoodV2GHierarchy,
                        V2GRole.SECCLeaf,
                        "Expired"
                    ) with
                    {
                        Validity = TimeSpan.FromDays(60)
                    },
                    GoodV2GHierarchy.Algorithm,
                    SecureRandom,
                    GoodV2GHierarchy.CpoSubCa2,
                    notBefore: DateTime.UtcNow.AddDays(-90)
                )
            ),


            // 2. Not-yet-valid SECC leaf — NotBefore in the future.
            new (
                "not_yet_valid_secc",
                "SECC leaf with NotBefore 30 days in the future",
                V2GCertificateBuilder.Issue(
                    Profile(
                        GoodV2GHierarchy,
                        V2GRole.SECCLeaf,
                        "Future"
                    ),
                    GoodV2GHierarchy.Algorithm,
                    SecureRandom,
                    GoodV2GHierarchy.CpoSubCa2,
                    notBefore: DateTime.UtcNow.AddDays(30)
                )
            ),


            // 3. SECC leaf without serverAuth EKU — should be rejected.
            new (
                "secc_no_serverauth",
                "SECC leaf missing serverAuth EKU (only clientAuth)",
                V2GCertificateBuilder.Issue(
                    Profile(
                        GoodV2GHierarchy,
                        V2GRole.SECCLeaf,
                        "NoServerAuth"
                    ) with {
                        ExtendedKeyUsages = [ KeyPurposeID.id_kp_clientAuth ]
                    },
                    GoodV2GHierarchy.Algorithm,
                    SecureRandom,
                    GoodV2GHierarchy.CpoSubCa2
                )
            ),


            // 4. Contract cert with serverAuth EKU — should be rejected (privilege escalation).
            new (
                "contract_with_serverauth",
                "Contract cert carries serverAuth EKU — could be presented as SECC",
                V2GCertificateBuilder.Issue(
                    Profile(
                        GoodV2GHierarchy,
                        V2GRole.ContractCertLeaf,
                        "WithServerAuth"
                    ) with {
                        ExtendedKeyUsages   = [ KeyPurposeID.id_kp_clientAuth, KeyPurposeID.id_kp_serverAuth ],
                        SubjectAltDnsNames  = [ "secc.v2g.local" ]
                    },
                    GoodV2GHierarchy.Algorithm,
                    SecureRandom,
                    GoodV2GHierarchy.MoSubCa2
                )
            ),


            // 5. SECC leaf signed by MO chain — wrong issuer family.
            new (
                "secc_signed_by_mo",
                "SECC leaf signed by MO Sub-CA instead of CPO Sub-CA",
                V2GCertificateBuilder.Issue(
                    Profile(
                        GoodV2GHierarchy,
                        V2GRole.SECCLeaf,
                        "WrongIssuer"
                    ),
                    GoodV2GHierarchy.Algorithm,
                    SecureRandom,
                    GoodV2GHierarchy.MoSubCa2
                )
            ),


            // 6. SECC leaf with wrong DNS SAN.
            new (
                "secc_wrong_san",
                "SECC leaf with attacker-controlled SAN",
                V2GCertificateBuilder.Issue(
                    Profile(
                        GoodV2GHierarchy,
                        V2GRole.SECCLeaf,
                        "WrongSAN"
                    ) with {
                        SubjectAltDnsNames = [ "attacker.example.com" ]
                    },
                    GoodV2GHierarchy.Algorithm,
                    SecureRandom,
                    GoodV2GHierarchy.CpoSubCa2
                )
            ),


            // 7. SECC leaf without basicConstraints — RFC 5280 says EE certs MAY
            //    omit BC but tests how validators handle the absence vs. cA=false.
            new (
                "secc_no_basic_constraints",
                "SECC leaf without basicConstraints extension at all",
                V2GCertificateBuilder.Issue(
                    Profile(
                        GoodV2GHierarchy,
                        V2GRole.SECCLeaf,
                        "NoBC"
                    ),
                    GoodV2GHierarchy.Algorithm,
                    SecureRandom,
                    GoodV2GHierarchy.CpoSubCa2,
                    omitDefaultBasicConstraints: true
                )
            )

        };


        // 8. Path-length-violating sub-CA — Sub-CA 2 issues a *third* Sub-CA
        //    when its pathLen=0 forbids it.
        var rogueSubCA  = V2GCertificateBuilder.Issue(
                              Profile(
                                  GoodV2GHierarchy,
                                  V2GRole.CPOSubCA2,
                                  "PathLenViolation"
                              ) with {
                                  CommonName = "Rogue CPO Sub-CA 3",
                                  PathLenConstraint = 0
                              },
                              GoodV2GHierarchy.Algorithm,
                              SecureRandom,
                              GoodV2GHierarchy.CpoSubCa2
                          );

        variants.Add(
            new EvilVariant(
                "path_len_violation",
                "Sub-CA issued under path-len=0 parent (should fail validation)",
                rogueSubCA
            )
        );


        // 9. Leaf signed by a leaf — nonsensical but tests path validation.
        var leafIssuingLeaf = V2GCertificateBuilder.Issue(
                                  Profile(
                                      GoodV2GHierarchy,
                                      V2GRole.SECCLeaf,
                                      "ChildOfLeaf"
                                  ),
                                  GoodV2GHierarchy.Algorithm,
                                  SecureRandom,
                                  GoodV2GHierarchy.SeccLeaf
                              );

        variants.Add(
            new EvilVariant(
                "leaf_issued_by_leaf",
                "SECC leaf with another leaf cert as issuer (cA=false)",
                leafIssuingLeaf
            )
        );


        // 10. Evil-Root parallel hierarchy. Same DN as good root, different keys.
        var evilRoot  = V2GCertificateBuilder.Issue(
                            Profile(
                                GoodV2GHierarchy,
                                V2GRole.V2GRootCA
                            ),    // identical DN!
                            GoodV2GHierarchy.Algorithm,
                            SecureRandom,
                            issuer: null
                        );

        var evilCPO1  = V2GCertificateBuilder.Issue(
                            Profile(
                                GoodV2GHierarchy,
                                V2GRole.CPOSubCA1
                            ),    // identical DN!
                            GoodV2GHierarchy.Algorithm,
                            SecureRandom,
                            evilRoot
                        );

        var evilCPO2  = V2GCertificateBuilder.Issue(
                            Profile(
                                GoodV2GHierarchy,
                                V2GRole.CPOSubCA2
                            ),
                            GoodV2GHierarchy.Algorithm,
                            SecureRandom,
                            evilCPO1
                        );

        var evilSECC  = V2GCertificateBuilder.Issue(
                            Profile(
                                GoodV2GHierarchy,
                                V2GRole.SECCLeaf,
                                "EvilTwin"
                            ),
                            GoodV2GHierarchy.Algorithm,
                            SecureRandom,
                            evilCPO2
                        );

        variants.Add(
            new EvilVariant(
                "evil_twin_root",
                "Identical-DN parallel V2G Root with attacker keys (DN-collision attack)",
                evilSECC,
                ChainCerts: [ evilRoot, evilCPO1, evilCPO2 ]
            )
        );


        // 11. Weak-key SECC: RSA-1024.
        // Out of profile. Tests algorithm policy.
        variants.Add(
            BuildWeakKeyRsaSecc(
                GoodV2GHierarchy,
                SecureRandom
            )
        );


        // 12. Cross-algorithm mix: Ed448 leaf signed by ECDSA sub-CA. Useful
        //     for validators that must enforce profile-wide algorithm policy.
        if (GoodV2GHierarchy.Algorithm is V2GAlgorithm.EcdsaP256
                                       or V2GAlgorithm.EcdsaP384
                                       or V2GAlgorithm.EcdsaP521)
        {

            var ed448Leaf = V2GCertificateBuilder.Issue(
                                V2GCertProfile.ForRole(
                                    V2GRole.SECCLeaf,
                                    GoodV2GHierarchy.Options with { Algorithm = V2GAlgorithm.Ed448 },
                                    "Ed448InEcdsaChain"
                                ),
                                V2GAlgorithm.Ed448,
                                SecureRandom,
                                GoodV2GHierarchy.CpoSubCa2
                            );

            variants.Add(
                new EvilVariant(
                    "ed448_leaf_in_ecdsa_chain",
                    $"Ed448 SECC leaf in a {GoodV2GHierarchy.Algorithm} chain",
                    ed448Leaf
                )
            );

        }


        // 13. KeyUsage extension non-critical — some validators only enforce
        //     KU when marked critical.
        variants.Add(
            new EvilVariant(
                "ku_not_critical",
                "SECC leaf with KeyUsage extension non-critical",
                V2GCertificateBuilder.Issue(
                    Profile(
                        GoodV2GHierarchy,
                        V2GRole.SECCLeaf,
                        "KUNonCritical"
                    ),
                    GoodV2GHierarchy.Algorithm,
                    SecureRandom,
                    GoodV2GHierarchy.CpoSubCa2,
                    omitDefaultKeyUsage:  true,
                    customizer:           gen => gen.AddExtension(
                                                     X509Extensions.KeyUsage,
                                                     critical: false,
                                                     new KeyUsage(
                                                         Profile(
                                                             GoodV2GHierarchy,
                                                             V2GRole.SECCLeaf
                                                         ).KeyUsageBits
                                                     )
                                                 )
                )
            )
        );


        // 14. Wildcard-domain CN/SAN — V2G should not allow wildcards.
        variants.Add(
            new EvilVariant(
                "secc_wildcard_san",
                "SECC leaf with *.v2g.local wildcard SAN",
                V2GCertificateBuilder.Issue(
                    Profile(
                        GoodV2GHierarchy,
                        V2GRole.SECCLeaf,
                        "Wildcard"
                    ) with {
                        SubjectAltDnsNames = ["*.v2g.local"]
                    },
                    GoodV2GHierarchy.Algorithm,
                    SecureRandom,
                    GoodV2GHierarchy.CpoSubCa2
                )
            )
        );


        return variants;

    }

    private static EvilVariant BuildWeakKeyRsaSecc(V2GHierarchy  GoodV2GHierarchy,
                                                   SecureRandom  SecureRandom)
    {

        var rsaGen   = new RsaKeyPairGenerator();
        rsaGen.Init(
            new Org.BouncyCastle.Crypto.Parameters.RsaKeyGenerationParameters(
                BigInteger.ValueOf(0x10001),
                SecureRandom,
                strength:  1024,
                certainty:   25
            )
        );

        var rsaKp    = rsaGen.GenerateKeyPair();

        var profile  = Profile(
                           GoodV2GHierarchy,
                           V2GRole.SECCLeaf,
                           "RSA1024"
                       );

        var issued   = V2GCertificateBuilder.Issue(
                           profile,
                           GoodV2GHierarchy.Algorithm,
                           SecureRandom,
                           GoodV2GHierarchy.CpoSubCa2,
                           subjectKeyPairOverride: rsaKp
                       );

        return new EvilVariant(
                   "secc_rsa1024",
                   "SECC leaf with RSA-1024 subject key (weak, out-of-spec)",
                   issued
               );

    }

    private static V2GCertProfile Profile(V2GHierarchy  Hierarchy,
                                          V2GRole       Role,
                                          String?       cnSuffix   = null)

        => V2GCertProfile.ForRole(
               Role,
               Hierarchy.Options,
               cnSuffix
           );

}
