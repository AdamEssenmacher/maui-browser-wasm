using Mirage.Core.Semantics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageImageHandler : MirageViewHandler<Image>
{
    public static IPropertyMapper<Image, MirageImageHandler> Mapper =
        new PropertyMapper<Image, MirageImageHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(Image.Source)] = MapSource,
            [nameof(Image.Aspect)] = MapAspect,
            [nameof(Image.IsOpaque)] = MapIsOpaque
        };

    public MirageImageHandler()
        : base(ComponentKind.Image, Mapper)
    {
        DefaultDisplayStyle = "block";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        var changed = SetStyleIfChanged("display", "block");
        changed |= SetStyleIfChanged("width", "100%");
        changed |= SetStyleIfChanged("height", "100%");
        changed |= SetStyleIfChanged("boxSizing", "border-box");
        changed |= SetStyleIfChanged("objectPosition", "center center");
        changed |= UpdateSourceProjection(VirtualView);
        changed |= UpdateAspectProjection(VirtualView);
        changed |= UpdateOpaqueProjection(VirtualView);
        base.ConnectHandler(platformView);
        if (changed)
        {
            RequestRender();
        }
    }

    public static void MapSource(MirageImageHandler handler, Image image)
    {
        if (!handler.UpdateSourceProjection(image))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapAspect(MirageImageHandler handler, Image image)
    {
        if (!handler.UpdateAspectProjection(image))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapIsOpaque(MirageImageHandler handler, Image image)
    {
        if (!handler.UpdateOpaqueProjection(image))
        {
            return;
        }

        handler.RequestRender();
    }

    private bool UpdateSourceProjection(Image image)
    {
        var projection = MirageImageSourceProjection.BuildProjection(image.Source, image);
        var changed = SetPropertyIfChanged("Source", projection.SourceValue);
        changed |= SetPropertyIfChanged("SourceKind", projection.SourceKind);
        changed |= SetPropertyIfChanged("SourceState", projection.InitialState);
        return changed;
    }

    private bool UpdateAspectProjection(Image image)
    {
        var objectFit = MirageImageSourceProjection.ResolveObjectFit(image.Aspect);
        var changed = SetStyleIfChanged("objectFit", objectFit);
        changed |= SetStyleIfChanged("objectPosition", "center center");
        return changed;
    }

    private bool UpdateOpaqueProjection(Image image)
    {
        return SetPropertyIfChanged("IsOpaque", image.IsOpaque ? "true" : null);
    }

    private bool SetPropertyIfChanged(string propertyName, string? value)
    {
        if (string.Equals(PlatformView.GetPropertyString(propertyName), value, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetProperty(propertyName, value);
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
}
