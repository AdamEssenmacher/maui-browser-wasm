# AGENTS.md — Contributor & Agent Operating Guide

This repository builds a third-party Browser/WASM DOM backend for .NET MAUI,
hosted inside a Blazor WebAssembly app. The project is incremental: every
milestone must produce a runnable sample in `samples/`.

See also:
- `docs/CONSTRAINTS.md` (non-negotiable invariants + guardrails)
- `.agents/skills/` (repeatable playbooks)

## Prime Directive
Always keep the repo in a working state. Prefer small, verifiable changes over
large refactors. If unsure, add a spike under `docs/spikes/` rather than
shipping partial implementations.

## Project Invariants (do not change without an ADR)
1. Host runtime is **Blazor WebAssembly** (do not invent a custom host/bootloader).
2. Rendering is **Blazor component-driven DOM** (DOM-first). JS interop is allowed but must be batched.
3. Layout strategy prefers **CSS flex/grid**. Absolute positioning is allowed only behind a documented flag
   or for specific controls with a rationale.
4. Renderers must be **pluggable**. Third-party libraries (e.g., MudBlazor) can override control renderers
   via DI/registry without forking the core.
5. Accessibility is tracked from day 1: every rendered control must expose basic semantics
   (even if full parity is deferred).

## Ground Rules
1. No breaking changes to public APIs without an ADR under `docs/DECISIONS/`.
2. Every feature must have:
   - a sample demonstrating it OR a unit/integration test proving it
   - documentation updates (README or docs)
3. One PR = one idea. Avoid mixing refactors with features.
4. Avoid forking upstream (dotnet/maui, dotnet/sdk) unless blocked. If blocked:
   - document the block
   - propose a workaround
   - add an ADR justifying any fork/patch

## Coding Standards
- Nullable enabled everywhere
- Treat warnings as errors (repo-wide)
- Public APIs must be documented (`///` XML)
- Prefer interfaces at boundaries: rendering, interop, services

## Milestone Discipline
Work must align to the current milestone in `docs/ROADMAP.md`.
If a task doesn't fit, create a new milestone entry or a spike doc.

## Definition of Done (for any milestone item)
- `dotnet build` succeeds at repo root
- sample runs and visually demonstrates the change
- CI passes
- docs updated

## Repo Commands
- Build: `dotnet build`
- Test: `dotnet test`
- Run sample: see `samples/HelloWorld/README.md`

## What NOT to do
- Do not attempt to implement everything at once.
- Do not introduce new frameworks without an ADR.
- Do not silently skip hard problems (IME, accessibility, virtualization). Track them.
