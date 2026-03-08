using Mirage.Core.Providers;
using Mirage.Core.Semantics;

namespace Mirage.Provider.Fast;

public sealed class FastMirageComponentProvider : IMirageComponentProvider
{
    private static readonly IReadOnlyDictionary<string, ProviderPropertyDescriptor> CommonProperties =
        new Dictionary<string, ProviderPropertyDescriptor>(StringComparer.Ordinal)
        {
            ["AutomationId"] = new(PropertyTarget.Attribute, "data-automation-id")
        };

    private static readonly IReadOnlyDictionary<ComponentKind, TypedComponentDescriptor> TypedComponents =
        new Dictionary<ComponentKind, TypedComponentDescriptor>
        {
            [ComponentKind.Root] = new(
                ComponentKind.Root,
                "div",
                defaultAttributes: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["data-mirage-provider"] = "fast"
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
                "fast-text-field",
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
                "fast-button",
                defaultAttributes: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["role"] = "button",
                    ["tabindex"] = "0"
                },
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
        supportsTypedNodes: true,
        supportsElementNodes: true,
        features: new ProviderFeatureSet(
            supportsAdvancedInputSemantics: true,
            supportsOverlayHosting: false,
            supportsVirtualization: false,
            supportsThemingTokens: false,
            eventPayloadSupport: ProviderEventPayloadSupport.InputValueString),
        values: new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["provider"] = "fast",
            ["phase"] = "1",
            ["supports-typed"] = "true",
            ["supports-generic-element"] = "true"
        });

    public TypedComponentDescriptor ResolveTyped(ComponentKind kind)
    {
        if (!TypedComponents.TryGetValue(kind, out var descriptor))
        {
            throw new InvalidOperationException($"FAST provider does not map component kind '{kind}'.");
        }

        return descriptor;
    }

    public ElementComponentDescriptor ResolveElement(string elementName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementName);

        return new ElementComponentDescriptor(
            elementName,
            supportsAttributes: true,
            supportsProperties: true,
            supportsStyles: true,
            supportsClasses: true,
            supportsEvents: true);
    }
}
