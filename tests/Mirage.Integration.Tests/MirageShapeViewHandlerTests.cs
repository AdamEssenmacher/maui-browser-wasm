using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Mirage.Maui.Images;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Xunit;
using MauiGraphicsShape = Microsoft.Maui.Graphics.IShape;
using MauiPath = Microsoft.Maui.Controls.Shapes.Path;
using MauiShape = Microsoft.Maui.Controls.Shapes.Shape;

namespace Mirage.Integration.Tests;

public sealed class MirageShapeViewHandlerTests
{
    [Fact]
    public void ShapeViewHandler_ProjectsBuiltInControlKind_AndStableSvgStructure()
    {
        var handler = CreateHandler();
        var rectangle = new Rectangle
        {
            Fill = Brush.Coral,
            Stroke = Brush.Black,
            StrokeThickness = 2
        };

        ((IElementHandler)handler).SetVirtualView(rectangle);
        handler.PlatformArrange(new Rect(0, 0, 160, 90));

        Assert.Equal(ComponentKind.ShapeView, handler.PlatformView.ControlKind);
        Assert.Equal("block", handler.PlatformView.GetStyleValue("display"));

        var svgNode = GetSvgNode(handler);
        var defsNode = GetDefsNode(svgNode);
        var pathNode = GetPathNode(svgNode);

        Assert.NotNull(svgNode);
        Assert.NotNull(defsNode);
        Assert.NotNull(pathNode);
        Assert.Equal("svg", svgNode.ElementName);
        Assert.Equal("path", pathNode.ElementName);
        Assert.Equal("0 0 160 90", svgNode.Attributes["viewBox"]);
        Assert.False(string.IsNullOrWhiteSpace(pathNode.Attributes["d"]));
    }

    [Fact]
    public void ShapeViewHandler_LineShape_ProjectsStrokeAttributesDeterministically()
    {
        var handler = CreateLineHandler();
        var line = new Line
        {
            X1 = 0,
            Y1 = 0,
            X2 = 120,
            Y2 = 80,
            Stroke = Brush.Red,
            StrokeThickness = 4,
            StrokeDashArray = new DoubleCollection { 2, 3 },
            StrokeDashOffset = 1,
            StrokeLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Bevel
        };

        ((IElementHandler)handler).SetVirtualView(line);
        handler.PlatformArrange(new Rect(0, 0, 120, 80));

        var pathNode = GetPathNode(GetSvgNode(handler));
        Assert.Equal("rgba(255,0,0,1)", pathNode.Attributes["stroke"]);
        Assert.Equal("4", pathNode.Attributes["stroke-width"]);
        Assert.Equal("2 3", pathNode.Attributes["stroke-dasharray"]);
        Assert.Equal("1", pathNode.Attributes["stroke-dashoffset"]);
        Assert.Equal("round", pathNode.Attributes["stroke-linecap"]);
        Assert.Equal("bevel", pathNode.Attributes["stroke-linejoin"]);

        line.StrokeDashOffset = -2;
        MirageLineHandler.Mapper.UpdateProperty(handler, line, nameof(IShapeView.StrokeDashOffset));
        Assert.Equal("-2", pathNode.Attributes["stroke-dashoffset"]);
    }

    [Fact]
    public void ShapeViewHandler_PathShape_ProjectsDeterministicSvgPath()
    {
        var handler = CreatePathHandler();
        var path = new MauiPath
        {
            Data = new PathGeometry
            {
                FillRule = FillRule.EvenOdd,
                Figures =
                [
                    new PathFigure
                    {
                        StartPoint = new Point(10, 10),
                        IsClosed = true,
                        Segments =
                        [
                            new LineSegment { Point = new Point(90, 10) },
                            new LineSegment { Point = new Point(90, 70) },
                            new LineSegment { Point = new Point(10, 70) }
                        ]
                    }
                ]
            },
            Fill = Brush.Blue
        };

        ((IElementHandler)handler).SetVirtualView(path);
        handler.PlatformArrange(new Rect(0, 0, 100, 80));

        var svgNode = GetSvgNode(handler);
        var pathNode = GetPathNode(svgNode);
        Assert.Equal("evenodd", pathNode.Attributes["fill-rule"]);
        Assert.Contains("M ", pathNode.Attributes["d"]);
        Assert.Contains("L ", pathNode.Attributes["d"]);
        Assert.Contains("Z", pathNode.Attributes["d"]);

        path.Data = new PathGeometry
        {
            FillRule = FillRule.Nonzero,
            Figures =
            [
                new PathFigure
                {
                    StartPoint = new Point(5, 5),
                    IsClosed = false,
                    Segments =
                    [
                        new LineSegment { Point = new Point(95, 75) }
                    ]
                }
            ]
        };

        MiragePathHandler.Mapper.UpdateProperty(handler, path, nameof(MauiPath.Data));
        Assert.Equal("nonzero", pathNode.Attributes["fill-rule"]);
        Assert.Contains("95", pathNode.Attributes["d"]);

        var beforeTransform = pathNode.Attributes.TryGetValue("transform", out var beforeTransformValue)
            ? beforeTransformValue
            : null;
        path.RenderTransform = new TranslateTransform
        {
            X = 12,
            Y = 6
        };

        MiragePathHandler.Mapper.UpdateProperty(handler, path, nameof(MauiPath.RenderTransform));
        Assert.Equal("translate(12 6)", pathNode.Attributes["transform"]);
        Assert.NotEqual(beforeTransform, pathNode.Attributes["transform"]);

        path.RenderTransform = new ScaleTransform
        {
            ScaleX = 1.2,
            ScaleY = 0.8
        };

        MiragePathHandler.Mapper.UpdateProperty(handler, path, nameof(MauiPath.RenderTransform));
        Assert.False(pathNode.Attributes.ContainsKey("transform"));
    }

    [Fact]
    public void ShapeViewHandler_AspectProjection_MapsPreserveAspectRatioModes()
    {
        var handler = CreateHandler();
        var rectangle = new Rectangle
        {
            Fill = Brush.Green
        };

        ((IElementHandler)handler).SetVirtualView(rectangle);
        handler.PlatformArrange(new Rect(0, 0, 200, 100));

        var svgNode = GetSvgNode(handler);
        Assert.Equal("none", svgNode.Attributes["preserveAspectRatio"]);

        rectangle.Aspect = Stretch.Uniform;
        MirageShapeViewHandler.Mapper.UpdateProperty(handler, rectangle, nameof(IShapeView.Aspect));
        Assert.Equal("xMidYMid meet", svgNode.Attributes["preserveAspectRatio"]);

        rectangle.Aspect = Stretch.UniformToFill;
        MirageShapeViewHandler.Mapper.UpdateProperty(handler, rectangle, nameof(IShapeView.Aspect));
        Assert.Equal("xMidYMid slice", svgNode.Attributes["preserveAspectRatio"]);

        rectangle.Aspect = Stretch.None;
        MirageShapeViewHandler.Mapper.UpdateProperty(handler, rectangle, nameof(IShapeView.Aspect));
        Assert.Equal("xMinYMin meet", svgNode.Attributes["preserveAspectRatio"]);
    }

    [Fact]
    public void ShapeViewHandler_PolygonPointsCollectionChanges_UpdatePathWithoutReconnect()
    {
        var handler = CreatePolygonHandler();
        var polygon = new Polygon
        {
            Fill = Brush.Orange,
            Points =
            [
                new Point(0, 0),
                new Point(80, 0),
                new Point(40, 40)
            ]
        };

        ((IElementHandler)handler).SetVirtualView(polygon);
        handler.PlatformArrange(new Rect(0, 0, 100, 60));

        var svgNodeBefore = GetSvgNode(handler);
        var pathNodeBefore = GetPathNode(svgNodeBefore);
        var pathDataBefore = pathNodeBefore.Attributes["d"];

        polygon.Points.Add(new Point(0, 40));

        var svgNodeAfter = GetSvgNode(handler);
        var pathNodeAfter = GetPathNode(svgNodeAfter);
        var pathDataAfter = pathNodeAfter.Attributes["d"];

        Assert.Same(svgNodeBefore, svgNodeAfter);
        Assert.Same(pathNodeBefore, pathNodeAfter);
        Assert.NotEqual(pathDataBefore, pathDataAfter);
    }

    [Fact]
    public void ShapeViewHandler_PolygonPointsReplacement_RebindsCollectionChangedUpdates()
    {
        var handler = CreatePolygonHandler();
        var polygon = new Polygon
        {
            Fill = Brush.Orange,
            Points =
            [
                new Point(0, 0),
                new Point(80, 0),
                new Point(40, 40)
            ]
        };

        ((IElementHandler)handler).SetVirtualView(polygon);
        handler.PlatformArrange(new Rect(0, 0, 100, 60));

        var pathNode = GetPathNode(GetSvgNode(handler));
        var pathDataBeforeReplacement = pathNode.Attributes["d"];
        var oldPoints = polygon.Points;

        polygon.Points =
        [
            new Point(2, 2),
            new Point(92, 10),
            new Point(12, 52)
        ];
        MiragePolygonHandler.Mapper.UpdateProperty(handler, polygon, nameof(Polygon.Points));
        var pathDataAfterReplacement = pathNode.Attributes["d"];
        Assert.NotEqual(pathDataBeforeReplacement, pathDataAfterReplacement);

        oldPoints.Add(new Point(96, 58));
        Assert.Equal(pathDataAfterReplacement, pathNode.Attributes["d"]);

        polygon.Points.Add(new Point(4, 42));
        var pathDataAfterMutation = pathNode.Attributes["d"];
        Assert.NotEqual(pathDataAfterReplacement, pathDataAfterMutation);
    }

    [Fact]
    public void ShapeViewHandler_PolylinePointsCollectionChanges_UpdatePathWithoutReconnect()
    {
        var handler = CreatePolylineHandler();
        var polyline = new Polyline
        {
            Fill = Brush.Orange,
            Points =
            [
                new Point(0, 0),
                new Point(80, 0),
                new Point(40, 40)
            ]
        };

        ((IElementHandler)handler).SetVirtualView(polyline);
        handler.PlatformArrange(new Rect(0, 0, 100, 60));

        var svgNodeBefore = GetSvgNode(handler);
        var pathNodeBefore = GetPathNode(svgNodeBefore);
        var pathDataBefore = pathNodeBefore.Attributes["d"];

        polyline.Points.Add(new Point(0, 40));

        var svgNodeAfter = GetSvgNode(handler);
        var pathNodeAfter = GetPathNode(svgNodeAfter);
        var pathDataAfter = pathNodeAfter.Attributes["d"];

        Assert.Same(svgNodeBefore, svgNodeAfter);
        Assert.Same(pathNodeBefore, pathNodeAfter);
        Assert.NotEqual(pathDataBefore, pathDataAfter);
    }

    [Fact]
    public void ShapeViewHandler_PolylinePointsReplacement_RebindsCollectionChangedUpdates()
    {
        var handler = CreatePolylineHandler();
        var polyline = new Polyline
        {
            Fill = Brush.Orange,
            Points =
            [
                new Point(0, 0),
                new Point(80, 0),
                new Point(40, 40)
            ]
        };

        ((IElementHandler)handler).SetVirtualView(polyline);
        handler.PlatformArrange(new Rect(0, 0, 100, 60));

        var pathNode = GetPathNode(GetSvgNode(handler));
        var pathDataBeforeReplacement = pathNode.Attributes["d"];
        var oldPoints = polyline.Points;

        polyline.Points =
        [
            new Point(10, 3),
            new Point(95, 16),
            new Point(6, 51)
        ];
        MiragePolylineHandler.Mapper.UpdateProperty(handler, polyline, nameof(Polyline.Points));
        var pathDataAfterReplacement = pathNode.Attributes["d"];
        Assert.NotEqual(pathDataBeforeReplacement, pathDataAfterReplacement);

        oldPoints.Add(new Point(102, 57));
        Assert.Equal(pathDataAfterReplacement, pathNode.Attributes["d"]);

        polyline.Points.Add(new Point(9, 44));
        var pathDataAfterMutation = pathNode.Attributes["d"];
        Assert.NotEqual(pathDataAfterReplacement, pathDataAfterMutation);
    }

    [Fact]
    public void ShapeViewHandler_ProjectsSolidLinearRadialAndImageBrushPaints()
    {
        var resolver = new StubImageSourceResolver();
        resolver.SetResolved("https://example.com/shape-fill.png", "/_mirage/assets/test-shape-fill.png");

        var handler = CreateHandler(services => services.AddSingleton<IMirageImageSourceResolver>(resolver));

        var rectangle = new Rectangle
        {
            Fill = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Colors.Red, 0),
                    new GradientStop(Colors.Blue, 1)
                },
                new Point(0, 0),
                new Point(1, 1)),
            Stroke = new RadialGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Colors.White, 0),
                    new GradientStop(Colors.Black, 1)
                },
                new Point(0.5, 0.5),
                0.5),
            StrokeThickness = 3
        };

        ((IElementHandler)handler).SetVirtualView(rectangle);
        handler.PlatformArrange(new Rect(0, 0, 120, 80));

        var svgNode = GetSvgNode(handler);
        var defsNode = GetDefsNode(svgNode);
        var pathNode = GetPathNode(svgNode);

        Assert.Contains("linearGradient", defsNode.GetPropertyString("innerHTML"));
        Assert.Contains("radialGradient", defsNode.GetPropertyString("innerHTML"));
        Assert.StartsWith("url(#", pathNode.Attributes["fill"]);
        Assert.StartsWith("url(#", pathNode.Attributes["stroke"]);

        var imageBrush = CreateImageBrush("https://example.com/shape-fill.png");
        rectangle.Fill = imageBrush;
        MirageShapeViewHandler.Mapper.UpdateProperty(handler, rectangle, nameof(IShapeView.Fill));

        Assert.Contains("pattern", defsNode.GetPropertyString("innerHTML"));
        Assert.Contains("/_mirage/assets/test-shape-fill.png", defsNode.GetPropertyString("innerHTML"));
    }

    [Fact]
    public async Task ShapeViewHandler_ImageBrushWithStreamSource_UsesAsyncResolverPath()
    {
        var resolver = new StubImageSourceResolver();
        resolver.SetPendingStreamResolution();
        var handler = CreateHandler(services => services.AddSingleton<IMirageImageSourceResolver>(resolver));

        var rectangle = new Rectangle
        {
            Fill = CreateImageBrushFromStream()
        };

        ((IElementHandler)handler).SetVirtualView(rectangle);
        handler.PlatformArrange(new Rect(0, 0, 160, 90));

        var defsNode = GetDefsNode(GetSvgNode(handler));
        var pathNode = GetPathNode(GetSvgNode(handler));
        Assert.DoesNotContain("pattern", defsNode.GetPropertyString("innerHTML"));
        Assert.Equal("none", pathNode.Attributes["fill"]);

        resolver.CompletePendingStreamResolution("/_mirage/assets/test-shape-stream.png");

        await WaitForAsync(
            () =>
            {
                var markup = defsNode.GetPropertyString("innerHTML");
                return !string.IsNullOrEmpty(markup)
                    && markup.Contains("/_mirage/assets/test-shape-stream.png", StringComparison.Ordinal);
            },
            timeout: TimeSpan.FromSeconds(2));
        Assert.Contains("pattern", defsNode.GetPropertyString("innerHTML"));
        Assert.StartsWith("url(#", pathNode.Attributes["fill"], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShapeViewHandler_ImageBrushWithManifestFileSource_UsesAsyncResolverPath()
    {
        var resolver = new StubImageSourceResolver();
        resolver.SetPendingManifestInitialization();
        resolver.SetResolvedFile("/_mirage/assets/test-shape-file.png");
        var handler = CreateHandler(services => services.AddSingleton<IMirageImageSourceResolver>(resolver));

        var rectangle = new Rectangle
        {
            Fill = CreateImageBrushFromFile("dotnet_bot.png")
        };

        ((IElementHandler)handler).SetVirtualView(rectangle);
        handler.PlatformArrange(new Rect(0, 0, 160, 90));

        var defsNode = GetDefsNode(GetSvgNode(handler));
        var pathNode = GetPathNode(GetSvgNode(handler));
        Assert.DoesNotContain("pattern", defsNode.GetPropertyString("innerHTML"));
        Assert.Equal("none", pathNode.Attributes["fill"]);

        await WaitForAsync(() => resolver.EnsureInitializedAsyncCallCount >= 1, timeout: TimeSpan.FromSeconds(2));
        resolver.CompletePendingManifestInitialization();

        await WaitForAsync(
            () =>
            {
                var markup = defsNode.GetPropertyString("innerHTML");
                return !string.IsNullOrEmpty(markup)
                    && markup.Contains("/_mirage/assets/test-shape-file.png", StringComparison.Ordinal);
            },
            timeout: TimeSpan.FromSeconds(2));

        Assert.True(resolver.FileResolveAsyncCallCount >= 1);
        Assert.StartsWith("url(#", pathNode.Attributes["fill"], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShapeViewHandler_StreamImageBrushReuse_ReResolvesAfterManagedUrlRelease()
    {
        var resolver = new StubImageSourceResolver();
        resolver.SetPendingStreamResolution();
        var handler = CreateHandler(services => services.AddSingleton<IMirageImageSourceResolver>(resolver));

        var streamBrush = CreateImageBrushFromStream();
        var rectangle = new Rectangle
        {
            Fill = streamBrush
        };

        ((IElementHandler)handler).SetVirtualView(rectangle);
        handler.PlatformArrange(new Rect(0, 0, 160, 90));

        var defsNode = GetDefsNode(GetSvgNode(handler));
        var pathNode = GetPathNode(GetSvgNode(handler));
        Assert.Equal("none", pathNode.Attributes["fill"]);

        resolver.CompletePendingStreamResolution("/_mirage/assets/test-shape-stream-first.png");
        await WaitForAsync(
            () =>
            {
                var markup = defsNode.GetPropertyString("innerHTML");
                return !string.IsNullOrEmpty(markup)
                    && markup.Contains("/_mirage/assets/test-shape-stream-first.png", StringComparison.Ordinal);
            },
            timeout: TimeSpan.FromSeconds(2));
        Assert.True(resolver.StreamResolveAsyncCallCount >= 1);

        rectangle.Fill = Brush.Green;
        MirageShapeViewHandler.Mapper.UpdateProperty(handler, rectangle, nameof(IShapeView.Fill));

        resolver.SetPendingStreamResolution();
        rectangle.Fill = streamBrush;
        MirageShapeViewHandler.Mapper.UpdateProperty(handler, rectangle, nameof(IShapeView.Fill));
        Assert.DoesNotContain(
            "/_mirage/assets/test-shape-stream-first.png",
            defsNode.GetPropertyString("innerHTML"),
            StringComparison.Ordinal);

        resolver.CompletePendingStreamResolution("/_mirage/assets/test-shape-stream-second.png");
        await WaitForAsync(
            () =>
            {
                var markup = defsNode.GetPropertyString("innerHTML");
                return !string.IsNullOrEmpty(markup)
                    && markup.Contains("/_mirage/assets/test-shape-stream-second.png", StringComparison.Ordinal);
            },
            timeout: TimeSpan.FromSeconds(2));

        Assert.DoesNotContain(
            "/_mirage/assets/test-shape-stream-first.png",
            defsNode.GetPropertyString("innerHTML"),
            StringComparison.Ordinal);
        Assert.True(resolver.StreamResolveAsyncCallCount >= 2);
    }

    [Fact]
    public void ShapeViewHandler_AsyncImageSourceKey_ChangesWhenStreamSourceMutates()
    {
        var mutableBrush = CreateImageBrushFromStream();
        var streamSource = Assert.IsType<StreamImageSource>(GetImageBrushSource(mutableBrush));
        var key1 = CreateAsyncImageSourceKeyForTest(streamSource);
        streamSource.Stream = _ => Task.FromResult<Stream>(new MemoryStream([0x2, 0x3, 0x4]));
        var key2 = CreateAsyncImageSourceKeyForTest(streamSource);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void ShapeViewHandler_AsyncImageSourceKey_ChangesWhenFileSourceMutates()
    {
        var mutableBrush = CreateImageBrushFromFile("assets/shapes/file-mut-1.png");
        var fileSource = Assert.IsType<FileImageSource>(GetImageBrushSource(mutableBrush));
        var key1 = CreateAsyncImageSourceKeyForTest(fileSource);
        fileSource.File = "assets/shapes/file-mut-2.png";
        var key2 = CreateAsyncImageSourceKeyForTest(fileSource);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public async Task ShapeViewHandler_SupersededStreamImageBrushResolution_ReleasesStaleManagedUrl()
    {
        var resolver = new StubImageSourceResolver();
        resolver.SetPendingStreamResolution();
        var handler = CreateHandler(services => services.AddSingleton<IMirageImageSourceResolver>(resolver));

        var staleBrush = CreateImageBrushFromStream();
        var rectangle = new Rectangle
        {
            Fill = staleBrush
        };

        ((IElementHandler)handler).SetVirtualView(rectangle);
        handler.PlatformArrange(new Rect(0, 0, 160, 90));
        await WaitForAsync(() => resolver.StreamResolveAsyncCallCount >= 1, timeout: TimeSpan.FromSeconds(2));

        rectangle.Fill = Brush.Green;
        MirageShapeViewHandler.Mapper.UpdateProperty(handler, rectangle, nameof(IShapeView.Fill));

        var staleUrl = "/_mirage/assets/test-shape-stream-stale.png";
        resolver.CompletePendingStreamResolution(staleUrl);

        await WaitForAsync(() => resolver.WasReleased(staleUrl), timeout: TimeSpan.FromSeconds(2));

        var defsNode = GetDefsNode(GetSvgNode(handler));
        var pathNode = GetPathNode(GetSvgNode(handler));
        Assert.DoesNotContain(staleUrl, defsNode.GetPropertyString("innerHTML"), StringComparison.Ordinal);
        var fillAttribute = pathNode.Attributes.TryGetValue("fill", out var fillValue)
            ? fillValue ?? string.Empty
            : string.Empty;
        Assert.False(
            fillAttribute.StartsWith("url(#", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ShapeViewHandler_InactiveUnresolvedStreamSource_IsEvictedFromAsyncCache()
    {
        var resolver = new StubImageSourceResolver();
        resolver.EnqueueStreamUnresolvedResult("first unresolved");
        resolver.EnqueueStreamUnresolvedResult("second unresolved");
        var handler = CreateHandler(services => services.AddSingleton<IMirageImageSourceResolver>(resolver));

        var streamBrush = CreateImageBrushFromStream();
        var rectangle = new Rectangle
        {
            Fill = streamBrush
        };

        ((IElementHandler)handler).SetVirtualView(rectangle);
        handler.PlatformArrange(new Rect(0, 0, 160, 90));

        await WaitForAsync(() => resolver.StreamResolveAsyncCallCount >= 1, timeout: TimeSpan.FromSeconds(2));
        Assert.Equal(1, resolver.StreamResolveAsyncCallCount);
        await Task.Delay(50);

        rectangle.Fill = Brush.Blue;
        MirageShapeViewHandler.Mapper.UpdateProperty(handler, rectangle, nameof(IShapeView.Fill));

        rectangle.Fill = streamBrush;
        MirageShapeViewHandler.Mapper.UpdateProperty(handler, rectangle, nameof(IShapeView.Fill));

        await WaitForAsync(() => resolver.StreamResolveAsyncCallCount >= 2, timeout: TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task ShapeViewHandler_StreamImageBrushTransientFailure_RetryIsThrottledForRapidUpdates()
    {
        var resolver = new StubImageSourceResolver();
        resolver.EnqueueStreamUnresolvedResult("transient unresolved");
        var handler = CreateHandler(services => services.AddSingleton<IMirageImageSourceResolver>(resolver));

        var rectangle = new Rectangle
        {
            Fill = CreateImageBrushFromStream()
        };

        ((IElementHandler)handler).SetVirtualView(rectangle);
        handler.PlatformArrange(new Rect(0, 0, 160, 90));

        await WaitForAsync(() => resolver.StreamResolveAsyncCallCount >= 1, timeout: TimeSpan.FromSeconds(2));
        Assert.Equal(1, resolver.StreamResolveAsyncCallCount);
        await Task.Delay(50);

        for (var attempt = 0; attempt < 8; attempt++)
        {
            MirageShapeViewHandler.Mapper.UpdateProperty(handler, rectangle, nameof(IShapeView.Fill));
            await Task.Delay(10);
        }

        Assert.Equal(1, resolver.StreamResolveAsyncCallCount);

        resolver.EnqueueStreamUnresolvedResult("still unresolved");
        await Task.Delay(350);
        MirageShapeViewHandler.Mapper.UpdateProperty(handler, rectangle, nameof(IShapeView.Fill));
        await WaitForAsync(() => resolver.StreamResolveAsyncCallCount >= 2, timeout: TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task ShapeViewHandler_StreamImageBrushTransientFailure_RetriesResolutionOnSubsequentMapUpdate()
    {
        var resolver = new StubImageSourceResolver();
        resolver.EnqueueStreamUnresolvedResult("transient unresolved");
        var handler = CreateHandler(services => services.AddSingleton<IMirageImageSourceResolver>(resolver));

        var rectangle = new Rectangle
        {
            Fill = CreateImageBrushFromStream()
        };

        ((IElementHandler)handler).SetVirtualView(rectangle);
        handler.PlatformArrange(new Rect(0, 0, 160, 90));

        await WaitForAsync(() => resolver.StreamResolveAsyncCallCount >= 1, timeout: TimeSpan.FromSeconds(2));
        Assert.Equal(1, resolver.StreamResolveAsyncCallCount);
        await Task.Delay(50);

        var defsNode = GetDefsNode(GetSvgNode(handler));
        var pathNode = GetPathNode(GetSvgNode(handler));
        Assert.DoesNotContain("pattern", defsNode.GetPropertyString("innerHTML"));
        Assert.Equal("none", pathNode.Attributes["fill"]);

        Assert.Equal("StreamImageSource", GetImageBrushSourceTypeName(rectangle.Fill));
        resolver.EnqueueStreamResolvedResult("/_mirage/assets/test-shape-stream-recovered.png");
        await Task.Delay(350);
        MirageShapeViewHandler.Mapper.UpdateProperty(handler, rectangle, nameof(IShapeView.Fill));
        await WaitForAsync(() => resolver.StreamResolveAsyncCallCount >= 2, timeout: TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void ShapeViewHandler_UnsupportedGraphicsPaints_ProduceDeterministicFallback()
    {
        var handler = CreateHandler();
        var shapeView = new ShapeViewPaintProbe
        {
            Fill = new PatternPaint(),
            Stroke = new ImagePaint(),
            StrokeThickness = 3
        };

        ((IElementHandler)handler).SetVirtualView(shapeView);
        handler.PlatformArrange(new Rect(0, 0, 80, 60));

        var pathNode = GetPathNode(GetSvgNode(handler));
        Assert.Equal("none", pathNode.Attributes["fill"]);
        Assert.Equal("none", pathNode.Attributes["stroke"]);
    }

    [Fact]
    public void ShapeViewHandler_UsesRequestedSizeFallback_WhenArrangeHasNotRunYet()
    {
        var handler = CreatePolygonHandler();
        var polygon = new Polygon
        {
            WidthRequest = 180,
            HeightRequest = 90,
            Fill = Brush.Orange,
            Points =
            [
                new Point(0, 0),
                new Point(178, 12),
                new Point(90, 84)
            ]
        };

        ((IElementHandler)handler).SetVirtualView(polygon);

        var pathNode = GetPathNode(GetSvgNode(handler));
        var svgNode = GetSvgNode(handler);
        Assert.False(string.IsNullOrWhiteSpace(pathNode.Attributes["d"]));
        Assert.Equal("0 0 180 90", svgNode.Attributes["viewBox"]);
    }

    private static MirageShapeViewHandler CreateHandler(Action<ServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        configureServices?.Invoke(services);
        var handler = new MirageShapeViewHandler();
        ((IElementHandler)handler).SetMauiContext(new MauiContext(services.BuildServiceProvider()));
        return handler;
    }

    private static MirageLineHandler CreateLineHandler()
    {
        var handler = new MirageLineHandler();
        ((IElementHandler)handler).SetMauiContext(new MauiContext(new ServiceCollection().BuildServiceProvider()));
        return handler;
    }

    private static MiragePathHandler CreatePathHandler()
    {
        var handler = new MiragePathHandler();
        ((IElementHandler)handler).SetMauiContext(new MauiContext(new ServiceCollection().BuildServiceProvider()));
        return handler;
    }

    private static MiragePolygonHandler CreatePolygonHandler()
    {
        var handler = new MiragePolygonHandler();
        ((IElementHandler)handler).SetMauiContext(new MauiContext(new ServiceCollection().BuildServiceProvider()));
        return handler;
    }

    private static MiragePolylineHandler CreatePolylineHandler()
    {
        var handler = new MiragePolylineHandler();
        ((IElementHandler)handler).SetMauiContext(new MauiContext(new ServiceCollection().BuildServiceProvider()));
        return handler;
    }

    private static MiragePlatformView GetSvgNode(MirageShapeViewHandler handler)
    {
        return handler.PlatformView.Children
            .First(child => child.NodeType == PlatformNodeType.Web && string.Equals(child.ElementName, "svg", StringComparison.Ordinal));
    }

    private static MiragePlatformView GetDefsNode(MiragePlatformView svgNode)
    {
        return svgNode.Children
            .First(child => child.NodeType == PlatformNodeType.Web && string.Equals(child.ElementName, "defs", StringComparison.Ordinal));
    }

    private static MiragePlatformView GetPathNode(MiragePlatformView svgNode)
    {
        return svgNode.Children
            .First(child => child.NodeType == PlatformNodeType.Web && string.Equals(child.ElementName, "path", StringComparison.Ordinal));
    }

    private static Brush CreateImageBrush(string sourceUri)
    {
        var imageBrushType = typeof(MauiShape).Assembly.GetType("Microsoft.Maui.Controls.ImageBrush", throwOnError: true)!;
        var brush = (Brush)Activator.CreateInstance(imageBrushType)!;
        var imageSourceProperty = imageBrushType.GetProperty("ImageSource")!;
        imageSourceProperty.SetValue(brush, ImageSource.FromUri(new Uri(sourceUri, UriKind.Absolute)));
        return brush;
    }

    private static Brush CreateImageBrushFromStream()
    {
        var imageBrushType = typeof(MauiShape).Assembly.GetType("Microsoft.Maui.Controls.ImageBrush", throwOnError: true)!;
        var brush = (Brush)Activator.CreateInstance(imageBrushType)!;
        var imageSourceProperty = imageBrushType.GetProperty("ImageSource")!;
        imageSourceProperty.SetValue(
            brush,
            new StreamImageSource
            {
                Stream = _ => Task.FromResult<Stream>(new MemoryStream([0x1, 0x2, 0x3]))
            });
        return brush;
    }

    private static Brush CreateImageBrushFromFile(string file)
    {
        var imageBrushType = typeof(MauiShape).Assembly.GetType("Microsoft.Maui.Controls.ImageBrush", throwOnError: true)!;
        var brush = (Brush)Activator.CreateInstance(imageBrushType)!;
        var imageSourceProperty = imageBrushType.GetProperty("ImageSource")!;
        imageSourceProperty.SetValue(brush, ImageSource.FromFile(file));
        return brush;
    }

    private static string? GetImageBrushSourceTypeName(Brush? brush)
    {
        if (brush is null)
        {
            return null;
        }

        var source = GetImageBrushSource(brush);
        return source?.GetType().Name;
    }

    private static string CreateAsyncImageSourceKeyForTest(ImageSource imageSource)
    {
        var keyFactory = typeof(MirageShapeViewHandler).GetMethod(
            "CreateAsyncImageSourceKey",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(keyFactory);
        var key = keyFactory.Invoke(null, [imageSource]) as string;
        Assert.False(string.IsNullOrWhiteSpace(key));
        return key!;
    }

    private static ImageSource? GetImageBrushSource(Brush brush)
    {
        var imageSourceProperty = brush.GetType().GetProperty("ImageSource", BindingFlags.Public | BindingFlags.Instance);
        return imageSourceProperty?.GetValue(brush) as ImageSource;
    }

    private static async Task WaitForAsync(Func<bool> condition, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (!condition())
        {
            if (DateTime.UtcNow - start > timeout)
            {
                throw new Xunit.Sdk.XunitException("Timed out waiting for expected async shape projection update.");
            }

            await Task.Delay(10);
        }
    }

    private sealed class StubImageSourceResolver : IMirageImageSourceResolver
    {
        private readonly Dictionary<string, MirageImageSourceProjectionResult> _uriResults = new(StringComparer.Ordinal);
        private readonly Queue<MirageImageSourceProjectionResult> _queuedStreamResults = new();
        private readonly object _sync = new();
        private readonly HashSet<string> _releasedUrls = new(StringComparer.Ordinal);
        private MirageImageSourceProjectionResult? _streamResult;
        private MirageImageSourceProjectionResult? _fileResult;
        private TaskCompletionSource<MirageImageSourceProjectionResult>? _pendingStreamResult;
        private TaskCompletionSource<bool>? _pendingEnsureInitialized;
        private int _streamResolveAsyncCallCount;
        private int _fileResolveAsyncCallCount;
        private int _ensureInitializedAsyncCallCount;

        public int StreamResolveAsyncCallCount => Volatile.Read(ref _streamResolveAsyncCallCount);
        public int FileResolveAsyncCallCount => Volatile.Read(ref _fileResolveAsyncCallCount);
        public int EnsureInitializedAsyncCallCount => Volatile.Read(ref _ensureInitializedAsyncCallCount);

        public bool WasReleased(string browserUrl)
        {
            lock (_sync)
            {
                return _releasedUrls.Contains(browserUrl);
            }
        }

        public void SetResolved(string sourceUri, string browserUrl)
        {
            _uriResults[sourceUri] = MirageImageSourceProjectionResult.Resolved(browserUrl, "uri");
        }

        public void SetResolvedStream(string browserUrl)
        {
            _streamResult = CreateResolvedStreamProjection(browserUrl);
        }

        public void SetResolvedFile(string browserUrl)
        {
            _fileResult = MirageImageSourceProjectionResult.Resolved(browserUrl, "file");
        }

        public void EnqueueStreamResolvedResult(string browserUrl)
        {
            lock (_sync)
            {
                _queuedStreamResults.Enqueue(CreateResolvedStreamProjection(browserUrl));
            }
        }

        public void EnqueueStreamUnresolvedResult(string failureReason)
        {
            lock (_sync)
            {
                _queuedStreamResults.Enqueue(MirageImageSourceProjectionResult.Unresolved("stream", failureReason));
            }
        }

        public void SetPendingStreamResolution()
        {
            _pendingStreamResult = new TaskCompletionSource<MirageImageSourceProjectionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public void CompletePendingStreamResolution(string browserUrl)
        {
            _streamResult = CreateResolvedStreamProjection(browserUrl);
            _pendingStreamResult?.TrySetResult(_streamResult.Value);
            _pendingStreamResult = null;
        }

        public void SetPendingManifestInitialization()
        {
            _pendingEnsureInitialized = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public void CompletePendingManifestInitialization()
        {
            _pendingEnsureInitialized?.TrySetResult(true);
            _pendingEnsureInitialized = null;
        }

        public ValueTask EnsureInitializedAsync()
        {
            Interlocked.Increment(ref _ensureInitializedAsyncCallCount);
            if (_pendingEnsureInitialized is { Task: { } pendingTask })
            {
                return new ValueTask(pendingTask);
            }

            return ValueTask.CompletedTask;
        }

        public MirageImageSourceResolveResult Resolve(string? source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return MirageImageSourceResolveResult.Unresolved("empty source");
            }

            return _uriResults.TryGetValue(source, out var result)
                ? MirageImageSourceResolveResult.Resolved(result.BrowserUrl!)
                : MirageImageSourceResolveResult.Unresolved("unmapped source");
        }

        public MirageImageSourceProjectionResult Resolve(ImageSource? source)
        {
            if (source is UriImageSource uriSource && uriSource.Uri is not null)
            {
                return _uriResults.TryGetValue(uriSource.Uri.ToString(), out var result)
                    ? result
                    : MirageImageSourceProjectionResult.Unresolved("uri", "unmapped source");
            }

            if (source is StreamImageSource)
            {
                return MirageImageSourceProjectionResult.Unresolved("stream", "StreamImageSource requires async resolver execution.");
            }

            if (source is FileImageSource)
            {
                return MirageImageSourceProjectionResult.Unresolved("file", "FileImageSource requires async resolver execution.");
            }

            return MirageImageSourceProjectionResult.Unresolved("unsupported", "unsupported image source kind");
        }

        public ValueTask<MirageImageSourceProjectionResult> ResolveAsync(ImageSource? source, CancellationToken cancellationToken = default)
        {
            if (source is StreamImageSource)
            {
                Interlocked.Increment(ref _streamResolveAsyncCallCount);

                if (_pendingStreamResult is { Task: { } pendingTask })
                {
                    return WaitForPendingStreamResolutionAsync(pendingTask, cancellationToken);
                }

                lock (_sync)
                {
                    if (_queuedStreamResults.Count > 0)
                    {
                        return ValueTask.FromResult(_queuedStreamResults.Dequeue());
                    }
                }

                return ValueTask.FromResult(
                    _streamResult
                    ?? MirageImageSourceProjectionResult.Unresolved("stream", "unmapped stream source"));
            }

            if (source is FileImageSource)
            {
                Interlocked.Increment(ref _fileResolveAsyncCallCount);
                return ValueTask.FromResult(
                    _fileResult
                    ?? MirageImageSourceProjectionResult.Unresolved("file", "unmapped file source"));
            }

            return ValueTask.FromResult(Resolve(source));
        }

        public void Release(string? browserUrl)
        {
            if (string.IsNullOrWhiteSpace(browserUrl))
            {
                return;
            }

            lock (_sync)
            {
                _releasedUrls.Add(browserUrl);
            }
        }

        private static async ValueTask<MirageImageSourceProjectionResult> WaitForPendingStreamResolutionAsync(
            Task<MirageImageSourceProjectionResult> pendingTask,
            CancellationToken cancellationToken)
        {
            return await pendingTask.WaitAsync(cancellationToken);
        }

        private static MirageImageSourceProjectionResult CreateResolvedStreamProjection(string browserUrl)
        {
            return MirageImageSourceProjectionResult.Resolved(
                browserUrl,
                "stream",
                isManagedObjectUrl: true);
        }
    }

    private sealed class ShapeViewPaintProbe : View, IShapeView
    {
        public MauiGraphicsShape? Shape { get; set; } = new ProbeGraphicsShape();

        public PathAspect Aspect { get; set; } = PathAspect.Stretch;

        public Paint? Fill { get; set; } = new SolidPaint(Colors.Transparent);

        public Paint? Stroke { get; set; } = new SolidPaint(Colors.Black);

        public double StrokeThickness { get; set; } = 1;

        public LineCap StrokeLineCap { get; set; } = LineCap.Butt;

        public LineJoin StrokeLineJoin { get; set; } = LineJoin.Miter;

        public float[]? StrokeDashPattern { get; set; }

        public float StrokeDashOffset { get; set; }

        public float StrokeMiterLimit { get; set; } = 10;
    }

    private sealed class ProbeGraphicsShape : MauiGraphicsShape
    {
        public PathF PathForBounds(Rect bounds)
        {
            var path = new PathF();
            path.AppendRectangle((float)bounds.X, (float)bounds.Y, (float)bounds.Width, (float)bounds.Height);
            return path;
        }
    }
}
