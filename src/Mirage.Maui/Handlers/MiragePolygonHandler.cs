using System.Collections.Specialized;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MiragePolygonHandler : MirageShapeViewHandler
{
    private PointCollection? _subscribedPoints;

    internal new static readonly string[] ParityPropertyKeys =
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
        nameof(IShapeView.StrokeMiterLimit),
        nameof(Polygon.Points),
        nameof(Polygon.FillRule)
    };

    public static new IPropertyMapper<Polygon, MiragePolygonHandler> Mapper =
        new PropertyMapper<Polygon, MiragePolygonHandler>(MirageShapeViewHandler.Mapper)
        {
            [nameof(IShapeView.Shape)] = MapShape,
            [nameof(Polygon.Points)] = MapPoints,
            [nameof(Polygon.FillRule)] = MapFillRule
        };

    public MiragePolygonHandler()
        : base(Mapper)
    {
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        if (VirtualView is Polygon polygon)
        {
            SubscribePointsCollection(polygon.Points);
        }

        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        SubscribePointsCollection(null);

        base.DisconnectHandler(platformView);
    }

    public static void MapShape(MiragePolygonHandler handler, Polygon polygon)
    {
        handler.MapShapeFromDerived(polygon);
    }

    public static void MapPoints(MiragePolygonHandler handler, Polygon polygon)
    {
        handler.SubscribePointsCollection(polygon.Points);
        handler.MapShapeFromDerived(polygon);
    }

    public static void MapFillRule(MiragePolygonHandler handler, Polygon polygon)
    {
        handler.MapShapeFromDerived(polygon);
    }

    private void OnPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (VirtualView is Polygon polygon)
        {
            MapShapeFromDerived(polygon);
        }
    }

    private void SubscribePointsCollection(PointCollection? points)
    {
        if (ReferenceEquals(_subscribedPoints, points))
        {
            return;
        }

        if (_subscribedPoints is not null)
        {
            _subscribedPoints.CollectionChanged -= OnPointsCollectionChanged;
        }

        _subscribedPoints = points;

        if (_subscribedPoints is not null)
        {
            _subscribedPoints.CollectionChanged += OnPointsCollectionChanged;
        }
    }
}
