# Mirage Provider Authoring (Hybrid Tree)

A Mirage provider implements `IMirageComponentProvider` and must realize both typed built-in nodes and generic `ElementNode` paths.

## Required contract

```csharp
public interface IMirageComponentProvider
{
    ProviderCapabilities Capabilities { get; }
    TypedComponentDescriptor ResolveTyped(ComponentKind kind);
    ElementComponentDescriptor ResolveElement(string elementName);
}
```

Capability surface is structured through:

- `ProviderCapabilities.SupportsTypedNodes`
- `ProviderCapabilities.SupportsElementNodes`
- `ProviderCapabilities.Features` (`ProviderFeatureSet`)

## Typed realization

`ResolveTyped` maps Mirage-owned semantic kinds to concrete realization metadata.

Phase-1 typed kinds:
- `Root`
- `ContentContainer`
- `VerticalStack`
- `HorizontalStack`
- `Grid`
- `Label`
- `Entry`
- `Button`

Typed descriptors provide:
- `ElementTagName`
- optional `ValueElementTagName`
- default attributes
- property mappings
- event mappings

### Layout and visual realization expectations

Providers must preserve typed layout/container identity and allow Mirage-authored styles to flow through without reinterpretation.

- Stack kinds (`VerticalStack`, `HorizontalStack`) are realized as typed layout containers and must honor style mutations for `display`, `flexDirection`, `gap`, alignment, spacing, visibility, and sizing.
- `Grid` is realized as a typed layout container and must honor style mutations for CSS grid templates, row/column gaps, placement, span, and cell alignment (`gridRow*`, `gridColumn*`, `justifySelf`, `alignSelf`).
- Shared visual projection from handlers includes background/opacity/size/visibility/margin/padding styles. Providers must not suppress these updates for typed containers.
- Background support currently required by Mirage wave policy is solid + linear-gradient output; unsupported paint semantics are deferred and should remain deterministic (clear/skip instead of provider-specific reinterpretation).

## Generic realization (`ElementNode`)

`ResolveElement` is required for generic element rendering and must preserve element identity.

Example:
- request: `"vendor-widget"`
- descriptor must keep `ElementName == "vendor-widget"`

`ResolveElement(...)` is capability/validation-oriented, not semantic remapping.
Providers must not reinterpret the requested generic element into a different identity.

Generic descriptors declare support/capability for:
- attributes
- properties (typed `MirageValue`)
- styles
- classes
- events

### Generic event payload policy (current)

- Default generic events are name-only (`EventPayloadKind.None`).
- Use `EventPayloadKind.InputValue` only for explicit input-like mappings.
- Structured generic payload extraction is currently deferred.
- Providers should advertise payload extraction support through typed capabilities (`ProviderFeatureSet.EventPayloadSupport`).

## Typed value model (`MirageValue`)

Node properties are typed:
- `Null`
- `String`
- `Bool`
- `Number`
- `Json` (structured payload)

Attributes/styles remain string-based in this cutover.

`MirageValue.Json` constraints:
- allowed shapes are `JsonObject` and `JsonArray`
- primitive JSON values are not valid
- JSON payloads are deep-cloned and compared structurally

## Boundaries

- Keep provider-specific logic in provider projects.
- Do not move provider mapping logic into `Mirage.Core` or `Mirage.Web`.
- Do not reinterpret generic element identity into provider-specific control identity.
- `Mirage.Web` remains provider-neutral runtime infrastructure.
