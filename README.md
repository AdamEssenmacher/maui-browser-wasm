# Mirage

Mirage is an experimental **MAUI handler-backed browser platform** for `net10.0-browser`.

It renders MAUI UI through a provider-backed DOM realization pipeline:

`MAUI virtual view -> Mirage MAUI handlers -> Mirage platform tree (control + web) -> Mirage diff -> Mirage runtime + provider descriptors -> DOM/custom elements`

Mirage currently uses providers for built-in control realization metadata plus a raw custom-element hosting seam for `WebNode`.
`Mirage.Provider.Fluent` is the first polished reference provider for the built-in control-node path.

## Phase 1 scope (locked)

Supported semantic kinds:
- `Root`
- `ContentContainer`
- `VerticalStack`
- `HorizontalStack`
- `Grid`
- `Map`
- `Label`
- `Entry`
- `Button`

Built-in handlers stay inside this control set. Third-party/custom handlers can emit `WebNode` for generic web realization of arbitrary DOM/Web Components.

## Active project layout

- `src/Mirage.Core`
  - Hybrid platform node model (`ControlNode` + `WebNode`)
  - Typed value model (`MirageValue`)
  - Diff engine
  - Provider contracts
- `src/Mirage.Web`
  - Browser runtime
  - JSImport DOM bridge
  - Hybrid patch applier/event bridge
- `src/Mirage.Maui`
  - MAUI browser handlers + host APIs
  - `UseMirageBrowserPlatform()` / `UseMirageComponentProvider<T>()`
  - `MirageBrowserAppHost.RunAsync(...)`
- `src/Mirage.Provider.Fluent`
  - Fluent UI Web Components descriptor provider for phase 1 built-in control-node realization
- `src/Mirage.Provider.LeafletMaps`
  - Browser map provider implementation (Leaflet) for built-in `Map` control semantics
- `samples/Counter.Shared`
  - Host-agnostic shared counter UI/state (`CounterPage`, XAML, and bindings)
- `samples/Counter.Fluent`
  - Mirage browser host for the shared counter app (`Counter.Shared`)
- `samples/Counter.Maui`
  - Native MAUI host for the shared counter app (`Counter.Shared`)
- `samples/Showcase.Shared`
  - Host-agnostic shared showcase UI/state/resources (`ShowcaseHeroPage`, styles)
- `samples/Showcase.Fluent`
  - Mirage browser host for the shared showcase app (`Showcase.Shared`)
  - Registers `UseMirageNavigation(...)` route projection (`/`, `/layout-lab`, `/xaml-lab`, `/navigation-lab`, `/map-lab`, `/projects/{id}`) while keeping `NavigationPage` stack semantics as source of truth
  - Registers `UseMirageLeafletMaps(...)` and includes built-in `Map` proof route (`/map-lab`) using ControlNode map semantics (not WebNode fallback)
  - Includes an exhaustive dedicated `Layout Lab` page validating sizing, constraints, positioning, and nested container behavior across `ContentView`, stacks, and `Grid`
  - Includes a constrained `XAML Lab` page (`/xaml-lab`) with a WebNode-hosted Monaco editor and debounced fragment preview rendered through the normal Mirage MAUI handler pipeline
  - Uses explicit `ScrollView` hosting for long-page scrolling; `ContentPage` remains non-scrollable by default
  - Demonstrates MAUI ResourceDictionary-first styling with near-zero host CSS (`index.html` reduced to app-shell sizing reset only)
- `samples/Showcase.Maui`
  - Native MAUI host for the shared showcase app (`Showcase.Shared`)
- `tests/Mirage.Core.Tests`
  - Diff correctness + determinism
- `tests/Mirage.Integration.Tests`
  - Provider validation + provider metadata coverage
- `tests/Mirage.E2E.Tests`
  - Browser smoke for `Counter.Fluent` and `Showcase.Fluent`

## Prerequisites

1. .NET SDK `10.0.102` or compatible .NET 10 SDK.
2. Workloads:
   - `dotnet workload install maui wasm-tools`
3. Playwright browser runtime (for E2E):
   - `pwsh tests/Mirage.E2E.Tests/bin/Release/net10.0/playwright.ps1 install --with-deps chromium`

## Build

```bash
dotnet restore Mirage.sln
dotnet build Mirage.sln
```

## Run samples

```bash
dotnet run --project samples/Counter.Fluent/Counter.Fluent.csproj
dotnet run --project samples/Showcase.Fluent/Showcase.Fluent.csproj
dotnet run --project samples/ControlsSample.Fluent/ControlsSample.Fluent.csproj
```

Native MAUI hosts are intentionally **outside** `Mirage.sln`/CI but are runnable directly:

```bash
dotnet build samples/Counter.Maui/Counter.Maui.csproj
dotnet build samples/Showcase.Maui/Showcase.Maui.csproj
dotnet build samples/ControlsSample.Maui/ControlsSample.Maui.csproj
```

`ControlsSample.Maui` includes Android + iOS + MacCatalyst targets by default on non-Linux hosts.
If your local Apple toolchain is not ready, build Android-only with:

```bash
dotnet build samples/ControlsSample.Maui/ControlsSample.Maui.csproj -p:ControlsSampleEnableAppleTargets=false
```

## Run tests

```bash
dotnet test tests/Mirage.Core.Tests/Mirage.Core.Tests.csproj
dotnet test tests/Mirage.Integration.Tests/Mirage.Integration.Tests.csproj
dotnet test tests/Mirage.E2E.Tests/Mirage.E2E.Tests.csproj
```

## Performance diagnostics and budgets

Render coalescing/scoped-materialization/input-latency diagnostics are built into Mirage when `MIRAGE_DIAGNOSTICS=1`.

Run the slowness matrix locally with balanced perf budgets:

```bash
MIRAGE_MATRIX_RUNS=1 \
MIRAGE_PERF_BUDGET_FILE=tools/diagnostics/perf-budgets-balanced.cfg \
tools/diagnostics/run-slowness-matrix.sh
```

The matrix writes machine-readable summaries under a timestamped output directory and exits non-zero when the configured hot-reload-off budget profile fails.

## Runtime invariant

Mirage startup requires `IMirageComponentProvider` registration.

If missing, startup fails with:

`MirageRuntime requires IMirageComponentProvider. Register one with UseMirageComponentProvider<TProvider>().`

## Map Support (Provider Model)

- `Map` is a built-in Mirage control-node path (`ComponentKind.Map`), not a generic `WebNode`.
- Mirage core semantics are region-first (`center + span`) with MAUI-shaped map options (`MapType`, `IsShowingUser`, `IsScrollEnabled`, `IsZoomEnabled`, `IsTrafficEnabled`), identity-based pins, and identity-based map elements (`Circle`/`Polyline`/`Polygon`).
- Browser map realization is provider-owned through `IMirageMapProvider`/`IMirageMapAdapter`; Mirage core/runtime stays provider-neutral.
- First browser map provider is Leaflet (`UseMirageLeafletMaps(...)`) in `src/Mirage.Provider.LeafletMaps`.
- Leaflet asset policy defaults to CDN JS/CSS URLs (configurable), with explicit initialization diagnostics and visible error state on provider/asset failures.
- `LeafletAssetMode.Local` requires explicit local JS/CSS URLs; Mirage fails fast with diagnostics when local mode is enabled without local URLs.
- Leaflet default tile source is OpenStreetMap with attribution as a provider default policy; tile URL/attribution are configurable and not Mirage core semantics.
- Runtime parity contract and CI guard live at `docs/parity/map-parity-contract.json` and `tests/Mirage.Integration.Tests/MapParityContractTests.cs`.
- Explicit non-parity/constraint entries are tracked in `docs/parity/map-parity-divergences.json` with test references.
- Upstream MAUI shared-code risks for unofficial/browser targets are tracked separately in `docs/UPSTREAM_TARGET_RISKS.md`.

## Notes

- `Mirage.Web` is provider-neutral browser infrastructure.
- MAUI framework/logical semantics remain MAUI-side; Mirage models platform realization only.
- `VisualNodeBase` is a shared platform-side visual/layout projection layer, not a duplicate MAUI framework model.
- Provider-specific mappings (Fluent, UI5, custom) live only in provider projects.
- Current provider abstraction is intentionally narrow: deep portability across arbitrary web-component libraries is deferred until Mirage grows slots/child projection, structured payloads, overlay hosting, provider-owned lifecycle hooks, and shadow/part-aware targeting.
- Navigation is `NavigationPage`-first: MAUI stack changes are the source of truth and Mirage projects them into browser URL/history through `UseMirageNavigation(...)`.
- `NavigationPage` is realized as a built-in navigation shell (nav bar, back button, title slot, toolbar slot, bounded content host) on the control-node path.
- Dependency direction for samples is locked: `*.Fluent -> *.Shared` and `*.Maui -> *.Shared`.
- Fluent UI Web Components are built on FAST foundations, but Mirage’s public reference-provider story is Fluent-first.
- Providers must support control-node and web-node realization for full Mirage behavior.
- Mirage preserves MAUI’s explicit scrolling model: `ContentPage` is non-scrollable by default and scrolling is introduced explicitly with `ScrollView`.
- Shared sample projects (`Counter.Shared`, `Showcase.Shared`) are included in `Mirage.sln`; native MAUI host projects are not.
- `ControlsSample` wrappers (`ControlsSample.Shared`, `ControlsSample.Fluent`, `ControlsSample.Maui`) provide a subtree-synced integration lane for upstream `dotnet/maui` Controls.Sample.
- Mirage browser hosts automatically collect bundled `MauiImage` assets (including transitive `ProjectReference` projects), publish them to `/_mirage/assets/...`, and resolve MAUI image source strings through a shared runtime resolver + manifest (`/_mirage/assets/images.manifest.v1.json`).
- Bundled-image wave scope is file-based images only (`.png`, `.jpg`, `.jpeg`, `.gif`, `.webp`, `.svg`); stream/font/full MAUI image pipeline parity is explicitly deferred.
- Upstream license compliance for the vendored Controls.Sample lane is tracked in `docs/THIRD_PARTY_NOTICES.md`.
- The old `Maui.Browser*` showcase/parity stack is not part of the active solution wiring.
- Architecture benchmark harness: `tools/benchmarks/Mirage.ArchitectureBenchmarks`.
- Current rendering wave focus is correctness-first (shared visual projection, stack/grid determinism, and DOM-backed surface rendering) before richer control breadth.

## Early-lock architecture guardrails

- Built-in Mirage handlers must continue to emit control nodes.
- `WebNode` is for extensibility (third-party/custom/BYO components), not built-in fallback.
- `Mirage.Web` stays provider-neutral and must not contain provider mappings.
- Web-node realization must preserve requested element identity end-to-end.
- See [Architecture Guardrails](docs/ARCHITECTURE_GUARDRAILS.md) and [Provider Authoring](docs/PROVIDER_AUTHORING.md).
