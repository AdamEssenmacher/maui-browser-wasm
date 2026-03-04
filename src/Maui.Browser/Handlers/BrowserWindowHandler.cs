using Maui.Browser.Dom;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Maui.Browser.Handlers;

public sealed class BrowserWindowHandler : ElementHandler<Window, DomElement>
{
    public static IPropertyMapper<Window, BrowserWindowHandler> Mapper =
        new PropertyMapper<Window, BrowserWindowHandler>(ElementMapper)
        {
            [nameof(Window.Page)] = MapPage
        };

    private Page? _currentPage;
    private IElementHandler? _currentPageHandler;
    private double _lastArrangedWidth;
    private double _lastArrangedHeight;
    private bool _hasArrangedWindowFrame;

    public BrowserWindowHandler()
        : base(Mapper)
    {
    }

    protected override DomElement CreatePlatformElement() => DomElement.Create("div");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Style("overflow", "hidden");
        SetPage(VirtualView.Page);
        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(DomElement platformView)
    {
        DisconnectCurrentPage();
        base.DisconnectHandler(platformView);
        platformView.Release();
    }

    public void ArrangeWindow(double width, double height)
    {
        var frame = new Rect(0, 0, Math.Max(0, width), Math.Max(0, height));
        _lastArrangedWidth = frame.Width;
        _lastArrangedHeight = frame.Height;
        _hasArrangedWindowFrame = true;

        BrowserViewMapper.ApplyFrame(PlatformView, frame);

        if (VirtualView.Page is IView pageView)
        {
            pageView.Measure(frame.Width, frame.Height);
            pageView.Arrange(new Rect(0, 0, frame.Width, frame.Height));
        }
    }

    public static void MapPage(BrowserWindowHandler handler, Window window)
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
            return;
        }

        var pageHandler = page.ToHandler(MauiContext);
        if (pageHandler.PlatformView is not DomElement pageElement)
        {
            return;
        }

        PlatformView.Append(pageElement);
        _currentPage = page;
        _currentPageHandler = pageHandler;
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

        var clientSize = PlatformView.ClientSize();
        if (clientSize.Width > 0 && clientSize.Height > 0)
        {
            ArrangeWindow(clientSize.Width, clientSize.Height);
        }
    }

    private void DisconnectCurrentPage()
    {
        if (_currentPageHandler is null)
        {
            _currentPage = null;
            return;
        }

        _currentPageHandler.DisconnectHandler();
        _currentPageHandler = null;
        _currentPage = null;
    }
}
