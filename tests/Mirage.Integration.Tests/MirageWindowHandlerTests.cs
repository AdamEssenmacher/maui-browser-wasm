using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Mirage.Maui.Hosting;
using Mirage.Web.Runtime;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class MirageWindowHandlerTests
{
    [Fact]
    public void WindowHandler_SettingPageToNull_FiresDisappearingOnPreviousPage()
    {
        var page = new LifecycleProbePage();
        var window = new Window(page);
        var handler = CreateAttachedWindowHandler(window);
        EnsureAppeared(page);

        Assert.Equal(1, page.AppearingCount);
        Assert.Equal(0, page.DisappearingCount);

        window.Page = null;
        MirageWindowHandler.MapPage(handler, window);

        Assert.Equal(1, page.AppearingCount);
        Assert.Equal(1, page.DisappearingCount);
    }

    [Fact]
    public void WindowHandler_DisconnectHandler_FiresDisappearingOnCurrentPage()
    {
        var page = new LifecycleProbePage();
        var window = new Window(page);
        var handler = CreateAttachedWindowHandler(window);
        EnsureAppeared(page);

        Assert.Equal(1, page.AppearingCount);
        Assert.Equal(0, page.DisappearingCount);

        MirageHandlerDisconnect.SafeDisconnect(handler);

        Assert.Equal(1, page.AppearingCount);
        Assert.Equal(1, page.DisappearingCount);
    }

    private static MirageWindowHandler CreateAttachedWindowHandler(Window window)
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        coordinator.AttachRuntime(new RuntimeProbe());

        var builder = MauiApp.CreateBuilder();
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler(typeof(Window), typeof(MirageWindowHandler));
            handlers.AddHandler(typeof(Page), typeof(MiragePageHandler));
            handlers.AddHandler(typeof(ContentPage), typeof(MirageContentPageHandler));
        });
        builder.Services.AddSingleton<IMirageRenderCoordinator>(coordinator);

        var mauiApp = builder.Build();
        var mauiContext = new MauiContext(mauiApp.Services);

        var handler = new MirageWindowHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(window);
        handler.ArrangeWindow(1024, 768);
        return handler;
    }

    private static void EnsureAppeared(LifecycleProbePage page)
    {
        if (page.AppearingCount == 0)
        {
            page.ForceAppearedForTest();
        }
    }

    private sealed class LifecycleProbePage : ContentPage
    {
        public int AppearingCount { get; private set; }

        public int DisappearingCount { get; private set; }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            AppearingCount++;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            DisappearingCount++;
        }

        public void ForceAppearedForTest()
        {
            var hasAppearedField = typeof(Page).GetField("_hasAppeared", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(hasAppearedField);
            hasAppearedField!.SetValue(this, true);
            OnAppearing();
        }
    }

    private sealed class RuntimeProbe : IMirageRuntime
    {
        public void SetEventSink(IMirageEventSink? eventSink)
        {
            _ = eventSink;
        }

        public void Render(NodeBase nextRoot)
        {
            _ = nextRoot;
        }

        public void SetViewport(double width, double height)
        {
            _ = width;
            _ = height;
        }

        public string? TryGetNodeInputValue(string nodeId)
        {
            _ = nodeId;
            return null;
        }

        public bool TryGetActiveNodeId(out string? nodeId)
        {
            nodeId = null;
            return false;
        }

        public bool TryGetNodeScrollPosition(string nodeId, out double left, out double top)
        {
            _ = nodeId;
            left = 0;
            top = 0;
            return false;
        }

        public bool TrySetNodeScrollPosition(string nodeId, double left, double top, out double actualLeft, out double actualTop)
        {
            _ = nodeId;
            _ = left;
            _ = top;
            actualLeft = 0;
            actualTop = 0;
            return false;
        }

        public bool TryScrollNodeChildIntoView(
            string containerNodeId,
            string childNodeId,
            int scrollToPosition,
            bool isHorizontalOrientation,
            bool isAnimated,
            out double actualLeft,
            out double actualTop)
        {
            _ = containerNodeId;
            _ = childNodeId;
            _ = scrollToPosition;
            _ = isHorizontalOrientation;
            _ = isAnimated;
            actualLeft = 0;
            actualTop = 0;
            return false;
        }

        public bool TryRequestNodePickerOpen(string nodeId)
        {
            _ = nodeId;
            return false;
        }

        public bool TrySetNodeFocus(string nodeId, bool isFocused)
        {
            _ = nodeId;
            _ = isFocused;
            return false;
        }

        public bool TryGetNodeTextSelection(string nodeId, out int selectionStart, out int selectionEnd)
        {
            _ = nodeId;
            selectionStart = 0;
            selectionEnd = 0;
            return false;
        }

        public bool TrySetNodeTextSelection(string nodeId, int selectionStart, int selectionEnd)
        {
            _ = nodeId;
            _ = selectionStart;
            _ = selectionEnd;
            return false;
        }

        public void Dispose()
        {
        }
    }
}
