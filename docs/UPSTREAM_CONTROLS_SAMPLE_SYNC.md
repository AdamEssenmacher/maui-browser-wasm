# Controls.Sample Upstream Sync

Mirage vendors the upstream `.NET MAUI` Controls sample under:

- `vendor/upstream/controls-sample`

This is maintained via `git subtree` using `tools/vendor/sync-controls-sample.sh`.

## One-time setup

```bash
chmod +x tools/vendor/sync-controls-sample.sh
```

## Sync from upstream

```bash
tools/vendor/sync-controls-sample.sh
```

Optional arguments:

```bash
tools/vendor/sync-controls-sample.sh --ref main
tools/vendor/sync-controls-sample.sh --repo https://github.com/dotnet/maui.git
tools/vendor/sync-controls-sample.sh --dry-run
```

## What the script does

1. Clones `dotnet/maui` shallowly to a temporary directory.
2. Extracts `src/Controls/samples/Controls.Sample` into a split snapshot repository.
3. Runs `git subtree add/pull --squash` into `vendor/upstream/controls-sample`.
4. Copies upstream licensing artifacts into the vendor folder (fails hard if upstream license/notice files are missing).
5. Writes sync metadata to `vendor/upstream/controls-sample/.mirage-upstream.json`.

## Host wrappers

- Browser host: `samples/ControlsSample.Fluent`
- Native host: `samples/ControlsSample.Maui`
- Shared wrapper/catalog: `samples/ControlsSample.Shared`

The browser host intentionally filters to Mirage-runnable entries, while the native host includes Mirage-unsupported entries except externally blocked categories.
Bundled images for browser runs are now provided by the shared Mirage bundled-image pipeline (`Directory.Build.targets`) and manifest resolver (`/_mirage/assets/images.manifest.v1.json`), not by ControlsSample-specific `wwwroot` copy targets.

Current curated upstream pages wired in `ControlsSample.Shared`:

- `Pages/Compatibility/TabbedPageGalleryMainPage` (browser runnable)
- `Pages/Layouts/LayoutIsEnabledPage` (native-only for now)
- `Pages/Core/InputTransparentPage` (native-only for now)
