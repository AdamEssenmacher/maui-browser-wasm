using Mirage.Core.Semantics;

namespace Mirage.Web.Runtime;

public interface IMirageRuntime : IDisposable
{
    void SetEventSink(IMirageEventSink? eventSink);

    void Render(NodeBase nextRoot);

    void SetViewport(double width, double height);

    string? TryGetNodeInputValue(string nodeId);
}
