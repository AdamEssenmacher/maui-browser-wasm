using Mirage.Web.Dom;

namespace Mirage.Maui.Images;

internal sealed class DomMirageImageManifestProvider : IMirageImageManifestProvider
{
    public async Task<MirageImageManifestLoadResult> LoadAsync(string manifestPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);

        var normalizedPath = manifestPath.Trim();
        var relativePath = normalizedPath.TrimStart('/');
        var candidates = new[]
        {
            normalizedPath,
            relativePath,
            $"./{relativePath}"
        };

        var lastStatusCode = 0;
        foreach (var candidate in candidates.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.Ordinal))
        {
            var response = await DomElement.FetchTextWithStatusAsync(candidate);
            if (response.IsSuccess && !string.IsNullOrWhiteSpace(response.Text))
            {
                return new MirageImageManifestLoadResult(true, response.StatusCode, response.Text);
            }

            lastStatusCode = response.StatusCode;
            if (response.StatusCode == 0)
            {
                var text = await DomElement.FetchTextAsync(candidate);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return new MirageImageManifestLoadResult(true, 200, text);
                }
            }
        }

        return new MirageImageManifestLoadResult(false, lastStatusCode, null);
    }
}
