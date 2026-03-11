using Mirage.Core.Diagnostics;
using Mirage.Web.Runtime;
using Microsoft.Maui.Dispatching;

namespace Mirage.Maui.Handlers;

internal sealed class MirageRenderCoordinator : IMirageRenderCoordinator
{
    private readonly IDispatcher _dispatcher;
    private readonly Dictionary<string, IMirageRuntimeEventTarget> _eventTargetsByNodeId = new(StringComparer.Ordinal);
    private readonly HashSet<MiragePlatformView> _registeredNodes = new(ReferenceEqualityComparer.Instance);
    private readonly object _batchSync = new();
    private int _nextNodeId = 1;

    private IMirageRuntime? _runtime;
    private MiragePlatformView? _root;
    private bool _renderPending;
    private int _renderBatchDepth;
    private bool _renderRequestedDuringBatch;
    private long _pendingRenderSequence;

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
        _renderBatchDepth = 0;
        _renderRequestedDuringBatch = false;
        _pendingRenderSequence = 0;
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
        if (_runtime is null || _root is null)
        {
            return;
        }

        lock (_batchSync)
        {
            if (_renderBatchDepth > 0)
            {
                _renderRequestedDuringBatch = true;
                return;
            }
        }

        if (_renderPending)
        {
            return;
        }

        _renderPending = true;
        _pendingRenderSequence = MirageDiagnostics.IsEnabled
            ? MirageDiagnostics.NextRenderSequence()
            : 0;
        _ = _dispatcher.Dispatch(RenderPending);
    }

    public IDisposable BeginRenderBatch()
    {
        lock (_batchSync)
        {
            _renderBatchDepth++;
        }

        return new RenderBatchScope(this);
    }

    public bool TryGetNodeScrollPosition(string nodeId, out double left, out double top)
    {
        if (_runtime is null)
        {
            left = 0;
            top = 0;
            return false;
        }

        return _runtime.TryGetNodeScrollPosition(nodeId, out left, out top);
    }

    public bool TrySetNodeScrollPosition(string nodeId, double left, double top, out double actualLeft, out double actualTop)
    {
        if (_runtime is null)
        {
            actualLeft = 0;
            actualTop = 0;
            return false;
        }

        return _runtime.TrySetNodeScrollPosition(nodeId, left, top, out actualLeft, out actualTop);
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
        var renderSequence = _pendingRenderSequence;
        _pendingRenderSequence = 0;

        if (_runtime is null || _root is null)
        {
            return;
        }

        _runtime.Render(_root.ToNode());
        if (renderSequence > 0)
        {
            MirageDiagnostics.MarkRenderCompleted(renderSequence, MirageDiagnostics.GetTimestamp());
        }
    }

    private void EndRenderBatch()
    {
        var shouldFlushRender = false;
        lock (_batchSync)
        {
            if (_renderBatchDepth == 0)
            {
                return;
            }

            _renderBatchDepth--;
            if (_renderBatchDepth == 0 && _renderRequestedDuringBatch)
            {
                _renderRequestedDuringBatch = false;
                shouldFlushRender = true;
            }
        }

        if (shouldFlushRender)
        {
            RequestRender();
        }
    }

    private sealed class RenderBatchScope : IDisposable
    {
        private MirageRenderCoordinator? _owner;

        public RenderBatchScope(MirageRenderCoordinator owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            var owner = Interlocked.Exchange(ref _owner, null);
            owner?.EndRenderBatch();
        }
    }
}
