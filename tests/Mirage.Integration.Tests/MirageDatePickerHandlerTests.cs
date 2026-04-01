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
public sealed class MirageDatePickerHandlerTests
{
    [Fact]
    public void DatePickerHandler_ProjectsBuiltInControlKind_AndHostStructure()
    {
        var (handler, _, _, _) = CreateAttachedHandler(new DatePicker
        {
            Date = new DateTime(2026, 4, 2),
            Format = "yyyy-MM-dd"
        });

        Assert.Equal(ComponentKind.DatePicker, handler.PlatformView.ControlKind);
        Assert.Equal("inline-flex", handler.PlatformView.GetStyleValue("display"));
        Assert.Equal("36px", handler.PlatformView.GetStyleValue("minHeight"));
        Assert.Empty(handler.PlatformView.Children);
        Assert.Equal("2026-04-02", handler.PlatformView.GetPropertyString("Value"));
    }

    [Fact]
    public void DatePickerHandler_GetDesiredSize_UsesLegacyFallbackAndConstraintClamp()
    {
        var (handler, _, _, _) = CreateAttachedHandler(new DatePicker
        {
            Date = new DateTime(2026, 4, 2)
        });

        var unconstrained = handler.GetDesiredSize(double.PositiveInfinity, double.PositiveInfinity);
        Assert.Equal(180, unconstrained.Width);
        Assert.Equal(36, unconstrained.Height);

        var constrained = handler.GetDesiredSize(120, 24);
        Assert.Equal(120, constrained.Width);
        Assert.Equal(24, constrained.Height);
    }

    [Fact]
    public void DatePickerHandler_GetDesiredSize_RespectsExplicitMinAndMaxRequests()
    {
        var (handler, _, _, _) = CreateAttachedHandler(new DatePicker
        {
            Date = new DateTime(2026, 4, 2),
            WidthRequest = 220,
            MaximumWidthRequest = 200,
            HeightRequest = 20,
            MinimumHeightRequest = 48
        });

        var size = handler.GetDesiredSize(double.PositiveInfinity, double.PositiveInfinity);
        Assert.Equal(200, size.Width);
        Assert.Equal(48, size.Height);
    }

    [Fact]
    public void DatePickerHandler_MapsCoreProperties_Deterministically()
    {
        var datePicker = new DatePicker
        {
            Date = new DateTime(2026, 4, 2, 18, 30, 0),
            MinimumDate = new DateTime(2026, 4, 1, 8, 0, 0),
            MaximumDate = new DateTime(2026, 4, 30, 22, 0, 0),
            Format = "D",
            Background = Brush.Bisque,
            TextColor = Colors.Crimson,
            CharacterSpacing = 1.5,
            FlowDirection = FlowDirection.RightToLeft,
            IsEnabled = false
        };

        var (handler, _, _, _) = CreateAttachedHandler(datePicker);

        Assert.Equal("2026-04-02", handler.PlatformView.GetPropertyString("Value"));
        Assert.Equal("2026-04-01", handler.PlatformView.GetPropertyString("Min"));
        Assert.Equal("2026-04-30", handler.PlatformView.GetPropertyString("Max"));
        Assert.Equal("D", handler.PlatformView.GetPropertyString("Format"));
        Assert.Equal("disabled", handler.PlatformView.GetPropertyString("Disabled"));
        Assert.Equal("rtl", handler.PlatformView.GetStyleValue("direction"));
        Assert.Equal("rgba(220,20,60,1)", handler.PlatformView.GetStyleValue("color"));
        Assert.Equal("rgba(255,228,196,1)", handler.PlatformView.GetStyleValue("backgroundColor"));
        Assert.Equal("1.5px", handler.PlatformView.GetStyleValue("letterSpacing"));
    }

    [Fact]
    public void DatePickerHandler_FormatProjection_UsesDeterministicBrowserSubset()
    {
        var datePicker = new DatePicker
        {
            Date = new DateTime(2026, 4, 2),
            Format = "MMMM d, yyyy"
        };

        var (handler, _, _, _) = CreateAttachedHandler(datePicker);

        Assert.Equal("MMMM d, yyyy", handler.PlatformView.GetPropertyString("Format"));
        Assert.Equal("2026-04-02", handler.PlatformView.GetPropertyString("Value"));

        datePicker.Format = "ddd";
        MirageDatePickerHandler.Mapper.UpdateProperty(handler, datePicker, nameof(IDatePicker.Format));

        Assert.Equal("ddd", handler.PlatformView.GetPropertyString("Format"));
        Assert.Equal("2026-04-02", handler.PlatformView.GetPropertyString("Value"));
    }

    [Fact]
    public void DatePickerHandler_RuntimeChange_ParsesAndUpdatesDate()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var (handler, datePicker, _, _) = CreateAttachedHandler(new DatePicker
        {
            Date = null,
            IsEnabled = true
        }, coordinator);

        coordinator.OnEvent(handler.PlatformView.NodeId, "change", "2026-04-03");
        Assert.Equal(new DateTime(2026, 4, 3), datePicker.Date);

        coordinator.OnEvent(handler.PlatformView.NodeId, "change", "invalid");
        Assert.Equal(new DateTime(2026, 4, 3), datePicker.Date);

        coordinator.OnEvent(handler.PlatformView.NodeId, "change", "");
        Assert.Null(datePicker.Date);
    }

    [Fact]
    public void DatePickerHandler_RuntimeChange_ClosesIsOpenWithoutRequiringBlur()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var (handler, datePicker, _, _) = CreateAttachedHandler(new DatePicker
        {
            Date = new DateTime(2026, 4, 2),
            IsEnabled = true,
            IsOpen = false
        }, coordinator);

        datePicker.IsOpen = true;
        Assert.True(datePicker.IsOpen);

        coordinator.OnEvent(handler.PlatformView.NodeId, "change", "2026-04-05");
        Assert.Equal(new DateTime(2026, 4, 5), datePicker.Date);
        Assert.False(datePicker.IsOpen);

        datePicker.IsOpen = true;
        Assert.True(datePicker.IsOpen);

        coordinator.OnEvent(handler.PlatformView.NodeId, "change", "2026-04-05");
        Assert.Equal(new DateTime(2026, 4, 5), datePicker.Date);
        Assert.False(datePicker.IsOpen);
    }

    [Fact]
    public void DatePickerHandler_DisabledState_BlocksRuntimeChanges()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var (handler, datePicker, _, _) = CreateAttachedHandler(new DatePicker
        {
            Date = null,
            IsEnabled = true
        }, coordinator);

        coordinator.OnEvent(handler.PlatformView.NodeId, "change", "2026-04-08");
        Assert.Equal(new DateTime(2026, 4, 8), datePicker.Date);

        datePicker.IsEnabled = false;
        MirageDatePickerHandler.Mapper.UpdateProperty(handler, datePicker, nameof(IView.IsEnabled));

        coordinator.OnEvent(handler.PlatformView.NodeId, "change", "2026-04-10");
        Assert.Equal(new DateTime(2026, 4, 8), datePicker.Date);
        Assert.Equal("disabled", handler.PlatformView.GetPropertyString("Disabled"));
        Assert.NotEqual("none", handler.PlatformView.GetStyleValue("pointerEvents"));
    }

    [Fact]
    public void DatePickerHandler_IsOpen_UsesProgrammaticOpenAndIgnoresFocusAsOpenSignal()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new DatePickerRuntimeProbe();
        coordinator.AttachRuntime(runtime);
        var (handler, datePicker, _, _) = CreateAttachedHandler(new DatePicker
        {
            IsEnabled = true,
            IsOpen = false
        }, coordinator, runtime);
        var baselineFocusCallCount = runtime.FocusCallCount;
        var baselineOpenPickerCallCount = runtime.OpenPickerCallCount;

        datePicker.IsOpen = true;

        Assert.Equal(baselineOpenPickerCallCount + 1, runtime.OpenPickerCallCount);
        Assert.Equal(handler.PlatformView.NodeId, runtime.LastOpenPickerNodeId);
        Assert.Equal(baselineFocusCallCount, runtime.FocusCallCount);

        datePicker.IsOpen = false;

        Assert.Equal(baselineFocusCallCount + 1, runtime.FocusCallCount);
        Assert.Equal(handler.PlatformView.NodeId, runtime.LastFocusedNodeId);
        Assert.False(runtime.LastFocusState);

        var openPickerCallsBeforeRuntimeFocus = runtime.OpenPickerCallCount;
        coordinator.OnEvent(handler.PlatformView.NodeId, "focus", null);
        Assert.False(datePicker.IsOpen);
        Assert.Equal(openPickerCallsBeforeRuntimeFocus, runtime.OpenPickerCallCount);

        coordinator.OnEvent(handler.PlatformView.NodeId, "blur", null);
        Assert.False(datePicker.IsOpen);
    }

    [Fact]
    public void DatePickerHandler_IsOpen_RevertsWhenRuntimeOpenFails()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new DatePickerRuntimeProbe
        {
            OpenPickerResult = false
        };
        coordinator.AttachRuntime(runtime);

        var (handler, datePicker, _, _) = CreateAttachedHandler(new DatePicker
        {
            IsOpen = false
        }, coordinator, runtime);

        var baselineOpenPickerCalls = runtime.OpenPickerCallCount;
        datePicker.IsOpen = true;

        Assert.True(runtime.OpenPickerCallCount >= baselineOpenPickerCalls + 1);
        Assert.Equal(handler.PlatformView.NodeId, runtime.LastOpenPickerNodeId);
        AssertEventually(() => !datePicker.IsOpen);
    }

    [Fact]
    public void DatePickerHandler_IsOpen_DoesNotRollbackBeforeDeferredRetry()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new DatePickerRuntimeProbe();
        runtime.EnqueueOpenPickerResult(false);
        runtime.EnqueueOpenPickerResult(true);
        coordinator.AttachRuntime(runtime);

        var (handler, datePicker, _, _) = CreateAttachedHandler(new DatePicker
        {
            IsOpen = false
        }, coordinator, runtime);

        datePicker.IsOpen = true;
        Assert.True(datePicker.IsOpen);
        AssertEventually(() => runtime.OpenPickerCallCount >= 2);
        Assert.Equal(handler.PlatformView.NodeId, runtime.LastOpenPickerNodeId);
        Assert.True(datePicker.IsOpen);
    }

    [Fact]
    public void DatePickerHandler_IsOpen_IgnoresStaleRetryAfterCloseAndReopen()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new DatePickerRuntimeProbe();
        runtime.EnqueueOpenPickerResult(false);
        runtime.EnqueueOpenPickerResult(true);
        coordinator.AttachRuntime(runtime);

        var (_, datePicker, _, _) = CreateAttachedHandler(new DatePicker
        {
            IsOpen = false
        }, coordinator, runtime);

        datePicker.IsOpen = true;
        Assert.True(datePicker.IsOpen);
        Assert.Equal(1, runtime.OpenPickerCallCount);

        datePicker.IsOpen = false;
        Assert.False(datePicker.IsOpen);

        datePicker.IsOpen = true;
        Assert.True(datePicker.IsOpen);
        Assert.Equal(2, runtime.OpenPickerCallCount);

        Thread.Sleep(75);
        Assert.True(datePicker.IsOpen);
        Assert.Equal(2, runtime.OpenPickerCallCount);
    }

    [Fact]
    public void DatePickerHandler_IsOpen_RetriesAfterPriorOpenFailure()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new DatePickerRuntimeProbe
        {
            OpenPickerResult = false
        };
        coordinator.AttachRuntime(runtime);

        var (handler, datePicker, _, _) = CreateAttachedHandler(new DatePicker
        {
            IsOpen = false
        }, coordinator, runtime);

        datePicker.IsOpen = true;
        AssertEventually(() => !datePicker.IsOpen);
        var failedAttemptCalls = runtime.OpenPickerCallCount;
        Assert.True(failedAttemptCalls > 0);

        runtime.OpenPickerResult = true;
        datePicker.IsOpen = true;

        Assert.True(runtime.OpenPickerCallCount > failedAttemptCalls);
        Assert.Equal(handler.PlatformView.NodeId, runtime.LastOpenPickerNodeId);
    }

    [Fact]
    public void DatePickerHandler_IsOpen_DisabledControlRejectsOpenRequest()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new DatePickerRuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var (_, datePicker, _, _) = CreateAttachedHandler(new DatePicker
        {
            IsEnabled = false,
            IsOpen = false
        }, coordinator, runtime);
        var baselineOpenPickerCalls = runtime.OpenPickerCallCount;

        datePicker.IsOpen = true;

        Assert.False(datePicker.IsOpen);
        Assert.Equal(baselineOpenPickerCalls, runtime.OpenPickerCallCount);
    }

    [Fact]
    public void DatePickerHandler_FlowDirection_ProjectsDirectionStyle()
    {
        var datePicker = new DatePicker
        {
            FlowDirection = FlowDirection.RightToLeft
        };

        var (handler, _, _, _) = CreateAttachedHandler(datePicker);
        Assert.Equal("rtl", handler.PlatformView.GetStyleValue("direction"));

        datePicker.FlowDirection = FlowDirection.LeftToRight;
        MirageDatePickerHandler.Mapper.UpdateProperty(handler, datePicker, nameof(IView.FlowDirection));
        Assert.Equal("ltr", handler.PlatformView.GetStyleValue("direction"));
    }

    private static (MirageDatePickerHandler Handler, DatePicker DatePicker, MirageRenderCoordinator Coordinator, DatePickerRuntimeProbe Runtime) CreateAttachedHandler(
        DatePicker datePicker,
        MirageRenderCoordinator? coordinator = null,
        DatePickerRuntimeProbe? runtime = null)
    {
        coordinator ??= new MirageRenderCoordinator(new BrowserDispatcher());
        runtime ??= new DatePickerRuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var services = new ServiceCollection()
            .AddSingleton<IMirageRenderCoordinator>(coordinator)
            .BuildServiceProvider();
        var mauiContext = new MauiContext(services);

        var handler = new MirageDatePickerHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(datePicker);
        return (handler, datePicker, coordinator, runtime);
    }

    private static void AssertEventually(Func<bool> predicate, int timeoutMs = 1_000)
    {
        Assert.True(SpinWait.SpinUntil(predicate, timeoutMs), $"Condition did not become true within {timeoutMs}ms.");
    }

    private sealed class DatePickerRuntimeProbe : IMirageRuntime
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
