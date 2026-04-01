using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Text;
using Microsoft.Maui.Handlers;
using MauiGraphicsFont = Microsoft.Maui.Graphics.Font;
using MauiGraphicsImage = Microsoft.Maui.Graphics.IImage;

namespace Mirage.Maui.Handlers;

internal sealed class MirageGraphicsViewHandler : MirageViewHandler<IGraphicsView>, IMirageRuntimeEventTarget
{
    private static readonly TimeSpan RepaintCoalesceDelay = TimeSpan.Zero;
    private static readonly TimeSpan RepaintRetryDelay = TimeSpan.FromMilliseconds(16);
    internal static readonly TimeSpan SlowRepaintRetryDelay = TimeSpan.FromMilliseconds(250);
    private const double DefaultWidth = 300;
    private const double DefaultHeight = 150;
    internal const int MaxRepaintRetryAttempts = 60;
    private static readonly JsonSerializerOptions CanvasJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    internal static readonly string[] ParityPropertyKeys =
    [
        nameof(IView.Background),
        nameof(IGraphicsView.Drawable),
        nameof(IView.FlowDirection)
    ];

    internal static readonly string[] ParityCommandKeys =
    [
        nameof(IGraphicsView.Invalidate)
    ];

    public static IPropertyMapper<IGraphicsView, MirageGraphicsViewHandler> Mapper =
        new PropertyMapper<IGraphicsView, MirageGraphicsViewHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(IView.Background)] = MapBackground,
            ["BackgroundColor"] = MapBackground,
            [nameof(IGraphicsView.Drawable)] = MapDrawable,
            [nameof(IView.FlowDirection)] = MapFlowDirection
        };

    public static CommandMapper<IGraphicsView, MirageGraphicsViewHandler> CommandMapper =
        new(MirageViewMapper.CommonViewCommandMapper)
        {
            [nameof(IGraphicsView.Invalidate)] = MapInvalidate
        };

    private readonly Dictionary<long, PointF> _activePointers = new();
    private bool _hoverActive;
    private bool _repaintQueued;
    private bool _repaintRetryQueued;
    private bool _isConnected;
    private double _arrangedWidth;
    private double _arrangedHeight;
    private int _consecutiveRepaintFailures;

    public MirageGraphicsViewHandler()
        : base(ComponentKind.GraphicsView, Mapper, CommandMapper)
    {
        DefaultDisplayStyle = "block";
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        return ResolveMeasuredSizeWithConstraints(VirtualView, widthConstraint, heightConstraint, DefaultWidth, DefaultHeight);
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        _isConnected = true;
        platformView.SetStyle("display", "block");
        platformView.SetStyle("boxSizing", "border-box");
        platformView.SetStyle("touchAction", "none");
        platformView.SetStyle("userSelect", "none");
        platformView.SetStyle("-webkit-user-select", "none");
        platformView.SetEvent("pointerdown", enabled: true);
        platformView.SetEvent("pointermove", enabled: true);
        platformView.SetEvent("pointerup", enabled: true);
        platformView.SetEvent("pointercancel", enabled: true);
        platformView.SetEvent("pointerenter", enabled: true);
        platformView.SetEvent("pointerleave", enabled: true);

        var changed = ApplyGraphicsViewProjectionCore(VirtualView);
        base.ConnectHandler(platformView);

        if (changed)
        {
            RequestRender();
        }

        QueueRepaint();
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        _isConnected = false;
        _activePointers.Clear();
        _hoverActive = false;
        _repaintQueued = false;
        _repaintRetryQueued = false;
        _arrangedWidth = 0;
        _arrangedHeight = 0;
        _consecutiveRepaintFailures = 0;
        base.DisconnectHandler(platformView);
    }

    public override void PlatformArrange(Rect frame)
    {
        base.PlatformArrange(frame);

        var width = Math.Max(0, frame.Width);
        var height = Math.Max(0, frame.Height);
        var sizeChanged = !NearlyEqual(width, _arrangedWidth) || !NearlyEqual(height, _arrangedHeight);
        _arrangedWidth = width;
        _arrangedHeight = height;

        if (sizeChanged)
        {
            QueueRepaint();
        }
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        if (!TryParsePointerPayload(payload, out var pointer))
        {
            if (string.Equals(eventName, "pointerleave", StringComparison.Ordinal))
            {
                EndHoverInteraction();
            }

            return;
        }

        switch (eventName)
        {
            case "pointerdown":
                HandlePointerDown(pointer);
                break;
            case "pointermove":
                HandlePointerMove(pointer);
                break;
            case "pointerup":
                HandlePointerUp(pointer);
                break;
            case "pointercancel":
                HandlePointerCancel(pointer);
                break;
            case "pointerenter":
                HandlePointerEnter(pointer);
                break;
            case "pointerleave":
                HandlePointerLeave(pointer);
                break;
        }
    }

    public static void MapBackground(MirageGraphicsViewHandler handler, IGraphicsView graphicsView)
    {
        var changed = handler.UpdateBackgroundProjection(graphicsView);
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
        handler.QueueRepaint();
    }

    public static void MapDrawable(MirageGraphicsViewHandler handler, IGraphicsView graphicsView)
    {
        _ = graphicsView;
        handler.QueueRepaint();
    }

    public static void MapFlowDirection(MirageGraphicsViewHandler handler, IGraphicsView graphicsView)
    {
        if (!handler.UpdateFlowDirectionProjection(graphicsView))
        {
            return;
        }

        handler.RequestRender();
        handler.QueueRepaint();
    }

    public static void MapInvalidate(MirageGraphicsViewHandler handler, IGraphicsView graphicsView, object? args)
    {
        _ = graphicsView;
        _ = args;
        handler.QueueRepaint();
    }

    private bool ApplyGraphicsViewProjectionCore(IGraphicsView graphicsView)
    {
        using var _ = BeginRenderBatch();

        var changed = UpdateBackgroundProjection(graphicsView);
        changed |= UpdateFlowDirectionProjection(graphicsView);
        return changed;
    }

    private bool UpdateBackgroundProjection(IGraphicsView graphicsView)
    {
        return MirageCommonViewProjection.MapBackground(this, graphicsView);
    }

    private bool UpdateFlowDirectionProjection(IGraphicsView graphicsView)
    {
        var direction = graphicsView.FlowDirection switch
        {
            FlowDirection.LeftToRight => "ltr",
            FlowDirection.RightToLeft => "rtl",
            _ => null
        };

        return SetStyleIfChanged("direction", direction);
    }

    private void QueueRepaint()
    {
        if (_repaintQueued)
        {
            return;
        }

        _consecutiveRepaintFailures = 0;
        _repaintQueued = true;
        if (Coordinator?.DispatchDelayed(RepaintCoalesceDelay, FlushQueuedRepaint) == true)
        {
            return;
        }

        FlushQueuedRepaint();
    }

    private void FlushQueuedRepaint()
    {
        _repaintQueued = false;

        if (TryRepaintCanvas())
        {
            _consecutiveRepaintFailures = 0;
            _repaintRetryQueued = false;
            return;
        }

        HandleRepaintFailure();
    }

    private void RetryQueuedRepaint()
    {
        _repaintRetryQueued = false;
        if (TryRepaintCanvas())
        {
            _consecutiveRepaintFailures = 0;
            return;
        }

        HandleRepaintFailure();
    }

    private bool TryRepaintCanvas()
    {
        if (Coordinator is not { } coordinator
            || string.IsNullOrWhiteSpace(PlatformView.NodeId)
            || !TryGetLogicalCanvasSize(out var logicalWidth, out var logicalHeight))
        {
            return false;
        }

        if (!coordinator.TryConfigureNodeCanvas(PlatformView.NodeId, logicalWidth, logicalHeight, out var displayScale))
        {
            return false;
        }

        var recordingCanvas = new MirageRecordingCanvas((float)Math.Max(1, displayScale), VirtualView.FlowDirection);
        VirtualView.Drawable?.Draw(recordingCanvas, new RectF(0, 0, (float)logicalWidth, (float)logicalHeight));
        var payload = recordingCanvas.CreatePayload((float)logicalWidth, (float)logicalHeight);
        var commandsJson = JsonSerializer.Serialize(payload, CanvasJsonOptions);
        return coordinator.TryRenderNodeCanvas(PlatformView.NodeId, commandsJson);
    }

    private bool TryGetLogicalCanvasSize(out double width, out double height)
    {
        width = _arrangedWidth > 0
            ? _arrangedWidth
            : ResolveRequestedCanvasDimension(isWidth: true, DefaultWidth);
        height = _arrangedHeight > 0
            ? _arrangedHeight
            : ResolveRequestedCanvasDimension(isWidth: false, DefaultHeight);

        return width > 0 && height > 0;
    }

    private void ScheduleRetryRepaint(TimeSpan delay)
    {
        if (_repaintRetryQueued || !_isConnected || Coordinator is not { } coordinator)
        {
            return;
        }

        _repaintRetryQueued = true;
        if (!coordinator.DispatchDelayed(delay, RetryQueuedRepaint))
        {
            _repaintRetryQueued = false;
        }
    }

    private void HandleRepaintFailure()
    {
        if (!_isConnected)
        {
            return;
        }

        _consecutiveRepaintFailures++;
        var delay = _consecutiveRepaintFailures >= MaxRepaintRetryAttempts
            ? SlowRepaintRetryDelay
            : RepaintRetryDelay;
        ScheduleRetryRepaint(delay);
    }

    private void HandlePointerDown(GraphicsPointerPayload payload)
    {
        EndHoverInteraction();
        _activePointers[payload.PointerId] = new PointF(payload.X, payload.Y);
        VirtualView.StartInteraction(BuildActivePointArray());
    }

    private void HandlePointerMove(GraphicsPointerPayload payload)
    {
        var point = new PointF(payload.X, payload.Y);
        if (_activePointers.ContainsKey(payload.PointerId))
        {
            _activePointers[payload.PointerId] = point;
            VirtualView.DragInteraction(BuildActivePointArray());
            return;
        }

        if (!_hoverActive)
        {
            _hoverActive = true;
            VirtualView.StartHoverInteraction([point]);
            return;
        }

        VirtualView.MoveHoverInteraction([point]);
    }

    private void HandlePointerUp(GraphicsPointerPayload payload)
    {
        if (!_activePointers.ContainsKey(payload.PointerId))
        {
            return;
        }

        _activePointers[payload.PointerId] = new PointF(payload.X, payload.Y);
        var points = BuildActivePointArray();
        _activePointers.Remove(payload.PointerId);
        VirtualView.EndInteraction(points, payload.IsInsideBounds);
    }

    private void HandlePointerCancel(GraphicsPointerPayload payload)
    {
        _ = payload;
        if (_activePointers.Count == 0)
        {
            return;
        }

        _activePointers.Clear();
        VirtualView.CancelInteraction();
    }

    private void HandlePointerEnter(GraphicsPointerPayload payload)
    {
        if (_activePointers.Count > 0)
        {
            return;
        }

        var point = new PointF(payload.X, payload.Y);
        if (_hoverActive)
        {
            VirtualView.MoveHoverInteraction([point]);
            return;
        }

        _hoverActive = true;
        VirtualView.StartHoverInteraction([point]);
    }

    private void HandlePointerLeave(GraphicsPointerPayload payload)
    {
        _ = payload;
        EndHoverInteraction();
    }

    private void EndHoverInteraction()
    {
        if (!_hoverActive)
        {
            return;
        }

        _hoverActive = false;
        VirtualView.EndHoverInteraction();
    }

    private PointF[] BuildActivePointArray()
    {
        if (_activePointers.Count == 0)
        {
            return [];
        }

        return _activePointers
            .OrderBy(static pair => pair.Key)
            .Select(static pair => pair.Value)
            .ToArray();
    }

    private bool SetStyleIfChanged(string name, string? value)
    {
        if (string.Equals(PlatformView.GetStyleValue(name), value, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetStyle(name, value);
        return true;
    }

    private static bool TryParsePointerPayload(string? payload, out GraphicsPointerPayload pointer)
    {
        pointer = new GraphicsPointerPayload();
        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<GraphicsPointerPayload>(payload, CanvasJsonOptions);
            if (parsed is null)
            {
                return false;
            }

            pointer = parsed;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool NearlyEqual(double left, double right)
    {
        return Math.Abs(left - right) < 0.01;
    }

    private sealed class MirageRecordingCanvas : ICanvas
    {
        private const float DefaultFontSize = 14f;
        private const string DefaultFontFamily = "system-ui";

        private readonly List<CanvasCommand> _commands = [];
        private readonly Stack<CanvasStateSnapshot> _stateStack = [];
        private readonly FlowDirection _flowDirection;
        private CanvasStateSnapshot _state;

        public MirageRecordingCanvas(float displayScale, FlowDirection flowDirection)
        {
            _flowDirection = flowDirection;
            _state = CanvasStateSnapshot.CreateDefault(displayScale, flowDirection);
        }

        public float DisplayScale
        {
            get => _state.DisplayScale;
            set => _state.DisplayScale = float.IsFinite(value) && value > 0 ? value : _state.DisplayScale;
        }

        public float StrokeSize
        {
            get => _state.StrokeSize;
            set
            {
                var normalized = NormalizeNonNegative(value, 1f);
                if (NearlyEqual(normalized, _state.StrokeSize))
                {
                    return;
                }

                _state.StrokeSize = normalized;
                _commands.Add(new CanvasCommand { Type = "stroke-size", Value = normalized });
            }
        }

        public float MiterLimit
        {
            get => _state.MiterLimit;
            set
            {
                var normalized = NormalizeNonNegative(value, 10f);
                if (NearlyEqual(normalized, _state.MiterLimit))
                {
                    return;
                }

                _state.MiterLimit = normalized;
                _commands.Add(new CanvasCommand { Type = "miter-limit", Value = normalized });
            }
        }

        public Color StrokeColor
        {
            get => _state.StrokeColor;
            set
            {
                if (ColorEquals(_state.StrokeColor, value))
                {
                    return;
                }

                _state.StrokeColor = value;
                _commands.Add(new CanvasCommand { Type = "stroke-color", Color = ToCssColor(value) });
            }
        }

        public LineCap StrokeLineCap
        {
            get => _state.StrokeLineCap;
            set
            {
                if (_state.StrokeLineCap == value)
                {
                    return;
                }

                _state.StrokeLineCap = value;
                _commands.Add(new CanvasCommand { Type = "line-cap", ValueText = value switch { LineCap.Round => "round", LineCap.Square => "square", _ => "butt" } });
            }
        }

        public LineJoin StrokeLineJoin
        {
            get => _state.StrokeLineJoin;
            set
            {
                if (_state.StrokeLineJoin == value)
                {
                    return;
                }

                _state.StrokeLineJoin = value;
                _commands.Add(new CanvasCommand { Type = "line-join", ValueText = value switch { LineJoin.Round => "round", LineJoin.Bevel => "bevel", _ => "miter" } });
            }
        }

        public float[] StrokeDashPattern
        {
            get => _state.StrokeDashPattern;
            set
            {
                var normalized = value?.Where(static v => float.IsFinite(v) && v >= 0).ToArray() ?? [];
                if (_state.StrokeDashPattern.SequenceEqual(normalized))
                {
                    return;
                }

                _state.StrokeDashPattern = normalized;
                _commands.Add(new CanvasCommand { Type = "dash", Pattern = normalized, Offset = _state.StrokeDashOffset });
            }
        }

        public float StrokeDashOffset
        {
            get => _state.StrokeDashOffset;
            set
            {
                var normalized = float.IsFinite(value) ? value : 0f;
                if (NearlyEqual(normalized, _state.StrokeDashOffset))
                {
                    return;
                }

                _state.StrokeDashOffset = normalized;
                _commands.Add(new CanvasCommand { Type = "dash", Pattern = _state.StrokeDashPattern, Offset = normalized });
            }
        }

        public Color FillColor
        {
            get => _state.FillColor;
            set
            {
                if (ColorEquals(_state.FillColor, value))
                {
                    return;
                }

                _state.FillColor = value;
                _commands.Add(new CanvasCommand { Type = "fill-color", Color = ToCssColor(value) });
            }
        }

        public Color FontColor
        {
            get => _state.FontColor;
            set
            {
                if (ColorEquals(_state.FontColor, value))
                {
                    return;
                }

                _state.FontColor = value;
                _commands.Add(new CanvasCommand { Type = "font-color", Color = ToCssColor(value) });
            }
        }

        public IFont Font
        {
            get => _state.Font ?? MauiGraphicsFont.Default;
            set
            {
                _state.Font = value ?? MauiGraphicsFont.Default;
                RecordFontCommand();
            }
        }

        public float FontSize
        {
            get => _state.FontSize;
            set
            {
                var normalized = NormalizePositive(value, DefaultFontSize);
                if (NearlyEqual(normalized, _state.FontSize))
                {
                    return;
                }

                _state.FontSize = normalized;
                RecordFontCommand();
            }
        }

        public float Alpha
        {
            get => _state.Alpha;
            set
            {
                var normalized = NormalizeClamped(value, 1f, 0f, 1f);
                if (NearlyEqual(normalized, _state.Alpha))
                {
                    return;
                }

                _state.Alpha = normalized;
                _commands.Add(new CanvasCommand { Type = "alpha", Value = normalized });
            }
        }

        public bool Antialias
        {
            get => _state.Antialias;
            set => _state.Antialias = value;
        }

        public BlendMode BlendMode
        {
            get => _state.BlendMode;
            set => _state.BlendMode = value;
        }

        public void DrawPath(PathF path)
        {
            var pathData = BuildSvgPathData(path);
            if (string.IsNullOrWhiteSpace(pathData))
            {
                return;
            }

            _commands.Add(new CanvasCommand { Type = "path-stroke", PathData = pathData });
        }

        public void FillPath(PathF path, WindingMode windingMode)
        {
            var pathData = BuildSvgPathData(path);
            if (string.IsNullOrWhiteSpace(pathData))
            {
                return;
            }

            _commands.Add(new CanvasCommand
            {
                Type = "path-fill",
                PathData = pathData,
                FillRule = windingMode == WindingMode.EvenOdd ? "evenodd" : "nonzero"
            });
        }

        public void SubtractFromClip(float x, float y, float width, float height)
        {
            _ = x;
            _ = y;
            _ = width;
            _ = height;
        }

        public void ClipPath(PathF path, WindingMode windingMode)
        {
            _ = path;
            _ = windingMode;
        }

        public void ClipRectangle(float x, float y, float width, float height)
        {
            _commands.Add(new CanvasCommand
            {
                Type = "clip-rect",
                X = x,
                Y = y,
                Width = width,
                Height = height
            });
        }

        public void DrawLine(float x1, float y1, float x2, float y2)
        {
            _commands.Add(new CanvasCommand { Type = "line", X1 = x1, Y1 = y1, X2 = x2, Y2 = y2 });
        }

        public void DrawArc(float x, float y, float width, float height, float startAngle, float endAngle, bool clockwise, bool closed)
        {
            _commands.Add(new CanvasCommand
            {
                Type = "arc",
                X = x,
                Y = y,
                Width = width,
                Height = height,
                StartAngle = startAngle,
                EndAngle = endAngle,
                Clockwise = clockwise,
                Closed = closed
            });
        }

        public void FillArc(float x, float y, float width, float height, float startAngle, float endAngle, bool clockwise)
        {
            _commands.Add(new CanvasCommand
            {
                Type = "fill-arc",
                X = x,
                Y = y,
                Width = width,
                Height = height,
                StartAngle = startAngle,
                EndAngle = endAngle,
                Clockwise = clockwise,
                Closed = true
            });
        }

        public void DrawRectangle(float x, float y, float width, float height)
        {
            _commands.Add(new CanvasCommand { Type = "rectangle", X = x, Y = y, Width = width, Height = height });
        }

        public void FillRectangle(float x, float y, float width, float height)
        {
            _commands.Add(new CanvasCommand { Type = "fill-rectangle", X = x, Y = y, Width = width, Height = height });
        }

        public void DrawRoundedRectangle(float x, float y, float width, float height, float cornerRadius)
        {
            _commands.Add(new CanvasCommand { Type = "rounded-rectangle", X = x, Y = y, Width = width, Height = height, Radius = cornerRadius });
        }

        public void FillRoundedRectangle(float x, float y, float width, float height, float cornerRadius)
        {
            _commands.Add(new CanvasCommand { Type = "fill-rounded-rectangle", X = x, Y = y, Width = width, Height = height, Radius = cornerRadius });
        }

        public void DrawEllipse(float x, float y, float width, float height)
        {
            _commands.Add(new CanvasCommand { Type = "ellipse", X = x, Y = y, Width = width, Height = height });
        }

        public void FillEllipse(float x, float y, float width, float height)
        {
            _commands.Add(new CanvasCommand { Type = "fill-ellipse", X = x, Y = y, Width = width, Height = height });
        }

        public void DrawString(string value, float x, float y, HorizontalAlignment horizontalAlignment)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            _commands.Add(new CanvasCommand
            {
                Type = "draw-string-point",
                ValueText = value,
                X = x,
                Y = y,
                HorizontalAlignment = horizontalAlignment.ToString()
            });
        }

        public void DrawString(string value, float x, float y, float width, float height, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, TextFlow textFlow, float lineSpacingAdjustment)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            _commands.Add(new CanvasCommand
            {
                Type = "draw-string-rect",
                ValueText = value,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                HorizontalAlignment = horizontalAlignment.ToString(),
                VerticalAlignment = verticalAlignment.ToString(),
                TextFlow = textFlow.ToString(),
                LineSpacingAdjustment = lineSpacingAdjustment
            });
        }

        public void DrawText(IAttributedText value, float x, float y, float width, float height)
        {
            // Wave 1 keeps attributed text deterministic by falling back to plain content
            // until span-level styling/layout fidelity is implemented in the follow-up wave.
            if (value is null || string.IsNullOrWhiteSpace(value.Text))
            {
                return;
            }

            DrawString(value.Text, x, y, width, height, HorizontalAlignment.Left, VerticalAlignment.Top, TextFlow.ClipBounds, 0);
        }

        public void Rotate(float degrees, float x, float y)
        {
            _commands.Add(new CanvasCommand { Type = "rotate-at", Radians = DegreesToRadians(degrees), X = x, Y = y });
        }

        public void Rotate(float degrees)
        {
            _commands.Add(new CanvasCommand { Type = "rotate", Radians = DegreesToRadians(degrees) });
        }

        public void Scale(float sx, float sy)
        {
            _commands.Add(new CanvasCommand { Type = "scale", X = sx, Y = sy });
        }

        public void Translate(float tx, float ty)
        {
            _commands.Add(new CanvasCommand { Type = "translate", X = tx, Y = ty });
        }

        public void ConcatenateTransform(Matrix3x2 transform)
        {
            _commands.Add(new CanvasCommand
            {
                Type = "matrix",
                Matrix = new[]
                {
                    transform.M11,
                    transform.M12,
                    transform.M21,
                    transform.M22,
                    transform.M31,
                    transform.M32
                }
            });
        }

        public void SaveState()
        {
            _stateStack.Push(_state.Clone());
            _commands.Add(new CanvasCommand { Type = "save" });
        }

        public bool RestoreState()
        {
            if (_stateStack.Count == 0)
            {
                return false;
            }

            _state = _stateStack.Pop();
            _commands.Add(new CanvasCommand { Type = "restore" });
            return true;
        }

        public void ResetState()
        {
            _state = CanvasStateSnapshot.CreateDefault(DisplayScale, _flowDirection);
            _commands.Add(new CanvasCommand { Type = "alpha", Value = _state.Alpha });
            _commands.Add(new CanvasCommand { Type = "stroke-color", Color = ToCssColor(_state.StrokeColor) });
            _commands.Add(new CanvasCommand { Type = "fill-color", Color = ToCssColor(_state.FillColor) });
            _commands.Add(new CanvasCommand { Type = "font-color", Color = ToCssColor(_state.FontColor) });
            _commands.Add(new CanvasCommand { Type = "stroke-size", Value = _state.StrokeSize });
            _commands.Add(new CanvasCommand { Type = "miter-limit", Value = _state.MiterLimit });
            _commands.Add(new CanvasCommand { Type = "line-cap", ValueText = "butt" });
            _commands.Add(new CanvasCommand { Type = "line-join", ValueText = "miter" });
            _commands.Add(new CanvasCommand { Type = "dash", Pattern = [], Offset = 0 });
            RecordFontCommand();
            _commands.Add(new CanvasCommand { Type = "shadow", OffsetX = 0, OffsetY = 0, Blur = 0, Color = "rgba(0,0,0,0)" });
        }

        public void SetShadow(SizeF offset, float blur, Color color)
        {
            _commands.Add(new CanvasCommand
            {
                Type = "shadow",
                OffsetX = offset.Width,
                OffsetY = offset.Height,
                Blur = blur,
                Color = ToCssColor(color) ?? "rgba(0,0,0,0)"
            });
        }

        public void SetFillPaint(Paint paint, RectF rectangle)
        {
            var serializedPaint = SerializePaint(paint);
            if (serializedPaint is null)
            {
                return;
            }

            _commands.Add(new CanvasCommand
            {
                Type = "fill-paint",
                Paint = serializedPaint,
                Rectangle = new CanvasRectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height)
            });
        }

        public void DrawImage(MauiGraphicsImage image, float x, float y, float width, float height)
        {
            _ = image;
            _ = x;
            _ = y;
            _ = width;
            _ = height;
        }

        public SizeF GetStringSize(string value, IFont font, float fontSize)
        {
            _ = value;
            _ = font;
            _ = fontSize;
            return SizeF.Zero;
        }

        public SizeF GetStringSize(string value, IFont font, float fontSize, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment)
        {
            _ = value;
            _ = font;
            _ = fontSize;
            _ = horizontalAlignment;
            _ = verticalAlignment;
            return SizeF.Zero;
        }

        public CanvasPayload CreatePayload(float logicalWidth, float logicalHeight)
        {
            return new CanvasPayload
            {
                LogicalWidth = logicalWidth,
                LogicalHeight = logicalHeight,
                DisplayScale = DisplayScale,
                Commands = _commands.ToArray()
            };
        }

        private void RecordFontCommand()
        {
            _commands.Add(new CanvasCommand { Type = "font", ValueText = BuildFontCss(_state.Font, _state.FontSize) });
        }

        private static SerializedPaint? SerializePaint(Paint? paint)
        {
            if (paint is null)
            {
                return null;
            }

            switch (paint)
            {
                case SolidPaint solidPaint:
                    return new SerializedPaint
                    {
                        Kind = "solid",
                        Color = ToCssColor(solidPaint.Color)
                    };
                case LinearGradientPaint linearGradientPaint:
                    return new SerializedPaint
                    {
                        Kind = "linear",
                        StartX = (float)linearGradientPaint.StartPoint.X,
                        StartY = (float)linearGradientPaint.StartPoint.Y,
                        EndX = (float)linearGradientPaint.EndPoint.X,
                        EndY = (float)linearGradientPaint.EndPoint.Y,
                        Stops = SerializeGradientStops(linearGradientPaint.GradientStops)
                    };
                case RadialGradientPaint radialGradientPaint:
                    return new SerializedPaint
                    {
                        Kind = "radial",
                        CenterX = (float)radialGradientPaint.Center.X,
                        CenterY = (float)radialGradientPaint.Center.Y,
                        Radius = (float)radialGradientPaint.Radius,
                        Stops = SerializeGradientStops(radialGradientPaint.GradientStops)
                    };
                default:
                    return null;
            }
        }

        private static SerializedGradientStop[] SerializeGradientStops(PaintGradientStop[]? stops)
        {
            if (stops is null || stops.Length == 0)
            {
                return
                [
                    new SerializedGradientStop(0, "#000000"),
                    new SerializedGradientStop(1, "#000000")
                ];
            }

            var projectedStops = stops
                .Select(static stop => new SerializedGradientStop(
                    NormalizeClamped(stop.Offset, 0f, 0f, 1f),
                    ToCssColor(stop.Color) ?? "#000000"))
                .OrderBy(static stop => stop.Offset)
                .ToArray();

            if (projectedStops.Length == 1)
            {
                return [projectedStops[0], new SerializedGradientStop(1, projectedStops[0].Color)];
            }

            return projectedStops;
        }

        private static string BuildFontCss(IFont? font, float fontSize)
        {
            var size = NormalizePositive(fontSize, DefaultFontSize);
            var weight = font?.Weight > 0 ? font.Weight.ToString(CultureInfo.InvariantCulture) : "400";
            var style = font?.StyleType switch
            {
                FontStyleType.Italic => "italic",
                FontStyleType.Oblique => "oblique",
                _ => "normal"
            };
            var family = NormalizeFontFamily(font?.Name);
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{style} {weight} {size:0.###}px {family}");
        }

        private static string NormalizeFontFamily(string? fontName)
        {
            if (string.IsNullOrWhiteSpace(fontName))
            {
                return DefaultFontFamily;
            }

            var families = fontName
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(static family => family.IndexOf(' ') >= 0 || family.IndexOf('"') >= 0
                    ? $"\"{family.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\""
                    : family)
                .ToArray();

            return families.Length == 0
                ? DefaultFontFamily
                : string.Join(", ", families);
        }

        private static string BuildSvgPathData(PathF path)
        {
            var operations = path.SegmentTypes.ToArray();
            var points = path.Points.ToArray();
            var pointIndex = 0;
            var builder = new StringBuilder();

            foreach (var operation in operations)
            {
                switch (operation)
                {
                    case PathOperation.Move:
                        if (TryReadPoint(points, ref pointIndex, out var movePoint))
                        {
                            AppendPathCommand(builder, 'M', movePoint);
                        }
                        break;
                    case PathOperation.Line:
                        if (TryReadPoint(points, ref pointIndex, out var linePoint))
                        {
                            AppendPathCommand(builder, 'L', linePoint);
                        }
                        break;
                    case PathOperation.Quad:
                        if (TryReadPoint(points, ref pointIndex, out var quadControl)
                            && TryReadPoint(points, ref pointIndex, out var quadEnd))
                        {
                            AppendCommandPrefix(builder, 'Q');
                            builder.Append(CssNumber(quadControl.X)).Append(' ').Append(CssNumber(quadControl.Y)).Append(' ')
                                .Append(CssNumber(quadEnd.X)).Append(' ').Append(CssNumber(quadEnd.Y));
                        }
                        break;
                    case PathOperation.Cubic:
                        if (TryReadPoint(points, ref pointIndex, out var cubicControl1)
                            && TryReadPoint(points, ref pointIndex, out var cubicControl2)
                            && TryReadPoint(points, ref pointIndex, out var cubicEnd))
                        {
                            AppendCommandPrefix(builder, 'C');
                            builder.Append(CssNumber(cubicControl1.X)).Append(' ').Append(CssNumber(cubicControl1.Y)).Append(' ')
                                .Append(CssNumber(cubicControl2.X)).Append(' ').Append(CssNumber(cubicControl2.Y)).Append(' ')
                                .Append(CssNumber(cubicEnd.X)).Append(' ').Append(CssNumber(cubicEnd.Y));
                        }
                        break;
                    case PathOperation.Arc:
                        if (TryReadPoint(points, ref pointIndex, out var arcEnd))
                        {
                            AppendPathCommand(builder, 'L', arcEnd);
                        }
                        break;
                    case PathOperation.Close:
                        AppendCommandPrefix(builder, 'Z');
                        break;
                }
            }

            return builder.ToString();
        }

        private static void AppendPathCommand(StringBuilder builder, char command, PointF point)
        {
            AppendCommandPrefix(builder, command);
            builder.Append(CssNumber(point.X)).Append(' ').Append(CssNumber(point.Y));
        }

        private static void AppendCommandPrefix(StringBuilder builder, char command)
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(command).Append(' ');
        }

        private static bool TryReadPoint(IReadOnlyList<PointF> points, ref int pointIndex, out PointF point)
        {
            if (pointIndex < points.Count)
            {
                point = points[pointIndex++];
                return true;
            }

            point = default;
            return false;
        }

        private static string CssNumber(float value)
        {
            var normalized = float.IsFinite(value) ? value : 0f;
            return normalized.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string? ToCssColor(Color? color)
        {
            return MirageCommonViewProjection.ToCssColor(color);
        }

        private static bool ColorEquals(Color? left, Color? right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left is null || right is null)
            {
                return false;
            }

            return Math.Abs(left.Red - right.Red) < 0.0001f
                && Math.Abs(left.Green - right.Green) < 0.0001f
                && Math.Abs(left.Blue - right.Blue) < 0.0001f
                && Math.Abs(left.Alpha - right.Alpha) < 0.0001f;
        }

        private static float DegreesToRadians(float degrees)
        {
            return float.IsFinite(degrees)
                ? degrees * (MathF.PI / 180f)
                : 0f;
        }

        private static float NormalizePositive(float value, float fallback)
        {
            return float.IsFinite(value) && value > 0
                ? value
                : fallback;
        }

        private static float NormalizeNonNegative(float value, float fallback)
        {
            return float.IsFinite(value) && value >= 0
                ? value
                : fallback;
        }

        private static float NormalizeClamped(float value, float fallback, float minimum, float maximum)
        {
            if (!float.IsFinite(value))
            {
                return fallback;
            }

            return Math.Clamp(value, minimum, maximum);
        }

        private static bool NearlyEqual(float left, float right)
        {
            return Math.Abs(left - right) < 0.0001f;
        }
    }

    private sealed class CanvasStateSnapshot
    {
        public float DisplayScale { get; set; }
        public float StrokeSize { get; set; }
        public float MiterLimit { get; set; }
        public Color StrokeColor { get; set; } = Colors.Black;
        public LineCap StrokeLineCap { get; set; } = LineCap.Butt;
        public LineJoin StrokeLineJoin { get; set; } = LineJoin.Miter;
        public float[] StrokeDashPattern { get; set; } = [];
        public float StrokeDashOffset { get; set; }
        public Color FillColor { get; set; } = Colors.Transparent;
        public Color FontColor { get; set; } = Colors.Black;
        public IFont? Font { get; set; }
        public float FontSize { get; set; } = 14f;
        public float Alpha { get; set; } = 1f;
        public bool Antialias { get; set; } = true;
        public BlendMode BlendMode { get; set; } = BlendMode.Normal;
        public string Direction { get; set; } = "ltr";

        public static CanvasStateSnapshot CreateDefault(float displayScale, FlowDirection flowDirection)
        {
            return new CanvasStateSnapshot
            {
                DisplayScale = displayScale,
                StrokeSize = 1f,
                MiterLimit = 10f,
                StrokeColor = Colors.Black,
                FillColor = Colors.Black,
                Font = MauiGraphicsFont.Default,
                FontColor = Colors.Black,
                FontSize = 14f,
                Alpha = 1f,
                Direction = flowDirection == FlowDirection.RightToLeft ? "rtl" : "ltr"
            };
        }

        public CanvasStateSnapshot Clone()
        {
            return new CanvasStateSnapshot
            {
                DisplayScale = DisplayScale,
                StrokeSize = StrokeSize,
                MiterLimit = MiterLimit,
                StrokeColor = StrokeColor,
                StrokeLineCap = StrokeLineCap,
                StrokeLineJoin = StrokeLineJoin,
                StrokeDashPattern = StrokeDashPattern.ToArray(),
                StrokeDashOffset = StrokeDashOffset,
                FillColor = FillColor,
                FontColor = FontColor,
                Font = Font ?? MauiGraphicsFont.Default,
                FontSize = FontSize,
                Alpha = Alpha,
                Antialias = Antialias,
                BlendMode = BlendMode,
                Direction = Direction
            };
        }
    }

    private sealed class CanvasPayload
    {
        public float LogicalWidth { get; set; }
        public float LogicalHeight { get; set; }
        public float DisplayScale { get; set; }
        public CanvasCommand[] Commands { get; set; } = [];
    }

    private sealed class CanvasCommand
    {
        public string Type { get; set; } = string.Empty;
        public float? Value { get; set; }
        public string? ValueText { get; set; }
        public string? Color { get; set; }
        public float[]? Pattern { get; set; }
        public float? Offset { get; set; }
        public float? X { get; set; }
        public float? Y { get; set; }
        public float? Width { get; set; }
        public float? Height { get; set; }
        public float? Radius { get; set; }
        public float? X1 { get; set; }
        public float? Y1 { get; set; }
        public float? X2 { get; set; }
        public float? Y2 { get; set; }
        public float? Cx { get; set; }
        public float? Cy { get; set; }
        public float? StartAngle { get; set; }
        public float? EndAngle { get; set; }
        public bool? Clockwise { get; set; }
        public bool? Closed { get; set; }
        public string? PathData { get; set; }
        public string? FillRule { get; set; }
        public SerializedPaint? Paint { get; set; }
        public CanvasRectangle? Rectangle { get; set; }
        public float? OffsetX { get; set; }
        public float? OffsetY { get; set; }
        public float? Blur { get; set; }
        public string? HorizontalAlignment { get; set; }
        public string? VerticalAlignment { get; set; }
        public string? TextFlow { get; set; }
        public float? LineSpacingAdjustment { get; set; }
        public float? Radians { get; set; }
        public float[]? Matrix { get; set; }
    }

    private sealed class SerializedPaint
    {
        public string Kind { get; set; } = string.Empty;
        public string? Color { get; set; }
        public float? StartX { get; set; }
        public float? StartY { get; set; }
        public float? EndX { get; set; }
        public float? EndY { get; set; }
        public float? CenterX { get; set; }
        public float? CenterY { get; set; }
        public float? Radius { get; set; }
        public SerializedGradientStop[]? Stops { get; set; }
    }

    private sealed record SerializedGradientStop(float Offset, string Color);

    private sealed record CanvasRectangle(float X, float Y, float Width, float Height);

    private sealed class GraphicsPointerPayload
    {
        public long PointerId { get; set; }
        public string PointerType { get; set; } = "mouse";
        public float X { get; set; }
        public float Y { get; set; }
        public bool IsInsideBounds { get; set; }
    }

    private static Size ResolveMeasuredSizeWithConstraints(
        IGraphicsView graphicsView,
        double widthConstraint,
        double heightConstraint,
        double measuredWidth,
        double measuredHeight)
    {
        var clampedWidth = ResolveDesiredDimension(widthConstraint, measuredWidth);
        var clampedHeight = ResolveDesiredDimension(heightConstraint, measuredHeight);
        var (requestedWidth, requestedMinimumWidth, requestedMaximumWidth) = ResolveRequestedDimensionRequests(graphicsView, isWidth: true);
        var (requestedHeight, requestedMinimumHeight, requestedMaximumHeight) = ResolveRequestedDimensionRequests(graphicsView, isWidth: false);
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
        return IsExplicitLengthSet(value)
            ? value
            : 0;
    }

    private static bool IsExplicitLengthSet(double value)
    {
        return double.IsFinite(value) && value >= 0;
    }

    private static (double Exact, double Minimum, double Maximum) ResolveRequestedDimensionRequests(IGraphicsView graphicsView, bool isWidth)
    {
        if (graphicsView is not VisualElement element)
        {
            return (-1, -1, -1);
        }

        return isWidth
            ? (element.WidthRequest, element.MinimumWidthRequest, element.MaximumWidthRequest)
            : (element.HeightRequest, element.MinimumHeightRequest, element.MaximumHeightRequest);
    }

    private double ResolveRequestedCanvasDimension(bool isWidth, double fallback)
    {
        var (exact, minimum, maximum) = ResolveRequestedDimensionRequests(VirtualView, isWidth);
        var resolved = fallback;
        if (double.IsFinite(exact) && exact > 0)
        {
            resolved = exact;
        }

        if (double.IsFinite(minimum) && minimum > 0)
        {
            resolved = Math.Max(resolved, minimum);
        }

        if (double.IsFinite(maximum) && maximum > 0)
        {
            resolved = Math.Min(resolved, maximum);
        }

        return resolved > 0 ? resolved : fallback;
    }
}
