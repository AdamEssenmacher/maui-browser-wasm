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
    public void Conformance_ResolvesAllPhaseOneControlKinds_WithGraphicsViewAsTheOnlyCanvasRootException()
    {
        var provider = CreateProvider();

        foreach (var kind in Enum.GetValues<ComponentKind>())
        {
            var descriptor = provider.ResolveControl(kind);
            Assert.False(string.IsNullOrWhiteSpace(descriptor.ElementTagName));
            if (kind == ComponentKind.GraphicsView)
            {
                Assert.Equal("canvas", descriptor.ElementTagName);
                continue;
            }

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
    public void Conformance_EntryDescriptorUsesHostOnlyInputSemantics()
    {
        var provider = CreateProvider();
        var entry = provider.ResolveControl(ComponentKind.Entry);

        Assert.Equal("div", entry.ElementTagName);
        Assert.True(entry.PropertyMappings.ContainsKey("AutomationId"));
        Assert.False(entry.PropertyMappings.ContainsKey("Text"));
        Assert.False(entry.PropertyMappings.ContainsKey("Placeholder"));
        Assert.Empty(entry.EventMappings);
    }

    [Fact]
    public void Conformance_EditorDescriptorUsesHostOnlyInputSemantics()
    {
        var provider = CreateProvider();
        var editor = provider.ResolveControl(ComponentKind.Editor);

        Assert.Equal("div", editor.ElementTagName);
        Assert.True(editor.PropertyMappings.ContainsKey("AutomationId"));
        Assert.False(editor.PropertyMappings.ContainsKey("Text"));
        Assert.False(editor.PropertyMappings.ContainsKey("Placeholder"));
        Assert.Empty(editor.EventMappings);
    }

    [Fact]
    public void Conformance_WebInputNodeInputEventHasInputPayloadMapping()
    {
        var provider = CreateProvider();
        var input = provider.ResolveWebNode("input");

        Assert.True(input.EventMappings.TryGetValue("input", out var mapping));
        Assert.Equal(EventPayloadKind.InputValue, mapping.PayloadKind);
    }

    [Fact]
    public void Conformance_WebInputNodeKeydownEventHasKeyPayloadMapping()
    {
        var provider = CreateProvider();
        var input = provider.ResolveWebNode("input");

        Assert.True(input.EventMappings.TryGetValue("keydown", out var mapping));
        Assert.Equal(EventPayloadKind.Key, mapping.PayloadKind);
    }

    [Fact]
    public void Conformance_WebInputNodeCheckedChangeAliasHasCheckedPayloadMapping()
    {
        var provider = CreateProvider();
        var input = provider.ResolveWebNode("input");

        Assert.True(input.EventMappings.TryGetValue("checkedchange", out var mapping));
        Assert.Equal(EventPayloadKind.InputChecked, mapping.PayloadKind);
        Assert.Equal("change", mapping.DomEventName);
    }

    [Fact]
    public void Conformance_WebTextAreaNodeSupportsInputValueAndKeyPayloadMappings()
    {
        var provider = CreateProvider();
        var textarea = provider.ResolveWebNode("textarea");

        Assert.True(textarea.EventMappings.TryGetValue("input", out var inputMapping));
        Assert.Equal(EventPayloadKind.InputValue, inputMapping.PayloadKind);

        Assert.True(textarea.EventMappings.TryGetValue("change", out var changeMapping));
        Assert.Equal(EventPayloadKind.InputValue, changeMapping.PayloadKind);

        Assert.True(textarea.EventMappings.TryGetValue("keydown", out var keyMapping));
        Assert.Equal(EventPayloadKind.Key, keyMapping.PayloadKind);
    }
}
