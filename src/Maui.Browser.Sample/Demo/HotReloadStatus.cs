using System.Reflection.Metadata;

namespace Maui.Browser.Sample.Demo;

internal static class HotReloadStatus
{
    private static int s_metadataDeltaCount;
    private static string s_lastUpdateKind = "startup";

    public static bool MetadataUpdaterSupported => MetadataUpdater.IsSupported;

    public static int MetadataDeltaCount => Volatile.Read(ref s_metadataDeltaCount);

    public static string LastUpdateKind => Volatile.Read(ref s_lastUpdateKind);

    public static void RecordMetadataDelta()
    {
        Interlocked.Increment(ref s_metadataDeltaCount);
        Volatile.Write(ref s_lastUpdateKind, "metadata-delta");
    }

    public static void RecordXamlLiveApply()
    {
        Volatile.Write(ref s_lastUpdateKind, "xaml-live-apply");
    }

    public static void RecordXamlApplyFailure()
    {
        Volatile.Write(ref s_lastUpdateKind, "xaml-apply-failed");
    }

    public static string BuildSummary()
    {
        var mode = MetadataUpdaterSupported ? "metadata" : "refresh-only";
        var modifiableAssemblies = ReadEnvOrDefault("DOTNET_MODIFIABLE_ASSEMBLIES");
        var browserTools = ReadEnvOrDefault("__ASPNETCORE_BROWSER_TOOLS");

        return $"Hot reload mode: {mode}, last update: {LastUpdateKind}, metadata deltas: {MetadataDeltaCount} (DOTNET_MODIFIABLE_ASSEMBLIES={modifiableAssemblies}, __ASPNETCORE_BROWSER_TOOLS={browserTools})";
    }

    private static string ReadEnvOrDefault(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? "<unset>" : value;
    }
}
