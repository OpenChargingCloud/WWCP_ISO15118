#!/usr/bin/env bash
#
# Fetches the ISO 15118 XML schemas this codec is generated from, and puts each one where the
# build expects it.
#
# The schemas are not in this repository. They are ISO's, published by ISO at
# https://standards.iso.org/iso/15118/ under the ISO Customer Licence Agreement, which grants use
# and not redistribution. Running this script is you accepting that agreement — clauses 1 (ISO's
# copyright), 7 (termination), 8 (limitations) and 9 (governing law) — exactly as you would by
# downloading them from the portal by hand. Nobody can accept it on your behalf.
#
#   bash new/download-schemas.sh
#
# Needs curl and unzip. Idempotent: run it again and it overwrites what it fetched before.

set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

ISO2='https://standards.iso.org/iso/15118/-2/ed-2/en'
ISO20='https://standards.iso.org/iso/15118/-20/ed-1/en'
AMD1="$ISO20/Amd/1/AMD1_xsdSchema.zip"

say() { printf '  %s\n' "$*"; }

# SCHEMA_CACHE lets a machine that already has the files lay them out without fetching again —
# offline rebuilds, CI, and a second checkout on the same machine. It expects the three
# directories this script would otherwise create: iso-2/, iso-20/ and amd1/.
if [ -n "${SCHEMA_CACHE:-}" ]; then
    work="$SCHEMA_CACHE"
    echo "Using schemas from \$SCHEMA_CACHE — nothing is downloaded."
    for d in iso-2 iso-20 amd1; do
        [ -d "$work/$d" ] || { echo "  missing: $work/$d" >&2; exit 1; }
    done
else
    work="$(mktemp -d)"
    trap 'rm -rf "$work"' EXIT

    echo "Fetching ISO 15118 schemas — by continuing you accept the ISO Customer Licence Agreement."
    echo

    mkdir -p "$work/iso-2" "$work/iso-20" "$work/amd1"

    for f in V2G_CI_MsgDef V2G_CI_MsgHeader V2G_CI_MsgBody V2G_CI_MsgDataTypes xmldsig-core-schema; do
        curl -fsS -o "$work/iso-2/$f.xsd" "$ISO2/$f.xsd"
    done
    say "ISO 15118-2 ed-2: 5 files"

    for f in V2G_CI_CommonMessages V2G_CI_CommonTypes V2G_CI_AC V2G_CI_DC V2G_CI_WPT V2G_CI_ACDP \
             V2G_CI_AppProtocol xmldsig-core-schema; do
        curl -fsS -o "$work/iso-20/$f.xsd" "$ISO20/$f.xsd"
    done
    say "ISO 15118-20 ed-1: 8 files"

    curl -fsS -o "$work/amd1.zip" "$AMD1"
    unzip -qo "$work/amd1.zip" -d "$work/amd1"
    # The archive's internal layout is ISO's, not ours — find the schemas wherever they landed.
    find "$work/amd1" -name 'V2G_CI_AC_DER_*.xsd' -exec cp {} "$work/amd1/" \; 2>/dev/null || true
    say "ISO 15118-20 Amd 1: $(find "$work/amd1" -maxdepth 1 -name '*.xsd' | wc -l) files"
fi
echo

# ---- lay them out the way each project's <AdditionalFiles> expects --------------------------
#
# A message set is generated from a *set* of schemas, and the grammar is built per set — which is
# why CommonTypes and xmldsig are copied into each one rather than shared. See SCHEMAS.md.

place() {                       # place <target-project> <source-dir> <file>...
    local project="$1" target="$1/Schemas" src="$2"; shift 2
    mkdir -p "$target"
    for f in "$@"; do cp "$src/$f" "$target/$f"; done
    say "$(printf '%-46s %d file(s)' "$project" $#)"
}

# ISO 15118-2. Note that AppProtocol does NOT come from here: the -2 directory carries an older
# revision of it — no elementFormDefault="qualified", and an extra protocolNameType capped at 30
# characters — which encodes differently. The -20 copy is the one this codec is pinned against.
place Vanaheimr.V2G.Exi.Iso15118_2 "$work/iso-2" \
    V2G_CI_MsgDef.xsd V2G_CI_MsgHeader.xsd V2G_CI_MsgBody.xsd V2G_CI_MsgDataTypes.xsd \
    xmldsig-core-schema.xsd

place Vanaheimr.V2G.Exi.Prototype "$work/iso-20" V2G_CI_AppProtocol.xsd
place Vanaheimr.V2G.Exi.XmlDsig   "$work/iso-20" xmldsig-core-schema.xsd

for set in CommonMessages:V2G_CI_CommonMessages AC:V2G_CI_AC DC:V2G_CI_DC \
           WPT:V2G_CI_WPT ACDP:V2G_CI_ACDP; do
    place "Vanaheimr.V2G.Exi.Iso15118_20.${set%%:*}" "$work/iso-20" \
        "${set##*:}.xsd" V2G_CI_CommonTypes.xsd xmldsig-core-schema.xsd
done

# The DER sets layer their own schema on top of the AC one, and theirs comes from Amendment 1.
for der in IEC SAE; do
    place "Vanaheimr.V2G.Exi.Iso15118_20.AC_DER_$der" "$work/iso-20" \
        V2G_CI_AC.xsd V2G_CI_CommonTypes.xsd xmldsig-core-schema.xsd
    cp "$work/amd1/V2G_CI_AC_DER_$der.xsd" \
       "Vanaheimr.V2G.Exi.Iso15118_20.AC_DER_$der/Schemas/"
done

echo
echo "Done. Now: dotnet test -c Release new/WWCP_ISO15118.EXI.slnx"
echo "The vector corpus is what tells you the schemas are the ones this codec was pinned against."
