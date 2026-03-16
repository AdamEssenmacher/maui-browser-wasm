using Mirage.Core.Semantics;

namespace Mirage.Core.Providers;

public sealed class ControlComponentDescriptor
{
    public ControlComponentDescriptor(
        ComponentKind kind,
        string elementTagName,
        IReadOnlyDictionary<string, string>? defaultAttributes = null,
        IReadOnlyDictionary<string, ProviderPropertyDescriptor>? propertyMappings = null,
        IReadOnlyDictionary<string, ProviderEventDescriptor>? eventMappings = null,
        string? valueElementTagName = null,
        string? secondaryElementTagName = null,
        bool stretchValueElementWidth = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementTagName);

        Kind = kind;
        ElementTagName = elementTagName;
        DefaultAttributes = defaultAttributes ?? new Dictionary<string, string>(StringComparer.Ordinal);
        PropertyMappings = propertyMappings ?? new Dictionary<string, ProviderPropertyDescriptor>(StringComparer.Ordinal);
        EventMappings = eventMappings ?? new Dictionary<string, ProviderEventDescriptor>(StringComparer.Ordinal);
        ValueElementTagName = valueElementTagName;
        SecondaryElementTagName = secondaryElementTagName;
        StretchValueElementWidth = stretchValueElementWidth;
    }

    public ComponentKind Kind { get; }

    public string ElementTagName { get; }

    public string? ValueElementTagName { get; }

    public string? SecondaryElementTagName { get; }

    public bool StretchValueElementWidth { get; }

    public IReadOnlyDictionary<string, string> DefaultAttributes { get; }

    public IReadOnlyDictionary<string, ProviderPropertyDescriptor> PropertyMappings { get; }

    public IReadOnlyDictionary<string, ProviderEventDescriptor> EventMappings { get; }
}
