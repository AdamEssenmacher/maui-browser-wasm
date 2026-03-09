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
