using System;
using System.Collections.Generic;
using System.Windows.Input;
using MauiBrowser.Runtime.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Maui.Controls;
using MudBlazor;

namespace MauiBrowser.Plugins.MudBlazor.Rendering;

/// <summary>
/// MudBlazor renderer factory override for <see cref="Button"/> controls.
/// </summary>
public sealed class MudBlazorButtonRendererFactory : IRendererFactory
{
    /// <inheritdoc />
    public RenderFragment Create(object virtualView, RenderContext context)
    {
        ArgumentNullException.ThrowIfNull(virtualView);
        ArgumentNullException.ThrowIfNull(context);

        if (virtualView is not Button button)
        {
            throw new ArgumentException("The provided view is not a MAUI Button.", nameof(virtualView));
        }

        var isDisabled = context.Semantics.Disabled;
        var classes = MudBlazorRenderStyleBuilder.BuildClassValue(context.StyleTokens.CssClasses, "maui-mudblazor-button");
        var style = MudBlazorRenderStyleBuilder.BuildStyleValue(context.StyleTokens.InlineStyles, context.StyleTokens.CssVariables);
        var userAttributes = BuildUserAttributes(context.Semantics, isDisabled);

        return builder =>
        {
            var sequence = 0;
            builder.OpenComponent<MudButton>(sequence++);
            builder.AddAttribute(sequence++, "ButtonType", ButtonType.Button);
            builder.AddAttribute(sequence++, "Variant", Variant.Filled);
            builder.AddAttribute(sequence++, "Color", Color.Primary);
            builder.AddAttribute(sequence++, "Class", classes);

            if (!string.IsNullOrWhiteSpace(style))
            {
                builder.AddAttribute(sequence++, "Style", style);
            }

            if (userAttributes.Count > 0)
            {
                builder.AddAttribute(sequence++, "UserAttributes", userAttributes);
            }

            if (isDisabled)
            {
                builder.AddAttribute(sequence++, "Disabled", true);
            }
            else
            {
                builder.AddAttribute(
                    sequence++,
                    "OnClick",
                    EventCallback.Factory.Create<MouseEventArgs>(button, _ => DispatchClick(button)));
            }

            builder.AddAttribute(sequence++, "ChildContent", (RenderFragment)(contentBuilder =>
            {
                contentBuilder.AddContent(0, button.Text ?? string.Empty);
            }));

            builder.CloseComponent();
        };
    }

    /// <inheritdoc />
    public bool CanRender(object virtualView) => virtualView is Button;

    private static Dictionary<string, object> BuildUserAttributes(Semantics semantics, bool isDisabled)
    {
        var attributes = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["data-maui-plugin"] = "mudblazor-button"
        };

        if (!string.IsNullOrWhiteSpace(semantics.Label))
        {
            attributes["aria-label"] = semantics.Label;
        }

        if (!string.IsNullOrWhiteSpace(semantics.Role))
        {
            attributes["role"] = semantics.Role;
        }

        if (isDisabled)
        {
            attributes["aria-disabled"] = "true";
        }

        return attributes;
    }

    private static void DispatchClick(Button button)
    {
        ArgumentNullException.ThrowIfNull(button);

        if (!button.IsEnabled)
        {
            return;
        }

        if (!CanExecuteCommand(button.Command, button.CommandParameter))
        {
            return;
        }

        button.SendClicked();
    }

    private static bool CanExecuteCommand(ICommand? command, object? commandParameter)
    {
        return command is null || command.CanExecute(commandParameter);
    }
}
