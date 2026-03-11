using Mirage.Maui.Navigation;
using Mirage.Web.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class NavigationProjectionTests
{
    [Fact]
    public void NavigationBuilder_RejectsDuplicateMappings()
    {
        var duplicateTemplateBuilder = new MirageNavigationBuilder();
        duplicateTemplateBuilder.Map<TestHomePage>("/");
        var duplicateTemplate = Assert.Throws<InvalidOperationException>(() => duplicateTemplateBuilder.Map<TestSettingsPage>("/"));
        Assert.Contains("already mapped", duplicateTemplate.Message, StringComparison.OrdinalIgnoreCase);

        var duplicateTypeBuilder = new MirageNavigationBuilder();
        duplicateTypeBuilder.Map<TestProjectPage>("/projects/{id}");
        var duplicateType = Assert.Throws<InvalidOperationException>(() => duplicateTypeBuilder.Map<TestProjectPage>("/projects/{value}"));
        Assert.Contains("already mapped", duplicateType.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NavigationBuilder_RejectsParameterizedFallbackRoute()
    {
        var builder = new MirageNavigationBuilder();
        builder.Map<TestProjectPage>("/projects/{projectId}");
        builder.SetFallback<TestProjectPage>();

        var error = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("must not contain route parameters", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RouteParserAndSerializer_HandleTemplateAndQuery()
    {
        var configuration = BuildConfiguration(builder =>
        {
            builder.Map<TestHomePage>("/");
            builder.Map<TestProjectPage>("/projects/{projectId}");
            builder.SetFallback<TestHomePage>();
        });

        var routeRegistry = new DefaultRouteRegistry(configuration);
        var parser = new DefaultRouteParser();
        var serializer = new DefaultRouteSerializer();

        var parsed = parser.TryParse("/projects/42?mode=edit", routeRegistry, out var match);
        Assert.True(parsed);
        Assert.Equal(typeof(TestProjectPage), match.Route.PageType);
        Assert.Equal("42", match.Parameters["projectId"]);
        Assert.Equal("edit", match.Query["mode"]);

        var page = new TestProjectPage
        {
            ProjectId = "42"
        };
        var serialized = serializer.TrySerialize(page, routeRegistry, out var path);
        Assert.True(serialized);
        Assert.Equal("/projects/42", path);
    }

    [Fact]
    public void RouteRegistry_PrefersMostSpecificAssignableType()
    {
        var configuration = BuildConfiguration(builder =>
        {
            builder.Map<TestHomePage>("/");
            builder.Map<TestProjectBasePage>("/projects/base");
            builder.Map<TestDerivedProjectPage>("/projects/derived");
            builder.SetFallback<TestHomePage>();
        });

        var routeRegistry = new DefaultRouteRegistry(configuration);
        var resolved = routeRegistry.TryGetRoute(typeof(TestDerivedProjectProxyPage), out var route);

        Assert.True(resolved);
        Assert.Equal(typeof(TestDerivedProjectPage), route.PageType);
        Assert.Equal("/projects/derived", route.NormalizedTemplate);
    }

    [Fact]
    public void RouteParameterBinder_BindsSupportedPrimitiveVariants()
    {
        var binder = new DefaultRouteParameterBinder();
        var page = new BindingProbePage();
        var expectedGuid = Guid.NewGuid();

        binder.Bind(page, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ProbeId"] = "17",
            ["Enabled"] = "true",
            ["Score"] = "42.75",
            ["GuidValue"] = expectedGuid.ToString(),
            ["Mode"] = "Advanced",
            ["OptionalCount"] = "99"
        });

        Assert.Equal(17, page.ProbeId);
        Assert.True(page.Enabled);
        Assert.Equal(42.75, page.Score);
        Assert.Equal(expectedGuid, page.GuidValue);
        Assert.Equal(BindingProbeMode.Advanced, page.Mode);
        Assert.Equal(99, page.OptionalCount);
    }

    [Fact]
    public void RouteParameterBinder_MapsEmptyNullableValuesToNull()
    {
        var binder = new DefaultRouteParameterBinder();
        var page = new NullableBindingProbePage
        {
            OptionalCount = 9,
            OptionalEnabled = true,
            OptionalGuid = Guid.NewGuid(),
            OptionalMode = BindingProbeMode.Advanced
        };

        binder.Bind(page, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["OptionalCount"] = "",
            ["OptionalEnabled"] = "",
            ["OptionalGuid"] = "",
            ["OptionalMode"] = ""
        });

        Assert.Null(page.OptionalCount);
        Assert.Null(page.OptionalEnabled);
        Assert.Null(page.OptionalGuid);
        Assert.Null(page.OptionalMode);
    }

    [Fact]
    public async Task StartupResolver_UnknownRoute_UsesFallbackAndReplace()
    {
        var history = new BrowserHistoryProbe("/missing");
        var configuration = BuildConfiguration(builder =>
        {
            builder.Map<TestHomePage>("/");
            builder.Map<TestSettingsPage>("/settings");
            builder.SetFallback<TestHomePage>();
        });

        var services = CreateNavigationServices(configuration, history);
        var resolver = new MirageNavigationStartupResolver();
        var originalNavigationPage = new NavigationPage(new TestSettingsPage());
        var window = new Window(originalNavigationPage);

        await resolver.ApplyInitialRouteAsync(window, services);

        var navigationPage = Assert.IsType<NavigationPage>(window.Page);
        Assert.Same(originalNavigationPage, navigationPage);
        Assert.IsType<TestHomePage>(ResolveCurrentPage(navigationPage));
        Assert.Equal("/", history.CurrentLocation);
        Assert.Contains("replace:/", history.Writes);
    }

    [Fact]
    public async Task StartupResolver_UnknownRoute_WhenCurrentPageAlreadyMatchesFallback_ReplacesUrl()
    {
        var history = new BrowserHistoryProbe("/missing");
        var configuration = BuildConfiguration(builder =>
        {
            builder.Map<TestHomePage>("/");
            builder.Map<TestSettingsPage>("/settings");
            builder.SetFallback<TestHomePage>();
        });

        var services = CreateNavigationServices(configuration, history);
        var resolver = new MirageNavigationStartupResolver();
        var window = new Window(new NavigationPage(new TestHomePage()));

        await resolver.ApplyInitialRouteAsync(window, services);

        var navigationPage = Assert.IsType<NavigationPage>(window.Page);
        Assert.IsType<TestHomePage>(ResolveCurrentPage(navigationPage));
        Assert.Equal("/", history.CurrentLocation);
        Assert.Contains("replace:/", history.Writes);
    }

    [Fact]
    public async Task StartupResolver_DeepLink_PushesActivatedPage()
    {
        var history = new BrowserHistoryProbe("/projects/55?mode=details");
        var configuration = BuildConfiguration(builder =>
        {
            builder.Map<TestHomePage>("/");
            builder.Map<TestProjectPage>("/projects/{projectId}");
            builder.SetFallback<TestHomePage>();
        });

        var services = CreateNavigationServices(configuration, history);
        var resolver = new MirageNavigationStartupResolver();
        var window = new Window(new NavigationPage(new TestHomePage()));

        await resolver.ApplyInitialRouteAsync(window, services);

        var navigationPage = Assert.IsType<NavigationPage>(window.Page);
        var currentPage = Assert.IsType<TestProjectPage>(ResolveCurrentPage(navigationPage));
        Assert.Equal("55", currentPage.ProjectId);
        Assert.Equal("details", currentPage.Mode);
        Assert.Equal("/projects/55", history.CurrentLocation);
    }

    [Fact]
    public async Task StartupResolver_DeepLink_RouteParametersOverrideQueryOnCollision()
    {
        var history = new BrowserHistoryProbe("/projects/55?projectId=12");
        var configuration = BuildConfiguration(builder =>
        {
            builder.Map<TestHomePage>("/");
            builder.Map<TestProjectPage>("/projects/{projectId}");
            builder.SetFallback<TestHomePage>();
        });

        var services = CreateNavigationServices(configuration, history);
        var resolver = new MirageNavigationStartupResolver();
        var window = new Window(new NavigationPage(new TestHomePage()));

        await resolver.ApplyInitialRouteAsync(window, services);

        var navigationPage = Assert.IsType<NavigationPage>(window.Page);
        var currentPage = Assert.IsType<TestProjectPage>(ResolveCurrentPage(navigationPage));
        Assert.Equal("55", currentPage.ProjectId);
        Assert.Equal("/projects/55", history.CurrentLocation);
    }

    [Fact]
    public async Task StartupResolver_DeepLink_GuardCancel_PreventsActivationAndRestoresCurrentRoute()
    {
        var history = new BrowserHistoryProbe("/projects/55?mode=details");
        var configuration = BuildConfiguration(builder =>
        {
            builder.Map<TestHomePage>("/");
            builder.Map<TestProjectPage>("/projects/{projectId}");
            builder.SetFallback<TestHomePage>();
        });

        var services = CreateNavigationServices(configuration, history, new CancelProjectsGuard());
        var resolver = new MirageNavigationStartupResolver();
        var window = new Window(new NavigationPage(new TestHomePage()));

        await resolver.ApplyInitialRouteAsync(window, services);

        var navigationPage = Assert.IsType<NavigationPage>(window.Page);
        Assert.IsType<TestHomePage>(ResolveCurrentPage(navigationPage));
        Assert.Equal("/", history.CurrentLocation);
        Assert.Contains("replace:/", history.Writes);
    }

    [Fact]
    public void NavigationHistoryPolicy_MapsTriggersToDeterministicWriteModes()
    {
        var configuration = BuildConfiguration(builder =>
        {
            builder.Map<TestHomePage>("/");
            builder.Map<TestProjectPage>("/projects/{projectId}");
            builder.SetFallback<TestHomePage>();
            builder.SetDefaultTitle("Mirage Navigation Tests");
        });

        var historyPolicy = new DefaultNavigationHistoryPolicy(configuration);
        Assert.Equal(
            MirageHistoryWriteMode.Push,
            historyPolicy.ResolveWriteMode(new MirageNavigationHistoryContext(MirageNavigationTrigger.MauiPush, "/", "/settings", false)));
        Assert.Equal(
            MirageHistoryWriteMode.Push,
            historyPolicy.ResolveWriteMode(new MirageNavigationHistoryContext(MirageNavigationTrigger.MauiRequestNavigation, "/settings", "/settings", false)));
        Assert.Equal(
            MirageHistoryWriteMode.Replace,
            historyPolicy.ResolveWriteMode(new MirageNavigationHistoryContext(MirageNavigationTrigger.MauiPop, "/settings", "/", true)));
        Assert.Equal(
            MirageHistoryWriteMode.Replace,
            historyPolicy.ResolveWriteMode(new MirageNavigationHistoryContext(MirageNavigationTrigger.BrowserPopState, "/projects/7", "/projects/6", true)));
    }

    [Fact]
    public void NavigationTitlePolicy_UsesCurrentPageTitleAndFallback()
    {
        var configuration = BuildConfiguration(builder =>
        {
            builder.Map<TestHomePage>("/");
            builder.SetFallback<TestHomePage>();
            builder.SetDefaultTitle("Mirage Navigation Tests");
        });

        var titlePolicy = new DefaultNavigationTitlePolicy(configuration);
        Assert.Equal("Project 5", titlePolicy.ResolveTitle(new TestProjectPage { Title = "Project 5" }, "/projects/5"));
        Assert.Equal("Mirage Navigation Tests", titlePolicy.ResolveTitle(new TestHomePage(), "/"));
    }

    private static MirageNavigationConfiguration BuildConfiguration(Action<MirageNavigationBuilder> configure)
    {
        var builder = new MirageNavigationBuilder();
        configure(builder);
        return builder.Build();
    }

    private static ServiceProvider CreateNavigationServices(
        MirageNavigationConfiguration configuration,
        BrowserHistoryProbe history,
        params INavigationGuard[] guards)
    {
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddSingleton<IRouteRegistry, DefaultRouteRegistry>();
        services.AddSingleton<IRouteParser, DefaultRouteParser>();
        services.AddSingleton<IRouteSerializer, DefaultRouteSerializer>();
        services.AddSingleton<IPageActivator, DefaultPageActivator>();
        services.AddSingleton<IRouteParameterBinder, DefaultRouteParameterBinder>();
        services.AddSingleton<INavigationHistoryPolicy, DefaultNavigationHistoryPolicy>();
        services.AddSingleton<INavigationTitlePolicy, DefaultNavigationTitlePolicy>();
        if (guards.Length == 0)
        {
            services.AddSingleton<INavigationGuard, AllowAllNavigationGuard>();
        }
        else
        {
            foreach (var guard in guards)
            {
                services.AddSingleton<INavigationGuard>(guard);
            }
        }
        services.AddSingleton<IMirageBrowserHistory>(history);

        return services.BuildServiceProvider();
    }

    private static Page ResolveCurrentPage(NavigationPage navigationPage)
    {
        if (navigationPage.Navigation.NavigationStack is { Count: > 0 } stack)
        {
            return stack[^1];
        }

        return navigationPage.CurrentPage ?? navigationPage.RootPage!;
    }

    private sealed class BrowserHistoryProbe(string initialLocation) : IMirageBrowserHistory
    {
        private readonly Dictionary<int, Action<string>> _subscriptions = new();
        private int _nextSubscriptionId = 1;

        public string CurrentLocation { get; private set; } = string.IsNullOrWhiteSpace(initialLocation) ? "/" : initialLocation;

        public List<string> Writes { get; } = [];

        public string DocumentTitle { get; private set; } = string.Empty;

        public string GetCurrentLocation() => CurrentLocation;

        public void Push(string path)
        {
            CurrentLocation = path;
            Writes.Add($"push:{path}");
        }

        public void Replace(string path)
        {
            CurrentLocation = path;
            Writes.Add($"replace:{path}");
        }

        public string GetDocumentTitle() => DocumentTitle;

        public void SetDocumentTitle(string? title)
        {
            DocumentTitle = title ?? string.Empty;
        }

        public int SubscribePopState(Action<string> callback)
        {
            ArgumentNullException.ThrowIfNull(callback);
            var id = _nextSubscriptionId++;
            _subscriptions[id] = callback;
            return id;
        }

        public void UnsubscribePopState(int subscriptionId)
        {
            _subscriptions.Remove(subscriptionId);
        }

        public void TriggerPopState(string path)
        {
            CurrentLocation = path;
            foreach (var callback in _subscriptions.Values.ToArray())
            {
                callback(path);
            }
        }
    }

    private sealed class TestHomePage : ContentPage
    {
    }

    private sealed class TestSettingsPage : ContentPage
    {
    }

    private sealed class TestProjectPage : ContentPage
    {
        public string? ProjectId { get; set; }

        public string? Mode { get; set; }
    }

    private class TestProjectBasePage : ContentPage
    {
    }

    private class TestDerivedProjectPage : TestProjectBasePage
    {
    }

    private sealed class TestDerivedProjectProxyPage : TestDerivedProjectPage
    {
    }

    private sealed class BindingProbePage : ContentPage
    {
        public int ProbeId { get; set; }

        public bool Enabled { get; set; }

        public double Score { get; set; }

        public Guid GuidValue { get; set; }

        public BindingProbeMode Mode { get; set; }

        public int? OptionalCount { get; set; }
    }

    private sealed class NullableBindingProbePage : ContentPage
    {
        public int? OptionalCount { get; set; }

        public bool? OptionalEnabled { get; set; }

        public Guid? OptionalGuid { get; set; }

        public BindingProbeMode? OptionalMode { get; set; }
    }

    private enum BindingProbeMode
    {
        Basic,
        Advanced
    }

    private sealed class CancelProjectsGuard : INavigationGuard
    {
        public ValueTask<NavigationGuardDecision> EvaluateAsync(MirageNavigationGuardContext context, CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            if (context.TargetRoute.Route.PageType == typeof(TestProjectPage))
            {
                return ValueTask.FromResult(NavigationGuardDecision.Cancel());
            }

            return ValueTask.FromResult(NavigationGuardDecision.Allow());
        }
    }
}
