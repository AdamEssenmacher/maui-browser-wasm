using Mirage.Core.Providers;
using Mirage.Core.Semantics;
using Mirage.Provider.Fluent;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class ProviderValidationTests
{
    [Fact]
    public void GetRequiredProvider_ThrowsWhenProviderMissing()
    {
        var services = new ServiceCollection().BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(
            () => MirageProviderValidation.GetRequiredProvider(services));

        Assert.Contains("IMirageComponentProvider", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FluentProvider_ResolvesAllPhaseOneKinds()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();

        foreach (var kind in Enum.GetValues<ComponentKind>())
        {
            var descriptor = provider.ResolveControl(kind);
            Assert.Equal(kind, descriptor.Kind);
            Assert.False(string.IsNullOrWhiteSpace(descriptor.ElementTagName));
        }
    }

    [Fact]
    public void FluentEntryDescriptor_ExposesInputMappingMetadata()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.Entry);

        Assert.Equal("fluent-text-field", descriptor.ElementTagName);
        Assert.True(descriptor.PropertyMappings.ContainsKey("Text"));
        Assert.True(descriptor.PropertyMappings.ContainsKey("Placeholder"));
        Assert.True(descriptor.EventMappings.ContainsKey("input"));
    }

    [Fact]
    public void FluentButtonDescriptor_MapsToFluentWebComponent()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.Button);

        Assert.Equal("fluent-button", descriptor.ElementTagName);
        Assert.True(descriptor.EventMappings.ContainsKey("click"));
    }

    [Fact]
    public void FluentProvider_ResolvesHorizontalStackSemanticKind()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.HorizontalStack);

        Assert.Equal(ComponentKind.HorizontalStack, descriptor.Kind);
        Assert.Equal("div", descriptor.ElementTagName);
    }

    [Fact]
    public void FluentProvider_ResolvesGridSemanticKind()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.Grid);

        Assert.Equal(ComponentKind.Grid, descriptor.Kind);
        Assert.Equal("div", descriptor.ElementTagName);
    }

    [Fact]
    public void FluentLayoutDescriptors_ExposeAutomationIdMapping()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var layoutKinds = new[]
        {
            ComponentKind.Root,
            ComponentKind.ContentContainer,
            ComponentKind.VerticalStack,
            ComponentKind.HorizontalStack,
            ComponentKind.Grid
        };

        foreach (var kind in layoutKinds)
        {
            var descriptor = provider.ResolveControl(kind);
            Assert.True(descriptor.PropertyMappings.ContainsKey("AutomationId"));
        }
    }

    [Fact]
    public void FluentProvider_ResolvesWebNodeWithoutReinterpretingIdentity()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveWebNode("vendor-widget");

        Assert.Equal("vendor-widget", descriptor.ElementName);
        Assert.True(descriptor.SupportsProperties);
        Assert.True(descriptor.SupportsEvents);
    }

    [Fact]
    public void ProviderWithoutWebNodeCapability_FailsDeterministically()
    {
        IMirageComponentProvider provider = new ControlOnlyProvider();

        var exception = Assert.Throws<InvalidOperationException>(() => provider.ResolveWebNode("vendor-widget"));
        Assert.Contains("web", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ControlOnlyProvider : IMirageComponentProvider
    {
        public ProviderCapabilities Capabilities { get; } = new(
            supportsControlNodes: true,
            supportsWebNodes: false,
            features: new ProviderFeatureSet(eventPayloadSupport: ProviderEventPayloadSupport.NameOnly));

        public ControlComponentDescriptor ResolveControl(ComponentKind kind)
        {
            return new ControlComponentDescriptor(kind, "div");
        }

        public WebComponentDescriptor ResolveWebNode(string elementName)
        {
            _ = elementName;
            throw new InvalidOperationException("Provider does not support web nodes.");
        }
    }
}
