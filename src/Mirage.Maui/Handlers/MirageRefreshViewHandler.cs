using System.Globalization;
using System.Text.Json;
using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Mirage.Maui.Handlers;

internal sealed class MirageRefreshViewHandler : MirageViewHandler<IRefreshView>, IRefreshViewHandler, IMirageRuntimeEventTarget
{
    private const double DefaultWidth = 260;
    private const double DefaultContentHeight = 220;
    private const double IndicatorRowHeight = 44;
    private const double PullTriggerDistance = 72;
    private const string DefaultIndicatorColorCss = "rgba(51,65,85,1)";
    private const string DefaultTriggerColorCss = "rgba(15,23,42,1)";
    private const string DefaultTriggerBackgroundCss = "rgba(248,250,252,1)";
    private const string PullIndicatorBackgroundCss = "rgba(14,165,233,0.08)";
    private static long s_internalNodeIdCounter;

    private static readonly JsonSerializerOptions PullStateJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal static readonly string[] ParityPropertyKeys =
    [
        nameof(IRefreshView.Content),
        nameof(IRefreshView.IsRefreshing),
        nameof(IRefreshView.IsRefreshEnabled),
        nameof(IRefreshView.RefreshColor),
        nameof(RefreshView.Command),
        nameof(RefreshView.CommandParameter)
    ];

    internal static readonly string[] ParityCommandKeys = [];

    internal static readonly string[] ParityExtensionPropertyKeys =
    [
        nameof(IView.IsEnabled)
    ];

    public static IPropertyMapper<IRefreshView, MirageRefreshViewHandler> Mapper =
        new PropertyMapper<IRefreshView, MirageRefreshViewHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(IRefreshView.Content)] = MapContent,
            [nameof(IRefreshView.IsRefreshing)] = MapIsRefreshing,
            [nameof(IRefreshView.IsRefreshEnabled)] = MapIsRefreshEnabled,
            [nameof(IRefreshView.RefreshColor)] = MapRefreshColor,
            [nameof(RefreshView.Command)] = MapCommand,
            [nameof(RefreshView.CommandParameter)] = MapCommandParameter,
            [nameof(IView.IsEnabled)] = MapIsEnabled,
            ["IsEnabled"] = MapIsEnabled
        };

    public static CommandMapper<IRefreshView, MirageRefreshViewHandler> CommandMapper =
        new(MirageViewMapper.CommonViewCommandMapper);

    private MiragePlatformView? _indicatorHost;
    private MiragePlatformView? _stateHost;
    private MiragePlatformView? _spinnerNode;
    private MiragePlatformView? _stateLabel;
    private MiragePlatformView? _triggerButton;
    private MiragePlatformView? _contentViewport;
    private RefreshTriggerRuntimeEventTarget? _triggerEventTarget;

    private IView? _currentContent;
    private IElementHandler? _currentContentHandler;
    private MiragePlatformView? _currentContentPlatformView;
    private bool _currentContentUsesChildScrollSource;

    private bool _pullActive;
    private bool _pullArmed;
    private int _pullDistance;
    private string? _configuredPullSourceNodeId;
    private bool _isPullConfigured;
    private Rect _lastArrangedFrame;

    public MirageRefreshViewHandler()
        : base(ComponentKind.RefreshView, Mapper, CommandMapper)
    {
        DefaultDisplayStyle = "flex";
    }

    IRefreshView IRefreshViewHandler.VirtualView => VirtualView;

    object IRefreshViewHandler.PlatformView => PlatformView;

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        var content = ResolvePresentedContent(VirtualView);
        if (content is null)
        {
            return ResolveMeasuredSizeWithConstraints(
                VirtualView,
                widthConstraint,
                heightConstraint,
                DefaultWidth,
                DefaultContentHeight + IndicatorRowHeight);
        }

        var contentHeightConstraint = double.IsFinite(heightConstraint)
            ? Math.Max(0, heightConstraint - IndicatorRowHeight)
            : heightConstraint;
        var contentSize = content.Measure(widthConstraint, contentHeightConstraint);
        var measuredWidth = contentSize.Width > 0 ? contentSize.Width : DefaultWidth;
        var measuredHeight = contentSize.Height > 0 ? contentSize.Height : DefaultContentHeight;
        return ResolveMeasuredSizeWithConstraints(
            VirtualView,
            widthConstraint,
            heightConstraint,
            measuredWidth,
            measuredHeight + IndicatorRowHeight);
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        EnsureStructure();
        ApplyContainerStyles();
        _ = ApplyRefreshColor(VirtualView);
        _ = ApplyRefreshState(VirtualView);

        base.ConnectHandler(platformView);

        SetContent(ResolvePresentedContent(VirtualView));
        ConfigurePullObservation();
        ArrangeUsingLastFrame();
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        DisablePullObservation(platformView.NodeId);
        DisconnectCurrentContent(suppressLifecycleUpdates: true);
        DisconnectStructure(platformView);
        platformView.ClearChildren();
        _pullActive = false;
        _pullArmed = false;
        _pullDistance = 0;
        _lastArrangedFrame = default;
        base.DisconnectHandler(platformView);
    }

    public override void PlatformArrange(Rect frame)
    {
        _lastArrangedFrame = frame;
        base.PlatformArrange(frame);

        var width = Math.Max(0, frame.Width);
        var height = Math.Max(0, frame.Height);
        var contentHeight = Math.Max(0, height - IndicatorRowHeight);

        if (ResolvePresentedContent(VirtualView) is not IView content || _currentContentPlatformView is null)
        {
            return;
        }

        NormalizeHostedContentFlowStyles(_currentContentPlatformView);

        if (_currentContentUsesChildScrollSource)
        {
            content.Arrange(new Rect(0, 0, width, contentHeight));
            return;
        }

        var desiredHeight = content.Measure(width, double.PositiveInfinity).Height;
        var arrangedHeight = Math.Max(contentHeight, desiredHeight);
        content.Arrange(new Rect(0, 0, width, arrangedHeight));
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        if (string.Equals(eventName, "pulltrigger", StringComparison.Ordinal))
        {
            OnPullTriggered();
            return;
        }

        if (!string.Equals(eventName, "pullstate", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(payload))
        {
            return;
        }

        PullStatePayload? nextState;
        try
        {
            nextState = JsonSerializer.Deserialize<PullStatePayload>(payload, PullStateJsonOptions);
        }
        catch (JsonException)
        {
            return;
        }

        if (nextState is null)
        {
            return;
        }

        if (!UpdatePullState(nextState.Active, nextState.Armed, nextState.Distance))
        {
            return;
        }

        _ = ApplyRefreshState(VirtualView);
        RequestRender();
    }

    public static void MapContent(MirageRefreshViewHandler handler, IRefreshView refreshView)
    {
        handler.SetContent(ResolvePresentedContent(refreshView));
        handler.ConfigurePullObservation();
        handler.ArrangeUsingLastFrame();
    }

    public static void MapIsRefreshing(MirageRefreshViewHandler handler, IRefreshView refreshView)
    {
        if (!refreshView.IsRefreshing)
        {
            handler.UpdatePullState(active: false, armed: false, distance: 0);
        }

        var changed = handler.ApplyRefreshState(refreshView);
        handler.ConfigurePullObservation();
        if (changed)
        {
            handler.RequestRender();
        }
    }

    public static void MapIsRefreshEnabled(MirageRefreshViewHandler handler, IRefreshView refreshView)
    {
        if (!refreshView.IsRefreshEnabled)
        {
            handler.UpdatePullState(active: false, armed: false, distance: 0);
        }

        var changed = handler.ApplyRefreshState(refreshView);
        handler.ConfigurePullObservation();
        if (changed)
        {
            handler.RequestRender();
        }
    }

    public static void MapRefreshColor(MirageRefreshViewHandler handler, IRefreshView refreshView)
    {
        var changed = handler.ApplyRefreshColor(refreshView);
        changed |= handler.ApplyRefreshState(refreshView);
        if (changed)
        {
            handler.RequestRender();
        }
    }

    public static void MapCommand(MirageRefreshViewHandler handler, IRefreshView refreshView)
    {
        if (!handler.ApplyRefreshState(refreshView))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapCommandParameter(MirageRefreshViewHandler handler, IRefreshView refreshView)
    {
        if (!handler.ApplyRefreshState(refreshView))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapIsEnabled(MirageRefreshViewHandler handler, IRefreshView refreshView)
    {
        if (!refreshView.IsEnabled)
        {
            handler.UpdatePullState(active: false, armed: false, distance: 0);
        }

        var changed = handler.ApplyRefreshState(refreshView);
        handler.ConfigurePullObservation();
        if (changed)
        {
            handler.RequestRender();
        }
    }

    private bool EnsureStructure()
    {
        var changed = false;
        if (_indicatorHost is null)
        {
            _indicatorHost = MiragePlatformView.CreateWeb("div");
            _indicatorHost.SetAttribute("data-maui-rv-indicator-host", "true");
            RegisterChildNode(_indicatorHost);
            changed = true;
        }

        if (_stateHost is null)
        {
            _stateHost = MiragePlatformView.CreateWeb("div");
            _stateHost.SetAttribute("data-maui-rv-state-host", "true");
            RegisterChildNode(_stateHost);
            changed = true;
        }

        if (_spinnerNode is null)
        {
            _spinnerNode = MiragePlatformView.CreateWeb("span");
            _spinnerNode.SetAttribute("data-maui-rv-spinner", "true");
            _spinnerNode.SetAttribute("aria-hidden", "true");
            RegisterChildNode(_spinnerNode);
            changed = true;
        }

        if (_stateLabel is null)
        {
            _stateLabel = MiragePlatformView.CreateWeb("span");
            _stateLabel.SetAttribute("data-maui-rv-state", "idle");
            RegisterChildNode(_stateLabel);
            changed = true;
        }

        if (_triggerButton is null)
        {
            _triggerButton = MiragePlatformView.CreateWeb("button");
            _triggerButton.SetAttribute("type", "button");
            _triggerButton.SetAttribute("data-maui-rv-trigger", "true");
            _triggerButton.SetProperty("textContent", "Refresh");
            _triggerButton.SetEvent("click", enabled: true);
            _triggerEventTarget = new RefreshTriggerRuntimeEventTarget(this);
            RegisterChildNode(_triggerButton, _triggerEventTarget);
            changed = true;
        }

        if (_contentViewport is null)
        {
            _contentViewport = MiragePlatformView.CreateWeb("div");
            _contentViewport.SetAttribute("data-maui-rv-content-host", "true");
            _contentViewport.SetAttribute("data-maui-rv-scroll-host", "true");
            RegisterChildNode(_contentViewport);
            changed = true;
        }

        if (_stateHost is not null
            && _spinnerNode is not null
            && _stateLabel is not null
            && (_stateHost.Children.Count != 2
                || !ReferenceEquals(_stateHost.Children[0], _spinnerNode)
                || !ReferenceEquals(_stateHost.Children[1], _stateLabel)))
        {
            _stateHost.ClearChildren();
            _stateHost.AddChild(_spinnerNode);
            _stateHost.AddChild(_stateLabel);
            changed = true;
        }

        if (_indicatorHost is not null
            && _stateHost is not null
            && _triggerButton is not null
            && (_indicatorHost.Children.Count != 2
                || !ReferenceEquals(_indicatorHost.Children[0], _stateHost)
                || !ReferenceEquals(_indicatorHost.Children[1], _triggerButton)))
        {
            _indicatorHost.ClearChildren();
            _indicatorHost.AddChild(_stateHost);
            _indicatorHost.AddChild(_triggerButton);
            changed = true;
        }

        if (_indicatorHost is not null
            && _contentViewport is not null
            && (PlatformView.Children.Count != 2
                || !ReferenceEquals(PlatformView.Children[0], _indicatorHost)
                || !ReferenceEquals(PlatformView.Children[1], _contentViewport)))
        {
            PlatformView.ClearChildren();
            PlatformView.AddChild(_indicatorHost);
            PlatformView.AddChild(_contentViewport);
            changed = true;
        }

        return changed;
    }

    private void DisconnectStructure(MiragePlatformView rootView)
    {
        rootView.ClearChildren();
        UnregisterChildNode(_indicatorHost);
        UnregisterChildNode(_stateHost);
        UnregisterChildNode(_spinnerNode);
        UnregisterChildNode(_stateLabel);
        UnregisterChildNode(_triggerButton);
        UnregisterChildNode(_contentViewport);
        _indicatorHost = null;
        _stateHost = null;
        _spinnerNode = null;
        _stateLabel = null;
        _triggerButton = null;
        _contentViewport = null;
        _triggerEventTarget = null;
    }

    private bool ApplyContainerStyles()
    {
        using var _ = BeginRenderBatch();

        var changed = SetStyleIfChanged(PlatformView, "display", DefaultDisplayStyle);
        changed |= SetStyleIfChanged(PlatformView, "flexDirection", "column");
        changed |= SetStyleIfChanged(PlatformView, "boxSizing", "border-box");
        changed |= SetStyleIfChanged(PlatformView, "overflow", "hidden");
        changed |= SetStyleIfChanged(PlatformView, "alignItems", "stretch");
        changed |= SetStyleIfChanged(PlatformView, "position", "relative");

        changed |= SetStyleIfChanged(_indicatorHost, "display", "flex");
        changed |= SetStyleIfChanged(_indicatorHost, "alignItems", "center");
        changed |= SetStyleIfChanged(_indicatorHost, "justifyContent", "space-between");
        changed |= SetStyleIfChanged(_indicatorHost, "gap", "8px");
        changed |= SetStyleIfChanged(_indicatorHost, "padding", "6px 10px");
        changed |= SetStyleIfChanged(_indicatorHost, "boxSizing", "border-box");
        changed |= SetStyleIfChanged(_indicatorHost, "borderBottom", "1px solid rgba(148,163,184,0.45)");
        changed |= SetStyleIfChanged(_indicatorHost, "minHeight", $"{IndicatorRowHeight:0.###}px");
        changed |= SetStyleIfChanged(_indicatorHost, "maxHeight", $"{IndicatorRowHeight:0.###}px");
        changed |= SetStyleIfChanged(_indicatorHost, "flex", $"0 0 {IndicatorRowHeight:0.###}px");

        changed |= SetStyleIfChanged(_stateHost, "display", "inline-flex");
        changed |= SetStyleIfChanged(_stateHost, "alignItems", "center");
        changed |= SetStyleIfChanged(_stateHost, "gap", "6px");
        changed |= SetStyleIfChanged(_stateHost, "minHeight", "16px");

        changed |= SetStyleIfChanged(_spinnerNode, "display", "inline-block");
        changed |= SetStyleIfChanged(_spinnerNode, "width", "13px");
        changed |= SetStyleIfChanged(_spinnerNode, "height", "13px");
        changed |= SetStyleIfChanged(_spinnerNode, "borderWidth", "2px");
        changed |= SetStyleIfChanged(_spinnerNode, "borderStyle", "solid");
        changed |= SetStyleIfChanged(_spinnerNode, "borderColor", "currentColor");
        changed |= SetStyleIfChanged(_spinnerNode, "borderRadius", "999px");
        changed |= SetStyleIfChanged(_spinnerNode, "boxSizing", "border-box");
        changed |= SetStyleIfChanged(_spinnerNode, "borderRightColor", "transparent");
        changed |= SetStyleIfChanged(_spinnerNode, "color", DefaultIndicatorColorCss);
        changed |= SetStyleIfChanged(_spinnerNode, "visibility", "visible");
        changed |= SetStyleIfChanged(_spinnerNode, "opacity", "0.35");
        changed |= SetStyleIfChanged(_spinnerNode, "animation", "maui-rv-spin 0.9s linear infinite");
        changed |= SetStyleIfChanged(_spinnerNode, "animationPlayState", "paused");

        changed |= SetStyleIfChanged(_stateLabel, "fontSize", "12px");
        changed |= SetStyleIfChanged(_stateLabel, "color", DefaultIndicatorColorCss);

        changed |= SetStyleIfChanged(_triggerButton, "padding", "5px 10px");
        changed |= SetStyleIfChanged(_triggerButton, "borderRadius", "8px");
        changed |= SetStyleIfChanged(_triggerButton, "border", "1px solid rgba(100,116,139,0.65)");
        changed |= SetStyleIfChanged(_triggerButton, "backgroundColor", DefaultTriggerBackgroundCss);
        changed |= SetStyleIfChanged(_triggerButton, "fontSize", "12px");
        changed |= SetStyleIfChanged(_triggerButton, "lineHeight", "1.2");
        changed |= SetStyleIfChanged(_triggerButton, "cursor", "pointer");
        changed |= SetStyleIfChanged(_triggerButton, "color", DefaultTriggerColorCss);

        changed |= SetStyleIfChanged(_contentViewport, "boxSizing", "border-box");
        changed |= SetStyleIfChanged(_contentViewport, "position", "relative");
        changed |= SetStyleIfChanged(_contentViewport, "minHeight", "0");
        changed |= SetStyleIfChanged(_contentViewport, "flex", "1 1 auto");
        changed |= SetStyleIfChanged(_contentViewport, "overflowX", "hidden");
        return changed;
    }

    private void SetContent(IView? content)
    {
        if (ReferenceEquals(_currentContent, content) && _currentContentHandler is not null)
        {
            return;
        }

        if (_currentContentHandler is not null && !ReferenceEquals(_currentContent, content))
        {
            DisconnectCurrentContent();
        }

        _contentViewport?.ClearChildren();
        _currentContent = null;
        _currentContentHandler = null;
        _currentContentPlatformView = null;
        _currentContentUsesChildScrollSource = false;

        if (content is null || MauiContext is null || _contentViewport is null)
        {
            ConfigurePullObservation();
            RequestRender();
            return;
        }

        IElementHandler contentHandler;
        try
        {
            contentHandler = content.ToHandler(MauiContext);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to create handler for RefreshView content type '{content.GetType().FullName}'.",
                ex);
        }

        if (contentHandler.PlatformView is not MiragePlatformView contentPlatformView)
        {
            MirageHandlerDisconnect.SafeDisconnect(contentHandler);
            throw new InvalidOperationException($"{GetType().Name} requires RefreshView content handlers to expose MiragePlatformView.");
        }

        _contentViewport.AddChild(contentPlatformView);
        if (contentHandler is IViewHandler viewHandler)
        {
            MirageViewMapper.MapLayoutAlignment(viewHandler, content);
        }

        _currentContent = content;
        _currentContentHandler = contentHandler;
        _currentContentPlatformView = contentPlatformView;
        _currentContentUsesChildScrollSource = contentHandler is IMirageScrollSourceProvider scrollSourceProvider
            && scrollSourceProvider.TryGetScrollSourceNodeId(out _);

        ApplyContentViewportMode();
        ConfigurePullObservation();
        RequestRender();
    }

    private void DisconnectCurrentContent(bool suppressLifecycleUpdates = false)
    {
        MirageHandlerDisconnect.SafeDisconnect(_currentContentHandler);
        _currentContentHandler = null;
        _currentContent = null;
        _currentContentPlatformView = null;
        _currentContentUsesChildScrollSource = false;
        _contentViewport?.ClearChildren();
        if (suppressLifecycleUpdates)
        {
            return;
        }

        ApplyContentViewportMode();
        ConfigurePullObservation();
        RequestRender();
    }

    private void ArrangeUsingLastFrame()
    {
        if (double.IsNaN(_lastArrangedFrame.Width) || double.IsNaN(_lastArrangedFrame.Height))
        {
            return;
        }

        if (Math.Abs(_lastArrangedFrame.Width) < 0.001 && Math.Abs(_lastArrangedFrame.Height) < 0.001)
        {
            return;
        }

        PlatformArrange(_lastArrangedFrame);
    }

    private bool ApplyRefreshState(IRefreshView refreshView)
    {
        using var _ = BeginRenderBatch();

        var effectiveEnabled = refreshView.IsEnabled && refreshView.IsRefreshEnabled;
        var triggerInteractive = effectiveEnabled && !refreshView.IsRefreshing;
        var stateKey = ResolveStateKey(refreshView);
        var changed = SetPropertyIfChanged("RefreshingState", refreshView.IsRefreshing ? "true" : "false");
        changed |= SetPropertyIfChanged("EnabledState", effectiveEnabled ? "true" : "false");
        changed |= SetPropertyIfChanged("BusyState", refreshView.IsRefreshing ? "true" : "false");
        changed |= SetPropertyIfChanged("PullActiveState", _pullActive ? "true" : "false");
        changed |= SetPropertyIfChanged("PullArmedState", _pullArmed ? "true" : "false");
        changed |= SetPropertyIfChanged("PullDistanceState", _pullDistance.ToString(CultureInfo.InvariantCulture));

        changed |= SetAttributeIfChanged(_stateLabel, "data-maui-rv-state", stateKey);
        changed |= SetPropertyIfChanged(_stateLabel, "textContent", ResolveStateText(refreshView, stateKey));

        changed |= SetAttributeIfChanged(_spinnerNode, "data-maui-rv-spinner-active", refreshView.IsRefreshing ? "true" : "false");
        changed |= SetStyleIfChanged(_spinnerNode, "visibility", "visible");
        changed |= SetStyleIfChanged(_spinnerNode, "opacity", refreshView.IsRefreshing ? "1" : _pullActive ? (_pullArmed ? "1" : "0.75") : "0.35");
        changed |= SetStyleIfChanged(_spinnerNode, "animationPlayState", refreshView.IsRefreshing ? "running" : "paused");
        changed |= SetStyleIfChanged(_indicatorHost, "backgroundColor", _pullActive && !refreshView.IsRefreshing ? PullIndicatorBackgroundCss : null);

        changed |= SetAttributeIfChanged(_triggerButton, "disabled", triggerInteractive ? null : "disabled");
        changed |= SetAttributeIfChanged(_triggerButton, "aria-disabled", triggerInteractive ? "false" : "true");
        changed |= SetStyleIfChanged(_triggerButton, "cursor", triggerInteractive ? "pointer" : "default");
        changed |= SetStyleIfChanged(_triggerButton, "opacity", triggerInteractive ? "1" : "0.65");
        changed |= SetPropertyIfChanged(_triggerButton, "textContent", refreshView.IsRefreshing ? "Refreshing..." : "Refresh");
        return changed;
    }

    private bool ApplyRefreshColor(IRefreshView refreshView)
    {
        var cssColor = refreshView.RefreshColor is SolidPaint solidPaint && solidPaint.Color is not null
            ? MirageCommonViewProjection.ToCssColor(solidPaint.Color)
            : null;

        using var _ = BeginRenderBatch();
        var changed = SetStyleIfChanged(_stateLabel, "color", cssColor ?? DefaultIndicatorColorCss);
        changed |= SetStyleIfChanged(_spinnerNode, "color", cssColor ?? DefaultIndicatorColorCss);
        changed |= SetStyleIfChanged(_triggerButton, "borderColor", cssColor);
        changed |= SetStyleIfChanged(_triggerButton, "color", cssColor ?? DefaultTriggerColorCss);
        return changed;
    }

    private void ApplyContentViewportMode()
    {
        var overflowY = _currentContentUsesChildScrollSource ? "hidden" : "auto";
        _ = SetStyleIfChanged(_contentViewport, "overflowY", overflowY);
    }

    private void ConfigurePullObservation()
    {
        if (Coordinator is null || string.IsNullOrWhiteSpace(PlatformView.NodeId))
        {
            return;
        }

        var sourceNodeId = ResolvePullSourceNodeId();
        var enabled = ShouldEnablePull(refreshView: VirtualView) && !string.IsNullOrWhiteSpace(sourceNodeId);
        if (!enabled)
        {
            DisablePullObservation();
            return;
        }

        if (_isPullConfigured
            && string.Equals(_configuredPullSourceNodeId, sourceNodeId, StringComparison.Ordinal))
        {
            return;
        }

        if (Coordinator.TryConfigureNodePullToRefresh(PlatformView.NodeId, sourceNodeId!, enabled: true, PullTriggerDistance))
        {
            _configuredPullSourceNodeId = sourceNodeId;
            _isPullConfigured = true;
        }
    }

    private void DisablePullObservation(string? ownerNodeId = null)
    {
        var resolvedOwnerNodeId = ownerNodeId;
        if (string.IsNullOrWhiteSpace(resolvedOwnerNodeId))
        {
            if (Coordinator is null || string.IsNullOrWhiteSpace(PlatformView.NodeId))
            {
                _configuredPullSourceNodeId = null;
                _isPullConfigured = false;
                return;
            }

            resolvedOwnerNodeId = PlatformView.NodeId;
        }

        if (Coordinator is null)
        {
            _configuredPullSourceNodeId = null;
            _isPullConfigured = false;
            return;
        }

        if (_isPullConfigured)
        {
            _ = Coordinator.TryConfigureNodePullToRefresh(
                resolvedOwnerNodeId,
                _configuredPullSourceNodeId ?? resolvedOwnerNodeId,
                enabled: false,
                PullTriggerDistance);
        }

        _configuredPullSourceNodeId = null;
        _isPullConfigured = false;
    }

    private string? ResolvePullSourceNodeId()
    {
        if (_currentContentHandler is IMirageScrollSourceProvider scrollSourceProvider
            && scrollSourceProvider.TryGetScrollSourceNodeId(out var childScrollSourceNodeId))
        {
            _currentContentUsesChildScrollSource = true;
            return childScrollSourceNodeId;
        }

        _currentContentUsesChildScrollSource = false;
        return _contentViewport is not null && !string.IsNullOrWhiteSpace(_contentViewport.NodeId)
            ? _contentViewport.NodeId
            : null;
    }

    private void OnPullTriggered()
    {
        TryStartRefresh();
    }

    private void OnTriggerClicked()
    {
        TryStartRefresh();
    }

    private void TryStartRefresh()
    {
        if (!VirtualView.IsEnabled || !VirtualView.IsRefreshEnabled || VirtualView.IsRefreshing)
        {
            return;
        }

        UpdatePullState(active: false, armed: false, distance: 0);
        VirtualView.IsRefreshing = true;
    }

    private bool UpdatePullState(bool active, bool armed, int distance)
    {
        var normalizedDistance = Math.Max(0, distance);
        if (_pullActive == active && _pullArmed == armed && _pullDistance == normalizedDistance)
        {
            return false;
        }

        _pullActive = active;
        _pullArmed = armed;
        _pullDistance = normalizedDistance;
        return true;
    }

    private string ResolveStateKey(IRefreshView refreshView)
    {
        if (refreshView.IsRefreshing)
        {
            return "refreshing";
        }

        if (!refreshView.IsEnabled)
        {
            return "disabled";
        }

        if (!refreshView.IsRefreshEnabled)
        {
            return "refresh-disabled";
        }

        if (_pullArmed)
        {
            return "armed";
        }

        if (_pullActive)
        {
            return "pulling";
        }

        return "idle";
    }

    private string ResolveStateText(IRefreshView refreshView, string stateKey)
    {
        return stateKey switch
        {
            "refreshing" => "Refreshing",
            "disabled" => "Control disabled",
            "refresh-disabled" => "Refresh disabled",
            "armed" => $"Release to refresh ({_pullDistance}px)",
            "pulling" => $"Pulling {_pullDistance}px",
            _ => "Refresh idle"
        };
    }

    private void RegisterChildNode(MiragePlatformView childNode, IMirageRuntimeEventTarget? eventTarget = null)
    {
        if (Coordinator is { } coordinator)
        {
            coordinator.Register(childNode, eventTarget);
            return;
        }

        if (!string.IsNullOrWhiteSpace(childNode.NodeId))
        {
            return;
        }

        var localId = Interlocked.Increment(ref s_internalNodeIdCounter);
        childNode.AssignNodeId($"mirage-refresh-node-{localId:D4}");
    }

    private void UnregisterChildNode(MiragePlatformView? childNode)
    {
        if (childNode is null)
        {
            return;
        }

        Coordinator?.Unregister(childNode);
    }

    private static bool ShouldEnablePull(IRefreshView refreshView)
    {
        return refreshView.IsEnabled && refreshView.IsRefreshEnabled && !refreshView.IsRefreshing;
    }

    private static IView? ResolvePresentedContent(IRefreshView refreshView)
    {
        return refreshView is IContentView contentView ? contentView.PresentedContent : refreshView.Content;
    }

    private static void NormalizeHostedContentFlowStyles(MiragePlatformView contentPlatformView)
    {
        contentPlatformView.SetStyle("position", "relative");
        contentPlatformView.SetStyle("left", null);
        contentPlatformView.SetStyle("top", null);
        contentPlatformView.SetStyle("right", null);
        contentPlatformView.SetStyle("bottom", null);
    }

    private bool SetPropertyIfChanged(string propertyName, string? value)
    {
        return SetPropertyIfChanged(PlatformView, propertyName, value);
    }

    private static bool SetPropertyIfChanged(MiragePlatformView? node, string propertyName, string? value)
    {
        if (node is null)
        {
            return false;
        }

        if (string.Equals(node.GetPropertyString(propertyName), value, StringComparison.Ordinal))
        {
            return false;
        }

        node.SetProperty(propertyName, value);
        return true;
    }

    private static bool SetStyleIfChanged(MiragePlatformView? node, string styleName, string? value)
    {
        if (node is null)
        {
            return false;
        }

        if (string.Equals(node.GetStyleValue(styleName), value, StringComparison.Ordinal))
        {
            return false;
        }

        node.SetStyle(styleName, value);
        return true;
    }

    private static bool SetAttributeIfChanged(MiragePlatformView? node, string attributeName, string? value)
    {
        if (node is null)
        {
            return false;
        }

        node.Attributes.TryGetValue(attributeName, out var currentValue);
        if (string.Equals(currentValue, value, StringComparison.Ordinal))
        {
            return false;
        }

        node.SetAttribute(attributeName, value);
        return true;
    }

    private static Size ResolveMeasuredSizeWithConstraints(
        IRefreshView refreshView,
        double widthConstraint,
        double heightConstraint,
        double measuredWidth,
        double measuredHeight)
    {
        var clampedWidth = ResolveDesiredDimension(widthConstraint, measuredWidth);
        var clampedHeight = ResolveDesiredDimension(heightConstraint, measuredHeight);
        var (requestedWidth, requestedMinimumWidth, requestedMaximumWidth) = ResolveRequestedDimensionRequests(refreshView, isWidth: true);
        var (requestedHeight, requestedMinimumHeight, requestedMaximumHeight) = ResolveRequestedDimensionRequests(refreshView, isWidth: false);
        return new Size(
            ResolveConstraints(clampedWidth, requestedWidth, requestedMinimumWidth, requestedMaximumWidth),
            ResolveConstraints(clampedHeight, requestedHeight, requestedMinimumHeight, requestedMaximumHeight));
    }

    private static double ResolveDesiredDimension(double constraint, double fallback)
    {
        if (double.IsNaN(constraint) || double.IsInfinity(constraint))
        {
            return Math.Max(0, fallback);
        }

        return Math.Max(0, Math.Min(constraint, fallback));
    }

    private static double ResolveConstraints(double measured, double exact, double min, double max)
    {
        var resolved = measured;
        var resolvedMin = ResolveMinimum(min);

        if (IsExplicitLengthSet(exact))
        {
            resolved = exact;
        }

        if (IsExplicitLengthSet(max) && resolved > max)
        {
            resolved = max;
        }

        if (resolved < resolvedMin)
        {
            resolved = resolvedMin;
        }

        return resolved;
    }

    private static double ResolveMinimum(double value)
    {
        return IsExplicitLengthSet(value) ? value : 0;
    }

    private static bool IsExplicitLengthSet(double value)
    {
        return double.IsFinite(value) && value >= 0;
    }

    private static (double Exact, double Min, double Max) ResolveRequestedDimensionRequests(IRefreshView view, bool isWidth)
    {
        if (view is not VisualElement visualElement)
        {
            return (double.NaN, double.NaN, double.NaN);
        }

        var exact = isWidth ? visualElement.WidthRequest : visualElement.HeightRequest;
        var min = isWidth ? visualElement.MinimumWidthRequest : visualElement.MinimumHeightRequest;
        var max = isWidth ? visualElement.MaximumWidthRequest : visualElement.MaximumHeightRequest;
        return (exact, min, max);
    }

    private sealed class RefreshTriggerRuntimeEventTarget : IMirageRuntimeEventTarget
    {
        private readonly MirageRefreshViewHandler _owner;

        public RefreshTriggerRuntimeEventTarget(MirageRefreshViewHandler owner)
        {
            _owner = owner;
        }

        public void OnRuntimeEvent(string eventName, string? payload)
        {
            _ = payload;
            if (!string.Equals(eventName, "click", StringComparison.Ordinal))
            {
                return;
            }

            _owner.OnTriggerClicked();
        }
    }

    private sealed class PullStatePayload
    {
        public bool Active { get; set; }

        public bool Armed { get; set; }

        public int Distance { get; set; }
    }
}
