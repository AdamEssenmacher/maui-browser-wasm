using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageCheckBoxHandler : MirageViewHandler<ICheckBox>, IMirageRuntimeEventTarget
{
    public static IPropertyMapper<ICheckBox, MirageCheckBoxHandler> Mapper =
        new PropertyMapper<ICheckBox, MirageCheckBoxHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(ICheckBox.IsChecked)] = MapIsChecked,
            [nameof(ICheckBox.Foreground)] = MapForeground,
            [nameof(IView.IsEnabled)] = MapIsEnabled,
            ["IsEnabled"] = MapIsEnabled,
            ["Color"] = MapColor
        };

    public MirageCheckBoxHandler()
        : base(ComponentKind.CheckBox, Mapper)
    {
        DefaultDisplayStyle = "inline-flex";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        PlatformView.SetStyle("display", "inline-flex");
        PlatformView.SetEvent("change", enabled: true);
        UpdateEnabledProjection(VirtualView);
        UpdateForeground(VirtualView);
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

        var nextValue = string.Equals(payload, "true", StringComparison.OrdinalIgnoreCase);
        if (VirtualView.IsChecked == nextValue)
        {
            return;
        }

        VirtualView.IsChecked = nextValue;
    }

    public static void MapIsChecked(MirageCheckBoxHandler handler, ICheckBox checkBox)
    {
        if (handler.PlatformView.GetPropertyString("IsChecked") == (checkBox.IsChecked ? "true" : "false"))
        {
            return;
        }

        handler.PlatformView.SetProperty("IsChecked", checkBox.IsChecked ? "true" : "false");
        handler.RequestRender();
    }

    public static void MapForeground(MirageCheckBoxHandler handler, ICheckBox checkBox)
    {
        if (!handler.UpdateForeground(checkBox))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapColor(MirageCheckBoxHandler handler, ICheckBox checkBox)
    {
        if (!handler.UpdateForeground(checkBox))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapIsEnabled(MirageCheckBoxHandler handler, ICheckBox checkBox)
    {
        if (!handler.UpdateEnabledProjection(checkBox))
        {
            return;
        }

        handler.RequestRender();
    }

    private bool UpdateForeground(ICheckBox checkBox)
    {
        var foreground = checkBox.Foreground;
        var cssColor = foreground switch
        {
            SolidPaint solidPaint => MirageCommonViewProjection.ToCssColor(solidPaint.Color),
            _ => TryGetCompatibilityColor(checkBox)
        };

        return SetStyleIfChanged("accentColor", cssColor);
    }

    private bool UpdateEnabledProjection(ICheckBox checkBox)
    {
        var isEnabled = checkBox.IsEnabled;
        var changed = SetStyleIfChanged("cursor", isEnabled ? "pointer" : "default");
        changed |= SetStyleIfChanged("pointerEvents", isEnabled ? "auto" : "none");
        changed |= SetStyleIfChanged("opacity", isEnabled ? null : "0.6");
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

    private static string? TryGetCompatibilityColor(ICheckBox checkBox)
    {
        if (checkBox is not CheckBox compatCheckBox)
        {
            return null;
        }

        return MirageCommonViewProjection.ToCssColor(compatCheckBox.Color);
    }
}
