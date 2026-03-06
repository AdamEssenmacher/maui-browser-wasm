# Decisions Log

Keep this short. Append new entries with date and a 2–5 sentence rationale.

## 2026-03-04
- DOM is the platform UI for the Browser backend. No canvas renderer.
- No MAUI fork initially; build an external Browser platform package using handlers + DI.
- Host remains a plain `net10.0-browser` app (no Razor required).

## 2026-03-04 (Vertical Slice 1)
- Bootstrap choice: root-view-first host (`BrowserAppHost.RunAsync(MauiApp, IView, ...)`) instead of full MAUI `Application -> Window` wiring in this slice.
- Dependency alignment: explicit MAUI NuGet package references (`Microsoft.Maui.Core` + `Microsoft.Maui.Controls` pinned via `MauiPackageVersion`) because `UseMaui` on `net10.0-browser` did not reliably provide compile-time references in this setup.
- Layout projection: apply MAUI-arranged frames directly to DOM via `position:absolute` + `left/top/width/height` for deterministic correctness first.
- Measurement scope: minimal `BrowserTextMeasurer` backed by canvas `measureText`; deferred full async font readiness pipeline to a later slice.
- Interop contract: keep `DomApi` signatures on primitive `nint` handles and maintain a JS-side handle registry (`number -> Element`) in `dom.js` rather than passing JS object references across the JSImport boundary.
- Dispatcher bootstrap: install a Browser `IDispatcherProvider` in `UseMauiBrowser()` so MAUI bindings and `BindableObject.Dispatcher` can operate without a native windowing platform.

## 2026-03-04 (Vertical Slice 2)
- Bootstrap moved to `Application -> Window` first (`BrowserAppHost.RunAsync(MauiApp, ...)`) with the root-view API retained as a compatibility overload to avoid breaking existing slice-1 callers.
- Browser window lifecycle is encapsulated behind `IBrowserWindowHost` so attach/detach/resize concerns are explicit and testable.
- Added dedicated `Window` + `ContentPage` handlers as minimal DOM container projections, keeping all platform mapping in `Maui.Browser` rather than sample composition.
- Added Playwright-based E2E smoke + stress checks as proof gates, and introduced debug diagnostics (`getDiagnostics`) in DOM interop to validate that dynamic add/remove operations return near baseline handle/subscription counts.
- Multi-window support is intentionally deferred; current Browser host explicitly tracks and uses a single active window.
- MAUI Controls in this slice still expose `IsVisible` flow for probe interactions (collapsed semantics); explicit `Visibility.Hidden` authoring from controls is deferred and remains a known gap for a later slice.
- Sample app now includes a real `.xaml` `ContentPage` rendered through Browser handlers; for this prototype the page is loaded from embedded XAML via `LoadFromXaml(typeof(...))` to keep startup deterministic in the `net10.0-browser` host without introducing additional platform tooling seams.
- `Window.Page` remaps now trigger an immediate measure/arrange pass using the last known window frame (or current DOM client size fallback) so runtime root-page swaps do not wait for browser resize to become visible.

## 2026-03-05 (Vertical Slice 3)
- Form-input breadth is the next proof step after lifecycle bootstrap: add MAUI handler-backed `Switch`, `CheckBox`, `Slider`, and `Picker` to validate boolean, numeric, and selection input semantics on DOM platform views.
- `Switch` is intentionally projected to semantic `<input type="checkbox">` in this slice to prioritize handler/event/data-flow correctness over custom browser-only switch rendering.
- Visual color parity for browser form controls is treated as best effort (`accent-color` for switch/checkbox/slider plus slider track gradient), with functional two-way behavior taking precedence over pixel parity.
- Picker scope remains `Items` + `SelectedIndex` driven behavior with deterministic option rebuild and index synchronization; richer picker visuals and platform-specific popup semantics are deferred.

## 2026-03-05 (Vertical Slice 3.1)
- The initial switch projection as a styled checkbox was replaced with a dedicated semantic toggle (`button` with `role="switch"` and explicit track/thumb children) because checkbox styling produced stretched, slider-like visuals under MAUI row layout.
- Switch arrange behavior now uses an intrinsic frame (`~52x32`) clamped to available bounds and aligned inside the arranged slot, so rendered and clickable regions remain bounded even when parent layout assigns wide width.
- Switch accessibility state is mapped explicitly via `aria-checked`, and visual disabled treatment is handled in the handler while preserving shared enabled/disabled semantics from `BrowserViewMapper`.

## 2026-03-05 (Vertical Slice 4)
- Added a shared `IView`/`ITransform` mapper + command mapper (`CommonViewMapper`, `CommonViewCommandMapper`) and chained all browser handlers through it so core behavior is implemented once and remains consistent across controls.
- Core scope in this slice includes automation id (`data-automation-id`), flow direction (`direction`), basic semantics (`aria-label`, `aria-description`, heading role/level on heading labels), effective interaction policy (`IsEnabled` + `InputTransparent` -> `pointer-events`), solid background, z-index, focus/unfocus, and deterministic transform composition.
- Focus/unfocus now uses explicit DOM interop (`focusElement`, `blurElement`) through `DomApi`, preserving the single JSImport entry surface.
- `Clip` and `Shadow` are intentionally deferred: they are no-op in this slice and emit DEBUG warnings so unsupported mappings are visible during development without breaking runtime flow.

## 2026-03-05 (Vertical Slice 5)
- StackLayout semantic projection is the next proof step: `VerticalStackLayout` and `HorizontalStackLayout` now project to flex containers with automatic fallback to absolute projection for non-stack layouts.
- Rollout is auto-with-safe-fallback by default so no app-level feature flag is required for this slice; if semantic projection mode is not recognized, handler behavior remains absolute-compatible.
- Playwright proof gates now assert both behavior and DOM style (`display:flex`/direction) for stack containers while confirming a known non-stack (`Grid`) does not claim flex projection in this slice.
- `Grid -> CSS Grid` semantic projection is explicitly deferred to the next layout slice to keep risk bounded and acceptance criteria deterministic.

## 2026-03-05 (Vertical Slice 6)
- `Grid` now projects to CSS Grid as a first-class semantic mode in `BrowserLayoutHandler`, while non-grid/unusable metadata paths still degrade to absolute projection.
- Track mapping policy is explicit: absolute -> `px`, auto -> `auto`, star -> `fr` with non-positive star values normalized to `1fr`; invalid absolute track values or invalid spacing trigger absolute fallback.
- Grid child mapping now applies row/column placement (`grid-row-start`/`grid-column-start` + span end values), in-cell alignment (`justify-self`/`align-self`), and margin projection in semantic mode.
- Existing stack flex semantics remain unchanged; regression gates now cover both stack and grid semantic style + placement behavior.
- Deferred items after this slice remain clip/shadow implementation and font-load-synchronized measurement behavior.

## 2026-03-05 (Vertical Slice 7)
- Scope is `NavigationPage` + `ScrollView` only, with explicit no-Shell policy; this keeps navigation proof targeted to page-stack viability without investing in route/flyout abstractions yet.
- `NavigationPage` now projects to a minimal DOM navigation surface (`Back` + title + active-page host) and handles stack transitions via `IStackNavigation.RequestNavigation`, while lifecycle notifications (`SendAppearing`/`SendDisappearing`) are forwarded on active-page swaps.
- `ScrollView` uses native browser overflow behavior, maps orientation/scrollbar visibility through CSS overflow axes, and supports programmatic `RequestScrollTo` through centralized `DomApi` scroll interop.
- Automated proof adds navigation/scroll flow and push-pop leak-loop gates; stack and event subscription counts are asserted near baseline after repeated navigation churn.
- Multi-window navigation stacks and Shell-specific behaviors remain explicitly deferred.

## 2026-03-06 (Vertical Slice 8)
- CollectionView is the next viability proof step to validate that handler-backed item templates and dynamic data flows can run on the DOM platform without introducing a parallel UI layer.
- Scope is intentionally vertical and non-virtualized: `DataTemplate` item realization, `EmptyView`, and observable collection `Add`/`Remove`/`Reset` updates are in-scope; unsupported `Move`/`Replace` fall back to rebuild for deterministic behavior.
- Selection is explicitly deferred in this slice (`SelectionMode != None` emits DEBUG unsupported diagnostics) so list rendering/data-change correctness can be proven first.
- Browser projection uses a dedicated scroll host + items host + empty host under a single CollectionView platform root, keeping all DOM interop in `DomApi` and all mapping logic in `Maui.Browser`.

## 2026-03-06 (Parity Program Baseline)
- Added a parity program source-of-truth ledger (`docs/PARITY_MATRIX.md`) and explicit gap tracker (`docs/PARITY_GAPS.md`) to map modern MAUI controls/layouts to browser handlers with status and proof links.
- Added parity tooling (`tools/parity`) plus CI gating (`check_parity_matrix.sh`) so matrix coverage cannot silently drift behind the declared modern-control inventory.
- Sample app startup now routes through a parity catalog index page with stable automation IDs and family-page entry points, while preserving the consolidated probe page for current smoke coverage.
- E2E suite structure moved to family-oriented files using a shared test base, enabling parity-wave expansion without one monolithic test file.
