# ISO 15118-2 schemas — provenance

The normative ISO 15118-2 (2013) EXI schemas, used as `<AdditionalFiles>` input to the
source generator. ISO schemas are copyrighted, so they are not redistributed by the codec
generators (cbexigen ships none); these copies are taken verbatim from the open-source
RISE-V2G reference implementation.

- **Source:** [SwitchEV/RISE-V2G](https://github.com/SwitchEV/RISE-V2G),
  `RISE-V2G-Shared/src/main/resources/schemas/`
- **Pinned commit:** `055806d22c591f843186579b9945255793d0800f`

| file | targetNamespace |
|---|---|
| `V2G_CI_MsgDef.xsd` | `urn:iso:15118:2:2013:MsgDef` (the `V2G_Message` wrapper) |
| `V2G_CI_MsgHeader.xsd` | `urn:iso:15118:2:2013:MsgHeader` (SessionID + optional Signature) |
| `V2G_CI_MsgBody.xsd` | `urn:iso:15118:2:2013:MsgBody` (all message bodies via `BodyElement`) |
| `V2G_CI_MsgDataTypes.xsd` | `urn:iso:15118:2:2013:MsgDataTypes` (shared types, enums) |
| `xmldsig-core-schema.xsd` | `http://www.w3.org/2000/09/xmldsig#` (XML signature) |

The construct inventory of this set is in `docs/xsd-inventory-15118-2.md`.
