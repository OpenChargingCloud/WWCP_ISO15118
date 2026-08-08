# Demos

Runnable programs, one per subdirectory, that exercise the libraries above them. They are here to
be read as much as run: each one is the shortest honest use of its project, and several print every
frame they send so the wire is visible.

Every command below is written from the repository root. Fetch the schemas first
([`../README.md`](../README.md)) — the EXI demo does not build without them; the SDP and SLAC demos
do, because they carry no generated codec.

## Start here: the EXI codec

| | What it does | Needs |
|---|---|---|
| [`SECC_Example.md`](SECC_Example.md) | A charging station in one file: accept an EV, finish the handshake, answer its first message. The four things that catch people, written out. | reading only |
| `ChargingSimulation` | An ISO 15118-2 session end to end, AC or DC, EV and SECC talking to each other. **Every line it prints is a real EXI round trip** — no mocked bytes anywhere. | nothing but the schemas |

```bash
dotnet run --project demos/ChargingSimulation -- dc
```

`ac` picks the other mode. `--slow` makes the SECC stall on `AuthorizationReq` until the EV's
timeout fires; `--break-sequence` makes the EV send `PowerDeliveryReq` out of order until the SECC's
sequence guard rejects it. Both flags exist to show a guard doing its job — the fastest way to see
what a state machine is actually for. This is the runnable companion to `SECC_Example.md`.

> **This is a teaching demo, not the stack's EVCC and SECC.** Its `Evcc` and `Secc` are ~150 lines
> each, written to be read in one sitting: ISO 15118-2 only, EIM only, a `Wire` that hands the bytes
> straight to an in-process object, and three charge-loop cycles. The real session layer is
> `WWCP_ISO15118_Session` — both protocols, Plug & Charge, TLS, SLAC and SDP in front of it — and
> [`WWCP_ISO15118_SECC`](../WWCP_ISO15118_SECC/README.md) and
> [`WWCP_ISO15118_EVCC`](../WWCP_ISO15118_EVCC/README.md) run *that* over a socket, one program per
> role. If you want a peer to point at another implementation, you want those two; if you want to
> see what the messages are, you want this.

## SECC discovery: SDP

An EVCC finds a charging station on the local link before any of the above happens. These two are a
pair — run the server, then point the client at the same interface.

| | What it does |
|---|---|
| `SDP/SECC_SDP_Demo` | Listens for SDP requests on a network interface and logs request/response. |
| `SDP/EVCC_SDP_Demo` | Broadcasts an SDP request and reports the SECC it discovers. |

```bash
dotnet run --project demos/SDP/SECC_SDP_Demo -- eth0     # in one terminal
dotnet run --project demos/SDP/EVCC_SDP_Demo -- eth0     # in another
```

The argument is the interface name (default `eth0`). SDP is link-local UDP multicast, so this wants
a real interface and, on most systems, the privilege to bind it — it is a network demo, not a
desktop one.

## SLAC: pairing over the pilot line

Below ISO 15118 sits SLAC — HomePlug Green PHY, matching an EV to an EVSE over the CP pilot wire.
The working demos simulate the pilot as a UDP "bus" so they run without PLC hardware; the bridge is
the exception that talks to a real Layer 2.

| | What it does | Needs |
|---|---|---|
| `SLAC/EV_SLAC_Demo` | The PEV side of a SLAC match — broadcasts `CM_SLAC_PARM.REQ`, runs the sounding, learns the EVSE. | UDP bus, any OS |
| `SLAC/EVSE_SLAC_Demo` | The EVSE side — learns each PEV from its broadcast, rotates NMK/NID per match. | UDP bus, any OS |
| `SLAC/SLAC_Bridge_Demo` | Bridges the simulated UDP bus to a real Ethernet interface (`AF_PACKET`), so simulated and hardware nodes share one AVLN. | **Linux**, raw socket |

Run the EVSE, then the EV against it — the two comments at the top of each `Program.cs` spell out the
port convention (the CP-toggle bus is the SLAC port + 1000).

### Pentests

`WWCP_ISO15118_SLAC_Pentests` turned into runnable tools. Each carries a defensive note in its
source saying what a compliant EVSE should do instead — the point is to make the missing mitigation
observable, not to hand out a weapon.

| | The attack it demonstrates |
|---|---|
| `SLAC/SLAC.Pentest.Rogue` | The canonical one: SLAC has no authentication. A rogue EVSE advertises the lowest attenuation and serves its own NMK; a PEV trusting SLAC alone joins an attacker's AVLN. |
| `SLAC/SLAC.Pentest.Flood` | RunID flood — a fresh RunID every frame makes an un-rate-limited listener allocate session state without bound. |
| `SLAC/SLAC.Pentest.Fuzzing` | Structure-aware mutational fuzzer; emits each finding with the exact mutated bytes so it reproduces. |
| `SLAC/SLAC.Pentest.Replay` | Capture then replay, with RunID/MAC rewriting so the replay survives a non-trivial target. |

Read [`../SecurityResearch.md`](../SecurityResearch.md) before this row of the table — the rogue-EVSE
demo in particular only means something against the papers behind it.

---

Everything here is AGPL-3.0, like the rest of the repository — the header of each `Program.cs` says
so in full. If you build something proprietary on top, that is the licence to read first.
