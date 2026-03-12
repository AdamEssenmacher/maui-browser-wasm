using Mirage.Core.Diagnostics;
using Mirage.Core.Semantics;

namespace Mirage.Maui.Handlers;

public sealed class MiragePlatformView
{
    private readonly Dictionary<string, MirageValue> _properties = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string?> _styles = new(StringComparer.Ordinal);
    private readonly HashSet<string> _events = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string?> _attributes = new(StringComparer.Ordinal);
    private readonly HashSet<string> _classes = new(StringComparer.Ordinal);
    private readonly List<MiragePlatformView> _children = [];

    private readonly ComponentKind? _controlKind;
    private readonly string? _elementName;
    private static long s_nextRevision;

    private long _localRevision;
    private long _localStructureRevision;
    private long _subtreeRevision;
    private long _subtreeStructureRevision;

    public MiragePlatformView(ComponentKind kind)
    {
        _controlKind = kind;
        NodeType = PlatformNodeType.Control;
        InitializeRevisionTracking();
    }

    private MiragePlatformView(string elementName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementName);

        _elementName = elementName;
        NodeType = PlatformNodeType.Web;
        InitializeRevisionTracking();
    }

    public PlatformNodeType NodeType { get; }

    public ComponentKind? ControlKind => _controlKind;

    public string? ElementName => _elementName;

    public string NodeId { get; private set; } = string.Empty;

    public IReadOnlyList<MiragePlatformView> Children => _children;

    public IReadOnlyDictionary<string, MirageValue> Properties => _properties;

    public IReadOnlyDictionary<string, string?> Styles => _styles;

    public IReadOnlyCollection<string> Events => _events;

    public IReadOnlyDictionary<string, string?> Attributes => _attributes;

    public IReadOnlyCollection<string> Classes => _classes;

    public MiragePlatformView? Parent { get; private set; }

    internal long LocalRevision => _localRevision;

    internal long LocalStructureRevision => _localStructureRevision;

    internal long SubtreeRevision => _subtreeRevision;

    internal long SubtreeStructureRevision => _subtreeStructureRevision;

    public static MiragePlatformView CreateControl(ComponentKind kind) => new(kind);

    public static MiragePlatformView CreateWeb(string elementName) => new(elementName);

    public void AssignNodeId(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            throw new ArgumentException("NodeId must not be null or whitespace.", nameof(nodeId));
        }

        if (!string.IsNullOrWhiteSpace(NodeId) && !string.Equals(NodeId, nodeId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("NodeId is immutable once assigned.");
        }

        NodeId = nodeId;
    }

    public void SetProperty(string name, string? value) => SetProperty(name, MirageValue.String(value));

    public void SetProperty(string name, MirageValue? value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (value is null || value.Value.Kind == MirageValueKind.Null)
        {
            if (_properties.Remove(name))
            {
                MarkMutation(structural: false);
            }
            return;
        }

        if (_properties.TryGetValue(name, out var current) && current.Equals(value.Value))
        {
            return;
        }

        _properties[name] = value.Value;
        MarkMutation(structural: false);
    }

    public string? GetPropertyString(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return _properties.TryGetValue(name, out var value)
            ? value.GetStringOrNull()
            : null;
    }

    public string? GetStyleValue(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return _styles.TryGetValue(name, out var value)
            ? value
            : null;
    }

    public void SetStyle(string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (value is null)
        {
            if (_styles.Remove(name))
            {
                MarkMutation(structural: false);
            }
            return;
        }

        if (_styles.TryGetValue(name, out var current) && string.Equals(current, value, StringComparison.Ordinal))
        {
            return;
        }

        _styles[name] = value;
        MarkMutation(structural: false);
    }

    public void SetEvent(string eventName, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            return;
        }

        if (enabled)
        {
            if (_events.Add(eventName))
            {
                MarkMutation(structural: false);
            }
            return;
        }

        if (_events.Remove(eventName))
        {
            MarkMutation(structural: false);
        }
    }

    public void SetAttribute(string name, string? value)
    {
        if (NodeType != PlatformNodeType.Web || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (value is null)
        {
            if (_attributes.Remove(name))
            {
                MarkMutation(structural: false);
            }
            return;
        }

        if (_attributes.TryGetValue(name, out var current) && string.Equals(current, value, StringComparison.Ordinal))
        {
            return;
        }

        _attributes[name] = value;
        MarkMutation(structural: false);
    }

    public void AddClass(string className)
    {
        if (NodeType != PlatformNodeType.Web || string.IsNullOrWhiteSpace(className))
        {
            return;
        }

        if (_classes.Add(className))
        {
            MarkMutation(structural: false);
        }
    }

    public void RemoveClass(string className)
    {
        if (NodeType != PlatformNodeType.Web || string.IsNullOrWhiteSpace(className))
        {
            return;
        }

        if (_classes.Remove(className))
        {
            MarkMutation(structural: false);
        }
    }

    public void ClearChildren()
    {
        if (_children.Count == 0)
        {
            return;
        }

        foreach (var child in _children)
        {
            child.Parent = null;
        }

        _children.Clear();
        MarkMutation(structural: true);
    }

    public void AddChild(MiragePlatformView child)
    {
        InsertChild(_children.Count, child);
    }

    public void InsertChild(int index, MiragePlatformView child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (ReferenceEquals(child, this))
        {
            throw new InvalidOperationException("A platform view cannot be its own child.");
        }

        if (child.Parent is not null)
        {
            child.Parent.RemoveChild(child);
        }

        var normalizedIndex = Math.Clamp(index, 0, _children.Count);
        _children.Insert(normalizedIndex, child);
        child.Parent = this;
        MarkMutation(structural: true);
    }

    public void RemoveChild(MiragePlatformView child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (_children.Remove(child))
        {
            child.Parent = null;
            MarkMutation(structural: true);
        }
    }

    public NodeBase ToNode()
    {
        if (MirageDiagnostics.IsEnabled && Parent is null)
        {
            var startTimestamp = MirageDiagnostics.GetTimestamp();
            var node = ToNodeCore();
            var elapsedMs = MirageDiagnostics.GetElapsedMilliseconds(startTimestamp, MirageDiagnostics.GetTimestamp());
            MirageDiagnostics.Log(
                "node.to-node",
                $"rootNodeId={NodeId} subtreeNodeCount={CountSubtreeNodes()} elapsedMs={elapsedMs:0.###}");
            return node;
        }

        return ToNodeCore();
    }

    internal NodeBase CreateNode(IReadOnlyList<NodeBase> childNodes)
    {
        if (string.IsNullOrWhiteSpace(NodeId))
        {
            throw new InvalidOperationException("Cannot create semantic node for platform view without assigned node id.");
        }

        return NodeType switch
        {
            PlatformNodeType.Control => new ControlNode(
                NodeId,
                _controlKind ?? throw new InvalidOperationException("Control platform node is missing ComponentKind."),
                properties: _properties,
                styles: _styles,
                events: _events,
                children: childNodes),
            PlatformNodeType.Web => new WebNode(
                NodeId,
                _elementName ?? throw new InvalidOperationException("Web platform node is missing ElementName."),
                attributes: _attributes,
                properties: _properties,
                classes: _classes,
                styles: _styles,
                events: _events,
                children: childNodes),
            _ => throw new InvalidOperationException($"Unsupported platform node type '{NodeType}'.")
        };
    }

    private NodeBase ToNodeCore()
    {
        var childNodes = new List<NodeBase>(_children.Count);
        foreach (var child in _children)
        {
            childNodes.Add(child.ToNodeCore());
        }

        return CreateNode(childNodes);
    }

    private int CountSubtreeNodes()
    {
        var count = 1;
        foreach (var child in _children)
        {
            count += child.CountSubtreeNodes();
        }

        return count;
    }

    private static long NextRevision()
    {
        return Interlocked.Increment(ref s_nextRevision);
    }

    private void InitializeRevisionTracking()
    {
        var revision = NextRevision();
        _localRevision = revision;
        _localStructureRevision = revision;
        _subtreeRevision = revision;
        _subtreeStructureRevision = revision;
    }

    private void MarkMutation(bool structural)
    {
        var revision = NextRevision();
        _localRevision = revision;
        _subtreeRevision = revision;

        if (structural)
        {
            _localStructureRevision = revision;
            _subtreeStructureRevision = revision;
        }

        Parent?.PropagateSubtreeRevision(revision, structural);
    }

    private void PropagateSubtreeRevision(long revision, bool structural)
    {
        var changed = false;
        if (revision > _subtreeRevision)
        {
            _subtreeRevision = revision;
            changed = true;
        }

        if (structural && revision > _subtreeStructureRevision)
        {
            _subtreeStructureRevision = revision;
            changed = true;
        }

        if (changed)
        {
            Parent?.PropagateSubtreeRevision(revision, structural);
        }
    }
}
