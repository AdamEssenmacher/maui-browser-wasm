namespace Mirage.Core.Semantics;

public static class MirageMapPropertyNames
{
    public const string Region = "MapRegion";
    public const string RegionRequestToken = "MapRegionRequestToken";
    public const string MapType = "MapType";
    public const string IsShowingUser = "MapIsShowingUser";
    public const string IsScrollEnabled = "MapIsScrollEnabled";
    public const string IsZoomEnabled = "MapIsZoomEnabled";
    public const string IsTrafficEnabled = "MapIsTrafficEnabled";
    public const string Pins = "MapPins";
    public const string Elements = "MapElements";
    public const string DismissPopupKey = "MapDismissPopupKey";
    public const string DismissPopupRequestToken = "MapDismissPopupRequestToken";
}

public enum MirageMapType
{
    Street,
    Satellite,
    Hybrid,
    Unknown
}

public enum MirageMapElementKind
{
    Unknown,
    Circle,
    Polyline,
    Polygon
}

public readonly record struct MirageMapRegion(
    double CenterLatitude,
    double CenterLongitude,
    double LatitudeSpan,
    double LongitudeSpan)
{
    public static bool TryCreate(
        double centerLatitude,
        double centerLongitude,
        double latitudeSpan,
        double longitudeSpan,
        out MirageMapRegion region)
    {
        region = default;
        if (!double.IsFinite(centerLatitude)
            || !double.IsFinite(centerLongitude)
            || !double.IsFinite(latitudeSpan)
            || !double.IsFinite(longitudeSpan))
        {
            return false;
        }

        var normalizedLatitude = Math.Clamp(centerLatitude, -90d, 90d);
        var normalizedLongitude = NormalizeLongitude(centerLongitude);
        var normalizedLatitudeSpan = Math.Clamp(Math.Abs(latitudeSpan), 0.0001d, 180d);
        var normalizedLongitudeSpan = Math.Clamp(Math.Abs(longitudeSpan), 0.0001d, 360d);
        region = new MirageMapRegion(
            normalizedLatitude,
            normalizedLongitude,
            normalizedLatitudeSpan,
            normalizedLongitudeSpan);
        return true;
    }

    private static double NormalizeLongitude(double longitude)
    {
        var normalized = longitude;
        while (normalized < -180d)
        {
            normalized += 360d;
        }

        while (normalized > 180d)
        {
            normalized -= 360d;
        }

        return normalized;
    }
}

public sealed record MirageMapPin(
    string Key,
    double Latitude,
    double Longitude,
    string? Label,
    string? Address,
    string? Type);

public readonly record struct MirageMapCoordinate(
    double Latitude,
    double Longitude);

public sealed record MirageMapElement(
    string Key,
    MirageMapElementKind Kind,
    IReadOnlyList<MirageMapCoordinate> Coordinates,
    double? RadiusMeters,
    string? StrokeColor,
    double StrokeThickness,
    string? FillColor);

public sealed record MirageMapSemanticState(
    MirageMapRegion? Region,
    long RegionRequestToken,
    MirageMapType MapType,
    bool IsShowingUser,
    bool IsScrollEnabled,
    bool IsZoomEnabled,
    bool IsTrafficEnabled,
    IReadOnlyList<MirageMapPin> Pins,
    IReadOnlyList<MirageMapElement> Elements,
    string? DismissPopupKey,
    long DismissPopupRequestToken)
{
    public static MirageMapSemanticState Empty { get; } =
        new(
            Region: null,
            RegionRequestToken: 0,
            MapType: MirageMapType.Street,
            IsShowingUser: false,
            IsScrollEnabled: true,
            IsZoomEnabled: true,
            IsTrafficEnabled: false,
            Pins: Array.Empty<MirageMapPin>(),
            Elements: Array.Empty<MirageMapElement>(),
            DismissPopupKey: null,
            DismissPopupRequestToken: 0);

    public static MirageMapType ParseMapType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return MirageMapType.Street;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "street" => MirageMapType.Street,
            "satellite" => MirageMapType.Satellite,
            "hybrid" => MirageMapType.Hybrid,
            _ => MirageMapType.Unknown
        };
    }

    public static bool ParseBoolean(string? value, bool defaultValue = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "1" => true,
            "true" => true,
            "yes" => true,
            "on" => true,
            "0" => false,
            "false" => false,
            "no" => false,
            "off" => false,
            _ => defaultValue
        };
    }
}
