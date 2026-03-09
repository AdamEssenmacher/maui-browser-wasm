namespace Mirage.Maui.HotReload;

public static class MirageHotReloadMetadataUpdateHandler
{
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
