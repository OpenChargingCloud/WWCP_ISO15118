# ISO 15118 EXI — using it in your own project

This is the wire layer: XML schemas in, the exact bytes an EV and a charging station exchange out.
A source generator turns the ISO 15118 XSDs into codecs at build time, and the codecs read and
write EXI — the bit-packed encoding the standard uses instead of XML on the wire.

**What it is not**, and this is the first thing worth knowing: there is no TCP server here, no TLS,
no SECC discovery, no state machine, and no notion of a charging session. It decodes a frame into a
message object and encodes a message object back into a frame. Everything around that is yours, or
lives in one of the projects listed under [What is deliberately not here](#what-is-deliberately-not-here).

---

## Which projects do I reference?

| Project | Reference it when you need |
|---|---|
| `Vanaheimr.V2G.Exi.Prototype` | **Always.** Bit reader/writer, EXI primitives, the V2GTP header, and the hand-written SupportedAppProtocol handshake codec |
| `Vanaheimr.V2G.Exi.Iso15118_2` | ISO 15118-2 messages (the 2013 edition nearly every car on the road speaks today) |
| `Vanaheimr.V2G.Exi.Iso15118_20.CommonMessages` | ISO 15118-20 session, authorisation, service discovery, schedules |
| `Vanaheimr.V2G.Exi.Iso15118_20.AC` / `.DC` | -20 energy transfer, one per mode |
| `Vanaheimr.V2G.Exi.Iso15118_20.AC_DER_IEC` / `.AC_DER_SAE` | -20 with distributed energy resources |
| `Vanaheimr.V2G.Exi.Iso15118_20.WPT` / `.ACDP` | -20 wireless and automatic connection device |
| `Vanaheimr.V2G.Exi.XmlDsig` | The standalone W3C XMLDSig grammar. Plug & Charge needs it — see below |
| `Vanaheimr.V2G.Exi.Dispatch` | You are reading frames off a socket and want the payload type resolved for you |
| `Vanaheimr.V2G.Exi.SourceGenerator` | Never directly. Every project above pulls it in as an analyzer |

A `.csproj` for a station that speaks ISO 15118-2:

```xml
<ItemGroup>
  <ProjectReference Include="…\WWCP_ISO15118\new\Vanaheimr.V2G.Exi.Prototype\Vanaheimr.V2G.Exi.Prototype.csproj" />
  <ProjectReference Include="…\WWCP_ISO15118\new\Vanaheimr.V2G.Exi.Iso15118_2\Vanaheimr.V2G.Exi.Iso15118_2.csproj" />
  <ProjectReference Include="…\WWCP_ISO15118\new\Vanaheimr.V2G.Exi.Dispatch\Vanaheimr.V2G.Exi.Dispatch.csproj" />
</ItemGroup>
```

Nothing else to configure. There is no NuGet package, no generator setting to switch on, and no
build step of your own: referencing a message-set project is enough, because the generator runs
inside *its* compilation and ships the finished types in its assembly.

Target `net10.0`. If you are on Windows and want to run a station over TLS 1.3, read
`docs/pki-model.md` in the conformance repository first — Schannel cannot do parts of the -20
profile, and the fallback is not automatic.

---

## A minimal SECC: receiving messages from an EV

The whole loop. This compiles and runs against the three project references above; it accepts one
EV, completes the protocol handshake, and answers `SessionSetupReq`.

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

A worked version of the rest — the full -2 sequence with a guard that refuses out-of-order
requests, and both AC and DC — is `Vanaheimr.V2G.Exi.Simulation`. It runs without a socket:

```bash
dotnet run --project Vanaheimr.V2G.Exi.Simulation -- dc
```

Every line it prints is a real EXI round trip. `--break-sequence` and `--slow` make the EV
misbehave, which is the fastest way to see what the guards are for.

---

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

**Plug & Charge signatures need `Vanaheimr.V2G.Exi.XmlDsig`, not the message set's own.** A
`SignedInfo` that Josev or EXIficient produced is encoded against `xmldsig-core-schema.xsd`
*standalone*, which is a different grammar from the combined one each message set carries. Verify
with the wrong one and every signature fails — locally consistent, interoperable with nobody.

---

## ISO 15118-20 instead of -2

Same shape, different projects. Each -20 message set is its own assembly with its own V2GTP payload
type, and the sets do not reference each other:

| Set | Project | Codec | Payload type |
|---|---|---|--:|
| Common | `…Iso15118_20.CommonMessages` | `CommonMessagesCodec` | `0x8002` |
| AC | `…Iso15118_20.AC` | `AcCodec` | `0x8003` |
| DC | `…Iso15118_20.DC` | `DcCodec` | `0x8004` |
| ACDP | `…Iso15118_20.ACDP` | `AcdpCodec` | `0x8005` |
| WPT | `…Iso15118_20.WPT` | `WptCodec` | `0x8006` |

A -20 session interleaves two of them: the common messages carry the session and authorisation,
and the AC or DC set carries the energy transfer, on separate payload types over the same socket.
`V2GTPDispatcher` handles that — it is the reason it exists.

`AC_DER_IEC` and `AC_DER_SAE` layer distributed-energy-resource schemas on top of `V2G_CI_AC.xsd`
and are separate assemblies again.

The `CommonTypes` and XMLDSig schemas are duplicated into each message set rather than factored
into a shared assembly. That is deliberate and mirrors cbexigen/cbV2G: the EXI grammars are built
per schema *set*, and the same type in two sets is not the same grammar. Please do not tidy it up.

---

## What is deliberately not here

| You need | Where it is |
|---|---|
| SECC discovery (SDP) | `WWCP_ISO15118_SDP` in this repository |
| SLAC / HomePlug Green PHY | `WWCP_ISO15118_SLAC` |
| V2G PKI, certificate chains, CSRs | `WWCP_ISO15118_PKI` |
| SECC/EVCC state machines, TLS profiles, metering, an OCPP-facing backend | `Vanaheimr.V2G.Simulation`, in the `Vanaheimr.V2G.Exi` conformance repository — the one this repository is a submodule of |
| Kotlin, Swift or TypeScript codecs | Generated by `tools/EVSimulatorApp.Codegen` in the `EVSimulatorApp` repository. The port back ends live with their only consumer; the front end and the C# back end are here |

---

## The generated code

None of it is checked in. The XSDs are, and the generator turns them into C# during the build; the
output lands in `obj/…/generated/` for reading. So there is nothing to regenerate by hand and no
generated file to review in a diff — a schema change shows up as a schema change.

The schemas are ISO's, redistributed here rather than downloaded per clone. Where each set came
from, and why that choice was made rather than cbexigen's, is in [`SCHEMAS.md`](SCHEMAS.md); each
`Schemas/README.md` carries its own source and file table.

The types you write against are named after the XSD, not after the prose of the standard:
`SessionSetupReqType`, `AuthorizationResType`, `V2G_Message`. Everything for a set is in
`cloud.charging.open.protocols.ISO15118_2.Generated` or
`cloud.charging.open.protocols.ISO15118_20.<Set>.Generated`.

Correctness is pinned against a reference encoder rather than against our own opinion. The vectors
under `Vanaheimr.V2G.Exi.Tests/Vectors/` are bytes produced by cbV2G and EXIficient, and the rule
for this codebase is in `CLAUDE.md`: never change wire semantics speculatively, only on a concrete
byte diff against one of them.

```bash
dotnet test -c Release WWCP_ISO15118.EXI.slnx
```

must pass without a C toolchain, without Java, and without a network. The reference encoders under
`tools/` are for regenerating vectors, never for running the tests.
