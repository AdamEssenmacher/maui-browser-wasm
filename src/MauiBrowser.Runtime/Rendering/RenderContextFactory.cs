using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Input;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace MauiBrowser.Runtime.Rendering;

internal static class RenderContextFactory
{
    private const string BackgroundColorVar = "maui-background-color";
    private const string TextColorVar = "maui-text-color";
    private const string FontSizeVar = "maui-font-size";
    private const string FontFamilyVar = "maui-font-family";
    private const string OpacityVar = "maui-opacity";
    private const string MarginVar = "maui-margin";
    private const string BorderColorVar = "maui-border-color";
    private const string BorderWidthVar = "maui-border-width";
    private const string BorderRadiusVar = "maui-border-radius";
    private const string PaddingVar = "maui-padding";

    public static RenderContext CreateFor(object virtualView)
    {
        ArgumentNullException.ThrowIfNull(virtualView);

        var cssVariables = BuildCssVariables(virtualView);
        var inlineStyles = BuildInlineStyles(virtualView, cssVariables);
        var semanticsLabel = virtualView switch
        {
            Label label => label.Text,
            Button button => button.Text,
            _ => null
        };
        var semanticsDisabled = virtualView switch
        {
            Button button => !button.IsEnabled && !IsCommandDisabled(button),
            VisualElement visualElement => !visualElement.IsEnabled,
            _ => false
        };
        var semanticsRole = virtualView is Button ? "button" : null;

        return new RenderContext
        {
            StyleTokens = new StyleTokens
            {
                CssVariables = cssVariables,
                InlineStyles = inlineStyles
            },
            Semantics = new Semantics
            {
                Label = semanticsLabel,
                Disabled = semanticsDisabled,
                Role = semanticsRole
            },
            Events = EventDispatcher.None
        };
    }

    private static Dictionary<string, string> BuildCssVariables(object virtualView)
    {
        var cssVariables = new Dictionary<string, string>(StringComparer.Ordinal);

        if (virtualView is VisualElement visualElement)
        {
            if (TryConvertColor(visualElement.BackgroundColor, out var backgroundColor))
            {
                cssVariables[BackgroundColorVar] = backgroundColor;
            }

            if (!AreClose(visualElement.Opacity, 1d))
            {
                cssVariables[OpacityVar] = ToCssNumber(visualElement.Opacity);
            }

            if (virtualView is View view && HasNonZeroThickness(view.Margin))
            {
                cssVariables[MarginVar] = ToCssThickness(view.Margin);
            }
        }

        switch (virtualView)
        {
            case Label label:
                AddTextStyleVariables(label.TextColor, label.FontSize, label.FontFamily, cssVariables);
                break;
            case Button button:
                AddTextStyleVariables(button.TextColor, button.FontSize, button.FontFamily, cssVariables);

                if (TryConvertColor(button.BorderColor, out var borderColor))
                {
                    cssVariables[BorderColorVar] = borderColor;
                }

                if (button.BorderWidth >= 0 && double.IsFinite(button.BorderWidth))
                {
                    cssVariables[BorderWidthVar] = ToCssPx(button.BorderWidth);
                }

                if (button.CornerRadius >= 0)
                {
                    cssVariables[BorderRadiusVar] = ToCssPx(button.CornerRadius);
                }

                if (IsFiniteThickness(button.Padding))
                {
                    cssVariables[PaddingVar] = ToCssThickness(button.Padding);
                }

                break;
        }

        return cssVariables;
    }

    private static Dictionary<string, string> BuildInlineStyles(
        object virtualView,
        IReadOnlyDictionary<string, string> cssVariables)
    {
        var inlineStyles = new Dictionary<string, string>(StringComparer.Ordinal);

        AddVariableStyle(BackgroundColorVar, "background-color", cssVariables, inlineStyles);
        AddVariableStyle(TextColorVar, "color", cssVariables, inlineStyles);
        AddVariableStyle(FontSizeVar, "font-size", cssVariables, inlineStyles);
        AddVariableStyle(FontFamilyVar, "font-family", cssVariables, inlineStyles);
        AddVariableStyle(OpacityVar, "opacity", cssVariables, inlineStyles);
        AddVariableStyle(MarginVar, "margin", cssVariables, inlineStyles);

        if (virtualView is Button)
        {
            AddVariableStyle(BorderColorVar, "border-color", cssVariables, inlineStyles);
            AddVariableStyle(BorderWidthVar, "border-width", cssVariables, inlineStyles);
            AddVariableStyle(BorderRadiusVar, "border-radius", cssVariables, inlineStyles);
            AddVariableStyle(PaddingVar, "padding", cssVariables, inlineStyles);

            if (cssVariables.ContainsKey(BorderColorVar) || cssVariables.ContainsKey(BorderWidthVar))
            {
                inlineStyles["border-style"] = "solid";
            }
        }

        return inlineStyles;
    }

    private static void AddTextStyleVariables(
        Color? textColor,
        double fontSize,
        string? fontFamily,
        IDictionary<string, string> cssVariables)
    {
        if (TryConvertColor(textColor, out var cssTextColor))
        {
            cssVariables[TextColorVar] = cssTextColor;
        }

        if (fontSize > 0 && double.IsFinite(fontSize))
        {
            cssVariables[FontSizeVar] = ToCssPx(fontSize);
        }

        if (!string.IsNullOrWhiteSpace(fontFamily))
        {
            cssVariables[FontFamilyVar] = fontFamily;
        }
    }

    private static void AddVariableStyle(
        string variableName,
        string propertyName,
        IReadOnlyDictionary<string, string> cssVariables,
        IDictionary<string, string> inlineStyles)
    {
        if (cssVariables.ContainsKey(variableName))
        {
            inlineStyles[propertyName] = $"var(--{variableName})";
        }
    }

    private static bool HasNonZeroThickness(Thickness thickness)
    {
        return !AreClose(thickness.Left, 0)
            || !AreClose(thickness.Top, 0)
            || !AreClose(thickness.Right, 0)
            || !AreClose(thickness.Bottom, 0);
    }

    private static bool IsFiniteThickness(Thickness thickness)
    {
        return double.IsFinite(thickness.Left)
            && double.IsFinite(thickness.Top)
            && double.IsFinite(thickness.Right)
            && double.IsFinite(thickness.Bottom);
    }

    private static bool IsCommandDisabled(Button button)
    {
        return button.Command is not null && !CanExecuteCommand(button.Command, button.CommandParameter);
    }

    private static bool CanExecuteCommand(ICommand? command, object? commandParameter)
    {
        return command is null || command.CanExecute(commandParameter);
    }

    private static bool TryConvertColor(Color? color, out string cssColor)
    {
        if (color is null)
        {
            cssColor = string.Empty;
            return false;
        }

        cssColor = ToCssColor(color);
        return true;
    }

    private static string ToCssColor(Color color)
    {
        var red = ClampToByte(color.Red * 255d);
        var green = ClampToByte(color.Green * 255d);
        var blue = ClampToByte(color.Blue * 255d);
        var alpha = Math.Clamp(color.Alpha, 0d, 1d);

        return $"rgba({red},{green},{blue},{ToCssNumber(alpha)})";
    }

    private static int ClampToByte(double value)
    {
        var rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
        return Math.Clamp(rounded, 0, 255);
    }

    private static string ToCssThickness(Thickness thickness)
    {
        return $"{ToCssPx(thickness.Top)} {ToCssPx(thickness.Right)} {ToCssPx(thickness.Bottom)} {ToCssPx(thickness.Left)}";
    }

    private static string ToCssPx(double value)
    {
        return $"{ToCssNumber(value)}px";
    }

    private static string ToCssNumber(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static bool AreClose(double left, double right)
    {
        return Math.Abs(left - right) < 0.0001d;
    }
}
