using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using AndroidSwipeViewConfiguration = Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.SwipeView;
using IOSSwipeViewConfiguration = Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.SwipeView;
using Mirage.Core.Semantics;
using Mirage.Maui.Images;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Mirage.Maui.Handlers;

internal sealed class MirageSwipeViewHandler : MirageViewHandler<ISwipeView>, ISwipeViewHandler, IMirageRuntimeEventTarget
{
    private static readonly TimeSpan RuntimeConfigureRetryDelay = TimeSpan.FromMilliseconds(16);
    private static readonly TimeSpan SlowRuntimeConfigureRetryDelay = TimeSpan.FromMilliseconds(250);
    private const int MaxFastRuntimeConfigureRetryAttempts = 60;
    private const double DefaultWidth = 220;
    private const double DefaultHeight = 64;
    private const double HorizontalActionWidth = 88;
    private const double VerticalActionHeight = 56;
    private const string DefaultActionBackgroundCss = "rgba(30,41,59,0.94)";
    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private static readonly JsonSerializerOptions RuntimeJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static long s_internalNodeIdCounter;

    internal static readonly string[] ParityPropertyKeys =
    [
        nameof(ISwipeView.Content),
        nameof(ISwipeView.LeftItems),
        nameof(ISwipeView.RightItems),
        nameof(ISwipeView.TopItems),
        nameof(ISwipeView.BottomItems),
        nameof(ISwipeView.SwipeTransitionMode)
    ];

    internal static readonly string[] ParityCommandKeys =
    [
        nameof(ISwipeView.RequestOpen),
        nameof(ISwipeView.RequestClose)
    ];

    internal static readonly string[] ParityExtensionPropertyKeys =
    [
        nameof(IView.IsEnabled)
    ];

    public static IPropertyMapper<ISwipeView, MirageSwipeViewHandler> Mapper =
        new PropertyMapper<ISwipeView, MirageSwipeViewHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(ISwipeView.Content)] = MapContent,
            [nameof(ISwipeView.LeftItems)] = MapLeftItems,
            [nameof(ISwipeView.RightItems)] = MapRightItems,
            [nameof(ISwipeView.TopItems)] = MapTopItems,
            [nameof(ISwipeView.BottomItems)] = MapBottomItems,
            [nameof(ISwipeView.SwipeTransitionMode)] = MapSwipeTransitionMode,
            [nameof(IView.IsEnabled)] = MapIsEnabled,
            ["IsEnabled"] = MapIsEnabled
        };

    public static CommandMapper<ISwipeView, MirageSwipeViewHandler> CommandMapper =
        new(MirageViewMapper.CommonViewCommandMapper)
        {
            [nameof(ISwipeView.RequestOpen)] = MapRequestOpen,
            [nameof(ISwipeView.RequestClose)] = MapRequestClose
        };

    private MiragePlatformView? _clipHost;
    private MiragePlatformView? _trackHost;
    private MiragePlatformView? _contentHost;
    private MiragePlatformView? _leftActionsHost;
    private MiragePlatformView? _rightActionsHost;
    private MiragePlatformView? _topActionsHost;
    private MiragePlatformView? _bottomActionsHost;

    private IView? _currentContent;
    private IElementHandler? _currentContentHandler;
    private MiragePlatformView? _currentContentPlatformView;

    private readonly List<SwipeActionRealization> _leftRealizations = [];
    private readonly List<SwipeActionRealization> _rightRealizations = [];
    private readonly List<SwipeActionRealization> _topRealizations = [];
    private readonly List<SwipeActionRealization> _bottomRealizations = [];
    private readonly Dictionary<INotifyPropertyChanged, int> _subscribedItemNotifiers = new();

    private SwipeItems? _leftItemsSource;
    private SwipeItems? _rightItemsSource;
    private SwipeItems? _topItemsSource;
    private SwipeItems? _bottomItemsSource;

    private NotifyCollectionChangedEventHandler? _swipeItemsCollectionChangedHandler;
    private PropertyChangedEventHandler? _swipeItemsPropertyChangedHandler;
    private PropertyChangedEventHandler? _swipeItemPropertyChangedHandler;
    private PropertyChangedEventHandler? _swipeViewPropertyChangedHandler;
    private EventHandler<OpenRequestedEventArgs>? _openRequestedHandler;
    private EventHandler<CloseRequestedEventArgs>? _closeRequestedHandler;

    // The handler owns MAUI-facing open state and pending programmatic commands.
    // Gesture progress, offsets, and DOM visibility are owned by the browser runtime.
    private SwipeSide _openSide;
    private Rect _lastArrangedFrame;
    private IMirageImageSourceResolver? _imageSourceResolver;
    private bool _isConnected;
    private bool _runtimeConfigureRetryQueued;
    private int _consecutiveRuntimeConfigureFailures;
    private PendingRuntimeCommand _pendingRuntimeCommand = PendingRuntimeCommand.None;

    public MirageSwipeViewHandler()
        : base(ComponentKind.SwipeView, Mapper, CommandMapper)
    {
        DefaultDisplayStyle = "block";
    }

    ISwipeView ISwipeViewHandler.VirtualView => VirtualView;

    object ISwipeViewHandler.PlatformView => PlatformView;

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        var content = ResolvePresentedContent(VirtualView);
        if (content is null)
        {
            return ResolveMeasuredSizeWithConstraints(VirtualView, widthConstraint, heightConstraint, DefaultWidth, DefaultHeight);
        }

        var contentSize = content.Measure(widthConstraint, heightConstraint);
        return ResolveMeasuredSizeWithConstraints(
            VirtualView,
            widthConstraint,
            heightConstraint,
            contentSize.Width > 0 ? contentSize.Width : DefaultWidth,
            contentSize.Height > 0 ? contentSize.Height : DefaultHeight);
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        _isConnected = true;
        EnsureStructure();
        ApplyRootStyles();
        ApplyDiagnostics();
        SubscribeToSwipeViewEvents();
        BindSideCollections();

        base.ConnectHandler(platformView);

        SetContent(ResolvePresentedContent(VirtualView));
        RebuildAllActionHosts();
        ConfigureRuntime();
        ArrangeUsingLastFrame();
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        _isConnected = false;
        ResetRuntimeConfigureRetryState();
        ClearPendingRuntimeCommand();
        DisableRuntimeSwipe();
        UnsubscribeFromSwipeViewEvents();
        UnbindSideCollections();
        DisconnectCurrentContent();
        DisconnectAllActions();
        DisconnectStructure(platformView);
        _lastArrangedFrame = default;
        _openSide = SwipeSide.None;
        base.DisconnectHandler(platformView);
    }

    public override void PlatformArrange(Rect frame)
    {
        _lastArrangedFrame = frame;
        base.PlatformArrange(frame);

        var width = Math.Max(0, frame.Width);
        var height = Math.Max(0, frame.Height);

        if (_currentContent is IView content)
        {
            NormalizeHostedContentFlowStyles(_currentContentPlatformView);
            content.Arrange(new Rect(0, 0, width, height));
        }

        ArrangeSwipeItemViews(_leftRealizations, width: HorizontalActionWidth, height);
        ArrangeSwipeItemViews(_rightRealizations, width: HorizontalActionWidth, height);
        ArrangeSwipeItemViews(_topRealizations, width, height: VerticalActionHeight);
        ArrangeSwipeItemViews(_bottomRealizations, width, height: VerticalActionHeight);
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        switch (eventName)
        {
            case "swipestart":
                HandleSwipeStart(payload);
                break;
            case "swipechange":
                HandleSwipeChange(payload);
                break;
            case "swipeend":
                HandleSwipeEnd(payload);
                break;
            case "swipeinvoke":
                HandleSwipeInvoke(payload);
                break;
        }
    }

    public static void MapContent(MirageSwipeViewHandler handler, ISwipeView swipeView)
    {
        handler.SetContent(ResolvePresentedContent(swipeView));
        handler.ArrangeUsingLastFrame();
    }

    public static void MapLeftItems(MirageSwipeViewHandler handler, ISwipeView swipeView)
    {
        handler.BindSideCollection(SwipeSide.Left, ResolveSwipeItems(swipeView, SwipeSide.Left));
        handler.RebuildSideHost(SwipeSide.Left);
    }

    public static void MapRightItems(MirageSwipeViewHandler handler, ISwipeView swipeView)
    {
        handler.BindSideCollection(SwipeSide.Right, ResolveSwipeItems(swipeView, SwipeSide.Right));
        handler.RebuildSideHost(SwipeSide.Right);
    }

    public static void MapTopItems(MirageSwipeViewHandler handler, ISwipeView swipeView)
    {
        handler.BindSideCollection(SwipeSide.Top, ResolveSwipeItems(swipeView, SwipeSide.Top));
        handler.RebuildSideHost(SwipeSide.Top);
    }

    public static void MapBottomItems(MirageSwipeViewHandler handler, ISwipeView swipeView)
    {
        handler.BindSideCollection(SwipeSide.Bottom, ResolveSwipeItems(swipeView, SwipeSide.Bottom));
        handler.RebuildSideHost(SwipeSide.Bottom);
    }

    public static void MapSwipeTransitionMode(MirageSwipeViewHandler handler, ISwipeView swipeView)
    {
        if (!handler.ApplyDiagnostics())
        {
            handler.ConfigureRuntime();
            return;
        }

        handler.ConfigureRuntime();
        handler.RequestRender();
    }

    public static void MapIsEnabled(MirageSwipeViewHandler handler, ISwipeView swipeView)
    {
        handler.ResetOpenStateIfDisabled();

        if (!handler.ApplyDiagnostics())
        {
            handler.ConfigureRuntime();
        }
        else
        {
            handler.ConfigureRuntime();
            handler.RequestRender();
        }
    }

    public static void MapRequestOpen(MirageSwipeViewHandler handler, ISwipeView swipeView, object? args)
    {
        if (args is not SwipeViewOpenRequest request)
        {
            return;
        }

        handler.TryRequestOpen(MapOpenSwipeItemToSide(request.OpenSwipeItem), request.Animated);
    }

    public static void MapRequestClose(MirageSwipeViewHandler handler, ISwipeView swipeView, object? args)
    {
        if (args is not SwipeViewCloseRequest request)
        {
            return;
        }

        handler.TryRequestClose(request.Animated);
    }

    private bool EnsureStructure()
    {
        var changed = false;

        if (_leftActionsHost is null)
        {
            _leftActionsHost = CreateRegisteredWebNode("div", "data-maui-sv-action-host", "left");
            changed = true;
        }

        if (_rightActionsHost is null)
        {
            _rightActionsHost = CreateRegisteredWebNode("div", "data-maui-sv-action-host", "right");
            changed = true;
        }

        if (_topActionsHost is null)
        {
            _topActionsHost = CreateRegisteredWebNode("div", "data-maui-sv-action-host", "top");
            changed = true;
        }

        if (_bottomActionsHost is null)
        {
            _bottomActionsHost = CreateRegisteredWebNode("div", "data-maui-sv-action-host", "bottom");
            changed = true;
        }

        if (_clipHost is null)
        {
            _clipHost = CreateRegisteredWebNode("div", "data-maui-sv-clip", "true");
            changed = true;
        }

        if (_trackHost is null)
        {
            _trackHost = CreateRegisteredWebNode("div", "data-maui-sv-track", "true");
            changed = true;
        }

        if (_contentHost is null)
        {
            _contentHost = CreateRegisteredWebNode("div", "data-maui-sv-content-host", "true");
            changed = true;
        }

        if (_trackHost is not null
            && _contentHost is not null
            && (_trackHost.Children.Count != 1 || !ReferenceEquals(_trackHost.Children[0], _contentHost)))
        {
            _trackHost.ClearChildren();
            _trackHost.AddChild(_contentHost);
            changed = true;
        }

        if (_clipHost is not null
            && _trackHost is not null
            && (_clipHost.Children.Count != 1 || !ReferenceEquals(_clipHost.Children[0], _trackHost)))
        {
            _clipHost.ClearChildren();
            _clipHost.AddChild(_trackHost);
            changed = true;
        }

        if (_leftActionsHost is not null
            && _rightActionsHost is not null
            && _topActionsHost is not null
            && _bottomActionsHost is not null
            && _clipHost is not null
            && (PlatformView.Children.Count != 5
                || !ReferenceEquals(PlatformView.Children[0], _leftActionsHost)
                || !ReferenceEquals(PlatformView.Children[1], _rightActionsHost)
                || !ReferenceEquals(PlatformView.Children[2], _topActionsHost)
                || !ReferenceEquals(PlatformView.Children[3], _bottomActionsHost)
                || !ReferenceEquals(PlatformView.Children[4], _clipHost)))
        {
            PlatformView.ClearChildren();
            PlatformView.AddChild(_leftActionsHost);
            PlatformView.AddChild(_rightActionsHost);
            PlatformView.AddChild(_topActionsHost);
            PlatformView.AddChild(_bottomActionsHost);
            PlatformView.AddChild(_clipHost);
            changed = true;
        }

        return changed;
    }

    private bool ApplyRootStyles()
    {
        using var _ = BeginRenderBatch();

        var changed = SetStyleIfChanged(PlatformView, "display", DefaultDisplayStyle);
        changed |= SetStyleIfChanged(PlatformView, "position", "relative");
        changed |= SetStyleIfChanged(PlatformView, "overflow", "hidden");
        changed |= SetStyleIfChanged(PlatformView, "boxSizing", "border-box");
        changed |= SetStyleIfChanged(PlatformView, "touchAction", "auto");

        changed |= ApplyActionHostStyles(_leftActionsHost, SwipeSide.Left);
        changed |= ApplyActionHostStyles(_rightActionsHost, SwipeSide.Right);
        changed |= ApplyActionHostStyles(_topActionsHost, SwipeSide.Top);
        changed |= ApplyActionHostStyles(_bottomActionsHost, SwipeSide.Bottom);

        changed |= SetStyleIfChanged(_clipHost, "position", "absolute");
        changed |= SetStyleIfChanged(_clipHost, "left", "0");
        changed |= SetStyleIfChanged(_clipHost, "top", "0");
        changed |= SetStyleIfChanged(_clipHost, "right", "0");
        changed |= SetStyleIfChanged(_clipHost, "bottom", "0");
        changed |= SetStyleIfChanged(_clipHost, "overflow", "hidden");

        changed |= SetStyleIfChanged(_trackHost, "position", "absolute");
        changed |= SetStyleIfChanged(_trackHost, "left", "0");
        changed |= SetStyleIfChanged(_trackHost, "top", "0");
        changed |= SetStyleIfChanged(_trackHost, "right", "0");
        changed |= SetStyleIfChanged(_trackHost, "bottom", "0");
        changed |= SetStyleIfChanged(_trackHost, "zIndex", "2");
        changed |= SetStyleIfChanged(_trackHost, "transform", "translate3d(0, 0, 0)");

        changed |= SetStyleIfChanged(_contentHost, "position", "absolute");
        changed |= SetStyleIfChanged(_contentHost, "left", "0");
        changed |= SetStyleIfChanged(_contentHost, "top", "0");
        changed |= SetStyleIfChanged(_contentHost, "right", "0");
        changed |= SetStyleIfChanged(_contentHost, "bottom", "0");
        changed |= SetStyleIfChanged(_contentHost, "backgroundColor", "transparent");
        return changed;
    }

    private bool ApplyActionHostStyles(MiragePlatformView? host, SwipeSide side)
    {
        if (host is null)
        {
            return false;
        }

        var isHorizontal = side is SwipeSide.Left or SwipeSide.Right;
        var changed = SetStyleIfChanged(host, "position", "absolute");
        changed |= SetStyleIfChanged(host, "display", "flex");
        changed |= SetStyleIfChanged(host, "overflow", "hidden");
        changed |= SetStyleIfChanged(host, "pointerEvents", "none");
        changed |= SetStyleIfChanged(host, "visibility", "hidden");
        changed |= SetStyleIfChanged(host, "backgroundColor", DefaultActionBackgroundCss);
        changed |= SetStyleIfChanged(host, "boxSizing", "border-box");
        changed |= SetStyleIfChanged(host, "zIndex", "1");

        if (isHorizontal)
        {
            changed |= SetStyleIfChanged(host, "top", "0");
            changed |= SetStyleIfChanged(host, "bottom", "0");
            changed |= SetStyleIfChanged(host, "width", "0px");
            changed |= SetStyleIfChanged(host, "flexDirection", "row");
            changed |= SetStyleIfChanged(host, "alignItems", "stretch");
            changed |= SetStyleIfChanged(host, "justifyContent", "stretch");
            changed |= SetStyleIfChanged(host, "left", side == SwipeSide.Left ? "0" : null);
            changed |= SetStyleIfChanged(host, "right", side == SwipeSide.Right ? "0" : null);
        }
        else
        {
            changed |= SetStyleIfChanged(host, "left", "0");
            changed |= SetStyleIfChanged(host, "right", "0");
            changed |= SetStyleIfChanged(host, "height", "0px");
            changed |= SetStyleIfChanged(host, "flexDirection", "column");
            changed |= SetStyleIfChanged(host, "alignItems", "stretch");
            changed |= SetStyleIfChanged(host, "justifyContent", "stretch");
            changed |= SetStyleIfChanged(host, "top", side == SwipeSide.Top ? "0" : null);
            changed |= SetStyleIfChanged(host, "bottom", side == SwipeSide.Bottom ? "0" : null);
        }

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

        _contentHost?.ClearChildren();
        _currentContent = null;
        _currentContentHandler = null;
        _currentContentPlatformView = null;

        if (content is null || MauiContext is null || _contentHost is null)
        {
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
                $"Failed to create handler for SwipeView content type '{content.GetType().FullName}'.",
                ex);
        }

        if (contentHandler.PlatformView is not MiragePlatformView contentPlatformView)
        {
            MirageHandlerDisconnect.SafeDisconnect(contentHandler);
            throw new InvalidOperationException($"{GetType().Name} requires SwipeView content handlers to expose MiragePlatformView.");
        }

        NormalizeHostedContentFlowStyles(contentPlatformView);
        _contentHost.AddChild(contentPlatformView);
        if (contentHandler is IViewHandler viewHandler)
        {
            MirageViewMapper.MapLayoutAlignment(viewHandler, content);
        }

        _currentContent = content;
        _currentContentHandler = contentHandler;
        _currentContentPlatformView = contentPlatformView;
        RequestRender();
    }

    private void DisconnectCurrentContent()
    {
        MirageHandlerDisconnect.SafeDisconnect(_currentContentHandler);
        _currentContentHandler = null;
        _currentContent = null;
        _currentContentPlatformView = null;
        _contentHost?.ClearChildren();
        RequestRender();
    }

    private void BindSideCollections()
    {
        BindSideCollection(SwipeSide.Left, ResolveSwipeItems(VirtualView, SwipeSide.Left));
        BindSideCollection(SwipeSide.Right, ResolveSwipeItems(VirtualView, SwipeSide.Right));
        BindSideCollection(SwipeSide.Top, ResolveSwipeItems(VirtualView, SwipeSide.Top));
        BindSideCollection(SwipeSide.Bottom, ResolveSwipeItems(VirtualView, SwipeSide.Bottom));
    }

    private void UnbindSideCollections()
    {
        BindSideCollection(SwipeSide.Left, null);
        BindSideCollection(SwipeSide.Right, null);
        BindSideCollection(SwipeSide.Top, null);
        BindSideCollection(SwipeSide.Bottom, null);
        UnsubscribeFromAllActionItemNotifications();
    }

    private void BindSideCollection(SwipeSide side, SwipeItems? items)
    {
        _swipeItemsCollectionChangedHandler ??= OnSwipeItemsCollectionChanged;
        _swipeItemsPropertyChangedHandler ??= OnSwipeItemsPropertyChanged;

        var current = GetItemsSource(side);
        if (ReferenceEquals(current, items))
        {
            return;
        }

        if (current is not null)
        {
            if (current is INotifyCollectionChanged notifyCollectionChanged)
            {
                notifyCollectionChanged.CollectionChanged -= _swipeItemsCollectionChangedHandler;
            }

            if (current is INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged -= _swipeItemsPropertyChangedHandler;
            }
        }

        SetItemsSource(side, items);

        if (items is not null)
        {
            if (items is INotifyCollectionChanged notifyCollectionChanged)
            {
                notifyCollectionChanged.CollectionChanged += _swipeItemsCollectionChangedHandler;
            }

            if (items is INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged += _swipeItemsPropertyChangedHandler;
            }
        }
    }

    private void SubscribeToSwipeViewEvents()
    {
        if (VirtualView is not SwipeView swipeView)
        {
            return;
        }

        _openRequestedHandler ??= OnOpenRequested;
        _closeRequestedHandler ??= OnCloseRequested;
        _swipeViewPropertyChangedHandler ??= OnSwipeViewPropertyChanged;

        swipeView.OpenRequested += _openRequestedHandler;
        swipeView.CloseRequested += _closeRequestedHandler;
        swipeView.PropertyChanged += _swipeViewPropertyChangedHandler;
    }

    private void UnsubscribeFromSwipeViewEvents()
    {
        if (VirtualView is not SwipeView swipeView)
        {
            return;
        }

        if (_openRequestedHandler is not null)
        {
            swipeView.OpenRequested -= _openRequestedHandler;
        }

        if (_closeRequestedHandler is not null)
        {
            swipeView.CloseRequested -= _closeRequestedHandler;
        }

        if (_swipeViewPropertyChangedHandler is not null)
        {
            swipeView.PropertyChanged -= _swipeViewPropertyChangedHandler;
        }
    }

    private void OnSwipeItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _ = e;
        RebuildSideHost(ResolveSideForItemsSource(sender));
    }

    private void OnSwipeItemsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.PropertyName)
            && !string.Equals(e.PropertyName, nameof(SwipeItems.Mode), StringComparison.Ordinal)
            && !string.Equals(e.PropertyName, nameof(SwipeItems.SwipeBehaviorOnInvoked), StringComparison.Ordinal))
        {
            return;
        }

        RebuildSideHost(ResolveSideForItemsSource(sender));
    }

    private void OnSwipeItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        RebuildAllActionHosts();
    }

    private void OnSwipeViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = sender;
        if (!string.Equals(e.PropertyName, nameof(SwipeView.Threshold), StringComparison.Ordinal))
        {
            return;
        }

        var changed = ApplyDiagnostics();
        ConfigureRuntime();
        if (changed)
        {
            RequestRender();
        }
    }

    private void OnOpenRequested(object? sender, OpenRequestedEventArgs e)
    {
        _ = sender;
        TryRequestOpen(MapOpenSwipeItemToSide(e.OpenSwipeItem), e.Animated);
    }

    private void OnCloseRequested(object? sender, CloseRequestedEventArgs e)
    {
        _ = sender;
        TryRequestClose(e.Animated);
    }

    private void RebuildAllActionHosts()
    {
        UnsubscribeFromAllActionItemNotifications();
        RebuildSideRealizations(SwipeSide.Left, _leftActionsHost, _leftRealizations, _leftItemsSource);
        RebuildSideRealizations(SwipeSide.Right, _rightActionsHost, _rightRealizations, _rightItemsSource);
        RebuildSideRealizations(SwipeSide.Top, _topActionsHost, _topRealizations, _topItemsSource);
        RebuildSideRealizations(SwipeSide.Bottom, _bottomActionsHost, _bottomRealizations, _bottomItemsSource);
        SubscribeToAllActionItemNotifications();
        ApplyDiagnostics();
        ConfigureRuntime();
        ArrangeUsingLastFrame();
        RequestRender();
    }

    private void RebuildSideHost(SwipeSide side)
    {
        UnsubscribeFromAllActionItemNotifications();
        RebuildSideRealizations(side, ResolveActionHost(side), ResolveRealizations(side), GetItemsSource(side));
        SubscribeToAllActionItemNotifications();
        ApplyDiagnostics();
        ConfigureRuntime();
        ArrangeUsingLastFrame();
        RequestRender();
    }

    private void RebuildSideRealizations(
        SwipeSide side,
        MiragePlatformView? host,
        List<SwipeActionRealization> target,
        SwipeItems? items)
    {
        DisconnectRealizations(target, host);
        if (host is null)
        {
            return;
        }

        if (items is null || MauiContext is null)
        {
            ApplySideHostExtent(side, host, 0);
            return;
        }

        var isHorizontal = side is SwipeSide.Left or SwipeSide.Right;
        var itemIndex = 0;
        foreach (var rawItem in items)
        {
            if (rawItem is SwipeItem swipeItem)
            {
                var menuHandler = new MirageSwipeItemMenuItemHandler(swipeItem, ToSideToken(side), itemIndex);
                foreach (var node in menuHandler.AllNodes)
                {
                    RegisterChildNode(node);
                }

                menuHandler.Update(GetImageSourceResolver(), isHorizontal);
                host.AddChild(menuHandler.RootNode);
                target.Add(SwipeActionRealization.CreateMenu(side, swipeItem, menuHandler));
                itemIndex++;
                continue;
            }

            if (rawItem is SwipeItemView swipeItemView)
            {
                var wrapper = CreateRegisteredWebNode("div", "data-maui-sv-action-wrapper", "true");
                wrapper.SetAttribute("data-maui-sv-action-kind", "view");
                wrapper.SetAttribute("data-maui-sv-action-side", ToSideToken(side));
                wrapper.SetAttribute("data-maui-sv-action-index", itemIndex.ToString(CultureInfo.InvariantCulture));
                wrapper.SetAttribute("role", "button");
                wrapper.SetAttribute("tabindex", "-1");

                var presentedContent = ResolvePresentedContent(swipeItemView);
                if (presentedContent is null)
                {
                    RegisterChildNode(wrapper);
                    host.AddChild(wrapper);
                    target.Add(SwipeActionRealization.CreateView(side, swipeItemView, wrapper, null, null));
                    itemIndex++;
                    continue;
                }

                if (!TryGetChildStable(presentedContent, out var childHandler, out var childPlatformView))
                {
                    throw new InvalidOperationException(
                        $"{GetType().Name} requires SwipeItemView presented content handlers to expose MiragePlatformView.");
                }

                wrapper.AddChild(childPlatformView);
                NormalizeHostedContentFlowStyles(childPlatformView);
                childPlatformView.SetStyle("width", "100%");
                childPlatformView.SetStyle("height", "100%");
                RegisterChildNode(wrapper);
                host.AddChild(wrapper);
                target.Add(SwipeActionRealization.CreateView(side, swipeItemView, wrapper, childHandler, childPlatformView));
                itemIndex++;
            }
        }

        foreach (var realization in target)
        {
            ApplyActionRealizationProjection(realization, isHorizontal);
        }

        var extent = ResolveExtentForSide(side, target);
        ApplySideHostExtent(side, host, extent);
    }

    private void ApplyActionRealizationProjection(SwipeActionRealization realization, bool isHorizontal)
    {
        var visible = realization.IsVisible;
        var enabled = realization.IsEnabled;
        SetAttributeIfChanged(realization.WrapperNode, "data-maui-sv-action-visible", visible ? "true" : "false");
        SetAttributeIfChanged(realization.WrapperNode, "data-maui-sv-action-enabled", enabled ? "true" : "false");
        SetStyleIfChanged(realization.WrapperNode, "display", visible ? "flex" : "none");
        SetStyleIfChanged(realization.WrapperNode, "alignItems", "stretch");
        SetStyleIfChanged(realization.WrapperNode, "justifyContent", "stretch");
        SetStyleIfChanged(realization.WrapperNode, "boxSizing", "border-box");
        SetStyleIfChanged(realization.WrapperNode, "overflow", "hidden");
        SetStyleIfChanged(realization.WrapperNode, "opacity", enabled ? "1" : "0.55");
        SetStyleIfChanged(realization.WrapperNode, "cursor", enabled ? "pointer" : "default");
        if (isHorizontal)
        {
            SetStyleIfChanged(realization.WrapperNode, "width", $"{HorizontalActionWidth:0.###}px");
            SetStyleIfChanged(realization.WrapperNode, "height", "100%");
            SetStyleIfChanged(realization.WrapperNode, "flex", $"0 0 {HorizontalActionWidth:0.###}px");
        }
        else
        {
            SetStyleIfChanged(realization.WrapperNode, "width", "100%");
            SetStyleIfChanged(realization.WrapperNode, "height", $"{VerticalActionHeight:0.###}px");
            SetStyleIfChanged(realization.WrapperNode, "flex", $"0 0 {VerticalActionHeight:0.###}px");
        }

        if (realization.MenuItemHandler is not null)
        {
            realization.MenuItemHandler.Update(GetImageSourceResolver(), isHorizontal);
            return;
        }

        if (realization.SwipeItemView is null)
        {
            return;
        }

        SetStyleIfChanged(realization.WrapperNode, "backgroundColor", realization.SwipeItemView.Background is SolidColorBrush solidColorBrush && solidColorBrush.Color is not null
            ? MirageCommonViewProjection.ToCssColor(solidColorBrush.Color)
            : DefaultActionBackgroundCss);
    }

    private void ArrangeSwipeItemViews(IEnumerable<SwipeActionRealization> realizations, double width, double height)
    {
        foreach (var realization in realizations)
        {
            if (realization.ChildHandler?.VirtualView is not IView childView)
            {
                continue;
            }

            childView.Arrange(new Rect(0, 0, Math.Max(0, width), Math.Max(0, height)));
        }
    }

    private void DisconnectAllActions()
    {
        DisconnectRealizations(_leftRealizations, _leftActionsHost);
        DisconnectRealizations(_rightRealizations, _rightActionsHost);
        DisconnectRealizations(_topRealizations, _topActionsHost);
        DisconnectRealizations(_bottomRealizations, _bottomActionsHost);
        UnsubscribeFromAllActionItemNotifications();
    }

    private void DisconnectRealizations(List<SwipeActionRealization> realizations, MiragePlatformView? host)
    {
        foreach (var realization in realizations)
        {
            if (realization.MenuItemHandler is not null)
            {
                foreach (var node in realization.MenuItemHandler.AllNodes)
                {
                    UnregisterChildNode(node);
                }
            }

            if (realization.ChildHandler is not null)
            {
                MirageHandlerDisconnect.SafeDisconnect(realization.ChildHandler);
            }

            if (realization.ChildPlatformView is not null)
            {
                realization.ChildPlatformView.SetStyle("pointerEvents", null);
            }

            UnregisterChildNode(realization.WrapperNode);
        }

        realizations.Clear();
        host?.ClearChildren();
    }

    private void SubscribeToAllActionItemNotifications()
    {
        _swipeItemPropertyChangedHandler ??= OnSwipeItemPropertyChanged;
        SubscribeRealizations(_leftRealizations);
        SubscribeRealizations(_rightRealizations);
        SubscribeRealizations(_topRealizations);
        SubscribeRealizations(_bottomRealizations);
    }

    private void SubscribeRealizations(IEnumerable<SwipeActionRealization> realizations)
    {
        foreach (var realization in realizations)
        {
            if (realization.Item is not INotifyPropertyChanged notifyPropertyChanged)
            {
                continue;
            }

            if (_subscribedItemNotifiers.TryGetValue(notifyPropertyChanged, out var existingCount))
            {
                _subscribedItemNotifiers[notifyPropertyChanged] = existingCount + 1;
                continue;
            }

            notifyPropertyChanged.PropertyChanged += _swipeItemPropertyChangedHandler;
            _subscribedItemNotifiers[notifyPropertyChanged] = 1;
        }
    }

    private void UnsubscribeFromAllActionItemNotifications()
    {
        if (_swipeItemPropertyChangedHandler is null)
        {
            _subscribedItemNotifiers.Clear();
            return;
        }

        foreach (var pair in _subscribedItemNotifiers)
        {
            pair.Key.PropertyChanged -= _swipeItemPropertyChangedHandler;
        }

        _subscribedItemNotifiers.Clear();
    }

    private void ApplySideHostExtent(SwipeSide side, MiragePlatformView host, double extent)
    {
        if (side is SwipeSide.Left or SwipeSide.Right)
        {
            SetStyleIfChanged(host, "width", $"{Math.Max(0, extent):0.###}px");
        }
        else
        {
            SetStyleIfChanged(host, "height", $"{Math.Max(0, extent):0.###}px");
        }
    }

    private double ResolveExtentForSide(SwipeSide side, List<SwipeActionRealization> realizations)
    {
        var visibleCount = realizations.Count(static realization => realization.IsVisible);
        if (visibleCount <= 0)
        {
            return 0;
        }

        return side is SwipeSide.Left or SwipeSide.Right
            ? visibleCount * HorizontalActionWidth
            : visibleCount * VerticalActionHeight;
    }

    private bool ApplyDiagnostics()
    {
        using var _ = BeginRenderBatch();

        var openSideToken = ToSideToken(_openSide);
        var changed = SetPropertyIfChanged("OpenState", _openSide == SwipeSide.None ? "false" : "true");
        changed |= SetPropertyIfChanged("OpenSideState", openSideToken);
        changed |= SetPropertyIfChanged("EnabledState", VirtualView.IsEnabled ? "true" : "false");
        changed |= SetPropertyIfChanged("TransitionModeState", ToTransitionModeToken(ResolveSwipeTransitionMode(VirtualView)));
        changed |= SetPropertyIfChanged("ThresholdState", VirtualView.Threshold.ToString(CultureInfo.InvariantCulture));
        return changed;
    }

    private void ConfigureRuntime()
    {
        if (!_isConnected || Coordinator is null || string.IsNullOrWhiteSpace(PlatformView.NodeId))
        {
            return;
        }

        var configuration = new SwipeRuntimeConfiguration(
            Enabled: VirtualView.IsEnabled,
            Threshold: VirtualView.Threshold,
            TransitionMode: ToTransitionModeToken(ResolveSwipeTransitionMode(VirtualView)),
            Left: BuildSideConfiguration(SwipeSide.Left, _leftItemsSource, _leftRealizations),
            Right: BuildSideConfiguration(SwipeSide.Right, _rightItemsSource, _rightRealizations),
            Top: BuildSideConfiguration(SwipeSide.Top, _topItemsSource, _topRealizations),
            Bottom: BuildSideConfiguration(SwipeSide.Bottom, _bottomItemsSource, _bottomRealizations));
        var configurationJson = JsonSerializer.Serialize(configuration, RuntimeJsonOptions);
        var configured = Coordinator.TryConfigureNodeSwipeView(PlatformView.NodeId, enabled: true, configurationJson);
        if (!configured)
        {
            ScheduleRuntimeConfigurationRetry();
            return;
        }

        ResetRuntimeConfigureRetryState();

        if (ReplayPendingRuntimeCommand())
        {
            return;
        }

        ReplayManagedOpenStateAfterConfiguration();
    }

    private void DisableRuntimeSwipe()
    {
        if (Coordinator is null || string.IsNullOrWhiteSpace(PlatformView.NodeId))
        {
            return;
        }

        _ = Coordinator.TryConfigureNodeSwipeView(PlatformView.NodeId, enabled: false, "{}");
    }

    private bool TryRequestOpen(SwipeSide side, bool animated)
    {
        if (side == SwipeSide.None || !VirtualView.IsEnabled || Coordinator is null)
        {
            return false;
        }

        var accepted = TryRequestRuntimeOpen(side, animated);
        if (!accepted)
        {
            SetPendingRuntimeCommand(PendingRuntimeCommand.Open(side, animated));
            ConfigureRuntime();
            return false;
        }

        ClearPendingRuntimeCommand();
        return true;
    }

    private bool TryRequestClose(bool animated)
    {
        if (Coordinator is null)
        {
            return false;
        }

        var accepted = TryRequestRuntimeClose(animated);
        if (!accepted)
        {
            SetPendingRuntimeCommand(PendingRuntimeCommand.Close(animated));
            ConfigureRuntime();
            return false;
        }

        ClearPendingRuntimeCommand();
        return true;
    }

    private bool ReplayPendingRuntimeCommand()
    {
        if (Coordinator is null)
        {
            return false;
        }

        if (_pendingRuntimeCommand.Kind == PendingRuntimeCommandKind.None)
        {
            return false;
        }

        if (_pendingRuntimeCommand.Kind == PendingRuntimeCommandKind.Open && !VirtualView.IsEnabled)
        {
            ClearPendingRuntimeCommand();
            SynchronizeManagedClosedState();
            return true;
        }

        if (_pendingRuntimeCommand.Kind == PendingRuntimeCommandKind.Open
            && _pendingRuntimeCommand.Side != SwipeSide.None
            && !CanRuntimeOpenSide(_pendingRuntimeCommand.Side))
        {
            ClearPendingRuntimeCommand();
            SynchronizeManagedClosedState();
            return true;
        }

        var accepted = _pendingRuntimeCommand.Kind switch
        {
            PendingRuntimeCommandKind.Open when _pendingRuntimeCommand.Side != SwipeSide.None
                => TryRequestRuntimeOpen(_pendingRuntimeCommand.Side, _pendingRuntimeCommand.Animated),
            PendingRuntimeCommandKind.Close
                => TryRequestRuntimeClose(_pendingRuntimeCommand.Animated),
            _ => true
        };

        if (accepted)
        {
            ClearPendingRuntimeCommand();
            return true;
        }

        ScheduleRuntimeConfigurationRetry();
        return true;
    }

    private bool CanRuntimeOpenSide(SwipeSide side)
    {
        if (side == SwipeSide.None)
        {
            return false;
        }

        return ResolveExtentForSide(side, ResolveRealizations(side)) > 0;
    }

    private void ResetOpenStateIfDisabled()
    {
        if (VirtualView.IsEnabled)
        {
            return;
        }

        ClearPendingRuntimeCommand();
        SynchronizeManagedClosedState();
    }

    private void SynchronizeManagedOpenState(SwipeSide nextOpenSide, bool isOpen)
    {
        var normalizedOpenSide = isOpen ? nextOpenSide : SwipeSide.None;
        var changed = false;
        if (_openSide != normalizedOpenSide)
        {
            _openSide = normalizedOpenSide;
            changed = true;
        }

        if (VirtualView.IsOpen != isOpen)
        {
            VirtualView.IsOpen = isOpen;
            changed = true;
        }

        if (ApplyDiagnostics() || changed)
        {
            RequestRender();
        }
    }

    private void SynchronizeManagedClosedState()
    {
        SynchronizeManagedOpenState(SwipeSide.None, isOpen: false);
    }

    private void ResetRuntimeConfigureRetryState()
    {
        _runtimeConfigureRetryQueued = false;
        _consecutiveRuntimeConfigureFailures = 0;
    }

    private void SetPendingRuntimeCommand(PendingRuntimeCommand command)
    {
        _pendingRuntimeCommand = command;
    }

    private void ClearPendingRuntimeCommand()
    {
        _pendingRuntimeCommand = PendingRuntimeCommand.None;
    }

    private bool TryRequestRuntimeOpen(SwipeSide side, bool animated)
    {
        return Coordinator is not null
            && side != SwipeSide.None
            && Coordinator.TryRequestNodeSwipeViewOpen(PlatformView.NodeId, ToSideToken(side), animated);
    }

    private bool TryRequestRuntimeClose(bool animated)
    {
        return Coordinator is not null
            && Coordinator.TryRequestNodeSwipeViewClose(PlatformView.NodeId, animated);
    }

    private void ReplayManagedOpenStateAfterConfiguration()
    {
        if (!VirtualView.IsEnabled || _openSide == SwipeSide.None)
        {
            return;
        }

        if (!CanRuntimeOpenSide(_openSide) || !TryRequestRuntimeOpen(_openSide, animated: false))
        {
            SynchronizeManagedClosedState();
        }
    }

    private void ScheduleRuntimeConfigurationRetry()
    {
        if (_runtimeConfigureRetryQueued || !_isConnected || Coordinator is null)
        {
            return;
        }

        _consecutiveRuntimeConfigureFailures++;
        var delay = _consecutiveRuntimeConfigureFailures <= MaxFastRuntimeConfigureRetryAttempts
            ? RuntimeConfigureRetryDelay
            : SlowRuntimeConfigureRetryDelay;
        if (!Coordinator.DispatchDelayed(delay, FlushRuntimeConfigurationRetry))
        {
            return;
        }

        _runtimeConfigureRetryQueued = true;
    }

    private void FlushRuntimeConfigurationRetry()
    {
        _runtimeConfigureRetryQueued = false;
        if (!_isConnected)
        {
            return;
        }

        ConfigureRuntime();
    }

    private void HandleSwipeStart(string? payload)
    {
        if (!VirtualView.IsEnabled)
        {
            return;
        }

        if (!TryDeserialize(payload, out SwipeDirectionPayload? next))
        {
            return;
        }

        if (!TryMapDirection(next!.Direction, out var direction))
        {
            return;
        }

        VirtualView.SwipeStarted(new SwipeViewSwipeStarted(direction));
    }

    private void HandleSwipeChange(string? payload)
    {
        if (!VirtualView.IsEnabled)
        {
            return;
        }

        if (!TryDeserialize(payload, out SwipeChangingPayload? next))
        {
            return;
        }

        if (!TryMapDirection(next!.Direction, out var direction))
        {
            return;
        }

        VirtualView.SwipeChanging(new SwipeViewSwipeChanging(direction, next.Offset));
    }

    private void HandleSwipeEnd(string? payload)
    {
        if (!TryDeserialize(payload, out SwipeEndedPayload? next))
        {
            return;
        }

        if (!TryMapDirection(next!.Direction, out var direction))
        {
            return;
        }

        SynchronizeManagedOpenState(next.IsOpen ? MapDirectionToSide(direction) : SwipeSide.None, next.IsOpen);
        VirtualView.SwipeEnded(new SwipeViewSwipeEnded(direction, next.IsOpen));
    }

    private void HandleSwipeInvoke(string? payload)
    {
        if (!VirtualView.IsEnabled)
        {
            return;
        }

        if (!TryDeserialize(payload, out SwipeInvokedPayload? next))
        {
            return;
        }

        if (!TryMapDirection(next!.Direction, out var direction))
        {
            return;
        }

        var side = MapDirectionToSide(direction);
        var realizations = ResolveRealizations(side);
        if (next.ItemIndex < 0 || next.ItemIndex >= realizations.Count)
        {
            return;
        }

        var realization = realizations[next.ItemIndex];
        if (!realization.IsVisible || !realization.IsEnabled)
        {
            return;
        }

        if (realization.Item is Microsoft.Maui.ISwipeItem swipeItem)
        {
            swipeItem.OnInvoked();
        }
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

    private SwipeItems? GetItemsSource(SwipeSide side)
    {
        return side switch
        {
            SwipeSide.Left => _leftItemsSource,
            SwipeSide.Right => _rightItemsSource,
            SwipeSide.Top => _topItemsSource,
            SwipeSide.Bottom => _bottomItemsSource,
            _ => null
        };
    }

    private void SetItemsSource(SwipeSide side, SwipeItems? items)
    {
        switch (side)
        {
            case SwipeSide.Left:
                _leftItemsSource = items;
                break;
            case SwipeSide.Right:
                _rightItemsSource = items;
                break;
            case SwipeSide.Top:
                _topItemsSource = items;
                break;
            case SwipeSide.Bottom:
                _bottomItemsSource = items;
                break;
        }
    }

    private SwipeSide ResolveSideForItemsSource(object? sender)
    {
        if (ReferenceEquals(sender, _leftItemsSource))
        {
            return SwipeSide.Left;
        }

        if (ReferenceEquals(sender, _rightItemsSource))
        {
            return SwipeSide.Right;
        }

        if (ReferenceEquals(sender, _topItemsSource))
        {
            return SwipeSide.Top;
        }

        if (ReferenceEquals(sender, _bottomItemsSource))
        {
            return SwipeSide.Bottom;
        }

        return SwipeSide.None;
    }

    private MiragePlatformView? ResolveActionHost(SwipeSide side)
    {
        return side switch
        {
            SwipeSide.Left => _leftActionsHost,
            SwipeSide.Right => _rightActionsHost,
            SwipeSide.Top => _topActionsHost,
            SwipeSide.Bottom => _bottomActionsHost,
            _ => null
        };
    }

    private List<SwipeActionRealization> ResolveRealizations(SwipeSide side)
    {
        return side switch
        {
            SwipeSide.Left => _leftRealizations,
            SwipeSide.Right => _rightRealizations,
            SwipeSide.Top => _topRealizations,
            SwipeSide.Bottom => _bottomRealizations,
            _ => _leftRealizations
        };
    }

    private MiragePlatformView CreateRegisteredWebNode(string elementName, string attributeName, string attributeValue)
    {
        var node = MiragePlatformView.CreateWeb(elementName);
        node.SetAttribute(attributeName, attributeValue);
        RegisterChildNode(node);
        return node;
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

        var localId = System.Threading.Interlocked.Increment(ref s_internalNodeIdCounter);
        childNode.AssignNodeId($"mirage-swipe-node-{localId:D4}");
    }

    private void UnregisterChildNode(MiragePlatformView? childNode)
    {
        if (childNode is null)
        {
            return;
        }

        Coordinator?.Unregister(childNode);
    }

    private void DisconnectStructure(MiragePlatformView platformView)
    {
        platformView.ClearChildren();
        UnregisterChildNode(_leftActionsHost);
        UnregisterChildNode(_rightActionsHost);
        UnregisterChildNode(_topActionsHost);
        UnregisterChildNode(_bottomActionsHost);
        UnregisterChildNode(_clipHost);
        UnregisterChildNode(_trackHost);
        UnregisterChildNode(_contentHost);
        _leftActionsHost = null;
        _rightActionsHost = null;
        _topActionsHost = null;
        _bottomActionsHost = null;
        _clipHost = null;
        _trackHost = null;
        _contentHost = null;
    }

    private IMirageImageSourceResolver? GetImageSourceResolver()
    {
        if (_imageSourceResolver is not null)
        {
            return _imageSourceResolver;
        }

        _imageSourceResolver = MauiContext?.Services.GetService<IMirageImageSourceResolver>();
        return _imageSourceResolver;
    }

    private static bool TryDeserialize<T>(string? payload, out T? value)
        where T : class
    {
        value = null;
        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<T>(payload, PayloadJsonOptions);
        }
        catch (JsonException)
        {
            return false;
        }

        return value is not null;
    }

    private static bool TryMapDirection(string? payload, out SwipeDirection direction)
    {
        direction = default;
        switch (payload)
        {
            case "right":
                direction = SwipeDirection.Right;
                return true;
            case "left":
                direction = SwipeDirection.Left;
                return true;
            case "down":
                direction = SwipeDirection.Down;
                return true;
            case "up":
                direction = SwipeDirection.Up;
                return true;
            default:
                return false;
        }
    }

    private static SwipeSide MapDirectionToSide(SwipeDirection direction)
    {
        return direction switch
        {
            SwipeDirection.Right => SwipeSide.Left,
            SwipeDirection.Left => SwipeSide.Right,
            SwipeDirection.Down => SwipeSide.Top,
            SwipeDirection.Up => SwipeSide.Bottom,
            _ => SwipeSide.None
        };
    }

    private static SwipeSide MapOpenSwipeItemToSide(OpenSwipeItem openSwipeItem)
    {
        return openSwipeItem switch
        {
            OpenSwipeItem.LeftItems => SwipeSide.Left,
            OpenSwipeItem.RightItems => SwipeSide.Right,
            OpenSwipeItem.TopItems => SwipeSide.Top,
            OpenSwipeItem.BottomItems => SwipeSide.Bottom,
            _ => SwipeSide.None
        };
    }

    private static string ToSideToken(SwipeSide side)
    {
        return side switch
        {
            SwipeSide.Left => "left",
            SwipeSide.Right => "right",
            SwipeSide.Top => "top",
            SwipeSide.Bottom => "bottom",
            _ => "none"
        };
    }

    private static string ToTransitionModeToken(SwipeTransitionMode transitionMode)
    {
        return transitionMode == SwipeTransitionMode.Drag ? "drag" : "reveal";
    }

    private static SwipeTransitionMode ResolveSwipeTransitionMode(ISwipeView swipeView)
    {
        if (swipeView is SwipeView controlsSwipeView)
        {
            var androidTransition = AndroidSwipeViewConfiguration.GetSwipeTransitionMode(controlsSwipeView);
            if (androidTransition == SwipeTransitionMode.Drag)
            {
                return SwipeTransitionMode.Drag;
            }

            var iosTransition = IOSSwipeViewConfiguration.GetSwipeTransitionMode(controlsSwipeView);
            if (iosTransition == SwipeTransitionMode.Drag)
            {
                return SwipeTransitionMode.Drag;
            }
        }

        return swipeView.SwipeTransitionMode;
    }

    private static string ToSwipeModeToken(SwipeItems? swipeItems)
    {
        return swipeItems?.Mode == SwipeMode.Execute ? "execute" : "reveal";
    }

    private static string ToSwipeBehaviorToken(SwipeItems? swipeItems)
    {
        return swipeItems?.SwipeBehaviorOnInvoked switch
        {
            SwipeBehaviorOnInvoked.Close => "close",
            SwipeBehaviorOnInvoked.RemainOpen => "remainopen",
            _ => "auto"
        };
    }

    private static SwipeRuntimeSideConfiguration BuildSideConfiguration(
        SwipeSide side,
        SwipeItems? swipeItems,
        List<SwipeActionRealization> realizations)
    {
        var visibleCount = realizations.Count(static realization => realization.IsVisible);
        var extent = side is SwipeSide.Left or SwipeSide.Right
            ? visibleCount * HorizontalActionWidth
            : visibleCount * VerticalActionHeight;
        return new SwipeRuntimeSideConfiguration(extent, ToSwipeModeToken(swipeItems), ToSwipeBehaviorToken(swipeItems));
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

    private static IView? ResolvePresentedContent(ISwipeView swipeView)
    {
        if (swipeView is IContentView contentView)
        {
            return contentView.PresentedContent;
        }

        return swipeView.Content as IView;
    }

    private static IView? ResolvePresentedContent(SwipeItemView swipeItemView)
    {
        return ((IContentView)swipeItemView).PresentedContent;
    }

    private static SwipeItems? ResolveSwipeItems(ISwipeView swipeView, SwipeSide side)
    {
        if (swipeView is SwipeView controlsSwipeView)
        {
            return side switch
            {
                SwipeSide.Left => controlsSwipeView.LeftItems,
                SwipeSide.Right => controlsSwipeView.RightItems,
                SwipeSide.Top => controlsSwipeView.TopItems,
                SwipeSide.Bottom => controlsSwipeView.BottomItems,
                _ => null
            };
        }

        return side switch
        {
            SwipeSide.Left => swipeView.LeftItems as SwipeItems,
            SwipeSide.Right => swipeView.RightItems as SwipeItems,
            SwipeSide.Top => swipeView.TopItems as SwipeItems,
            SwipeSide.Bottom => swipeView.BottomItems as SwipeItems,
            _ => null
        };
    }

    private bool TryGetChildStable(IView view, out IElementHandler childHandler, out MiragePlatformView childPlatformView)
    {
        if (TryGetChild(view, out childHandler, out childPlatformView))
        {
            return true;
        }

        if (MauiContext is null)
        {
            childHandler = null!;
            childPlatformView = null!;
            return false;
        }

        var fallbackHandler = CreateFallbackChildHandler(view);
        if (fallbackHandler is null)
        {
            childHandler = null!;
            childPlatformView = null!;
            return false;
        }

        ((IElementHandler)fallbackHandler).SetMauiContext(MauiContext);
        ((IElementHandler)fallbackHandler).SetVirtualView(view);
        if (fallbackHandler.PlatformView is MiragePlatformView fallbackPlatformView)
        {
            childHandler = fallbackHandler;
            childPlatformView = fallbackPlatformView;
            return true;
        }

        MirageHandlerDisconnect.SafeDisconnect(fallbackHandler);
        childHandler = null!;
        childPlatformView = null!;
        return false;
    }

    private static IElementHandler? CreateFallbackChildHandler(IView view)
    {
        return view switch
        {
            Border => new MirageBorderHandler(),
            Label => new MirageLabelHandler(),
            Layout => new MirageLayoutHandler(),
            ContentView => new MirageContentViewHandler(),
            _ => null
        };
    }

    private static void NormalizeHostedContentFlowStyles(MiragePlatformView? contentPlatformView)
    {
        if (contentPlatformView is null)
        {
            return;
        }

        contentPlatformView.SetStyle("position", "relative");
        contentPlatformView.SetStyle("left", null);
        contentPlatformView.SetStyle("top", null);
        contentPlatformView.SetStyle("right", null);
        contentPlatformView.SetStyle("bottom", null);
        contentPlatformView.SetStyle("width", "100%");
        contentPlatformView.SetStyle("height", "100%");
    }

    private static Size ResolveMeasuredSizeWithConstraints(
        ISwipeView swipeView,
        double widthConstraint,
        double heightConstraint,
        double measuredWidth,
        double measuredHeight)
    {
        var clampedWidth = ResolveDesiredDimension(widthConstraint, measuredWidth);
        var clampedHeight = ResolveDesiredDimension(heightConstraint, measuredHeight);
        var (requestedWidth, requestedMinimumWidth, requestedMaximumWidth) = ResolveRequestedDimensionRequests(swipeView, isWidth: true);
        var (requestedHeight, requestedMinimumHeight, requestedMaximumHeight) = ResolveRequestedDimensionRequests(swipeView, isWidth: false);
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
        return IsExplicitLengthSet(value) ? Math.Max(0, value) : 0;
    }

    private static bool IsExplicitLengthSet(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;
    }

    private static (double Exact, double Minimum, double Maximum) ResolveRequestedDimensionRequests(ISwipeView swipeView, bool isWidth)
    {
        if (swipeView is not VisualElement visualElement)
        {
            return (double.NaN, double.NaN, double.NaN);
        }

        var exact = isWidth ? visualElement.WidthRequest : visualElement.HeightRequest;
        var minimum = isWidth ? visualElement.MinimumWidthRequest : visualElement.MinimumHeightRequest;
        var maximum = isWidth ? visualElement.MaximumWidthRequest : visualElement.MaximumHeightRequest;
        return (exact, minimum, maximum);
    }

    private enum SwipeSide
    {
        None,
        Left,
        Right,
        Top,
        Bottom
    }

    private enum PendingRuntimeCommandKind
    {
        None,
        Open,
        Close
    }

    private sealed record SwipeRuntimeConfiguration(
        bool Enabled,
        double Threshold,
        string TransitionMode,
        SwipeRuntimeSideConfiguration Left,
        SwipeRuntimeSideConfiguration Right,
        SwipeRuntimeSideConfiguration Top,
        SwipeRuntimeSideConfiguration Bottom);

    private sealed record SwipeRuntimeSideConfiguration(double Extent, string Mode, string Behavior);

    private readonly record struct PendingRuntimeCommand(PendingRuntimeCommandKind Kind, SwipeSide Side, bool Animated)
    {
        public static PendingRuntimeCommand None => new(PendingRuntimeCommandKind.None, SwipeSide.None, false);

        public static PendingRuntimeCommand Open(SwipeSide side, bool animated) => new(PendingRuntimeCommandKind.Open, side, animated);

        public static PendingRuntimeCommand Close(bool animated) => new(PendingRuntimeCommandKind.Close, SwipeSide.None, animated);
    }

    private sealed class SwipeActionRealization
    {
        private SwipeActionRealization(
            SwipeSide side,
            Microsoft.Maui.ISwipeItem item,
            MiragePlatformView wrapperNode,
            MirageSwipeItemMenuItemHandler? menuItemHandler,
            SwipeItemView? swipeItemView,
            IElementHandler? childHandler,
            MiragePlatformView? childPlatformView)
        {
            Side = side;
            Item = item;
            WrapperNode = wrapperNode;
            MenuItemHandler = menuItemHandler;
            SwipeItemView = swipeItemView;
            ChildHandler = childHandler;
            ChildPlatformView = childPlatformView;
        }

        public SwipeSide Side { get; }

        public Microsoft.Maui.ISwipeItem Item { get; }

        public MiragePlatformView WrapperNode { get; }

        public MirageSwipeItemMenuItemHandler? MenuItemHandler { get; }

        public SwipeItemView? SwipeItemView { get; }

        public IElementHandler? ChildHandler { get; }

        public MiragePlatformView? ChildPlatformView { get; }

        public bool IsVisible => Item switch
        {
            SwipeItem swipeItem => swipeItem.IsVisible,
            SwipeItemView swipeItemView => swipeItemView.IsVisible,
            _ => true
        };

        public bool IsEnabled => Item switch
        {
            SwipeItem swipeItem => swipeItem.IsEnabled,
            SwipeItemView swipeItemView => swipeItemView.IsEnabled,
            _ => true
        };

        public static SwipeActionRealization CreateMenu(SwipeSide side, SwipeItem item, MirageSwipeItemMenuItemHandler menuItemHandler)
        {
            return new SwipeActionRealization(side, item, menuItemHandler.RootNode, menuItemHandler, null, null, null);
        }

        public static SwipeActionRealization CreateView(
            SwipeSide side,
            SwipeItemView item,
            MiragePlatformView wrapperNode,
            IElementHandler? childHandler,
            MiragePlatformView? childPlatformView)
        {
            return new SwipeActionRealization(side, item, wrapperNode, null, item, childHandler, childPlatformView);
        }
    }

    private class SwipeDirectionPayload
    {
        public string? Direction { get; set; }
    }

    private sealed class SwipeChangingPayload : SwipeDirectionPayload
    {
        public double Offset { get; set; }
    }

    private sealed class SwipeEndedPayload : SwipeDirectionPayload
    {
        public bool IsOpen { get; set; }
    }

    private sealed class SwipeInvokedPayload : SwipeDirectionPayload
    {
        public int ItemIndex { get; set; }

        public string? ItemKind { get; set; }
    }
}
