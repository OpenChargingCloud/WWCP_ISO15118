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

using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;

using NUnit.Framework;

using cloud.charging.open.protocols.ISO15118.Sap;
using cloud.charging.open.protocols.ISO15118.Session;
using cloud.charging.open.protocols.ISO15118.StateMachines;

namespace cloud.charging.open.protocols.ISO15118.Tests.Sap;

/// <summary>
/// ISO 15118-20 may only ride on TLS 1.3: <c>[V2G20-1237]</c> forbids the car to <i>offer</i> it anywhere
/// else, <c>[V2G20-2356]</c> forbids the station to <i>select</i> it there, and Table 5 lists <c>-20</c>
/// in the 1.3 row alone.
/// </summary>
/// <remarks>
/// <para>
/// The first of these is a defect this project committed against a real station. On 2026-08-06 our EVCC
/// offered both protocols over a connection that had negotiated TLS 1.2, EVerest's <c>IsoMux</c> selected
/// the <c>-20</c> entry, and a complete DC session ran on a profile the standard does not allow. Their
/// half is filed; <c>AMixedOfferLosesIso20OnAnInsecureTransport</c> is ours, and it fails against the code
/// as it stood that day.
/// </para>
/// <para>
/// Note what is <b>not</b> asserted: that an insecure transport stops a session. It does not, by design —
/// <see cref="TransportSecurity.Unknown"/> stands the rule down, most of this project's interop matrix
/// runs <c>-20</c> over plain TCP on purpose, and the two runnable peers say out loud when they use that.
/// The defect was silence, not the plain-TCP run.
/// </para>
/// </remarks>
[TestFixture]
public class Iso20TransportTests
{

    #region The classification itself

    [TestCase(SslProtocols.Tls13, TransportSecurity.Tls13)]
    [TestCase(SslProtocols.Tls12, TransportSecurity.Tls12OrLower)]
    [TestCase(SslProtocols.None,  TransportSecurity.Unknown)]
    public void AVersionIsClassifiedByWhatItMayCarry(SslProtocols protocol, TransportSecurity expected)
        => Assert.That(Iso20Transport.FromSslProtocol(protocol), Is.EqualTo(expected));

    /// <summary>An unauthenticated stream reports <c>None</c>, and it means "not yet" — reading that as
    /// "no encryption" would be the wrong answer in the direction that matters.</summary>
    [Test]
    public void AnUnfinishedHandshakeIsUnknown_NotPlaintext()
        => Assert.That(Iso20Transport.FromSslProtocol(SslProtocols.None), Is.Not.EqualTo(TransportSecurity.None));

    [Test]
    public void MayCarryIso20_OnlyOnTls13_OrWhenNothingWasClaimed()
        => Assert.Multiple(() =>
        {
            Assert.That(Iso20Transport.MayCarryIso20(TransportSecurity.Tls13),        Is.True);
            Assert.That(Iso20Transport.MayCarryIso20(TransportSecurity.Unknown),      Is.True, "the deliberate stand-down");
            Assert.That(Iso20Transport.MayCarryIso20(TransportSecurity.Tls12OrLower), Is.False);
            Assert.That(Iso20Transport.MayCarryIso20(TransportSecurity.None),         Is.False);
        });

    [Test]
    public async Task ABareSocketIsRecognisedAsPlainTcp()
    {
        var (evcc, secc) = await LoopbackAsync();
        using (evcc)
        using (secc)
            Assert.That(Iso20Transport.Of(evcc), Is.EqualTo(TransportSecurity.None));
    }

    /// <summary>Anything else — the BouncyCastle backend's stream, every in-process test double — is
    /// <c>Unknown</c> rather than guessed at from its type.</summary>
    [Test]
    public void AnUnfamiliarStreamIsNotGuessedAt()
        => Assert.That(Iso20Transport.Of(new MemoryStream()), Is.EqualTo(TransportSecurity.Unknown));

    #endregion

    #region The car — [V2G20-1237]

    /// <summary>
    /// <b>The 2026-08-06 regression.</b> Both protocols offered on a TLS 1.2 connection: the <c>-20</c>
    /// entry must not leave the car, and the <c>-2</c> entry must — so the session still happens, on the
    /// protocol that connection may carry.
    /// </summary>
    [TestCase(TransportSecurity.Tls12OrLower)]
    [TestCase(TransportSecurity.None)]
    public async Task AMixedOfferLosesIso20OnAnInsecureTransport(TransportSecurity transport)
    {
        var (evcc, secc) = await LoopbackAsync();
        using (evcc)
        using (secc)
        {
            // A station that supports both, so nothing but the rule can decide the answer.
            var station = SapHandshake.RunSeccSideAsync(secc, BothOffers, Ct);
            var settled = await SapHandshake.RunEvccSideAsync(evcc, BothOffers, Ct, transport);
            var seen    = await station;

            Assert.Multiple(() =>
            {
                Assert.That(settled.Protocol, Is.EqualTo(ProtocolVariant.Iso15118_2),
                            "the car ran -20 on a connection that may not carry it");
                Assert.That(seen.Protocol,    Is.EqualTo(ProtocolVariant.Iso15118_2),
                            "the station was offered -20 on a connection that may not carry it");
            });
        }
    }

    [Test]
    public async Task AMixedOfferKeepsIso20OnTls13()
    {
        var (evcc, secc) = await LoopbackAsync();
        using (evcc)
        using (secc)
        {
            var station = SapHandshake.RunSeccSideAsync(secc, BothOffers, Ct, TransportSecurity.Tls13);
            var settled = await SapHandshake.RunEvccSideAsync(evcc, BothOffers, Ct, TransportSecurity.Tls13);

            Assert.That(settled.Protocol, Is.EqualTo(ProtocolVariant.Iso15118_20));
            Assert.That((await station).Protocol, Is.EqualTo(ProtocolVariant.Iso15118_20));
        }
    }

    /// <summary>The stand-down, and it is the case most of this project's runs are in.</summary>
    [Test]
    public async Task AnUnstatedTransportOffersIso20OverPlainTcp()
    {
        var (evcc, secc) = await LoopbackAsync();
        using (evcc)
        using (secc)
        {
            var station = SapHandshake.RunSeccSideAsync(secc, BothOffers, Ct);
            var settled = await SapHandshake.RunEvccSideAsync(evcc, BothOffers, Ct);

            Assert.That(settled.Protocol, Is.EqualTo(ProtocolVariant.Iso15118_20));
            Assert.That((await station).Protocol, Is.EqualTo(ProtocolVariant.Iso15118_20));
        }
    }

    /// <summary>
    /// A <c>-20</c>-only car has nothing left to offer, and an empty <c>SupportedAppProtocolReq</c> is not
    /// a legal message — so this aborts before anything goes on the wire, naming the requirement and how
    /// to proceed on purpose.
    /// </summary>
    [Test]
    public async Task AnIso20OnlyOfferAbortsRatherThanGoingOutEmpty()
    {
        var (evcc, secc) = await LoopbackAsync();
        using (evcc)
        using (secc)
        {
            var aborted = Assert.ThrowsAsync<SessionAborted>(async () =>
                await SapHandshake.RunEvccSideAsync(evcc, [new SapOffer(ProtocolVariant.Iso15118_20)],
                                                    Ct, TransportSecurity.Tls12OrLower));

            Assert.That(aborted!.Message, Does.Contain("[V2G20-1237]"));
        }
    }

    #endregion

    #region The station — [V2G20-2356]

    /// <summary>
    /// The station's obligation is written separately from the car's precisely for the case where the car
    /// got it wrong — which is the case here, and the one EVerest's <c>IsoMux</c> is on the wrong side of.
    /// The car offers <c>-20</c> at priority 1 anyway; the station may not take it.
    /// </summary>
    [Test]
    public async Task AStationDoesNotSelectIso20ItWasOfferedOnAnInsecureTransport()
    {
        var (evcc, secc) = await LoopbackAsync();
        using (evcc)
        using (secc)
        {
            var station = SapHandshake.RunSeccSideAsync(secc, BothOffers, Ct, TransportSecurity.Tls12OrLower);

            // The car states nothing, so it offers -20 first — a non-conformant offer, deliberately.
            var settled = await SapHandshake.RunEvccSideAsync(evcc, BothOffers, Ct);
            var chosen  = await station;

            Assert.Multiple(() =>
            {
                Assert.That(chosen.Protocol,  Is.EqualTo(ProtocolVariant.Iso15118_2));
                Assert.That(settled.Protocol, Is.EqualTo(ProtocolVariant.Iso15118_2),
                            "the car must follow the answered SchemaID, not its own ranking");
            });
        }
    }

    [Test]
    public async Task AStationRefusesAnIso20OnlyOfferOnAnInsecureTransport()
    {
        var (evcc, secc) = await LoopbackAsync();
        using (evcc)
        using (secc)
        {
            var station = SapHandshake.RunSeccSideAsync(secc, BothOffers, Ct, TransportSecurity.None);
            var car     = SapHandshake.RunEvccSideAsync(evcc, [new SapOffer(ProtocolVariant.Iso15118_20)], Ct);

            var refused = Assert.ThrowsAsync<SessionAborted>(async () => await station);
            Assert.That(refused!.Message, Does.Contain("[V2G20-2356]"));

            // And the car is told, rather than left waiting: Failed_NoNegotiation on the wire.
            var seen = Assert.ThrowsAsync<SessionAborted>(async () => await car);
            Assert.That(seen!.Message, Does.Contain("Failed_NoNegotiation"));
        }
    }

    #endregion

    #region Plumbing

    /// <summary>Nothing here talks to a peer that might not answer: every handshake is bounded, so a
    /// rule that stops applying deadlocks the suite instead of hanging it.</summary>
    private static CancellationToken Ct => new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

    private static SapOffer[] BothOffers =>
        [new(ProtocolVariant.Iso15118_20), new(ProtocolVariant.Iso15118_2)];

    /// <summary>A real socket pair on the loopback interface, so <see cref="Iso20Transport.Of"/> is asked
    /// about the stream type it was written for rather than about a stand-in.</summary>
    private static async Task<(Stream Evcc, Stream Secc)> LoopbackAsync()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var accepting = listener.AcceptTcpClientAsync();
            var client    = new TcpClient();
            await client.ConnectAsync((IPEndPoint) listener.LocalEndpoint);
            var server    = await accepting;
            return (client.GetStream(), server.GetStream());
        }
        finally
        {
            listener.Stop();
        }
    }

    #endregion

}
