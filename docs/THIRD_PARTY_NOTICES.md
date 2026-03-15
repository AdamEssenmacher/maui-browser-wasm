# Third-Party Notices

## dotnet/maui Controls.Sample

Portions of this repository vendor source from:

- Repository: https://github.com/dotnet/maui
- Subtree path: `src/Controls/samples/Controls.Sample`
- Local path: `vendor/upstream/controls-sample`

Licensing:

- Upstream project license: MIT
- Local copy of upstream license: `vendor/upstream/controls-sample/LICENSE.upstream-MIT.txt`
- Upstream third-party notices snapshot: `vendor/upstream/controls-sample/THIRD-PARTY-NOTICES.upstream.txt`

Mirage wrappers around the vendored sample live in:

- `samples/ControlsSample.Shared`
- `samples/ControlsSample.Fluent`
- `samples/ControlsSample.Maui`

Compliance workflow:

- Keep upstream file headers and notices intact in vendored source.
- Run `tools/vendor/sync-controls-sample.sh` for updates so license artifacts are refreshed automatically.
- The sync script now fails if it cannot find upstream license or third-party notices files.
- Do not ship/remove the wrapper integration without keeping:
  - `vendor/upstream/controls-sample/LICENSE.upstream-MIT.txt`
  - `vendor/upstream/controls-sample/THIRD-PARTY-NOTICES.upstream.txt`
