using Mirage.Maui.Handlers;
using Microsoft.Maui.Controls;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class HandlerLifecycleSafetyTests
{
    [Fact]
    public void ContentPage_MapContent_DoesNotThrow_WhenPlatformViewIsUnavailable()
    {
        var handler = new MirageContentPageHandler();
        var page = new ContentPage
        {
            Content = new Label { Text = "Page content probe" }
        };

        var exception = Record.Exception(() => MirageContentPageHandler.MapContent(handler, page));
        Assert.Null(exception);
    }

    [Fact]
    public void ContentPage_MapPadding_DoesNotThrow_WhenPlatformViewIsUnavailable()
    {
        var handler = new MirageContentPageHandler();
        var page = new ContentPage
        {
            Padding = new Thickness(12)
        };

        var exception = Record.Exception(() => MirageContentPageHandler.MapPadding(handler, page));
        Assert.Null(exception);
    }

    [Fact]
    public void ContentView_MapContent_DoesNotThrow_WhenPlatformViewIsUnavailable()
    {
        var handler = new MirageContentViewHandler();
        var contentView = new ContentView
        {
            Content = new Label { Text = "ContentView content probe" }
        };

        var exception = Record.Exception(() => MirageContentViewHandler.MapContent(handler, contentView));
        Assert.Null(exception);
    }

    [Fact]
    public void ContentView_MapPadding_DoesNotThrow_WhenPlatformViewIsUnavailable()
    {
        var handler = new MirageContentViewHandler();
        var contentView = new ContentView
        {
            Padding = new Thickness(8)
        };

        var exception = Record.Exception(() => MirageContentViewHandler.MapPadding(handler, contentView));
        Assert.Null(exception);
    }
}

