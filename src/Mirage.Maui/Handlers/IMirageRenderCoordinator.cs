using Mirage.Web.Runtime;

namespace Mirage.Maui.Handlers;

public interface IMirageRenderCoordinator : IMirageEventSink
{
    void AttachRuntime(IMirageRuntime runtime);

    void DetachRuntime();

    void Register(MiragePlatformView platformView, IMirageRuntimeEventTarget? eventTarget);

    void Unregister(MiragePlatformView platformView);

    void SetRoot(MiragePlatformView? root);

    void RequestRender();

    bool DispatchDelayed(TimeSpan delay, Action action);

    IDisposable BeginRenderBatch();

    bool TryGetNodeInputValue(string nodeId, out string? value);

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

    // Mirrors IMirageRuntime.TryRequestNodePickerOpen: true means the runtime accepted
    // the request via the strongest available browser signal, not that popup
    // visibility is directly observable.
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

    bool TryRequestNodeSwipeViewOpen(string ownerNodeId, string side, bool animated) => false;

    bool TryRequestNodeSwipeViewClose(string ownerNodeId, bool animated) => false;

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
