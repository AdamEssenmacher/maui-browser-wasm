using System.Collections.ObjectModel;
using System.Globalization;

namespace Mirage.Core.Semantics;

public abstract class VisualNodeBase : NodeBase
{
    protected VisualNodeBase(
        string nodeId,
        IReadOnlyDictionary<string, MirageValue>? properties = null,
        IReadOnlyDictionary<string, string?>? styles = null,
        IReadOnlyCollection<string>? events = null,
        IReadOnlyDictionary<string, MirageValue>? layoutMetadata = null,
        string? automationId = null,
        MirageVisibility visibility = MirageVisibility.Visible,
        double? opacity = null,
        MirageThickness? margin = null,
        IReadOnlyList<NodeBase>? children = null)
        : base(nodeId, children)
    {
        var normalizedProperties = NormalizeProperties(properties, automationId, out var resolvedAutomationId);
        var normalizedStyles = NormalizeStyles(styles, visibility, opacity, margin);

        Properties = normalizedProperties;
        Styles = normalizedStyles;
        Events = NormalizeEvents(events);
        LayoutMetadata = NormalizeProperties(layoutMetadata, automationId: null, out _);
        AutomationId = resolvedAutomationId;
        Visibility = visibility;
        Opacity = opacity;
        Margin = margin;
    }

    public string? AutomationId { get; }

    public MirageVisibility Visibility { get; }

    public double? Opacity { get; }

    public MirageThickness? Margin { get; }

    public IReadOnlyDictionary<string, MirageValue> Properties { get; }

    public IReadOnlyDictionary<string, string?> Styles { get; }

    public IReadOnlyList<string> Events { get; }

    public IReadOnlyDictionary<string, MirageValue> LayoutMetadata { get; }

    private static IReadOnlyDictionary<string, MirageValue> NormalizeProperties(
        IReadOnlyDictionary<string, MirageValue>? source,
        string? automationId,
        out string? resolvedAutomationId)
    {
        var clone = new Dictionary<string, MirageValue>(StringComparer.Ordinal);
        if (source is not null)
        {
            foreach (var (key, value) in source)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                clone[key] = value;
            }
        }

        if (!string.IsNullOrWhiteSpace(automationId))
        {
            clone["AutomationId"] = MirageValue.String(automationId);
            resolvedAutomationId = automationId;
        }
        else if (clone.TryGetValue("AutomationId", out var value) && value.TryGetString(out var mapped))
        {
            resolvedAutomationId = string.IsNullOrWhiteSpace(mapped) ? null : mapped;
        }
        else
        {
            resolvedAutomationId = null;
        }

        return new ReadOnlyDictionary<string, MirageValue>(clone);
    }

    private static IReadOnlyDictionary<string, string?> NormalizeStyles(
        IReadOnlyDictionary<string, string?>? source,
        MirageVisibility visibility,
        double? opacity,
        MirageThickness? margin)
    {
        var clone = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (source is not null)
        {
            foreach (var (key, value) in source)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                clone[key] = value;
            }
        }

        if (visibility != MirageVisibility.Visible)
        {
            clone["display"] = "none";
        }

        if (opacity is double explicitOpacity)
        {
            var normalized = double.IsFinite(explicitOpacity)
                ? Math.Clamp(explicitOpacity, 0, 1)
                : 1;
            clone["opacity"] = string.Create(CultureInfo.InvariantCulture, $"{normalized:0.###}");
        }

        if (margin is MirageThickness explicitMargin)
        {
            clone["margin"] = explicitMargin.ToCssString();
        }

        return new ReadOnlyDictionary<string, string?>(clone);
    }

    private static IReadOnlyList<string> NormalizeEvents(IReadOnlyCollection<string>? source)
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
}
