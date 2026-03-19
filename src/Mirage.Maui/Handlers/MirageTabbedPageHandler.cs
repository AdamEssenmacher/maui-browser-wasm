using System.Linq;
using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Mirage.Maui.Handlers;

internal sealed class MirageTabbedPageHandler : MirageViewHandler<TabbedPage>
{
    private readonly Dictionary<Page, IElementHandler> _pageHandlers = [];
    private Page? _currentPage;

    public static IPropertyMapper<TabbedPage, MirageTabbedPageHandler> Mapper =
        new PropertyMapper<TabbedPage, MirageTabbedPageHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(TabbedPage.CurrentPage)] = MapCurrentPage
        };

    public MirageTabbedPageHandler()
        : base(ComponentKind.ContentContainer, Mapper)
    {
        DefaultDisplayStyle = "grid";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        ApplyBaseStyles(platformView);

        VirtualView.CurrentPageChanged += OnCurrentPageChanged;
        VirtualView.ChildAdded += OnChildAdded;
        VirtualView.ChildRemoved += OnChildRemoved;

        MountCurrentPage();
        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        VirtualView.CurrentPageChanged -= OnCurrentPageChanged;
        VirtualView.ChildAdded -= OnChildAdded;
        VirtualView.ChildRemoved -= OnChildRemoved;

        foreach (var pageHandler in _pageHandlers.Values)
        {
            MirageHandlerDisconnect.SafeDisconnect(pageHandler);
        }

        _pageHandlers.Clear();
        _currentPage = null;
        platformView.ClearChildren();
        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        if (ResolveCurrentPage() is not IView currentPage)
        {
            return base.GetDesiredSize(widthConstraint, heightConstraint);
        }

        var contentSize = currentPage.Measure(widthConstraint, heightConstraint);
        return new Size(contentSize.Width, contentSize.Height);
    }

    public override void PlatformArrange(Rect frame)
    {
        base.PlatformArrange(frame);

        var width = Math.Max(0, frame.Width);
        var height = Math.Max(0, frame.Height);
        if (ResolveCurrentPage() is not IView currentPage)
        {
            return;
        }

        currentPage.Arrange(new Rect(0, 0, width, height));
        if (TryGetCurrentPagePlatformView(out var currentPageView))
        {
            NormalizeHostedContentFlowStyles(currentPageView);
        }
    }

    public static void MapCurrentPage(MirageTabbedPageHandler handler, TabbedPage tabbedPage)
    {
        _ = tabbedPage;
        handler.MountCurrentPage();
    }

    private void OnCurrentPageChanged(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        MountCurrentPage();
    }

    private void OnChildAdded(object? sender, ElementEventArgs e)
    {
        _ = sender;
        _ = e;
        MountCurrentPage();
    }

    private void OnChildRemoved(object? sender, ElementEventArgs e)
    {
        _ = sender;
        if (e.Element is Page removedPage && _pageHandlers.Remove(removedPage, out var removedHandler))
        {
            MirageHandlerDisconnect.SafeDisconnect(removedHandler);
        }

        MountCurrentPage();
    }

    private void ApplyBaseStyles(MiragePlatformView platformView)
    {
        platformView.SetStyle("display", "grid");
        platformView.SetStyle("gridTemplateColumns", "minmax(0, 1fr)");
        platformView.SetStyle("gridTemplateRows", "minmax(0, 1fr)");
        platformView.SetStyle("justifyItems", "stretch");
        platformView.SetStyle("alignItems", "stretch");
        platformView.SetStyle("width", "100%");
        platformView.SetStyle("height", "100%");
        platformView.SetStyle("overflow", "hidden");
        platformView.SetStyle("overflowX", "hidden");
        platformView.SetStyle("overflowY", "hidden");
    }

    private void MountCurrentPage()
    {
        if (!TryGetPlatformView(out var platformView))
        {
            RequestRender();
            return;
        }

        var requestedPage = ResolveCurrentPage();
        _currentPage = requestedPage;

        platformView.ClearChildren();
        if (requestedPage is null || MauiContext is null)
        {
            RequestRender();
            return;
        }

        var pageHandler = GetOrCreatePageHandler(requestedPage);
        if (pageHandler.PlatformView is not MiragePlatformView pagePlatformView)
        {
            throw new InvalidOperationException($"{GetType().Name} requires child handlers to expose MiragePlatformView.");
        }

        NormalizeHostedContentFlowStyles(pagePlatformView);
        platformView.AddChild(pagePlatformView);
        RequestRender();
    }

    private IElementHandler GetOrCreatePageHandler(Page page)
    {
        if (_pageHandlers.TryGetValue(page, out var existingHandler))
        {
            return existingHandler;
        }

        if (MauiContext is null)
        {
            throw new InvalidOperationException($"{GetType().Name} requires a non-null MauiContext to create child page handlers.");
        }

        var createdHandler = page.ToHandler(MauiContext);
        _pageHandlers[page] = createdHandler;
        return createdHandler;
    }

    private Page? ResolveCurrentPage()
    {
        return VirtualView.CurrentPage ?? VirtualView.Children.FirstOrDefault();
    }

    private bool TryGetCurrentPagePlatformView(out MiragePlatformView platformView)
    {
        if (_currentPage is not null &&
            _pageHandlers.TryGetValue(_currentPage, out var currentPageHandler) &&
            currentPageHandler.PlatformView is MiragePlatformView miragePlatformView)
        {
            platformView = miragePlatformView;
            return true;
        }

        platformView = null!;
        return false;
    }

    private static void NormalizeHostedContentFlowStyles(MiragePlatformView contentPlatformView)
    {
        contentPlatformView.SetStyle("position", "relative");
        contentPlatformView.SetStyle("left", null);
        contentPlatformView.SetStyle("top", null);
        contentPlatformView.SetStyle("right", null);
        contentPlatformView.SetStyle("bottom", null);
    }
}
