# ISO 15118-2 schemas — provenance

The normative ISO 15118-2 (2013) EXI schemas, used as `<AdditionalFiles>` input to the
source generator. Taken verbatim from the open-source RISE-V2G reference implementation,
which has carried them in a public Apache-2.0 repository for years.

- **Source:** [SwitchEV/RISE-V2G](https://github.com/SwitchEV/RISE-V2G),
  `RISE-V2G-Shared/src/main/resources/schemas/`
- **Pinned commit:** `055806d22c591f843186579b9945255793d0800f`

ISO schemas are copyrighted, and the codec generators disagree about what follows from that —
cbexigen ships none, RISE-V2G ships them. We follow RISE-V2G. The reasoning, and what would
make us revisit it, is in [`../../SCHEMAS.md`](../../SCHEMAS.md).

**Two things about this particular set.** RISE-V2G is discontinued — its README points at Josev
Community as the successor — so this is a frozen tree, which is what the pinned commit is for.
And the four `V2G_CI_Msg*.xsd` files still carry ISO's own header comment naming the catalogue
entry, so they are originals passed on unchanged; `xmldsig-core-schema.xsd` below is **not**.

That copy is stripped: no XML declaration, no DOCTYPE internal subset, `version="0.1"` — 98 lines
apart from the W3C original that the -20 sets carry. It is deliberately not corrected towards the
original, because `SignedInfo` is in this project's `<ExiFragmentElements>` and two checked-in
vectors pin its encoded bytes. Which of the two variants cbV2G and EXIficient actually agree with
is a question for the vector corpus, not for a tidy-up.

| file | targetNamespace |
|---|---|
| `V2G_CI_MsgDef.xsd` | `urn:iso:15118:2:2013:MsgDef` (the `V2G_Message` wrapper) |
| `V2G_CI_MsgHeader.xsd` | `urn:iso:15118:2:2013:MsgHeader` (SessionID + optional Signature) |
| `V2G_CI_MsgBody.xsd` | `urn:iso:15118:2:2013:MsgBody` (all message bodies via `BodyElement`) |
| `V2G_CI_MsgDataTypes.xsd` | `urn:iso:15118:2:2013:MsgDataTypes` (shared types, enums) |
| `xmldsig-core-schema.xsd` | `http://www.w3.org/2000/09/xmldsig#` (XML signature) |

The construct inventory of this set is in `docs/xsd-inventory-15118-2.md`.
