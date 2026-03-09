using Mirage.Core.Providers;
using Mirage.Maui.Handlers;
using Mirage.Web.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Hosting;

namespace Mirage.Maui.Hosting;

public static class MirageHostingExtensions
{
    public static MauiAppBuilder UseMirageBrowserPlatform(this MauiAppBuilder builder)
    {
        var dispatcherProvider = new BrowserDispatcherProvider();
        DispatcherProvider.SetCurrent(dispatcherProvider);

        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler(typeof(Window), typeof(MirageWindowHandler));
            handlers.AddHandler(typeof(ContentPage), typeof(MirageContentPageHandler));
            handlers.AddHandler(typeof(ContentView), typeof(MirageContentViewHandler));
            handlers.AddHandler(typeof(ScrollView), typeof(MirageScrollViewHandler));
            handlers.AddHandler(typeof(VerticalStackLayout), typeof(MirageLayoutHandler));
            handlers.AddHandler(typeof(HorizontalStackLayout), typeof(MirageLayoutHandler));
            handlers.AddHandler(typeof(Grid), typeof(MirageLayoutHandler));
            handlers.AddHandler(typeof(Label), typeof(MirageLabelHandler));
            handlers.AddHandler(typeof(Entry), typeof(MirageEntryHandler));
            handlers.AddHandler(typeof(Button), typeof(MirageButtonHandler));
        });

        builder.Services.TryAddSingleton<IDispatcherProvider>(dispatcherProvider);
        builder.Services.TryAddSingleton(sp => sp.GetRequiredService<IDispatcherProvider>().GetForCurrentThread() ?? new BrowserDispatcher());
        builder.Services.TryAddSingleton<IMirageRenderCoordinator, MirageRenderCoordinator>();
        builder.Services.TryAddSingleton<BrowserPageNavigator>();
        builder.Services.TryAddSingleton<IMiragePageNavigator>(sp => sp.GetRequiredService<BrowserPageNavigator>());

        return builder;
    }

    public static MauiAppBuilder UseMirageComponentProvider<TProvider>(this MauiAppBuilder builder)
        where TProvider : class, IMirageComponentProvider
    {
        builder.Services.AddSingleton<IMirageComponentProvider, TProvider>();
        return builder;
    }
}
