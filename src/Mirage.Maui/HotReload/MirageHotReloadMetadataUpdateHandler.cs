using System.Reflection;

namespace Mirage.Maui.HotReload;

public static class MirageHotReloadMetadataUpdateHandler
{
    public static bool IsEnabledForAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        foreach (var attribute in assembly.CustomAttributes)
        {
            if (!string.Equals(
                    attribute.AttributeType.FullName,
                    "System.Reflection.Metadata.MetadataUpdateHandlerAttribute",
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (attribute.ConstructorArguments.Count != 1)
            {
                continue;
            }

            if (attribute.ConstructorArguments[0].Value is Type handlerType
                && handlerType == typeof(MirageHotReloadMetadataUpdateHandler))
            {
                return true;
            }
        }

        return false;
    }

    public static void ClearCache(Type[]? updatedTypes)
    {
        _ = updatedTypes;
    }

    public static void UpdateApplication(Type[]? updatedTypes)
    {
        _ = updatedTypes;
        Hosting.MirageBrowserAppHost.NotifyHotReloadApplied();
    }
}
