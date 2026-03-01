using MauiBrowser.Plugins.Sample.DependencyInjection;
using MauiBrowser.Plugins.Sample.Rendering;
using MauiBrowser.Runtime.DependencyInjection;
using MauiBrowser.Runtime.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace MauiBrowser.Plugins.Sample.Tests;

public sealed class MauiBrowserSamplePluginServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMauiBrowserSampleButtonPlugin_RegistersPluginFactory()
    {
        var services = new ServiceCollection();
        services.AddMauiBrowserRuntime();
        services.AddMauiBrowserSampleButtonPlugin();

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetService<SamplePluginButtonRendererFactory>();

        Assert.NotNull(factory);
    }

    [Fact]
    public void UseMauiBrowserSampleButtonPlugin_WithHigherPriority_OverridesDefaultButtonRenderer()
    {
        var services = new ServiceCollection();
        services.AddMauiBrowserRuntime();
        services.AddMauiBrowserSampleButtonPlugin();

        using var provider = services.BuildServiceProvider();
        provider.UseMauiBrowserSampleButtonPlugin(priority: 100);

        var registry = provider.GetRequiredService<IRendererRegistry>();
        var found = registry.TryResolve(new Button(), out var factory);

        Assert.True(found);
        Assert.IsType<SamplePluginButtonRendererFactory>(factory);
    }

    [Fact]
    public void UseMauiBrowserSampleButtonPlugin_WithLowerPriority_DoesNotOverrideDefaultButtonRenderer()
    {
        var services = new ServiceCollection();
        services.AddMauiBrowserRuntime();
        services.AddMauiBrowserSampleButtonPlugin();

        using var provider = services.BuildServiceProvider();
        provider.UseMauiBrowserSampleButtonPlugin(priority: -1);

        var registry = provider.GetRequiredService<IRendererRegistry>();
        var found = registry.TryResolve(new Button(), out var factory);

        Assert.True(found);
        Assert.IsType<MauiButtonRendererFactory>(factory);
    }
}
