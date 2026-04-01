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
using Xunit;

namespace Mirage.Integration.Tests;

[Collection("Mirage NonParallel")]
public sealed class MiragePickerHandlerTests
{
    [Fact]
    public void PickerHandler_ProjectsBuiltInControlKind_AndHostStructure()
    {
        var picker = new Picker
        {
            Title = "Choose shape"
        };
        picker.Items.Add("Rectangle");
        picker.Items.Add("RoundRectangle");

        var (handler, _, _, _) = CreateAttachedHandler(picker);

        Assert.Equal(ComponentKind.Picker, handler.PlatformView.ControlKind);
        Assert.Equal("inline-block", handler.PlatformView.GetStyleValue("display"));
        Assert.Equal("36px", handler.PlatformView.GetStyleValue("minHeight"));
        Assert.Equal("Choose shape", handler.PlatformView.GetPropertyString("Title"));
        Assert.Equal(string.Empty, handler.PlatformView.GetPropertyString("SelectedValue"));
        Assert.Equal(3, handler.PlatformView.Children.Count);
    }

    [Fact]
    public void PickerHandler_GetDesiredSize_UsesLegacyFallbackAndConstraintClamp()
    {
        var picker = new Picker();
        picker.Items.Add("One");

        var (handler, _, _, _) = CreateAttachedHandler(picker);

        var unconstrained = handler.GetDesiredSize(double.PositiveInfinity, double.PositiveInfinity);
        Assert.Equal(180, unconstrained.Width);
        Assert.Equal(36, unconstrained.Height);

        var constrained = handler.GetDesiredSize(120, 24);
        Assert.Equal(120, constrained.Width);
        Assert.Equal(24, constrained.Height);
    }

    [Fact]
    public void PickerHandler_GetDesiredSize_RespectsExplicitMinAndMaxRequests()
    {
        var picker = new Picker
        {
            WidthRequest = 220,
            MaximumWidthRequest = 200,
            HeightRequest = 20,
            MinimumHeightRequest = 48
        };
        picker.Items.Add("One");

        var (handler, _, _, _) = CreateAttachedHandler(picker);

        var size = handler.GetDesiredSize(double.PositiveInfinity, double.PositiveInfinity);
        Assert.Equal(200, size.Width);
        Assert.Equal(48, size.Height);
    }

    [Fact]
    public void PickerHandler_MapsCoreProperties_Deterministically()
    {
        var picker = new Picker
        {
            Title = "Choose shape",
            TitleColor = Colors.Silver,
            TextColor = Colors.Black,
            Background = Brush.Bisque,
            CharacterSpacing = 1.5,
            HorizontalTextAlignment = TextAlignment.End,
            VerticalTextAlignment = TextAlignment.End,
            FontSize = 18,
            IsEnabled = false
        };
        picker.Items.Add("Rectangle");
        picker.Items.Add("RoundRectangle");

        var (handler, _, _, _) = CreateAttachedHandler(picker);

        Assert.Equal("Choose shape", handler.PlatformView.GetPropertyString("Title"));
        Assert.Equal("disabled", handler.PlatformView.GetPropertyString("Disabled"));
        Assert.Equal("default", handler.PlatformView.GetStyleValue("cursor"));
        Assert.Equal("rgba(255,228,196,1)", handler.PlatformView.GetStyleValue("backgroundColor"));
        Assert.Equal("rgba(192,192,192,1)", handler.PlatformView.GetStyleValue("color"));
        Assert.Equal("1.5px", handler.PlatformView.GetStyleValue("letterSpacing"));
        Assert.Equal("18px", handler.PlatformView.GetStyleValue("fontSize"));
        Assert.Equal("end", handler.PlatformView.GetStyleValue("textAlign"));
        Assert.Equal("6px", handler.PlatformView.GetStyleValue("paddingTop"));
        Assert.Equal("0px", handler.PlatformView.GetStyleValue("paddingBottom"));
    }

    [Fact]
    public void PickerHandler_SemanticsDescription_TakesPrecedenceOverTitleAccessibilityLabel()
    {
        var picker = new Picker
        {
            Title = "Choose planet"
        };
        picker.Items.Add("Mercury");
        SemanticProperties.SetDescription(picker, "Planet selector");
        SemanticProperties.SetHint(picker, "Opens the available planets");

        var (handler, _, _, _) = CreateAttachedHandler(picker);

        Assert.Equal("Planet selector", handler.PlatformView.GetPropertyString("AriaLabel"));
        Assert.Equal("Opens the available planets", handler.PlatformView.GetPropertyString("AriaDescription"));
        Assert.Null(handler.PlatformView.GetPropertyString("Title"));

        SemanticProperties.SetDescription(picker, null);
        SemanticProperties.SetHint(picker, null);
        MiragePickerHandler.Mapper.UpdateProperty(handler, picker, nameof(IView.Semantics));

        Assert.Null(handler.PlatformView.GetPropertyString("AriaLabel"));
        Assert.Null(handler.PlatformView.GetPropertyString("AriaDescription"));
        Assert.Equal("Choose planet", handler.PlatformView.GetPropertyString("Title"));
    }

    [Fact]
    public void PickerHandler_RuntimeChange_UpdatesSelectedIndexAndClosesIsOpen()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var picker = new Picker
        {
            Title = "Choose shape",
            IsOpen = false,
            IsEnabled = true
        };
        picker.Items.Add("Rectangle");
        picker.Items.Add("RoundRectangle");

        var (handler, _, _, _) = CreateAttachedHandler(picker, coordinator);

        picker.IsOpen = true;
        Assert.True(picker.IsOpen);

        coordinator.OnEvent(handler.PlatformView.NodeId, "change", "1");
        Assert.Equal(1, picker.SelectedIndex);
        Assert.False(picker.IsOpen);

        picker.IsOpen = true;
        coordinator.OnEvent(handler.PlatformView.NodeId, "change", "1");
        Assert.Equal(1, picker.SelectedIndex);
        Assert.False(picker.IsOpen);
    }

    [Fact]
    public void PickerHandler_DisabledState_BlocksRuntimeChanges()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var picker = new Picker
        {
            Title = "Choose shape",
            IsEnabled = true
        };
        picker.Items.Add("Rectangle");
        picker.Items.Add("RoundRectangle");

        var (handler, _, _, _) = CreateAttachedHandler(picker, coordinator);

        coordinator.OnEvent(handler.PlatformView.NodeId, "change", "1");
        Assert.Equal(1, picker.SelectedIndex);

        picker.IsEnabled = false;
        MiragePickerHandler.Mapper.UpdateProperty(handler, picker, nameof(IView.IsEnabled));

        coordinator.OnEvent(handler.PlatformView.NodeId, "change", "0");
        Assert.Equal(1, picker.SelectedIndex);
        Assert.Equal("disabled", handler.PlatformView.GetPropertyString("Disabled"));
        Assert.NotEqual("none", handler.PlatformView.GetStyleValue("pointerEvents"));
    }

    [Fact]
    public void PickerHandler_IsOpen_UsesProgrammaticOpenAndIgnoresFocusAsOpenSignal()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new PickerRuntimeProbe();
        coordinator.AttachRuntime(runtime);
        var picker = new Picker
        {
            IsEnabled = true,
            IsOpen = false
        };
        picker.Items.Add("Rectangle");

        var (handler, _, _, _) = CreateAttachedHandler(picker, coordinator, runtime);
        var baselineFocusCallCount = runtime.FocusCallCount;
        var baselineOpenPickerCallCount = runtime.OpenPickerCallCount;

        picker.IsOpen = true;

        Assert.Equal(baselineOpenPickerCallCount + 1, runtime.OpenPickerCallCount);
        Assert.Equal(handler.PlatformView.NodeId, runtime.LastOpenPickerNodeId);
        Assert.Equal(baselineFocusCallCount, runtime.FocusCallCount);

        picker.IsOpen = false;

        Assert.Equal(baselineFocusCallCount + 1, runtime.FocusCallCount);
        Assert.Equal(handler.PlatformView.NodeId, runtime.LastFocusedNodeId);
        Assert.False(runtime.LastFocusState);

        var openPickerCallsBeforeRuntimeFocus = runtime.OpenPickerCallCount;
        coordinator.OnEvent(handler.PlatformView.NodeId, "focus", null);
        Assert.False(picker.IsOpen);
        Assert.Equal(openPickerCallsBeforeRuntimeFocus, runtime.OpenPickerCallCount);

        coordinator.OnEvent(handler.PlatformView.NodeId, "blur", null);
        Assert.False(picker.IsOpen);
    }

    [Fact]
    public void PickerHandler_IsOpen_RevertsWhenRuntimeOpenFails()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new PickerRuntimeProbe
        {
            OpenPickerResult = false
        };
        coordinator.AttachRuntime(runtime);
        var picker = new Picker
        {
            IsOpen = false
        };
        picker.Items.Add("Rectangle");

        var (handler, _, _, _) = CreateAttachedHandler(picker, coordinator, runtime);

        var baselineOpenPickerCalls = runtime.OpenPickerCallCount;
        picker.IsOpen = true;

        Assert.True(runtime.OpenPickerCallCount >= baselineOpenPickerCalls + 1);
        Assert.Equal(handler.PlatformView.NodeId, runtime.LastOpenPickerNodeId);
        AssertEventually(() => !picker.IsOpen);
    }

    [Fact]
    public void PickerHandler_FocusAndUnfocus_UseRuntimeSeams()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new PickerRuntimeProbe();
        coordinator.AttachRuntime(runtime);
        var picker = new Picker
        {
            IsEnabled = true,
            IsOpen = false
        };
        picker.Items.Add("Rectangle");

        var (handler, _, _, _) = CreateAttachedHandler(picker, coordinator, runtime);
        var baselineFocusCallCount = runtime.FocusCallCount;
        var baselineOpenPickerCallCount = runtime.OpenPickerCallCount;

        MiragePickerHandler.MapFocus(handler, picker, args: null);

        Assert.Equal(baselineFocusCallCount + 1, runtime.FocusCallCount);
        Assert.Equal(baselineOpenPickerCallCount + 1, runtime.OpenPickerCallCount);
        Assert.True(picker.IsOpen);

        MiragePickerHandler.MapUnfocus(handler, picker, args: null);

        Assert.Equal(baselineFocusCallCount + 2, runtime.FocusCallCount);
        Assert.False(runtime.LastFocusState);
        Assert.False(picker.IsOpen);
    }

    [Fact]
    public void PickerHandler_Focus_DoesNotOpenPickerWhenRuntimeFocusFails()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new PickerRuntimeProbe
        {
            FocusResult = false
        };
        coordinator.AttachRuntime(runtime);
        var picker = new Picker
        {
            IsEnabled = true,
            IsOpen = false
        };
        picker.Items.Add("Mercury");

        var (handler, _, _, _) = CreateAttachedHandler(picker, coordinator, runtime);
        var focusRequest = new FocusRequest();
        var baselineFocusCallCount = runtime.FocusCallCount;

        MiragePickerHandler.MapFocus(handler, picker, focusRequest);

        Assert.False(focusRequest.Result);
        Assert.Equal(baselineFocusCallCount + 1, runtime.FocusCallCount);
        Assert.Equal(0, runtime.OpenPickerCallCount);
        Assert.False(picker.IsOpen);
    }

    [Fact]
    public void PickerHandler_Focus_ReissuesOpenRequestThroughRetryStateMachine_WhenAlreadyOpen()
    {
        var coordinator = new QueuedPickerCoordinator();
        var runtime = new PickerRuntimeProbe();
        runtime.EnqueueOpenPickerResult(false);
        runtime.EnqueueOpenPickerResult(true);
        coordinator.AttachRuntime(runtime);

        var picker = new Picker
        {
            IsEnabled = true,
            IsOpen = false
        };
        picker.Items.Add("Mercury");

        var (handler, _, _, _) = CreateAttachedHandler(picker, coordinator, runtime);

        picker.IsOpen = true;

        Assert.Equal(1, runtime.OpenPickerCallCount);
        Assert.Equal(1, coordinator.PendingDelayedCount);
        Assert.True(picker.IsOpen);

        var baselineFocusCallCount = runtime.FocusCallCount;
        MiragePickerHandler.MapFocus(handler, picker, args: null);

        Assert.Equal(baselineFocusCallCount + 1, runtime.FocusCallCount);
        Assert.True(runtime.LastFocusState);
        Assert.Equal(2, runtime.OpenPickerCallCount);
        Assert.Equal(handler.PlatformView.NodeId, runtime.LastOpenPickerNodeId);
        Assert.Equal(1, coordinator.PendingDelayedCount);
        Assert.True(picker.IsOpen);

        coordinator.FlushNextDelayed();

        Assert.Equal(2, runtime.OpenPickerCallCount);
        Assert.True(picker.IsOpen);
    }

    [Fact]
    public void PickerHandler_ItemsSource_ItemDisplayBinding_AndSelectedItem_Roundtrip()
    {
        var source = new ObservableCollection<PickerOption>
        {
            new("Mercury", "Planet Mercury"),
            new("Venus", "Planet Venus")
        };

        var picker = new Picker
        {
            Title = "Choose planet",
            ItemsSource = source,
            ItemDisplayBinding = new Binding(nameof(PickerOption.Display)),
            SelectedItem = source[1]
        };

        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var (handler, _, _, _) = CreateAttachedHandler(picker, coordinator);

        Assert.Equal(3, handler.PlatformView.Children.Count);
        Assert.Equal("Planet Mercury", handler.PlatformView.Children[1].GetPropertyString("textContent"));
        Assert.Equal("Planet Venus", handler.PlatformView.Children[2].GetPropertyString("textContent"));
        Assert.Equal("1", handler.PlatformView.GetPropertyString("SelectedValue"));
        Assert.Same(source[1], picker.SelectedItem);

        picker.ItemDisplayBinding = new Binding(nameof(PickerOption.Name));
        Assert.Equal("Mercury", handler.PlatformView.Children[1].GetPropertyString("textContent"));
        Assert.Equal("Venus", handler.PlatformView.Children[2].GetPropertyString("textContent"));

        coordinator.OnEvent(handler.PlatformView.NodeId, "change", "0");
        Assert.Equal(0, picker.SelectedIndex);
        Assert.Same(source[0], picker.SelectedItem);

        var replacement = new ObservableCollection<PickerOption>
        {
            new("Earth", "Planet Earth"),
            new("Mars", "Planet Mars")
        };
        picker.ItemsSource = replacement;

        Assert.Equal("Earth", handler.PlatformView.Children[1].GetPropertyString("textContent"));
        Assert.Equal("Mars", handler.PlatformView.Children[2].GetPropertyString("textContent"));

        picker.SelectedItem = replacement[1];
        Assert.Equal(1, picker.SelectedIndex);
        Assert.Equal("1", handler.PlatformView.GetPropertyString("SelectedValue"));
        Assert.Same(replacement[1], picker.SelectedItem);
    }

    private static (MiragePickerHandler Handler, Picker Picker, MirageRenderCoordinator Coordinator, PickerRuntimeProbe Runtime) CreateAttachedHandler(
        Picker picker,
        MirageRenderCoordinator? coordinator = null,
        PickerRuntimeProbe? runtime = null)
    {
        coordinator ??= new MirageRenderCoordinator(new BrowserDispatcher());
        runtime ??= new PickerRuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MiragePickerHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(picker);
        return (handler, picker, coordinator, runtime);
    }

    private static (MiragePickerHandler Handler, Picker Picker, QueuedPickerCoordinator Coordinator, PickerRuntimeProbe Runtime) CreateAttachedHandler(
        Picker picker,
        QueuedPickerCoordinator coordinator,
        PickerRuntimeProbe runtime)
    {
        coordinator.AttachRuntime(runtime);

        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MiragePickerHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(picker);
        return (handler, picker, coordinator, runtime);
    }

    private static void AssertEventually(Func<bool> predicate, int timeoutMs = 1_000)
    {
        Assert.True(SpinWait.SpinUntil(predicate, timeoutMs), $"Condition did not become true within {timeoutMs}ms.");
    }

    private sealed record PickerOption(string Name, string Display);

    private sealed class PickerRuntimeProbe : IMirageRuntime
    {
        private readonly Queue<bool> _openPickerResultQueue = [];

        public int OpenPickerCallCount { get; private set; }

        public string? LastOpenPickerNodeId { get; private set; }

        public int FocusCallCount { get; private set; }

        public string? LastFocusedNodeId { get; private set; }

        public bool LastFocusState { get; private set; }

        public bool OpenPickerResult { get; set; } = true;

        public bool FocusResult { get; set; } = true;

        public void EnqueueOpenPickerResult(bool result)
        {
            _openPickerResultQueue.Enqueue(result);
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
            OpenPickerCallCount++;
            LastOpenPickerNodeId = nodeId;
            if (_openPickerResultQueue.Count > 0)
            {
                return _openPickerResultQueue.Dequeue();
            }

            return OpenPickerResult;
        }

        public bool TrySetNodeFocus(string nodeId, bool isFocused)
        {
            FocusCallCount++;
            LastFocusedNodeId = nodeId;
            LastFocusState = isFocused;
            return FocusResult;
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

        public void Dispose()
        {
        }
    }

    private sealed class QueuedPickerCoordinator : IMirageRenderCoordinator
    {
        private readonly Queue<Action> _pendingDelayedActions = [];
        private IMirageRuntime? _runtime;
        private int _nodeIdCounter;

        public int PendingDelayedCount => _pendingDelayedActions.Count;

        public void AttachRuntime(IMirageRuntime runtime)
        {
            _runtime = runtime;
            _runtime.SetEventSink(this);
        }

        public void DetachRuntime()
        {
            if (_runtime is not null)
            {
                _runtime.SetEventSink(null);
            }

            _runtime = null;
            _pendingDelayedActions.Clear();
        }

        public void Register(MiragePlatformView platformView, IMirageRuntimeEventTarget? eventTarget)
        {
            if (string.IsNullOrWhiteSpace(platformView.NodeId))
            {
                var localId = Interlocked.Increment(ref _nodeIdCounter);
                platformView.AssignNodeId($"queued-picker-node-{localId:D4}");
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
            return _runtime?.TryRequestNodePickerOpen(nodeId) ?? false;
        }

        public bool TrySetNodeFocus(string nodeId, bool isFocused)
        {
            return _runtime?.TrySetNodeFocus(nodeId, isFocused) ?? false;
        }

        public bool TryGetNodeTextSelection(string nodeId, out int selectionStart, out int selectionEnd)
        {
            if (_runtime is not null)
            {
                return _runtime.TryGetNodeTextSelection(nodeId, out selectionStart, out selectionEnd);
            }

            selectionStart = 0;
            selectionEnd = 0;
            return false;
        }

        public bool TrySetNodeTextSelection(string nodeId, int selectionStart, int selectionEnd)
        {
            return _runtime?.TrySetNodeTextSelection(nodeId, selectionStart, selectionEnd) ?? false;
        }

        public void OnEvent(string nodeId, string eventName, string? payload)
        {
            _ = nodeId;
            _ = eventName;
            _ = payload;
        }

        public void FlushNextDelayed()
        {
            if (_pendingDelayedActions.Count == 0)
            {
                return;
            }

            var action = _pendingDelayedActions.Dequeue();
            action();
        }

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
