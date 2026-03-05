using Maui.Browser.Hosting;
using Maui.Browser.Sample.Demo;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;

public static class Program
{
    private static XamlLiveReloadClient? s_liveReloadClient;

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

        Console.WriteLine($"Hot reload capability: {(HotReloadStatus.MetadataUpdaterSupported ? "metadata" : "refresh-only")}.");

        var mauiApp = builder.Build();
        await BrowserAppHost.RunAsync(mauiApp, rootElementId: "app");

        s_liveReloadClient = await XamlLiveReloadClient.TryStartAsync();
        if (s_liveReloadClient is null)
        {
            Console.WriteLine("[hot-reload] Dev host endpoints were not detected. Live XAML reapply is disabled.");
        }

        // Keep the browser runtime alive after mounting the root view.
        await Task.Delay(Timeout.InfiniteTimeSpan);
    }
}

internal sealed class SampleApp : Microsoft.Maui.Controls.Application
{
    protected override Microsoft.Maui.Controls.Window CreateWindow(Microsoft.Maui.IActivationState? activationState)
    {
        return new Microsoft.Maui.Controls.Window(new XamlDemoPage());
    }
}
