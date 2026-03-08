namespace Mirage.Web.Runtime;

public interface IMirageEventSink
{
    void OnEvent(string nodeId, string eventName, string? payload);
}
