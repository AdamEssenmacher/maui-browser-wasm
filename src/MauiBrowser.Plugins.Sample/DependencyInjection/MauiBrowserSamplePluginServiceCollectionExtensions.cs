using System;
using MauiBrowser.Plugins.Sample.Rendering;
using MauiBrowser.Runtime.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace MauiBrowser.Plugins.Sample.DependencyInjection;

/// <summary>
/// Service registration extensions for sample MAUI Browser plugin overrides.
/// </summary>
public static class MauiBrowserSamplePluginServiceCollectionExtensions
{
    /// <summary>
    /// Registers sample plugin services required to override runtime renderers.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddMauiBrowserSampleButtonPlugin(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<SamplePluginButtonRendererFactory>();
        return services;
    }

    /// <summary>
    /// Applies sample plugin renderer registrations to the runtime registry.
    /// </summary>
    /// <param name="serviceProvider">The service provider containing runtime and plugin services.</param>
    /// <param name="priority">Priority used for plugin renderer registration. Higher values win.</param>
    /// <returns>The same service provider for chaining.</returns>
    public static IServiceProvider UseMauiBrowserSampleButtonPlugin(
        this IServiceProvider serviceProvider,
        int priority = 100)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var registry = serviceProvider.GetRequiredService<IRendererRegistry>();
        var factory = serviceProvider.GetRequiredService<SamplePluginButtonRendererFactory>();
        registry.Register<Button>(factory, priority);
        return serviceProvider;
    }
}
