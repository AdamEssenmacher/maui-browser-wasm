using System.Globalization;
using System.Text;
using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageBorderHandler : MirageContentViewHandlerBase<Border>
{
    private static int s_internalNodeIdCounter;

    public static IPropertyMapper<Border, MirageBorderHandler> Mapper =
        new PropertyMapper<Border, MirageBorderHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(Border.Content)] = MapContent,
            [nameof(Border.Padding)] = MapPadding,
            [nameof(Border.Stroke)] = MapStroke,
            [nameof(Border.StrokeThickness)] = MapStrokeThickness,
            [nameof(Border.StrokeShape)] = MapStrokeShape,
            [nameof(Border.StrokeDashArray)] = MapStrokeDashArray,
            [nameof(Border.StrokeDashOffset)] = MapStrokeDashOffset,
            [nameof(Border.StrokeLineCap)] = MapStrokeLineCap,
            [nameof(Border.StrokeLineJoin)] = MapStrokeLineJoin
        };

#if DEBUG
    private static readonly HashSet<string> UnsupportedWarnings = new();
#endif

    private MiragePlatformView? _strokeOverlayNode;
    private MiragePlatformView? _strokeDefinitionsNode;
    private MiragePlatformView? _strokePathNode;
    private double _arrangedWidth;
    private double _arrangedHeight;

    public MirageBorderHandler()
        : base(Mapper, kind: ComponentKind.Border)
    {
        DefaultDisplayStyle = "grid";
    }

    protected override IPadding PaddingSource => VirtualView;

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        base.ConnectHandler(platformView);

        var changed = SetStyleIfChanged(PlatformView, "boxSizing", "border-box");
        changed |= EnsureStrokeOverlayStructure();
        changed |= AttachStrokeOverlay();
        changed |= UpdateBorderProjection(VirtualView);
        if (changed)
        {
            RequestRender();
        }
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        base.DisconnectHandler(platformView);
        DisposeStrokeOverlayStructure();
        _arrangedWidth = 0;
        _arrangedHeight = 0;
    }

    public override void PlatformArrange(Rect frame)
    {
        base.PlatformArrange(frame);

        var width = Math.Max(0, frame.Width);
        var height = Math.Max(0, frame.Height);
        var geometryChanged = !NearlyEqual(width, _arrangedWidth) || !NearlyEqual(height, _arrangedHeight);
        _arrangedWidth = width;
        _arrangedHeight = height;

        var changed = UpdateBorderProjection(VirtualView);
        if (!geometryChanged && !changed)
        {
            return;
        }

        RequestRender();
    }

    protected override void OnPresentedContentConnected(IView content, IElementHandler contentHandler, MiragePlatformView contentPlatformView)
    {
        base.OnPresentedContentConnected(content, contentHandler, contentPlatformView);
        var changed = EnsureStrokeOverlayStructure();
        changed |= AttachStrokeOverlay();
        changed |= UpdateBorderProjection(VirtualView);
        if (changed)
        {
            RequestRender();
        }
    }

    protected override void OnPresentedContentDisconnected()
    {
        base.OnPresentedContentDisconnected();
        var changed = EnsureStrokeOverlayStructure();
        changed |= AttachStrokeOverlay();
        changed |= UpdateBorderProjection(VirtualView);
        if (changed)
        {
            RequestRender();
        }
    }

    public static void MapContent(MirageBorderHandler handler, Border border)
    {
        handler.SetPresentedContent(((IContentView)border).PresentedContent);
    }

    public static void MapPadding(MirageBorderHandler handler, Border border)
    {
        if (!handler.UpdateBorderProjection(border))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapStroke(MirageBorderHandler handler, Border border)
    {
        if (!handler.UpdateBorderProjection(border))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapStrokeThickness(MirageBorderHandler handler, Border border)
    {
        if (!handler.UpdateBorderProjection(border))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapStrokeShape(MirageBorderHandler handler, Border border)
    {
        if (!handler.UpdateBorderProjection(border))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapStrokeDashArray(MirageBorderHandler handler, Border border)
    {
        if (!handler.UpdateBorderProjection(border))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapStrokeDashOffset(MirageBorderHandler handler, Border border)
    {
        if (!handler.UpdateBorderProjection(border))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapStrokeLineCap(MirageBorderHandler handler, Border border)
    {
        if (!handler.UpdateBorderProjection(border))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapStrokeLineJoin(MirageBorderHandler handler, Border border)
    {
        if (!handler.UpdateBorderProjection(border))
        {
            return;
        }

        handler.RequestRender();
    }

    private bool UpdateBorderProjection(Border border)
    {
        var changed = EnsureStrokeOverlayStructure();
        changed |= AttachStrokeOverlay();

        changed |= SetStyleIfChanged(PlatformView, "borderStyle", "none");
        changed |= SetStyleIfChanged(PlatformView, "borderWidth", null);
        changed |= SetStyleIfChanged(PlatformView, "borderColor", null);
        changed |= SetStyleIfChanged(PlatformView, "overflow", "hidden");
        changed |= SetStyleIfChanged(PlatformView, "overflowX", "hidden");
        changed |= SetStyleIfChanged(PlatformView, "overflowY", "hidden");

        var strokeThickness = NormalizeThickness(border.StrokeThickness);
        var hasValidArrangedFrame = HasValidArrangedFrame(_arrangedWidth, _arrangedHeight);
        var width = hasValidArrangedFrame ? _arrangedWidth : 0d;
        var height = hasValidArrangedFrame ? _arrangedHeight : 0d;

        var hasRenderableStroke = HasRenderableStroke(border.Stroke, strokeThickness);
        changed |= SetStyleIfChanged(
            PlatformView,
            "padding",
            MirageCommonViewProjection.ToPaddingThickness(BuildEffectivePadding(border.Padding, strokeThickness, hasRenderableStroke)));

        var projection = BuildShapeProjection(border.StrokeShape, width, height, strokeThickness, border);
        changed |= SetStyleIfChanged(PlatformView, "borderRadius", projection.BorderRadiusCss);
        changed |= SetStyleIfChanged(PlatformView, "clipPath", projection.ClipPathCss);
        changed |= SetStyleIfChanged(PlatformView, "-webkit-clip-path", projection.ClipPathCss);
        changed |= ApplyPresentedContentClipProjection(projection);

        changed |= SetPropertyIfChanged(
            PlatformView,
            "StrokeDashOffset",
            border.StrokeDashOffset > 0 && double.IsFinite(border.StrokeDashOffset)
                ? CssNumber(border.StrokeDashOffset)
                : null);

        if (_strokeOverlayNode is null || _strokePathNode is null || _strokeDefinitionsNode is null)
        {
            return changed;
        }

        if (!hasValidArrangedFrame)
        {
            changed |= SetStyleIfChanged(_strokeOverlayNode, "display", "none");
            changed |= SetAttributeIfChanged(_strokePathNode, "d", null);
            changed |= SetAttributeIfChanged(_strokePathNode, "stroke", null);
            changed |= SetAttributeIfChanged(_strokePathNode, "stroke-width", null);
            changed |= SetAttributeIfChanged(_strokePathNode, "stroke-dasharray", null);
            changed |= SetAttributeIfChanged(_strokePathNode, "stroke-dashoffset", null);
            changed |= SetAttributeIfChanged(_strokePathNode, "stroke-linecap", null);
            changed |= SetAttributeIfChanged(_strokePathNode, "stroke-linejoin", null);
            changed |= SetPropertyIfChanged(_strokeDefinitionsNode, "innerHTML", null);
            return changed;
        }

        string? strokeValue = null;
        string? definitionsMarkup = null;
        var hasStroke = hasRenderableStroke
            && !string.IsNullOrWhiteSpace(projection.StrokePath)
            && ResolveStrokeProjection(border.Stroke, border, out strokeValue, out definitionsMarkup);

        if (!hasStroke)
        {
            changed |= SetStyleIfChanged(_strokeOverlayNode, "display", "none");
            changed |= SetAttributeIfChanged(_strokePathNode, "d", null);
            changed |= SetAttributeIfChanged(_strokePathNode, "stroke", null);
            changed |= SetAttributeIfChanged(_strokePathNode, "stroke-width", null);
            changed |= SetAttributeIfChanged(_strokePathNode, "stroke-dasharray", null);
            changed |= SetAttributeIfChanged(_strokePathNode, "stroke-dashoffset", null);
            changed |= SetAttributeIfChanged(_strokePathNode, "stroke-linecap", null);
            changed |= SetAttributeIfChanged(_strokePathNode, "stroke-linejoin", null);
            changed |= SetPropertyIfChanged(_strokeDefinitionsNode, "innerHTML", null);
            return changed;
        }

        changed |= SetStyleIfChanged(_strokeOverlayNode, "display", "block");
        changed |= SetAttributeIfChanged(_strokeOverlayNode, "viewBox", BuildViewBox(width, height));
        changed |= SetAttributeIfChanged(_strokePathNode, "d", projection.StrokePath);
        changed |= SetAttributeIfChanged(_strokePathNode, "stroke", strokeValue);
        changed |= SetAttributeIfChanged(_strokePathNode, "stroke-width", CssNumber(strokeThickness));
        changed |= SetAttributeIfChanged(_strokePathNode, "stroke-linecap", ToSvgLineCap(border.StrokeLineCap));
        changed |= SetAttributeIfChanged(_strokePathNode, "stroke-linejoin", ToSvgLineJoin(border.StrokeLineJoin));
        changed |= SetAttributeIfChanged(_strokePathNode, "stroke-dasharray", ToDashArray(border.StrokeDashArray, strokeThickness));
        changed |= SetAttributeIfChanged(
            _strokePathNode,
            "stroke-dashoffset",
            border.StrokeDashOffset > 0 && double.IsFinite(border.StrokeDashOffset)
                ? CssNumber(border.StrokeDashOffset * strokeThickness)
                : null);
        changed |= SetPropertyIfChanged(_strokeDefinitionsNode, "innerHTML", definitionsMarkup);

        return changed;
    }

    private bool EnsureStrokeOverlayStructure()
    {
        if (_strokeOverlayNode is not null && _strokeDefinitionsNode is not null && _strokePathNode is not null)
        {
            return false;
        }

        _strokeOverlayNode = CreateInternalWebNode("svg", "stroke-overlay");
        _strokeDefinitionsNode = CreateInternalWebNode("defs", "stroke-defs");
        _strokePathNode = CreateInternalWebNode("path", "stroke-path");

        _strokeOverlayNode.SetAttribute("xmlns", "http://www.w3.org/2000/svg");
        _strokeOverlayNode.SetAttribute("aria-hidden", "true");
        _strokeOverlayNode.SetAttribute("preserveAspectRatio", "none");
        _strokeOverlayNode.SetStyle("position", "absolute");
        _strokeOverlayNode.SetStyle("left", "0");
        _strokeOverlayNode.SetStyle("top", "0");
        _strokeOverlayNode.SetStyle("width", "100%");
        _strokeOverlayNode.SetStyle("height", "100%");
        _strokeOverlayNode.SetStyle("pointerEvents", "none");
        _strokeOverlayNode.SetStyle("overflow", "visible");
        _strokeOverlayNode.SetStyle("zIndex", "1");

        _strokePathNode.SetAttribute("fill", "none");
        _strokePathNode.SetAttribute("vector-effect", "non-scaling-stroke");

        _strokeOverlayNode.AddChild(_strokeDefinitionsNode);
        _strokeOverlayNode.AddChild(_strokePathNode);

        return true;
    }

    private bool AttachStrokeOverlay()
    {
        if (_strokeOverlayNode is null)
        {
            return false;
        }

        var changed = false;
        if (!ReferenceEquals(_strokeOverlayNode.Parent, PlatformView))
        {
            PlatformView.AddChild(_strokeOverlayNode);
            changed = true;
        }

        if (!ReferenceEquals(PlatformView.Children.LastOrDefault(), _strokeOverlayNode))
        {
            PlatformView.RemoveChild(_strokeOverlayNode);
            PlatformView.AddChild(_strokeOverlayNode);
            changed = true;
        }

        return changed;
    }

    private void DisposeStrokeOverlayStructure()
    {
        if (_strokeOverlayNode is not null && ReferenceEquals(_strokeOverlayNode.Parent, PlatformView))
        {
            PlatformView.RemoveChild(_strokeOverlayNode);
        }

        if (Coordinator is { } coordinator)
        {
            if (_strokePathNode is not null)
            {
                coordinator.Unregister(_strokePathNode);
            }

            if (_strokeDefinitionsNode is not null)
            {
                coordinator.Unregister(_strokeDefinitionsNode);
            }

            if (_strokeOverlayNode is not null)
            {
                coordinator.Unregister(_strokeOverlayNode);
            }
        }

        _strokePathNode = null;
        _strokeDefinitionsNode = null;
        _strokeOverlayNode = null;
    }

    private MiragePlatformView CreateInternalWebNode(string elementName, string partName)
    {
        var node = MiragePlatformView.CreateWeb(elementName);
        node.SetAttribute("data-maui-border-part", partName);
        RegisterInternalNode(node);
        return node;
    }

    private void RegisterInternalNode(MiragePlatformView node)
    {
        if (Coordinator is { } coordinator)
        {
            coordinator.Register(node, eventTarget: null);
            return;
        }

        if (!string.IsNullOrWhiteSpace(node.NodeId))
        {
            return;
        }

        var localId = Interlocked.Increment(ref s_internalNodeIdCounter);
        node.AssignNodeId($"border-internal-{localId:D4}");
    }

    private static ShapeProjection BuildShapeProjection(IShape? shape, double width, double height, double strokeThickness, Border border)
    {
        if (width <= 0 || height <= 0)
        {
            return BuildShapeFallbackProjection(shape, border);
        }

        var inset = strokeThickness > 0 ? strokeThickness / 2d : 0d;
        var innerWidth = Math.Max(0, width - strokeThickness);
        var innerHeight = Math.Max(0, height - strokeThickness);

        if (shape is null || shape is Rectangle)
        {
            return new ShapeProjection(
                StrokePath: BuildRectanglePath(inset, inset, innerWidth, innerHeight),
                ClipPathCss: null,
                BorderRadiusCss: "0px");
        }

        if (shape is RoundRectangle roundRectangle)
        {
            var normalizedCorner = NormalizeCornerRadius(roundRectangle.CornerRadius, innerWidth, innerHeight);
            var strokePath = BuildRoundRectanglePath(inset, inset, innerWidth, innerHeight, normalizedCorner);
            var borderRadius =
                $"{MirageViewMapper.ToPx(normalizedCorner.TopLeft)} {MirageViewMapper.ToPx(normalizedCorner.TopRight)} {MirageViewMapper.ToPx(normalizedCorner.BottomRight)} {MirageViewMapper.ToPx(normalizedCorner.BottomLeft)}";
            var clipPath = BuildRoundRectangleClipPath(normalizedCorner);

            return new ShapeProjection(
                StrokePath: strokePath,
                ClipPathCss: clipPath,
                BorderRadiusCss: borderRadius);
        }

        if (shape is Ellipse)
        {
            return new ShapeProjection(
                StrokePath: BuildEllipsePath(inset, inset, innerWidth, innerHeight),
                ClipPathCss: "ellipse(50% 50% at 50% 50%)",
                BorderRadiusCss: null);
        }

        if (shape is Polygon polygon)
        {
            if (!TryGetPolygonPoints(polygon, out var points))
            {
                ReportUnsupported(nameof(Border.StrokeShape), border, shape);
                return new ShapeProjection(
                    StrokePath: BuildRectanglePath(inset, inset, innerWidth, innerHeight),
                    ClipPathCss: null,
                    BorderRadiusCss: "0px");
            }

            var strokePath = BuildPolygonPath(points, inset, inset, innerWidth, innerHeight);
            var clipPath = BuildPolygonClipPath(points);
            return new ShapeProjection(
                StrokePath: strokePath,
                ClipPathCss: clipPath,
                BorderRadiusCss: null);
        }

        ReportUnsupported(nameof(Border.StrokeShape), border, shape);
        return new ShapeProjection(
            StrokePath: BuildRectanglePath(inset, inset, innerWidth, innerHeight),
            ClipPathCss: null,
            BorderRadiusCss: "0px");
    }

    private static ShapeProjection BuildShapeFallbackProjection(IShape? shape, Border border)
    {
        if (shape is null || shape is Rectangle)
        {
            return new ShapeProjection(
                StrokePath: string.Empty,
                ClipPathCss: null,
                BorderRadiusCss: "0px");
        }

        if (shape is RoundRectangle roundRectangle)
        {
            var cornerRadius = SanitizeCornerRadius(roundRectangle.CornerRadius);
            var borderRadius =
                $"{MirageViewMapper.ToPx(cornerRadius.TopLeft)} {MirageViewMapper.ToPx(cornerRadius.TopRight)} {MirageViewMapper.ToPx(cornerRadius.BottomRight)} {MirageViewMapper.ToPx(cornerRadius.BottomLeft)}";
            return new ShapeProjection(
                StrokePath: string.Empty,
                ClipPathCss: BuildRoundRectangleClipPath(cornerRadius),
                BorderRadiusCss: borderRadius);
        }

        if (shape is Ellipse)
        {
            return new ShapeProjection(
                StrokePath: string.Empty,
                ClipPathCss: "ellipse(50% 50% at 50% 50%)",
                BorderRadiusCss: null);
        }

        if (shape is Polygon polygon && TryGetPolygonPoints(polygon, out var points))
        {
            return new ShapeProjection(
                StrokePath: string.Empty,
                ClipPathCss: BuildPolygonClipPath(points),
                BorderRadiusCss: null);
        }

        ReportUnsupported(nameof(Border.StrokeShape), border, shape);
        return ShapeProjection.Empty;
    }

    private bool ResolveStrokeProjection(Brush? stroke, Border border, out string? strokeValue, out string? definitionsMarkup)
    {
        strokeValue = null;
        definitionsMarkup = null;

        if (stroke is null)
        {
            return false;
        }

        if (stroke is SolidColorBrush solidColorBrush)
        {
            strokeValue = MirageCommonViewProjection.ToCssColor(solidColorBrush.Color);
            return !string.IsNullOrWhiteSpace(strokeValue);
        }

        if (stroke is LinearGradientBrush linearGradientBrush)
        {
            var gradientId = BuildGradientId();
            definitionsMarkup = BuildLinearGradientMarkup(gradientId, linearGradientBrush);
            strokeValue = $"url(#{gradientId})";
            return !string.IsNullOrWhiteSpace(definitionsMarkup);
        }

        ReportUnsupported(nameof(Border.Stroke), border, stroke);
        return false;
    }

    private string BuildGradientId()
    {
        var nodeId = string.IsNullOrWhiteSpace(PlatformView.NodeId)
            ? $"border-{Interlocked.Increment(ref s_internalNodeIdCounter):D4}"
            : PlatformView.NodeId;

        var builder = new StringBuilder(nodeId.Length + 16);
        builder.Append("border-stroke-");
        foreach (var c in nodeId)
        {
            builder.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-');
        }

        return builder.ToString();
    }

    private static string BuildLinearGradientMarkup(string gradientId, LinearGradientBrush linearGradientBrush)
    {
        var start = linearGradientBrush.StartPoint;
        var end = linearGradientBrush.EndPoint;

        var x1 = CssPercent(start.X);
        var y1 = CssPercent(start.Y);
        var x2 = CssPercent(end.X);
        var y2 = CssPercent(end.Y);

        var stops = linearGradientBrush.GradientStops;
        var orderedStops = stops is null || stops.Count == 0
            ? []
            : stops.OrderBy(stop => stop.Offset).ToArray();

        var markup = new StringBuilder(256);
        markup.Append("<linearGradient id=\"")
            .Append(gradientId)
            .Append("\" gradientUnits=\"objectBoundingBox\" x1=\"")
            .Append(x1)
            .Append("\" y1=\"")
            .Append(y1)
            .Append("\" x2=\"")
            .Append(x2)
            .Append("\" y2=\"")
            .Append(y2)
            .Append("\">");

        if (orderedStops.Length == 0)
        {
            markup.Append("<stop offset=\"0%\" stop-color=\"rgba(0,0,0,1)\"/>")
                .Append("<stop offset=\"100%\" stop-color=\"rgba(0,0,0,1)\"/>");
        }
        else
        {
            foreach (var stop in orderedStops)
            {
                var color = MirageCommonViewProjection.ToCssColor(stop.Color) ?? "rgba(0,0,0,1)";
                markup.Append("<stop offset=\"")
                    .Append(CssPercent(stop.Offset))
                    .Append("\" stop-color=\"")
                    .Append(color)
                    .Append("\"/>");
            }
        }

        markup.Append("</linearGradient>");
        return markup.ToString();
    }

    private static string BuildViewBox(double width, double height)
    {
        return $"0 0 {CssNumber(width)} {CssNumber(height)}";
    }

    private static string? ToDashArray(DoubleCollection? dashArray, double strokeThickness)
    {
        if (dashArray is null || dashArray.Count == 0)
        {
            return null;
        }

        var values = new List<string>(dashArray.Count);
        foreach (var entry in dashArray)
        {
            if (!double.IsFinite(entry) || entry <= 0)
            {
                continue;
            }

            values.Add(CssNumber(entry * strokeThickness));
        }

        return values.Count == 0
            ? null
            : string.Join(" ", values);
    }

    private static string BuildRectanglePath(double x, double y, double width, double height)
    {
        if (width <= 0 || height <= 0)
        {
            return string.Empty;
        }

        var left = CssNumber(x);
        var top = CssNumber(y);
        var right = CssNumber(x + width);
        var bottom = CssNumber(y + height);

        return $"M {left} {top} H {right} V {bottom} H {left} Z";
    }

    private static string BuildRoundRectanglePath(double x, double y, double width, double height, CornerRadius cornerRadius)
    {
        if (width <= 0 || height <= 0)
        {
            return string.Empty;
        }

        var topLeft = cornerRadius.TopLeft;
        var topRight = cornerRadius.TopRight;
        var bottomRight = cornerRadius.BottomRight;
        var bottomLeft = cornerRadius.BottomLeft;

        var right = x + width;
        var bottom = y + height;

        var builder = new StringBuilder(256);
        builder.Append("M ").Append(CssNumber(x + topLeft)).Append(' ').Append(CssNumber(y));
        builder.Append(" H ").Append(CssNumber(right - topRight));
        AppendArc(builder, topRight, right, y + topRight);
        builder.Append(" V ").Append(CssNumber(bottom - bottomRight));
        AppendArc(builder, bottomRight, right - bottomRight, bottom);
        builder.Append(" H ").Append(CssNumber(x + bottomLeft));
        AppendArc(builder, bottomLeft, x, bottom - bottomLeft);
        builder.Append(" V ").Append(CssNumber(y + topLeft));
        AppendArc(builder, topLeft, x + topLeft, y);
        builder.Append(" Z");
        return builder.ToString();
    }

    private static void AppendArc(StringBuilder builder, double radius, double x, double y)
    {
        if (radius <= 0)
        {
            builder.Append(" L ").Append(CssNumber(x)).Append(' ').Append(CssNumber(y));
            return;
        }

        var radiusValue = CssNumber(radius);
        builder.Append(" A ")
            .Append(radiusValue)
            .Append(' ')
            .Append(radiusValue)
            .Append(" 0 0 1 ")
            .Append(CssNumber(x))
            .Append(' ')
            .Append(CssNumber(y));
    }

    private static string BuildEllipsePath(double x, double y, double width, double height)
    {
        if (width <= 0 || height <= 0)
        {
            return string.Empty;
        }

        var rx = width / 2d;
        var ry = height / 2d;
        var cx = x + rx;
        var cy = y + ry;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"M {CssNumber(cx - rx)} {CssNumber(cy)} " +
            $"A {CssNumber(rx)} {CssNumber(ry)} 0 1 0 {CssNumber(cx + rx)} {CssNumber(cy)} " +
            $"A {CssNumber(rx)} {CssNumber(ry)} 0 1 0 {CssNumber(cx - rx)} {CssNumber(cy)} Z");
    }

    private static string BuildPolygonPath(IReadOnlyList<Point> points, double x, double y, double width, double height)
    {
        if (points.Count < 3 || width <= 0 || height <= 0)
        {
            return string.Empty;
        }

        var normalized = NormalizePolygonPoints(points);
        var builder = new StringBuilder(256);

        for (var i = 0; i < normalized.Count; i++)
        {
            var point = normalized[i];
            var projectedX = x + (point.X * width);
            var projectedY = y + (point.Y * height);
            builder.Append(i == 0 ? "M " : " L ")
                .Append(CssNumber(projectedX))
                .Append(' ')
                .Append(CssNumber(projectedY));
        }

        builder.Append(" Z");
        return builder.ToString();
    }

    private static string BuildPolygonClipPath(IReadOnlyList<Point> points)
    {
        if (points.Count < 3)
        {
            return "polygon(0% 0%, 100% 0%, 100% 100%, 0% 100%)";
        }

        var normalized = NormalizePolygonPoints(points);
        var coordinates = normalized
            .Select(point => $"{CssPercent(point.X)} {CssPercent(point.Y)}")
            .ToArray();

        return $"polygon({string.Join(", ", coordinates)})";
    }

    private static IReadOnlyList<Point> NormalizePolygonPoints(IReadOnlyList<Point> points)
    {
        var minX = points.Min(point => point.X);
        var maxX = points.Max(point => point.X);
        var minY = points.Min(point => point.Y);
        var maxY = points.Max(point => point.Y);

        var spanX = maxX - minX;
        var spanY = maxY - minY;

        if (Math.Abs(spanX) < 0.0001)
        {
            spanX = 1;
        }

        if (Math.Abs(spanY) < 0.0001)
        {
            spanY = 1;
        }

        var normalizedPoints = new List<Point>(points.Count);
        foreach (var point in points)
        {
            var normalizedX = (point.X - minX) / spanX;
            var normalizedY = (point.Y - minY) / spanY;
            normalizedPoints.Add(new Point(normalizedX, normalizedY));
        }

        return normalizedPoints;
    }

    private static CornerRadius NormalizeCornerRadius(CornerRadius cornerRadius, double width, double height)
    {
        var topLeft = ClampCornerRadius(cornerRadius.TopLeft);
        var topRight = ClampCornerRadius(cornerRadius.TopRight);
        var bottomRight = ClampCornerRadius(cornerRadius.BottomRight);
        var bottomLeft = ClampCornerRadius(cornerRadius.BottomLeft);

        var maxRadius = Math.Max(0, Math.Min(width, height) / 2d);
        topLeft = Math.Min(topLeft, maxRadius);
        topRight = Math.Min(topRight, maxRadius);
        bottomRight = Math.Min(bottomRight, maxRadius);
        bottomLeft = Math.Min(bottomLeft, maxRadius);

        var topScale = ScaleRadiusPair(width, topLeft, topRight);
        topLeft *= topScale;
        topRight *= topScale;

        var bottomScale = ScaleRadiusPair(width, bottomLeft, bottomRight);
        bottomLeft *= bottomScale;
        bottomRight *= bottomScale;

        var leftScale = ScaleRadiusPair(height, topLeft, bottomLeft);
        topLeft *= leftScale;
        bottomLeft *= leftScale;

        var rightScale = ScaleRadiusPair(height, topRight, bottomRight);
        topRight *= rightScale;
        bottomRight *= rightScale;

        return new CornerRadius(topLeft, topRight, bottomLeft, bottomRight);
    }

    private static CornerRadius SanitizeCornerRadius(CornerRadius cornerRadius)
    {
        return new CornerRadius(
            ClampCornerRadius(cornerRadius.TopLeft),
            ClampCornerRadius(cornerRadius.TopRight),
            ClampCornerRadius(cornerRadius.BottomLeft),
            ClampCornerRadius(cornerRadius.BottomRight));
    }

    private static string BuildRoundRectangleClipPath(CornerRadius cornerRadius)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"inset(0 round {MirageViewMapper.ToPx(cornerRadius.TopLeft)} {MirageViewMapper.ToPx(cornerRadius.TopRight)} {MirageViewMapper.ToPx(cornerRadius.BottomRight)} {MirageViewMapper.ToPx(cornerRadius.BottomLeft)})");
    }

    private static bool HasRenderableStroke(Brush? stroke, double strokeThickness)
    {
        if (strokeThickness <= 0)
        {
            return false;
        }

        return stroke is SolidColorBrush or LinearGradientBrush;
    }

    private static Thickness BuildEffectivePadding(Thickness padding, double strokeThickness, bool hasRenderableStroke)
    {
        if (!hasRenderableStroke || strokeThickness <= 0)
        {
            return padding;
        }

        return new Thickness(
            Math.Max(0, padding.Left + strokeThickness),
            Math.Max(0, padding.Top + strokeThickness),
            Math.Max(0, padding.Right + strokeThickness),
            Math.Max(0, padding.Bottom + strokeThickness));
    }

    private static double ScaleRadiusPair(double available, double first, double second)
    {
        var sum = first + second;
        if (sum <= 0.0001)
        {
            return 1;
        }

        return Math.Min(1, available / sum);
    }

    private static double ClampCornerRadius(double value)
    {
        return double.IsFinite(value)
            ? Math.Max(0, value)
            : 0;
    }

    private static bool TryGetPolygonPoints(Polygon polygon, out IReadOnlyList<Point> points)
    {
        var polygonPoints = polygon.Points;
        if (polygonPoints is null || polygonPoints.Count < 3)
        {
            points = [];
            return false;
        }

        points = polygonPoints
            .Select(point => new Point(point.X, point.Y))
            .ToArray();

        return points.Count >= 3;
    }

    private static string ToSvgLineCap(PenLineCap lineCap)
    {
        return lineCap switch
        {
            PenLineCap.Round => "round",
            PenLineCap.Square => "square",
            _ => "butt"
        };
    }

    private static string ToSvgLineJoin(PenLineJoin lineJoin)
    {
        return lineJoin switch
        {
            PenLineJoin.Round => "round",
            PenLineJoin.Bevel => "bevel",
            _ => "miter"
        };
    }

    private static bool NearlyEqual(double left, double right)
    {
        return Math.Abs(left - right) <= 0.0001;
    }

    private static bool HasValidArrangedFrame(double width, double height)
    {
        return double.IsFinite(width)
            && double.IsFinite(height)
            && width > 0
            && height > 0;
    }

    private static double NormalizeThickness(double strokeThickness)
    {
        if (!double.IsFinite(strokeThickness))
        {
            return 0;
        }

        return Math.Max(0, strokeThickness);
    }

    private static string CssPercent(double value)
    {
        return $"{CssNumber(Math.Clamp(value, 0, 1) * 100)}%";
    }

    private static string CssNumber(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static bool SetStyleIfChanged(MiragePlatformView view, string styleName, string? styleValue)
    {
        if (string.Equals(view.GetStyleValue(styleName), styleValue, StringComparison.Ordinal))
        {
            return false;
        }

        view.SetStyle(styleName, styleValue);
        return true;
    }

    private bool ApplyPresentedContentClipProjection(ShapeProjection projection)
    {
        if (!TryGetPresentedContentPlatformView(out var contentPlatformView))
        {
            return false;
        }

        _ = projection;

        // Border host projection owns clipping. Applying clip styles to the presented content
        // can desync geometry when content uses a different local size than the border frame.
        var changed = SetStyleIfChanged(contentPlatformView, "borderRadius", null);
        changed |= SetStyleIfChanged(contentPlatformView, "clipPath", null);
        changed |= SetStyleIfChanged(contentPlatformView, "-webkit-clip-path", null);
        changed |= SetStyleIfChanged(contentPlatformView, "overflow", null);
        return changed;
    }

    private static bool SetPropertyIfChanged(MiragePlatformView view, string propertyName, string? propertyValue)
    {
        if (string.Equals(view.GetPropertyString(propertyName), propertyValue, StringComparison.Ordinal))
        {
            return false;
        }

        view.SetProperty(propertyName, propertyValue);
        return true;
    }

    private static bool SetAttributeIfChanged(MiragePlatformView view, string attributeName, string? attributeValue)
    {
        if (view.Attributes.TryGetValue(attributeName, out var current)
            && string.Equals(current, attributeValue, StringComparison.Ordinal))
        {
            return false;
        }

        if (!view.Attributes.ContainsKey(attributeName) && attributeValue is null)
        {
            return false;
        }

        view.SetAttribute(attributeName, attributeValue);
        return true;
    }

    private static void ReportUnsupported(string propertyName, Border border, object? value)
    {
#if DEBUG
        var key = $"{propertyName}|{border.GetType().FullName}|{value?.GetType().FullName}";
        if (UnsupportedWarnings.Add(key))
        {
            Console.WriteLine(
                $"[Mirage] Unsupported border mapping for {propertyName} on {border.GetType().Name}. Value type: {value?.GetType().Name ?? "<null>"}.");
        }
#endif
    }

    private readonly record struct ShapeProjection(string StrokePath, string? ClipPathCss, string? BorderRadiusCss)
    {
        public static ShapeProjection Empty => new(string.Empty, null, null);
    }
}
