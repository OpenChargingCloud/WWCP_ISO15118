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

using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;
using Org.BouncyCastle.Crypto.Operators;

using cloud.charging.open.protocols.ISO15118.PKI.Evil;

#endregion

namespace cloud.charging.open.protocols.ISO15118.PKI;

/// <summary>
/// Serializes certs and private keys as PEM and DER, plus chain bundles.
/// </summary>
public static class V2GIO
{

    public static void WriteIssued(V2GIssued  V2GIssued,
                                   String     OutputPath)
    {

        Directory.CreateDirectory(OutputPath);
        var slug = V2GIssued.Slug;

        // Cert: DER
        var certDer = V2GIssued.Certificate.GetEncoded();
        File.WriteAllBytes(
            Path.Combine(OutputPath, $"{slug}.cert.der"),
            certDer
        );

        // Cert: PEM
        File.WriteAllText(
            Path.Combine(OutputPath, $"{slug}.cert.pem"),
            ToPEM("CERTIFICATE", certDer)
        );

        // Private key: PKCS#8 DER (algorithm-agnostic across ECDSA, EdDSA, and PQC)
        var pkcs8 = PrivateKeyInfoFactory.
                        CreatePrivateKeyInfo(V2GIssued.KeyPair.Private).
                        GetEncoded();

        File.WriteAllBytes(Path.Combine(OutputPath, $"{slug}.key.der"), pkcs8);

        // Private key: PKCS#8 PEM
        File.WriteAllText(
            Path.Combine(OutputPath, $"{slug}.key.pem"),
            ToPEM("PRIVATE KEY", pkcs8));

    }

    public static void WriteHierarchy(V2GHierarchy  V2GHierarchy,
                                      String        OutputPath)
    {

        var algoTag = V2GAlgorithms.Tag(V2GHierarchy.Algorithm);
        var baseDir = Path.Combine(OutputPath, $"{ProfileTag(V2GHierarchy.Options.Flavor)}_{algoTag}");
        Directory.CreateDirectory(baseDir);

        var ordered = new[]
        {

            ("01_v2g_root_ca",        V2GHierarchy.Root),

            ("02_cpo_sub_ca_1",       V2GHierarchy.CpoSubCa1),
            ("03_cpo_sub_ca_2",       V2GHierarchy.CpoSubCa2),
            ("04_secc_leaf",          V2GHierarchy.SeccLeaf),

            ("05_mo_sub_ca_1",        V2GHierarchy.MoSubCa1),
            ("06_mo_sub_ca_2",        V2GHierarchy.MoSubCa2),
            ("07_contract_leaf",      V2GHierarchy.ContractLeaf),

            ("08_oem_sub_ca_1",       V2GHierarchy.OemSubCa1),
            ("09_oem_sub_ca_2",       V2GHierarchy.OemSubCa2),
            ("10_oem_prov_leaf",      V2GHierarchy.OemProvLeaf),

            ("11_cps_sub_ca",         V2GHierarchy.CpsSubCa),
            ("12_cps_signing_leaf",   V2GHierarchy.CpsSigningLeaf)

        };

        foreach (var (subdir, issued) in ordered)
            WriteIssued(issued, Path.Combine(baseDir, subdir));

        // Chain bundles for each leaf, ordered leaf -> root.
        WriteChain(Path.Combine(baseDir, "chains", "secc_chain.pem"),
                   V2GHierarchy.SeccLeaf, V2GHierarchy.CpoSubCa2, V2GHierarchy.CpoSubCa1, V2GHierarchy.Root);
        WriteChain(Path.Combine(baseDir, "chains", "contract_chain.pem"),
                   V2GHierarchy.ContractLeaf, V2GHierarchy.MoSubCa2, V2GHierarchy.MoSubCa1, V2GHierarchy.Root);
        WriteChain(Path.Combine(baseDir, "chains", "oem_prov_chain.pem"),
                   V2GHierarchy.OemProvLeaf, V2GHierarchy.OemSubCa2, V2GHierarchy.OemSubCa1, V2GHierarchy.Root);
        WriteChain(Path.Combine(baseDir, "chains", "cps_signing_chain.pem"),
                   V2GHierarchy.CpsSigningLeaf, V2GHierarchy.CpsSubCa, V2GHierarchy.Root);

        // Trust store: just the root.
        File.WriteAllText(Path.Combine(baseDir, "chains", "v2g_root_trust.pem"),
            ToPEM("CERTIFICATE", V2GHierarchy.Root.Certificate.GetEncoded()));

        WriteCrls(baseDir, V2GHierarchy);
        WriteSizeReport(baseDir, V2GHierarchy);

    }

    public static void WriteEvilVariants(IEnumerable<EvilVariant>  EvilVariants,
                                         V2GAlgorithm              V2GAlgorithm,
                                         String                    OutputPath)
    {

        var algoTag = V2GAlgorithms.Tag(V2GAlgorithm);
        var baseDir = Path.Combine(OutputPath, $"evil_{algoTag}");
        Directory.CreateDirectory(baseDir);

        var index = new List<String> { "# Evil V2G Certificate Variants", "" };

        foreach (var evilVariant in EvilVariants)
        {

            var dir = Path.Combine(baseDir, evilVariant.Name);
            WriteIssued(evilVariant.Issued, dir);

            // If the evil variant carries an issuer chain (e.g. evil-twin root +
            // sub-CAs), write those into a 'chain/' subfolder so the operator
            // gets a full deployable hierarchy.
            if (evilVariant.ChainCerts is { Length: > 0 } chain)
            {

                var chainDir = Path.Combine(dir, "chain");
                foreach (var c in chain)
                    WriteIssued(c, chainDir);

                // Bundle (leaf -> root order)
                var allInOrder = chain.Reverse().
                                       Concat([ evilVariant.Issued ]).
                                       Reverse().
                                       ToArray();

                WriteChainBundle(Path.Combine(dir, "chain_bundle.pem"), allInOrder);

            }

            index.Add($"## {evilVariant.Name}");
            index.Add(evilVariant.Description);
            index.Add($"- Subject: {evilVariant.Issued.Certificate.SubjectDN}");
            index.Add($"- Issuer:  {evilVariant.Issued.Certificate.IssuerDN}");
            index.Add($"- Valid:   {evilVariant.Issued.Certificate.NotBefore:u}  ..  {evilVariant.Issued.Certificate.NotAfter:u}");
            if (evilVariant.ChainCerts is { Length: > 0 } cc)
                index.Add($"- Chain:   {string.Join(" → ", cc.Select(c => c.Profile.CommonName))}");
            index.Add("");

        }

        File.WriteAllLines(Path.Combine(baseDir, "INDEX.md"), index);

    }

    private static void WriteChain(String              Path,
                                   params V2GIssued[]  IssuedChain)

        => WriteChainBundle(
               Path,
               IssuedChain
           );

    private static void WriteChainBundle(String                  Path,
                                         IEnumerable<V2GIssued>  IssuedChain)
    {

        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);

        using var sw = new StreamWriter(Path);

        foreach (var i in IssuedChain)
            sw.Write(ToPEM("CERTIFICATE", i.Certificate.GetEncoded()));

    }

    private static String ToPEM(String  PEMLabel,
                                Byte[]  DEREncoded)
    {

        var b64  = Convert.ToBase64String(DEREncoded);
        var sw   = new StringWriter();

        sw.WriteLine($"-----BEGIN {PEMLabel}-----");
        for (var i = 0; i < b64.Length; i += 64)
            sw.WriteLine(b64.Substring(i, Math.Min(64, b64.Length - i)));
        sw.WriteLine($"-----END {PEMLabel}-----");

        return sw.ToString();

    }

    private static String ProfileTag(V2GProfileFlavor V2GProfileFlavor)

        => V2GProfileFlavor switch {
               V2GProfileFlavor.Strict15118_2    => "strict_15118_2",
               V2GProfileFlavor.Strict15118_20   => "strict_15118_20",
               V2GProfileFlavor.Lab              => "lab",
               V2GProfileFlavor.ExperimentalPqc  => "experimental_pqc",
               _                                 => "profile"
           };

    private static void WriteCrls(String        BaseDirectory,
                                  V2GHierarchy  V2GHierarchy)
    {

        var crlDir = Path.Combine(BaseDirectory, "crls");
        Directory.CreateDirectory(crlDir);

        foreach (var ca in V2GHierarchy.AllCerts().Where(i => i.Profile.IsCa))
        {
            var crl = CreateEmptyCrl(ca);
            var der = crl.GetEncoded();
            File.WriteAllBytes(Path.Combine(crlDir, $"{ca.Slug}.crl"),     der);
            File.WriteAllBytes(Path.Combine(crlDir, $"{ca.Slug}.crl.der"), der);
            File.WriteAllText (Path.Combine(crlDir, $"{ca.Slug}.crl.pem"), ToPEM("X509 CRL", der));
        }

    }

    private static X509Crl CreateEmptyCrl(V2GIssued Issuer)
    {

        var now            = DateTime.UtcNow.AddMinutes(-5);

        var gen            = new X509V2CrlGenerator();
        gen.SetIssuerDN(Issuer.Certificate.SubjectDN);
        gen.SetThisUpdate(now);
        gen.SetNextUpdate(now.AddDays(7));

        var issuerPubInfo  = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(
                                 Issuer.KeyPair.Public
                             );

        gen.AddExtension(
            Org.BouncyCastle.Asn1.X509.X509Extensions.AuthorityKeyIdentifier,
            critical:        false,
            extensionValue:  X509ExtensionUtilities.CreateAuthorityKeyIdentifier(issuerPubInfo)
        );

        var sigFactory     = new Asn1SignatureFactory(
                                 V2GCertificateBuilder.SignatureAlgorithmName(Issuer.Algorithm),
                                 Issuer.KeyPair.Private
                             );

        return gen.Generate(sigFactory);

    }

    private static void WriteSizeReport(String        BaseDir,
                                        V2GHierarchy  V2GHierarchy)
    {

        var lines = new List<String> {
                        "# Certificate Size Report",
                        "",
                        $"Algorithm: `{V2GHierarchy.Algorithm}`",
                        $"Profile: `{V2GHierarchy.Options.Flavor}`",
                        "",
                        "| Role | Certificate DER | Signature | SPKI | PKCS#8 private key |",
                        "|------|----------------:|----------:|-----:|------------------:|"
                    };

        foreach (var issued in V2GHierarchy.AllCerts())
        {

            var certDer    = issued.Certificate.GetEncoded().  Length;
            var signature  = issued.Certificate.GetSignature().Length;
            var spki       = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(issued.KeyPair.Public). GetEncoded().Length;
            var pkcs8      = PrivateKeyInfoFactory.      CreatePrivateKeyInfo      (issued.KeyPair.Private).GetEncoded().Length;

            lines.Add($"| {issued.Profile.Role} | {certDer} | {signature} | {spki} | {pkcs8} |");

        }

        File.WriteAllLines(
            Path.Combine(BaseDir, "size_report.md"),
            lines
        );

    }

}
