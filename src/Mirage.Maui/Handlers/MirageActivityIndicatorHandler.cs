using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Controls;

namespace Mirage.Maui.Handlers;

internal sealed class MirageActivityIndicatorHandler : MirageViewHandler<IActivityIndicator>
{
    private const string DefaultSpinnerHeight = "24px";

    public static IPropertyMapper<IActivityIndicator, MirageActivityIndicatorHandler> Mapper =
        new PropertyMapper<IActivityIndicator, MirageActivityIndicatorHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(IView.Visibility)] = MapVisibility,
            ["IsVisible"] = MapVisibility,
            [nameof(VisualElement.WidthRequest)] = MapSizeRequestsAndLayoutAlignment,
            [nameof(VisualElement.HeightRequest)] = MapSizeRequestsAndLayoutAlignment,
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
        PlatformView.SetStyle("display", DefaultDisplayStyle);
        PlatformView.SetStyle("alignItems", "center");
        PlatformView.SetStyle("justifyContent", "center");
        PlatformView.SetStyle("boxSizing", "border-box");
        PlatformView.SetStyle("height", DefaultSpinnerHeight);

        UpdateColorProjection(VirtualView);
        UpdateRunningProjection(VirtualView);
        UpdateVisibilityProjection(VirtualView);

        base.ConnectHandler(platformView);
        RequestRender();
    }

    public static void MapVisibility(MirageActivityIndicatorHandler handler, IActivityIndicator activityIndicator)
    {
        if (!handler.UpdateVisibilityProjection(activityIndicator))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapColor(MirageActivityIndicatorHandler handler, IActivityIndicator activityIndicator)
    {
        if (!handler.UpdateColorProjection(activityIndicator))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapSizeRequestsAndLayoutAlignment(MirageActivityIndicatorHandler handler, IActivityIndicator activityIndicator)
    {
        var changed = MirageCommonViewProjection.MapSizeRequests(handler, activityIndicator);
        changed |= MirageCommonViewProjection.MapLayoutAlignment(handler, activityIndicator);
        changed |= handler.ApplyDefaultHeightIfUnspecified(activityIndicator);
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapIsRunning(MirageActivityIndicatorHandler handler, IActivityIndicator activityIndicator)
    {
        var changed = handler.UpdateRunningProjection(activityIndicator);
        changed |= handler.UpdateVisibilityProjection(activityIndicator);
        if (!changed)
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

    private bool UpdateVisibilityProjection(IActivityIndicator activityIndicator)
    {
        if (!activityIndicator.IsRunning)
        {
            var hiddenChanged = SetStyleIfChanged("display", "none");
            hiddenChanged |= SetStyleIfChanged("visibility", "hidden");
            return hiddenChanged;
        }

        string? display;
        string? visibility;

        switch (activityIndicator.Visibility)
        {
            case Visibility.Visible:
                display = DefaultDisplayStyle;
                visibility = "visible";
                break;
            case Visibility.Hidden:
                display = DefaultDisplayStyle;
                visibility = "hidden";
                break;
            default:
                display = "none";
                visibility = "hidden";
                break;
        }

        var changed = SetStyleIfChanged("display", display);
        changed |= SetStyleIfChanged("visibility", visibility);
        return changed;
    }

    private bool ApplyDefaultHeightIfUnspecified(IActivityIndicator activityIndicator)
    {
        if (activityIndicator is VisualElement visualElement
            && double.IsFinite(visualElement.HeightRequest)
            && visualElement.HeightRequest >= 0)
        {
            return false;
        }

        return SetStyleIfChanged("height", DefaultSpinnerHeight);
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

    private bool SetStyleIfChanged(string propertyName, string? value)
    {
        if (string.Equals(PlatformView.GetStyleValue(propertyName), value, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetStyle(propertyName, value);
        return true;
    }
}
