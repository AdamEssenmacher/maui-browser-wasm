using Mirage.Core.Semantics;
using Mirage.Maui.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Platform;
using System.Text.Json.Nodes;
using Xunit;
using MauiMap = Microsoft.Maui.Controls.Maps.Map;

namespace Mirage.Integration.Tests;

public sealed class MirageMapHandlerTests
{
    [Fact]
    public void MapHandler_ProjectsBuiltInMapControlKind()
    {
        var handler = CreateHandler();
        var map = new MauiMap();

        ((IElementHandler)handler).SetVirtualView(map);

        Assert.Equal(ComponentKind.Map, handler.PlatformView.ControlKind);
        Assert.Equal("block", handler.PlatformView.GetStyleValue("display"));
        Assert.Equal("100%", handler.PlatformView.GetStyleValue("width"));
        Assert.Equal("100%", handler.PlatformView.GetStyleValue("height"));
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

    private static long ReadRegionRequestToken(MiragePlatformView node)
    {
        var token = node.GetPropertyString(MirageMapPropertyNames.RegionRequestToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return 0;
        }

        return long.TryParse(token, out var parsed) ? parsed : 0;
    }

    private sealed record PinPayload(string Key, string? Label, double Latitude, double Longitude);
}
