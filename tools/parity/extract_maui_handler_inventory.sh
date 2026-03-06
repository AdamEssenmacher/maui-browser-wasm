#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
inventory_file="$repo_root/tools/parity/maui-modern-controls.tsv"
matrix_file="$repo_root/docs/PARITY_MATRIX.md"
hosting_file="$repo_root/src/Maui.Browser/Hosting/MauiBrowserHostingExtensions.cs"

echo "== MAUI modern controls inventory =="
awk -F'\t' 'NF >= 2 && $1 !~ /^#/ { printf "%-24s %s\n", $1, $2 }' "$inventory_file"

echo
echo "== Browser handler registrations =="
rg 'AddHandler\(typeof\(' "$hosting_file" | sed 's/^[[:space:]]*//'

echo
echo "== Matrix status snapshot =="
awk -F'|' '
  /^\| `[^`]+` \|/ {
    control=$2; gsub(/^[ \t`]+|[ \t`]+$/, "", control);
    status=$5; gsub(/^[ \t]+|[ \t]+$/, "", status);
    if (status != "" && status != "---") {
      printf "%-24s %s\n", control, status;
    }
  }
' "$matrix_file"
