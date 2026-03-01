using Microsoft.AspNetCore.Components;

namespace MauiBrowser.Runtime.Rendering;

/// <summary>
/// Renders MAUI virtual views into Blazor render fragments.
/// </summary>
public interface IMauiViewRenderer
{
    /// <summary>
    /// Creates a render fragment for a MAUI virtual view instance.
    /// </summary>
    /// <param name="virtualView">The MAUI virtual view instance to render.</param>
    /// <returns>A Blazor render fragment for the provided view.</returns>
    RenderFragment Render(object virtualView);
}
