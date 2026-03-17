using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Mirage.Core.Diagnostics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal static class MirageCommonViewProjection
{
    internal const string DefaultSystemTextColor = "CanvasText";
    internal const string DefaultSystemFontFamily = "system-ui";
    private const double DefaultTextFallbackFontSize = 14;
    private static readonly HashSet<string> GenericFontFamilies = new(StringComparer.OrdinalIgnoreCase)
    {
        "serif",
        "sans-serif",
        "monospace",
        "cursive",
        "fantasy",
        "system-ui",
        "ui-serif",
        "ui-sans-serif",
        "ui-monospace",
        "ui-rounded"
    };

    private static readonly ConditionalWeakTable<MiragePlatformView, RequestedSizeState> RequestedSizeStates = new();
    private static readonly ConditionalWeakTable<MiragePlatformView, TransformCompositionState> TransformCompositionStates = new();

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

        return SetStyleIfChanged(platformHandler, "margin", ToMarginThickness(view.Margin));
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

        var backgroundPaint = ResolveBackgroundPaint(view);
        if (backgroundPaint is null)
        {
            return ClearBackgroundStyles(platformHandler);
        }

        if (backgroundPaint is SolidPaint solidPaint)
        {
            var cssColor = ToCssColor(solidPaint.Color);
            var changed = SetStyleIfChanged(platformHandler, "backgroundColor", cssColor);
            changed |= SetStyleIfChanged(platformHandler, "backgroundImage", null);
            changed |= SetStyleIfChanged(platformHandler, "backgroundSize", null);
            changed |= SetStyleIfChanged(platformHandler, "backgroundRepeat", null);
            changed |= SetStyleIfChanged(platformHandler, "backgroundPosition", null);
            return changed;
        }

        if (backgroundPaint is LinearGradientPaint linearGradientPaint
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

    public static bool MapClip(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformHandler(handler, out var platformHandler))
        {
            return false;
        }

        if (view.Clip is null)
        {
            return SetStyleIfChanged(platformHandler, "clipPath", null);
        }

        var projection = BuildClipPathProjection(view.Clip);
        return SetStyleIfChanged(platformHandler, "clipPath", projection.ToCssValue());
    }

    public static bool MapShadow(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformHandler(handler, out var platformHandler)
            || view is not VisualElement visualElement)
        {
            return false;
        }

        var shadow = visualElement.Shadow;
        if (shadow is null)
        {
            return SetStyleIfChanged(platformHandler, "boxShadow", null);
        }

        var shadowColor = ResolveShadowCssColor(shadow);
        if (string.IsNullOrWhiteSpace(shadowColor))
        {
            MirageDiagnostics.Log(
                "shadow.map",
                $"unsupported brush type={shadow.Brush?.GetType().Name ?? "<null>"} view={view.GetType().Name}");
            return SetStyleIfChanged(platformHandler, "boxShadow", null);
        }

        var offsetX = NormalizeFinite(shadow.Offset.X, 0);
        var offsetY = NormalizeFinite(shadow.Offset.Y, 0);
        var blur = Math.Max(0, NormalizeFinite(shadow.Radius, 0));
        return SetStyleIfChanged(
            platformHandler,
            "boxShadow",
            $"{ToPxNoClamp(offsetX)} {ToPxNoClamp(offsetY)} {ToPx(blur)} {shadowColor}");
    }

    public static bool MapTransform(IViewHandler handler, IView view)
    {
        if (!TryGetPlatformHandler(handler, out var platformHandler)
            || view is not ITransform transform)
        {
            return false;
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
        var transformOrigin = $"{CssNumber(anchorX * 100)}% {CssNumber(anchorY * 100)}%";

        var changed = SetStyleIfChanged(platformHandler, "transformOrigin", transformOrigin);

        var isIdentity =
            Math.Abs(translationX) < 0.0001 &&
            Math.Abs(translationY) < 0.0001 &&
            Math.Abs(scaleX - 1) < 0.0001 &&
            Math.Abs(scaleY - 1) < 0.0001 &&
            Math.Abs(rotation) < 0.0001 &&
            Math.Abs(rotationX) < 0.0001 &&
            Math.Abs(rotationY) < 0.0001;

        var viewTransform = isIdentity
            ? null
            : string.Create(
                CultureInfo.InvariantCulture,
                $"translate({ToPxNoClamp(translationX)}, {ToPxNoClamp(translationY)}) " +
                $"rotate({CssNumber(rotation)}deg) " +
                $"rotateX({CssNumber(rotationX)}deg) " +
                $"rotateY({CssNumber(rotationY)}deg) " +
                $"scale({CssNumber(scaleX)}, {CssNumber(scaleY)})");

        changed |= SetViewTransformFragment(platformHandler.MiragePlatformView, viewTransform);
        return changed;
    }

    internal static bool SetLayoutTransformFragment(MiragePlatformView platformView, string? layoutTransform)
    {
        var state = TransformCompositionStates.GetValue(platformView, static _ => new TransformCompositionState());
        if (string.Equals(state.LayoutTransform, layoutTransform, StringComparison.Ordinal))
        {
            return false;
        }

        state.LayoutTransform = layoutTransform;
        return ApplyComposedTransformStyle(platformView, state);
    }

    public static bool MapLayoutClipsToBounds(IViewHandler handler, Microsoft.Maui.ILayout layout)
    {
        if (!TryGetPlatformHandler(handler, out var platformHandler))
        {
            return false;
        }

        var clipsToBounds = layout.ClipsToBounds
            || (layout is IView layoutView && layoutView.Clip is not null);

        return SetStyleIfChanged(platformHandler, "overflow", clipsToBounds ? "hidden" : "visible");
    }

    public static bool MapPadding(IViewHandler handler, IPadding paddingView)
    {
        if (!TryGetPlatformHandler(handler, out var platformHandler))
        {
            return false;
        }

        return SetStyleIfChanged(platformHandler, "padding", ToPaddingThickness(paddingView.Padding));
    }

    public static bool MapTextColor(IViewHandler handler, IView view, string? fallbackCssColor = null)
    {
        if (!TryGetPlatformHandler(handler, out var platformHandler)
            || view is not ITextStyle textStyle)
        {
            return false;
        }

        var cssColor = ToCssColor(textStyle.TextColor) ?? fallbackCssColor;
        return SetStyleIfChanged(platformHandler, "color", cssColor);
    }

    public static bool MapFont(
        IViewHandler handler,
        IView view,
        double fallbackSize = DefaultTextFallbackFontSize,
        string? fallbackFamily = null)
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
        var resolvedFamily = ResolveFontFamily(font.Family, fallbackFamily);

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

        var parentKind = ResolveParentLayoutKind(view, platformHandler.MiragePlatformView.Parent?.ControlKind);
        if (parentKind is null)
        {
            return false;
        }

        var horizontalAlignment = ResolveHorizontalAlignment(view);
        var verticalAlignment = ResolveVerticalAlignment(view);

        var changed = false;
        switch (parentKind.Value)
        {
            case Core.Semantics.ComponentKind.VerticalStack:
                changed |= SetStyleIfChanged(
                    platformHandler,
                    "alignSelf",
                    ResolveStackCrossAxisAlignment(
                        ResolveEffectiveStackCrossAxisAlignment(view, parentIsVerticalStack: true)));
                changed |= SetStyleIfChanged(platformHandler, "justifySelf", null);
                break;
            case Core.Semantics.ComponentKind.HorizontalStack:
                changed |= SetStyleIfChanged(
                    platformHandler,
                    "alignSelf",
                    ResolveStackCrossAxisAlignment(
                        ResolveEffectiveStackCrossAxisAlignment(view, parentIsVerticalStack: false)));
                changed |= SetStyleIfChanged(platformHandler, "justifySelf", null);
                break;
            case Core.Semantics.ComponentKind.ContentContainer:
            case Core.Semantics.ComponentKind.Border:
                changed |= SetStyleIfChanged(platformHandler, "justifySelf", ResolveGridSelfAlignment(horizontalAlignment));
                changed |= SetStyleIfChanged(platformHandler, "alignSelf", ResolveGridSelfAlignment(verticalAlignment));
                break;
            case Core.Semantics.ComponentKind.Grid:
                changed |= SetStyleIfChanged(platformHandler, "justifySelf", ResolveGridSelfAlignment(horizontalAlignment));
                changed |= SetStyleIfChanged(platformHandler, "alignSelf", ResolveGridSelfAlignment(verticalAlignment));
                break;
            default:
                changed |= SetStyleIfChanged(platformHandler, "justifySelf", null);
                changed |= SetStyleIfChanged(platformHandler, "alignSelf", null);
                break;
        }

        return changed;
    }

    internal static Microsoft.Maui.Primitives.LayoutAlignment ResolveEffectiveStackCrossAxisAlignment(
        IView view,
        bool parentIsVerticalStack)
    {
        var alignment = parentIsVerticalStack
            ? ResolveHorizontalAlignment(view)
            : ResolveVerticalAlignment(view);

        if (alignment != Microsoft.Maui.Primitives.LayoutAlignment.Fill
            || view is not VisualElement visualElement)
        {
            return alignment;
        }

        var crossAxisRequest = parentIsVerticalStack
            ? visualElement.WidthRequest
            : visualElement.HeightRequest;

        // MAUI stack layout keeps explicit cross-axis requests sized and centered instead of stretching.
        return HasFiniteCrossAxisRequest(crossAxisRequest)
            ? Microsoft.Maui.Primitives.LayoutAlignment.Center
            : alignment;
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

    internal static Microsoft.Maui.Primitives.LayoutAlignment ResolveHorizontalAlignment(IView view)
    {
        if (view is View mauiView)
        {
            return ToPrimitivesAlignment(mauiView.HorizontalOptions.Alignment);
        }

        return view.HorizontalLayoutAlignment;
    }

    internal static Microsoft.Maui.Primitives.LayoutAlignment ResolveVerticalAlignment(IView view)
    {
        if (view is View mauiView)
        {
            return ToPrimitivesAlignment(mauiView.VerticalOptions.Alignment);
        }

        return view.VerticalLayoutAlignment;
    }

    private static bool HasFiniteCrossAxisRequest(double requestedLength)
    {
        return double.IsFinite(requestedLength) && requestedLength > 0;
    }

    private static Mirage.Core.Semantics.ComponentKind? ResolveParentLayoutKind(
        IView view,
        Mirage.Core.Semantics.ComponentKind? projectedParentKind)
    {
        return view.Parent switch
        {
            StackLayout stack => stack.Orientation == StackOrientation.Horizontal
                ? Mirage.Core.Semantics.ComponentKind.HorizontalStack
                : Mirage.Core.Semantics.ComponentKind.VerticalStack,
            VerticalStackLayout => Mirage.Core.Semantics.ComponentKind.VerticalStack,
            HorizontalStackLayout => Mirage.Core.Semantics.ComponentKind.HorizontalStack,
            Grid => Mirage.Core.Semantics.ComponentKind.Grid,
            _ => projectedParentKind
        };
    }

    private static Microsoft.Maui.Primitives.LayoutAlignment ToPrimitivesAlignment(Microsoft.Maui.Controls.LayoutAlignment alignment)
    {
        return alignment switch
        {
            Microsoft.Maui.Controls.LayoutAlignment.Start => Microsoft.Maui.Primitives.LayoutAlignment.Start,
            Microsoft.Maui.Controls.LayoutAlignment.Center => Microsoft.Maui.Primitives.LayoutAlignment.Center,
            Microsoft.Maui.Controls.LayoutAlignment.End => Microsoft.Maui.Primitives.LayoutAlignment.End,
            _ => Microsoft.Maui.Primitives.LayoutAlignment.Fill
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

    public static string ToPaddingThickness(Thickness thickness)
    {
        return $"{ToPx(thickness.Top)} {ToPx(thickness.Right)} {ToPx(thickness.Bottom)} {ToPx(thickness.Left)}";
    }

    public static string ToMarginThickness(Thickness thickness)
    {
        return $"{ToPxNoClamp(thickness.Top)} {ToPxNoClamp(thickness.Right)} {ToPxNoClamp(thickness.Bottom)} {ToPxNoClamp(thickness.Left)}";
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

    private static string? ResolveShadowCssColor(Shadow shadow)
    {
        if (shadow.Brush is not SolidColorBrush solidColorBrush || solidColorBrush.Color is null)
        {
            return null;
        }

        var opacity = double.IsFinite(shadow.Opacity)
            ? Math.Clamp(shadow.Opacity, 0, 1)
            : 1d;

        var alpha = Math.Clamp(solidColorBrush.Color.Alpha * (float)opacity, 0f, 1f);
        var color = new Color(
            solidColorBrush.Color.Red,
            solidColorBrush.Color.Green,
            solidColorBrush.Color.Blue,
            alpha);

        return ToCssColor(color);
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

    private static bool SetViewTransformFragment(MiragePlatformView platformView, string? viewTransform)
    {
        var state = TransformCompositionStates.GetValue(platformView, static _ => new TransformCompositionState());
        if (string.Equals(state.ViewTransform, viewTransform, StringComparison.Ordinal))
        {
            return false;
        }

        state.ViewTransform = viewTransform;
        return ApplyComposedTransformStyle(platformView, state);
    }

    private static bool ApplyComposedTransformStyle(MiragePlatformView platformView, TransformCompositionState state)
    {
        var composed = ComposeTransform(state.LayoutTransform, state.ViewTransform);
        if (string.Equals(platformView.GetStyleValue("transform"), composed, StringComparison.Ordinal))
        {
            return false;
        }

        platformView.SetStyle("transform", composed);
        return true;
    }

    private static string? ComposeTransform(string? layoutTransform, string? viewTransform)
    {
        var hasLayoutTransform = !string.IsNullOrWhiteSpace(layoutTransform);
        var hasViewTransform = !string.IsNullOrWhiteSpace(viewTransform);

        if (!hasLayoutTransform && !hasViewTransform)
        {
            return null;
        }

        if (!hasLayoutTransform)
        {
            return viewTransform;
        }

        if (!hasViewTransform)
        {
            return layoutTransform;
        }

        return $"{layoutTransform} {viewTransform}";
    }

    private static bool TryGetPlatformHandler(IElementHandler handler, out IMiragePlatformHandler platformHandler)
    {
        if (handler is IMiragePlatformHandler miragePlatformHandler)
        {
            try
            {
                _ = miragePlatformHandler.MiragePlatformView;
                platformHandler = miragePlatformHandler;
                return true;
            }
            catch (InvalidOperationException)
            {
                platformHandler = null!;
                return false;
            }
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

    private static Paint? ResolveBackgroundPaint(IView view)
    {
        if (view.Background is not null)
        {
            return view.Background;
        }

        if (view is VisualElement visualElement
            && visualElement.IsSet(VisualElement.BackgroundColorProperty)
            && visualElement.BackgroundColor is { } backgroundColor)
        {
            return new SolidPaint(backgroundColor);
        }

        return null;
    }

    private static string? ResolveFontFamily(string? family, string? fallbackFamily)
    {
        var normalizedFallback = NormalizeFallbackFontFamily(fallbackFamily);

        if (string.IsNullOrWhiteSpace(family))
        {
            return normalizedFallback;
        }

        var trimmed = family.Trim();
        if (trimmed.Length == 0)
        {
            return normalizedFallback;
        }

        if (trimmed.Contains(',', StringComparison.Ordinal) || GenericFontFamilies.Contains(trimmed))
        {
            return trimmed;
        }

        var quotedFamily = trimmed.StartsWith('"') || trimmed.StartsWith('\'')
            ? trimmed
            : $"\"{trimmed.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";

        if (string.IsNullOrWhiteSpace(normalizedFallback))
        {
            return $"{quotedFamily}, sans-serif";
        }

        if (string.Equals(normalizedFallback, "sans-serif", StringComparison.OrdinalIgnoreCase))
        {
            return $"{quotedFamily}, sans-serif";
        }

        return $"{quotedFamily}, {normalizedFallback}, sans-serif";
    }

    private static string? NormalizeFallbackFontFamily(string? fallbackFamily)
    {
        if (string.IsNullOrWhiteSpace(fallbackFamily))
        {
            return null;
        }

        var trimmed = fallbackFamily.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static ClipPathProjection BuildClipPathProjection(IShape geometry)
    {
        return geometry switch
        {
            RectangleGeometry rectangleGeometry => new ClipPathProjection(
                BuildRectanglePathData(ValidateRect(rectangleGeometry.Rect, nameof(RectangleGeometry))),
                false),
            RoundRectangleGeometry roundRectangleGeometry => new ClipPathProjection(
                BuildRoundRectanglePathData(
                    ValidateRect(roundRectangleGeometry.Rect, nameof(RoundRectangleGeometry)),
                    roundRectangleGeometry.CornerRadius),
                false),
            EllipseGeometry ellipseGeometry => new ClipPathProjection(
                BuildEllipsePathData(ellipseGeometry),
                false),
            LineGeometry lineGeometry => new ClipPathProjection(
                BuildLinePathData(lineGeometry),
                false),
            PathGeometry pathGeometry => BuildPathGeometryProjection(pathGeometry),
            GeometryGroup geometryGroup => BuildGeometryGroupProjection(geometryGroup),
            _ => throw new InvalidOperationException(
                $"Mirage does not support clip geometry type '{geometry.GetType().FullName}'.")
        };
    }

    private static ClipPathProjection BuildPathGeometryProjection(PathGeometry pathGeometry)
    {
        if (pathGeometry.Figures is null || pathGeometry.Figures.Count == 0)
        {
            throw new InvalidOperationException("Mirage clip projection requires PathGeometry with at least one figure.");
        }

        var builder = new StringBuilder();
        foreach (var figure in pathGeometry.Figures)
        {
            if (figure is null)
            {
                continue;
            }

            AppendPathFigure(builder, figure);
        }

        if (builder.Length == 0)
        {
            throw new InvalidOperationException("Mirage clip projection requires PathGeometry with valid figure content.");
        }

        return new ClipPathProjection(
            builder.ToString().Trim(),
            pathGeometry.FillRule == FillRule.EvenOdd);
    }

    private static ClipPathProjection BuildGeometryGroupProjection(GeometryGroup geometryGroup)
    {
        if (geometryGroup.Children is null || geometryGroup.Children.Count == 0)
        {
            throw new InvalidOperationException("Mirage clip projection requires GeometryGroup with at least one child geometry.");
        }

        var builder = new StringBuilder();
        foreach (var child in geometryGroup.Children)
        {
            if (child is null)
            {
                continue;
            }

            var childProjection = BuildClipPathProjection(child);
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(childProjection.PathData);
        }

        if (builder.Length == 0)
        {
            throw new InvalidOperationException("Mirage clip projection requires GeometryGroup with valid child geometries.");
        }

        return new ClipPathProjection(
            builder.ToString(),
            geometryGroup.FillRule == FillRule.EvenOdd);
    }

    private static void AppendPathFigure(StringBuilder builder, PathFigure figure)
    {
        var start = ValidatePoint(figure.StartPoint, $"{nameof(PathFigure)}.{nameof(PathFigure.StartPoint)}");
        if (builder.Length > 0)
        {
            builder.Append(' ');
        }

        builder.Append("M ").Append(CssNumber(start.X)).Append(' ').Append(CssNumber(start.Y));

        if (figure.Segments is not null)
        {
            foreach (var segment in figure.Segments)
            {
                if (segment is null)
                {
                    continue;
                }

                AppendPathSegment(builder, segment);
            }
        }

        if (figure.IsClosed)
        {
            builder.Append(" Z");
        }
    }

    private static void AppendPathSegment(StringBuilder builder, PathSegment segment)
    {
        switch (segment)
        {
            case LineSegment lineSegment:
            {
                var point = ValidatePoint(lineSegment.Point, $"{nameof(LineSegment)}.{nameof(LineSegment.Point)}");
                builder.Append(" L ").Append(CssNumber(point.X)).Append(' ').Append(CssNumber(point.Y));
                break;
            }
            case PolyLineSegment polyLineSegment:
            {
                AppendPolyPoints(builder, polyLineSegment.Points, 1, "L", nameof(PolyLineSegment));
                break;
            }
            case BezierSegment bezierSegment:
            {
                var point1 = ValidatePoint(bezierSegment.Point1, $"{nameof(BezierSegment)}.{nameof(BezierSegment.Point1)}");
                var point2 = ValidatePoint(bezierSegment.Point2, $"{nameof(BezierSegment)}.{nameof(BezierSegment.Point2)}");
                var point3 = ValidatePoint(bezierSegment.Point3, $"{nameof(BezierSegment)}.{nameof(BezierSegment.Point3)}");
                builder.Append(" C ")
                    .Append(CssNumber(point1.X)).Append(' ').Append(CssNumber(point1.Y)).Append(' ')
                    .Append(CssNumber(point2.X)).Append(' ').Append(CssNumber(point2.Y)).Append(' ')
                    .Append(CssNumber(point3.X)).Append(' ').Append(CssNumber(point3.Y));
                break;
            }
            case PolyBezierSegment polyBezierSegment:
            {
                AppendPolyPoints(builder, polyBezierSegment.Points, 3, "C", nameof(PolyBezierSegment));
                break;
            }
            case QuadraticBezierSegment quadraticBezierSegment:
            {
                var point1 = ValidatePoint(quadraticBezierSegment.Point1, $"{nameof(QuadraticBezierSegment)}.{nameof(QuadraticBezierSegment.Point1)}");
                var point2 = ValidatePoint(quadraticBezierSegment.Point2, $"{nameof(QuadraticBezierSegment)}.{nameof(QuadraticBezierSegment.Point2)}");
                builder.Append(" Q ")
                    .Append(CssNumber(point1.X)).Append(' ').Append(CssNumber(point1.Y)).Append(' ')
                    .Append(CssNumber(point2.X)).Append(' ').Append(CssNumber(point2.Y));
                break;
            }
            case PolyQuadraticBezierSegment polyQuadraticBezierSegment:
            {
                AppendPolyPoints(builder, polyQuadraticBezierSegment.Points, 2, "Q", nameof(PolyQuadraticBezierSegment));
                break;
            }
            case ArcSegment arcSegment:
            {
                var point = ValidatePoint(arcSegment.Point, $"{nameof(ArcSegment)}.{nameof(ArcSegment.Point)}");
                var size = arcSegment.Size;
                var radiusX = ValidateNonNegativeFinite(size.Width, $"{nameof(ArcSegment)}.{nameof(ArcSegment.Size)}.Width");
                var radiusY = ValidateNonNegativeFinite(size.Height, $"{nameof(ArcSegment)}.{nameof(ArcSegment.Size)}.Height");
                if (radiusX <= 0 || radiusY <= 0)
                {
                    builder.Append(" L ").Append(CssNumber(point.X)).Append(' ').Append(CssNumber(point.Y));
                    break;
                }

                var rotation = ValidateFinite(arcSegment.RotationAngle, $"{nameof(ArcSegment)}.{nameof(ArcSegment.RotationAngle)}");
                var largeArc = arcSegment.IsLargeArc ? "1" : "0";
                var sweep = arcSegment.SweepDirection == SweepDirection.Clockwise ? "1" : "0";
                builder.Append(" A ")
                    .Append(CssNumber(radiusX)).Append(' ').Append(CssNumber(radiusY)).Append(' ')
                    .Append(CssNumber(rotation)).Append(' ')
                    .Append(largeArc).Append(' ')
                    .Append(sweep).Append(' ')
                    .Append(CssNumber(point.X)).Append(' ').Append(CssNumber(point.Y));
                break;
            }
            default:
                throw new InvalidOperationException(
                    $"Mirage does not support clip path segment type '{segment.GetType().FullName}'.");
        }
    }

    private static void AppendPolyPoints(
        StringBuilder builder,
        IList<Point>? points,
        int expectedStep,
        string command,
        string segmentName)
    {
        if (points is null || points.Count == 0)
        {
            return;
        }

        if (points.Count % expectedStep != 0)
        {
            throw new InvalidOperationException(
                $"Mirage clip projection requires '{segmentName}' points count to be divisible by {expectedStep.ToString(CultureInfo.InvariantCulture)}.");
        }

        for (var index = 0; index < points.Count; index += expectedStep)
        {
            builder.Append(' ').Append(command).Append(' ');
            for (var step = 0; step < expectedStep; step++)
            {
                var point = ValidatePoint(points[index + step], $"{segmentName}.Points[{(index + step).ToString(CultureInfo.InvariantCulture)}]");
                if (step > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(CssNumber(point.X)).Append(' ').Append(CssNumber(point.Y));
            }
        }
    }

    private static string BuildRectanglePathData(Rect rect)
    {
        var left = rect.X;
        var top = rect.Y;
        var right = rect.X + rect.Width;
        var bottom = rect.Y + rect.Height;

        return $"M {CssNumber(left)} {CssNumber(top)} H {CssNumber(right)} V {CssNumber(bottom)} H {CssNumber(left)} Z";
    }

    private static string BuildRoundRectanglePathData(Rect rect, CornerRadius cornerRadius)
    {
        var left = rect.X;
        var top = rect.Y;
        var right = rect.X + rect.Width;
        var bottom = rect.Y + rect.Height;

        var topLeft = NormalizeCornerRadius(cornerRadius.TopLeft, rect, nameof(CornerRadius.TopLeft));
        var topRight = NormalizeCornerRadius(cornerRadius.TopRight, rect, nameof(CornerRadius.TopRight));
        var bottomRight = NormalizeCornerRadius(cornerRadius.BottomRight, rect, nameof(CornerRadius.BottomRight));
        var bottomLeft = NormalizeCornerRadius(cornerRadius.BottomLeft, rect, nameof(CornerRadius.BottomLeft));

        var builder = new StringBuilder();
        builder.Append("M ").Append(CssNumber(left + topLeft)).Append(' ').Append(CssNumber(top));
        builder.Append(" H ").Append(CssNumber(right - topRight));
        AppendArcOrLine(builder, topRight, right, top + topRight);
        builder.Append(" V ").Append(CssNumber(bottom - bottomRight));
        AppendArcOrLine(builder, bottomRight, right - bottomRight, bottom);
        builder.Append(" H ").Append(CssNumber(left + bottomLeft));
        AppendArcOrLine(builder, bottomLeft, left, bottom - bottomLeft);
        builder.Append(" V ").Append(CssNumber(top + topLeft));
        AppendArcOrLine(builder, topLeft, left + topLeft, top);
        builder.Append(" Z");
        return builder.ToString();
    }

    private static string BuildEllipsePathData(EllipseGeometry ellipseGeometry)
    {
        var center = ValidatePoint(ellipseGeometry.Center, $"{nameof(EllipseGeometry)}.{nameof(EllipseGeometry.Center)}");
        var radiusX = ValidateNonNegativeFinite(ellipseGeometry.RadiusX, $"{nameof(EllipseGeometry)}.{nameof(EllipseGeometry.RadiusX)}");
        var radiusY = ValidateNonNegativeFinite(ellipseGeometry.RadiusY, $"{nameof(EllipseGeometry)}.{nameof(EllipseGeometry.RadiusY)}");
        if (radiusX <= 0 || radiusY <= 0)
        {
            return $"M {CssNumber(center.X)} {CssNumber(center.Y)} Z";
        }

        var startX = center.X - radiusX;
        var endX = center.X + radiusX;
        var centerY = center.Y;

        return $"M {CssNumber(startX)} {CssNumber(centerY)} A {CssNumber(radiusX)} {CssNumber(radiusY)} 0 1 0 {CssNumber(endX)} {CssNumber(centerY)} A {CssNumber(radiusX)} {CssNumber(radiusY)} 0 1 0 {CssNumber(startX)} {CssNumber(centerY)} Z";
    }

    private static string BuildLinePathData(LineGeometry lineGeometry)
    {
        var start = ValidatePoint(lineGeometry.StartPoint, $"{nameof(LineGeometry)}.{nameof(LineGeometry.StartPoint)}");
        var end = ValidatePoint(lineGeometry.EndPoint, $"{nameof(LineGeometry)}.{nameof(LineGeometry.EndPoint)}");
        return $"M {CssNumber(start.X)} {CssNumber(start.Y)} L {CssNumber(end.X)} {CssNumber(end.Y)} Z";
    }

    private static void AppendArcOrLine(StringBuilder builder, double radius, double targetX, double targetY)
    {
        if (radius <= 0)
        {
            builder.Append(" L ").Append(CssNumber(targetX)).Append(' ').Append(CssNumber(targetY));
            return;
        }

        builder.Append(" A ")
            .Append(CssNumber(radius)).Append(' ').Append(CssNumber(radius))
            .Append(" 0 0 1 ")
            .Append(CssNumber(targetX)).Append(' ').Append(CssNumber(targetY));
    }

    private static Rect ValidateRect(Rect rect, string geometryName)
    {
        var x = ValidateFinite(rect.X, $"{geometryName}.Rect.X");
        var y = ValidateFinite(rect.Y, $"{geometryName}.Rect.Y");
        var width = ValidateNonNegativeFinite(rect.Width, $"{geometryName}.Rect.Width");
        var height = ValidateNonNegativeFinite(rect.Height, $"{geometryName}.Rect.Height");
        return new Rect(x, y, width, height);
    }

    private static Point ValidatePoint(Point point, string pointName)
    {
        var x = ValidateFinite(point.X, $"{pointName}.X");
        var y = ValidateFinite(point.Y, $"{pointName}.Y");
        return new Point(x, y);
    }

    private static double NormalizeCornerRadius(double value, Rect rect, string cornerName)
    {
        var radius = ValidateNonNegativeFinite(value, cornerName);
        var maxRadius = Math.Min(rect.Width, rect.Height) * 0.5;
        return Math.Min(radius, maxRadius);
    }

    private static double ValidateFinite(double value, string memberName)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidOperationException($"Mirage clip projection requires finite value for '{memberName}'.");
        }

        return value;
    }

    private static double ValidateNonNegativeFinite(double value, string memberName)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            throw new InvalidOperationException(
                $"Mirage clip projection requires non-negative finite value for '{memberName}'.");
        }

        return value;
    }

    private readonly record struct ClipPathProjection(string PathData, bool UseEvenOdd)
    {
        public string ToCssValue()
        {
            return UseEvenOdd
                ? $"path(evenodd, '{PathData}')"
                : $"path('{PathData}')";
        }
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

    private sealed class TransformCompositionState
    {
        public string? LayoutTransform { get; set; }

        public string? ViewTransform { get; set; }
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
