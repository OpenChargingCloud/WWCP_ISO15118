using cloud.charging.open.protocols.ISO15118.PKI;
using cloud.charging.open.protocols.ISO15118.PKI.Evil;

using NUnit.Framework;

using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;

namespace cloud.charging.open.protocols.ISO15118.PKIBuilder.Tests;

[TestFixture]
public sealed class V2GPKIBuilderTests
{

    private const String TestPolicyArc = "1.3.6.1.4.1.99999.15118.9";

    private static SecureRandom Random() =>
        new();

    private static V2GProfileOptions Options(
        V2GProfileFlavor flavor,
        V2GAlgorithm algorithm,
        V2GPolicySet? policies = null) =>
        new(flavor, algorithm, policies ?? V2GPolicySet.FromArc(TestPolicyArc));

    [Test]
    public void Strict15118_2_P256_hierarchy_verifies()
    {
        var hierarchy = V2GHierarchy.Build(
            V2GAlgorithm.EcdsaP256,
            Random(),
            V2GProfileOptions: Options(V2GProfileFlavor.Strict15118_2, V2GAlgorithm.EcdsaP256));

        var results = V2GVerifier.VerifyGood(hierarchy);

        Assert.That(results, Has.Count.EqualTo(4));
        Assert.That(results, Has.All.Matches<V2GVerifier.VerificationResult>(result => result.Ok));
    }

    [TestCase(V2GAlgorithm.EcdsaP521)]
    [TestCase(V2GAlgorithm.Ed448)]
    public void Strict15118_20_hierarchies_verify(V2GAlgorithm algorithm)
    {
        var hierarchy = V2GHierarchy.Build(
            algorithm,
            Random(),
            V2GProfileOptions: Options(V2GProfileFlavor.Strict15118_20, algorithm));

        Assert.That(
            V2GVerifier.VerifyGood(hierarchy),
            Has.All.Matches<V2GVerifier.VerificationResult>(result => result.Ok));
    }

    [Test]
    public void Strict15118_20_rejects_P256_profile_drift()
    {
        var hierarchy = V2GHierarchy.Build(
            V2GAlgorithm.EcdsaP256,
            Random(),
            V2GProfileOptions: Options(V2GProfileFlavor.Strict15118_20, V2GAlgorithm.EcdsaP256));

        var results = V2GVerifier.VerifyGood(hierarchy);

        Assert.That(results, Has.Some.Matches<V2GVerifier.VerificationResult>(
            result => !result.Ok && result.Error?.Contains("strict ISO 15118-20", StringComparison.Ordinal) == true));
    }

    [Test]
    public void Strict_profile_without_policy_arc_omits_certificate_policies()
    {
        var hierarchy = V2GHierarchy.Build(
            V2GAlgorithm.EcdsaP521,
            Random(),
            V2GProfileOptions: Options(V2GProfileFlavor.Strict15118_20, V2GAlgorithm.EcdsaP521, V2GPolicySet.None));

        Assert.That(GetPolicyOids(hierarchy.SeccLeaf.Certificate), Is.Empty);
    }

    [Test]
    public void Configured_policy_arc_is_emitted_on_leaf_profiles()
    {
        var hierarchy = V2GHierarchy.Build(
            V2GAlgorithm.EcdsaP521,
            Random(),
            V2GProfileOptions: Options(V2GProfileFlavor.Strict15118_20, V2GAlgorithm.EcdsaP521));

        var seccPolicies = GetPolicyOids(hierarchy.SeccLeaf.Certificate);

        Assert.That(seccPolicies, Does.Contain($"{TestPolicyArc}.1"));
        Assert.That(seccPolicies, Does.Contain($"{TestPolicyArc}.2"));
    }

    [TestCase(V2GAlgorithm.Ed448)]
    [TestCase(V2GAlgorithm.MLDsa44)]
    public void Signature_only_leaf_certificates_do_not_carry_key_agreement(V2GAlgorithm algorithm)
    {
        var flavor = V2GAlgorithms.IsPqc(algorithm)
            ? V2GProfileFlavor.ExperimentalPqc
            : V2GProfileFlavor.Strict15118_20;

        var hierarchy = V2GHierarchy.Build(
            algorithm,
            Random(),
            V2GProfileOptions: Options(flavor, algorithm));

        Assert.That(HasKeyUsage(hierarchy.SeccLeaf.Certificate, KeyUsageBit.KeyAgreement), Is.False);
        Assert.That(HasKeyUsage(hierarchy.SeccLeaf.Certificate, KeyUsageBit.DigitalSignature), Is.True);
    }

    [Test]
    public void Lab_secc_leaf_has_client_auth_but_strict_leaf_does_not()
    {
        var lab = V2GHierarchy.Build(
            V2GAlgorithm.EcdsaP521,
            Random(),
            V2GProfileOptions: Options(V2GProfileFlavor.Lab, V2GAlgorithm.EcdsaP521));
        var strict = V2GHierarchy.Build(
            V2GAlgorithm.EcdsaP521,
            Random(),
            V2GProfileOptions: Options(V2GProfileFlavor.Strict15118_20, V2GAlgorithm.EcdsaP521));

        Assert.That(GetExtendedKeyUsageOids(lab.SeccLeaf.Certificate), Does.Contain(KeyPurposeID.id_kp_clientAuth.Id));
        Assert.That(GetExtendedKeyUsageOids(strict.SeccLeaf.Certificate), Does.Not.Contain(KeyPurposeID.id_kp_clientAuth.Id));
        Assert.That(GetExtendedKeyUsageOids(strict.SeccLeaf.Certificate), Does.Contain(KeyPurposeID.id_kp_serverAuth.Id));
    }

    [Test]
    public void Evil_variants_include_rsa1024_leaf_with_rsa_subject_key()
    {
        var hierarchy = V2GHierarchy.Build(
            V2GAlgorithm.EcdsaP521,
            Random(),
            V2GProfileOptions: Options(V2GProfileFlavor.Lab, V2GAlgorithm.EcdsaP521));

        var rsaVariant = V2GEvilFactory.Build(hierarchy, Random())
            .Single(variant => variant.Name == "secc_rsa1024");

        Assert.Multiple(() =>
        {
            Assert.That(rsaVariant.Issued.KeyPair.Public, Is.InstanceOf<RsaKeyParameters>());
            Assert.That(((RsaKeyParameters) rsaVariant.Issued.KeyPair.Public).Modulus.BitLength, Is.EqualTo(1024));
            Assert.That(rsaVariant.Issued.Certificate.IssuerDN, Is.EqualTo(hierarchy.CpoSubCa2.Certificate.SubjectDN));
        });
    }

    [Test]
    public void Evil_variant_inventory_has_expected_core_cases()
    {
        var hierarchy = V2GHierarchy.Build(
            V2GAlgorithm.EcdsaP521,
            Random(),
            V2GProfileOptions: Options(V2GProfileFlavor.Lab, V2GAlgorithm.EcdsaP521));

        var names = V2GEvilFactory.Build(hierarchy, Random())
            .Select(variant => variant.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.That(names, Is.SupersetOf(new[]
        {
            "expired_secc",
            "not_yet_valid_secc",
            "secc_no_serverauth",
            "contract_with_serverauth",
            "secc_signed_by_mo",
            "secc_wrong_san",
            "secc_no_basic_constraints",
            "path_len_violation",
            "leaf_issued_by_leaf",
            "evil_twin_root",
            "secc_rsa1024",
            "ed448_leaf_in_ecdsa_chain",
            "ku_not_critical",
            "secc_wildcard_san"
        }));
    }

    [Test]
    public void Write_hierarchy_emits_chains_crls_and_size_report()
    {
        var hierarchy = V2GHierarchy.Build(
            V2GAlgorithm.EcdsaP521,
            Random(),
            V2GProfileOptions: Options(V2GProfileFlavor.Strict15118_20, V2GAlgorithm.EcdsaP521),
            RevocationBaseURL: "https://pki.example.test/v2g");

        var outDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"pki-{Guid.NewGuid():N}");

        V2GIO.WriteHierarchy(hierarchy, outDir);

        var baseDir = Path.Combine(outDir, "strict_15118_20_ecdsa_p521");
        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(baseDir, "chains", "secc_chain.pem")), Is.True);
            Assert.That(File.Exists(Path.Combine(baseDir, "chains", "v2g_root_trust.pem")), Is.True);
            Assert.That(File.Exists(Path.Combine(baseDir, "crls", "v2g_root_ca.crl")), Is.True);
            Assert.That(File.Exists(Path.Combine(baseDir, "size_report.md")), Is.True);
        });
    }

    [Test]
    public void Revocation_uris_are_added_when_base_uri_is_configured()
    {
        var hierarchy = V2GHierarchy.Build(
            V2GAlgorithm.EcdsaP521,
            Random(),
            V2GProfileOptions: Options(V2GProfileFlavor.Strict15118_20, V2GAlgorithm.EcdsaP521),
            RevocationBaseURL: "https://pki.example.test/v2g");

        Assert.That(hierarchy.SeccLeaf.Certificate.GetExtensionValue(X509Extensions.CrlDistributionPoints), Is.Not.Null);
        Assert.That(hierarchy.SeccLeaf.Certificate.GetExtensionValue(X509Extensions.AuthorityInfoAccess), Is.Not.Null);
    }

    private static HashSet<string> GetPolicyOids(X509Certificate certificate)
    {
        var extension = certificate.GetExtensionValue(X509Extensions.CertificatePolicies);
        if (extension is null)
            return [];

        return CertificatePolicies.GetInstance(X509ExtensionUtilities.FromExtensionValue(extension))
            .GetPolicyInformation()
            .Select(policy => policy.PolicyIdentifier.Id)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static HashSet<string> GetExtendedKeyUsageOids(X509Certificate certificate) =>
        certificate.GetExtendedKeyUsage()?
            .OfType<DerObjectIdentifier>()
            .Select(oid => oid.Id)
            .ToHashSet(StringComparer.Ordinal) ?? [];

    private static bool HasKeyUsage(X509Certificate certificate, KeyUsageBit bit)
    {
        var keyUsage = certificate.GetKeyUsage();
        var index = (int) bit;
        return keyUsage is not null && keyUsage.Length > index && keyUsage[index];
    }

    private enum KeyUsageBit
    {
        DigitalSignature = 0,
        KeyAgreement     = 4
    }

}
