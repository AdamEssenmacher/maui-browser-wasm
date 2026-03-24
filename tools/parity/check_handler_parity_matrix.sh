#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
matrix_file="$repo_root/docs/HANDLER_PARITY_MATRIX.md"
required_types_file="$repo_root/tools/parity/handler-parity-required-types.txt"

if [[ ! -f "$matrix_file" ]]; then
    echo "Missing matrix file: $matrix_file" >&2
    exit 1
fi

if [[ ! -f "$required_types_file" ]]; then
    echo "Missing required types file: $required_types_file" >&2
    exit 1
fi

tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT

expected_types_file="$tmp_dir/expected-types.txt"
actual_types_file="$tmp_dir/actual-types.txt"

awk '
    NF > 0 && $1 !~ /^#/ { print $1 }
' "$required_types_file" | awk '!seen[$0]++' > "$expected_types_file"

awk -F'|' '
    /^\| `[^`]+` \|/ {
        t = $2
        gsub(/^[ \t`]+|[ \t`]+$/, "", t)
        print t
    }
' "$matrix_file" > "$actual_types_file"

missing=0
while IFS= read -r expected_type; do
    [[ -z "$expected_type" ]] && continue
    if ! rg -Fqx "$expected_type" "$actual_types_file"; then
        echo "HANDLER_PARITY_MATRIX missing row for type: $expected_type" >&2
        missing=1
    fi
done < "$expected_types_file"

if [[ "$missing" -ne 0 ]]; then
    exit 1
fi

header_expected='| MAUI Type | Android Handler(s) (official) | iOS Handler(s) (official) | Mirage Handler(s) | Shape Grade | Implementation Grade | Divergence Notes |'
if ! rg -Fq "$header_expected" "$matrix_file"; then
    echo "HANDLER_PARITY_MATRIX header mismatch or missing required columns." >&2
    exit 1
fi

ff_rule_failed="$(
    awk -F'|' '
        function trim(s) {
            gsub(/^[ \t]+|[ \t]+$/, "", s)
            return s
        }
        /^\| `[^`]+` \|/ {
            mirage = trim($5)
            shape = trim($6)
            impl = trim($7)
            if (shape == "F" && impl == "F" && mirage != "none") {
                print $0
            }
        }
    ' "$matrix_file"
)"

if [[ -n "$ff_rule_failed" ]]; then
    echo "Rows with F/F must have Mirage Handler(s) = none:" >&2
    echo "$ff_rule_failed" >&2
    exit 1
fi

echo "Handler parity matrix checks passed."
