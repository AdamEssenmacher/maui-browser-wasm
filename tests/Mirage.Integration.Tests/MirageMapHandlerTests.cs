using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Platform;
using System.Linq;
using System.Text.Json.Nodes;
using Xunit;
using MauiMap = Microsoft.Maui.Controls.Maps.Map;

namespace Mirage.Integration.Tests;

public sealed class MirageMapHandlerTests
{
    [Fact]
    public void MapHandler_ProjectsBuiltInMapControlKind_AndRegistersRuntimeEvents()
    {
        var handler = CreateHandler();
        var map = new MauiMap();

        ((IElementHandler)handler).SetVirtualView(map);

        Assert.Equal(ComponentKind.Map, handler.PlatformView.ControlKind);
        Assert.Equal("block", handler.PlatformView.GetStyleValue("display"));
        Assert.Contains("mapregionchanged", handler.PlatformView.Events);
        Assert.Contains("mapclicked", handler.PlatformView.Events);
        Assert.Contains("pinclicked", handler.PlatformView.Events);
        Assert.Contains("pininfowindowclicked", handler.PlatformView.Events);
    }

    [Fact]
    public void MapHandler_MoveToRegion_ProjectsRegionAndRequestToken()
    {
        var handler = CreateHandler();
        var map = new MauiMap();

        ((IElementHandler)handler).SetVirtualView(map);
        var baselineToken = ReadRegionRequestToken(handler.PlatformView);
        var firstRegion = MapSpan.FromCenterAndRadius(new Location(37.7749, -122.4194), Distance.FromKilometers(5));
        MirageMapHandler.MapMoveToRegion(handler, map, firstRegion);

        Assert.Equal((baselineToken + 1).ToString(), handler.PlatformView.GetPropertyString(MirageMapPropertyNames.RegionRequestToken));
        var projectedRegion = ReadRegion(handler.PlatformView);
        Assert.Equal(37.7749, projectedRegion.CenterLatitude, 4);
        Assert.Equal(-122.4194, projectedRegion.CenterLongitude, 4);

        var secondRegion = MapSpan.FromCenterAndRadius(new Location(47.6062, -122.3321), Distance.FromKilometers(8));
        MirageMapHandler.MapMoveToRegion(handler, map, secondRegion);

        Assert.Equal((baselineToken + 2).ToString(), handler.PlatformView.GetPropertyString(MirageMapPropertyNames.RegionRequestToken));
        var projectedSecondRegion = ReadRegion(handler.PlatformView);
        Assert.Equal(47.6062, projectedSecondRegion.CenterLatitude, 4);
        Assert.Equal(-122.3321, projectedSecondRegion.CenterLongitude, 4);
    }

    [Fact]
    public void MapHandler_ProjectsMapOptions_AndElements()
    {
        var handler = CreateHandler();
        var map = new MauiMap
        {
            IsShowingUser = true,
            IsScrollEnabled = false,
            IsZoomEnabled = false,
            IsTrafficEnabled = true,
            MapType = MapType.Hybrid
        };

        map.MapElements.Add(new Circle
        {
            Center = new Location(37.78, -122.40),
            Radius = Distance.FromKilometers(1.8),
            StrokeColor = Colors.DodgerBlue,
            FillColor = Colors.LightBlue,
            StrokeWidth = 4f
        });

        var polyline = new Polyline
        {
            StrokeColor = Colors.OrangeRed,
            StrokeWidth = 3f
        };
        polyline.Geopath.Add(new Location(37.78, -122.40));
        polyline.Geopath.Add(new Location(37.79, -122.41));
        map.MapElements.Add(polyline);

        var polygon = new Polygon
        {
            StrokeColor = Colors.MediumSeaGreen,
            FillColor = Colors.PaleGreen,
            StrokeWidth = 2f
        };
        polygon.Geopath.Add(new Location(37.80, -122.42));
        polygon.Geopath.Add(new Location(37.81, -122.41));
        polygon.Geopath.Add(new Location(37.79, -122.40));
        map.MapElements.Add(polygon);

        ((IElementHandler)handler).SetVirtualView(map);

        Assert.True(ReadBoolProperty(handler.PlatformView, MirageMapPropertyNames.IsShowingUser));
        Assert.False(ReadBoolProperty(handler.PlatformView, MirageMapPropertyNames.IsScrollEnabled));
        Assert.False(ReadBoolProperty(handler.PlatformView, MirageMapPropertyNames.IsZoomEnabled));
        Assert.True(ReadBoolProperty(handler.PlatformView, MirageMapPropertyNames.IsTrafficEnabled));
        Assert.Equal("hybrid", handler.PlatformView.GetPropertyString(MirageMapPropertyNames.MapType));

        var elements = ReadElementPayloads(handler.PlatformView);
        Assert.Equal(3, elements.Count);
        Assert.Contains(elements, element => string.Equals(element.Kind, "circle", StringComparison.Ordinal));
        Assert.Contains(elements, element => string.Equals(element.Kind, "polyline", StringComparison.Ordinal));
        Assert.Contains(elements, element => string.Equals(element.Kind, "polygon", StringComparison.Ordinal));
    }

    [Fact]
    public void MapHandler_PinProjection_UsesStableIdentityAndDeterministicSync_WithoutExplicitMapperCalls()
    {
        var handler = CreateHandler();
        var map = new MauiMap();

        var firstPin = new Pin
        {
            Label = "First",
            Location = new Location(37.7800, -122.4000),
            Type = PinType.Place
        };
        var secondPin = new Pin
        {
            Label = "Second",
            Location = new Location(37.7900, -122.4100),
            Type = PinType.Place
        };

        map.Pins.Add(firstPin);
        map.Pins.Add(secondPin);
        ((IElementHandler)handler).SetVirtualView(map);

        var initialPins = ReadPinPayloads(handler.PlatformView);
        Assert.Equal(2, initialPins.Count);
        var initialFirstKey = initialPins.Single(pin => pin.Label == "First").Key;
        var initialSecondKey = initialPins.Single(pin => pin.Label == "Second").Key;

        firstPin.Label = "First Updated";
        firstPin.Location = new Location(37.7850, -122.4050);

        var updatedPins = ReadPinPayloads(handler.PlatformView);
        Assert.Equal(2, updatedPins.Count);
        var updatedFirst = updatedPins.Single(pin => pin.Label == "First Updated");
        Assert.Equal(initialFirstKey, updatedFirst.Key);
        Assert.Contains(updatedPins, pin => pin.Key == initialSecondKey);

        map.Pins.Remove(secondPin);
        var thirdPin = new Pin
        {
            Label = "Third",
            Location = new Location(37.8000, -122.4200),
            Type = PinType.Place
        };
        map.Pins.Add(thirdPin);

        var replacementPins = ReadPinPayloads(handler.PlatformView);
        Assert.Equal(2, replacementPins.Count);
        Assert.Contains(replacementPins, pin => pin.Key == initialFirstKey);
        Assert.DoesNotContain(replacementPins, pin => pin.Key == initialSecondKey);
        Assert.Contains(replacementPins, pin => pin.Label == "Third" && pin.Key != initialFirstKey);
    }

    [Fact]
    public void MapHandler_RuntimeMapClicked_ForwardsToMapClickedEvent()
    {
        var handler = CreateHandler();
        var map = new MauiMap();
        var clickCount = 0;
        Location? clickedLocation = null;
        map.MapClicked += (_, args) =>
        {
            clickCount++;
            clickedLocation = args.Location;
        };

        ((IElementHandler)handler).SetVirtualView(map);
        handler.OnRuntimeEvent("mapclicked", "{\"latitude\":47.61,\"longitude\":-122.33}");

        Assert.Equal(1, clickCount);
        Assert.NotNull(clickedLocation);
        Assert.Equal(47.61, clickedLocation!.Latitude, 2);
        Assert.Equal(-122.33, clickedLocation.Longitude, 2);
    }

    [Fact]
    public void MapHandler_RuntimePinClick_WithHideInfoWindow_RequestsPopupDismiss()
    {
        var handler = CreateHandler();
        var map = new MauiMap();
        var pin = new Pin
        {
            Label = "Mirage",
            Address = "SF",
            Location = new Location(37.78, -122.40)
        };

        map.Pins.Add(pin);
        pin.MarkerClicked += (_, args) => args.HideInfoWindow = true;

        ((IElementHandler)handler).SetVirtualView(map);

        var pinKey = ReadPinPayloads(handler.PlatformView).Single().Key;
        handler.OnRuntimeEvent("pinclicked", $"{{\"key\":\"{pinKey}\"}}");

        Assert.Equal(pinKey, handler.PlatformView.GetPropertyString(MirageMapPropertyNames.DismissPopupKey));
        Assert.True(ReadDismissPopupToken(handler.PlatformView) > 0);
    }

    [Fact]
    public void MapHandler_RuntimeRegionChanged_UpdatesVisibleRegionWithoutMoveTokenBump()
    {
        var handler = CreateHandler();
        var map = new MauiMap();

        ((IElementHandler)handler).SetVirtualView(map);
        var baselineToken = ReadRegionRequestToken(handler.PlatformView);

        handler.OnRuntimeEvent(
            "mapregionchanged",
            "{\"centerLatitude\":47.6062,\"centerLongitude\":-122.3321,\"latitudeSpan\":0.24,\"longitudeSpan\":0.38}");

        Assert.NotNull(map.VisibleRegion);
        Assert.Equal(47.6062, map.VisibleRegion!.Center.Latitude, 4);
        Assert.Equal(-122.3321, map.VisibleRegion.Center.Longitude, 4);
        Assert.Equal(baselineToken, ReadRegionRequestToken(handler.PlatformView));
    }

    private static MirageMapHandler CreateHandler()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var handler = new MirageMapHandler();
        ((IElementHandler)handler).SetMauiContext(new MauiContext(services));
        return handler;
    }

    private static MirageMapRegion ReadRegion(MiragePlatformView node)
    {
        Assert.True(node.Properties.TryGetValue(MirageMapPropertyNames.Region, out var value));
        Assert.True(value.TryGetJson(out var regionNode));
        var regionJson = Assert.IsType<JsonObject>(regionNode);
        Assert.True(MirageMapRegion.TryCreate(
            centerLatitude: regionJson["centerLatitude"]!.GetValue<double>(),
            centerLongitude: regionJson["centerLongitude"]!.GetValue<double>(),
            latitudeSpan: regionJson["latitudeSpan"]!.GetValue<double>(),
            longitudeSpan: regionJson["longitudeSpan"]!.GetValue<double>(),
            out var region));
        return region;
    }

    private static IReadOnlyList<PinPayload> ReadPinPayloads(MiragePlatformView node)
    {
        Assert.True(node.Properties.TryGetValue(MirageMapPropertyNames.Pins, out var value));
        Assert.True(value.TryGetJson(out var pinsNode));
        var pinsArray = Assert.IsType<JsonArray>(pinsNode);
        var pins = new List<PinPayload>();
        foreach (var item in pinsArray)
        {
            if (item is not JsonObject pinJson)
            {
                continue;
            }

            pins.Add(new PinPayload(
                Key: pinJson["key"]?.GetValue<string>() ?? string.Empty,
                Label: pinJson["label"]?.GetValue<string>(),
                Latitude: pinJson["latitude"]?.GetValue<double>() ?? 0,
                Longitude: pinJson["longitude"]?.GetValue<double>() ?? 0));
        }

        return pins;
    }

    private static IReadOnlyList<ElementPayload> ReadElementPayloads(MiragePlatformView node)
    {
        Assert.True(node.Properties.TryGetValue(MirageMapPropertyNames.Elements, out var value));
        Assert.True(value.TryGetJson(out var elementsNode));
        var elementsArray = Assert.IsType<JsonArray>(elementsNode);
        var elements = new List<ElementPayload>();
        foreach (var item in elementsArray)
        {
            if (item is not JsonObject elementJson)
            {
                continue;
            }

            elements.Add(new ElementPayload(
                Key: elementJson["key"]?.GetValue<string>() ?? string.Empty,
                Kind: elementJson["kind"]?.GetValue<string>() ?? string.Empty));
        }

        return elements;
    }

    private static bool ReadBoolProperty(MiragePlatformView node, string propertyName)
    {
        Assert.True(node.Properties.TryGetValue(propertyName, out var value));
        if (value.TryGetBool(out var boolValue))
        {
            return boolValue;
        }

        return bool.TryParse(value.GetStringOrNull(), out var parsed) && parsed;
    }

    private static long ReadRegionRequestToken(MiragePlatformView node)
    {
        var token = node.GetPropertyString(MirageMapPropertyNames.RegionRequestToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return 0;
        }

        return long.TryParse(token, out var parsed) ? parsed : 0;
    }

    private static long ReadDismissPopupToken(MiragePlatformView node)
    {
        var token = node.GetPropertyString(MirageMapPropertyNames.DismissPopupRequestToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return 0;
        }

        return long.TryParse(token, out var parsed) ? parsed : 0;
    }

    private sealed record PinPayload(string Key, string? Label, double Latitude, double Longitude);

    private sealed record ElementPayload(string Key, string Kind);
}
