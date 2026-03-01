using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Maui.Controls;

namespace MauiBrowser.Runtime.Rendering;

/// <summary>
/// Renderer factory for <see cref="StackLayout"/> controls.
/// </summary>
public sealed class MauiStackLayoutRendererFactory : IRendererFactory
{
    private readonly IRendererRegistry _rendererRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="MauiStackLayoutRendererFactory"/> class.
    /// </summary>
    /// <param name="rendererRegistry">Renderer registry used to render child views.</param>
    public MauiStackLayoutRendererFactory(IRendererRegistry rendererRegistry)
    {
        _rendererRegistry = rendererRegistry ?? throw new ArgumentNullException(nameof(rendererRegistry));
    }

    /// <inheritdoc />
    public RenderFragment Create(object virtualView, RenderContext context)
    {
        ArgumentNullException.ThrowIfNull(virtualView);
        ArgumentNullException.ThrowIfNull(context);

        if (virtualView is not StackLayout stackLayout)
        {
            throw new ArgumentException("The provided view is not a MAUI StackLayout.", nameof(virtualView));
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

            var inlineStyles = new Dictionary<string, string>(context.StyleTokens.InlineStyles)
            {
                ["display"] = "flex",
                ["flex-direction"] = stackLayout.Orientation == StackOrientation.Horizontal ? "row" : "column"
            };

            if (stackLayout.Spacing > 0)
            {
                inlineStyles["gap"] = $"{stackLayout.Spacing.ToString(CultureInfo.InvariantCulture)}px";
            }

            var style = DomRenderStyleBuilder.BuildStyleValue(inlineStyles, context.StyleTokens.CssVariables);
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

            foreach (var child in stackLayout.Children)
            {
                if (!_rendererRegistry.TryResolve(child, out var childFactory))
                {
                    throw new InvalidOperationException(
                        $"No renderer factory registered for child view type '{child.GetType().FullName}' in StackLayout.");
                }

                var childContext = RenderContextFactory.CreateFor(child, context.Semantics.Disabled);
                builder.AddContent(sequence++, childFactory.Create(child, childContext));
            }

            builder.CloseElement();
        };
    }

    /// <inheritdoc />
    public bool CanRender(object virtualView) => virtualView is StackLayout;
}
