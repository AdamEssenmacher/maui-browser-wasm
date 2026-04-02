using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json;
using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Mirage.Maui.Hosting;
using Mirage.Web.Runtime;
using AndroidSwipeViewConfiguration = Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.SwipeView;
using IOSSwipeViewConfiguration = Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.SwipeView;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Mirage.Integration.Tests;

[Collection("Mirage NonParallel")]
public sealed class MirageSwipeViewHandlerTests
{
    [Fact]
    public void SwipeViewHandler_ProjectsBuiltInRootHostTopology_AndConfiguresRuntime()
    {
        var swipeView = CreateSwipeView();
        swipeView.Threshold = 84;
        ApplySwipeTransitionMode(swipeView, SwipeTransitionMode.Drag);
        swipeView.LeftItems = CreateSwipeItems(CreateMenuItem("Archive", "#0F766E"));
        swipeView.RightItems = CreateSwipeItems(CreateMenuItem("Delete", "#B91C1C"));
        swipeView.TopItems = CreateSwipeItems(CreateMenuItem("Pin", "#1D4ED8"));
        swipeView.BottomItems = CreateSwipeItems(CreateMenuItem("Later", "#7C3AED"));

        var (handler, _, _, runtime) = CreateAttachedHandler(swipeView);

        var clipHost = FindNodeByAttribute(handler.PlatformView, "data-maui-sv-clip", "true");
        var trackHost = FindNodeByAttribute(handler.PlatformView, "data-maui-sv-track", "true");
        var contentHost = FindNodeByAttribute(handler.PlatformView, "data-maui-sv-content-host", "true");
        var leftHost = FindNodeByAttribute(handler.PlatformView, "data-maui-sv-action-host", "left");
        var rightHost = FindNodeByAttribute(handler.PlatformView, "data-maui-sv-action-host", "right");
        var topHost = FindNodeByAttribute(handler.PlatformView, "data-maui-sv-action-host", "top");
        var bottomHost = FindNodeByAttribute(handler.PlatformView, "data-maui-sv-action-host", "bottom");

        Assert.Equal(ComponentKind.SwipeView, handler.PlatformView.ControlKind);
        Assert.Equal(5, handler.PlatformView.Children.Count);
        Assert.Same(leftHost, handler.PlatformView.Children[0]);
        Assert.Same(rightHost, handler.PlatformView.Children[1]);
        Assert.Same(topHost, handler.PlatformView.Children[2]);
        Assert.Same(bottomHost, handler.PlatformView.Children[3]);
        Assert.Same(clipHost, handler.PlatformView.Children[4]);
        Assert.Single(clipHost.Children);
        Assert.Same(trackHost, clipHost.Children[0]);
        Assert.Single(trackHost.Children);
        Assert.Same(contentHost, trackHost.Children[0]);
        Assert.Equal("false", handler.PlatformView.GetPropertyString("OpenState"));
        Assert.Equal("none", handler.PlatformView.GetPropertyString("OpenSideState"));
        Assert.Equal("true", handler.PlatformView.GetPropertyString("EnabledState"));
        Assert.Equal("drag", handler.PlatformView.GetPropertyString("TransitionModeState"));
        Assert.Equal("84", handler.PlatformView.GetPropertyString("ThresholdState"));

        using var payload = JsonDocument.Parse(runtime.LastConfigurationJson);
        Assert.True(payload.RootElement.GetProperty("enabled").GetBoolean());
        Assert.Equal(84, payload.RootElement.GetProperty("threshold").GetDouble(), 3);
        Assert.Equal("drag", payload.RootElement.GetProperty("transitionMode").GetString());
        Assert.Equal(88, payload.RootElement.GetProperty("left").GetProperty("extent").GetDouble(), 3);
        Assert.Equal(88, payload.RootElement.GetProperty("right").GetProperty("extent").GetDouble(), 3);
        Assert.Equal(56, payload.RootElement.GetProperty("top").GetProperty("extent").GetDouble(), 3);
        Assert.Equal(56, payload.RootElement.GetProperty("bottom").GetProperty("extent").GetDouble(), 3);
    }

    [Fact]
    public void SwipeViewHandler_RealizesSwipeItemAndSwipeItemView_AndRebuildsOnMutation()
    {
        var viewAction = CreateSwipeItemViewAction("Details");
        viewAction.IsVisible = true;
        var items = CreateSwipeItems(
            CreateMenuItem("Archive", "#0F766E"),
            viewAction);

        var swipeView = CreateSwipeView();
        swipeView.LeftItems = items;

        var (handler, _, _, runtime) = CreateAttachedHandler(swipeView);
        var leftHost = FindNodeByAttribute(handler.PlatformView, "data-maui-sv-action-host", "left");

        Assert.Equal(2, FindNodesByAttribute(leftHost, "data-maui-sv-action-wrapper", "true").Count);
        Assert.NotNull(FindNodeByAttributeOrNull(leftHost, "data-maui-sv-action-kind", "menu"));
        Assert.NotNull(FindNodeByAttributeOrNull(leftHost, "data-maui-sv-action-kind", "view"));
        Assert.NotNull(FindNodeByPropertyOrNull(leftHost, "AutomationId", "controls-sample-swipeitemview-action-label"));
        var actionButton = FindNodeByPropertyOrNull(leftHost, "AutomationId", "controls-sample-swipeitemview-action-button");
        Assert.NotNull(actionButton);
        Assert.NotEqual("none", actionButton!.GetStyleValue("pointerEvents"));

        ((SwipeItem)items[0]).IsVisible = false;
        viewAction.IsVisible = false;

        Assert.Equal("0px", leftHost.GetStyleValue("width"));

        items.Add(CreateMenuItem("Later", "#7C3AED"));

        Assert.Equal("88px", leftHost.GetStyleValue("width"));
        Assert.Equal(3, FindNodesByAttribute(leftHost, "data-maui-sv-action-wrapper", "true").Count);
        using var payload = JsonDocument.Parse(runtime.LastConfigurationJson);
        Assert.Equal(88, payload.RootElement.GetProperty("left").GetProperty("extent").GetDouble(), 3);
    }

    [Fact]
    public void SwipeViewHandler_RequestOpenClose_AndRuntimeLifecycle_StaySynchronized()
    {
        var swipeView = CreateSwipeView();
        swipeView.LeftItems = CreateSwipeItems(CreateMenuItem("Archive", "#0F766E"));

        var started = 0;
        var changing = 0;
        var ended = 0;
        swipeView.SwipeStarted += (_, _) => started++;
        swipeView.SwipeChanging += (_, _) => changing++;
        swipeView.SwipeEnded += (_, _) => ended++;

        var (handler, _, coordinator, runtime) = CreateAttachedHandler(swipeView);

        swipeView.Open(OpenSwipeItem.LeftItems, animated: true);
        Assert.Equal("left", runtime.LastOpenSide);
        Assert.True(runtime.LastOpenAnimated);

        coordinator.OnEvent(handler.PlatformView.NodeId, "swipestart", """{"direction":"right"}""");
        coordinator.OnEvent(handler.PlatformView.NodeId, "swipechange", """{"direction":"right","offset":64}""");
        coordinator.OnEvent(handler.PlatformView.NodeId, "swipeend", """{"direction":"right","isOpen":true}""");

        Assert.Equal(1, started);
        Assert.Equal(1, changing);
        Assert.Equal(1, ended);
        Assert.True(((Microsoft.Maui.ISwipeView)swipeView).IsOpen);
        Assert.Equal("true", handler.PlatformView.GetPropertyString("OpenState"));
        Assert.Equal("left", handler.PlatformView.GetPropertyString("OpenSideState"));

        swipeView.Close(animated: false);
        Assert.False(runtime.LastCloseAnimated);

        coordinator.OnEvent(handler.PlatformView.NodeId, "swipeend", """{"direction":"right","isOpen":false}""");

        Assert.Equal(2, ended);
        Assert.False(((Microsoft.Maui.ISwipeView)swipeView).IsOpen);
        Assert.Equal("false", handler.PlatformView.GetPropertyString("OpenState"));
        Assert.Equal("none", handler.PlatformView.GetPropertyString("OpenSideState"));
    }

    [Fact]
    public void SwipeViewHandler_SwipeInvoke_InvokesSwipeItemAndSwipeItemView_AndIgnoresDisabledRuntimeEvents()
    {
        var menuInvokeCount = 0;
        var viewInvokeCount = 0;
        var started = 0;

        var menuItem = CreateMenuItem("Archive", "#0F766E");
        menuItem.Command = new Command(() => menuInvokeCount++);

        var viewAction = CreateSwipeItemViewAction("Details");
        viewAction.Command = new Command(() => viewInvokeCount++);

        var swipeView = CreateSwipeView();
        swipeView.LeftItems = CreateSwipeItems(menuItem);
        swipeView.TopItems = CreateSwipeItems(viewAction);
        swipeView.SwipeStarted += (_, _) => started++;

        var (handler, _, coordinator, _) = CreateAttachedHandler(swipeView);

        coordinator.OnEvent(handler.PlatformView.NodeId, "swipeinvoke", """{"direction":"right","itemIndex":0,"itemKind":"menu"}""");
        coordinator.OnEvent(handler.PlatformView.NodeId, "swipeinvoke", """{"direction":"down","itemIndex":0,"itemKind":"view"}""");

        Assert.Equal(1, menuInvokeCount);
        Assert.Equal(1, viewInvokeCount);

        swipeView.IsEnabled = false;
        MirageSwipeViewHandler.Mapper.UpdateProperty(handler, swipeView, nameof(IView.IsEnabled));

        coordinator.OnEvent(handler.PlatformView.NodeId, "swipestart", """{"direction":"right"}""");
        coordinator.OnEvent(handler.PlatformView.NodeId, "swipeinvoke", """{"direction":"right","itemIndex":0,"itemKind":"menu"}""");
        coordinator.OnEvent(handler.PlatformView.NodeId, "swipeinvoke", """{"direction":"down","itemIndex":0,"itemKind":"view"}""");

        Assert.Equal(0, started);
        Assert.Equal(1, menuInvokeCount);
        Assert.Equal(1, viewInvokeCount);
        Assert.Equal("false", handler.PlatformView.GetPropertyString("EnabledState"));
    }

    [Fact]
    public void SwipeViewHandler_RetriesRuntimeConfiguration_WhenInitialAttachPrecedesRuntimeNodeAvailability()
    {
        var swipeView = CreateSwipeView();
        swipeView.LeftItems = CreateSwipeItems(CreateMenuItem("Archive", "#0F766E"));

        var coordinator = new QueuedSwipeCoordinator();
        coordinator.EnqueueConfigureResult(false);
        coordinator.EnqueueConfigureResult(true);

        var handler = CreateAttachedHandler(swipeView, coordinator);
        var baselineConfigureCallCount = coordinator.ConfigureSwipeViewCallCount;

        Assert.True(baselineConfigureCallCount >= 1);
        Assert.Equal(handler.PlatformView.NodeId, coordinator.LastConfiguredOwnerNodeId);
        Assert.Equal(1, coordinator.PendingDelayedCount);

        coordinator.FlushNextDelayed();

        Assert.Equal(baselineConfigureCallCount + 1, coordinator.ConfigureSwipeViewCallCount);
        Assert.Equal(handler.PlatformView.NodeId, coordinator.LastConfiguredOwnerNodeId);
        Assert.Equal(0, coordinator.PendingDelayedCount);
        using var payload = JsonDocument.Parse(coordinator.LastConfigurationJson);
        Assert.Equal(88, payload.RootElement.GetProperty("left").GetProperty("extent").GetDouble(), 3);
    }

    [Fact]
    public void SwipeViewHandler_ReplaysPendingOpenRequest_WhenInitialRuntimeOpenIsRejected()
    {
        var swipeView = CreateSwipeView();
        swipeView.LeftItems = CreateSwipeItems(CreateMenuItem("Archive", "#0F766E"));

        var coordinator = new QueuedSwipeCoordinator();
        coordinator.EnqueueOpenResult(false);
        coordinator.EnqueueOpenResult(true);

        var handler = CreateAttachedHandler(swipeView, coordinator);

        MirageSwipeViewHandler.MapRequestOpen(handler, swipeView, CreateSwipeViewOpenRequest(OpenSwipeItem.LeftItems, animated: true));

        Assert.Equal(2, coordinator.OpenSwipeViewCallCount);
        Assert.Equal("left", coordinator.LastOpenSide);
        Assert.True(coordinator.LastOpenAnimated);
        Assert.Equal(0, coordinator.PendingDelayedCount);
    }

    [Fact]
    public void SwipeViewHandler_ReplaysPendingCloseRequest_WithoutReopeningConfirmedSide()
    {
        var swipeView = CreateSwipeView();
        swipeView.LeftItems = CreateSwipeItems(CreateMenuItem("Archive", "#0F766E"));

        var coordinator = new QueuedSwipeCoordinator();
        coordinator.EnqueueCloseResult(false);
        coordinator.EnqueueCloseResult(true);

        var handler = CreateAttachedHandler(swipeView, coordinator);
        handler.OnRuntimeEvent("swipeend", """{"direction":"right","isOpen":true}""");

        MirageSwipeViewHandler.MapRequestClose(handler, swipeView, CreateSwipeViewCloseRequest(animated: false));

        Assert.Equal(2, coordinator.CloseSwipeViewCallCount);
        Assert.False(coordinator.LastCloseAnimated);
        Assert.Equal(0, coordinator.OpenSwipeViewCallCount);
        Assert.Equal(0, coordinator.PendingDelayedCount);
    }

    private static (MirageSwipeViewHandler Handler, SwipeView SwipeView, MirageRenderCoordinator Coordinator, SwipeRuntimeProbe Runtime) CreateAttachedHandler(
        SwipeView swipeView,
        MirageRenderCoordinator? coordinator = null,
        SwipeRuntimeProbe? runtime = null)
    {
        coordinator ??= new MirageRenderCoordinator(new BrowserDispatcher());
        runtime ??= new SwipeRuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton<IMirageRenderCoordinator>(coordinator);
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler(typeof(SwipeView), typeof(MirageSwipeViewHandler));
            handlers.AddHandler(typeof(SwipeItemView), typeof(MirageSwipeItemViewHandler));
            handlers.AddHandler(typeof(Border), typeof(MirageBorderHandler));
            handlers.AddHandler(typeof(Button), typeof(MirageButtonHandler));
            handlers.AddHandler(typeof(Label), typeof(MirageLabelHandler));
            handlers.AddHandler(typeof(VerticalStackLayout), typeof(MirageLayoutHandler));
        });

        var mauiApp = builder.Build();
        var mauiContext = new MauiContext(mauiApp.Services);

        var handler = new MirageSwipeViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(swipeView);
        handler.PlatformArrange(new Rect(0, 0, 240, 72));
        return (handler, swipeView, coordinator, runtime);
    }

    private static MirageSwipeViewHandler CreateAttachedHandler(
        SwipeView swipeView,
        IMirageRenderCoordinator coordinator)
    {
        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton(coordinator);
        builder.Services.AddSingleton(typeof(IMirageRenderCoordinator), coordinator);
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler(typeof(SwipeView), typeof(MirageSwipeViewHandler));
            handlers.AddHandler(typeof(SwipeItemView), typeof(MirageSwipeItemViewHandler));
            handlers.AddHandler(typeof(Border), typeof(MirageBorderHandler));
            handlers.AddHandler(typeof(Button), typeof(MirageButtonHandler));
            handlers.AddHandler(typeof(Label), typeof(MirageLabelHandler));
            handlers.AddHandler(typeof(VerticalStackLayout), typeof(MirageLayoutHandler));
        });

        var mauiApp = builder.Build();
        var mauiContext = new MauiContext(mauiApp.Services);

        var handler = new MirageSwipeViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(swipeView);
        handler.PlatformArrange(new Rect(0, 0, 240, 72));
        return handler;
    }

    private static SwipeView CreateSwipeView()
    {
        var swipeView = new SwipeView
        {
            WidthRequest = 240,
            HeightRequest = 72,
            Threshold = 72,
            Content = new Border
            {
                AutomationId = "controls-sample-swipeview-content",
                Padding = new Thickness(12, 8),
                StrokeThickness = 0,
                Content = new Label
                {
                    Text = "Swipe me",
                    AutomationId = "controls-sample-swipeview-content-label"
                }
            }
        };

        ApplySwipeTransitionMode(swipeView, SwipeTransitionMode.Reveal);
        return swipeView;
    }

    private static SwipeItems CreateSwipeItems(params Microsoft.Maui.Controls.ISwipeItem[] items)
    {
        var swipeItems = new SwipeItems();
        foreach (var item in items)
        {
            swipeItems.Add(item);
        }

        return swipeItems;
    }

    private static SwipeItem CreateMenuItem(string text, string backgroundHex)
    {
        return new SwipeItem
        {
            Text = text,
            BackgroundColor = Color.FromArgb(backgroundHex)
        };
    }

    private static SwipeItemView CreateSwipeItemViewAction(string text)
    {
        return new SwipeItemView
        {
            Content = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb("#E0F2FE")),
                StrokeThickness = 0,
                Padding = new Thickness(10, 8),
                Content = new VerticalStackLayout
                {
                    Spacing = 6,
                    Children =
                    {
                        new Label
                        {
                            AutomationId = "controls-sample-swipeitemview-action-label",
                            Text = text
                        },
                        new Button
                        {
                            AutomationId = "controls-sample-swipeitemview-action-button",
                            Text = "Inner action"
                        }
                    }
                }
            }
        };
    }

    private static void ApplySwipeTransitionMode(SwipeView swipeView, SwipeTransitionMode mode)
    {
        AndroidSwipeViewConfiguration.SetSwipeTransitionMode(swipeView, mode);
        IOSSwipeViewConfiguration.SetSwipeTransitionMode(swipeView, mode);
    }

    private static object CreateSwipeViewOpenRequest(OpenSwipeItem openSwipeItem, bool animated)
    {
        var requestType = typeof(ISwipeView).Assembly.GetType("Microsoft.Maui.SwipeViewOpenRequest", throwOnError: true)
            ?? throw new InvalidOperationException("Unable to locate Microsoft.Maui.SwipeViewOpenRequest.");
        var constructor = requestType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(ctor =>
            {
                var parameters = ctor.GetParameters();
                return parameters.Length == 2
                    && parameters[0].ParameterType == typeof(OpenSwipeItem)
                    && parameters[1].ParameterType == typeof(bool);
            });
        return constructor.Invoke([openSwipeItem, animated]);
    }

    private static object CreateSwipeViewCloseRequest(bool animated)
    {
        var requestType = typeof(ISwipeView).Assembly.GetType("Microsoft.Maui.SwipeViewCloseRequest", throwOnError: true)
            ?? throw new InvalidOperationException("Unable to locate Microsoft.Maui.SwipeViewCloseRequest.");
        var constructor = requestType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(ctor =>
            {
                var parameters = ctor.GetParameters();
                return parameters.Length == 1
                    && parameters[0].ParameterType == typeof(bool);
            });
        return constructor.Invoke([animated]);
    }

    private static MiragePlatformView FindNodeByAttribute(MiragePlatformView root, string attributeName, string expectedValue)
    {
        var match = FindNodeByAttributeOrNull(root, attributeName, expectedValue);
        return match ?? throw new InvalidOperationException($"Unable to locate node with attribute '{attributeName}'='{expectedValue}'.");
    }

    private static MiragePlatformView? FindNodeByAttributeOrNull(MiragePlatformView root, string attributeName, string? expectedValue)
    {
        if (root.Attributes.TryGetValue(attributeName, out var value)
            && string.Equals(value, expectedValue, StringComparison.Ordinal))
        {
            return root;
        }

        foreach (var child in root.Children)
        {
            var match = FindNodeByAttributeOrNull(child, attributeName, expectedValue);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static IReadOnlyList<MiragePlatformView> FindNodesByAttribute(MiragePlatformView root, string attributeName, string expectedValue)
    {
        var matches = new List<MiragePlatformView>();
        CollectNodesByAttribute(root, attributeName, expectedValue, matches);
        return matches;
    }

    private static void CollectNodesByAttribute(MiragePlatformView root, string attributeName, string expectedValue, List<MiragePlatformView> matches)
    {
        if (root.Attributes.TryGetValue(attributeName, out var value)
            && string.Equals(value, expectedValue, StringComparison.Ordinal))
        {
            matches.Add(root);
        }

        foreach (var child in root.Children)
        {
            CollectNodesByAttribute(child, attributeName, expectedValue, matches);
        }
    }

    private static MiragePlatformView? FindNodeByPropertyOrNull(MiragePlatformView root, string propertyName, string? expectedValue)
    {
        if (string.Equals(root.GetPropertyString(propertyName), expectedValue, StringComparison.Ordinal))
        {
            return root;
        }

        foreach (var child in root.Children)
        {
            var match = FindNodeByPropertyOrNull(child, propertyName, expectedValue);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private sealed class SwipeRuntimeProbe : IMirageRuntime
    {
        public List<SwipeConfigurationCall> Configurations { get; } = [];

        public string LastConfigurationJson => Configurations.LastOrDefault()?.ConfigurationJson ?? "{}";

        public string? LastOpenOwnerNodeId { get; private set; }

        public string? LastOpenSide { get; private set; }

        public bool LastOpenAnimated { get; private set; }

        public string? LastCloseOwnerNodeId { get; private set; }

        public bool LastCloseAnimated { get; private set; }

        public void Dispose()
        {
        }

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
            actualLeft = left;
            actualTop = top;
            return true;
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

        public bool TryConfigureNodeSwipeView(string ownerNodeId, bool enabled, string configurationJson)
        {
            Configurations.Add(new SwipeConfigurationCall(ownerNodeId, enabled, configurationJson));
            return true;
        }

        public bool TryRequestNodeSwipeViewOpen(string ownerNodeId, string side, bool animated)
        {
            LastOpenOwnerNodeId = ownerNodeId;
            LastOpenSide = side;
            LastOpenAnimated = animated;
            return true;
        }

        public bool TryRequestNodeSwipeViewClose(string ownerNodeId, bool animated)
        {
            LastCloseOwnerNodeId = ownerNodeId;
            LastCloseAnimated = animated;
            return true;
        }

        public bool TrySetNodeFocus(string nodeId, bool isFocused)
        {
            _ = nodeId;
            _ = isFocused;
            return true;
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
            return true;
        }

        public bool TryConfigureNodePullToRefresh(string ownerNodeId, string sourceNodeId, bool enabled, double threshold)
        {
            _ = ownerNodeId;
            _ = sourceNodeId;
            _ = enabled;
            _ = threshold;
            return false;
        }

        public bool TryConfigureNodeCanvas(string nodeId, double logicalWidth, double logicalHeight, out double displayScale)
        {
            _ = nodeId;
            _ = logicalWidth;
            _ = logicalHeight;
            displayScale = 1;
            return false;
        }

        public bool TryRenderNodeCanvas(string nodeId, string commandsJson)
        {
            _ = nodeId;
            _ = commandsJson;
            return false;
        }
    }

    private sealed class QueuedSwipeCoordinator : IMirageRenderCoordinator
    {
        private readonly Queue<Action> _pendingDelayedActions = [];
        private readonly Queue<bool> _configureResults = [];
        private readonly Queue<bool> _openResults = [];
        private readonly Queue<bool> _closeResults = [];
        private int _nodeIdCounter;

        public int PendingDelayedCount => _pendingDelayedActions.Count;

        public int ConfigureSwipeViewCallCount { get; private set; }

        public string? LastConfiguredOwnerNodeId { get; private set; }

        public string LastConfigurationJson { get; private set; } = "{}";

        public int OpenSwipeViewCallCount { get; private set; }

        public string? LastOpenSide { get; private set; }

        public bool LastOpenAnimated { get; private set; }

        public int CloseSwipeViewCallCount { get; private set; }

        public bool LastCloseAnimated { get; private set; }

        public void EnqueueConfigureResult(bool result)
        {
            _configureResults.Enqueue(result);
        }

        public void EnqueueOpenResult(bool result)
        {
            _openResults.Enqueue(result);
        }

        public void EnqueueCloseResult(bool result)
        {
            _closeResults.Enqueue(result);
        }

        public void FlushNextDelayed()
        {
            Assert.True(_pendingDelayedActions.Count > 0, "Expected a pending delayed swipe configuration retry.");
            var action = _pendingDelayedActions.Dequeue();
            action();
        }

        public void AttachRuntime(IMirageRuntime runtime)
        {
            _ = runtime;
        }

        public void DetachRuntime()
        {
        }

        public void Register(MiragePlatformView platformView, IMirageRuntimeEventTarget? eventTarget)
        {
            if (string.IsNullOrWhiteSpace(platformView.NodeId))
            {
                var localId = System.Threading.Interlocked.Increment(ref _nodeIdCounter);
                platformView.AssignNodeId($"queued-swipe-node-{localId:D4}");
            }

            _ = eventTarget;
        }

        public void Unregister(MiragePlatformView platformView)
        {
            _ = platformView;
        }

        public void SetRoot(MiragePlatformView? root)
        {
            _ = root;
        }

        public void RequestRender()
        {
        }

        public bool DispatchDelayed(TimeSpan delay, Action action)
        {
            _ = delay;
            _pendingDelayedActions.Enqueue(action);
            return true;
        }

        public IDisposable BeginRenderBatch()
        {
            return NullDisposable.Instance;
        }

        public bool TryGetNodeInputValue(string nodeId, out string? value)
        {
            _ = nodeId;
            value = null;
            return false;
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

        public bool TryConfigureNodePullToRefresh(string ownerNodeId, string sourceNodeId, bool enabled, double threshold)
        {
            _ = ownerNodeId;
            _ = sourceNodeId;
            _ = enabled;
            _ = threshold;
            return false;
        }

        public bool TryConfigureNodeSwipeView(string ownerNodeId, bool enabled, string configurationJson)
        {
            ConfigureSwipeViewCallCount++;
            LastConfiguredOwnerNodeId = ownerNodeId;
            LastConfigurationJson = configurationJson;
            _ = enabled;
            return _configureResults.Count > 0 ? _configureResults.Dequeue() : true;
        }

        public bool TryRequestNodeSwipeViewOpen(string ownerNodeId, string side, bool animated)
        {
            _ = ownerNodeId;
            OpenSwipeViewCallCount++;
            LastOpenSide = side;
            LastOpenAnimated = animated;
            return _openResults.Count > 0 ? _openResults.Dequeue() : true;
        }

        public bool TryRequestNodeSwipeViewClose(string ownerNodeId, bool animated)
        {
            _ = ownerNodeId;
            CloseSwipeViewCallCount++;
            LastCloseAnimated = animated;
            return _closeResults.Count > 0 ? _closeResults.Dequeue() : true;
        }

        public bool TryConfigureNodeCanvas(string nodeId, double logicalWidth, double logicalHeight, out double displayScale)
        {
            _ = nodeId;
            _ = logicalWidth;
            _ = logicalHeight;
            displayScale = 1;
            return false;
        }

        public bool TryRenderNodeCanvas(string nodeId, string commandsJson)
        {
            _ = nodeId;
            _ = commandsJson;
            return false;
        }

        public void OnEvent(string nodeId, string eventName, string? payload)
        {
            _ = nodeId;
            _ = eventName;
            _ = payload;
        }
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();

        public void Dispose()
        {
        }
    }

    private sealed record SwipeConfigurationCall(string OwnerNodeId, bool Enabled, string ConfigurationJson);
}
