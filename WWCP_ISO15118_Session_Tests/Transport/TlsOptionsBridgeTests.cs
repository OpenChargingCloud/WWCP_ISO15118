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
    // PKI, and the shape Schannel rejects. Internal because TlsOptionsPeerChainBridgeTests below needs the
    // same thing, private key included: duplicating it would duplicate the clock caveat with it.
    internal static (X509Certificate2 Root, X509Certificate2 Leaf) Hierarchy(ECCurve curve, HashAlgorithmName hash)
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


/// <summary>
/// The other half of the same bridge: what a .NET <see cref="RemoteCertificateValidationCallback"/> gets to
/// see once the session runs on the managed backend.
/// </summary>
/// <remarks>
/// <para>
/// Until 2026-08-16 the answer was "the leaf, and a null chain". A callback asking <i>"is this the exact
/// certificate I expect"</i> was satisfied by that; one asking <i>"does this reach a root I trust"</i> was
/// not, and with a <b>root-only</b> trust store it refused every peer — including one serving exactly the
/// chain it had been told to trust. A trust bundle carrying the intermediates hides it completely, which is
/// how it survived a year of TLS runs and was found only by an arm designed with a single anchor.
/// </para>
/// <para>
/// <b>Third costume of one defect.</b> The app's two runnable peers dropped the peer chain (fixed
/// 2026-08-09), the interop fixture's own callback dropped it (fixed 2026-08-14), and neither time was the
/// question <i>where do the intermediates come from</i> answered for this backend. The rule the tests below
/// pin is the portable one: they arrive in <c>ChainPolicy.ExtraStore</c>, which is where <c>SslStream</c>
/// puts them and where <c>TrustRoots.PeerIntermediates</c> looks.
/// </para>
/// </remarks>
[TestFixture]
public class TlsOptionsPeerChainBridgeTests
{

    // P-256 rather than the -20 curve: the station side of this bridge has to hand the leaf's key to the
    // managed backend, and nothing here is about the profile.
    private static (X509Certificate2 Root, X509Certificate2 Leaf) Hierarchy()
        => TlsOptionsBridgeTests.Hierarchy(ECCurve.NamedCurves.nistP256, HashAlgorithmName.SHA256);

    // What the callback saw, recorded rather than judged: these tests are about what reaches it.
    private sealed record Seen(byte[]? Leaf, byte[][] Extra);

    private static (RemoteCertificateValidationCallback Callback, Func<Seen?> Result) Recorder()
    {
        Seen? seen = null;

        return ((_, presented, chain, _) =>
                {
                    seen = new Seen(presented?.GetRawCertData(),
                                    chain is null
                                        ? []
                                        : [.. chain.ChainPolicy.ExtraStore
                                                   .OfType<X509Certificate2>()
                                                   .Select(c => c.RawData)]);
                    return true;
                },
                () => seen);
    }

    private static TlsOptions ClientOptions(RemoteCertificateValidationCallback validate) => new()
    {
        EnabledSslProtocols         = SslProtocols.Tls13,
        Backend                     = TlsBackend.BouncyCastle,
        ServerCertificateValidation = validate
    };

    /// <summary>The EVCC side, and the assertion the fix exists for.</summary>
    [Test]
    public void TheStationsWholeChainReachesTheCallback()
    {
        var (root, leaf) = Hierarchy();
        using var _1 = root;
        using var _2 = leaf;

        var (callback, result) = Recorder();
        var options            = TlsPlatform.ToBcClientOptions(ClientOptions(callback));

        Assert.That(options.ValidatePeerChain, Is.Not.Null, "the chain hook is the one a TlsOptions session bridges onto");

        // The shape BcV2GTls.ValidatePeer hands over: the peer's certificate list, leaf first.
        Assert.That(options.ValidatePeerChain!([leaf.RawData, root.RawData]), Is.True);

        var seen = result();
        Assert.That(seen, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(seen!.Leaf,  Is.EqualTo(leaf.RawData), "the presented certificate is the leaf");
            Assert.That(seen.Extra,  Has.Length.EqualTo(1),    "and everything past it is what the peer sent");
            Assert.That(seen.Extra[0], Is.EqualTo(root.RawData));
        });
    }

    /// <summary>
    /// The leaf hook stays unset, and that is a decision rather than an omission: <c>ValidatePeer</c> runs
    /// both when both are set and requires both to pass, so a leaf-only invocation of a chain-checking
    /// callback would fail the handshake before the chain form ever ran — the exact failure being fixed.
    /// </summary>
    [Test]
    public void TheLeafOnlyHookIsLeftUnsetOnPurpose()
    {
        var (root, leaf) = Hierarchy();
        using var _1 = root;
        using var _2 = leaf;

        var (callback, _) = Recorder();

        Assert.Multiple(() =>
        {
            Assert.That(TlsPlatform.ToBcClientOptions(ClientOptions(callback)).ValidatePeerLeaf,        Is.Null);
            Assert.That(TlsPlatform.ToBcServerOptions(ServerOptions(leaf, callback)).ValidatePeerLeaf,  Is.Null);
        });
    }

    /// <summary>
    /// A peer that really does send a bare leaf must stay distinguishable from one whose chain we discarded
    /// — that indistinguishability *was* the 2026-08-08 defect, written up as a property of a counterparty.
    /// </summary>
    [Test]
    public void ABareLeafArrivesAsAnEmptyExtraStore()
    {
        var (root, leaf) = Hierarchy();
        using var _1 = root;
        using var _2 = leaf;

        var (callback, result) = Recorder();

        TlsPlatform.ToBcClientOptions(ClientOptions(callback)).ValidatePeerChain!([leaf.RawData]);

        Assert.That(result()!.Extra, Is.Empty);
    }

    /// <summary>The SECC side bridges the same way — a station validating a car's chain has the same need.</summary>
    [Test]
    public void TheCarsWholeChainReachesTheStationsCallback()
    {
        var (root, leaf) = Hierarchy();
        using var _1 = root;
        using var _2 = leaf;

        var (callback, result) = Recorder();
        var options            = TlsPlatform.ToBcServerOptions(ServerOptions(leaf, callback));

        Assert.That(options.ValidatePeerChain, Is.Not.Null);
        Assert.That(options.ValidatePeerChain!([leaf.RawData, root.RawData]), Is.True);
        Assert.That(result()!.Extra, Has.Length.EqualTo(1));
    }

    /// <summary>No callback configured stays no callback — this must not become "accept nothing".</summary>
    [Test]
    public void WithoutACallbackThereIsNoChainHook()
    {
        Assert.That(TlsPlatform.ToBcClientOptions(new TlsOptions
                    {
                        EnabledSslProtocols = SslProtocols.Tls13,
                        Backend             = TlsBackend.BouncyCastle
                    }).ValidatePeerChain,
                    Is.Null);
    }

    private static TlsOptions ServerOptions(X509Certificate2 leaf, RemoteCertificateValidationCallback validate) => new()
    {
        EnabledSslProtocols         = SslProtocols.Tls13,
        Backend                     = TlsBackend.BouncyCastle,
        ServerCertificate           = leaf,
        RequireClientCertificate    = true,
        ClientCertificateValidation = validate
    };

}
