# Primitives vectors: EXIficient as the intended oracle

`Primitives.vectors.json` pins the byte layout of the schema-less EXI datatypes
(Unsigned/Signed Integer, Binary, Boolean, String). Right now its `expectedHex` is
**self-encoded** by our own `ExiPrimitives` — it guards against regressions but does not
prove wire conformance. The plan is to regenerate it against **EXIficient**, the reference
EXI processor that (unlike cbV2G) fully implements the datatype and value-table machinery.

## Why EXIficient and not cbV2G

cbV2G is our oracle for the *grammar* layer (AppProtocol, and later -2/-20 messages), but it
is a narrow, schema-specific codec: it is miss-only for strings and does not expose the raw
primitive datatypes in isolation. EXIficient is a general W3C EXI processor with a CLI, so it
can encode a bare typed value and hand back the exact bytes — the right tool for byte-level
datatype conformance and for the string value-table behaviour in `ExiStringTable`.

## Regeneration sketch (not yet wired up)

EXIficient needs a JRE. Kept out of the offline `dotnet test` path — this is a manual,
opt-in refresh, like `tools/cbv2g-ref` for the AppProtocol vectors.

1. Get EXIficient (pin a release): https://github.com/EXIficient/exificient. A convenient
   CLI wrapper is `com.siemens.ct.exi.cmd.EXIficientCMD`, or use
   [V2Gdecoder](https://github.com/FlUxIuS/V2Gdecoder) for a ready hex ⟷ XML tool.
2. For each datatype, encode a minimal schema-typed document whose single value is the vector
   input, with the ISO 15118 EXI options (bit-packed, `-strict`), and capture the value bytes
   (strip the header/grammar framing so only the datatype's octets remain).
3. Patch `expectedHex` back in, set `referenceEncoder.commit` to the pinned EXIficient version,
   and drop the `generatorNote` self-encoding warning.

Until then, `PrimitiveVectorTests` asserts our encoder reproduces these self-encoded bytes —
a regression guard, not a conformance claim. The hand-computed vectors in `ExiDatatypeTests`
and the CsCheck round-trips in `PrimitivePropertyTests` are the stronger current evidence.
