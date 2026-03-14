using Mirage.Maui.Hosting;
using Mirage.Provider.Fluent;
using Mirage.Web.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Hosting;
using Showcase.Shared.App;

public static class Program
{
    public static async Task Main()
    {
        if (!OperatingSystem.IsBrowser())
        {
            Console.Error.WriteLine("Showcase.Fluent must run inside browser WebAssembly runtime.");
            Console.Error.WriteLine("Use: dotnet run --project samples/Showcase.Fluent/Showcase.Fluent.csproj -r browser-wasm");
            return;
        }

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<ShowcaseApp>();
        builder.ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        });
        builder.UseMirageBrowserPlatform();
        builder.UseMirageNavigation(routes =>
        {
            routes.Map<ShowcaseHeroPage>("/");
            routes.Map<ShowcaseLayoutLabPage>("/layout-lab");
            routes.Map<ShowcaseNavigationLabPage>("/navigation-lab");
            routes.Map<ShowcaseProjectPage>("/projects/{projectId}");
            routes.SetFallback<ShowcaseHeroPage>();
            routes.SetDefaultTitle("Mirage Showcase");
        });
        builder.UseMirageComponentProvider<FluentMirageComponentProvider>();

        var app = builder.Build();
        await MirageBrowserAppHost.RunAsync(app, rootElementId: "app");

        await Task.Delay(Timeout.InfiniteTimeSpan);
    }
}

internal sealed class ShowcaseApp : Application
{
    public ShowcaseApp()
    {
        Resources ??= new ResourceDictionary();
        Resources.MergedDictionaries.Add(new ShowcaseMauiTemplateColors());
        Resources.MergedDictionaries.Add(new ShowcaseMauiTemplateStyles());
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        _ = activationState;
        return new Window(new NavigationPage(new ShowcaseHeroPage())
        {
            AutomationId = "showcase-navigation-root"
        });
    }
}
