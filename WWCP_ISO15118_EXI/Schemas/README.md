# ISO 15118 SupportedAppProtocol schema — provenance

The SupportedAppProtocol schema — the handshake that chooses between ISO 15118-2 and -20
before either has been agreed, which is why it has a namespace of its own and predates both.

- **Source:** <https://standards.iso.org/iso/15118/-20/ed-1/en/> — the **-20** directory

**These files are not in the repository.** `bash tools/download-schemas.sh` fetches them from ISO and
puts them here; [`../../SCHEMAS.md`](../../SCHEMAS.md) says why that is your download to make and
not ours to ship.

**Taken from -20 on purpose, even though this is the -2-era handshake.** ISO's -2 directory carries
an older revision of the same file: it lacks `elementFormDefault="qualified"` and adds a
`protocolNameType` capped at 30 characters. `elementFormDefault` decides whether local elements are
namespace-qualified, so the two revisions do not encode alike — and of every schema substitution
tried here, this is the only one that does not survive the vector corpus. The hand-written
`AppProtocolEntry` beside it agrees with the -20 revision: `ProtocolNamespace` is capped at 100.

This one is the diff reference for the whole generator: the AppProtocol codec beside it is
hand-written, and the generated one must agree with it byte for byte.


| file | targetNamespace |
|---|---|
| `V2G_CI_AppProtocol.xsd` | `urn:iso:15118:2:2010:AppProtocol` |
