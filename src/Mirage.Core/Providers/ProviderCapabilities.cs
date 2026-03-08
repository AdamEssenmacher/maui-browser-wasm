namespace Mirage.Core.Providers;

public sealed class ProviderCapabilities
{
    public static readonly ProviderCapabilities None = new(
        supportsTypedNodes: false,
        supportsElementNodes: false,
        features: new ProviderFeatureSet());

    public ProviderCapabilities(
        bool supportsTypedNodes = true,
        bool supportsElementNodes = false,
        ProviderFeatureSet? features = null,
        IReadOnlyDictionary<string, string>? values = null)
    {
        SupportsTypedNodes = supportsTypedNodes;
        SupportsElementNodes = supportsElementNodes;
        Features = features ?? new ProviderFeatureSet();
        Values = values ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

    public bool SupportsTypedNodes { get; }

    public bool SupportsElementNodes { get; }

    public ProviderFeatureSet Features { get; }

    public IReadOnlyDictionary<string, string> Values { get; }
}
