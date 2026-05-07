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

using Org.BouncyCastle.Security;

using cloud.charging.open.protocols.ISO15118.PKI;
using cloud.charging.open.protocols.ISO15118.PKI.Evil;

#endregion

// Minimal CLI:
//   v2gpki [--out <dir>] [--algo p256|p384|p521|ed448|15118-20|pqc|all] [--profile strict-2|strict-20|lab|pqc] [--policy-arc <oid>] [--evil] [--seed <hex>]

String   outDir             = "out";
String   algoFlag           = "p521";
String   profileFlag        = "strict-20";
String?  policyArc          = null;
String?  revocationBaseUri  = "http://pki.v2g.local";
Boolean  buildEvil          = false;
Byte[]?  seed               = null;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {

        case "--out":                 outDir             = args[++i];                        break;
        case "--algo":                algoFlag           = args[++i].ToLowerInvariant();     break;
        case "--profile":             profileFlag        = args[++i].ToLowerInvariant();     break;
        case "--policy-arc":          policyArc          = args[++i];                        break;
        case "--revocation-base-uri": revocationBaseUri  = args[++i];                        break;
        case "--no-revocation-uris":  revocationBaseUri  = null;                             break;
        case "--evil":                buildEvil          = true;                             break;
        case "--seed":                seed               = Convert.FromHexString(args[++i]); break;

        case "-h" or "--help":
            PrintHelp();
            return 0;

        default:
            Console.Error.WriteLine($"unknown argument: {args[i]}");
            PrintHelp();
            return 2;

    }
}

var random = seed is null
    ? new SecureRandom()
    : SecureRandom.GetInstance("SHA256PRNG", autoSeed: false);

if (seed is not null)
    random.SetSeed(seed);


V2GAlgorithm[] algorithms = algoFlag switch {

    "p256" or "ecdsa-p256" or "secp256r1"      => [ V2GAlgorithm.EcdsaP256 ],
    "p384" or "ecdsa-p384" or "secp384r1"      => [ V2GAlgorithm.EcdsaP384 ],
    "p521" or "ecdsa-p521" or "secp521r1"      => [ V2GAlgorithm.EcdsaP521 ],

    "ed25519"                                  => [ V2GAlgorithm.Ed25519   ],
    "ed448"                                    => [ V2GAlgorithm.Ed448     ],

    "15118-20" or "iso15118-20"                => [ V2GAlgorithm.EcdsaP521,
                                                    V2GAlgorithm.Ed448     ],

    "ml-dsa-44" or "mldsa44"                   => [ V2GAlgorithm.MLDsa44   ],
    "ml-dsa-65" or "mldsa65"                   => [ V2GAlgorithm.MLDsa65   ],
    "ml-dsa-87" or "mldsa87"                   => [ V2GAlgorithm.MLDsa87   ],

    "pqc" or "ml-dsa"                          => [ V2GAlgorithm.MLDsa44,
                                                    V2GAlgorithm.MLDsa65,
                                                    V2GAlgorithm.MLDsa87   ],

    "both"                                     => [ V2GAlgorithm.EcdsaP256,
                                                    V2GAlgorithm.Ed25519   ],
    "classical"                                => [ V2GAlgorithm.EcdsaP256,
                                                    V2GAlgorithm.EcdsaP384,
                                                    V2GAlgorithm.EcdsaP521,
                                                    V2GAlgorithm.Ed25519,
                                                    V2GAlgorithm.Ed448     ],
    "all"                                      => [ V2GAlgorithm.EcdsaP256,
                                                    V2GAlgorithm.EcdsaP384,
                                                    V2GAlgorithm.EcdsaP521,
                                                    V2GAlgorithm.Ed25519,
                                                    V2GAlgorithm.Ed448,
                                                    V2GAlgorithm.MLDsa44,
                                                    V2GAlgorithm.MLDsa65,
                                                    V2GAlgorithm.MLDsa87   ],

    _                                          => throw new ArgumentException($"unknown algorithm: '{algoFlag}'!")

};


var profileFlavor = profileFlag switch {
                        "strict-2"  or "strict15118-2"    or "15118-2"       => V2GProfileFlavor.Strict15118_2,
                        "strict-20" or "strict15118-20"   or "15118-20"      => V2GProfileFlavor.Strict15118_20,
                        "lab"       or "pentest"                             => V2GProfileFlavor.Lab,
                        "pqc"       or "experimental-pqc" or "post-quantum"  => V2GProfileFlavor.ExperimentalPqc,
                        _                                                    => throw new ArgumentException($"unknown profile: {profileFlag}")
                    };

if (profileFlavor == V2GProfileFlavor.Strict15118_2 && algorithms.Any(a => a != V2GAlgorithm.EcdsaP256))
    throw new ArgumentException("strict ISO 15118-2 profile only supports --algo p256");

if (profileFlavor == V2GProfileFlavor.Strict15118_20 && algorithms.Any(a => !V2GAlgorithms.IsStrict15118_20(a)))
    throw new ArgumentException("strict ISO 15118-20 profile only supports --algo p521, ed448, or 15118-20");

if (profileFlavor is V2GProfileFlavor.Strict15118_2 or V2GProfileFlavor.Strict15118_20 && algorithms.Any(V2GAlgorithms.IsPqc))
    throw new ArgumentException("PQC algorithms are experimental; use --profile pqc or --profile lab");

if (profileFlavor == V2GProfileFlavor.ExperimentalPqc && algorithms.Any(a => !V2GAlgorithms.IsPqc(a)))
    throw new ArgumentException("experimental PQC profile only supports --algo pqc, ml-dsa-44, ml-dsa-65, or ml-dsa-87");

var policySet = profileFlavor is V2GProfileFlavor.Lab or V2GProfileFlavor.ExperimentalPqc
    ? (policyArc is null ? V2GPolicySet.Lab : V2GPolicySet.FromArc(policyArc))
    : (policyArc is null ? V2GPolicySet.None : V2GPolicySet.FromArc(policyArc));

if (profileFlavor is V2GProfileFlavor.Strict15118_2 or V2GProfileFlavor.Strict15118_20 && policyArc is null)
    Console.Error.WriteLine("[!] Strict profile selected without --policy-arc; certificatePolicies will be omitted rather than populated with placeholder OIDs.");

Directory.CreateDirectory(outDir);

foreach (var algo in algorithms)
{

    var options = new V2GProfileOptions(profileFlavor, algo, policySet);
    Console.WriteLine($"[+] Building {profileFlavor} hierarchy ({algo})…");
    var good = V2GHierarchy.Build(algo, random, V2GProfileOptions: options, RevocationBaseURL: revocationBaseUri);
    V2GIO.WriteHierarchy(good, outDir);
    Console.WriteLine($"    {good.AllCerts().Count()} certs written.");

    Console.WriteLine($"[+] Self-verifying good chains…");
    foreach (var r in V2GVerifier.VerifyGood(good))
    {
        var marker = r.Ok ? "OK " : "FAIL";
        Console.WriteLine($"    [{marker}] {r.Slug}{(r.Error is null ? "" : "  — " + r.Error)}");
    }

    if (buildEvil)
    {
        Console.WriteLine($"[+] Building evil variants ({algo})…");
        var variants = V2GEvilFactory.Build(good, random);
        V2GIO.WriteEvilVariants(variants, algo, outDir);
        Console.WriteLine($"    {variants.Count} evil certs written.");
    }

}

Console.WriteLine("[+] Done.");
return 0;

static void PrintHelp()
{
    Console.WriteLine("""
        cloud.charging.open.protocols.ISO15118.PKI — ISO 15118 certificate hierarchy builder

        Usage:
          v2gpki [options]

        Options:
          --out <dir>          Output directory (default: ./out)
          --algo <profile>     p256 | p384 | p521 | ed25519 | ed448 | 15118-20 | both |
                               classical | ml-dsa-44 | ml-dsa-65 | ml-dsa-87 | pqc | all
                               (default: p521)
          --profile <profile>  strict-2 | strict-20 | lab | pqc   (default: strict-20)
          --policy-arc <oid>   Policy OID arc to emit as <arc>.0 .. <arc>.3
          --revocation-base-uri <uri>
                               Base URI for CRL DP and AIA/OCSP (default: http://pki.v2g.local)
          --no-revocation-uris Omit CRL DP and AIA/OCSP extensions
          --evil               Also generate malformed cert variants for pentesting
          --seed <hex>         Deterministic PRNG seed (testing/repro)
          -h, --help           This help

        Output layout:
          out/
            strict_15118_20_ecdsa_p521/
              01_v2g_root_ca/  …  12_cps_signing_leaf/
              chains/
                secc_chain.pem
                contract_chain.pem
              crls/
                v2g_root_ca.crl
                …
            strict_15118_20_ed448/       ← if --algo 15118-20
            experimental_pqc_ml_dsa_44/  ← if --profile pqc --algo pqc
            evil_ecdsa_p256/             ← if --evil
              expired_secc/
              evil_twin_root/
              …
              INDEX.md
        """);
}
