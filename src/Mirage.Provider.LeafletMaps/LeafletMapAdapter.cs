using Mirage.Core.Diagnostics;
using Mirage.Core.Semantics;
using Mirage.Web.Dom;
using Mirage.Web.Maps;
using System.Text.Json.Nodes;

namespace Mirage.Provider.LeafletMaps;

internal sealed class LeafletMapAdapter : IMirageMapAdapter
{
    private readonly string _nodeId;
    private readonly DomElement _hostElement;
    private readonly LeafletMapProvider _provider;
    private bool _isInitialized;
    private bool _isDisposed;

    public LeafletMapAdapter(string nodeId, DomElement hostElement, LeafletMapProvider provider)
    {
        _nodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
        _hostElement = hostElement ?? throw new ArgumentNullException(nameof(hostElement));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public void ApplyState(MirageMapSemanticState state)
    {
        if (_isDisposed)
        {
            return;
        }

        var initialization = _provider.Initialization;
        if (!initialization.IsReady)
        {
            ShowError(
                initialization.ErrorCode ?? "provider-not-ready",
                initialization.ErrorMessage ?? "Leaflet provider is not ready.");
            return;
        }

        EnsureInitialized();
        if (!_isInitialized)
        {
            return;
        }

        var pins = new JsonArray();
        foreach (var pin in state.Pins)
        {
            pins.Add(new JsonObject
            {
                ["key"] = pin.Key,
                ["latitude"] = pin.Latitude,
                ["longitude"] = pin.Longitude,
                ["label"] = pin.Label,
                ["address"] = pin.Address,
                ["type"] = pin.Type
            });
        }

        var elements = new JsonArray();
        foreach (var element in state.Elements)
        {
            var coordinates = new JsonArray();
            foreach (var coordinate in element.Coordinates)
            {
                coordinates.Add(new JsonObject
                {
                    ["latitude"] = coordinate.Latitude,
                    ["longitude"] = coordinate.Longitude
                });
            }

            elements.Add(new JsonObject
            {
                ["key"] = element.Key,
                ["kind"] = element.Kind.ToString().ToLowerInvariant(),
                ["coordinates"] = coordinates,
                ["radiusMeters"] = element.RadiusMeters,
                ["strokeColor"] = element.StrokeColor,
                ["strokeThickness"] = element.StrokeThickness,
                ["fillColor"] = element.FillColor
            });
        }

        JsonNode? regionNode = null;
        if (state.Region is MirageMapRegion region)
        {
            regionNode = new JsonObject
            {
                ["centerLatitude"] = region.CenterLatitude,
                ["centerLongitude"] = region.CenterLongitude,
                ["latitudeSpan"] = region.LatitudeSpan,
                ["longitudeSpan"] = region.LongitudeSpan
            };
        }

        var payloadJson = new JsonObject
        {
            ["mapType"] = ToMapTypeString(state.MapType),
            ["isShowingUser"] = state.IsShowingUser,
            ["isScrollEnabled"] = state.IsScrollEnabled,
            ["isZoomEnabled"] = state.IsZoomEnabled,
            ["isTrafficEnabled"] = state.IsTrafficEnabled,
            ["regionRequestToken"] = state.RegionRequestToken,
            ["region"] = regionNode,
            ["pins"] = pins,
            ["elements"] = elements,
            ["dismissPopupKey"] = state.DismissPopupKey,
            ["dismissPopupRequestToken"] = state.DismissPopupRequestToken
        }.ToJsonString();

        var result = LeafletMapProvider.ParseInteropResult(LeafletMapInterop.ApplyMapStateJson(_nodeId, payloadJson));
        if (!result.Ok)
        {
            ShowError(
                result.ErrorCode ?? "apply-failed",
                result.ErrorMessage ?? "Leaflet map apply failed.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(result.WarningCode))
        {
            MirageDiagnostics.Log(
                "map.provider",
                $"provider=leaflet node={_nodeId} warningCode={result.WarningCode} warning={result.WarningMessage ?? string.Empty}");
        }
    }

    public void ShowError(string errorCode, string message)
    {
        if (_isDisposed)
        {
            return;
        }

        LeafletMapInterop.ShowMapError(_nodeId, errorCode, message);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        try
        {
            LeafletMapInterop.DisposeMap(_nodeId);
        }
        catch
        {
            // best effort
        }
    }

    private void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        var result = LeafletMapProvider.ParseInteropResult(LeafletMapInterop.InitializeMapJson(_nodeId, _provider.RuntimeOptionsJson));
        if (!result.Ok)
        {
            ShowError(
                result.ErrorCode ?? "initialize-failed",
                result.ErrorMessage ?? "Leaflet map initialization failed.");
            return;
        }

        _isInitialized = true;
        _hostElement.Attr("data-mirage-map-status", "ready");
    }

    private static string ToMapTypeString(MirageMapType mapType)
    {
        return mapType switch
        {
            MirageMapType.Satellite => "satellite",
            MirageMapType.Hybrid => "hybrid",
            MirageMapType.Street => "street",
            _ => "unknown"
        };
    }
}
