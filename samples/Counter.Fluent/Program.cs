using Counter.Shared.App;
using Mirage.Maui.Hosting;
using Mirage.Provider.Fluent;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Hosting;

public static class Program
{
    public static async Task Main()
    {
        if (!OperatingSystem.IsBrowser())
        {
            Console.Error.WriteLine("Counter.Fluent must run inside browser WebAssembly runtime.");
            Console.Error.WriteLine("Use: dotnet run --project samples/Counter.Fluent/Counter.Fluent.csproj -r browser-wasm");
            return;
        }

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<CounterApp>();
        builder.UseMirageBrowserPlatform();
        builder.UseMirageComponentProvider<FluentMirageComponentProvider>();

        var app = builder.Build();
        await MirageBrowserAppHost.RunAsync(app, rootElementId: "app");

        await Task.Delay(Timeout.InfiniteTimeSpan);
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
