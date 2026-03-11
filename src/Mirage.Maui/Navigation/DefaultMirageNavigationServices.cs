using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace Mirage.Maui.Navigation;

internal sealed class DefaultRouteRegistry : IRouteRegistry
{
    private readonly Dictionary<Type, MirageRouteDefinition> _byPageType;

    public DefaultRouteRegistry(MirageNavigationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        Routes = configuration.Routes
            .OrderBy(route => route.Order)
            .ToArray();

        FallbackRoute = configuration.FallbackRoute;
        _byPageType = Routes.ToDictionary(route => route.PageType);
    }

    public IReadOnlyList<MirageRouteDefinition> Routes { get; }

    public MirageRouteDefinition? FallbackRoute { get; }

    public bool TryGetRoute(Type pageType, out MirageRouteDefinition route)
    {
        ArgumentNullException.ThrowIfNull(pageType);

        if (_byPageType.TryGetValue(pageType, out var exact))
        {
            route = exact;
            return true;
        }

        foreach (var entry in _byPageType)
        {
            if (entry.Key.IsAssignableFrom(pageType))
            {
                route = entry.Value;
                return true;
            }
        }

        route = null!;
        return false;
    }
}

internal sealed class DefaultRouteParser : IRouteParser
{
    public bool TryParse(string location, IRouteRegistry routeRegistry, out MirageRouteMatch match)
    {
        ArgumentNullException.ThrowIfNull(routeRegistry);

        if (string.IsNullOrWhiteSpace(location))
        {
            location = "/";
        }

        var (normalizedPath, query) = ParseLocation(location);

        var candidates = new List<(MirageRouteDefinition Route, Dictionary<string, string> Parameters, int LiteralScore)>();
        foreach (var route in routeRegistry.Routes)
        {
            if (TryMatchRoute(route, normalizedPath, out var parameters, out var literalScore))
            {
                candidates.Add((route, parameters, literalScore));
            }
        }

        if (candidates.Count == 0)
        {
            match = null!;
            return false;
        }

        var selected = candidates
            .OrderByDescending(candidate => candidate.LiteralScore)
            .ThenBy(candidate => candidate.Route.Order)
            .First();

        match = new MirageRouteMatch(selected.Route, normalizedPath, selected.Parameters, query);
        return true;
    }

    private static bool TryMatchRoute(
        MirageRouteDefinition route,
        string normalizedPath,
        out Dictionary<string, string> parameters,
        out int literalScore)
    {
        parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        literalScore = 0;

        var pathSegments = SplitPath(normalizedPath);
        var templateSegments = SplitPath(route.NormalizedTemplate);

        if (pathSegments.Length != templateSegments.Length)
        {
            return false;
        }

        for (var index = 0; index < templateSegments.Length; index++)
        {
            var templateSegment = templateSegments[index];
            var pathSegment = pathSegments[index];

            if (TryReadParameter(templateSegment, out var parameterName))
            {
                parameters[parameterName] = Uri.UnescapeDataString(pathSegment);
                continue;
            }

            if (!string.Equals(templateSegment, pathSegment, StringComparison.OrdinalIgnoreCase))
            {
                parameters.Clear();
                return false;
            }

            literalScore++;
        }

        return true;
    }

    internal static (string NormalizedPath, Dictionary<string, string> Query) ParseLocation(string location)
    {
        var normalized = location.Trim();
        if (!normalized.StartsWith("/", StringComparison.Ordinal)
            && !normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "/" + normalized;
        }

        var isAbsoluteHttp = normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        var uri = isAbsoluteHttp
            ? new Uri(normalized, UriKind.Absolute)
            : new Uri("https://mirage.local" + normalized, UriKind.Absolute);

        var path = NormalizePath(uri.AbsolutePath);
        var query = ParseQuery(uri.Query);
        return (path, query);
    }

    internal static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var normalized = path.Trim();
        if (!normalized.StartsWith("/", StringComparison.Ordinal))
        {
            normalized = "/" + normalized;
        }

        if (normalized.Length > 1)
        {
            normalized = normalized.TrimEnd('/');
        }

        return normalized;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
        {
            return result;
        }

        var body = query[0] == '?' ? query[1..] : query;
        if (string.IsNullOrWhiteSpace(body))
        {
            return result;
        }

        foreach (var pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var equalsIndex = pair.IndexOf('=');
            if (equalsIndex <= 0)
            {
                result[Uri.UnescapeDataString(pair)] = string.Empty;
                continue;
            }

            var name = Uri.UnescapeDataString(pair[..equalsIndex]);
            var value = Uri.UnescapeDataString(pair[(equalsIndex + 1)..]);
            result[name] = value;
        }

        return result;
    }

    private static string[] SplitPath(string path)
    {
        if (string.Equals(path, "/", StringComparison.Ordinal))
        {
            return Array.Empty<string>();
        }

        return path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
    }

    private static bool TryReadParameter(string templateSegment, out string parameterName)
    {
        if (templateSegment.Length > 2
            && templateSegment[0] == '{'
            && templateSegment[^1] == '}')
        {
            parameterName = templateSegment[1..^1];
            return !string.IsNullOrWhiteSpace(parameterName);
        }

        parameterName = string.Empty;
        return false;
    }
}

internal sealed class DefaultRouteSerializer : IRouteSerializer
{
    public bool TrySerialize(Page page, IRouteRegistry routeRegistry, out string path)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(routeRegistry);

        if (!routeRegistry.TryGetRoute(page.GetType(), out var route))
        {
            path = string.Empty;
            return false;
        }

        var segments = route.NormalizedTemplate == "/"
            ? Array.Empty<string>()
            : route.NormalizedTemplate.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
        {
            path = "/";
            return true;
        }

        var properties = BuildReadablePropertyMap(page.GetType());
        var builtSegments = new string[segments.Length];

        for (var index = 0; index < segments.Length; index++)
        {
            var segment = segments[index];
            if (!TryReadParameter(segment, out var parameterName))
            {
                builtSegments[index] = segment;
                continue;
            }

            if (!properties.TryGetValue(parameterName, out var property))
            {
                path = string.Empty;
                return false;
            }

            var value = property.GetValue(page);
            if (value is null)
            {
                path = string.Empty;
                return false;
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(text))
            {
                path = string.Empty;
                return false;
            }

            builtSegments[index] = Uri.EscapeDataString(text);
        }

        path = "/" + string.Join('/', builtSegments);
        return true;
    }

    private static bool TryReadParameter(string templateSegment, out string parameterName)
    {
        if (templateSegment.Length > 2
            && templateSegment[0] == '{'
            && templateSegment[^1] == '}')
        {
            parameterName = templateSegment[1..^1];
            return !string.IsNullOrWhiteSpace(parameterName);
        }

        parameterName = string.Empty;
        return false;
    }

    private static Dictionary<string, PropertyInfo> BuildReadablePropertyMap(Type pageType)
    {
        var map = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in pageType
                     .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                     .Where(static property => property.CanRead)
                     .OrderByDescending(property => GetTypeDepth(property.DeclaringType)))
        {
            map.TryAdd(property.Name, property);
        }

        return map;
    }

    private static int GetTypeDepth(Type? type)
    {
        var depth = 0;
        while (type is not null)
        {
            depth++;
            type = type.BaseType;
        }

        return depth;
    }
}

internal sealed class DefaultPageActivator : IPageActivator
{
    public Page CreatePage(MirageRouteDefinition routeDefinition, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(routeDefinition);
        ArgumentNullException.ThrowIfNull(services);

        if (!typeof(Page).IsAssignableFrom(routeDefinition.PageType))
        {
            throw new InvalidOperationException($"Configured route page type '{routeDefinition.PageType.FullName}' does not inherit from Page.");
        }

        if (ActivatorUtilities.GetServiceOrCreateInstance(services, routeDefinition.PageType) is not Page page)
        {
            throw new InvalidOperationException($"Could not activate page type '{routeDefinition.PageType.FullName}' for route '{routeDefinition.NormalizedTemplate}'.");
        }

        return page;
    }
}

internal sealed class DefaultRouteParameterBinder : IRouteParameterBinder
{
    public void Bind(Page page, IReadOnlyDictionary<string, string> parameters)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(parameters);

        if (parameters.Count == 0)
        {
            return;
        }

        var properties = BuildWritablePropertyMap(page.GetType());

        foreach (var (name, rawValue) in parameters)
        {
            if (!properties.TryGetValue(name, out var property))
            {
                continue;
            }

            var converted = ConvertToPropertyType(rawValue, property.PropertyType, name);
            property.SetValue(page, converted);
        }
    }

    private static object? ConvertToPropertyType(string rawValue, Type targetType, string parameterName)
    {
        var nullableUnderlyingType = Nullable.GetUnderlyingType(targetType);
        var effectiveType = nullableUnderlyingType ?? targetType;

        try
        {
            if (effectiveType == typeof(string))
            {
                return rawValue;
            }

            if (effectiveType == typeof(Guid))
            {
                return Guid.Parse(rawValue);
            }

            if (effectiveType.IsEnum)
            {
                return Enum.Parse(effectiveType, rawValue, ignoreCase: true);
            }

            if (effectiveType == typeof(bool))
            {
                if (string.Equals(rawValue, "1", StringComparison.Ordinal))
                {
                    return true;
                }

                if (string.Equals(rawValue, "0", StringComparison.Ordinal))
                {
                    return false;
                }

                return bool.Parse(rawValue);
            }

            if (effectiveType == typeof(int))
            {
                return int.Parse(rawValue, CultureInfo.InvariantCulture);
            }

            if (effectiveType == typeof(long))
            {
                return long.Parse(rawValue, CultureInfo.InvariantCulture);
            }

            if (effectiveType == typeof(short))
            {
                return short.Parse(rawValue, CultureInfo.InvariantCulture);
            }

            if (effectiveType == typeof(byte))
            {
                return byte.Parse(rawValue, CultureInfo.InvariantCulture);
            }

            if (effectiveType == typeof(float))
            {
                return float.Parse(rawValue, CultureInfo.InvariantCulture);
            }

            if (effectiveType == typeof(double))
            {
                return double.Parse(rawValue, CultureInfo.InvariantCulture);
            }

            if (effectiveType == typeof(decimal))
            {
                return decimal.Parse(rawValue, CultureInfo.InvariantCulture);
            }

            throw new InvalidOperationException($"Mirage route parameter '{parameterName}' cannot bind to unsupported property type '{targetType.FullName}'.");
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
        {
            throw new InvalidOperationException(
                $"Mirage route parameter '{parameterName}' with value '{rawValue}' could not bind to property type '{targetType.FullName}'.",
                ex);
        }
    }

    private static Dictionary<string, PropertyInfo> BuildWritablePropertyMap(Type pageType)
    {
        var map = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in pageType
                     .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                     .Where(static property => property.CanWrite)
                     .OrderByDescending(property => GetTypeDepth(property.DeclaringType)))
        {
            map.TryAdd(property.Name, property);
        }

        return map;
    }

    private static int GetTypeDepth(Type? type)
    {
        var depth = 0;
        while (type is not null)
        {
            depth++;
            type = type.BaseType;
        }

        return depth;
    }
}

internal sealed class AllowAllNavigationGuard : INavigationGuard
{
    public ValueTask<NavigationGuardDecision> EvaluateAsync(MirageNavigationGuardContext context, CancellationToken cancellationToken = default)
    {
        _ = context;
        _ = cancellationToken;
        return ValueTask.FromResult(NavigationGuardDecision.Allow());
    }
}

internal sealed class DefaultNavigationHistoryPolicy : INavigationHistoryPolicy
{
    private readonly MirageHistoryWriteMode _defaultMode;

    public DefaultNavigationHistoryPolicy(MirageNavigationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _defaultMode = configuration.DefaultHistoryMode;
    }

    public MirageHistoryWriteMode ResolveWriteMode(MirageNavigationHistoryContext context)
    {
        return context.Trigger switch
        {
            MirageNavigationTrigger.MauiPush => MirageHistoryWriteMode.Push,
            MirageNavigationTrigger.MauiRequestNavigation => MirageHistoryWriteMode.Push,
            MirageNavigationTrigger.MauiPop => MirageHistoryWriteMode.Replace,
            MirageNavigationTrigger.MauiPopToRoot => MirageHistoryWriteMode.Replace,
            MirageNavigationTrigger.BrowserPopState => MirageHistoryWriteMode.Replace,
            MirageNavigationTrigger.Startup => MirageHistoryWriteMode.Replace,
            MirageNavigationTrigger.GuardRedirect => MirageHistoryWriteMode.Replace,
            MirageNavigationTrigger.Manual => MirageHistoryWriteMode.Replace,
            _ => _defaultMode
        };
    }
}

internal sealed class DefaultNavigationTitlePolicy : INavigationTitlePolicy
{
    private readonly string? _defaultTitle;

    public DefaultNavigationTitlePolicy(MirageNavigationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _defaultTitle = string.IsNullOrWhiteSpace(configuration.DefaultTitle)
            ? "Mirage"
            : configuration.DefaultTitle;
    }

    public string ResolveTitle(Page? currentPage, string? routePath)
    {
        _ = routePath;
        if (!string.IsNullOrWhiteSpace(currentPage?.Title))
        {
            return currentPage.Title;
        }

        return _defaultTitle ?? "Mirage";
    }
}
