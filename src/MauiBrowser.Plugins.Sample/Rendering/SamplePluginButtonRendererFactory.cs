using System;
using System.Collections.Generic;
using System.Windows.Input;
using MauiBrowser.Runtime.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.Maui.Controls;

namespace MauiBrowser.Plugins.Sample.Rendering;

/// <summary>
/// Sample plugin renderer factory that overrides MAUI button rendering.
/// </summary>
public sealed class SamplePluginButtonRendererFactory : IRendererFactory
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
            builder.AddAttribute(sequence++, "data-maui-plugin", "sample-button");

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
                builder.AddAttribute(sequence++, "disabled", true);
                builder.AddAttribute(sequence++, "aria-disabled", "true");
            }
            else
            {
                builder.AddAttribute(sequence++, "onclick", EventCallback.Factory.Create(button, () => DispatchClick(button)));
            }

            builder.AddContent(sequence++, $"[Sample Plugin] {button.Text ?? string.Empty}");
            builder.CloseElement();
        };
    }

    /// <inheritdoc />
    public bool CanRender(object virtualView) => virtualView is Button;

    private static string BuildClassValue(IReadOnlyCollection<string> classes)
    {
        var allClasses = new List<string>(classes.Count + 1)
        {
            "maui-plugin-button"
        };

        foreach (var cssClass in classes)
        {
            if (!string.IsNullOrWhiteSpace(cssClass))
            {
                allClasses.Add(cssClass);
            }
        }

        return string.Join(" ", allClasses);
    }

    private static string? BuildStyleValue(
        IReadOnlyDictionary<string, string> inlineStyles,
        IReadOnlyDictionary<string, string> cssVariables)
    {
        if (inlineStyles.Count == 0 && cssVariables.Count == 0)
        {
            return null;
        }

        var fragments = new List<string>(inlineStyles.Count + cssVariables.Count);
        foreach (var pair in inlineStyles)
        {
            fragments.Add($"{pair.Key}:{pair.Value}");
        }

        foreach (var pair in cssVariables)
        {
            fragments.Add($"--{pair.Key}:{pair.Value}");
        }

        return string.Join(";", fragments);
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
