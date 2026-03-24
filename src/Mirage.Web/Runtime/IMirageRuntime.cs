using Mirage.Core.Semantics;

namespace Mirage.Web.Runtime;

public interface IMirageRuntime : IDisposable
{
    void SetEventSink(IMirageEventSink? eventSink);

    void Render(NodeBase nextRoot);

    void SetViewport(double width, double height);

    string? TryGetNodeInputValue(string nodeId);

    bool TryGetNodeScrollPosition(string nodeId, out double left, out double top);

    bool TrySetNodeScrollPosition(string nodeId, double left, double top, out double actualLeft, out double actualTop);

    bool TryScrollNodeChildIntoView(
        string containerNodeId,
        string childNodeId,
        int scrollToPosition,
        bool isHorizontalOrientation,
        bool isAnimated,
        out double actualLeft,
        out double actualTop);

    bool TrySetNodeFocus(string nodeId, bool isFocused);
}
