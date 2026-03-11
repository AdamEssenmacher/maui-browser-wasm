using Microsoft.Maui.Controls;

namespace Mirage.Maui.Navigation;

public sealed class MirageNavigationBuilder
{
    private readonly List<RouteMapping> _mappings = [];
    private Type? _fallbackPageType;
    private string? _defaultTitle;
    private MirageHistoryWriteMode _defaultHistoryMode = MirageHistoryWriteMode.Replace;

    public MirageNavigationBuilder Map<TPage>(string template)
        where TPage : Page
    {
        var normalizedTemplate = NormalizeTemplate(template);

        if (_mappings.Any(existing => existing.PageType == typeof(TPage)))
        {
            throw new InvalidOperationException($"Navigation route is already mapped for page type '{typeof(TPage).FullName}'.");
        }

        if (_mappings.Any(existing => string.Equals(existing.NormalizedTemplate, normalizedTemplate, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Navigation route template '{normalizedTemplate}' is already mapped.");
        }

        _mappings.Add(new RouteMapping(typeof(TPage), template, normalizedTemplate));
        return this;
    }

    public MirageNavigationBuilder SetFallback<TPage>()
        where TPage : Page
    {
        _fallbackPageType = typeof(TPage);
        return this;
    }

    public MirageNavigationBuilder SetDefaultTitle(string title)
    {
        _defaultTitle = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        return this;
    }

    public MirageNavigationBuilder SetDefaultHistoryMode(MirageHistoryWriteMode writeMode)
    {
        _defaultHistoryMode = writeMode;
        return this;
    }

    internal MirageNavigationConfiguration Build()
    {
        var routes = new List<MirageRouteDefinition>(_mappings.Count);
        for (var index = 0; index < _mappings.Count; index++)
        {
            var mapping = _mappings[index];
            routes.Add(new MirageRouteDefinition(mapping.Template, mapping.NormalizedTemplate, mapping.PageType, index));
        }

        MirageRouteDefinition? fallbackRoute = null;
        if (_fallbackPageType is not null)
        {
            fallbackRoute = routes.FirstOrDefault(route => route.PageType == _fallbackPageType)
                ?? throw new InvalidOperationException($"Fallback page type '{_fallbackPageType.FullName}' is not mapped in Mirage navigation routes.");

            if (ContainsRouteParameters(fallbackRoute.NormalizedTemplate))
            {
                throw new InvalidOperationException(
                    $"Fallback route '{fallbackRoute.NormalizedTemplate}' must not contain route parameters.");
            }
        }

        return new MirageNavigationConfiguration(routes, fallbackRoute, _defaultTitle, _defaultHistoryMode);
    }

    internal static string NormalizeTemplate(string template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            throw new ArgumentException("Route template must not be empty.", nameof(template));
        }

        var trimmed = template.Trim();
        if (!trimmed.StartsWith("/", StringComparison.Ordinal))
        {
            trimmed = "/" + trimmed;
        }

        if (trimmed.Length > 1)
        {
            trimmed = trimmed.TrimEnd('/');
        }

        return trimmed;
    }

    private static bool ContainsRouteParameters(string normalizedTemplate)
    {
        if (string.IsNullOrWhiteSpace(normalizedTemplate) || string.Equals(normalizedTemplate, "/", StringComparison.Ordinal))
        {
            return false;
        }

        var segments = normalizedTemplate.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (segment.Length > 2 && segment[0] == '{' && segment[^1] == '}')
            {
                return true;
            }
        }

        return false;
    }

    private sealed record RouteMapping(Type PageType, string Template, string NormalizedTemplate);
}

internal sealed record MirageNavigationConfiguration(
    IReadOnlyList<MirageRouteDefinition> Routes,
    MirageRouteDefinition? FallbackRoute,
    string? DefaultTitle,
    MirageHistoryWriteMode DefaultHistoryMode)
{
    public static MirageNavigationConfiguration Empty { get; } = new(
        Array.Empty<MirageRouteDefinition>(),
        FallbackRoute: null,
        DefaultTitle: null,
        DefaultHistoryMode: MirageHistoryWriteMode.Replace);
}
