using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Microsoft.Maui.Handlers;

namespace Showcase.Fluent.App;

public sealed class ShowcaseVendorWidgetHandler : MirageViewHandler<ShowcaseVendorWidget>, IMirageRuntimeEventTarget
{
    public static readonly IPropertyMapper<ShowcaseVendorWidget, ShowcaseVendorWidgetHandler> Mapper =
        new PropertyMapper<ShowcaseVendorWidget, ShowcaseVendorWidgetHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(ShowcaseVendorWidget.Text)] = MapText
        };

    public ShowcaseVendorWidgetHandler()
        : base(() => MiragePlatformView.CreateElement("vendor-pill"), Mapper)
    {
        DefaultDisplayStyle = "inline-flex";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        platformView.SetAttribute("role", "button");
        platformView.SetAttribute("tabindex", "0");
        platformView.AddClass("showcase-vendor-pill");
        platformView.SetEvent("click", enabled: true);
        platformView.SetStyle("display", "inline-flex");
        platformView.SetStyle("alignItems", "center");
        platformView.SetStyle("padding", "6px 12px");
        platformView.SetStyle("borderRadius", "999px");
        platformView.SetStyle("cursor", "pointer");
        platformView.SetStyle("border", "1px solid rgba(0, 95, 128, 0.35)");
        platformView.SetStyle("background", "rgba(0, 95, 128, 0.08)");
        platformView.SetStyle("color", "#003b5c");
        MapText(this, VirtualView);
        base.ConnectHandler(platformView);
        RequestRender();
    }

    public static void MapText(ShowcaseVendorWidgetHandler handler, ShowcaseVendorWidget view)
    {
        handler.PlatformView.SetProperty("textContent", MirageValue.String(view.Text));
        handler.RequestRender();
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
}
