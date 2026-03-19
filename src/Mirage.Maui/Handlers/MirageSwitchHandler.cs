using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageSwitchHandler : MirageViewHandler<ISwitch>, IMirageRuntimeEventTarget
{
    public static IPropertyMapper<ISwitch, MirageSwitchHandler> Mapper =
        new PropertyMapper<ISwitch, MirageSwitchHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(ISwitch.IsOn)] = MapIsOn,
            ["IsToggled"] = MapIsOn,
            [nameof(ISwitch.TrackColor)] = MapTrackColor,
            ["OnColor"] = MapTrackColor,
            [nameof(ISwitch.ThumbColor)] = MapThumbColor,
            [nameof(IView.IsEnabled)] = MapIsEnabled,
            ["IsEnabled"] = MapIsEnabled,
            [nameof(VisualElement.WidthRequest)] = MapSizeRequestsAndLayoutAlignment,
            [nameof(VisualElement.HeightRequest)] = MapSizeRequestsAndLayoutAlignment,
            [nameof(VisualElement.MinimumWidthRequest)] = MapSizeRequests,
            [nameof(VisualElement.MinimumHeightRequest)] = MapSizeRequests,
            [nameof(VisualElement.MaximumWidthRequest)] = MapSizeRequests,
            [nameof(VisualElement.MaximumHeightRequest)] = MapSizeRequests
        };

    public MirageSwitchHandler()
        : base(ComponentKind.Switch, Mapper)
    {
        DefaultDisplayStyle = "inline-flex";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        PlatformView.SetStyle("display", "inline-flex");
        PlatformView.SetStyle("boxSizing", "border-box");
        PlatformView.SetStyle("width", "52px");
        PlatformView.SetStyle("height", "32px");
        PlatformView.SetStyle("minWidth", "52px");
        PlatformView.SetStyle("minHeight", "32px");
        PlatformView.SetEvent("change", enabled: true);
        UpdateIsOnProjection(VirtualView);
        UpdateTrackColorProjection(VirtualView);
        UpdateThumbColorProjection(VirtualView);
        UpdateEnabledProjection(VirtualView);
        base.ConnectHandler(platformView);
        RequestRender();
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        if (!string.Equals(eventName, "change", StringComparison.Ordinal))
        {
            return;
        }

        if (!VirtualView.IsEnabled)
        {
            return;
        }

        var isOn = string.Equals(payload, "true", StringComparison.OrdinalIgnoreCase);
        if (VirtualView.IsOn == isOn)
        {
            return;
        }

        VirtualView.IsOn = isOn;
    }

    public static void MapIsOn(MirageSwitchHandler handler, ISwitch @switch)
    {
        if (!handler.UpdateIsOnProjection(@switch))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapTrackColor(MirageSwitchHandler handler, ISwitch @switch)
    {
        if (!handler.UpdateTrackColorProjection(@switch))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapThumbColor(MirageSwitchHandler handler, ISwitch @switch)
    {
        if (!handler.UpdateThumbColorProjection(@switch))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapIsEnabled(MirageSwitchHandler handler, ISwitch @switch)
    {
        if (!handler.UpdateEnabledProjection(@switch))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapSizeRequests(MirageSwitchHandler handler, ISwitch @switch)
    {
        if (!MirageCommonViewProjection.MapSizeRequests(handler, @switch))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapSizeRequestsAndLayoutAlignment(MirageSwitchHandler handler, ISwitch @switch)
    {
        var changed = MirageCommonViewProjection.MapSizeRequests(handler, @switch);
        changed |= MirageCommonViewProjection.MapLayoutAlignment(handler, @switch);
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
    }

    private bool UpdateIsOnProjection(ISwitch @switch)
    {
        return SetPropertyIfChanged("IsOn", @switch.IsOn ? "true" : "false");
    }

    private bool UpdateTrackColorProjection(ISwitch @switch)
    {
        var trackColor = MirageCommonViewProjection.ToCssColor(@switch.TrackColor) ?? TryGetCompatibilityOnColor(@switch);
        var changed = SetStyleIfChanged("--mirage-switch-track-on", trackColor);
        changed |= SetStyleIfChanged("--mirage-switch-track-off", "rgba(148,163,184,1)");
        return changed;
    }

    private bool UpdateThumbColorProjection(ISwitch @switch)
    {
        var thumbColor = MirageCommonViewProjection.ToCssColor(@switch.ThumbColor) ?? "rgba(255,255,255,1)";
        return SetStyleIfChanged("--mirage-switch-thumb", thumbColor);
    }

    private bool UpdateEnabledProjection(ISwitch @switch)
    {
        var isEnabled = @switch.IsEnabled;
        var changed = SetStyleIfChanged("cursor", isEnabled ? "pointer" : "default");
        changed |= SetStyleIfChanged("pointerEvents", isEnabled ? "auto" : "none");
        changed |= SetPropertyIfChanged("Disabled", isEnabled ? null : "disabled");
        return changed;
    }

    private bool SetStyleIfChanged(string styleName, string? value)
    {
        if (string.Equals(PlatformView.GetStyleValue(styleName), value, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetStyle(styleName, value);
        return true;
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

    private static string? TryGetCompatibilityOnColor(ISwitch @switch)
    {
        if (@switch is not Switch compatSwitch)
        {
            return null;
        }

        return MirageCommonViewProjection.ToCssColor(compatSwitch.OnColor);
    }
}
