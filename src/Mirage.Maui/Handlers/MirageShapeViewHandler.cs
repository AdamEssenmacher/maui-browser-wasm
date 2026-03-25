using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Mirage.Core.Diagnostics;
using Mirage.Core.Semantics;
using Mirage.Maui.Images;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using MauiFillRule = Microsoft.Maui.Controls.Shapes.FillRule;
using MauiGraphicsShape = Microsoft.Maui.Graphics.IShape;
using MauiPath = Microsoft.Maui.Controls.Shapes.Path;
using MauiShape = Microsoft.Maui.Controls.Shapes.Shape;
using MauiShapeTransform = Microsoft.Maui.Controls.Shapes.Transform;
using MauiTranslateTransform = Microsoft.Maui.Controls.Shapes.TranslateTransform;

namespace Mirage.Maui.Handlers;

internal class MirageShapeViewHandler : MirageViewHandler<IShapeView>
{
    private static readonly TimeSpan AsyncImageProjectionRetryDelay = TimeSpan.FromMilliseconds(300);
    private static int s_internalNodeIdCounter;
    private static long s_instanceCounter;
    private static readonly HashSet<string> UnsupportedWarnings = [];
    private static readonly object UnsupportedWarningsGate = new();

    internal static readonly string[] ParityPropertyKeys =
    {
        nameof(IShapeView.Background),
        nameof(IShapeView.Shape),
        nameof(IShapeView.Aspect),
        nameof(IShapeView.Fill),
        nameof(IShapeView.Stroke),
        nameof(IShapeView.StrokeThickness),
        nameof(IShapeView.StrokeDashPattern),
        nameof(IShapeView.StrokeDashOffset),
        nameof(IShapeView.StrokeLineCap),
        nameof(IShapeView.StrokeLineJoin),
        nameof(IShapeView.StrokeMiterLimit)
    };

    internal static readonly string[] ParityCommandKeys = [];

    public static IPropertyMapper<IShapeView, MirageShapeViewHandler> Mapper =
        new PropertyMapper<IShapeView, MirageShapeViewHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(IShapeView.Background)] = MapBackground,
            [nameof(IShapeView.Shape)] = MapShape,
            [nameof(IShapeView.Aspect)] = MapAspect,
            [nameof(IShapeView.Fill)] = MapFill,
            [nameof(IShapeView.Stroke)] = MapStroke,
            [nameof(IShapeView.StrokeThickness)] = MapStrokeThickness,
            [nameof(IShapeView.StrokeDashPattern)] = MapStrokeDashPattern,
            [nameof(IShapeView.StrokeDashOffset)] = MapStrokeDashOffset,
            [nameof(IShapeView.StrokeLineCap)] = MapStrokeLineCap,
            [nameof(IShapeView.StrokeLineJoin)] = MapStrokeLineJoin,
            [nameof(IShapeView.StrokeMiterLimit)] = MapStrokeMiterLimit
        };

    public static CommandMapper<IShapeView, MirageShapeViewHandler> CommandMapper =
        new(ViewCommandMapper)
        {
        };

    private readonly string _instanceId = Interlocked.Increment(ref s_instanceCounter).ToString(CultureInfo.InvariantCulture);

    private MiragePlatformView? _svgNode;
    private MiragePlatformView? _defsNode;
    private MiragePlatformView? _pathNode;
    private double _arrangedWidth;
    private double _arrangedHeight;
    private int _paintDefinitionCounter;
    private IMirageImageSourceResolver? _imageSourceResolver;
    private readonly HashSet<string> _ownedObjectUrls = new(StringComparer.Ordinal);
    private readonly Dictionary<ImageSource, MirageImageSourceProjectionResult> _asyncImageProjectionCache = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<ImageSource> _asyncImageProjectionInFlight = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<ImageSource, DateTime> _asyncImageProjectionRetryNotBeforeUtc = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<ImageSource> _activeAsyncImageSources = new(ReferenceEqualityComparer.Instance);
    private readonly object _asyncImageProjectionSync = new();
    private CancellationTokenSource? _asyncImageProjectionCancellation;

    public MirageShapeViewHandler()
        : this(Mapper)
    {
    }

    protected MirageShapeViewHandler(IPropertyMapper mapper)
        : base(ComponentKind.ShapeView, mapper, CommandMapper)
    {
        DefaultDisplayStyle = "block";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        _asyncImageProjectionCancellation ??= new CancellationTokenSource();

        var changed = SetStyleIfChanged("display", "block");
        changed |= SetStyleIfChanged("position", "relative");
        changed |= SetStyleIfChanged("overflow", "hidden");
        changed |= SetStyleIfChanged("boxSizing", "border-box");
        changed |= SetStyleIfChanged("minWidth", "0px");
        changed |= SetStyleIfChanged("minHeight", "0px");
        changed |= EnsureShapeStructure();
        changed |= AttachShapeStructure();
        changed |= UpdateShapeProjection(VirtualView);

        base.ConnectHandler(platformView);

        if (changed)
        {
            RequestRender();
        }
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        _asyncImageProjectionCancellation?.Cancel();
        _asyncImageProjectionCancellation?.Dispose();
        _asyncImageProjectionCancellation = null;
        ReleaseCachedAsyncImageProjections();
        ReleaseOwnedObjectUrls();
        DisposeShapeStructure();
        _arrangedWidth = 0;
        _arrangedHeight = 0;
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

        if (!sizeChanged)
        {
            return;
        }

        if (!UpdateShapeProjection(VirtualView))
        {
            return;
        }

        RequestRender();
    }

    public static void MapBackground(MirageShapeViewHandler handler, IShapeView shapeView)
    {
        var changed = MirageCommonViewProjection.MapBackground(handler, shapeView);
        changed |= handler.UpdateShapeProjection(shapeView);
        if (changed)
        {
            handler.RequestRender();
        }
    }

    public static void MapShape(MirageShapeViewHandler handler, IShapeView shapeView)
    {
        handler.MapShapeCore(shapeView);
    }

    public static void MapAspect(MirageShapeViewHandler handler, IShapeView shapeView)
    {
        handler.MapShapeCore(shapeView);
    }

    public static void MapFill(MirageShapeViewHandler handler, IShapeView shapeView)
    {
        handler.MapShapeCore(shapeView);
    }

    public static void MapStroke(MirageShapeViewHandler handler, IShapeView shapeView)
    {
        handler.MapShapeCore(shapeView);
    }

    public static void MapStrokeThickness(MirageShapeViewHandler handler, IShapeView shapeView)
    {
        handler.MapShapeCore(shapeView);
    }

    public static void MapStrokeDashPattern(MirageShapeViewHandler handler, IShapeView shapeView)
    {
        handler.MapShapeCore(shapeView);
    }

    public static void MapStrokeDashOffset(MirageShapeViewHandler handler, IShapeView shapeView)
    {
        handler.MapShapeCore(shapeView);
    }

    public static void MapStrokeLineCap(MirageShapeViewHandler handler, IShapeView shapeView)
    {
        handler.MapShapeCore(shapeView);
    }

    public static void MapStrokeLineJoin(MirageShapeViewHandler handler, IShapeView shapeView)
    {
        handler.MapShapeCore(shapeView);
    }

    public static void MapStrokeMiterLimit(MirageShapeViewHandler handler, IShapeView shapeView)
    {
        handler.MapShapeCore(shapeView);
    }

    protected void MapShapeCore(IShapeView shapeView)
    {
        if (!UpdateShapeProjection(shapeView))
        {
            return;
        }

        RequestRender();
    }

    protected void MapShapeFromDerived(IShapeView shapeView)
    {
        MapShapeCore(shapeView);
    }

    private bool UpdateShapeProjection(IShapeView shapeView)
    {
        using var _ = BeginRenderBatch();

        UpdateActiveAsyncImageSources(shapeView);

        var changed = EnsureShapeStructure();
        changed |= AttachShapeStructure();

        if (_svgNode is null || _defsNode is null || _pathNode is null)
        {
            return changed;
        }

        var width = Math.Max(0, _arrangedWidth);
        var height = Math.Max(0, _arrangedHeight);
        if (width <= 0 || height <= 0)
        {
            var (fallbackWidth, fallbackHeight) = ResolveFallbackProjectionSize(shapeView);
            if (width <= 0 && fallbackWidth > 0)
            {
                width = fallbackWidth;
            }

            if (height <= 0 && fallbackHeight > 0)
            {
                height = fallbackHeight;
            }
        }

        if (width <= 0 || height <= 0)
        {
            changed |= SetStyleIfChanged(_svgNode, "display", "none");
            changed |= SetAttributeIfChanged(_svgNode, "viewBox", null);
            changed |= SetAttributeIfChanged(_pathNode, "d", null);
            changed |= SetAttributeIfChanged(_pathNode, "fill", null);
            changed |= SetAttributeIfChanged(_pathNode, "stroke", null);
            changed |= SetAttributeIfChanged(_pathNode, "stroke-width", null);
            changed |= SetAttributeIfChanged(_pathNode, "stroke-dasharray", null);
            changed |= SetAttributeIfChanged(_pathNode, "stroke-dashoffset", null);
            changed |= SetAttributeIfChanged(_pathNode, "stroke-linecap", null);
            changed |= SetAttributeIfChanged(_pathNode, "stroke-linejoin", null);
            changed |= SetAttributeIfChanged(_pathNode, "stroke-miterlimit", null);
            changed |= SetAttributeIfChanged(_pathNode, "transform", null);
            changed |= SetPropertyIfChanged(_defsNode, "innerHTML", null);
            SyncOwnedObjectUrls([]);
            return changed;
        }

        changed |= SetStyleIfChanged(_svgNode, "display", "block");

        if (!TryBuildPathProjection(shapeView, width, height, out var pathData, out var useEvenOddFillRule, out var projectionError))
        {
            changed |= SetAttributeIfChanged(_svgNode, "viewBox", null);
            changed |= SetAttributeIfChanged(_pathNode, "d", null);
            changed |= SetAttributeIfChanged(_pathNode, "fill", null);
            changed |= SetAttributeIfChanged(_pathNode, "stroke", null);
            changed |= SetAttributeIfChanged(_pathNode, "stroke-width", null);
            changed |= SetAttributeIfChanged(_pathNode, "stroke-dasharray", null);
            changed |= SetAttributeIfChanged(_pathNode, "stroke-dashoffset", null);
            changed |= SetAttributeIfChanged(_pathNode, "stroke-linecap", null);
            changed |= SetAttributeIfChanged(_pathNode, "stroke-linejoin", null);
            changed |= SetAttributeIfChanged(_pathNode, "stroke-miterlimit", null);
            changed |= SetAttributeIfChanged(_pathNode, "transform", null);
            changed |= SetPropertyIfChanged(_defsNode, "innerHTML", null);
            SyncOwnedObjectUrls([]);
            ReportUnsupported(nameof(IShapeView.Shape), shapeView, shapeView.Shape, projectionError ?? "shape projection failed");
            return changed;
        }

        var fillProjection = ResolveFillProjection(shapeView);
        var strokeThickness = NormalizeStrokeThickness(shapeView.StrokeThickness);
        var strokeProjection = strokeThickness > 0
            ? ResolveStrokeProjection(shapeView)
            : PaintProjection.None;

        if (!fillProjection.IsSupported && fillProjection.UnsupportedReason is not null)
        {
            ReportUnsupported(nameof(IShapeView.Fill), shapeView, shapeView.Fill, fillProjection.UnsupportedReason);
        }

        if (strokeThickness > 0 && !strokeProjection.IsSupported && strokeProjection.UnsupportedReason is not null)
        {
            ReportUnsupported(nameof(IShapeView.Stroke), shapeView, shapeView.Stroke, strokeProjection.UnsupportedReason);
        }

        var definitionsMarkup = BuildDefinitionsMarkup(fillProjection.DefinitionsMarkup, strokeProjection.DefinitionsMarkup);

        var nextOwnedObjectUrls = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(fillProjection.OwnedObjectUrl))
        {
            nextOwnedObjectUrls.Add(fillProjection.OwnedObjectUrl!);
        }

        if (!string.IsNullOrWhiteSpace(strokeProjection.OwnedObjectUrl))
        {
            nextOwnedObjectUrls.Add(strokeProjection.OwnedObjectUrl!);
        }

        SyncOwnedObjectUrls(nextOwnedObjectUrls);

        changed |= SetAttributeIfChanged(_svgNode, "viewBox", $"0 0 {CssNumber(width)} {CssNumber(height)}");
        changed |= SetAttributeIfChanged(_svgNode, "preserveAspectRatio", ResolvePreserveAspectRatio(shapeView.Aspect));
        changed |= SetAttributeIfChanged(_pathNode, "d", pathData);
        changed |= SetAttributeIfChanged(_pathNode, "fill-rule", useEvenOddFillRule ? "evenodd" : "nonzero");
        changed |= SetAttributeIfChanged(_pathNode, "transform", ResolvePathRenderTransform(shapeView));
        changed |= SetAttributeIfChanged(_pathNode, "fill", fillProjection.PaintValue ?? "none");

        if (strokeProjection.PaintValue is null || strokeThickness <= 0)
        {
            changed |= SetAttributeIfChanged(_pathNode, "stroke", "none");
            changed |= SetAttributeIfChanged(_pathNode, "stroke-width", null);
            changed |= SetAttributeIfChanged(_pathNode, "stroke-dasharray", null);
            changed |= SetAttributeIfChanged(_pathNode, "stroke-dashoffset", null);
            changed |= SetAttributeIfChanged(_pathNode, "stroke-linecap", null);
            changed |= SetAttributeIfChanged(_pathNode, "stroke-linejoin", null);
            changed |= SetAttributeIfChanged(_pathNode, "stroke-miterlimit", null);
        }
        else
        {
            changed |= SetAttributeIfChanged(_pathNode, "stroke", strokeProjection.PaintValue);
            changed |= SetAttributeIfChanged(_pathNode, "stroke-width", CssNumber(strokeThickness));
            changed |= SetAttributeIfChanged(_pathNode, "stroke-dasharray", ToDashArray(shapeView.StrokeDashPattern));
            changed |= SetAttributeIfChanged(
                _pathNode,
                "stroke-dashoffset",
                shapeView.StrokeDashOffset > 0 && double.IsFinite(shapeView.StrokeDashOffset)
                    ? CssNumber(shapeView.StrokeDashOffset)
                    : null);
            changed |= SetAttributeIfChanged(_pathNode, "stroke-linecap", ToSvgLineCap(shapeView.StrokeLineCap));
            changed |= SetAttributeIfChanged(_pathNode, "stroke-linejoin", ToSvgLineJoin(shapeView.StrokeLineJoin));
            changed |= SetAttributeIfChanged(
                _pathNode,
                "stroke-miterlimit",
                shapeView.StrokeMiterLimit > 0 && double.IsFinite(shapeView.StrokeMiterLimit)
                    ? CssNumber(shapeView.StrokeMiterLimit)
                    : null);
        }

        changed |= SetPropertyIfChanged(_defsNode, "innerHTML", definitionsMarkup);

        return changed;
    }

    private string? ResolvePathRenderTransform(IShapeView shapeView)
    {
        if (shapeView is not MauiPath path || path.RenderTransform is null)
        {
            return null;
        }

        if (TryBuildSvgPathTransform(path.RenderTransform, out var transformValue, out var error))
        {
            return transformValue;
        }

        ReportUnsupported(nameof(MauiPath.RenderTransform), path, path.RenderTransform, error ?? "shape render transform is not supported.");
        return null;
    }

    private static bool TryBuildSvgPathTransform(
        MauiShapeTransform transform,
        out string? transformValue,
        out string? error)
    {
        switch (transform)
        {
            case MauiTranslateTransform translate:
            {
                if (!double.IsFinite(translate.X) || !double.IsFinite(translate.Y))
                {
                    transformValue = null;
                    error = "TranslateTransform contains a non-finite coordinate.";
                    return false;
                }

                if (NearlyEqual(translate.X, 0) && NearlyEqual(translate.Y, 0))
                {
                    transformValue = null;
                    error = null;
                    return true;
                }

                transformValue = $"translate({CssNumber(translate.X)} {CssNumber(translate.Y)})";
                error = null;
                return true;
            }
            default:
            {
                transformValue = null;
                error = $"RenderTransform type '{transform.GetType().Name}' is not currently projected for shape paths.";
                return false;
            }
        }
    }

    private bool EnsureShapeStructure()
    {
        if (_svgNode is not null && _defsNode is not null && _pathNode is not null)
        {
            return false;
        }

        _svgNode = CreateInternalWebNode("svg", "shape-svg");
        _defsNode = CreateInternalWebNode("defs", "shape-defs");
        _pathNode = CreateInternalWebNode("path", "shape-path");

        _svgNode.AddClass("mirage-shape-svg");
        _pathNode.AddClass("mirage-shape-path");

        _svgNode.SetAttribute("xmlns", "http://www.w3.org/2000/svg");
        _svgNode.SetAttribute("aria-hidden", "true");
        _svgNode.SetAttribute("focusable", "false");
        _svgNode.SetAttribute("preserveAspectRatio", "none");

        _svgNode.SetStyle("position", "absolute");
        _svgNode.SetStyle("left", "0");
        _svgNode.SetStyle("top", "0");
        _svgNode.SetStyle("width", "100%");
        _svgNode.SetStyle("height", "100%");
        _svgNode.SetStyle("overflow", "visible");
        _svgNode.SetStyle("pointerEvents", "none");

        _pathNode.SetAttribute("fill", "none");
        _pathNode.SetAttribute("stroke", "none");

        _svgNode.AddChild(_defsNode);
        _svgNode.AddChild(_pathNode);

        return true;
    }

    private bool AttachShapeStructure()
    {
        if (_svgNode is null)
        {
            return false;
        }

        var changed = false;
        if (!ReferenceEquals(_svgNode.Parent, PlatformView))
        {
            PlatformView.AddChild(_svgNode);
            changed = true;
        }

        if (!ReferenceEquals(PlatformView.Children.LastOrDefault(), _svgNode))
        {
            PlatformView.RemoveChild(_svgNode);
            PlatformView.AddChild(_svgNode);
            changed = true;
        }

        return changed;
    }

    private void DisposeShapeStructure()
    {
        if (_svgNode is not null && ReferenceEquals(_svgNode.Parent, PlatformView))
        {
            PlatformView.RemoveChild(_svgNode);
        }

        _svgNode = null;
        _defsNode = null;
        _pathNode = null;
        _paintDefinitionCounter = 0;
    }

    private MiragePlatformView CreateInternalWebNode(string elementName, string partName)
    {
        var node = MiragePlatformView.CreateWeb(elementName);
        node.AssignNodeId($"internal-shape-{Interlocked.Increment(ref s_internalNodeIdCounter).ToString(CultureInfo.InvariantCulture)}");
        node.SetAttribute("data-maui-shape-part", partName);
        return node;
    }

    private PaintProjection ResolveFillProjection(IShapeView shapeView)
    {
        if (shapeView is MauiShape shape)
        {
            var projectionFromBrush = ResolveBrushProjection(shape.Fill, shapeView, "fill");
            if (!projectionFromBrush.IsEmpty || !shape.Fill.IsNullOrEmpty())
            {
                return projectionFromBrush;
            }
        }

        return ResolveGraphicsPaintProjection(shapeView.Fill, shapeView, "fill");
    }

    private PaintProjection ResolveStrokeProjection(IShapeView shapeView)
    {
        if (shapeView is MauiShape shape)
        {
            var projectionFromBrush = ResolveBrushProjection(shape.Stroke, shapeView, "stroke");
            if (!projectionFromBrush.IsEmpty || !shape.Stroke.IsNullOrEmpty())
            {
                return projectionFromBrush;
            }
        }

        return ResolveGraphicsPaintProjection(shapeView.Stroke, shapeView, "stroke");
    }

    private PaintProjection ResolveBrushProjection(Brush? brush, IShapeView ownerView, string paintRole)
    {
        if (brush is null || brush.IsEmpty)
        {
            return PaintProjection.None;
        }

        switch (brush)
        {
            case SolidColorBrush solidColorBrush:
                return new PaintProjection(MirageCommonViewProjection.ToCssColor(solidColorBrush.Color), null, null, true, null);
            case LinearGradientBrush linearGradientBrush:
            {
                if (!TryBuildGradientStops(linearGradientBrush.GradientStops, out var gradientStops))
                {
                    return new PaintProjection(null, null, null, false, "LinearGradientBrush does not contain renderable gradient stops.");
                }

                var gradientId = BuildPaintDefinitionId($"{paintRole}-linear");
                var markup = BuildLinearGradientMarkup(gradientId, linearGradientBrush.StartPoint, linearGradientBrush.EndPoint, gradientStops);
                return new PaintProjection($"url(#{gradientId})", markup, null, true, null);
            }
            case RadialGradientBrush radialGradientBrush:
            {
                if (!TryBuildGradientStops(radialGradientBrush.GradientStops, out var gradientStops))
                {
                    return new PaintProjection(null, null, null, false, "RadialGradientBrush does not contain renderable gradient stops.");
                }

                var gradientId = BuildPaintDefinitionId($"{paintRole}-radial");
                var markup = BuildRadialGradientMarkup(gradientId, radialGradientBrush.Center, radialGradientBrush.Radius, gradientStops);
                return new PaintProjection($"url(#{gradientId})", markup, null, true, null);
            }
            default:
            {
                if (!TryResolveImageBrushProjection(brush, ownerView, paintRole, out var imageProjection))
                {
                    return new PaintProjection(
                        null,
                        null,
                        null,
                        false,
                        $"Brush type '{brush.GetType().Name}' is not supported for shape {paintRole} projection.");
                }

                return imageProjection;
            }
        }
    }

    private PaintProjection ResolveGraphicsPaintProjection(Paint? paint, IShapeView ownerView, string paintRole)
    {
        if (paint is null)
        {
            return PaintProjection.None;
        }

        switch (paint)
        {
            case SolidPaint solidPaint:
                return new PaintProjection(MirageCommonViewProjection.ToCssColor(solidPaint.Color), null, null, true, null);
            case LinearGradientPaint linearGradientPaint:
            {
                if (!TryBuildGradientStops(linearGradientPaint.GradientStops, out var gradientStops))
                {
                    return new PaintProjection(null, null, null, false, "LinearGradientPaint does not contain renderable gradient stops.");
                }

                var gradientId = BuildPaintDefinitionId($"{paintRole}-linear-paint");
                var markup = BuildLinearGradientMarkup(gradientId, linearGradientPaint.StartPoint, linearGradientPaint.EndPoint, gradientStops);
                return new PaintProjection($"url(#{gradientId})", markup, null, true, null);
            }
            case RadialGradientPaint radialGradientPaint:
            {
                if (!TryBuildGradientStops(radialGradientPaint.GradientStops, out var gradientStops))
                {
                    return new PaintProjection(null, null, null, false, "RadialGradientPaint does not contain renderable gradient stops.");
                }

                var gradientId = BuildPaintDefinitionId($"{paintRole}-radial-paint");
                var markup = BuildRadialGradientMarkup(gradientId, radialGradientPaint.Center, radialGradientPaint.Radius, gradientStops);
                return new PaintProjection($"url(#{gradientId})", markup, null, true, null);
            }
            case ImagePaint:
            case PatternPaint:
                return new PaintProjection(null, null, null, false, $"Paint type '{paint.GetType().Name}' is not currently projected for shape {paintRole}.");
            default:
                return new PaintProjection(null, null, null, false, $"Paint type '{paint.GetType().Name}' is not supported for shape {paintRole} projection.");
        }
    }

    private bool TryResolveImageBrushProjection(Brush brush, IShapeView ownerView, string paintRole, out PaintProjection projection)
    {
        projection = PaintProjection.None;

        var imageSourceProperty = brush.GetType().GetProperty("ImageSource", BindingFlags.Public | BindingFlags.Instance);
        if (imageSourceProperty is null)
        {
            return false;
        }

        if (imageSourceProperty.GetValue(brush) is not ImageSource imageSource || imageSource.IsEmpty)
        {
            projection = PaintProjection.None;
            return true;
        }

        var requiresAsyncResolution = imageSource is StreamImageSource || RequiresManifestInitialization(imageSource);
        if (requiresAsyncResolution)
        {
            if (TryGetCachedAsyncImageProjection(imageSource, out var cachedProjection))
            {
                if (!cachedProjection.IsResolved || string.IsNullOrWhiteSpace(cachedProjection.BrowserUrl))
                {
                    QueueAsyncImageProjection(imageSource, allowRetryForCachedUnresolved: true);
                    projection = new PaintProjection(
                        null,
                        null,
                        null,
                        false,
                        cachedProjection.FailureReason ?? "Image source could not be resolved for shape paint projection.");
                    return true;
                }

                var cachedPatternId = BuildPaintDefinitionId($"{paintRole}-image");
                var cachedMarkup = BuildImagePatternMarkup(cachedPatternId, cachedProjection.BrowserUrl);
                projection = new PaintProjection(
                    $"url(#{cachedPatternId})",
                    cachedMarkup,
                    cachedProjection.IsManagedObjectUrl ? cachedProjection.BrowserUrl : null,
                    true,
                    null);
                return true;
            }

            QueueAsyncImageProjection(imageSource);
            projection = PaintProjection.None;
            return true;
        }

        var imageSourceResolver = GetImageSourceResolver();
        if (imageSourceResolver is null)
        {
            projection = new PaintProjection(
                null,
                null,
                null,
                false,
                "IMirageImageSourceResolver was not available.");
            return true;
        }

        try
        {
            var resolved = imageSourceResolver.Resolve(imageSource);
            if (!resolved.IsResolved || string.IsNullOrWhiteSpace(resolved.BrowserUrl))
            {
                projection = new PaintProjection(
                    null,
                    null,
                    null,
                    false,
                    resolved.FailureReason ?? "Image source could not be resolved for shape paint projection.");
                return true;
            }

            var patternId = BuildPaintDefinitionId($"{paintRole}-image");
            var markup = BuildImagePatternMarkup(patternId, resolved.BrowserUrl);
            projection = new PaintProjection(
                $"url(#{patternId})",
                markup,
                resolved.IsManagedObjectUrl ? resolved.BrowserUrl : null,
                true,
                null);
            return true;
        }
        catch (Exception ex)
        {
            projection = new PaintProjection(
                null,
                null,
                null,
                false,
                $"Image brush projection failed: {ex.GetType().Name}.");
            return true;
        }
    }

    private void QueueAsyncImageProjection(
        ImageSource imageSource,
        bool allowRetryForCachedUnresolved = false)
    {
        CancellationToken cancellationToken;
        lock (_asyncImageProjectionSync)
        {
            if (_asyncImageProjectionInFlight.Contains(imageSource))
            {
                return;
            }

            if (_asyncImageProjectionCache.TryGetValue(imageSource, out var cachedProjection))
            {
                if (!allowRetryForCachedUnresolved || cachedProjection.IsResolved)
                {
                    return;
                }

                if (_asyncImageProjectionRetryNotBeforeUtc.TryGetValue(imageSource, out var retryNotBeforeUtc)
                    && DateTime.UtcNow < retryNotBeforeUtc)
                {
                    return;
                }

                _asyncImageProjectionCache.Remove(imageSource);
            }

            _asyncImageProjectionInFlight.Add(imageSource);
            _asyncImageProjectionCancellation ??= new CancellationTokenSource();
            cancellationToken = _asyncImageProjectionCancellation.Token;
        }

        _ = ResolveAsyncImageProjectionCore(imageSource, cancellationToken);
    }

    private async Task ResolveAsyncImageProjectionCore(ImageSource imageSource, CancellationToken cancellationToken)
    {
        MirageImageSourceProjectionResult resolved;
        var resolver = GetImageSourceResolver();
        if (resolver is null)
        {
            resolved = MirageImageSourceProjectionResult.Unresolved(
                "unsupported",
                "IMirageImageSourceResolver was not available.");
        }
        else
        {
            try
            {
                if (RequiresManifestInitialization(imageSource))
                {
                    await resolver.EnsureInitializedAsync();
                }

                resolved = await resolver.ResolveAsync(imageSource, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                RemoveAsyncImageProjectionInFlight(imageSource);
                return;
            }
            catch (Exception ex)
            {
                resolved = MirageImageSourceProjectionResult.Unresolved(
                    "unsupported",
                    $"Image brush projection failed: {ex.GetType().Name}.");
            }
        }

        if (cancellationToken.IsCancellationRequested)
        {
            RemoveAsyncImageProjectionInFlight(imageSource);
            ReleaseIfManagedObjectUrl(resolved);
            return;
        }

        if (!IsActiveAsyncImageSource(imageSource))
        {
            RemoveAsyncImageProjectionInFlight(imageSource);
            RemoveAsyncImageProjectionFromCache(imageSource);
            ReleaseIfManagedObjectUrl(resolved);
            return;
        }

        CacheAsyncImageProjection(imageSource, resolved);
        if (resolved.IsResolved)
        {
            RequestAsyncProjectionRefresh();
        }
    }

    private bool TryGetCachedAsyncImageProjection(
        ImageSource imageSource,
        out MirageImageSourceProjectionResult projection)
    {
        lock (_asyncImageProjectionSync)
        {
            return _asyncImageProjectionCache.TryGetValue(imageSource, out projection);
        }
    }

    private void CacheAsyncImageProjection(ImageSource imageSource, MirageImageSourceProjectionResult projection)
    {
        MirageImageSourceProjectionResult? previousProjection = null;
        lock (_asyncImageProjectionSync)
        {
            _asyncImageProjectionInFlight.Remove(imageSource);
            if (_asyncImageProjectionCache.TryGetValue(imageSource, out var previous))
            {
                previousProjection = previous;
            }

            _asyncImageProjectionCache[imageSource] = projection;
            if (projection.IsResolved)
            {
                _asyncImageProjectionRetryNotBeforeUtc.Remove(imageSource);
            }
            else
            {
                _asyncImageProjectionRetryNotBeforeUtc[imageSource] = DateTime.UtcNow.Add(AsyncImageProjectionRetryDelay);
            }
        }

        if (previousProjection is { IsManagedObjectUrl: true, BrowserUrl: { Length: > 0 } previousUrl }
            && !string.Equals(previousUrl, projection.BrowserUrl, StringComparison.Ordinal))
        {
            GetImageSourceResolver()?.Release(previousUrl);
            InvalidateCachedAsyncImageProjectionByUrl(previousUrl);
        }
    }

    private void RemoveAsyncImageProjectionInFlight(ImageSource imageSource)
    {
        lock (_asyncImageProjectionSync)
        {
            _asyncImageProjectionInFlight.Remove(imageSource);
        }
    }

    private void RemoveAsyncImageProjectionFromCache(ImageSource imageSource)
    {
        lock (_asyncImageProjectionSync)
        {
            _asyncImageProjectionCache.Remove(imageSource);
            _asyncImageProjectionRetryNotBeforeUtc.Remove(imageSource);
        }
    }

    private bool IsActiveAsyncImageSource(ImageSource imageSource)
    {
        lock (_asyncImageProjectionSync)
        {
            return _activeAsyncImageSources.Contains(imageSource);
        }
    }

    private void RequestAsyncProjectionRefresh()
    {
        var dispatcher = MauiContext?.Services.GetService<IDispatcher>();
        if (dispatcher is not null && dispatcher.IsDispatchRequired)
        {
            if (dispatcher.Dispatch(RefreshFromAsyncProjection))
            {
                return;
            }
        }

        RefreshFromAsyncProjection();
    }

    private void RefreshFromAsyncProjection()
    {
        if (_svgNode is null || _defsNode is null || _pathNode is null)
        {
            return;
        }

        var shapeView = VirtualView;
        if (shapeView is null)
        {
            return;
        }

        MapShapeCore(shapeView);
    }

    private void ReleaseCachedAsyncImageProjections()
    {
        List<string>? urlsToRelease = null;
        lock (_asyncImageProjectionSync)
        {
            foreach (var projection in _asyncImageProjectionCache.Values)
            {
                if (!projection.IsManagedObjectUrl || string.IsNullOrWhiteSpace(projection.BrowserUrl))
                {
                    continue;
                }

                urlsToRelease ??= [];
                urlsToRelease.Add(projection.BrowserUrl!);
            }

            _asyncImageProjectionCache.Clear();
            _asyncImageProjectionInFlight.Clear();
            _asyncImageProjectionRetryNotBeforeUtc.Clear();
            _activeAsyncImageSources.Clear();
        }

        if (urlsToRelease is null || urlsToRelease.Count == 0)
        {
            return;
        }

        var resolver = GetImageSourceResolver();
        if (resolver is null)
        {
            return;
        }

        foreach (var url in urlsToRelease)
        {
            resolver.Release(url);
        }
    }

    private void ReleaseIfManagedObjectUrl(MirageImageSourceProjectionResult projection)
    {
        if (!projection.IsManagedObjectUrl || string.IsNullOrWhiteSpace(projection.BrowserUrl))
        {
            return;
        }

        GetImageSourceResolver()?.Release(projection.BrowserUrl);
    }

    private static bool RequiresManifestInitialization(ImageSource? source)
    {
        if (source is not FileImageSource fileImageSource)
        {
            return false;
        }

        return RequiresManifestLookup(fileImageSource.File);
    }

    private static bool RequiresManifestLookup(string? fileSource)
    {
        if (string.IsNullOrWhiteSpace(fileSource))
        {
            return false;
        }

        var trimmed = fileSource.Trim();
        return !(trimmed.StartsWith("/", StringComparison.Ordinal)
            || trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("blob:", StringComparison.OrdinalIgnoreCase));
    }

    private void UpdateActiveAsyncImageSources(IShapeView shapeView)
    {
        var nextSources = new HashSet<ImageSource>(ReferenceEqualityComparer.Instance);
        if (shapeView is MauiShape shape)
        {
            AddBrushImageSource(nextSources, shape.Fill);
            AddBrushImageSource(nextSources, shape.Stroke);
        }

        lock (_asyncImageProjectionSync)
        {
            _activeAsyncImageSources.Clear();
            foreach (var source in nextSources)
            {
                _activeAsyncImageSources.Add(source);
            }
        }
    }

    private static void AddBrushImageSource(HashSet<ImageSource> sources, Brush? brush)
    {
        if (brush is null || brush.IsEmpty)
        {
            return;
        }

        var imageSourceProperty = brush.GetType().GetProperty("ImageSource", BindingFlags.Public | BindingFlags.Instance);
        if (imageSourceProperty?.GetValue(brush) is not ImageSource imageSource || imageSource.IsEmpty)
        {
            return;
        }

        sources.Add(imageSource);
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

    private void SyncOwnedObjectUrls(HashSet<string> nextOwnedObjectUrls)
    {
        if (_ownedObjectUrls.Count == 0 && nextOwnedObjectUrls.Count == 0)
        {
            return;
        }

        var resolver = GetImageSourceResolver();
        if (resolver is null)
        {
            _ownedObjectUrls.Clear();
            return;
        }

        foreach (var existingUrl in _ownedObjectUrls.ToArray())
        {
            if (nextOwnedObjectUrls.Contains(existingUrl))
            {
                continue;
            }

            resolver.Release(existingUrl);
            InvalidateCachedAsyncImageProjectionByUrl(existingUrl);
            _ownedObjectUrls.Remove(existingUrl);
        }

        foreach (var nextUrl in nextOwnedObjectUrls)
        {
            _ownedObjectUrls.Add(nextUrl);
        }
    }

    private void ReleaseOwnedObjectUrls()
    {
        if (_ownedObjectUrls.Count == 0)
        {
            return;
        }

        var resolver = GetImageSourceResolver();
        if (resolver is not null)
        {
            foreach (var existingUrl in _ownedObjectUrls)
            {
                resolver.Release(existingUrl);
                InvalidateCachedAsyncImageProjectionByUrl(existingUrl);
            }
        }

        _ownedObjectUrls.Clear();
    }

    private void InvalidateCachedAsyncImageProjectionByUrl(string browserUrl)
    {
        if (string.IsNullOrWhiteSpace(browserUrl))
        {
            return;
        }

        lock (_asyncImageProjectionSync)
        {
            if (_asyncImageProjectionCache.Count == 0)
            {
                return;
            }

            var keysToRemove = _asyncImageProjectionCache
                .Where(entry =>
                    entry.Value.IsManagedObjectUrl
                    && string.Equals(entry.Value.BrowserUrl, browserUrl, StringComparison.Ordinal))
                .Select(entry => entry.Key)
                .ToArray();

            for (var index = 0; index < keysToRemove.Length; index++)
            {
                _asyncImageProjectionCache.Remove(keysToRemove[index]);
                _asyncImageProjectionRetryNotBeforeUtc.Remove(keysToRemove[index]);
            }
        }
    }

    private bool TryBuildPathProjection(
        IShapeView shapeView,
        double width,
        double height,
        out string pathData,
        out bool useEvenOddFillRule,
        out string? error)
    {
        pathData = string.Empty;
        useEvenOddFillRule = false;
        error = null;

        if (shapeView.Shape is not MauiGraphicsShape shapeGeometry)
        {
            error = "IShapeView.Shape is null or unsupported.";
            return false;
        }

        var bounds = new Rect(0, 0, width, height);

        PathF shapePath;
        try
        {
            shapePath = shapeGeometry.PathForBounds(bounds);
        }
        catch (Exception ex)
        {
            error = $"Shape path projection threw {ex.GetType().Name}.";
            return false;
        }

        PathF flattenedPath;
        try
        {
            flattenedPath = shapePath.GetFlattenedPath(1f, includeSubPaths: true);
        }
        catch
        {
            flattenedPath = shapePath;
        }

        pathData = BuildSvgPathData(flattenedPath);
        if (string.IsNullOrWhiteSpace(pathData))
        {
            error = "Shape path projection produced an empty path.";
            return false;
        }

        useEvenOddFillRule = ResolveUseEvenOddFillRule(shapeView);
        return true;
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
                {
                    if (!TryReadPoint(points, ref pointIndex, out var point))
                    {
                        continue;
                    }

                    AppendCommandPrefix(builder, 'M');
                    builder.Append(CssNumber(point.X)).Append(' ').Append(CssNumber(point.Y));
                    break;
                }
                case PathOperation.Line:
                {
                    if (!TryReadPoint(points, ref pointIndex, out var point))
                    {
                        continue;
                    }

                    AppendCommandPrefix(builder, 'L');
                    builder.Append(CssNumber(point.X)).Append(' ').Append(CssNumber(point.Y));
                    break;
                }
                case PathOperation.Quad:
                {
                    if (!TryReadPoint(points, ref pointIndex, out var control)
                        || !TryReadPoint(points, ref pointIndex, out var end))
                    {
                        continue;
                    }

                    AppendCommandPrefix(builder, 'Q');
                    builder.Append(CssNumber(control.X)).Append(' ').Append(CssNumber(control.Y)).Append(' ')
                        .Append(CssNumber(end.X)).Append(' ').Append(CssNumber(end.Y));
                    break;
                }
                case PathOperation.Cubic:
                {
                    if (!TryReadPoint(points, ref pointIndex, out var control1)
                        || !TryReadPoint(points, ref pointIndex, out var control2)
                        || !TryReadPoint(points, ref pointIndex, out var end))
                    {
                        continue;
                    }

                    AppendCommandPrefix(builder, 'C');
                    builder.Append(CssNumber(control1.X)).Append(' ').Append(CssNumber(control1.Y)).Append(' ')
                        .Append(CssNumber(control2.X)).Append(' ').Append(CssNumber(control2.Y)).Append(' ')
                        .Append(CssNumber(end.X)).Append(' ').Append(CssNumber(end.Y));
                    break;
                }
                case PathOperation.Arc:
                {
                    if (!TryReadPoint(points, ref pointIndex, out var arcEnd))
                    {
                        continue;
                    }

                    // Flattened path projections should avoid arc operations, but treat any residual arc as a line fallback.
                    AppendCommandPrefix(builder, 'L');
                    builder.Append(CssNumber(arcEnd.X)).Append(' ').Append(CssNumber(arcEnd.Y));
                    break;
                }
                case PathOperation.Close:
                    AppendCommandPrefix(builder, 'Z');
                    break;
                default:
                    break;
            }
        }

        return builder.ToString();
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

    private static void AppendCommandPrefix(StringBuilder builder, char command)
    {
        if (builder.Length > 0)
        {
            builder.Append(' ');
        }

        builder.Append(command).Append(' ');
    }

    private static bool ResolveUseEvenOddFillRule(IShapeView shapeView)
    {
        switch (shapeView)
        {
            case MauiPath path when path.Data is PathGeometry pathGeometry:
                return pathGeometry.FillRule == MauiFillRule.EvenOdd;
            case MauiPath path when path.Data is GeometryGroup geometryGroup:
                return geometryGroup.FillRule == MauiFillRule.EvenOdd;
            case Polygon polygon:
                return polygon.FillRule == MauiFillRule.EvenOdd;
            case Polyline polyline:
                return polyline.FillRule == MauiFillRule.EvenOdd;
        }

        return shapeView.Shape switch
        {
            PathGeometry pathGeometry => pathGeometry.FillRule == MauiFillRule.EvenOdd,
            GeometryGroup geometryGroup => geometryGroup.FillRule == MauiFillRule.EvenOdd,
            _ => false
        };
    }

    private string BuildPaintDefinitionId(string prefix)
    {
        var counter = Interlocked.Increment(ref _paintDefinitionCounter);
        return $"mirage-shape-{_instanceId}-{prefix}-{counter}";
    }

    private static string ResolvePreserveAspectRatio(PathAspect aspect)
    {
        return aspect switch
        {
            PathAspect.Stretch => "none",
            PathAspect.AspectFill => "xMidYMid slice",
            PathAspect.AspectFit => "xMidYMid meet",
            PathAspect.None => "xMinYMin meet",
            _ => "none"
        };
    }

    private static bool TryBuildGradientStops(
        GradientStopCollection? stops,
        out IReadOnlyList<GradientStopProjection> gradientStops)
    {
        gradientStops = [];
        if (stops is null || stops.Count == 0)
        {
            return false;
        }

        var projectedStops = new List<GradientStopProjection>();
        for (var index = 0; index < stops.Count; index++)
        {
            var stop = stops[index];
            if (stop is null)
            {
                continue;
            }

            var colorCss = MirageCommonViewProjection.ToCssColor(stop.Color);
            if (string.IsNullOrWhiteSpace(colorCss))
            {
                continue;
            }

            var offset = double.IsFinite(stop.Offset)
                ? Math.Clamp(stop.Offset, 0, 1)
                : 0;
            projectedStops.Add(new GradientStopProjection(index, offset, colorCss));
        }

        if (projectedStops.Count == 0)
        {
            return false;
        }

        if (projectedStops.Count == 1)
        {
            projectedStops.Add(new GradientStopProjection(projectedStops[0].Index + 1, 1, projectedStops[0].ColorCss));
        }

        gradientStops = projectedStops
            .OrderBy(static projection => projection.Offset)
            .ThenBy(static projection => projection.Index)
            .ToArray();
        return true;
    }

    private static bool TryBuildGradientStops(
        PaintGradientStop[]? stops,
        out IReadOnlyList<GradientStopProjection> gradientStops)
    {
        gradientStops = [];
        if (stops is null || stops.Length == 0)
        {
            return false;
        }

        var projectedStops = new List<GradientStopProjection>();
        for (var index = 0; index < stops.Length; index++)
        {
            var stop = stops[index];
            var colorCss = MirageCommonViewProjection.ToCssColor(stop.Color);
            if (string.IsNullOrWhiteSpace(colorCss))
            {
                continue;
            }

            var offset = double.IsFinite(stop.Offset)
                ? Math.Clamp(stop.Offset, 0, 1)
                : 0;
            projectedStops.Add(new GradientStopProjection(index, offset, colorCss));
        }

        if (projectedStops.Count == 0)
        {
            return false;
        }

        if (projectedStops.Count == 1)
        {
            projectedStops.Add(new GradientStopProjection(projectedStops[0].Index + 1, 1, projectedStops[0].ColorCss));
        }

        gradientStops = projectedStops
            .OrderBy(static projection => projection.Offset)
            .ThenBy(static projection => projection.Index)
            .ToArray();
        return true;
    }

    private static string BuildLinearGradientMarkup(
        string gradientId,
        Point startPoint,
        Point endPoint,
        IReadOnlyList<GradientStopProjection> gradientStops)
    {
        var startX = NormalizeUnit(startPoint.X, 0);
        var startY = NormalizeUnit(startPoint.Y, 0);
        var endX = NormalizeUnit(endPoint.X, 1);
        var endY = NormalizeUnit(endPoint.Y, 1);

        var markup = new StringBuilder();
        markup.Append("<linearGradient id='").Append(EscapeAttribute(gradientId)).Append("' gradientUnits='objectBoundingBox' x1='")
            .Append(CssNumber(startX * 100)).Append("%' y1='")
            .Append(CssNumber(startY * 100)).Append("%' x2='")
            .Append(CssNumber(endX * 100)).Append("%' y2='")
            .Append(CssNumber(endY * 100)).Append("%'>");

        foreach (var stop in gradientStops)
        {
            markup.Append("<stop offset='")
                .Append(CssNumber(stop.Offset * 100)).Append("%' stop-color='")
                .Append(EscapeAttribute(stop.ColorCss)).Append("'/>");
        }

        markup.Append("</linearGradient>");
        return markup.ToString();
    }

    private static string BuildRadialGradientMarkup(
        string gradientId,
        Point center,
        double radius,
        IReadOnlyList<GradientStopProjection> gradientStops)
    {
        var centerX = NormalizeUnit(center.X, 0.5);
        var centerY = NormalizeUnit(center.Y, 0.5);
        var normalizedRadius = NormalizeUnit(radius, 0.5);

        var markup = new StringBuilder();
        markup.Append("<radialGradient id='").Append(EscapeAttribute(gradientId)).Append("' gradientUnits='objectBoundingBox' cx='")
            .Append(CssNumber(centerX * 100)).Append("%' cy='")
            .Append(CssNumber(centerY * 100)).Append("%' r='")
            .Append(CssNumber(normalizedRadius * 100)).Append("%'>");

        foreach (var stop in gradientStops)
        {
            markup.Append("<stop offset='")
                .Append(CssNumber(stop.Offset * 100)).Append("%' stop-color='")
                .Append(EscapeAttribute(stop.ColorCss)).Append("'/>");
        }

        markup.Append("</radialGradient>");
        return markup.ToString();
    }

    private static string BuildImagePatternMarkup(string patternId, string imageUrl)
    {
        var escapedId = EscapeAttribute(patternId);
        var escapedUrl = EscapeAttribute(imageUrl);

        var markup = new StringBuilder();
        markup.Append("<pattern id='").Append(escapedId)
            .Append("' patternUnits='objectBoundingBox' patternContentUnits='objectBoundingBox' x='0' y='0' width='1' height='1'>");
        markup.Append("<image href='").Append(escapedUrl)
            .Append("' x='0' y='0' width='1' height='1' preserveAspectRatio='xMidYMid slice'/>");
        markup.Append("</pattern>");
        return markup.ToString();
    }

    private static string? BuildDefinitionsMarkup(string? fillDefinitions, string? strokeDefinitions)
    {
        if (string.IsNullOrWhiteSpace(fillDefinitions) && string.IsNullOrWhiteSpace(strokeDefinitions))
        {
            return null;
        }

        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(fillDefinitions))
        {
            builder.Append(fillDefinitions);
        }

        if (!string.IsNullOrWhiteSpace(strokeDefinitions))
        {
            builder.Append(strokeDefinitions);
        }

        return builder.ToString();
    }

    private static string? ToDashArray(float[]? dashPattern)
    {
        if (dashPattern is null || dashPattern.Length == 0)
        {
            return null;
        }

        var values = dashPattern
            .Where(static value => double.IsFinite(value) && value > 0)
            .Select(static value => CssNumber(value))
            .ToArray();
        return values.Length == 0
            ? null
            : string.Join(" ", values);
    }

    private static string ToSvgLineCap(LineCap lineCap)
    {
        return lineCap switch
        {
            LineCap.Round => "round",
            LineCap.Square => "square",
            _ => "butt"
        };
    }

    private static string ToSvgLineJoin(LineJoin lineJoin)
    {
        return lineJoin switch
        {
            LineJoin.Round => "round",
            LineJoin.Bevel => "bevel",
            _ => "miter"
        };
    }

    private static double NormalizeUnit(double value, double fallback)
    {
        if (!double.IsFinite(value))
        {
            return fallback;
        }

        return Math.Clamp(value, 0, 1);
    }

    private static double NormalizeStrokeThickness(double value)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            return 0;
        }

        return value;
    }

    private static bool NearlyEqual(double left, double right)
    {
        return Math.Abs(left - right) <= 0.001;
    }

    private static (double Width, double Height) ResolveFallbackProjectionSize(IShapeView shapeView)
    {
        var width = 0d;
        var height = 0d;

        if (shapeView is IView view)
        {
            width = PickPositiveFinite(view.Width, view.DesiredSize.Width);
            height = PickPositiveFinite(view.Height, view.DesiredSize.Height);
        }

        if (shapeView is VisualElement visualElement)
        {
            if (width <= 0)
            {
                width = PickPositiveFinite(visualElement.WidthRequest);
            }

            if (height <= 0)
            {
                height = PickPositiveFinite(visualElement.HeightRequest);
            }
        }

        return (width, height);
    }

    private static double PickPositiveFinite(params double[] values)
    {
        for (var index = 0; index < values.Length; index++)
        {
            var value = values[index];
            if (double.IsFinite(value) && value > 0)
            {
                return value;
            }
        }

        return 0;
    }

    private void ReportUnsupported(string propertyName, IShapeView owner, object? value, string reason)
    {
        var key = $"{propertyName}|{owner.GetType().FullName}|{value?.GetType().FullName}|{reason}";
        var shouldLog = false;
        lock (UnsupportedWarningsGate)
        {
            if (UnsupportedWarnings.Add(key))
            {
                shouldLog = true;
            }
        }

        if (!shouldLog)
        {
            return;
        }

        MirageDiagnostics.Log(
            "shape.map",
            $"Unsupported shape mapping: property={propertyName} view={owner.GetType().Name} value={value?.GetType().Name ?? "<null>"}. {reason}");
    }

    private bool SetStyleIfChanged(string styleName, string? value)
    {
        return SetStyleIfChanged(PlatformView, styleName, value);
    }

    private static bool SetStyleIfChanged(MiragePlatformView platformView, string styleName, string? value)
    {
        if (string.Equals(platformView.GetStyleValue(styleName), value, StringComparison.Ordinal))
        {
            return false;
        }

        platformView.SetStyle(styleName, value);
        return true;
    }

    private static bool SetPropertyIfChanged(MiragePlatformView platformView, string propertyName, string? propertyValue)
    {
        if (string.Equals(platformView.GetPropertyString(propertyName), propertyValue, StringComparison.Ordinal))
        {
            return false;
        }

        platformView.SetProperty(propertyName, propertyValue);
        return true;
    }

    private static bool SetAttributeIfChanged(MiragePlatformView platformView, string attributeName, string? value)
    {
        if (platformView.Attributes.TryGetValue(attributeName, out var current)
            && string.Equals(current, value, StringComparison.Ordinal))
        {
            return false;
        }

        if (!platformView.Attributes.ContainsKey(attributeName) && value is null)
        {
            return false;
        }

        platformView.SetAttribute(attributeName, value);
        return true;
    }

    private static string EscapeAttribute(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("'", "&apos;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
    }

    private static string CssNumber(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private readonly record struct GradientStopProjection(int Index, double Offset, string ColorCss);

    private readonly record struct PaintProjection(
        string? PaintValue,
        string? DefinitionsMarkup,
        string? OwnedObjectUrl,
        bool IsSupported,
        string? UnsupportedReason)
    {
        public static PaintProjection None => new(null, null, null, true, null);

        public bool IsEmpty =>
            PaintValue is null
            && string.IsNullOrWhiteSpace(DefinitionsMarkup)
            && string.IsNullOrWhiteSpace(OwnedObjectUrl)
            && IsSupported;
    }
}

internal static class BrushExtensions
{
    public static bool IsNullOrEmpty(this Brush? brush)
    {
        return brush is null || brush.IsEmpty;
    }
}
