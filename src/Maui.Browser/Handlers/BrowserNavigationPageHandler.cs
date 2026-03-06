using System.ComponentModel;
using Maui.Browser.Dom;
using Maui.Browser.Utils;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Maui.Browser.Handlers;

public sealed class BrowserNavigationPageHandler : BrowserViewHandler<NavigationPage>, INavigationViewHandler
{
    private const double DefaultBarHeight = 46;

    public static IPropertyMapper<NavigationPage, BrowserNavigationPageHandler> Mapper =
        new PropertyMapper<NavigationPage, BrowserNavigationPageHandler>(BrowserViewMapper.CommonViewMapper)
        {
            [nameof(NavigationPage.CurrentPage)] = MapCurrentPage,
            [nameof(NavigationPage.RootPage)] = MapRootPage,
            [nameof(Page.Title)] = MapTitle,
            [nameof(NavigationPage.BarBackgroundColor)] = MapBarBackgroundColor,
            [nameof(NavigationPage.BarTextColor)] = MapBarTextColor
        };

    public static CommandMapper<NavigationPage, BrowserNavigationPageHandler> CommandMapper =
        new(BrowserViewMapper.CommonViewCommandMapper)
        {
            [nameof(IStackNavigation.RequestNavigation)] = MapRequestNavigation
        };

    private readonly Dictionary<Page, IElementHandler> _pageHandlers = new();

    private DomElement? _barElement;
    private DomElement? _backButtonElement;
    private DomElement? _titleElement;
    private DomElement? _contentHostElement;

    private Action? _backClickCallback;
    private int _backClickSubscriptionId;

    private Page? _currentPage;
    private bool _hasArrangedFrame;
    private double _lastArrangedWidth;
    private double _lastArrangedHeight;
    private int _currentStackCount;
    private bool _navigationBarVisible = true;

    public BrowserNavigationPageHandler()
        : base(Mapper, CommandMapper)
    {
    }

    IStackNavigationView INavigationViewHandler.VirtualView => VirtualView;

    object INavigationViewHandler.PlatformView => PlatformView;

    protected override DomElement CreatePlatformView() => DomElement.Create("div");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Style("overflow", "hidden");

        _barElement = DomElement.Create("div");
        _barElement.Attr("data-automation-id", "navigation-bar");
        _barElement.Style("display", "flex");
        _barElement.Style("alignItems", "center");
        _barElement.Style("gap", "8px");
        _barElement.Style("boxSizing", "border-box");
        _barElement.Style("padding", "0 12px");
        _barElement.Style("borderBottom", "1px solid rgba(15,23,42,0.16)");
        _barElement.Style("backgroundColor", "rgba(241,245,249,1)");

        _backButtonElement = DomElement.Create("button");
        _backButtonElement.Attr("type", "button");
        _backButtonElement.Attr("data-automation-id", "navigation-back-button");
        _backButtonElement.Text("Back");
        _backButtonElement.Style("minWidth", "68px");
        _backButtonElement.Style("height", "30px");
        _backButtonElement.Style("cursor", "pointer");
        _backButtonElement.Style("whiteSpace", "nowrap");

        _titleElement = DomElement.Create("div");
        _titleElement.Attr("data-automation-id", "navigation-title");
        _titleElement.Style("fontWeight", "600");
        _titleElement.Style("overflow", "hidden");
        _titleElement.Style("textOverflow", "ellipsis");
        _titleElement.Style("whiteSpace", "nowrap");

        _barElement.Append(_backButtonElement);
        _barElement.Append(_titleElement);

        _contentHostElement = DomElement.Create("div");
        _contentHostElement.Attr("data-automation-id", "navigation-content-host");
        _contentHostElement.Style("overflow", "hidden");

        platformView.Append(_barElement);
        platformView.Append(_contentHostElement);

        _backClickCallback = OnBackClicked;
        _backClickSubscriptionId = _backButtonElement.On("click", _backClickCallback);

        VirtualView.Pushed += OnNavigationChanged;
        VirtualView.Popped += OnNavigationChanged;
        VirtualView.PoppedToRoot += OnNavigationChanged;

        ApplyNavigationStack(ResolveNavigationStack(VirtualView));
        UpdateNavigationBarVisual();
        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(DomElement platformView)
    {
        VirtualView.Pushed -= OnNavigationChanged;
        VirtualView.Popped -= OnNavigationChanged;
        VirtualView.PoppedToRoot -= OnNavigationChanged;

        if (_backClickSubscriptionId != 0)
        {
            DomElement.Off(_backClickSubscriptionId);
            _backClickSubscriptionId = 0;
        }

        _backClickCallback = null;
        _currentPage?.SendDisappearing();
        UnsubscribeCurrentPageTitle();

        foreach (var page in _pageHandlers.Keys.ToArray())
        {
            DisconnectPage(page);
        }

        _pageHandlers.Clear();
        _currentStackCount = 0;
        _barElement = null;
        _backButtonElement = null;
        _titleElement = null;
        _contentHostElement = null;

        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        var currentPage = _currentPage ?? VirtualView.CurrentPage;
        var barHeight = _navigationBarVisible ? DefaultBarHeight : 0;
        var contentHeightConstraint = double.IsInfinity(heightConstraint) || double.IsNaN(heightConstraint)
            ? double.PositiveInfinity
            : Math.Max(0, heightConstraint - barHeight);

        var contentSize = currentPage is IView currentView
            ? currentView.Measure(widthConstraint, contentHeightConstraint)
            : Size.Zero;

        var desiredWidth = ResolveDesiredDimension(widthConstraint, contentSize.Width);
        var desiredHeight = ResolveDesiredDimension(
            heightConstraint,
            contentSize.Height + (_navigationBarVisible ? DefaultBarHeight : 0));

        return new Size(desiredWidth, desiredHeight);
    }

    public override void PlatformArrange(Rect frame)
    {
        base.PlatformArrange(frame);
        _hasArrangedFrame = true;
        _lastArrangedWidth = Math.Max(0, frame.Width);
        _lastArrangedHeight = Math.Max(0, frame.Height);
        ArrangeCurrentPage();
    }

    public static void MapCurrentPage(BrowserNavigationPageHandler handler, NavigationPage navigationPage)
    {
        handler.ApplyNavigationStack(ResolveNavigationStack(navigationPage));
    }

    public static void MapRootPage(BrowserNavigationPageHandler handler, NavigationPage navigationPage)
    {
        handler.ApplyNavigationStack(ResolveNavigationStack(navigationPage));
    }

    public static void MapBarBackgroundColor(BrowserNavigationPageHandler handler, NavigationPage navigationPage)
    {
        handler.UpdateNavigationBarVisual();
    }

    public static void MapTitle(BrowserNavigationPageHandler handler, NavigationPage navigationPage)
    {
        handler.UpdateNavigationBarVisual();
    }

    public static void MapBarTextColor(BrowserNavigationPageHandler handler, NavigationPage navigationPage)
    {
        handler.UpdateNavigationBarVisual();
    }

    public static void MapRequestNavigation(BrowserNavigationPageHandler handler, NavigationPage navigationPage, object? args)
    {
        if (args is NavigationRequest request)
        {
            var requestPages = request.NavigationStack.OfType<Page>().ToArray();
            handler.ApplyNavigationStack(requestPages);

            if (navigationPage is IStackNavigation stackNavigation)
            {
                stackNavigation.NavigationFinished(request.NavigationStack);
            }

            return;
        }

        handler.ApplyNavigationStack(ResolveNavigationStack(navigationPage));
    }

    private void OnNavigationChanged(object? sender, NavigationEventArgs e)
    {
        ApplyNavigationStack(ResolveNavigationStack(VirtualView));
    }

    private void OnBackClicked()
    {
        if (_currentStackCount <= 1 || !_navigationBarVisible || !VirtualView.IsEnabled)
        {
            return;
        }

        _ = VirtualView.PopAsync(false);
    }

    private void ApplyNavigationStack(IReadOnlyList<Page> requestedStack)
    {
        var previousPage = _currentPage;
        var normalizedStack = NormalizeNavigationStack(requestedStack);
        var removedPages = _pageHandlers.Keys.Where(page => !normalizedStack.Contains(page)).ToArray();
        foreach (var removedPage in removedPages)
        {
            DisconnectPage(removedPage);
        }

        foreach (var page in normalizedStack)
        {
            if (_pageHandlers.ContainsKey(page) || MauiContext is null)
            {
                continue;
            }

            _pageHandlers[page] = page.ToHandler(MauiContext);
        }

        _contentHostElement?.ClearChildren();
        var currentPage = normalizedStack.Count > 0 ? normalizedStack[^1] : null;
        if (currentPage is not null
            && _pageHandlers.TryGetValue(currentPage, out var currentHandler)
            && currentHandler.PlatformView is DomElement currentPlatformView)
        {
            _contentHostElement?.Append(currentPlatformView);
        }

        _currentStackCount = normalizedStack.Count;
        SubscribeCurrentPageTitle(currentPage);

        if (previousPage is not null && !ReferenceEquals(previousPage, currentPage))
        {
            previousPage.SendDisappearing();
        }

        if (currentPage is not null && !ReferenceEquals(previousPage, currentPage))
        {
            currentPage.SendAppearing();
        }

        UpdateNavigationBarVisual();
        if (_hasArrangedFrame)
        {
            ArrangeCurrentPage();
        }
    }

    private static IReadOnlyList<Page> ResolveNavigationStack(NavigationPage navigationPage)
    {
        if (navigationPage.Navigation?.NavigationStack is { Count: > 0 } stack)
        {
            return stack.OfType<Page>().ToArray();
        }

        var fallback = new List<Page>();
        if (navigationPage.RootPage is not null)
        {
            fallback.Add(navigationPage.RootPage);
        }

        if (navigationPage.CurrentPage is not null && !fallback.Contains(navigationPage.CurrentPage))
        {
            fallback.Add(navigationPage.CurrentPage);
        }

        return fallback;
    }

    private static IReadOnlyList<Page> NormalizeNavigationStack(IReadOnlyList<Page> stack)
    {
        if (stack.Count == 0)
        {
            return Array.Empty<Page>();
        }

        return stack.Where(page => page is not null).Distinct().ToArray();
    }

    private void SubscribeCurrentPageTitle(Page? page)
    {
        if (ReferenceEquals(_currentPage, page))
        {
            return;
        }

        UnsubscribeCurrentPageTitle();
        _currentPage = page;

        if (_currentPage is not null)
        {
            _currentPage.PropertyChanged += OnCurrentPagePropertyChanged;
        }
    }

    private void UnsubscribeCurrentPageTitle()
    {
        if (_currentPage is null)
        {
            return;
        }

        _currentPage.PropertyChanged -= OnCurrentPagePropertyChanged;
        _currentPage = null;
    }

    private void OnCurrentPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var propertyName = e.PropertyName;
        if (!string.IsNullOrEmpty(propertyName)
            && propertyName != nameof(Page.Title)
            && propertyName != NavigationPage.HasNavigationBarProperty.PropertyName
            && propertyName != NavigationPage.HasBackButtonProperty.PropertyName)
        {
            return;
        }

        var previousBarVisibility = _navigationBarVisible;
        UpdateNavigationBarVisual();
        if (_hasArrangedFrame && previousBarVisibility != _navigationBarVisible)
        {
            ArrangeCurrentPage();
        }
    }

    private void ArrangeCurrentPage()
    {
        var width = _lastArrangedWidth;
        var height = _lastArrangedHeight;
        var barHeight = _navigationBarVisible ? DefaultBarHeight : 0;
        var contentHeight = Math.Max(0, height - barHeight);

        if (_barElement is not null)
        {
            if (_navigationBarVisible)
            {
                _barElement.Style("display", "flex");
                BrowserViewMapper.ApplyFrame(_barElement, new Rect(0, 0, width, barHeight));
            }
            else
            {
                _barElement.Style("display", "none");
            }
        }

        if (_contentHostElement is not null)
        {
            BrowserViewMapper.ApplyFrame(_contentHostElement, new Rect(0, barHeight, width, contentHeight));
        }

        if (_currentPage is IView currentView)
        {
            currentView.Measure(width, contentHeight);
            currentView.Arrange(new Rect(0, 0, width, contentHeight));
        }
    }

    private void UpdateNavigationBarVisual()
    {
        var currentPage = _currentPage;
        _navigationBarVisible = currentPage is null || NavigationPage.GetHasNavigationBar(currentPage);

        var barBackground = CssUtil.ToCssColor(VirtualView.BarBackgroundColor) ?? "rgba(241,245,249,1)";
        var barTextColor = CssUtil.ToCssColor(VirtualView.BarTextColor) ?? "rgba(15,23,42,1)";

        _barElement?.Style("backgroundColor", barBackground);
        _titleElement?.Style("color", barTextColor);

        var title = currentPage?.Title;
        if (string.IsNullOrWhiteSpace(title))
        {
            title = VirtualView.Title;
        }

        _titleElement?.Text(title ?? string.Empty);

        var hasBackButton = _currentStackCount > 1
                            && (currentPage is null || NavigationPage.GetHasBackButton(currentPage))
                            && _navigationBarVisible;
        if (_backButtonElement is not null)
        {
            _backButtonElement.Style("display", hasBackButton ? "inline-block" : "none");
            _backButtonElement.Style("color", barTextColor);
        }
    }

    private void DisconnectPage(Page page)
    {
        if (!_pageHandlers.TryGetValue(page, out var handler))
        {
            return;
        }

        handler.DisconnectHandler();
        _pageHandlers.Remove(page);
    }

    private static double ResolveDesiredDimension(double constraint, double contentDimension)
    {
        if (double.IsNaN(constraint) || double.IsInfinity(constraint))
        {
            return Math.Max(0, contentDimension);
        }

        return Math.Max(0, Math.Min(constraint, contentDimension));
    }
}
