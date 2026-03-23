using System.Runtime.InteropServices.JavaScript;
using System.Threading;

namespace Mirage.Web.Dom;

internal static partial class DomApi
{
    private const string Module = "mirage-dom";
    private const string ModuleUrl = "../_framework/mirage-dom.js";
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

    [JSImport("getAttr", Module)]
    internal static partial string? GetAttr(nint element, string name);

    [JSImport("setStyle", Module)]
    internal static partial void SetStyle(nint element, string name, string? value);

    [JSImport("addClass", Module)]
    internal static partial void AddClass(nint element, string className);

    [JSImport("removeClass", Module)]
    internal static partial void RemoveClass(nint element, string className);

    [JSImport("setPropertyString", Module)]
    internal static partial void SetPropertyString(nint element, string name, string? value);

    [JSImport("setPropertyBool", Module)]
    internal static partial void SetPropertyBool(nint element, string name, bool value);

    [JSImport("setPropertyNumber", Module)]
    internal static partial void SetPropertyNumber(nint element, string name, double value);

    [JSImport("setPropertyNull", Module)]
    internal static partial void SetPropertyNull(nint element, string name);

    [JSImport("setPropertyJson", Module)]
    internal static partial void SetPropertyJson(nint element, string name, string? json);

    [JSImport("removeProperty", Module)]
    internal static partial void RemoveProperty(nint element, string name);

    [JSImport("subscribeEvent", Module)]
    internal static partial int SubscribeEvent(nint element, string eventName, [JSMarshalAs<JSType.Function>] Action callback);

    [JSImport("subscribeEventWithPayload", Module)]
    internal static partial int SubscribeEventWithPayload(
        nint element,
        string eventName,
        string payloadKind,
        [JSMarshalAs<JSType.Function>] Action callback);

    [JSImport("consumeTrackedEventPayload", Module)]
    internal static partial string? ConsumeTrackedEventPayload(nint element, string payloadKind);

    [JSImport("unsubscribeEvent", Module)]
    internal static partial void UnsubscribeEvent(int subscriptionId);

    [JSImport("setInputValue", Module)]
    internal static partial void SetInputValue(nint element, string? value);

    [JSImport("getInputValue", Module)]
    internal static partial string GetInputValue(nint element);

    [JSImport("setInputChecked", Module)]
    internal static partial void SetInputChecked(nint element, bool value);

    [JSImport("getInputChecked", Module)]
    internal static partial bool GetInputChecked(nint element);

    [JSImport("setScrollPosition", Module)]
    internal static partial void SetScrollPosition(nint element, double left, double top);

    [JSImport("getScrollPosition", Module)]
    internal static partial double[] GetScrollPosition(nint element);

    [JSImport("scrollChildIntoView", Module)]
    internal static partial double[] ScrollChildIntoView(
        nint containerElement,
        nint childElement,
        int scrollToPosition,
        bool isHorizontalOrientation,
        bool isAnimated);

    [JSImport("subscribeWindowResize", Module)]
    internal static partial int SubscribeWindowResize([JSMarshalAs<JSType.Function>] Action callback);

    [JSImport("unsubscribeWindowResize", Module)]
    internal static partial void UnsubscribeWindowResize(int subscriptionId);

    [JSImport("getClientSize", Module)]
    internal static partial double[] GetClientSize(nint element);

    [JSImport("measureText", Module)]
    internal static partial double[] MeasureText(string? text, string? font);

    [JSImport("scrollToAutomationId", Module)]
    internal static partial bool ScrollToAutomationId(nint rootElement, string automationId, bool smooth);

    [JSImport("getLocationPathAndQueryAndHash", Module)]
    internal static partial string GetLocationPathAndQueryAndHash();

    [JSImport("pushHistoryState", Module)]
    internal static partial void PushHistoryState(string path);

    [JSImport("replaceHistoryState", Module)]
    internal static partial void ReplaceHistoryState(string path);

    [JSImport("getDocumentTitle", Module)]
    internal static partial string GetDocumentTitle();

    [JSImport("setDocumentTitle", Module)]
    internal static partial void SetDocumentTitle(string? title);

    [JSImport("subscribePopState", Module)]
    internal static partial int SubscribePopState([JSMarshalAs<JSType.Function>] Action callback);

    [JSImport("unsubscribePopState", Module)]
    internal static partial void UnsubscribePopState(int subscriptionId);

    [JSImport("getPreferredColorScheme", Module)]
    internal static partial string GetPreferredColorScheme();

    [JSImport("subscribePreferredColorSchemeChanged", Module)]
    internal static partial int SubscribePreferredColorSchemeChanged([JSMarshalAs<JSType.Function>] Action callback);

    [JSImport("unsubscribePreferredColorSchemeChanged", Module)]
    internal static partial void UnsubscribePreferredColorSchemeChanged(int subscriptionId);

    [JSImport("isDiagnosticsEnabled", Module)]
    internal static partial bool IsDiagnosticsEnabled();

    [JSImport("fetchText", Module)]
    internal static partial Task<string?> FetchTextAsync(string path);

    [JSImport("fetchTextWithStatusJson", Module)]
    internal static partial Task<string?> FetchTextWithStatusJsonAsync(string path);

    [JSImport("createObjectUrlFromBase64", Module)]
    internal static partial string? CreateObjectUrlFromBase64(string base64, string? contentType);

    [JSImport("revokeObjectUrl", Module)]
    internal static partial void RevokeObjectUrl(string objectUrl);

    [JSImport("installHotReloadHintBridge", Module)]
    internal static partial void InstallHotReloadHintBridge();

    [JSImport("drainHotReloadHintsJson", Module)]
    internal static partial string DrainHotReloadHintsJson();
}
