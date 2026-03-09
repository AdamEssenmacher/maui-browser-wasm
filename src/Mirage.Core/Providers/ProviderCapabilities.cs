namespace Mirage.Core.Providers;

public sealed class ProviderCapabilities
{
    public static readonly ProviderCapabilities None = new(
        supportsControlNodes: false,
        supportsWebNodes: false,
        features: new ProviderFeatureSet());

    public ProviderCapabilities(
        bool supportsControlNodes = true,
        bool supportsWebNodes = false,
        ProviderFeatureSet? features = null,
        IReadOnlyDictionary<string, string>? values = null)
    {
        SupportsControlNodes = supportsControlNodes;
        SupportsWebNodes = supportsWebNodes;
        Features = features ?? new ProviderFeatureSet();
        Values = values ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

    public bool SupportsControlNodes { get; }

    public bool SupportsWebNodes { get; }

    public ProviderFeatureSet Features { get; }

    public IReadOnlyDictionary<string, string> Values { get; }
}
