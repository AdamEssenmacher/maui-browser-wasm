using Mirage.Maui.Handlers;
using Mirage.Maui.Hosting;
using Mirage.Maui.Navigation;
using Mirage.Web.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Handlers;
using Xunit;

namespace Mirage.Integration.Tests;

[Collection("Mirage NonParallel")]
public sealed class MiragePageHandlerTests
{
    [Fact]
    public void PageHandler_TitleProjection_IgnoresDetachedAndNavigationOwnedPages()
    {
        var history = new BrowserHistoryProbe("/");
        var mauiContext = CreateMauiContext(history, includeNavigationServices: false);

        var detachedPage = new ContentPage
        {
            Title = "Detached Title"
        };

        var detachedHandler = new MirageContentPageHandler();
        ((IElementHandler)detachedHandler).SetMauiContext(mauiContext);
        ((IElementHandler)detachedHandler).SetVirtualView(detachedPage);

        // Detached handlers should not claim document-title ownership.
        Assert.Equal(string.Empty, history.DocumentTitle);
        MirageContentPageHandler.Mapper.UpdateProperty(detachedHandler, detachedPage, nameof(Page.Title));
        Assert.Equal(string.Empty, history.DocumentTitle);

        var rootNode = MiragePlatformView.CreateControl(Core.Semantics.ComponentKind.Root);
        rootNode.AssignNodeId("node-root-page-test");
        rootNode.AddChild(detachedHandler.PlatformView);
        MirageContentPageHandler.Mapper.UpdateProperty(detachedHandler, detachedPage, nameof(Page.Title));
        Assert.Equal("Detached Title", history.DocumentTitle);

        var navOwnedPage = new ContentPage
        {
            Title = "Navigation Child"
        };
        _ = new NavigationPage(navOwnedPage);

        var navOwnedHandler = new MirageContentPageHandler();
        ((IElementHandler)navOwnedHandler).SetMauiContext(mauiContext);
        ((IElementHandler)navOwnedHandler).SetVirtualView(navOwnedPage);
        rootNode.AddChild(navOwnedHandler.PlatformView);

        MirageContentPageHandler.Mapper.UpdateProperty(navOwnedHandler, navOwnedPage, nameof(Page.Title));
        Assert.Equal("Detached Title", history.DocumentTitle);
    }

    [Fact]
    public void PageHandler_UnsupportedPageSubtype_ThrowsClearError()
    {
        var history = new BrowserHistoryProbe("/");
        var mauiContext = CreateMauiContext(history, includeNavigationServices: false);

        var unsupportedPage = new FlyoutPage();
        var handler = new MiragePageHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);

        var error = Assert.Throws<NotSupportedException>(() => ((IElementHandler)handler).SetVirtualView(unsupportedPage));
        Assert.Contains(nameof(FlyoutPage), error.Message, StringComparison.Ordinal);
        Assert.Contains("dedicated handler", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ContentPageHandler_TitleProjection_ProjectsForActiveRootPage_AndUpdates()
    {
        var history = new BrowserHistoryProbe("/");
        var mauiContext = CreateMauiContext(history, includeNavigationServices: false);

        var rootPage = new ContentPage
        {
            Title = "Root Title"
        };
        var rootNode = MiragePlatformView.CreateControl(Core.Semantics.ComponentKind.Root);
        rootNode.AssignNodeId("node-root-content-page");

        var rootPageHandler = new MirageContentPageHandler();
        ((IElementHandler)rootPageHandler).SetMauiContext(mauiContext);
        ((IElementHandler)rootPageHandler).SetVirtualView(rootPage);
        rootNode.AddChild(rootPageHandler.PlatformView);

        Assert.True(rootPageHandler.TryProjectActivePageTitle());
        Assert.Equal("Root Title", history.DocumentTitle);

        rootPage.Title = "Updated Root Title";
        MirageContentPageHandler.Mapper.UpdateProperty(rootPageHandler, rootPage, nameof(Page.Title));
        Assert.Equal("Updated Root Title", history.DocumentTitle);
    }

    [Fact]
    public void ContentPageHandler_TitleProjection_DoesNotCompeteWithNavigationPageAuthority()
    {
        var history = new BrowserHistoryProbe("/");
        var mauiContext = CreateMauiContext(history, includeNavigationServices: true);
        var rootNode = MiragePlatformView.CreateControl(Core.Semantics.ComponentKind.Root);
        rootNode.AssignNodeId("node-root-navigation-page");

        var homePage = new ContentPage
        {
            Title = "Home"
        };
        var homePageHandler = new MirageContentPageHandler();
        ((IElementHandler)homePageHandler).SetMauiContext(mauiContext);
        ((IElementHandler)homePageHandler).SetVirtualView(homePage);
        var navigationPage = new NavigationPage(homePage);

        var navigationHandler = new MirageNavigationPageHandler();
        ((IElementHandler)navigationHandler).SetMauiContext(mauiContext);
        ((IElementHandler)navigationHandler).SetVirtualView(navigationPage);
        rootNode.AddChild(navigationHandler.PlatformView);

        var childHandler = Assert.IsType<MirageContentPageHandler>(homePage.Handler);
        var baselineSetDocumentTitleCalls = history.SetDocumentTitleCallCount;

        // NavigationPage should remain the title authority in stack scenarios.
        MirageContentPageHandler.Mapper.UpdateProperty(childHandler, homePage, nameof(Page.Title));
        Assert.Equal(baselineSetDocumentTitleCalls, history.SetDocumentTitleCallCount);
    }

    [Fact]
    public void TabbedPageHandler_TitleProjection_ProjectsActiveTab_AndUpdatesOnTabSwitch()
    {
        var originalDispatcherProvider = DispatcherProvider.Current;
        try
        {
            DispatcherProvider.SetCurrent(new BrowserDispatcherProvider());
            var history = new BrowserHistoryProbe("/");
            var mauiContext = CreateMauiContext(history, includeNavigationServices: false);

            var firstTab = CreateAttachedContentPage(mauiContext, "First Tab");
            var secondTab = CreateAttachedContentPage(mauiContext, "Second Tab");
            var tabbedPage = new TabbedPage();
            tabbedPage.Children.Add(firstTab);
            tabbedPage.Children.Add(secondTab);
            tabbedPage.CurrentPage = firstTab;

            var tabbedHandler = new MirageTabbedPageHandler();
            ((IElementHandler)tabbedHandler).SetMauiContext(mauiContext);
            ((IElementHandler)tabbedHandler).SetVirtualView(tabbedPage);

            Assert.Equal(string.Empty, history.DocumentTitle);

            var rootNode = MiragePlatformView.CreateControl(Core.Semantics.ComponentKind.Root);
            rootNode.AssignNodeId("node-root-tabbed-page");
            rootNode.AddChild(tabbedHandler.PlatformView);
            Assert.True(tabbedHandler.TryProjectActivePageTitle());
            Assert.Equal("First Tab", history.DocumentTitle);

            tabbedPage.CurrentPage = secondTab;
            MirageTabbedPageHandler.Mapper.UpdateProperty(tabbedHandler, tabbedPage, nameof(TabbedPage.CurrentPage));
            Assert.Equal("Second Tab", history.DocumentTitle);
        }
        finally
        {
            DispatcherProvider.SetCurrent(originalDispatcherProvider);
        }
    }

    [Fact]
    public void TabbedPageHandler_TitleProjection_DelegatesToNavigationPageAuthority_ForNavigationTabs()
    {
        var originalDispatcherProvider = DispatcherProvider.Current;
        try
        {
            DispatcherProvider.SetCurrent(new BrowserDispatcherProvider());
            var history = new BrowserHistoryProbe("/");
            var mauiContext = CreateMauiContext(
                history,
                includeNavigationServices: true,
                configureNavigation: builder =>
                {
                    builder.Map<FirstTabbedRoutePage>("/first");
                    builder.Map<NavigationTabHomePage>("/nav/home");
                    builder.SetFallback<FirstTabbedRoutePage>();
                });

            var firstTab = AttachContentPageHandler(mauiContext, new FirstTabbedRoutePage
            {
                Title = "First Tab"
            });
            var navigationHome = AttachContentPageHandler(mauiContext, new NavigationTabHomePage
            {
                Title = "Navigation Home"
            });
            var navigationTab = AttachNavigationPageHandler(mauiContext, new NavigationPage(navigationHome));

            var tabbedPage = new TabbedPage();
            tabbedPage.Children.Add(firstTab);
            tabbedPage.Children.Add(navigationTab);
            tabbedPage.CurrentPage = firstTab;

            var tabbedHandler = new MirageTabbedPageHandler();
            ((IElementHandler)tabbedHandler).SetMauiContext(mauiContext);
            ((IElementHandler)tabbedHandler).SetVirtualView(tabbedPage);

            var rootNode = MiragePlatformView.CreateControl(Core.Semantics.ComponentKind.Root);
            rootNode.AssignNodeId("node-root-tabbed-nav-title");
            rootNode.AddChild(tabbedHandler.PlatformView);

            Assert.True(tabbedHandler.TryProjectActivePageTitle());
            Assert.Equal("First Tab", history.DocumentTitle);

            tabbedPage.CurrentPage = navigationTab;
            MirageTabbedPageHandler.Mapper.UpdateProperty(tabbedHandler, tabbedPage, nameof(TabbedPage.CurrentPage));

            // NavigationPage remains the title authority for its stack when hosted in TabbedPage.
            Assert.Equal("Navigation Home", history.DocumentTitle);
        }
        finally
        {
            DispatcherProvider.SetCurrent(originalDispatcherProvider);
        }
    }

    [Fact]
    public void ContentPageHandler_TitleProjection_RequiresRootAncestry_NotOnlyImmediateParent()
    {
        var history = new BrowserHistoryProbe("/");
        var mauiContext = CreateMauiContext(history, includeNavigationServices: false);

        var page = new ContentPage
        {
            Title = "Nested Root-Ancestry Title"
        };
        var handler = new MirageContentPageHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(page);

        var detachedContainer = MiragePlatformView.CreateControl(Core.Semantics.ComponentKind.ContentContainer);
        detachedContainer.AssignNodeId("node-detached-non-root-container");
        detachedContainer.AddChild(handler.PlatformView);

        // Immediate-parent attachment is insufficient: subtree must be rooted under ComponentKind.Root.
        MirageContentPageHandler.Mapper.UpdateProperty(handler, page, nameof(Page.Title));
        Assert.Equal(string.Empty, history.DocumentTitle);

        var rootNode = MiragePlatformView.CreateControl(Core.Semantics.ComponentKind.Root);
        rootNode.AssignNodeId("node-root-nested-container");
        rootNode.AddChild(detachedContainer);

        MirageContentPageHandler.Mapper.UpdateProperty(handler, page, nameof(Page.Title));
        Assert.Equal("Nested Root-Ancestry Title", history.DocumentTitle);
    }

    private static ContentPage CreateAttachedContentPage(IMauiContext mauiContext, string title)
    {
        var page = new ContentPage
        {
            Title = title
        };

        var handler = new MirageContentPageHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(page);
        return page;
    }

    private static TPage AttachContentPageHandler<TPage>(IMauiContext mauiContext, TPage page)
        where TPage : ContentPage
    {
        var handler = new MirageContentPageHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(page);
        return page;
    }

    private static NavigationPage AttachNavigationPageHandler(IMauiContext mauiContext, NavigationPage navigationPage)
    {
        var handler = new MirageNavigationPageHandler();
        ((IElementHandler)handler).SetMauiContext(mauiContext);
        ((IElementHandler)handler).SetVirtualView(navigationPage);
        return navigationPage;
    }

    private static IMauiContext CreateMauiContext(
        BrowserHistoryProbe history,
        bool includeNavigationServices,
        Action<MirageNavigationBuilder>? configureNavigation = null)
    {
        var coordinator = new MirageRenderCoordinator(new BrowserDispatcher());
        var services = new ServiceCollection();
        services.AddSingleton<IMirageRenderCoordinator>(coordinator);
        services.AddSingleton<IMirageBrowserHistory>(history);
        services.AddSingleton(
            includeNavigationServices
                ? BuildNavigationConfiguration(defaultTitle: "Mirage Navigation Tests", configureNavigation)
                : BuildNavigationConfiguration(defaultTitle: "Mirage", configureNavigation));
        services.AddSingleton<INavigationTitlePolicy, DefaultNavigationTitlePolicy>();

        if (includeNavigationServices)
        {
            services.AddSingleton<IRouteRegistry, DefaultRouteRegistry>();
            services.AddSingleton<IRouteParser, DefaultRouteParser>();
            services.AddSingleton<IRouteSerializer, DefaultRouteSerializer>();
            services.AddSingleton<IPageActivator, DefaultPageActivator>();
            services.AddSingleton<IRouteParameterBinder, DefaultRouteParameterBinder>();
            services.AddSingleton<INavigationHistoryPolicy, DefaultNavigationHistoryPolicy>();
            services.AddSingleton<INavigationGuard, AllowAllNavigationGuard>();
        }

        return new MauiContext(services.BuildServiceProvider());
    }

    private static MirageNavigationConfiguration BuildNavigationConfiguration(
        string defaultTitle,
        Action<MirageNavigationBuilder>? configureNavigation = null)
    {
        var builder = new MirageNavigationBuilder();
        builder.SetDefaultTitle(defaultTitle);
        configureNavigation?.Invoke(builder);
        return builder.Build();
    }

    private sealed class FirstTabbedRoutePage : ContentPage;

    private sealed class NavigationTabHomePage : ContentPage;

    private sealed class BrowserHistoryProbe(string initialLocation) : IMirageBrowserHistory
    {
        private readonly Dictionary<int, Action<string>> _subscriptions = new();
        private int _nextSubscriptionId = 1;

        public string CurrentLocation { get; private set; } = string.IsNullOrWhiteSpace(initialLocation) ? "/" : initialLocation;

        public string DocumentTitle { get; private set; } = string.Empty;

        public int SetDocumentTitleCallCount { get; private set; }

        public string GetCurrentLocation() => CurrentLocation;

        public void Push(string path)
        {
            CurrentLocation = path;
        }

        public void Replace(string path)
        {
            CurrentLocation = path;
        }

        public string GetDocumentTitle() => DocumentTitle;

        public void SetDocumentTitle(string? title)
        {
            SetDocumentTitleCallCount++;
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
    }
}
