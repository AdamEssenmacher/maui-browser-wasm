# MAUI Browser Parity Gaps (Behavior-First)

This document tracks explicit, intentional parity gaps for the browser backend.
Every gap listed here must have deterministic behavior and regression tests.

## Current gaps

1. CollectionView virtualization and full templated-items parity
- Current behavior: non-virtualized realization with `Add`/`Remove`/`Reset`; `Move`/`Replace` rebuild fallback.
- Deferred: virtualization, grouping, header/footer, advanced scroll APIs, full selection model.

2. Selection behavior on CollectionView
- Current behavior: `SelectionMode != None` is treated as unsupported in this slice and emits DEBUG diagnostics.
- Deferred: behavior parity for selected item(s), events, visual states.

3. Clip geometry coverage
- Current behavior: `RectangleGeometry` and `RoundRectangleGeometry` project to CSS `clip-path`.
- Deferred: `EllipseGeometry`, `PathGeometry`, `GeometryGroup`, and other geometry types remain unsupported fallback paths with DEBUG diagnostics.

4. Shadow paint coverage
- Current behavior: shadow projection supports solid paint via CSS `box-shadow`.
- Deferred: non-solid shadow paint (gradient/image/pattern) remains unsupported fallback with DEBUG diagnostics.

5. Background paint coverage
- Current behavior: solid and linear-gradient paints are mapped to DOM background styles.
- Deferred: radial/image/pattern paints remain unsupported fallback with DEBUG diagnostics.

6. Label line-break truncation parity depth
- Current behavior: `NoWrap`, `WordWrap`, `CharacterWrap`, and truncation modes map to CSS white-space/overflow behaviors, with `MaxLines` projected through line-clamp styles.
- Deferred: head/middle truncation visual parity and platform-specific truncation glyph placement remain best-effort.

7. Text-input parity depth (`Entry` / `Editor` / `SearchBar`)
- Current behavior: text/placeholder/placeholder-color, read-only, keyboard hinting, max-length, spell-check, prediction, and return/search events are handler-mapped.
- Deferred: cursor-position/selection-length mapping, IME composition edge cases, and native search-icon/cancel-button parity remain partial.

8. Date/time format parity depth
- Current behavior: `DatePicker` and `TimePicker` map to browser-native date/time inputs using deterministic ISO wire formats.
- Deferred: full custom MAUI format token parity across all browser locales is not complete in this slice.

9. Numeric/status control parity depth
- Current behavior: `Stepper`, `ProgressBar`, and `ActivityIndicator` handlers are implemented with deterministic browser projections and two-way state updates.
- Deferred: native long-press stepper repeat behavior and activity-indicator visual parity are best-effort in this slice.

10. Font-load synchronized measurement
- Current behavior: text measurement is canvas-based with deterministic fallback.
- Deferred: async font readiness synchronization and re-measure strategy.

11. Browser-hard controls not yet implemented
- Current behavior: no browser handlers yet for controls marked `missing` in `PARITY_MATRIX.md`.
- Deferred: implementation waves by family per roadmap.

12. Shell
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
