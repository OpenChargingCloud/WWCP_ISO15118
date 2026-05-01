using NUnit.Framework;
using Vanaheimr.V2G.Sdp.Messages;
using Vanaheimr.V2G.V2GTP;

namespace Vanaheimr.V2G.Sdp.Tests;

[TestFixture]
public class SdpRequestWirelessTests
{
    [Test]
    public void Encode_HasFixedSize62()
    {
        var req = new SdpRequestWireless(
            Security:          SdpSecurity.Tls,
            TransportProtocol: SdpTransportProtocol.Tcp,
            P2psPpd:           0x1234,
            CouplingType:      0x01,
            Evid:              "ZZ00000",
            Evseid:            "DE*ABC*E12345*1");

        var bytes = req.EncodePayload();

        Assert.Multiple(() =>
        {
            Assert.That(bytes.Length, Is.EqualTo(SdpRequestWireless.PayloadSize));
            Assert.That(bytes[0],     Is.EqualTo(0x00));   // TLS
            Assert.That(bytes[1],     Is.EqualTo(0x00));   // TCP
            Assert.That(bytes[2],     Is.EqualTo(0x12));   // P2PS/PPD high byte
            Assert.That(bytes[3],     Is.EqualTo(0x34));   // P2PS/PPD low byte
            Assert.That(bytes[4],     Is.EqualTo(0x01));   // CouplingType
        });
    }

    [Test]
    public void RoundTrip_PreservesAllFields()
    {
        var orig = new SdpRequestWireless(
            Security:          SdpSecurity.Tls,
            TransportProtocol: SdpTransportProtocol.Tcp,
            P2psPpd:           0xCAFE,
            CouplingType:      0x02,
            Evid:              "ZZ12345",
            Evseid:            "DE*ABC*E12345*1");

        var decoded = SdpRequestWireless.Decode(orig.EncodePayload());

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Security,          Is.EqualTo(orig.Security));
            Assert.That(decoded.TransportProtocol, Is.EqualTo(orig.TransportProtocol));
            Assert.That(decoded.P2psPpd,           Is.EqualTo(orig.P2psPpd));
            Assert.That(decoded.CouplingType,      Is.EqualTo(orig.CouplingType));
            Assert.That(decoded.Evid,              Is.EqualTo(orig.Evid));
            Assert.That(decoded.Evseid,            Is.EqualTo(orig.Evseid));
        });
    }

    [Test]
    public void EncodeFrame_UsesPayloadType9002()
    {
        var req = new SdpRequestWireless(
            SdpSecurity.Tls, SdpTransportProtocol.Tcp,
            0, 0, "", "");

        var frame = req.EncodeFrame();

        Assert.Multiple(() =>
        {
            Assert.That(frame.Length, Is.EqualTo(V2GTPHeader.Size + SdpRequestWireless.PayloadSize));
            Assert.That(frame[2],     Is.EqualTo(0x90));
            Assert.That(frame[3],     Is.EqualTo(0x02));
        });
    }

    [Test]
    public void Decode_RejectsWrongLength()
    {
        Assert.Throws<ArgumentException>(() => SdpRequestWireless.Decode(new byte[61]));
        Assert.Throws<ArgumentException>(() => SdpRequestWireless.Decode(new byte[63]));
    }

    [Test]
    public void DefaultVersion_IsIso15118_20()
    {
        var req = new SdpRequestWireless(
            SdpSecurity.Tls, SdpTransportProtocol.Tcp, 0, 0, "", "");
        Assert.That(req.Version, Is.EqualTo(SdpVersion.Iso15118_20));
    }

    [Test]
    public void Evid_IsPaddedToTwentyBytes()
    {
        var req = new SdpRequestWireless(
            SdpSecurity.Tls, SdpTransportProtocol.Tcp, 0, 0, "X", "");
        var bytes = req.EncodePayload();

        // EVID lives at offset 5..24; first byte is 'X' = 0x58, the rest must be 0x00.
        Assert.Multiple(() =>
        {
            Assert.That(bytes[5], Is.EqualTo((byte)'X'));
            for (var i = 6; i < 25; i++)
                Assert.That(bytes[i], Is.EqualTo(0x00), $"byte at offset {i} not zero");
        });
    }
}

[TestFixture]
public class SdpResponseWirelessTests
{
    private static byte[] LinkLocalIpv6(byte tail)
    {
        var b = new byte[16];
        b[0] = 0xFE; b[1] = 0x80;
        b[15] = tail;
        return b;
    }

    [Test]
    public void Encode_HasFixedSize59()
    {
        var resp = new SdpResponseWireless(
            SeccIpAddress:     LinkLocalIpv6(0x01),
            SeccTcpPort:       64109,
            Security:          SdpSecurity.Tls,
            TransportProtocol: SdpTransportProtocol.Tcp,
            DiagStatus:        0x01,
            CouplingType:      0x02,
            Evseid:            "DE*ABC*E12345*1");

        var bytes = resp.EncodePayload();

        Assert.Multiple(() =>
        {
            Assert.That(bytes.Length, Is.EqualTo(SdpResponseWireless.PayloadSize));
            Assert.That(bytes[0],     Is.EqualTo(0xFE));                            // link-local prefix
            Assert.That(bytes[1],     Is.EqualTo(0x80));
            Assert.That(bytes[16],    Is.EqualTo(0xFA));                            // 64109 high byte
            Assert.That(bytes[17],    Is.EqualTo(0x6D));                            // 64109 low byte
            Assert.That(bytes[18],    Is.EqualTo((byte)SdpSecurity.Tls));
            Assert.That(bytes[19],    Is.EqualTo((byte)SdpTransportProtocol.Tcp));
            Assert.That(bytes[20],    Is.EqualTo(0x01));                            // DiagStatus
            Assert.That(bytes[21],    Is.EqualTo(0x02));                            // CouplingType
        });
    }

    [Test]
    public void RoundTrip_PreservesAllFields()
    {
        var orig = new SdpResponseWireless(
            SeccIpAddress:     LinkLocalIpv6(0x42),
            SeccTcpPort:       15119,
            Security:          SdpSecurity.Tls,
            TransportProtocol: SdpTransportProtocol.Tcp,
            DiagStatus:        0x02,
            CouplingType:      0x01,
            Evseid:            "DE*XYZ*E99999*9");

        var decoded = SdpResponseWireless.Decode(orig.EncodePayload());

        Assert.Multiple(() =>
        {
            Assert.That(decoded.SeccIpAddress,     Is.EqualTo(orig.SeccIpAddress));
            Assert.That(decoded.SeccTcpPort,       Is.EqualTo(orig.SeccTcpPort));
            Assert.That(decoded.Security,          Is.EqualTo(orig.Security));
            Assert.That(decoded.TransportProtocol, Is.EqualTo(orig.TransportProtocol));
            Assert.That(decoded.DiagStatus,        Is.EqualTo(orig.DiagStatus));
            Assert.That(decoded.CouplingType,      Is.EqualTo(orig.CouplingType));
            Assert.That(decoded.Evseid,            Is.EqualTo(orig.Evseid));
        });
    }

    [Test]
    public void EncodeFrame_UsesPayloadType9003_TotalLength67()
    {
        var resp = new SdpResponseWireless(
            LinkLocalIpv6(0x01), 64109, SdpSecurity.Tls, SdpTransportProtocol.Tcp,
            0x00, 0x00, "");

        var frame = resp.EncodeFrame();

        Assert.Multiple(() =>
        {
            Assert.That(frame.Length, Is.EqualTo(V2GTPHeader.Size + SdpResponseWireless.PayloadSize));
            Assert.That(frame.Length, Is.EqualTo(67));            // 8 + 59
            Assert.That(frame[2],     Is.EqualTo(0x90));
            Assert.That(frame[3],     Is.EqualTo(0x03));
            Assert.That(frame[7],     Is.EqualTo(0x3B));          // length = 59 = 0x3B
        });
    }

    [Test]
    public void Encode_RejectsBadIpAddressLength()
    {
        var resp = new SdpResponseWireless(
            new byte[15], 64109, SdpSecurity.Tls, SdpTransportProtocol.Tcp,
            0x00, 0x00, "");

        Assert.Throws<ArgumentException>(() => resp.EncodePayload());
    }

    [Test]
    public void Decode_RejectsWrongLength()
    {
        Assert.Throws<ArgumentException>(() => SdpResponseWireless.Decode(new byte[58]));
        Assert.Throws<ArgumentException>(() => SdpResponseWireless.Decode(new byte[60]));
    }
}
