using System;
using Microsoft.AspNetCore.Components;

namespace MauiBrowser.Runtime.Rendering;

/// <summary>
/// Default MAUI view renderer that resolves a factory from <see cref="IRendererRegistry"/>.
/// </summary>
public sealed class MauiViewRenderer : IMauiViewRenderer
{
    private readonly IRendererRegistry _rendererRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="MauiViewRenderer"/> class.
    /// </summary>
    /// <param name="rendererRegistry">Renderer registry used for view-to-factory resolution.</param>
    public MauiViewRenderer(IRendererRegistry rendererRegistry)
    {
        _rendererRegistry = rendererRegistry ?? throw new ArgumentNullException(nameof(rendererRegistry));
    }

    /// <inheritdoc />
    public RenderFragment Render(object virtualView)
    {
        ArgumentNullException.ThrowIfNull(virtualView);

        if (!_rendererRegistry.TryResolve(virtualView, out var factory))
        {
            throw new InvalidOperationException(
                $"No renderer factory registered for view type '{virtualView.GetType().FullName}'.");
        }

        return factory.Create(virtualView, RenderContextFactory.CreateFor(virtualView));
    }
}
