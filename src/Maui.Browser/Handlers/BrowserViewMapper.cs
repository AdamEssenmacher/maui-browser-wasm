using System.Globalization;
using System.Diagnostics;
using Maui.Browser.Dom;
using Maui.Browser.Utils;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Maui.Browser.Handlers;

internal static class BrowserViewMapper
{
    private const string HeadingRoleMarker = "data-maui-heading-role";

#if DEBUG
    private static readonly HashSet<string> UnsupportedWarnings = new();
#endif

    public static IPropertyMapper<IView, IViewHandler> CommonViewMapper = new PropertyMapper<IView, IViewHandler>(ViewHandler.ViewMapper)
    {
        [nameof(IView.AutomationId)] = MapAutomationId,
        [nameof(IView.FlowDirection)] = MapFlowDirection,
        [nameof(IView.Semantics)] = MapSemantics,
        [nameof(IView.IsEnabled)] = MapEnabled,
        [nameof(IView.InputTransparent)] = MapInputTransparent,
        [nameof(IView.Visibility)] = MapVisibility,
        [nameof(IView.Opacity)] = MapOpacity,
        [nameof(IView.Background)] = MapBackground,
        [nameof(IView.ZIndex)] = MapZIndex,
        [nameof(IView.Clip)] = MapClip,
        [nameof(IView.Shadow)] = MapShadow,
        [nameof(ITransform.TranslationX)] = MapTransform,
        [nameof(ITransform.TranslationY)] = MapTransform,
        [nameof(ITransform.Scale)] = MapTransform,
        [nameof(ITransform.ScaleX)] = MapTransform,
        [nameof(ITransform.ScaleY)] = MapTransform,
        [nameof(ITransform.Rotation)] = MapTransform,
        [nameof(ITransform.RotationX)] = MapTransform,
        [nameof(ITransform.RotationY)] = MapTransform,
        [nameof(ITransform.AnchorX)] = MapTransform,
        [nameof(ITransform.AnchorY)] = MapTransform
    };

    public static CommandMapper<IView, IViewHandler> CommonViewCommandMapper = new(ViewHandler.ViewCommandMapper)
    {
        [nameof(IView.Focus)] = MapFocus,
        [nameof(IView.Unfocus)] = MapUnfocus
    };

    public static void ApplyFrame(DomElement platformView, Rect frame)
    {
        platformView.Style("position", "absolute");
        platformView.Style("left", CssUtil.Px(frame.X));
        platformView.Style("top", CssUtil.Px(frame.Y));
        platformView.Style("width", CssUtil.Px(Math.Max(0, frame.Width)));
        platformView.Style("height", CssUtil.Px(Math.Max(0, frame.Height)));
        platformView.Style("boxSizing", "border-box");
    }

    public static void MapAutomationId(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformView(handler, out var platformView))
        {
            return;
        }

        platformView.Attr("data-automation-id", string.IsNullOrWhiteSpace(view.AutomationId) ? null : view.AutomationId);
    }

    public static void MapFlowDirection(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformView(handler, out var platformView))
        {
            return;
        }

        var direction = view.FlowDirection switch
        {
            FlowDirection.LeftToRight => "ltr",
            FlowDirection.RightToLeft => "rtl",
            _ => null
        };

        platformView.Style("direction", direction);
    }

    public static void MapSemantics(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformView(handler, out var platformView))
        {
            return;
        }

        var semantics = view.Semantics;
        platformView.Attr("aria-label", string.IsNullOrWhiteSpace(semantics?.Description) ? null : semantics.Description);
        platformView.Attr("aria-description", string.IsNullOrWhiteSpace(semantics?.Hint) ? null : semantics.Hint);

        if (view is not ILabel)
        {
            return;
        }

        var headingLevel = semantics?.HeadingLevel ?? SemanticHeadingLevel.None;
        var isHeading = (semantics?.IsHeading ?? false) || headingLevel != SemanticHeadingLevel.None;
        if (!isHeading)
        {
            platformView.Attr("aria-level", null);
            platformView.Attr(HeadingRoleMarker, null);
            platformView.Attr("role", null);
            return;
        }

        var level = headingLevel == SemanticHeadingLevel.None ? 1 : Math.Clamp((int)headingLevel, 1, 9);
        platformView.Attr("role", "heading");
        platformView.Attr("aria-level", level.ToString(CultureInfo.InvariantCulture));
        platformView.Attr(HeadingRoleMarker, "true");
    }

    public static void MapFocus(IViewHandler handler, IView view, object? args)
    {
        var focusRequest = args as FocusRequest;

        if (!view.IsEnabled)
        {
            if (focusRequest is not null)
            {
                focusRequest.TrySetResult(false);
            }

            return;
        }

        if (!TryGetPlatformView(handler, out var platformView))
        {
            if (focusRequest is not null)
            {
                focusRequest.TrySetResult(false);
            }

            return;
        }

        try
        {
            if (!IsNaturallyFocusable(view))
            {
                platformView.Attr("tabindex", "-1");
            }

            var focused = platformView.Focus();
            if (focusRequest is not null)
            {
                focusRequest.TrySetResult(focused);
            }
        }
        catch
        {
            if (focusRequest is not null)
            {
                focusRequest.TrySetResult(false);
            }
        }
    }

    public static void MapUnfocus(IViewHandler handler, IView view, object? args)
    {
        if (!TryGetPlatformView(handler, out var platformView))
        {
            return;
        }

        try
        {
            platformView.Blur();
        }
        catch
        {
            // Browser blur can fail for detached nodes; safe to ignore.
        }
    }

    public static void MapVisibility(IView view, DomElement platformView)
    {
        switch (view.Visibility)
        {
            case Visibility.Visible:
                platformView.Style("display", string.Empty);
                platformView.Style("visibility", "visible");
                break;
            case Visibility.Hidden:
                platformView.Style("display", string.Empty);
                platformView.Style("visibility", "hidden");
                break;
            default:
                platformView.Style("display", "none");
                platformView.Style("visibility", "hidden");
                break;
        }
    }

    public static void MapOpacity(IView view, DomElement platformView)
    {
        platformView.Style("opacity", view.Opacity.ToString("0.###", CultureInfo.InvariantCulture));
    }

    public static void MapEnabled(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformView(handler, out var platformView))
        {
            return;
        }

        MapEnabled(view, platformView);
    }

    public static void MapEnabled(IView view, DomElement platformView)
    {
        if (view.IsEnabled)
        {
            platformView.Attr("disabled", null);
            platformView.Attr("aria-disabled", null);
        }
        else
        {
            platformView.Attr("disabled", "disabled");
            platformView.Attr("aria-disabled", "true");
        }

        ApplyInteractionState(view, platformView);
    }

    public static void MapVisibility(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformView(handler, out var platformView))
        {
            return;
        }

        MapVisibility(view, platformView);
    }

    public static void MapOpacity(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformView(handler, out var platformView))
        {
            return;
        }

        MapOpacity(view, platformView);
    }

    public static void MapInputTransparent(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformView(handler, out var platformView))
        {
            return;
        }

        ApplyInteractionState(view, platformView);
    }

    public static void MapBackground(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformView(handler, out var platformView))
        {
            return;
        }

        if (view.Background is null)
        {
            platformView.Style("backgroundColor", null);
            platformView.Style("backgroundImage", null);
            return;
        }

        if (view.Background is SolidPaint solidPaint)
        {
            platformView.Style("backgroundColor", CssUtil.ToCssColor(solidPaint.Color));
            platformView.Style("backgroundImage", null);
            return;
        }

        if (view.Background is LinearGradientPaint linearGradientPaint)
        {
            if (TryBuildLinearGradient(linearGradientPaint, out var gradientCss))
            {
                platformView.Style("backgroundColor", null);
                platformView.Style("backgroundImage", gradientCss);
            }
            else
            {
                platformView.Style("backgroundImage", null);
                platformView.Style("backgroundColor", null);
                ReportUnsupported(nameof(IView.Background), view, view.Background);
            }

            return;
        }

        platformView.Style("backgroundColor", null);
        platformView.Style("backgroundImage", null);
        ReportUnsupported(nameof(IView.Background), view, view.Background);
    }

    public static void MapZIndex(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformView(handler, out var platformView))
        {
            return;
        }

        platformView.Style("zIndex", view.ZIndex.ToString(CultureInfo.InvariantCulture));
    }

    public static void MapClip(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformView(handler, out var platformView))
        {
            return;
        }

        if (view.Clip is null)
        {
            platformView.Style("clipPath", null);
            return;
        }

        if (view.Clip is RoundRectangleGeometry roundRectangleGeometry)
        {
            if (!TryNormalizeRect(roundRectangleGeometry.Rect, out var rect))
            {
                platformView.Style("clipPath", null);
                ReportUnsupported(nameof(IView.Clip), view, view.Clip);
                return;
            }

            platformView.Style("clipPath", BuildRoundRectangleClipPath(rect, roundRectangleGeometry.CornerRadius));
            return;
        }

        if (view.Clip is RectangleGeometry rectangleGeometry)
        {
            if (!TryNormalizeRect(rectangleGeometry.Rect, out var rect))
            {
                platformView.Style("clipPath", null);
                ReportUnsupported(nameof(IView.Clip), view, view.Clip);
                return;
            }

            platformView.Style("clipPath", BuildRectangleClipPath(rect));
            return;
        }

        platformView.Style("clipPath", null);
        ReportUnsupported(nameof(IView.Clip), view, view.Clip);
    }

    public static void MapShadow(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformView(handler, out var platformView))
        {
            return;
        }

        if (view.Shadow is null)
        {
            platformView.Style("boxShadow", null);
            return;
        }

        if (view.Shadow.Paint is not SolidPaint solidPaint || solidPaint.Color is null)
        {
            platformView.Style("boxShadow", null);
            ReportUnsupported(nameof(IView.Shadow), view, view.Shadow?.Paint);
            return;
        }

        var shadowOpacity = Math.Clamp(NormalizeFinite(view.Shadow.Opacity, 1), 0, 1);
        var colorCss = CssUtil.ToCssColor(solidPaint.Color, shadowOpacity);
        if (string.IsNullOrWhiteSpace(colorCss))
        {
            platformView.Style("boxShadow", null);
            ReportUnsupported(nameof(IView.Shadow), view, view.Shadow?.Paint);
            return;
        }

        var offsetX = NormalizeFinite(view.Shadow.Offset.X, 0);
        var offsetY = NormalizeFinite(view.Shadow.Offset.Y, 0);
        var blur = Math.Max(0, NormalizeFinite(view.Shadow.Radius, 0));
        platformView.Style("boxShadow", $"{CssUtil.Px(offsetX)} {CssUtil.Px(offsetY)} {CssUtil.Px(blur)} {colorCss}");
    }

    public static void MapTransform(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformView(handler, out var platformView))
        {
            return;
        }

        if (view is not ITransform transform)
        {
            return;
        }

        var translationX = NormalizeFinite(transform.TranslationX, 0);
        var translationY = NormalizeFinite(transform.TranslationY, 0);

        var baseScale = NormalizeFinite(transform.Scale, 1);
        var scaleX = baseScale * NormalizeFinite(transform.ScaleX, 1);
        var scaleY = baseScale * NormalizeFinite(transform.ScaleY, 1);

        var rotation = NormalizeFinite(transform.Rotation, 0);
        var rotationX = NormalizeFinite(transform.RotationX, 0);
        var rotationY = NormalizeFinite(transform.RotationY, 0);

        var anchorX = Math.Clamp(NormalizeFinite(transform.AnchorX, 0.5), 0, 1);
        var anchorY = Math.Clamp(NormalizeFinite(transform.AnchorY, 0.5), 0, 1);

        var origin = $"{(anchorX * 100).ToString("0.###", CultureInfo.InvariantCulture)}% {(anchorY * 100).ToString("0.###", CultureInfo.InvariantCulture)}%";
        platformView.Style("transformOrigin", origin);

        var isIdentity =
            Math.Abs(translationX) < 0.0001 &&
            Math.Abs(translationY) < 0.0001 &&
            Math.Abs(scaleX - 1) < 0.0001 &&
            Math.Abs(scaleY - 1) < 0.0001 &&
            Math.Abs(rotation) < 0.0001 &&
            Math.Abs(rotationX) < 0.0001 &&
            Math.Abs(rotationY) < 0.0001;

        if (isIdentity)
        {
            platformView.Style("transform", null);
            return;
        }

        var transformCss =
            $"translate({CssUtil.Px(translationX)}, {CssUtil.Px(translationY)}) " +
            $"rotate({rotation.ToString("0.###", CultureInfo.InvariantCulture)}deg) " +
            $"rotateX({rotationX.ToString("0.###", CultureInfo.InvariantCulture)}deg) " +
            $"rotateY({rotationY.ToString("0.###", CultureInfo.InvariantCulture)}deg) " +
            $"scale({scaleX.ToString("0.###", CultureInfo.InvariantCulture)}, {scaleY.ToString("0.###", CultureInfo.InvariantCulture)})";

        platformView.Style("transform", transformCss);
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

    private static bool TryGetPlatformView(IViewHandler handler, out DomElement platformView)
    {
        if (handler.PlatformView is DomElement domElement)
        {
            platformView = domElement;
            return true;
        }

        platformView = null!;
        return false;
    }

    private static void ApplyInteractionState(IView view, DomElement platformView)
    {
        var pointerDisabled = !view.IsEnabled || view.InputTransparent;
        platformView.Style("pointerEvents", pointerDisabled ? "none" : null);
    }

    private static bool IsNaturallyFocusable(IView view)
    {
        return view is IEntry
            or ITextButton
            or ICheckBox
            or ISlider
            or IPicker
            or ISwitch;
    }

    private static double NormalizeFinite(double value, double fallback)
    {
        return double.IsFinite(value) ? value : fallback;
    }

    private static bool TryBuildLinearGradient(LinearGradientPaint paint, out string cssValue)
    {
        cssValue = string.Empty;
        if (paint.GradientStops is null || paint.GradientStops.Length == 0)
        {
            return false;
        }

        var stops = new List<(double Offset, string ColorCss)>();
        foreach (var gradientStop in paint.GradientStops)
        {
            var colorCss = CssUtil.ToCssColor(gradientStop.Color);
            if (string.IsNullOrWhiteSpace(colorCss))
            {
                continue;
            }

            var offset = Math.Clamp(NormalizeFinite(gradientStop.Offset, 0), 0, 1);
            stops.Add((offset, colorCss));
        }

        if (stops.Count == 0)
        {
            return false;
        }

        stops.Sort(static (left, right) => left.Offset.CompareTo(right.Offset));
        if (stops.Count == 1)
        {
            stops.Add((1, stops[0].ColorCss));
        }

        var stopSegments = new List<string>(stops.Count);
        foreach (var stop in stops)
        {
            stopSegments.Add($"{stop.ColorCss} {CssUtil.Number(stop.Offset * 100)}%");
        }

        var angle = ResolveLinearGradientAngle(paint.StartPoint, paint.EndPoint);
        cssValue = $"linear-gradient({CssUtil.Number(angle)}deg, {string.Join(", ", stopSegments)})";
        return true;
    }

    private static double ResolveLinearGradientAngle(Point start, Point end)
    {
        var startX = NormalizeFinite(start.X, 0);
        var startY = NormalizeFinite(start.Y, 0);
        var endX = NormalizeFinite(end.X, 1);
        var endY = NormalizeFinite(end.Y, 1);
        var deltaX = endX - startX;
        var deltaY = endY - startY;
        if (Math.Abs(deltaX) < 0.0001 && Math.Abs(deltaY) < 0.0001)
        {
            return 180;
        }

        var angle = Math.Atan2(deltaY, deltaX) * (180 / Math.PI) + 90;
        while (angle < 0)
        {
            angle += 360;
        }

        while (angle >= 360)
        {
            angle -= 360;
        }

        return angle;
    }

    private static bool TryNormalizeRect(Rect source, out Rect normalized)
    {
        var x = NormalizeFinite(source.X, 0);
        var y = NormalizeFinite(source.Y, 0);
        var width = NormalizeFinite(source.Width, 0);
        var height = NormalizeFinite(source.Height, 0);
        if (width < 0 || height < 0)
        {
            normalized = Rect.Zero;
            return false;
        }

        normalized = new Rect(x, y, width, height);
        return true;
    }

    private static string BuildRectangleClipPath(Rect rect)
    {
        var (top, right, bottom, left) = BuildInsetOffsets(rect);
        return $"inset({top} {right} {bottom} {left})";
    }

    private static string BuildRoundRectangleClipPath(Rect rect, CornerRadius cornerRadius)
    {
        var (top, right, bottom, left) = BuildInsetOffsets(rect);
        var topLeft = CssUtil.Px(Math.Max(0, NormalizeFinite(cornerRadius.TopLeft, 0)));
        var topRight = CssUtil.Px(Math.Max(0, NormalizeFinite(cornerRadius.TopRight, 0)));
        var bottomRight = CssUtil.Px(Math.Max(0, NormalizeFinite(cornerRadius.BottomRight, 0)));
        var bottomLeft = CssUtil.Px(Math.Max(0, NormalizeFinite(cornerRadius.BottomLeft, 0)));
        return $"inset({top} {right} {bottom} {left} round {topLeft} {topRight} {bottomRight} {bottomLeft})";
    }

    private static (string Top, string Right, string Bottom, string Left) BuildInsetOffsets(Rect rect)
    {
        var top = CssUtil.Px(Math.Max(0, rect.Y));
        var left = CssUtil.Px(Math.Max(0, rect.X));
        var rightEdge = Math.Max(0, rect.X + rect.Width);
        var bottomEdge = Math.Max(0, rect.Y + rect.Height);
        var right = CssUtil.CalcRemainingPx(rightEdge);
        var bottom = CssUtil.CalcRemainingPx(bottomEdge);
        return (top, right, bottom, left);
    }

    private static void ReportUnsupported(string propertyName, IView view, object? value)
    {
#if DEBUG
        var key = $"{propertyName}|{view.GetType().FullName}|{value?.GetType().FullName}";
        if (UnsupportedWarnings.Add(key))
        {
            Debug.WriteLine(
                $"[Maui.Browser] Unsupported mapping for {propertyName} on {view.GetType().Name}. Value type: {value?.GetType().Name ?? "<null>"}.");
        }
#endif
    }
}
