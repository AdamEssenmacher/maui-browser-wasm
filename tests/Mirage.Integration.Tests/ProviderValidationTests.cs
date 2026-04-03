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
    public void FluentEntryDescriptor_MapsToMirageOwnedEntryHost()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.Entry);

        Assert.Equal("div", descriptor.ElementTagName);
        Assert.True(descriptor.PropertyMappings.ContainsKey("AutomationId"));
        Assert.False(descriptor.PropertyMappings.ContainsKey("Text"));
        Assert.False(descriptor.PropertyMappings.ContainsKey("Placeholder"));
        Assert.Empty(descriptor.EventMappings);
    }

    [Fact]
    public void FluentEditorDescriptor_MapsToMirageOwnedEditorHost()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.Editor);

        Assert.Equal("div", descriptor.ElementTagName);
        Assert.True(descriptor.PropertyMappings.ContainsKey("AutomationId"));
        Assert.False(descriptor.PropertyMappings.ContainsKey("Text"));
        Assert.False(descriptor.PropertyMappings.ContainsKey("Placeholder"));
        Assert.Empty(descriptor.EventMappings);
    }

    [Fact]
    public void FluentSearchBarDescriptor_MapsToMirageOwnedSearchBarHost()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.SearchBar);

        Assert.Equal("div", descriptor.ElementTagName);
        Assert.True(descriptor.PropertyMappings.ContainsKey("AutomationId"));
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
    public void FluentActivityIndicatorDescriptor_MapsRunningBusyAndColorChannels()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.ActivityIndicator);

        Assert.Equal("div", descriptor.ElementTagName);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["IsRunning"].Target);
        Assert.Equal("data-maui-activity-running", descriptor.PropertyMappings["IsRunning"].Name);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["BusyState"].Target);
        Assert.Equal("aria-busy", descriptor.PropertyMappings["BusyState"].Name);
        Assert.Equal(PropertyTarget.Style, descriptor.PropertyMappings["SpinnerColor"].Target);
        Assert.Equal("--mirage-activity-color", descriptor.PropertyMappings["SpinnerColor"].Name);
    }

    [Fact]
    public void FluentImageDescriptor_MapsSourceToImgSrcAttribute()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.Image);

        Assert.Equal("img", descriptor.ElementTagName);
        Assert.Equal(string.Empty, descriptor.DefaultAttributes["alt"]);
        Assert.Equal("async", descriptor.DefaultAttributes["decoding"]);
        Assert.Equal("eager", descriptor.DefaultAttributes["loading"]);
        Assert.True(descriptor.PropertyMappings.ContainsKey("Source"));
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["Source"].Target);
        Assert.Equal("src", descriptor.PropertyMappings["Source"].Name);
        Assert.True(descriptor.EventMappings.ContainsKey("load"));
        Assert.True(descriptor.EventMappings.ContainsKey("error"));
        Assert.Equal(EventPayloadKind.SourceAttribute, descriptor.EventMappings["load"].PayloadKind);
        Assert.Equal(EventPayloadKind.SourceAttribute, descriptor.EventMappings["error"].PayloadKind);
    }

    [Fact]
    public void FluentImageButtonDescriptor_MapsToMirageOwnedButtonAndSecondaryImage()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.ImageButton);

        Assert.Equal("button", descriptor.ElementTagName);
        Assert.Equal("button", descriptor.DefaultAttributes["type"]);
        Assert.Equal("img", descriptor.SecondaryElementTagName);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["AriaLabel"].Target);
        Assert.Equal("aria-label", descriptor.PropertyMappings["AriaLabel"].Name);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["AriaDescription"].Target);
        Assert.Equal("aria-description", descriptor.PropertyMappings["AriaDescription"].Name);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["Disabled"].Target);
        Assert.Equal("disabled", descriptor.PropertyMappings["Disabled"].Name);
        Assert.Equal(PropertyElementTarget.SecondaryElement, descriptor.PropertyMappings["Source"].ElementTarget);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["Source"].Target);
        Assert.Equal("src", descriptor.PropertyMappings["Source"].Name);
        Assert.Equal(PropertyElementTarget.SecondaryElement, descriptor.PropertyMappings["ImageObjectFit"].ElementTarget);
        Assert.True(descriptor.EventMappings.ContainsKey("click"));
        Assert.True(descriptor.EventMappings.ContainsKey("pointerdown"));
        Assert.True(descriptor.EventMappings.ContainsKey("pointerup"));
        Assert.False(descriptor.EventMappings.ContainsKey("pointerleave"));
        Assert.True(descriptor.EventMappings.ContainsKey("keydown"));
        Assert.Equal(EventPayloadKind.Key, descriptor.EventMappings["keydown"].PayloadKind);
        Assert.True(descriptor.EventMappings.ContainsKey("load"));
        Assert.Equal(EventTarget.SecondaryElement, descriptor.EventMappings["load"].Target);
        Assert.Equal(EventPayloadKind.SourceAttribute, descriptor.EventMappings["load"].PayloadKind);
        Assert.True(descriptor.EventMappings.ContainsKey("error"));
        Assert.Equal(EventTarget.SecondaryElement, descriptor.EventMappings["error"].Target);
        Assert.Equal(EventPayloadKind.SourceAttribute, descriptor.EventMappings["error"].PayloadKind);
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
    public void FluentRadioButtonDescriptor_MapsToMirageOwnedRadioHost()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.RadioButton);

        Assert.Equal("div", descriptor.ElementTagName);
        Assert.True(descriptor.PropertyMappings.ContainsKey("AutomationId"));
        Assert.True(descriptor.EventMappings.ContainsKey("click"));
        Assert.Equal("click", descriptor.EventMappings["click"].DomEventName);
        Assert.Equal(EventPayloadKind.None, descriptor.EventMappings["click"].PayloadKind);
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
        Assert.True(descriptor.EventMappings.ContainsKey("focus"));
        Assert.True(descriptor.EventMappings.ContainsKey("blur"));
    }

    [Fact]
    public void FluentTimePickerDescriptor_MapsInputProjectionChannels()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.TimePicker);

        Assert.Equal("input", descriptor.ElementTagName);
        Assert.Equal("time", descriptor.DefaultAttributes["type"]);
        Assert.Equal("60", descriptor.DefaultAttributes["step"]);
        Assert.Equal(PropertyTarget.InputValue, descriptor.PropertyMappings["Value"].Target);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["Step"].Target);
        Assert.Equal("step", descriptor.PropertyMappings["Step"].Name);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["Format"].Target);
        Assert.Equal("data-maui-time-format", descriptor.PropertyMappings["Format"].Name);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["Disabled"].Target);
        Assert.True(descriptor.EventMappings.ContainsKey("change"));
        Assert.Equal(EventPayloadKind.InputValue, descriptor.EventMappings["change"].PayloadKind);
        Assert.True(descriptor.EventMappings.ContainsKey("focus"));
        Assert.True(descriptor.EventMappings.ContainsKey("blur"));
    }

    [Fact]
    public void FluentDatePickerDescriptor_MapsInputProjectionChannels()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.DatePicker);

        Assert.Equal("input", descriptor.ElementTagName);
        Assert.Equal("date", descriptor.DefaultAttributes["type"]);
        Assert.Equal(PropertyTarget.InputValue, descriptor.PropertyMappings["Value"].Target);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["Min"].Target);
        Assert.Equal("min", descriptor.PropertyMappings["Min"].Name);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["Max"].Target);
        Assert.Equal("max", descriptor.PropertyMappings["Max"].Name);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["Format"].Target);
        Assert.Equal("data-maui-date-format", descriptor.PropertyMappings["Format"].Name);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["Disabled"].Target);
        Assert.True(descriptor.EventMappings.ContainsKey("change"));
        Assert.Equal(EventPayloadKind.InputValue, descriptor.EventMappings["change"].PayloadKind);
        Assert.True(descriptor.EventMappings.ContainsKey("focus"));
        Assert.True(descriptor.EventMappings.ContainsKey("blur"));
    }

    [Fact]
    public void FluentWebViewDescriptor_MapsIframeDiagnosticsProjectionChannels()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.WebView);

        Assert.Equal("iframe", descriptor.ElementTagName);
        Assert.True(descriptor.PropertyMappings.ContainsKey("AutomationId"));
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["SourceKindState"].Target);
        Assert.Equal("data-maui-wv-source-kind", descriptor.PropertyMappings["SourceKindState"].Name);
        Assert.Equal("data-maui-wv-url", descriptor.PropertyMappings["CurrentUrlState"].Name);
        Assert.Equal("data-maui-wv-can-go-back", descriptor.PropertyMappings["CanGoBackState"].Name);
        Assert.Equal("data-maui-wv-can-go-forward", descriptor.PropertyMappings["CanGoForwardState"].Name);
        Assert.Equal("data-maui-wv-eval-capability", descriptor.PropertyMappings["EvalCapabilityState"].Name);
        Assert.Equal("data-maui-wv-user-agent", descriptor.PropertyMappings["UserAgentState"].Name);
        Assert.Empty(descriptor.EventMappings);
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
    public void FluentProgressBarDescriptor_MapsNativeProgressProjectionChannels()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.ProgressBar);

        Assert.Equal("progress", descriptor.ElementTagName);
        Assert.Equal("1", descriptor.DefaultAttributes["max"]);
        Assert.Equal(PropertyTarget.Property, descriptor.PropertyMappings["Value"].Target);
        Assert.Equal("value", descriptor.PropertyMappings["Value"].Name);
        Assert.True(descriptor.PropertyMappings.ContainsKey("AutomationId"));
    }

    [Fact]
    public void FluentRefreshViewDescriptor_MapsDiagnosticsProjectionChannels()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.RefreshView);

        Assert.Equal("div", descriptor.ElementTagName);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["RefreshingState"].Target);
        Assert.Equal("data-maui-rv-refreshing", descriptor.PropertyMappings["RefreshingState"].Name);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["EnabledState"].Target);
        Assert.Equal("data-maui-rv-enabled", descriptor.PropertyMappings["EnabledState"].Name);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["PullActiveState"].Target);
        Assert.Equal("data-maui-rv-pull-active", descriptor.PropertyMappings["PullActiveState"].Name);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["PullArmedState"].Target);
        Assert.Equal("data-maui-rv-pull-armed", descriptor.PropertyMappings["PullArmedState"].Name);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["PullDistanceState"].Target);
        Assert.Equal("data-maui-rv-pull-distance", descriptor.PropertyMappings["PullDistanceState"].Name);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["BusyState"].Target);
        Assert.Equal("aria-busy", descriptor.PropertyMappings["BusyState"].Name);
        Assert.Empty(descriptor.EventMappings);
    }

    [Fact]
    public void FluentSwipeViewDescriptor_MapsDiagnosticsProjectionChannels()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.SwipeView);

        Assert.Equal("div", descriptor.ElementTagName);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["OpenState"].Target);
        Assert.Equal("data-maui-sv-open", descriptor.PropertyMappings["OpenState"].Name);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["OpenSideState"].Target);
        Assert.Equal("data-maui-sv-open-side", descriptor.PropertyMappings["OpenSideState"].Name);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["EnabledState"].Target);
        Assert.Equal("data-maui-sv-enabled", descriptor.PropertyMappings["EnabledState"].Name);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["TransitionModeState"].Target);
        Assert.Equal("data-maui-sv-transition", descriptor.PropertyMappings["TransitionModeState"].Name);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["ThresholdState"].Target);
        Assert.Equal("data-maui-sv-threshold", descriptor.PropertyMappings["ThresholdState"].Name);
        Assert.Empty(descriptor.EventMappings);
    }

    [Fact]
    public void FluentContentContainerDescriptor_MapsCollectionViewRoleDiagnostics()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.ContentContainer);

        Assert.Equal("div", descriptor.ElementTagName);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["CollectionViewHostRole"].Target);
        Assert.Equal("data-maui-cv-host-role", descriptor.PropertyMappings["CollectionViewHostRole"].Name);
        Assert.Equal(PropertyTarget.Attribute, descriptor.PropertyMappings["CollectionViewHostedRole"].Target);
        Assert.Equal("data-maui-cv-hosted-role", descriptor.PropertyMappings["CollectionViewHostedRole"].Name);
        Assert.True(descriptor.EventMappings.ContainsKey("scroll"));
    }

    [Fact]
    public void FluentGraphicsViewDescriptor_MapsCanvasPointerProjectionChannels()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveControl(ComponentKind.GraphicsView);

        Assert.Equal("canvas", descriptor.ElementTagName);
        Assert.True(descriptor.PropertyMappings.ContainsKey("AutomationId"));
        Assert.True(descriptor.EventMappings.ContainsKey("pointerdown"));
        Assert.True(descriptor.EventMappings.ContainsKey("pointermove"));
        Assert.True(descriptor.EventMappings.ContainsKey("pointerup"));
        Assert.True(descriptor.EventMappings.ContainsKey("pointercancel"));
        Assert.True(descriptor.EventMappings.ContainsKey("pointerenter"));
        Assert.True(descriptor.EventMappings.ContainsKey("pointerleave"));
        Assert.Equal(EventPayloadKind.Pointer, descriptor.EventMappings["pointerdown"].PayloadKind);
        Assert.Equal(EventPayloadKind.Pointer, descriptor.EventMappings["pointermove"].PayloadKind);
    }

    [Fact]
    public void FluentProvider_ReportsStructuredPayloadSupport()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();

        Assert.Equal(ProviderEventPayloadSupport.StructuredPayload, provider.Capabilities.Features.EventPayloadSupport);
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
    public void FluentProvider_GenericWebNodeDescriptor_RemainsThinEscapeHatch()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveWebNode("vendor-widget");

        Assert.Equal("vendor-widget", descriptor.ElementName);
        Assert.Empty(descriptor.DefaultAttributes);
        Assert.Empty(descriptor.EventMappings);
        Assert.True(descriptor.SupportsAttributes);
        Assert.True(descriptor.SupportsProperties);
        Assert.True(descriptor.SupportsStyles);
        Assert.True(descriptor.SupportsClasses);
        Assert.True(descriptor.SupportsEvents);
    }

    [Fact]
    public void FluentProvider_MonacoWebNodeDescriptor_OnlyAddsInputBridge()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveWebNode("mirage-monaco-editor");

        var mapping = Assert.Single(descriptor.EventMappings);
        Assert.Equal("input", mapping.Key);
        Assert.Equal("input", mapping.Value.DomEventName);
        Assert.Equal(EventPayloadKind.InputValue, mapping.Value.PayloadKind);
    }

    [Fact]
    public void FluentProvider_TextareaWebNodeDescriptor_MapsInputChangeAndKeyPayloads()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveWebNode("textarea");

        Assert.True(descriptor.EventMappings.TryGetValue("input", out var inputMapping));
        Assert.Equal("input", inputMapping.DomEventName);
        Assert.Equal(EventPayloadKind.InputValue, inputMapping.PayloadKind);

        Assert.True(descriptor.EventMappings.TryGetValue("change", out var changeMapping));
        Assert.Equal("change", changeMapping.DomEventName);
        Assert.Equal(EventPayloadKind.InputValue, changeMapping.PayloadKind);

        Assert.True(descriptor.EventMappings.TryGetValue("keydown", out var keydownMapping));
        Assert.Equal("keydown", keydownMapping.DomEventName);
        Assert.Equal(EventPayloadKind.Key, keydownMapping.PayloadKind);
    }

    [Fact]
    public void FluentProvider_InputWebNodeDescriptor_MapsCheckedChangeAliasToCheckedPayload()
    {
        IMirageComponentProvider provider = new FluentMirageComponentProvider();
        var descriptor = provider.ResolveWebNode("input");

        Assert.True(descriptor.EventMappings.TryGetValue("checkedchange", out var checkedChangeMapping));
        Assert.Equal("change", checkedChangeMapping.DomEventName);
        Assert.Equal(EventPayloadKind.InputChecked, checkedChangeMapping.PayloadKind);
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
