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

using NUnit.Framework;

namespace Vanaheimr.V2G.V2GTP.Tests;

[TestFixture]
public class V2GTPFrameTests
{
    [Test]
    public void Wrap_BuildsHeaderAutomatically()
    {
        var payload = new byte[] { 0x00, 0x00 };           // SDP_Request, TLS, TCP
        var frame   = V2GTPFrame.Wrap(V2GTPPayloadType.SdpRequest, payload);

        Assert.Multiple(() =>
        {
            Assert.That(frame.Header.PayloadType,    Is.EqualTo(V2GTPPayloadType.SdpRequest));
            Assert.That(frame.Header.PayloadLength,  Is.EqualTo(2u));
            Assert.That(frame.Header.IsVersionValid, Is.True);
        });
    }

    [Test]
    public void Parse_RoundTrips()
    {
        var payload = new byte[] { 0x10, 0x00 };           // no-TLS, TCP
        var bytes   = V2GTPFrame.Wrap(V2GTPPayloadType.SdpRequest, payload).ToArray();

        var f = V2GTPFrame.Parse(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(f.Header.PayloadType, Is.EqualTo(V2GTPPayloadType.SdpRequest));
            Assert.That(f.Payload.ToArray(),  Is.EqualTo(payload));
        });
    }

    [Test]
    public void Parse_RejectsLengthMismatch()
    {
        // header claims 100 bytes, buffer has only 2
        var bytes = new byte[]
        {
            0x01, 0xFE, 0x90, 0x00, 0x00, 0x00, 0x00, 0x64, // header: SDP_Req, len=100
            0x00, 0x00,
        };
        Assert.Throws<V2GTPPayloadLengthException>(() => V2GTPFrame.Parse(bytes));
    }

    [Test]
    public void ParseRaw_TruncatesGracefully_ForPentest()
    {
        var bytes = new byte[]
        {
            0x01, 0xFE, 0x90, 0x00, 0x00, 0x00, 0x00, 0x64, // header: len=100
            0x00, 0x00,
        };
        var f = V2GTPFrame.ParseRaw(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(f.Payload.Length,        Is.EqualTo(2));    // takes what's available
            Assert.That(f.Header.PayloadLength,  Is.EqualTo(100u)); // but reports declared length unchanged
        });
    }
}
