using System.Net;
using NUnit.Framework;
using Vanaheimr.V2G.Sdp.Messages;
using Vanaheimr.V2G.V2GTP;

namespace Vanaheimr.V2G.Sdp.Tests;

[TestFixture]
public class SdpRequestTests
{
    [Test]
    public void Encode_TlsTcp_TwoBytes_00_00()
    {
        var req = new SdpRequest(SdpSecurity.Tls, SdpTransportProtocol.Tcp);
        Assert.That(req.EncodePayload(), Is.EqualTo(new byte[] { 0x00, 0x00 }));
    }

    [Test]
    public void Encode_NoTlsTcp_TwoBytes_10_00()
    {
        var req = new SdpRequest(SdpSecurity.NoTls, SdpTransportProtocol.Tcp);
        Assert.That(req.EncodePayload(), Is.EqualTo(new byte[] { 0x10, 0x00 }));
    }

    [Test]
    public void Decode_RoundTrips()
    {
        var bytes = new byte[] { 0x10, 0x00 };
        var req   = SdpRequest.Decode(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(req.Security,          Is.EqualTo(SdpSecurity.NoTls));
            Assert.That(req.TransportProtocol, Is.EqualTo(SdpTransportProtocol.Tcp));
        });
    }

    [Test]
    public void Decode_RejectsWrongLength()
    {
        Assert.Throws<ArgumentException>(() => SdpRequest.Decode(new byte[] { 0x00 }));
        Assert.Throws<ArgumentException>(() => SdpRequest.Decode(new byte[] { 0x00, 0x00, 0x00 }));
    }

    [Test]
    public void DecodeLenient_AcceptsTrailingBytes()
    {
        var bytes = new byte[] { 0x00, 0x00, 0xCA, 0xFE };
        var req   = SdpRequest.DecodeLenient(bytes);
        Assert.That(req.Security, Is.EqualTo(SdpSecurity.Tls));
    }

    [Test]
    public void EncodeFrame_HasV2GTPHeader_PayloadType9000()
    {
        var req   = new SdpRequest(SdpSecurity.Tls, SdpTransportProtocol.Tcp);
        var frame = req.EncodeFrame();

        Assert.Multiple(() =>
        {
            Assert.That(frame.Length, Is.EqualTo(10));            // 8 header + 2 payload
            Assert.That(frame[2],     Is.EqualTo(0x90));
            Assert.That(frame[3],     Is.EqualTo(0x00));
        });
    }
}

[TestFixture]
public class SdpResponseTests
{
    [Test]
    public void Encode_LinkLocalSecc_RoundTrips()
    {
        var addr  = IPAddress.Parse("fe80::1234:5678:9abc:def0");
        var resp  = new SdpResponse(addr, 64109, SdpSecurity.Tls, SdpTransportProtocol.Tcp);
        var bytes = resp.EncodePayload();

        Assert.That(bytes.Length, Is.EqualTo(SdpResponse.PayloadSize));

        var parsed = SdpResponse.Decode(bytes);
        Assert.Multiple(() =>
        {
            Assert.That(parsed.SeccIPAddress,     Is.EqualTo(addr));
            Assert.That(parsed.SeccPort,          Is.EqualTo((ushort)64109));
            Assert.That(parsed.Security,          Is.EqualTo(SdpSecurity.Tls));
            Assert.That(parsed.TransportProtocol, Is.EqualTo(SdpTransportProtocol.Tcp));
        });
    }

    [Test]
    public void Encode_PortBigEndian()
    {
        var addr  = IPAddress.Parse("fe80::1");
        var resp  = new SdpResponse(addr, 0xFA01, SdpSecurity.Tls, SdpTransportProtocol.Tcp);
        var bytes = resp.EncodePayload();

        Assert.Multiple(() =>
        {
            Assert.That(bytes[16], Is.EqualTo(0xFA));
            Assert.That(bytes[17], Is.EqualTo(0x01));
        });
    }

    [Test]
    public void Encode_RejectsIPv4()
    {
        var v4   = IPAddress.Parse("192.0.2.1");
        var resp = new SdpResponse(v4, 1234, SdpSecurity.Tls, SdpTransportProtocol.Tcp);
        Assert.Throws<InvalidOperationException>(() => resp.EncodePayload());
    }

    [Test]
    public void Decode_RejectsWrongLength()
    {
        Assert.Throws<ArgumentException>(() => SdpResponse.Decode(new byte[19]));
        Assert.Throws<ArgumentException>(() => SdpResponse.Decode(new byte[21]));
    }

    [Test]
    public void EncodeFrame_HasV2GTPHeader_PayloadType9001_Length20()
    {
        var resp  = new SdpResponse(IPAddress.Parse("fe80::1"), 15119, SdpSecurity.Tls, SdpTransportProtocol.Tcp);
        var frame = resp.EncodeFrame();

        Assert.Multiple(() =>
        {
            Assert.That(frame.Length, Is.EqualTo(28));            // 8 + 20
            Assert.That(frame[2],     Is.EqualTo(0x90));
            Assert.That(frame[3],     Is.EqualTo(0x01));
            Assert.That(frame[7],     Is.EqualTo(0x14));          // big-endian length = 20 = 0x14
        });

        // round-trip via V2GTPFrame
        var f      = V2GTPFrame.Parse(frame);
        var parsed = SdpResponse.Decode(f.Payload.Span);

        Assert.Multiple(() =>
        {
            Assert.That(f.Header.PayloadType, Is.EqualTo(V2GTPPayloadType.SdpResponse));
            Assert.That(parsed.SeccPort,      Is.EqualTo((ushort)15119));
        });
    }
}
