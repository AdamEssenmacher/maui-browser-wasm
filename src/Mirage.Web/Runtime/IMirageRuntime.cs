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

    bool TryGetActiveNodeId(out string? nodeId);

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

    // Browser hosts do not expose a definitive "picker popup is visible" callback.
    // True here means the runtime accepted the open request using the strongest
    // signal available on the current platform (for example, successful showPicker()).
    bool TryRequestNodePickerOpen(string nodeId);

    bool TrySetNodeFocus(string nodeId, bool isFocused);

    bool TrySetNodeFocus(string nodeId, bool isFocused, bool suppressNextFocusEvent)
    {
        return TrySetNodeFocus(nodeId, isFocused);
    }

    bool TryGetNodeTextSelection(string nodeId, out int selectionStart, out int selectionEnd);

    bool TrySetNodeTextSelection(string nodeId, int selectionStart, int selectionEnd);

    bool TryConfigureNodePullToRefresh(string ownerNodeId, string sourceNodeId, bool enabled, double threshold) => false;

    bool TryConfigureNodeSwipeView(string ownerNodeId, bool enabled, string configurationJson) => false;

    bool TryRequestNodeSwipeViewOpen(string ownerNodeId, string side, bool animated, bool emitEndEvent = true) => false;

    bool TryRequestNodeSwipeViewClose(string ownerNodeId, bool animated) => false;

    bool TryConfigureNodeWebView(string nodeId, bool enabled, string configurationJson) => false;

    bool TryRequestNodeWebViewNavigate(string nodeId, string url) => false;

    bool TryRequestNodeWebViewGoBack(string nodeId) => false;

    bool TryRequestNodeWebViewGoForward(string nodeId) => false;

    bool TryRequestNodeWebViewReload(string nodeId) => false;

    bool TryEvaluateNodeWebViewScript(string nodeId, long requestId, string script, bool captureResult) => false;

    bool TryConfigureNodeCanvas(string nodeId, double logicalWidth, double logicalHeight, out double displayScale)
    {
        displayScale = 1;
        return false;
    }

    bool TryRenderNodeCanvas(string nodeId, string commandsJson) => false;

    bool TryGetNodeTextContentHeight(string nodeId, out double height)
    {
        height = 0;
        return false;
    }
}
