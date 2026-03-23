# MAUI Browser Parity Gaps (Behavior-First)

This document tracks explicit, intentional parity gaps for the browser backend.
Every gap listed here must have deterministic behavior and regression tests.

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
- Current behavior: text/placeholder/placeholder-color, read-only, keyboard hinting, max-length, spell-check, prediction, and return/search events are handler-mapped.
- Deferred: cursor-position/selection-length mapping, IME composition edge cases, and native search-icon/cancel-button parity remain partial.

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
- Current behavior: browser `Label` mapping now includes MAUI-aligned core style primitives (`CharacterSpacing`, `TextDecorations`, `LineHeight`, `Padding`) plus controls remap keys for wrap/clamp (`LineBreakMode`, `MaxLines`) while keeping plain-text projection authoritative (`Label.Text` when set, otherwise deterministic flattened span text).
- Current behavior: controls-only text-source features (`FormattedText`, `TextType`, `TextTransform`) use deterministic unsupported fallbacks with diagnostics and stable plain-text output.
- Deferred: span-level formatted text projection, HTML text rendering semantics (`TextType.Html`), and controls-layer text-transform parity are explicitly gap-documented in this slice.

## Gap lifecycle policy

1. Any new gap requires:
- Entry in this file
- Corresponding `partial` or `gap-documented` status in `PARITY_MATRIX.md`
- Tests that assert fallback behavior (not just happy path behavior)

2. Any resolved gap requires:
- Removal or update in this file
- Status update in `PARITY_MATRIX.md`
- Passing regression tests proving parity behavior
