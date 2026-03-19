using Mirage.Core.Semantics;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageMapPinHandler : ElementHandler<IMapPin, MirageMapPinPlatformView>
{
    internal static readonly string[] ParityPropertyKeys =
    {
        nameof(IMapPin.Location),
        nameof(IMapPin.Label),
        nameof(IMapPin.Address)
    };

    public static IPropertyMapper<IMapPin, MirageMapPinHandler> Mapper =
        new PropertyMapper<IMapPin, MirageMapPinHandler>(ElementMapper)
        {
            [nameof(IMapPin.Location)] = MapLocation,
            [nameof(IMapPin.Label)] = MapLabel,
            [nameof(IMapPin.Address)] = MapAddress
        };

    public MirageMapPinHandler()
        : base(Mapper)
    {
    }

    protected override MirageMapPinPlatformView CreatePlatformElement() => new();

    public static void MapLocation(MirageMapPinHandler handler, IMapPin pin)
    {
        handler.PlatformView.Latitude = pin.Location.Latitude;
        handler.PlatformView.Longitude = pin.Location.Longitude;
    }

    public static void MapLabel(MirageMapPinHandler handler, IMapPin pin)
    {
        handler.PlatformView.Label = pin.Label;
    }

    public static void MapAddress(MirageMapPinHandler handler, IMapPin pin)
    {
        handler.PlatformView.Address = pin.Address;
    }

    internal static bool TryCreateProjection(IMapPin pin, string key, out MirageMapPin projection)
    {
        projection = default!;
        ArgumentNullException.ThrowIfNull(pin);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var location = pin.Location;
        if (location is null
            || !double.IsFinite(location.Latitude)
            || !double.IsFinite(location.Longitude))
        {
            return false;
        }

        var type = pin is Pin concretePin
            ? concretePin.Type.ToString()
            : null;
        projection = new MirageMapPin(
            key,
            location.Latitude,
            location.Longitude,
            pin.Label,
            pin.Address,
            type);
        return true;
    }
}

internal sealed class MirageMapPinPlatformView
{
    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string? Label { get; set; }

    public string? Address { get; set; }
}
