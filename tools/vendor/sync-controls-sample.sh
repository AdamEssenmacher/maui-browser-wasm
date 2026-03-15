#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage: tools/vendor/sync-controls-sample.sh [--ref <git-ref>] [--repo <git-url>] [--dry-run]

Syncs dotnet/maui Controls.Sample into:
  vendor/upstream/controls-sample

The script:
1) Clones dotnet/maui to a temporary directory.
2) Splits the history for src/Controls/samples/Controls.Sample.
3) Runs git subtree add/pull --squash into this repository.
4) Writes metadata to vendor/upstream/controls-sample/.mirage-upstream.json.
EOF
}

require_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Missing required command: $1" >&2
    exit 1
  fi
}

ROOT="$(git rev-parse --show-toplevel)"
PREFIX="vendor/upstream/controls-sample"
UPSTREAM_REPO="https://github.com/dotnet/maui.git"
UPSTREAM_REF="main"
DRY_RUN="false"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --repo)
      UPSTREAM_REPO="$2"
      shift 2
      ;;
    --ref)
      UPSTREAM_REF="$2"
      shift 2
      ;;
    --dry-run)
      DRY_RUN="true"
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

require_cmd git
require_cmd date
require_cmd rsync

if [[ "$DRY_RUN" == "true" ]]; then
  echo "[dry-run] Preconditions validated."
  echo "[dry-run] Would sync '$UPSTREAM_REPO' ref '$UPSTREAM_REF' into '$PREFIX' via subtree."
  exit 0
fi

TMP_DIR="$(mktemp -d "${TMPDIR:-/tmp}/controls-sample-sync.XXXXXX")"
cleanup() {
  rm -rf "$TMP_DIR"
}
trap cleanup EXIT

UPSTREAM_CLONE="$TMP_DIR/maui"
SPLIT_REPO="$TMP_DIR/controls-sample-split"

echo "Cloning upstream repo (shallow): $UPSTREAM_REPO"
git clone --depth 1 --filter=blob:none --no-checkout "$UPSTREAM_REPO" "$UPSTREAM_CLONE"

git -C "$UPSTREAM_CLONE" sparse-checkout init --cone
git -C "$UPSTREAM_CLONE" sparse-checkout set src/Controls/samples/Controls.Sample
git -C "$UPSTREAM_CLONE" checkout "$UPSTREAM_REF"

echo "Creating split snapshot repo for src/Controls/samples/Controls.Sample"
mkdir -p "$SPLIT_REPO"
git -C "$SPLIT_REPO" init
git -C "$SPLIT_REPO" switch -c controls-sample-split
git -C "$SPLIT_REPO" config user.name "Mirage Vendor Sync"
git -C "$SPLIT_REPO" config user.email "mirage-vendor-sync@example.invalid"
rsync -a \
  --exclude ".git/" \
  "$UPSTREAM_CLONE/src/Controls/samples/Controls.Sample/" \
  "$SPLIT_REPO/"

UPSTREAM_SHA="$(git -C "$UPSTREAM_CLONE" rev-parse HEAD)"
git -C "$SPLIT_REPO" add .
git -C "$SPLIT_REPO" commit -m "Snapshot Controls.Sample from $UPSTREAM_SHA"
SPLIT_SHA="$(git -C "$SPLIT_REPO" rev-parse HEAD)"
UPSTREAM_SHA="$(git -C "$UPSTREAM_CLONE" rev-parse HEAD)"
SYNCED_AT_UTC="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"

echo "Split SHA: $SPLIT_SHA"
echo "Upstream SHA: $UPSTREAM_SHA"

if [[ -d "$ROOT/$PREFIX" ]]; then
  echo "Running subtree pull into $PREFIX"
  git -C "$ROOT" subtree pull --prefix="$PREFIX" "$SPLIT_REPO" "$SPLIT_SHA" --squash
else
  echo "Running subtree add into $PREFIX"
  git -C "$ROOT" subtree add --prefix="$PREFIX" "$SPLIT_REPO" "$SPLIT_SHA" --squash
fi

mkdir -p "$ROOT/$PREFIX"
LICENSE_SOURCE=""
for candidate in \
  "$UPSTREAM_CLONE/LICENSE.txt" \
  "$UPSTREAM_CLONE/LICENSE.TXT" \
  "$UPSTREAM_CLONE/LICENSE"; do
  if [[ -f "$candidate" ]]; then
    LICENSE_SOURCE="$candidate"
    break
  fi
done

THIRD_PARTY_SOURCE=""
for candidate in \
  "$UPSTREAM_CLONE/THIRD-PARTY-NOTICES.TXT" \
  "$UPSTREAM_CLONE/THIRD-PARTY-NOTICES.txt" \
  "$UPSTREAM_CLONE/THIRD-PARTY-NOTICES"; do
  if [[ -f "$candidate" ]]; then
    THIRD_PARTY_SOURCE="$candidate"
    break
  fi
done

if [[ -z "$LICENSE_SOURCE" ]]; then
  echo "Unable to locate upstream license file in $UPSTREAM_CLONE" >&2
  exit 1
fi

if [[ -z "$THIRD_PARTY_SOURCE" ]]; then
  echo "Unable to locate upstream third-party notices file in $UPSTREAM_CLONE" >&2
  exit 1
fi

cp "$LICENSE_SOURCE" "$ROOT/$PREFIX/LICENSE.upstream-MIT.txt"
cp "$THIRD_PARTY_SOURCE" "$ROOT/$PREFIX/THIRD-PARTY-NOTICES.upstream.txt"
cat > "$ROOT/$PREFIX/.mirage-upstream.json" <<EOF
{
  "upstreamRepo": "$UPSTREAM_REPO",
  "upstreamRef": "$UPSTREAM_REF",
  "upstreamHeadSha": "$UPSTREAM_SHA",
  "splitSha": "$SPLIT_SHA",
  "syncedAtUtc": "$SYNCED_AT_UTC"
}
EOF

echo "Controls.Sample sync complete: $PREFIX"
