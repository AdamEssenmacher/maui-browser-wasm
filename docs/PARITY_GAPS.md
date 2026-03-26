# MAUI Browser Parity Gaps (Behavior-First)

This document tracks explicit, intentional parity gaps for the browser backend.
Every gap listed here must have deterministic behavior and regression tests.
Upstream MAUI shared-code risks for unofficial/browser targets are tracked separately in [UPSTREAM_TARGET_RISKS.md](UPSTREAM_TARGET_RISKS.md).

## Current gaps

1. CollectionView virtualization and full templated-items parity
- Current behavior: non-virtualized realization with deterministic incremental `Add`/`Remove`/`Move`/`Replace`/`Reset`, plus header/footer/empty view and `ScrollToRequested` support through the generic runtime child-scroll seam.
- Deferred: virtualization, grouped realization, sticky headers, reorder UX, and advanced anchoring behavior under large-data churn.

2. Selection behavior on CollectionView
- Current behavior: `SelectionMode.None|Single|Multiple` is supported with pointer-selection updates and equality-based `SelectedItem`/`SelectedItems` synchronization.
- Deferred: full keyboard-modifier selection semantics, richer selected visual state parity, and grouped-selection behavior.

3. CollectionView sizing/scroll mode/reorder parity depth
- Current behavior: `ItemSizingStrategy`, grouped templates, reorder, and `ItemsUpdatingScrollMode.KeepItemsInView` are key-mapped with deterministic diagnostics; `KeepItemsInView` currently falls back to `KeepScrollOffset`.
- Deferred: full item-size strategy parity, grouped template realization, reorder interactions, and complete `KeepItemsInView` anchoring semantics.

4. RefreshView native affordance parity depth
- Current behavior: `RefreshView` supports command/state-first interaction plus pull-drag threshold activation on scroll-top content (`Content`, `IsRefreshing`, `IsRefreshEnabled`, `RefreshColor`, `Command`, `CommandParameter`).
- Deferred: platform-native overscroll elasticity/physics and deeper affordance parity beyond the current deterministic browser indicator row.

5. Clip geometry coverage
- Current behavior: `RectangleGeometry` and `RoundRectangleGeometry` project to CSS `clip-path`.
- Deferred: `EllipseGeometry`, `PathGeometry`, `GeometryGroup`, and other geometry types remain unsupported fallback paths with DEBUG diagnostics.

6. Shadow paint coverage
- Current behavior: shadow projection supports solid paint via CSS `box-shadow`.
- Deferred: non-solid shadow paint (gradient/image/pattern) remains unsupported fallback with DEBUG diagnostics.

7. Background paint coverage
- Current behavior: solid and linear-gradient paints are mapped to DOM background styles.
- Deferred: radial/image/pattern paints remain unsupported fallback with DEBUG diagnostics.

8. Label line-break truncation parity depth
- Current behavior: `NoWrap`, `WordWrap`, `CharacterWrap`, and truncation modes map to deterministic CSS white-space/overflow behaviors, with `MaxLines` projected through line-clamp styles and clamp-aware visibility projection.
- Deferred: head/middle truncation visual parity and platform-specific truncation glyph placement remain best-effort.

9. Text-input parity depth (`Entry` / `Editor` / `SearchBar`)
- Current behavior: `Entry` maps text/placeholder/placeholder-color, read-only, keyboard hinting, max-length, spell-check, prediction, clear-button semantics, return/complete events, and cursor-position/selection-length synchronization in both directions on the ControlNode path.
- Current behavior: `Entry` suppresses Enter-submit while browser composition is active (`compositionstart`/`compositionend`) to preserve text-input semantics during IME entry.
- Current behavior: `SearchBar` maps text/placeholder/placeholder-color, read-only, keyboard hinting, max-length, spell-check, prediction, return/search events, and deterministic browser clear/search button interactions on the ControlNode path.
- Deferred: IME composition edge-case parity for text inputs and native icon-channel fidelity for `SearchBar` (`CancelButtonColor`/`SearchIconColor` projected to button text-color channels) remain partial.

10. Date/time format parity depth
- Current behavior: `DatePicker` and `TimePicker` map to browser-native date/time inputs using deterministic ISO wire formats.
- Deferred: full custom MAUI format token parity across all browser locales is not complete in this slice.

11. Numeric/status control parity depth
- Current behavior: `Stepper`, `ProgressBar`, and `ActivityIndicator` handlers are implemented with deterministic browser projections and two-way state updates.
- Deferred: native long-press stepper repeat behavior and activity-indicator visual parity are best-effort in this slice.

12. Media source parity depth (`Image` / `ImageButton`)
- Current behavior: `UriImageSource`, `FileImageSource`, and `FontImageSource` project to DOM `<img src=...>` with deterministic source-kind + load-state attributes.
- Deferred: stream/image-source service pipeline parity (including cancellation semantics and decode caching) remains unsupported fallback in this slice.

13. Border parity depth
- Current behavior: single-child hosting, solid stroke color/thickness, round-rectangle/rectangle shape mapping, and dashed best-effort stroke style are projected to CSS.
- Deferred: full stroke-shape and stroke-paint parity (ellipse/path/geometry groups, non-solid stroke paint, line-cap/join/miter exact behavior) remains partial.

14. FlexLayout parity depth
- Current behavior: semantic CSS flex projection is implemented for container properties (`Direction`, `Wrap`, `JustifyContent`, `AlignItems`, `AlignContent`) and item attached properties (`Order`, `Grow`, `Shrink`, `Basis`, `AlignSelf`).
- Current behavior: nested `FlexLayout` children with `Position=Absolute` are projected through frame-driven geometry (`left`/`top`/`width`/`height`) during arrange, while relative flex children stay semantic.
- Current behavior: runtime `Position` toggles for nested `FlexLayout` children deterministically reproject between absolute-frame and flex-item styles (`order`/`grow`/`shrink`/`basis`/`align-self`) without child-handler churn.
- Upstream MAUI constraint: `FlexLayout.Position` is valid only for `FlexLayout` children; non-`FlexLayout` controls do not participate in the absolute-position path.

15. Font-load synchronized measurement
- Current behavior: text measurement is canvas-based with deterministic fallback.
- Deferred: async font readiness synchronization and re-measure strategy.

16. Browser-hard controls not yet implemented
- Current behavior: no browser handlers yet for controls marked `missing` in `PARITY_MATRIX.md`.
- Deferred: implementation waves by family per roadmap.

17. Shell
- Current behavior: out of scope by decision.
- Deferred: no Shell implementation planned in this parity program.

18. MAUI style-system parity depth
- Current behavior: showcase track proves implicit/explicit styles, `BasedOn`, `StyleClass`, runtime `DynamicResource` theme switching, and common control state styling (`Pressed`/`Disabled`/`Focused`) across mapped browser handlers.
- Deferred: advanced trigger precedence edge cases, full VisualState parity across all control families, and complex style/resource merge-order compatibility are still partial.

19. Label text-source parity depth (`FormattedText` / `TextType` / `TextTransform`)
- Current behavior: browser `Label` mapping includes MAUI-aligned core style primitives (`CharacterSpacing`, `TextDecorations`, `LineHeight`, `Padding`), wrap/clamp projection (`LineBreakMode`, `MaxLines`), and controls text-source projection for `FormattedText`, `TextType`, and `TextTransform`.
- Current behavior: `TextType.Html` uses a deterministic safe-subset sanitizer; unsupported tags/attributes are stripped while preserving text content.
- Deferred: full HTML breadth parity and span-level gesture hit-region parity for formatted text remain partial in this slice.

20. IndicatorView parity depth
- Current behavior: `IndicatorView` is mapped through a dedicated Mirage handler with MAUI-aligned mapper keys (`Count`, `Position`, `HideSingle`, `MaximumVisible`, `IndicatorSize`, `IndicatorColor`, `SelectedIndicatorColor`, `IndicatorsShape`) and deterministic default-dot projection on the ControlNode path.
- Current behavior: template hosting is supported through `ITemplatedIndicatorView.IndicatorsLayoutOverride` with a stable host pipeline that suppresses default dots while template layout is active.
- Deferred: non-solid indicator paint parity and deeper templated-indicator interaction parity remain partial; unsupported paints currently fall back deterministically to default solid colors.

21. Shapes handler parity depth (`Shapes.*`)
- Current behavior: all `Shapes.*` rows (`Ellipse`, `Line`, `Path`, `Polygon`, `Polyline`, `Rectangle`, `RoundRectangle`) are mapped through MAUI-shaped Mirage handlers on the built-in `ControlNode` path with stable SVG-host realization.
- Current behavior: deterministic geometry projection, aspect updates, stroke option mapping (dash/cap/join/miter), and solid/linear/radial paint projection are implemented; image brush fill/stroke projection resolves through `IMirageImageSourceResolver`.
- Current behavior: `ControlsSample.Shared/App/ShapesParityProbePage.cs` is the parity probe surface with E2E coverage for runtime shape switching plus points/path-data/path-transform mutation flows.
- Deferred: graphics-native `PatternPaint`/`ImagePaint`, exact native arc fidelity, and non-translate `Path.RenderTransform` types are still divergence-documented deterministic fallback/flattened paths in this slice.

## Gap lifecycle policy

1. Any new gap requires:
- Entry in this file
- Corresponding `partial` or `gap-documented` status in `PARITY_MATRIX.md`
- Tests that assert fallback behavior (not just happy path behavior)

2. Any resolved gap requires:
- Removal or update in this file
- Status update in `PARITY_MATRIX.md`
- Passing regression tests proving parity behavior
