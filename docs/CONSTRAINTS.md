# Constraints & Guardrails

This document is the project's "constitution". Changes require an ADR.

## Hard Constraints
- Host runtime is **Blazor WebAssembly**.
- Rendering is **component-driven DOM** (Blazor components/RenderFragments).
- Prefer **CSS flex/grid** for layout translation.
  - Absolute positioning may be used only behind a documented flag or for specific controls.
- JS interop must be **batched**:
  - No per-property-change JS calls during normal rendering.
  - Prefer single interop calls per render batch/frame.
- Accessibility is tracked from day 1:
  - All interactive controls must set basic semantics (role/label/disabled).
  - Track gaps explicitly in docs/issues; do not ignore.

## Engineering Guardrails
- Small PRs: keep changes reviewable.
- Repo must remain buildable:
  - Always run `dotnet build` before finishing work.
  - Samples must remain runnable.
- Dependencies:
  - No new major dependencies without an ADR.
  - Prefer built-in .NET/Blazor capabilities first.
- Public API changes require:
  - docs update
  - ADR if breaking or architectural
