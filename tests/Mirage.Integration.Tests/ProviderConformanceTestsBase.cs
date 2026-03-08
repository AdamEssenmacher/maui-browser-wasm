using Mirage.Core.Providers;
using Mirage.Core.Semantics;
using Xunit;

namespace Mirage.Integration.Tests;

public abstract class ProviderConformanceTestsBase
{
    protected abstract IMirageComponentProvider CreateProvider();

    [Fact]
    public void Conformance_ReportsTypedNodeCapability()
    {
        var provider = CreateProvider();
        Assert.True(provider.Capabilities.SupportsTypedNodes);
    }

    [Fact]
    public void Conformance_ReportsGenericElementCapability()
    {
        var provider = CreateProvider();
        Assert.True(provider.Capabilities.SupportsElementNodes);
    }

    [Fact]
    public void Conformance_ResolvesAllPhaseOneTypedKindsWithoutCanvasRoot()
    {
        var provider = CreateProvider();

        foreach (var kind in Enum.GetValues<ComponentKind>())
        {
            var descriptor = provider.ResolveTyped(kind);
            Assert.False(string.IsNullOrWhiteSpace(descriptor.ElementTagName));
            Assert.False(string.Equals("canvas", descriptor.ElementTagName, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Conformance_PreservesRequestedGenericElementIdentity()
    {
        var provider = CreateProvider();
        var requestedNames = new[] { "vendor-widget", "fast-button", "ui5-input", "x-custom-grid" };

        foreach (var requested in requestedNames)
        {
            var descriptor = provider.ResolveElement(requested);
            Assert.Equal(requested, descriptor.ElementName);
        }
    }

    [Fact]
    public void Conformance_GenericDescriptorSupportsCoreMutationChannels()
    {
        var provider = CreateProvider();
        var descriptor = provider.ResolveElement("vendor-widget");

        Assert.True(descriptor.SupportsAttributes);
        Assert.True(descriptor.SupportsProperties);
        Assert.True(descriptor.SupportsStyles);
        Assert.True(descriptor.SupportsClasses);
        Assert.True(descriptor.SupportsEvents);
    }

    [Fact]
    public void Conformance_EntryInputEventHasInputPayloadMapping()
    {
        var provider = CreateProvider();
        var entry = provider.ResolveTyped(ComponentKind.Entry);

        Assert.True(entry.EventMappings.TryGetValue("input", out var mapping));
        Assert.Equal(EventPayloadKind.InputValue, mapping.PayloadKind);
        Assert.Equal(ProviderEventPayloadSupport.InputValueString, provider.Capabilities.Features.EventPayloadSupport);
    }
}
