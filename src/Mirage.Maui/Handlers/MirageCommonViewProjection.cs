using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal static class MirageCommonViewProjection
{
    private const double DefaultTextFallbackFontSize = 14;

    private static readonly ConditionalWeakTable<MiragePlatformView, RequestedSizeState> RequestedSizeStates = new();

    public static bool MapAutomationId(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformHandler(handler, out var platformHandler))
        {
            return false;
        }

        var nextAutomationId = string.IsNullOrWhiteSpace(view.AutomationId) ? null : view.AutomationId;
        if (string.Equals(platformHandler.MiragePlatformView.GetPropertyString("AutomationId"), nextAutomationId, StringComparison.Ordinal))
        {
            return false;
        }

        platformHandler.MiragePlatformView.SetProperty("AutomationId", nextAutomationId);
        return true;
    }

    public static bool MapMargin(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformHandler(handler, out var platformHandler))
        {
            return false;
        }

        return SetStyleIfChanged(platformHandler, "margin", ToThickness(view.Margin));
    }

    public static bool MapVisibility(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformHandler(handler, out var platformHandler))
        {
            return false;
        }

        string? display;
        string? visibility;

        switch (view.Visibility)
        {
            case Visibility.Visible:
                display = platformHandler.DefaultDisplayStyle;
                visibility = "visible";
                break;
            case Visibility.Hidden:
                display = platformHandler.DefaultDisplayStyle;
                visibility = "hidden";
                break;
            default:
                display = "none";
                visibility = "hidden";
                break;
        }

        var changed = SetStyleIfChanged(platformHandler, "display", display);
        changed |= SetStyleIfChanged(platformHandler, "visibility", visibility);
        return changed;
    }

    public static bool MapOpacity(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformHandler(handler, out var platformHandler))
        {
            return false;
        }

        var normalized = double.IsFinite(view.Opacity)
            ? Math.Clamp(view.Opacity, 0, 1)
            : 1;

        var opacityStyle = normalized >= 0.999 ? null : $"{normalized:0.###}";
        return SetStyleIfChanged(platformHandler, "opacity", opacityStyle);
    }

    public static bool MapBackground(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformHandler(handler, out var platformHandler))
        {
            return false;
        }

        if (view.Background is null)
        {
            return ClearBackgroundStyles(platformHandler);
        }

        if (view.Background is SolidPaint solidPaint)
        {
            var cssColor = ToCssColor(solidPaint.Color);
            var changed = SetStyleIfChanged(platformHandler, "backgroundColor", cssColor);
            changed |= SetStyleIfChanged(platformHandler, "backgroundImage", null);
            changed |= SetStyleIfChanged(platformHandler, "backgroundSize", null);
            changed |= SetStyleIfChanged(platformHandler, "backgroundRepeat", null);
            changed |= SetStyleIfChanged(platformHandler, "backgroundPosition", null);
            return changed;
        }

        if (view.Background is LinearGradientPaint linearGradientPaint
            && TryBuildLinearGradient(linearGradientPaint, out var gradientCss))
        {
            var changed = SetStyleIfChanged(platformHandler, "backgroundColor", null);
            changed |= SetStyleIfChanged(platformHandler, "backgroundImage", gradientCss);
            changed |= SetStyleIfChanged(platformHandler, "backgroundSize", "100% 100%");
            changed |= SetStyleIfChanged(platformHandler, "backgroundRepeat", "no-repeat");
            changed |= SetStyleIfChanged(platformHandler, "backgroundPosition", "0 0");
            return changed;
        }

        return ClearBackgroundStyles(platformHandler);
    }

    public static bool MapPadding(IViewHandler handler, IPadding paddingView)
    {
        if (!TryGetPlatformHandler(handler, out var platformHandler))
        {
            return false;
        }

        return SetStyleIfChanged(platformHandler, "padding", ToThickness(paddingView.Padding));
    }

    public static bool MapTextColor(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformHandler(handler, out var platformHandler)
            || view is not ITextStyle textStyle)
        {
            return false;
        }

        return SetStyleIfChanged(platformHandler, "color", ToCssColor(textStyle.TextColor));
    }

    public static bool MapFont(IViewHandler handler, IView view, double fallbackSize = DefaultTextFallbackFontSize)
    {
        if (!TryGetPlatformHandler(handler, out var platformHandler)
            || view is not ITextStyle textStyle)
        {
            return false;
        }

        var font = textStyle.Font;
        var resolvedSize = ResolveFontSize(font.Size, fallbackSize);
        var resolvedWeight = ResolveFontWeight(font.Weight);
        var resolvedStyle = ResolveFontStyle(font.Slant);
        var resolvedFamily = string.IsNullOrWhiteSpace(font.Family) ? null : font.Family;

        var changed = SetStyleIfChanged(platformHandler, "fontSize", ToPx(resolvedSize));
        changed |= SetStyleIfChanged(platformHandler, "fontWeight", resolvedWeight);
        changed |= SetStyleIfChanged(platformHandler, "fontStyle", resolvedStyle);
        changed |= SetStyleIfChanged(platformHandler, "fontFamily", resolvedFamily);
        return changed;
    }

    public static bool MapCharacterSpacing(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformHandler(handler, out var platformHandler)
            || view is not ITextStyle textStyle)
        {
            return false;
        }

        string? spacingStyle = null;
        if (double.IsFinite(textStyle.CharacterSpacing))
        {
            spacingStyle = ToPxNoClamp(textStyle.CharacterSpacing);
        }

        return SetStyleIfChanged(platformHandler, "letterSpacing", spacingStyle);
    }

    public static bool MapSizeRequests(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformHandler(handler, out var platformHandler)
            || view is not VisualElement visualElement)
        {
            return false;
        }

        var state = RequestedSizeStates.GetValue(platformHandler.MiragePlatformView, static _ => new RequestedSizeState());
        var changed = false;

        changed |= ApplyRequestedLength(platformHandler, "width", visualElement.WidthRequest, state, RequestedLength.Width);
        changed |= ApplyRequestedLength(platformHandler, "height", visualElement.HeightRequest, state, RequestedLength.Height);
        changed |= ApplyRequestedLength(platformHandler, "minWidth", visualElement.MinimumWidthRequest, state, RequestedLength.MinWidth);
        changed |= ApplyRequestedLength(platformHandler, "minHeight", visualElement.MinimumHeightRequest, state, RequestedLength.MinHeight);
        changed |= ApplyRequestedLength(platformHandler, "maxWidth", visualElement.MaximumWidthRequest, state, RequestedLength.MaxWidth);
        changed |= ApplyRequestedLength(platformHandler, "maxHeight", visualElement.MaximumHeightRequest, state, RequestedLength.MaxHeight);
        return changed;
    }

    public static bool MapLayoutAlignment(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformHandler(handler, out var platformHandler))
        {
            return false;
        }

        var parentKind = platformHandler.MiragePlatformView.Parent?.TypedKind;
        if (parentKind is null)
        {
            return false;
        }

        var changed = false;
        switch (parentKind.Value)
        {
            case Core.Semantics.ComponentKind.VerticalStack:
                changed |= SetStyleIfChanged(platformHandler, "alignSelf", ResolveStackCrossAxisAlignment(view.HorizontalLayoutAlignment));
                changed |= SetStyleIfChanged(platformHandler, "justifySelf", null);
                break;
            case Core.Semantics.ComponentKind.HorizontalStack:
                changed |= SetStyleIfChanged(platformHandler, "alignSelf", ResolveStackCrossAxisAlignment(view.VerticalLayoutAlignment));
                changed |= SetStyleIfChanged(platformHandler, "justifySelf", null);
                break;
            case Core.Semantics.ComponentKind.Grid:
                changed |= SetStyleIfChanged(platformHandler, "justifySelf", ResolveGridSelfAlignment(view.HorizontalLayoutAlignment));
                changed |= SetStyleIfChanged(platformHandler, "alignSelf", ResolveGridSelfAlignment(view.VerticalLayoutAlignment));
                break;
            default:
                changed |= SetStyleIfChanged(platformHandler, "justifySelf", null);
                changed |= SetStyleIfChanged(platformHandler, "alignSelf", null);
                break;
        }

        return changed;
    }

    public static string ResolveStackCrossAxisAlignment(Microsoft.Maui.Primitives.LayoutAlignment alignment)
    {
        return alignment switch
        {
            Microsoft.Maui.Primitives.LayoutAlignment.Start => "flex-start",
            Microsoft.Maui.Primitives.LayoutAlignment.Center => "center",
            Microsoft.Maui.Primitives.LayoutAlignment.End => "flex-end",
            _ => "stretch"
        };
    }

    public static string ResolveGridSelfAlignment(Microsoft.Maui.Primitives.LayoutAlignment alignment)
    {
        return alignment switch
        {
            Microsoft.Maui.Primitives.LayoutAlignment.Start => "start",
            Microsoft.Maui.Primitives.LayoutAlignment.Center => "center",
            Microsoft.Maui.Primitives.LayoutAlignment.End => "end",
            _ => "stretch"
        };
    }

    public static string ToPx(double value)
    {
        var normalized = double.IsFinite(value) ? Math.Max(0, value) : 0;
        return $"{normalized:0.###}px";
    }

    public static string ToPxNoClamp(double value)
    {
        var normalized = double.IsFinite(value) ? value : 0;
        return $"{normalized:0.###}px";
    }

    public static string ToThickness(Thickness thickness)
    {
        return $"{ToPx(thickness.Top)} {ToPx(thickness.Right)} {ToPx(thickness.Bottom)} {ToPx(thickness.Left)}";
    }

    private static bool ApplyRequestedLength(
        IMiragePlatformHandler platformHandler,
        string cssName,
        double value,
        RequestedSizeState state,
        RequestedLength requestedLength)
    {
        var hasExplicitValue = double.IsFinite(value) && value >= 0;
        if (hasExplicitValue)
        {
            SetRequestedFlag(state, requestedLength, true);
            return SetStyleIfChanged(platformHandler, cssName, ToPx(value));
        }

        if (!GetRequestedFlag(state, requestedLength))
        {
            return false;
        }

        SetRequestedFlag(state, requestedLength, false);
        return SetStyleIfChanged(platformHandler, cssName, null);
    }

    private static bool ClearBackgroundStyles(IMiragePlatformHandler platformHandler)
    {
        var changed = SetStyleIfChanged(platformHandler, "backgroundColor", null);
        changed |= SetStyleIfChanged(platformHandler, "backgroundImage", null);
        changed |= SetStyleIfChanged(platformHandler, "backgroundSize", null);
        changed |= SetStyleIfChanged(platformHandler, "backgroundRepeat", null);
        changed |= SetStyleIfChanged(platformHandler, "backgroundPosition", null);
        return changed;
    }

    private static bool SetStyleIfChanged(IMiragePlatformHandler platformHandler, string name, string? value)
    {
        if (string.Equals(platformHandler.MiragePlatformView.GetStyleValue(name), value, StringComparison.Ordinal))
        {
            return false;
        }

        platformHandler.MiragePlatformView.SetStyle(name, value);
        return true;
    }

    private static bool TryGetPlatformHandler(IElementHandler handler, out IMiragePlatformHandler platformHandler)
    {
        if (handler is IMiragePlatformHandler typed)
        {
            platformHandler = typed;
            return true;
        }

        platformHandler = null!;
        return false;
    }

    internal static string? ToCssColor(Color? color)
    {
        if (color is null)
        {
            return null;
        }

        var r = ToByte(color.Red);
        var g = ToByte(color.Green);
        var b = ToByte(color.Blue);
        var alpha = Math.Clamp((double)color.Alpha, 0, 1);
        return $"rgba({r},{g},{b},{alpha.ToString("0.###", CultureInfo.InvariantCulture)})";
    }

    private static byte ToByte(float component)
    {
        var scaled = Math.Round(component * 255.0);
        if (scaled <= 0)
        {
            return 0;
        }

        if (scaled >= 255)
        {
            return 255;
        }

        return (byte)scaled;
    }

    private static double ResolveFontSize(double fontSize, double fallbackSize)
    {
        if (!double.IsFinite(fallbackSize) || fallbackSize <= 0)
        {
            fallbackSize = DefaultTextFallbackFontSize;
        }

        if (!double.IsFinite(fontSize) || fontSize <= 0)
        {
            return fallbackSize;
        }

        return fontSize;
    }

    private static string ResolveFontWeight(FontWeight weight)
    {
        var resolvedWeight = (int)weight;
        if (resolvedWeight <= 0)
        {
            resolvedWeight = (int)FontWeight.Regular;
        }

        return resolvedWeight.ToString(CultureInfo.InvariantCulture);
    }

    private static string ResolveFontStyle(FontSlant slant)
    {
        return slant switch
        {
            FontSlant.Italic => "italic",
            FontSlant.Oblique => "oblique",
            _ => "normal"
        };
    }

    private readonly record struct GradientStopProjection(int Index, double Offset, string ColorCss);

    private static bool TryBuildLinearGradient(LinearGradientPaint paint, out string cssValue)
    {
        cssValue = string.Empty;
        if (paint.GradientStops is null || paint.GradientStops.Length == 0)
        {
            return false;
        }

        var stops = new List<GradientStopProjection>();
        for (var i = 0; i < paint.GradientStops.Length; i++)
        {
            var gradientStop = paint.GradientStops[i];
            var colorCss = ToCssColor(gradientStop.Color);
            if (string.IsNullOrWhiteSpace(colorCss))
            {
                continue;
            }

            var offset = Math.Clamp(NormalizeFinite(gradientStop.Offset, 0), 0, 1);
            stops.Add(new GradientStopProjection(i, offset, colorCss));
        }

        if (stops.Count == 0)
        {
            return false;
        }

        stops.Sort(static (left, right) =>
        {
            var offsetComparison = left.Offset.CompareTo(right.Offset);
            return offsetComparison != 0 ? offsetComparison : left.Index.CompareTo(right.Index);
        });

        if (stops.Count == 1)
        {
            stops.Add(new GradientStopProjection(stops[0].Index + 1, 1, stops[0].ColorCss));
        }

        var startX = NormalizeFinite(paint.StartPoint.X, 0);
        var startY = NormalizeFinite(paint.StartPoint.Y, 0);
        var endX = NormalizeFinite(paint.EndPoint.X, 1);
        var endY = NormalizeFinite(paint.EndPoint.Y, 1);

        if (Math.Abs(endX - startX) < 0.0001 && Math.Abs(endY - startY) < 0.0001)
        {
            endY = startY + 1;
        }

        var svg = new StringBuilder();
        svg.Append("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1 1' preserveAspectRatio='none'>");
        svg.Append("<defs><linearGradient id='g' gradientUnits='objectBoundingBox' x1='");
        svg.Append(CssNumber(startX * 100)).Append("%' y1='");
        svg.Append(CssNumber(startY * 100)).Append("%' x2='");
        svg.Append(CssNumber(endX * 100)).Append("%' y2='");
        svg.Append(CssNumber(endY * 100)).Append("%'>");

        foreach (var stop in stops)
        {
            svg.Append("<stop offset='");
            svg.Append(CssNumber(stop.Offset * 100)).Append("%' stop-color='");
            svg.Append(stop.ColorCss).Append("' />");
        }

        svg.Append("</linearGradient></defs>");
        svg.Append("<rect x='0' y='0' width='1' height='1' fill='url(#g)' /></svg>");
        var encodedSvg = Convert.ToBase64String(Encoding.UTF8.GetBytes(svg.ToString()));
        cssValue = $"url(\"data:image/svg+xml;base64,{encodedSvg}\")";
        return true;
    }

    private static string CssNumber(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static double NormalizeFinite(double value, double fallback) => double.IsFinite(value) ? value : fallback;

    private static bool GetRequestedFlag(RequestedSizeState state, RequestedLength requestedLength)
    {
        return requestedLength switch
        {
            RequestedLength.Width => state.WidthApplied,
            RequestedLength.Height => state.HeightApplied,
            RequestedLength.MinWidth => state.MinWidthApplied,
            RequestedLength.MinHeight => state.MinHeightApplied,
            RequestedLength.MaxWidth => state.MaxWidthApplied,
            RequestedLength.MaxHeight => state.MaxHeightApplied,
            _ => false
        };
    }

    private static void SetRequestedFlag(RequestedSizeState state, RequestedLength requestedLength, bool value)
    {
        switch (requestedLength)
        {
            case RequestedLength.Width:
                state.WidthApplied = value;
                break;
            case RequestedLength.Height:
                state.HeightApplied = value;
                break;
            case RequestedLength.MinWidth:
                state.MinWidthApplied = value;
                break;
            case RequestedLength.MinHeight:
                state.MinHeightApplied = value;
                break;
            case RequestedLength.MaxWidth:
                state.MaxWidthApplied = value;
                break;
            case RequestedLength.MaxHeight:
                state.MaxHeightApplied = value;
                break;
        }
    }

    private sealed class RequestedSizeState
    {
        public bool WidthApplied { get; set; }

        public bool HeightApplied { get; set; }

        public bool MinWidthApplied { get; set; }

        public bool MinHeightApplied { get; set; }

        public bool MaxWidthApplied { get; set; }

        public bool MaxHeightApplied { get; set; }
    }

    private enum RequestedLength
    {
        Width,
        Height,
        MinWidth,
        MinHeight,
        MaxWidth,
        MaxHeight
    }
}
