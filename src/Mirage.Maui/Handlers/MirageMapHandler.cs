using Mirage.Core.Diagnostics;
using Mirage.Core.Semantics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Maps.Handlers;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using MauiIMap = Microsoft.Maui.Maps.IMap;
using MauiMap = Microsoft.Maui.Controls.Maps.Map;

namespace Mirage.Maui.Handlers;

internal sealed class MirageMapHandler : MirageViewHandler<MauiMap>, IMirageRuntimeEventTarget
{
    private const string RuntimeEventRegionChanged = "mapregionchanged";
    private const string RuntimeEventMapClicked = "mapclicked";
    private const string RuntimeEventPinClicked = "pinclicked";
    private const string RuntimeEventPinInfoWindowClicked = "pininfowindowclicked";
    private const string RuntimeEventDiagnostic = "mapdiagnostic";

    private readonly ConditionalWeakTable<IMapPin, PinIdentity> _pinIdentityByPin = new();
    private readonly ConditionalWeakTable<MapElement, MapElementIdentity> _elementIdentityByElement = new();
    private readonly HashSet<IMapPin> _subscribedPins = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<MapElement> _subscribedElements = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<string, IMapPin> _pinByKey = new(StringComparer.Ordinal);
    private INotifyCollectionChanged? _pinsCollection;
    private INotifyCollectionChanged? _elementsCollection;
    private long _nextPinIdentity = 1;
    private long _nextElementIdentity = 1;
    private long _regionRequestToken;
    private long _dismissPopupRequestToken;
    private bool _applyingRuntimeRegion;

    internal static readonly string[] ParityPropertyKeys =
    {
        nameof(MauiIMap.MapType),
        nameof(MauiIMap.IsShowingUser),
        nameof(MauiIMap.IsScrollEnabled),
        nameof(MauiIMap.IsTrafficEnabled),
        nameof(MauiIMap.IsZoomEnabled),
        nameof(MauiIMap.Pins),
        nameof(MauiIMap.Elements),
        nameof(MauiIMap.VisibleRegion)
    };

    internal static readonly string[] ParityCommandKeys =
    {
        nameof(MauiIMap.MoveToRegion),
        nameof(IMapHandler.UpdateMapElement)
    };

    internal static readonly string[] ParityRuntimeEvents =
    {
        RuntimeEventRegionChanged,
        RuntimeEventMapClicked,
        RuntimeEventPinClicked,
        RuntimeEventPinInfoWindowClicked,
        RuntimeEventDiagnostic
    };

    public static IPropertyMapper<MauiMap, MirageMapHandler> Mapper =
        new PropertyMapper<MauiMap, MirageMapHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(MauiIMap.MapType)] = MapMapType,
            [nameof(MauiIMap.IsShowingUser)] = MapIsShowingUser,
            [nameof(MauiIMap.IsScrollEnabled)] = MapIsScrollEnabled,
            [nameof(MauiIMap.IsTrafficEnabled)] = MapIsTrafficEnabled,
            [nameof(MauiIMap.IsZoomEnabled)] = MapIsZoomEnabled,
            [nameof(MauiIMap.Pins)] = MapPins,
            [nameof(MauiIMap.Elements)] = MapElements,
            [nameof(MauiIMap.VisibleRegion)] = MapVisibleRegion
        };

    public static CommandMapper<MauiMap, MirageMapHandler> CommandMapper =
        new(MirageViewMapper.CommonViewCommandMapper)
        {
            [nameof(MauiIMap.MoveToRegion)] = MapMoveToRegion,
            [nameof(IMapHandler.UpdateMapElement)] = MapUpdateMapElement
        };

    public MirageMapHandler()
        : base(ComponentKind.Map, Mapper, CommandMapper)
    {
        DefaultDisplayStyle = "block";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        HookPinsCollection(VirtualView);
        HookElementsCollection(VirtualView);
        SyncPinSubscriptions(VirtualView);
        SyncElementSubscriptions(VirtualView);

        var changed = SetStyleIfChanged("display", "block");
        changed |= SetEventIfChanged(RuntimeEventRegionChanged, enabled: true);
        changed |= SetEventIfChanged(RuntimeEventMapClicked, enabled: true);
        changed |= SetEventIfChanged(RuntimeEventPinClicked, enabled: true);
        changed |= SetEventIfChanged(RuntimeEventPinInfoWindowClicked, enabled: true);
        changed |= SetEventIfChanged(RuntimeEventDiagnostic, enabled: true);
        changed |= UpdateMapTypeProjection(VirtualView);
        changed |= UpdateMapOptionsProjection(VirtualView);
        changed |= UpdatePinsProjection(VirtualView);
        changed |= UpdateElementsProjection(VirtualView);
        if (VirtualView.VisibleRegion is MapSpan visibleRegion)
        {
            changed |= UpdateRegionProjection(visibleRegion, fromMoveRequest: false);
        }

        base.ConnectHandler(platformView);
        if (changed)
        {
            RequestRender();
        }
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        UnhookPinsCollection();
        UnhookElementsCollection();
        UnhookPinPropertySubscriptions();
        UnhookElementPropertySubscriptions();
        _pinByKey.Clear();
        base.DisconnectHandler(platformView);
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        switch (eventName)
        {
            case RuntimeEventRegionChanged:
                ApplyRuntimeVisibleRegion(payload);
                break;
            case RuntimeEventMapClicked:
                DispatchMapClicked(payload);
                break;
            case RuntimeEventPinClicked:
                DispatchPinClicked(payload, infoWindowClick: false);
                break;
            case RuntimeEventPinInfoWindowClicked:
                DispatchPinClicked(payload, infoWindowClick: true);
                break;
            case RuntimeEventDiagnostic:
                LogMapDiagnostic(payload);
                break;
        }
    }

    public static void MapMapType(MirageMapHandler handler, MauiMap map)
    {
        if (!handler.UpdateMapTypeProjection(map))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapIsShowingUser(MirageMapHandler handler, MauiMap map)
    {
        if (!handler.UpdateMapOptionsProjection(map))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapIsScrollEnabled(MirageMapHandler handler, MauiMap map)
    {
        if (!handler.UpdateMapOptionsProjection(map))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapIsTrafficEnabled(MirageMapHandler handler, MauiMap map)
    {
        if (!handler.UpdateMapOptionsProjection(map))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapIsZoomEnabled(MirageMapHandler handler, MauiMap map)
    {
        if (!handler.UpdateMapOptionsProjection(map))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapVisibleRegion(MirageMapHandler handler, MauiMap map)
    {
        if (map.VisibleRegion is not MapSpan visibleRegion)
        {
            return;
        }

        if (!handler.UpdateRegionProjection(visibleRegion, fromMoveRequest: false))
        {
            return;
        }

        if (handler._applyingRuntimeRegion)
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapPins(MirageMapHandler handler, MauiMap map)
    {
        handler.HookPinsCollection(map);
        handler.SyncPinSubscriptions(map);
        if (!handler.UpdatePinsProjection(map))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapElements(MirageMapHandler handler, MauiMap map)
    {
        handler.HookElementsCollection(map);
        handler.SyncElementSubscriptions(map);
        if (!handler.UpdateElementsProjection(map))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapMoveToRegion(MirageMapHandler handler, MauiMap map, object? args)
    {
        if (args is not MapSpan requestedRegion)
        {
            if (map.VisibleRegion is not MapSpan visibleRegion)
            {
                return;
            }

            requestedRegion = visibleRegion;
        }

        if (!handler.UpdateRegionProjection(requestedRegion, fromMoveRequest: true))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapUpdateMapElement(MirageMapHandler handler, MauiMap map, object? args)
    {
        _ = args;
        handler.HookElementsCollection(map);
        handler.SyncElementSubscriptions(map);
        if (!handler.UpdateElementsProjection(map))
        {
            return;
        }

        handler.RequestRender();
    }

    private bool UpdateMapTypeProjection(MauiMap map)
    {
        var mapType = map.MapType switch
        {
            MapType.Satellite => "satellite",
            MapType.Hybrid => "hybrid",
            _ => "street"
        };

        return SetPropertyIfChanged(MirageMapPropertyNames.MapType, MirageValue.String(mapType));
    }

    private bool UpdateMapOptionsProjection(MauiMap map)
    {
        var changed = SetPropertyIfChanged(
            MirageMapPropertyNames.IsShowingUser,
            MirageValue.Bool(map.IsShowingUser));
        changed |= SetPropertyIfChanged(
            MirageMapPropertyNames.IsScrollEnabled,
            MirageValue.Bool(map.IsScrollEnabled));
        changed |= SetPropertyIfChanged(
            MirageMapPropertyNames.IsZoomEnabled,
            MirageValue.Bool(map.IsZoomEnabled));
        changed |= SetPropertyIfChanged(
            MirageMapPropertyNames.IsTrafficEnabled,
            MirageValue.Bool(map.IsTrafficEnabled));
        return changed;
    }

    private bool UpdateRegionProjection(MapSpan region, bool fromMoveRequest)
    {
        var center = region.Center;
        if (!MirageMapRegion.TryCreate(
                center.Latitude,
                center.Longitude,
                region.LatitudeDegrees,
                region.LongitudeDegrees,
                out var normalizedRegion))
        {
            return false;
        }

        var regionPayload = new JsonObject
        {
            ["centerLatitude"] = normalizedRegion.CenterLatitude,
            ["centerLongitude"] = normalizedRegion.CenterLongitude,
            ["latitudeSpan"] = normalizedRegion.LatitudeSpan,
            ["longitudeSpan"] = normalizedRegion.LongitudeSpan
        };

        var changed = SetPropertyIfChanged(MirageMapPropertyNames.Region, MirageValue.Json(regionPayload));
        if (fromMoveRequest)
        {
            _regionRequestToken++;
            changed |= SetPropertyIfChanged(
                MirageMapPropertyNames.RegionRequestToken,
                MirageValue.String(_regionRequestToken.ToString(CultureInfo.InvariantCulture)));
        }

        return changed;
    }

    private bool UpdatePinsProjection(MauiMap map)
    {
        var pinsPayload = new JsonArray();
        _pinByKey.Clear();

        foreach (var pin in ((MauiIMap)map).Pins)
        {
            var key = GetOrCreatePinKey(pin);
            if (!MirageMapPinHandler.TryCreateProjection(pin, key, out var projection))
            {
                continue;
            }

            _pinByKey[key] = pin;
            pinsPayload.Add(new JsonObject
            {
                ["key"] = projection.Key,
                ["latitude"] = projection.Latitude,
                ["longitude"] = projection.Longitude,
                ["label"] = projection.Label,
                ["address"] = projection.Address,
                ["type"] = projection.Type
            });
        }

        return SetPropertyIfChanged(MirageMapPropertyNames.Pins, MirageValue.Json(pinsPayload));
    }

    private bool UpdateElementsProjection(MauiMap map)
    {
        var elementsPayload = new JsonArray();
        foreach (var element in map.MapElements)
        {
            var key = GetOrCreateElementKey(element);
            if (!MirageMapElementHandler.TryCreateProjection(element, key, out var projection))
            {
                continue;
            }

            var coordinates = new JsonArray();
            foreach (var coordinate in projection.Coordinates)
            {
                coordinates.Add(new JsonObject
                {
                    ["latitude"] = coordinate.Latitude,
                    ["longitude"] = coordinate.Longitude
                });
            }

            var elementPayload = new JsonObject
            {
                ["key"] = projection.Key,
                ["kind"] = projection.Kind.ToString().ToLowerInvariant(),
                ["coordinates"] = coordinates,
                ["radiusMeters"] = projection.RadiusMeters,
                ["strokeColor"] = projection.StrokeColor,
                ["strokeThickness"] = projection.StrokeThickness,
                ["fillColor"] = projection.FillColor
            };
            elementsPayload.Add(elementPayload);
        }

        return SetPropertyIfChanged(MirageMapPropertyNames.Elements, MirageValue.Json(elementsPayload));
    }

    private string GetOrCreatePinKey(IMapPin pin)
    {
        if (_pinIdentityByPin.TryGetValue(pin, out var identity))
        {
            return identity.Key;
        }

        identity = new PinIdentity($"pin-{_nextPinIdentity++:D6}");
        _pinIdentityByPin.Add(pin, identity);
        return identity.Key;
    }

    private string GetOrCreateElementKey(MapElement element)
    {
        if (_elementIdentityByElement.TryGetValue(element, out var identity))
        {
            return identity.Key;
        }

        identity = new MapElementIdentity($"element-{_nextElementIdentity++:D6}");
        _elementIdentityByElement.Add(element, identity);
        return identity.Key;
    }

    private void HookPinsCollection(MauiMap map)
    {
        if (map.Pins is INotifyCollectionChanged pinsCollection
            && ReferenceEquals(pinsCollection, _pinsCollection))
        {
            return;
        }

        UnhookPinsCollection();
        if (map.Pins is INotifyCollectionChanged nextCollection)
        {
            _pinsCollection = nextCollection;
            _pinsCollection.CollectionChanged += OnPinsCollectionChanged;
        }
    }

    private void HookElementsCollection(MauiMap map)
    {
        if (map.MapElements is INotifyCollectionChanged elementsCollection
            && ReferenceEquals(elementsCollection, _elementsCollection))
        {
            return;
        }

        UnhookElementsCollection();
        if (map.MapElements is INotifyCollectionChanged nextCollection)
        {
            _elementsCollection = nextCollection;
            _elementsCollection.CollectionChanged += OnElementsCollectionChanged;
        }
    }

    private void UnhookPinsCollection()
    {
        if (_pinsCollection is null)
        {
            return;
        }

        _pinsCollection.CollectionChanged -= OnPinsCollectionChanged;
        _pinsCollection = null;
    }

    private void UnhookElementsCollection()
    {
        if (_elementsCollection is null)
        {
            return;
        }

        _elementsCollection.CollectionChanged -= OnElementsCollectionChanged;
        _elementsCollection = null;
    }

    private void OnPinsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        SyncPinSubscriptions(VirtualView);
        if (!UpdatePinsProjection(VirtualView))
        {
            return;
        }

        RequestRender();
    }

    private void OnElementsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        SyncElementSubscriptions(VirtualView);
        if (!UpdateElementsProjection(VirtualView))
        {
            return;
        }

        RequestRender();
    }

    private void OnPinPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = e;
        if (sender is not IMapPin)
        {
            return;
        }

        if (!UpdatePinsProjection(VirtualView))
        {
            return;
        }

        RequestRender();
    }

    private void OnMapElementPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = e;
        if (sender is not MapElement)
        {
            return;
        }

        if (!UpdateElementsProjection(VirtualView))
        {
            return;
        }

        RequestRender();
    }

    private void SyncPinSubscriptions(MauiMap map)
    {
        var activePins = new HashSet<IMapPin>(ReferenceEqualityComparer.Instance);
        foreach (var candidate in ((MauiIMap)map).Pins)
        {
            activePins.Add(candidate);
        }

        if (_subscribedPins.Count > 0)
        {
            var removedPins = new List<IMapPin>();
            foreach (var pin in _subscribedPins)
            {
                if (!activePins.Contains(pin))
                {
                    removedPins.Add(pin);
                }
            }

            foreach (var pin in removedPins)
            {
                if (pin is INotifyPropertyChanged changed)
                {
                    changed.PropertyChanged -= OnPinPropertyChanged;
                }

                _subscribedPins.Remove(pin);
            }
        }

        foreach (var pin in activePins)
        {
            if (_subscribedPins.Add(pin) && pin is INotifyPropertyChanged changed)
            {
                changed.PropertyChanged += OnPinPropertyChanged;
            }
        }
    }

    private void SyncElementSubscriptions(MauiMap map)
    {
        var activeElements = new HashSet<MapElement>(ReferenceEqualityComparer.Instance);
        foreach (var candidate in map.MapElements)
        {
            activeElements.Add(candidate);
        }

        if (_subscribedElements.Count > 0)
        {
            var removedElements = new List<MapElement>();
            foreach (var element in _subscribedElements)
            {
                if (!activeElements.Contains(element))
                {
                    removedElements.Add(element);
                }
            }

            foreach (var element in removedElements)
            {
                element.PropertyChanged -= OnMapElementPropertyChanged;
                _subscribedElements.Remove(element);
            }
        }

        foreach (var element in activeElements)
        {
            if (_subscribedElements.Add(element))
            {
                element.PropertyChanged += OnMapElementPropertyChanged;
            }
        }
    }

    private void UnhookPinPropertySubscriptions()
    {
        if (_subscribedPins.Count == 0)
        {
            return;
        }

        foreach (var pin in _subscribedPins)
        {
            if (pin is INotifyPropertyChanged changed)
            {
                changed.PropertyChanged -= OnPinPropertyChanged;
            }
        }

        _subscribedPins.Clear();
    }

    private void UnhookElementPropertySubscriptions()
    {
        if (_subscribedElements.Count == 0)
        {
            return;
        }

        foreach (var element in _subscribedElements)
        {
            element.PropertyChanged -= OnMapElementPropertyChanged;
        }

        _subscribedElements.Clear();
    }

    private void ApplyRuntimeVisibleRegion(string? payload)
    {
        if (!TryParseRuntimeRegion(payload, out var region))
        {
            return;
        }

        _applyingRuntimeRegion = true;
        try
        {
            ((MauiIMap)VirtualView).VisibleRegion = new MapSpan(
                new Location(region.CenterLatitude, region.CenterLongitude),
                region.LatitudeSpan,
                region.LongitudeSpan);
        }
        finally
        {
            _applyingRuntimeRegion = false;
        }
    }

    private void DispatchMapClicked(string? payload)
    {
        if (!TryReadCoordinatePayload(payload, out var latitude, out var longitude))
        {
            return;
        }

        ((MauiIMap)VirtualView).Clicked(new Location(latitude, longitude));
    }

    private void DispatchPinClicked(string? payload, bool infoWindowClick)
    {
        if (!TryReadPinKey(payload, out var pinKey)
            || !_pinByKey.TryGetValue(pinKey, out var pin))
        {
            return;
        }

        var hideInfoWindow = infoWindowClick
            ? pin.SendInfoWindowClick()
            : pin.SendMarkerClick();

        if (hideInfoWindow)
        {
            RequestPopupDismiss(pinKey);
        }
    }

    private void RequestPopupDismiss(string pinKey)
    {
        var changed = SetPropertyIfChanged(MirageMapPropertyNames.DismissPopupKey, MirageValue.String(pinKey));
        _dismissPopupRequestToken++;
        changed |= SetPropertyIfChanged(
            MirageMapPropertyNames.DismissPopupRequestToken,
            MirageValue.String(_dismissPopupRequestToken.ToString(CultureInfo.InvariantCulture)));

        if (changed)
        {
            RequestRender();
        }
    }

    private static bool TryParseRuntimeRegion(string? payload, out MirageMapRegion region)
    {
        region = default;
        if (!TryParseJsonObject(payload, out var payloadJson))
        {
            return false;
        }

        var centerLatitude = TryGetNumber(payloadJson, "centerLatitude");
        var centerLongitude = TryGetNumber(payloadJson, "centerLongitude");
        var latitudeSpan = TryGetNumber(payloadJson, "latitudeSpan");
        var longitudeSpan = TryGetNumber(payloadJson, "longitudeSpan");
        return MirageMapRegion.TryCreate(
            centerLatitude,
            centerLongitude,
            latitudeSpan,
            longitudeSpan,
            out region);
    }

    private static bool TryReadCoordinatePayload(string? payload, out double latitude, out double longitude)
    {
        latitude = 0;
        longitude = 0;
        if (!TryParseJsonObject(payload, out var payloadJson))
        {
            return false;
        }

        latitude = TryGetNumber(payloadJson, "latitude");
        longitude = TryGetNumber(payloadJson, "longitude");
        return double.IsFinite(latitude) && double.IsFinite(longitude);
    }

    private static bool TryReadPinKey(string? payload, out string pinKey)
    {
        pinKey = string.Empty;
        if (!TryParseJsonObject(payload, out var payloadJson))
        {
            return false;
        }

        if (payloadJson["key"] is not JsonValue valueNode
            || !valueNode.TryGetValue<string>(out var key)
            || string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        pinKey = key;
        return true;
    }

    private static bool TryParseJsonObject(string? payload, out JsonObject payloadJson)
    {
        payloadJson = null!;
        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        try
        {
            payloadJson = JsonNode.Parse(payload) as JsonObject ?? new JsonObject();
            return payloadJson.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private void LogMapDiagnostic(string? payload)
    {
        if (!TryParseJsonObject(payload, out var diagnosticJson))
        {
            return;
        }

        var code = TryGetString(diagnosticJson, "code") ?? "provider-diagnostic";
        var message = TryGetString(diagnosticJson, "message") ?? "Map provider diagnostic event.";
        var provider = TryGetString(diagnosticJson, "provider") ?? "unknown";
        MirageDiagnostics.Log("map.provider", $"provider={provider} node={PlatformView.NodeId} code={code} message={message}");
    }

    private static double TryGetNumber(JsonObject source, string propertyName)
    {
        if (source[propertyName] is JsonValue valueNode
            && valueNode.TryGetValue<double>(out var number))
        {
            return number;
        }

        return double.NaN;
    }

    private static string? TryGetString(JsonObject source, string propertyName)
    {
        if (source[propertyName] is JsonValue valueNode
            && valueNode.TryGetValue<string>(out var text))
        {
            return text;
        }

        return null;
    }

    private bool SetPropertyIfChanged(string name, MirageValue value)
    {
        if (PlatformView.Properties.TryGetValue(name, out var current) && current.Equals(value))
        {
            return false;
        }

        PlatformView.SetProperty(name, value);
        return true;
    }

    private bool SetStyleIfChanged(string name, string? value)
    {
        if (string.Equals(PlatformView.GetStyleValue(name), value, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetStyle(name, value);
        return true;
    }

    private bool SetEventIfChanged(string eventName, bool enabled)
    {
        var isEnabled = false;
        foreach (var existingEvent in PlatformView.Events)
        {
            if (!string.Equals(existingEvent, eventName, StringComparison.Ordinal))
            {
                continue;
            }

            isEnabled = true;
            break;
        }

        if (isEnabled == enabled)
        {
            return false;
        }

        PlatformView.SetEvent(eventName, enabled);
        return true;
    }

    private sealed class PinIdentity
    {
        public PinIdentity(string key)
        {
            Key = key;
        }

        public string Key { get; }
    }

    private sealed class MapElementIdentity
    {
        public MapElementIdentity(string key)
        {
            Key = key;
        }

        public string Key { get; }
    }
}
