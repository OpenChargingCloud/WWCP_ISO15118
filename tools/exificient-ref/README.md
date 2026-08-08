# exificient-ref — second, independent EXI oracle (Siemens EXIficient)

A tiny CLI around [EXIficient](https://github.com/EXIficient/exificient) (the Siemens
implementation of the generic W3C EXI 1.0 specification) used to cross-validate our
XMLDSig fragment wire encoding against an EXI processor that has **no shared lineage**
with cbV2G/cbexigen. Where `tools/cbv2g-ref` diffs bytes against the *reference*
encoder our generator is modelled on, this tool answers a different question: are the
bytes we (and cbV2G) produce valid, standards-conformant, schema-informed EXI that any
compliant processor can decode back to the intended values?

This is a **development tool only**. `dotnet test` never runs it (no Java/network in
CI, per the project's build rule) — it exists purely to produce the evidence recorded
below. There is nothing to regenerate on every change; re-run it only if the signed
fragments' wire shape changes.

## Build

Needs JDK ≥ 11 and Gradle (dependencies resolve from Maven Central on first run):

```sh
export JAVA_HOME="/path/to/jdk-21"   # Gradle 9.x requires JVM 17+
gradle compileJava
```

## Usage

```
gradle run --args="encode <xsd-entry-point> <fragment|document> <in.xml>  <out.hex>"
gradle run --args="decode <xsd-entry-point> <fragment|document> <in.hex>  <out.xml>"
gradle run --args="primitives <in.tsv> <out.tsv>"
```

The `primitives` mode is driven by [`primitives.py`](primitives.py) rather than by hand:

```sh
python tools/exificient-ref/primitives.py             # report
python tools/exificient-ref/primitives.py --update    # + rewrite the vector file's provenance
```

`<xsd-entry-point>` is the top-level schema file; EXIficient's `XSDGrammarsBuilder`
follows `<xs:import>` transitively (Xerces resolves `schemaLocation` relative to the
importing file), so pointing at `V2G_CI_MsgDef.xsd` (-2) or `V2G_CI_CommonMessages.xsd`
(-20 CommonMessages) pulls in the header/body/datatypes schemas and
`xmldsig-core-schema.xsd` the same way our own generator's fragment grammar does.
Coding mode is BIT_PACKED with `FidelityOptions.createDefault()` (non-strict
schema-informed grammar) — the same convention cbV2G/cbexigen and our generator use.

## What was cross-checked

Both of the SignedInfo fragments already byte-diffed against cbV2G
([`Iso15118_2FragmentTests.cs`](../../WWCP_ISO15118_EXI_Tests/Iso15118_2FragmentTests.cs),
[`Iso15118_20FragmentTests.cs`](../../WWCP_ISO15118_EXI_Tests/Iso15118_20FragmentTests.cs))
were fed to EXIficient's **decoder** with the matching XSD entry point and
`fragment=true`:

| fixture | schema entry point | expected bytes (cbV2G-verified) |
|---|---|---|
| [`fixtures/iso2-signedinfo-expected.hex`](fixtures/iso2-signedinfo-expected.hex) | `WWCP_ISO15118_2/Schemas/V2G_CI_MsgDef.xsd` | `Iso15118_2FragmentTests.SignedInfo_Fragment_MatchesCbV2G` |
| [`fixtures/iso20-common-signedinfo-expected.hex`](fixtures/iso20-common-signedinfo-expected.hex) | `WWCP_ISO15118_20.CommonMessages/Schemas/V2G_CI_CommonMessages.xsd` | `Iso15118_20FragmentTests.SignedInfo_Fragment_MatchesCbV2G` |

Both decoded to exactly the expected `SignedInfo` content — same `CanonicalizationMethod`/
`SignatureMethod`/`DigestMethod` algorithm URIs, same `Reference URI`, same base64
digest bytes (1..32 for -2/SHA-256, 1..64 for -20/SHA-512) — see
[`fixtures/iso2-signedinfo-expected-decoded.xml`](fixtures/iso2-signedinfo-expected-decoded.xml)
and
[`fixtures/iso20-common-signedinfo-expected-decoded.xml`](fixtures/iso20-common-signedinfo-expected-decoded.xml).

**Conclusion:** the exact bytes our codec emits (byte-identical to cbV2G) are valid
schema-informed EXI per the real `xmldsig-core-schema.xsd` grammar, decodable by an
independent, generic EXI 1.0 processor with no relationship to cbV2G/cbexigen, and
they carry the intended cryptographic values. That is the property that actually
matters for XMLDSig correctness (the verifier must recover the exact bytes that were
hashed/signed), so this closes out the "external cross-validation" item for both
Phase 3 (-2) and Phase 4 (-20 CommonMessages).

### Schema-less primitives — `BitEncoderChannel` (2026-07-25)

`Primitives.vectors.json` was the **last self-referential vector file in the repo**: its
`expectedHex` was produced by the very codec it tests, so it caught regressions but proved
nothing about wire conformance. cbV2G is no help here — it is a schema-informed codec, and
these are the raw EXI 1.0 §7.1 datatypes underneath any grammar.

EXIficient exposes exactly that layer as
`com.siemens.ct.exi.core.io.channel.BitEncoderChannel`, so the `primitives` mode encodes each
vector through it and [`primitives.py`](primitives.py) diffs the result:

**All 18 baseline vectors match byte-for-byte** — 4 unsignedInteger, 6 signedInteger (incl.
negatives), 3 binary, 2 boolean, 3 string. The vector file's provenance block now records this.

**Plus five non-ASCII string vectors sourced *from* EXIficient** (23 total). These are the one
place cbV2G structurally cannot help — it rejects code points > U+007F, so no cbV2G vector for
them can ever exist. EXIficient has no such limit, so here it is the *primary* oracle rather than
a counter-check, and our codec is verified against it:

| vector | value | bytes |
|---|---|---|
| `string_nonascii_uuml` | `ü` (U+00FC) | `03 fc 01` |
| `string_nonascii_euro` | `€` (U+20AC) | `03 ac 41` |
| `string_nonascii_mixed` | `Grüße aus Jena` | `10 47 72 fc 01 …` |
| `string_nonascii_astral` | `😀` (U+1F600) | `03 80 ec 07` |
| `string_nonascii_astral_mixed` | `a😀b` | `05 61 80 ec 07 62` |

The astral rows are the ones with teeth: U+1F600 is **one code point but two UTF-16 units**, and
both encoders emit length `1 + 2` — so an encoder that counted UTF-16 units would diverge here and
nowhere else. Our `EnumerateRunes`-based encoding agrees with EXIficient exactly.

Note **Josev cannot substitute for EXIficient here**: it re-encodes with EXIficient itself, so it
would be the same encoder behind a Python wrapper — realistic at session level, but not an
independent oracle.

One honest detail: `encodeString` writes a bare length prefix, while ISO 15118's schema-less
string *values* use the value-table **miss framing** (length + 2) that lives one layer above the
channel. The harness applies that offset explicitly and lets EXIficient do the character
encoding — so the string rows compare the character encoding independently, with the framing
convention stated rather than assumed.

### AC DER (-20 Amendment 1) — whole documents, `fragment=false` (2026-07-25)

This is the **only** external oracle available for AC DER: cbexigen cannot generate the
amendment schemas at all (it crashes on a substitution-group head fed by two schemas —
see `docs/ac-der.md` in the `EVSimulatorApp` repository above this one), so there are no cbV2G reference bytes to
diff against. EXIficient is schema-generic and has no such limitation.

Because there is no cbV2G ground truth for AC DER, the run was **calibrated first** on a
case where ground truth does exist — a plain, non-DER AC message, whose bytes our AC codec
produces and whose grammar cbV2G does cover:

| fixture | schema entry point | source of the bytes |
|---|---|---|
| [`fixtures/iso20-ac-cpdreq-plain-expected.hex`](fixtures/iso20-ac-cpdreq-plain-expected.hex) | `…Iso15118_20.AC/Schemas/V2G_CI_AC.xsd` | calibration: plain AC codec |
| [`fixtures/iso20-ac-der-iec-cpdreq-expected.hex`](fixtures/iso20-ac-der-iec-cpdreq-expected.hex) | `…Iso15118_20.AC_DER_IEC/Schemas/V2G_CI_AC_DER_IEC.xsd` | `Iso15118_20AcDerTests.Iec_DerEnergyTransferMode_Roundtrips` |

Both decoded correctly. The DER one is the interesting result — EXIficient recovers
`DER_AC_CPDReqEnergyTransferMode` as the selected substitution member, and the
**namespace split is the evidence that the extension was understood**: the inherited
fields come back in `urn:iso:std:iso:15118:-20:AC` while the DER-only fields
(`EVProcessing`, `EVMaximumDischargePower`, `EVMinimumDischargePower`,
`EVSessionTotalDischargeEnergyAvailable`) come back in
`urn:iso:std:iso:15118:-20:AC-DER-IEC`, with every value intact — see
[`fixtures/iso20-ac-der-iec-cpdreq-expected-decoded.xml`](fixtures/iso20-ac-der-iec-cpdreq-expected-decoded.xml).

Note the two bitstreams differ in exactly one selector byte plus the appended DER content
(`…fa a0 62 …` plain vs `…fa a0 63 …` + DER fields), which is what a substitution-group
choice should look like.

**What this does and does not prove.** It proves our AC DER bytes are valid,
standards-conformant, schema-informed EXI that an independent processor decodes to the
intended values — a genuine external check, and the same property the SignedInfo
cross-check establishes. It is **not** a byte-for-byte comparison against a second
*encoder*: as the section below records, EXIficient's encoder uses a different profile
and emits longer streams, so encode-side diffing is not apples-to-apples for any of our
message sets. AC DER therefore has a real oracle in the decode direction only.

## Known open point: EXIficient's own *encoder* takes more bits

Running `encode` on the equivalent XML input (`fixtures/iso2-signedinfo.xml`,
`fixtures/iso20-common-signedinfo.xml`) does **not** reproduce the cbV2G byte length —
EXIficient's own encoder emits a noticeably longer bitstream (243 vs. 173 bytes for
-2) for semantically identical content. This was investigated but not root-caused:

- It isn't whitespace/pretty-printing (minifying the input XML made no difference).
- It isn't the `mixed="true"`/`<xs:any>` wildcard extensibility points on
  `CanonicalizationMethodType`/`SignatureMethodType`/`DigestMethodType` (stripping
  them from a scratch copy of the schema changed the output by 1 byte, not ~70).
  cbV2G's own C structs don't model that wildcard content at all (see the generated
  `ANY` fields our codec exposes), so this was the leading hypothesis; it wasn't it.
- The trailing `DigestValue` binary octets are bit-identical in both streams (just
  shifted by whatever bit offset precedes them), confirming the divergence is
  entirely in how the three `anyURI` `Algorithm` string values (and/or the preceding
  event-code choices) get bit-packed, not in a structural/semantic difference.

Per the project's wire-semantics rule, this is **not** acted on — cbV2G byte-exact
match stays the authoritative conformance oracle, and this is a second-oracle
*validation* tool, not a wire-format source of truth. (That rule still stands; see
*The one place the rule now has a switch* below for the single case where it was
worth making the alternative buildable rather than only arguable.) Recorded here so nobody
re-discovers this from scratch: if the encode-side gap is ever worth closing (e.g. to
also byte-diff against EXIficient directly), start by dumping EXIficient's grammar
event trace for the `SignatureMethod`/`CanonicalizationMethod`/`DigestMethod`
start-tags to see exactly which event get 2nd-level vs. 1st-level codes for the
`anyURI` `Algorithm` attribute.

## Files

| file | purpose |
|------|---------|
| `build.gradle` | EXIficient 1.0.7 dependency + `application` plugin (`mainClass = ExificientRef`) |
| `src/main/java/ExificientRef.java` | the encode/decode CLI |
| `fixtures/*.xml` | the exact SignedInfo content from the C# fragment tests, as standalone XML |
| `fixtures/*-expected.hex` | the cbV2G-verified `expectedHex` from the C# tests, space-separated |
| `fixtures/*-expected-decoded.xml` | EXIficient's decode of the above — the cross-validation evidence |

## Plug & Charge SignedInfo signing form (2026-07-21 — root-caused)

`fixtures/iso20-common-signedinfo-transforms.xml` is Josev's exact live PnC `SignedInfo` (a `Transforms`
element + SHA-256 URIs). Set `EXIF_CANONICAL=1` to encode in EXIficient's **Canonical EXI** (W3C exi-c14n)
mode instead of the default.

Josev's `SignedInfo` signature verifies against **none** of the fragment encodings built over the *combined*
`V2G_CI_CommonMessages` schema (our cbV2G-matched 210 B; EXIficient default 245 B; EXIficient Canonical EXI
246 B), even though our fragment codec is byte-exact for the reference *digest*. **Root cause (found by
decompiling Josev's `EXICodec.jar`):** Josev maps the XMLDSig namespace to `BuiltInSchema.XSDCore` →
`XMLDSIG_Core_Schema_Grammar`, a grammar built from **`xmldsig-core-schema.xsd` standalone**, so its EXI
*Fragment* top-level element event code is one bit narrower (far fewer global elements) and the whole bitstream
shifts. Josev's own codec emits a **209-byte** `SignedInfo`, and Josev's captured signature verifies against it
(`JosevPnCSignatureDiag.JosevSignsSignedInfoOverStandaloneXmldsigGrammar`).

Note: encoding this same `SignedInfo` here with the **standalone** `xmldsig-core-schema.xsd` as the entry point
(`encode …/xmldsig-core-schema.xsd fragment …`) via EXIficient's *runtime* `XSDGrammarsBuilder` gives **244 B**
— close but not byte-identical to Josev's **209 B** *pre-generated* grammar. So the faithful reproduction uses
Josev's own jar/grammar, not EXIficient's runtime build of the same schema. See
`WWCP_ISO15118_EXI_Tests/Interop/JosevPnCSignatureDiag.cs` and
`docs/interop-runs/2026-07-21-iso20-dc-pnc-tls/notes.md`.

## The one place the rule now has a switch: ACDP document-element numbering (2026-08-07)

Every encoded message opens with a *document element selector* — an index into the schema's global
elements. cbexigen and a specification-built EXI processor disagree about that index whenever two
global elements share one named type: cbexigen groups them behind the alphabetically-first element of
the type, plain lexicographic order does not. In ISO 15118 that happens exactly once, so **ACDP is the
only message set the two number differently**:

```
cbexigen : 0 ACDP_ConnectReq  1 ACDP_DisconnectReq  2 ACDP_ConnectRes  3 ACDP_DisconnectRes
sorted   : 0 ACDP_ConnectReq  1 ACDP_ConnectRes     2 ACDP_DisconnectReq 3 ACDP_DisconnectRes
```

`ACDP_DisconnectReq` and `ACDP_DisconnectRes` have no types of their own — ISO commented the
declarations out and pointed the elements at `ACDP_ConnectReqType`/`ResType`.

**How it surfaced.** The conformance repository ran the whole `-20` corpus through EXIficient on
2026-08-07 (347 frames, 332 byte-exact — `docs/interop-runs/2026-08-07-exificient-iso20/`). Two ACDP
frames did not survive, and they are exactly the two indices above:

- our `ACDP_DisconnectReq` (selector 1) is read as `ACDP_ConnectRes`, a longer type → the decoder runs
  out of bits;
- our `ACDP_ConnectRes` (selector 2) is read as `ACDP_DisconnectReq`, a shorter type → **it decodes
  cleanly, as the wrong message.** That is the worse of the two: nothing reports it.

**What was done, and what was not.** The default is unchanged and stays cbexigen-compatible: the vector
corpus is cbV2G's output, and every cbexigen-derived stack in the field (EVerest, tux-evse) numbers
these the same way we do. What changed is that the alternative is now *buildable* rather than only
arguable —

```xml
<ExiDocumentElementOrder>ExiSorted</ExiDocumentElementOrder>
```

— declared `CompilerVisibleProperty` in every codec project, read by `ExiCodecGenerator`, applied in
`GrammarBuilder.OrderDocumentElements`, and covered by `GeneratorDocumentOrderTests` on a synthetic
schema of the same shape rather than on ISO's. Verified end to end: building
`WWCP_ISO15118_20.ACDP` with the property set produces the sorted numbering, and the first byte after
the EXI header for `ACDP_DisconnectReq` becomes `08` — which is the byte EXIficient writes for that
message. Unset, the numbering and every byte are as before.

Deciding which one is *right* needs the EXI 1.0 text on the document grammar's global-element order,
not this file. What is settled is the consequence of the disagreement, and that producing either
encoding is now a build property rather than a rewrite.

> **Decided 2026-08-08 — and the paragraph above is superseded.** The EXI 1.0 text does settle it.
> Second Edition §8.5.1 gives the `DocContent` grammar one `SE` production per global element, over the
> qnames "sorted lexicographically, first by local-name, then by uri", with nothing said about shared
> types. Checked against an implementation too, on a synthetic three-element schema in the ACDP shape
> (two sharing a type, a third sorting between them): EXIficient assigns 0/1/2 where the grouping gives
> 0/2/1.
>
> So `Directory.Build.props` now sets `ExiSorted` for these codecs. The property still accepts
> `CbV2GCompatible` and the generator's own default is still `CbV2GCompatible` — this is the ISO
> codecs' decision, not the generator's. Two vectors moved, both ACDP, both recorded in
> `Iso15118_20.ACDP.vectors.json` under `deviatesFromReferenceEncoder`.

## …and the WPT particle grammar, the same day

The other four frames EXIficient could not read are a different construct: an optional repeating
element followed by an optional one, which in ISO 15118 occurs only in
`WPT_FinePositioning{,Setup}Req/ResType` — `VendorSpecificDataContainer` (`minOccurs="0"
maxOccurs="16"`) then an optional `WPT_LF_DataPackageList`.

Verified against cbV2G at `03350be048b3` (`encode_iso20_wpt_WPT_FinePositioningReqType`, grammar ids
178/179/180):

```
cbexigen  state 178 (no items):   SE(list)=0                 EE=1
          state 179 (one item):   LOOP=0  SE(LF list)=1      EE=2
          state 180 (two items):          SE(LF list)=0      EE=1    <- no third item, ever

schema    state A   (no items):   SE(list)=0  SE(LF list)=1  EE=2
          state B   (n items):    LOOP=0      SE(LF list)=1  EE=2    <- loops to maxOccurs
```

Unlike the ACDP ordering, this one is **not** an EXI interpretation question. The schema says
`maxOccurs="16"` and makes the suffix independently optional; cbexigen's grammar caps at two and gates
the suffix behind at least one container. Its own struct array is sized 16, so the ceiling is in the
grammar, not the storage. Two valid documents are therefore unrepresentable, and the generated encoder
throws rather than truncate.

The visible symptom is one event code: with no items and no LF list, cbexigen's end-element is `1` and
the schema grammar's is `2`. EXIficient read our `1` as a start element, looked for content that was
not there, and reported `Premature EOS` — for all four messages.

Same treatment as ACDP, same default:

```xml
<ExiParticleGrammar>SchemaConformant</ExiParticleGrammar>
```

`GrammarBuilder` carries it on the plan; `CodecEmitter` branches to a pair of emitters that are
*shorter* than the cbexigen ones, because there is nothing to unroll. Covered by
`GeneratorParticleGrammarTests` on a synthetic schema of the same shape. Building
`WWCP_ISO15118_20.WPT` with it emits `foreach` over the list with a 16-item guard, the LF list
reachable from the empty state, and `w.WriteBits(2, 2)` for the empty case — which turns the tail of
our `WPT_FinePositioningReq` from `…0120` into `…0140`, the bytes EXIficient writes for that message.
Unset, every byte is as before.

> **Decided 2026-08-08: set.** `Directory.Build.props` selects `SchemaConformant`. The deciding point
> was not readability but capability — reproducing cbexigen's grammar meant our encoder *refused
> messages ISO permits* (a third container item; the suffix without a preceding item), which is a worse
> thing for a conformance codec to do than to differ by a byte from one implementation. A test that
> asserted the refusal is now a round-trip test of the message it used to reject. Four vectors moved,
> all WPT, recorded in `Iso15118_20.WPT.vectors.json` under `deviatesFromReferenceEncoder`.

## And the third frame, which was simply a bug (2026-08-07)

The sixth frame EXIficient could not read — `AC_ChargeParameterDiscoveryRes_DER` — was neither of the
above. No switch, no fork, no cbV2G to match: a defect.

Where the other two needed a judgement about which reading of EXI to follow, this one has only one
reading. EXI unrolls a bounded particle into one grammar state per occurrence; below `minOccurs` the
state has a **single** production, `SE(item)`, because ending the element there would be invalid, so
its event code is one bit. Only from `minOccurs` on does the state also offer the end-element and the
code widen to two. The generator gave every occurrence after the first the wide code — correct for
`minOccurs≤1`, one bit too many for anything else.

ISO 15118 has five particles with `minOccurs="2"`: `CurveDataPoint` in both DER amendments, and
`TxSpecData`, `RxSpecData`, `PulseSequenceOrder` in WPT. cbexigen generates none of them — it cannot
build the DER schemas at all, and the cbV2G WPT corpus leaves `LF_SystemSetupData`, which is where the
three WPT ones live, absent. So there were no reference bytes for any of the five, which is exactly why
this survived: the corpus can only catch what something else also encoded.

Confirmed without ISO's schemas, by asking EXIficient to encode a synthetic
`minOccurs="2" maxOccurs="10"` document and reading the bits back:

```
minOccurs=1, 2 items:  SE(Doc) SE·CH·val·EE   SE(2 bits)·CH·val·EE   EE(2 bits)
minOccurs=2, 2 items:  SE(Doc) SE·CH·val·EE   SE(1 bit) ·CH·val·EE   EE(2 bits)
either,     10 items:  … the tenth occurrence is followed by a ONE-bit EE — the max-reached state
```

The ten-item `minOccurs="2"` body is 120 bits with no padding at all, which leaves no room to be wrong
about the arithmetic.

The second line of that table also generalised something the emitter had only as a `maxOccurs=2`
special case: a list filled to its maximum ends with a one-bit end-element for **any** maximum. The
special case was the general rule.

`CodecEmitter.ForcedOccurrences` now drives all four list emitters and their decode counterparts.
Everything with `minOccurs≤1` is emitted character for character as before — asserted in
`GeneratorForcedOccurrenceTests`, since the vector corpus is worth exactly as much as the stability of
those bytes. One vector changed: `AC_ChargeParameterDiscoveryRes_DER`, still 241 B, and EXIficient now
round-trips it.
