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
public sealed class MirageTimePickerHandlerTests
{
    [Fact]
    public void TimePickerHandler_ProjectsBuiltInControlKind_AndHostStructure()
    {
        var (handler, _, _, _) = CreateAttachedHandler(new TimePicker
        {
            Time = new TimeSpan(9, 30, 0),
            Format = "HH:mm"
        });

        Assert.Equal(ComponentKind.TimePicker, handler.PlatformView.ControlKind);
        Assert.Equal("inline-flex", handler.PlatformView.GetStyleValue("display"));
        Assert.Equal("36px", handler.PlatformView.GetStyleValue("minHeight"));
        Assert.Empty(handler.PlatformView.Children);
        Assert.Equal("09:30", handler.PlatformView.GetPropertyString("Value"));
        Assert.Equal("60", handler.PlatformView.GetPropertyString("Step"));
    }

    [Fact]
    public void TimePickerHandler_MapsCoreProperties_Deterministically()
    {
        var timePicker = new TimePicker
        {
            Time = new TimeSpan(4, 15, 26),
            Format = "hh:mm",
            Background = Brush.Bisque,
            TextColor = Colors.Crimson,
            CharacterSpacing = 1.5,
            FlowDirection = FlowDirection.RightToLeft,
            IsEnabled = false
        };

        var (handler, _, _, _) = CreateAttachedHandler(timePicker);

        Assert.Equal("04:15", handler.PlatformView.GetPropertyString("Value"));
        Assert.Equal("60", handler.PlatformView.GetPropertyString("Step"));
        Assert.Equal("hh:mm", handler.PlatformView.GetPropertyString("Format"));
        Assert.Equal("disabled", handler.PlatformView.GetPropertyString("Disabled"));
        Assert.Equal("rtl", handler.PlatformView.GetStyleValue("direction"));
        Assert.Equal("rgba(220,20,60,1)", handler.PlatformView.GetStyleValue("color"));
        Assert.Equal("rgba(255,228,196,1)", handler.PlatformView.GetStyleValue("backgroundColor"));
        Assert.Equal("1.5px", handler.PlatformView.GetStyleValue("letterSpacing"));
    }

    [Fact]
    public void TimePickerHandler_FormatProjection_UsesDeterministicSecondsSubset()
    {
        var timePicker = new TimePicker
        {
            Time = new TimeSpan(12, 34, 56),
            Format = "HH:mm:ss"
        };

        var (handler, _, _, _) = CreateAttachedHandler(timePicker);

        Assert.Equal("1", handler.PlatformView.GetPropertyString("Step"));
        Assert.Equal("12:34:56", handler.PlatformView.GetPropertyString("Value"));

        timePicker.Format = "mm:ss";
        MirageTimePickerHandler.Mapper.UpdateProperty(handler, timePicker, nameof(ITimePicker.Format));

        Assert.Equal("1", handler.PlatformView.GetPropertyString("Step"));
        Assert.Equal("12:34:56", handler.PlatformView.GetPropertyString("Value"));
    }

    [Fact]
    public void TimePickerHandler_RuntimeChange_ParsesAndUpdatesTime()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var (handler, timePicker, _, _) = CreateAttachedHandler(new TimePicker
        {
            Time = null,
            IsEnabled = true
        }, coordinator);

        coordinator.OnEvent(handler.PlatformView.NodeId, "change", "07:45");
        Assert.Equal(new TimeSpan(7, 45, 0), timePicker.Time);

        coordinator.OnEvent(handler.PlatformView.NodeId, "change", "07:45:33");
        Assert.Equal(new TimeSpan(7, 45, 33), timePicker.Time);

        coordinator.OnEvent(handler.PlatformView.NodeId, "change", "7:5");
        Assert.Equal(new TimeSpan(7, 45, 33), timePicker.Time);

        coordinator.OnEvent(handler.PlatformView.NodeId, "change", "invalid");
        Assert.Equal(new TimeSpan(7, 45, 33), timePicker.Time);

        coordinator.OnEvent(handler.PlatformView.NodeId, "change", "");
        Assert.Null(timePicker.Time);
    }

    [Fact]
    public void TimePickerHandler_RuntimeChange_ClosesIsOpenWithoutRequiringBlur()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var (handler, timePicker, _, _) = CreateAttachedHandler(new TimePicker
        {
            Time = new TimeSpan(8, 0, 0),
            IsEnabled = true,
            IsOpen = false
        }, coordinator);

        timePicker.IsOpen = true;
        Assert.True(timePicker.IsOpen);

        coordinator.OnEvent(handler.PlatformView.NodeId, "change", "08:15");
        Assert.Equal(new TimeSpan(8, 15, 0), timePicker.Time);
        Assert.False(timePicker.IsOpen);

        timePicker.IsOpen = true;
        Assert.True(timePicker.IsOpen);

        // Same-value commits should also close open state even when time doesn't change.
        coordinator.OnEvent(handler.PlatformView.NodeId, "change", "08:15");
        Assert.Equal(new TimeSpan(8, 15, 0), timePicker.Time);
        Assert.False(timePicker.IsOpen);
    }

    [Fact]
    public void TimePickerHandler_DisabledState_BlocksRuntimeChanges()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var (handler, timePicker, _, _) = CreateAttachedHandler(new TimePicker
        {
            Time = null,
            IsEnabled = true
        }, coordinator);

        coordinator.OnEvent(handler.PlatformView.NodeId, "change", "08:00");
        Assert.Equal(new TimeSpan(8, 0, 0), timePicker.Time);

        timePicker.IsEnabled = false;
        MirageTimePickerHandler.Mapper.UpdateProperty(handler, timePicker, nameof(IView.IsEnabled));

        coordinator.OnEvent(handler.PlatformView.NodeId, "change", "10:00");
        Assert.Equal(new TimeSpan(8, 0, 0), timePicker.Time);
        Assert.Equal("disabled", handler.PlatformView.GetPropertyString("Disabled"));
    }

    [Fact]
    public void TimePickerHandler_IsOpen_UsesProgrammaticOpenAndIgnoresFocusAsOpenSignal()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new TimePickerRuntimeProbe();
        coordinator.AttachRuntime(runtime);
        var (handler, timePicker, _, _) = CreateAttachedHandler(new TimePicker
        {
            IsEnabled = true,
            IsOpen = false
        }, coordinator, runtime);
        var baselineFocusCallCount = runtime.FocusCallCount;
        var baselineOpenPickerCallCount = runtime.OpenPickerCallCount;

        timePicker.IsOpen = true;

        Assert.Equal(baselineOpenPickerCallCount + 1, runtime.OpenPickerCallCount);
        Assert.Equal(handler.PlatformView.NodeId, runtime.LastOpenPickerNodeId);
        Assert.Equal(baselineFocusCallCount, runtime.FocusCallCount);

        timePicker.IsOpen = false;

        Assert.Equal(baselineFocusCallCount + 1, runtime.FocusCallCount);
        Assert.Equal(handler.PlatformView.NodeId, runtime.LastFocusedNodeId);
        Assert.False(runtime.LastFocusState);

        var openPickerCallsBeforeRuntimeFocus = runtime.OpenPickerCallCount;
        coordinator.OnEvent(handler.PlatformView.NodeId, "focus", null);
        Assert.False(timePicker.IsOpen);
        Assert.Equal(openPickerCallsBeforeRuntimeFocus, runtime.OpenPickerCallCount);

        coordinator.OnEvent(handler.PlatformView.NodeId, "blur", null);
        Assert.False(timePicker.IsOpen);
    }

    [Fact]
    public void TimePickerHandler_IsOpen_RevertsWhenRuntimeOpenFails()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new TimePickerRuntimeProbe
        {
            OpenPickerResult = false
        };
        coordinator.AttachRuntime(runtime);

        var (handler, timePicker, _, _) = CreateAttachedHandler(new TimePicker
        {
            IsOpen = false
        }, coordinator, runtime);

        var baselineOpenPickerCalls = runtime.OpenPickerCallCount;
        timePicker.IsOpen = true;

        Assert.True(runtime.OpenPickerCallCount >= baselineOpenPickerCalls + 1);
        Assert.Equal(handler.PlatformView.NodeId, runtime.LastOpenPickerNodeId);
        AssertEventually(() => !timePicker.IsOpen);
    }

    [Fact]
    public void TimePickerHandler_IsOpen_DoesNotRollbackBeforeDeferredRetry()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new TimePickerRuntimeProbe();
        runtime.EnqueueOpenPickerResult(false);
        runtime.EnqueueOpenPickerResult(true);
        coordinator.AttachRuntime(runtime);

        var (handler, timePicker, _, _) = CreateAttachedHandler(new TimePicker
        {
            IsOpen = false
        }, coordinator, runtime);

        timePicker.IsOpen = true;
        Assert.True(timePicker.IsOpen);
        AssertEventually(() => runtime.OpenPickerCallCount >= 2);
        Assert.Equal(handler.PlatformView.NodeId, runtime.LastOpenPickerNodeId);
        Assert.True(timePicker.IsOpen);
    }

    [Fact]
    public void TimePickerHandler_IsOpen_IgnoresStaleRetryAfterCloseAndReopen()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new TimePickerRuntimeProbe();
        runtime.EnqueueOpenPickerResult(false);
        runtime.EnqueueOpenPickerResult(true);
        coordinator.AttachRuntime(runtime);

        var (_, timePicker, _, _) = CreateAttachedHandler(new TimePicker
        {
            IsOpen = false
        }, coordinator, runtime);

        timePicker.IsOpen = true;
        Assert.True(timePicker.IsOpen);
        Assert.Equal(1, runtime.OpenPickerCallCount);

        timePicker.IsOpen = false;
        Assert.False(timePicker.IsOpen);

        timePicker.IsOpen = true;
        Assert.True(timePicker.IsOpen);
        Assert.Equal(2, runtime.OpenPickerCallCount);

        // Wait past retry delay; stale callback from the first attempt must not mutate current state.
        Thread.Sleep(75);
        Assert.True(timePicker.IsOpen);
        Assert.Equal(2, runtime.OpenPickerCallCount);
    }

    [Fact]
    public void TimePickerHandler_IsOpen_RetriesAfterPriorOpenFailure()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new TimePickerRuntimeProbe
        {
            OpenPickerResult = false
        };
        coordinator.AttachRuntime(runtime);

        var (handler, timePicker, _, _) = CreateAttachedHandler(new TimePicker
        {
            IsOpen = false
        }, coordinator, runtime);

        timePicker.IsOpen = true;
        AssertEventually(() => !timePicker.IsOpen);
        var failedAttemptCalls = runtime.OpenPickerCallCount;
        Assert.True(failedAttemptCalls > 0);

        runtime.OpenPickerResult = true;
        timePicker.IsOpen = true;

        Assert.True(runtime.OpenPickerCallCount > failedAttemptCalls);
        Assert.Equal(handler.PlatformView.NodeId, runtime.LastOpenPickerNodeId);
    }

    [Fact]
    public void TimePickerHandler_IsOpen_DisabledControlRejectsOpenRequest()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new TimePickerRuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var (_, timePicker, _, _) = CreateAttachedHandler(new TimePicker
        {
            IsEnabled = false,
            IsOpen = false
        }, coordinator, runtime);
        var baselineOpenPickerCalls = runtime.OpenPickerCallCount;

        timePicker.IsOpen = true;

        Assert.False(timePicker.IsOpen);
        Assert.Equal(baselineOpenPickerCalls, runtime.OpenPickerCallCount);
    }

    [Fact]
    public void TimePickerHandler_FlowDirection_ProjectsDirectionStyle()
    {
        var timePicker = new TimePicker
        {
            FlowDirection = FlowDirection.RightToLeft
        };

        var (handler, _, _, _) = CreateAttachedHandler(timePicker);
        Assert.Equal("rtl", handler.PlatformView.GetStyleValue("direction"));

        timePicker.FlowDirection = FlowDirection.LeftToRight;
        MirageTimePickerHandler.Mapper.UpdateProperty(handler, timePicker, nameof(IView.FlowDirection));
        Assert.Equal("ltr", handler.PlatformView.GetStyleValue("direction"));
    }

    private static (MirageTimePickerHandler Handler, TimePicker TimePicker, MirageRenderCoordinator Coordinator, TimePickerRuntimeProbe Runtime) CreateAttachedHandler(
        TimePicker timePicker,
        MirageRenderCoordinator? coordinator = null,
        TimePickerRuntimeProbe? runtime = null)
    {
        coordinator ??= new MirageRenderCoordinator(new BrowserDispatcher());
        runtime ??= new TimePickerRuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageTimePickerHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(timePicker);
        return (handler, timePicker, coordinator, runtime);
    }

    private static void AssertEventually(Func<bool> predicate, int timeoutMs = 1_000)
    {
        Assert.True(SpinWait.SpinUntil(predicate, timeoutMs), $"Condition did not become true within {timeoutMs}ms.");
    }

    private sealed class TimePickerRuntimeProbe : IMirageRuntime
    {
        private readonly Queue<bool> _openPickerResultQueue = [];

        public int OpenPickerCallCount { get; private set; }

        public string? LastOpenPickerNodeId { get; private set; }

        public int FocusCallCount { get; private set; }

        public string? LastFocusedNodeId { get; private set; }

        public bool LastFocusState { get; private set; }

        public bool OpenPickerResult { get; set; } = true;

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
            return false;
        }

        public void Dispose()
        {
        }
    }
}
