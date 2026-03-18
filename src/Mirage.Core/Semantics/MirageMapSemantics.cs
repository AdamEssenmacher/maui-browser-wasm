namespace Mirage.Core.Semantics;

public static class MirageMapPropertyNames
{
    public const string Region = "MapRegion";
    public const string RegionRequestToken = "MapRegionRequestToken";
    public const string MapType = "MapType";
    public const string Pins = "MapPins";
}

public enum MirageMapType
{
    Street,
    Satellite,
    Hybrid,
    Unknown
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

public sealed record MirageMapSemanticState(
    MirageMapRegion? Region,
    long RegionRequestToken,
    MirageMapType MapType,
    IReadOnlyList<MirageMapPin> Pins)
{
    public static MirageMapSemanticState Empty { get; } =
        new(
            Region: null,
            RegionRequestToken: 0,
            MapType: MirageMapType.Street,
            Pins: Array.Empty<MirageMapPin>());

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
}
