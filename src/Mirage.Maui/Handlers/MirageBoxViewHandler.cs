using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageBoxViewHandler : MirageViewHandler<BoxView>
{
    public static IPropertyMapper<BoxView, MirageBoxViewHandler> Mapper =
        new PropertyMapper<BoxView, MirageBoxViewHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(BoxView.Color)] = MapColor
        };

    public MirageBoxViewHandler()
        : base(ComponentKind.BoxView, Mapper)
    {
        DefaultDisplayStyle = "block";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        var changed = SetStyleIfChanged("display", "block");
        changed |= UpdateColorStyles(VirtualView);
        base.ConnectHandler(platformView);
        if (changed)
        {
            RequestRender();
        }
    }

    public static void MapColor(MirageBoxViewHandler handler, BoxView boxView)
    {
        if (!handler.UpdateColorStyles(boxView))
        {
            return;
        }

        handler.RequestRender();
    }

    private bool UpdateColorStyles(BoxView boxView)
    {
        var changed = MirageCommonViewProjection.MapBackground(this, boxView);

        if (boxView.IsSet(BoxView.ColorProperty))
        {
            var cssColor = MirageCommonViewProjection.ToCssColor(boxView.Color);
            changed |= SetStyleIfChanged("backgroundColor", cssColor);
            changed |= SetStyleIfChanged("backgroundImage", null);
            changed |= SetStyleIfChanged("backgroundSize", null);
            changed |= SetStyleIfChanged("backgroundRepeat", null);
            changed |= SetStyleIfChanged("backgroundPosition", null);
        }

        changed |= SetStyleIfChanged("borderRadius", null);
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
