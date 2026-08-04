# ISO 15118-20 CommonMessages schemas — provenance

The normative ISO 15118-20 (2022) EXI schemas for the **CommonMessages** message set, used as
`<AdditionalFiles>` input to the source generator.

- **Source:** ISO directly — <https://standards.iso.org/iso/15118/>
- **Edition:** ISO 15118-20:2022

**These files are not in the repository.** `bash tools/download-schemas.sh` fetches them from ISO
and puts them here; [`../../SCHEMAS.md`](../../SCHEMAS.md) says why that is your download to make
and not ours to ship.


| file | targetNamespace |
|---|---|
| `V2G_CI_CommonMessages.xsd` | `urn:iso:std:iso:15118:-20:CommonMessages` |
| `V2G_CI_CommonTypes.xsd` | `urn:iso:std:iso:15118:-20:CommonTypes` |
| `xmldsig-core-schema.xsd` | `http://www.w3.org/2000/09/xmldsig#` |
