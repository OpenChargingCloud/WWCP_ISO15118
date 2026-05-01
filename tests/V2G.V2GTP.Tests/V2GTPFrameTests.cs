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
