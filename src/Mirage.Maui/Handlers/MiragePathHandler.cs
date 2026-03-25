using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using System.Globalization;
using MauiPath = Microsoft.Maui.Controls.Shapes.Path;

namespace Mirage.Maui.Handlers;

internal sealed class MiragePathHandler : MirageViewHandler<MauiPath>
{
    private static int s_internalNodeIdCounter;
    private readonly MiragePlatformView _pathNode = MiragePlatformView.CreateWeb("path");

    public static IPropertyMapper<MauiPath, MiragePathHandler> Mapper =
        new PropertyMapper<MauiPath, MiragePathHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(MauiPath.Data)] = MapData,
            [nameof(MauiPath.Fill)] = MapFill,
            [nameof(MauiPath.Stroke)] = MapStroke,
            [nameof(MauiPath.StrokeThickness)] = MapStroke,
            [nameof(MauiPath.Aspect)] = MapAspect
        };

    public MiragePathHandler()
        : base(() => MiragePlatformView.CreateWeb("svg"), Mapper)
    {
        DefaultDisplayStyle = "block";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        RegisterInternalNode(_pathNode);
        if (!ReferenceEquals(_pathNode.Parent, platformView))
        {
            platformView.ClearChildren();
            platformView.AddChild(_pathNode);
        }

        platformView.SetStyle("display", DefaultDisplayStyle);
        platformView.SetStyle("overflow", "visible");

        _pathNode.SetAttribute("vector-effect", "non-scaling-stroke");

        base.ConnectHandler(platformView);
        RefreshProjection();
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        if (Coordinator is { } coordinator)
        {
            coordinator.Unregister(_pathNode);
        }

        base.DisconnectHandler(platformView);
    }

    public static void MapData(MiragePathHandler handler, MauiPath path)
    {
        _ = path;
        handler.RefreshProjection();
    }

    public static void MapFill(MiragePathHandler handler, MauiPath path)
    {
        _ = path;
        handler.RefreshProjection();
    }

    public static void MapStroke(MiragePathHandler handler, MauiPath path)
    {
        _ = path;
        handler.RefreshProjection();
    }

    public static void MapAspect(MiragePathHandler handler, MauiPath path)
    {
        _ = path;
        handler.RefreshProjection();
    }

    private void RefreshProjection()
    {
        if (!TryGetPlatformView(out var platformView))
        {
            return;
        }

        var geometry = VirtualView.Data;
        if (geometry is null)
        {
            _pathNode.SetAttribute("d", null);
            platformView.SetAttribute("viewBox", null);
        }
        else
        {
            var data = geometry.ToString();
            _pathNode.SetAttribute("d", string.IsNullOrWhiteSpace(data) ? null : data);
            platformView.SetAttribute("viewBox", BuildViewBox(VirtualView.GetPath()));
        }

        if (VirtualView.Fill is SolidColorBrush fillBrush)
        {
            _pathNode.SetAttribute("fill", MirageCommonViewProjection.ToCssColor(fillBrush.Color));
        }
        else
        {
            _pathNode.SetAttribute("fill", "none");
        }

        var strokeThickness = VirtualView.StrokeThickness;
        if (VirtualView.Stroke is SolidColorBrush strokeBrush && strokeThickness > 0d && double.IsFinite(strokeThickness))
        {
            _pathNode.SetAttribute("stroke", MirageCommonViewProjection.ToCssColor(strokeBrush.Color));
            _pathNode.SetAttribute(
                "stroke-width",
                strokeThickness.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
        }
        else
        {
            _pathNode.SetAttribute("stroke", null);
            _pathNode.SetAttribute("stroke-width", null);
        }

        platformView.SetAttribute("preserveAspectRatio", MapAspectToPreserveAspectRatio(VirtualView.Aspect));
        RequestRender();
    }

    private static string MapAspectToPreserveAspectRatio(Stretch aspect)
    {
        return aspect switch
        {
            // Stretch.None should keep aspect ratio and avoid fill-style distortion.
            Stretch.None => "xMinYMin meet",
            Stretch.Fill => "none",
            Stretch.UniformToFill => "xMidYMid slice",
            _ => "xMidYMid meet"
        };
    }

    private static string? BuildViewBox(PathF? path)
    {
        if (path is null)
        {
            return null;
        }

        var bounds = path.Bounds;
        if (!float.IsFinite(bounds.X)
            || !float.IsFinite(bounds.Y)
            || !float.IsFinite(bounds.Width)
            || !float.IsFinite(bounds.Height)
            || bounds.Width <= 0f
            || bounds.Height <= 0f)
        {
            return null;
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{FormatSvgNumber(bounds.X)} {FormatSvgNumber(bounds.Y)} {FormatSvgNumber(bounds.Width)} {FormatSvgNumber(bounds.Height)}");
    }

    private static string FormatSvgNumber(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
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
        node.AssignNodeId($"path-internal-{localId:D4}");
    }
}
