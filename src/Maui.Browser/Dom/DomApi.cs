using System.Runtime.InteropServices.JavaScript;
using System.Threading;

namespace Maui.Browser.Dom;

/// <summary>
/// JSImport-only layer. All direct JS interop must go through this file.
/// </summary>
internal static partial class DomApi
{
    // JSImport uses a logical module name; the actual URL is loaded via JSHost.ImportAsync.
    private const string Module = "maui-dom";
    // Resolve relative to dotnet.js in _framework so subpath hosting also works.
    private const string ModuleUrl = "../_framework/dom.js";
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
        // JSImport attributes refer to this module name, so import it once before first call.
        s_moduleInstance = await JSHost.ImportAsync(Module, ModuleUrl);
    }

    [JSImport("getRootById", Module)]
    internal static partial nint GetRootById(string id);

    [JSImport("createElement", Module)]
    internal static partial nint CreateElement(string tag);

    [JSImport("appendChild", Module)]
    internal static partial void AppendChild(nint parent, nint child);

    [JSImport("insertChildAt", Module)]
    internal static partial void InsertChildAt(nint parent, nint child, int index);

    [JSImport("removeChild", Module)]
    internal static partial void RemoveChild(nint parent, nint child);

    [JSImport("clearChildren", Module)]
    internal static partial void ClearChildren(nint parent);

    [JSImport("releaseElement", Module)]
    internal static partial void ReleaseElement(nint element);

    [JSImport("setText", Module)]
    internal static partial void SetText(nint element, string? text);

    [JSImport("setAttr", Module)]
    internal static partial void SetAttr(nint element, string name, string? value);

    [JSImport("setStyle", Module)]
    internal static partial void SetStyle(nint element, string name, string? value);

    [JSImport("getClientSize", Module)]
    internal static partial double[] GetClientSize(nint element);

    [JSImport("setScrollPosition", Module)]
    internal static partial void SetScrollPosition(nint element, double left, double top);

    [JSImport("getScrollPosition", Module)]
    internal static partial double[] GetScrollPosition(nint element);

    [JSImport("focusElement", Module)]
    internal static partial bool FocusElement(nint element);

    [JSImport("blurElement", Module)]
    internal static partial void BlurElement(nint element);

    [JSImport("subscribeEvent", Module)]
    internal static partial int SubscribeEvent(nint element, string eventName, [JSMarshalAs<JSType.Function>] Action callback);

    [JSImport("unsubscribeEvent", Module)]
    internal static partial void UnsubscribeEvent(int subscriptionId);

    [JSImport("subscribeEnterKey", Module)]
    internal static partial int SubscribeEnterKey(nint element, [JSMarshalAs<JSType.Function>] Action callback);

    [JSImport("setInputValue", Module)]
    internal static partial void SetInputValue(nint element, string? value);

    [JSImport("getInputValue", Module)]
    internal static partial string GetInputValue(nint element);

    [JSImport("setInputChecked", Module)]
    internal static partial void SetInputChecked(nint element, bool value);

    [JSImport("getInputChecked", Module)]
    internal static partial bool GetInputChecked(nint element);

    [JSImport("setPlaceholderColor", Module)]
    internal static partial void SetPlaceholderColor(nint element, string? value);

    [JSImport("setSelectSelectedIndex", Module)]
    internal static partial void SetSelectSelectedIndex(nint element, int value);

    [JSImport("getSelectSelectedIndex", Module)]
    internal static partial int GetSelectSelectedIndex(nint element);

    [JSImport("subscribeWindowResize", Module)]
    internal static partial int SubscribeWindowResize([JSMarshalAs<JSType.Function>] Action callback);

    [JSImport("unsubscribeWindowResize", Module)]
    internal static partial void UnsubscribeWindowResize(int subscriptionId);

    [JSImport("subscribeWindowPointerRelease", Module)]
    internal static partial int SubscribeWindowPointerRelease([JSMarshalAs<JSType.Function>] Action callback);

    [JSImport("unsubscribeWindowPointerRelease", Module)]
    internal static partial void UnsubscribeWindowPointerRelease(int subscriptionId);

    [JSImport("measureText", Module)]
    internal static partial double[] MeasureText(string? text, string? font);

    [JSImport("openPicker", Module)]
    internal static partial bool OpenPicker(nint element);

#if DEBUG
    [JSImport("getDiagnostics", Module)]
    internal static partial int[] GetDiagnostics();
#endif
}
