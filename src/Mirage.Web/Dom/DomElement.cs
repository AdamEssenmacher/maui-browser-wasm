using Mirage.Core.Semantics;
using System.Text.Json;

namespace Mirage.Web.Dom;

public sealed class DomElement
{
    private static readonly JsonSerializerOptions FetchPayloadSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DomElement(nint handle) => Handle = handle;

    public nint Handle { get; }

    public static Task EnsureInteropReadyAsync() => DomApi.EnsureModuleLoadedAsync();

    public static DomElement RootById(string id) => new(DomApi.GetRootById(id));

    public static DomElement Create(string tag) => new(DomApi.CreateElement(tag));

    public void Append(DomElement child) => DomApi.AppendChild(Handle, child.Handle);

    public void Insert(int index, DomElement child) => DomApi.InsertChildAt(Handle, child.Handle, index);

    public void Remove(DomElement child) => DomApi.RemoveChild(Handle, child.Handle);

    public void ClearChildren() => DomApi.ClearChildren(Handle);

    public void Release() => DomApi.ReleaseElement(Handle);

    public void Text(string? text) => DomApi.SetText(Handle, text);

    public void Attr(string name, string? value) => DomApi.SetAttr(Handle, name, value);

    public string? GetAttr(string name) => DomApi.GetAttr(Handle, name);

    public void Style(string name, string? value) => DomApi.SetStyle(Handle, name, value);

    public void AddClass(string className) => DomApi.AddClass(Handle, className);

    public void RemoveClass(string className) => DomApi.RemoveClass(Handle, className);

    public void SetProperty(string name, MirageValue value)
    {
        switch (value.Kind)
        {
            case MirageValueKind.Null:
                DomApi.SetPropertyNull(Handle, name);
                break;
            case MirageValueKind.String:
                DomApi.SetPropertyString(Handle, name, value.GetStringOrNull());
                break;
            case MirageValueKind.Bool:
                if (!value.TryGetBool(out var boolValue))
                {
                    DomApi.SetPropertyNull(Handle, name);
                    break;
                }

                DomApi.SetPropertyBool(Handle, name, boolValue);
                break;
            case MirageValueKind.Number:
                if (!value.TryGetNumber(out var numberValue))
                {
                    DomApi.SetPropertyNull(Handle, name);
                    break;
                }

                DomApi.SetPropertyNumber(Handle, name, numberValue);
                break;
            case MirageValueKind.Json:
                DomApi.SetPropertyJson(Handle, name, value.ToJsonString());
                break;
            default:
                DomApi.SetPropertyNull(Handle, name);
                break;
        }
    }

    public void RemoveProperty(string name) => DomApi.RemoveProperty(Handle, name);

    internal static void ApplyMutationBatch(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        DomApi.ApplyMutationBatch(json);
    }

    public int On(string eventName, Action callback) => DomApi.SubscribeEvent(Handle, eventName, callback);

    public int On(string eventName, string payloadKind, Action<string?> callback)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadKind);
        ArgumentNullException.ThrowIfNull(callback);

        return DomApi.SubscribeEventWithPayload(Handle, eventName, payloadKind, () =>
        {
            callback(DomApi.ConsumeTrackedEventPayload(Handle, payloadKind));
        });
    }

    public static void Off(int subscriptionId) => DomApi.UnsubscribeEvent(subscriptionId);

    public void SetInputValue(string? value) => DomApi.SetInputValue(Handle, value);

    public string GetInputValue() => DomApi.GetInputValue(Handle);

    public static string? GetActiveMirageNodeId() => DomApi.GetActiveMirageNodeId();

    public void SetInputChecked(bool value) => DomApi.SetInputChecked(Handle, value);

    public bool GetInputChecked() => DomApi.GetInputChecked(Handle);

    public bool TryGetInputSelection(out int selectionStart, out int selectionEnd)
    {
        var values = DomApi.GetInputSelection(Handle);
        if (values.Length < 2)
        {
            selectionStart = 0;
            selectionEnd = 0;
            return false;
        }

        selectionStart = values[0];
        selectionEnd = values[1];
        return true;
    }

    public bool TrySetInputSelection(int selectionStart, int selectionEnd)
    {
        return DomApi.SetInputSelection(Handle, selectionStart, selectionEnd);
    }

    public bool Focus() => DomApi.FocusElement(Handle);

    public void Blur() => DomApi.BlurElement(Handle);

    public bool OpenPicker() => DomApi.OpenPicker(Handle);

    public void SetScrollPosition(double left, double top) => DomApi.SetScrollPosition(Handle, left, top);

    public (double Left, double Top) GetScrollPosition()
    {
        var values = DomApi.GetScrollPosition(Handle);
        return (values.Length > 0 ? values[0] : 0, values.Length > 1 ? values[1] : 0);
    }

    public (double Left, double Top) ScrollChildIntoView(
        DomElement child,
        int scrollToPosition,
        bool isHorizontalOrientation,
        bool isAnimated)
    {
        ArgumentNullException.ThrowIfNull(child);

        var values = DomApi.ScrollChildIntoView(
            Handle,
            child.Handle,
            scrollToPosition,
            isHorizontalOrientation,
            isAnimated);
        return (values.Length > 0 ? values[0] : 0, values.Length > 1 ? values[1] : 0);
    }

    public (double LogicalWidth, double LogicalHeight, double DisplayScale) ConfigureCanvas(double logicalWidth, double logicalHeight)
    {
        var values = DomApi.ConfigureCanvas(Handle, logicalWidth, logicalHeight);
        return (
            values.Length > 0 ? values[0] : 0,
            values.Length > 1 ? values[1] : 0,
            values.Length > 2 ? values[2] : 1);
    }

    public bool RenderCanvasCommands(string commandsJson)
    {
        if (string.IsNullOrWhiteSpace(commandsJson))
        {
            return false;
        }

        return DomApi.RenderCanvasCommands(Handle, commandsJson);
    }

    public static int SubscribeWindowResize(Action callback) => DomApi.SubscribeWindowResize(callback);

    public static void UnsubscribeWindowResize(int subscriptionId) => DomApi.UnsubscribeWindowResize(subscriptionId);

    public (double Width, double Height) ClientSize()
    {
        var values = DomApi.GetClientSize(Handle);
        return (values.Length > 0 ? values[0] : 0, values.Length > 1 ? values[1] : 0);
    }

    public static (double Width, double Height) MeasureText(string? text, string? font)
    {
        var values = DomApi.MeasureText(text, font);
        return (values.Length > 0 ? values[0] : 0, values.Length > 1 ? values[1] : 0);
    }

    public bool ScrollToAutomationId(string automationId, bool smooth = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        return DomApi.ScrollToAutomationId(Handle, automationId, smooth);
    }

    public static string GetLocationPathAndQueryAndHash() => DomApi.GetLocationPathAndQueryAndHash();

    public static void PushHistoryState(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        DomApi.PushHistoryState(path);
    }

    public static void ReplaceHistoryState(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        DomApi.ReplaceHistoryState(path);
    }

    public static string GetDocumentTitle() => DomApi.GetDocumentTitle();

    public static void SetDocumentTitle(string? title) => DomApi.SetDocumentTitle(title);

    public static string? GetLocalStorageItem(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return DomApi.GetLocalStorageItem(key);
    }

    public static void SetLocalStorageItem(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        DomApi.SetLocalStorageItem(key, value);
    }

    public static void RemoveLocalStorageItem(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        DomApi.RemoveLocalStorageItem(key);
    }

    public static bool ContainsLocalStorageKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return DomApi.ContainsLocalStorageKey(key);
    }

    public static void ClearLocalStorageByPrefix(string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        DomApi.ClearLocalStorageByPrefix(prefix);
    }

    public static int SubscribePopState(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return DomApi.SubscribePopState(callback);
    }

    public static void UnsubscribePopState(int subscriptionId) => DomApi.UnsubscribePopState(subscriptionId);

    public static string GetPreferredColorScheme() => DomApi.GetPreferredColorScheme();

    public static (double Width, double Height) GetViewportSize()
    {
        var values = DomApi.GetViewportSize();
        return (values.Length > 0 ? values[0] : 0, values.Length > 1 ? values[1] : 0);
    }

    public static int SubscribePreferredColorSchemeChanged(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return DomApi.SubscribePreferredColorSchemeChanged(callback);
    }

    public static void UnsubscribePreferredColorSchemeChanged(int subscriptionId) =>
        DomApi.UnsubscribePreferredColorSchemeChanged(subscriptionId);

    public static bool IsDiagnosticsEnabled() => DomApi.IsDiagnosticsEnabled();

    public static Task<string?> FetchTextAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return DomApi.FetchTextAsync(path);
    }

    public static async Task<FetchTextResult> FetchTextWithStatusAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var json = await DomApi.FetchTextWithStatusJsonAsync(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            return FetchTextResult.Failure(0);
        }

        try
        {
            var payload = JsonSerializer.Deserialize<FetchTextPayload>(json, FetchPayloadSerializerOptions);
            if (payload is null)
            {
                return FetchTextResult.Failure(0);
            }

            return payload.Ok
                ? FetchTextResult.Success(payload.Status, payload.Text)
                : FetchTextResult.Failure(payload.Status);
        }
        catch
        {
            return FetchTextResult.Failure(0);
        }
    }

    public static string? CreateObjectUrlFromBase64(string base64, string? contentType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(base64);
        return DomApi.CreateObjectUrlFromBase64(base64, contentType);
    }

    public static void RevokeObjectUrl(string objectUrl)
    {
        if (string.IsNullOrWhiteSpace(objectUrl))
        {
            return;
        }

        DomApi.RevokeObjectUrl(objectUrl);
    }

    public static void InstallHotReloadHintBridge() => DomApi.InstallHotReloadHintBridge();

    public static string DrainHotReloadHintsJson() => DomApi.DrainHotReloadHintsJson();

    public readonly record struct FetchTextResult(bool IsSuccess, int StatusCode, string? Text)
    {
        public static FetchTextResult Success(int statusCode, string? text) => new(true, statusCode, text);

        public static FetchTextResult Failure(int statusCode) => new(false, statusCode, null);
    }

    private sealed class FetchTextPayload
    {
        public bool Ok { get; set; }

        public int Status { get; set; }

        public string? Text { get; set; }
    }
}
