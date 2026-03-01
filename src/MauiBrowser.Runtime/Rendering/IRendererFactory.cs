using Microsoft.AspNetCore.Components;

namespace MauiBrowser.Runtime.Rendering;

/// <summary>
/// Creates Blazor render fragments for virtual view instances.
/// </summary>
public interface IRendererFactory
{
    /// <summary>
    /// Creates a render fragment for the provided virtual view and rendering context.
    /// </summary>
    /// <param name="virtualView">The virtual view instance to render.</param>
    /// <param name="context">Rendering context for semantics, style tokens, and events.</param>
    /// <returns>A render fragment representing the virtual view.</returns>
    RenderFragment Create(object virtualView, RenderContext context);

    /// <summary>
    /// Determines whether this factory can render the supplied virtual view instance.
    /// </summary>
    /// <param name="virtualView">The virtual view instance to inspect.</param>
    /// <returns><c>true</c> when the factory supports the view; otherwise, <c>false</c>.</returns>
    bool CanRender(object virtualView);
}
