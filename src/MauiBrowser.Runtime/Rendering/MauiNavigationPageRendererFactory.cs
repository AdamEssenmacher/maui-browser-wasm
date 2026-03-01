using System;
using Microsoft.AspNetCore.Components;
using Microsoft.Maui.Controls;

namespace MauiBrowser.Runtime.Rendering;

/// <summary>
/// Renderer factory for <see cref="NavigationPage"/> controls.
/// </summary>
public sealed class MauiNavigationPageRendererFactory : IRendererFactory
{
    private readonly IRendererRegistry _rendererRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="MauiNavigationPageRendererFactory"/> class.
    /// </summary>
    /// <param name="rendererRegistry">Renderer registry used to resolve the current page renderer.</param>
    public MauiNavigationPageRendererFactory(IRendererRegistry rendererRegistry)
    {
        _rendererRegistry = rendererRegistry ?? throw new ArgumentNullException(nameof(rendererRegistry));
    }

    /// <inheritdoc />
    public RenderFragment Create(object virtualView, RenderContext context)
    {
        ArgumentNullException.ThrowIfNull(virtualView);
        ArgumentNullException.ThrowIfNull(context);

        if (virtualView is not NavigationPage navigationPage)
        {
            throw new ArgumentException("The provided view is not a MAUI NavigationPage.", nameof(virtualView));
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

            var currentPage = navigationPage.CurrentPage;
            if (currentPage is not null)
            {
                if (!_rendererRegistry.TryResolve(currentPage, out var pageFactory))
                {
                    throw new InvalidOperationException(
                        $"No renderer factory registered for page type '{currentPage.GetType().FullName}' in NavigationPage.");
                }

                var childContext = RenderContextFactory.CreateFor(currentPage, context.Semantics.Disabled);
                builder.AddContent(sequence++, pageFactory.Create(currentPage, childContext));
            }

            builder.CloseElement();
        };
    }

    /// <inheritdoc />
    public bool CanRender(object virtualView) => virtualView is NavigationPage;
}
