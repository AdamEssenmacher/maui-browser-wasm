using Mirage.Core.Semantics;
using Mirage.Maui.Dialogs;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
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
    private MiragePlatformView? _dialogHost;
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
        EnsureDialogHost(coordinator);
        SetPage(VirtualView.Page);
        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        GetDialogService()?.CancelDialogs(VirtualView);
        DisconnectCurrentPage();

        var coordinator = GetCoordinator();
        if (_dialogHost is not null)
        {
            GetDialogService()?.DetachWindow(VirtualView);
            platformView.RemoveChild(_dialogHost);
            coordinator.Unregister(_dialogHost);
            _dialogHost = null;
        }

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
        var previousPage = _currentPage;

        if (_currentPageHandler is not null && !ReferenceEquals(_currentPage, page))
        {
            GetDialogService()?.CancelDialogs(VirtualView);
            DisconnectCurrentPage();
        }

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

        if (_dialogHost is not null && ReferenceEquals(_dialogHost.Parent, PlatformView))
        {
            PlatformView.InsertChild(Math.Max(0, PlatformView.Children.Count - 1), pagePlatformView);
        }
        else
        {
            PlatformView.AddChild(pagePlatformView);
        }

        _currentPage = page;
        _currentPageHandler = pageHandler;
        if (_dialogHost is not null)
        {
            GetDialogService()?.AttachWindow(VirtualView, _dialogHost, GetCoordinator());
        }

        _ = MiragePageHandler.TryProjectActivePageTitle(pageHandler);

        if (!ReferenceEquals(previousPage, page))
        {
            ((IVisualElementController)page).IsPlatformEnabled = true;
            page.SendAppearing();
        }

        GetCoordinator().RequestRender();
    }

    private void DisconnectCurrentPage()
    {
        if (_currentPage is not null)
        {
            _currentPage.SendDisappearing();
        }

        if (_currentPageHandler?.PlatformView is MiragePlatformView currentPagePlatformView
            && ReferenceEquals(currentPagePlatformView.Parent, PlatformView))
        {
            PlatformView.RemoveChild(currentPagePlatformView);
        }

        MirageHandlerDisconnect.SafeDisconnect(_currentPageHandler);

        if (_currentPage is not null)
        {
            ((IVisualElementController)_currentPage).IsPlatformEnabled = false;
        }

        _currentPageHandler = null;
        _currentPage = null;
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

    private void EnsureDialogHost(IMirageRenderCoordinator coordinator)
    {
        if (_dialogHost is not null)
        {
            if (!ReferenceEquals(_dialogHost.Parent, PlatformView))
            {
                PlatformView.AddChild(_dialogHost);
            }

            return;
        }

        _dialogHost = MiragePlatformView.CreateControl(ComponentKind.ContentContainer);
        coordinator.Register(_dialogHost, null);
        PlatformView.AddChild(_dialogHost);
    }

    private IMirageBrowserDialogService? GetDialogService()
    {
        return MauiContext?.Services.GetService(typeof(IMirageBrowserDialogService)) as IMirageBrowserDialogService;
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
