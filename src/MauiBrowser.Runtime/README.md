# MauiBrowser.Runtime

Runtime services for rendering MAUI virtual views into a Blazor component/DOM tree.

## Current Scope (M1)

- pluggable renderer registry (`IRendererRegistry`)
- MAUI view renderer (`IMauiViewRenderer`)
- baseline `Label` renderer (`MauiLabelRendererFactory`)
- baseline `StackLayout` renderer (`MauiStackLayoutRendererFactory`, CSS flex)
- baseline `Grid` renderer (`MauiGridRendererFactory`, CSS grid)
- DI registration via `AddMauiBrowserRuntime(IServiceCollection)`

## Usage

Register services:

```csharp
builder.Services.AddMauiBrowserRuntime();
```

Render a MAUI view:

```csharp
@inject MauiBrowser.Runtime.Rendering.IMauiViewRenderer Renderer
@using Microsoft.Maui.Controls

@Renderer.Render(new Label { Text = "Hello from MAUI." })
```
