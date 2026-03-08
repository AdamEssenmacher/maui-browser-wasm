using Mirage.Core.Semantics;

namespace Mirage.Core.Diff;

public sealed class SemanticDiffEngine
{
    public IReadOnlyList<PatchOperation> Diff(NodeBase? previousRoot, NodeBase nextRoot)
    {
        ArgumentNullException.ThrowIfNull(nextRoot);

        if (previousRoot is not null && !string.Equals(previousRoot.NodeId, nextRoot.NodeId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Root node id must remain stable between diff snapshots.");
        }

        var previousIndex = previousRoot is null ? NodeIndex.Empty : NodeIndex.Create(previousRoot);
        var nextIndex = NodeIndex.Create(nextRoot);

        var operations = new List<PatchOperation>();

        AppendCreates(operations, previousIndex, nextIndex);
        AppendMoves(operations, previousIndex, nextIndex);
        AppendPropertyDiffs(operations, previousIndex, nextIndex);
        AppendStyleDiffs(operations, previousIndex, nextIndex);
        AppendEventDiffs(operations, previousIndex, nextIndex);
        AppendClassDiffs(operations, previousIndex, nextIndex);
        AppendAttributeDiffs(operations, previousIndex, nextIndex);
        AppendRemovals(operations, previousIndex, nextIndex);

        return operations;
    }

    private static void AppendCreates(List<PatchOperation> operations, NodeIndex previousIndex, NodeIndex nextIndex)
    {
        foreach (var info in nextIndex.PreOrder)
        {
            if (previousIndex.Contains(info.Node.NodeId))
            {
                continue;
            }

            operations.Add(CreateNodeOperationFrom(info.Node, info.ParentId, info.Index));

            if (info.Node is not VisualNodeBase visual)
            {
                continue;
            }

            foreach (var property in visual.Properties.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
            {
                operations.Add(new SetPropertyOperation(info.Node.NodeId, property.Key, property.Value));
            }

            foreach (var style in visual.Styles.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
            {
                operations.Add(new SetStyleOperation(info.Node.NodeId, style.Key, style.Value));
            }

            foreach (var eventName in visual.Events)
            {
                operations.Add(new AttachEventOperation(info.Node.NodeId, eventName));
            }

            if (info.Node is ElementNode element)
            {
                foreach (var @class in element.Classes)
                {
                    operations.Add(new AddClassOperation(info.Node.NodeId, @class));
                }

                foreach (var attribute in element.Attributes.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
                {
                    operations.Add(new SetAttributeOperation(info.Node.NodeId, attribute.Key, attribute.Value));
                }
            }
        }
    }

    private static void AppendMoves(List<PatchOperation> operations, NodeIndex previousIndex, NodeIndex nextIndex)
    {
        foreach (var info in nextIndex.PreOrder)
        {
            if (!previousIndex.TryGet(info.Node.NodeId, out var previousInfo))
            {
                continue;
            }

            EnsureNodeShapeStable(previousInfo.Node, info.Node);

            if (info.ParentId is null)
            {
                continue;
            }

            if (string.Equals(previousInfo.ParentId, info.ParentId, StringComparison.Ordinal)
                && previousInfo.Index == info.Index)
            {
                continue;
            }

            operations.Add(new MoveNodeOperation(info.Node.NodeId, info.ParentId, info.Index));
        }
    }

    private static void AppendPropertyDiffs(List<PatchOperation> operations, NodeIndex previousIndex, NodeIndex nextIndex)
    {
        foreach (var info in nextIndex.PreOrder)
        {
            if (info.Node is not VisualNodeBase nextVisual
                || !previousIndex.TryGet(info.Node.NodeId, out var previousInfo)
                || previousInfo.Node is not VisualNodeBase previousVisual)
            {
                continue;
            }

            EnsureNodeShapeStable(previousInfo.Node, info.Node);
            AppendDictionaryDiff(
                previousVisual.Properties,
                nextVisual.Properties,
                removed => operations.Add(new RemovePropertyOperation(info.Node.NodeId, removed)),
                (key, value) => operations.Add(new SetPropertyOperation(info.Node.NodeId, key, value)));
        }
    }

    private static void AppendStyleDiffs(List<PatchOperation> operations, NodeIndex previousIndex, NodeIndex nextIndex)
    {
        foreach (var info in nextIndex.PreOrder)
        {
            if (info.Node is not VisualNodeBase nextVisual
                || !previousIndex.TryGet(info.Node.NodeId, out var previousInfo)
                || previousInfo.Node is not VisualNodeBase previousVisual)
            {
                continue;
            }

            EnsureNodeShapeStable(previousInfo.Node, info.Node);
            AppendDictionaryDiff(
                previousVisual.Styles,
                nextVisual.Styles,
                removed => operations.Add(new SetStyleOperation(info.Node.NodeId, removed, null)),
                (key, value) => operations.Add(new SetStyleOperation(info.Node.NodeId, key, value)));
        }
    }

    private static void AppendEventDiffs(List<PatchOperation> operations, NodeIndex previousIndex, NodeIndex nextIndex)
    {
        foreach (var info in nextIndex.PreOrder)
        {
            if (info.Node is not VisualNodeBase nextVisual
                || !previousIndex.TryGet(info.Node.NodeId, out var previousInfo)
                || previousInfo.Node is not VisualNodeBase previousVisual)
            {
                continue;
            }

            EnsureNodeShapeStable(previousInfo.Node, info.Node);

            var previousEvents = previousVisual.Events.ToHashSet(StringComparer.Ordinal);
            var nextEvents = nextVisual.Events.ToHashSet(StringComparer.Ordinal);

            foreach (var detached in previousEvents.Except(nextEvents).OrderBy(static value => value, StringComparer.Ordinal))
            {
                operations.Add(new DetachEventOperation(info.Node.NodeId, detached));
            }

            foreach (var attached in nextEvents.Except(previousEvents).OrderBy(static value => value, StringComparer.Ordinal))
            {
                operations.Add(new AttachEventOperation(info.Node.NodeId, attached));
            }
        }
    }

    private static void AppendClassDiffs(List<PatchOperation> operations, NodeIndex previousIndex, NodeIndex nextIndex)
    {
        foreach (var info in nextIndex.PreOrder.Where(static info => info.Node is ElementNode))
        {
            if (info.Node is not ElementNode nextElement
                || !previousIndex.TryGet(info.Node.NodeId, out var previousInfo))
            {
                continue;
            }

            EnsureNodeShapeStable(previousInfo.Node, info.Node);
            if (previousInfo.Node is not ElementNode previousElement)
            {
                continue;
            }

            var previousClasses = previousElement.Classes.ToHashSet(StringComparer.Ordinal);
            var nextClasses = nextElement.Classes.ToHashSet(StringComparer.Ordinal);

            foreach (var removed in previousClasses.Except(nextClasses).OrderBy(static value => value, StringComparer.Ordinal))
            {
                operations.Add(new RemoveClassOperation(info.Node.NodeId, removed));
            }

            foreach (var added in nextClasses.Except(previousClasses).OrderBy(static value => value, StringComparer.Ordinal))
            {
                operations.Add(new AddClassOperation(info.Node.NodeId, added));
            }
        }
    }

    private static void AppendAttributeDiffs(List<PatchOperation> operations, NodeIndex previousIndex, NodeIndex nextIndex)
    {
        foreach (var info in nextIndex.PreOrder.Where(static info => info.Node is ElementNode))
        {
            if (info.Node is not ElementNode nextElement
                || !previousIndex.TryGet(info.Node.NodeId, out var previousInfo))
            {
                continue;
            }

            EnsureNodeShapeStable(previousInfo.Node, info.Node);
            if (previousInfo.Node is not ElementNode previousElement)
            {
                continue;
            }

            AppendDictionaryDiff(
                previousElement.Attributes,
                nextElement.Attributes,
                removed => operations.Add(new RemoveAttributeOperation(info.Node.NodeId, removed)),
                (key, value) => operations.Add(new SetAttributeOperation(info.Node.NodeId, key, value)));
        }
    }

    private static void AppendRemovals(List<PatchOperation> operations, NodeIndex previousIndex, NodeIndex nextIndex)
    {
        var removed = previousIndex.PreOrder
            .Where(static info => info.ParentId is not null)
            .Where(info => !nextIndex.Contains(info.Node.NodeId))
            .OrderByDescending(static info => info.Depth)
            .ThenByDescending(static info => info.PreOrderIndex)
            .ToArray();

        foreach (var info in removed)
        {
            if (info.Node is VisualNodeBase visual)
            {
                foreach (var eventName in visual.Events.Reverse())
                {
                    operations.Add(new DetachEventOperation(info.Node.NodeId, eventName));
                }
            }

            operations.Add(new RemoveNodeOperation(info.Node.NodeId, info.ParentId));
        }
    }

    private static void AppendDictionaryDiff<TValue>(
        IReadOnlyDictionary<string, TValue> previous,
        IReadOnlyDictionary<string, TValue> next,
        Action<string> onRemoved,
        Action<string, TValue> onAddedOrChanged)
    {
        var previousKeys = previous.Keys.ToHashSet(StringComparer.Ordinal);
        var nextKeys = next.Keys.ToHashSet(StringComparer.Ordinal);

        foreach (var removed in previousKeys.Except(nextKeys).OrderBy(static value => value, StringComparer.Ordinal))
        {
            onRemoved(removed);
        }

        foreach (var key in nextKeys.OrderBy(static value => value, StringComparer.Ordinal))
        {
            var nextValue = next[key];
            var hadPrevious = previous.TryGetValue(key, out var previousValue);
            if (!hadPrevious || EqualityComparer<TValue>.Default.Equals(previousValue!, nextValue) == false)
            {
                onAddedOrChanged(key, nextValue);
            }
        }
    }

    private static void EnsureNodeShapeStable(NodeBase previous, NodeBase next)
    {
        if (previous.NodeType != next.NodeType)
        {
            throw new InvalidOperationException(
                $"Node '{next.NodeId}' changed node type from '{previous.NodeType}' to '{next.NodeType}'. Node shape must remain stable.");
        }

        if (previous is TypedNode previousTyped && next is TypedNode nextTyped && previousTyped.Kind != nextTyped.Kind)
        {
            throw new InvalidOperationException(
                $"Node '{next.NodeId}' changed component kind from '{previousTyped.Kind}' to '{nextTyped.Kind}'. Node kind must remain stable.");
        }

        if (previous is ElementNode previousElement
            && next is ElementNode nextElement
            && !string.Equals(previousElement.ElementName, nextElement.ElementName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Node '{next.NodeId}' changed element name from '{previousElement.ElementName}' to '{nextElement.ElementName}'. Element identity must remain stable.");
        }
    }

    private static CreateNodeOperation CreateNodeOperationFrom(NodeBase node, string? parentId, int index)
    {
        return node switch
        {
            TypedNode typed => new CreateNodeOperation(
                node.NodeId,
                NodeCreationKind.Typed,
                typed.Kind,
                ElementName: null,
                parentId,
                index),
            ElementNode element => new CreateNodeOperation(
                node.NodeId,
                NodeCreationKind.Element,
                TypedKind: null,
                element.ElementName,
                parentId,
                index),
            _ => throw new InvalidOperationException($"Unsupported node type '{node.GetType().Name}'.")
        };
    }

    private sealed class NodeIndex
    {
        public static readonly NodeIndex Empty = new([], new Dictionary<string, NodeInfo>(StringComparer.Ordinal));

        private NodeIndex(IReadOnlyList<NodeInfo> preOrder, IReadOnlyDictionary<string, NodeInfo> byId)
        {
            PreOrder = preOrder;
            ById = byId;
        }

        public IReadOnlyList<NodeInfo> PreOrder { get; }

        private IReadOnlyDictionary<string, NodeInfo> ById { get; }

        public bool Contains(string nodeId) => ById.ContainsKey(nodeId);

        public bool TryGet(string nodeId, out NodeInfo info)
        {
            if (ById.TryGetValue(nodeId, out var found))
            {
                info = found;
                return true;
            }

            info = default;
            return false;
        }

        public static NodeIndex Create(NodeBase root)
        {
            var byId = new Dictionary<string, NodeInfo>(StringComparer.Ordinal);
            var list = new List<NodeInfo>();
            Traverse(root, parentId: null, index: 0, depth: 0, list, byId);
            return new NodeIndex(list, byId);
        }

        private static void Traverse(
            NodeBase node,
            string? parentId,
            int index,
            int depth,
            List<NodeInfo> list,
            Dictionary<string, NodeInfo> byId)
        {
            if (byId.ContainsKey(node.NodeId))
            {
                throw new InvalidOperationException($"Duplicate semantic node id detected: '{node.NodeId}'.");
            }

            var info = new NodeInfo(node, parentId, index, depth, list.Count);
            list.Add(info);
            byId[node.NodeId] = info;

            for (var childIndex = 0; childIndex < node.Children.Count; childIndex++)
            {
                Traverse(node.Children[childIndex], node.NodeId, childIndex, depth + 1, list, byId);
            }
        }
    }

    private readonly record struct NodeInfo(
        NodeBase Node,
        string? ParentId,
        int Index,
        int Depth,
        int PreOrderIndex);
}
