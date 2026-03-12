using System.Globalization;
using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Showcase.Fluent.App;

public sealed class ShowcaseVendorWidgetHandler : MirageViewHandler<ShowcaseVendorWidget>, IMirageRuntimeEventTarget
{
    public static readonly IPropertyMapper<ShowcaseVendorWidget, ShowcaseVendorWidgetHandler> Mapper =
        new PropertyMapper<ShowcaseVendorWidget, ShowcaseVendorWidgetHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(ShowcaseVendorWidget.Text)] = MapText,
            [nameof(ShowcaseVendorWidget.ChipPadding)] = MapChipPadding,
            [nameof(ShowcaseVendorWidget.ChipCornerRadius)] = MapChipCornerRadius,
            [nameof(ShowcaseVendorWidget.ChipBorderWidth)] = MapChipBorder,
            [nameof(ShowcaseVendorWidget.ChipBorderColor)] = MapChipBorder,
            [nameof(ShowcaseVendorWidget.ChipBackgroundColor)] = MapChipBackgroundColor,
            [nameof(ShowcaseVendorWidget.ChipTextColor)] = MapChipTextColor
        };

    public ShowcaseVendorWidgetHandler()
        : base(() => MiragePlatformView.CreateWeb("vendor-pill"), Mapper)
    {
        DefaultDisplayStyle = "inline-flex";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        platformView.SetAttribute("role", "button");
        platformView.SetAttribute("tabindex", "0");
        platformView.SetEvent("click", enabled: true);
        platformView.SetStyle("display", "inline-flex");
        platformView.SetStyle("alignItems", "center");
        platformView.SetStyle("cursor", "pointer");
        MapText(this, VirtualView);
        MapChipPadding(this, VirtualView);
        MapChipCornerRadius(this, VirtualView);
        MapChipBorder(this, VirtualView);
        MapChipBackgroundColor(this, VirtualView);
        MapChipTextColor(this, VirtualView);
        base.ConnectHandler(platformView);
    }

    public static void MapText(ShowcaseVendorWidgetHandler handler, ShowcaseVendorWidget view)
    {
        SetPropertyIfChanged(handler, "textContent", view.Text);
    }

    public static void MapChipPadding(ShowcaseVendorWidgetHandler handler, ShowcaseVendorWidget view)
    {
        var padding = view.ChipPadding;
        SetStyleIfChanged(handler, "padding", ToCssThickness(padding));
    }

    public static void MapChipCornerRadius(ShowcaseVendorWidgetHandler handler, ShowcaseVendorWidget view)
    {
        var value = double.IsFinite(view.ChipCornerRadius)
            ? Math.Max(0, view.ChipCornerRadius)
            : 0;
        SetStyleIfChanged(handler, "borderRadius", ToPx(value));
    }

    public static void MapChipBorder(ShowcaseVendorWidgetHandler handler, ShowcaseVendorWidget view)
    {
        var width = double.IsFinite(view.ChipBorderWidth)
            ? Math.Max(0, view.ChipBorderWidth)
            : 0;
        var color = ToCssColor(view.ChipBorderColor);
        var borderValue = width > 0 && !string.IsNullOrWhiteSpace(color)
            ? $"{ToPx(width)} solid {color}"
            : null;
        SetStyleIfChanged(handler, "border", borderValue);
    }

    public static void MapChipBackgroundColor(ShowcaseVendorWidgetHandler handler, ShowcaseVendorWidget view)
    {
        SetStyleIfChanged(handler, "background", ToCssColor(view.ChipBackgroundColor));
    }

    public static void MapChipTextColor(ShowcaseVendorWidgetHandler handler, ShowcaseVendorWidget view)
    {
        SetStyleIfChanged(handler, "color", ToCssColor(view.ChipTextColor));
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        _ = payload;
        if (!string.Equals(eventName, "click", StringComparison.Ordinal))
        {
            return;
        }

        VirtualView.SendClicked();
    }

    private static void SetPropertyIfChanged(ShowcaseVendorWidgetHandler handler, string propertyName, string? value)
    {
        if (string.Equals(handler.PlatformView.GetPropertyString(propertyName), value, StringComparison.Ordinal))
        {
            return;
        }

        handler.PlatformView.SetProperty(propertyName, MirageValue.String(value));
        handler.RequestRender();
    }

    private static void SetStyleIfChanged(ShowcaseVendorWidgetHandler handler, string styleName, string? value)
    {
        if (string.Equals(handler.PlatformView.GetStyleValue(styleName), value, StringComparison.Ordinal))
        {
            return;
        }

        handler.PlatformView.SetStyle(styleName, value);
        handler.RequestRender();
    }

    private static string ToPx(double value)
    {
        var normalized = double.IsFinite(value) ? Math.Max(0, value) : 0;
        return $"{normalized:0.###}px";
    }

    private static string ToCssThickness(Thickness thickness)
    {
        return $"{ToPx(thickness.Top)} {ToPx(thickness.Right)} {ToPx(thickness.Bottom)} {ToPx(thickness.Left)}";
    }

    private static string? ToCssColor(Color? color)
    {
        if (color is null)
        {
            return null;
        }

        var red = ToByte(color.Red);
        var green = ToByte(color.Green);
        var blue = ToByte(color.Blue);
        var alpha = Math.Clamp((double)color.Alpha, 0, 1);
        return $"rgba({red},{green},{blue},{alpha.ToString("0.###", CultureInfo.InvariantCulture)})";
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
}
