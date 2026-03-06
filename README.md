# MAUI -> WASM (DOM) Prototype

This repository is an experiment for running .NET MAUI on `net10.0-browser` with the DOM as the platform UI:

MAUI virtual views -> MAUI handlers -> DOM elements + CSS + JS interop.

Current state:
- App startup supports real MAUI `Application -> Window` bootstrap (`BrowserAppHost.RunAsync(MauiApp, ...)`).
- Root-view bootstrap (`BrowserAppHost.RunAsync(MauiApp, IView, ...)`) remains as a compatibility path.
- DOM platform view wrapper is `DomElement`; JSImport entrypoints are centralized in `DomApi`.
- Implemented handlers: `Window`, `NavigationPage`, `ContentPage`, `ScrollView`, `Label`, `Button`, `Entry`, `Switch`, `CheckBox`, `Slider`, `Picker`, `Layout`, `ContentView`.
- Shared `IView`/`ITransform` mapping core is applied across handlers: automation id, flow direction, basic semantics (ARIA), interaction state (`IsEnabled` + `InputTransparent`), background (solid), z-index, focus/unfocus, and transform composition.
- Sample app demonstrates a real `.xaml` `ContentPage` (with code-behind) and covers binding, input, click, resize, visibility, opacity, form-input breadth, IView core/transform behavior, navigation push/pop, scroll behavior, and stress cleanup.

## Repo layout

- `src/Maui.Browser/`
  Browser backend: DOM interop, handlers, hosting, measurement.
- `src/Maui.Browser.Sample/`
  `net10.0-browser` host app mounted under `#app`.
- `tests/Maui.Browser.E2E.Tests/`
  Playwright smoke and stress validation.
- `docs/DECISIONS.md`
  Architecture/implementation decision log.

## Prerequisites

1. .NET SDK `10.0.102` (or compatible .NET 10 SDK).
2. Required workloads:
   - `dotnet workload install maui wasm-tools`
3. Playwright browser runtime for E2E tests:
   - `pwsh tests/Maui.Browser.E2E.Tests/bin/Release/net10.0/playwright.ps1 install --with-deps chromium`

## Build and run sample

1. Build:
   - `dotnet build MauiDomWasm.sln`
2. Run sample:
   - `dotnet run --project src/Maui.Browser.Sample/Maui.Browser.Sample.csproj`
3. Open the launched app URL (default launch profile uses `http://localhost:5057`).

Important:
- Do not execute `src/Maui.Browser.Sample/bin/.../Maui.Browser.Sample` directly. That runs as a native host process and `JSImport` throws `PlatformNotSupportedException`.
- If `WasmAppHost` reports `no perHostConfigs found`, run:
  - `dotnet clean src/Maui.Browser.Sample/Maui.Browser.Sample.csproj`
  - `dotnet build src/Maui.Browser.Sample/Maui.Browser.Sample.csproj -t:Rebuild`

## Automated smoke checks

1. Build release artifacts:
   - `dotnet build MauiDomWasm.sln --configuration Release`
2. Install Playwright browser:
   - `pwsh tests/Maui.Browser.E2E.Tests/bin/Release/net10.0/playwright.ps1 install --with-deps chromium`
3. Run smoke tests:
   - `dotnet test tests/Maui.Browser.E2E.Tests/Maui.Browser.E2E.Tests.csproj --configuration Release --no-build`

Expected pass signals:
- All E2E tests pass.
- Entry -> label binding works.
- Button click updates count.
- Switch and CheckBox toggles update bound summaries.
- Slider movement updates bound numeric summary live.
- Picker selection changes update bound index/text summary.
- Programmatic form-state updates reflect back into control visuals.
- Resize updates viewport text.
- Visibility/opacity transitions apply correctly.
- StackLayout semantic projection checks pass (`VerticalStackLayout`/`HorizontalStackLayout` compute to `display:flex` with expected direction).
- Grid semantic projection checks pass (`Grid` computes to `display:grid` with expected track, gap, and placement styles).
- IView core checks pass: automation ids, flow direction, semantics ARIA updates, focus/unfocus, input transparency policy, transform and background updates.
- Navigation/scroll checks pass: root page pushes/pops detail page through `NavigationPage`, title/back controls update, and detail `ScrollView` is scrollable with state retained when returning.
- Navigation push/pop loop keeps DOM handle/subscription counts near baseline.
- Stress mutation test keeps DOM handle/subscription counts near baseline.

## Manual runtime checklist

- Page renders under `#app`.
- Typing into `Entry` updates the live text label.
- Clicking increment button updates click summary.
- Resizing browser updates viewport/layout status text.
- Toggling visibility moves probe through `visible <-> collapsed`.
- Cycling opacity updates probe opacity summary.
- Toggling `Switch` and `CheckBox` updates their summary labels.
- Moving `Slider` updates slider summary value.
- Changing `Picker` selection updates index/text summary.
- Clicking `Apply programmatic form update` updates all form controls from state.
- Toggling core flow direction updates container direction (`ltr`/`rtl`).
- Toggling core input transparency disables probe interaction without disabling the control.
- Focus/unfocus buttons move focus to/from the core entry.
- Transform/background/semantics actions update the probe style and ARIA attributes.
- Root vertical and key horizontal stack rows render with semantic flex styles.
- Grid probe section renders as CSS grid with expected row/column placement and span behavior.
- AbsoluteLayout probe remains on absolute-compatible projection (non-grid fallback path).
- Navigation section pushes to detail page and back without reloading the app.
- Detail page `ScrollView` allows native browser scrolling and returns to root with updated navigation summary state.
- Running leak stress increments stress run count and remains interactive.

## Prototype proof checklist

- Lifecycle proof: app boots from `UseMauiApp<App>()` via `Application -> Window` host path.
- Handler proof: controls/layout/page/window mapping stays in `Maui.Browser` handlers.
- Automated proof: Playwright smoke runs in CI alongside solution build.
- Leak-resistance proof: stress scenario validates handle/subscription cleanup near baseline.

## Notes

- Layout now uses semantic projection for `VerticalStackLayout`/`HorizontalStackLayout` (flex) and `Grid` (CSS grid) with automatic fallback to absolute projection for unresolved layouts.
- Text measurement is currently canvas `measureText` based.
- Font-load synchronization remains deferred.
- `Switch` now renders as a dedicated toggle (`button[role=switch]` + track/thumb) with intrinsic hit width, so it stays visually distinct from `CheckBox` and does not stretch across the arranged row.
- `CheckBox`/`Slider` color parity remains best-effort browser styling (`accent-color` plus slider background gradient); exact native visual parity is intentionally deferred.
- `NavigationPage` is now projected with a minimal DOM navigation bar (`Back` + title) and a single active page host; Shell is intentionally out of scope.
- `ScrollView` uses native browser overflow scrolling and supports programmatic `RequestScrollTo` mapping.
- `Clip` and `Shadow` are intentionally deferred in this slice: current behavior is no-op with DEBUG diagnostic warnings.
- Grid track mapping is currently: absolute -> `px`, auto -> `auto`, star -> `fr` (non-positive star defaults to `1fr`); invalid grid metadata falls back to absolute projection.

See `AGENTS.md` for repository guardrails.
