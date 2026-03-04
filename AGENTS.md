# AGENTS.md — Guardrails for Codex / Agents

This repository is an experimental skeleton for **making .NET MAUI run on WebAssembly with the DOM as the platform UI**.

## Core vision (non-negotiable)
1. **DOM is the “platform UI.”**
   - Platform views are DOM nodes.
   - Styling is CSS.
   - Input is Pointer/Keyboard events.
   - Accessibility maps to ARIA.
2. **Use MAUI’s handler architecture, not external emulation.**
   - Implement `IViewHandler`/`ViewHandler<,>`-style handlers for a Browser platform.
   - Property updates flow through MAUI mappers into platform views.
   - Do not create a parallel “MAUI-like” component system outside MAUI.
3. **Avoid forking MAUI if at all possible.**
   - Prefer extension points: `ConfigureMauiHandlers`, DI services, conditional compilation.
   - If a MAUI internal seam blocks progress, document it and propose the smallest upstreamable change.
4. **No “canvas-drawn UI” approach.**
   - Do not implement a Skia/canvas renderer here.
   - The goal is DOM parity and web-native semantics.
5. **Razor is optional; do not require `.razor` files.**
   - The host should be a plain `net10.0-browser` app. You may use Blazor runtime packages if needed for hosting, but do not build a component UI layer in Blazor.

## Preferred implementation strategy
- **PlatformView type**: `DomElement` (wrapper over a DOM `nint` handle).
- **Interop**: all JS interop goes through `DomApi` (`JSImport`).
- **Handlers**: implement minimal set first:
  - `Label` → `<span>` / `<div>`
  - `Button` → `<button>`
  - `Entry` → `<input type="text">`
  - Container (`ContentView`, `Layout`) → `<div>` that hosts children
- **Layout**:
  - Start with correctness: set `position:absolute` and apply MAUI-arranged rectangles to DOM styles (`left/top/width/height`).
  - Later optimize: map certain layouts to Flex/Grid when semantics match.
- **Measurement**:
  - Add a `BrowserTextMeasurer` (canvas `measureText`) for Label.
  - Use a hidden measurement root for complex styled elements if required.
  - Beware async font loading: measurement must handle “fonts not loaded yet”.

## Anti-goals / traps to avoid (what failed last time)
- Do **not** start from a plain Blazor UI project and “rebuild MAUI controls” as Blazor components.
- Do **not** create a renderer per control that merely reads properties and attempts to mimic behavior without being a true MAUI platform backend.
- Do **not** bake platform mapping logic into the sample host app; keep it in `Maui.Browser`.

## Development workflow expectations
- Keep changes small and incremental.
- Every significant change should update `docs/DECISIONS.md` (add a short entry).
- Prefer deterministic behavior over “magic” auto-measure.
- Add at least one minimal test or demo per capability:
  - property updates
  - click/tap event
  - resize
  - visibility/opacity

## “Definition of Done” for the first vertical slice
A single page with:
- `Label` (text updates live)
- `Entry` (typing updates bound state)
- `Button` (click triggers handler)
- Layout responds to browser resize
- No `.razor` files involved
