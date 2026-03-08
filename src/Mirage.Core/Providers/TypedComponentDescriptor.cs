using Mirage.Core.Semantics;

namespace Mirage.Core.Providers;

public sealed class TypedComponentDescriptor
{
    public TypedComponentDescriptor(
        ComponentKind kind,
        string elementTagName,
        IReadOnlyDictionary<string, string>? defaultAttributes = null,
        IReadOnlyDictionary<string, ProviderPropertyDescriptor>? propertyMappings = null,
        IReadOnlyDictionary<string, ProviderEventDescriptor>? eventMappings = null,
        string? valueElementTagName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementTagName);

        Kind = kind;
        ElementTagName = elementTagName;
        DefaultAttributes = defaultAttributes ?? new Dictionary<string, string>(StringComparer.Ordinal);
        PropertyMappings = propertyMappings ?? new Dictionary<string, ProviderPropertyDescriptor>(StringComparer.Ordinal);
        EventMappings = eventMappings ?? new Dictionary<string, ProviderEventDescriptor>(StringComparer.Ordinal);
        ValueElementTagName = valueElementTagName;
    }

    public ComponentKind Kind { get; }

    public string ElementTagName { get; }

    public string? ValueElementTagName { get; }

    public IReadOnlyDictionary<string, string> DefaultAttributes { get; }

    public IReadOnlyDictionary<string, ProviderPropertyDescriptor> PropertyMappings { get; }

    public IReadOnlyDictionary<string, ProviderEventDescriptor> EventMappings { get; }
}
