using System.Runtime.InteropServices.JavaScript;
using System.Threading;

namespace Mirage.Provider.LeafletMaps;

internal static partial class LeafletMapInterop
{
    private const string Module = "mirage-leaflet-provider";
    private const string ModuleUrl = "../_framework/mirage-leaflet-provider.js";
    private static Task? s_moduleImportTask;

    internal static Task EnsureModuleLoadedAsync()
    {
        var existing = Volatile.Read(ref s_moduleImportTask);
        if (existing is not null)
        {
            return existing;
        }

        var created = JSHost.ImportAsync(Module, ModuleUrl);
        var prior = Interlocked.CompareExchange(ref s_moduleImportTask, created, null);
        return prior ?? created;
    }

    [JSImport("ensureLeafletReadyJson", Module)]
    internal static partial Task<string?> EnsureLeafletReadyJsonAsync(string optionsJson);

    [JSImport("initializeMapJson", Module)]
    internal static partial string InitializeMapJson(string nodeId, string optionsJson);

    [JSImport("applyMapStateJson", Module)]
    internal static partial string ApplyMapStateJson(string nodeId, string stateJson);

    [JSImport("disposeMap", Module)]
    internal static partial void DisposeMap(string nodeId);

    [JSImport("showMapError", Module)]
    internal static partial void ShowMapError(string nodeId, string errorCode, string message);
}
