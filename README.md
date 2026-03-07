# MAUI -> WASM (DOM) Prototype

This repository is an experiment for running .NET MAUI on `net10.0-browser` with the DOM as the platform UI:

MAUI virtual views -> MAUI handlers -> DOM elements + CSS + JS interop.

Current state:
- App startup supports real MAUI `Application -> Window` bootstrap (`BrowserAppHost.RunAsync(MauiApp, ...)`).
- Root-view bootstrap (`BrowserAppHost.RunAsync(MauiApp, IView, ...)`) remains as a compatibility path.
- DOM platform view wrapper is `DomElement`; JSImport entrypoints are centralized in `DomApi`.
- Implemented handlers: `Window`, `NavigationPage`, `ContentPage`, `ScrollView`, `CollectionView`, `RefreshView`, `Label`, `Button`, `Entry`, `Editor`, `SearchBar`, `Image`, `ImageButton`, `Border`, `Switch`, `CheckBox`, `Slider`, `Picker`, `DatePicker`, `TimePicker`, `Stepper`, `ProgressBar`, `ActivityIndicator`, `Layout`, `ContentView`.
- Shared `IView`/`ITransform` mapping core is applied across handlers: automation id, flow direction, basic semantics (ARIA), interaction state (`IsEnabled` + `InputTransparent`), background (solid), z-index, focus/unfocus, and transform composition.
- Core view substrate now includes: `Clip` (`RectangleGeometry` + `RoundRectangleGeometry`), `Shadow` (solid paint), and `Background` linear-gradient projection.
- Sample app demonstrates a real `.xaml` `ContentPage` (with code-behind) and covers binding, input, click, resize, visibility, opacity, form-input breadth, IView core/transform behavior, navigation push/pop, scroll behavior, and stress cleanup.
- Sample now starts at the showcase home page (`showcase-page-home`).
- Showcase catalog (`showcase-page-catalog`) provides deep-dive links for regression/proof pages.
- Showcase home now includes a 1:1 control-type index (`showcase-control-list-row-*`) listing every MAUI control/layout type in scope; missing handlers are explicitly marked not implemented yet.
- MAUI styling proof is now demonstrated in the showcase track: implicit/explicit styles, `BasedOn`, `StyleClass`, runtime `DynamicResource` theme updates, and state-specific button/input visuals.

## Repo layout

- `src/Maui.Browser/`
  Browser backend: DOM interop, handlers, hosting, measurement.
- `src/Maui.Browser.Sample/`
  `net10.0-browser` host app mounted under `#app`.
- `tests/Maui.Browser.E2E.Tests/`
  Playwright smoke and stress validation.
- `docs/DECISIONS.md`
  Architecture/implementation decision log.
- `docs/PARITY_MATRIX.md`
  Source-of-truth control/layout parity ledger.
- `docs/PARITY_GAPS.md`
  Explicit fallback/non-parity policy tracking.
- `tools/parity/`
  Inventory and parity-matrix validation helpers.

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
2. Validate parity matrix completeness:
   - `bash tools/parity/check_parity_matrix.sh`
3. Install Playwright browser:
   - `pwsh tests/Maui.Browser.E2E.Tests/bin/Release/net10.0/playwright.ps1 install --with-deps chromium`
4. Run smoke tests:
   - `dotnet test tests/Maui.Browser.E2E.Tests/Maui.Browser.E2E.Tests.csproj --configuration Release --no-build`

Expected pass signals:
- All E2E tests pass.
- Entry -> label binding works.
- Button click updates count.
- Text/Input family page checks pass for `Label`, `Entry`, `Editor`, and `SearchBar`.
- Pickers/Date-Time family page checks pass for `DatePicker`, `TimePicker`, `Stepper`, `ProgressBar`, and `ActivityIndicator`.
- Media/Visual family page checks pass for `Image`, `ImageButton`, and `Border` source/style/interaction probes.
- Switch and CheckBox toggles update bound summaries.
- Slider movement updates bound numeric summary live.
- Picker selection changes update bound index/text summary.
- Programmatic form-state updates reflect back into control visuals.
- CollectionView family checks pass for selection (`None`/`Single`/`Multiple`), live item mutations (`Add`/`Remove`/`Move`/`Replace`/`Reset`), header/footer rendering, and `ScrollTo` behavior.
- Collection/Templated family checks pass for `RefreshView` command/state integration plus pull-gesture activation (`IsRefreshing`, `IsRefreshEnabled`, trigger/pull gating, completion path) while keeping CollectionView interactions working inside refresh content.
- Clearing collection data shows EmptyView; reset restores item rows.
- Resize updates viewport text.
- Visibility/opacity transitions apply correctly.
- StackLayout semantic projection checks pass (`VerticalStackLayout`/`HorizontalStackLayout` compute to `display:flex` with expected direction).
- Grid semantic projection checks pass (`Grid` computes to `display:grid` with expected track, gap, and placement styles).
- IView core checks pass: automation ids, flow direction, semantics ARIA updates, focus/unfocus, input transparency policy, transform and background updates.
- Containers/Layout family page verifies core substrate clip/shadow/linear-gradient style projection and unsupported fallback behavior.
- Containers/Layout family page verifies FlexLayout semantic projection (`display:flex` + direction/wrap/justify/align + item attached style mappings).
- Navigation/scroll checks pass: root page pushes/pops detail page through `NavigationPage`, title/back controls update, and detail `ScrollView` is scrollable with state retained when returning.
- Navigation push/pop loop keeps DOM handle/subscription counts near baseline.
- Stress mutation test keeps DOM handle/subscription counts near baseline.
- Showcase track checks pass: catalog navigation to all showcase pages, theme toggle updates live styles without full reload, and style-lab probes validate implicit/explicit/class-based styling plus button state styling.

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
- CollectionView renders templated rows from bound data and updates selection summaries from row clicks/programmatic selection.
- Collection action buttons add/remove/move/replace/clear/reset items and update summary text.
- Collection selection mode toggle cycles through `None`, `Single`, and `Multiple` with deterministic selected-row markers.
- Refresh trigger row or pull-down gesture on Collection/Templated family page sets `IsRefreshing=true`, increments refresh command count once per refresh cycle, ignores re-entry while refreshing, and respects refresh-enabled toggles.
- Collection header/footer mode toggle swaps between template-backed and object-view paths.
- Collection scroll buttons trigger deterministic `ScrollTo` (`Start`/`Center`/`End`/`MakeVisible`) behavior.
- Clearing collection data shows the empty view; reset brings rows back.
- Toggling core flow direction updates container direction (`ltr`/`rtl`).
- Toggling core input transparency disables probe interaction without disabling the control.
- Focus/unfocus buttons move focus to/from the core entry.
- Transform/background/semantics actions update the probe style and ARIA attributes.
- In Containers/Layout family page, clip mode buttons toggle `clip-path` (rectangle/round-rectangle) and unsupported geometry fallback.
- In Containers/Layout family page, shadow mode buttons toggle `box-shadow` (solid/unsupported fallback) and background mode toggles solid/linear/unsupported radial fallback.
- In Containers/Layout family page, flex toggles update direction/wrap/justify/align modes and item preset maps order/grow/shrink/basis/align-self styles.
- In Text/Input family page, entry/editor/search probes update summaries and programmatic state changes reflect back into controls.
- In Pickers/Date-Time family page, date/time and stepper/progress/activity probes update summaries and reflect programmatic updates.
- In Media/Visual family page, image source cycling (`uri`/`file`/`font`), aspect toggles, image-button counters, and border mode toggles all update deterministically.
- Root vertical and key horizontal stack rows render with semantic flex styles.
- Grid probe section renders as CSS grid with expected row/column placement and span behavior.
- AbsoluteLayout probe remains on absolute-compatible projection (non-grid fallback path).
- Navigation section pushes to detail page and back without reloading the app.
- Detail page `ScrollView` allows native browser scrolling and returns to root with updated navigation summary state.
- Running leak stress increments stress run count and remains interactive.
- Initial app route is `Showcase Home`; use `Open showcase catalog` for deep-dive pages, including `Control Lab`.

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
- `CollectionView` currently supports vertical, non-virtualized parity depth with `DataTemplate` item rendering, `EmptyView`, selection (`None`/`Single`/`Multiple`), live `Add`/`Remove`/`Move`/`Replace`/`Reset` updates, header/footer, and `ScrollTo`.
- `RefreshView` currently supports command/state-first parity plus pull-down threshold activation with explicit indicator UI and full state/command mapping (`Content`, `IsRefreshing`, `IsRefreshEnabled`, `RefreshColor`, `Command`, `CommandParameter`); native overscroll elasticity visuals remain deferred.
- `Image` currently supports `UriImageSource`, `FileImageSource`, and `FontImageSource`; unsupported source types (for example stream-based sources) use deterministic fallback with diagnostics.
- `Border` currently supports solid stroke paint with rectangle/round-rectangle mapping and dashed best-effort style projection; advanced shapes/paints are deferred.
- Clip coverage currently supports `RectangleGeometry` and `RoundRectangleGeometry`; other clip geometries remain fallback with DEBUG diagnostics.
- Shadow coverage currently supports solid paint only; non-solid shadow paints remain fallback with DEBUG diagnostics.
- Background coverage currently supports solid and linear gradients; radial/image/pattern paints remain fallback with DEBUG diagnostics.
- Grid track mapping is currently: absolute -> `px`, auto -> `auto`, star -> `fr` (non-positive star defaults to `1fr`); invalid grid metadata falls back to absolute projection.
- FlexLayout semantic mapping currently covers container direction/wrap/justify/align and item order/grow/shrink/basis/align-self; `FlexLayout.Position` and advanced mixed absolute/relative semantics remain deferred.
- `CollectionView` still defers virtualization, grouping, sticky-header behavior, and advanced large-data scroll anchoring semantics.
- Full parity rollout tracking now lives in `docs/PARITY_MATRIX.md` and `docs/PARITY_GAPS.md`.

See `AGENTS.md` for repository guardrails.
