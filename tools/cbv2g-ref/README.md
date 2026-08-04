# cbv2g-ref — SupportedAppProtocol reference oracle

A tiny CLI around EVerest's [libcbv2g](https://github.com/EVerest/libcbv2g) that
encodes/decodes the ISO 15118 **SupportedAppProtocol** (SAP) handshake to and from
wire-conformant EXI hex. It is the reference oracle used to (re)generate the
`expectedHex` values in
[`../../Vanaheimr.V2G.Exi.Tests/Vectors/AppProtocol.vectors.json`](../../Vanaheimr.V2G.Exi.Tests/Vectors/AppProtocol.vectors.json).

This is a **development tool only**. `dotnet test` never runs it — the tests read the
checked-in vector JSON. You only need it to regenerate those vectors.

## Pinned reference

libcbv2g is fetched at a pinned commit via CMake `FetchContent`, declared in
[`CMakeLists.txt`](CMakeLists.txt) (`CBV2G_GIT_TAG`). The same SHA is recorded in the
vector file's `referenceEncoder.commit`. Bump both together.

Current pin: `03350be048b35b179905129005a97144a4bdcf93`
(cbexigen generator version 871fcb3, libcbv2g 0.3.1).

The libcbv2g clone lives in the build directory and is **not** checked into this repo.
Only the harness (`main.c`, `CMakeLists.txt`, scripts) is versioned.

## Build

Needs a C compiler, CMake ≥ 3.14, and network access (for the first fetch). On this
Windows machine the path of least resistance is **WSL** (Debian, gcc + cmake):

```sh
wsl -d Debian -- bash /mnt/d/Coding/OpenChargingCloud/Vanaheimr.V2G.Exi/tools/cbv2g-ref/build.sh
```

The binary lands at `~/cbv2g-ref-build/cbv2g_ref` (override with `CBV2G_REF_BUILD`).

## Line protocol

`cbv2g_ref encode` reads a message description from stdin and writes space-separated
lowercase hex (including the `0x80` EXI header, excluding any V2GTP header):

```
req
<major> <minor> <schemaId> <priority> <namespace>   # one line per AppProtocol
```
```
res
<codeIndex> <schemaId|->     # codeIndex: 0=OK, 1=OK_minor, 2=Failed (XSD declaration order)
```

`cbv2g_ref decode` reads hex from stdin and prints the same shape back, so a
round-trip can be asserted.

### Constraints inherited from libcbv2g

- **ASCII only.** cbV2G rejects any namespace character > U+007F.
- **Max 5 AppProtocol entries.** cbV2G caps the array at 5 (buffer limit; the wire
  grammar itself is unbounded up to the schema's `maxOccurs=20`).

## Regenerate the vectors

After building, run the driver (Python 3, under WSL so it can call the Linux binary):

```sh
wsl -d Debian -- python3 /mnt/d/Coding/OpenChargingCloud/Vanaheimr.V2G.Exi/tools/regenerate-appprotocol-vectors.py
```

It pipes every vector's `input` through `encode`, verifies each round-trips through
`decode`, and writes `expectedHex` / `expectedBytes` / `generator` /
`generatedAtUtc` back into the vector file.

## Files

| file | purpose |
|------|---------|
| `CMakeLists.txt` | fetch pinned libcbv2g, compile the SAP subset + `main.c` |
| `main.c`         | the encode/decode CLI |
| `build.sh`       | one-shot configure + build |
| `smoke.sh`       | quick manual sanity check against known values |
