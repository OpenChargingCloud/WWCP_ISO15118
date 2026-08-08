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

using System.Security.Authentication;

using NUnit.Framework;

using Vanaheimr.V2G.Simulation.Transport;

namespace Vanaheimr.V2G.Simulation.Tests.Transport;

/// <summary>
/// Which backend a <see cref="TlsOptions"/> session lands on. The rules here are deliberately
/// platform-independent where they can be: the only assertion allowed to branch is the one about a
/// platform's <c>SslStream</c> capability, and it branches on
/// <see cref="TlsPlatform.SslStreamSupportsTls13"/> rather than on an <c>OperatingSystem</c> check, so
/// the expectation and the code under test cannot disagree about which platform this is.
/// </summary>
[TestFixture]
public class TlsBackendSelectionTests
{

    private static TlsOptions Options(SslProtocols protocols, TlsBackend backend = TlsBackend.Auto)
        => new() { EnabledSslProtocols = protocols, Backend = backend };

    [Test]
    public void AutoResolvesToSomethingConcrete()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TlsPlatform.ResolveBackend(Options(SslProtocols.Tls12)), Is.Not.EqualTo(TlsBackend.Auto));
            Assert.That(TlsPlatform.ResolveBackend(Options(SslProtocols.Tls13)), Is.Not.EqualTo(TlsBackend.Auto));
        });
    }

    [Test]
    public void AnIso2SessionStaysOnTheNativeStackEverywhere()
    {
        Assert.That(TlsPlatform.ResolveBackend(Options(SslProtocols.Tls12)), Is.EqualTo(TlsBackend.SslStream));
    }

    [Test]
    public void AutoRoutesTls13AwayFromSslStreamOnlyWhereItCannotServeIt()
    {
        var expected = TlsPlatform.SslStreamSupportsTls13
                           ? TlsBackend.SslStream
                           : TlsBackend.BouncyCastle;

        Assert.That(TlsPlatform.ResolveBackend(Options(SslProtocols.Tls13)), Is.EqualTo(expected));
    }

    // The Finding-4 fix: the managed stack used to be reachable only where SslStream lacked TLS 1.3, i.e.
    // macOS. A Windows EVCC therefore had to go through Schannel, which will not present a client chain
    // rooted outside the machine trust store — so a -20 mutual-TLS session against a test PKI could not be
    // run at all. Asking for the backend now works on every platform.
    [Test]
    public void TheManagedBackendIsReachableOnEveryPlatform()
    {
        Assert.That(TlsPlatform.ResolveBackend(Options(SslProtocols.Tls13, TlsBackend.BouncyCastle)),
                    Is.EqualTo(TlsBackend.BouncyCastle));
    }

    // A permissive set says "any of these will do", and 1.3 is in it — so serving 1.3 is not a deviation.
    [Test]
    public void TheManagedBackendAcceptsAPermissiveSetContainingTls13()
    {
        Assert.That(TlsPlatform.ResolveBackend(Options(SslProtocols.Tls12 | SslProtocols.Tls13, TlsBackend.BouncyCastle)),
                    Is.EqualTo(TlsBackend.BouncyCastle));
    }

    // BcV2GTlsClient/Server offer TLS 1.3 and nothing else, so a set without it would be answered with a
    // version nobody asked for. Refused up front rather than negotiated behind the caller's back.
    [Test]
    public void TheManagedBackendRefusesASessionThatRulesOutTls13(
        [Values(SslProtocols.Tls12, SslProtocols.None)] SslProtocols protocols)
    {
        Assert.That(() => TlsPlatform.ResolveBackend(Options(protocols, TlsBackend.BouncyCastle)),
                    Throws.TypeOf<NotSupportedException>().With.Message.Contains("TLS 1.3"));
    }

    [Test]
    public void TheNativeStackCanBeNamedExplicitly()
    {
        Assert.That(TlsPlatform.ResolveBackend(Options(SslProtocols.Tls12, TlsBackend.SslStream)),
                    Is.EqualTo(TlsBackend.SslStream));
    }

    // Naming SslStream is not a way to opt out of the platform's limits: where its TLS 1.3 does not exist
    // the request fails here, instead of surfacing as a PlatformNotSupportedException mid-handshake.
    [Test]
    public void NamingTheNativeStackForTls13FailsWhereItHasNoTls13()
    {
        var resolve = () => TlsPlatform.ResolveBackend(Options(SslProtocols.Tls13, TlsBackend.SslStream));

        if (TlsPlatform.SslStreamSupportsTls13)
            Assert.That(resolve(), Is.EqualTo(TlsBackend.SslStream));
        else
            Assert.That(() => resolve(), Throws.TypeOf<PlatformNotSupportedException>());
    }

}


/// <summary>
/// The <c>V2G_TLS_BACKEND</c> knob — for an interop run that drives a binary whose <see cref="TlsOptions"/>
/// it cannot edit. Ambient state, so these run alone and restore what they found.
/// </summary>
[TestFixture]
[NonParallelizable]
public class TlsBackendEnvironmentTests
{

    private string? saved;

    [SetUp]
    public void SaveEnvironment()
        => saved = Environment.GetEnvironmentVariable(TlsPlatform.BackendEnvironmentVariable);

    [TearDown]
    public void RestoreEnvironment()
        => Environment.SetEnvironmentVariable(TlsPlatform.BackendEnvironmentVariable, saved);

    private static void Set(string? value)
        => Environment.SetEnvironmentVariable(TlsPlatform.BackendEnvironmentVariable, value);

    private static TlsOptions Options(TlsBackend backend = TlsBackend.Auto)
        => new() { EnabledSslProtocols = SslProtocols.Tls13, Backend = backend };

    [Test]
    public void TheEnvironmentSelectsTheBackendForAnAutoSession()
    {
        Set("BouncyCastle");

        Assert.That(TlsPlatform.ResolveBackend(Options()), Is.EqualTo(TlsBackend.BouncyCastle));
    }

    [Test]
    public void TheEnvironmentIsCaseInsensitiveAndTrimmed()
    {
        Set("  bouncycastle ");

        Assert.That(TlsPlatform.ResolveBackend(Options()), Is.EqualTo(TlsBackend.BouncyCastle));
    }

    // Precedence, and the direction matters: a session that states its backend is stating part of its
    // protocol profile, and an ambient variable must not quietly overrule it.
    [Test]
    public void AnExplicitBackendOutranksTheEnvironment()
    {
        Set("BouncyCastle");

        Assert.That(TlsPlatform.ResolveBackend(new TlsOptions
                    {
                        EnabledSslProtocols = SslProtocols.Tls12,
                        Backend             = TlsBackend.SslStream
                    }),
                    Is.EqualTo(TlsBackend.SslStream));
    }

    [Test]
    public void AnUnsetOrBlankVariableChangesNothing([Values(null, "", "   ")] string? value)
    {
        Set(value);

        Assert.That(TlsPlatform.ResolveBackend(new TlsOptions { EnabledSslProtocols = SslProtocols.Tls12 }),
                    Is.EqualTo(TlsBackend.SslStream));
    }

    // A typo that silently left the session on the platform stack is the failure this knob exists to
    // prevent, so an unreadable value stops the session rather than being ignored.
    [Test]
    public void AMisspelledValueIsRefusedRatherThanIgnored()
    {
        Set("bouncy-castle");

        Assert.That(() => TlsPlatform.ResolveBackend(Options()),
                    Throws.TypeOf<ArgumentException>().With.Message.Contains("BouncyCastle"));
    }

    [Test]
    public void ParseBackendReadsTheBackendNames()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TlsPlatform.ParseBackend("auto"),          Is.EqualTo(TlsBackend.Auto));
            Assert.That(TlsPlatform.ParseBackend("SslStream"),     Is.EqualTo(TlsBackend.SslStream));
            Assert.That(TlsPlatform.ParseBackend("BOUNCYCASTLE"),  Is.EqualTo(TlsBackend.BouncyCastle));
            Assert.That(TlsPlatform.ParseBackend(null),            Is.Null);
            Assert.That(TlsPlatform.ParseBackend("  "),            Is.Null);
        });
    }

    // Enum.TryParse also accepts the numeric form, and would happily hand back an undefined value —
    // hence the Enum.IsDefined guard behind ParseBackend.
    [Test]
    public void ParseBackendRejectsAnOutOfRangeNumber()
    {
        Assert.That(() => TlsPlatform.ParseBackend("99"), Throws.TypeOf<ArgumentException>());
    }

}
