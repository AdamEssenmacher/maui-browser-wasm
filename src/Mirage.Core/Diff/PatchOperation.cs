using Mirage.Core.Semantics;

namespace Mirage.Core.Diff;

public abstract record PatchOperation(string NodeId);

public enum NodeCreationKind
{
    Typed,
    Element
}

public sealed record CreateNodeOperation(
    string NodeId,
    NodeCreationKind CreationKind,
    ComponentKind? TypedKind,
    string? ElementName,
    string? ParentId,
    int Index) : PatchOperation(NodeId);

public sealed record RemoveNodeOperation(string NodeId, string? ParentId) : PatchOperation(NodeId);

public sealed record MoveNodeOperation(string NodeId, string ParentId, int Index) : PatchOperation(NodeId);

public sealed record SetPropertyOperation(string NodeId, string PropertyName, MirageValue Value) : PatchOperation(NodeId);

public sealed record RemovePropertyOperation(string NodeId, string PropertyName) : PatchOperation(NodeId);

public sealed record SetAttributeOperation(string NodeId, string AttributeName, string? Value) : PatchOperation(NodeId);

public sealed record RemoveAttributeOperation(string NodeId, string AttributeName) : PatchOperation(NodeId);

public sealed record AddClassOperation(string NodeId, string ClassName) : PatchOperation(NodeId);

public sealed record RemoveClassOperation(string NodeId, string ClassName) : PatchOperation(NodeId);

public sealed record AttachEventOperation(string NodeId, string EventName) : PatchOperation(NodeId);

public sealed record DetachEventOperation(string NodeId, string EventName) : PatchOperation(NodeId);

public sealed record SetStyleOperation(string NodeId, string StyleName, string? Value) : PatchOperation(NodeId);
