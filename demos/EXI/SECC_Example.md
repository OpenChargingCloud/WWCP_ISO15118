# A minimal SECC: receiving messages from an EV

The whole loop, in one file. It accepts one EV, completes the protocol handshake, and answers
`SessionSetupReq`. This was compiled against the three project references below before it was
written down — which is also how the second `ResponseCode` was found.

Start from [`README.md`](../../README.md) if you have not fetched the schemas yet; nothing here
builds until `bash tools/download-schemas.sh` has run. Every command below is written from the
repository root, not from this directory.

## The project

```xml
<ItemGroup>
  <ProjectReference Include="…\WWCP_ISO15118\WWCP_ISO15118_EXI\WWCP_ISO15118_EXI.csproj" />
  <ProjectReference Include="…\WWCP_ISO15118\WWCP_ISO15118_2\WWCP_ISO15118_2.csproj" />
  <ProjectReference Include="…\WWCP_ISO15118\WWCP_ISO15118_EXI_Dispatch\WWCP_ISO15118_EXI_Dispatch.csproj" />
</ItemGroup>
```

Nothing else to configure. There is no NuGet package, no generator setting to switch on, and no
build step of your own: referencing a message-set project is enough, because the generator runs
inside *its* compilation and ships the finished types in its assembly. Target `net10.0`.

## The loop

```csharp
using System.Net;
using System.Net.Sockets;

using cloud.charging.open.protocols.ISO15118.AppProtocol;
using cloud.charging.open.protocols.ISO15118.EXI.Dispatch;
using cloud.charging.open.protocols.ISO15118_2.Generated;

// Two ResponseCode enums are in play and neither is a superset of the other — the handshake's
// three values are about protocol negotiation, -2's about the charging session.
using SapResponseCode  = cloud.charging.open.protocols.ISO15118.AppProtocol.ResponseCode;
using Iso2ResponseCode = cloud.charging.open.protocols.ISO15118_2.Generated.ResponseCode;

var listener = new TcpListener(IPAddress.IPv6Loopback, 15118);
listener.Start();
Console.WriteLine("SECC listening on [::1]:15118");

using var client = await listener.AcceptTcpClientAsync();
using var stream = client.GetStream();

var frame      = new byte[V2GTP.HeaderSize + 8192];   // one whole frame, header included
var payloadOut = new byte[8192];                      // the EXI payload we are about to send
var sessionId  = new byte[8];
Random.Shared.NextBytes(sessionId);

var negotiated = false;

while (true)
{
    // ---- one V2GTP frame: eight header bytes, then exactly what they declare ----------------
    try { await stream.ReadExactlyAsync(frame.AsMemory(0, V2GTP.HeaderSize)); }
    catch (EndOfStreamException) { break; }                       // the EV hung up

    if (!V2GTP.TryReadHeader(frame, out var payloadType, out var payloadLength))
        throw new InvalidDataException("not a V2GTP frame");

    await stream.ReadExactlyAsync(frame.AsMemory(V2GTP.HeaderSize, (int) payloadLength));

    var whole   = frame.AsMemory(0, V2GTP.HeaderSize + (int) payloadLength);
    var payload = whole[V2GTP.HeaderSize..];

    int written;
    MessageSet set;

    if (!negotiated)
    {
        // 0x8001 carries BOTH the handshake and every ISO 15118-2 message. The handshake happens
        // before a protocol has been agreed, so it cannot have a payload type of its own — what
        // tells them apart is the position in the session, never the header.
        var offer = (SupportedAppProtocolReq) SupportedAppProtocolCodec.DecodeAny(payload.Span, out _);
        Console.WriteLine("EV offers: " +
                          string.Join(", ", offer.AppProtocols.Select(p => p.ProtocolNamespace)));

        var chosen = offer.AppProtocols.First(p => p.ProtocolNamespace.Contains(":iso:15118:2:2013:MsgDef"));

        SupportedAppProtocolCodec.TryEncodeResponse(
            new SupportedAppProtocolRes(SapResponseCode.OK_SuccessfulNegotiation, chosen.SchemaID),
            payloadOut, out written);

        set        = MessageSet.AppProtocol;
        negotiated = true;
    }
    else
    {
        if (!V2GTPDispatcher.TryDecode(whole.Span, out set, out var message, out var error))
            throw new InvalidDataException(error);

        var request = (V2G_Message) message!;
        Console.WriteLine($"← {request.Body.BodyElement?.GetType().Name}");

        BodyBaseType answer = request.Body.BodyElement switch
        {
            SessionSetupReqType =>
                new SessionSetupResType(Iso2ResponseCode.OK_NewSessionEstablished, "DE*ABC*E1", 1_600_000_000L),

            // …the rest of the -2 sequence goes here.
            _ => throw new NotSupportedException($"no answer wired up for {request.Body.BodyElement}"),
        };

        var reply = new V2G_Message(
            new MessageHeaderType(sessionId, Notification: null, Signature: null),
            new BodyType(answer));

        if (!reply.TryEncode(payloadOut, out written))
            throw new InvalidOperationException("EXI encode failed — buffer too small?");
    }

    // ---- put the header back on and send -----------------------------------------------------
    var outgoing = new byte[V2GTP.HeaderSize + written];
    V2GTPDispatcher.TryEncode(set, payloadOut.AsSpan(0, written), outgoing, out var total);
    await stream.WriteAsync(outgoing.AsMemory(0, total));
}
```

## The rest of the sequence

A worked version — the full -2 flow with a guard that refuses out-of-order requests, and both AC
and DC — is `ChargingSimulation`. It runs without a socket:

```bash
dotnet run --project demos/EXI/ChargingSimulation -- dc
```

Every line it prints is a real EXI round trip. `--break-sequence` and `--slow` make the EV
misbehave, which is the fastest way to see what the guards are for.

## Four things that catch people

**The payload type does not identify the message set.** `0x8001` is both the SupportedAppProtocol
handshake *and* every ISO 15118-2 message, because the handshake happens before a protocol has been
agreed and so cannot have an id of its own. What tells them apart is where you are in the session.
`V2GTPDispatcher.TryDecode` therefore resolves `0x8001` to ISO 15118-2 and never to the handshake —
decode the handshake explicitly first, as the loop above does.

**Read the length, then read that many bytes.** TCP does not preserve message boundaries. A single
`Read` can hand you half a frame or two frames at once; the eight-byte header exists precisely so
you know where the next one ends. `ReadExactlyAsync` is doing the real work in the loop above.

**Everything is `Span` and `Try`.** Nothing allocates a buffer for you and nothing throws on a
malformed *frame* — you get `false` and a reason. Malformed EXI *inside* a recognised set does
throw `InvalidDataException`, because at that point the peer has claimed a schema and then not
followed it. `V2GTP.MaximumPayloadBytes` is the ceiling worth sizing a buffer against.

**Plug & Charge signatures need `WWCP_ISO15118_XMLDSig`, not the message set's own.** A
`SignedInfo` that Josev or EXIficient produced is encoded against `xmldsig-core-schema.xsd`
*standalone*, which is a different grammar from the combined one each message set carries. Verify
with the wrong one and every signature fails — locally consistent, interoperable with nobody.
