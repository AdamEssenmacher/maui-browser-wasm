using Maui.Browser.Handlers;
using Maui.Browser.Measurement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Hosting;

namespace Maui.Browser.Hosting;

public static class MauiBrowserHostingExtensions
{
    public static MauiAppBuilder UseMauiBrowser(this MauiAppBuilder builder)
    {
        var dispatcherProvider = new BrowserDispatcherProvider();
        DispatcherProvider.SetCurrent(dispatcherProvider);

        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler(typeof(Label), typeof(BrowserLabelHandler));
            handlers.AddHandler(typeof(Button), typeof(BrowserButtonHandler));
            handlers.AddHandler(typeof(Entry), typeof(BrowserEntryHandler));
            handlers.AddHandler(typeof(ContentView), typeof(BrowserContentViewHandler));
            handlers.AddHandler(typeof(VerticalStackLayout), typeof(BrowserLayoutHandler));
            handlers.AddHandler(typeof(HorizontalStackLayout), typeof(BrowserLayoutHandler));
            handlers.AddHandler(typeof(Grid), typeof(BrowserLayoutHandler));
            handlers.AddHandler(typeof(AbsoluteLayout), typeof(BrowserLayoutHandler));
        });

        builder.Services.AddSingleton<IDispatcherProvider>(dispatcherProvider);
        builder.Services.AddSingleton(sp => sp.GetRequiredService<IDispatcherProvider>().GetForCurrentThread() ?? new BrowserDispatcher());
        builder.Services.AddSingleton<IBrowserTextMeasurer, BrowserTextMeasurer>();
        return builder;
    }
}
