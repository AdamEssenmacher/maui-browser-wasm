using MauiBrowser.Runtime.DependencyInjection;
using MauiBrowser.Runtime.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace MauiBrowser.Runtime.Tests;

public sealed class MauiBrowserRuntimeServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMauiBrowserRuntime_ThrowsWhenServicesIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            MauiBrowserRuntimeServiceCollectionExtensions.AddMauiBrowserRuntime(null!));

        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    public void AddMauiBrowserRuntime_RegistersCoreServices()
    {
        var services = new ServiceCollection();
        services.AddMauiBrowserRuntime();

        using var provider = services.BuildServiceProvider();

        var renderer = provider.GetService<IMauiViewRenderer>();
        var registry = provider.GetService<IRendererRegistry>();

        Assert.NotNull(renderer);
        Assert.NotNull(registry);
        Assert.True(registry.TryResolve(new Label(), out _));
    }
}
