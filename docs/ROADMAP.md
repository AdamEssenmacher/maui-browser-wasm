# Roadmap

## M0 — Repo Skeleton
**Goal:** establish repo structure + CI + runnable sample.

**Proof (runnable):**
- `samples/HelloWorld` runs and shows the index page at `/` with “Hello, browser.”

**Definition of done:**
- `dotnet build` passes at repo root
- `dotnet test` passes at repo root
- CI passes on PR
- sample runs

- [x] Solution + projects layout
- [x] CI: build/test
- [x] Sample: Blazor WASM "Hello"

## M1 — MAUI Host in Blazor WASM
**Goal:** render a minimal MAUI visual tree inside a Blazor WASM app.

**Proof (runnable):**
- `samples/HelloWorld` shows a MAUI-rendered label on `/maui` (or equivalent route).

**Scope exclusions (explicit):**
- No CollectionView, no IME fidelity, no full accessibility parity
- But: interactive elements must expose basic semantics; label must render text.

**Definition of done:**
- sample renders a MAUI Label on screen
- renderer pipeline exists (even if only for Label)
- architecture doc updated with pipeline + core interfaces

- [x] `MauiBrowser.Runtime` runtime package
- [x] `MauiBrowser.Sdk` MSBuild SDK package (composes Blazor WASM pipeline)
- [x] Minimal renderer registry + Label renderer
- [x] Sample: MAUI Label rendered

## M2 — Core Controls & Layout (v0)
**Proof (runnable):**
- sample page demonstrates StackLayout + Grid + Button click.

**Scope exclusions:**
- No advanced styling system; just basic mapping to CSS vars/classes.

- [x] StackLayout (CSS flex)
- [x] Grid (CSS grid)
- [ ] Button (click + command)
- [ ] Styling bridge v0 (CSS variables)

## M3 — Navigation (v0)
- [ ] NavigationPage or minimal Shell support
- [ ] Back stack + URL considerations documented

## M4 — Plugin System
**Proof (runnable):**
- sample uses a plugin package to override Button rendering.

- [ ] Renderer registry priorities
- [ ] Sample plugin: MudBlazor overrides Button/Entry

## M5 — Hard Controls Spike
- [ ] CollectionView prototype + virtualization decision
- [ ] Text input/IME plan documented
- [ ] Accessibility audit checklist started
