using System.Collections.Specialized;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MiragePolylineHandler : MirageShapeViewHandler
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
        nameof(Polyline.Points),
        nameof(Polyline.FillRule)
    };

    public static new IPropertyMapper<Polyline, MiragePolylineHandler> Mapper =
        new PropertyMapper<Polyline, MiragePolylineHandler>(MirageShapeViewHandler.Mapper)
        {
            [nameof(IShapeView.Shape)] = MapShape,
            [nameof(Polyline.Points)] = MapPoints,
            [nameof(Polyline.FillRule)] = MapFillRule
        };

    public MiragePolylineHandler()
        : base(Mapper)
    {
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        if (VirtualView is Polyline polyline)
        {
            SubscribePointsCollection(polyline.Points);
        }

        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        SubscribePointsCollection(null);

        base.DisconnectHandler(platformView);
    }

    public static void MapShape(MiragePolylineHandler handler, Polyline polyline)
    {
        handler.MapShapeFromDerived(polyline);
    }

    public static void MapPoints(MiragePolylineHandler handler, Polyline polyline)
    {
        handler.SubscribePointsCollection(polyline.Points);
        handler.MapShapeFromDerived(polyline);
    }

    public static void MapFillRule(MiragePolylineHandler handler, Polyline polyline)
    {
        handler.MapShapeFromDerived(polyline);
    }

    private void OnPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (VirtualView is Polyline polyline)
        {
            MapShapeFromDerived(polyline);
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
