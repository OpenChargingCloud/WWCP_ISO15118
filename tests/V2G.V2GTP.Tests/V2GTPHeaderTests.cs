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

namespace Vanaheimr.V2G.V2GTPCodec.Tests;

[TestFixture]
public class V2GTPHeaderTests
{
    [Test]
    public void Standard_RoundTrips()
    {
        var h = V2GTPHeader.Standard(V2GTPPayloadType.SdpRequest, 2);

        var buf = new byte[V2GTPHeader.Size];
        h.WriteTo(buf);

        // 01 FE 90 00 00 00 00 02
        Assert.That(buf, Is.EqualTo(new byte[] { 0x01, 0xFE, 0x90, 0x00, 0x00, 0x00, 0x00, 0x02 }));

        var parsed = V2GTPHeader.Parse(buf);
        Assert.That(parsed,                Is.EqualTo(h));
        Assert.That(parsed.IsVersionValid, Is.True);
    }

    [Test]
    public void SdpResponse_PayloadLength_IsTwenty_OnTheWire()
    {
        var h   = V2GTPHeader.Standard(V2GTPPayloadType.SdpResponse, 20);
        var buf = h.ToArray();

        // 01 FE 90 01 00 00 00 14
        Assert.Multiple(() =>
        {
            Assert.That(buf[0], Is.EqualTo(0x01));
            Assert.That(buf[1], Is.EqualTo(0xFE));
            Assert.That(buf[2], Is.EqualTo(0x90));
            Assert.That(buf[3], Is.EqualTo(0x01));
            Assert.That(buf[4], Is.EqualTo(0x00));
            Assert.That(buf[5], Is.EqualTo(0x00));
            Assert.That(buf[6], Is.EqualTo(0x00));
            Assert.That(buf[7], Is.EqualTo(0x14));
        });
    }

    [Test]
    public void Parse_RejectsBadInverseVersion()
    {
        var bad = new byte[] { 0x01, 0x00, 0x90, 0x00, 0x00, 0x00, 0x00, 0x02 };
        Assert.Throws<V2GTPProtocolVersionException>(() => V2GTPHeader.Parse(bad));
    }

    [Test]
    public void ParseRaw_AcceptsBadInverseVersion_ForPentest()
    {
        var bad = new byte[] { 0x01, 0x00, 0x90, 0x00, 0x00, 0x00, 0x00, 0x02 };
        var h   = V2GTPHeader.ParseRaw(bad);

        Assert.Multiple(() =>
        {
            Assert.That(h.IsVersionValid, Is.False);
            Assert.That(h.PayloadType,    Is.EqualTo(V2GTPPayloadType.SdpRequest));
            Assert.That(h.PayloadLength,  Is.EqualTo(2u));
        });
    }

    [Test]
    public void Parse_ThrowsOnTruncatedBuffer()
    {
        var truncated = new byte[] { 0x01, 0xFE, 0x90 };
        Assert.Throws<V2GTPTruncatedException>(() => V2GTPHeader.Parse(truncated));
    }

    [TestCase((ushort)0x8001, V2GTPPayloadType.ExiMainstream)]
    [TestCase((ushort)0x8002, V2GTPPayloadType.ExiAC)]
    [TestCase((ushort)0x9000, V2GTPPayloadType.SdpRequest)]
    [TestCase((ushort)0x9001, V2GTPPayloadType.SdpResponse)]
    [TestCase((ushort)0x9002, V2GTPPayloadType.SdpRequestWireless)]
    [TestCase((ushort)0x9003, V2GTPPayloadType.SdpResponseWireless)]
    public void PayloadTypes_HaveExpectedNumericValues(ushort wire, V2GTPPayloadType expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That((V2GTPPayloadType)wire, Is.EqualTo(expected));
            Assert.That(expected.IsKnown(),     Is.True);
        });
    }

    [Test]
    public void UnknownPayloadType_IsNotKnown()
    {
        Assert.That(((V2GTPPayloadType)0xA000).IsKnown(), Is.False);
    }
}
