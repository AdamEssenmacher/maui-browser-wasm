using System.Collections.ObjectModel;
using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Mirage.Maui.Hosting;
using Mirage.Web.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;

namespace Mirage.Integration.Tests;

[Collection("Mirage NonParallel")]
public sealed class MirageRefreshViewHandlerTests
{
    [Fact]
    public void RefreshViewHandler_ProjectsBuiltInRootHostTopology_AndViewportPullSource()
    {
        var refreshView = CreateRefreshView(CreatePlainContent());
        refreshView.RefreshColor = Color.FromArgb("#0EA5E9");

        var (handler, _, _, runtime) = CreateAttachedHandler(refreshView);

        var indicatorHost = FindNodeByAttribute(handler.PlatformView, "data-maui-rv-indicator-host", "true");
        var stateHost = FindNodeByAttribute(handler.PlatformView, "data-maui-rv-state-host", "true");
        var spinnerNode = FindNodeByAttribute(handler.PlatformView, "data-maui-rv-spinner", "true");
        var stateLabel = FindNodeByAttribute(handler.PlatformView, "data-maui-rv-state", "idle");
        var triggerButton = FindNodeByAttribute(handler.PlatformView, "data-maui-rv-trigger", "true");
        var contentViewport = FindNodeByAttribute(handler.PlatformView, "data-maui-rv-content-host", "true");

        Assert.Equal(ComponentKind.RefreshView, handler.PlatformView.ControlKind);
        Assert.Equal("flex", handler.PlatformView.GetStyleValue("display"));
        Assert.Equal("column", handler.PlatformView.GetStyleValue("flexDirection"));
        Assert.Equal("auto", contentViewport.GetStyleValue("overflowY"));
        Assert.Equal(2, handler.PlatformView.Children.Count);
        Assert.Same(indicatorHost, handler.PlatformView.Children[0]);
        Assert.Same(contentViewport, handler.PlatformView.Children[1]);
        Assert.Equal(2, indicatorHost.Children.Count);
        Assert.Same(stateHost, indicatorHost.Children[0]);
        Assert.Same(triggerButton, indicatorHost.Children[1]);
        Assert.Equal(2, stateHost.Children.Count);
        Assert.Same(spinnerNode, stateHost.Children[0]);
        Assert.Same(stateLabel, stateHost.Children[1]);
        Assert.Equal("true", handler.PlatformView.GetPropertyString("EnabledState"));
        Assert.Equal(contentViewport.NodeId, runtime.LastConfiguredSourceNodeId);
        Assert.True(runtime.LastConfigureEnabled);
        Assert.Equal(72, runtime.LastConfigureThreshold, 3);
        Assert.Equal("rgba(14,165,233,1)", stateLabel.GetStyleValue("color"));
        Assert.Equal("rgba(14,165,233,1)", triggerButton.GetStyleValue("color"));
    }

    [Fact]
    public void RefreshViewHandler_GetDesiredSize_UsesLegacyFallbackAndConstraintClamp()
    {
        var refreshView = CreateRefreshView(content: null);
        var (handler, _, _, _) = CreateAttachedHandler(refreshView);

        var unconstrained = handler.GetDesiredSize(double.PositiveInfinity, double.PositiveInfinity);
        Assert.Equal(260, unconstrained.Width);
        Assert.Equal(264, unconstrained.Height);

        var constrained = handler.GetDesiredSize(180, 120);
        Assert.Equal(180, constrained.Width);
        Assert.Equal(120, constrained.Height);
    }

    [Fact]
    public void RefreshViewHandler_GetDesiredSize_RespectsExplicitMinAndMaxRequests()
    {
        var refreshView = CreateRefreshView(CreatePlainContent());
        refreshView.WidthRequest = 320;
        refreshView.MaximumWidthRequest = 280;
        refreshView.HeightRequest = 180;
        refreshView.MinimumHeightRequest = 296;

        var (handler, _, _, _) = CreateAttachedHandler(refreshView);

        var size = handler.GetDesiredSize(double.PositiveInfinity, double.PositiveInfinity);
        Assert.Equal(280, size.Width);
        Assert.Equal(296, size.Height);
    }

    [Fact]
    public void RefreshViewHandler_RebindsPullSourceAcrossPlainScrollAndCollectionContent()
    {
        var runtime = new RefreshRuntimeProbe();
        var refreshView = CreateRefreshView(CreatePlainContent());
        var (handler, _, _, _) = CreateAttachedHandler(refreshView, runtime: runtime);

        var contentViewport = FindNodeByAttribute(handler.PlatformView, "data-maui-rv-content-host", "true");
        Assert.Equal(contentViewport.NodeId, runtime.LastEnabledConfiguration().SourceNodeId);
        Assert.Equal("auto", contentViewport.GetStyleValue("overflowY"));

        var scrollView = new ScrollView
        {
            Orientation = ScrollOrientation.Vertical,
            Content = CreatePlainContent()
        };
        refreshView.Content = scrollView;
        MirageRefreshViewHandler.Mapper.UpdateProperty(handler, refreshView, nameof(IRefreshView.Content));

        var scrollHandler = Assert.IsType<MirageScrollViewHandler>(scrollView.Handler);
        Assert.Equal(scrollHandler.PlatformView.NodeId, runtime.LastEnabledConfiguration().SourceNodeId);
        Assert.Equal("hidden", contentViewport.GetStyleValue("overflowY"));

        var collectionView = CreateCollectionContent();
        refreshView.Content = collectionView;
        MirageRefreshViewHandler.Mapper.UpdateProperty(handler, refreshView, nameof(IRefreshView.Content));

        var collectionHandler = Assert.IsType<MirageCollectionViewHandler>(collectionView.Handler);
        Assert.Equal(collectionHandler.PlatformView.NodeId, runtime.LastEnabledConfiguration().SourceNodeId);
        Assert.Equal("hidden", contentViewport.GetStyleValue("overflowY"));
        Assert.True(runtime.PullConfigurations.Count(static call => call.Enabled) >= 3);
    }

    [Fact]
    public void RefreshViewHandler_TriggerClickAndPullRuntimeEvents_SetIsRefreshingAndCommandParameter()
    {
        var commandCount = 0;
        var lastParameter = "<none>";
        var refreshView = CreateRefreshView(CreatePlainContent());
        refreshView.Command = new Command<object?>(parameter =>
        {
            commandCount++;
            lastParameter = parameter?.ToString() ?? "<null>";
        });
        refreshView.CommandParameter = "button-mode";

        var (handler, _, coordinator, _) = CreateAttachedHandler(refreshView);
        var triggerButton = FindNodeByAttribute(handler.PlatformView, "data-maui-rv-trigger", "true");

        coordinator.OnEvent(triggerButton.NodeId, "click", null);
        AssertEventually(() => refreshView.IsRefreshing && commandCount == 1);
        Assert.Equal("button-mode", lastParameter);
        Assert.Equal("true", handler.PlatformView.GetPropertyString("RefreshingState"));

        coordinator.OnEvent(handler.PlatformView.NodeId, "pulltrigger", null);
        Assert.Equal(1, commandCount);

        refreshView.IsRefreshing = false;
        MirageRefreshViewHandler.Mapper.UpdateProperty(handler, refreshView, nameof(IRefreshView.IsRefreshing));
        refreshView.CommandParameter = "pull-mode";
        MirageRefreshViewHandler.Mapper.UpdateProperty(handler, refreshView, nameof(RefreshView.CommandParameter));

        coordinator.OnEvent(handler.PlatformView.NodeId, "pullstate", """{"active":true,"armed":true,"distance":96}""");
        Assert.Equal("true", handler.PlatformView.GetPropertyString("PullActiveState"));
        Assert.Equal("true", handler.PlatformView.GetPropertyString("PullArmedState"));
        Assert.Equal("96", handler.PlatformView.GetPropertyString("PullDistanceState"));

        coordinator.OnEvent(handler.PlatformView.NodeId, "pulltrigger", null);
        AssertEventually(() => refreshView.IsRefreshing && commandCount == 2);
        Assert.Equal("pull-mode", lastParameter);
    }

    [Fact]
    public void RefreshViewHandler_DisabledOrRefreshDisabled_GatesTriggerAndPullConfiguration()
    {
        var commandCount = 0;
        var runtime = new RefreshRuntimeProbe();
        var refreshView = CreateRefreshView(CreatePlainContent());
        refreshView.Command = new Command(() => commandCount++);

        var (handler, _, coordinator, _) = CreateAttachedHandler(refreshView, runtime: runtime);
        var triggerButton = FindNodeByAttribute(handler.PlatformView, "data-maui-rv-trigger", "true");

        refreshView.IsRefreshEnabled = false;
        MirageRefreshViewHandler.Mapper.UpdateProperty(handler, refreshView, nameof(IRefreshView.IsRefreshEnabled));

        Assert.False(runtime.LastConfigureEnabled);
        Assert.Equal("false", handler.PlatformView.GetPropertyString("EnabledState"));
        Assert.Equal("disabled", triggerButton.Attributes["disabled"]);
        coordinator.OnEvent(triggerButton.NodeId, "click", null);
        coordinator.OnEvent(handler.PlatformView.NodeId, "pulltrigger", null);
        Assert.False(refreshView.IsRefreshing);
        Assert.Equal(0, commandCount);

        refreshView.IsRefreshEnabled = true;
        MirageRefreshViewHandler.Mapper.UpdateProperty(handler, refreshView, nameof(IRefreshView.IsRefreshEnabled));
        Assert.True(runtime.LastConfigureEnabled);

        refreshView.IsEnabled = false;
        MirageRefreshViewHandler.Mapper.UpdateProperty(handler, refreshView, nameof(IView.IsEnabled));

        Assert.False(runtime.LastConfigureEnabled);
        Assert.Equal("false", handler.PlatformView.GetPropertyString("EnabledState"));
        coordinator.OnEvent(triggerButton.NodeId, "click", null);
        coordinator.OnEvent(handler.PlatformView.NodeId, "pulltrigger", null);
        Assert.False(refreshView.IsRefreshing);
        Assert.Equal(0, commandCount);
    }

    [Fact]
    public void RefreshViewHandler_Disconnect_ClearsPullObservation()
    {
        var runtime = new RefreshRuntimeProbe();
        var refreshView = CreateRefreshView(CreatePlainContent());
        var (handler, _, _, _) = CreateAttachedHandler(refreshView, runtime: runtime);
        var ownerNodeId = handler.PlatformView.NodeId;

        ((IElementHandler)handler).DisconnectHandler();

        Assert.False(runtime.LastConfigureEnabled);
        Assert.Equal(ownerNodeId, runtime.LastConfiguredOwnerNodeId);
    }

    private static (MirageRefreshViewHandler Handler, RefreshView RefreshView, MirageRenderCoordinator Coordinator, RefreshRuntimeProbe Runtime) CreateAttachedHandler(
        RefreshView refreshView,
        MirageRenderCoordinator? coordinator = null,
        RefreshRuntimeProbe? runtime = null)
    {
        coordinator ??= new MirageRenderCoordinator(new BrowserDispatcher());
        runtime ??= new RefreshRuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton<IMirageRenderCoordinator>(coordinator);
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler(typeof(RefreshView), typeof(MirageRefreshViewHandler));
            handlers.AddHandler(typeof(ScrollView), typeof(MirageScrollViewHandler));
            handlers.AddHandler(typeof(CollectionView), typeof(MirageCollectionViewHandler));
            handlers.AddHandler(typeof(VerticalStackLayout), typeof(MirageLayoutHandler));
            handlers.AddHandler(typeof(Label), typeof(MirageLabelHandler));
            handlers.AddHandler(typeof(Border), typeof(MirageBorderHandler));
        });

        var mauiApp = builder.Build();
        var mauiContext = new MauiContext(mauiApp.Services);

        var handler = new MirageRefreshViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(refreshView);
        handler.PlatformArrange(new Rect(0, 0, 280, 264));
        return (handler, refreshView, coordinator, runtime);
    }

    private static RefreshView CreateRefreshView(View? content)
    {
        return new RefreshView
        {
            Content = content,
            IsEnabled = true,
            IsRefreshEnabled = true,
            IsRefreshing = false
        };
    }

    private static CollectionView CreateCollectionContent()
    {
        return new CollectionView
        {
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical) { ItemSpacing = 6 },
            HeightRequest = 220,
            ItemsSource = new ObservableCollection<string>(
            [
                "Alpha",
                "Beta",
                "Gamma",
                "Delta",
                "Epsilon",
                "Zeta",
                "Eta",
                "Theta",
                "Iota",
                "Kappa"
            ]),
            ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label
                {
                    FontSize = 13,
                    TextColor = Colors.Black,
                    Margin = new Thickness(0)
                };
                label.SetBinding(Label.TextProperty, ".");

                return new Border
                {
                    Padding = new Thickness(10, 8),
                    Background = new SolidColorBrush(Color.FromArgb("#E2E8F0")),
                    Stroke = Colors.Transparent,
                    Content = label
                };
            })
        };
    }

    private static VerticalStackLayout CreatePlainContent()
    {
        var layout = new VerticalStackLayout
        {
            Spacing = 8,
            Padding = new Thickness(10, 8)
        };

        for (var i = 0; i < 16; i++)
        {
            layout.Children.Add(new Border
            {
                Background = new SolidColorBrush(i % 2 == 0 ? Color.FromArgb("#DBEAFE") : Color.FromArgb("#F8FAFC")),
                Stroke = Colors.Transparent,
                Padding = new Thickness(10, 8),
                Content = new Label
                {
                    FontSize = 13,
                    Text = $"Plain row {i + 1}",
                    TextColor = Colors.Black
                }
            });
        }

        return layout;
    }

    private static MiragePlatformView FindNodeByAttribute(MiragePlatformView root, string attributeName, string? expectedValue)
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

        throw new InvalidOperationException($"Unable to locate node with attribute '{attributeName}'='{expectedValue}'.");
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

    private static void AssertEventually(Func<bool> predicate, int timeoutMs = 1_000)
    {
        Assert.True(SpinWait.SpinUntil(predicate, timeoutMs), $"Condition did not become true within {timeoutMs}ms.");
    }

    private sealed class RefreshRuntimeProbe : IMirageRuntime
    {
        public List<PullConfigurationCall> PullConfigurations { get; } = [];

        public string? LastConfiguredOwnerNodeId => PullConfigurations.LastOrDefault()?.OwnerNodeId;

        public string? LastConfiguredSourceNodeId => PullConfigurations.LastOrDefault()?.SourceNodeId;

        public bool LastConfigureEnabled => PullConfigurations.LastOrDefault()?.Enabled ?? false;

        public double LastConfigureThreshold => PullConfigurations.LastOrDefault()?.Threshold ?? 0;

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
            PullConfigurations.Add(new PullConfigurationCall(ownerNodeId, sourceNodeId, enabled, threshold));
            return true;
        }

        public PullConfigurationCall LastEnabledConfiguration()
        {
            var call = PullConfigurations.LastOrDefault(static entry => entry.Enabled);
            Assert.NotNull(call);
            return call!;
        }
    }

    private sealed record PullConfigurationCall(string OwnerNodeId, string SourceNodeId, bool Enabled, double Threshold);
}
