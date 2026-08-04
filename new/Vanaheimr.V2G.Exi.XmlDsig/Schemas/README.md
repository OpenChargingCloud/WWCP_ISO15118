# W3C XMLDSig schema — provenance

The W3C XML Signature schema, standalone.

- **Source:** [W3C](https://www.w3.org/TR/xmldsig-core/), `xmldsig-core-schema.xsd`
- **Licence:** the W3C Document and Software licences, which permit redistribution. This is the
  one schema here that is not ISO's, so [`../../SCHEMAS.md`](../../SCHEMAS.md) does not apply to it.

**Not a duplicate of the -2 copy.** `Vanaheimr.V2G.Exi.Iso15118_2/Schemas/xmldsig-core-schema.xsd`
came from RISE-V2G and is stripped — no XML declaration, no DOCTYPE internal subset,
`version="0.1"` — and differs from this one in 98 lines. This is the full W3C original.

That difference is the reason this set exists at all. A Plug & Charge `SignedInfo` produced by
Josev or EXIficient is encoded against *this* grammar, standalone, which is not the combined
fragment grammar each message set carries. Verify with the wrong one and every signature fails
while looking locally consistent.


| file | targetNamespace |
|---|---|
| `xmldsig-core-schema.xsd` | `http://www.w3.org/2000/09/xmldsig#` |
