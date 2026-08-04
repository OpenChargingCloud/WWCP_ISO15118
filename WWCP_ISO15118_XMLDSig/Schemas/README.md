# W3C XMLDSig schema — provenance

The W3C XML Signature schema, standalone.

- **Source:** ISO's copy, distributed alongside both parts —
  <https://standards.iso.org/iso/15118/-20/ed-1/en/>
- The schema itself is W3C's, under the W3C Document and Software licences, which do permit
  redistribution. It is fetched with the rest only because it arrives in the same directories.

**These files are not in the repository.** `bash tools/download-schemas.sh` fetches them from ISO and
puts them here; [`../../SCHEMAS.md`](../../SCHEMAS.md) says why that is your download to make and
not ours to ship.

**The same file as every message set carries, and still a set of its own.** That is not an
oversight, and it is not because the files differ — measured, ISO's -2 copy, ISO's -20 copy and the
old RISE-V2G one are all structurally identical, 158 nodes, differing only in formatting. What
differs is *set membership*: a Plug & Charge `SignedInfo` produced by Josev or EXIficient is
encoded against this schema **standalone**, which builds a different grammar from the combined one
each message set produces. Verify with the wrong one of the two and every signature fails while
looking locally consistent.


| file | targetNamespace |
|---|---|
| `xmldsig-core-schema.xsd` | `http://www.w3.org/2000/09/xmldsig#` |
