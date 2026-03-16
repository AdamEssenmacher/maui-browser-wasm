using System.Globalization;
using System.Text;
using Maui.Browser.Dom;
using Maui.Browser.Utils;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Maui.Browser.Handlers;

internal static class BrowserImageSourceMapper
{
    internal readonly record struct SourceProjection(string? SourceValue, string SourceKind, string InitialState, bool IsSupported);

#if DEBUG
    private static readonly HashSet<string> UnsupportedWarnings = new();
#endif

    public static SourceProjection BuildProjection(ImageSource? source, IView? ownerView = null)
    {
        if (source is null)
        {
            return new SourceProjection(null, "none", "empty", true);
        }

        if (source is UriImageSource uriImageSource)
        {
            var uri = uriImageSource.Uri?.ToString();
            if (!string.IsNullOrWhiteSpace(uri))
            {
                return new SourceProjection(uri, "uri", "loading", true);
            }

            ReportUnsupported(ownerView, source, "UriImageSource was missing a Uri.");
            return new SourceProjection(null, "unsupported", "unsupported", false);
        }

        if (source is FileImageSource fileImageSource)
        {
            var normalized = NormalizeFileSource(fileImageSource.File);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return new SourceProjection(normalized, "file", "loading", true);
            }

            ReportUnsupported(ownerView, source, "FileImageSource was missing a file path.");
            return new SourceProjection(null, "unsupported", "unsupported", false);
        }

        if (source is FontImageSource fontImageSource)
        {
            if (TryBuildFontImageDataUri(fontImageSource, out var dataUri))
            {
                return new SourceProjection(dataUri, "font", "loading", true);
            }

            ReportUnsupported(ownerView, source, "FontImageSource did not have enough information to project.");
            return new SourceProjection(null, "unsupported", "unsupported", false);
        }

        ReportUnsupported(ownerView, source, "ImageSource type is not yet supported.");
        return new SourceProjection(null, "unsupported", "unsupported", false);
    }

    public static void ApplyProjection(DomElement targetElement, SourceProjection projection)
    {
        targetElement.Attr("src", projection.SourceValue);
        targetElement.Attr("data-maui-image-source-kind", projection.SourceKind);
        targetElement.Attr("data-maui-image-state", projection.InitialState);
    }

    public static void ApplyAspect(DomElement imageElement, Aspect aspect)
    {
        var objectFit = aspect switch
        {
            Aspect.AspectFill => "cover",
            Aspect.Fill => "fill",
            Aspect.Center => "none",
            _ => "contain"
        };

        imageElement.Style("objectFit", objectFit);
        imageElement.Style("objectPosition", "center center");
    }

    private static string? NormalizeFileSource(string? file)
    {
        if (string.IsNullOrWhiteSpace(file))
        {
            return null;
        }

        var trimmed = file.Trim().Replace('\\', '/');
        if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("blob:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("/", StringComparison.Ordinal))
        {
            return trimmed;
        }

        if (trimmed.StartsWith("./", StringComparison.Ordinal))
        {
            trimmed = trimmed[2..];
        }

        return $"/{trimmed}";
    }

    private static bool TryBuildFontImageDataUri(FontImageSource fontImageSource, out string dataUri)
    {
        dataUri = string.Empty;
        if (string.IsNullOrWhiteSpace(fontImageSource.Glyph))
        {
            return false;
        }

        var glyph = EscapeXml(fontImageSource.Glyph);
        var color = CssUtil.ToCssColor(fontImageSource.Color);
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
        svg.Append(CssUtil.Number(iconSize));
        svg.Append("' height='");
        svg.Append(CssUtil.Number(iconSize));
        svg.Append("' viewBox='0 0 ");
        svg.Append(CssUtil.Number(iconSize));
        svg.Append(' ');
        svg.Append(CssUtil.Number(iconSize));
        svg.Append("'>");
        svg.Append("<text x='50%' y='50%' dominant-baseline='central' text-anchor='middle' font-family='");
        svg.Append(fontFamily);
        svg.Append("' font-size='");
        svg.Append(CssUtil.Number(size));
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

    private static void ReportUnsupported(IView? ownerView, object? value, string details)
    {
#if DEBUG
        var key = $"{ownerView?.GetType().FullName}|{value?.GetType().FullName}|{details}";
        if (UnsupportedWarnings.Add(key))
        {
            Console.WriteLine(
                $"[Maui.Browser] Unsupported image source mapping for {ownerView?.GetType().Name ?? "unknown view"}: {value?.GetType().Name ?? "<null>"}. {details}");
        }
#endif
    }
}
