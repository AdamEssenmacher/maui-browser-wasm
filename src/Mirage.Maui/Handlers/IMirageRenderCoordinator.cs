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
