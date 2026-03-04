using System.Globalization;
using Maui.Browser.Dom;
using Maui.Browser.Utils;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;

namespace Maui.Browser.Handlers;

internal static class BrowserViewMapper
{
    public static void ApplyFrame(DomElement platformView, Rect frame)
    {
        platformView.Style("position", "absolute");
        platformView.Style("left", CssUtil.Px(frame.X));
        platformView.Style("top", CssUtil.Px(frame.Y));
        platformView.Style("width", CssUtil.Px(Math.Max(0, frame.Width)));
        platformView.Style("height", CssUtil.Px(Math.Max(0, frame.Height)));
        platformView.Style("boxSizing", "border-box");
    }

    public static void MapVisibility(IView view, DomElement platformView)
    {
        platformView.Style("display", view.Visibility == Visibility.Visible ? string.Empty : "none");
    }

    public static void MapOpacity(IView view, DomElement platformView)
    {
        platformView.Style("opacity", view.Opacity.ToString("0.###", CultureInfo.InvariantCulture));
    }

    public static void MapEnabled(IView view, DomElement platformView)
    {
        if (view.IsEnabled)
        {
            platformView.Attr("disabled", null);
            platformView.Attr("aria-disabled", null);
            platformView.Style("pointerEvents", string.Empty);
            return;
        }

        platformView.Attr("disabled", "disabled");
        platformView.Attr("aria-disabled", "true");
        platformView.Style("pointerEvents", "none");
    }

    public static void MapPadding(IPadding view, DomElement platformView)
    {
        var padding = view.Padding;
        var cssPadding = $"{CssUtil.Px(Math.Max(0, padding.Top))} {CssUtil.Px(Math.Max(0, padding.Right))} {CssUtil.Px(Math.Max(0, padding.Bottom))} {CssUtil.Px(Math.Max(0, padding.Left))}";
        platformView.Style("padding", cssPadding);
    }

    public static void MapTextColor(ITextStyle style, DomElement platformView)
    {
        var cssColor = CssUtil.ToCssColor(style.TextColor);
        platformView.Style("color", cssColor);
    }

    public static void MapFont(ITextStyle style, DomElement platformView, double fallbackSize = 14)
    {
        platformView.Style("font", CssUtil.ToCssFont(style.Font, fallbackSize));
    }

    public static void MapCharacterSpacing(ITextStyle style, DomElement platformView)
    {
        if (double.IsNaN(style.CharacterSpacing))
        {
            platformView.Style("letterSpacing", string.Empty);
            return;
        }

        platformView.Style("letterSpacing", CssUtil.Px(style.CharacterSpacing));
    }

    public static void MapTextAlignment(ITextAlignment textAlignment, DomElement platformView)
    {
        var horizontal = textAlignment.HorizontalTextAlignment switch
        {
            TextAlignment.Center => "center",
            TextAlignment.End => "right",
            TextAlignment.Justify => "justify",
            _ => "left"
        };

        platformView.Style("textAlign", horizontal);
    }
}
