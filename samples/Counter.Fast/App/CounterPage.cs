using Mirage.Maui.Hosting;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using System.Runtime.InteropServices.JavaScript;

namespace Counter.Fast.App;

public sealed partial class CounterPage : ContentPage
{
    private const string HotReloadXamlPath = "/_hotreload/CounterPage.xaml";
    private static readonly TimeSpan HotReloadPollInterval = TimeSpan.FromMilliseconds(750);
    private readonly CancellationTokenSource _hotReloadCancellation = new();
    private string? _lastHotReloadXaml;

    public CounterPage()
    {
        BindingContext = new CounterState();
        InitializeComponent();

        if (OperatingSystem.IsBrowser() && string.Equals(Environment.GetEnvironmentVariable("DOTNET_WATCH"), "1", StringComparison.Ordinal))
        {
            _ = WatchForXamlChangesAsync(_hotReloadCancellation.Token);
        }
    }

    protected override void OnDisappearing()
    {
        _hotReloadCancellation.Cancel();
        base.OnDisappearing();
    }

    private async Task WatchForXamlChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await CounterPageHotReloadInterop.EnsureModuleLoadedAsync();
        }
        catch
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            string? xaml;
            try
            {
                xaml = await CounterPageHotReloadInterop.FetchTextAsync($"{HotReloadXamlPath}?nonce={DateTime.UtcNow.Ticks}");
            }
            catch
            {
                xaml = null;
            }

            if (!string.IsNullOrWhiteSpace(xaml) && !string.Equals(_lastHotReloadXaml, xaml, StringComparison.Ordinal))
            {
                _lastHotReloadXaml = xaml;
                ApplyHotReloadXamlOnDispatcher(xaml);
            }

            try
            {
                await Task.Delay(HotReloadPollInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void ApplyHotReloadXamlOnDispatcher(string xaml)
    {
        var dispatcher = Dispatcher;
        if (dispatcher is null)
        {
            ApplyHotReloadXaml(xaml);
            return;
        }

        if (!dispatcher.Dispatch(() => ApplyHotReloadXaml(xaml)))
        {
            ApplyHotReloadXaml(xaml);
        }
    }

    private void ApplyHotReloadXaml(string xaml)
    {
        var bindingContext = BindingContext;
#pragma warning disable IL2026
        this.LoadFromXaml(xaml);
#pragma warning restore IL2026
        BindingContext = bindingContext;
        MirageBrowserAppHost.NotifyHotReloadApplied();
    }

    private static partial class CounterPageHotReloadInterop
    {
        private const string Module = "mirage-dom";
        private const string ModuleUrl = "../_framework/mirage-dom.js";
        private static Task? s_moduleLoadTask;

        internal static Task EnsureModuleLoadedAsync()
        {
            var existing = Volatile.Read(ref s_moduleLoadTask);
            if (existing is not null)
            {
                return existing;
            }

            var created = JSHost.ImportAsync(Module, ModuleUrl);
            var prior = Interlocked.CompareExchange(ref s_moduleLoadTask, created, null);
            return prior ?? created;
        }

        [JSImport("fetchText", "mirage-dom")]
        internal static partial Task<string?> FetchTextAsync(string path);
    }
}
