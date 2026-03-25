using System.Collections.ObjectModel;

namespace Mirage.Core.Semantics;

public sealed class WebNode : VisualNodeBase
{
    public WebNode(
        string nodeId,
        string elementName,
        IReadOnlyDictionary<string, string?>? attributes = null,
        IReadOnlyDictionary<string, MirageValue>? properties = null,
        IReadOnlyCollection<string>? classes = null,
        IReadOnlyDictionary<string, string?>? styles = null,
        IReadOnlyCollection<string>? events = null,
        IReadOnlyDictionary<string, MirageValue>? metadata = null,
        IReadOnlyDictionary<string, string?>? slots = null,
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
        ArgumentException.ThrowIfNullOrWhiteSpace(elementName);

        ElementName = elementName;
        Attributes = NormalizeAttributes(attributes);
        Classes = NormalizeClasses(classes);
        Metadata = NormalizeMetadata(metadata);
        Slots = NormalizeAttributes(slots);
    }

    public string ElementName { get; }

    public IReadOnlyDictionary<string, string?> Attributes { get; }

    public IReadOnlyList<string> Classes { get; }

    // Reserved until Mirage diff/runtime support metadata end-to-end.
    public IReadOnlyDictionary<string, MirageValue> Metadata { get; }

    // Reserved until Mirage diff/runtime support slot projection end-to-end.
    public IReadOnlyDictionary<string, string?> Slots { get; }

    public override PlatformNodeType NodeType => PlatformNodeType.Web;

    private static IReadOnlyDictionary<string, string?> NormalizeAttributes(IReadOnlyDictionary<string, string?>? source)
    {
        if (source is null || source.Count == 0)
        {
            return new ReadOnlyDictionary<string, string?>(new Dictionary<string, string?>(StringComparer.Ordinal));
        }

        var clone = new Dictionary<string, string?>(source.Count, StringComparer.Ordinal);
        foreach (var (key, value) in source)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            clone[key] = value;
        }

        return new ReadOnlyDictionary<string, string?>(clone);
    }

    private static IReadOnlyList<string> NormalizeClasses(IReadOnlyCollection<string>? source)
    {
        if (source is null || source.Count == 0)
        {
            return [];
        }

        var list = source
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

        return new ReadOnlyCollection<string>(list);
    }

    private static IReadOnlyDictionary<string, MirageValue> NormalizeMetadata(IReadOnlyDictionary<string, MirageValue>? source)
    {
        if (source is null || source.Count == 0)
        {
            return new ReadOnlyDictionary<string, MirageValue>(new Dictionary<string, MirageValue>(StringComparer.Ordinal));
        }

        var clone = new Dictionary<string, MirageValue>(source.Count, StringComparer.Ordinal);
        foreach (var (key, value) in source)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            clone[key] = value;
        }

        return new ReadOnlyDictionary<string, MirageValue>(clone);
    }
}
