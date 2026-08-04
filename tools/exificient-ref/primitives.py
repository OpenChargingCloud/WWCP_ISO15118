#!/usr/bin/env python3
"""Cross-check Primitives.vectors.json against EXIficient's own bit-level encoder channel.

Development tool only — `dotnet test` never runs this (no JRE in CI, per the project's build rule).
It converts the vector file to the TSV the Java harness reads, invokes
`ExificientRef primitives`, and diffs the result against the checked-in `expectedHex`.

    python tools/exificient-ref/primitives.py            # report only
    python tools/exificient-ref/primitives.py --update    # also rewrite the vector file's provenance

Requires JAVA_HOME and gradle on PATH (see README.md).
"""
import argparse
import json
import pathlib
import shutil
import subprocess
import sys

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parent.parent
VECTORS = REPO / "Vanaheimr.V2G.Exi.Tests" / "Vectors" / "Primitives.vectors.json"


def vector_arg(v: dict) -> str:
    """The single 'value' column the Java side expects, per datatype."""
    return v["valueHex"] if v["datatype"] == "binary" else v["value"]


def norm(hexstr: str) -> str:
    return hexstr.replace(" ", "").lower()


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--update", action="store_true",
                    help="rewrite the vector file's provenance block when everything matches")
    args = ap.parse_args()

    doc = json.loads(VECTORS.read_text(encoding="utf-8"))
    vectors = doc["vectors"]

    tsv_in, tsv_out = HERE / "out-primitives-in.tsv", HERE / "out-primitives-out.tsv"
    tsv_in.write_text(
        "".join(f"{v['name']}\t{v['datatype']}\t{vector_arg(v)}\n" for v in vectors),
        encoding="utf-8")

    gradle = next((p for p in (shutil.which(n) for n in ("gradle", "gradle.exe", "gradle.bat")) if p), None)
    if gradle is None:
        print("gradle not found on PATH — see tools/exificient-ref/README.md", file=sys.stderr)
        return 2
    subprocess.run(
        [gradle, "-q", "--console=plain", "run",
         "--args=primitives out-primitives-in.tsv out-primitives-out.tsv"],
        cwd=HERE, check=True)

    got = dict(line.split("\t", 1)
               for line in tsv_out.read_text(encoding="utf-8").splitlines() if line)

    width = max(len(v["name"]) for v in vectors)
    mismatches = []
    for v in vectors:
        ours, theirs = norm(v["expectedHex"]), norm(got.get(v["name"], ""))
        ok = ours == theirs
        if not ok:
            mismatches.append((v["name"], v["expectedHex"], got.get(v["name"], "<missing>")))
        print(f"{'ok  ' if ok else 'DIFF'} {v['name']:<{width}}  ours={v['expectedHex']:<14} "
              f"exificient={got.get(v['name'], '<missing>')}")

    print(f"\n{len(vectors) - len(mismatches)}/{len(vectors)} match")
    for name, ours, theirs in mismatches:
        print(f"  MISMATCH {name}: ours={ours!r} exificient={theirs!r}")

    if args.update and not mismatches:
        doc["generator"] = "self-encoded, cross-checked against EXIficient (see tools/exificient-ref/primitives.py)"
        doc["generatorNote"] = (
            "Expected bytes are produced by this repo's codec AND independently reproduced by "
            "EXIficient's BitEncoderChannel (EXI 1.0 §7.1), so they are no longer self-referential. "
            "Re-run tools/exificient-ref/primitives.py after changing the primitive layer."
        )
        doc["referenceEncoder"] = {
            "name": "EXIficient",
            "repo": "https://github.com/EXIficient/exificient",
            "artifact": "com.siemens.ct.exi:exificient:1.0.7",
            "note": "Cross-checked via BitEncoderChannel; string vectors use the value-table miss "
                    "framing (length + 2), which sits above the channel API and is applied explicitly.",
        }
        VECTORS.write_text(json.dumps(doc, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
        print(f"\nupdated provenance in {VECTORS.relative_to(REPO)}")

    return 1 if mismatches else 0


if __name__ == "__main__":
    sys.exit(main())
