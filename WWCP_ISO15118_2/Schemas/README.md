# ISO 15118-2 schemas — provenance

The normative ISO 15118-2 EXI schemas, used as `<AdditionalFiles>` input to the source generator.

- **Source:** <https://standards.iso.org/iso/15118/-2/ed-2/en/>
- **Edition:** ed-2. The namespaces say 2013 (`urn:iso:15118:2:2013:MsgDef`); both years are correct
  and refer to different things.

**These files are not in the repository.** `bash new/download-schemas.sh` fetches them from ISO and
puts them here; [`../../SCHEMAS.md`](../../SCHEMAS.md) says why that is your download to make and
not ours to ship.

**`V2G_CI_AppProtocol.xsd` is not among them**, even though ISO's -2 directory carries one. That
copy is an older revision — no `elementFormDefault="qualified"`, plus an extra `protocolNameType`
capped at 30 characters — and it encodes differently. The one this codec is pinned against comes
from the -20 directory and lives in `WWCP_ISO15118_EXI/Schemas/`.

Until 2026-08 these came from [SwitchEV/RISE-V2G](https://github.com/SwitchEV/RISE-V2G) instead,
which is discontinued and whose redistribution chargebyte reads as legally shaky. Its copies turned
out to be the same schemas reformatted — indentation stripped, the `<xs:schema>` attributes
re-wrapped, ISO's editor comment replaced — and swapping in ISO's originals left all 834 tests and
every generated Kotlin file byte-identical.

| file | targetNamespace |
|---|---|
| `V2G_CI_MsgDef.xsd` | `urn:iso:15118:2:2013:MsgDef` (the `V2G_Message` wrapper) |
| `V2G_CI_MsgHeader.xsd` | `urn:iso:15118:2:2013:MsgHeader` (SessionID + optional Signature) |
| `V2G_CI_MsgBody.xsd` | `urn:iso:15118:2:2013:MsgBody` (all message bodies via `BodyElement`) |
| `V2G_CI_MsgDataTypes.xsd` | `urn:iso:15118:2:2013:MsgDataTypes` (shared types, enums) |
| `xmldsig-core-schema.xsd` | `http://www.w3.org/2000/09/xmldsig#` (XML signature) |

The construct inventory of this set is in `docs/xsd-inventory-15118-2.md`.
