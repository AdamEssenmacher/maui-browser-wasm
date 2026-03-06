#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
inventory_file="$repo_root/tools/parity/maui-modern-controls.tsv"
matrix_file="$repo_root/docs/PARITY_MATRIX.md"

if [[ ! -f "$inventory_file" ]]; then
  echo "Missing inventory file: $inventory_file" >&2
  exit 1
fi

if [[ ! -f "$matrix_file" ]]; then
  echo "Missing matrix file: $matrix_file" >&2
  exit 1
fi

missing=0
while IFS=$'\t' read -r control _; do
  if [[ -z "${control:-}" || "$control" == \#* ]]; then
    continue
  fi

  if ! rg -q "^\| \`$control\` \|" "$matrix_file"; then
    echo "PARITY_MATRIX missing row for control: $control" >&2
    missing=1
  fi
done < "$inventory_file"

if [[ "$missing" -ne 0 ]]; then
  exit 1
fi

echo "Parity matrix completeness check passed."
