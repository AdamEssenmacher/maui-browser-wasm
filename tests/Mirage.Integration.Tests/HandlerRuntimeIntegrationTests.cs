using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Mirage.Maui.Hosting;
using Mirage.Web.Runtime;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class HandlerRuntimeIntegrationTests
{
    [Fact]
    public void CommonMapper_MapsAutomationMarginVisibilityAndPaddingThroughHandler()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "flex");
        var view = new Label
        {
            AutomationId = "mapped-id",
            Margin = new Thickness(1, 2, 3, 4),
            IsVisible = false,
            Padding = new Thickness(5, 6, 7, 8)
        };

        MirageViewMapper.MapAutomationId(handler, view);
        MirageViewMapper.MapMargin(handler, view);
        MirageViewMapper.MapVisibility(handler, view);
        MirageViewMapper.MapPadding(handler, view);

        Assert.Equal("mapped-id", handler.Node.GetPropertyString("AutomationId"));
        Assert.Equal("2px 3px 4px 1px", handler.Node.Styles["margin"]);
        Assert.Equal("none", handler.Node.Styles["display"]);
        Assert.Equal("6px 7px 8px 5px", handler.Node.Styles["padding"]);
        Assert.Equal(4, handler.RenderRequestCount);
    }

    [Fact]
    public void CommonMapper_ReapplyingSameValues_IsIdempotent()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var view = new Label
        {
            AutomationId = "stable-id",
            Margin = new Thickness(4, 3, 2, 1),
            IsVisible = true,
            Padding = new Thickness(2)
        };

        MirageViewMapper.MapAutomationId(handler, view);
        MirageViewMapper.MapMargin(handler, view);
        MirageViewMapper.MapVisibility(handler, view);
        MirageViewMapper.MapPadding(handler, view);
        var firstRenderCount = handler.RenderRequestCount;

        MirageViewMapper.MapAutomationId(handler, view);
        MirageViewMapper.MapMargin(handler, view);
        MirageViewMapper.MapVisibility(handler, view);
        MirageViewMapper.MapPadding(handler, view);

        Assert.Equal(firstRenderCount, handler.RenderRequestCount);
    }

    [Fact]
    public void CommonMapper_ThicknessFormatting_IsDeterministic()
    {
        Assert.Equal("0px", MirageViewMapper.ToPx(-15));
        Assert.Equal("0px", MirageViewMapper.ToPx(double.NaN));
        Assert.Equal("12.345px", MirageViewMapper.ToPx(12.3454));
        Assert.Equal("2px 8.5px 0px 1.25px", MirageCommonViewProjection.ToThickness(new Thickness(1.25, 2, 8.5, -3)));
    }

    [Fact]
    public void CommonMapper_MapsOpacityBackgroundAndSizeRequests()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var view = new Label
        {
            Opacity = 0.42,
            WidthRequest = 120,
            HeightRequest = 48,
            MinimumWidthRequest = 40,
            MinimumHeightRequest = 24,
            MaximumWidthRequest = 320,
            MaximumHeightRequest = 220,
            Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Colors.CornflowerBlue, 0),
                    new GradientStop(Colors.MediumPurple, 1)
                },
                new Point(0, 0),
                new Point(1, 1))
        };

        MirageViewMapper.MapOpacity(handler, view);
        MirageViewMapper.MapBackground(handler, view);
        MirageViewMapper.MapSizeRequests(handler, view);

        Assert.Equal("0.42", handler.Node.GetStyleValue("opacity"));
        Assert.Equal("120px", handler.Node.GetStyleValue("width"));
        Assert.Equal("48px", handler.Node.GetStyleValue("height"));
        Assert.Equal("40px", handler.Node.GetStyleValue("minWidth"));
        Assert.Equal("24px", handler.Node.GetStyleValue("minHeight"));
        Assert.Equal("320px", handler.Node.GetStyleValue("maxWidth"));
        Assert.Equal("220px", handler.Node.GetStyleValue("maxHeight"));
        Assert.NotNull(handler.Node.GetStyleValue("backgroundImage"));
    }

    [Fact]
    public void CommonMapper_MapsTextStylePrimitives_AndIsIdempotent()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var label = new Label
        {
            TextColor = Colors.Red,
            CharacterSpacing = 1.5
        };

        label.FontFamily = "Space Grotesk";
        label.FontSize = 22;
        label.FontAttributes = FontAttributes.Bold;

        MirageViewMapper.MapTextColor(handler, label);
        MirageViewMapper.MapFont(handler, label);
        MirageViewMapper.MapCharacterSpacing(handler, label);

        Assert.Equal("rgba(255,0,0,1)", handler.Node.GetStyleValue("color"));
        Assert.Equal("22px", handler.Node.GetStyleValue("fontSize"));
        Assert.Equal("700", handler.Node.GetStyleValue("fontWeight"));
        Assert.Equal("normal", handler.Node.GetStyleValue("fontStyle"));
        Assert.Equal("Space Grotesk", handler.Node.GetStyleValue("fontFamily"));
        Assert.Equal("1.5px", handler.Node.GetStyleValue("letterSpacing"));

        var firstRenderCount = handler.RenderRequestCount;
        MirageViewMapper.MapTextColor(handler, label);
        MirageViewMapper.MapFont(handler, label);
        MirageViewMapper.MapCharacterSpacing(handler, label);
        Assert.Equal(firstRenderCount, handler.RenderRequestCount);

        label.FontSize = -1;
        MirageViewMapper.MapFont(handler, label);
        Assert.Equal("14px", handler.Node.GetStyleValue("fontSize"));
    }

    [Fact]
    public void CommonMapper_SizeRequests_DoNotClearExistingWidthUntilRequestWasApplied()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var view = new Label();

        handler.Node.SetStyle("width", "100%");

        MirageViewMapper.MapSizeRequests(handler, view);
        Assert.Equal("100%", handler.Node.GetStyleValue("width"));

        view.WidthRequest = 132;
        MirageViewMapper.MapSizeRequests(handler, view);
        Assert.Equal("132px", handler.Node.GetStyleValue("width"));

        view.WidthRequest = -1;
        MirageViewMapper.MapSizeRequests(handler, view);
        Assert.Null(handler.Node.GetStyleValue("width"));
    }

    [Fact]
    public void CommonMapper_MapsLayoutClipsToBoundsToOverflowStyle()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "flex");
        var layout = new VerticalStackLayout();

        var changedVisible = MirageCommonViewProjection.MapLayoutClipsToBounds(handler, layout);
        Assert.True(changedVisible);
        Assert.Equal("visible", handler.Node.GetStyleValue("overflow"));

        var unchanged = MirageCommonViewProjection.MapLayoutClipsToBounds(handler, layout);
        Assert.False(unchanged);

        layout.Clip = new RectangleGeometry(new Rect(0, 0, 30, 20));
        var changedHidden = MirageCommonViewProjection.MapLayoutClipsToBounds(handler, layout);
        Assert.True(changedHidden);
        Assert.Equal("hidden", handler.Node.GetStyleValue("overflow"));
    }

    [Fact]
    public void CommonMapper_MapsClipProjection_ForBuiltInGeometryTypes()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var view = new ContentView();

        view.Clip = new RectangleGeometry(new Rect(0, 0, 120, 72));
        MirageViewMapper.MapClip(handler, view);
        Assert.Contains("path('M", handler.Node.GetStyleValue("clipPath"), StringComparison.Ordinal);

        view.Clip = new RoundRectangleGeometry(new CornerRadius(12, 8, 14, 6), new Rect(4, 2, 110, 66));
        MirageViewMapper.MapClip(handler, view);
        Assert.Contains(" A ", handler.Node.GetStyleValue("clipPath"), StringComparison.Ordinal);

        view.Clip = new EllipseGeometry(new Point(60, 36), 40, 24);
        MirageViewMapper.MapClip(handler, view);
        Assert.Contains(" A ", handler.Node.GetStyleValue("clipPath"), StringComparison.Ordinal);

        view.Clip = new LineGeometry
        {
            StartPoint = new Point(2, 3),
            EndPoint = new Point(98, 42)
        };
        MirageViewMapper.MapClip(handler, view);
        Assert.Contains(" L ", handler.Node.GetStyleValue("clipPath"), StringComparison.Ordinal);

        var path = new PathGeometry
        {
            FillRule = FillRule.EvenOdd
        };
        var figure = new PathFigure
        {
            StartPoint = new Point(4, 4),
            IsClosed = true
        };
        figure.Segments.Add(new LineSegment { Point = new Point(84, 4) });
        figure.Segments.Add(new LineSegment { Point = new Point(84, 44) });
        figure.Segments.Add(new LineSegment { Point = new Point(4, 44) });
        path.Figures.Add(figure);
        view.Clip = path;
        MirageViewMapper.MapClip(handler, view);
        Assert.StartsWith("path(evenodd, '", handler.Node.GetStyleValue("clipPath"), StringComparison.Ordinal);

        var group = new GeometryGroup
        {
            FillRule = FillRule.EvenOdd
        };
        group.Children.Add(new RectangleGeometry(new Rect(2, 2, 44, 28)));
        group.Children.Add(new EllipseGeometry(new Point(56, 22), 18, 12));
        view.Clip = group;
        MirageViewMapper.MapClip(handler, view);
        Assert.StartsWith("path(evenodd, '", handler.Node.GetStyleValue("clipPath"), StringComparison.Ordinal);
    }

    [Fact]
    public void CommonMapper_MapClip_InvalidOrUnsupportedGeometry_ThrowsDeterministically()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var view = new ContentView();

        view.Clip = new RectangleGeometry(new Rect(0, 0, double.NaN, 20));
        var invalidRect = Assert.Throws<InvalidOperationException>(() => MirageViewMapper.MapClip(handler, view));
        Assert.Contains("Rect.Width", invalidRect.Message, StringComparison.Ordinal);

        var invalidPath = new PathGeometry();
        var invalidFigure = new PathFigure { StartPoint = new Point(0, 0), IsClosed = false };
        var invalidPolyBezier = new PolyBezierSegment();
        invalidPolyBezier.Points.Add(new Point(4, 4));
        invalidPolyBezier.Points.Add(new Point(8, 8));
        invalidFigure.Segments.Add(invalidPolyBezier);
        invalidPath.Figures.Add(invalidFigure);
        view.Clip = invalidPath;
        var invalidPathException = Assert.Throws<InvalidOperationException>(() => MirageViewMapper.MapClip(handler, view));
        Assert.Contains("divisible by 3", invalidPathException.Message, StringComparison.Ordinal);

        view.Clip = new UnsupportedGeometry();
        var unsupported = Assert.Throws<InvalidOperationException>(() => MirageViewMapper.MapClip(handler, view));
        Assert.Contains("does not support clip geometry type", unsupported.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CommonMapper_AlignmentProjection_UsesParentLayoutKind()
    {
        var handler = new MapperProbeHandler(defaultDisplayStyle: "block");
        var child = new Label
        {
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center
        };

        var gridParent = MiragePlatformView.CreateControl(ComponentKind.Grid);
        gridParent.AddChild(handler.Node);

        MirageViewMapper.MapLayoutAlignment(handler, child);
        Assert.Equal("end", handler.Node.GetStyleValue("justifySelf"));
        Assert.Equal("center", handler.Node.GetStyleValue("alignSelf"));

        var verticalParent = MiragePlatformView.CreateControl(ComponentKind.VerticalStack);
        verticalParent.AddChild(handler.Node);
        MirageViewMapper.MapLayoutAlignment(handler, child);
        Assert.Equal("flex-end", handler.Node.GetStyleValue("alignSelf"));

        var contentContainerParent = MiragePlatformView.CreateControl(ComponentKind.ContentContainer);
        contentContainerParent.AddChild(handler.Node);
        MirageViewMapper.MapLayoutAlignment(handler, child);
        Assert.Equal("end", handler.Node.GetStyleValue("justifySelf"));
        Assert.Equal("center", handler.Node.GetStyleValue("alignSelf"));
    }

    [Fact]
    public void RenderCoordinator_RendersTypedSemanticSnapshotAndRoutesEvents()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new RuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var root = MiragePlatformView.CreateControl(ComponentKind.Root);
        root.SetStyle("display", "flex");

        var button = MiragePlatformView.CreateControl(ComponentKind.Button);
        button.SetProperty("Text", "Click");
        button.SetEvent("click", enabled: true);
        root.AddChild(button);

        var target = new EventTargetProbe();
        coordinator.Register(root, eventTarget: null);
        coordinator.Register(button, target);
        coordinator.SetRoot(root);

        Assert.NotNull(runtime.LastRendered);
        var rootNode = Assert.IsType<ControlNode>(runtime.LastRendered);
        Assert.Equal(ComponentKind.Root, rootNode.Kind);
        var child = Assert.Single(rootNode.Children);
        var controlChild = Assert.IsType<ControlNode>(child);
        Assert.Equal(ComponentKind.Button, controlChild.Kind);
        Assert.Equal("Click", controlChild.Properties["Text"].GetStringOrNull());

        coordinator.OnEvent(button.NodeId, "click", null);
        Assert.Equal("click", target.LastEventName);
    }

    [Fact]
    public void RenderCoordinator_RendersGenericWebNode_AndRoutesEventsByNodeId()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var runtime = new RuntimeProbe();
        coordinator.AttachRuntime(runtime);

        var root = MiragePlatformView.CreateControl(ComponentKind.Root);
        var vendor = MiragePlatformView.CreateWeb("vendor-widget");
        vendor.SetProperty("open", MirageValue.Bool(true));
        vendor.SetProperty("payload", MirageValue.Json(System.Text.Json.Nodes.JsonNode.Parse("""{"mode":"demo"}""")));
        vendor.SetAttribute("aria-label", "Vendor Widget");
        vendor.AddClass("is-ready");
        vendor.SetEvent("widget-click", enabled: true);
        root.AddChild(vendor);

        var target = new EventTargetProbe();
        coordinator.Register(root, eventTarget: null);
        coordinator.Register(vendor, eventTarget: target);
        coordinator.SetRoot(root);

        var rootNode = Assert.IsType<ControlNode>(runtime.LastRendered);
        var child = Assert.Single(rootNode.Children);
        var webNode = Assert.IsType<WebNode>(child);
        Assert.Equal("vendor-widget", webNode.ElementName);
        Assert.True(webNode.Properties["open"].TryGetBool(out var isOpen) && isOpen);
        Assert.Contains("is-ready", webNode.Classes);
        Assert.Equal("Vendor Widget", webNode.Attributes["aria-label"]);

        coordinator.OnEvent(vendor.NodeId, "widget-click", payload: null);
        Assert.Equal("widget-click", target.LastEventName);
    }

    [Fact]
    public void RenderCoordinator_AssignsStableNodeIds()
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        coordinator.AttachRuntime(new RuntimeProbe());

        var first = MiragePlatformView.CreateControl(ComponentKind.Label);
        var second = MiragePlatformView.CreateWeb("vendor-input");

        coordinator.Register(first, null);
        coordinator.Register(second, null);

        Assert.Equal("node-0001", first.NodeId);
        Assert.Equal("node-0002", second.NodeId);
    }

    [Fact]
    public void PlatformView_ChildOrderRemainsDeterministicAcrossMutations()
    {
        var root = MiragePlatformView.CreateControl(ComponentKind.VerticalStack);
        root.AssignNodeId("root");
        var first = MiragePlatformView.CreateControl(ComponentKind.Label);
        first.AssignNodeId("first");
        var second = MiragePlatformView.CreateControl(ComponentKind.Label);
        second.AssignNodeId("second");
        var third = MiragePlatformView.CreateControl(ComponentKind.Label);
        third.AssignNodeId("third");

        root.AddChild(first);
        root.AddChild(second);
        root.AddChild(third);

        root.RemoveChild(second);
        root.InsertChild(0, second);

        Assert.Equal(["second", "first", "third"], root.Children.Select(static child => child.NodeId).ToArray());
    }

    private sealed class MapperProbeHandler : ViewHandler<IView, object>, IMiragePlatformHandler
    {
        public MapperProbeHandler(string? defaultDisplayStyle)
            : base(new PropertyMapper<IView, MapperProbeHandler>())
        {
            DefaultDisplayStyle = defaultDisplayStyle;
        }

        public MiragePlatformView Node { get; } = MiragePlatformView.CreateControl(ComponentKind.Label);

        public int RenderRequestCount { get; private set; }

        public string? DefaultDisplayStyle { get; }

        MiragePlatformView IMiragePlatformHandler.MiragePlatformView => Node;

        public void RequestRender()
        {
            RenderRequestCount++;
        }

        protected override object CreatePlatformView() => new();
    }

    private sealed class RuntimeProbe : IMirageRuntime
    {
        public NodeBase? LastRendered { get; private set; }

        public IMirageEventSink? Sink { get; private set; }

        public void SetEventSink(IMirageEventSink? eventSink)
        {
            Sink = eventSink;
        }

        public void Render(NodeBase nextRoot)
        {
            LastRendered = nextRoot;
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

        public void Dispose()
        {
        }
    }

    private sealed class EventTargetProbe : IMirageRuntimeEventTarget
    {
        public string? LastEventName { get; private set; }

        public void OnRuntimeEvent(string eventName, string? payload)
        {
            _ = payload;
            LastEventName = eventName;
        }
    }

    private sealed class UnsupportedGeometry : Geometry
    {
        public override void AppendPath(PathF path)
        {
            _ = path;
        }
    }
}
