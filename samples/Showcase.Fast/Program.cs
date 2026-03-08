using Mirage.Maui.Hosting;
using Mirage.Provider.Fast;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Hosting;
using Showcase.Fast.App;

public static class Program
{
    public static async Task Main()
    {
        if (!OperatingSystem.IsBrowser())
        {
            Console.Error.WriteLine("Showcase.Fast must run inside browser WebAssembly runtime.");
            Console.Error.WriteLine("Use: dotnet run --project samples/Showcase.Fast/Showcase.Fast.csproj -r browser-wasm");
            return;
        }

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<ShowcaseApp>();
        builder.UseMirageBrowserPlatform();
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler(typeof(ShowcaseVendorWidget), typeof(ShowcaseVendorWidgetHandler));
        });
        builder.UseMirageComponentProvider<FastMirageComponentProvider>();

        var app = builder.Build();
        await MirageBrowserAppHost.RunAsync(app, rootElementId: "app");

        await Task.Delay(Timeout.InfiniteTimeSpan);
    }
}

internal sealed class ShowcaseApp : Application
{
    private readonly Mirage.Web.Runtime.IMiragePageNavigator _pageNavigator;

    public ShowcaseApp(Mirage.Web.Runtime.IMiragePageNavigator pageNavigator)
    {
        _pageNavigator = pageNavigator;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new ShowcaseHeroPage(_pageNavigator));
    }
}
