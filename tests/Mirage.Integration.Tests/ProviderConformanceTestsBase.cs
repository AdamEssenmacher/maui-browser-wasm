using Mirage.Core.Providers;
using Mirage.Core.Semantics;
using Xunit;

namespace Mirage.Integration.Tests;

public abstract class ProviderConformanceTestsBase
{
    protected abstract IMirageComponentProvider CreateProvider();

    [Fact]
    public void Conformance_ReportsControlNodeCapability()
    {
        var provider = CreateProvider();
        Assert.True(provider.Capabilities.SupportsControlNodes);
    }

    [Fact]
    public void Conformance_ReportsWebNodeCapability()
    {
        var provider = CreateProvider();
        Assert.True(provider.Capabilities.SupportsWebNodes);
    }

    [Fact]
    public void Conformance_ResolvesAllPhaseOneControlKindsWithoutCanvasRoot()
    {
        var provider = CreateProvider();

        foreach (var kind in Enum.GetValues<ComponentKind>())
        {
            var descriptor = provider.ResolveControl(kind);
            Assert.False(string.IsNullOrWhiteSpace(descriptor.ElementTagName));
            Assert.False(string.Equals("canvas", descriptor.ElementTagName, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Conformance_PreservesRequestedWebNodeIdentity()
    {
        var provider = CreateProvider();
        var requestedNames = new[] { "vendor-widget", "fluent-button", "ui5-input", "x-custom-grid" };

        foreach (var requested in requestedNames)
        {
            var descriptor = provider.ResolveWebNode(requested);
            Assert.Equal(requested, descriptor.ElementName);
        }
    }

    [Fact]
    public void Conformance_WebNodeDescriptorSupportsCoreMutationChannels()
    {
        var provider = CreateProvider();
        var descriptor = provider.ResolveWebNode("vendor-widget");

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
        var entry = provider.ResolveControl(ComponentKind.Entry);

        Assert.True(entry.EventMappings.TryGetValue("input", out var mapping));
        Assert.Equal(EventPayloadKind.InputValue, mapping.PayloadKind);
        Assert.Equal(ProviderEventPayloadSupport.InputValueString, provider.Capabilities.Features.EventPayloadSupport);
    }

    [Fact]
    public void Conformance_WebInputNodeInputEventHasInputPayloadMapping()
    {
        var provider = CreateProvider();
        var input = provider.ResolveWebNode("input");

        Assert.True(input.EventMappings.TryGetValue("input", out var mapping));
        Assert.Equal(EventPayloadKind.InputValue, mapping.PayloadKind);
    }
}
