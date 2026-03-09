using Counter.Fast.App;
using Mirage.Maui.Hosting;
using Mirage.Provider.Fast;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Hosting;

[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(CounterHotReloadHandler))]

public static class Program
{
    public static async Task Main()
    {
        if (!OperatingSystem.IsBrowser())
        {
            Console.Error.WriteLine("Counter.Fast must run inside browser WebAssembly runtime.");
            Console.Error.WriteLine("Use: dotnet run --project samples/Counter.Fast/Counter.Fast.csproj -r browser-wasm");
            return;
        }

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<CounterApp>();
        builder.UseMirageBrowserPlatform();
        builder.UseMirageComponentProvider<FastMirageComponentProvider>();

        var app = builder.Build();
        await MirageBrowserAppHost.RunAsync(app, rootElementId: "app");

        await Task.Delay(Timeout.InfiniteTimeSpan);
    }
}

internal static class CounterHotReloadHandler
{
    public static void ClearCache(Type[]? updatedTypes)
    {
    }

    public static void UpdateApplication(Type[]? updatedTypes)
    {
        if (OperatingSystem.IsBrowser())
        {
            MirageBrowserAppHost.NotifyHotReloadApplied();
        }
    }
}

internal sealed class CounterApp : Application
{
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var page = new CounterPage();
        return new Window(page);
    }
}
