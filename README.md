# WWCP ISO/IEC 15118

This software allows communication between World Wide Charging Protocol (WWCP) entities and
entities implementing ISO 15118 — _Signal Level Attenuation Characterization (SLAC)_, _SECC
Discovery Protocol (SDP)_, _Vehicle-To-Grid Transport Protocol (V2GTP)_, _ISO/IEC 15118-2_,
_ISO/IEC 15118-20_ and _ISO/IEC 15118-8_ for wireless communication.

The focus is the communication between an electric vehicle and an e-mobility charging station.

In addition, this library implements multiple attack vectors and penetration-testing workflows for
the different subprotocols. An overview of the research papers, vulnerability disclosures and
practitioner whitepapers behind them is in **[SecurityResearch.md](SecurityResearch.md)** — worth
reading before the code, because several decisions here only make sense against it.

Two layers, and the difference matters when you go looking for something: the **codec** turns frames
into message objects and back, and the **session** layer above it is what actually holds a
conversation — SLAC, discovery, TLS, the handshake, and the EVCC and SECC state machines for both
protocols. Since 2026-08-08 both are here; the state machines used to live one repository up.


## The ISO 15118 EXI codec

The wire layer: XML schemas in, the exact bytes an EV and a charging station exchange out. A source
generator turns the ISO 15118 XSDs into codecs at build time, and the codecs read and write EXI —
the bit-packed encoding the standard uses instead of XML on the wire.

**What it is not**, and this is the first thing worth knowing: the codec is not a station. There is
no TCP server in it, no TLS, no discovery, no state machine and no notion of a charging session. It
turns a frame into a message object and back. Everything around that is yours, or lives in one of
the other projects in this repository.


### Before anything builds: fetch the schemas

The schemas are not in this repository. They are ISO's, and ISO's licence grants use rather than
redistribution, so you fetch them yourself:

```bash
bash tools/download-schemas.sh
```

One command, needs `curl` and `unzip`, and running it is you accepting the ISO Customer Licence
Agreement — which is exactly why it is a script you run rather than files we ship.
[`SCHEMAS.md`](SCHEMAS.md) has the reasoning, the sources, and what to do if the vector corpus goes
red afterwards.


### Which projects do I reference?

| Project | Reference it when you need |
|---|---|
| `WWCP_ISO15118_EXI` | **Always.** Bit reader/writer, EXI primitives, and the hand-written SupportedAppProtocol handshake codec |
| `WWCP_ISO15118_V2GTP` | The 8-byte V2GTP header, its payload types, and the SDP framing |
| `WWCP_ISO15118_2` | ISO 15118-2 messages — what nearly every car on the road speaks today |
| `WWCP_ISO15118_20.CommonMessages` | ISO 15118-20 session, authorisation, service discovery, schedules |
| `WWCP_ISO15118_20.AC` / `.DC` | -20 energy transfer, one per mode |
| `WWCP_ISO15118_20.AC_DER_IEC` / `.AC_DER_SAE` | -20 with distributed energy resources |
| `WWCP_ISO15118_20.WPT` / `.ACDP` | -20 wireless and automatic connection device |
| `WWCP_ISO15118_XMLDSig` | The standalone W3C XMLDSig grammar. Plug & Charge needs it |
| `WWCP_ISO15118_EXI_Dispatch` | You are reading frames off a socket and want the payload type resolved for you |
| `WWCP_ISO15118_EXI_SourceGenerator` | Never directly. Every project above pulls it in as an analyzer |

**[demos/SECC_Example.md](demos/SECC_Example.md)** is the whole thing put together: a charging
station that accepts an EV, completes the handshake and answers its first message, in one file —
with the four things that catch people written out. It sits next to `ChargingSimulation`, the
runnable version of the same sequence.


### ISO 15118-20 instead of -2

Same shape, different projects. Each -20 message set is its own assembly with its own V2GTP payload
type, and the sets do not reference each other:

| Set | Project | Codec | Payload type |
|---|---|---|--:|
| Common | `WWCP_ISO15118_20.CommonMessages` | `CommonMessagesCodec` | `0x8002` |
| AC | `WWCP_ISO15118_20.AC` | `AcCodec` | `0x8003` |
| DC | `WWCP_ISO15118_20.DC` | `DcCodec` | `0x8004` |
| ACDP | `WWCP_ISO15118_20.ACDP` | `AcdpCodec` | `0x8005` |
| WPT | `WWCP_ISO15118_20.WPT` | `WptCodec` | `0x8006` |

A -20 session interleaves two of them: the common messages carry the session and authorisation, the
AC or DC set carries the energy transfer, on separate payload types over the same socket.
`V2GTPDispatcher` handles that — it is the reason it exists.

`AC_DER_IEC` and `AC_DER_SAE` layer distributed-energy-resource schemas on top of `V2G_CI_AC.xsd`
and are separate assemblies again.

The `CommonTypes` and XMLDSig schemas are duplicated into each message set rather than factored into
a shared assembly. That is deliberate and mirrors cbexigen/cbV2G: EXI grammars are built per schema
*set*, and the same type in two sets is not the same grammar. Please do not tidy it up.


### The generated code

None of it is checked in, and neither are the XSDs it comes from. The generator turns the schemas
into C# during the build and the output lands in `obj/…/generated/` for reading. So there is nothing
to regenerate by hand and no generated file to review in a diff — a schema change shows up as a
schema change, in a file you fetched.

The types you write against are named after the XSD, not after the prose of the standard:
`SessionSetupReqType`, `AuthorizationResType`, `V2G_Message`. Everything for a set is in
`cloud.charging.open.protocols.ISO15118_2.Generated` or
`cloud.charging.open.protocols.ISO15118_20.<Set>.Generated`.

Correctness is pinned against a reference encoder rather than against our own opinion. The vectors
under `WWCP_ISO15118_EXI_Tests/Vectors/` are bytes produced by cbV2G and EXIficient, and the rule is
in `CLAUDE.md`: never change wire semantics speculatively, only on a concrete byte diff against one
of them.

```bash
dotnet test -c Release WWCP_ISO15118.EXI.slnx
```

must pass without a C toolchain, without Java, and without a network. The reference encoders under
`tools/` are for regenerating vectors, never for running the tests.


## The session layer

`WWCP_ISO15118_Session` is the part that holds a conversation rather than encoding one. An EVCC and
a SECC for each protocol, the transport under them, and the stages that run before the first V2G
message:

| | |
|---|---|
| `StateMachines/Iso2/` | `Evcc2`, `Secc2` — the -2 session, EIM and Plug & Charge: PaymentDetails, signed `AuthorizationReq`, signed `MeteringReceiptReq`, signed SalesTariff offers, pause/resume, renegotiation |
| `StateMachines/Iso20/` | `Evcc20Base`/`Secc20Base` plus AC, DC and MCS specialisations — Scheduled and Dynamic control modes, bidirectional (BPT), Plug & Charge, CertificateInstallation, and resume bound to the vehicle certificate |
| `Transport/` | TCP, and TLS through two backends: .NET `SslStream`, or BouncyCastle for the secp521r1/Ed448 profile -20 asks for and Windows Schannel cannot do |
| `Sap/`, `Discovery/`, `Slac/` | the handshake, SDP, and the ISO 15118-3 pairing stage — everything before the first `SessionSetupReq` |
| `Metering/` | a signing meter, and `ISessionBackend`: the one-method seam where a station reports what it delivered |

`Secc2.Handle` and its -20 equivalent are pure synchronous transitions — a request in, a response
and the next phase out — so a session is testable without a socket; `RunAsync` is the thin loop that
drives one from a real stream. Two programs are exactly that, one per role, each with its own
solution you can open in Visual Studio and its own README:
[`WWCP_ISO15118_SECC`](WWCP_ISO15118_SECC/README.md) and
[`WWCP_ISO15118_EVCC`](WWCP_ISO15118_EVCC/README.md).

```bash
dotnet run --project WWCP_ISO15118_SECC
dotnet run --project WWCP_ISO15118_EVCC -- --connect '[::1]:15118'
```

Bare like that, the station accepts both protocols and the car offers both with `-20` first, so the
handshake settles on `-20` and says so on both sides; both default to DC on port 15118.
`--protocol 2` or `--protocol 20` pins one when that is the point of the run, and `--mode ac` picks
the other connector — that one is not negotiated, so both sides need telling. Splitting the roles apart is what makes `--help` useful: each program
documents and accepts only its own flags, and refuses the other's by name instead of ignoring them.

**This half does not build from a standalone clone**, and that is why `WWCP_ISO15118.EXI.slnx` holds
the codec projects only. `WWCP_ISO15118_SLAC` references `..\..\Hermod\Hermod\Hermod.csproj` — a
sibling of *this* directory, which exists only when this repository is checked out where it normally
is, at `libs/WWCP_ISO15118/` inside `EVSimulatorApp`. Session and CLI depend on SLAC, so they inherit
that. Build them through `EVSimulatorApp.slnx` one level up, or through the conformance repository's
solution two levels up; the codec solution here needs nothing outside this repository.

**What it deliberately is not.** This is a conformance and research peer, and four things it does
not do are the reason it should not be put in front of a real car or a real charger as anything but
a test instrument:

- **No certificate chain is validated.** Signatures are verified against the leaf the peer presented;
  nothing walks `SubCertificates` to a V2G root, checks validity dates, or consults revocation, and
  the CLI's TLS callbacks accept any peer certificate. Good enough to prove a signature is
  well-formed and byte-exact, nowhere near enough to decide that a contract is *good*.
- **The timeouts are not the standard's.** `MessageTimeoutOptions` says so itself: a flat 2 s per
  message and 60 s per sequence, not the per-message performance tables of -2 and -20.
- **The charge loop is a fixed three iterations**, not a battery filling up.
- **There is no electrical layer at all** — no contactor, no Control Pilot, no isolation monitoring,
  no power electronics. On the SECC side that is the entire safety-relevant half of a charging
  station, and it is governed by IEC 61851 rather than by ISO 15118.

What it *is* good for is the other half: what the bytes on the wire are, and whether an independent
stack agrees. That claim is the one the conformance repository above this one measures, counterparty
by counterparty.


## What else is in here

| You need | Where it is |
|---|---|
| **A charging session** — the EVCC and SECC state machines, -2 and -20 | `WWCP_ISO15118_Session` |
| A station you can run | [`WWCP_ISO15118_SECC`](WWCP_ISO15118_SECC/README.md) — own solution, own README |
| A car you can run | [`WWCP_ISO15118_EVCC`](WWCP_ISO15118_EVCC/README.md) — own solution, own README |
| SECC discovery | `WWCP_ISO15118_SDP` |
| SLAC / HomePlug Green PHY | `WWCP_ISO15118_SLAC`, and `WWCP_ISO15118_SLAC_Pentests` |
| V2G PKI, certificate chains, CSRs | `WWCP_ISO15118_PKI` |
| Enumerating and picking the V2G network interface | `WWCP_ISO15118_NetworkInterfaces` |
| Runnable demos | [`demos/`](demos/README.md) |

New here? Start in **[`demos/`](demos/README.md)** — one runnable program per subprotocol, each the
shortest honest use of its project, several printing every frame they send.

Not here: the **apps** built on this stack — the WebView EV simulator, the Capacitor shells, the QR
pairing and its Raspberry-Pi SECC counterpart, the Kotlin/Swift/TypeScript ports of the codec and
the post-quantum experiment — live in the `EVSimulatorApp` repository above this one, along with the
OCPP-facing backend a station's `ISessionBackend` gets wired to. The **evidence** that any of it
conforms — the recorded corpus, the loopback E2Es, the live cross-checks against Josev, EVerest,
eVDriveFlow and tux-evse — lives in `ISO15118ConformanceTests` above that.


### Future

Currently not in scope:
 - ISO 15118-202 Extensible SECC Discovery Protocol (ESDP)
 - ISO 15118-202 EventNotification Protocol


### Your participation

This software is free and Open Source under [GNU Affero General Public License (AGPL)](LICENSE).
We appreciate your participation in this ongoing project, and your help to
improve it and the e-mobility ICT in general. If you find bugs, want to request
a feature or send us a pull request, feel free to use the normal GitHub
features to do so. For this please read the Contributor License Agreement
carefully and send us a signed copy or use a similar free and open license.
