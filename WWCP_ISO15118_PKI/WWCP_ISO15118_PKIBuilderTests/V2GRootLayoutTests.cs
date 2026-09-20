/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

using cloud.charging.open.protocols.ISO15118.PKI;
using NUnit.Framework;
using Org.BouncyCastle.Security;

namespace cloud.charging.open.protocols.ISO15118.PKIBuilder.Tests;

/// <summary>
/// One root or three: the MO and OEM branches below roots of their own, and
/// what that changes for whoever verifies a chain or reads the directory.
/// </summary>
[TestFixture]
public sealed class V2GRootLayoutTests
{

    private static SecureRandom Random() => new();

    private static V2GProfileOptions Separate(V2GProfileFlavor flavor, V2GAlgorithm algorithm) =>
        new(flavor, algorithm, V2GPolicySet.None, V2GRootLayout.SeparateRoots);

    [Test]
    public void The_default_is_one_root_and_the_anchors_are_that_root()
    {
        var hierarchy = V2GHierarchy.Build(
            V2GAlgorithm.EcdsaP256,
            Random(),
            V2GProfileOptions: new V2GProfileOptions(V2GProfileFlavor.Strict15118_2, V2GAlgorithm.EcdsaP256, V2GPolicySet.None));

        Assert.Multiple(() =>
        {
            Assert.That(hierarchy.HasSeparateRoots, Is.False);
            Assert.That(hierarchy.MoRoot,  Is.SameAs(hierarchy.Root), "the MO branch is anchored at the V2G root");
            Assert.That(hierarchy.OemRoot, Is.SameAs(hierarchy.Root), "and so is the OEM branch");
            Assert.That(hierarchy.Roots().Count(), Is.EqualTo(1));
            Assert.That(hierarchy.AllCerts().Count(), Is.EqualTo(15));
            Assert.That(hierarchy.MoSubCa1.Certificate.IssuerDN, Is.EqualTo(hierarchy.Root.Certificate.SubjectDN));
        });
    }

    [TestCase(V2GProfileFlavor.Strict15118_2,  V2GAlgorithm.EcdsaP256)]
    [TestCase(V2GProfileFlavor.Strict15118_20, V2GAlgorithm.EcdsaP521)]
    [TestCase(V2GProfileFlavor.Lab,            V2GAlgorithm.Ed25519)]
    public void Separate_roots_put_the_mo_and_oem_branches_below_roots_of_their_own(V2GProfileFlavor flavor, V2GAlgorithm algorithm)
    {
        var hierarchy = V2GHierarchy.Build(algorithm, Random(), CommonNameSuffix: "(three roots)", V2GProfileOptions: Separate(flavor, algorithm));

        Assert.Multiple(() =>
        {
            Assert.That(hierarchy.HasSeparateRoots, Is.True);
            Assert.That(hierarchy.Roots().Count(),   Is.EqualTo(3));
            Assert.That(hierarchy.AllCerts().Count(), Is.EqualTo(17));

            // Three self-signed anchors with names of their own.
            foreach (var root in hierarchy.Roots())
            {
                Assert.That(root.Certificate.IssuerDN, Is.EqualTo(root.Certificate.SubjectDN), $"{root.Profile.CommonName} is self-signed");
                Assert.That(root.Profile.IsCa, Is.True);
            }
            Assert.That(hierarchy.MoRoot.Profile.CommonName,  Is.EqualTo("MO Root CA (three roots)"));
            Assert.That(hierarchy.OemRoot.Profile.CommonName, Is.EqualTo("OEM Root CA (three roots)"));
            Assert.That(hierarchy.Root.Profile.CommonName,    Is.EqualTo("V2G Root CA (three roots)"));

            // Each branch below the root that vouches for it - and nowhere else.
            Assert.That(hierarchy.CpoSubCa1.Certificate.IssuerDN,     Is.EqualTo(hierarchy.Root.Certificate.SubjectDN));
            Assert.That(hierarchy.CpsSubCa.Certificate.IssuerDN,      Is.EqualTo(hierarchy.Root.Certificate.SubjectDN));
            Assert.That(hierarchy.MoSubCa1.Certificate.IssuerDN,      Is.EqualTo(hierarchy.MoRoot.Certificate.SubjectDN));
            Assert.That(hierarchy.OemSubCa1.Certificate.IssuerDN,     Is.EqualTo(hierarchy.OemRoot.Certificate.SubjectDN));
            Assert.That(hierarchy.VehicleSubCa1.Certificate.IssuerDN, Is.EqualTo(hierarchy.OemRoot.Certificate.SubjectDN));
        });
    }

    [Test]
    public void Every_chain_verifies_against_its_own_anchor()
    {
        var hierarchy = V2GHierarchy.Build(V2GAlgorithm.EcdsaP521, Random(), V2GProfileOptions: Separate(V2GProfileFlavor.Strict15118_20, V2GAlgorithm.EcdsaP521));

        var results = V2GVerifier.VerifyGood(hierarchy);

        Assert.That(results, Has.Count.EqualTo(5));
        Assert.That(results, Has.All.Matches<V2GVerifier.VerificationResult>(result => result.Ok),
                    String.Join("; ", results.Where(r => !r.Ok).Select(r => $"{r.Slug}: {r.Error}")));
    }

    [Test]
    public void A_contract_chain_does_not_verify_against_the_v2g_root_alone()
    {
        // The point of keeping the roots apart: the V2G root vouches for
        // stations, and a contract that only it could verify is a contract
        // nobody in the MO's hierarchy issued.
        var hierarchy = V2GHierarchy.Build(V2GAlgorithm.EcdsaP256, Random(), V2GProfileOptions: Separate(V2GProfileFlavor.Strict15118_2, V2GAlgorithm.EcdsaP256));

        var chain = new[] { hierarchy.ContractLeaf, hierarchy.MoSubCa2, hierarchy.MoSubCa1 };

        Assert.That(ChainsTo(chain, hierarchy.MoRoot), Is.True,  "the MO root anchors the contract");
        Assert.That(ChainsTo(chain, hierarchy.Root),   Is.False, "the V2G root does not");
    }

    [Test]
    public void Write_hierarchy_emits_the_roots_and_one_trust_file_per_anchor()
    {
        var hierarchy = V2GHierarchy.Build(V2GAlgorithm.EcdsaP521, Random(), V2GProfileOptions: Separate(V2GProfileFlavor.Strict15118_20, V2GAlgorithm.EcdsaP521));

        var outDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"pki-{Guid.NewGuid():N}");

        V2GIO.WriteHierarchy(hierarchy, outDir);

        var baseDir = Path.Combine(outDir, "strict_15118_20_ecdsa_p521");
        Assert.Multiple(() =>
        {
            Assert.That(Directory.Exists(Path.Combine(baseDir, "01_v2g_root_ca")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(baseDir, "01_mo_root_ca")),  Is.True);
            Assert.That(Directory.Exists(Path.Combine(baseDir, "01_oem_root_ca")), Is.True);
            Assert.That(File.Exists(Path.Combine(baseDir, "chains", "v2g_root_trust.pem")), Is.True);
            Assert.That(File.Exists(Path.Combine(baseDir, "chains", "mo_root_trust.pem")),  Is.True);
            Assert.That(File.Exists(Path.Combine(baseDir, "chains", "oem_root_trust.pem")), Is.True);
            Assert.That(File.Exists(Path.Combine(baseDir, "crls", "mo_root_ca.crl")),  Is.True, "a root of its own signs a CRL of its own");
            Assert.That(File.Exists(Path.Combine(baseDir, "crls", "oem_root_ca.crl")), Is.True);

            // The contract chain bundle ends at the MO root, not at the V2G root.
            var contractChain = File.ReadAllText(Path.Combine(baseDir, "chains", "contract_chain.pem"));
            Assert.That(contractChain.Split("-----BEGIN CERTIFICATE-----").Length - 1, Is.EqualTo(4));
        });

        // And a single-root hierarchy writes neither the directories nor the files.
        var single    = V2GHierarchy.Build(V2GAlgorithm.EcdsaP256, Random(), V2GProfileOptions: new V2GProfileOptions(V2GProfileFlavor.Strict15118_2, V2GAlgorithm.EcdsaP256, V2GPolicySet.None));
        var singleOut = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"pki-{Guid.NewGuid():N}");

        V2GIO.WriteHierarchy(single, singleOut);

        Assert.That(Directory.Exists(Path.Combine(singleOut, "strict_15118_2_ecdsa_p256", "01_mo_root_ca")), Is.False);
        Assert.That(File.Exists(Path.Combine(singleOut, "strict_15118_2_ecdsa_p256", "chains", "mo_root_trust.pem")), Is.False);
    }

    private static Boolean ChainsTo(V2GIssued[] chain, V2GIssued root)
    {
        try
        {
            var trust    = new HashSet<Org.BouncyCastle.Pkix.TrustAnchor> { new(root.Certificate, null) };
            var store    = Org.BouncyCastle.Utilities.Collections.CollectionUtilities.CreateStore(chain.Select(c => c.Certificate).ToList());
            var selector = new Org.BouncyCastle.X509.Store.X509CertStoreSelector { Certificate = chain[0].Certificate };
            var pkix     = new Org.BouncyCastle.Pkix.PkixBuilderParameters(trust, selector) { IsRevocationEnabled = false };
            pkix.AddStoreCert(store);
            new Org.BouncyCastle.Pkix.PkixCertPathBuilder().Build(pkix);
            return true;
        }
        catch (Org.BouncyCastle.Pkix.PkixCertPathBuilderException)
        {
            return false;
        }
    }

}
