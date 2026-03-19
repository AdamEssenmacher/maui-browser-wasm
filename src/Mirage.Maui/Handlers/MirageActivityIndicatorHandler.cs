using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageActivityIndicatorHandler : MirageViewHandler<IActivityIndicator>
{
    private const string DefaultSpinnerHeight = "24px";

    public static IPropertyMapper<IActivityIndicator, MirageActivityIndicatorHandler> Mapper =
        new PropertyMapper<IActivityIndicator, MirageActivityIndicatorHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(IActivityIndicator.Color)] = MapColor,
            [nameof(IActivityIndicator.IsRunning)] = MapIsRunning
        };

    public MirageActivityIndicatorHandler()
        : base(ComponentKind.ActivityIndicator, Mapper)
    {
        DefaultDisplayStyle = "inline-flex";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        PlatformView.SetStyle("display", "inline-flex");
        PlatformView.SetStyle("alignItems", "center");
        PlatformView.SetStyle("justifyContent", "center");
        PlatformView.SetStyle("boxSizing", "border-box");
        PlatformView.SetStyle("height", DefaultSpinnerHeight);

        UpdateColorProjection(VirtualView);
        UpdateRunningProjection(VirtualView);

        base.ConnectHandler(platformView);
        RequestRender();
    }

    public static void MapColor(MirageActivityIndicatorHandler handler, IActivityIndicator activityIndicator)
    {
        if (!handler.UpdateColorProjection(activityIndicator))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapIsRunning(MirageActivityIndicatorHandler handler, IActivityIndicator activityIndicator)
    {
        if (!handler.UpdateRunningProjection(activityIndicator))
        {
            return;
        }

        handler.RequestRender();
    }

    private bool UpdateColorProjection(IActivityIndicator activityIndicator)
    {
        var cssColor = MirageCommonViewProjection.ToCssColor(activityIndicator.Color);
        return SetPropertyIfChanged("SpinnerColor", cssColor);
    }

    private bool UpdateRunningProjection(IActivityIndicator activityIndicator)
    {
        var isRunning = activityIndicator.IsRunning;
        var changed = SetPropertyIfChanged("IsRunning", isRunning ? "true" : "false");
        changed |= SetPropertyIfChanged("BusyState", isRunning ? "true" : "false");
        return changed;
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
}
