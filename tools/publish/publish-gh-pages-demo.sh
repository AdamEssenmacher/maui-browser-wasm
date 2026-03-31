#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage:
  tools/publish/publish-gh-pages-demo.sh --demo <id> [--public-repo-dir <path>] [--dry-run]

Options:
  --demo <id>              Demo id from tools/publish/demos.manifest.json (required)
  --public-repo-dir <path> Local checkout of the public GitHub Pages repo (default: ../MirageUI)
  --dry-run                Build and stage assets, then print intended repo changes without mutating/pushing
  -h, --help               Show this help text
USAGE
}

fail() {
  echo "ERROR: $*" >&2
  exit 1
}

require_cmd() {
  local cmd="$1"
  command -v "$cmd" >/dev/null 2>&1 || fail "Required command not found: $cmd"
}

normalize_repo_url() {
  local url="$1"
  url="${url#git@github.com:}"
  if [[ "$url" != "$1" ]]; then
    url="https://github.com/$url"
  fi
  url="${url%.git}"
  echo "$url"
}

ensure_branch_checked_out() {
  local repo_dir="$1"
  local branch="$2"

  if git -C "$repo_dir" show-ref --verify --quiet "refs/heads/$branch"; then
    git -C "$repo_dir" checkout "$branch" >/dev/null
  else
    git -C "$repo_dir" checkout -b "$branch" "origin/$branch" >/dev/null
  fi
}

write_root_index_html() {
  local output_path="$1"
  local site_base_path="$2"
  local merged_json="$3"

  local site_root="${site_base_path%/}/"
  {
    cat <<EOF
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>MirageUI Demos</title>
  <style>
    :root { color-scheme: light dark; }
    body {
      margin: 0;
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
      background: #111827;
      color: #e5e7eb;
    }
    main {
      max-width: 960px;
      margin: 0 auto;
      padding: 3rem 1.25rem;
    }
    h1 { margin-top: 0; }
    .subtitle { color: #9ca3af; margin-bottom: 2rem; }
    .demo-list { list-style: none; margin: 0; padding: 0; display: grid; gap: 1rem; }
    .demo-card {
      background: #1f2937;
      border: 1px solid #374151;
      border-radius: 10px;
      padding: 1rem 1.25rem;
    }
    .demo-card a {
      color: #93c5fd;
      font-weight: 600;
      text-decoration: none;
      font-size: 1.05rem;
    }
    .demo-card a:hover { text-decoration: underline; }
    .demo-card p { margin: .5rem 0 0; color: #d1d5db; }
    footer { margin-top: 2.5rem; font-size: .875rem; color: #9ca3af; }
    footer a { color: #93c5fd; }
  </style>
</head>
<body>
  <main>
    <h1>MirageUI Demo Pages</h1>
    <p class="subtitle">Published WebAssembly demos built from the private Mirage source repository.</p>
    <ul class="demo-list">
EOF

    while IFS= read -r demo; do
      local title description public_path href
      title="$(jq -r '.title | @html' <<<"$demo")"
      description="$(jq -r '.description | @html' <<<"$demo")"
      public_path="$(jq -r '.publicPath' <<<"$demo")"
      href="${site_root}demos/${public_path%/}/"

      printf '      <li class="demo-card">\n'
      printf '        <a href="%s">%s</a>\n' "$href" "$title"
      printf '        <p>%s</p>\n' "$description"
      printf '      </li>\n'
    done < <(jq -c '.[]' "$merged_json")

    cat <<EOF
    </ul>
    <footer>
      <p>License notices: <a href="${site_root}THIRD_PARTY_NOTICES.md">THIRD_PARTY_NOTICES.md</a></p>
    </footer>
  </main>
</body>
</html>
EOF
  } >"$output_path"
}

write_root_404_html() {
  local output_path="$1"
  local site_base_path="$2"
  local site_root="${site_base_path%/}/"

  cat >"$output_path" <<EOF
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>MirageUI Demo Redirect</title>
</head>
<body>
  <script>
    (function () {
      var siteRoot = "${site_root}";
      var path = window.location.pathname || "";
      var search = window.location.search || "";
      var hash = window.location.hash || "";

      if (!path.startsWith(siteRoot)) {
        window.location.replace(siteRoot);
        return;
      }

      var relative = path.substring(siteRoot.length);
      var segments = relative.split("/").filter(Boolean);

      if (segments.length >= 2 && segments[0] === "demos") {
        var demoId = segments[1];
        var demoRoot = siteRoot + "demos/" + demoId + "/";
        var deepLinkSuffix = path.substring(demoRoot.length) + search + hash;
        var redirectUrl = new URL(demoRoot, window.location.origin);

        if (deepLinkSuffix) {
          redirectUrl.searchParams.set("__mirage_route__", deepLinkSuffix);
        }

        window.location.replace(redirectUrl.toString());
        return;
      }

      window.location.replace(siteRoot + search + hash);
    })();
  </script>
</body>
</html>
EOF
}

normalize_demo_index_html() {
  local index_path="$1"
  local base_href="$2"

  perl -0777 -i -pe 's{\b(src|href)=(")/(?!/)}{$1=$2./}g' "$index_path"

  BASE_HREF="$base_href" perl -0777 -i -pe '
my $base = $ENV{"BASE_HREF"};
if (!s{<base\s+href="[^"]*"\s*/?>}{<base href="$base">}i) {
  s{<head>}{<head>\n  <base href="$base">}i;
}
' "$index_path"

  perl -0777 -i -pe '
my $script = <<'"'"'SCRIPT'"'"';
  <script>
    (function () {
      var currentUrl = new URL(window.location.href);
      var pendingRoute = currentUrl.searchParams.get("__mirage_route__");

      if (!pendingRoute) {
        return;
      }

      currentUrl.searchParams.delete("__mirage_route__");

      var baseHref = document.querySelector("base")?.getAttribute("href") || "/";
      history.replaceState(null, "", baseHref + pendingRoute);
    })();
  </script>
SCRIPT

if (index($_, "__mirage_route__") < 0) {
  s{</head>}{$script\n</head>}i;
}
' "$index_path"
}

stamp_demo_entrypoints() {
  local demo_dir="$1"
  local cache_stamp="$2"
  local index_path="$demo_dir/index.html"
  local main_path="$demo_dir/main.mjs"

  [[ -f "$index_path" ]] || return
  [[ -f "$main_path" ]] || return

  CACHE_STAMP="$cache_stamp" perl -0777 -i -pe '
my $stamp = $ENV{"CACHE_STAMP"};
s{src="main\.mjs(?:\?v=[^"]*)?"}{src="main.mjs?v=$stamp"}gi;
' "$index_path"

  CACHE_STAMP="$cache_stamp" perl -0777 -i -pe '
my $stamp = $ENV{"CACHE_STAMP"};
s{"\./_framework/dotnet\.js(?:\?v=[^"]*)?"}{"./_framework/dotnet.js?v=$stamp"}g;
s{'"'"'\./_framework/dotnet\.js(?:\?v=[^'"'"']*)?'"'"'}{'"'"'./_framework/dotnet.js?v=$stamp'"'"'}g;
s{"\./_framework/mirage-dom\.js(?:\?v=[^"]*)?"}{"./_framework/mirage-dom.js?v=$stamp"}g;
s{'"'"'\./_framework/mirage-dom\.js(?:\?v=[^'"'"']*)?'"'"'}{'"'"'./_framework/mirage-dom.js?v=$stamp'"'"'}g;
' "$main_path"

  # GH Pages demo deploys intentionally omit .pdb assets (ignored in the public repo),
  # so ensure runtime startup doesn't hard-fail when symbol downloads are unavailable.
  perl -0777 -i -pe '
if (index($_, ".withDebugging(0)") < 0) {
  s/\.withDiagnosticTracing\(false\)/.withDiagnosticTracing(false)\n      .withDebugging(0)\n      .withConfig({ ignorePdbLoadErrors: true })/g;
}
' "$main_path"
}

DEMO_ID=""
DRY_RUN=0

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
MANIFEST_PATH="$REPO_ROOT/tools/publish/demos.manifest.json"
PUBLIC_REPO_DIR="$REPO_ROOT/../MirageUI"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --demo)
      [[ $# -ge 2 ]] || fail "--demo requires a value"
      DEMO_ID="$2"
      shift 2
      ;;
    --public-repo-dir)
      [[ $# -ge 2 ]] || fail "--public-repo-dir requires a value"
      PUBLIC_REPO_DIR="$2"
      shift 2
      ;;
    --dry-run)
      DRY_RUN=1
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      fail "Unknown argument: $1"
      ;;
  esac
done

[[ -n "$DEMO_ID" ]] || fail "--demo is required"
[[ -f "$MANIFEST_PATH" ]] || fail "Manifest not found at $MANIFEST_PATH"

require_cmd dotnet
require_cmd git
require_cmd jq
require_cmd rsync
require_cmd perl

PAGES_PUBLIC_REPO_URL="$(jq -er '.pages.publicRepoUrl' "$MANIFEST_PATH")"
PAGES_PUBLIC_BRANCH="$(jq -er '.pages.publicBranch' "$MANIFEST_PATH")"
PAGES_SITE_BASE_PATH="$(jq -er '.pages.siteBasePath' "$MANIFEST_PATH")"

DEMO_ENTRY_JSON="$(jq -cer --arg id "$DEMO_ID" '.demos[] | select(.id == $id)' "$MANIFEST_PATH")" \
  || fail "Demo id '$DEMO_ID' was not found in $MANIFEST_PATH"

DEMO_TITLE="$(jq -er '.title' <<<"$DEMO_ENTRY_JSON")"
DEMO_DESCRIPTION="$(jq -er '.description' <<<"$DEMO_ENTRY_JSON")"
DEMO_PROJECT_PATH="$(jq -er '.projectPath' <<<"$DEMO_ENTRY_JSON")"
DEMO_PUBLIC_PATH="$(jq -er '.publicPath' <<<"$DEMO_ENTRY_JSON")"
DEMO_DEST_REL="demos/${DEMO_PUBLIC_PATH%/}"
DEMO_BASE_HREF="${PAGES_SITE_BASE_PATH%/}/${DEMO_DEST_REL}/"

PROJECT_ABS_PATH="$REPO_ROOT/$DEMO_PROJECT_PATH"
[[ -f "$PROJECT_ABS_PATH" ]] || fail "Demo project not found: $PROJECT_ABS_PATH"

if [[ "$DRY_RUN" -eq 0 && -n "$(git -C "$REPO_ROOT" status --porcelain)" ]]; then
  fail "Source repo working tree must be clean before publishing: $REPO_ROOT"
fi

PUBLIC_REPO_DIR="$(cd "$PUBLIC_REPO_DIR" 2>/dev/null && pwd)" \
  || fail "Public repo directory does not exist: $PUBLIC_REPO_DIR"
[[ -d "$PUBLIC_REPO_DIR/.git" ]] || fail "Public repo dir is not a git checkout: $PUBLIC_REPO_DIR"

PUBLIC_REMOTE_URL="$(git -C "$PUBLIC_REPO_DIR" remote get-url origin)"
if [[ "$(normalize_repo_url "$PUBLIC_REMOTE_URL")" != "$(normalize_repo_url "$PAGES_PUBLIC_REPO_URL")" ]]; then
  fail "Public repo origin URL mismatch. Expected $PAGES_PUBLIC_REPO_URL, found $PUBLIC_REMOTE_URL"
fi

if [[ -n "$(git -C "$PUBLIC_REPO_DIR" status --porcelain)" ]]; then
  fail "Public repo working tree must be clean before publishing: $PUBLIC_REPO_DIR"
fi

if [[ "$DRY_RUN" -eq 0 ]]; then
  git -C "$PUBLIC_REPO_DIR" fetch origin "$PAGES_PUBLIC_BRANCH"
  ensure_branch_checked_out "$PUBLIC_REPO_DIR" "$PAGES_PUBLIC_BRANCH"
  git -C "$PUBLIC_REPO_DIR" pull --ff-only origin "$PAGES_PUBLIC_BRANCH"
  git -C "$PUBLIC_REPO_DIR" push --dry-run origin "HEAD:$PAGES_PUBLIC_BRANCH" >/dev/null
fi

echo "Publishing demo '$DEMO_ID' ($DEMO_TITLE)"
echo "Project: $DEMO_PROJECT_PATH"
echo "Public target: ${PAGES_PUBLIC_REPO_URL} [branch: ${PAGES_PUBLIC_BRANCH}] -> /${DEMO_DEST_REL}/"
[[ "$DRY_RUN" -eq 1 ]] && echo "Mode: DRY RUN"

WORK_DIR="$(mktemp -d "/tmp/mirage-gh-pages-${DEMO_ID}.XXXXXX")"
trap 'rm -rf "$WORK_DIR"' EXIT

PROJECT_DIR="$(cd "$(dirname "$PROJECT_ABS_PATH")" && pwd)"
BUILD_OUTPUT_ROOT="$PROJECT_DIR/bin/Release/net10.0-browser"
PROJECT_WWWROOT="$PROJECT_DIR/wwwroot"

run_demo_build() {
  local restore_arg="$1"
  local args=(dotnet build "$DEMO_PROJECT_PATH" -c Release -r browser-wasm -p:WasmBuildAppBundle=true)

  if [[ -n "$restore_arg" ]]; then
    args+=("$restore_arg")
  fi

  (
    cd "$REPO_ROOT"
    "${args[@]}"
  )
}

if ! run_demo_build "--no-restore"; then
  echo "Build with --no-restore failed, retrying with restore..."
  run_demo_build ""
fi

PUBLISH_WWWROOT="$BUILD_OUTPUT_ROOT/wwwroot"
[[ -f "$PUBLISH_WWWROOT/index.html" ]] || fail "Publish output missing index.html: $PUBLISH_WWWROOT"
[[ -d "$PROJECT_WWWROOT" ]] || fail "Project wwwroot missing: $PROJECT_WWWROOT"

STAGED_SITE_ROOT="$WORK_DIR/site-root"
STAGED_DEMO_DIR="$STAGED_SITE_ROOT/$DEMO_DEST_REL"
mkdir -p "$STAGED_DEMO_DIR"

# Preserve project-owned static assets (including Mirage manifests/assets), then
# overlay the generated browser bundle from the build output.
rsync -a --delete "$PROJECT_WWWROOT"/ "$STAGED_DEMO_DIR"/
rsync -a "$PUBLISH_WWWROOT"/ "$STAGED_DEMO_DIR"/

if [[ -f "$PROJECT_WWWROOT/_mirage/assets/images.manifest.v1.json" && ! -f "$STAGED_DEMO_DIR/_mirage/assets/images.manifest.v1.json" ]]; then
  fail "Staged demo is missing Mirage image manifest after merge: $STAGED_DEMO_DIR/_mirage/assets/images.manifest.v1.json"
fi

normalize_demo_index_html "$STAGED_DEMO_DIR/index.html" "$DEMO_BASE_HREF"

SOURCE_REPO_URL="$(git -C "$REPO_ROOT" remote get-url origin)"
SOURCE_COMMIT="$(git -C "$REPO_ROOT" rev-parse HEAD)"
GENERATED_AT_UTC="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
PUBLISH_CACHE_STAMP="$(git -C "$REPO_ROOT" rev-parse --short=12 HEAD)"
PUBLISH_CACHE_STAMP="${PUBLISH_CACHE_STAMP}-$(date -u +"%Y%m%d%H%M%S")"

stamp_demo_entrypoints "$STAGED_DEMO_DIR" "$PUBLISH_CACHE_STAMP"

jq -n \
  --arg id "$DEMO_ID" \
  --arg title "$DEMO_TITLE" \
  --arg description "$DEMO_DESCRIPTION" \
  --arg publicPath "$DEMO_PUBLIC_PATH" \
  --arg sourceRepository "$SOURCE_REPO_URL" \
  --arg sourceCommit "$SOURCE_COMMIT" \
  --arg generatedAtUtc "$GENERATED_AT_UTC" \
  --arg baseHref "$DEMO_BASE_HREF" \
  '{
    id: $id,
    title: $title,
    description: $description,
    publicPath: $publicPath,
    baseHref: $baseHref,
    sourceRepository: $sourceRepository,
    sourceCommit: $sourceCommit,
    generatedAtUtc: $generatedAtUtc
  }' >"$STAGED_DEMO_DIR/demo.json"

cp "$REPO_ROOT/docs/THIRD_PARTY_NOTICES.md" "$STAGED_SITE_ROOT/THIRD_PARTY_NOTICES.md"
cp "$REPO_ROOT/docs/THIRD_PARTY_NOTICES.md" "$STAGED_DEMO_DIR/THIRD_PARTY_NOTICES.md"

declare -a demo_metadata_files=()
if [[ -d "$PUBLIC_REPO_DIR/demos" ]]; then
  while IFS= read -r -d '' file_path; do
    demo_metadata_files+=("$file_path")
  done < <(find "$PUBLIC_REPO_DIR/demos" -mindepth 2 -maxdepth 2 -name demo.json -print0)
fi
demo_metadata_files+=("$STAGED_DEMO_DIR/demo.json")

MERGED_DEMOS_JSON="$WORK_DIR/merged-demos.json"
jq -s 'reduce .[] as $item ({}; .[$item.id] = $item) | [.[]] | sort_by(.title)' \
  "${demo_metadata_files[@]}" >"$MERGED_DEMOS_JSON"

write_root_index_html "$STAGED_SITE_ROOT/index.html" "$PAGES_SITE_BASE_PATH" "$MERGED_DEMOS_JSON"
write_root_404_html "$STAGED_SITE_ROOT/404.html" "$PAGES_SITE_BASE_PATH"
touch "$STAGED_SITE_ROOT/.nojekyll"

if [[ "$DRY_RUN" -eq 1 ]]; then
  echo
  echo "Dry-run summary:"
  echo "  Demo staging: $STAGED_DEMO_DIR"
  echo "  Base href:    $DEMO_BASE_HREF"
  echo
  echo "Planned demo sync changes:"
  if [[ -d "$PUBLIC_REPO_DIR/$DEMO_DEST_REL" ]]; then
    rsync -ani --delete "$STAGED_DEMO_DIR"/ "$PUBLIC_REPO_DIR/$DEMO_DEST_REL"/ || true
  else
    echo "  (new demo directory)"
    find "$STAGED_DEMO_DIR" -type f | sed "s#^$STAGED_DEMO_DIR/#  + /$DEMO_DEST_REL/#"
  fi
  echo
  for root_file in index.html 404.html THIRD_PARTY_NOTICES.md .nojekyll; do
    target_path="$PUBLIC_REPO_DIR/$root_file"
    staged_path="$STAGED_SITE_ROOT/$root_file"
    echo "Planned root file update: $root_file"
    if [[ -f "$target_path" ]]; then
      diff -u "$target_path" "$staged_path" || true
    else
      echo "  (new file)"
    fi
  done
  echo
  echo "Dry run completed without mutating $PUBLIC_REPO_DIR"
  exit 0
fi

mkdir -p "$PUBLIC_REPO_DIR/demos"
rsync -a --delete "$STAGED_DEMO_DIR"/ "$PUBLIC_REPO_DIR/$DEMO_DEST_REL"/
cp "$STAGED_SITE_ROOT/index.html" "$PUBLIC_REPO_DIR/index.html"
cp "$STAGED_SITE_ROOT/404.html" "$PUBLIC_REPO_DIR/404.html"
cp "$STAGED_SITE_ROOT/THIRD_PARTY_NOTICES.md" "$PUBLIC_REPO_DIR/THIRD_PARTY_NOTICES.md"
cp "$STAGED_SITE_ROOT/.nojekyll" "$PUBLIC_REPO_DIR/.nojekyll"

git -C "$PUBLIC_REPO_DIR" add \
  "$DEMO_DEST_REL" \
  index.html \
  404.html \
  .nojekyll \
  THIRD_PARTY_NOTICES.md

if git -C "$PUBLIC_REPO_DIR" diff --cached --quiet; then
  echo "No publish changes detected."
  exit 0
fi

COMMIT_SHORT="${SOURCE_COMMIT:0:12}"
git -C "$PUBLIC_REPO_DIR" commit -m "Publish demo: ${DEMO_ID} (${COMMIT_SHORT})"
git -C "$PUBLIC_REPO_DIR" push origin "$PAGES_PUBLIC_BRANCH"

echo
echo "Published ${DEMO_ID} to ${PAGES_PUBLIC_REPO_URL} (${PAGES_PUBLIC_BRANCH})"
echo "Demo URL: ${PAGES_SITE_BASE_PATH%/}/${DEMO_DEST_REL}/"
