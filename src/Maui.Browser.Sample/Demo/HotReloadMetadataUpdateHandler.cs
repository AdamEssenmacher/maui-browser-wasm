using System.Reflection.Metadata;

[assembly: MetadataUpdateHandler(typeof(Maui.Browser.Sample.Demo.HotReloadMetadataUpdateHandler))]

namespace Maui.Browser.Sample.Demo;

internal static class HotReloadMetadataUpdateHandler
{
    public static event Action? MetadataDeltaApplied;

    public static void ClearCache(Type[]? updatedTypes)
    {
        // No cache layer to clear in this prototype.
    }

    public static void UpdateApplication(Type[]? updatedTypes)
    {
        HotReloadStatus.RecordMetadataDelta();
        MetadataDeltaApplied?.Invoke();
    }
}
