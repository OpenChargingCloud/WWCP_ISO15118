# WWCP_ISO15118_EVCC — the vehicle

A runnable ISO 15118 **EVCC**: it finds a station, negotiates a protocol, and charges to
`SessionStop`. One program, one role. The station is
[`WWCP_ISO15118_SECC`](../WWCP_ISO15118_SECC/README.md), and the two are meant to be pointed at each
other — or at somebody else's implementation, which is what most of the runs in this project actually
do.

Open [`WWCP_ISO15118_EVCC.slnx`](WWCP_ISO15118_EVCC.slnx) in Visual Studio and press F5. It carries
the whole stack underneath — the session layer, the codec, SDP, SLAC, the PKI builder — so you can
set a breakpoint in `Evcc20Base.RunAsync` and step through a session message by message.

```bash
# in one terminal
dotnet run --project WWCP_ISO15118_SECC -- --mode dc
# in another
dotnet run --project WWCP_ISO15118_EVCC -- --connect "[::1]:15118" --mode dc
```

Which prints, from the car:

```
SAP: offered -20 (priority 1) and -2 (priority 2); the station picked -20.
  17 exchanges, 531 bytes on the wire (request side), auth: eim, session setup: OK_NewSessionEstablished.
```

## What it does

Everything the session layer implements, on the car's side:

| | |
|---|---|
| **Both protocols** | offers `-20` and `-2` in one handshake and runs whichever the station picks |
| **AC, DC and MCS** | `--mode ac\|dc\|mcs`; MCS is the DC message set under energy-transfer services 8/9 |
| **EIM and Plug & Charge** | `--contract-cert` signs the authorization with a real contract certificate instead of paying externally |
| **Scheduled and Dynamic** | follows the control mode the station offers |
| **Signed tariffs** | `--tariff-cert` verifies the station's signed SalesTariff / AbsolutePriceSchedule and reports digest and ECDSA separately |
| **Pause / resume** | `--pause-resume` pauses after the charge loop, reconnects and rejoins — and says so when the station refuses |
| **Renegotiation** | `--renegotiate` sends `PowerDelivery(Renegotiate)` after the first cycle |
| **TLS** | .NET `SslStream`, or BouncyCastle for the profile `-20` actually asks for |
| **SDP and SLAC** | `--sdp` finds the station on the link; `--slac` runs a pairing stage first |

## The defaults, and why

**`--protocol both`, with `-20` at priority 1.** That is what a modern car does: offer what you
speak, let the station choose. Against a `-20` station the session runs `-20`; against a `-2`-only
station it falls back inside the same handshake, without a second connection and without the car
having to know in advance. So the usual outcome of a bare run is a `-20` session, and the `-2` path
is still one flag away.

Pin one with `--protocol 2` or `--protocol 20` when the point of the run *is* that protocol — which
is what the interop harnesses under `tools/interop-*` do, because a run that silently changed
protocol would prove nothing.

**`--mode ac`**, because the connector decides the mode. Pass `--mode dc` for a DC session, and make
sure the station agrees.

There is no default station: a car needs somewhere to drive to, so either `--connect host:port` or
`--sdp --interface <name>` is required.

## Worth trying

```bash
# a DC session against a station on this machine. IPv6 literals must be bracketed.
dotnet run --project WWCP_ISO15118_EVCC -- --connect "[::1]:15118" --mode dc

# find the station instead of being told where it is (needs a real interface)
dotnet run --project WWCP_ISO15118_EVCC -- --sdp --interface eth0 --mode dc

# Plug & Charge: sign the authorization with a contract certificate
dotnet run --project WWCP_ISO15118_EVCC -- --connect "[::1]:15118" --contract-cert contract.p12

# pause after the charge loop, reconnect, rejoin the same session
dotnet run --project WWCP_ISO15118_EVCC -- --connect "[::1]:15118" --pause-resume

# the -20-faithful TLS profile. Start the station with the same --pki-dir FIRST:
# it mints the hierarchy and writes this car's chain and key into that directory.
dotnet run --project WWCP_ISO15118_EVCC -- --connect "[::1]:15118" --tls-backend bc --pki-dir /tmp/v2gpki

# force the -2 path through a station that offers both
dotnet run --project WWCP_ISO15118_EVCC -- --connect "[::1]:15118" --protocol 2
```

`--help` prints the full flag list. Flags that belong to the station — `--listen`, `--dynamic`,
`--no-pnc`, `--server-cert` — are refused here by name rather than ignored, which is the main
practical gain of splitting the two roles apart.

## Two things that will catch you

**Bracket IPv6 literals, zone included:** `[fe80::1%eth0]:15118`. An unbracketed `::1:8080` is a
valid address in its own right (`::0.1.128.128`), so splitting it at the last colon would connect
somewhere else entirely — `--connect` refuses the ambiguous form rather than guessing.

**`--tls` accepts any server certificate.** There is no out-of-band way here to learn a dev
station's thumbprint, so the check is disabled and the program says so on every run. Never point
that at a real SECC. `--tls-backend bc` does pin the station's leaf, because the station minted it.

## What it is not

A conformance and research peer, not a car. It validates no certificate chain (signatures verify
against the presented leaf; nothing walks `SubCertificates` to a V2G root), its timeouts are a flat
2 s / 60 s rather than the standard's performance tables, its charge loop is a fixed three
iterations rather than a battery filling up, and it has no electrical layer at all. The station's
README lists the same limits from the other side, where they matter more.

## Where the actual implementation is

Here is the wiring; the behaviour is in [`WWCP_ISO15118_Session`](../WWCP_ISO15118_Session), mostly
in `StateMachines/Iso2/Evcc2.cs` and `StateMachines/Iso20/Evcc20*.cs`.

Whether any of it conforms is a separate question, answered counterparty by counterparty in the
[conformance repository](../../../../README.md) two levels up.
