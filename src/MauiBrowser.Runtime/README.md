# MauiBrowser.Runtime

Runtime services for rendering MAUI virtual views into a Blazor component/DOM tree.

## Current Scope

- pluggable renderer registry (`IRendererRegistry`)
- MAUI view renderer (`IMauiViewRenderer`)
- baseline `Label` renderer (`MauiLabelRendererFactory`)
- baseline `Button` renderer (`MauiButtonRendererFactory`, click + command)
- baseline `StackLayout` renderer (`MauiStackLayoutRendererFactory`, CSS flex)
- baseline `Grid` renderer (`MauiGridRendererFactory`, CSS grid)
- baseline `CollectionView` prototype renderer (`MauiCollectionViewRendererFactory`, item template + capped materialization)
- styling bridge v0 (`RenderContextFactory` maps baseline MAUI style properties into `StyleTokens` CSS variables)
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
