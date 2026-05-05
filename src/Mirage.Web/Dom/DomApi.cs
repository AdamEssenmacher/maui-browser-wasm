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

    [JSImport("applyMutationBatch", Module)]
    internal static partial void ApplyMutationBatch(string json);

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

    [JSImport("getActiveMirageNodeId", Module)]
    internal static partial string? GetActiveMirageNodeId();

    [JSImport("setInputChecked", Module)]
    internal static partial void SetInputChecked(nint element, bool value);

    [JSImport("getInputChecked", Module)]
    internal static partial bool GetInputChecked(nint element);

    [JSImport("getInputSelection", Module)]
    internal static partial int[] GetInputSelection(nint element);

    [JSImport("setInputSelection", Module)]
    internal static partial bool SetInputSelection(nint element, int selectionStart, int selectionEnd);

    [JSImport("configurePullToRefresh", Module)]
    internal static partial int ConfigurePullToRefresh(
        nint ownerElement,
        nint sourceElement,
        double threshold,
        [JSMarshalAs<JSType.Function>] Action stateChangedCallback,
        [JSMarshalAs<JSType.Function>] Action triggerCallback);

    [JSImport("consumePullToRefreshStateJson", Module)]
    internal static partial string? ConsumePullToRefreshStateJson(nint ownerElement);

    [JSImport("unsubscribePullToRefresh", Module)]
    internal static partial void UnsubscribePullToRefresh(int subscriptionId);

    [JSImport("getTextInputContentHeight", Module)]
    internal static partial double GetTextInputContentHeight(nint element);

    [JSImport("configureSwipeView", Module)]
    internal static partial int ConfigureSwipeView(
        nint ownerElement,
        string configurationJson,
        [JSMarshalAs<JSType.Function>] Action swipeStartCallback,
        [JSMarshalAs<JSType.Function>] Action swipeChangeCallback,
        [JSMarshalAs<JSType.Function>] Action swipeEndCallback,
        [JSMarshalAs<JSType.Function>] Action swipeInvokeCallback);

    [JSImport("consumeSwipeViewEventPayload", Module)]
    internal static partial string? ConsumeSwipeViewEventPayload(nint ownerElement, string eventName);

    [JSImport("unsubscribeSwipeView", Module)]
    internal static partial void UnsubscribeSwipeView(int subscriptionId);

    [JSImport("requestSwipeViewOpen", Module)]
    internal static partial bool RequestSwipeViewOpen(nint ownerElement, string side, bool animated, bool emitEndEvent);

    [JSImport("requestSwipeViewClose", Module)]
    internal static partial bool RequestSwipeViewClose(nint ownerElement, bool animated);

    [JSImport("configureWebView", Module)]
    internal static partial int ConfigureWebView(
        nint element,
        string configurationJson,
        [JSMarshalAs<JSType.Function>] Action navigatingCallback,
        [JSMarshalAs<JSType.Function>] Action navigatedCallback,
        [JSMarshalAs<JSType.Function>] Action evalResultCallback);

    [JSImport("consumeWebViewEventPayload", Module)]
    internal static partial string? ConsumeWebViewEventPayload(nint element, string eventName);

    [JSImport("unsubscribeWebView", Module)]
    internal static partial void UnsubscribeWebView(int subscriptionId);

    [JSImport("requestWebViewGoBack", Module)]
    internal static partial bool RequestWebViewGoBack(nint element);

    [JSImport("requestWebViewNavigate", Module)]
    internal static partial bool RequestWebViewNavigate(
        nint element,
        [JSMarshalAs<JSType.Number>] long requestId,
        string url);

    [JSImport("cancelWebViewNavigation", Module)]
    internal static partial bool CancelWebViewNavigation(
        nint element,
        [JSMarshalAs<JSType.Number>] long requestId);

    [JSImport("requestWebViewGoForward", Module)]
    internal static partial bool RequestWebViewGoForward(nint element);

    [JSImport("requestWebViewReload", Module)]
    internal static partial bool RequestWebViewReload(nint element);

    [JSImport("evaluateWebViewScript", Module)]
    internal static partial bool EvaluateWebViewScript(
        nint element,
        [JSMarshalAs<JSType.Number>] long requestId,
        string script,
        bool captureResult);

    [JSImport("focusElement", Module)]
    internal static partial bool FocusElement(nint element);

    [JSImport("blurElement", Module)]
    internal static partial void BlurElement(nint element);

    [JSImport("openPicker", Module)]
    internal static partial bool OpenPicker(nint element);

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

    [JSImport("configureCanvas", Module)]
    internal static partial double[] ConfigureCanvas(nint element, double logicalWidth, double logicalHeight);

    [JSImport("renderCanvasCommands", Module)]
    internal static partial bool RenderCanvasCommands(nint element, string commandsJson);

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

    [JSImport("announceSemanticText", Module)]
    internal static partial void AnnounceSemanticText(string? text);

    [JSImport("openExternalUri", Module)]
    internal static partial bool OpenExternalUri(string uri, string target);

    [JSImport("setClipboardText", Module)]
    internal static partial Task<bool> SetClipboardTextAsync(string? text);

    [JSImport("getClipboardText", Module)]
    internal static partial Task<string?> GetClipboardTextAsync();

    [JSImport("hasClipboardText", Module)]
    internal static partial bool HasClipboardText();

    [JSImport("getDisplayInfo", Module)]
    internal static partial double[] GetDisplayInfo();

    [JSImport("getCurrentGeolocationJson", Module)]
    internal static partial Task<string?> GetCurrentGeolocationJsonAsync(bool enableHighAccuracy, double timeoutMs);

    [JSImport("startGeolocationWatch", Module)]
    internal static partial int StartGeolocationWatch(
        bool enableHighAccuracy,
        double minimumTimeMs,
        [JSMarshalAs<JSType.Function>] Action changedCallback,
        [JSMarshalAs<JSType.Function>] Action failedCallback);

    [JSImport("consumeGeolocationWatchPayloadJson", Module)]
    internal static partial string? ConsumeGeolocationWatchPayloadJson(int subscriptionId);

    [JSImport("unsubscribeGeolocationWatch", Module)]
    internal static partial void UnsubscribeGeolocationWatch(int subscriptionId);

    [JSImport("speakText", Module)]
    internal static partial Task<bool> SpeakTextAsync(string text, double rate, double pitch, double volume);

    [JSImport("cancelSpeech", Module)]
    internal static partial void CancelSpeech();

    [JSImport("getLocalStorageItem", Module)]
    internal static partial string? GetLocalStorageItem(string key);

    [JSImport("setLocalStorageItem", Module)]
    internal static partial void SetLocalStorageItem(string key, string value);

    [JSImport("removeLocalStorageItem", Module)]
    internal static partial void RemoveLocalStorageItem(string key);

    [JSImport("containsLocalStorageKey", Module)]
    internal static partial bool ContainsLocalStorageKey(string key);

    [JSImport("clearLocalStorageByPrefix", Module)]
    internal static partial void ClearLocalStorageByPrefix(string prefix);

    [JSImport("subscribePopState", Module)]
    internal static partial int SubscribePopState([JSMarshalAs<JSType.Function>] Action callback);

    [JSImport("unsubscribePopState", Module)]
    internal static partial void UnsubscribePopState(int subscriptionId);

    [JSImport("getPreferredColorScheme", Module)]
    internal static partial string GetPreferredColorScheme();

    [JSImport("getViewportSize", Module)]
    internal static partial double[] GetViewportSize();

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
