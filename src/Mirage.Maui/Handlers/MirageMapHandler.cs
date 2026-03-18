using Mirage.Core.Semantics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Handlers;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using MauiIMap = Microsoft.Maui.Maps.IMap;
using MauiMap = Microsoft.Maui.Controls.Maps.Map;

namespace Mirage.Maui.Handlers;

internal sealed class MirageMapHandler : MirageViewHandler<MauiMap>
{
    private readonly ConditionalWeakTable<Pin, PinIdentity> _pinIdentityByPin = new();
    private readonly HashSet<Pin> _subscribedPins = new();
    private INotifyCollectionChanged? _pinsCollection;
    private long _nextPinIdentity = 1;
    private long _regionRequestToken;

    public static IPropertyMapper<MauiMap, MirageMapHandler> Mapper =
        new PropertyMapper<MauiMap, MirageMapHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(MauiIMap.MapType)] = MapMapType,
            [nameof(MauiIMap.VisibleRegion)] = MapVisibleRegion,
            [nameof(MauiIMap.Pins)] = MapPins
        };

    public static CommandMapper<MauiMap, MirageMapHandler> CommandMapper =
        new(MirageViewMapper.CommonViewCommandMapper)
        {
            [nameof(MauiIMap.MoveToRegion)] = MapMoveToRegion
        };

    public MirageMapHandler()
        : base(ComponentKind.Map, Mapper, CommandMapper)
    {
        DefaultDisplayStyle = "block";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        HookPinsCollection(VirtualView);
        SyncPinSubscriptions(VirtualView);

        var changed = SetStyleIfChanged("display", "block");
        changed |= SetStyleIfChanged("width", "100%");
        changed |= SetStyleIfChanged("height", "100%");
        changed |= SetStyleIfChanged("minHeight", "180px");
        changed |= SetStyleIfChanged("overflow", "hidden");
        changed |= SetStyleIfChanged("position", "relative");
        changed |= UpdateMapTypeProjection(VirtualView);
        changed |= UpdatePinsProjection(VirtualView);
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
        UnhookPinPropertySubscriptions();
        base.DisconnectHandler(platformView);
    }

    public static void MapMapType(MirageMapHandler handler, MauiMap map)
    {
        if (!handler.UpdateMapTypeProjection(map))
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
        foreach (var pin in map.Pins)
        {
            if (pin is not Pin concretePin)
            {
                continue;
            }

            var location = concretePin.Location;
            if (location is null
                || !double.IsFinite(location.Latitude)
                || !double.IsFinite(location.Longitude))
            {
                continue;
            }

            var pinPayload = new JsonObject
            {
                ["key"] = GetOrCreatePinKey(concretePin),
                ["latitude"] = location.Latitude,
                ["longitude"] = location.Longitude,
                ["label"] = concretePin.Label,
                ["address"] = concretePin.Address,
                ["type"] = concretePin.Type.ToString()
            };
            pinsPayload.Add(pinPayload);
        }

        return SetPropertyIfChanged(MirageMapPropertyNames.Pins, MirageValue.Json(pinsPayload));
    }

    private string GetOrCreatePinKey(Pin pin)
    {
        if (_pinIdentityByPin.TryGetValue(pin, out var identity))
        {
            return identity.Key;
        }

        identity = new PinIdentity($"pin-{_nextPinIdentity++:D6}");
        _pinIdentityByPin.Add(pin, identity);
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

    private void UnhookPinsCollection()
    {
        if (_pinsCollection is null)
        {
            return;
        }

        _pinsCollection.CollectionChanged -= OnPinsCollectionChanged;
        _pinsCollection = null;
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

    private void OnPinPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = e;
        if (sender is not Pin)
        {
            return;
        }

        if (!UpdatePinsProjection(VirtualView))
        {
            return;
        }

        RequestRender();
    }

    private void SyncPinSubscriptions(MauiMap map)
    {
        var activePins = new HashSet<Pin>();
        foreach (var candidate in map.Pins)
        {
            if (candidate is Pin concretePin)
            {
                activePins.Add(concretePin);
            }
        }

        if (_subscribedPins.Count > 0)
        {
            var removedPins = new List<Pin>();
            foreach (var pin in _subscribedPins)
            {
                if (!activePins.Contains(pin))
                {
                    removedPins.Add(pin);
                }
            }

            foreach (var pin in removedPins)
            {
                pin.PropertyChanged -= OnPinPropertyChanged;
                _subscribedPins.Remove(pin);
            }
        }

        foreach (var pin in activePins)
        {
            if (_subscribedPins.Add(pin))
            {
                pin.PropertyChanged += OnPinPropertyChanged;
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
            pin.PropertyChanged -= OnPinPropertyChanged;
        }

        _subscribedPins.Clear();
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

    private sealed class PinIdentity
    {
        public PinIdentity(string key)
        {
            Key = key;
        }

        public string Key { get; }
    }
}
