using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
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

internal sealed class MirageNavigationPageHandler : MirageViewHandler<NavigationPage>, INavigationViewHandler, IMiragePageTitleProjectionHost
{
    private const double DefaultNavigationBarHeight = 46;
    private const string NavigationBarAutomationId = "mirage-navigation-bar";
    private const string NavigationBackButtonAutomationId = "mirage-navigation-back-button";
    private const string NavigationTitleAutomationId = "mirage-navigation-title";
    private const string NavigationToolbarAutomationId = "mirage-navigation-toolbar";
    private const string NavigationContentHostAutomationId = "mirage-navigation-content-host";
    private static int s_shellNodeIdCounter = 1;

    public static IPropertyMapper<NavigationPage, MirageNavigationPageHandler> Mapper =
        new PropertyMapper<NavigationPage, MirageNavigationPageHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(NavigationPage.CurrentPage)] = MapCurrentPage,
            [nameof(NavigationPage.RootPage)] = MapRootPage,
            [nameof(Page.Title)] = MapTitle,
            [nameof(NavigationPage.BarBackgroundColor)] = MapBarBackgroundColor,
            [nameof(NavigationPage.BarTextColor)] = MapBarTextColor
        };

    public static CommandMapper<NavigationPage, MirageNavigationPageHandler> CommandMapper =
        new(MirageViewMapper.CommonViewCommandMapper)
        {
            [nameof(IStackNavigation.RequestNavigation)] = MapRequestNavigation
        };

    private readonly Dictionary<Page, IElementHandler> _pageHandlers = [];
    private readonly Dictionary<ToolbarItem, ToolbarItemSubscription> _toolbarItemSubscriptions = [];
    private readonly List<MiragePlatformView> _shellNodes = [];
    private readonly List<MiragePlatformView> _toolbarButtonNodes = [];
    private readonly List<MiragePlatformView> _interactiveNodes = [];
    private readonly Dictionary<MiragePlatformView, IMirageRuntimeEventTarget> _interactiveTargets = new(ReferenceEqualityComparer.Instance);

    private Page? _currentPage;
    private int _currentStackCount;
    private string _toolbarSignature = string.Empty;
    private bool _hasArrangedFrame;
    private double _lastArrangedWidth;
    private double _lastArrangedHeight;
    private bool _navigationBarVisible = true;
    private INotifyCollectionChanged? _currentToolbarCollection;
    private bool _historyWriteSuppressed;
    private readonly object _popStateSync = new();
    private bool _isPopStatePumpRunning;
    private string? _pendingPopStateLocation;
    private int _popStateSubscriptionId;
    private string? _lastProjectedPath;

    private MiragePlatformView? _navigationBar;
    private MiragePlatformView? _backButtonSlot;
    private MiragePlatformView? _titleSlot;
    private MiragePlatformView? _toolbarSlot;
    private MiragePlatformView? _contentHost;
    private MiragePlatformView? _backButtonNode;
    private MiragePlatformView? _titleNode;

    public MirageNavigationPageHandler()
        : base(ComponentKind.ContentContainer, Mapper, CommandMapper)
    {
        DefaultDisplayStyle = "grid";
    }

    IStackNavigationView INavigationViewHandler.VirtualView => VirtualView;

    object INavigationViewHandler.PlatformView => PlatformView;

    internal bool TryProjectActivePageTitle()
    {
        return ProjectCurrentRoute(MirageNavigationTrigger.Manual);
    }

    bool IMiragePageTitleProjectionHost.TryProjectActivePageTitle() => TryProjectActivePageTitle();

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        base.ConnectHandler(platformView);
        ApplyBaseStyles();
        EnsureShellStructure();
        SubscribeNavigationEvents();
        SubscribePopState();
        ApplyNavigationStack(ResolveNavigationStack(VirtualView), MirageNavigationTrigger.Startup);
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        UnsubscribePopState();
        UnsubscribeNavigationEvents();
        UnsubscribeCurrentPage();

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

        UnregisterShellStructure();
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

        var navigationBarVisible = currentPage is null || NavigationPage.GetHasNavigationBar(currentPage);
        var barHeight = navigationBarVisible ? DefaultNavigationBarHeight : 0;
        var contentHeightConstraint = heightConstraint;
        if (double.IsFinite(contentHeightConstraint))
        {
            contentHeightConstraint = Math.Max(0, contentHeightConstraint - barHeight);
        }

        var contentSize = currentView.Measure(widthConstraint, contentHeightConstraint);
        var desiredHeight = Math.Max(0, contentSize.Height + barHeight);
        return new Size(
            ResolveDesiredDimension(widthConstraint, contentSize.Width),
            ResolveDesiredDimension(heightConstraint, desiredHeight));
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
        var visibilityChanged = handler.UpdateNavigationChrome();
        if (visibilityChanged && handler._hasArrangedFrame)
        {
            handler.ArrangeCurrentPage();
        }
        handler.ProjectCurrentRoute(MirageNavigationTrigger.Manual);
        handler.RequestRender();
    }

    public static void MapBarBackgroundColor(MirageNavigationPageHandler handler, NavigationPage navigationPage)
    {
        _ = navigationPage;
        if (!handler.UpdateNavigationChrome())
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapBarTextColor(MirageNavigationPageHandler handler, NavigationPage navigationPage)
    {
        _ = navigationPage;
        if (!handler.UpdateNavigationChrome())
        {
            return;
        }

        handler.RequestRender();
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
            EnsureShellStructure();
            _contentHost?.ClearChildren();
            if (currentPage is not null
                && _pageHandlers.TryGetValue(currentPage, out var currentHandler)
                && currentHandler.PlatformView is MiragePlatformView currentPlatformView)
            {
                _contentHost?.AddChild(currentPlatformView);
            }
        }
        CaptureStep("mount");

        _currentStackCount = normalizedStack.Count;
        SubscribeCurrentPage(currentPage);
        UpdateNavigationChrome();
        CaptureStep("chrome");

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
        var barHeight = _navigationBarVisible ? DefaultNavigationBarHeight : 0;
        var contentHeight = Math.Max(0, _lastArrangedHeight - barHeight);

        if (_currentPage is not IView currentView)
        {
            return;
        }

        currentView.Measure(_lastArrangedWidth, contentHeight);
        currentView.Arrange(new Rect(0, 0, _lastArrangedWidth, contentHeight));
    }

    private void ApplyBaseStyles()
    {
        if (!TryGetPlatformView(out var platformView))
        {
            return;
        }

        platformView.SetStyle("display", "grid");
        platformView.SetStyle("gridTemplateColumns", "minmax(0, 1fr)");
        platformView.SetStyle("gridTemplateRows", $"{MirageViewMapper.ToPx(DefaultNavigationBarHeight)} minmax(0, 1fr)");
        platformView.SetStyle("justifyItems", "stretch");
        platformView.SetStyle("alignItems", "stretch");
        platformView.SetStyle("width", "100%");
        platformView.SetStyle("height", "100%");
        platformView.SetStyle("overflow", "hidden");
        platformView.SetStyle("overflowY", "hidden");
        platformView.SetStyle("overflowX", "hidden");
    }

    private void EnsureShellStructure()
    {
        if (_navigationBar is not null
            && _contentHost is not null
            && _backButtonNode is not null
            && _titleNode is not null
            && _toolbarSlot is not null)
        {
            return;
        }

        if (!TryGetPlatformView(out var platformView))
        {
            return;
        }

        platformView.ClearChildren();
        _shellNodes.Clear();
        _toolbarButtonNodes.Clear();
        _interactiveNodes.Clear();
        _interactiveTargets.Clear();

        _navigationBar = CreateShellNode(ComponentKind.ContentContainer, NavigationBarAutomationId);
        _backButtonSlot = CreateShellNode(ComponentKind.ContentContainer, null);
        _titleSlot = CreateShellNode(ComponentKind.ContentContainer, null);
        _toolbarSlot = CreateShellNode(ComponentKind.HorizontalStack, NavigationToolbarAutomationId);
        _contentHost = CreateShellNode(ComponentKind.ContentContainer, NavigationContentHostAutomationId);
        _backButtonNode = CreateInteractiveButtonNode(
            NavigationBackButtonAutomationId,
            "Back",
            OnBackButtonClicked);
        _titleNode = CreateShellNode(ComponentKind.Label, NavigationTitleAutomationId);
        _titleNode.SetProperty("Text", string.Empty);

        _backButtonSlot.AddChild(_backButtonNode);
        _titleSlot.AddChild(_titleNode);
        _navigationBar.AddChild(_backButtonSlot);
        _navigationBar.AddChild(_titleSlot);
        _navigationBar.AddChild(_toolbarSlot);
        platformView.AddChild(_navigationBar);
        platformView.AddChild(_contentHost);

        ApplyShellStyles();
        UpdateNavigationChrome();
        EnsureRegisteredNodes();
    }

    private MiragePlatformView CreateShellNode(ComponentKind kind, string? automationId)
    {
        var node = MiragePlatformView.CreateControl(kind);
        if (!string.IsNullOrWhiteSpace(automationId))
        {
            node.SetProperty("AutomationId", automationId);
        }

        RegisterNode(node, eventTarget: null);
        _shellNodes.Add(node);
        return node;
    }

    private MiragePlatformView CreateInteractiveButtonNode(
        string automationId,
        string text,
        Action onClick)
    {
        var node = MiragePlatformView.CreateControl(ComponentKind.Button);
        node.SetProperty("AutomationId", automationId);
        node.SetProperty("Text", text);
        node.SetEvent("click", enabled: true);
        var eventTarget = new ActionEventTarget((eventName, payload) =>
        {
            _ = payload;
            if (string.Equals(eventName, "click", StringComparison.Ordinal))
            {
                onClick();
            }
        });
        RegisterNode(node, eventTarget);
        _shellNodes.Add(node);
        _interactiveNodes.Add(node);
        _interactiveTargets[node] = eventTarget;
        return node;
    }

    private void RegisterNode(MiragePlatformView node, IMirageRuntimeEventTarget? eventTarget)
    {
        if (Coordinator is { } coordinator)
        {
            coordinator.Register(node, eventTarget);
            return;
        }

        if (!string.IsNullOrWhiteSpace(node.NodeId))
        {
            return;
        }

        var localId = Interlocked.Increment(ref s_shellNodeIdCounter);
        node.AssignNodeId($"nav-shell-{localId:D4}");
    }

    private void EnsureRegisteredNodes()
    {
        if (Coordinator is not { } coordinator)
        {
            return;
        }

        foreach (var node in _shellNodes)
        {
            _interactiveTargets.TryGetValue(node, out var target);
            coordinator.Register(node, target);
        }

        foreach (var node in _toolbarButtonNodes)
        {
            _interactiveTargets.TryGetValue(node, out var target);
            coordinator.Register(node, target);
        }
    }

    private void UnregisterShellStructure()
    {
        ClearToolbarNodes();
        UnsubscribeToolbarCollection();

        if (Coordinator is { } coordinator)
        {
            foreach (var node in _interactiveNodes)
            {
                coordinator.Unregister(node);
            }

            foreach (var node in _shellNodes)
            {
                coordinator.Unregister(node);
            }
        }

        _interactiveNodes.Clear();
        _interactiveTargets.Clear();
        _shellNodes.Clear();
        _navigationBar = null;
        _backButtonSlot = null;
        _titleSlot = null;
        _toolbarSlot = null;
        _contentHost = null;
        _backButtonNode = null;
        _titleNode = null;
        _toolbarSignature = string.Empty;
    }

    private void ApplyShellStyles()
    {
        if (_navigationBar is null
            || _backButtonSlot is null
            || _titleSlot is null
            || _toolbarSlot is null
            || _contentHost is null
            || _backButtonNode is null
            || _titleNode is null)
        {
            return;
        }

        _navigationBar.SetStyle("display", "grid");
        _navigationBar.SetStyle("gridColumnStart", "1");
        _navigationBar.SetStyle("gridColumnEnd", "2");
        _navigationBar.SetStyle("gridRowStart", "1");
        _navigationBar.SetStyle("gridRowEnd", "2");
        // Keep left/right columns symmetric so title stays centered on the full page width.
        _navigationBar.SetStyle("gridTemplateColumns", "minmax(0, 1fr) auto minmax(0, 1fr)");
        _navigationBar.SetStyle("alignItems", "center");
        _navigationBar.SetStyle("columnGap", "10px");
        _navigationBar.SetStyle("boxSizing", "border-box");
        _navigationBar.SetStyle("padding", "0 12px");
        _navigationBar.SetStyle("width", "100%");
        _navigationBar.SetStyle("height", MirageViewMapper.ToPx(DefaultNavigationBarHeight));
        _navigationBar.SetStyle("borderBottom", "1px solid rgba(15,23,42,0.16)");
        _navigationBar.SetStyle("overflow", "hidden");

        _backButtonSlot.SetStyle("display", "grid");
        _backButtonSlot.SetStyle("alignItems", "center");
        _backButtonSlot.SetStyle("justifyItems", "start");
        _backButtonSlot.SetStyle("minWidth", "72px");

        _titleSlot.SetStyle("display", "grid");
        _titleSlot.SetStyle("alignItems", "center");
        _titleSlot.SetStyle("justifyItems", "stretch");
        _titleSlot.SetStyle("overflow", "hidden");
        _titleSlot.SetStyle("minWidth", "0");

        _titleNode.SetStyle("fontWeight", "600");
        _titleNode.SetStyle("fontFamily", MirageCommonViewProjection.DefaultSystemFontFamily);
        _titleNode.SetStyle("textAlign", "center");
        _titleNode.SetStyle("width", "100%");
        _titleNode.SetStyle("justifySelf", "stretch");
        _titleNode.SetStyle("overflow", "hidden");
        _titleNode.SetStyle("textOverflow", "ellipsis");
        _titleNode.SetStyle("whiteSpace", "nowrap");
        _titleNode.SetStyle("display", "block");

        _toolbarSlot.SetStyle("display", "flex");
        _toolbarSlot.SetStyle("flexDirection", "row");
        _toolbarSlot.SetStyle("alignItems", "center");
        _toolbarSlot.SetStyle("gap", "8px");
        _toolbarSlot.SetStyle("justifyContent", "flex-end");
        _toolbarSlot.SetStyle("justifySelf", "stretch");
        _toolbarSlot.SetStyle("minWidth", "0");

        _backButtonNode.SetStyle("minWidth", "68px");
        _backButtonNode.SetStyle("height", "30px");
        _backButtonNode.SetStyle("cursor", "pointer");
        _backButtonNode.SetStyle("whiteSpace", "nowrap");

        _contentHost.SetStyle("display", "grid");
        _contentHost.SetStyle("gridColumnStart", "1");
        _contentHost.SetStyle("gridColumnEnd", "2");
        _contentHost.SetStyle("gridRowStart", "2");
        _contentHost.SetStyle("gridRowEnd", "3");
        _contentHost.SetStyle("gridTemplateColumns", "minmax(0, 1fr)");
        _contentHost.SetStyle("gridTemplateRows", "minmax(0, 1fr)");
        _contentHost.SetStyle("width", "100%");
        _contentHost.SetStyle("height", "100%");
        _contentHost.SetStyle("minWidth", "0");
        _contentHost.SetStyle("minHeight", "0");
        _contentHost.SetStyle("overflow", "hidden");
        _contentHost.SetStyle("overflowX", "hidden");
        _contentHost.SetStyle("overflowY", "hidden");
    }

    private bool UpdateNavigationChrome()
    {
        if (!TryGetPlatformView(out var platformView))
        {
            return false;
        }

        EnsureShellStructure();
        EnsureRegisteredNodes();
        if (_navigationBar is null
            || _backButtonNode is null
            || _titleNode is null
            || _toolbarSlot is null)
        {
            return false;
        }

        var changed = false;
        var currentPage = _currentPage;
        var navigationBarVisible = currentPage is null || NavigationPage.GetHasNavigationBar(currentPage);
        var barRowCss = navigationBarVisible
            ? MirageViewMapper.ToPx(DefaultNavigationBarHeight)
            : "0px";
        changed |= SetStyleIfChanged(platformView, "gridTemplateRows", $"{barRowCss} minmax(0, 1fr)");
        changed |= SetStyleIfChanged(_navigationBar, "display", navigationBarVisible ? "grid" : "none");

        _navigationBarVisible = navigationBarVisible;

        var barBackground = MirageCommonViewProjection.ToCssColor(VirtualView.BarBackgroundColor) ?? "rgba(241,245,249,1)";
        var barForeground = MirageCommonViewProjection.ToCssColor(VirtualView.BarTextColor) ?? MirageCommonViewProjection.DefaultSystemTextColor;

        changed |= SetStyleIfChanged(_navigationBar, "backgroundColor", barBackground);
        changed |= SetStyleIfChanged(_titleNode, "color", barForeground);

        var title = ResolveVisibleTitle(currentPage, VirtualView);
        changed |= SetPropertyIfChanged(_titleNode, "Text", title);

        var backButtonVisible = _currentStackCount > 1
            && navigationBarVisible
            && (currentPage is null || NavigationPage.GetHasBackButton(currentPage));
        changed |= SetStyleIfChanged(_backButtonNode, "display", backButtonVisible ? "inline-flex" : "none");
        changed |= SetStyleIfChanged(_backButtonNode, "color", barForeground);
        changed |= SetStyleIfChanged(_backButtonNode, "--accent-foreground-rest", barForeground);
        changed |= SetStyleIfChanged(_backButtonNode, "--accent-foreground-hover", barForeground);
        changed |= SetStyleIfChanged(_backButtonNode, "--accent-foreground-active", barForeground);
        changed |= SetStyleIfChanged(_backButtonNode, "--foreground-on-accent-rest", barForeground);
        changed |= SetStyleIfChanged(_backButtonNode, "--foreground-on-accent-hover", barForeground);
        changed |= SetStyleIfChanged(_backButtonNode, "--foreground-on-accent-active", barForeground);

        changed |= RebuildToolbarNodes(barForeground, navigationBarVisible);
        EnsureRegisteredNodes();
        return changed;
    }

    private bool RebuildToolbarNodes(string barForeground, bool navigationBarVisible)
    {
        if (_toolbarSlot is null)
        {
            return false;
        }

        var orderedItems = ResolveOrderedToolbarItems(_currentPage);
        var signature = BuildToolbarSignature(orderedItems, navigationBarVisible, barForeground);
        if (string.Equals(signature, _toolbarSignature, StringComparison.Ordinal))
        {
            return false;
        }

        ClearToolbarNodes();

        for (var index = 0; index < orderedItems.Length; index++)
        {
            var item = orderedItems[index];
            var itemEnabled = navigationBarVisible && IsToolbarItemEnabled(item);
            var buttonNode = MiragePlatformView.CreateControl(ComponentKind.Button);
            buttonNode.SetProperty("AutomationId", $"mirage-navigation-toolbar-item-{index + 1}");
            buttonNode.SetProperty("Text", item.Text ?? string.Empty);
            buttonNode.SetEvent("click", enabled: true);
            ApplyToolbarButtonStyle(buttonNode, itemEnabled, barForeground);

            var eventTarget = new ActionEventTarget((eventName, payload) =>
            {
                _ = payload;
                if (string.Equals(eventName, "click", StringComparison.Ordinal))
                {
                    OnToolbarItemClicked(item);
                }
            });

            RegisterNode(buttonNode, eventTarget);
            _interactiveNodes.Add(buttonNode);
            _interactiveTargets[buttonNode] = eventTarget;
            _toolbarButtonNodes.Add(buttonNode);
            _toolbarSlot.AddChild(buttonNode);
        }

        _toolbarSignature = signature;
        return true;
    }

    private void ClearToolbarNodes()
    {
        if (_toolbarSlot is not null)
        {
            _toolbarSlot.ClearChildren();
        }

        if (Coordinator is { } coordinator)
        {
            foreach (var node in _toolbarButtonNodes)
            {
                coordinator.Unregister(node);
            }
        }

        foreach (var node in _toolbarButtonNodes)
        {
            _interactiveTargets.Remove(node);
            _interactiveNodes.Remove(node);
        }

        _toolbarButtonNodes.Clear();
    }

    private static ToolbarItem[] ResolveOrderedToolbarItems(Page? page)
    {
        if (page?.ToolbarItems is null || page.ToolbarItems.Count == 0)
        {
            return [];
        }

        return page.ToolbarItems
            .Select((item, index) => new ToolbarSortRecord(item, index))
            .OrderBy(static record => (int)record.Item.Order)
            .ThenBy(static record => record.Item.Priority)
            .ThenBy(static record => record.OriginalIndex)
            .Select(static record => record.Item)
            .ToArray();
    }

    private static string BuildToolbarSignature(ToolbarItem[] items, bool navigationBarVisible, string barForeground)
    {
        var parts = new List<string>(items.Length + 2)
        {
            navigationBarVisible ? "bar:visible" : "bar:hidden",
            $"count:{items.Length}",
            $"fg:{barForeground}"
        };

        for (var index = 0; index < items.Length; index++)
        {
            var item = items[index];
            var isEnabled = IsToolbarItemEnabled(item);
            parts.Add(
                $"{index}:{RuntimeHelpers.GetHashCode(item)}:{(int)item.Order}:{item.Priority}:{item.Text}:{isEnabled}:{item.CommandParameter}");
        }

        return string.Join("|", parts);
    }

    private static bool IsToolbarItemEnabled(ToolbarItem item)
    {
        var parameter = item.CommandParameter;
        return item.IsEnabled && (item.Command?.CanExecute(parameter) ?? true);
    }

    private static void ApplyToolbarButtonStyle(MiragePlatformView node, bool isEnabled, string foregroundColor)
    {
        node.SetStyle("height", "30px");
        node.SetStyle("whiteSpace", "nowrap");
        node.SetStyle("cursor", isEnabled ? "pointer" : "default");
        node.SetStyle("opacity", isEnabled ? "1" : "0.55");
        node.SetStyle("color", foregroundColor);
        node.SetStyle("--accent-foreground-rest", foregroundColor);
        node.SetStyle("--accent-foreground-hover", foregroundColor);
        node.SetStyle("--accent-foreground-active", foregroundColor);
        node.SetStyle("--foreground-on-accent-rest", foregroundColor);
        node.SetStyle("--foreground-on-accent-hover", foregroundColor);
        node.SetStyle("--foreground-on-accent-active", foregroundColor);
    }

    private void OnBackButtonClicked()
    {
        DispatchOnUiThreadIfRequired(() =>
        {
            var stackCount = VirtualView.Navigation.NavigationStack.Count;
            if (stackCount <= 1)
            {
                return;
            }

            if (_currentPage is not null && !NavigationPage.GetHasBackButton(_currentPage))
            {
                return;
            }

            var popTask = VirtualView.Navigation.PopAsync(animated: false);
            if (!MirageDiagnostics.IsEnabled || popTask.IsCompletedSuccessfully)
            {
                return;
            }

            _ = popTask.ContinueWith(
                static task =>
                {
                    if (!task.IsFaulted)
                    {
                        return;
                    }

                    var message = task.Exception?.GetBaseException().ToString() ?? "<unknown>";
                    message = message.Replace(Environment.NewLine, " | ", StringComparison.Ordinal);
                    MirageDiagnostics.Log("nav.back", $"popTask faulted exception={message}");
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        });
    }

    private void OnToolbarItemClicked(ToolbarItem item)
    {
        DispatchOnUiThreadIfRequired(() =>
        {
            if (!_navigationBarVisible || !IsToolbarItemEnabled(item))
            {
                return;
            }

            ActivateToolbarItem(item);
        });
    }

    private static void ActivateToolbarItem(ToolbarItem item)
    {
        // Prefer MAUI's menu-item activation path so Clicked handlers fire
        // even when Command is not configured.
        if (item is IMenuItemController menuItemController)
        {
            menuItemController.Activate();
            return;
        }

        var command = item.Command;
        var parameter = item.CommandParameter;
        if (command?.CanExecute(parameter) is true)
        {
            command.Execute(parameter);
        }
    }

    private void DispatchOnUiThreadIfRequired(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var dispatcher = GetDispatcher();
        if (dispatcher is null || !dispatcher.IsDispatchRequired)
        {
            action();
            return;
        }

        if (!dispatcher.Dispatch(action))
        {
            action();
        }
    }

    private static string ResolveVisibleTitle(Page? currentPage, NavigationPage navigationPage)
    {
        if (!string.IsNullOrWhiteSpace(currentPage?.Title))
        {
            return currentPage.Title;
        }

        return navigationPage.Title ?? string.Empty;
    }

    private static bool SetStyleIfChanged(MiragePlatformView node, string styleName, string? value)
    {
        var current = node.GetStyleValue(styleName);
        if (string.Equals(current, value, StringComparison.Ordinal))
        {
            return false;
        }

        node.SetStyle(styleName, value);
        return true;
    }

    private static bool SetPropertyIfChanged(MiragePlatformView node, string propertyName, string? value)
    {
        var current = node.GetPropertyString(propertyName);
        if (string.Equals(current, value, StringComparison.Ordinal))
        {
            return false;
        }

        node.SetProperty(propertyName, value);
        return true;
    }

    private bool ProjectCurrentRoute(MirageNavigationTrigger trigger)
    {
        if (_historyWriteSuppressed)
        {
            return false;
        }

        if (_currentPage is null)
        {
            return false;
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
            return false;
        }

        if (!routeSerializer.TrySerialize(_currentPage, routeRegistry, out var path))
        {
            return false;
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
        return true;
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

    private void SubscribeCurrentPage(Page? page)
    {
        if (ReferenceEquals(_currentPage, page))
        {
            return;
        }

        UnsubscribeCurrentPage();
        _currentPage = page;

        if (_currentPage is not null)
        {
            _currentPage.PropertyChanged += OnCurrentPagePropertyChanged;
        }

        SubscribeToolbarCollection(page);
    }

    private void UnsubscribeCurrentPage()
    {
        if (_currentPage is null)
        {
            UnsubscribeToolbarCollection();
            return;
        }

        _currentPage.PropertyChanged -= OnCurrentPagePropertyChanged;
        _currentPage = null;
        UnsubscribeToolbarCollection();
    }

    private void SubscribeToolbarCollection(Page? page)
    {
        UnsubscribeToolbarCollection();

        if (page?.ToolbarItems is null)
        {
            return;
        }

        if (page.ToolbarItems is INotifyCollectionChanged collection)
        {
            _currentToolbarCollection = collection;
            _currentToolbarCollection.CollectionChanged += OnToolbarCollectionChanged;
        }

        SyncToolbarSubscriptions(page.ToolbarItems);
    }

    private void UnsubscribeToolbarCollection()
    {
        if (_currentToolbarCollection is not null)
        {
            _currentToolbarCollection.CollectionChanged -= OnToolbarCollectionChanged;
            _currentToolbarCollection = null;
        }

        foreach (var toolbarItem in _toolbarItemSubscriptions.Keys.ToArray())
        {
            RemoveToolbarSubscription(toolbarItem);
        }

        _toolbarItemSubscriptions.Clear();
        _toolbarSignature = string.Empty;
    }

    private void OnToolbarCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        SyncToolbarSubscriptions(_currentPage?.ToolbarItems);
        if (!UpdateNavigationChrome())
        {
            return;
        }

        RequestRender();
    }

    private void SyncToolbarSubscriptions(IEnumerable<ToolbarItem>? items)
    {
        var currentItems = items?.Where(static item => item is not null).ToArray() ?? [];
        var currentSet = new HashSet<ToolbarItem>(currentItems);
        foreach (var existing in _toolbarItemSubscriptions.Keys.ToArray())
        {
            if (!currentSet.Contains(existing))
            {
                RemoveToolbarSubscription(existing);
            }
        }

        foreach (var item in currentItems)
        {
            if (!_toolbarItemSubscriptions.ContainsKey(item))
            {
                AddToolbarSubscription(item);
            }
            else
            {
                RefreshToolbarCommandSubscription(item);
            }
        }
    }

    private void AddToolbarSubscription(ToolbarItem item)
    {
        var subscription = new ToolbarItemSubscription(item);
        subscription.PropertyChangedHandler = OnToolbarItemPropertyChanged;
        subscription.CommandCanExecuteChangedHandler = OnToolbarItemCommandCanExecuteChanged;
        item.PropertyChanged += subscription.PropertyChangedHandler;
        subscription.Command = item.Command;
        subscription.Command?.CanExecuteChanged += subscription.CommandCanExecuteChangedHandler;
        _toolbarItemSubscriptions[item] = subscription;
    }

    private void RemoveToolbarSubscription(ToolbarItem item)
    {
        if (!_toolbarItemSubscriptions.TryGetValue(item, out var subscription))
        {
            return;
        }

        item.PropertyChanged -= subscription.PropertyChangedHandler;
        if (subscription.Command is not null)
        {
            subscription.Command.CanExecuteChanged -= subscription.CommandCanExecuteChangedHandler;
        }

        _toolbarItemSubscriptions.Remove(item);
    }

    private void RefreshToolbarCommandSubscription(ToolbarItem item)
    {
        if (!_toolbarItemSubscriptions.TryGetValue(item, out var subscription))
        {
            return;
        }

        if (ReferenceEquals(subscription.Command, item.Command))
        {
            return;
        }

        if (subscription.Command is not null)
        {
            subscription.Command.CanExecuteChanged -= subscription.CommandCanExecuteChangedHandler;
        }

        subscription.Command = item.Command;
        subscription.Command?.CanExecuteChanged += subscription.CommandCanExecuteChangedHandler;
    }

    private void OnToolbarItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ToolbarItem toolbarItem)
        {
            return;
        }

        if (string.Equals(e.PropertyName, nameof(MenuItem.Command), StringComparison.Ordinal))
        {
            RefreshToolbarCommandSubscription(toolbarItem);
        }

        if (!UpdateNavigationChrome())
        {
            return;
        }

        RequestRender();
    }

    private void OnToolbarItemCommandCanExecuteChanged(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        if (!UpdateNavigationChrome())
        {
            return;
        }

        RequestRender();
    }

    private void OnCurrentPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = sender;
        var propertyName = e.PropertyName;
        if (!string.IsNullOrEmpty(propertyName)
            && !string.Equals(propertyName, nameof(Page.Title), StringComparison.Ordinal)
            && propertyName != NavigationPage.HasNavigationBarProperty.PropertyName
            && propertyName != NavigationPage.HasBackButtonProperty.PropertyName)
        {
            return;
        }

        var chromeChanged = UpdateNavigationChrome();
        if (propertyName is null || string.Equals(propertyName, nameof(Page.Title), StringComparison.Ordinal))
        {
            ProjectCurrentRoute(MirageNavigationTrigger.Manual);
        }

        if (chromeChanged && _hasArrangedFrame)
        {
            ArrangeCurrentPage();
        }

        if (chromeChanged || propertyName is null || string.Equals(propertyName, nameof(Page.Title), StringComparison.Ordinal))
        {
            RequestRender();
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

    private void DisconnectPage(Page page)
    {
        if (!_pageHandlers.TryGetValue(page, out var handler))
        {
            return;
        }

        MirageHandlerDisconnect.SafeDisconnect(handler);
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

    private readonly record struct ToolbarSortRecord(ToolbarItem Item, int OriginalIndex);

    private sealed class ActionEventTarget : IMirageRuntimeEventTarget
    {
        private readonly Action<string, string?> _dispatch;

        public ActionEventTarget(Action<string, string?> dispatch)
        {
            _dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
        }

        public void OnRuntimeEvent(string eventName, string? payload)
        {
            _dispatch(eventName, payload);
        }
    }

    private sealed class ToolbarItemSubscription
    {
        public ToolbarItemSubscription(ToolbarItem item)
        {
            Item = item;
        }

        public ToolbarItem Item { get; }

        public PropertyChangedEventHandler PropertyChangedHandler { get; set; } = default!;

        public EventHandler CommandCanExecuteChangedHandler { get; set; } = default!;

        public ICommand? Command { get; set; }
    }
}
