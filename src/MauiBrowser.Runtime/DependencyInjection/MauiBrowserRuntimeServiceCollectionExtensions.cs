using System;
using MauiBrowser.Runtime.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace MauiBrowser.Runtime.DependencyInjection;

/// <summary>
/// Service registration extensions for the MAUI browser runtime.
/// </summary>
public static class MauiBrowserRuntimeServiceCollectionExtensions
{
    /// <summary>
    /// Registers baseline runtime services required to render MAUI views in the browser.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddMauiBrowserRuntime(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<MauiLabelRendererFactory>();

        services.AddSingleton<IRendererRegistry>(sp =>
        {
            var registry = new RendererRegistry();
            registry.Register<Label>(sp.GetRequiredService<MauiLabelRendererFactory>());
            registry.Register<StackLayout>(new MauiStackLayoutRendererFactory(registry));
            registry.Register<Grid>(new MauiGridRendererFactory(registry));
            return registry;
        });

        services.AddSingleton<IMauiViewRenderer, MauiViewRenderer>();
        return services;
    }
}
