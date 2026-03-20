using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Mirage.Core.Diagnostics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Mirage.Maui.Images;

internal sealed class MirageManifestImageSourceResolver : IMirageImageSourceResolver
{
    private const string ManifestPath = "/_mirage/assets/images.manifest.v1.json";
    private const int SupportedManifestVersion = 1;
    private const int ManifestLoadMaxAttempts = 5;
    private const int ManifestLoadRetryDelayMilliseconds = 150;
    private const int MaxDiagnosticKeyCount = 256;

    private readonly IMirageImageManifestProvider _manifestProvider;
    private readonly IMirageObjectUrlFactory? _objectUrlFactory;
    private readonly object _diagnosticSync = new();
    private readonly HashSet<string> _diagnosticKeys = new(StringComparer.Ordinal);
    private readonly object _objectUrlSync = new();
    private readonly HashSet<string> _ownedObjectUrls = new(StringComparer.Ordinal);

    private Dictionary<string, string> _imagesByIdentity = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string[]> _logicalPathAliases = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string[]> _fileNameAliases = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string[]> _fileStemAliases = new(StringComparer.OrdinalIgnoreCase);
    private Task? _initializeTask;
    private bool _degradedMode = true;
    private bool _manifestLoaded;
    private string _degradedReason = "Bundled image resolver is running in degraded mode because the manifest was unavailable.";

    public MirageManifestImageSourceResolver(
        IMirageImageManifestProvider manifestProvider,
        IMirageObjectUrlFactory? objectUrlFactory = null)
    {
        _manifestProvider = manifestProvider ?? throw new ArgumentNullException(nameof(manifestProvider));
        _objectUrlFactory = objectUrlFactory;
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
            return Fail("Image source was empty.", "empty-source");
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
                "degraded");
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
                : Fail(logicalLookup.FailureReason!, "alias-logicalpath");
        }

        var fileName = Path.GetFileName(normalizedLookup);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var fileNameLookup = ResolveAlias(_fileNameAliases, fileName.ToLowerInvariant(), "fileName");
            if (fileNameLookup.HasAliasMatch)
            {
                return fileNameLookup.IsResolved
                    ? Succeed(fileNameLookup.BrowserUrl!, "fileName", fileName)
                    : Fail(fileNameLookup.FailureReason!, "alias-filename");
            }

            var fileStem = Path.GetFileNameWithoutExtension(fileName);
            if (!string.IsNullOrWhiteSpace(fileStem))
            {
                var fileStemLookup = ResolveAlias(_fileStemAliases, fileStem.ToLowerInvariant(), "fileStem");
                if (fileStemLookup.HasAliasMatch)
                {
                    return fileStemLookup.IsResolved
                        ? Succeed(fileStemLookup.BrowserUrl!, "fileStem", fileStem)
                        : Fail(fileStemLookup.FailureReason!, "alias-filestem");
                }
            }
        }

        return Fail($"Bundled image source '{trimmed}' was not found in the Mirage image manifest.", "not-found");
    }

    public MirageImageSourceProjectionResult Resolve(ImageSource? source)
    {
        if (source is null)
        {
            return MirageImageSourceProjectionResult.Empty();
        }

        if (source is UriImageSource uriImageSource)
        {
            return ResolveUriImageSource(uriImageSource);
        }

        if (source is FileImageSource fileImageSource)
        {
            return ResolveFileImageSource(fileImageSource);
        }

        if (source is FontImageSource fontImageSource)
        {
            return ResolveFontImageSource(fontImageSource);
        }

        if (source is StreamImageSource)
        {
            return MirageImageSourceProjectionResult.Unresolved(
                "stream",
                "StreamImageSource requires async resolver execution.");
        }

        return MirageImageSourceProjectionResult.Unresolved(
            "unsupported",
            $"Image source kind '{source.GetType().Name}' is not supported.");
    }

    public async ValueTask<MirageImageSourceProjectionResult> ResolveAsync(
        ImageSource? source,
        CancellationToken cancellationToken = default)
    {
        if (source is StreamImageSource streamImageSource)
        {
            return await ResolveStreamImageSourceAsync(streamImageSource, cancellationToken);
        }

        return Resolve(source);
    }

    public void Release(string? browserUrl)
    {
        if (string.IsNullOrWhiteSpace(browserUrl))
        {
            return;
        }

        var trimmed = browserUrl.Trim();
        var shouldRelease = false;
        lock (_objectUrlSync)
        {
            if (_ownedObjectUrls.Remove(trimmed))
            {
                shouldRelease = true;
            }
        }

        if (!shouldRelease)
        {
            return;
        }

        if (_objectUrlFactory is null)
        {
            ReportOnce(
                "stream-release-without-factory",
                "Attempted to release a stream blob URL without an object URL factory.");
            return;
        }

        try
        {
            _objectUrlFactory.RevokeObjectUrl(trimmed);
            ReportOnce(
                "stream-release-success",
                "Released stream image blob URL.");
        }
        catch (Exception ex)
        {
            ReportOnce(
                $"stream-release-failed:{ex.GetType().Name}",
                $"Failed to release a stream image blob URL: {ex.GetType().Name}.");
        }
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
            $"resolve-success:{resolutionStage}",
            $"Resolved bundled image source via {resolutionStage}: '{lookupKey}' -> '{browserUrl}'.");
        return MirageImageSourceResolveResult.Resolved(browserUrl);
    }

    private MirageImageSourceResolveResult Fail(string reason, string? errorCode = null)
    {
        var normalizedDiagnosticKey = string.IsNullOrWhiteSpace(errorCode)
            ? "resolve-failure:generic"
            : $"resolve-failure:{errorCode}";
        ReportOnce(normalizedDiagnosticKey, reason);
        return MirageImageSourceResolveResult.Unresolved(reason);
    }

    private MirageImageSourceProjectionResult ResolveUriImageSource(UriImageSource source)
    {
        var uriValue = source.Uri?.OriginalString;
        if (string.IsNullOrWhiteSpace(uriValue))
        {
            var fallbackValue = source.ToString();
            if (!string.IsNullOrWhiteSpace(fallbackValue)
                && !string.Equals(fallbackValue, source.GetType().Name, StringComparison.Ordinal)
                && !string.Equals(fallbackValue, source.GetType().FullName, StringComparison.Ordinal))
            {
                uriValue = fallbackValue;
            }
        }

        if (string.IsNullOrWhiteSpace(uriValue))
        {
            return MirageImageSourceProjectionResult.Unresolved("uri", "UriImageSource was missing a Uri value.");
        }

        var trimmed = uriValue.Trim();
        if (!IsAllowedUriImageSource(trimmed))
        {
            return MirageImageSourceProjectionResult.Unresolved(
                "uri",
                $"UriImageSource value is not supported for browser passthrough: '{trimmed}'.");
        }

        return MirageImageSourceProjectionResult.Resolved(
            browserUrl: trimmed,
            sourceKind: "uri",
            isGifSource: IsGifPath(trimmed));
    }

    private MirageImageSourceProjectionResult ResolveFileImageSource(FileImageSource source)
    {
        var fileValue = source.File;
        var resolved = Resolve(fileValue);
        if (!resolved.IsResolved || string.IsNullOrWhiteSpace(resolved.BrowserUrl))
        {
            var reason = string.IsNullOrWhiteSpace(resolved.FailureReason)
                ? "FileImageSource could not be resolved via bundled-image manifest lookup."
                : resolved.FailureReason;
            return MirageImageSourceProjectionResult.Unresolved("file", reason);
        }

        return MirageImageSourceProjectionResult.Resolved(
            browserUrl: resolved.BrowserUrl,
            sourceKind: "file",
            isGifSource: IsGifPath(resolved.BrowserUrl));
    }

    private MirageImageSourceProjectionResult ResolveFontImageSource(FontImageSource source)
    {
        if (!TryBuildFontImageDataUri(source, out var dataUri, out var reason))
        {
            return MirageImageSourceProjectionResult.Unresolved("font", reason);
        }

        return MirageImageSourceProjectionResult.Resolved(dataUri, "font");
    }

    private async ValueTask<MirageImageSourceProjectionResult> ResolveStreamImageSourceAsync(
        StreamImageSource source,
        CancellationToken cancellationToken)
    {
        if (_objectUrlFactory is null)
        {
            return MirageImageSourceProjectionResult.Unresolved(
                "stream",
                "StreamImageSource is unavailable because IMirageObjectUrlFactory was not registered.");
        }

        if (source.Stream is null)
        {
            return MirageImageSourceProjectionResult.Unresolved("stream", "StreamImageSource did not provide a stream factory.");
        }

        try
        {
            await using var stream = await source.Stream(cancellationToken);
            if (stream is null)
            {
                return MirageImageSourceProjectionResult.Unresolved("stream", "StreamImageSource produced a null stream.");
            }

            await using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            var bytes = buffer.ToArray();
            if (bytes.Length == 0)
            {
                return MirageImageSourceProjectionResult.Unresolved("stream", "StreamImageSource produced an empty stream.");
            }

            var contentType = InferStreamContentType(bytes);
            var objectUrl = _objectUrlFactory.CreateObjectUrl(bytes, contentType);
            if (string.IsNullOrWhiteSpace(objectUrl))
            {
                return MirageImageSourceProjectionResult.Unresolved("stream", "StreamImageSource blob URL creation failed.");
            }

            lock (_objectUrlSync)
            {
                _ownedObjectUrls.Add(objectUrl);
            }

            ReportOnce(
                "stream-created-success",
                "Created stream image blob URL.");

            return MirageImageSourceProjectionResult.Resolved(
                browserUrl: objectUrl,
                sourceKind: "stream",
                isManagedObjectUrl: true,
                isGifSource: contentType.Equals("image/gif", StringComparison.OrdinalIgnoreCase));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return MirageImageSourceProjectionResult.Unresolved(
                "stream",
                $"StreamImageSource conversion failed: {ex.GetType().Name}.");
        }
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
            if (_diagnosticKeys.Count >= MaxDiagnosticKeyCount)
            {
                _diagnosticKeys.Clear();
            }

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
        return IsAllowedDirectUri(value) || value.StartsWith("/", StringComparison.Ordinal);
    }

    private static bool IsAllowedDirectUri(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        return trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("blob:", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("/", StringComparison.Ordinal);
    }

    private static bool IsAllowedUriImageSource(string value)
    {
        return IsAllowedDirectUri(value) || IsSafeRelativeUri(value);
    }

    private static bool IsSafeRelativeUri(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.StartsWith("//", StringComparison.Ordinal)
            || trimmed.StartsWith("\\\\", StringComparison.Ordinal))
        {
            return false;
        }

        if (trimmed.Contains('\0')
            || trimmed.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("vbscript:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (HasUriScheme(trimmed))
        {
            return false;
        }

        return true;
    }

    private static bool HasUriScheme(string value)
    {
        var colonIndex = value.IndexOf(':', StringComparison.Ordinal);
        if (colonIndex <= 0)
        {
            return false;
        }

        for (var i = 0; i < colonIndex; i++)
        {
            var c = value[i];
            var isValid = char.IsLetterOrDigit(c) || c is '+' or '-' or '.';
            if (!isValid)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsGifPath(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        var normalized = StripQueryAndFragment(source.Trim());
        return normalized.EndsWith(".gif", StringComparison.OrdinalIgnoreCase);
    }

    private static string InferStreamContentType(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 8
            && bytes[0] == 0x89
            && bytes[1] == 0x50
            && bytes[2] == 0x4E
            && bytes[3] == 0x47
            && bytes[4] == 0x0D
            && bytes[5] == 0x0A
            && bytes[6] == 0x1A
            && bytes[7] == 0x0A)
        {
            return "image/png";
        }

        if (bytes.Length >= 3
            && bytes[0] == 0xFF
            && bytes[1] == 0xD8
            && bytes[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (bytes.Length >= 6)
        {
            var header = Encoding.ASCII.GetString(bytes[..6]);
            if (string.Equals(header, "GIF87a", StringComparison.Ordinal)
                || string.Equals(header, "GIF89a", StringComparison.Ordinal))
            {
                return "image/gif";
            }
        }

        if (bytes.Length >= 4
            && bytes[0] == 0x52
            && bytes[1] == 0x49
            && bytes[2] == 0x46
            && bytes[3] == 0x46)
        {
            return "image/webp";
        }

        var sniffLength = Math.Min(bytes.Length, 256);
        var prefix = Encoding.UTF8.GetString(bytes[..sniffLength]);
        if (prefix.Contains("<svg", StringComparison.OrdinalIgnoreCase))
        {
            return "image/svg+xml";
        }

        return "application/octet-stream";
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

    private static bool TryBuildFontImageDataUri(FontImageSource fontImageSource, out string dataUri, out string failureReason)
    {
        dataUri = string.Empty;
        failureReason = string.Empty;

        if (string.IsNullOrWhiteSpace(fontImageSource.Glyph))
        {
            failureReason = "FontImageSource did not specify a glyph.";
            return false;
        }

        var glyph = EscapeXml(fontImageSource.Glyph);
        var color = ToCssColor(fontImageSource.Color);
        if (string.IsNullOrWhiteSpace(color))
        {
            color = "#000000";
        }

        var fontFamily = string.IsNullOrWhiteSpace(fontImageSource.FontFamily)
            ? "sans-serif"
            : EscapeXml(fontImageSource.FontFamily);

        var size = fontImageSource.Size;
        if (!double.IsFinite(size) || size <= 0)
        {
            size = 24;
        }

        var iconSize = Math.Max(8, size * 1.6);
        var svg = new StringBuilder();
        svg.Append("<svg xmlns='http://www.w3.org/2000/svg' width='");
        svg.Append(iconSize.ToString("0.###", CultureInfo.InvariantCulture));
        svg.Append("' height='");
        svg.Append(iconSize.ToString("0.###", CultureInfo.InvariantCulture));
        svg.Append("' viewBox='0 0 ");
        svg.Append(iconSize.ToString("0.###", CultureInfo.InvariantCulture));
        svg.Append(' ');
        svg.Append(iconSize.ToString("0.###", CultureInfo.InvariantCulture));
        svg.Append("'>");
        svg.Append("<text x='50%' y='50%' dominant-baseline='central' text-anchor='middle' font-family='");
        svg.Append(fontFamily);
        svg.Append("' font-size='");
        svg.Append(size.ToString("0.###", CultureInfo.InvariantCulture));
        svg.Append("' fill='");
        svg.Append(color);
        svg.Append("'>");
        svg.Append(glyph);
        svg.Append("</text></svg>");

        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(svg.ToString()));
        dataUri = $"data:image/svg+xml;base64,{encoded}";
        return true;
    }

    private static string EscapeXml(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&apos;", StringComparison.Ordinal);
    }

    private static string? ToCssColor(Color? color)
    {
        if (color is null)
        {
            return null;
        }

        var red = (int)Math.Round(Math.Clamp(color.Red, 0, 1) * 255, MidpointRounding.AwayFromZero);
        var green = (int)Math.Round(Math.Clamp(color.Green, 0, 1) * 255, MidpointRounding.AwayFromZero);
        var blue = (int)Math.Round(Math.Clamp(color.Blue, 0, 1) * 255, MidpointRounding.AwayFromZero);
        var alpha = Math.Clamp(color.Alpha, 0, 1);

        if (alpha >= 0.999)
        {
            return string.Create(CultureInfo.InvariantCulture, $"rgb({red},{green},{blue})");
        }

        return string.Create(CultureInfo.InvariantCulture, $"rgba({red},{green},{blue},{alpha:0.###})");
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
