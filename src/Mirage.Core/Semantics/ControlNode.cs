namespace Mirage.Core.Semantics;

public sealed class ControlNode : VisualNodeBase
{
    public ControlNode(
        string nodeId,
        ComponentKind kind,
        IReadOnlyDictionary<string, MirageValue>? properties = null,
        IReadOnlyDictionary<string, string?>? styles = null,
        IReadOnlyCollection<string>? events = null,
        IReadOnlyDictionary<string, MirageValue>? layoutMetadata = null,
        string? automationId = null,
        MirageVisibility visibility = MirageVisibility.Visible,
        double? opacity = null,
        MirageThickness? margin = null,
        IReadOnlyList<NodeBase>? children = null)
        : base(
            nodeId,
            properties,
            styles,
            events,
            layoutMetadata,
            automationId,
            visibility,
            opacity,
            margin,
            children)
    {
        Kind = kind;
    }

    public ComponentKind Kind { get; }

    public override PlatformNodeType NodeType => PlatformNodeType.Control;
}
