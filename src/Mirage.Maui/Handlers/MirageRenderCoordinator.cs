using Mirage.Core.Diagnostics;
using Mirage.Core.Semantics;
using Mirage.Web.Runtime;
using Microsoft.Maui.Dispatching;

namespace Mirage.Maui.Handlers;

internal sealed class MirageRenderCoordinator : IMirageRenderCoordinator
{
    private static readonly TimeSpan RenderDispatchDelay = TimeSpan.FromMilliseconds(1);
    private readonly IDispatcher _dispatcher;
    private readonly Dictionary<string, IMirageRuntimeEventTarget> _eventTargetsByNodeId = new(StringComparer.Ordinal);
    private readonly HashSet<MiragePlatformView> _registeredNodes = new(ReferenceEqualityComparer.Instance);
    private Dictionary<string, NodeSnapshot> _snapshotByNodeId = new(StringComparer.Ordinal);
    private readonly object _batchSync = new();
    private int _nextNodeId = 1;

    private IMirageRuntime? _runtime;
    private MiragePlatformView? _root;
    private NodeBase? _cachedRootNode;
    private bool _renderPending;
    private bool _renderInProgress;
    private int _renderBatchDepth;
    private bool _renderRequestedDuringBatch;
    private bool _renderRequestedDuringRender;
    private long _pendingRenderSequence;
    private long _coalescedRenderRequestCount;
    private long _renderPassCount;
    private long _fullMaterializationCount;
    private long _scopedMaterializationCount;
    private long _cachedRootStructureRevision;

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
        _cachedRootNode = null;
        _renderPending = false;
        _renderInProgress = false;
        _renderBatchDepth = 0;
        _renderRequestedDuringBatch = false;
        _renderRequestedDuringRender = false;
        _pendingRenderSequence = 0;
        _snapshotByNodeId = new Dictionary<string, NodeSnapshot>(StringComparer.Ordinal);
        _cachedRootStructureRevision = 0;
        _coalescedRenderRequestCount = 0;
        _renderPassCount = 0;
        _fullMaterializationCount = 0;
        _scopedMaterializationCount = 0;
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

        if (_renderInProgress)
        {
            _renderRequestedDuringRender = true;
            Interlocked.Increment(ref _coalescedRenderRequestCount);
            return;
        }

        lock (_batchSync)
        {
            if (_renderBatchDepth > 0)
            {
                _renderRequestedDuringBatch = true;
                Interlocked.Increment(ref _coalescedRenderRequestCount);
                return;
            }
        }

        if (_renderPending)
        {
            Interlocked.Increment(ref _coalescedRenderRequestCount);
            return;
        }

        _renderPending = true;
        _pendingRenderSequence = MirageDiagnostics.IsEnabled
            ? MirageDiagnostics.NextRenderSequence()
            : 0;
        ScheduleRenderPending();
    }

    public bool DispatchDelayed(TimeSpan delay, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return _dispatcher.DispatchDelayed(delay, action);
    }

    public IDisposable BeginRenderBatch()
    {
        lock (_batchSync)
        {
            _renderBatchDepth++;
        }

        return new RenderBatchScope(this);
    }

    public bool TryGetNodeInputValue(string nodeId, out string? value)
    {
        value = _runtime?.TryGetNodeInputValue(nodeId);
        return value is not null;
    }

    public bool TryGetActiveNodeId(out string? nodeId)
    {
        if (_runtime is null)
        {
            nodeId = null;
            return false;
        }

        return _runtime.TryGetActiveNodeId(out nodeId);
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

    public bool TryScrollNodeChildIntoView(
        string containerNodeId,
        string childNodeId,
        int scrollToPosition,
        bool isHorizontalOrientation,
        bool isAnimated,
        out double actualLeft,
        out double actualTop)
    {
        if (_runtime is null)
        {
            actualLeft = 0;
            actualTop = 0;
            return false;
        }

        return _runtime.TryScrollNodeChildIntoView(
            containerNodeId,
            childNodeId,
            scrollToPosition,
            isHorizontalOrientation,
            isAnimated,
            out actualLeft,
            out actualTop);
    }

    public bool TryRequestNodePickerOpen(string nodeId)
    {
        if (_runtime is null)
        {
            return false;
        }

        return _runtime.TryRequestNodePickerOpen(nodeId);
    }

    public bool TrySetNodeFocus(string nodeId, bool isFocused)
    {
        if (_runtime is null)
        {
            return false;
        }

        return _runtime.TrySetNodeFocus(nodeId, isFocused);
    }

    public bool TrySetNodeFocus(string nodeId, bool isFocused, bool suppressNextFocusEvent)
    {
        if (_runtime is null)
        {
            return false;
        }

        return _runtime.TrySetNodeFocus(nodeId, isFocused, suppressNextFocusEvent);
    }

    public bool TryGetNodeTextSelection(string nodeId, out int selectionStart, out int selectionEnd)
    {
        if (_runtime is null)
        {
            selectionStart = 0;
            selectionEnd = 0;
            return false;
        }

        return _runtime.TryGetNodeTextSelection(nodeId, out selectionStart, out selectionEnd);
    }

    public bool TrySetNodeTextSelection(string nodeId, int selectionStart, int selectionEnd)
    {
        if (_runtime is null)
        {
            return false;
        }

        return _runtime.TrySetNodeTextSelection(nodeId, selectionStart, selectionEnd);
    }

    public bool TryConfigureNodePullToRefresh(string ownerNodeId, string sourceNodeId, bool enabled, double threshold)
    {
        if (_runtime is null)
        {
            return false;
        }

        return _runtime.TryConfigureNodePullToRefresh(ownerNodeId, sourceNodeId, enabled, threshold);
    }

    public bool TryConfigureNodeSwipeView(string ownerNodeId, bool enabled, string configurationJson)
    {
        if (_runtime is null)
        {
            return false;
        }

        return _runtime.TryConfigureNodeSwipeView(ownerNodeId, enabled, configurationJson);
    }

    public bool TryRequestNodeSwipeViewOpen(string ownerNodeId, string side, bool animated, bool emitEndEvent = true)
    {
        if (_runtime is null)
        {
            return false;
        }

        return _runtime.TryRequestNodeSwipeViewOpen(ownerNodeId, side, animated, emitEndEvent);
    }

    public bool TryRequestNodeSwipeViewClose(string ownerNodeId, bool animated)
    {
        if (_runtime is null)
        {
            return false;
        }

        return _runtime.TryRequestNodeSwipeViewClose(ownerNodeId, animated);
    }

    public bool TryConfigureNodeWebView(string nodeId, bool enabled, string configurationJson)
    {
        if (_runtime is null)
        {
            return false;
        }

        return _runtime.TryConfigureNodeWebView(nodeId, enabled, configurationJson);
    }

    public bool TryRequestNodeWebViewNavigate(string nodeId, string url)
    {
        if (_runtime is null)
        {
            return false;
        }

        return _runtime.TryRequestNodeWebViewNavigate(nodeId, url);
    }

    public bool TryRequestNodeWebViewGoBack(string nodeId)
    {
        if (_runtime is null)
        {
            return false;
        }

        return _runtime.TryRequestNodeWebViewGoBack(nodeId);
    }

    public bool TryRequestNodeWebViewGoForward(string nodeId)
    {
        if (_runtime is null)
        {
            return false;
        }

        return _runtime.TryRequestNodeWebViewGoForward(nodeId);
    }

    public bool TryRequestNodeWebViewReload(string nodeId)
    {
        if (_runtime is null)
        {
            return false;
        }

        return _runtime.TryRequestNodeWebViewReload(nodeId);
    }

    public bool TryEvaluateNodeWebViewScript(string nodeId, long requestId, string script, bool captureResult)
    {
        if (_runtime is null)
        {
            return false;
        }

        return _runtime.TryEvaluateNodeWebViewScript(nodeId, requestId, script, captureResult);
    }

    public bool TryConfigureNodeCanvas(string nodeId, double logicalWidth, double logicalHeight, out double displayScale)
    {
        if (_runtime is null)
        {
            displayScale = 1;
            return false;
        }

        return _runtime.TryConfigureNodeCanvas(nodeId, logicalWidth, logicalHeight, out displayScale);
    }

    public bool TryRenderNodeCanvas(string nodeId, string commandsJson)
    {
        if (_runtime is null)
        {
            return false;
        }

        return _runtime.TryRenderNodeCanvas(nodeId, commandsJson);
    }

    public bool TryGetNodeTextContentHeight(string nodeId, out double height)
    {
        if (_runtime is null)
        {
            height = 0;
            return false;
        }

        return _runtime.TryGetNodeTextContentHeight(nodeId, out height);
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
        if (_renderInProgress)
        {
            _renderPending = false;
            _renderRequestedDuringRender = true;
            Interlocked.Increment(ref _coalescedRenderRequestCount);
            return;
        }

        _renderPending = false;
        var renderSequence = _pendingRenderSequence;
        _pendingRenderSequence = 0;

        if (_runtime is null || _root is null)
        {
            return;
        }

        _renderInProgress = true;
        var diagnosticsEnabled = MirageDiagnostics.IsEnabled;
        var renderStartTimestamp = diagnosticsEnabled ? MirageDiagnostics.GetTimestamp() : 0;
        var scopedNodeCount = 0;
        var reusedNodeCount = 0;
        var materializationMode = "full";
        var coalescedSinceLastPass = Interlocked.Exchange(ref _coalescedRenderRequestCount, 0);
        NodeBase? nextRoot = null;
        var nextSnapshot = new Dictionary<string, NodeSnapshot>(StringComparer.Ordinal);

        try
        {
            var canUseScopedMaterialization = _cachedRootNode is not null
                && _cachedRootNode.NodeId == _root.NodeId
                && _cachedRootStructureRevision == _root.SubtreeStructureRevision
                && _snapshotByNodeId.Count > 0;

            materializationMode = canUseScopedMaterialization ? "scoped" : "full";
            nextRoot = MaterializeNode(
                _root,
                canUseScopedMaterialization ? _snapshotByNodeId : null,
                nextSnapshot,
                ref scopedNodeCount,
                ref reusedNodeCount);

            if (canUseScopedMaterialization)
            {
                Interlocked.Increment(ref _scopedMaterializationCount);
            }
            else
            {
                Interlocked.Increment(ref _fullMaterializationCount);
            }

            _runtime.Render(nextRoot);
            _cachedRootNode = nextRoot;
            _snapshotByNodeId = nextSnapshot;
            _cachedRootStructureRevision = _root.SubtreeStructureRevision;

            if (renderSequence > 0)
            {
                MirageDiagnostics.MarkRenderCompleted(renderSequence, MirageDiagnostics.GetTimestamp());
            }
        }
        finally
        {
            _renderInProgress = false;
        }

        var followUpScheduled = false;
        if (_renderRequestedDuringRender)
        {
            _renderRequestedDuringRender = false;
            followUpScheduled = true;
            RequestRender();
        }

        if (!diagnosticsEnabled || nextRoot is null)
        {
            return;
        }

        var elapsedMs = MirageDiagnostics.GetElapsedMilliseconds(renderStartTimestamp, MirageDiagnostics.GetTimestamp());
        var pass = Interlocked.Increment(ref _renderPassCount);
        MirageDiagnostics.Log(
            "coordinator.render",
            $"pass={pass} root={nextRoot.NodeId} mode={materializationMode} rematerializedNodes={scopedNodeCount} reusedNodes={reusedNodeCount} coalescedRequests={coalescedSinceLastPass} followUpScheduled={followUpScheduled} fullMaterializations={Interlocked.Read(ref _fullMaterializationCount)} scopedMaterializations={Interlocked.Read(ref _scopedMaterializationCount)} elapsedMs={elapsedMs:0.###}");
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

    private void ScheduleRenderPending()
    {
        if (!_dispatcher.DispatchDelayed(RenderDispatchDelay, RenderPending))
        {
            RenderPending();
        }
    }

    private static NodeBase MaterializeNode(
        MiragePlatformView platformView,
        IReadOnlyDictionary<string, NodeSnapshot>? previousSnapshot,
        IDictionary<string, NodeSnapshot> nextSnapshot,
        ref int rematerializedNodeCount,
        ref int reusedNodeCount)
    {
        if (string.IsNullOrWhiteSpace(platformView.NodeId))
        {
            throw new InvalidOperationException("Cannot materialize platform node without assigned node id.");
        }

        if (previousSnapshot is not null
            && previousSnapshot.TryGetValue(platformView.NodeId, out var snapshot)
            && SnapshotMatches(platformView, snapshot))
        {
            nextSnapshot[platformView.NodeId] = snapshot;
            reusedNodeCount++;
            CarryReusedSubtreeSnapshots(platformView, snapshot.Node, nextSnapshot, ref reusedNodeCount);
            return snapshot.Node;
        }

        var childViews = platformView.Children.ToArray();
        var childNodes = new List<NodeBase>(childViews.Length);
        foreach (var child in childViews)
        {
            childNodes.Add(MaterializeNode(child, previousSnapshot, nextSnapshot, ref rematerializedNodeCount, ref reusedNodeCount));
        }

        var node = platformView.CreateNode(childNodes);
        rematerializedNodeCount++;
        nextSnapshot[platformView.NodeId] = new NodeSnapshot(
            node,
            platformView.NodeType,
            platformView.ControlKind,
            platformView.ElementName,
            platformView.SubtreeRevision,
            platformView.SubtreeStructureRevision);
        return node;
    }

    private static bool SnapshotMatches(MiragePlatformView platformView, NodeSnapshot snapshot)
    {
        if (snapshot.SubtreeRevision != platformView.SubtreeRevision
            || snapshot.SubtreeStructureRevision != platformView.SubtreeStructureRevision
            || snapshot.NodeType != platformView.NodeType
            || snapshot.ControlKind != platformView.ControlKind)
        {
            return false;
        }

        if (snapshot.NodeType == PlatformNodeType.Web)
        {
            return string.Equals(snapshot.ElementName, platformView.ElementName, StringComparison.Ordinal);
        }

        return true;
    }

    private static void CarryReusedSubtreeSnapshots(
        MiragePlatformView platformView,
        NodeBase node,
        IDictionary<string, NodeSnapshot> nextSnapshot,
        ref int reusedNodeCount)
    {
        var childViews = platformView.Children.ToArray();
        var childNodes = node.Children;
        if (childViews.Length != childNodes.Count)
        {
            return;
        }

        for (var index = 0; index < childViews.Length; index++)
        {
            var childView = childViews[index];
            var childNode = childNodes[index];
            if (!string.Equals(childView.NodeId, childNode.NodeId, StringComparison.Ordinal))
            {
                continue;
            }

            nextSnapshot[childView.NodeId] = new NodeSnapshot(
                childNode,
                childView.NodeType,
                childView.ControlKind,
                childView.ElementName,
                childView.SubtreeRevision,
                childView.SubtreeStructureRevision);
            reusedNodeCount++;
            CarryReusedSubtreeSnapshots(childView, childNode, nextSnapshot, ref reusedNodeCount);
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

    private sealed record NodeSnapshot(
        NodeBase Node,
        PlatformNodeType NodeType,
        ComponentKind? ControlKind,
        string? ElementName,
        long SubtreeRevision,
        long SubtreeStructureRevision);
}
