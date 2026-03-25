namespace Mirage.Core.Providers;

// Most capability flags are currently advisory. Only EventPayloadSupport is
// consumed by the active runtime/provider flow today.
public sealed class ProviderFeatureSet
{
    public ProviderFeatureSet(
        bool supportsAdvancedInputSemantics = false,
        bool supportsOverlayHosting = false,
        bool supportsVirtualization = false,
        bool supportsThemingTokens = false,
        ProviderEventPayloadSupport eventPayloadSupport = ProviderEventPayloadSupport.NameOnly)
    {
        SupportsAdvancedInputSemantics = supportsAdvancedInputSemantics;
        SupportsOverlayHosting = supportsOverlayHosting;
        SupportsVirtualization = supportsVirtualization;
        SupportsThemingTokens = supportsThemingTokens;
        EventPayloadSupport = eventPayloadSupport;
    }

    // Reserved until Mirage grows concrete end-to-end provider seams here.
    public bool SupportsAdvancedInputSemantics { get; }

    public bool SupportsOverlayHosting { get; }

    public bool SupportsVirtualization { get; }

    public bool SupportsThemingTokens { get; }

    // Active capability surface used by current event-bridge contracts.
    public ProviderEventPayloadSupport EventPayloadSupport { get; }
}
