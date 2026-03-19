namespace Mirage.Provider.LeafletMaps;

public enum LeafletAssetMode
{
    Cdn,
    Local
}

public sealed class LeafletMapOptions
{
    internal const string DefaultCdnJavaScriptUrl = "https://unpkg.com/leaflet@1.9.4/dist/leaflet.js";
    internal const string DefaultCdnStylesheetUrl = "https://unpkg.com/leaflet@1.9.4/dist/leaflet.css";

    public LeafletAssetMode AssetMode { get; set; } = LeafletAssetMode.Cdn;

    public string JavaScriptUrl { get; set; } = DefaultCdnJavaScriptUrl;

    public string StylesheetUrl { get; set; } = DefaultCdnStylesheetUrl;

    public int ReadinessTimeoutMs { get; set; } = 10000;

    public string TileLayerUrlTemplate { get; set; } = "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png";

    public string TileLayerAttribution { get; set; } = "&copy; OpenStreetMap contributors";

    public int TileLayerMinZoom { get; set; } = 1;

    public int TileLayerMaxZoom { get; set; } = 19;

    internal int NormalizeTimeoutMs()
    {
        return Math.Clamp(ReadinessTimeoutMs, 1000, 60000);
    }

    internal int NormalizeMinZoom()
    {
        return Math.Clamp(TileLayerMinZoom, 0, 22);
    }

    internal int NormalizeMaxZoom()
    {
        return Math.Clamp(Math.Max(TileLayerMinZoom, TileLayerMaxZoom), 0, 22);
    }

    internal bool TryResolveAssetUrls(
        out string javaScriptUrl,
        out string stylesheetUrl,
        out string? errorCode,
        out string? errorMessage)
    {
        javaScriptUrl = NormalizeUrlOrEmpty(JavaScriptUrl);
        stylesheetUrl = NormalizeUrlOrEmpty(StylesheetUrl);
        errorCode = null;
        errorMessage = null;

        if (AssetMode == LeafletAssetMode.Cdn)
        {
            javaScriptUrl = string.IsNullOrWhiteSpace(javaScriptUrl) ? DefaultCdnJavaScriptUrl : javaScriptUrl;
            stylesheetUrl = string.IsNullOrWhiteSpace(stylesheetUrl) ? DefaultCdnStylesheetUrl : stylesheetUrl;
            return true;
        }

        if (string.IsNullOrWhiteSpace(javaScriptUrl)
            || string.IsNullOrWhiteSpace(stylesheetUrl)
            || string.Equals(javaScriptUrl, DefaultCdnJavaScriptUrl, StringComparison.OrdinalIgnoreCase)
            || string.Equals(stylesheetUrl, DefaultCdnStylesheetUrl, StringComparison.OrdinalIgnoreCase))
        {
            errorCode = "local-assets-not-configured";
            errorMessage =
                "Leaflet local asset mode requires explicit local JavaScriptUrl and StylesheetUrl values. " +
                "Set local paths (for example '/leaflet/leaflet.js' and '/leaflet/leaflet.css').";
            return false;
        }

        if (IsDisallowedLocalAssetUrl(javaScriptUrl) || IsDisallowedLocalAssetUrl(stylesheetUrl))
        {
            errorCode = "local-assets-invalid-url";
            errorMessage =
                "Leaflet local asset mode only accepts local/relative asset URLs. " +
                "Do not use protocol-relative or absolute URLs when AssetMode=Local.";
            return false;
        }

        return true;
    }

    private static string NormalizeUrlOrEmpty(string? url)
    {
        return string.IsNullOrWhiteSpace(url)
            ? string.Empty
            : url.Trim();
    }

    private static bool IsDisallowedLocalAssetUrl(string url)
    {
        var normalized = NormalizeUrlOrEmpty(url);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return true;
        }

        if (normalized.StartsWith("//", StringComparison.Ordinal))
        {
            return true;
        }

        return Uri.TryCreate(normalized, UriKind.Absolute, out _);
    }
}
