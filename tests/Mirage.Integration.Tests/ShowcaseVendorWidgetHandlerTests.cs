using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Showcase.Fluent.App;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class ShowcaseVendorWidgetHandlerTests
{
    [Fact]
    public void VendorWidgetHandler_MapsStyleProperties_AndClearsDeterministically()
    {
        var handler = CreateHandler();
        var widget = new ShowcaseVendorWidget
        {
            Text = "Vendor",
            ChipPadding = new Thickness(12, 6),
            ChipCornerRadius = 24,
            ChipBorderWidth = 2,
            ChipBorderColor = Color.FromArgb("#801E40AF"),
            ChipBackgroundColor = Color.FromArgb("#332564EB"),
            ChipTextColor = Color.FromArgb("#1E3A8A")
        };

        ((IElementHandler)handler).SetVirtualView(widget);

        Assert.Equal("Vendor", handler.PlatformView.GetPropertyString("textContent"));
        Assert.Equal("6px 12px 6px 12px", handler.PlatformView.GetStyleValue("padding"));
        Assert.Equal("24px", handler.PlatformView.GetStyleValue("borderRadius"));
        Assert.Equal("2px solid rgba(30,64,175,0.502)", handler.PlatformView.GetStyleValue("border"));
        Assert.Equal("rgba(37,100,235,0.2)", handler.PlatformView.GetStyleValue("background"));
        Assert.Equal("rgba(30,58,138,1)", handler.PlatformView.GetStyleValue("color"));

        widget.ChipBorderWidth = 0;
        ShowcaseVendorWidgetHandler.MapChipBorder(handler, widget);
        Assert.Null(handler.PlatformView.GetStyleValue("border"));

        widget.ChipBackgroundColor = null;
        ShowcaseVendorWidgetHandler.MapChipBackgroundColor(handler, widget);
        Assert.Null(handler.PlatformView.GetStyleValue("background"));

        widget.ChipTextColor = null;
        ShowcaseVendorWidgetHandler.MapChipTextColor(handler, widget);
        Assert.Null(handler.PlatformView.GetStyleValue("color"));
    }

    [Fact]
    public void VendorWidgetHandler_ReapplyingSameValues_IsIdempotent()
    {
        var handler = CreateHandler();
        var widget = new ShowcaseVendorWidget
        {
            Text = "Stable",
            ChipPadding = new Thickness(14, 7),
            ChipCornerRadius = 30,
            ChipBorderWidth = 1.5,
            ChipBorderColor = Color.FromArgb("#663B82F6"),
            ChipBackgroundColor = Color.FromArgb("#1A0EA5E9"),
            ChipTextColor = Color.FromArgb("#0F172A")
        };

        ((IElementHandler)handler).SetVirtualView(widget);

        var revisionBefore = handler.PlatformView.LocalRevision;
        ShowcaseVendorWidgetHandler.MapText(handler, widget);
        ShowcaseVendorWidgetHandler.MapChipPadding(handler, widget);
        ShowcaseVendorWidgetHandler.MapChipCornerRadius(handler, widget);
        ShowcaseVendorWidgetHandler.MapChipBorder(handler, widget);
        ShowcaseVendorWidgetHandler.MapChipBackgroundColor(handler, widget);
        ShowcaseVendorWidgetHandler.MapChipTextColor(handler, widget);
        var revisionAfter = handler.PlatformView.LocalRevision;

        Assert.Equal(revisionBefore, revisionAfter);
    }

    private static ShowcaseVendorWidgetHandler CreateHandler()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var mauiContext = new MauiContext(services);
        var handler = new ShowcaseVendorWidgetHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        return handler;
    }
}
