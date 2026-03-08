namespace Mirage.Maui.Handlers;

public interface IMirageRuntimeEventTarget
{
    void OnRuntimeEvent(string eventName, string? payload);
}
