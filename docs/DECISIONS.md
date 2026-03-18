# Decisions Log

Keep this short. Append new entries with date and a 2–5 sentence rationale.

## 2026-03-18 (Map hardening follow-up: provider-scoped module sync + local-mode validation + pin item updates)
- Replaced hardcoded Leaflet module sync in `Directory.Build.targets` with provider-declared `MirageBrowserRuntimeModule` discovery/copy so browser runtime module assets are owned by referenced provider projects instead of repo-root path assumptions.
- Hardened Leaflet local-asset mode by requiring explicit local JS/CSS URLs when `AssetMode=Local`; invalid local-mode configuration now fails fast with deterministic diagnostics (`local-assets-not-configured` / `local-assets-invalid-url`).
- Extended map handler behavior to subscribe to individual `Pin` property changes so label/location updates on existing pin instances project without requiring explicit mapper calls.
- Tightened local-asset validation to reject protocol-relative and absolute URLs in `AssetMode=Local` so the local-mode contract is strict and deterministic.
- Removed runtime-side unconditional map-error clearing after adapter apply; provider/adapter status now remains authoritative and failed apply operations cannot be masked as ready.
- Updated Leaflet host registration to explicitly replace any prior `IMirageMapProvider` registration so `UseMirageLeafletMaps(...)` is deterministic in host composition order.

## 2026-03-17 (Built-in Map ControlNode + Leaflet provider architecture)
- Added first-class `Map` support on the built-in control-node path (`ComponentKind.Map`) with Mirage-owned, provider-neutral semantic state (`region/span` as authority, map type, and identity-keyed pins).
- Introduced map provider contracts (`IMirageMapProvider`, `IMirageMapAdapter`) so map engine lifecycle is provider-owned (create once per host, update many times, dispose deterministically) while handlers remain the MAUI mapping boundary.
- Added `Mirage.Provider.LeafletMaps` as the first browser map provider with CDN-default asset loading, explicit readiness diagnostics, deterministic error states for missing provider/assets, and configurable tile policy (default OSM + attribution).
- Added `Showcase` map proof surface (`/map-lab`) and integration/E2E coverage for ControlNode projection, provider-required behavior, region updates, and identity-based pin synchronization.
- E2E host fixtures now serialize sample-host startup and run browser samples with `--configuration Release --no-build` to prevent static-web-assets/MSBuild contention during parallel fixture initialization.

## 2026-03-16 (Stepper moved from numeric input to Mirage-owned +/- chrome)
- Replaced the browser Stepper realization from `input[type=number]` with a Mirage-owned two-button control (`-` and `+`) so interaction semantics match MAUI Stepper behavior instead of free-form text entry.
- Added provider/runtime secondary-element event targeting to route decrement and increment clicks without handler-side DOM assumptions.
- Chose compact Android-like split button chrome as the default web Stepper style while preserving common MAUI background/brush projection on the host.

## 2026-03-16 (ScrollView page family runnable via Frame + CollectionView handler additions)
- Enabled upstream `ScrollViewPage` and its nested scroll scenarios in ControlsSample by adding Mirage `Frame` and `CollectionView` handlers on the MAUI handler path (no sample-side DOM hacks).
- `CollectionView` in this wave is intentionally non-virtualized and selection-command focused, which is enough for the ScrollView catalog pattern (`SelectionMode=Single`, templated item rows, and command-based navigation).
- Added the missing theme/style resource keys used by upstream `ScrollViewPage` so gallery cards/search surfaces render with upstream-compatible styling from MAUI resources.
- `ScrollViewOrientationPage` remains imported but filtered from browser navigation until Picker support lands; other ScrollView subpages are runnable now.

## 2026-03-16 (TransformationsPage runnable + shared transform composition)
- Enabled `TransformationsPage` as a runnable ControlsSample page with deterministic automation IDs for target label, sliders, and anchor steppers.
- Added full `ITransform` mapping to the shared Mirage view mapper/projection path (`Translation*`, `Scale*`, `Rotation*`, `Anchor*`) instead of page-specific transform logic.
- Introduced internal transform fragment composition so layout-authored transforms (for example AbsoluteLayout proportional translate) compose with view-authored transforms in deterministic order (`layout` then `view`) without overwriting each other.

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

## 2026-03-06 (Vertical Slice 9)
- Shared `IView` substrate parity now maps `Clip` (`RectangleGeometry`/`RoundRectangleGeometry`) and `Shadow` (solid paint) into CSS (`clip-path`, `box-shadow`) in the common browser mapper; unsupported geometry/paint types remain deterministic fallback paths with DEBUG diagnostics.
- `Background` mapping now supports both solid and linear-gradient paints; radial/image/pattern paints remain explicitly deferred with style-clear fallback and diagnostics.
- Clip projection uses CSS `inset(...)` expressions with `calc(100% - Npx)` for right/bottom offsets so clipping remains stable across arrange timing and resize without requiring additional JS interop.
- Proof surface for this slice is family-page-only: `ParityContainersLayoutPage` is now a real XAML probe page with dedicated clip/shadow/background controls, keeping consolidated demo probes unchanged.

## 2026-03-06 (Vertical Slice 10)
- Text/Input parity wave was implemented as handler-first additions and hardening: `Entry` now maps password/keyboard/placeholder-color/return-type semantics, `Label` maps line-break and max-line projection, and new browser handlers were added for `Editor` and `SearchBar`.
- Date/time and numeric/status controls were added in the same wave (`DatePicker`, `TimePicker`, `Stepper`, `ProgressBar`, `ActivityIndicator`) using deterministic browser-native projections and two-way state updates.
- Family pages `ParityTextInputPage` and `ParityPickersDateTimePage` were upgraded from placeholders to real XAML probes with stable automation IDs and programmatic-update actions.
- Known non-parity areas for this wave remain explicit and documented: full custom date/time format token parity, cursor/selection fidelity, and platform-specific spinner/stepper visuals are still best-effort.

## 2026-03-06 (Vertical Slice 11)
- Media/Visual parity started with handler-backed `Image`, `ImageButton`, and `Border` projections in `Maui.Browser`, replacing the previous placeholder media family page with a real XAML probe surface and E2E regression coverage.
- Image source policy for this slice is explicit and deterministic: `UriImageSource`, `FileImageSource`, and `FontImageSource` are supported; unsupported source types (such as stream-based sources) map to fallback state + diagnostics without throwing.
- `ImageButton` uses a semantic `<button>` host with an `<img>` child so image-source/aspect mapping can be shared with `Image` while preserving click/pressed/released event parity.
- `Border` maps single-child hosting, solid stroke thickness/color, round-rectangle/rectangle shape radius, and dashed best-effort style; non-solid stroke paints and unsupported stroke shapes are deferred as documented gaps.

## 2026-03-07 (Vertical Slice 12)
- Added semantic `FlexLayout` projection to `BrowserLayoutHandler` as a dedicated mode, while preserving existing stack/grid semantics and absolute fallback behavior for unresolved metadata.
- Flex container mappings now project `Direction`, `Wrap`, `JustifyContent`, `AlignItems`, and `AlignContent`; item mappings project attached `Order`, `Grow`, `Shrink`, `Basis`, and `AlignSelf`.
- For MAUI 10, `FlexBasis` in this environment exposes `Auto` + absolute `Length`; percentage basis semantics are deferred and explicitly tracked as a parity gap.
- Containers/Layout family page now includes a real flex probe surface with deterministic toggle actions and Playwright assertions for container styles, item style projection, ordering geometry, and mutation stability.

## 2026-03-07 (Vertical Slice 13)
- CollectionView moved from viability-only to parity-depth scope while remaining non-virtualized: selection (`None`/`Single`/`Multiple`), incremental `Move`/`Replace`, header/footer realization, and `ScrollTo` are now handler-mapped.
- Scroll behavior uses a deterministic DOM child-into-view interop helper (`Start`/`Center`/`End`/`MakeVisible`) exposed via `DomApi`, keeping JSImport centralized.
- Selection markers are projected as deterministic DOM attributes/styles (`data-maui-cv-selected`, `aria-selected`) to keep E2E assertions stable and to avoid hidden state in tests.
- RefreshView integration remains explicitly deferred so CollectionView parity can harden independently before pull-to-refresh coupling.

## 2026-03-07 (Vertical Slice 14)
- Added `BrowserRefreshViewHandler` with command/state-first interaction to match MAUI upstream trigger semantics: user action sets `IsRefreshing=true`, while `RefreshView` raises `Refreshing` and executes `Command`.
- This slice intentionally uses an explicit trigger/indicator row in the browser projection and defers pull-drag gesture parity; deferred behavior is tracked in `PARITY_GAPS.md`.
- `RefreshView` proof is integrated directly with the Collection/Templated family page to validate list behavior under active refresh state transitions instead of testing refresh in isolation.

## 2026-03-07 (Vertical Slice 15)
- Added browser pull-to-refresh activation on top of the existing `RefreshView` command/state handler path using a dedicated DOM interop subscription (`subscribePullToRefresh`) that triggers refresh only after a deterministic downward threshold at scroll-top.
- Pull detection is centralized in `DomApi` and cleaned up through existing event-subscription lifecycle, keeping JS entrypoints single-surface while avoiding handler-side direct JS calls.
- `BrowserRefreshViewHandler` now keeps explicit pull state attributes (`data-maui-rv-pull-active`, `data-maui-rv-pull-armed`, `data-maui-rv-pull-distance`) for deterministic diagnostics/E2E assertions and maps pull triggers through the same `IsEnabled`/`IsRefreshEnabled`/`IsRefreshing` gate as button-triggered refresh.
- Native overscroll elasticity physics and platform-specific pull affordance visuals remain deferred; this slice targets behavior parity first.

## 2026-03-07 (Vertical Slice 15: Styling + Presentation Showcase)
- Added a dedicated showcase track alongside parity lab pages so polished product presentation and parity-regression proof can evolve independently.
- Themeing uses MAUI `DynamicResource` tokens updated in `Application.Resources`, enabling live theme swaps without full reload while preserving page state.
- Shared showcase styles explicitly prove MAUI style-system behavior (`implicit`, `explicit`, `BasedOn`, `StyleClass`, and control state styles) on top of existing browser handlers.
- `BrowserButtonHandler` now maps button border/corner styling and pointer press/release state transitions so style-state behavior is deterministic for both parity pages and showcase UI.

## 2026-03-07 (Showcase Catalog Reorganization)
- Startup now lands on `ShowcaseHomePage`, and showcase catalog navigation is treated as the canonical entry path for E2E (`showcase-page-home` -> `showcase-page-catalog`).
- Added a 1:1 control-type catalog (`ShowcaseControlCatalog`) rendered directly on `ShowcaseHomePage`, so every MAUI control/layout type is visible on the top-level showcase entry point.
- Missing handlers (`GraphicsView`, `CarouselView`, `IndicatorView`, `SwipeView`, `WebView`) are explicitly listed as “not implemented yet” instead of being omitted from the showcase.
- Existing deep-dive scenario pages are retained as supplemental flows, but all are now showcase-styled and accessible from the same showcase catalog.

## 2026-03-07 (Vertical Slice 16)
- Replaced the previous standalone Label demo entry with a dedicated interactive Label depth page (`showcase-page-label-depth`) as the canonical parity proof surface for Label.
- The new page is state-driven and property-editable in real time, covering mapped Label and inherited IView properties in a single live preview host to make mapping regressions immediately visible.
- Controls-layer Label gaps (`FormattedText`, `TextType`, `TextTransform`) are now surfaced as explicit, deterministic interactive probes on the same page instead of being implicit omissions.
- Home control catalog routing now opens Label depth directly from the Label row, while Text/Input depth remains focused on `Entry`/`Editor`/`SearchBar`.

## 2026-03-07 (Mirage Architecture Reset: Phase 1)
- The active runtime was reset from `Maui.Browser` direct handler-to-DOM projection to a new Mirage architecture with a required provider contract (`IMirageComponentProvider`) and provider-backed realization pipeline.
- Active solution wiring now targets `Mirage.Core`, `Mirage.Web`, `Mirage.Maui`, `Mirage.Provider.Fast`, and `samples/Counter.Fast`; legacy `Maui.Browser*` projects remain in the repository but are intentionally removed from the active build/test path.
- Phase-1 semantic scope is locked to six kinds only: `Root`, `ContentContainer`, `VerticalStack`, `Label`, `Entry`, `Button`; any other control kind is explicitly out of scope and treated as unsupported.
- `Mirage.Core` now owns deterministic node-id-based diffing and patch operations, with parent-safe operation ordering guarantees (create before property/event/style updates, child-first removals).
- `Mirage.Web` owns browser runtime infrastructure only (JSImport bridge, patch applier, event bridge, measurement), and provider-specific mapping logic is isolated to `Mirage.Provider.Fast`.
- FAST is the only implemented provider in this slice; UI5/default provider implementations are intentionally deferred.

## 2026-03-07 (Showcase.Fast Hero: DOM-First Public Demo)
- Added `samples/Showcase.Fast` as a public-facing Mirage sample alongside `Counter.Fast`, keeping the minimal counter app as a stability harness while introducing a polished hero/landing proof page.
- Extended the semantic model with exactly one new kind, `HorizontalStack`, to support nav/CTA row composition without broadening layout scope beyond what the hero requires.
- Added a browser page-navigation capability (`IMiragePageNavigator` + JS interop `scrollToAutomationId`) for smooth in-page section scrolling to stable section ids (`hero`, `demo`, `architecture`, `providers`); routing remains out of scope.
- Hero messaging now explicitly reinforces Mirage's differentiator in multiple places: MAUI UI rendered as real DOM and FAST Web Components, with explicit contrast against canvas-based rendering approaches.
- Hero surface intentionally keeps interactive behavior minimal (entry binding + command button counter) and uses local sample styling only; no global theming framework, no non-FAST providers, and no canvas/Skia rendering were introduced.

## 2026-03-08 (Showcase Hero Anchor + Provider Mapping Hardening)
- Locked section anchors (`hero`, `demo`, `architecture`, `providers`) onto section container nodes rather than heading labels so smooth-scroll and automation queries target stable structural nodes.
- Corrected a FAST provider static-initialization ordering bug so shared layout descriptors (`Root`, `ContentContainer`, `VerticalStack`, `HorizontalStack`) always carry `AutomationId -> data-automation-id` mapping metadata.
- Added integration regression coverage asserting layout descriptors expose `AutomationId` mapping to prevent silent loss of container automation ids and to keep E2E anchor behavior deterministic.

## 2026-03-08 (Showcase Hero XAML Authoring)
- Re-authored `ShowcaseHeroPage` as XAML with a code-behind partial class so the public demo page is declarative and easier to maintain as presentation content evolves.
- Kept the same Phase-1 control/layout surface (`ContentView`, `VerticalStackLayout`, `HorizontalStackLayout`, `Label`, `Entry`, `Button`) and preserved existing stable automation ids for E2E and smooth-scroll targets.
- Added `Microsoft.Maui.Controls.Xaml` + `MauiXaml` build wiring in `Showcase.Fast` so page markup is loaded via MAUI XAML resource flow without changing runtime architecture boundaries.

## 2026-03-08 (Mirage.Maui Common View Projection Centralization)
- Consolidated shared MAUI adapter projection logic into `MirageCommonViewProjection` so `AutomationId`, margin, visibility, and thickness/px normalization are mapped from one internal path instead of repeated per-node helper calls.
- Kept Phase-1 behavior unchanged: padding is still projected only where it was previously applied (`ContentPage`, `ContentView`, stack layouts), and no additional `IView` properties were introduced in this step.
- Added integration-level regression tests for projection behavior (automation-id mapping, deterministic thickness formatting, hidden-view display mapping, and padding inclusion scope) to guard against adapter drift as additional mappings are added later.

## 2026-03-08 (Mirage Handler-Centered Browser Backend Cutover)
- Removed the translator-style `MirageMauiAdapter` path and shifted Mirage.Maui to MAUI handlers as the control mapping boundary (`Window`, `ContentPage`, `ContentView`, `VerticalStackLayout`, `HorizontalStackLayout`, `Label`, `Entry`, `Button`).
- Added `MiragePlatformView` + `MirageRenderCoordinator` so handlers mutate platform/semantic node state while runtime rendering remains semantic-tree + deterministic diff + provider patch application.
- Runtime event dispatch now routes from `IMirageRuntime` to handler-owned node targets by stable node id, with phase-1 handler behavior for `Entry` input and `Button` click.
- Startup APIs were intentionally broken and renamed to platform-oriented names: `UseMirageBrowserPlatform`, `UseMirageComponentProvider<T>`, and `MirageBrowserAppHost.RunAsync`.

## 2026-03-08 (Mirage Hybrid Platform Tree Cutover)
- Replaced the typed-only semantic model with a hybrid platform tree (`ControlNode` + `WebNode`) backed by shared node/visual foundations and stable node-id identity.
- Added `MirageValue` as a typed property value model (`Null`, `String`, `Bool`, `Number`, `Json`) while intentionally keeping attributes/styles string-based in this cutover.
- Broke provider contracts to dual realization paths (`ResolveControl`, `ResolveWebNode`) and locked the rule that generic element realization must preserve requested element identity.
- Extended diff/patch contracts to mixed typed/generic trees with deterministic operation ordering and explicit support for generic attributes/classes in addition to property/style/event updates.
- Kept built-in Mirage handlers on typed-node output only, and added a minimal third-party extensibility proof via a custom handler emitting a `vendor-pill` `WebNode` in `Showcase.Fast`.

## 2026-03-08 (Early-lock Architecture Guardrails)
- Locked built-in typed-node purity as a hard invariant: Mirage-owned built-in handlers must emit typed nodes; `WebNode` remains the extensibility path for third-party/custom/BYO web components.
- Formalized shared visual contract scope so `VisualNodeBase` carries only cross-cutting concerns (automation id, visibility, opacity, margin, layout metadata, common mutation channels), while attributes/classes/slots stay element-specific and richer MAUI style semantics remain deferred.
- Tightened generic element contract wording and tests: `ResolveWebNode(...)` is capability/validation-oriented and must preserve requested element identity through provider/runtime realization.
- Added explicit architectural policy for generic event payloads: default name-only payloads, opt-in input-value payloads, structured generic payload extraction deferred pending typed payload contract.
- Constrained `MirageValue.Json`: only object/array shapes are valid, deep clone and structural equality are guaranteed, and usage is intended for structured payloads rather than high-frequency churn.
- Added provider conformance test layer and Mirage.Web provider-neutral boundary guard tests to prevent drift before introducing additional providers.
- Added lightweight architecture benchmark harness (`tools/benchmarks/Mirage.ArchitectureBenchmarks`) covering mixed typed/generic churn, property/event churn, JSON updates, and custom-element-heavy subtree mutation.

## 2026-03-08 (Mirage Foundations Hardening: Label + IView Core + VerticalStack)
- Added `MirageCommonViewProjection` as the single internal projection path for common `IView` concerns (`AutomationId`, `Margin`, `Visibility`, and deterministic thickness/px formatting), and routed `MirageViewMapper` through this helper.
- Kept behavior scope intentionally unchanged: no new cross-cutting MAUI semantics were added, and padding projection remains container-only (`ContentPage`, `ContentView`, stack layouts).
- Hardened baseline handlers for deterministic/idempotent updates: `MirageLabelHandler` now applies text/font-size updates only when values change, and stack style application avoids redundant style writes while preserving the same visual contract.
- Extended `Showcase.Fast` with a focused Foundations Lab proving label live updates, visibility mapping (`display:none`), margin mutation formatting, and vertical stack spacing reflow through stable automation ids.
- Added regression coverage and architectural guard tests to preserve this bounded scope and prevent accidental expansion into non-phase behavior.

## 2026-03-08 (Mirage Renderer-Correctness Wave: Stack/Grid/Surface)
- Expanded shared projection through `MirageCommonViewProjection` as the only cross-cutting path for `Background` (solid + linear-gradient), `Margin`, `Padding` (container-only policy retained), `Visibility`, `Opacity`, and explicit size requests (`Width`/`Height` + min/max), with deterministic set-if-changed behavior.
- Added natural layout alignment projection only where supported by layout semantics: stack cross-axis alignment and grid cell self-alignment metadata; no global speculative alignment engine was introduced.
- Promoted layout scope to typed `Grid` alongside hardened stack paths: `MirageLayoutHandler` now realizes vertical/horizontal stacks and grid deterministically, including spacing/gap behavior, child-order stability, and attach/detach/reorder safety.
- Grid v1 policy is locked to deterministic CSS Grid translation (`Auto -> auto`, `Star -> fr` with non-positive star normalized to `1fr`, `Absolute -> px`) plus row/column placement, span mapping, and row/column spacing (`row-gap`, `column-gap`).
- Updated FAST typed mappings to include `ComponentKind.Grid` while keeping `Mirage.Web` provider-neutral and preserving typed-node built-in invariants (`WebNode` remains extensibility path only).
- Extended `Showcase.Fast` with stack/grid/background proof sections and expanded integration/E2E coverage for idempotent projection, layout correctness, visible DOM-backed rendering, and explicit no-canvas guarantees.

## 2026-03-08 (Mirage Styling-First Wave: MAUI-Primary with CSS Escape Hatch)
- Expanded `MirageCommonViewProjection` with shared text-style mapping (`TextColor`, `Font`, `CharacterSpacing`) using deterministic set-if-changed updates and explicit fallback font-size normalization.
- Added button visual style primitives in `MirageButtonHandler` (`StrokeColor`, `StrokeThickness`, `CornerRadius`) so MAUI button styling can drive DOM output without provider/runtime API changes.
- `Showcase.Fast` now uses app-level MAUI `ResourceDictionary` styles as the primary styling path; CSS in `wwwroot/index.html` is intentionally narrowed to browser/FAST escape-hatch concerns (sticky/backdrop shell, internal FAST input polish, and table chip presentation).
- Deferred in this wave: trigger/visual-state parity expansion, global theming framework rollout, and broad control-style parity beyond the built-in surface.

## 2026-03-08 (Showcase Dedicated Control Demo Pages: ContentView First)
- Showcase now includes a dedicated `ContentView` demo surface (page-style section) focused on handler correctness depth rather than marketing narrative.
- Navigation to the demo uses existing in-page automation-id scrolling (`IMiragePageNavigator`) and avoids introducing routing or runtime page-switch infrastructure in this slice.
- The `ContentView` demo is locked to core `IView` probes (background, opacity, visibility, margin/padding, and `HorizontalOptions`/`VerticalOptions` positioning) with stable automation IDs for E2E verification.
- Styling for the demo follows the MAUI ResourceDictionary-first policy; no additional global `index.html` styling was introduced for this feature.

## 2026-03-08 (ContentView Size/Overflow Parity + Clip Mapping)
- Added a dedicated `ContentView` size/overflow lab in `Showcase.Fast` with deterministic static rows and interactive probes for `WidthRequest`/`HeightRequest`, alignment (`HorizontalOptions`/`VerticalOptions`), and clipped vs unclipped overflow behavior.
- Removed positioning scaffold filler text and normalized horizontal/vertical positioning probes to explicit equal width/height constraints to keep visual comparison unambiguous.
- `Mirage.Maui` now maps `IView.Clip` into CSS `clip-path` and maps `Layout.ClipsToBounds` into `overflow:hidden|visible` via the shared projection path, preserving idempotent set-if-changed behavior.
- Clip projection policy for this wave is strict and deterministic: all built-in MAUI geometry classes (`RectangleGeometry`, `RoundRectangleGeometry`, `EllipseGeometry`, `LineGeometry`, `PathGeometry`, `GeometryGroup`) are supported, while unsupported geometry types or invalid numeric geometry state throw in both Debug and Release builds (no fallback).

## 2026-03-09 (Fluent-First Provider Cutover)
- Switched the active reference provider from `Mirage.Provider.Fast` to `Mirage.Provider.Fluent` and renamed public sample harnesses to `Counter.Fluent` and `Showcase.Fluent` so the default Mirage story is Fluent-first.
- Typed phase-1 mapping stays architecture-neutral in core contracts while provider realization now targets Fluent elements (`fluent-button`, `fluent-text-field`) and keeps generic `WebNode` identity passthrough unchanged.
- Public messaging and docs now position Mirage as a provider-based runtime that adapts complete web component libraries, with Fluent UI Web Components as the first polished reference provider.
- FAST remains acknowledged as a lower-level foundation for Fluent, but it is no longer the primary public-facing provider identity in active solution/docs/tests.
- This cutover intentionally does not expand `ComponentKind` or rewrite Mirage core/runtime contracts; additional typed control breadth remains a follow-up wave.

## 2026-03-09 (ControlNode / WebNode Architectural Alignment Cutover)
- Renamed Mirage’s hybrid platform-tree categories and contracts from typed/element vocabulary to role-based `ControlNode`/`WebNode` terminology (`ResolveControl`, `ResolveWebNode`, `SupportsControlNodes`, `SupportsWebNodes`) to align with handler-centered MAUI platform realization semantics.
- Kept behavior intact while tightening architecture language: MAUI framework/logical semantics stay MAUI-side, handlers remain the mapping boundary, and Mirage models platform realization through control/web node trees.
- Updated runtime diagnostics and DOM markers to `control`/`web` node-type values and retained web-node element-identity preservation as a hard provider/runtime invariant.
- Added regression coverage preventing stale pre-cutover terminology in active source/docs and preserving built-in control-node purity vs web-node extensibility.

## 2026-03-09 (Showcase Layout Lab: Container Sizing / Constraints / Positioning)
- Added a dedicated same-page `layout-lab` section in `Showcase.Fluent` as a correctness surface (not marketing) with an exhaustive 25-card scenario matrix spanning `ContentView`, `VerticalStackLayout`, `HorizontalStackLayout`, `Grid`, and nested layout contexts.
- Locked stable automation-id conventions for layout-lab cards, parent surfaces, primary child probes, and expected-behavior notes so geometry and visibility assertions can be expanded safely in E2E.
- Added targeted handler-side layout correctness fixes needed for MAUI semantics in these scenarios: content-container child alignment projection (`HorizontalOptions`/`VerticalOptions`) and deterministic stack child no-shrink defaults (`flexGrow=0`, `flexShrink=0`, `flexBasis=auto`) under constrained bounds.
- Updated `ContentView`/`ContentPage` container base realization to a single-cell container model to support deterministic child placement semantics without provider-specific shortcuts.
- Scope remains layout/rendering correctness only; no control-breadth expansion, no provider contract changes, and no runtime/provider hacks specific to showcase scenarios.

## 2026-03-09 (Layout Lab Consolidation + Handler Guard Tightening)
- Removed the standalone `ContentView` demo section from `Showcase.Fluent` (including nav target/state/actions) so the new `Layout Lab` is the single correctness surface for container sizing, constraints, and positioning behavior.
- Removed ad hoc child `minWidth/minHeight = 0px` overrides from `MirageContentViewHandler` and `MirageContentPageHandler` to avoid clobbering explicit child minimum-size intent.
- Replaced brittle source-text regression coverage with behavior-focused verification in E2E (horizontal constrained stack probe asserts `flex-shrink: 0`, and content-container behavior checks guard against forced zero child minimum-size in both `ContentView` and `ContentPage` paths).
- Kept architectural boundaries unchanged: handler-centered mapping, built-in control-node paths, provider-neutral runtime, and DOM-first output.

## 2026-03-09 (ContentPage Scrolling Policy Lock: Explicit ScrollView)
- Locked Mirage to MAUI’s explicit scrolling model: `ContentPage` and window surfaces are non-scrollable by default, and scrolling must be introduced explicitly through `ScrollView`.
- Updated default platform styles to bounded app surfaces (`overflow:hidden` on window/page hosts) and removed implicit page/document scroll assumptions from handler defaults.
- Added a phase-bounded `MirageScrollViewHandler` (control-node `ContentContainer` reuse) covering content hosting, orientation, scrollbar-visibility projection, and `RequestScrollTo` command payload handling.
- Extended runtime/coordinator + DOM interop with node-id scroll read/write APIs so handlers can issue deterministic scroll commands without bypassing runtime/provider boundaries.
- Migrated `Showcase.Fluent` long-page hosting to an explicit root `ScrollView` (`showcase-scroll-root`) and kept DOM-first/no-canvas guarantees with updated integration/E2E coverage for non-scroll default page behavior and explicit scroll-surface navigation.

## 2026-03-09 (Mirage Production Hot Reload Infrastructure)
- Standardized active browser sample apps on `Microsoft.NET.Sdk.WebAssembly` and moved shared watch/runtime wiring into `Directory.Build.targets` so hot-reload boot/module behavior follows the official WebAssembly pipeline rather than per-app patching.
- Added shared browser-app watch safeguards (`RestoreBuildInParallel=false`, `BuildInParallel=false` under `DOTNET_WATCH=1`), automatic MauiXaml watch path mapping (`/_hotreload/{AssemblyName}/{relative-xaml-path}`), generated metadata-update handler attributes, and prebuild `mirage-dom.js` sync into each app `_framework` folder.
- Added a Mirage.Maui hot-reload runtime service and metadata update handler that use render-only refresh plus XAML reapply for live `Page/View/ResourceDictionary` instances (including NameScope clearing and `x:Name` field rebinding) without host restart.
- Added hybrid detection hooks in `mirage-dom.js`/`main.mjs`: websocket hint sniffing for low-latency scans plus polling fallback for correctness when hints are absent or partial.
- Added integration tests for repeated `x:Name` XAML reapply and `ResourceDictionary` clear+reload semantics, and converted `Counter.Fluent` page authoring to XAML as the minimal app-level hot-reload harness.
- XAML apply target discovery now includes the resolved primary host window in addition to `Application.Windows` so hot reload still applies when MAUI does not populate the application window collection in browser-hosted startup paths.

## 2026-03-15 (Controls.Sample Subtree Import + Wrapper Hosts)
- Added an upstream vendor lane for `dotnet/maui` Controls.Sample under `vendor/upstream/controls-sample`, synced via `git subtree` through `tools/vendor/sync-controls-sample.sh`, with sync metadata recorded in `.mirage-upstream.json`.
- Added licensing/compliance artifacts for the vendored sample (`LICENSE.upstream-MIT.txt` and `THIRD-PARTY-NOTICES.upstream.txt`) and documented third-party usage in `docs/THIRD_PARTY_NOTICES.md`.
- Locked sync compliance behavior: vendor sync now fails if upstream license/third-party notice files are not found, preventing accidental non-compliant updates.
- Shared wrapper compiles a curated first-wave upstream subset (Runnable: `TabbedPageGalleryMainPage`; native-only for now: `LayoutIsEnabledPage`, `InputTransparentPage`) while keeping the full upstream source vendored.
- Added wrapper hosts and shared catalog infrastructure: `ControlsSample.Shared`, `ControlsSample.Fluent`, and `ControlsSample.Maui`, with host-provided visibility policy (`IControlsSampleVisibilityPolicy`) so browser run surface remains Mirage-runnable while native host can expose broader imported pages.
- Added `ControlsSample.Maui` to `Mirage.sln`; the project supports Android + iOS + MacCatalyst by default on non-Linux hosts, with an opt-out switch (`ControlsSampleEnableAppleTargets=false`) for environments without the required Apple toolchain.
- Locked Razor handling policy for this lane: upstream `.razor` sources are retained in the vendor copy but excluded from browser/native run surfaces.

## 2026-03-10 (Navigation Extensibility Wave: NavigationPage-First Browser Projection)
- Navigation was locked to a `NavigationPage`-first model: MAUI stack operations (`PushAsync`, `PopAsync`, `PopToRootAsync`) remain source of truth while Mirage projects current-page state into browser URL/history/title.
- Added extensibility seams via `UseMirageNavigation(...)` and DI policies (`IRouteRegistry`, parser/serializer, page activator, parameter binder, guard, history policy, title policy) so apps control route shape/policy without replacing core stack semantics.
- Added provider-neutral browser history interop (`location`, `pushState`, `replaceState`, `popstate`, title) and startup deep-link bootstrap (`MirageBrowserAppHost`) to restore the current visible page route on initial load/refresh.

## 2026-03-13 (NavigationPage OOTB Parity: Built-in Navigation Shell)
- `MirageNavigationPageHandler` now realizes `NavigationPage` as a built-in shell on the control-node path with explicit nav-bar and bounded content-host regions instead of a content-host-only projection.
- Added first-wave chrome parity: visible title slot projection, stack-depth-driven back button visibility + pop behavior, deterministic inline toolbar item projection (order/priority/stable index), command invocation, and enabled-state handling.
- Added nav-bar state projection for `HasNavigationBar`/`HasBackButton` attached properties plus `BarBackgroundColor`/`BarTextColor` styling updates through handler-owned projection.
- Preserved existing architecture boundaries: stack/history routing model remains `NavigationPage`-first, runtime stays provider-neutral, no new provider contracts/component kinds were introduced, and page content remains bounded with explicit-scroll policy unchanged.
- `Showcase.Fluent` now dogfoods route projection with mapped routes (`/`, `/navigation-lab`, `/projects/{id}`) while preserving independent in-page section navigation (`IShowcaseSectionNavigator`) for hero section scrolling.
- Non-goals remain explicit: no Shell/tabs/flyout stack, no router-first replacement of MAUI semantics, and no provider-specific navigation behavior.
- Browser host startup now initializes the Mirage XAML hot-reload service unconditionally for browser `Application` hosts instead of gating on `DOTNET_WATCH`, because that environment variable is not reliably visible inside the WebAssembly runtime.
- Browser builds now create deterministic hot-reload mirror files under `wwwroot/_hotreload/{AssemblyName}/{relative-xaml-path}` before static-asset resolution, and attempt hard-link creation from `App/*.xaml` to keep source edits immediately visible at `/_hotreload/...` without host restart.
- To prevent duplicate static-web-asset collisions, generated mirror paths are excluded from default content discovery and only the explicit `MauiXaml`-backed hot-reload mappings participate in endpoint generation.
- Runtime XAML fetch is now constrained to `/_hotreload/...` paths (including hint-derived query variants under that prefix), eliminating `/App/...` fetch attempts that produced repeated 404 browser noise.
- Hint-driven candidate matching now uses `/_hotreload/...` document paths only, with periodic full-scan fallback retained for correctness when websocket hints are missing or incomplete.
- Hot-reload dispatch paths now fall back to inline execution when MAUI `IDispatcher.Dispatch(...)` refuses to enqueue, preventing silent drop of XAML apply/render work in browser sessions where dispatcher queueing reports failure.
- XAML apply failures now emit one-line browser-console diagnostics (per changed revision) with hot-reload path, target type, and exception details to make repaint failures debuggable without attaching a debugger.
- After successful element XAML reapply, Mirage now explicitly refreshes MAUI handlers for `IContentView` (`UpdateValue(Content)`) and `ILayout` roots (`Invoke(Update)`), ensuring DOM child trees remap even when internal XAML load mutations bypass normal property/collection notification paths.
- Element XAML reapply now includes a handler-state fallback: when direct `XamlLoader.Load(liveElement, xaml)` fails with `PlatformView cannot be null here`, Mirage loads into a detached clone and transfers staged `ContentPage`/`ContentView` trees back to the live element (plus `x:Name` field rebinding) to avoid live-handler null-platform crashes during browser hot reload.
- When that same handler-state failure occurs on a `Page` with an attached host window, Mirage now prioritizes window-page hot-swap (`window.Page = stagedClone`) before in-place transfer so page-level reload can succeed without mutating a handler instance whose platform view is already invalid.
- Detached clone creation now supports constructor injection from MAUI DI (`Handler.MauiContext.Services`) instead of requiring a parameterless constructor, so pages with host-provided constructor dependencies can participate in hot-reload fallback paths.
- Detached fallback now also triggers proactively when a live element has a handler instance whose `PlatformView` is already null (before calling `LoadXaml`), and clone constructor arguments can resolve from window/app service providers plus matching live-element fields to handle browser sessions where `Handler.MauiContext` is unavailable.
- Detached fallback now clears any existing staged element `NameScope` before reloading XAML, preventing constructor-preloaded pages from failing hot reload with duplicate `x:Name` registrations (for example `BacklogTableHost`).
- Element hot-reload apply now accepts the service-tracked primary host window as an explicit fallback lookup for page-swap reload, so constructor-injected pages can still reload when `Page.Window`/`Application.Windows` are not populated in browser-hosted sessions; page failure errors now prioritize page-swap root cause over the generic in-place unsupported-page message.

## 2026-03-13 (Showcase Layout Lab Routed-Page Split)
- Moved `Layout Lab` from an embedded hero section into a dedicated `ShowcaseLayoutLabPage` mapped to `/layout-lab`, while keeping the same layout probe automation IDs and scenario matrix for geometry/assertion continuity.
- Updated hero CTA behavior from in-page section scrolling to page navigation, preserving the `demo` in-page scroll CTA and route-driven `NavigationPage` stack semantics for lab navigation.
- Retargeted Showcase E2E coverage to the routed layout-lab page (`showcase-page-layout-lab` / `layout-lab-scroll-root`) for layout geometry checks and explicit scroll-surface assertions.

## 2026-03-14 (Showcase Hero Live Demo Removal)
- Removed the `Live Mirage Demo` card from `ShowcaseHeroPage.xaml`, including the entry/button/greeting probes and the embedded generic webnode demo host.
- Simplified hero CTA surface to keep only page-navigation actions (`Open Layout Lab`, navigation-lab/project routing) and removed hero-side in-page demo scroll behavior.
- Updated `ShowcaseFluentHeroE2ETests` expectations to enforce demo/generic-surface absence on hero while keeping layout-lab and navigation-shell assertions intact.

## 2026-03-09 (Hot Reload Null-PlatformView Lifecycle Hardening)
- Hot-reload element apply now treats MAUI handler `PlatformView` getter failures (`PlatformView cannot be null here`) as a missing-platform state, so detached/page-swap fallback can run instead of failing early on the getter access itself.
- `MirageContentPageHandler` and `MirageContentViewHandler` now tolerate pre-connect and post-disconnect mapper calls by short-circuiting child projection when no platform view is attached, while preserving the next-connect content rebuild path.
- Shared Mirage common view projection now no-ops when a handler exposes the Mirage platform contract but currently has no attached platform view, preventing style/property mapper crashes during transient handler detach windows.
- Added integration regression coverage for hot-reload apply with a handler that throws from `PlatformView` (matching MAUI `ViewHandler` behavior), validating detached fallback remains successful.

## 2026-03-09 (Hot Reload Scope Tightening)
- Browser-app hot reload build wiring now defaults to watch-driven activation (`DOTNET_WATCH=1` or `DotNetWatchBuild=true`) with explicit opt-in override (`MirageBrowserHotReloadAlwaysOn=true`) so non-watch builds do not automatically publish hot-reload asset mirrors.
- Runtime host startup keeps the hot-reload service available for browser apps, while build-time watch gating controls whether hot-reload metadata/assets are emitted.
- Platform-view null detection no longer depends on an exact exception message match; applicator fallbacks now detect platform-view getter failures by callsite (`get_PlatformView`) and include regression coverage for non-canonical exception messages.

## 2026-03-09 (Sample Triad Reorganization: Fluent + Maui + Shared)
- Reorganized both active sample lines into host-stable triads: `Counter.Fluent`/`Counter.Maui`/`Counter.Shared` and `Showcase.Fluent`/`Showcase.Maui`/`Showcase.Shared`, keeping Mirage host project names/paths unchanged for existing test harnesses.
- Moved reusable sample UI/state/resources into host-agnostic shared projects (`*.Shared`) and locked dependency direction to `*.Fluent -> *.Shared` and `*.Maui -> *.Shared`; shared projects do not reference Mirage browser runtime/provider concerns.
- Added runnable native MAUI hosts (`Counter.Maui`, `Showcase.Maui`) that consume shared root pages; these projects are intentionally repo-local and kept out of `Mirage.sln`/CI to avoid changing the browser-focused build pipeline.
- Refactored `ShowcaseHeroPage` to remove direct `IMiragePageNavigator` dependency; shared section scrolling uses MAUI `ScrollView` APIs with an optional shared host seam (`IShowcaseSectionNavigator`) for host-provided navigation optimization.
- Preserved Mirage-specific behavior through one narrow host seam: shared page accepts an optional generic-widget factory for `vendor-pill` composition while non-Mirage hosts use a MAUI-compatible fallback.
- Extended browser hot-reload build plumbing with external XAML support via `MirageHotReloadXaml` metadata (`HotReloadAssemblyName`, `HotReloadRelativePath`), deterministic `/_hotreload/{Assembly}/{RelativePath}` mapping, and explicit build errors for missing metadata or duplicate mapped paths.

## 2026-03-10 (Follow-up: Scroll Reliability + HotReload Packaging Boundaries)
- `ScrollView.ScrollToAsync(element, ...)` can surface element-mode requests with non-actionable offsets in Mirage runtime paths; `MirageScrollViewHandler` now computes offsets from target visual ancestry and otherwise falls back to MAUI-provided coordinate payloads (no private pending-request probing).
- `ShowcaseHeroPage` keeps shared code host-agnostic: section-link navigation first attempts an optional shared service seam (`IShowcaseSectionNavigator`, host-registered in `Showcase.Fluent` via `IMiragePageNavigator` adapter), then falls back to deterministic shared `ScrollView` offset scrolling and finally element-mode `ScrollToAsync`.
- Browser hosts now exclude `wwwroot/_hotreload/**` from baseline `None` and `WasmExtraFilesToDeploy` globs so watch-generated mirrors do not leak into normal non-watch packaging.
- Shared projects now explicitly own `MauiXaml` inclusion boundaries (`Remove="**/*.xaml"` + `Include="App/**/*.xaml"`), making shared XAML ownership deterministic and isolated to intended app surfaces.

## 2026-03-12 (Render Pipeline Hardening: Coordinator Coalescing + Scoped Materialization + Input Fast Path)
- Render coalescing is now a coordinator-level platform guarantee instead of a handler discipline requirement: `MirageRenderCoordinator.RequestRender()` coalesces same-turn invalidations, suppresses re-entrant immediate renders while a pass is active, and schedules at most one follow-up pass for mutations raised during render.
- Introduced scoped materialization v1 in the coordinator with cached node snapshots keyed by node id and subtree revision tracking from `MiragePlatformView`; non-structural updates can rematerialize only dirty subtrees while reusing unchanged siblings, while structural uncertainty safely falls back to full materialization.
- `MiragePlatformView` mutation paths are now deterministic set-if-changed operations with explicit structural vs non-structural revision propagation, reducing no-op churn before diff/runtime patching.
- Added an Entry high-frequency input fast path: runtime `input` events are coalesced to one latest-wins apply per dispatcher frame, and redundant render invalidations are skipped when the platform node already matches the committed value.
- Expanded diagnostics and perf guardrails with new render/input counters plus CI budget enforcement via `tools/diagnostics/run-slowness-matrix.sh` and `tools/diagnostics/perf-budgets-balanced.cfg` (hot-reload-off gating profile).

## 2026-03-12 (Styling Cutover: MAUI-First Samples with Near-Zero Host CSS)
- `Showcase.Fluent` and `Counter.Fluent` now keep `wwwroot/index.html` styles to shell-only host sizing/reset; sample-level visual styling is authored through MAUI resources/layout instead of automation-id CSS rules.
- Showcase backlog rendering moved to MAUI-owned structure and styles: a horizontal `ScrollView` host with MAUI `Grid`/cell styling for header/body typography, spacing, row separators, and deterministic min-width behavior.
- `ShowcaseVendorWidget` no longer hardcodes visual CSS in handler connect paths; it now exposes MAUI-facing bindable style properties (padding/corner/border/background/text color) that map through handler mappers into web realization styles.
- Counter sample styling now uses shared MAUI `ResourceDictionary` (`CounterStyles`) for page surface, typography, entry, and button visuals, replacing prior Fluent-element CSS overrides.
- Policy lock for active samples: MAUI style/layout is primary; host CSS is limited to app-shell bootstrap concerns only.

## 2026-03-12 (Showcase De-duplication: Layout Probes Consolidated in Layout Lab)
- Removed the `Foundations Lab` subsection from the `Live Mirage Demo` so layout/visual correctness probes are not duplicated across surfaces.
- `Live Mirage Demo` now stays focused on core interaction proof (entry binding, command click flow, and generic `WebNode` interactivity), while `Layout Lab` remains the single correctness surface for sizing/constraints/positioning and related visual probes.
- Removed Foundations-specific state/commands from `ShowcaseState` and replaced Foundations E2E coverage with an explicit absence guard (`showcase-foundations-lab` and `foundations-*` selectors not rendered).

## 2026-03-13 (Browser Theme Bridge for AppThemeBinding)
- Added a browser-to-MAUI theme bridge in Mirage host/runtime interop by wiring `prefers-color-scheme` read + change subscription through `mirage-dom.js` and `DomApi`/`DomElement`.
- `MirageBrowserAppHost` now applies browser light/dark preference to `Application.UserAppTheme` at startup and on media-query changes, so MAUI `AppThemeBinding` resources resolve and update in browser-hosted apps.
- Theme updates stay dispatcher-safe and do not introduce provider/runtime semantic changes beyond theme projection.

## 2026-03-14 (Sample XAML Flow Alignment: SourceGen + InitializeComponent)
- Sample app code-behind now follows normal MAUI conventions by calling `InitializeComponent()` directly instead of helper wrappers around `LoadFromXaml(typeof(...))`.
- Enabled `MauiXamlInflator=SourceGen` across browser/shared sample projects (`Counter.Fluent`, `Showcase.Fluent`, `Counter.Shared`, `Showcase.Shared`, `Maui.Browser.Sample`) so Debug and Release builds use compile-time generated XAML paths.
- Removed sample-only `IL2026` suppressions tied to runtime XAML inflation in style dictionary code-behind files.

## 2026-03-14 (Showcase XAML Lab Minimum Full Slice)
- Added a dedicated `/xaml-lab` showcase route and page with a two-pane workflow: a constrained XAML fragment editor on the left and a live Mirage-rendered MAUI preview host on the right.
- The editor is intentionally hosted via the `WebNode` path using a Fluent-specific custom element (`mirage-monaco-editor`) and handler (`ShowcaseMonacoEditorHandler`) to prove arbitrary web-widget hosting through normal Mirage architecture seams.
- Preview rendering stays in the standard Mirage MAUI pipeline via a shared fragment compiler + validator (`XamlLabFragmentCompiler`) and debounced preview session (`XamlLabPreviewSession`) with strict whitelist enforcement, explicit error reporting, and last-good-preview preservation.
- Scope is intentionally constrained to fragment authoring only (single root, no app/page roots, no arbitrary event-handler execution, no unrestricted directives/code-behind).

## 2026-03-15 (ControlsSample AbsoluteLayoutPage Enablement)
- Added browser runtime support for `AbsoluteLayout` and `BoxView` in Mirage (`MirageLayoutHandler` + new `MirageBoxViewHandler`) so foundational absolute-positioning scenarios can run without custom page rewrites.
- Extended semantic/provider mapping with `ComponentKind.AbsoluteLayout` and `ComponentKind.BoxView` in the Fluent provider, preserving control-node-based realization and existing provider/runtime contracts.
- Imported upstream `AbsoluteLayoutPage` into `ControlsSample.Shared` with a minimal vendor-compat `BasePage` shim to keep selective import boundaries small while honoring upstream XAML type expectations.
- Updated ControlsSample catalog/E2E/status tracking so `AbsoluteLayoutPage` is now a runnable, browser-validated entry in the staged implementation backlog.

## 2026-03-15 (ControlsSample Nav Title + Theme Parity Polish)
- Centered Mirage `NavigationPage` title projection within the nav title slot (`MirageNavigationPageHandler`) so wrapper pages align closer to native page-header expectations.
- Seeded upstream `AppResources` color keys in `ControlsSampleApp` (`LightBackgroundColor`/`DarkBackgroundColor` plus baseline foreground keys) so imported upstream pages like `BasePage`-derived layouts can resolve theme-driven page backgrounds in browser/native wrapper hosts.
- Applied explicit app-theme binding to wrapper `NavigationPage` chrome (`BarBackgroundColor`/`BarTextColor`) to remove light-only fallback behavior and keep dark-mode parity for ControlsSample runs.
- Updated nav-bar shell column sizing to symmetric left/right tracks so the title is centered relative to the full page width (not shifted by back-button-only chrome).
- Moved unstyled-text parity into Mirage platform defaults: `MirageLabelHandler` now projects system-like fallbacks (`CanvasText` + `system-ui`) when MAUI does not set `TextColor`/`FontFamily`, and synthetic nav-title chrome in `MirageNavigationPageHandler` uses the same fallback family/color path for consistency. `MirageWindowHandler` opts into `colorScheme: light dark` so those defaults track browser/system theme without sample-specific style enforcement.

## 2026-03-15 (ControlsSample Layout Slice: Grid + Clipping + ContentView)
- Imported upstream `GridPage`, `ClippingPage`, and `ContentViewPage` into `ControlsSample.Shared`, plus the upstream `CardView` control dependency used by `ContentViewPage`.
- Promoted `GridPage` to `Runnable` in Mirage and extended ControlsSample browser E2E coverage to assert catalog visibility/navigation for the new runnable layout page.
- Kept `ClippingPage` and `ContentViewPage` as `HiddenUnsupported` in browser host because this slice surfaces additional Mirage backend gaps (`Image`/`Border` paths and related clipping/content rendering behavior), while preserving native-host availability through the shared catalog policy.
- Added a minimal upstream-compat `Headline` style resource in wrapper app resources so imported page XAML that references this key can initialize without requiring full upstream app bootstrap wiring.

## 2026-03-15 (ControlsSample Media/Border Enablement: Image + Border + StackLayout)
- Added first-class Mirage browser handler support for `Image` and `Border` (`MirageImageHandler`, `MirageBorderHandler`) with corresponding semantic/provider expansion (`ComponentKind.Image`, `ComponentKind.Border`) in the Fluent provider.
- Added a shared image-source projection path for `FileImageSource`/`UriImageSource`/`FontImageSource` and projected image aspect/opaque metadata via control-node properties and CSS/object-fit styles.
- Extended layout compatibility for imported upstream pages by registering `StackLayout` on `MirageLayoutHandler`, mapping `StackLayout.Orientation` and `StackLayout.Spacing`, and honoring the `IsClippedToBounds` alias in addition to `ClipsToBounds`.
- Promoted `ClippingPage` and `ContentViewPage` to `Runnable` in the ControlsSample catalog/browser host now that required backend control paths are in place.
- Updated the ControlsSample page tracker to visual status markers (`[x]`, `[ ]`, `[!]`, `[R]`, `[P]`) for faster parity/backlog scanning.

## 2026-03-16 (Mirage Bundled Image Asset Pipeline Wave 1)
- Browser hosts now collect bundled `MauiImage` assets from both the host project and the full transitive `ProjectReference` graph via shared `Directory.Build.targets` targets (`MirageCollectBundledImagesForHost` and `MiragePrepareBundledImagesForBrowserHosts`).
- Build output now deterministically publishes bundled images under `wwwroot/_mirage/assets/{Assembly}/{logical-dir}/{name}.{hash8}{ext}` and writes a stable manifest at `wwwroot/_mirage/assets/images.manifest.v1.json` containing identity and alias maps.
- `FileImageSource` resolution is centralized behind `IMirageImageSourceResolver` (`MirageManifestImageSourceResolver`), with startup manifest load, deterministic degraded mode on manifest failures, explicit ambiguity handling, and no handler-side browser-path construction.
- ControlsSample browser image behavior now uses the shared pipeline; the old `ControlsSample.Fluent` image-mirroring target was removed.

## 2026-03-16 (ControlsSample ContentView Parity: Border Child Clipping)
- `MirageBorderHandler` now applies clipping semantics for supported border shapes (`Rectangle`/`RoundRectangle`) by projecting `overflow: hidden` on border hosts, so border-contained content cannot bleed outside shaped bounds.
- Handler connect ordering now applies border stroke/clipping styles after the content-container base styles, preventing base-style resets from silently clearing border clipping.
- `MirageBoxViewHandler` now applies shared background mapping first and treats `BoxView.Color` as an explicit override only when set, so `BackgroundColor`-driven BoxView scenarios (including `ContentViewPage` card dividers) no longer get overwritten to transparent.
- Added regression coverage at both levels: integration asserts border clipping and BoxView color precedence behavior, and ControlsSample E2E asserts card avatar hosts in `ContentViewPage` expose clipping (`overflow: hidden`) with non-zero corner radius plus visible divider rendering.

## 2026-03-16 (ControlsSample ContentView Parity: Avatar Framing + Title Vertical Alignment)
- `MirageImageHandler` now respects explicit `WidthRequest`/`HeightRequest` on `Image` before defaulting to fill sizing, preventing requested image sizes from being clobbered during handler connect and restoring expected framing for `CardView` avatars (`100x100` with negative margins).
- Image requested-dimension projection now reconciles through shared size-request state mapping before applying image fill fallback (`100%` when width/height requests are unset), so subsequent `Minimum*`/`Maximum*` updates cannot clear image width/height unexpectedly.
- `MirageLabelHandler` now maps `HorizontalTextAlignment` and `VerticalTextAlignment` semantics (`text-align` + flex `justify-content`) and uses `display:flex` as its visible baseline so vertical text centering works deterministically.
- Shared thickness projection now distinguishes margin vs padding semantics: margins preserve signed values, while padding remains non-negative-clamped. This restores MAUI negative-margin behavior used by upstream `CardView` avatar framing.
- Added integration + ControlsSample E2E guards for avatar geometry (`~100x100` with `-20px` offsets) and first-card title centerline alignment against the icon center.

## 2026-03-16 (ControlsSample ButtonPage Enablement + Mirage-Owned Button Composition)
- Promoted upstream `ButtonPage` to a runnable ControlsSample wrapper entry and imported its required upstream dependencies (`BoolToCustomValueConverter`, `ThicknessConverter`, plus a minimal `CustomButton` compatibility type in `Maui.Controls.Sample`).
- Shifted Fluent-provider button realization from `fluent-button` to a Mirage-owned button shell (`button` host + dedicated text and image sub-elements), so MAUI button content semantics are not constrained by a web-component-native content model.
- Extended Mirage core/runtime property plumbing with secondary control elements plus style-target property mapping and checked-value event/property support, enabling deterministic sub-element routing for button text/image projection and two-way checkbox state updates.
- Expanded Mirage browser control surface with first-class `CheckBox` and `Slider` handlers (with runtime event flow), and broadened `MirageButtonHandler` mapping to cover ButtonPage scenarios: image source/content-layout projection, spacing updates, line-break mode projection, padding, tooltip projection, and existing stroke/background/text behaviors.

## 2026-03-16 (ControlsSample Slider + CheckBox Slice with Slider ThumbImage Parity)
- Promoted upstream `CheckBoxPage` and `SliderPage` to runnable ControlsSample entries and added deterministic page-factory automation IDs for key controls used by E2E scenarios.
- Completed `MirageCheckBoxHandler` parity paths needed by upstream pages: compatibility `Color` alias support alongside `Foreground`, two-way `IsChecked`, and deterministic disabled semantics (`disabled` projection + interaction blocking).
- Extended `MirageSliderHandler` with disabled-state projection and `ThumbImageSource` resolution through `IMirageImageSourceResolver`, including deterministic clear/reapply behavior for runtime thumb-image toggles.
- Added framework-level slider realization styles in Mirage runtime (`mirage-dom.js`) using projected slider style channels (`--mirage-slider-track-gradient`, `--mirage-slider-thumb-color`, `--mirage-slider-thumb-image`) so thumb-image rendering is owned by Mirage platform plumbing rather than sample host CSS.

## 2026-03-16 (ControlsSample HorizontalStackLayoutPage Enablement)
- Promoted upstream `HorizontalStackLayoutPage` to a runnable ControlsSample entry using existing Mirage `HorizontalStackLayout` and `BoxView` support, with no new runtime/provider contracts required.
- Imported upstream page XAML/code-behind into `ControlsSample.Shared` and added ControlsSample E2E coverage for page discoverability plus horizontal child flow assertions (label + six `BoxView` children with left-to-right ordering and projected colors).

## 2026-03-16 (HorizontalStackLayoutPage Follow-up: BoxView Default Size Parity)
- While enabling `HorizontalStackLayoutPage`, we found a browser parity gap where unconstrained `BoxView` children could collapse to zero size in stack layouts.
- `MirageBoxViewHandler` now applies MAUI-aligned default minimum sizing (`40px`) only when no explicit width/height/min/max requests are set, preserving explicit size requests and avoiding sample-specific layout hacks.
- Added integration coverage for this fallback and clear-on-explicit-size behavior so future handler changes do not regress default `BoxView` visibility.

## 2026-03-16 (ControlsSample BoxViewPage Enablement + Shared Shadow Mapping)
- Promoted upstream `BoxViewPage` to a runnable ControlsSample entry and added deterministic automation IDs for each BoxView demo scenario so parity checks stay stable.
- Expanded `MirageBoxViewHandler` with `CornerRadius` projection (uniform and per-corner) while preserving existing `Color` vs. `Background` precedence and default-size fallback behavior.
- Implemented `IView.Shadow` projection in shared Mirage view mapping so BoxView shadow behavior flows through the same framework-level path used by other controls, with deterministic fallback (clear + diagnostics) for unsupported non-solid shadow brushes.

## 2026-03-16 (BoxView Native Sizing Parity Correction)
- Removed Mirage's synthetic `40px` default minimum sizing for unconstrained `BoxView` instances in `MirageBoxViewHandler`.
- Native MAUI behavior observed on iOS and Android allows unsized `BoxView` elements to collapse in layouts like `HorizontalStackLayoutPage`; Mirage now follows that behavior instead of forcing visibility.
- BoxView sizing is now driven only by explicit MAUI size requests (`WidthRequest`, `HeightRequest`, `Minimum*Request`, `Maximum*Request`) through shared size mapping.
- Updated ControlsSample HorizontalStackLayout E2E expectations to assert node presence with collapsed geometry rather than visible colored boxes.

## 2026-03-16 (ControlsSample VerticalStackLayoutPage Enablement)
- Promoted upstream `VerticalStackLayoutPage` to a runnable ControlsSample entry using existing Mirage `VerticalStackLayout` and `BoxView` support, with no new runtime/provider contracts required.
- Imported upstream page XAML/code-behind into `ControlsSample.Shared` and added ControlsSample E2E coverage for page discoverability.
- Aligned E2E parity expectations with native MAUI behavior (confirmed on iOS/Android): unsized `BoxView` children collapse rather than render visible colored blocks.

## 2026-03-17 (ControlsSample StackLayoutPage Enablement)
- Promoted upstream `StackLayoutPage` to a runnable ControlsSample entry using existing Mirage `StackLayout` and `BoxView` support, with no new runtime/provider contracts required.
- Imported upstream page XAML/code-behind into `ControlsSample.Shared` and extended ControlsSample E2E coverage for catalog discoverability plus page-level structure checks (`Vertical` and `Horizontal` sections with six `BoxView` children each).
- Kept parity expectations aligned to native MAUI behavior: unsized `BoxView` children in both sections are expected to collapse rather than render with synthetic default sizes.

## 2026-03-17 (ControlsSample CustomLayoutPage Enablement)
- Promoted upstream `CustomLayoutPage` to a runnable ControlsSample entry and linked its upstream XAML/code-behind into `ControlsSample.Shared`.
- Added a framework-level generic `Layout` realization path in Mirage (`ComponentKind.Layout`) so custom `Layout`-derived views (like upstream `DockLayout`) can run without sample-specific handler glue.
- `MirageLayoutHandler` now falls back to generic layout mode for non-built-in layouts and projects arranged child frames as absolute child styles (`left/top/width/height`) after MAUI cross-platform arrange, preserving MAUI custom-layout manager semantics in browser host runs.
- Extended ControlsSample E2E coverage to verify `CustomLayoutPage` docked button arrangement and updated the page status tracker to mark `CustomLayoutPage.xaml` as runnable + E2E covered.
