# Vector provenance: cbV2G reference output

**Status: done (Phase 0).** The vectors in `AppProtocol.vectors.json` are no longer
self-encoded. Their `expectedHex` is produced by EVerest's **libcbv2g** at a pinned
commit, so a green test run proves wire conformance against the de-facto ISO 15118
reference codec — not merely internal self-consistency.

## Regenerate

The oracle and the driver that runs it sit in two different repositories, so each command below is
written from the root of the one that owns it:

```sh
# 1. build the oracle (once; needs a C toolchain — WSL works well on Windows).
#    From this repository's root.
wsl -d Debian -- bash tools/cbv2g-ref/build.sh

# 2. regenerate expectedHex for every vector (also verifies each round-trips).
#    From the conformance repository's root — the driver lives there, with the rest of the rig.
wsl -d Debian -- python3 tools/regenerate-appprotocol-vectors.py
```

The pinned commit is recorded in `AppProtocol.vectors.json` under
`referenceEncoder.commit` and in `tools/cbv2g-ref/CMakeLists.txt` (`CBV2G_GIT_TAG`).
Bump both together. See `tools/cbv2g-ref/README.md` for details.

## Known limitations of cbV2G as an oracle

- **ASCII only.** cbV2G rejects code points > U+007F, so a non-ASCII-namespace
  vector cannot be generated against it. Multi-byte rune encoding is left to
  dedicated C# unit tests (Phase 1), which have no external oracle yet.
- **Max 5 AppProtocol entries.** cbV2G caps the array at 5 (a buffer limit). The
  wire grammar itself terminates the list with a 2-bit EE at any count, so there is
  no special "at maxOccurs" case to exercise; the 5-entry vector covers the loop.

## What the vectors cover

- All three `ResponseCode` values × {SchemaID present, absent}.
- Single-entry, two-entry and five-entry requests (the list-loop event codes).
- `Priority` at 1, 2, 19, 20 (the 5-bit n-bit-unsigned boundaries).
- A 99-character ASCII namespace (long character run) near `maxLength=100`.
- Version numbers above 127 (multi-byte EXI Unsigned Integer).
- DIN SPEC 70121, ISO 15118-2 and ISO 15118-20 namespaces.
