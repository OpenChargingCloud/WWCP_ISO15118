# Where the schemas come from, and why they are in this repository

Every codec here is generated from the ISO 15118 XML schemas, which are checked in under each
message set's `Schemas/` directory. The schemas are ISO's work, not ours. This note records where
each set came from and why we ship them, so that nobody has to reconstruct the reasoning later.

Each `Schemas/README.md` carries the per-set provenance and file table; this is the part they share.

## Two established practices

The open-source ISO 15118 implementations have settled on two different answers, and it is worth
seeing both before reading ours.

**cbexigen / EVerest ships none.** Its README: *"In order to be able to produce a codec, the
standards' XML schema files are required. These cannot be distributed with the code generator."*
It offers `--auto-download-public-xsd` instead, which fetches them and makes the user accept the
ISO Customer Licence Agreement at that moment — clauses 1 (ISO's copyright), 7, 8 and 9.

**RISE-V2G ships them.** `RISE-V2G-Shared/src/main/resources/schemas/` has carried
`V2G_CI_MsgDef.xsd`, `MsgHeader`, `MsgBody`, `MsgDataTypes`, `AppProtocol` and
`xmldsig-core-schema.xsd` in a public Apache-2.0 repository for years.

The distinction the two are reacting to is that **publicly downloadable is not the same as
redistributable**. ISO publishes the schemas at <https://standards.iso.org/iso/15118/>, and the
terms there grant use, not onward distribution: *"You are permitted to use the electronic insert(s)
available on this site, in their original format without any modifications for the purposes
specified in their respective ISO standard(s)."*

## What we do

**We follow RISE-V2G: the schemas are checked in.** A codec generator whose input is missing cannot
be built, cannot be tested against a byte-level oracle, and cannot be reviewed by anyone who has not
separately bought or downloaded the standard. That cost is paid on every clone, by every
contributor, forever; the alternative is a copy of a file ISO itself publishes.

**Nothing here has been edited by us.** Where two sets carry a file of the same name whose bytes
differ, that is because the upstream copies differ, not because one was changed here.

That is not quite the same as saying every file is an original, and the difference is worth being
exact about, because ISO's terms permit use *"in their original format without any modifications"*:

- The seven **-20** sets came from ISO directly and are originals.
- Four of the six **-2** files — `V2G_CI_MsgDef`, `MsgHeader`, `MsgBody`, `MsgDataTypes` — carry
  ISO's own header comment naming the catalogue entry, intact. RISE-V2G passed them on unchanged.
- The **-2 copy of `xmldsig-core-schema.xsd` is not an original.** It is a stripped variant: no XML
  declaration, no DOCTYPE internal subset, `version="0.1"`, 98 lines apart from the W3C file the
  -20 sets carry. It is W3C's schema rather than ISO's, so ISO's terms do not reach it — but it is
  also not what W3C publishes.

That last one is left alone deliberately rather than tidied towards the original, because
`SignedInfo` is in the -2 fragment list and two checked-in vectors pin its bytes. RISE-V2G may well
have used the stripped variant precisely because that is what interoperates. Establishing which of
the two the reference encoders agree with is a measurement against the vector corpus, not a
judgement call — see `CLAUDE.md`: never change wire semantics speculatively.

**RISE-V2G is discontinued.** Its README now points at Josev Community as the successor, so the -2
source is a frozen tree that will never be corrected. The pinned commit keeps that reproducible.
Moving the -2 set to ISO directly, as the -20 sets already are, would give one origin instead of
two and remove that dependency; it is the obvious next step and it is an experiment, because the
generator's input would change.

If ISO objects, the address is in the licence and we will take them out and switch to the cbexigen
arrangement. Until then this is a considered position rather than an oversight, which is the reason
it is written down.

## Per set

| Set | Source |
|---|---|
| ISO 15118-2 (2013) | [SwitchEV/RISE-V2G](https://github.com/SwitchEV/RISE-V2G) (discontinued), pinned commit — see `Vanaheimr.V2G.Exi.Iso15118_2/Schemas/README.md` |
| SupportedAppProtocol | RISE-V2G, same tree |
| ISO 15118-20 (2022), all seven sets | ISO directly, <https://standards.iso.org/iso/15118/> |
| W3C XMLDSig | [W3C](https://www.w3.org/TR/xmldsig-core/) — not ISO's, and under the W3C Document/Software licence, which does permit redistribution |

## Two things that look like mistakes and are not

**`V2G_CI_CommonTypes.xsd` and `xmldsig-core-schema.xsd` are duplicated into every -20 message
set.** All seven copies of `CommonTypes` are byte-identical, as are the three of `V2G_CI_AC.xsd`.
This mirrors cbexigen/cbV2G and is load-bearing: an EXI grammar is built per schema *set*, so the
same type in two sets is not the same grammar and the sets must stay self-contained. Factoring them
into one shared copy would change generated output.

**The -2 and -20 copies of `xmldsig-core-schema.xsd` are different files** — 98 differing lines.
The -2 copy, from RISE-V2G, is stripped: no XML declaration, no DOCTYPE internal subset,
`version="0.1"`. The -20 copy is the full W3C original. They are not interchangeable, and that is
also why `Vanaheimr.V2G.Exi.XmlDsig` exists as a set of its own: a Plug & Charge `SignedInfo` that
Josev or EXIficient produced is encoded against the standalone grammar, and verifying it with a
message set's combined grammar fails in the way that looks like it works.
