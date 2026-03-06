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

  row_prefix="| \`$control\` |"

  if command -v rg >/dev/null 2>&1; then
    row_found=0
    if rg -q "^\| \`$control\` \|" "$matrix_file"; then
      row_found=1
    fi
  elif command -v grep >/dev/null 2>&1; then
    row_found=0
    if grep -Fq "$row_prefix" "$matrix_file"; then
      row_found=1
    fi
  else
    echo "Neither 'rg' nor 'grep' is available; cannot validate parity matrix rows." >&2
    exit 1
  fi

  if [[ "$row_found" -ne 1 ]]; then
    echo "PARITY_MATRIX missing row for control: $control" >&2
    missing=1
  fi
done < "$inventory_file"

if [[ "$missing" -ne 0 ]]; then
  exit 1
fi

echo "Parity matrix completeness check passed."
