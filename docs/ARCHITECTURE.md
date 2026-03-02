# Architecture Overview

## Goal
Run a .NET MAUI UI tree in the browser by rendering it to DOM via Blazor components hosted in a Blazor WebAssembly app.

## Invariants
- Host runtime: **Blazor WebAssembly**
- Rendering: **Blazor component-driven DOM** (DOM-first)
- Layout: prefer **CSS flex/grid**, with absolute layout only as a documented escape hatch
- Extensibility: renderers must be **pluggable** (e.g., MudBlazor can replace core renderers)
- Interop: JS interop must be **batched**

## Pipeline
MAUI Virtual View Tree
  -> Browser Handlers (map VirtualView to renderer intent + behavior)
  -> Renderer Registry (selects component renderers, pluggable)
  -> Blazor Component Tree (RenderFragments / IComponents)
  -> DOM (Blazor diffing)

## Current Implementation Status
- Runtime includes a minimal pluggable renderer pipeline:
  - `IRendererRegistry` + `RendererRegistry` with priority-based resolution
  - `IRendererFactory` contract
  - `IMauiViewRenderer` + `MauiViewRenderer` orchestration
  - `MauiLabelRendererFactory` for `Microsoft.Maui.Controls.Label`
  - `MauiButtonRendererFactory` for `Microsoft.Maui.Controls.Button` (`<button>`, click + command)
  - `MauiStackLayoutRendererFactory` for `Microsoft.Maui.Controls.StackLayout` (`display:flex`)
  - `MauiGridRendererFactory` for `Microsoft.Maui.Controls.Grid` (`display:grid`)
  - `MauiCollectionViewRendererFactory` for `Microsoft.Maui.Controls.CollectionView` (prototype: item template rendering with capped materialization)
  - `MauiContentPageRendererFactory` for `Microsoft.Maui.Controls.ContentPage`
  - `MauiNavigationPageRendererFactory` for `Microsoft.Maui.Controls.NavigationPage`
  - `RenderContextFactory` style bridge v0 mapping MAUI style properties to CSS variables via `StyleTokens`
- Sample integration:
  - `samples/HelloWorld` registers runtime services via DI
  - `samples/HelloWorld` applies plugin overrides at startup (`MauiBrowser.Plugins.MudBlazor`) to demonstrate higher-priority renderer replacement
  - `/maui` route renders real MAUI `Label`, `StackLayout`, `Grid`, `CollectionView`, `Button`, `Entry`, and `NavigationPage` via runtime -> Blazor DOM pipeline with style-token to CSS variable projection
  - `/maui/{*stackPath}` uses slash-separated route keys as the source of truth for MAUI navigation stack state (`/maui` root, arbitrary pushed depth up to current v0 cap)
- Accessibility baseline:
  - label and button semantics can project to role/label/disabled attributes through `RenderContext.Semantics`
- SDK composition:
  - `MauiBrowser.Sdk` now imports `Microsoft.NET.Sdk.BlazorWebAssembly` props/targets
  - default runtime reference wiring is provided through SDK properties/targets
  - target framework validation defaults to `net10.0` with configurable opt-outs
- Deferred to follow-up:
  - additional controls/layout mappings beyond `Label`, `Button`, `StackLayout`, and `Grid`
  - richer style parity and advanced theming beyond styling bridge v0

## Handler Bridge Path (M6)
M6 shifts the project from "renderer-only proof" to "handler-path feasibility validation" without replacing the existing runtime pipeline yet.

### Why Hybrid Bridge (Not Immediate Rewrite)
- Current renderer pipeline is already working, test-covered, and plugin-extensible.
- MAUI handler contracts (`IElementHandler`, `IMauiContext`, `IMauiHandlersFactory`, mappers) introduce lifecycle and service assumptions that need controlled validation in browser host constraints.
- A hybrid bridge allows incremental migration and regression isolation per control.

### Migration Stages and Compatibility Guarantees
1. Stage A: feasibility and service inventory (docs + probes only).
2. Stage B: activate handler bridge for one simple control behind compatibility fallback.
3. Stage C: expand control-by-control where handler path is validated.
4. Stage D: keep renderer fallback for unsupported or blocked surfaces.

Compatibility guarantees for M6+:
- Existing `IMauiViewRenderer` entrypoint remains stable.
- Existing `IRendererRegistry`/`IRendererFactory` plugin override model remains valid.
- No big-bang cutover of all controls.

### Plugin Override Implications Under Bridge
- Plugins that currently register higher-priority renderers continue to work for controls still on renderer path.
- As controls migrate to handler-backed path, precedence rules must be explicit (plugin renderer override vs handler-backed default).
- Bridge work must document deterministic precedence and avoid mixed ambiguous resolution.

### M6 Non-Goals
- No claim of full MAUI platform target parity in this milestone.
- No immediate multi-window parity for browser host.
- No full input/IME parity or virtualization parity; these are explicit de-risk tracks.
- No SDK/package target rearchitecture in this slice (runtime-first focus).

## Styling Bridge v0 Tokens
- `--maui-background-color`
- `--maui-text-color`
- `--maui-font-size`
- `--maui-font-family`
- `--maui-opacity`
- `--maui-margin`
- `--maui-border-color` (button)
- `--maui-border-width` (button)
- `--maui-border-radius` (button)
- `--maui-padding` (button)

## Navigation v0 Behavior
- Scope: `NavigationPage` only. Shell is intentionally out of scope in this milestone.
- Route model:
  - Root route: `/maui` (implicit root page key)
  - Pushed pages: `/maui/{*stackPath}` with slash-separated keys (for example `/maui/details/settings/profile`)
  - Known keys in sample: `details`, `settings`, `profile` (case-insensitive normalization)
- State model:
  - Browser URL is authoritative
  - Sample command handlers issue `NavigationManager.NavigateTo(...)`
  - `OnParametersSetAsync` parses route keys, then synchronizes `NavigationPage` by pop-to-common-prefix then push-remaining
  - Unknown keys render a MAUI NotFound page and preserve the requested URL (no redirect rewrite)

## Plugin Override Model (M4)
- Runtime `RendererRegistry` resolves the matching renderer with highest priority; later registrations only win ties.
- Third-party/plugin packages can register renderers into the same registry and override core renderers without forking runtime code.
- In-repo proof packages:
  - `MauiBrowser.Plugins.Sample`
  - registers `SamplePluginButtonRendererFactory` for `Button` at higher priority than core default
  - `MauiBrowser.Plugins.MudBlazor`
  - registers `MudBlazorButtonRendererFactory` (`Button`) and `MudBlazorEntryRendererFactory` (`Entry`) at higher priority for sample runtime composition
  - sample app applies MudBlazor registration at startup so rendered MAUI button/entry controls visibly demonstrate third-party overrides

## Key Packages
- `MauiBrowser.Runtime`: runtime platform backend + handler set + services (Browser)
- `MauiBrowser.Sdk`: MSBuild SDK that composes MAUI + Blazor WASM build pipeline
- `MauiBrowser.Interop`: JS interop batching + DOM helpers
- `MauiBrowser.Plugins.*`: optional renderer packs (MudBlazor, etc.)

## Non-Goals (initially)
- Full parity with native MAUI controls
- Perfect performance for CollectionView virtualization (M5 v0 decision: no virtualization yet; see `docs/spikes/M5-collectionview-virtualization-decision.md`)
- Complete accessibility parity (but we must provide basic semantics from day 1)

## Core Interfaces
These remain intentionally small and evolve incrementally.

```csharp
namespace MauiBrowser.Runtime.Rendering;

public interface IRendererRegistry
{
    void Register<TVirtualView>(IRendererFactory factory, int priority = 0);
    bool TryResolve(object virtualView, out IRendererFactory factory);
}

public interface IRendererFactory
{
    /// Creates a Blazor render fragment for the provided view and context.
    Microsoft.AspNetCore.Components.RenderFragment Create(object virtualView, RenderContext context);
    bool CanRender(object virtualView);
}

public interface IMauiViewRenderer
{
    Microsoft.AspNetCore.Components.RenderFragment Render(object virtualView);
}

public sealed class RenderContext
{
    public required StyleTokens StyleTokens { get; init; }
    public required Semantics Semantics { get; init; }
    public required EventDispatcher Events { get; init; }
}

public sealed class StyleTokens
{
    public IReadOnlyDictionary<string, string> CssVariables { get; init; }
    public IReadOnlyDictionary<string, string> InlineStyles { get; init; }
    public IReadOnlyCollection<string> CssClasses { get; init; }
}

public sealed class Semantics
{
    public string? Role { get; init; }
    public string? Label { get; init; }
    public bool Disabled { get; init; }
}

public sealed class EventDispatcher
{
    // Placeholder for command/callback wiring.
}
```

## Layout Approach (initial)
- Translate MAUI layouts to CSS:
  - StackLayout -> flex column/row
  - Grid -> CSS grid
- Use absolute layout only for:
  - explicit AbsoluteLayout, or
  - documented edge-case controls where CSS flow cannot meet requirements.

## Accessibility (baseline)
- For each interactive control, set basic role/label/disabled semantics.
- Track gaps explicitly and do not silently omit semantics.
