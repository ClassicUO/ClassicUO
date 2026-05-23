#!/usr/bin/env bash
# Pair up label-a and label-b screenshots produced by `agent-desktop scenario`
# and run `agent-desktop diff` for each matched pair.
#
# Usage:
#   ./diff-pairs.sh <out-dir> <label-a> <label-b> [tolerance]
#
# Outputs:
#   <out-dir>/diff/<NN>__<name>.png   highlighted diff per pair
#   <out-dir>/diff/summary.tsv        one line per pair: NN name fraction
#
# Example:
#   ./diff-pairs.sh ./compare-out main ecs 4

set -euo pipefail

if [ $# -lt 3 ]; then
    echo "usage: $0 <out-dir> <label-a> <label-b> [tolerance]" >&2
    exit 2
fi

OUT_DIR="$1"
LABEL_A="$2"
LABEL_B="$3"
TOL="${4:-4}"

CLI_DIR="$(cd "$(dirname "$0")/.." && pwd)"
CLI="dotnet $CLI_DIR/bin/Debug/net9.0/agent-desktop.dll"

mkdir -p "$OUT_DIR/diff"
SUMMARY="$OUT_DIR/diff/summary.tsv"
printf "index\tname\tmismatched_fraction\tmismatched_pixels\ttotal_pixels\n" > "$SUMMARY"

for raw_a in "$OUT_DIR"/"$LABEL_A"__*.raw.bin; do
    base="$(basename "$raw_a")"
    # Strip label prefix to get NN__name.raw.bin
    rest="${base#${LABEL_A}__}"
    raw_b="$OUT_DIR/${LABEL_B}__${rest}"
    if [ ! -f "$raw_b" ]; then
        echo "warn: missing pair for $base (expected $raw_b)" >&2
        continue
    fi
    # NN__name.raw.bin -> NN__name
    stem="${rest%.raw.bin}"
    diff_out="$OUT_DIR/diff/${stem}.png"

    json_line=$($CLI diff --a "$raw_a" --b "$raw_b" --out "$diff_out" --tolerance "$TOL")
    echo "$json_line"

    # Extract mismatchedFraction etc. from JSON for the summary.
    frac=$(echo "$json_line" | sed -n 's/.*"mismatchedFraction":\([^,}]*\).*/\1/p')
    mp=$(echo "$json_line"   | sed -n 's/.*"mismatchedPixels":\([^,}]*\).*/\1/p')
    tp=$(echo "$json_line"   | sed -n 's/.*"totalPixels":\([^,}]*\).*/\1/p')
    nn="${stem%%__*}"
    nm="${stem#*__}"
    printf "%s\t%s\t%s\t%s\t%s\n" "$nn" "$nm" "$frac" "$mp" "$tp" >> "$SUMMARY"
done

echo "---"
echo "summary: $SUMMARY"
column -t -s $'\t' "$SUMMARY" || cat "$SUMMARY"
