using System.ComponentModel;
using Mirage.Core.Diagnostics;
using Mirage.Core.Semantics;
using Mirage.Maui.Navigation;
using Mirage.Web.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Mirage.Maui.Handlers;

internal sealed class MirageNavigationPageHandler : MirageViewHandler<NavigationPage>, INavigationViewHandler
{
    public static IPropertyMapper<NavigationPage, MirageNavigationPageHandler> Mapper =
        new PropertyMapper<NavigationPage, MirageNavigationPageHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(NavigationPage.CurrentPage)] = MapCurrentPage,
            [nameof(NavigationPage.RootPage)] = MapRootPage,
            [nameof(Page.Title)] = MapTitle
        };

    public static CommandMapper<NavigationPage, MirageNavigationPageHandler> CommandMapper =
        new(MirageViewMapper.CommonViewCommandMapper)
        {
            [nameof(IStackNavigation.RequestNavigation)] = MapRequestNavigation
        };

    private readonly Dictionary<Page, IElementHandler> _pageHandlers = [];

    private Page? _currentPage;
    private int _currentStackCount;
    private bool _hasArrangedFrame;
    private double _lastArrangedWidth;
    private double _lastArrangedHeight;
    private bool _historyWriteSuppressed;
    private readonly object _popStateSync = new();
    private bool _isPopStatePumpRunning;
    private string? _pendingPopStateLocation;
    private int _popStateSubscriptionId;
    private string? _lastProjectedPath;

    public MirageNavigationPageHandler()
        : base(ComponentKind.ContentContainer, Mapper, CommandMapper)
    {
        DefaultDisplayStyle = "grid";
    }

    IStackNavigationView INavigationViewHandler.VirtualView => VirtualView;

    object INavigationViewHandler.PlatformView => PlatformView;

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        ApplyBaseStyles();
        base.ConnectHandler(platformView);
        SubscribeNavigationEvents();
        SubscribePopState();
        ApplyNavigationStack(ResolveNavigationStack(VirtualView), MirageNavigationTrigger.Startup);
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        UnsubscribePopState();
        UnsubscribeNavigationEvents();
        UnsubscribeCurrentPageTitle();

        foreach (var page in _pageHandlers.Keys.ToArray())
        {
            DisconnectPage(page);
        }

        _pageHandlers.Clear();
        _currentStackCount = 0;
        _currentPage = null;
        _lastProjectedPath = null;
        lock (_popStateSync)
        {
            _isPopStatePumpRunning = false;
            _pendingPopStateLocation = null;
        }

        platformView.ClearChildren();
        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        var currentPage = _currentPage ?? VirtualView.CurrentPage;
        if (currentPage is not IView currentView)
        {
            return base.GetDesiredSize(widthConstraint, heightConstraint);
        }

        var contentSize = currentView.Measure(widthConstraint, heightConstraint);
        return new Size(ResolveDesiredDimension(widthConstraint, contentSize.Width), ResolveDesiredDimension(heightConstraint, contentSize.Height));
    }

    public override void PlatformArrange(Rect frame)
    {
        base.PlatformArrange(frame);
        _hasArrangedFrame = true;
        _lastArrangedWidth = Math.Max(0, frame.Width);
        _lastArrangedHeight = Math.Max(0, frame.Height);
        ArrangeCurrentPage();
    }

    public static void MapCurrentPage(MirageNavigationPageHandler handler, NavigationPage navigationPage)
    {
        _ = navigationPage;
        handler.ApplyNavigationStack(ResolveNavigationStack(handler.VirtualView), MirageNavigationTrigger.MauiRequestNavigation);
    }

    public static void MapRootPage(MirageNavigationPageHandler handler, NavigationPage navigationPage)
    {
        _ = navigationPage;
        handler.ApplyNavigationStack(ResolveNavigationStack(handler.VirtualView), MirageNavigationTrigger.MauiRequestNavigation);
    }

    public static void MapTitle(MirageNavigationPageHandler handler, NavigationPage navigationPage)
    {
        _ = navigationPage;
        handler.ProjectCurrentRoute(MirageNavigationTrigger.Manual);
    }

    public static void MapRequestNavigation(MirageNavigationPageHandler handler, NavigationPage navigationPage, object? args)
    {
        var diagnosticsEnabled = MirageDiagnostics.IsEnabled;
        var startTimestamp = diagnosticsEnabled ? MirageDiagnostics.GetTimestamp() : 0;
        if (diagnosticsEnabled)
        {
            var argType = args?.GetType().FullName ?? "<null>";
            MirageDiagnostics.Log(
                "nav.map-request",
                $"start argType={argType} stackCountBefore={handler._currentStackCount}");
        }

        if (args is NavigationRequest request)
        {
            var pages = request.NavigationStack.OfType<Page>().ToArray();
            var trigger = InferTriggerFromStackDelta(handler._currentStackCount, pages.Length, MirageNavigationTrigger.MauiRequestNavigation);
            handler.ApplyNavigationStack(pages, trigger);

            var finishedCalled = false;
            var finishedElapsedMs = 0d;
            if (navigationPage is IStackNavigation stackNavigation)
            {
                var finishedStart = diagnosticsEnabled ? MirageDiagnostics.GetTimestamp() : 0;
                stackNavigation.NavigationFinished(request.NavigationStack);
                finishedCalled = true;
                if (diagnosticsEnabled)
                {
                    finishedElapsedMs = MirageDiagnostics.GetElapsedMilliseconds(finishedStart, MirageDiagnostics.GetTimestamp());
                }
            }

            if (diagnosticsEnabled)
            {
                var totalMs = MirageDiagnostics.GetElapsedMilliseconds(startTimestamp, MirageDiagnostics.GetTimestamp());
                MirageDiagnostics.Log(
                    "nav.map-request",
                    $"end trigger={trigger} requestedStackCount={pages.Length} navigationFinishedCalled={finishedCalled} navigationFinishedMs={finishedElapsedMs:0.###} totalMs={totalMs:0.###}");
            }

            return;
        }

        handler.ApplyNavigationStack(ResolveNavigationStack(navigationPage), MirageNavigationTrigger.MauiRequestNavigation);
        if (diagnosticsEnabled)
        {
            var totalMs = MirageDiagnostics.GetElapsedMilliseconds(startTimestamp, MirageDiagnostics.GetTimestamp());
            MirageDiagnostics.Log("nav.map-request", $"end trigger={MirageNavigationTrigger.MauiRequestNavigation} totalMs={totalMs:0.###}");
        }
    }

    private void OnNavigationPushed(object? sender, NavigationEventArgs e)
    {
        _ = sender;
        _ = e;
        ApplyNavigationStack(ResolveNavigationStack(VirtualView), MirageNavigationTrigger.MauiPush);
    }

    private void OnNavigationPopped(object? sender, NavigationEventArgs e)
    {
        _ = sender;
        _ = e;
        ApplyNavigationStack(ResolveNavigationStack(VirtualView), MirageNavigationTrigger.MauiPop);
    }

    private void OnNavigationPoppedToRoot(object? sender, NavigationEventArgs e)
    {
        _ = sender;
        _ = e;
        ApplyNavigationStack(ResolveNavigationStack(VirtualView), MirageNavigationTrigger.MauiPopToRoot);
    }

    private async void OnBrowserPopState(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return;
        }

        var shouldStartPump = false;
        lock (_popStateSync)
        {
            _pendingPopStateLocation = location;
            if (!_isPopStatePumpRunning)
            {
                _isPopStatePumpRunning = true;
                shouldStartPump = true;
            }
        }

        if (!shouldStartPump)
        {
            return;
        }

        if (GetDispatcher() is { } dispatcher && dispatcher.IsDispatchRequired)
        {
            if (dispatcher.Dispatch(() => _ = SafeApplyBrowserPopStateAsync()))
            {
                return;
            }
        }

        await SafeApplyBrowserPopStateAsync();
    }

    private async Task SafeApplyBrowserPopStateAsync()
    {
        while (true)
        {
            var nextLocation = default(string);
            lock (_popStateSync)
            {
                nextLocation = _pendingPopStateLocation;
                _pendingPopStateLocation = null;
                if (nextLocation is null)
                {
                    _isPopStatePumpRunning = false;
                    return;
                }
            }

            try
            {
                await ApplyBrowserPopStateAsync(nextLocation);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Mirage.Navigation] Failed to process browser popstate '{nextLocation}': {ex}");
            }
        }
    }

    private async Task ApplyBrowserPopStateAsync(string location)
    {
        if (!TryResolveNavigationServices(
                out var routeRegistry,
                out var routeParser,
                out var routeSerializer,
                out var pageActivator,
                out var parameterBinder,
                out _,
                out _,
                out _))
        {
            return;
        }

        var targetMatch = ResolveTargetMatch(location, routeRegistry, routeParser);
        if (targetMatch is null)
        {
            return;
        }

        var guardDecision = await EvaluateGuardsAsync(targetMatch, MirageNavigationTrigger.BrowserPopState);
        if (guardDecision.Action == NavigationGuardAction.Cancel)
        {
            ProjectCurrentRoute(MirageNavigationTrigger.BrowserPopState);
            return;
        }

        if (guardDecision.Action == NavigationGuardAction.Redirect)
        {
            if (string.IsNullOrWhiteSpace(guardDecision.RedirectPath)
                || !routeParser.TryParse(guardDecision.RedirectPath, routeRegistry, out var redirectedMatch))
            {
                ProjectCurrentRoute(MirageNavigationTrigger.BrowserPopState);
                return;
            }

            targetMatch = redirectedMatch;
        }

        _historyWriteSuppressed = true;

        try
        {
            var stack = ResolveNavigationStack(VirtualView);
            var existingIndex = FindRouteInStack(stack, targetMatch.Path, routeRegistry, routeSerializer);
            if (existingIndex >= 0)
            {
                TrimStackToIndex(stack, existingIndex);
            }
            else
            {
                var page = pageActivator.CreatePage(targetMatch.Route, ResolveServices());
                parameterBinder.Bind(page, MergeParameters(targetMatch));
                await VirtualView.PushAsync(page, animated: false);
            }

            await WaitForTopRouteAsync(targetMatch.Path, routeRegistry, routeSerializer);

            // Re-sync handler state from the MAUI navigation stack even if specific stack events
            // are delayed or coalesced by the platform navigation pipeline.
            ApplyNavigationStack(ResolveNavigationStack(VirtualView), MirageNavigationTrigger.BrowserPopState);
        }
        finally
        {
            _historyWriteSuppressed = false;
        }

        var topRouteMatchesTarget = TryGetTopRoutePath(routeRegistry, routeSerializer, out var topRoutePath)
            && string.Equals(topRoutePath, targetMatch.Path, StringComparison.OrdinalIgnoreCase);
        if (topRouteMatchesTarget)
        {
            ProjectCurrentRoute(MirageNavigationTrigger.BrowserPopState);
            return;
        }

        _lastProjectedPath = targetMatch.Path;
    }

    private void ApplyNavigationStack(IReadOnlyList<Page> requestedStack, MirageNavigationTrigger trigger)
    {
        var diagnosticsEnabled = MirageDiagnostics.IsEnabled;
        var startTimestamp = diagnosticsEnabled ? MirageDiagnostics.GetTimestamp() : 0;
        var stepStartTimestamp = startTimestamp;
        var stepDiagnostics = diagnosticsEnabled ? new List<string>(8) : null;
        void CaptureStep(string name)
        {
            if (!diagnosticsEnabled)
            {
                return;
            }

            var now = MirageDiagnostics.GetTimestamp();
            var elapsedMs = MirageDiagnostics.GetElapsedMilliseconds(stepStartTimestamp, now);
            stepDiagnostics!.Add($"{name}Ms={elapsedMs:0.###}");
            stepStartTimestamp = now;
        }

        var stackCountBefore = _currentStackCount;
        var normalizedStack = NormalizeNavigationStack(requestedStack);
        CaptureStep("normalize");
        var previousPage = _currentPage;

        var removedPages = _pageHandlers.Keys.Where(page => !normalizedStack.Contains(page)).ToArray();
        foreach (var removedPage in removedPages)
        {
            DisconnectPage(removedPage);
        }
        CaptureStep("disconnect");

        foreach (var page in normalizedStack)
        {
            var pageIdentity = page.GetHashCode();
            if (_pageHandlers.ContainsKey(page) || MauiContext is null)
            {
                if (diagnosticsEnabled)
                {
                    stepDiagnostics!.Add($"pageReused={page.GetType().Name}#{pageIdentity}");
                }
                continue;
            }

            var createStartTimestamp = diagnosticsEnabled ? MirageDiagnostics.GetTimestamp() : 0;
            _pageHandlers[page] = page.ToHandler(MauiContext);
            if (diagnosticsEnabled)
            {
                var createElapsedMs = MirageDiagnostics.GetElapsedMilliseconds(createStartTimestamp, MirageDiagnostics.GetTimestamp());
                stepDiagnostics!.Add($"pageCreated={page.GetType().Name}#{pageIdentity}@{createElapsedMs:0.###}ms");
            }
        }
        CaptureStep("ensureHandlers");

        var currentPage = normalizedStack.Count > 0 ? normalizedStack[^1] : null;

        if (TryGetPlatformView(out var platformView))
        {
            platformView.ClearChildren();
            if (currentPage is not null
                && _pageHandlers.TryGetValue(currentPage, out var currentHandler)
                && currentHandler.PlatformView is MiragePlatformView currentPlatformView)
            {
                platformView.AddChild(currentPlatformView);
            }
        }
        CaptureStep("mount");

        SubscribeCurrentPageTitle(currentPage);
        _currentStackCount = normalizedStack.Count;

        if (previousPage is not null && !ReferenceEquals(previousPage, currentPage))
        {
            previousPage.SendDisappearing();
        }

        if (currentPage is not null && !ReferenceEquals(previousPage, currentPage))
        {
            currentPage.SendAppearing();
        }
        CaptureStep("lifecycle");

        if (_hasArrangedFrame)
        {
            ArrangeCurrentPage();
        }
        CaptureStep("arrange");

        ProjectCurrentRoute(trigger);
        CaptureStep("projectRoute");
        RequestRender();
        CaptureStep("requestRender");

        if (diagnosticsEnabled)
        {
            var elapsedMs = MirageDiagnostics.GetElapsedMilliseconds(startTimestamp, MirageDiagnostics.GetTimestamp());
            MirageDiagnostics.Log(
                "nav.apply-stack",
                $"trigger={trigger} stackBefore={stackCountBefore} stackAfter={_currentStackCount} removedHandlers={removedPages.Length} currentPage={(currentPage?.GetType().Name ?? "<null>")} elapsedMs={elapsedMs:0.###} steps={string.Join(',', stepDiagnostics!)}");
        }
    }

    private void ArrangeCurrentPage()
    {
        if (_currentPage is not IView currentView)
        {
            return;
        }

        currentView.Measure(_lastArrangedWidth, _lastArrangedHeight);
        currentView.Arrange(new Rect(0, 0, _lastArrangedWidth, _lastArrangedHeight));
    }

    private void ApplyBaseStyles()
    {
        if (!TryGetPlatformView(out var platformView))
        {
            return;
        }

        platformView.SetStyle("display", "grid");
        platformView.SetStyle("gridTemplateColumns", "minmax(0, 1fr)");
        platformView.SetStyle("gridTemplateRows", "minmax(0, 1fr)");
        platformView.SetStyle("justifyItems", "stretch");
        platformView.SetStyle("alignItems", "stretch");
        platformView.SetStyle("width", "100%");
        platformView.SetStyle("height", "100%");
        platformView.SetStyle("overflow", "hidden");
        platformView.SetStyle("overflowY", "hidden");
        platformView.SetStyle("overflowX", "hidden");
    }

    private void ProjectCurrentRoute(MirageNavigationTrigger trigger)
    {
        if (_historyWriteSuppressed)
        {
            return;
        }

        if (_currentPage is null)
        {
            return;
        }

        if (!TryResolveNavigationServices(
                out var routeRegistry,
                out var routeParser,
                out var routeSerializer,
                out _,
                out _,
                out var historyPolicy,
                out var titlePolicy,
                out var browserHistory))
        {
            return;
        }

        if (!routeSerializer.TrySerialize(_currentPage, routeRegistry, out var path))
        {
            return;
        }

        MirageRouteMatch? currentMatch = null;
        if (routeParser.TryParse(path, routeRegistry, out var parsedMatch))
        {
            currentMatch = parsedMatch;
        }

        var context = new MirageNavigationHistoryContext(
            trigger,
            _lastProjectedPath,
            path,
            trigger is MirageNavigationTrigger.MauiPop or MirageNavigationTrigger.MauiPopToRoot or MirageNavigationTrigger.BrowserPopState);

        var writeMode = historyPolicy.ResolveWriteMode(context);
        if (writeMode == MirageHistoryWriteMode.Push
                 && string.Equals(context.PreviousPath, context.NextPath, StringComparison.OrdinalIgnoreCase))
        {
            writeMode = MirageHistoryWriteMode.Suppress;
        }
        else if (writeMode == MirageHistoryWriteMode.Push)
        {
            var (currentLocationPath, _) = DefaultRouteParser.ParseLocation(browserHistory.GetCurrentLocation());
            if (string.Equals(currentLocationPath, path, StringComparison.OrdinalIgnoreCase))
            {
                writeMode = MirageHistoryWriteMode.Suppress;
            }
        }

        switch (writeMode)
        {
            case MirageHistoryWriteMode.Push:
                browserHistory.Push(path);
                break;
            case MirageHistoryWriteMode.Replace:
                browserHistory.Replace(path);
                break;
            case MirageHistoryWriteMode.Suppress:
                break;
            default:
                browserHistory.Replace(path);
                break;
        }

        _lastProjectedPath = path;
        browserHistory.SetDocumentTitle(titlePolicy.ResolveTitle(_currentPage, currentMatch?.Path));
    }

    private async ValueTask<NavigationGuardDecision> EvaluateGuardsAsync(MirageRouteMatch targetRoute, MirageNavigationTrigger trigger)
    {
        var services = ResolveServices();
        if (services is null)
        {
            return NavigationGuardDecision.Allow();
        }

        var guards = services.GetServices<INavigationGuard>().ToArray();
        if (guards.Length == 0)
        {
            return NavigationGuardDecision.Allow();
        }

        MirageRouteMatch? currentRoute = null;
        if (_currentPage is not null
            && TryResolveNavigationServices(
                out var routeRegistry,
                out var routeParser,
                out var routeSerializer,
                out _,
                out _,
                out _,
                out _,
                out _)
            && routeSerializer.TrySerialize(_currentPage, routeRegistry, out var currentPath)
            && routeParser.TryParse(currentPath, routeRegistry, out var parsedCurrent))
        {
            currentRoute = parsedCurrent;
        }

        var context = new MirageNavigationGuardContext(trigger, _currentPage, currentRoute, targetRoute);

        foreach (var guard in guards)
        {
            var decision = await guard.EvaluateAsync(context);
            if (decision.Action != NavigationGuardAction.Allow)
            {
                return decision;
            }
        }

        return NavigationGuardDecision.Allow();
    }

    private MirageRouteMatch? ResolveTargetMatch(string location, IRouteRegistry routeRegistry, IRouteParser routeParser)
    {
        if (routeParser.TryParse(location, routeRegistry, out var targetMatch))
        {
            return targetMatch;
        }

        var fallbackRoute = routeRegistry.FallbackRoute;
        if (fallbackRoute is null)
        {
            return null;
        }

        if (routeParser.TryParse(fallbackRoute.NormalizedTemplate, routeRegistry, out var fallbackMatch))
        {
            return fallbackMatch;
        }

        return new MirageRouteMatch(
            fallbackRoute,
            fallbackRoute.NormalizedTemplate,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
    }

    private static int FindRouteInStack(
        IReadOnlyList<Page> stack,
        string targetPath,
        IRouteRegistry routeRegistry,
        IRouteSerializer routeSerializer)
    {
        for (var index = stack.Count - 1; index >= 0; index--)
        {
            if (!routeSerializer.TrySerialize(stack[index], routeRegistry, out var path))
            {
                continue;
            }

            if (string.Equals(path, targetPath, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private void TrimStackToIndex(IReadOnlyList<Page> stack, int targetTopIndex)
    {
        var navigation = VirtualView.Navigation;
        if (navigation is null)
        {
            return;
        }

        for (var index = stack.Count - 1; index > targetTopIndex; index--)
        {
            navigation.RemovePage(stack[index]);
        }
    }

    private async Task WaitForTopRouteAsync(string targetPath, IRouteRegistry routeRegistry, IRouteSerializer routeSerializer)
    {
        const int pollIntervalMs = 10;
        const int maxWaitMs = 500;

        var waitedMs = 0;
        while (waitedMs < maxWaitMs)
        {
            if (TryGetTopRoutePath(routeRegistry, routeSerializer, out var currentPath)
                && string.Equals(currentPath, targetPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await Task.Delay(pollIntervalMs);
            waitedMs += pollIntervalMs;
        }
    }

    private bool TryGetTopRoutePath(IRouteRegistry routeRegistry, IRouteSerializer routeSerializer, out string path)
    {
        var stack = ResolveNavigationStack(VirtualView);
        if (stack.Count > 0
            && routeSerializer.TrySerialize(stack[^1], routeRegistry, out var currentPath))
        {
            path = currentPath;
            return true;
        }

        path = string.Empty;
        return false;
    }

    private static IReadOnlyDictionary<string, string> MergeParameters(MirageRouteMatch routeMatch)
    {
        var merged = new Dictionary<string, string>(routeMatch.Query, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in routeMatch.Parameters)
        {
            merged[key] = value;
        }

        return merged;
    }

    private void SubscribeNavigationEvents()
    {
        VirtualView.Pushed += OnNavigationPushed;
        VirtualView.Popped += OnNavigationPopped;
        VirtualView.PoppedToRoot += OnNavigationPoppedToRoot;
    }

    private void UnsubscribeNavigationEvents()
    {
        VirtualView.Pushed -= OnNavigationPushed;
        VirtualView.Popped -= OnNavigationPopped;
        VirtualView.PoppedToRoot -= OnNavigationPoppedToRoot;
    }

    private void SubscribePopState()
    {
        var history = ResolveServices()?.GetService<IMirageBrowserHistory>();
        if (history is null)
        {
            return;
        }

        _popStateSubscriptionId = history.SubscribePopState(OnBrowserPopState);
    }

    private void UnsubscribePopState()
    {
        if (_popStateSubscriptionId == 0)
        {
            return;
        }

        var history = ResolveServices()?.GetService<IMirageBrowserHistory>();
        history?.UnsubscribePopState(_popStateSubscriptionId);
        _popStateSubscriptionId = 0;
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
    }

    private void OnCurrentPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = sender;
        if (!string.IsNullOrEmpty(e.PropertyName) && !string.Equals(e.PropertyName, nameof(Page.Title), StringComparison.Ordinal))
        {
            return;
        }

        ProjectCurrentRoute(MirageNavigationTrigger.Manual);
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

    private void DisconnectPage(Page page)
    {
        if (!_pageHandlers.TryGetValue(page, out var handler))
        {
            return;
        }

        handler.DisconnectHandler();
        _pageHandlers.Remove(page);
    }

    private IServiceProvider ResolveServices()
    {
        if (MauiContext?.Services is IServiceProvider services)
        {
            return services;
        }

        throw new InvalidOperationException("MirageNavigationPageHandler requires MauiContext services.");
    }

    private IDispatcher? GetDispatcher()
    {
        return ResolveServices().GetService<IDispatcher>();
    }

    private bool TryResolveNavigationServices(
        out IRouteRegistry routeRegistry,
        out IRouteParser routeParser,
        out IRouteSerializer routeSerializer,
        out IPageActivator pageActivator,
        out IRouteParameterBinder parameterBinder,
        out INavigationHistoryPolicy historyPolicy,
        out INavigationTitlePolicy titlePolicy,
        out IMirageBrowserHistory browserHistory)
    {
        var services = ResolveServices();

        routeRegistry = services.GetService<IRouteRegistry>()!;
        routeParser = services.GetService<IRouteParser>()!;
        routeSerializer = services.GetService<IRouteSerializer>()!;
        pageActivator = services.GetService<IPageActivator>()!;
        parameterBinder = services.GetService<IRouteParameterBinder>()!;
        historyPolicy = services.GetService<INavigationHistoryPolicy>()!;
        titlePolicy = services.GetService<INavigationTitlePolicy>()!;
        browserHistory = services.GetService<IMirageBrowserHistory>()!;

        return routeRegistry is not null
            && routeParser is not null
            && routeSerializer is not null
            && pageActivator is not null
            && parameterBinder is not null
            && historyPolicy is not null
            && titlePolicy is not null
            && browserHistory is not null;
    }

    private static MirageNavigationTrigger InferTriggerFromStackDelta(int previousCount, int currentCount, MirageNavigationTrigger fallback)
    {
        if (currentCount > previousCount)
        {
            return MirageNavigationTrigger.MauiPush;
        }

        if (currentCount < previousCount)
        {
            return currentCount <= 1
                ? MirageNavigationTrigger.MauiPopToRoot
                : MirageNavigationTrigger.MauiPop;
        }

        return fallback;
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
