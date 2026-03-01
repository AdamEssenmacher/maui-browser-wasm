using System;
using MauiBrowser.Plugins.MudBlazor.Rendering;
using MauiBrowser.Runtime.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using MudBlazor.Services;

namespace MauiBrowser.Plugins.MudBlazor.DependencyInjection;

/// <summary>
/// Service registration extensions for MudBlazor MAUI Browser plugin overrides.
/// </summary>
public static class MauiBrowserMudBlazorPluginServiceCollectionExtensions
{
    /// <summary>
    /// Registers MudBlazor services and plugin renderer factories.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddMauiBrowserMudBlazorPlugin(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMudServices();
        services.AddSingleton<MudBlazorButtonRendererFactory>();
        services.AddSingleton<MudBlazorEntryRendererFactory>();
        return services;
    }

    /// <summary>
    /// Applies MudBlazor plugin renderer registrations to the runtime registry.
    /// </summary>
    /// <param name="serviceProvider">The service provider containing runtime and plugin services.</param>
    /// <param name="priority">Priority used for plugin renderer registration. Higher values win.</param>
    /// <returns>The same service provider for chaining.</returns>
    public static IServiceProvider UseMauiBrowserMudBlazorPlugin(
        this IServiceProvider serviceProvider,
        int priority = 100)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var registry = serviceProvider.GetRequiredService<IRendererRegistry>();
        registry.Register<Button>(serviceProvider.GetRequiredService<MudBlazorButtonRendererFactory>(), priority);
        registry.Register<Entry>(serviceProvider.GetRequiredService<MudBlazorEntryRendererFactory>(), priority);
        return serviceProvider;
    }
}
