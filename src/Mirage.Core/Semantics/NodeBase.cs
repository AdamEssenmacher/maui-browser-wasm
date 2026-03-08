using System.Collections.ObjectModel;

namespace Mirage.Core.Semantics;

public abstract class NodeBase
{
    protected NodeBase(string nodeId, IReadOnlyList<NodeBase>? children = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);

        NodeId = nodeId;
        Children = children is null
            ? []
            : new ReadOnlyCollection<NodeBase>(children.ToArray());
    }

    public string NodeId { get; }

    public IReadOnlyList<NodeBase> Children { get; }

    public abstract PlatformNodeType NodeType { get; }
}
