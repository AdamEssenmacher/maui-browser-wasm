using MauiBrowser.Plugins.MudBlazor.DependencyInjection;
using MauiBrowser.Plugins.MudBlazor.Rendering;
using MauiBrowser.Runtime.DependencyInjection;
using MauiBrowser.Runtime.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace MauiBrowser.Plugins.MudBlazor.Tests;

public sealed class MauiBrowserMudBlazorPluginServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMauiBrowserMudBlazorPlugin_RegistersPluginFactories()
    {
        var services = new ServiceCollection();
        services.AddMauiBrowserRuntime();
        services.AddMauiBrowserMudBlazorPlugin();

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<MudBlazorButtonRendererFactory>());
        Assert.NotNull(provider.GetService<MudBlazorEntryRendererFactory>());
    }

    [Fact]
    public void UseMauiBrowserMudBlazorPlugin_WithHigherPriority_OverridesButtonRenderer()
    {
        var services = new ServiceCollection();
        services.AddMauiBrowserRuntime();
        services.AddMauiBrowserMudBlazorPlugin();

        using var provider = services.BuildServiceProvider();
        provider.UseMauiBrowserMudBlazorPlugin(priority: 100);

        var registry = provider.GetRequiredService<IRendererRegistry>();
        Assert.True(registry.TryResolve(new Button(), out var buttonFactory));
        Assert.IsType<MudBlazorButtonRendererFactory>(buttonFactory);
    }

    [Fact]
    public void UseMauiBrowserMudBlazorPlugin_RegistersEntryRenderer()
    {
        var services = new ServiceCollection();
        services.AddMauiBrowserRuntime();
        services.AddMauiBrowserMudBlazorPlugin();

        using var provider = services.BuildServiceProvider();
        provider.UseMauiBrowserMudBlazorPlugin(priority: 100);

        var registry = provider.GetRequiredService<IRendererRegistry>();
        Assert.True(registry.TryResolve(new Entry(), out var entryFactory));
        Assert.IsType<MudBlazorEntryRendererFactory>(entryFactory);
    }
}
