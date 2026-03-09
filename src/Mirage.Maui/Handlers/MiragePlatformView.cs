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

    public MiragePlatformView(ComponentKind kind)
    {
        _controlKind = kind;
        NodeType = PlatformNodeType.Control;
    }

    private MiragePlatformView(string elementName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementName);

        _elementName = elementName;
        NodeType = PlatformNodeType.Web;
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
            _properties.Remove(name);
            return;
        }

        _properties[name] = value.Value;
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
            _styles.Remove(name);
            return;
        }

        _styles[name] = value;
    }

    public void SetEvent(string eventName, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            return;
        }

        if (enabled)
        {
            _events.Add(eventName);
            return;
        }

        _events.Remove(eventName);
    }

    public void SetAttribute(string name, string? value)
    {
        if (NodeType != PlatformNodeType.Web || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (value is null)
        {
            _attributes.Remove(name);
            return;
        }

        _attributes[name] = value;
    }

    public void AddClass(string className)
    {
        if (NodeType != PlatformNodeType.Web || string.IsNullOrWhiteSpace(className))
        {
            return;
        }

        _classes.Add(className);
    }

    public void RemoveClass(string className)
    {
        if (NodeType != PlatformNodeType.Web || string.IsNullOrWhiteSpace(className))
        {
            return;
        }

        _classes.Remove(className);
    }

    public void ClearChildren()
    {
        foreach (var child in _children)
        {
            child.Parent = null;
        }

        _children.Clear();
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
    }

    public void RemoveChild(MiragePlatformView child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (_children.Remove(child))
        {
            child.Parent = null;
        }
    }

    public NodeBase ToNode()
    {
        if (string.IsNullOrWhiteSpace(NodeId))
        {
            throw new InvalidOperationException("Cannot create semantic node for platform view without assigned node id.");
        }

        var childNodes = new List<NodeBase>(_children.Count);
        foreach (var child in _children)
        {
            childNodes.Add(child.ToNode());
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
}
