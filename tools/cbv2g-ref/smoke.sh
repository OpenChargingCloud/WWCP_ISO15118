#!/usr/bin/env bash
# Quick manual sanity check of the cbv2g-ref harness against known values.
set -euo pipefail
BIN="${CBV2G_REF_BUILD:-$HOME/cbv2g-ref-build}/cbv2g_ref"

echo "--- res OK_SuccessfulNegotiation, schemaId=1 (cbV2G test expects 80 40 00 40) ---"
printf 'res\n0 1\n' | "$BIN" encode

echo "--- res OK_minor_deviation, schemaId=2 ---"
printf 'res\n1 2\n' | "$BIN" encode

echo "--- res Failed_NoNegotiation, no schemaId ---"
printf 'res\n2 -\n' | "$BIN" encode

echo "--- req iso2_only (our vector: 80 0e ba b9 37 ...) ---"
printf 'req\n2 0 1 1 urn:iso:15118:2:2013:MsgDef\n' | "$BIN" encode

echo "--- req iso20_only ---"
printf 'req\n1 0 1 1 urn:iso:std:iso:15118:-20:CommonMessages\n' | "$BIN" encode

echo "--- roundtrip: decode the res we just made ---"
printf 'res\n0 1\n' | "$BIN" encode | "$BIN" decode

echo "--- roundtrip: decode req iso2 ---"
printf 'req\n2 0 1 1 urn:iso:15118:2:2013:MsgDef\n' | "$BIN" encode | "$BIN" decode
