# Mirage

Mirage is an experimental **MAUI handler-backed browser platform** for `net10.0-browser`.

It renders MAUI UI through a provider-based pipeline:

`MAUI virtual view -> Mirage MAUI handlers -> Mirage platform tree (control + web) -> Mirage diff -> Provider -> DOM/Web Components`

Mirage is provider-based and designed to adapt complete web component libraries.
`Mirage.Provider.Fluent` is the first polished reference provider.

## Phase 1 scope (locked)

Supported semantic kinds:
- `Root`
- `ContentContainer`
- `VerticalStack`
- `HorizontalStack`
- `Grid`
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
  - Fluent UI Web Components provider implementation for phase 1
- `samples/Counter.Fluent`
  - Minimal reference app proving label/entry/button/layout loop
- `samples/Showcase.Fluent`
  - Public-facing Mirage hero demo proving real DOM + Fluent UI Web Components rendering
  - Includes focused foundations/layout labs for `Label`, core `IView` mapping, stack behavior, grid placement/spans, and surface rendering
  - Includes an exhaustive `Layout Lab` matrix validating sizing, constraints, positioning, and nested container behavior across `ContentView`, stacks, and `Grid`
  - Uses explicit `ScrollView` hosting for long-page scrolling; `ContentPage` remains non-scrollable by default
  - Demonstrates MAUI ResourceDictionary-first styling with CSS retained only as a targeted browser/provider escape hatch
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
```

## Run tests

```bash
dotnet test tests/Mirage.Core.Tests/Mirage.Core.Tests.csproj
dotnet test tests/Mirage.Integration.Tests/Mirage.Integration.Tests.csproj
dotnet test tests/Mirage.E2E.Tests/Mirage.E2E.Tests.csproj
```

## Runtime invariant

Mirage startup requires `IMirageComponentProvider` registration.

If missing, startup fails with:

`MirageRuntime requires IMirageComponentProvider. Register one with UseMirageComponentProvider<TProvider>().`

## Notes

- `Mirage.Web` is provider-neutral browser infrastructure.
- MAUI framework/logical semantics remain MAUI-side; Mirage models platform realization only.
- `VisualNodeBase` is a shared platform-side visual/layout projection layer, not a duplicate MAUI framework model.
- Provider-specific mappings (Fluent, UI5, custom) live only in provider projects.
- Fluent UI Web Components are built on FAST foundations, but Mirage’s public reference-provider story is Fluent-first.
- Providers must support control-node and web-node realization for full Mirage behavior.
- Mirage preserves MAUI’s explicit scrolling model: `ContentPage` is non-scrollable by default and scrolling is introduced explicitly with `ScrollView`.
- The old `Maui.Browser*` showcase/parity stack is not part of the active solution wiring.
- Architecture benchmark harness: `tools/benchmarks/Mirage.ArchitectureBenchmarks`.
- Current rendering wave focus is correctness-first (shared visual projection, stack/grid determinism, and DOM-backed surface rendering) before richer control breadth.

## Early-lock architecture guardrails

- Built-in Mirage handlers must continue to emit control nodes.
- `WebNode` is for extensibility (third-party/custom/BYO components), not built-in fallback.
- `Mirage.Web` stays provider-neutral and must not contain provider mappings.
- Web-node realization must preserve requested element identity end-to-end.
- See [Architecture Guardrails](docs/ARCHITECTURE_GUARDRAILS.md) and [Provider Authoring](docs/PROVIDER_AUTHORING.md).
