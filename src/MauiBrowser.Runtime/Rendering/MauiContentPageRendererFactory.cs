using System;
using Microsoft.AspNetCore.Components;
using Microsoft.Maui.Controls;

namespace MauiBrowser.Runtime.Rendering;

/// <summary>
/// Renderer factory for <see cref="ContentPage"/> controls.
/// </summary>
public sealed class MauiContentPageRendererFactory : IRendererFactory
{
    private readonly IRendererRegistry _rendererRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="MauiContentPageRendererFactory"/> class.
    /// </summary>
    /// <param name="rendererRegistry">Renderer registry used to resolve the page content renderer.</param>
    public MauiContentPageRendererFactory(IRendererRegistry rendererRegistry)
    {
        _rendererRegistry = rendererRegistry ?? throw new ArgumentNullException(nameof(rendererRegistry));
    }

    /// <inheritdoc />
    public RenderFragment Create(object virtualView, RenderContext context)
    {
        ArgumentNullException.ThrowIfNull(virtualView);
        ArgumentNullException.ThrowIfNull(context);

        if (virtualView is not ContentPage contentPage)
        {
            throw new ArgumentException("The provided view is not a MAUI ContentPage.", nameof(virtualView));
        }

        return builder =>
        {
            var sequence = 0;
            builder.OpenElement(sequence++, "div");

            var classes = DomRenderStyleBuilder.BuildClassValue(context.StyleTokens.CssClasses);
            if (!string.IsNullOrWhiteSpace(classes))
            {
                builder.AddAttribute(sequence++, "class", classes);
            }

            var style = DomRenderStyleBuilder.BuildStyleValue(context.StyleTokens.InlineStyles, context.StyleTokens.CssVariables);
            if (!string.IsNullOrWhiteSpace(style))
            {
                builder.AddAttribute(sequence++, "style", style);
            }

            if (!string.IsNullOrWhiteSpace(context.Semantics.Role))
            {
                builder.AddAttribute(sequence++, "role", context.Semantics.Role);
            }

            if (!string.IsNullOrWhiteSpace(context.Semantics.Label))
            {
                builder.AddAttribute(sequence++, "aria-label", context.Semantics.Label);
            }

            if (context.Semantics.Disabled)
            {
                builder.AddAttribute(sequence++, "aria-disabled", "true");
            }

            if (contentPage.Content is not null)
            {
                if (!_rendererRegistry.TryResolve(contentPage.Content, out var childFactory))
                {
                    throw new InvalidOperationException(
                        $"No renderer factory registered for child view type '{contentPage.Content.GetType().FullName}' in ContentPage.");
                }

                var childContext = RenderContextFactory.CreateFor(contentPage.Content);
                builder.AddContent(sequence++, childFactory.Create(contentPage.Content, childContext));
            }

            builder.CloseElement();
        };
    }

    /// <inheritdoc />
    public bool CanRender(object virtualView) => virtualView is ContentPage;
}
