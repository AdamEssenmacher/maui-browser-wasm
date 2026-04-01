using System.IO;
using System.Numerics;
using System.Text.Json;
using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Mirage.Web.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Text;
using Microsoft.Maui.Handlers;
using Xunit;
using MauiGraphicsFont = Microsoft.Maui.Graphics.Font;
using MauiGraphicsImage = Microsoft.Maui.Graphics.IImage;

namespace Mirage.Integration.Tests;

[Collection("Mirage NonParallel")]
public sealed class MirageGraphicsViewHandlerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void GraphicsViewHandler_ProjectsBuiltInCanvasHost_AndRepaintsAfterArrange()
    {
        var coordinator = new CanvasCoordinatorProbe
        {
            DisplayScale = 2
        };
        var graphicsView = new TestGraphicsView
        {
            Drawable = new SolidRectangleDrawable(Colors.Crimson),
            Background = Brush.Bisque
        };

        var (handler, _, _) = CreateAttachedHandler(graphicsView, coordinator);
        handler.PlatformArrange(new Rect(0, 0, 120, 80));
        coordinator.FlushAllDelayed();

        Assert.Equal(ComponentKind.GraphicsView, handler.PlatformView.ControlKind);
        Assert.Equal("block", handler.PlatformView.GetStyleValue("display"));
        Assert.Equal("border-box", handler.PlatformView.GetStyleValue("boxSizing"));
        Assert.Equal("none", handler.PlatformView.GetStyleValue("touchAction"));
        Assert.Equal("rgba(255,228,196,1)", handler.PlatformView.GetStyleValue("backgroundColor"));
        Assert.Contains("pointerdown", handler.PlatformView.Events);
        Assert.Contains("pointermove", handler.PlatformView.Events);
        Assert.Contains("pointerup", handler.PlatformView.Events);
        Assert.Contains("pointercancel", handler.PlatformView.Events);
        Assert.Contains("pointerenter", handler.PlatformView.Events);
        Assert.Contains("pointerleave", handler.PlatformView.Events);

        Assert.True(coordinator.ConfigureCanvasCallCount >= 1);
        Assert.Equal("mirage-graphics-node-0001", coordinator.LastConfiguredNodeId);
        Assert.Equal(120, coordinator.LastConfiguredWidth, 3);
        Assert.Equal(80, coordinator.LastConfiguredHeight, 3);
        Assert.True(coordinator.RenderCanvasCallCount >= 1);
        Assert.True(coordinator.RequestRenderCallCount >= 1);

        var payload = coordinator.GetLastCanvasPayload();
        Assert.NotNull(payload);
        Assert.Equal(120, payload!.LogicalWidth, 3);
        Assert.Equal(80, payload.LogicalHeight, 3);
        Assert.Equal(2, payload.DisplayScale, 3);
        Assert.Contains(payload.Commands, static command => string.Equals(command.Type, "fill-rectangle", StringComparison.Ordinal));
    }

    [Fact]
    public void GraphicsViewHandler_GetDesiredSize_UsesCanvasFallbackAndConstraintClamp()
    {
        var (handler, _, _) = CreateAttachedHandler(new TestGraphicsView
        {
            Drawable = new SolidRectangleDrawable(Colors.DodgerBlue)
        });

        var unconstrained = handler.GetDesiredSize(double.PositiveInfinity, double.PositiveInfinity);
        Assert.Equal(300, unconstrained.Width);
        Assert.Equal(150, unconstrained.Height);

        var constrained = handler.GetDesiredSize(120, 90);
        Assert.Equal(120, constrained.Width);
        Assert.Equal(90, constrained.Height);
    }

    [Fact]
    public void GraphicsViewHandler_GetDesiredSize_RespectsExplicitMinAndMaxRequests()
    {
        var (handler, _, _) = CreateAttachedHandler(new TestGraphicsView
        {
            WidthRequest = 360,
            MaximumWidthRequest = 280,
            HeightRequest = 100,
            MinimumHeightRequest = 180
        });

        var size = handler.GetDesiredSize(double.PositiveInfinity, double.PositiveInfinity);
        Assert.Equal(280, size.Width);
        Assert.Equal(180, size.Height);
    }

    [Fact]
    public void GraphicsViewHandler_RepaintsForDrawableBackgroundFlowDirectionAndInvalidate()
    {
        var coordinator = new CanvasCoordinatorProbe();
        var graphicsView = new TestGraphicsView
        {
            Drawable = new SolidRectangleDrawable(Colors.SeaGreen),
            Background = Brush.White
        };

        var (handler, _, _) = CreateAttachedHandler(graphicsView, coordinator);
        handler.PlatformArrange(new Rect(0, 0, 100, 60));
        coordinator.FlushAllDelayed();
        var baselineRenderCount = coordinator.RenderCanvasCallCount;

        graphicsView.Background = Brush.BlanchedAlmond;
        MirageGraphicsViewHandler.Mapper.UpdateProperty(handler, graphicsView, nameof(IView.Background));
        coordinator.FlushAllDelayed();
        Assert.Equal("rgba(255,235,205,1)", handler.PlatformView.GetStyleValue("backgroundColor"));

        graphicsView.FlowDirection = FlowDirection.RightToLeft;
        MirageGraphicsViewHandler.Mapper.UpdateProperty(handler, graphicsView, nameof(IView.FlowDirection));
        coordinator.FlushAllDelayed();
        Assert.Equal("rtl", handler.PlatformView.GetStyleValue("direction"));

        graphicsView.Drawable = new TriangleDrawable(Colors.MediumPurple);
        MirageGraphicsViewHandler.Mapper.UpdateProperty(handler, graphicsView, nameof(IGraphicsView.Drawable));
        coordinator.FlushAllDelayed();

        MirageGraphicsViewHandler.CommandMapper.Invoke(handler, graphicsView, nameof(IGraphicsView.Invalidate), null);
        coordinator.FlushAllDelayed();

        Assert.True(coordinator.RenderCanvasCallCount >= baselineRenderCount + 4);
        var payload = coordinator.GetLastCanvasPayload();
        Assert.NotNull(payload);
        Assert.Contains(payload!.Commands, static command => string.Equals(command.Type, "path-fill", StringComparison.Ordinal));
    }

    [Fact]
    public void GraphicsViewHandler_CoalescesRepeatedInvalidationsIntoSingleQueuedRepaint()
    {
        var coordinator = new CanvasCoordinatorProbe();
        var graphicsView = new TestGraphicsView
        {
            Drawable = new SolidRectangleDrawable(Colors.DarkOrange)
        };

        var (handler, _, _) = CreateAttachedHandler(graphicsView, coordinator);
        handler.PlatformArrange(new Rect(0, 0, 140, 90));
        coordinator.FlushAllDelayed();
        var baselineRenderCount = coordinator.RenderCanvasCallCount;

        MirageGraphicsViewHandler.CommandMapper.Invoke(handler, graphicsView, nameof(IGraphicsView.Invalidate), null);
        MirageGraphicsViewHandler.CommandMapper.Invoke(handler, graphicsView, nameof(IGraphicsView.Invalidate), null);
        MirageGraphicsViewHandler.CommandMapper.Invoke(handler, graphicsView, nameof(IGraphicsView.Invalidate), null);

        Assert.Equal(1, coordinator.PendingDelayedCount);
        coordinator.FlushAllDelayed();

        Assert.Equal(baselineRenderCount + 1, coordinator.RenderCanvasCallCount);
    }

    [Fact]
    public void GraphicsViewHandler_RetriesCanvasRepaintUntilRuntimeCanvasIsReady()
    {
        var coordinator = new CanvasCoordinatorProbe
        {
            ConfigureFailuresRemaining = 2
        };
        var graphicsView = new TestGraphicsView
        {
            Drawable = new SolidRectangleDrawable(Colors.DeepSkyBlue)
        };

        var (handler, _, _) = CreateAttachedHandler(graphicsView, coordinator);
        handler.PlatformArrange(new Rect(0, 0, 160, 90));
        coordinator.FlushAllDelayed();

        Assert.Equal(3, coordinator.ConfigureCanvasCallCount);
        Assert.Equal(1, coordinator.RenderCanvasCallCount);
        Assert.Equal(160, coordinator.LastConfiguredWidth, 3);
        Assert.Equal(90, coordinator.LastConfiguredHeight, 3);
    }

    [Fact]
    public void GraphicsViewHandler_BacksOffToSlowRetry_AndRecoversWhenRuntimeBecomesReadyLater()
    {
        var coordinator = new CanvasCoordinatorProbe
        {
            ConfigureFailuresRemaining = MirageGraphicsViewHandler.MaxRepaintRetryAttempts
        };
        var graphicsView = new TestGraphicsView
        {
            Drawable = new SolidRectangleDrawable(Colors.DeepSkyBlue)
        };

        var (handler, _, _) = CreateAttachedHandler(graphicsView, coordinator);
        handler.PlatformArrange(new Rect(0, 0, 160, 90));
        for (var attempt = 0; attempt < MirageGraphicsViewHandler.MaxRepaintRetryAttempts; attempt++)
        {
            Assert.Equal(1, coordinator.PendingDelayedCount);
            coordinator.FlushNextDelayed();
        }

        Assert.Equal(MirageGraphicsViewHandler.MaxRepaintRetryAttempts, coordinator.ConfigureCanvasCallCount);
        Assert.Equal(0, coordinator.RenderCanvasCallCount);
        Assert.Equal(MirageGraphicsViewHandler.SlowRepaintRetryDelay, coordinator.LastDispatchDelay);
        Assert.Equal(1, coordinator.PendingDelayedCount);

        coordinator.ConfigureFailuresRemaining = 0;
        coordinator.FlushNextDelayed();

        Assert.Equal(MirageGraphicsViewHandler.MaxRepaintRetryAttempts + 1, coordinator.ConfigureCanvasCallCount);
        Assert.Equal(1, coordinator.RenderCanvasCallCount);
        Assert.Equal(0, coordinator.PendingDelayedCount);
    }

    [Fact]
    public void GraphicsViewControl_Invalidate_RedrawsViaPublicControlPath()
    {
        var coordinator = new CanvasCoordinatorProbe();
        var accent = Colors.Crimson;
        var graphicsView = new GraphicsView
        {
            Drawable = new MutableAccentDrawable(() => accent)
        };

        var (handler, _, _) = CreateAttachedHandler(graphicsView, coordinator);
        handler.PlatformArrange(new Rect(0, 0, 180, 120));
        coordinator.FlushAllDelayed();
        var baselineRenderCount = coordinator.RenderCanvasCallCount;

        accent = Colors.RoyalBlue;
        graphicsView.Invalidate();
        coordinator.FlushAllDelayed();

        Assert.Equal(baselineRenderCount + 1, coordinator.RenderCanvasCallCount);
    }

    [Fact]
    public void GraphicsViewHandler_RecordsWaveOneCanvasCommands()
    {
        var coordinator = new CanvasCoordinatorProbe();
        var graphicsView = new TestGraphicsView
        {
            Drawable = new CoverageDrawable()
        };

        var (handler, _, _) = CreateAttachedHandler(graphicsView, coordinator);
        handler.PlatformArrange(new Rect(0, 0, 160, 120));
        coordinator.FlushAllDelayed();

        var payload = coordinator.GetLastCanvasPayload();
        Assert.NotNull(payload);

        var commandTypes = payload!.Commands
            .Select(static command => command.Type)
            .Where(static type => !string.IsNullOrWhiteSpace(type))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("save", commandTypes);
        Assert.Contains("alpha", commandTypes);
        Assert.Contains("stroke-color", commandTypes);
        Assert.Contains("fill-color", commandTypes);
        Assert.Contains("font-color", commandTypes);
        Assert.Contains("stroke-size", commandTypes);
        Assert.Contains("line-cap", commandTypes);
        Assert.Contains("line-join", commandTypes);
        Assert.Contains("dash", commandTypes);
        Assert.Contains("translate", commandTypes);
        Assert.Contains("scale", commandTypes);
        Assert.Contains("rotate", commandTypes);
        Assert.Contains("rotate-at", commandTypes);
        Assert.Contains("matrix", commandTypes);
        Assert.Contains("clip-rect", commandTypes);
        Assert.Contains("line", commandTypes);
        Assert.Contains("rectangle", commandTypes);
        Assert.Contains("fill-rectangle", commandTypes);
        Assert.Contains("rounded-rectangle", commandTypes);
        Assert.Contains("fill-rounded-rectangle", commandTypes);
        Assert.Contains("ellipse", commandTypes);
        Assert.Contains("fill-ellipse", commandTypes);
        Assert.Contains("arc", commandTypes);
        Assert.Contains("fill-arc", commandTypes);
        Assert.Contains("path-stroke", commandTypes);
        Assert.Contains("path-fill", commandTypes);
        Assert.Contains("fill-paint", commandTypes);
        Assert.Contains("shadow", commandTypes);
        Assert.Contains("draw-string-point", commandTypes);
        Assert.Contains("draw-string-rect", commandTypes);
        Assert.Contains("restore", commandTypes);

        Assert.Contains(payload.Commands, static command => string.Equals(command.Type, "line-cap", StringComparison.Ordinal)
            && string.Equals(command.ValueText, "round", StringComparison.Ordinal));
        Assert.Contains(payload.Commands, static command => string.Equals(command.Type, "line-join", StringComparison.Ordinal)
            && string.Equals(command.ValueText, "bevel", StringComparison.Ordinal));
        Assert.Contains(payload.Commands, static command => string.Equals(command.Type, "font", StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(command.ValueText)
            && command.ValueText.Contains("px", StringComparison.Ordinal));
        Assert.Contains(payload.Commands, static command => string.Equals(command.Type, "draw-string-point", StringComparison.Ordinal)
            && string.Equals(command.ValueText, "Point", StringComparison.Ordinal));
        Assert.Contains(payload.Commands, static command => string.Equals(command.Type, "draw-string-rect", StringComparison.Ordinal)
            && string.Equals(command.ValueText, "Rect", StringComparison.Ordinal));
        Assert.Contains(payload.Commands, static command => string.Equals(command.Type, "fill-paint", StringComparison.Ordinal)
            && string.Equals(command.Paint?.Kind, "linear", StringComparison.Ordinal));
        Assert.Contains(payload.Commands, static command => string.Equals(command.Type, "fill-paint", StringComparison.Ordinal)
            && string.Equals(command.Paint?.Kind, "radial", StringComparison.Ordinal));
    }

    [Fact]
    public void GraphicsViewHandler_FallsBackToPlainStringForAttributedText()
    {
        var coordinator = new CanvasCoordinatorProbe();
        var graphicsView = new TestGraphicsView
        {
            Drawable = new AttributedTextDrawable("Attributed fallback")
        };

        var (handler, _, _) = CreateAttachedHandler(graphicsView, coordinator);
        handler.PlatformArrange(new Rect(0, 0, 160, 120));
        coordinator.FlushAllDelayed();

        var payload = coordinator.GetLastCanvasPayload();
        Assert.NotNull(payload);
        Assert.Contains(payload!.Commands, static command => string.Equals(command.Type, "draw-string-rect", StringComparison.Ordinal)
            && string.Equals(command.ValueText, "Attributed fallback", StringComparison.Ordinal));
    }

    [Fact]
    public void GraphicsViewHandler_DoesNotRecordDrawImageCommandsInWaveOne()
    {
        var coordinator = new CanvasCoordinatorProbe();
        var graphicsView = new TestGraphicsView
        {
            Drawable = new DrawImageDrawable()
        };

        var (handler, _, _) = CreateAttachedHandler(graphicsView, coordinator);
        handler.PlatformArrange(new Rect(0, 0, 160, 120));
        coordinator.FlushAllDelayed();

        var payload = coordinator.GetLastCanvasPayload();
        Assert.NotNull(payload);
        Assert.DoesNotContain(payload!.Commands, static command => command.Type.Contains("image", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GraphicsViewHandler_RoutesPointerAndHoverInteractions()
    {
        var graphicsView = new TestGraphicsView();
        var (handler, _, _) = CreateAttachedHandler(graphicsView);

        handler.OnRuntimeEvent("pointerenter", CreatePointerPayload(pointerId: 5, x: 12, y: 14, isInsideBounds: true));
        handler.OnRuntimeEvent("pointermove", CreatePointerPayload(pointerId: 5, x: 18, y: 20, isInsideBounds: true));
        handler.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 2, x: 30, y: 32, isInsideBounds: true));
        handler.OnRuntimeEvent("pointermove", CreatePointerPayload(pointerId: 2, x: 36, y: 40, isInsideBounds: true));
        handler.OnRuntimeEvent("pointerup", CreatePointerPayload(pointerId: 2, x: 42, y: 44, isInsideBounds: false));
        handler.OnRuntimeEvent("pointerenter", CreatePointerPayload(pointerId: 5, x: 22, y: 24, isInsideBounds: true));
        handler.OnRuntimeEvent("pointerleave", CreatePointerPayload(pointerId: 5, x: 22, y: 24, isInsideBounds: false));
        handler.OnRuntimeEvent("pointerdown", CreatePointerPayload(pointerId: 7, x: 50, y: 52, isInsideBounds: true));
        handler.OnRuntimeEvent("pointercancel", CreatePointerPayload(pointerId: 7, x: 50, y: 52, isInsideBounds: true));

        Assert.Equal(
            [
                "hover-start:[12,14]",
                "hover-move:[18,20]",
                "hover-end",
                "start:[30,32]",
                "drag:[36,40]",
                "end:[42,44]:inside=False",
                "hover-start:[22,24]",
                "hover-end",
                "start:[50,52]",
                "cancel"
            ],
            graphicsView.InteractionLog);
    }

    private static string CreatePointerPayload(long pointerId, float x, float y, bool isInsideBounds)
    {
        return JsonSerializer.Serialize(new
        {
            pointerId,
            pointerType = "mouse",
            x,
            y,
            isInsideBounds
        });
    }

    private static (MirageGraphicsViewHandler Handler, TGraphicsView GraphicsView, IServiceProvider Services) CreateAttachedHandler<TGraphicsView>(
        TGraphicsView graphicsView,
        CanvasCoordinatorProbe? coordinator = null)
        where TGraphicsView : View, IGraphicsView
    {
        var services = new ServiceCollection();
        if (coordinator is not null)
        {
            services.AddSingleton<IMirageRenderCoordinator>(coordinator);
        }

        var serviceProvider = services.BuildServiceProvider();
        var mauiContext = new MauiContext(serviceProvider);
        var handler = new MirageGraphicsViewHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(graphicsView);
        return (handler, graphicsView, serviceProvider);
    }

    private sealed class CanvasCoordinatorProbe : IMirageRenderCoordinator
    {
        private readonly Queue<Action> _pendingDelayedActions = [];
        private int _nodeIdCounter;

        public int ConfigureCanvasCallCount { get; private set; }

        public int RenderCanvasCallCount { get; private set; }

        public int RequestRenderCallCount { get; private set; }

        public double DisplayScale { get; set; } = 1.5;

        public string? LastConfiguredNodeId { get; private set; }

        public double LastConfiguredWidth { get; private set; }

        public double LastConfiguredHeight { get; private set; }

        public string? LastCanvasJson { get; private set; }

        public int ConfigureFailuresRemaining { get; set; }

        public int PendingDelayedCount => _pendingDelayedActions.Count;

        public TimeSpan LastDispatchDelay { get; private set; }

        public void Register(MiragePlatformView platformView, IMirageRuntimeEventTarget? eventTarget)
        {
            _ = eventTarget;
            if (string.IsNullOrWhiteSpace(platformView.NodeId))
            {
                var localId = Interlocked.Increment(ref _nodeIdCounter);
                platformView.AssignNodeId($"mirage-graphics-node-{localId:D4}");
            }
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
            RequestRenderCallCount++;
        }

        public bool DispatchDelayed(TimeSpan delay, Action action)
        {
            LastDispatchDelay = delay;
            _pendingDelayedActions.Enqueue(action);
            return true;
        }

        public IDisposable BeginRenderBatch()
        {
            return NullDisposable.Instance;
        }

        public void AttachRuntime(IMirageRuntime runtime)
        {
            _ = runtime;
        }

        public void DetachRuntime()
        {
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

        public bool TryConfigureNodeCanvas(string nodeId, double logicalWidth, double logicalHeight, out double displayScale)
        {
            ConfigureCanvasCallCount++;
            LastConfiguredNodeId = nodeId;
            LastConfiguredWidth = logicalWidth;
            LastConfiguredHeight = logicalHeight;
            displayScale = DisplayScale;
            if (ConfigureFailuresRemaining > 0)
            {
                ConfigureFailuresRemaining--;
                return false;
            }

            return true;
        }

        public bool TryRenderNodeCanvas(string nodeId, string commandsJson)
        {
            _ = nodeId;
            RenderCanvasCallCount++;
            LastCanvasJson = commandsJson;
            return true;
        }

        public CanvasPayloadProbe? GetLastCanvasPayload()
        {
            return string.IsNullOrWhiteSpace(LastCanvasJson)
                ? null
                : JsonSerializer.Deserialize<CanvasPayloadProbe>(LastCanvasJson, JsonOptions);
        }

        public void OnEvent(string nodeId, string eventName, string? payload)
        {
            _ = nodeId;
            _ = eventName;
            _ = payload;
        }

        public void FlushAllDelayed()
        {
            while (_pendingDelayedActions.Count > 0)
            {
                var action = _pendingDelayedActions.Dequeue();
                action();
            }
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

    private sealed class TestGraphicsView : View, IGraphicsView
    {
        public IDrawable Drawable { get; set; } = new SolidRectangleDrawable(Colors.Black);

        public List<string> InteractionLog { get; } = [];

        public void Invalidate()
        {
        }

        public void StartHoverInteraction(PointF[] points)
        {
            InteractionLog.Add($"hover-start:{FormatPoints(points)}");
        }

        public void MoveHoverInteraction(PointF[] points)
        {
            InteractionLog.Add($"hover-move:{FormatPoints(points)}");
        }

        public void EndHoverInteraction()
        {
            InteractionLog.Add("hover-end");
        }

        public void StartInteraction(PointF[] points)
        {
            InteractionLog.Add($"start:{FormatPoints(points)}");
        }

        public void DragInteraction(PointF[] points)
        {
            InteractionLog.Add($"drag:{FormatPoints(points)}");
        }

        public void EndInteraction(PointF[] points, bool isInsideBounds)
        {
            InteractionLog.Add($"end:{FormatPoints(points)}:inside={isInsideBounds}");
        }

        public void CancelInteraction()
        {
            InteractionLog.Add("cancel");
        }

        private static string FormatPoints(PointF[] points)
        {
            return "[" + string.Join(";", points.Select(static point => $"{point.X:0.###},{point.Y:0.###}")) + "]";
        }
    }

    private sealed class SolidRectangleDrawable(Color color) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FillColor = color;
            canvas.FillRectangle(0, 0, dirtyRect.Width, dirtyRect.Height);
        }
    }

    private sealed class TriangleDrawable(Color color) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var path = new PathF();
            path.MoveTo(dirtyRect.Width / 2, 0);
            path.LineTo(dirtyRect.Width, dirtyRect.Height);
            path.LineTo(0, dirtyRect.Height);
            path.Close();

            canvas.FillColor = color;
            canvas.FillPath(path);
        }
    }

    private sealed class CoverageDrawable : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            _ = dirtyRect;

            canvas.SaveState();
            canvas.Alpha = 0.5f;
            canvas.StrokeColor = Colors.Firebrick;
            canvas.FillColor = Colors.Gold;
            canvas.FontColor = Colors.MediumPurple;
            canvas.StrokeSize = 3f;
            canvas.MiterLimit = 6f;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Bevel;
            canvas.StrokeDashPattern = [4f, 2f];
            canvas.StrokeDashOffset = 1f;
            canvas.Font = MauiGraphicsFont.Default;
            canvas.FontSize = 18f;
            canvas.Translate(5, 6);
            canvas.Scale(1.1f, 0.9f);
            canvas.Rotate(12);
            canvas.Rotate(8, 30, 20);
            canvas.ConcatenateTransform(new Matrix3x2(1, 0.1f, -0.05f, 1, 2, 3));
            canvas.ClipRectangle(0, 0, 120, 90);
            canvas.DrawLine(0, 0, 60, 20);
            canvas.DrawRectangle(4, 4, 26, 18);
            canvas.FillRectangle(36, 4, 22, 18);
            canvas.DrawRoundedRectangle(64, 4, 28, 18, 6);
            canvas.FillRoundedRectangle(98, 4, 28, 18, 6);
            canvas.DrawEllipse(4, 30, 22, 16);
            canvas.FillEllipse(32, 30, 22, 16);
            canvas.DrawArc(60, 28, 28, 20, 0, 180, clockwise: true, closed: false);
            canvas.FillArc(94, 28, 28, 20, 45, 225, clockwise: false);

            var path = new PathF();
            path.MoveTo(4, 60);
            path.LineTo(24, 60);
            path.QuadTo(34, 50, 44, 60);
            path.CurveTo(54, 70, 64, 50, 74, 60);
            path.Close();
            canvas.DrawPath(path);
            canvas.FillPath(path, WindingMode.EvenOdd);

            canvas.SetFillPaint(
                new LinearGradientPaint(
                    [
                        new PaintGradientStop(0, Colors.DodgerBlue),
                        new PaintGradientStop(1, Colors.MediumPurple)
                    ],
                    new Point(0, 0),
                    new Point(1, 1)),
                new RectF(80, 56, 24, 18));

            canvas.SetFillPaint(
                new RadialGradientPaint
                {
                    Center = new Point(0.5, 0.5),
                    Radius = 0.75,
                    GradientStops =
                    [
                        new PaintGradientStop(0, Colors.Orange),
                        new PaintGradientStop(1, Colors.DarkOrange)
                    ]
                },
                new RectF(108, 56, 24, 18));

            canvas.SetShadow(new SizeF(2, 3), 4, Colors.Black.WithAlpha(0.35f));
            canvas.DrawString("Point", 20, 84, HorizontalAlignment.Center);
            canvas.DrawString("Rect", 60, 78, 50, 18, HorizontalAlignment.Center, VerticalAlignment.Center, TextFlow.ClipBounds, 0);
            canvas.RestoreState();
        }
    }

    private sealed class MutableAccentDrawable(Func<Color> getAccent) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FillColor = getAccent();
            canvas.FillRectangle(dirtyRect.Width * 0.2f, dirtyRect.Height * 0.2f, dirtyRect.Width * 0.5f, dirtyRect.Height * 0.5f);
        }
    }

    private sealed class AttributedTextDrawable(string text) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FontColor = Colors.DarkSlateBlue;
            canvas.DrawText(new StubAttributedText(text), 10, 10, dirtyRect.Width - 20, dirtyRect.Height - 20);
        }
    }

    private sealed class DrawImageDrawable : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.DrawImage(new StubImage(), 10, 10, dirtyRect.Width - 20, dirtyRect.Height - 20);
        }
    }

    private sealed class StubAttributedText(string text) : IAttributedText
    {
        public string Text { get; } = text;

        public IReadOnlyList<IAttributedTextRun> Runs { get; } = [];
    }

    private sealed class StubImage : MauiGraphicsImage
    {
        public float Width => 24;

        public float Height => 24;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            _ = canvas;
            _ = dirtyRect;
        }

        public MauiGraphicsImage Downsize(float maxWidthOrHeight, bool disposeOriginal)
        {
            _ = maxWidthOrHeight;
            _ = disposeOriginal;
            return this;
        }

        public MauiGraphicsImage Downsize(float maxWidth, float maxHeight, bool disposeOriginal)
        {
            _ = maxWidth;
            _ = maxHeight;
            _ = disposeOriginal;
            return this;
        }

        public MauiGraphicsImage Resize(float width, float height, ResizeMode resizeMode, bool disposeOriginal)
        {
            _ = width;
            _ = height;
            _ = resizeMode;
            _ = disposeOriginal;
            return this;
        }

        public void Save(Stream stream, ImageFormat format = ImageFormat.Png, float quality = 1)
        {
            _ = stream;
            _ = format;
            _ = quality;
        }

        public Task SaveAsync(Stream stream, ImageFormat format = ImageFormat.Png, float quality = 1)
        {
            Save(stream, format, quality);
            return Task.CompletedTask;
        }

        public MauiGraphicsImage ToPlatformImage()
        {
            return this;
        }

        public void Dispose()
        {
        }
    }

    private sealed class CanvasPayloadProbe
    {
        public float LogicalWidth { get; set; }

        public float LogicalHeight { get; set; }

        public float DisplayScale { get; set; }

        public CanvasCommandProbe[] Commands { get; set; } = [];
    }

    private sealed class CanvasCommandProbe
    {
        public string Type { get; set; } = string.Empty;

        public string? ValueText { get; set; }

        public CanvasPaintProbe? Paint { get; set; }
    }

    private sealed class CanvasPaintProbe
    {
        public string Kind { get; set; } = string.Empty;
    }
}
