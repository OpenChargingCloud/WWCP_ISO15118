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

using cloud.charging.open.protocols.ISO15118.EXI;
using cloud.charging.open.protocols.ISO15118_20.CommonMessages.Generated;

namespace cloud.charging.open.protocols.ISO15118.EXI.Tests;

/// <summary>
/// An encoded message must depend on the message and on nothing else — in particular not on what the
/// destination buffer happened to hold beforehand.
/// </summary>
/// <remarks>
/// <para>
/// This is a regression test for a leak found on 2026-07-31 while building the session-trace corpus.
/// <see cref="BitWriter"/> wrote only the bits a message actually occupies, so the unused bits of the
/// final, partial byte kept whatever was already there. Sessions reuse one send buffer, so "whatever was
/// already there" is the previous message: two recordings of the identical -20 <c>ServiceDiscoveryRes</c>
/// differed in the low six bits of their last byte, holding leftovers of the <c>AuthorizationSetupRes</c>
/// encoded one message earlier. Its GenChallenge is random, which is the only reason the difference was
/// visible; with a deterministic predecessor the same bits would have leaked silently and identically
/// every time.
/// </para>
/// <para>
/// Worth stating plainly, because it decides how much the leak matters: up to seven bits of the preceding
/// message travel on the wire. In a PnC session the preceding message is a contract chain or a signature.
/// </para>
/// <para>
/// <b>Why nothing caught it.</b> A round trip never reads padding, so encoder and decoder agreed. The
/// vector corpus encodes each message into its own fresh — therefore zeroed — array, so the recorded
/// bytes are exactly the ones a clean buffer produces and the corpus stayed green. Both gates were
/// looking; neither was looking here. That is the same shape as the two bugs the cross-emitter comparison
/// found (<c>docs/CONCEPT.md</c> §5, Track A note), and the same lesson: a check that only ever compares
/// a codec against itself, or against inputs it also controls, has a blind spot exactly where those two
/// overlap.
/// </para>
/// <para>
/// The Swift back end never had this: its writer appends a zero byte as it grows and its
/// <c>alignToByte</c> writes the padding bits out. Kotlin did, and was fixed with C#.
/// </para>
/// </remarks>
[TestFixture]
public class BitWriterBufferHygieneTests
{

    /// <summary>Two bits into a buffer full of 1s: the six unused bits of that byte must be zero, not
    /// the 1s that were there.</summary>
    [Test]
    public void TheTrailingPartialByteIsCleared()
    {

        var buffer = new byte[4];
        Array.Fill(buffer, (byte) 0xFF);

        var writer = new BitWriter(buffer);
        writer.WriteBits(0b01, 2);

        Assert.That(writer.BytesWritten, Is.EqualTo(1));
        Assert.That(buffer[0], Is.EqualTo(0b0100_0000),
                    "the six bits after the message are padding and must not carry the old contents");

    }


    /// <summary>Whole bytes in the middle of a message too — the case that was already handled, kept so a
    /// future rewrite cannot trade one hazard for the other.</summary>
    [Test]
    public void StaleBitsInsideTheMessageAreOverwritten()
    {

        var buffer = new byte[4];
        Array.Fill(buffer, (byte) 0xFF);

        var writer = new BitWriter(buffer);
        writer.WriteBits(0x00, 16);

        Assert.That(buffer[0], Is.EqualTo(0x00));
        Assert.That(buffer[1], Is.EqualTo(0x00));

    }


    /// <summary>The finding as it actually appeared: one real message, two buffers, same bytes.</summary>
    [Test]
    public void AMessageEncodesIdenticallyIntoACleanAndADirtyBuffer()
    {

        var message = new ServiceDiscoveryRes(
                          new MessageHeaderType([10, 11, 12, 13, 14, 15, 16, 17], 1_767_225_600UL, null),
                          ResponseCode.OK,
                          ServiceRenegotiationSupported: true,
                          new ServiceListType([new ServiceType(1, FreeService: true)]),
                          VASList: null);

        var clean = new byte[256];
        var dirty = new byte[256];
        Array.Fill(dirty, (byte) 0xFF);

        Assert.That(message.TryEncode(clean, out var cleanLength), Is.True);
        Assert.That(message.TryEncode(dirty, out var dirtyLength), Is.True);

        Assert.That(dirtyLength, Is.EqualTo(cleanLength));
        Assert.That(Convert.ToHexString(dirty.AsSpan(0, dirtyLength)),
                    Is.EqualTo(Convert.ToHexString(clean.AsSpan(0, cleanLength))),
                    "the encoding must not depend on the destination buffer's previous contents");

    }


    /// <summary>
    /// And the way it reaches the wire: one buffer, two messages in a row, as a session does it. The
    /// second encode must produce what it would have produced on its own.
    /// </summary>
    [Test]
    public void ReusingOneBufferAcrossMessagesDoesNotCarryTheFirstIntoTheSecond()
    {

        var header = new MessageHeaderType([10, 11, 12, 13, 14, 15, 16, 17], 1_767_225_600UL, null);

        var first  = new AuthorizationSetupRes(
                         header, ResponseCode.OK,
                         new[] { Authorization.EIM },
                         CertificateInstallationService: false,
                         new EIM_ASResAuthorizationModeType(),
                         PnC_ASResAuthorizationMode: null);

        var second = new ServiceDiscoveryRes(header, ResponseCode.OK, true,
                                             new ServiceListType([new ServiceType(1, true)]), null);

        var shared = new byte[512];
        Assert.That(first.TryEncode(shared, out _), Is.True);
        Assert.That(second.TryEncode(shared, out var reusedLength), Is.True);

        var alone = new byte[512];
        Assert.That(second.TryEncode(alone, out var aloneLength), Is.True);

        Assert.That(Convert.ToHexString(shared.AsSpan(0, reusedLength)),
                    Is.EqualTo(Convert.ToHexString(alone.AsSpan(0, aloneLength))),
                    "a message encoded after another one must not carry any of it");

    }

}
