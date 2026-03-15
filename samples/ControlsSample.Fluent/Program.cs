using ControlsSample.Shared.App;
using Mirage.Maui.Hosting;
using Mirage.Provider.Fluent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;

public static class Program
{
    public static async Task Main()
    {
        if (!OperatingSystem.IsBrowser())
        {
            Console.Error.WriteLine("ControlsSample.Fluent must run inside browser WebAssembly runtime.");
            Console.Error.WriteLine("Use: dotnet run --project samples/ControlsSample.Fluent/ControlsSample.Fluent.csproj -r browser-wasm");
            return;
        }

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<ControlsSampleApp>();
        builder.Services.AddSingleton<IControlsSampleVisibilityPolicy, MirageControlsSampleVisibilityPolicy>();
        builder.UseMirageBrowserPlatform();
        builder.UseMirageComponentProvider<FluentMirageComponentProvider>();

        var app = builder.Build();
        ControlsSampleApp.BootstrapServices = app.Services;
        await MirageBrowserAppHost.RunAsync(app, rootElementId: "app");

        await Task.Delay(Timeout.InfiniteTimeSpan);
    }
}
