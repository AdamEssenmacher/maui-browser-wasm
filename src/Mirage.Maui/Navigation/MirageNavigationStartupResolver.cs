using Mirage.Web.Runtime;
using Microsoft.Extensions.DependencyInjection;
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
        var guards = services.GetServices<INavigationGuard>().ToArray();

        if (browserHistory is null
            || routeRegistry is null
            || routeParser is null
            || pageActivator is null
            || parameterBinder is null)
        {
            return;
        }

        var location = browserHistory.GetCurrentLocation();
        if (!TryResolveTargetMatch(location, routeRegistry, routeParser, out var match))
        {
            return;
        }

        var currentPage = ResolveCurrentPage(navigationPage);
        var currentRoute = ResolveCurrentRoute(currentPage, routeRegistry, routeParser, routeSerializer);
        var guardDecision = await EvaluateGuardsAsync(guards, currentPage, currentRoute, match);
        if (guardDecision.Action == NavigationGuardAction.Cancel)
        {
            if (currentRoute is not null)
            {
                browserHistory.Replace(currentRoute.Path);
                return;
            }

            if (routeRegistry.FallbackRoute is null)
            {
                return;
            }

            var fallbackPage = pageActivator.CreatePage(routeRegistry.FallbackRoute, services);
            await ReplaceRootPageAsync(window, navigationPage, fallbackPage);
            browserHistory.Replace(routeRegistry.FallbackRoute.NormalizedTemplate);
            return;
        }

        if (guardDecision.Action == NavigationGuardAction.Redirect)
        {
            if (string.IsNullOrWhiteSpace(guardDecision.RedirectPath)
                || !TryResolveTargetMatch(guardDecision.RedirectPath, routeRegistry, routeParser, out var redirected))
            {
                if (currentRoute is not null)
                {
                    browserHistory.Replace(currentRoute.Path);
                }

                return;
            }

            match = redirected;
        }

        if (currentPage is not null
            && routeSerializer is not null
            && routeSerializer.TrySerialize(currentPage, routeRegistry, out var currentPath)
            && string.Equals(currentPath, match.Path, StringComparison.OrdinalIgnoreCase))
        {
            parameterBinder.Bind(currentPage, MergeParameters(match));
            browserHistory.Replace(match.Path);
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

    private static bool TryResolveTargetMatch(
        string location,
        IRouteRegistry routeRegistry,
        IRouteParser routeParser,
        out MirageRouteMatch match)
    {
        if (routeParser.TryParse(location, routeRegistry, out match))
        {
            return true;
        }

        var fallbackRoute = routeRegistry.FallbackRoute;
        if (fallbackRoute is null)
        {
            match = null!;
            return false;
        }

        if (routeParser.TryParse(fallbackRoute.NormalizedTemplate, routeRegistry, out var fallbackMatch))
        {
            match = fallbackMatch;
            return true;
        }

        match = new MirageRouteMatch(
            fallbackRoute,
            fallbackRoute.NormalizedTemplate,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        return true;
    }

    private static MirageRouteMatch? ResolveCurrentRoute(
        Page? currentPage,
        IRouteRegistry routeRegistry,
        IRouteParser routeParser,
        IRouteSerializer? routeSerializer)
    {
        if (currentPage is null
            || routeSerializer is null
            || !routeSerializer.TrySerialize(currentPage, routeRegistry, out var currentPath)
            || !routeParser.TryParse(currentPath, routeRegistry, out var currentMatch))
        {
            return null;
        }

        return currentMatch;
    }

    private static async ValueTask<NavigationGuardDecision> EvaluateGuardsAsync(
        IReadOnlyList<INavigationGuard> guards,
        Page? currentPage,
        MirageRouteMatch? currentRoute,
        MirageRouteMatch targetRoute)
    {
        if (guards.Count == 0)
        {
            return NavigationGuardDecision.Allow();
        }

        var context = new MirageNavigationGuardContext(
            MirageNavigationTrigger.Startup,
            currentPage,
            currentRoute,
            targetRoute);

        foreach (var guard in guards)
        {
            var decision = await guard.EvaluateAsync(context);
            if (decision.Action != NavigationGuardAction.Allow)
            {
                return decision;
            }
        }

        return NavigationGuardDecision.Allow();
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
