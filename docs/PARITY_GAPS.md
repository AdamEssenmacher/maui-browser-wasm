# MAUI Browser Parity Gaps (Behavior-First)

This document tracks explicit, intentional parity gaps for the browser backend.
Every gap listed here must have deterministic behavior and regression tests.

## Current gaps

1. CollectionView virtualization and full templated-items parity
- Current behavior: non-virtualized realization with `Add`/`Remove`/`Move`/`Replace`/`Reset`, plus header/footer and `ScrollTo` support.
- Deferred: virtualization, grouping, sticky headers, and advanced scroll/anchoring behavior under large-data churn.

2. Selection behavior on CollectionView
- Current behavior: `SelectionMode.None|Single|Multiple` is supported with pointer-selection updates and MAUI `SelectedItem`/`SelectedItems` synchronization.
- Deferred: full keyboard-modifier selection semantics, richer selected visual state parity, and grouped-selection behavior.

3. RefreshView native affordance parity depth
- Current behavior: `RefreshView` supports command/state-first interaction plus pull-drag threshold activation on scroll-top content (`Content`, `IsRefreshing`, `IsRefreshEnabled`, `RefreshColor`, `Command`, `CommandParameter`).
- Deferred: platform-native overscroll elasticity/physics and deeper affordance parity beyond the current deterministic browser indicator row.

4. Clip geometry coverage
- Current behavior: `RectangleGeometry` and `RoundRectangleGeometry` project to CSS `clip-path`.
- Deferred: `EllipseGeometry`, `PathGeometry`, `GeometryGroup`, and other geometry types remain unsupported fallback paths with DEBUG diagnostics.

5. Shadow paint coverage
- Current behavior: shadow projection supports solid paint via CSS `box-shadow`.
- Deferred: non-solid shadow paint (gradient/image/pattern) remains unsupported fallback with DEBUG diagnostics.

6. Background paint coverage
- Current behavior: solid and linear-gradient paints are mapped to DOM background styles.
- Deferred: radial/image/pattern paints remain unsupported fallback with DEBUG diagnostics.

7. Label line-break truncation parity depth
- Current behavior: `NoWrap`, `WordWrap`, `CharacterWrap`, and truncation modes map to CSS white-space/overflow behaviors, with `MaxLines` projected through line-clamp styles.
- Deferred: head/middle truncation visual parity and platform-specific truncation glyph placement remain best-effort.

8. Text-input parity depth (`Entry` / `Editor` / `SearchBar`)
- Current behavior: text/placeholder/placeholder-color, read-only, keyboard hinting, max-length, spell-check, prediction, and return/search events are handler-mapped.
- Deferred: cursor-position/selection-length mapping, IME composition edge cases, and native search-icon/cancel-button parity remain partial.

9. Date/time format parity depth
- Current behavior: `DatePicker` and `TimePicker` map to browser-native date/time inputs using deterministic ISO wire formats.
- Deferred: full custom MAUI format token parity across all browser locales is not complete in this slice.

10. Numeric/status control parity depth
- Current behavior: `Stepper`, `ProgressBar`, and `ActivityIndicator` handlers are implemented with deterministic browser projections and two-way state updates.
- Deferred: native long-press stepper repeat behavior and activity-indicator visual parity are best-effort in this slice.

11. Media source parity depth (`Image` / `ImageButton`)
- Current behavior: `UriImageSource`, `FileImageSource`, and `FontImageSource` project to DOM `<img src=...>` with deterministic source-kind + load-state attributes.
- Deferred: stream/image-source service pipeline parity (including cancellation semantics and decode caching) remains unsupported fallback in this slice.

12. Border parity depth
- Current behavior: single-child hosting, solid stroke color/thickness, round-rectangle/rectangle shape mapping, and dashed best-effort stroke style are projected to CSS.
- Deferred: full stroke-shape and stroke-paint parity (ellipse/path/geometry groups, non-solid stroke paint, line-cap/join/miter exact behavior) remains partial.

13. FlexLayout parity depth
- Current behavior: semantic CSS flex projection is implemented for container properties (`Direction`, `Wrap`, `JustifyContent`, `AlignItems`, `AlignContent`) and item attached properties (`Order`, `Grow`, `Shrink`, `Basis`, `AlignSelf`).
- Deferred: full `FlexLayout.Position` parity and advanced edge semantics across mixed absolute/relative flex children remain partial.

14. Font-load synchronized measurement
- Current behavior: text measurement is canvas-based with deterministic fallback.
- Deferred: async font readiness synchronization and re-measure strategy.

15. Browser-hard controls not yet implemented
- Current behavior: no browser handlers yet for controls marked `missing` in `PARITY_MATRIX.md`.
- Deferred: implementation waves by family per roadmap.

16. Shell
- Current behavior: out of scope by decision.
- Deferred: no Shell implementation planned in this parity program.

## Gap lifecycle policy

1. Any new gap requires:
- Entry in this file
- Corresponding `partial` or `gap-documented` status in `PARITY_MATRIX.md`
- Tests that assert fallback behavior (not just happy path behavior)

2. Any resolved gap requires:
- Removal or update in this file
- Status update in `PARITY_MATRIX.md`
- Passing regression tests proving parity behavior
