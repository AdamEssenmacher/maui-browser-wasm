# M5 Plan: Text Input and IME Handling

## Goal
Define a v1 implementation plan for robust text input behavior in browser-hosted MAUI controls, including Input Method Editor (IME) composition flows.

## Scope
- `Entry` and `Editor` renderer behavior in `MauiBrowser.Runtime`.
- Text changes, selection state, and command/event dispatch flow.
- IME composition lifecycle handling for East Asian and other composition-based input methods.

## Non-Goals (This Slice)
- No production-complete text editing parity in this milestone.
- No rich text features.
- No mobile virtual keyboard tuning beyond basic browser defaults.

## Constraints
- Must preserve project invariants from `docs/CONSTRAINTS.md`:
  - Blazor WebAssembly host.
  - DOM-first rendering via Blazor components/render fragments.
  - Batched JS interop only (no per-keystroke JS round trips when avoidable).
- Must keep renderers pluggable through the existing registry.

## Current Baseline
- `Entry` is rendered and interactive through the runtime pipeline.
- M5 collection and navigation slices are complete; this plan is focused on text fidelity and IME correctness next.

## Problem Statement
Browser text input has two overlapping event models:
- Standard text updates (`input`, `change`, keyboard events).
- IME composition (`compositionstart`, `compositionupdate`, `compositionend`), where intermediate text should not be treated as final committed input.

If we treat all intermediate composition updates as final values, MAUI state, validation, and command behavior become unstable for IME users.

## Behavioral Targets
1. Composition-safe updates:
   - Track composition state per control.
   - Do not treat composing text as committed model state until composition completes.
2. Deterministic value flow:
   - Keep DOM text value and MAUI view text synchronized without oscillation.
   - Avoid write-back loops when MAUI updates text programmatically.
3. Predictable events:
   - Continue to raise expected MAUI-side text change signals.
   - Keep behavior stable for both IME and non-IME typing.
4. Accessibility-safe interaction:
   - Preserve disabled, label, and focus semantics while handling text input.

## Proposed Architecture
1. Renderer event model
   - Keep core text handling in renderer factories (`Entry`, future `Editor`).
   - Maintain lightweight per-element input state:
     - `IsComposing`
     - `LastCommittedText`
     - optional selection snapshot fields for future parity
2. Commit semantics
   - `input` during composition updates renderer-local transient state only.
   - `compositionend` commits final text into MAUI virtual view state/event dispatch.
   - Non-IME typing commits on `input`.
3. JS interop strategy
   - Default path: Blazor event pipeline without JS interop for plain text updates.
   - Interop only for capabilities not exposed by Blazor event args (future selection/caret APIs), and then only batched.
4. Update loop guard
   - Skip DOM write when incoming MAUI value equals current DOM value.
   - Defer programmatic DOM writes while composing unless explicitly required.

## Phased Delivery Plan
1. Phase 1: IME-safe `Entry` commit semantics
   - Add composition tracking and commit behavior.
   - Add unit tests for IME and non-IME flow.
2. Phase 2: Extend to `Editor`
   - Reuse shared text input state logic.
   - Add multiline-specific tests.
3. Phase 3: Selection and caret parity spike
   - Document required interop surface and batching approach.
4. Phase 4: Validation and regression hardening
   - Expand manual matrix across OS/browser/IME combinations.

## Test Plan (Required for Implementation Slice)
- Unit tests:
  - Non-IME input commits immediately.
  - Composition start/update does not commit final value.
  - Composition end commits exactly once with final text.
  - Disabled control ignores input/commit events.
  - Programmatic MAUI text updates do not cause feedback loops.
- Manual checks:
  - English keyboard typing.
  - Japanese IME conversion.
  - Chinese Pinyin composition.
  - Korean Hangul composition.

## Risks and Mitigations
- Risk: browser-specific composition event ordering differences.
  - Mitigation: conservative commit-on-`compositionend` policy and cross-browser validation.
- Risk: regressions in existing simple input behavior.
  - Mitigation: preserve current non-IME event path and add regression tests.
- Risk: excessive JS interop pressure.
  - Mitigation: keep interop out of baseline text commit path.

## Exit Criteria for "IME Plan Documented"
- This plan exists in-repo and is linked from roadmap.
- It defines scope, event semantics, phases, test coverage, and risks.
- It is specific enough to drive the next implementation PR without additional discovery.
