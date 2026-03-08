using Mirage.Web.Runtime;
using Microsoft.Maui.Dispatching;

namespace Mirage.Maui.Handlers;

internal sealed class MirageRenderCoordinator : IMirageRenderCoordinator
{
    private readonly IDispatcher _dispatcher;
    private readonly Dictionary<string, IMirageRuntimeEventTarget> _eventTargetsByNodeId = new(StringComparer.Ordinal);
    private readonly HashSet<MiragePlatformView> _registeredNodes = new(ReferenceEqualityComparer.Instance);
    private int _nextNodeId = 1;

    private IMirageRuntime? _runtime;
    private MiragePlatformView? _root;
    private bool _renderPending;

    public MirageRenderCoordinator(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void AttachRuntime(IMirageRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        DetachRuntime();
        _runtime = runtime;
        _runtime.SetEventSink(this);
    }

    public void DetachRuntime()
    {
        if (_runtime is not null)
        {
            _runtime.SetEventSink(null);
        }

        _runtime = null;
        _root = null;
        _renderPending = false;
        _eventTargetsByNodeId.Clear();
        _registeredNodes.Clear();
    }

    public void Register(MiragePlatformView platformView, IMirageRuntimeEventTarget? eventTarget)
    {
        ArgumentNullException.ThrowIfNull(platformView);

        if (string.IsNullOrWhiteSpace(platformView.NodeId))
        {
            platformView.AssignNodeId($"node-{_nextNodeId++:D4}");
        }

        _registeredNodes.Add(platformView);

        if (eventTarget is not null)
        {
            _eventTargetsByNodeId[platformView.NodeId] = eventTarget;
        }
        else
        {
            _eventTargetsByNodeId.Remove(platformView.NodeId);
        }
    }

    public void Unregister(MiragePlatformView platformView)
    {
        ArgumentNullException.ThrowIfNull(platformView);

        _registeredNodes.Remove(platformView);
        if (!string.IsNullOrWhiteSpace(platformView.NodeId))
        {
            _eventTargetsByNodeId.Remove(platformView.NodeId);
        }
    }

    public void SetRoot(MiragePlatformView? root)
    {
        _root = root;
        RequestRender();
    }

    public void RequestRender()
    {
        if (_runtime is null || _root is null || _renderPending)
        {
            return;
        }

        _renderPending = true;
        _ = _dispatcher.Dispatch(RenderPending);
    }

    public void OnEvent(string nodeId, string eventName, string? payload)
    {
        if (!_eventTargetsByNodeId.TryGetValue(nodeId, out var target))
        {
            return;
        }

        target.OnRuntimeEvent(eventName, payload);
    }

    private void RenderPending()
    {
        _renderPending = false;

        if (_runtime is null || _root is null)
        {
            return;
        }

        _runtime.Render(_root.ToNode());
    }
}
