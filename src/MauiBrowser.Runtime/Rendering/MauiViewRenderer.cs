using System;
using Microsoft.AspNetCore.Components;
using Microsoft.Maui.Controls;

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

        return factory.Create(virtualView, CreateContext(virtualView));
    }

    private static RenderContext CreateContext(object virtualView)
    {
        var semanticsLabel = virtualView is Label label ? label.Text : null;

        return new RenderContext
        {
            StyleTokens = StyleTokens.Empty,
            Semantics = new Semantics
            {
                Label = semanticsLabel
            },
            Events = EventDispatcher.None
        };
    }
}
