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

    bool TryGetNodeScrollPosition(string nodeId, out double left, out double top);

    bool TrySetNodeScrollPosition(string nodeId, double left, double top, out double actualLeft, out double actualTop);
}
