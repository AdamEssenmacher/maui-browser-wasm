namespace Mirage.Core.Providers;

// Thin DOM realization hints for the generic WebNode path. This is
// intentionally smaller than a full arbitrary component-library adapter model.
public sealed class WebComponentDescriptor
{
    public WebComponentDescriptor(
        string elementName,
        IReadOnlyDictionary<string, string>? defaultAttributes = null,
        IReadOnlyDictionary<string, ProviderEventDescriptor>? eventMappings = null,
        bool supportsAttributes = true,
        bool supportsProperties = true,
        bool supportsStyles = true,
        bool supportsClasses = true,
        bool supportsEvents = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementName);

        ElementName = elementName;
        DefaultAttributes = defaultAttributes ?? new Dictionary<string, string>(StringComparer.Ordinal);
        EventMappings = eventMappings ?? new Dictionary<string, ProviderEventDescriptor>(StringComparer.Ordinal);
        SupportsAttributes = supportsAttributes;
        SupportsProperties = supportsProperties;
        SupportsStyles = supportsStyles;
        SupportsClasses = supportsClasses;
        SupportsEvents = supportsEvents;
    }

    public string ElementName { get; }

    public IReadOnlyDictionary<string, string> DefaultAttributes { get; }

    public IReadOnlyDictionary<string, ProviderEventDescriptor> EventMappings { get; }

    public bool SupportsAttributes { get; }

    public bool SupportsProperties { get; }

    public bool SupportsStyles { get; }

    public bool SupportsClasses { get; }

    public bool SupportsEvents { get; }
}
