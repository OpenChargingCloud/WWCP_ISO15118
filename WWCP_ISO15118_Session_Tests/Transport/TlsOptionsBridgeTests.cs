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
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using NUnit.Framework;

using Vanaheimr.V2G.Simulation.Transport;

using BcSignatureScheme = Org.BouncyCastle.Tls.SignatureScheme;

namespace Vanaheimr.V2G.Simulation.Tests.Transport;

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
        using var rootKey = ECDsa.Create(curve);
        var rootRequest   = new CertificateRequest("CN=Test V2G Root CA", rootKey, hash);
        rootRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, critical: true));
        rootRequest.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, critical: true));

        using var root = rootRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5),
                                                     DateTimeOffset.UtcNow.AddHours(1));

        using var leafKey = ECDsa.Create(curve);
        var leafRequest   = new CertificateRequest("CN=Vehicle", leafKey, hash);
        leafRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, critical: true));
        leafRequest.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true));

        using var issued = leafRequest.Create(root,
                                              DateTimeOffset.UtcNow.AddMinutes(-5),
                                              DateTimeOffset.UtcNow.AddHours(1),
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

    // ...and asking for suites it pins away from is refused rather than silently served with the -20 pair.
    [Test]
    public void PinningAnythingElseIsRefused()
    {
        var (root, leaf) = Hierarchy(ECCurve.NamedCurves.nistP521, HashAlgorithmName.SHA512);
        using var _1 = root;
        using var _2 = leaf;

        Assert.That(() => TlsPlatform.ToBcClientOptions(ClientOptions(leaf, suites: TlsProfiles.Iso2CipherSuites)),
                    Throws.TypeOf<NotSupportedException>());
    }

}
