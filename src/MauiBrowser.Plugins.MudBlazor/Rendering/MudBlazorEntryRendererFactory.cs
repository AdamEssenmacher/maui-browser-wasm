using System;
using System.Collections.Generic;
using MauiBrowser.Runtime.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.Maui.Controls;
using MudBlazor;

namespace MauiBrowser.Plugins.MudBlazor.Rendering;

/// <summary>
/// MudBlazor renderer factory override for <see cref="Entry"/> controls.
/// </summary>
public sealed class MudBlazorEntryRendererFactory : IRendererFactory
{
    /// <inheritdoc />
    public RenderFragment Create(object virtualView, RenderContext context)
    {
        ArgumentNullException.ThrowIfNull(virtualView);
        ArgumentNullException.ThrowIfNull(context);

        if (virtualView is not Entry entry)
        {
            throw new ArgumentException("The provided view is not a MAUI Entry.", nameof(virtualView));
        }

        var classes = MudBlazorRenderStyleBuilder.BuildClassValue(context.StyleTokens.CssClasses, "maui-mudblazor-entry");
        var style = MudBlazorRenderStyleBuilder.BuildStyleValue(context.StyleTokens.InlineStyles, context.StyleTokens.CssVariables);
        var userAttributes = BuildUserAttributes(context.Semantics);

        return builder =>
        {
            var sequence = 0;
            builder.OpenComponent<MudTextField<string>>(sequence++);
            builder.AddAttribute(sequence++, "Class", classes);
            builder.AddAttribute(sequence++, "Variant", Variant.Outlined);
            builder.AddAttribute(sequence++, "Immediate", true);
            builder.AddAttribute(sequence++, "InputType", InputType.Text);
            builder.AddAttribute(sequence++, "Disabled", context.Semantics.Disabled);
            builder.AddAttribute(sequence++, "Value", entry.Text ?? string.Empty);
            builder.AddAttribute(
                sequence++,
                "ValueChanged",
                EventCallback.Factory.Create<string>(entry, value => entry.Text = value));

            if (!string.IsNullOrWhiteSpace(style))
            {
                builder.AddAttribute(sequence++, "Style", style);
            }

            if (!string.IsNullOrWhiteSpace(entry.Placeholder))
            {
                builder.AddAttribute(sequence++, "Placeholder", entry.Placeholder);
            }

            if (userAttributes.Count > 0)
            {
                builder.AddAttribute(sequence++, "UserAttributes", userAttributes);
            }

            builder.CloseComponent();
        };
    }

    /// <inheritdoc />
    public bool CanRender(object virtualView) => virtualView is Entry;

    private static Dictionary<string, object> BuildUserAttributes(Semantics semantics)
    {
        var attributes = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["data-maui-plugin"] = "mudblazor-entry"
        };

        if (!string.IsNullOrWhiteSpace(semantics.Label))
        {
            attributes["aria-label"] = semantics.Label;
        }

        if (!string.IsNullOrWhiteSpace(semantics.Role))
        {
            attributes["role"] = semantics.Role;
        }

        return attributes;
    }
}
