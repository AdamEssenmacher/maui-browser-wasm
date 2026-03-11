using Microsoft.Maui.Controls;

namespace Mirage.Maui.Navigation;

public enum MirageNavigationTrigger
{
    Startup,
    MauiPush,
    MauiPop,
    MauiPopToRoot,
    MauiRequestNavigation,
    BrowserPopState,
    GuardRedirect,
    Manual
}

public enum MirageHistoryWriteMode
{
    Push,
    Replace,
    Suppress
}

public enum NavigationGuardAction
{
    Allow,
    Cancel,
    Redirect
}

public sealed record MirageRouteDefinition(string Template, string NormalizedTemplate, Type PageType, int Order);

public sealed record MirageRouteMatch(
    MirageRouteDefinition Route,
    string Path,
    IReadOnlyDictionary<string, string> Parameters,
    IReadOnlyDictionary<string, string> Query);

public sealed record MirageNavigationHistoryContext(
    MirageNavigationTrigger Trigger,
    string? PreviousPath,
    string NextPath,
    bool IsBackNavigation);

public sealed record MirageNavigationGuardContext(
    MirageNavigationTrigger Trigger,
    Page? CurrentPage,
    MirageRouteMatch? CurrentRoute,
    MirageRouteMatch TargetRoute);

public sealed record NavigationGuardDecision(NavigationGuardAction Action, string? RedirectPath = null)
{
    public static NavigationGuardDecision Allow() => new(NavigationGuardAction.Allow);

    public static NavigationGuardDecision Cancel() => new(NavigationGuardAction.Cancel);

    public static NavigationGuardDecision Redirect(string redirectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(redirectPath);
        return new NavigationGuardDecision(NavigationGuardAction.Redirect, redirectPath);
    }
}

public interface IRouteRegistry
{
    IReadOnlyList<MirageRouteDefinition> Routes { get; }

    MirageRouteDefinition? FallbackRoute { get; }

    bool TryGetRoute(Type pageType, out MirageRouteDefinition route);
}

public interface IRouteParser
{
    bool TryParse(string location, IRouteRegistry routeRegistry, out MirageRouteMatch match);
}

public interface IRouteSerializer
{
    bool TrySerialize(Page page, IRouteRegistry routeRegistry, out string path);
}

public interface IPageActivator
{
    Page CreatePage(MirageRouteDefinition routeDefinition, IServiceProvider services);
}

public interface IRouteParameterBinder
{
    void Bind(Page page, IReadOnlyDictionary<string, string> parameters);
}

public interface INavigationGuard
{
    ValueTask<NavigationGuardDecision> EvaluateAsync(MirageNavigationGuardContext context, CancellationToken cancellationToken = default);
}

public interface INavigationHistoryPolicy
{
    MirageHistoryWriteMode ResolveWriteMode(MirageNavigationHistoryContext context);
}

public interface INavigationTitlePolicy
{
    string ResolveTitle(Page? currentPage, string? routePath);
}
