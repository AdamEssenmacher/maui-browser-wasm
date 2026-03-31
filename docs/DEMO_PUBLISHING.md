# Demo Publishing (GitHub Pages)

This repo is private, so browser demo hosting is published to the public [`MirageUI`](https://github.com/AdamEssenmacher/MirageUI) repository on branch `demo-pages`.

## One-time setup

1. Clone the public repo locally (example path used by default script behavior):
   - `../MirageUI`
2. In the public repo settings, configure GitHub Pages:
   - Source: `Deploy from a branch`
   - Branch: `demo-pages`
   - Folder: `/ (root)`
3. Ensure your local public repo checkout has a clean working tree before publishing.

## Publish command

Use the private repo script:

```bash
tools/publish/publish-gh-pages-demo.sh \
  --demo controls-sample \
  --public-repo-dir ../MirageUI
```

Dry run (recommended first):

```bash
tools/publish/publish-gh-pages-demo.sh \
  --demo controls-sample \
  --public-repo-dir ../MirageUI \
  --dry-run
```

## Output contract in public repo

- `demos/<demo-id>/...` demo payload (WASM app static files)
- `demos/<demo-id>/demo.json` metadata (title/path/source commit)
- `index.html` generated demo landing page
- `404.html` generated fallback redirect
- `.nojekyll`
- `THIRD_PARTY_NOTICES.md`

For ControlsSample, the expected URL is:

- `https://adamessenmacher.github.io/MirageUI/demos/controls-sample/`

## Adding more demos

Edit `tools/publish/demos.manifest.json` in this repo and add another `demos[]` entry:

- `id` (CLI key used by `--demo`)
- `title`
- `description`
- `projectPath` (browser host `.csproj`)
- `publicPath` (folder name under `/demos/`)

Then publish the new demo:

```bash
tools/publish/publish-gh-pages-demo.sh --demo <new-id> --public-repo-dir ../MirageUI
```

The root landing page is regenerated from deployed `demo.json` files plus the currently staged demo.
