using Maui.Browser.Hosting;
using Maui.Browser.Sample.Demo;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;

public static class Program
{
    public static async Task Main()
    {
        if (!OperatingSystem.IsBrowser())
        {
            Console.Error.WriteLine("Maui.Browser.Sample must run inside the browser WebAssembly runtime.");
            Console.Error.WriteLine("Use: dotnet run --project src/Maui.Browser.Sample/Maui.Browser.Sample.csproj -r browser-wasm");
            return;
        }

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<SampleApp>();
        builder.UseMauiBrowser();

        var mauiApp = builder.Build();
        var rootView = DemoPageFactory.CreateRootView();
        await BrowserAppHost.RunAsync(mauiApp, rootView, rootElementId: "app");

        // Keep the browser runtime alive after mounting the root view.
        await Task.Delay(Timeout.InfiniteTimeSpan);
    }
}

internal sealed class SampleApp : Application
{
}
