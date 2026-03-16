using System.Text.Json;
using System.Text.RegularExpressions;
using Mirage.Core.Diagnostics;

namespace Mirage.Maui.Images;

internal sealed class MirageManifestImageSourceResolver : IMirageImageSourceResolver
{
    private const string ManifestPath = "/_mirage/assets/images.manifest.v1.json";
    private const int SupportedManifestVersion = 1;
    private const int ManifestLoadMaxAttempts = 5;
    private const int ManifestLoadRetryDelayMilliseconds = 150;

    private readonly IMirageImageManifestProvider _manifestProvider;
    private readonly object _diagnosticSync = new();
    private readonly HashSet<string> _diagnosticKeys = new(StringComparer.Ordinal);

    private Dictionary<string, string> _imagesByIdentity = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string[]> _logicalPathAliases = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string[]> _fileNameAliases = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string[]> _fileStemAliases = new(StringComparer.OrdinalIgnoreCase);
    private Task? _initializeTask;
    private bool _degradedMode = true;
    private bool _manifestLoaded;
    private string _degradedReason = "Bundled image resolver is running in degraded mode because the manifest was unavailable.";

    public MirageManifestImageSourceResolver(IMirageImageManifestProvider manifestProvider)
    {
        _manifestProvider = manifestProvider ?? throw new ArgumentNullException(nameof(manifestProvider));
    }

    public ValueTask EnsureInitializedAsync()
    {
        var existing = Volatile.Read(ref _initializeTask);
        if (existing is not null)
        {
            if (!(_degradedMode && existing.IsCompleted))
            {
                return new ValueTask(existing);
            }

            Interlocked.CompareExchange(ref _initializeTask, null, existing);
            existing = Volatile.Read(ref _initializeTask);
            if (existing is not null)
            {
                return new ValueTask(existing);
            }
        }

        var created = InitializeCoreAsync();
        var prior = Interlocked.CompareExchange(ref _initializeTask, created, null);
        return prior is null ? new ValueTask(created) : new ValueTask(prior);
    }

    public MirageImageSourceResolveResult Resolve(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return Fail("Image source was empty.");
        }

        var trimmed = source.Trim();
        if (IsSafePassthrough(trimmed))
        {
            return Succeed(trimmed, "passthrough", trimmed);
        }

        if (!_manifestLoaded || _degradedMode)
        {
            return Fail(
                _degradedReason,
                $"degraded:{trimmed.ToLowerInvariant()}");
        }

        if (_imagesByIdentity.TryGetValue(trimmed, out var exactIdentityUrl))
        {
            return Succeed(exactIdentityUrl, "identity", trimmed);
        }

        var normalizedLookup = NormalizeLookupKey(trimmed);
        var logicalLookup = ResolveAlias(_logicalPathAliases, normalizedLookup, "logicalPath");
        if (logicalLookup.HasAliasMatch)
        {
            return logicalLookup.IsResolved
                ? Succeed(logicalLookup.BrowserUrl!, "logicalPath", normalizedLookup)
                : Fail(logicalLookup.FailureReason!);
        }

        var fileName = Path.GetFileName(normalizedLookup);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var fileNameLookup = ResolveAlias(_fileNameAliases, fileName.ToLowerInvariant(), "fileName");
            if (fileNameLookup.HasAliasMatch)
            {
                return fileNameLookup.IsResolved
                    ? Succeed(fileNameLookup.BrowserUrl!, "fileName", fileName)
                    : Fail(fileNameLookup.FailureReason!);
            }

            var fileStem = Path.GetFileNameWithoutExtension(fileName);
            if (!string.IsNullOrWhiteSpace(fileStem))
            {
                var fileStemLookup = ResolveAlias(_fileStemAliases, fileStem.ToLowerInvariant(), "fileStem");
                if (fileStemLookup.HasAliasMatch)
                {
                    return fileStemLookup.IsResolved
                        ? Succeed(fileStemLookup.BrowserUrl!, "fileStem", fileStem)
                        : Fail(fileStemLookup.FailureReason!);
                }
            }
        }

        return Fail($"Bundled image source '{trimmed}' was not found in the Mirage image manifest.");
    }

    private async Task InitializeCoreAsync()
    {
        try
        {
            var manifest = await LoadManifestWithRetryAsync();
            if (!manifest.IsSuccess || string.IsNullOrWhiteSpace(manifest.Content))
            {
                EnterDegradedMode($"manifest-load-failed:{manifest.StatusCode}",
                    $"Unable to load bundled image manifest '{ManifestPath}' (status={manifest.StatusCode}).");
                return;
            }

            if (!TryParseManifest(manifest.Content, out var payload, out var parseError))
            {
                EnterDegradedMode(
                    "manifest-json-error",
                    $"Bundled image manifest '{ManifestPath}' could not be parsed: {parseError}.");
                return;
            }

            if (payload.Version != SupportedManifestVersion)
            {
                EnterDegradedMode(
                    $"manifest-version:{payload.Version}",
                    $"Bundled image manifest version '{payload.Version}' is unsupported. Expected version '{SupportedManifestVersion}'.");
                return;
            }

            if (payload.Images.Count == 0)
            {
                _imagesByIdentity = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _logicalPathAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                _fileNameAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                _fileStemAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                _manifestLoaded = true;
                _degradedMode = false;
                ReportOnce("manifest-loaded-empty",
                    $"Loaded bundled image manifest '{ManifestPath}' with 0 images.");
                return;
            }

            _imagesByIdentity = new Dictionary<string, string>(payload.Images, StringComparer.OrdinalIgnoreCase);
            _logicalPathAliases = NormalizeAliasMap(payload.LogicalPath);
            _fileNameAliases = NormalizeAliasMap(payload.FileName);
            _fileStemAliases = NormalizeAliasMap(payload.FileStem);
            _manifestLoaded = true;
            _degradedMode = false;

            ReportOnce(
                $"manifest-loaded:{_imagesByIdentity.Count}",
                $"Loaded bundled image manifest '{ManifestPath}' with {_imagesByIdentity.Count} images, {_logicalPathAliases.Count} logical-path aliases, {_fileNameAliases.Count} file-name aliases, and {_fileStemAliases.Count} file-stem aliases.");
        }
        catch (Exception ex)
        {
            EnterDegradedMode(
                "manifest-exception",
                $"Unhandled bundled image manifest load failure: {ex.GetType().Name}.");
        }
    }

    private async Task<MirageImageManifestLoadResult> LoadManifestWithRetryAsync()
    {
        MirageImageManifestLoadResult lastResult = default;

        for (var attempt = 1; attempt <= ManifestLoadMaxAttempts; attempt++)
        {
            lastResult = await _manifestProvider.LoadAsync(ManifestPath);
            if (lastResult.IsSuccess && !string.IsNullOrWhiteSpace(lastResult.Content))
            {
                if (attempt > 1)
                {
                    ReportOnce(
                        $"manifest-load-recovered:{attempt}",
                        $"Loaded bundled image manifest '{ManifestPath}' on retry attempt {attempt}/{ManifestLoadMaxAttempts}.");
                }

                return lastResult;
            }

            var shouldRetry = attempt < ManifestLoadMaxAttempts
                && (lastResult.StatusCode == 0 || lastResult.StatusCode == 404);
            if (!shouldRetry)
            {
                return lastResult;
            }

            ReportOnce(
                $"manifest-load-retry:{attempt}:{lastResult.StatusCode}",
                $"Retrying bundled image manifest load for '{ManifestPath}' after transient status {lastResult.StatusCode} (attempt {attempt}/{ManifestLoadMaxAttempts}).");
            await Task.Delay(ManifestLoadRetryDelayMilliseconds * attempt);
        }

        return lastResult;
    }

    private MirageImageSourceResolveResult Succeed(string browserUrl, string resolutionStage, string lookupKey)
    {
        ReportOnce(
            $"resolve-success:{resolutionStage}:{lookupKey.ToLowerInvariant()}",
            $"Resolved bundled image source via {resolutionStage}: '{lookupKey}' -> '{browserUrl}'.");
        return MirageImageSourceResolveResult.Resolved(browserUrl);
    }

    private MirageImageSourceResolveResult Fail(string reason, string? diagnosticKey = null)
    {
        var normalizedDiagnosticKey = diagnosticKey ?? $"resolve-failure:{reason.ToLowerInvariant()}";
        ReportOnce(normalizedDiagnosticKey, reason);
        return MirageImageSourceResolveResult.Unresolved(reason);
    }

    private AliasResolveResult ResolveAlias(
        IReadOnlyDictionary<string, string[]> aliasMap,
        string key,
        string aliasKind)
    {
        if (string.IsNullOrWhiteSpace(key) || !aliasMap.TryGetValue(key, out var identities) || identities.Length == 0)
        {
            return AliasResolveResult.NoMatch;
        }

        if (identities.Length != 1)
        {
            var failureReason = $"Bundled image alias '{aliasKind}:{key}' is ambiguous ({string.Join(", ", identities)}).";
            return AliasResolveResult.Failed(failureReason);
        }

        var identity = identities[0];
        if (!_imagesByIdentity.TryGetValue(identity, out var browserUrl) || string.IsNullOrWhiteSpace(browserUrl))
        {
            var failureReason = $"Bundled image alias '{aliasKind}:{key}' referenced missing identity '{identity}'.";
            return AliasResolveResult.Failed(failureReason);
        }

        return AliasResolveResult.Resolved(browserUrl);
    }

    private void EnterDegradedMode(string key, string reason)
    {
        _degradedMode = true;
        _manifestLoaded = false;
        _degradedReason = string.IsNullOrWhiteSpace(reason)
            ? "Bundled image resolver is running in degraded mode because the manifest was unavailable."
            : reason;
        ReportOnce(key, reason);
    }

    private void ReportOnce(string key, string message)
    {
        lock (_diagnosticSync)
        {
            if (!_diagnosticKeys.Add(key))
            {
                return;
            }
        }

        MirageDiagnostics.Log("image.resolver", message);
    }

    private static Dictionary<string, string[]> NormalizeAliasMap(Dictionary<string, string[]>? source)
    {
        var normalized = new Dictionary<string, SortedSet<string>>(StringComparer.OrdinalIgnoreCase);
        if (source is null)
        {
            return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        }

        foreach (var pair in source)
        {
            var key = NormalizeLookupKey(pair.Key);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (!normalized.TryGetValue(key, out var values))
            {
                values = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                normalized[key] = values;
            }

            foreach (var identity in pair.Value ?? [])
            {
                if (!string.IsNullOrWhiteSpace(identity))
                {
                    values.Add(identity.Trim());
                }
            }
        }

        return normalized.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsSafePassthrough(string value)
    {
        return value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("blob:", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/", StringComparison.Ordinal);
    }

    private static string NormalizeLookupKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = StripQueryAndFragment(value.Trim()).Replace('\\', '/');
        while (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        normalized = normalized.TrimStart('/');
        normalized = Regex.Replace(normalized, "/{2,}", "/");
        return normalized.ToLowerInvariant();
    }

    private static string StripQueryAndFragment(string source)
    {
        var queryIndex = source.IndexOf('?', StringComparison.Ordinal);
        var fragmentIndex = source.IndexOf('#', StringComparison.Ordinal);
        var cutIndex = queryIndex >= 0 && fragmentIndex >= 0
            ? Math.Min(queryIndex, fragmentIndex)
            : Math.Max(queryIndex, fragmentIndex);

        return cutIndex >= 0 ? source[..cutIndex] : source;
    }

    private static bool TryParseManifest(string content, out ParsedManifest manifest, out string parseError)
    {
        manifest = default;
        parseError = string.Empty;

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                parseError = "Root JSON node was not an object.";
                return false;
            }

            if (!root.TryGetProperty("version", out var versionElement) || versionElement.ValueKind != JsonValueKind.Number || !versionElement.TryGetInt32(out var version))
            {
                parseError = "Manifest version was missing or invalid.";
                return false;
            }

            var images = ParseImageMap(root, "images");
            var logicalPath = ParseAliasMap(root, "logicalPath");
            var fileName = ParseAliasMap(root, "fileName");
            var fileStem = ParseAliasMap(root, "fileStem");
            manifest = new ParsedManifest(version, images, logicalPath, fileName, fileStem);
            return true;
        }
        catch (JsonException ex)
        {
            parseError = $"{ex.GetType().Name}: {ex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            parseError = ex.GetType().Name;
            return false;
        }
    }

    private static Dictionary<string, string> ParseImageMap(JsonElement root, string propertyName)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!root.TryGetProperty(propertyName, out var mapElement) || mapElement.ValueKind != JsonValueKind.Object)
        {
            return map;
        }

        foreach (var property in mapElement.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var value = property.Value.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                map[property.Name] = value;
            }
        }

        return map;
    }

    private static Dictionary<string, string[]> ParseAliasMap(JsonElement root, string propertyName)
    {
        var map = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (!root.TryGetProperty(propertyName, out var mapElement) || mapElement.ValueKind != JsonValueKind.Object)
        {
            return map;
        }

        foreach (var property in mapElement.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var values = new List<string>();
            foreach (var valueElement in property.Value.EnumerateArray())
            {
                if (valueElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var value = valueElement.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    values.Add(value);
                }
            }

            map[property.Name] = values.ToArray();
        }

        return map;
    }

    private readonly record struct ParsedManifest(
        int Version,
        Dictionary<string, string> Images,
        Dictionary<string, string[]> LogicalPath,
        Dictionary<string, string[]> FileName,
        Dictionary<string, string[]> FileStem);

    private readonly record struct AliasResolveResult(bool HasAliasMatch, bool IsResolved, string? BrowserUrl, string? FailureReason)
    {
        public static AliasResolveResult NoMatch => new(false, false, null, null);

        public static AliasResolveResult Resolved(string browserUrl) => new(true, true, browserUrl, null);

        public static AliasResolveResult Failed(string failureReason) => new(true, false, null, failureReason);
    }
}
