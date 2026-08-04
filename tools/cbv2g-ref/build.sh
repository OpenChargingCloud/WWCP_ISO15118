#!/usr/bin/env bash
# Build the cbv2g-ref harness under WSL (or any Linux with cmake + a C compiler).
# Source dir is this script's directory; build output goes to a local build dir
# outside the repo so the fetched libcbv2g sources are never checked in.
set -euo pipefail

SRC="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD="${CBV2G_REF_BUILD:-$HOME/cbv2g-ref-build}"

echo "source: $SRC"
echo "build:  $BUILD"

cmake -S "$SRC" -B "$BUILD" -DCMAKE_BUILD_TYPE=Release
cmake --build "$BUILD" -j4

echo "binary: $BUILD/cbv2g_ref"
"$BUILD/cbv2g_ref" 2>/dev/null || true   # prints usage, exit 1 is expected
echo "OK"
