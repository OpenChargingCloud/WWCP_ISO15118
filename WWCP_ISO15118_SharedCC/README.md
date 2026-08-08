# WWCP_ISO15118_SharedCC — what both roles need

The parts [`WWCP_ISO15118_SECC`](../WWCP_ISO15118_SECC/README.md) and
[`WWCP_ISO15118_EVCC`](../WWCP_ISO15118_EVCC/README.md) would otherwise each carry a copy of. Not a
layer, not an abstraction over the two roles — just the handful of mechanics that are identical on
both sides and unpleasant to get subtly different.

| | |
|---|---|
| `Credentials` | every `--*-cert` flag on either program. A `.p12` is an unordered bag, so "the leaf" means "the one certificate with a private key" and everything else is the chain above it — written once, and every failure names the flag that caused it |
| `TlsBackend` | none / .NET `SslStream` / BouncyCastle, and the doc comment explaining when the last one stops being optional |
| `V2GInterface` | resolving `--interface` (listing what the machine *does* have when the name is wrong), a random MAC for a simulated SLAC node, and how `-2`/`-20` and `AC`/`DC` are spelled in logs |

The role-specific shapes stay with their role: `SeccPki` mints the dev hierarchy, `EvccPki` loads it,
and each program builds its own `PncEvccOptions`/`CertInstallEvccOptions` out of the primitives here.
The split is "who knows about PKCS#12" versus "who knows what a car does with a contract".

Nothing in here is an ISO 15118 concept. The protocol lives in
[`WWCP_ISO15118_Session`](../WWCP_ISO15118_Session) and below; this is command-line plumbing that two
programs happen to share, and it is a library rather than a copy-paste because the alternative was
about sixty duplicated lines whose two copies could drift apart without anything failing.
