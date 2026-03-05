using System.Runtime.InteropServices.JavaScript;
using System.Threading;

namespace Maui.Browser.Sample.Demo;

internal static partial class BrowserPageInterop
{
    private const string Module = "maui-browser-sample-dev";
    private const string ModuleUrl = "../_framework/devhost-interop.js";

    private static Task? s_moduleImportTask;
    private static JSObject? s_moduleInstance;

    internal static Task EnsureModuleLoadedAsync()
    {
        var existing = Volatile.Read(ref s_moduleImportTask);
        if (existing is not null)
        {
            return existing;
        }

        var created = ImportModuleCoreAsync();
        var prior = Interlocked.CompareExchange(ref s_moduleImportTask, created, null);
        return prior ?? created;
    }

    private static async Task ImportModuleCoreAsync()
    {
        s_moduleInstance = await JSHost.ImportAsync(Module, ModuleUrl);
    }

    [JSImport("getLocationOrigin", Module)]
    internal static partial string GetLocationOrigin();

    [JSImport("forceReload", Module)]
    internal static partial void ForceReload();
}
