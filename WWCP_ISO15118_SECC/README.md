# WWCP_ISO15118_SECC — the charging station

A runnable ISO 15118 **SECC**: it listens, accepts a car, and runs a charging session to
`SessionStop`. One program, one role. The car is [`WWCP_ISO15118_EVCC`](../WWCP_ISO15118_EVCC/README.md),
and the two are meant to be pointed at each other — or at somebody else's implementation, which is
what most of the runs in this project actually do.

Open [`WWCP_ISO15118_SECC.slnx`](WWCP_ISO15118_SECC.slnx) in Visual Studio and press F5. It carries
the whole stack underneath — the session layer, the codec, SDP, SLAC, the PKI builder — so you can
set a breakpoint in `Secc20Base.SessionSetup` and watch a real car walk into it.

```bash
dotnet run --project WWCP_ISO15118_SECC
```

That is a complete station on port 15118 accepting **both** ISO 15118-2 and -20, DC, no TLS. It stays
up for as long as a session is paused and waiting to be rejoined.

## What it does

Everything the session layer implements, on the station side:

| | |
|---|---|
| **Both protocols** | `-2` and `-20`, chosen per connection by the SupportedAppProtocol handshake |
| **AC, DC and MCS** | `--mode ac\|dc\|mcs`; MCS is the DC message set under energy-transfer services 8/9 |
| **EIM and Plug & Charge** | offers both by default; validates the car's signed `AuthorizationReq` and says whether the challenge, the digest and the ECDSA signature checked out |
| **CertificateInstallation** | `-20` contract provisioning, including whether the issued key could be wrapped for the car's OEM key |
| **Scheduled and Dynamic** | `--dynamic` puts the Dynamic control-mode parameter set first |
| **Signed tariffs** | `--tariff-cert` signs the `-2` SalesTariff / `-20` AbsolutePriceSchedule offer |
| **Pause / resume** | keeps accepting after a paused session, and for `-20` verifies that the car coming back is the car that left |
| **Renegotiation** | `--renegotiate` notifies once mid-loop |
| **TLS** | two backends — see [Which TLS backend](#which-tls-backend-and-why-it-is-not-a-preference) below, because on Windows and macOS this is not a matter of taste |
| **SDP and SLAC** | `--sdp` advertises the endpoint on a link; `--slac` runs a pairing stage first |

## Which TLS backend, and why it is not a preference

`--tls` / `--tls-backend dotnet` uses .NET's `SslStream`, which is fast and native. It also cannot
serve the ISO 15118-20 TLS profile on two of the three platforms this runs on, and the failures are
not all loud:

| | .NET `SslStream` | BouncyCastle |
|---|---|---|
| **Linux** (OpenSSL) | TLS 1.3, secp521r1, per-connection suite pinning — all fine | fine |
| **Windows** (Schannel) | TLS 1.3 yes, but **no secp521r1 certificates** (measured: P-256 mutual TLS succeeds, P-521 fails "Authentication failed" server-side), **no per-connection cipher-suite pinning** (Schannel takes its list from system-wide policy; .NET throws), and it refuses to present a client chain whose root the machine does not trust | fine |
| **macOS** (SecureTransport) | **no TLS 1.3 at all** — Apple's API never gained it and .NET throws `PlatformNotSupportedException` | fine |

ISO 15118-20 asks for TLS 1.3, secp521r1 (or Ed448), and a pinned suite pair. So:

- **On Linux, either works.** Use `--tls` unless you want the -20 profile enforced rather than merely available.
- **On Windows and macOS, a real `-20` TLS session needs `--tls-backend bc`.** With `--tls` there you
  get a session that looks like it worked and is not `-20`-conformant: on macOS it silently negotiates
  TLS 1.2, on Windows it runs on -2-grade P-256 material.
- **For `-2`, `--tls` is fine everywhere.** The -2 profile is TLS 1.2 with P-256, which every platform does.

That is why this project's own Windows mutual-TLS tests use P-256, and why the one counterparty that
ships genuine secp521r1 material was reached with the managed backend. The measurements live in
`Transport/TlsPlatform.cs`; the reasoning and the deviation policy in
`EVSimulatorApp/docs/pki-model.md`.

## The defaults, and why

**`--protocol both`.** A real station takes whatever drives up, so that is what this one does unless
told otherwise. Each connection's handshake settles it independently, `-20` preferred: a dual-stack
car gets `-20`, a `-2`-only car gets `-2`, and neither needs the station restarted. `--protocol 2` or
`--protocol 20` pins one when the point of the run is that protocol — which is what the interop
harnesses under `tools/interop-*` do, because a run that silently changed protocol would prove
nothing.

**`--listen 15118`**, the IANA-registered V2G port, so a bare run needs no flag.

**`--mode dc`.** Unlike the protocol, this is *not* negotiated — the connector decides it, and both
sides must be told the same thing or the session fails on a message set the other did not expect. DC
is the default because it is what this station is usually pointed at: every `-20` counterparty run
in this project is DC, and DC is where the interesting parts live (CableCheck, PreCharge,
WeldingDetection, the bidirectional envelopes). `--mode ac` for the other one.

## Worth trying

```bash
# everything at its default: port 15118, both protocols, DC — and watch the car
# negotiate its way to -20
dotnet run --project WWCP_ISO15118_SECC

# offer Dynamic first: a car that takes the first parameter set runs Dynamic
dotnet run --project WWCP_ISO15118_SECC -- --dynamic

# the other connector
dotnet run --project WWCP_ISO15118_SECC -- --mode ac

# EIM only — some cars cannot cope with a service list containing one they do not support
dotnet run --project WWCP_ISO15118_SECC -- --no-pnc

# megawatt charging: services 8/9, the DC message set with a bigger envelope
dotnet run --project WWCP_ISO15118_SECC -- --mode mcs --protocol 20

# the -20-faithful TLS profile: TLS 1.3, secp521r1, mutual. Start this first —
# it mints the V2G hierarchy and writes the car's half into the shared directory.
dotnet run --project WWCP_ISO15118_SECC -- --tls-backend bc --pki-dir /tmp/v2gpki

# advertise on a link instead of being told a port (needs a real interface)
dotnet run --project WWCP_ISO15118_SECC -- --sdp --interface eth0
```

`--help` prints the full flag list. Flags that belong to the car — `--connect`, `--contract-cert`,
`--pause-resume` — are refused here by name rather than ignored, which is the main practical gain of
splitting the two roles apart.

## What it is not

A conformance and research peer, not a charging station. Four things it does not do, and they are
the reason not to put it in front of a real car as anything but a test instrument:

- **No certificate chain is validated.** Signatures verify against the leaf the car presented;
  nothing walks `SubCertificates` to a V2G root, checks validity dates or consults revocation. It
  proves a signature is well-formed, not that a contract is good.

  The TLS half is worth stating separately, because "mutual TLS succeeded" reads like an identity
  check and here it mostly is not: `--require-client-cert` on the .NET backend **requires** a client
  certificate and then accepts **any** — the car has to present something, and nothing decides what.
  Only `--tls-backend bc` checks, and it checks by pinning the exact Vehicle leaf this station just
  minted into `--pki-dir`, which is one dev process recognising another rather than trust in a PKI.
  The car's README has the same table from its side.
- **The timeouts are not the standard's** — a flat 2 s per message and 60 s per sequence, not the
  per-message performance tables.
- **The charge loop is a fixed three iterations**, not a battery filling up.
- **There is no electrical layer at all**: no contactor, no Control Pilot, no isolation monitoring.
  On a station that is the entire safety-relevant half, and it belongs to IEC 61851 rather than to
  ISO 15118.

## Where the actual implementation is

Here is the wiring; the behaviour is in [`WWCP_ISO15118_Session`](../WWCP_ISO15118_Session), mostly
in `StateMachines/Iso2/Secc2.cs` and `StateMachines/Iso20/Secc20*.cs`. `Secc2.Handle` and its `-20`
equivalent are pure synchronous transitions — a request in, a response and the next phase out — so
the interesting part is unit-testable without a socket, and this program is the thin loop that drives
it from a real stream.

Whether any of it conforms is a separate question, answered counterparty by counterparty in the
[conformance repository](../../../../../README.md) two repositories up.
