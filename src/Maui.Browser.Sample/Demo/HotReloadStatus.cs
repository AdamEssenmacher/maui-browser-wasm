using System.Reflection.Metadata;

namespace Maui.Browser.Sample.Demo;

internal static class HotReloadStatus
{
    public static bool MetadataUpdaterSupported => MetadataUpdater.IsSupported;

    public static string BuildSummary()
    {
        var mode = MetadataUpdaterSupported ? "metadata" : "refresh-only";
        var modifiableAssemblies = ReadEnvOrDefault("DOTNET_MODIFIABLE_ASSEMBLIES");
        var browserTools = ReadEnvOrDefault("__ASPNETCORE_BROWSER_TOOLS");
        return $"Hot reload mode: {mode} (DOTNET_MODIFIABLE_ASSEMBLIES={modifiableAssemblies}, __ASPNETCORE_BROWSER_TOOLS={browserTools})";
    }

    private static string ReadEnvOrDefault(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? "<unset>" : value;
    }
}
