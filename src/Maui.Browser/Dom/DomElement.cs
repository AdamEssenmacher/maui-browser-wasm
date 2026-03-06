namespace Maui.Browser.Dom;

/// <summary>
/// Minimal wrapper over an underlying DOM node handle.
/// This is the Browser platform's "native view".
/// </summary>
public sealed class DomElement
{
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

    public void Style(string name, string? value) => DomApi.SetStyle(Handle, name, value);

    public (double Width, double Height) ClientSize()
    {
        var s = DomApi.GetClientSize(Handle);
        return (s.Length > 0 ? s[0] : 0, s.Length > 1 ? s[1] : 0);
    }

    public void SetScrollPosition(double left, double top) => DomApi.SetScrollPosition(Handle, left, top);

    public (double Left, double Top) GetScrollPosition()
    {
        var values = DomApi.GetScrollPosition(Handle);
        return (values.Length > 0 ? values[0] : 0, values.Length > 1 ? values[1] : 0);
    }

    public bool Focus() => DomApi.FocusElement(Handle);

    public void Blur() => DomApi.BlurElement(Handle);

    public int On(string eventName, Action callback) => DomApi.SubscribeEvent(Handle, eventName, callback);

    public int OnEnter(Action callback) => DomApi.SubscribeEnterKey(Handle, callback);

    public static void Off(int subscriptionId) => DomApi.UnsubscribeEvent(subscriptionId);

    public void SetInputValue(string? value) => DomApi.SetInputValue(Handle, value);

    public string GetInputValue() => DomApi.GetInputValue(Handle);

    public void SetInputChecked(bool value) => DomApi.SetInputChecked(Handle, value);

    public bool GetInputChecked() => DomApi.GetInputChecked(Handle);

    public void SetPlaceholderColor(string? value) => DomApi.SetPlaceholderColor(Handle, value);

    public void SetSelectSelectedIndex(int value) => DomApi.SetSelectSelectedIndex(Handle, value);

    public int GetSelectSelectedIndex() => DomApi.GetSelectSelectedIndex(Handle);

    public static int SubscribeWindowResize(Action callback) => DomApi.SubscribeWindowResize(callback);

    public static void UnsubscribeWindowResize(int subscriptionId) => DomApi.UnsubscribeWindowResize(subscriptionId);

    public static (double Width, double Height) MeasureText(string? text, string? font)
    {
        var metrics = DomApi.MeasureText(text, font);
        return (metrics.Length > 0 ? metrics[0] : 0, metrics.Length > 1 ? metrics[1] : 0);
    }

    public bool OpenPicker() => DomApi.OpenPicker(Handle);

#if DEBUG
    public static DomDiagnostics GetDiagnostics()
    {
        var values = DomApi.GetDiagnostics();
        return new DomDiagnostics(
            values.Length > 0 ? values[0] : 0,
            values.Length > 1 ? values[1] : 0,
            values.Length > 2 ? values[2] : 0);
    }
#endif
}

#if DEBUG
public readonly record struct DomDiagnostics(int ElementHandleCount, int EventSubscriptionCount, int ResizeSubscriptionCount);
#endif
