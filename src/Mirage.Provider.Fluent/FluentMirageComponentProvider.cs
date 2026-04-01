using Mirage.Core.Providers;
using Mirage.Core.Semantics;

namespace Mirage.Provider.Fluent;

public sealed class FluentMirageComponentProvider : IMirageComponentProvider
{
    private static readonly IReadOnlyDictionary<string, ProviderPropertyDescriptor> CommonProperties =
        new Dictionary<string, ProviderPropertyDescriptor>(StringComparer.Ordinal)
        {
            ["AutomationId"] = new(PropertyTarget.Attribute, "data-automation-id"),
            ["AriaLabel"] = new(PropertyTarget.Attribute, "aria-label"),
            ["AriaDescription"] = new(PropertyTarget.Attribute, "aria-description"),
            ["AriaRole"] = new(PropertyTarget.Attribute, "role"),
            ["AriaLevel"] = new(PropertyTarget.Attribute, "aria-level"),
            ["HeadingRoleMarker"] = new(PropertyTarget.Attribute, "data-maui-heading-role")
        };

    private static readonly IReadOnlyDictionary<ComponentKind, ControlComponentDescriptor> ControlComponents =
        new Dictionary<ComponentKind, ControlComponentDescriptor>
        {
            [ComponentKind.Root] = new(
                ComponentKind.Root,
                "div",
                defaultAttributes: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["data-mirage-provider"] = "fluent"
                },
                propertyMappings: CommonProperties),
            [ComponentKind.ContentContainer] = new(
                ComponentKind.ContentContainer,
                "div",
                propertyMappings: CommonProperties,
                eventMappings: new Dictionary<string, ProviderEventDescriptor>(StringComparer.Ordinal)
                {
                    ["click"] = new("click"),
                    ["scroll"] = new("scroll")
                }),
            [ComponentKind.VerticalStack] = new(
                ComponentKind.VerticalStack,
                "div",
                propertyMappings: CommonProperties),
            [ComponentKind.HorizontalStack] = new(
                ComponentKind.HorizontalStack,
                "div",
                propertyMappings: CommonProperties),
            [ComponentKind.Grid] = new(
                ComponentKind.Grid,
                "div",
                propertyMappings: CommonProperties),
            [ComponentKind.AbsoluteLayout] = new(
                ComponentKind.AbsoluteLayout,
                "div",
                propertyMappings: CommonProperties),
            [ComponentKind.Layout] = new(
                ComponentKind.Layout,
                "div",
                propertyMappings: CommonProperties),
            [ComponentKind.Label] = new(
                ComponentKind.Label,
                "span",
                propertyMappings: WithCommonProperties(new Dictionary<string, ProviderPropertyDescriptor>(StringComparer.Ordinal)
                {
                    ["Text"] = new(PropertyTarget.TextContent),
                    ["TextHtml"] = new(PropertyTarget.Property, "innerHTML")
                })),
            [ComponentKind.Entry] = new(
                ComponentKind.Entry,
                "div",
                propertyMappings: CommonProperties),
            [ComponentKind.Editor] = new(
                ComponentKind.Editor,
                "div",
                propertyMappings: CommonProperties),
            [ComponentKind.SearchBar] = new(
                ComponentKind.SearchBar,
                "div",
                propertyMappings: CommonProperties),
            [ComponentKind.Button] = new(
                ComponentKind.Button,
                "button",
                defaultAttributes: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["type"] = "button"
                },
                propertyMappings: WithCommonProperties(new Dictionary<string, ProviderPropertyDescriptor>(StringComparer.Ordinal)
                {
                    ["Text"] = new(PropertyTarget.TextContent, ElementTarget: PropertyElementTarget.ValueElement),
                    ["TextDisplay"] = new(PropertyTarget.Style, "display", ElementTarget: PropertyElementTarget.ValueElement),
                    ["TextWhiteSpace"] = new(PropertyTarget.Style, "whiteSpace", ElementTarget: PropertyElementTarget.ValueElement),
                    ["TextOverflow"] = new(PropertyTarget.Style, "overflow", ElementTarget: PropertyElementTarget.ValueElement),
                    ["TextOverflowStyle"] = new(PropertyTarget.Style, "textOverflow", ElementTarget: PropertyElementTarget.ValueElement),
                    ["TextWordBreak"] = new(PropertyTarget.Style, "wordBreak", ElementTarget: PropertyElementTarget.ValueElement),
                    ["TextOverflowWrap"] = new(PropertyTarget.Style, "overflowWrap", ElementTarget: PropertyElementTarget.ValueElement),
                    ["TextMarginTop"] = new(PropertyTarget.Style, "marginTop", ElementTarget: PropertyElementTarget.ValueElement),
                    ["TextMarginRight"] = new(PropertyTarget.Style, "marginRight", ElementTarget: PropertyElementTarget.ValueElement),
                    ["TextMarginBottom"] = new(PropertyTarget.Style, "marginBottom", ElementTarget: PropertyElementTarget.ValueElement),
                    ["TextMarginLeft"] = new(PropertyTarget.Style, "marginLeft", ElementTarget: PropertyElementTarget.ValueElement),
                    ["ImageSource"] = new(PropertyTarget.Attribute, "src", ElementTarget: PropertyElementTarget.SecondaryElement),
                    ["ImageDisplay"] = new(PropertyTarget.Style, "display", ElementTarget: PropertyElementTarget.SecondaryElement),
                    ["ImageAlt"] = new(PropertyTarget.Attribute, "alt", ElementTarget: PropertyElementTarget.SecondaryElement),
                    ["ImageWidth"] = new(PropertyTarget.Style, "width", ElementTarget: PropertyElementTarget.SecondaryElement),
                    ["ImageHeight"] = new(PropertyTarget.Style, "height", ElementTarget: PropertyElementTarget.SecondaryElement),
                    ["ImageObjectFit"] = new(PropertyTarget.Style, "objectFit", ElementTarget: PropertyElementTarget.SecondaryElement),
                    ["ImagePointerEvents"] = new(PropertyTarget.Style, "pointerEvents", ElementTarget: PropertyElementTarget.SecondaryElement),
                    ["TooltipText"] = new(PropertyTarget.Attribute, "title")
                }),
                eventMappings: new Dictionary<string, ProviderEventDescriptor>(StringComparer.Ordinal)
                {
                    ["click"] = new("click")
                },
                valueElementTagName: "span",
                secondaryElementTagName: "img",
                stretchValueElementWidth: false),
            [ComponentKind.ActivityIndicator] = new(
                ComponentKind.ActivityIndicator,
                "div",
                propertyMappings: WithCommonProperties(new Dictionary<string, ProviderPropertyDescriptor>(StringComparer.Ordinal)
                {
                    ["IsRunning"] = new(PropertyTarget.Attribute, "data-maui-activity-running"),
                    ["BusyState"] = new(PropertyTarget.Attribute, "aria-busy"),
                    ["SpinnerColor"] = new(PropertyTarget.Style, "--mirage-activity-color")
                })),
            [ComponentKind.Image] = new(
                ComponentKind.Image,
                "img",
                defaultAttributes: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["alt"] = string.Empty,
                    ["decoding"] = "async",
                    ["loading"] = "eager"
                },
                propertyMappings: WithCommonProperties(new Dictionary<string, ProviderPropertyDescriptor>(StringComparer.Ordinal)
                {
                    ["Source"] = new(PropertyTarget.Attribute, "src"),
                    ["SourceKind"] = new(PropertyTarget.Attribute, "data-maui-image-source-kind"),
                    ["SourceState"] = new(PropertyTarget.Attribute, "data-maui-image-state"),
                    ["IsOpaque"] = new(PropertyTarget.Attribute, "data-maui-image-opaque")
                }),
                eventMappings: new Dictionary<string, ProviderEventDescriptor>(StringComparer.Ordinal)
                {
                    ["load"] = new("load", EventTarget.Self, EventPayloadKind.SourceAttribute),
                    ["error"] = new("error", EventTarget.Self, EventPayloadKind.SourceAttribute)
                }),
            [ComponentKind.ImageButton] = new(
                ComponentKind.ImageButton,
                "button",
                defaultAttributes: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["type"] = "button"
                },
                propertyMappings: WithCommonProperties(new Dictionary<string, ProviderPropertyDescriptor>(StringComparer.Ordinal)
                {
                    ["Disabled"] = new(PropertyTarget.Attribute, "disabled"),
                    ["Source"] = new(PropertyTarget.Attribute, "src", ElementTarget: PropertyElementTarget.SecondaryElement),
                    ["SourceKind"] = new(PropertyTarget.Attribute, "data-maui-image-source-kind"),
                    ["SourceState"] = new(PropertyTarget.Attribute, "data-maui-image-state"),
                    ["IsOpaque"] = new(PropertyTarget.Attribute, "data-maui-image-opaque"),
                    ["ImageDisplay"] = new(PropertyTarget.Style, "display", ElementTarget: PropertyElementTarget.SecondaryElement),
                    ["ImageWidth"] = new(PropertyTarget.Style, "width", ElementTarget: PropertyElementTarget.SecondaryElement),
                    ["ImageHeight"] = new(PropertyTarget.Style, "height", ElementTarget: PropertyElementTarget.SecondaryElement),
                    ["ImageObjectFit"] = new(PropertyTarget.Style, "objectFit", ElementTarget: PropertyElementTarget.SecondaryElement),
                    ["ImageObjectPosition"] = new(PropertyTarget.Style, "objectPosition", ElementTarget: PropertyElementTarget.SecondaryElement),
                    ["ImagePointerEvents"] = new(PropertyTarget.Style, "pointerEvents", ElementTarget: PropertyElementTarget.SecondaryElement),
                    ["ImageAlt"] = new(PropertyTarget.Attribute, "alt", ElementTarget: PropertyElementTarget.SecondaryElement)
                }),
                eventMappings: new Dictionary<string, ProviderEventDescriptor>(StringComparer.Ordinal)
                {
                    ["click"] = new("click"),
                    ["pointerdown"] = new("pointerdown"),
                    ["pointerup"] = new("pointerup"),
                    ["pointercancel"] = new("pointercancel"),
                    ["keydown"] = new("keydown", EventTarget.Self, EventPayloadKind.Key),
                    ["load"] = new("load", EventTarget.SecondaryElement, EventPayloadKind.SourceAttribute),
                    ["error"] = new("error", EventTarget.SecondaryElement, EventPayloadKind.SourceAttribute)
                },
                secondaryElementTagName: "img"),
            [ComponentKind.Border] = new(
                ComponentKind.Border,
                "div",
                propertyMappings: WithCommonProperties(new Dictionary<string, ProviderPropertyDescriptor>(StringComparer.Ordinal)
                {
                    ["StrokeDashOffset"] = new(PropertyTarget.Attribute, "data-maui-border-dash-offset")
                })),
            [ComponentKind.BoxView] = new(
                ComponentKind.BoxView,
                "div",
                propertyMappings: CommonProperties),
            [ComponentKind.ShapeView] = new(
                ComponentKind.ShapeView,
                "div",
                propertyMappings: CommonProperties),
            [ComponentKind.CheckBox] = new(
                ComponentKind.CheckBox,
                "input",
                defaultAttributes: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["type"] = "checkbox"
                },
                propertyMappings: WithCommonProperties(new Dictionary<string, ProviderPropertyDescriptor>(StringComparer.Ordinal)
                {
                    ["IsChecked"] = new(PropertyTarget.InputChecked),
                    ["Disabled"] = new(PropertyTarget.Attribute, "disabled")
                }),
                eventMappings: new Dictionary<string, ProviderEventDescriptor>(StringComparer.Ordinal)
                {
                    ["change"] = new("change", EventTarget.Self, EventPayloadKind.InputChecked)
                }),
            [ComponentKind.RadioButton] = new(
                ComponentKind.RadioButton,
                "div",
                propertyMappings: CommonProperties,
                eventMappings: new Dictionary<string, ProviderEventDescriptor>(StringComparer.Ordinal)
                {
                    ["click"] = new("click")
                }),
            [ComponentKind.Switch] = new(
                ComponentKind.Switch,
                "input",
                defaultAttributes: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["type"] = "checkbox"
                },
                propertyMappings: WithCommonProperties(new Dictionary<string, ProviderPropertyDescriptor>(StringComparer.Ordinal)
                {
                    ["IsOn"] = new(PropertyTarget.InputChecked),
                    ["Disabled"] = new(PropertyTarget.Attribute, "disabled")
                }),
                eventMappings: new Dictionary<string, ProviderEventDescriptor>(StringComparer.Ordinal)
                {
                    ["change"] = new("change", EventTarget.Self, EventPayloadKind.InputChecked)
                }),
            [ComponentKind.Slider] = new(
                ComponentKind.Slider,
                "input",
                defaultAttributes: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["type"] = "range",
                    ["step"] = "any"
                },
                propertyMappings: WithCommonProperties(new Dictionary<string, ProviderPropertyDescriptor>(StringComparer.Ordinal)
                {
                    ["Minimum"] = new(PropertyTarget.Attribute, "min"),
                    ["Maximum"] = new(PropertyTarget.Attribute, "max"),
                    ["Step"] = new(PropertyTarget.Attribute, "step"),
                    ["Value"] = new(PropertyTarget.InputValue),
                    ["Disabled"] = new(PropertyTarget.Attribute, "disabled"),
                    ["TrackGradient"] = new(PropertyTarget.Style, "--mirage-slider-track-gradient"),
                    ["ThumbAccent"] = new(PropertyTarget.Style, "--mirage-slider-thumb-color"),
                    ["ThumbImage"] = new(PropertyTarget.Style, "--mirage-slider-thumb-image"),
                    ["ThumbImageState"] = new(PropertyTarget.Attribute, "data-maui-slider-thumb-image-state")
                }),
                eventMappings: new Dictionary<string, ProviderEventDescriptor>(StringComparer.Ordinal)
                {
                    ["input"] = new("input", EventTarget.Self, EventPayloadKind.InputValue),
                    ["change"] = new("change", EventTarget.Self, EventPayloadKind.InputValue)
                }),
            [ComponentKind.Picker] = new(
                ComponentKind.Picker,
                "select",
                propertyMappings: WithCommonProperties(new Dictionary<string, ProviderPropertyDescriptor>(StringComparer.Ordinal)
                {
                    ["Title"] = new(PropertyTarget.Attribute, "aria-label"),
                    ["SelectedValue"] = new(PropertyTarget.InputValue),
                    ["Disabled"] = new(PropertyTarget.Attribute, "disabled")
                }),
                eventMappings: new Dictionary<string, ProviderEventDescriptor>(StringComparer.Ordinal)
                {
                    ["change"] = new("change", EventTarget.Self, EventPayloadKind.InputValue),
                    ["focus"] = new("focus"),
                    ["blur"] = new("blur")
                }),
            [ComponentKind.DatePicker] = new(
                ComponentKind.DatePicker,
                "input",
                defaultAttributes: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["type"] = "date"
                },
                propertyMappings: WithCommonProperties(new Dictionary<string, ProviderPropertyDescriptor>(StringComparer.Ordinal)
                {
                    ["Value"] = new(PropertyTarget.InputValue),
                    ["Min"] = new(PropertyTarget.Attribute, "min"),
                    ["Max"] = new(PropertyTarget.Attribute, "max"),
                    ["Format"] = new(PropertyTarget.Attribute, "data-maui-date-format"),
                    ["Disabled"] = new(PropertyTarget.Attribute, "disabled")
                }),
                eventMappings: new Dictionary<string, ProviderEventDescriptor>(StringComparer.Ordinal)
                {
                    ["change"] = new("change", EventTarget.Self, EventPayloadKind.InputValue),
                    ["focus"] = new("focus"),
                    ["blur"] = new("blur")
                }),
            [ComponentKind.TimePicker] = new(
                ComponentKind.TimePicker,
                "input",
                defaultAttributes: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["type"] = "time",
                    ["step"] = "60"
                },
                propertyMappings: WithCommonProperties(new Dictionary<string, ProviderPropertyDescriptor>(StringComparer.Ordinal)
                {
                    ["Value"] = new(PropertyTarget.InputValue),
                    ["Step"] = new(PropertyTarget.Attribute, "step"),
                    ["Format"] = new(PropertyTarget.Attribute, "data-maui-time-format"),
                    ["Disabled"] = new(PropertyTarget.Attribute, "disabled")
                }),
                eventMappings: new Dictionary<string, ProviderEventDescriptor>(StringComparer.Ordinal)
                {
                    ["change"] = new("change", EventTarget.Self, EventPayloadKind.InputValue),
                    ["focus"] = new("focus"),
                    ["blur"] = new("blur")
                }),
            [ComponentKind.Stepper] = new(
                ComponentKind.Stepper,
                "div",
                propertyMappings: WithCommonProperties(new Dictionary<string, ProviderPropertyDescriptor>(StringComparer.Ordinal)
                {
                    ["Minimum"] = new(PropertyTarget.Attribute, "data-maui-stepper-min"),
                    ["Maximum"] = new(PropertyTarget.Attribute, "data-maui-stepper-max"),
                    ["Step"] = new(PropertyTarget.Attribute, "data-maui-stepper-step"),
                    ["Value"] = new(PropertyTarget.Attribute, "data-maui-stepper-value"),
                    ["Disabled"] = new(PropertyTarget.Attribute, "data-maui-stepper-disabled"),
                    ["DecrementText"] = new(PropertyTarget.TextContent, ElementTarget: PropertyElementTarget.SecondaryElement),
                    ["IncrementText"] = new(PropertyTarget.TextContent, ElementTarget: PropertyElementTarget.ValueElement),
                    ["DecrementDisabled"] = new(PropertyTarget.Attribute, "disabled", ElementTarget: PropertyElementTarget.SecondaryElement),
                    ["IncrementDisabled"] = new(PropertyTarget.Attribute, "disabled", ElementTarget: PropertyElementTarget.ValueElement)
                }),
                eventMappings: new Dictionary<string, ProviderEventDescriptor>(StringComparer.Ordinal)
                {
                    ["decrement"] = new("click", EventTarget.SecondaryElement),
                    ["increment"] = new("click", EventTarget.ValueElement)
                },
                valueElementTagName: "button",
                secondaryElementTagName: "button",
                stretchValueElementWidth: false),
            [ComponentKind.ProgressBar] = new(
                ComponentKind.ProgressBar,
                "progress",
                defaultAttributes: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["max"] = "1"
                },
                propertyMappings: WithCommonProperties(new Dictionary<string, ProviderPropertyDescriptor>(StringComparer.Ordinal)
                {
                    ["Value"] = new(PropertyTarget.Property, "value")
                })),
            [ComponentKind.IndicatorView] = new(
                ComponentKind.IndicatorView,
                "div",
                propertyMappings: CommonProperties),
            [ComponentKind.Map] = new(
                ComponentKind.Map,
                "div",
                propertyMappings: CommonProperties,
                eventMappings: new Dictionary<string, ProviderEventDescriptor>(StringComparer.Ordinal)
                {
                    ["mapregionchanged"] = new("mirage-map-region-changed", EventTarget.Self, EventPayloadKind.InputValue),
                    ["mapclicked"] = new("mirage-map-clicked", EventTarget.Self, EventPayloadKind.InputValue),
                    ["pinclicked"] = new("mirage-map-pin-clicked", EventTarget.Self, EventPayloadKind.InputValue),
                    ["pininfowindowclicked"] = new("mirage-map-pin-info-window-clicked", EventTarget.Self, EventPayloadKind.InputValue),
                    ["mapdiagnostic"] = new("mirage-map-diagnostic", EventTarget.Self, EventPayloadKind.InputValue)
                })
        };

    public ProviderCapabilities Capabilities { get; } = new(
        supportsControlNodes: true,
        supportsWebNodes: true,
        features: new ProviderFeatureSet(
            supportsAdvancedInputSemantics: true,
            supportsOverlayHosting: false,
            supportsVirtualization: false,
            supportsThemingTokens: true,
            eventPayloadSupport: ProviderEventPayloadSupport.InputValueString),
        values: new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["provider"] = "fluent",
            ["phase"] = "1",
            ["supports-control-nodes"] = "true",
            ["supports-web-nodes"] = "true"
        });

    public ControlComponentDescriptor ResolveControl(ComponentKind kind)
    {
        if (!ControlComponents.TryGetValue(kind, out var descriptor))
        {
            throw new InvalidOperationException($"Fluent provider does not map component kind '{kind}'.");
        }

        return descriptor;
    }

    public WebComponentDescriptor ResolveWebNode(string elementName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementName);

        if (string.Equals(elementName, "input", StringComparison.OrdinalIgnoreCase))
        {
            return new WebComponentDescriptor(
                elementName,
                eventMappings: new Dictionary<string, ProviderEventDescriptor>(StringComparer.Ordinal)
                {
                    ["input"] = new("input", EventTarget.Self, EventPayloadKind.InputValue),
                    ["change"] = new("change", EventTarget.Self, EventPayloadKind.InputValue),
                    ["checkedchange"] = new("change", EventTarget.Self, EventPayloadKind.InputChecked),
                    ["search"] = new("search", EventTarget.Self, EventPayloadKind.InputValue),
                    ["keydown"] = new("keydown", EventTarget.Self, EventPayloadKind.Key)
                },
                supportsAttributes: true,
                supportsProperties: true,
                supportsStyles: true,
                supportsClasses: true,
                supportsEvents: true);
        }

        if (string.Equals(elementName, "textarea", StringComparison.OrdinalIgnoreCase))
        {
            return new WebComponentDescriptor(
                elementName,
                eventMappings: new Dictionary<string, ProviderEventDescriptor>(StringComparer.Ordinal)
                {
                    ["input"] = new("input", EventTarget.Self, EventPayloadKind.InputValue),
                    ["change"] = new("change", EventTarget.Self, EventPayloadKind.InputValue),
                    ["keydown"] = new("keydown", EventTarget.Self, EventPayloadKind.Key)
                },
                supportsAttributes: true,
                supportsProperties: true,
                supportsStyles: true,
                supportsClasses: true,
                supportsEvents: true);
        }

        if (string.Equals(elementName, "button", StringComparison.OrdinalIgnoreCase))
        {
            return new WebComponentDescriptor(
                elementName,
                eventMappings: new Dictionary<string, ProviderEventDescriptor>(StringComparer.Ordinal)
                {
                    ["click"] = new("click"),
                    ["keydown"] = new("keydown", EventTarget.Self, EventPayloadKind.Key)
                },
                supportsAttributes: true,
                supportsProperties: true,
                supportsStyles: true,
                supportsClasses: true,
                supportsEvents: true);
        }

        if (string.Equals(elementName, "mirage-monaco-editor", StringComparison.Ordinal))
        {
            return new WebComponentDescriptor(
                elementName,
                eventMappings: new Dictionary<string, ProviderEventDescriptor>(StringComparer.Ordinal)
                {
                    ["input"] = new("input", EventTarget.Self, EventPayloadKind.InputValue)
                },
                supportsAttributes: true,
                supportsProperties: true,
                supportsStyles: true,
                supportsClasses: true,
                supportsEvents: true);
        }

        return new WebComponentDescriptor(
            elementName,
            supportsAttributes: true,
            supportsProperties: true,
            supportsStyles: true,
            supportsClasses: true,
            supportsEvents: true);
    }

    private static IReadOnlyDictionary<string, ProviderPropertyDescriptor> WithCommonProperties(
        IReadOnlyDictionary<string, ProviderPropertyDescriptor> specificProperties)
    {
        var merged = new Dictionary<string, ProviderPropertyDescriptor>(CommonProperties, StringComparer.Ordinal);
        foreach (var pair in specificProperties)
        {
            merged[pair.Key] = pair.Value;
        }

        return merged;
    }
}
