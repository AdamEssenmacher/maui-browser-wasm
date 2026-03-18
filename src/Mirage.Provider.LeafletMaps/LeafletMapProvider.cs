using Mirage.Core.Diagnostics;
using Mirage.Web.Dom;
using Mirage.Web.Maps;
using System.Text.Json.Nodes;

namespace Mirage.Provider.LeafletMaps;

internal sealed class LeafletMapProvider : IMirageMapProvider
{
    private readonly LeafletMapOptions _options;
    private readonly string _runtimeOptionsJson;
    private readonly object _sync = new();
    private Task<MirageMapProviderInitializationResult>? _initializationTask;
    private MirageMapProviderInitializationResult _initialization =
        MirageMapProviderInitializationResult.Failed("not-initialized", "Leaflet provider has not initialized yet.");

    public LeafletMapProvider(LeafletMapOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _runtimeOptionsJson = new JsonObject
        {
            ["tileLayerUrlTemplate"] = _options.TileLayerUrlTemplate,
            ["tileLayerAttribution"] = _options.TileLayerAttribution,
            ["tileLayerMinZoom"] = _options.NormalizeMinZoom(),
            ["tileLayerMaxZoom"] = _options.NormalizeMaxZoom()
        }.ToJsonString();
    }

    public string ProviderName => "leaflet";

    public MirageMapProviderCapabilities Capabilities { get; } = new(
        SupportsMultipleMapTypes: false,
        SupportsTrafficOverlay: false,
        SupportsUserLocation: true,
        SupportsMapElements: true);

    public MirageMapProviderInitializationResult Initialization
    {
        get
        {
            lock (_sync)
            {
                return _initialization;
            }
        }
    }

    public Task<MirageMapProviderInitializationResult> EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        lock (_sync)
        {
            _initializationTask ??= EnsureInitializedCoreAsync();
            return _initializationTask;
        }
    }

    public IMirageMapAdapter CreateAdapter(string nodeId, DomElement hostElement)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        ArgumentNullException.ThrowIfNull(hostElement);
        return new LeafletMapAdapter(nodeId, hostElement, this);
    }

    internal string RuntimeOptionsJson => _runtimeOptionsJson;

    private async Task<MirageMapProviderInitializationResult> EnsureInitializedCoreAsync()
    {
        try
        {
            if (!_options.TryResolveAssetUrls(
                    out var javaScriptUrl,
                    out var stylesheetUrl,
                    out var configurationErrorCode,
                    out var configurationErrorMessage))
            {
                var configurationFailed = MirageMapProviderInitializationResult.Failed(
                    configurationErrorCode ?? "invalid-configuration",
                    configurationErrorMessage ?? "Leaflet provider options are invalid.");
                lock (_sync)
                {
                    _initialization = configurationFailed;
                }

                MirageDiagnostics.Log(
                    "map.provider",
                    $"provider=leaflet code={configurationFailed.ErrorCode ?? "invalid-configuration"} message={configurationFailed.ErrorMessage ?? "Leaflet configuration is invalid."}");
                return configurationFailed;
            }

            await LeafletMapInterop.EnsureModuleLoadedAsync();
            var payloadJson = new JsonObject
            {
                ["assetMode"] = _options.AssetMode == LeafletAssetMode.Local ? "local" : "cdn",
                ["javaScriptUrl"] = javaScriptUrl,
                ["stylesheetUrl"] = stylesheetUrl,
                ["readinessTimeoutMs"] = _options.NormalizeTimeoutMs()
            }.ToJsonString();
            var resultJson = await LeafletMapInterop.EnsureLeafletReadyJsonAsync(payloadJson);
            var interop = ParseResult(resultJson);

            var result = interop.Ok
                ? MirageMapProviderInitializationResult.Ready()
                : MirageMapProviderInitializationResult.Failed(
                    string.IsNullOrWhiteSpace(interop.ErrorCode) ? "initialization-failed" : interop.ErrorCode,
                    string.IsNullOrWhiteSpace(interop.ErrorMessage) ? "Leaflet assets failed to initialize." : interop.ErrorMessage);

            lock (_sync)
            {
                _initialization = result;
            }

            if (!result.IsReady)
            {
                MirageDiagnostics.Log(
                    "map.provider",
                    $"provider=leaflet code={result.ErrorCode ?? "initialization-failed"} message={result.ErrorMessage ?? "Leaflet initialization failed."}");
            }

            return result;
        }
        catch (Exception error)
        {
            var failed = MirageMapProviderInitializationResult.Failed("initialization-exception", error.Message);
            lock (_sync)
            {
                _initialization = failed;
            }

            MirageDiagnostics.Log("map.provider", $"provider=leaflet code=initialization-exception message={error.Message}");
            return failed;
        }
    }

    private static LeafletInteropResult ParseResult(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return LeafletInteropResult.Failed("empty-result", "Leaflet interop returned an empty result.");
        }

        try
        {
            var parsed = JsonNode.Parse(json) as JsonObject;
            if (parsed is null)
            {
                return LeafletInteropResult.Failed("invalid-result", "Leaflet interop returned an invalid result.");
            }

            return new LeafletInteropResult(
                Ok: ReadBool(parsed, "ok"),
                ErrorCode: ReadString(parsed, "errorCode"),
                ErrorMessage: ReadString(parsed, "errorMessage"),
                WarningCode: ReadString(parsed, "warningCode"),
                WarningMessage: ReadString(parsed, "warningMessage"));
        }
        catch (Exception error)
        {
            return LeafletInteropResult.Failed("invalid-json", error.Message);
        }
    }

    internal static LeafletInteropResult ParseInteropResult(string? json) => ParseResult(json);

    private static bool ReadBool(JsonObject node, string propertyName)
    {
        if (node[propertyName] is not JsonValue value)
        {
            return false;
        }

        if (value.TryGetValue<bool>(out var direct))
        {
            return direct;
        }

        if (value.TryGetValue<string>(out var text) && bool.TryParse(text, out var parsed))
        {
            return parsed;
        }

        return false;
    }

    private static string? ReadString(JsonObject node, string propertyName)
    {
        if (node[propertyName] is not JsonValue value)
        {
            return null;
        }

        if (value.TryGetValue<string>(out var text))
        {
            return text;
        }

        return null;
    }
}

internal sealed record LeafletInteropResult(
    bool Ok,
    string? ErrorCode,
    string? ErrorMessage,
    string? WarningCode,
    string? WarningMessage)
{
    public static LeafletInteropResult Failed(string errorCode, string errorMessage) =>
        new(
            Ok: false,
            ErrorCode: errorCode,
            ErrorMessage: errorMessage,
            WarningCode: null,
            WarningMessage: null);
}
