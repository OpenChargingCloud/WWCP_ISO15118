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

using NUnit.Framework;

using cloud.charging.open.protocols.ISO15118.StateMachines.Iso20;
using cloud.charging.open.protocols.ISO15118.Transport.BouncyCastle;

namespace cloud.charging.open.protocols.ISO15118.Tests.StateMachines;

/// <summary>
/// Where the `-20` session binding gets the peer's certificate from.
/// </summary>
/// <remarks>
/// A binding decides who may rejoin a paused session, so a peer this cannot see is a resume that
/// can never succeed. That is not hypothetical: matching only <c>SslStream</c> made every
/// BouncyCastle session pause unbound, and since BouncyCastle is the backend a conformant `-20`
/// profile needs wherever Schannel cannot provide one, the conformant profile and pause/resume were
/// mutually exclusive on those platforms. Nothing failed loudly - the station said "unbound" and
/// refused the rejoin, which reads like a policy decision rather than a missing case.
/// </remarks>
[TestFixture]
public class SessionBinding20Tests
{

    private static readonly Byte[] leaf      = [ 0x30, 0x82, 0x01, 0x0A, 0xDE, 0xAD, 0xBE, 0xEF ];
    private static readonly Byte[] sessionId = [ 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 ];


    #region A_BouncyCastle_Stream_Yields_Its_Peer_Leaf()

    /// <summary>
    /// The case that was missing. A BouncyCastle session is a full mutual-TLS handshake and its peer
    /// is as known as any <c>SslStream</c>'s.
    /// </summary>
    [Test]
    public void A_BouncyCastle_Stream_Yields_Its_Peer_Leaf()
    {

        var stream = new BcTlsStream(Stream.Null, leaf);

        Assert.That(SessionBinding20.PeerLeafOf(stream), Is.EqualTo(leaf));

    }

    #endregion

    #region A_BouncyCastle_Stream_Without_A_Peer_Certificate_Yields_Nothing()

    /// <summary>
    /// Unilateral TLS: the station asked for no client certificate, so there is nothing to bind to
    /// and the answer must stay null rather than becoming an empty array that could compare equal to
    /// another.
    /// </summary>
    [Test]
    public void A_BouncyCastle_Stream_Without_A_Peer_Certificate_Yields_Nothing()
    {

        Assert.That(SessionBinding20.PeerLeafOf(new BcTlsStream(Stream.Null, null)), Is.Null);
        Assert.That(SessionBinding20.PeerLeafOf(new BcTlsStream(Stream.Null, [])),   Is.Null);

    }

    #endregion

    #region A_Plain_Stream_Yields_Nothing()

    /// <summary>
    /// Plain TCP is outside the protocol, and a session over it genuinely cannot be bound. This is
    /// the one case where a null is the right answer rather than a missing one.
    /// </summary>
    [Test]
    public void A_Plain_Stream_Yields_Nothing()
    {

        Assert.That(SessionBinding20.PeerLeafOf(Stream.Null), Is.Null);
        Assert.That(SessionBinding20.PeerLeafOf(null),        Is.Null);

    }

    #endregion

    #region An_Unbindable_Session_Never_Matches()

    /// <summary>
    /// The property the whole type rests on: a missing certificate produces no binding, and no
    /// binding matches nothing - least of all another absent one.
    /// </summary>
    [Test]
    public void An_Unbindable_Session_Never_Matches()
    {

        Assert.That(SessionBinding20.Compute(sessionId, null),                     Is.Null);
        Assert.That(SessionBinding20.Compute(null,      leaf),                     Is.Null);
        Assert.That(SessionBinding20.Matches(null,      null),                     Is.False);
        Assert.That(SessionBinding20.Matches(SessionBinding20.Compute(sessionId, leaf), null), Is.False);

    }

    #endregion

    #region The_Same_Peer_And_Session_Bind_Alike()

    /// <summary>
    /// What a rejoin relies on: the binding a station stored at pause time is reproducible from the
    /// same session id and the same certificate, and nothing else reproduces it.
    /// </summary>
    [Test]
    public void The_Same_Peer_And_Session_Bind_Alike()
    {

        var stored     = SessionBinding20.Compute(sessionId, leaf);
        var presented  = SessionBinding20.Compute(sessionId, leaf);

        Assert.That(SessionBinding20.Matches(stored, presented), Is.True);

        // A different car naming the same session id: the case the binding exists to refuse.
        var stranger   = SessionBinding20.Compute(sessionId, [ 0x30, 0x82, 0x01, 0x0A, 0xC0, 0xFF, 0xEE, 0x00 ]);

        Assert.That(SessionBinding20.Matches(stored, stranger),  Is.False);

    }

    #endregion

}
