using Mirage.Core.Semantics;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageMapElementHandler : ElementHandler<IMapElement, MirageMapElementPlatformView>
{
    internal static readonly string[] ParityPropertyKeys =
    {
        nameof(IMapElement.Stroke),
        nameof(IMapElement.StrokeThickness),
        nameof(IFilledMapElement.Fill),
        "Geopath",
        nameof(ICircleMapElement.Radius),
        nameof(ICircleMapElement.Center)
    };

    public static IPropertyMapper<IMapElement, MirageMapElementHandler> Mapper =
        new PropertyMapper<IMapElement, MirageMapElementHandler>(ElementMapper)
        {
            [nameof(IMapElement.Stroke)] = MapStroke,
            [nameof(IMapElement.StrokeThickness)] = MapStrokeThickness,
            [nameof(IFilledMapElement.Fill)] = MapFill,
            ["Geopath"] = MapGeopath,
            [nameof(ICircleMapElement.Radius)] = MapRadius,
            [nameof(ICircleMapElement.Center)] = MapCenter
        };

    public MirageMapElementHandler()
        : base(Mapper)
    {
    }

    protected override MirageMapElementPlatformView CreatePlatformElement() => new();

    public static void MapStroke(MirageMapElementHandler handler, IMapElement element)
    {
        handler.PlatformView.StrokeColor = ToCssColor(element.Stroke);
    }

    public static void MapStrokeThickness(MirageMapElementHandler handler, IMapElement element)
    {
        handler.PlatformView.StrokeThickness = element.StrokeThickness;
    }

    public static void MapFill(MirageMapElementHandler handler, IMapElement element)
    {
        if (element is IFilledMapElement filled)
        {
            handler.PlatformView.FillColor = ToCssColor(filled.Fill);
        }
    }

    public static void MapGeopath(MirageMapElementHandler handler, IMapElement element)
    {
        if (element is not IGeoPathMapElement geopath)
        {
            return;
        }

        var points = new List<MirageMapCoordinate>();
        for (var index = 0; index < geopath.Count; index++)
        {
            var point = geopath[index];
            if (point is null
                || !double.IsFinite(point.Latitude)
                || !double.IsFinite(point.Longitude))
            {
                continue;
            }

            points.Add(new MirageMapCoordinate(point.Latitude, point.Longitude));
        }

        handler.PlatformView.Coordinates = points;
    }

    public static void MapRadius(MirageMapElementHandler handler, IMapElement element)
    {
        if (element is ICircleMapElement circle)
        {
            handler.PlatformView.RadiusMeters = Math.Max(0, circle.Radius.Meters);
        }
    }

    public static void MapCenter(MirageMapElementHandler handler, IMapElement element)
    {
        if (element is not ICircleMapElement circle)
        {
            return;
        }

        if (circle.Center is null
            || !double.IsFinite(circle.Center.Latitude)
            || !double.IsFinite(circle.Center.Longitude))
        {
            return;
        }

        handler.PlatformView.CircleCenter = new MirageMapCoordinate(
            circle.Center.Latitude,
            circle.Center.Longitude);
    }

    internal static bool TryCreateProjection(IMapElement element, string key, out MirageMapElement projection)
    {
        projection = default!;
        ArgumentNullException.ThrowIfNull(element);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var kind = ResolveKind(element);
        var strokeColor = ToCssColor(element.Stroke);
        var strokeThickness = element.StrokeThickness;
        var fillColor = element is IFilledMapElement filled ? ToCssColor(filled.Fill) : null;
        double? radiusMeters = null;
        IReadOnlyList<MirageMapCoordinate> coordinates;

        switch (kind)
        {
            case MirageMapElementKind.Circle:
            {
                if (element is not ICircleMapElement circle
                    || circle.Center is null
                    || !double.IsFinite(circle.Center.Latitude)
                    || !double.IsFinite(circle.Center.Longitude))
                {
                    return false;
                }

                coordinates = new[]
                {
                    new MirageMapCoordinate(circle.Center.Latitude, circle.Center.Longitude)
                };
                radiusMeters = Math.Max(0, circle.Radius.Meters);
                break;
            }
            case MirageMapElementKind.Polygon:
            case MirageMapElementKind.Polyline:
            {
                if (element is not IGeoPathMapElement geopath)
                {
                    return false;
                }

                var points = new List<MirageMapCoordinate>(geopath.Count);
                for (var index = 0; index < geopath.Count; index++)
                {
                    var point = geopath[index];
                    if (point is null
                        || !double.IsFinite(point.Latitude)
                        || !double.IsFinite(point.Longitude))
                    {
                        continue;
                    }

                    points.Add(new MirageMapCoordinate(point.Latitude, point.Longitude));
                }

                if (points.Count == 0)
                {
                    return false;
                }

                coordinates = points;
                break;
            }
            default:
                return false;
        }

        projection = new MirageMapElement(
            key,
            kind,
            coordinates,
            radiusMeters,
            strokeColor,
            strokeThickness,
            fillColor);
        return true;
    }

    private static MirageMapElementKind ResolveKind(IMapElement element)
    {
        return element switch
        {
            Circle => MirageMapElementKind.Circle,
            Polygon => MirageMapElementKind.Polygon,
            Polyline => MirageMapElementKind.Polyline,
            ICircleMapElement => MirageMapElementKind.Circle,
            IFilledMapElement => MirageMapElementKind.Polygon,
            IGeoPathMapElement => MirageMapElementKind.Polyline,
            _ => MirageMapElementKind.Unknown
        };
    }

    private static string? ToCssColor(Paint? paint)
    {
        return paint switch
        {
            SolidPaint solid => MirageCommonViewProjection.ToCssColor(solid.Color),
            _ => null
        };
    }
}

internal sealed class MirageMapElementPlatformView
{
    public string? StrokeColor { get; set; }

    public double StrokeThickness { get; set; }

    public string? FillColor { get; set; }

    public IReadOnlyList<MirageMapCoordinate> Coordinates { get; set; } = Array.Empty<MirageMapCoordinate>();

    public MirageMapCoordinate? CircleCenter { get; set; }

    public double? RadiusMeters { get; set; }
}
