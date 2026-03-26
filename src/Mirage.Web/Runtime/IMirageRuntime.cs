using Mirage.Core.Semantics;

namespace Mirage.Web.Runtime;

public interface IMirageRuntime : IDisposable
{
    void SetEventSink(IMirageEventSink? eventSink);

    void Render(NodeBase nextRoot);

    void SetViewport(double width, double height);

    // Reserved seam from early runtime experiments. Active features should
    // prefer explicit event payload mappings or dedicated runtime queries.
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

    bool TryOpenNodePicker(string nodeId);

    bool TrySetNodeFocus(string nodeId, bool isFocused);

    bool TryGetNodeTextSelection(string nodeId, out int selectionStart, out int selectionEnd);

    bool TrySetNodeTextSelection(string nodeId, int selectionStart, int selectionEnd);
}
