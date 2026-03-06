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

3. Clip and shadow mapping
- Current behavior: no-op + DEBUG unsupported warnings in shared view mapping.
- Deferred: clip path and shadow rendering parity.

4. Font-load synchronized measurement
- Current behavior: text measurement is canvas-based with deterministic fallback.
- Deferred: async font readiness synchronization and re-measure strategy.

5. Browser-hard controls not yet implemented
- Current behavior: no browser handlers yet for controls marked `missing` in `PARITY_MATRIX.md`.
- Deferred: implementation waves by family per roadmap.

6. Shell
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
