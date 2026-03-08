namespace Mirage.Core.Providers;

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

    public bool SupportsAdvancedInputSemantics { get; }

    public bool SupportsOverlayHosting { get; }

    public bool SupportsVirtualization { get; }

    public bool SupportsThemingTokens { get; }

    public ProviderEventPayloadSupport EventPayloadSupport { get; }
}
