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
dotnet run --project WWCP_ISO15118_SECC
# in another
dotnet run --project WWCP_ISO15118_EVCC -- --connect "[::1]:15118"
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
| **CertificateInstallation** | `--oem-cert` asks the station to issue a contract, and unwraps the key it sends back (`-20`) |
| **Scheduled and Dynamic** | follows the control mode the station offers |
| **A battery** | nine flags turn the charge loop from three iterations into a session that ends when the car is done — see [The battery](#the-battery) |
| **Signed tariffs** | `--tariff-cert` verifies the station's signed SalesTariff / AbsolutePriceSchedule and reports digest and ECDSA separately |
| **Pause / resume** | `--pause-resume` pauses after the charge loop, reconnects and rejoins — and says so when the station refuses |
| **Renegotiation** | `--renegotiate` sends `PowerDelivery(Renegotiate)` after the first cycle |
| **TLS** | two backends — see [Which TLS backend](#which-tls-backend-and-why-it-is-not-a-preference), because on Windows and macOS this is not a matter of taste |
| **SDP and SLAC** | `--sdp` finds the station on the link; `--slac` runs a pairing stage first |

## The car's three certificates

They are not interchangeable, and mixing them up produces failures that read like protocol bugs.

| Flag | Which certificate | What it is for |
|---|---|---|
| `--vehicle-cert` | the **Vehicle** certificate | **who this car is.** Presented in the TLS handshake, on either backend. For `-20` it is also what the station's resume binding is computed over — `SHA-512(session-id ‖ SHA-512(vehicle leaf))` — so a resume only works if the car comes back with the same one. |
| `--contract-cert` | the **contract** certificate | **who pays.** Plug & Charge in `-2` and `-20`: signs the authorization instead of paying externally. |
| `--oem-cert` | the **OEM provisioning** certificate | **what the car was born with** — the only identity it has before it holds a contract. `-20`: sends a signed `CertificateInstallationReq` and ECDH-unwraps the contract key the station issues. |

The names come from the **CharIN V2G second-generation PKI Certificate Policy**, not from ISO 15118
directly — and that is deliberate rather than sloppy. The certificates themselves are ISO 15118's;
the Policy realises the standard's structure as five named branches (CSO, Vehicle, e-MSP, OEM Prov,
CPS) and is explicit that ISO 15118-20's own naming is not fully consistent, so this project uses the
Policy's names throughout. `EVSimulatorApp/docs/pki-model.md` records that decision and both source
documents.

Two things about `--oem-cert` worth knowing before you use it. Its key must be **P-521**: the unwrap
is an ECDH against the station's ephemeral secp521r1 key, so a `-2`-era P-256 OEM certificate gets a
well-formed response it cannot decrypt — the program warns rather than letting you discover it as a
decryption failure. And it is accepted for `-2` but does nothing there: this EVCC implements
CertificateInstallation only for `-20`, and says so on the run rather than dropping the flag quietly.

## Which TLS backend, and why it is not a preference

`--tls` / `--tls-backend dotnet` uses .NET's `SslStream`, which is fast and native. It also cannot
carry the ISO 15118-20 TLS profile on two of the three platforms this runs on:

| | .NET `SslStream` | BouncyCastle |
|---|---|---|
| **Linux** (OpenSSL) | TLS 1.3, secp521r1, suite pinning — all fine | fine |
| **Windows** (Schannel) | TLS 1.3 yes, but **no secp521r1 certificates** (measured: P-256 mutual TLS succeeds, P-521 fails "Authentication failed"), **no per-connection suite pinning**, and it will not present a client chain whose root the machine does not trust | fine |
| **macOS** (SecureTransport) | **no TLS 1.3 at all** — Apple's API never gained it | fine |

So: on Linux either works; **on Windows and macOS a real `-20` TLS session needs `--tls-backend bc`**,
because `--tls` there gives you something that looks like it worked and is not `-20`-conformant
(macOS quietly negotiates TLS 1.2, Windows runs on -2-grade P-256 material). For `-2`, whose profile
is TLS 1.2 with P-256, `--tls` is fine everywhere.

The Windows client-chain rule is the one that bites hardest here, because it is the car that presents
a client certificate: Schannel builds and validates that chain locally *before* the handshake and
refuses a root it does not trust — which a freshly minted V2G test root never is. `--tls-backend bc`
with `--vehicle-cert` sidesteps it entirely. Measurements in `Transport/TlsPlatform.cs`, reasoning in
`EVSimulatorApp/docs/pki-model.md`.

## The defaults, and why

**`--protocol both`, with `-20` at priority 1.** That is what a modern car does: offer what you
speak, let the station choose. Against a `-20` station the session runs `-20`; against a `-2`-only
station it falls back inside the same handshake, without a second connection and without the car
having to know in advance. So the usual outcome of a bare run is a `-20` session, and the `-2` path
is still one flag away.

Pin one with `--protocol 2` or `--protocol 20` when the point of the run *is* that protocol — which
is what the interop harnesses under `tools/interop-*` do, because a run that silently changed
protocol would prove nothing.

**`--mode dc`.** Unlike the protocol, this is *not* negotiated — the connector decides it, and the
station must be told the same thing or the session fails on a message set it did not expect. DC is
the default because it is what this car is usually pointed at, and where the interesting parts live
(CableCheck, PreCharge, WeldingDetection, the bidirectional envelopes). `--mode ac` for the other one.

There is no default station: a car needs somewhere to drive to, so either `--connect host:port` or
`--sdp --interface <name>` is required.

## The battery

By default this car has none: the charge loop runs three iterations and stops, which is a message
sequence rather than a charging session, and is what every recorded interop run in the conformance
repository was taken at. Any one of the nine flags below turns it into a session that ends when the
car is done.

| Flag | | |
|---|---|---|
| `--battery <kWh>` | usable capacity | default 60 |
| `--soc <percent>` | state of charge at plug-in | default: random 10–60 % |
| `--power <kW>` | what the car asks for | default: the fixed figure each mode always asked for |
| `--taper-from <percent>` | where the car starts asking for less | default 80; `100` charges flat |
| `--target-soc <percent>` | charge until this state of charge | |
| `--target-energy <kWh>` | charge until this much has been delivered | |
| `--max-charging-time <dur>` | stop after this much simulated time | `90`, `90m`, `2h`, `1h30m` |
| `--departure-time <dur>` | when the car leaves | |
| `--min-soc <percent>` | what the driver needs by then | a floor, not a goal — see below |

Name no goal at all and the goal is 100 %. Name several and the first one reached ends the session,
in the order a driver would care about: full, then target SoC, then delivered energy, then the time
limit, then departure. A goal that cannot be reached — a station delivering nothing, a target above
capacity — ends at a 5000-iteration ceiling rather than hanging on a live counterparty.

**One iteration is one simulated minute.** That is the period the meter already counts by, so the
car's counter and the station's signed reading are measuring the same process rather than two
different ones. It also means a full charge is several hundred exchanges: give `--max-charging-time`
when the far end is a live station and not a loopback.

### What of it reaches the station

Most of it does not, and the flag names hide that. The pack is the car's own business; only three of
the nine have a field to go in.

| | On the wire |
|---|---|
| `--power` | **yes, in all four modes** — see below |
| `--target-soc` | **yes, `-20` DC**, as `TargetSOC` at `DC_ChargeParameterDiscovery` |
| `--departure-time` | **yes, `-20`**, as `DepartureTime` in the Dynamic charge-loop request — the deadline a Dynamic station schedules against |
| `--taper-from` | indirectly: it shapes what `--power` asks for as the pack fills |
| `--battery`, `--soc`, `--target-energy`, `--max-charging-time`, `--min-soc` | **no.** They end the session, and the station learns of them only by the session ending. The fields they correspond to are still fixed literals here (`EAmount`, `EVTargetEnergyRequest`, `-2`'s `EVRESSSOC`), and `MinimumSOC` exists only in the station's *response*, so a car has nowhere to declare it at all. |

### Where `--power` lands

There is no "watts I want" field the four modes share; each carries the ask where it can.

| | Field | Tapers |
|---|---|---|
| `-20` DC | the Scheduled setpoint (`EVTargetCurrent`) and the Dynamic `EVMaximum*` limits | yes |
| `-20` AC | `EVPresentActivePower`, sent every iteration | yes |
| `-2` DC | `EVTargetCurrent` at the loop's own 400 V, plus `EVMaximumPowerLimit` | yes |
| `-2` AC | `EVMaxCurrent` at discovery, and the `ChargingProfile` committed at `PowerDelivery(Start)` | **no** |

`-2` AC is the one that cannot taper, and that is not an omission: its charge-loop request
(`ChargingStatusReq`) is an empty message, so there is no per-iteration field for the car to lower as
the pack fills. The profile is agreed once and both sides meter it. `-2` AC also holds the ask inside
6–32 A per phase, so `--power 1` charges at 4.2 kW three-phase — a contactor that cannot modulate
below 6 A per phase charges at 6 A whatever the driver typed.

In **Dynamic** control mode `--power` states limits and nothing more, because that is what Dynamic
means: the station names the operating point ([V2G20-1600]), and a car that asked for 9 kW may be
given something else. In **Scheduled** — the default here — the car names it.

Whatever is asked for, the battery fills with **what the meter counted**, not with what was requested:
a station that gives less than it was asked for shows up as a slower charge, which is the point of
asking. The one thing this cannot model is a station giving more than the car allowed.

`--min-soc` is deliberately not a stop condition. A floor cannot extend a session — you cannot charge
after you have driven off — so it neither prolongs the loop past a departure time nor past a charging
time limit. What it does is turn "the session ended" into "the session ended and the car had enough,
or did not", which is the question the driver actually asked and the one a scheduling station should
be judged on. The run says which.

## Worth trying

```bash
# a DC session against a station on this machine. IPv6 literals must be bracketed.
dotnet run --project WWCP_ISO15118_EVCC -- --connect "[::1]:15118"

# find the station instead of being told where it is (needs a real interface)
dotnet run --project WWCP_ISO15118_EVCC -- --sdp --interface eth0

# the other connector — the station has to agree, this one is not negotiated
dotnet run --project WWCP_ISO15118_EVCC -- --connect "[::1]:15118" --mode ac

# Plug & Charge: sign the authorization with a contract certificate
dotnet run --project WWCP_ISO15118_EVCC -- --connect "[::1]:15118" --contract-cert contract.p12

# ask the station to issue one instead, and unwrap the key it sends back (-20, P-521 OEM key)
dotnet run --project WWCP_ISO15118_EVCC -- --connect "[::1]:15118" --protocol 20 \
    --oem-cert oem.p12 --oem-cert-pass secret

# bring your own Vehicle chain to a station whose PKI is not ours
dotnet run --project WWCP_ISO15118_EVCC -- --connect "[::1]:15118" \
    --tls-backend bc --vehicle-cert vehicle.p12

# pause after the charge loop, reconnect, rejoin the same session
dotnet run --project WWCP_ISO15118_EVCC -- --connect "[::1]:15118" --pause-resume

# the -20-faithful TLS profile. Start the station with the same --pki-dir FIRST:
# it mints the hierarchy and writes this car's chain and key into that directory.
dotnet run --project WWCP_ISO15118_EVCC -- --connect "[::1]:15118" --tls-backend bc --pki-dir /tmp/v2gpki

# force the -2 path through a station that offers both
dotnet run --project WWCP_ISO15118_EVCC -- --connect "[::1]:15118" --protocol 2

# a 77 kWh pack at 20 %, charging until it is full rather than for three iterations
dotnet run --project WWCP_ISO15118_EVCC -- --connect "[::1]:15118" --battery 77 --soc 20

# 9 kW to 80 %, and give up after two simulated hours whatever happened
dotnet run --project WWCP_ISO15118_EVCC -- --connect "[::1]:15118" \
    --power 9 --target-soc 80 --max-charging-time 2h

# leaving in 45 minutes and needing 60 % by then. -20 puts the departure on the wire,
# so a Dynamic station has something to schedule against; the minimum is ours to check.
dotnet run --project WWCP_ISO15118_EVCC -- --connect "[::1]:15118" --protocol 20 \
    --departure-time 45m --min-soc 60
```

`--help` prints the full flag list. Flags that belong to the station — `--listen`, `--dynamic`,
`--no-pnc`, `--server-cert` — are refused here by name rather than ignored, which is the main
practical gain of splitting the two roles apart.

## Two things that will catch you

**Bracket IPv6 literals, zone included:** `[fe80::1%eth0]:15118`. An unbracketed `::1:8080` is a
valid address in its own right (`::0.1.128.128`), so splitting it at the last colon would connect
somewhere else entirely — `--connect` refuses the ambiguous form rather than guessing.

**Whether this car checks who the station is depends on what you gave it.** Presenting a certificate
and *verifying the other side's* are two different halves, and the second one is off unless asked
for. A real EVCC validates the station's chain up to a V2G root it was provisioned with at the
factory; `--trust-roots` is how you give this one that root — or a directory of them, when several
counterparties' hierarchies have to be accepted at once.

| | Does it check the station? |
|---|---|
| `--trust-roots <file-or-dir>` | **Yes, by chain** — the station's certificate is built to one of those roots, on either backend, and a failure aborts the handshake with the reason printed. This is the one that resembles what a real car does. |
| `--tls-backend bc --pki-dir <dir>` | **Yes, by pinning** — byte-for-byte against the `secc.leaf.der` the station wrote there. Only works because both processes share a filesystem: one dev process recognising another, not trust in any PKI sense. |
| `--tls` / `--tls-backend dotnet`, no roots | **No.** Any server certificate is accepted; a dev station mints a fresh self-signed one at startup and there is no channel to learn its fingerprint. The run prints a warning. |
| `--tls-backend bc --vehicle-cert <pfx>`, no roots and no `--pki-dir` | **No.** Nothing to pin against and nothing to chain to, so any peer is accepted and the run says so. |

Pinning and chaining are not alternatives and can both be on: one says "this exact station", the
other "a station some V2G root vouches for". What has not changed is the warning worth repeating —
without `--trust-roots`, a successful mutual-TLS handshake here is evidence that the *car* was
authenticated, not the station.

## What it is not

A conformance and research peer, not a car. Chains are validated only when `--trust-roots` says so
and revocation never is; its timeouts are a flat 2 s / 60 s rather than the standard's performance
tables; and it has no electrical layer at all. [The battery](#the-battery) is arithmetic on a
simulated clock — linear below the taper knee, with no temperature, no losses and no ageing — so a
run that ends "at 100 %" is reporting a sum and not a charging curve. The station's README lists the
same limits from the other side, where they matter more.

## Where the actual implementation is

Here is the wiring; the behaviour is in [`WWCP_ISO15118_Session`](../WWCP_ISO15118_Session), mostly
in `StateMachines/Iso2/Evcc2.cs` and `StateMachines/Iso20/Evcc20*.cs`.

Whether any of it conforms is a separate question, answered counterparty by counterparty in the
[conformance repository](../../../../../README.md) two repositories up.
