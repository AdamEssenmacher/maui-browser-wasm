using Mirage.Core.Semantics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Mirage.Maui.Handlers;

internal sealed class MirageWindowHandler : ElementHandler<Window, MiragePlatformView>
{
    public static IPropertyMapper<Window, MirageWindowHandler> Mapper =
        new PropertyMapper<Window, MirageWindowHandler>(ElementMapper)
        {
            [nameof(Window.Page)] = MapPage
        };

    private Page? _currentPage;
    private IElementHandler? _currentPageHandler;
    private double _lastArrangedWidth;
    private double _lastArrangedHeight;
    private bool _hasArrangedWindowFrame;

    public MirageWindowHandler()
        : base(Mapper)
    {
    }

    protected override MiragePlatformView CreatePlatformElement() => new(ComponentKind.Root);

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        var coordinator = GetCoordinator();
        coordinator.Register(platformView, null);
        coordinator.SetRoot(platformView);

        ApplyWindowStyles();
        SetPage(VirtualView.Page);
        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        DisconnectCurrentPage();

        var coordinator = GetCoordinator();
        coordinator.SetRoot(null);
        coordinator.Unregister(platformView);

        base.DisconnectHandler(platformView);
    }

    public void ArrangeWindow(double width, double height)
    {
        _lastArrangedWidth = Math.Max(0, width);
        _lastArrangedHeight = Math.Max(0, height);
        _hasArrangedWindowFrame = true;

        if (VirtualView.Page is not IView pageView)
        {
            return;
        }

        pageView.Measure(_lastArrangedWidth, _lastArrangedHeight);
        pageView.Arrange(new Rect(0, 0, _lastArrangedWidth, _lastArrangedHeight));
    }

    public static void MapPage(MirageWindowHandler handler, Window window)
    {
        handler.SetPage(window.Page);
        handler.ArrangeCurrentPageIfPossible();
    }

    private void SetPage(Page? page)
    {
        if (_currentPageHandler is not null && !ReferenceEquals(_currentPage, page))
        {
            DisconnectCurrentPage();
        }

        PlatformView.ClearChildren();

        if (page is null || MauiContext is null)
        {
            GetCoordinator().RequestRender();
            return;
        }

        var pageHandler = page.ToHandler(MauiContext);
        if (pageHandler.PlatformView is not MiragePlatformView pagePlatformView)
        {
            throw new InvalidOperationException("MirageWindowHandler requires page handlers to expose MiragePlatformView.");
        }

        PlatformView.AddChild(pagePlatformView);
        _currentPage = page;
        _currentPageHandler = pageHandler;
        _ = MiragePageHandler.TryProjectActivePageTitle(pageHandler);

        GetCoordinator().RequestRender();
    }

    private void DisconnectCurrentPage()
    {
        MirageHandlerDisconnect.SafeDisconnect(_currentPageHandler);

        _currentPageHandler = null;
        _currentPage = null;
        PlatformView.ClearChildren();
        GetCoordinator().RequestRender();
    }

    private void ArrangeCurrentPageIfPossible()
    {
        if (VirtualView.Page is not IView)
        {
            return;
        }

        if (_hasArrangedWindowFrame)
        {
            ArrangeWindow(_lastArrangedWidth, _lastArrangedHeight);
            return;
        }

        ArrangeWindow(1024, 768);
    }

    private void ApplyWindowStyles()
    {
        PlatformView.SetStyle("display", "flex");
        PlatformView.SetStyle("flexDirection", "column");
        PlatformView.SetStyle("alignItems", "stretch");
        PlatformView.SetStyle("width", "100%");
        PlatformView.SetStyle("height", "100%");
        PlatformView.SetStyle("overflow", "hidden");
        PlatformView.SetStyle("overflowY", "hidden");
        PlatformView.SetStyle("overflowX", "hidden");
        PlatformView.SetStyle("margin", "0");
        PlatformView.SetStyle("padding", "0");
        PlatformView.SetStyle("colorScheme", "light dark");
        GetCoordinator().RequestRender();
    }

    private IMirageRenderCoordinator GetCoordinator()
    {
        if (MauiContext?.Services.GetService(typeof(IMirageRenderCoordinator)) is IMirageRenderCoordinator coordinator)
        {
            return coordinator;
        }

        throw new InvalidOperationException("MirageWindowHandler requires IMirageRenderCoordinator in MauiContext services.");
    }
}
