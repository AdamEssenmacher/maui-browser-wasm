using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.Maui.Controls;

namespace MauiBrowser.Runtime.Rendering;

/// <summary>
/// Renderer factory for <see cref="Label"/> controls.
/// </summary>
public sealed class MauiLabelRendererFactory : IRendererFactory
{
    /// <inheritdoc />
    public RenderFragment Create(object virtualView, RenderContext context)
    {
        ArgumentNullException.ThrowIfNull(virtualView);
        ArgumentNullException.ThrowIfNull(context);

        if (virtualView is not Label label)
        {
            throw new ArgumentException("The provided view is not a MAUI Label.", nameof(virtualView));
        }

        return builder =>
        {
            var sequence = 0;
            builder.OpenElement(sequence++, "span");

            var classes = BuildClassValue(context.StyleTokens.CssClasses);
            if (!string.IsNullOrWhiteSpace(classes))
            {
                builder.AddAttribute(sequence++, "class", classes);
            }

            var style = BuildStyleValue(context.StyleTokens.InlineStyles, context.StyleTokens.CssVariables);
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

            builder.AddContent(sequence++, label.Text ?? string.Empty);
            builder.CloseElement();
        };
    }

    /// <inheritdoc />
    public bool CanRender(object virtualView) => virtualView is Label;

    private static string? BuildClassValue(IReadOnlyCollection<string> classes)
    {
        if (classes.Count == 0)
        {
            return null;
        }

        return string.Join(" ", classes);
    }

    private static string? BuildStyleValue(
        IReadOnlyDictionary<string, string> inlineStyles,
        IReadOnlyDictionary<string, string> cssVariables)
    {
        if (inlineStyles.Count == 0 && cssVariables.Count == 0)
        {
            return null;
        }

        var styleFragments = new List<string>(inlineStyles.Count + cssVariables.Count);
        foreach (var pair in inlineStyles)
        {
            styleFragments.Add($"{pair.Key}:{pair.Value}");
        }

        foreach (var pair in cssVariables)
        {
            styleFragments.Add($"--{pair.Key}:{pair.Value}");
        }

        return string.Join(";", styleFragments);
    }
}
