using Mirage.Web.Maps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Hosting;

namespace Mirage.Provider.LeafletMaps;

public static class MirageLeafletMapHostingExtensions
{
    public static MauiAppBuilder UseMirageLeafletMaps(this MauiAppBuilder builder, Action<LeafletMapOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new LeafletMapOptions();
        configure?.Invoke(options);

        builder.Services.Replace(ServiceDescriptor.Singleton(options));
        builder.Services.Replace(ServiceDescriptor.Singleton<IMirageMapProvider, LeafletMapProvider>());
        return builder;
    }
}
