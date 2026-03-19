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
    public void FluentButtonDescriptor_MapsToMirageOwnedButtonShell()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.Button);

        Assert.Equal("button", descriptor.ElementTagName);
        Assert.Equal("span", descriptor.ValueElementTagName);
        Assert.Equal("img", descriptor.SecondaryElementTagName);
        Assert.False(descriptor.StretchValueElementWidth);
        Assert.Equal(PropertyElementTarget.ValueElement, descriptor.PropertyMappings["Text"].ElementTarget);
        Assert.Equal(PropertyElementTarget.SecondaryElement, descriptor.PropertyMappings["ImageSource"].ElementTarget);
        Assert.True(descriptor.EventMappings.ContainsKey("click"));
    }

    [Fact]
    public void FluentImageDescriptor_MapsSourceToImgSrcAttribute()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.Image);

        Assert.Equal("img", descriptor.ElementTagName);
        Assert.True(descriptor.PropertyMappings.ContainsKey("Source"));
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["Source"].Target);
        Assert.Equal("src", descriptor.PropertyMappings["Source"].Name);
    }

    [Fact]
    public void FluentBorderDescriptor_MapsAutomationId()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.Border);

        Assert.Equal("div", descriptor.ElementTagName);
        Assert.True(descriptor.PropertyMappings.ContainsKey("AutomationId"));
        Assert.True(descriptor.PropertyMappings.ContainsKey("StrokeDashOffset"));
    }

    [Fact]
    public void FluentCheckBoxDescriptor_MapsCheckedAndDisabledState()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.CheckBox);

        Assert.Equal("input", descriptor.ElementTagName);
        Assert.Equal(PropertyTarget.InputChecked, descriptor.PropertyMappings["IsChecked"].Target);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["Disabled"].Target);
        Assert.Equal("disabled", descriptor.PropertyMappings["Disabled"].Name);
        Assert.True(descriptor.EventMappings.ContainsKey("change"));
    }

    [Fact]
    public void FluentSwitchDescriptor_MapsToggleAndDisabledState()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.Switch);

        Assert.Equal("input", descriptor.ElementTagName);
        Assert.Equal(PropertyTarget.InputChecked, descriptor.PropertyMappings["IsOn"].Target);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["Disabled"].Target);
        Assert.Equal("disabled", descriptor.PropertyMappings["Disabled"].Name);
        Assert.True(descriptor.EventMappings.ContainsKey("change"));
        Assert.Equal(EventPayloadKind.InputChecked, descriptor.EventMappings["change"].PayloadKind);
    }

    [Fact]
    public void FluentSliderDescriptor_MapsThumbImageProjectionChannels()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.Slider);

        Assert.Equal("input", descriptor.ElementTagName);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["Disabled"].Target);
        Assert.Equal(PropertyTarget.Style, descriptor.PropertyMappings["TrackGradient"].Target);
        Assert.Equal("--mirage-slider-track-gradient", descriptor.PropertyMappings["TrackGradient"].Name);
        Assert.Equal(PropertyTarget.Style, descriptor.PropertyMappings["ThumbAccent"].Target);
        Assert.Equal("--mirage-slider-thumb-color", descriptor.PropertyMappings["ThumbAccent"].Name);
        Assert.Equal(PropertyTarget.Style, descriptor.PropertyMappings["ThumbImage"].Target);
        Assert.Equal("--mirage-slider-thumb-image", descriptor.PropertyMappings["ThumbImage"].Name);
    }

    [Fact]
    public void FluentPickerDescriptor_MapsSelectProjectionChannels()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.Picker);

        Assert.Equal("select", descriptor.ElementTagName);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["Title"].Target);
        Assert.Equal("aria-label", descriptor.PropertyMappings["Title"].Name);
        Assert.Equal(PropertyTarget.InputValue, descriptor.PropertyMappings["SelectedValue"].Target);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["Disabled"].Target);
        Assert.Equal("disabled", descriptor.PropertyMappings["Disabled"].Name);
        Assert.True(descriptor.EventMappings.ContainsKey("change"));
        Assert.Equal(EventPayloadKind.InputValue, descriptor.EventMappings["change"].PayloadKind);
    }

    [Fact]
    public void FluentStepperDescriptor_MapsSplitButtonProjectionChannels()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.Stepper);

        Assert.Equal("div", descriptor.ElementTagName);
        Assert.Equal("button", descriptor.ValueElementTagName);
        Assert.Equal("button", descriptor.SecondaryElementTagName);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["Minimum"].Target);
        Assert.Equal("data-maui-stepper-min", descriptor.PropertyMappings["Minimum"].Name);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["Maximum"].Target);
        Assert.Equal("data-maui-stepper-max", descriptor.PropertyMappings["Maximum"].Name);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["Step"].Target);
        Assert.Equal("data-maui-stepper-step", descriptor.PropertyMappings["Step"].Name);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["Value"].Target);
        Assert.Equal("data-maui-stepper-value", descriptor.PropertyMappings["Value"].Name);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["Disabled"].Target);
        Assert.Equal("data-maui-stepper-disabled", descriptor.PropertyMappings["Disabled"].Name);
        Assert.Equal(PropertyElementTarget.SecondaryElement, descriptor.PropertyMappings["DecrementText"].ElementTarget);
        Assert.Equal(PropertyElementTarget.ValueElement, descriptor.PropertyMappings["IncrementText"].ElementTarget);
        Assert.Equal(EventTarget.SecondaryElement, descriptor.EventMappings["decrement"].Target);
        Assert.Equal(EventTarget.ValueElement, descriptor.EventMappings["increment"].Target);
    }

    [Fact]
    public void FluentMapDescriptor_MapsToMirageOwnedMapHost()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.Map);

        Assert.Equal("div", descriptor.ElementTagName);
        Assert.True(descriptor.PropertyMappings.ContainsKey("AutomationId"));
        Assert.Equal(5, descriptor.EventMappings.Count);
        Assert.True(descriptor.EventMappings.ContainsKey("mapregionchanged"));
        Assert.Equal("mirage-map-region-changed", descriptor.EventMappings["mapregionchanged"].DomEventName);
        Assert.True(descriptor.EventMappings.ContainsKey("mapclicked"));
        Assert.True(descriptor.EventMappings.ContainsKey("pinclicked"));
        Assert.True(descriptor.EventMappings.ContainsKey("pininfowindowclicked"));
        Assert.True(descriptor.EventMappings.ContainsKey("mapdiagnostic"));
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
            ComponentKind.Grid,
            ComponentKind.AbsoluteLayout,
            ComponentKind.Layout
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
