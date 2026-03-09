using Mirage.Core.Semantics;

namespace Mirage.Web.Dom;

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

    public int On(string eventName, Action callback) => DomApi.SubscribeEvent(Handle, eventName, callback);

    public static void Off(int subscriptionId) => DomApi.UnsubscribeEvent(subscriptionId);

    public void SetInputValue(string? value) => DomApi.SetInputValue(Handle, value);

    public string GetInputValue() => DomApi.GetInputValue(Handle);

    public void SetScrollPosition(double left, double top) => DomApi.SetScrollPosition(Handle, left, top);

    public (double Left, double Top) GetScrollPosition()
    {
        var values = DomApi.GetScrollPosition(Handle);
        return (values.Length > 0 ? values[0] : 0, values.Length > 1 ? values[1] : 0);
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
}
