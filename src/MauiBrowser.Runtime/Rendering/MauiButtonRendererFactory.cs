using System;
using System.Windows.Input;
using Microsoft.AspNetCore.Components;
using Microsoft.Maui.Controls;

namespace MauiBrowser.Runtime.Rendering;

/// <summary>
/// Renderer factory for <see cref="Button"/> controls.
/// </summary>
public sealed class MauiButtonRendererFactory : IRendererFactory
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

        return builder =>
        {
            var sequence = 0;
            builder.OpenElement(sequence++, "button");
            builder.AddAttribute(sequence++, "type", "button");

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

            var isDisabled = context.Semantics.Disabled
                || !button.IsEnabled
                || !CanExecuteCommand(button.Command, button.CommandParameter);
            if (isDisabled)
            {
                builder.AddAttribute(sequence++, "disabled", true);
                builder.AddAttribute(sequence++, "aria-disabled", "true");
            }
            else
            {
                builder.AddAttribute(sequence++, "onclick", EventCallback.Factory.Create(button, () => DispatchClick(button)));
            }

            builder.AddContent(sequence++, button.Text ?? string.Empty);
            builder.CloseElement();
        };
    }

    /// <inheritdoc />
    public bool CanRender(object virtualView) => virtualView is Button;

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
