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


## What else is in here

| You need | Where it is |
|---|---|
| SECC discovery | `WWCP_ISO15118_SDP` |
| SLAC / HomePlug Green PHY | `WWCP_ISO15118_SLAC`, and `WWCP_ISO15118_SLAC_Pentests` |
| V2G PKI, certificate chains, CSRs | `WWCP_ISO15118_PKI` |
| Runnable demos | [`demos/`](demos/README.md) |

New here? Start in **[`demos/`](demos/README.md)** — one runnable program per subprotocol, each the
shortest honest use of its project, several printing every frame they send. `ChargingSimulation`
alone is a full ISO 15118-2 session where every printed line is a real EXI round trip.

Not here: SECC/EVCC state machines, TLS profiles, metering and an OCPP-facing backend live in
`Vanaheimr.V2G.Simulation`, in the conformance repository this one is a submodule of. The Kotlin,
Swift and TypeScript codecs are generated by `tools/EVSimulatorApp.Codegen` in the `EVSimulatorApp`
repository — the port back ends live with their only consumer, while the generator front end and
the C# back end are here.


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
