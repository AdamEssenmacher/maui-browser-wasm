using Mirage.Core.Providers;
using Mirage.Core.Semantics;

namespace Mirage.Provider.Fluent;

public sealed class FluentMirageComponentProvider : IMirageComponentProvider
{
    private static readonly IReadOnlyDictionary<string, ProviderPropertyDescriptor> CommonProperties =
        new Dictionary<string, ProviderPropertyDescriptor>(StringComparer.Ordinal)
        {
            ["AutomationId"] = new(PropertyTarget.Attribute, "data-automation-id")
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
                propertyMappings: CommonProperties),
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
            [ComponentKind.Label] = new(
                ComponentKind.Label,
                "span",
                propertyMappings: new Dictionary<string, ProviderPropertyDescriptor>(StringComparer.Ordinal)
                {
                    ["AutomationId"] = new(PropertyTarget.Attribute, "data-automation-id"),
                    ["Text"] = new(PropertyTarget.TextContent)
                }),
            [ComponentKind.Entry] = new(
                ComponentKind.Entry,
                "fluent-text-field",
                propertyMappings: new Dictionary<string, ProviderPropertyDescriptor>(StringComparer.Ordinal)
                {
                    ["AutomationId"] = new(PropertyTarget.Attribute, "data-automation-id"),
                    ["Text"] = new(PropertyTarget.InputValue),
                    ["Placeholder"] = new(PropertyTarget.Attribute, "placeholder")
                },
                eventMappings: new Dictionary<string, ProviderEventDescriptor>(StringComparer.Ordinal)
                {
                    ["input"] = new("input", EventTarget.Self, EventPayloadKind.InputValue)
                }),
            [ComponentKind.Button] = new(
                ComponentKind.Button,
                "fluent-button",
                propertyMappings: new Dictionary<string, ProviderPropertyDescriptor>(StringComparer.Ordinal)
                {
                    ["AutomationId"] = new(PropertyTarget.Attribute, "data-automation-id"),
                    ["Text"] = new(PropertyTarget.TextContent)
                },
                eventMappings: new Dictionary<string, ProviderEventDescriptor>(StringComparer.Ordinal)
                {
                    ["click"] = new("click")
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

        return new WebComponentDescriptor(
            elementName,
            supportsAttributes: true,
            supportsProperties: true,
            supportsStyles: true,
            supportsClasses: true,
            supportsEvents: true);
    }
}
