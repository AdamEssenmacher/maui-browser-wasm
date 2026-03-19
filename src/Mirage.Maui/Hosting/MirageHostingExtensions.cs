using Mirage.Core.Providers;
using Mirage.Maui.Handlers;
using Mirage.Maui.Images;
using Mirage.Maui.Navigation;
using Mirage.Web.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Hosting;
using MauiMap = Microsoft.Maui.Controls.Maps.Map;

namespace Mirage.Maui.Hosting;

public static class MirageHostingExtensions
{
    public static MauiAppBuilder UseMirageBrowserPlatform(this MauiAppBuilder builder)
    {
        var dispatcherProvider = new BrowserDispatcherProvider();
        DispatcherProvider.SetCurrent(dispatcherProvider);
        DependencyService.Register<MirageFontNamedSizeService>();

        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler(typeof(Window), typeof(MirageWindowHandler));
            handlers.AddHandler(typeof(NavigationPage), typeof(MirageNavigationPageHandler));
            handlers.AddHandler(typeof(ContentPage), typeof(MirageContentPageHandler));
            handlers.AddHandler(typeof(TabbedPage), typeof(MirageTabbedPageHandler));
            handlers.AddHandler(typeof(ContentView), typeof(MirageContentViewHandler));
            handlers.AddHandler(typeof(Frame), typeof(MirageFrameHandler));
            handlers.AddHandler(typeof(ScrollView), typeof(MirageScrollViewHandler));
            handlers.AddHandler(typeof(CollectionView), typeof(MirageCollectionViewHandler));
            handlers.AddHandler(typeof(StackLayout), typeof(MirageLayoutHandler));
            handlers.AddHandler(typeof(VerticalStackLayout), typeof(MirageLayoutHandler));
            handlers.AddHandler(typeof(HorizontalStackLayout), typeof(MirageLayoutHandler));
            handlers.AddHandler(typeof(FlexLayout), typeof(MirageLayoutHandler));
            handlers.AddHandler(typeof(Grid), typeof(MirageLayoutHandler));
            handlers.AddHandler(typeof(AbsoluteLayout), typeof(MirageLayoutHandler));
            handlers.AddHandler(typeof(Layout), typeof(MirageLayoutHandler));
            handlers.AddHandler(typeof(Label), typeof(MirageLabelHandler));
            handlers.AddHandler(typeof(Entry), typeof(MirageEntryHandler));
            handlers.AddHandler(typeof(Button), typeof(MirageButtonHandler));
            handlers.AddHandler(typeof(ActivityIndicator), typeof(MirageActivityIndicatorHandler));
            handlers.AddHandler(typeof(CheckBox), typeof(MirageCheckBoxHandler));
            handlers.AddHandler(typeof(Switch), typeof(MirageSwitchHandler));
            handlers.AddHandler(typeof(Slider), typeof(MirageSliderHandler));
            handlers.AddHandler(typeof(Picker), typeof(MiragePickerHandler));
            handlers.AddHandler(typeof(Stepper), typeof(MirageStepperHandler));
            handlers.AddHandler(typeof(Image), typeof(MirageImageHandler));
            handlers.AddHandler(typeof(Border), typeof(MirageBorderHandler));
            handlers.AddHandler(typeof(BoxView), typeof(MirageBoxViewHandler));
            handlers.AddHandler(typeof(MauiMap), typeof(MirageMapHandler));
            handlers.AddHandler(typeof(Pin), typeof(MirageMapPinHandler));
            handlers.AddHandler(typeof(MapElement), typeof(MirageMapElementHandler));
            handlers.AddHandler(typeof(Circle), typeof(MirageMapElementHandler));
            handlers.AddHandler(typeof(Polygon), typeof(MirageMapElementHandler));
            handlers.AddHandler(typeof(Polyline), typeof(MirageMapElementHandler));
        });

        builder.Services.TryAddSingleton<IDispatcherProvider>(dispatcherProvider);
        builder.Services.TryAddSingleton(sp => sp.GetRequiredService<IDispatcherProvider>().GetForCurrentThread() ?? new BrowserDispatcher());
        builder.Services.TryAddSingleton<IMirageRenderCoordinator, MirageRenderCoordinator>();
        builder.Services.TryAddSingleton<BrowserPageNavigator>();
        builder.Services.TryAddSingleton<IMiragePageNavigator>(sp => sp.GetRequiredService<BrowserPageNavigator>());
        builder.Services.TryAddSingleton<IMirageBrowserHistory, BrowserHistoryService>();
        builder.Services.TryAddSingleton<IMirageImageManifestProvider, DomMirageImageManifestProvider>();
        builder.Services.TryAddSingleton<IMirageObjectUrlFactory, DomMirageObjectUrlFactory>();
        builder.Services.TryAddSingleton<IMirageImageSourceResolver, MirageManifestImageSourceResolver>();
        EnsureNavigationServices(builder.Services);

        return builder;
    }

    public static MauiAppBuilder UseMirageNavigation(this MauiAppBuilder builder, Action<MirageNavigationBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var navigationBuilder = new MirageNavigationBuilder();
        configure(navigationBuilder);
        var configuration = navigationBuilder.Build();

        builder.Services.Replace(ServiceDescriptor.Singleton(configuration));
        EnsureNavigationServices(builder.Services);
        return builder;
    }

    public static MauiAppBuilder UseMirageComponentProvider<TProvider>(this MauiAppBuilder builder)
        where TProvider : class, IMirageComponentProvider
    {
        builder.Services.AddSingleton<IMirageComponentProvider, TProvider>();
        return builder;
    }

    private static void EnsureNavigationServices(IServiceCollection services)
    {
        services.TryAddSingleton(MirageNavigationConfiguration.Empty);
        services.TryAddSingleton<IRouteRegistry, DefaultRouteRegistry>();
        services.TryAddSingleton<IRouteParser, DefaultRouteParser>();
        services.TryAddSingleton<IRouteSerializer, DefaultRouteSerializer>();
        services.TryAddSingleton<IPageActivator, DefaultPageActivator>();
        services.TryAddSingleton<IRouteParameterBinder, DefaultRouteParameterBinder>();
        services.TryAddSingleton<INavigationHistoryPolicy, DefaultNavigationHistoryPolicy>();
        services.TryAddSingleton<INavigationTitlePolicy, DefaultNavigationTitlePolicy>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<INavigationGuard, AllowAllNavigationGuard>());
        services.TryAddSingleton<IMirageNavigationStartupResolver, MirageNavigationStartupResolver>();
    }
}
