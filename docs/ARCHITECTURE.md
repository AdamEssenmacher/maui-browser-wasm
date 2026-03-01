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

## Current M1 Slice
- Runtime includes a minimal pluggable renderer pipeline:
  - `IRendererRegistry` + `RendererRegistry` with priority-based resolution
  - `IRendererFactory` contract
  - `IMauiViewRenderer` + `MauiViewRenderer` orchestration
  - `MauiLabelRendererFactory` for `Microsoft.Maui.Controls.Label`
- Sample integration:
  - `samples/HelloWorld` registers runtime services via DI
  - `/maui` route renders a real MAUI `Label` via runtime -> Blazor DOM pipeline
- Accessibility baseline:
  - label semantics can project to `aria-label` through `RenderContext.Semantics`
- Deferred to follow-up:
  - SDK composition work in `MauiBrowser.Sdk`
  - additional controls/layout mappings beyond `Label`

## Key Packages
- `MauiBrowser.Runtime`: runtime platform backend + handler set + services (Browser)
- `MauiBrowser.Sdk`: MSBuild SDK that composes MAUI + Blazor WASM build pipeline
- `MauiBrowser.Interop`: JS interop batching + DOM helpers
- `MauiBrowser.Plugins.*`: optional renderer packs (MudBlazor, etc.)

## Non-Goals (initially)
- Full parity with native MAUI controls
- Perfect performance for CollectionView virtualization
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
