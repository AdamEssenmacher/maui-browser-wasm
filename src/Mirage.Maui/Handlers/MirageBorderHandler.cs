using System.Globalization;
using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageBorderHandler : MirageContentViewHandlerBase<Border>
{
    public static IPropertyMapper<Border, MirageBorderHandler> Mapper =
        new PropertyMapper<Border, MirageBorderHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(Border.Content)] = MapContent,
            [nameof(Border.Padding)] = MapPadding,
            [nameof(Border.Stroke)] = MapStroke,
            [nameof(Border.StrokeThickness)] = MapStrokeThickness,
            [nameof(Border.StrokeShape)] = MapStrokeShape,
            [nameof(Border.StrokeDashArray)] = MapStrokeDashArray,
            [nameof(Border.StrokeDashOffset)] = MapStrokeDashOffset
        };

#if DEBUG
    private static readonly HashSet<string> UnsupportedWarnings = new();
#endif

    public MirageBorderHandler()
        : base(Mapper, kind: ComponentKind.Border)
    {
        DefaultDisplayStyle = "grid";
    }

    protected override IPadding PaddingSource => VirtualView;

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        var changed = SetStyleIfChanged("boxSizing", "border-box");
        changed |= UpdateStrokeStyles(VirtualView);
        base.ConnectHandler(platformView);
        if (changed)
        {
            RequestRender();
        }
    }

    public static void MapContent(MirageBorderHandler handler, Border border)
    {
        handler.SetPresentedContent(((IContentView)border).PresentedContent);
    }

    public static void MapPadding(MirageBorderHandler handler, Border border)
    {
        MirageViewMapper.MapPadding(handler, border);
    }

    public static void MapStroke(MirageBorderHandler handler, Border border)
    {
        if (!handler.UpdateStrokeStyles(border))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapStrokeThickness(MirageBorderHandler handler, Border border)
    {
        if (!handler.UpdateStrokeStyles(border))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapStrokeShape(MirageBorderHandler handler, Border border)
    {
        if (!handler.UpdateStrokeStyles(border))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapStrokeDashArray(MirageBorderHandler handler, Border border)
    {
        if (!handler.UpdateStrokeStyles(border))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapStrokeDashOffset(MirageBorderHandler handler, Border border)
    {
        if (!handler.UpdateStrokeStyles(border))
        {
            return;
        }

        handler.RequestRender();
    }

    private bool UpdateStrokeStyles(Border border)
    {
        var borderThickness = NormalizeThickness(border.StrokeThickness);
        var borderColor = ResolveStrokeColor(border.Stroke, border);
        var borderStyle = ResolveBorderStyle(border.StrokeDashArray);
        var borderRadius = ResolveBorderRadius(border.StrokeShape, border);

        var changed = false;
        if (borderColor is null || borderThickness <= 0)
        {
            changed |= SetStyleIfChanged("borderStyle", "none");
            changed |= SetStyleIfChanged("borderColor", null);
            changed |= SetStyleIfChanged("borderWidth", null);
        }
        else
        {
            changed |= SetStyleIfChanged("borderStyle", borderStyle);
            changed |= SetStyleIfChanged("borderColor", borderColor);
            changed |= SetStyleIfChanged("borderWidth", MirageViewMapper.ToPx(borderThickness));
        }

        changed |= SetStyleIfChanged("borderRadius", borderRadius);

        if (border.StrokeDashOffset > 0 && double.IsFinite(border.StrokeDashOffset))
        {
            changed |= SetPropertyIfChanged("StrokeDashOffset", border.StrokeDashOffset.ToString("0.###", CultureInfo.InvariantCulture));
        }
        else
        {
            changed |= SetPropertyIfChanged("StrokeDashOffset", null);
        }

        return changed;
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
            return MirageCommonViewProjection.ToCssColor(solidColorBrush.Color);
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
            return MirageViewMapper.ToPx(0);
        }

        if (shape is RoundRectangle roundRectangle)
        {
            var cornerRadius = roundRectangle.CornerRadius;
            var topLeft = MirageViewMapper.ToPx(Math.Max(0, NormalizeFinite(cornerRadius.TopLeft)));
            var topRight = MirageViewMapper.ToPx(Math.Max(0, NormalizeFinite(cornerRadius.TopRight)));
            var bottomRight = MirageViewMapper.ToPx(Math.Max(0, NormalizeFinite(cornerRadius.BottomRight)));
            var bottomLeft = MirageViewMapper.ToPx(Math.Max(0, NormalizeFinite(cornerRadius.BottomLeft)));
            return $"{topLeft} {topRight} {bottomRight} {bottomLeft}";
        }

        ReportUnsupported(nameof(Border.StrokeShape), border, shape);
        return null;
    }

    private static double NormalizeFinite(double value)
    {
        return double.IsFinite(value) ? value : 0;
    }

    private bool SetPropertyIfChanged(string propertyName, string? propertyValue)
    {
        if (string.Equals(PlatformView.GetPropertyString(propertyName), propertyValue, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetProperty(propertyName, propertyValue);
        return true;
    }

    private bool SetStyleIfChanged(string styleName, string? styleValue)
    {
        if (string.Equals(PlatformView.GetStyleValue(styleName), styleValue, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetStyle(styleName, styleValue);
        return true;
    }

    private static void ReportUnsupported(string propertyName, Border border, object? value)
    {
#if DEBUG
        var key = $"{propertyName}|{border.GetType().FullName}|{value?.GetType().FullName}";
        if (UnsupportedWarnings.Add(key))
        {
            Console.WriteLine(
                $"[Mirage] Unsupported border mapping for {propertyName} on {border.GetType().Name}. Value type: {value?.GetType().Name ?? "<null>"}.");
        }
#endif
    }
}
