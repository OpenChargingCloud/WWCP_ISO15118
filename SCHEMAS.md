# The schemas are not in this repository

Every codec here is generated from the ISO 15118 XML schemas. They are not checked in, so a fresh
clone will not build until you fetch them:

```bash
bash tools/download-schemas.sh
```

That is one command, it needs `curl` and `unzip`, and it puts each schema where the build expects
it. Then `dotnet test -c Release WWCP_ISO15118.EXI.slnx`.

## Why you have to do this yourself

The schemas are ISO's. ISO publishes them at <https://standards.iso.org/iso/15118/> under the ISO
Customer Licence Agreement, and the permission it grants is to *use* them: *"You are permitted to
use the electronic insert(s) available on this site, in their original format without any
modifications for the purposes specified in their respective ISO standard(s)."* That is not a
permission to redistribute, and running the script is you accepting the agreement — the same act as
downloading them from the portal by hand. Nobody can accept it on your behalf, which is the whole
reason this is a script you run rather than files we ship.

The open-source implementations split on this. **cbexigen** ships none and downloads on demand
behind exactly that acceptance; its README says the schemas *"cannot be distributed with the code
generator"*. **RISE-V2G** shipped the ISO 15118-2 set in a public Apache-2.0 tree for years, and
this repository used to take its -2 copies from there. chargebyte's reading is that the RISE-V2G
arrangement is legally shaky. We now follow cbexigen.

## Where each schema comes from

| Set | Source |
|---|---|
| ISO 15118-2 | `https://standards.iso.org/iso/15118/-2/ed-2/en/` |
| ISO 15118-20, all seven message sets | `https://standards.iso.org/iso/15118/-20/ed-1/en/` |
| `AC_DER_IEC`, `AC_DER_SAE` | Amendment 1, `…/-20/ed-1/en/Amd/1/AMD1_xsdSchema.zip` |
| W3C XMLDSig | ISO's copy, distributed alongside both parts |

`SCHEMA_CACHE=<dir>` makes the script lay out schemas you already have instead of fetching them
again — useful offline, in CI, and for a second checkout on the same machine. It wants three
directories: `iso-2/`, `iso-20/` and `amd1/`.

## Three things that look like mistakes and are not

**`V2G_CI_AppProtocol.xsd` comes from the -20 directory, not the -2 one.** ISO's -2 folder carries
an older revision: no `elementFormDefault="qualified"`, and an extra `protocolNameType` capped at
30 characters. `elementFormDefault` decides whether local elements are namespace-qualified, which
changes the EXI grammar — so the two revisions do not encode alike, and this codec is pinned
against the -20 one. Measured, not assumed: swapping in the -2 copy is the one substitution that
does not survive the vector corpus.

**`V2G_CI_CommonTypes.xsd` and `xmldsig-core-schema.xsd` are copied into every -20 message set.**
All seven copies of `CommonTypes` are byte-identical, as are the three of `V2G_CI_AC.xsd`. This
mirrors cbexigen/cbV2G and is load-bearing: an EXI grammar is built per schema *set*, so the same
type in two sets is not the same grammar, and the sets must stay self-contained. Merging the copies
would change generated output.

**`WWCP_ISO15118_XMLDSig` is the same file as the -20 sets carry, and still a separate set.**
Not a duplicate by accident. A Plug & Charge `SignedInfo` produced by Josev or EXIficient is
encoded against the XMLDSig schema *standalone*, which is a different grammar from the combined one
each message set builds — same input file, different set membership. Verifying with the wrong one
of the two fails in the way that looks like it works.

## If the vectors go red after downloading

That means the schemas you fetched are not the revision this codec was pinned against — ISO
publishes amendments, and `…/Amd/` gains entries. The vector corpus under
`WWCP_ISO15118_EXI_Tests/Vectors/` is bytes produced by cbV2G and EXIficient, so it is the thing
that tells you, and `CLAUDE.md` has the rule that follows: never change wire semantics
speculatively, only on a concrete byte diff against a reference encoder.
