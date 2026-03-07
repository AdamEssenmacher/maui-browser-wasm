using System.Globalization;
using Maui.Browser.Dom;
using Maui.Browser.Utils;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform;

namespace Maui.Browser.Handlers;

public sealed class BrowserBorderHandler : BrowserViewHandler<Border>
{
    public static IPropertyMapper<Border, BrowserBorderHandler> Mapper = new PropertyMapper<Border, BrowserBorderHandler>(BrowserViewMapper.CommonViewMapper)
    {
        [nameof(Border.Content)] = MapContent,
        [nameof(Border.Padding)] = MapPadding,
        [nameof(Border.Stroke)] = MapStroke,
        [nameof(Border.StrokeThickness)] = MapStrokeThickness,
        [nameof(Border.StrokeShape)] = MapStrokeShape,
        [nameof(Border.StrokeDashArray)] = MapStrokeDashArray,
        [nameof(Border.StrokeDashOffset)] = MapStrokeDashOffset
    };

    private IView? _currentContent;
    private IElementHandler? _currentContentHandler;

#if DEBUG
    private static readonly HashSet<string> UnsupportedWarnings = new();
#endif

    public BrowserBorderHandler()
        : base(Mapper)
    {
    }

    protected override DomElement CreatePlatformView() => DomElement.Create("div");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Style("boxSizing", "border-box");
        platformView.Style("overflow", "visible");
        SetContent(VirtualView.Content);
        ApplyStrokeStyles(VirtualView);
        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(DomElement platformView)
    {
        DisconnectCurrentContent();
        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        return VirtualView.CrossPlatformMeasure(widthConstraint, heightConstraint);
    }

    public override void PlatformArrange(Rect frame)
    {
        base.PlatformArrange(frame);
        VirtualView.CrossPlatformArrange(new Rect(0, 0, frame.Width, frame.Height));
    }

    public static void MapContent(BrowserBorderHandler handler, Border border)
    {
        handler.SetContent(border.Content);
    }

    public static void MapPadding(BrowserBorderHandler handler, Border border)
    {
        // Border content is arranged by MAUI layout (including padding offsets),
        // so adding CSS padding here would double-inset child content.
        handler.PlatformView.Style("padding", null);
    }

    public static void MapStroke(BrowserBorderHandler handler, Border border)
    {
        handler.ApplyStrokeStyles(border);
    }

    public static void MapStrokeThickness(BrowserBorderHandler handler, Border border)
    {
        handler.ApplyStrokeStyles(border);
    }

    public static void MapStrokeShape(BrowserBorderHandler handler, Border border)
    {
        handler.ApplyStrokeStyles(border);
    }

    public static void MapStrokeDashArray(BrowserBorderHandler handler, Border border)
    {
        handler.ApplyStrokeStyles(border);
    }

    public static void MapStrokeDashOffset(BrowserBorderHandler handler, Border border)
    {
        handler.ApplyStrokeStyles(border);
    }

    private void SetContent(IView? content)
    {
        if (_currentContentHandler is not null && !ReferenceEquals(_currentContent, content))
        {
            DisconnectCurrentContent();
        }

        PlatformView.ClearChildren();

        if (content is null || MauiContext is null)
        {
            return;
        }

        var contentHandler = content.ToHandler(MauiContext);
        if (contentHandler.PlatformView is not DomElement childElement)
        {
            return;
        }

        PlatformView.Append(childElement);
        _currentContent = content;
        _currentContentHandler = contentHandler;
    }

    private void DisconnectCurrentContent()
    {
        if (_currentContentHandler is null)
        {
            _currentContent = null;
            return;
        }

        _currentContentHandler.DisconnectHandler();
        _currentContentHandler = null;
        _currentContent = null;
    }

    private void ApplyStrokeStyles(Border border)
    {
        var borderThickness = NormalizeThickness(border.StrokeThickness);
        var borderColor = ResolveStrokeColor(border.Stroke, border);
        var borderStyle = ResolveBorderStyle(border.StrokeDashArray);
        var borderRadius = ResolveBorderRadius(border.StrokeShape, border);

        if (borderColor is null || borderThickness <= 0)
        {
            PlatformView.Style("borderStyle", "none");
            PlatformView.Style("borderColor", null);
            PlatformView.Style("borderWidth", null);
        }
        else
        {
            PlatformView.Style("borderStyle", borderStyle);
            PlatformView.Style("borderColor", borderColor);
            PlatformView.Style("borderWidth", CssUtil.Px(borderThickness));
        }

        PlatformView.Style("borderRadius", borderRadius);

        if (border.StrokeDashOffset > 0 && double.IsFinite(border.StrokeDashOffset))
        {
            PlatformView.Attr("data-maui-border-dash-offset", border.StrokeDashOffset.ToString("0.###", CultureInfo.InvariantCulture));
        }
        else
        {
            PlatformView.Attr("data-maui-border-dash-offset", null);
        }
    }

    private static double NormalizeThickness(double strokeThickness)
    {
        if (!double.IsFinite(strokeThickness))
        {
            return 0;
        }

        return Math.Max(0, strokeThickness);
    }

    private static string ResolveBorderStyle(DoubleCollection? dashArray)
    {
        if (dashArray is null || dashArray.Count == 0)
        {
            return "solid";
        }

        return "dashed";
    }

    private static string? ResolveStrokeColor(Brush? stroke, Border border)
    {
        if (stroke is null)
        {
            return null;
        }

        if (stroke is SolidColorBrush solidColorBrush)
        {
            return CssUtil.ToCssColor(solidColorBrush.Color);
        }

        ReportUnsupported(nameof(Border.Stroke), border, stroke);
        return null;
    }

    private static string? ResolveBorderRadius(IShape? shape, Border border)
    {
        if (shape is null)
        {
            return null;
        }

        if (shape is Rectangle)
        {
            return CssUtil.Px(0);
        }

        if (shape is RoundRectangle roundRectangle)
        {
            var cornerRadius = roundRectangle.CornerRadius;
            var topLeft = CssUtil.Px(Math.Max(0, NormalizeFinite(cornerRadius.TopLeft)));
            var topRight = CssUtil.Px(Math.Max(0, NormalizeFinite(cornerRadius.TopRight)));
            var bottomRight = CssUtil.Px(Math.Max(0, NormalizeFinite(cornerRadius.BottomRight)));
            var bottomLeft = CssUtil.Px(Math.Max(0, NormalizeFinite(cornerRadius.BottomLeft)));
            return $"{topLeft} {topRight} {bottomRight} {bottomLeft}";
        }

        ReportUnsupported(nameof(Border.StrokeShape), border, shape);
        return null;
    }

    private static double NormalizeFinite(double value)
    {
        return double.IsFinite(value) ? value : 0;
    }

    private static void ReportUnsupported(string propertyName, Border border, object? value)
    {
#if DEBUG
        var key = $"{propertyName}|{border.GetType().FullName}|{value?.GetType().FullName}";
        if (UnsupportedWarnings.Add(key))
        {
            Console.WriteLine(
                $"[Maui.Browser] Unsupported border mapping for {propertyName} on {border.GetType().Name}. Value type: {value?.GetType().Name ?? "<null>"}.");
        }
#endif
    }
}
