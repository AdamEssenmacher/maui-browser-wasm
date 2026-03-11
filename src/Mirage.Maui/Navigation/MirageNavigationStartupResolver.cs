using Mirage.Web.Runtime;
using Microsoft.Maui.Controls;

namespace Mirage.Maui.Navigation;

internal interface IMirageNavigationStartupResolver
{
    Task ApplyInitialRouteAsync(Window window, IServiceProvider services);
}

internal sealed class MirageNavigationStartupResolver : IMirageNavigationStartupResolver
{
    public async Task ApplyInitialRouteAsync(Window window, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(services);

        if (window.Page is not NavigationPage navigationPage)
        {
            return;
        }

        var browserHistory = services.GetService(typeof(IMirageBrowserHistory)) as IMirageBrowserHistory;
        var routeRegistry = services.GetService(typeof(IRouteRegistry)) as IRouteRegistry;
        var routeParser = services.GetService(typeof(IRouteParser)) as IRouteParser;
        var routeSerializer = services.GetService(typeof(IRouteSerializer)) as IRouteSerializer;
        var pageActivator = services.GetService(typeof(IPageActivator)) as IPageActivator;
        var parameterBinder = services.GetService(typeof(IRouteParameterBinder)) as IRouteParameterBinder;

        if (browserHistory is null
            || routeRegistry is null
            || routeParser is null
            || pageActivator is null
            || parameterBinder is null)
        {
            return;
        }

        var location = browserHistory.GetCurrentLocation();
        if (!routeParser.TryParse(location, routeRegistry, out var match))
        {
            if (routeRegistry.FallbackRoute is null)
            {
                return;
            }

            var fallbackPage = pageActivator.CreatePage(routeRegistry.FallbackRoute, services);
            await ReplaceRootPageAsync(window, navigationPage, fallbackPage);
            browserHistory.Replace(routeRegistry.FallbackRoute.NormalizedTemplate);
            return;
        }

        var currentPage = ResolveCurrentPage(navigationPage);
        if (currentPage is not null
            && routeSerializer is not null
            && routeSerializer.TrySerialize(currentPage, routeRegistry, out var currentPath)
            && string.Equals(currentPath, match.Path, StringComparison.OrdinalIgnoreCase))
        {
            parameterBinder.Bind(currentPage, MergeParameters(match));
            return;
        }

        if (navigationPage.RootPage is null)
        {
            var page = pageActivator.CreatePage(match.Route, services);
            parameterBinder.Bind(page, MergeParameters(match));
            window.Page = new NavigationPage(page);
            browserHistory.Replace(match.Path);
            return;
        }

        var activatedPage = pageActivator.CreatePage(match.Route, services);
        parameterBinder.Bind(activatedPage, MergeParameters(match));

        await navigationPage.PushAsync(activatedPage, animated: false);
        browserHistory.Replace(match.Path);
    }

    private static IReadOnlyDictionary<string, string> MergeParameters(MirageRouteMatch routeMatch)
    {
        if (routeMatch.Query.Count == 0)
        {
            return routeMatch.Parameters;
        }

        if (routeMatch.Parameters.Count == 0)
        {
            return routeMatch.Query;
        }

        // Query values are supplemental; route-path parameters remain authoritative.
        var merged = new Dictionary<string, string>(routeMatch.Query, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in routeMatch.Parameters)
        {
            merged[key] = value;
        }

        return merged;
    }

    private static async Task ReplaceRootPageAsync(Window window, NavigationPage navigationPage, Page replacementRoot)
    {
        if (navigationPage.Navigation?.NavigationStack is { Count: > 0 } stack)
        {
            navigationPage.Navigation.InsertPageBefore(replacementRoot, stack[0]);
            await navigationPage.PopToRootAsync(animated: false);
            return;
        }

        window.Page = new NavigationPage(replacementRoot);
    }

    private static Page? ResolveCurrentPage(NavigationPage navigationPage)
    {
        if (navigationPage.Navigation?.NavigationStack is { Count: > 0 } stack)
        {
            return stack[^1];
        }

        if (navigationPage.CurrentPage is not null)
        {
            return navigationPage.CurrentPage;
        }

        return navigationPage.RootPage;
    }
}
