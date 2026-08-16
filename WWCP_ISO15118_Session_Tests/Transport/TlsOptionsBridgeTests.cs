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

using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using NUnit.Framework;

using cloud.charging.open.protocols.ISO15118.Transport;

using BcSignatureScheme = Org.BouncyCastle.Tls.SignatureScheme;

namespace cloud.charging.open.protocols.ISO15118.Tests.Transport;

/// <summary>
/// The EVCC half of <see cref="TlsPlatform.ToBcClientOptions"/>: what a Vehicle certificate configured on
/// <see cref="TlsOptions"/> becomes once the session runs on the managed backend.
/// <para>
/// This is the mechanism Finding 4 of the 2026-08-05 EVerest run needed. Schannel builds the client chain
/// locally first and refuses to present one whose root the machine does not trust, so an off-store test
/// PKI never reaches the wire; the managed backend has no trust store in the path and just sends what it
/// was given, leaving path building to the peer — which is what ISO 15118-2/-20 expect. These are
/// throwaway hierarchies generated in-process: nothing is installed anywhere.
/// </para>
/// </summary>
[TestFixture]
public class TlsOptionsBridgeTests
{

    // A root that no trust store has ever heard of, plus a leaf it issued — the shape of every V2G test
    // PKI, and the shape Schannel rejects.
    private static (X509Certificate2 Root, X509Certificate2 Leaf) Hierarchy(ECCurve curve, HashAlgorithmName hash)
    {
        // One clock reading for the whole hierarchy, and a root window that strictly contains the leaf's.
        // X.509 validity is truncated to whole seconds, so four separate UtcNow calls put the leaf's
        // notAfter one second past the root's whenever a second boundary falls between them — and
        // CertificateRequest.Create refuses that outright ("later than issuerCertificate.NotAfter").
        // It failed in two of three consecutive runs on 2026-08-11 before this was pinned.
        var now = DateTimeOffset.UtcNow;

        using var rootKey = ECDsa.Create(curve);
        var rootRequest   = new CertificateRequest("CN=Test V2G Root CA", rootKey, hash);
        rootRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, critical: true));
        rootRequest.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, critical: true));

        using var root = rootRequest.CreateSelfSigned(now.AddMinutes(-10),
                                                     now.AddHours(2));

        using var leafKey = ECDsa.Create(curve);
        var leafRequest   = new CertificateRequest("CN=Vehicle", leafKey, hash);
        leafRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, critical: true));
        leafRequest.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true));

        using var issued = leafRequest.Create(root,
                                              now.AddMinutes(-5),
                                              now.AddHours(1),
                                              RandomNumberGenerator.GetBytes(8));

        using var withKey = issued.CopyWithPrivateKey(leafKey);

        // Re-import so the leaf carries an exportable key: the managed backend signs in managed code, so
        // BcCredentialBridge needs the raw key (TlsBackend.BouncyCastle says exactly this).
        var leaf = X509CertificateLoader.LoadPkcs12(withKey.Export(X509ContentType.Pfx),
                                                    password: null,
                                                    X509KeyStorageFlags.Exportable);

        return (X509CertificateLoader.LoadCertificate(root.RawData), leaf);
    }

    private static TlsOptions ClientOptions(X509Certificate2 leaf,
                                            X509Certificate2? intermediate       = null,
                                            IReadOnlyList<TlsCipherSuite>? suites = null)

        => new() {
               EnabledSslProtocols    = SslProtocols.Tls13,
               Backend                = TlsBackend.BouncyCastle,
               ClientCertificate      = leaf,
               ClientCertificateChain = intermediate is null ? null : [intermediate],
               CipherSuites           = suites
           };

    // What Schannel would not do: hand the peer the whole chain and let it build the path.
    [Test]
    public void TheClientChainIsCarriedLeafFirst()
    {
        var (root, leaf) = Hierarchy(ECCurve.NamedCurves.nistP521, HashAlgorithmName.SHA512);
        using var _1 = root;
        using var _2 = leaf;

        var credentials = TlsPlatform.ToBcClientOptions(ClientOptions(leaf, root)).OwnCredentials;

        Assert.That(credentials, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(credentials!.CertificateChain, Has.Length.EqualTo(2));
            Assert.That(credentials.CertificateChain[0], Is.EqualTo(leaf.RawData));
            Assert.That(credentials.CertificateChain[1], Is.EqualTo(root.RawData));
        });
    }

    [Test]
    public void AChainlessLeafIsPresentedAlone()
    {
        var (root, leaf) = Hierarchy(ECCurve.NamedCurves.nistP521, HashAlgorithmName.SHA512);
        using var _1 = root;
        using var _2 = leaf;

        var credentials = TlsPlatform.ToBcClientOptions(ClientOptions(leaf)).OwnCredentials;

        Assert.That(credentials!.CertificateChain, Has.Length.EqualTo(1));
    }

    // secp521r1 is the -20 profile's curve, and the one Schannel cannot use at all — so this pairing is
    // only reachable on the managed backend.
    [Test]
    public void TheSignatureSchemeFollowsTheLeafCurve()
    {
        var (root521, leaf521) = Hierarchy(ECCurve.NamedCurves.nistP521, HashAlgorithmName.SHA512);
        using var _1 = root521;
        using var _2 = leaf521;

        var (root256, leaf256) = Hierarchy(ECCurve.NamedCurves.nistP256, HashAlgorithmName.SHA256);
        using var _3 = root256;
        using var _4 = leaf256;

        Assert.Multiple(() =>
        {
            Assert.That(TlsPlatform.ToBcClientOptions(ClientOptions(leaf521)).OwnCredentials!.SignatureScheme,
                        Is.EqualTo(BcSignatureScheme.ecdsa_secp521r1_sha512));
            Assert.That(TlsPlatform.ToBcClientOptions(ClientOptions(leaf256)).OwnCredentials!.SignatureScheme,
                        Is.EqualTo(BcSignatureScheme.ecdsa_secp256r1_sha256));
        });
    }

    // Unilateral TLS, the ISO 15118-2 shape: no Vehicle certificate, so nothing to present.
    [Test]
    public void WithoutAVehicleCertificateThereAreNoCredentials()
    {
        var options = TlsPlatform.ToBcClientOptions(new TlsOptions
                      {
                          EnabledSslProtocols = SslProtocols.Tls13,
                          Backend             = TlsBackend.BouncyCastle
                      });

        Assert.That(options.OwnCredentials, Is.Null);
    }

    // On Windows the CipherSuitesPolicy route does not exist, so this backend is the only way to hold a
    // session to the -20 suites at all — asking for exactly them has to be accepted.
    [Test]
    public void PinningTheIso20SuitesIsAccepted()
    {
        var (root, leaf) = Hierarchy(ECCurve.NamedCurves.nistP521, HashAlgorithmName.SHA512);
        using var _1 = root;
        using var _2 = leaf;

        Assert.That(() => TlsPlatform.ToBcClientOptions(ClientOptions(leaf, suites: TlsProfiles.Iso20CipherSuites)),
                    Throws.Nothing);
    }

    // The -2 pair is accepted too since 2026-08-16 — this backend grew a TLS 1.2 profile because
    // trusted_ca_keys ([V2G2-651]) is an RFC 6066 extension SslStream cannot send. This assertion used to
    // be the negative case below, and updating it rather than deleting it is the point: the test caught
    // the widening, which is what it was for.
    [Test]
    public void PinningTheIso2SuitesIsAcceptedToo()
    {
        var (root, leaf) = Hierarchy(ECCurve.NamedCurves.nistP256, HashAlgorithmName.SHA256);
        using var _1 = root;
        using var _2 = leaf;

        Assert.That(() => TlsPlatform.ToBcClientOptions(ClientOptions(leaf, suites: TlsProfiles.Iso2CipherSuites)),
                    Throws.Nothing);
    }

    // ...and asking for suites it pins away from is still refused rather than silently served with a pair
    // the caller did not ask for.
    [Test]
    public void PinningAnythingElseIsRefused()
    {
        var (root, leaf) = Hierarchy(ECCurve.NamedCurves.nistP521, HashAlgorithmName.SHA512);
        using var _1 = root;
        using var _2 = leaf;

        Assert.That(() => TlsPlatform.ToBcClientOptions(
                              ClientOptions(leaf, suites: [TlsCipherSuite.TLS_AES_128_GCM_SHA256])),
                    Throws.TypeOf<NotSupportedException>());
    }


    #region trusted_ca_keys ([V2G2-651])

    // The roots reach the managed backend as DER, which is what the extension names by hash.
    [Test]
    public void TheRootsACarHoldsAreCarriedToTheBackend()
    {
        var (root, leaf) = Hierarchy(ECCurve.NamedCurves.nistP256, HashAlgorithmName.SHA256);
        using var _1 = root;
        using var _2 = leaf;

        var options = TlsPlatform.ToBcClientOptions(ClientOptions(leaf) with
                          {
                              EnabledSslProtocols = SslProtocols.Tls12,
                              TrustedCaKeys       = [root]
                          });

        Assert.Multiple(() =>
        {
            Assert.That(options.TrustedCaKeys, Is.Not.Null);
            Assert.That(options.TrustedCaKeys![0], Is.EqualTo(root.RawData));
            Assert.That(options.Iso2Profile, Is.True,
                        "TLS 1.2 alone is ISO 15118-2's transport, and the profile is read from the version "
                      + "rather than from a second switch that could disagree with it");
        });
    }

    // A -20 session is TLS 1.3 and uses certificate_authorities instead; the -2 profile must not leak in.
    [Test]
    public void ATls13SessionIsNotTheIso2Profile()
    {
        var (root, leaf) = Hierarchy(ECCurve.NamedCurves.nistP521, HashAlgorithmName.SHA512);
        using var _1 = root;
        using var _2 = leaf;

        Assert.That(TlsPlatform.ToBcClientOptions(ClientOptions(leaf)).Iso2Profile, Is.False);
    }

    #endregion

}
