using Mirage.Core.Semantics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageRectangleHandler : MirageViewHandler<Rectangle>
{
    public static IPropertyMapper<Rectangle, MirageRectangleHandler> Mapper =
        new PropertyMapper<Rectangle, MirageRectangleHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(Shape.Fill)] = MapFill,
            [nameof(Shape.Stroke)] = MapStroke,
            [nameof(Shape.StrokeThickness)] = MapStroke
        };

    public MirageRectangleHandler()
        : base(ComponentKind.BoxView, Mapper)
    {
        DefaultDisplayStyle = "block";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        var changed = SetStyleIfChanged("display", DefaultDisplayStyle);
        base.ConnectHandler(platformView);
        changed |= UpdateFillStyles(VirtualView);
        changed |= UpdateStrokeStyles(VirtualView);
        if (changed)
        {
            RequestRender();
        }
    }

    public static void MapFill(MirageRectangleHandler handler, Rectangle rectangle)
    {
        if (!handler.UpdateFillStyles(rectangle))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapStroke(MirageRectangleHandler handler, Rectangle rectangle)
    {
        if (!handler.UpdateStrokeStyles(rectangle))
        {
            return;
        }

        handler.RequestRender();
    }

    private bool UpdateFillStyles(Rectangle rectangle)
    {
        string? cssColor = null;
        if (rectangle.Fill is SolidColorBrush solidBrush)
        {
            cssColor = MirageCommonViewProjection.ToCssColor(solidBrush.Color);
        }

        var changed = SetStyleIfChanged("backgroundColor", cssColor);
        if (cssColor is null)
        {
            changed |= SetStyleIfChanged("backgroundImage", null);
            changed |= SetStyleIfChanged("backgroundRepeat", null);
            changed |= SetStyleIfChanged("backgroundPosition", null);
            changed |= SetStyleIfChanged("backgroundSize", null);
        }

        return changed;
    }

    private bool UpdateStrokeStyles(Rectangle rectangle)
    {
        var strokeThickness = rectangle.StrokeThickness;
        string? strokeCssColor = null;
        if (rectangle.Stroke is SolidColorBrush solidBrush)
        {
            strokeCssColor = MirageCommonViewProjection.ToCssColor(solidBrush.Color);
        }

        if (!double.IsFinite(strokeThickness)
            || strokeThickness <= 0d
            || string.IsNullOrWhiteSpace(strokeCssColor))
        {
            var cleared = SetStyleIfChanged("borderStyle", null);
            cleared |= SetStyleIfChanged("borderColor", null);
            cleared |= SetStyleIfChanged("borderWidth", null);
            return cleared;
        }

        var changed = SetStyleIfChanged("borderStyle", "solid");
        changed |= SetStyleIfChanged("borderColor", strokeCssColor);
        changed |= SetStyleIfChanged("borderWidth", MirageViewMapper.ToPx(strokeThickness));
        return changed;
    }

    private bool SetStyleIfChanged(string name, string? value)
    {
        if (string.Equals(PlatformView.GetStyleValue(name), value, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetStyle(name, value);
        return true;
    }
}
