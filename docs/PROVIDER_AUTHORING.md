# Mirage Provider Authoring (Hybrid Tree)

A Mirage provider implements `IMirageComponentProvider` and must realize both built-in `ControlNode` paths and extensible `WebNode` paths.

Mirage providers are adapters from Mirage semantic kinds to complete web component libraries (for example, Fluent UI Web Components), while preserving a raw BYO web-node path for arbitrary custom/vendor elements.

## Required contract

```csharp
public interface IMirageComponentProvider
{
    ProviderCapabilities Capabilities { get; }
    ControlComponentDescriptor ResolveControl(ComponentKind kind);
    WebComponentDescriptor ResolveWebNode(string elementName);
}
```

Capability surface is structured through:

- `ProviderCapabilities.SupportsControlNodes`
- `ProviderCapabilities.SupportsWebNodes`
- `ProviderCapabilities.Features` (`ProviderFeatureSet`)

## Control-node realization

`ResolveControl` maps Mirage-owned semantic kinds to concrete realization metadata.

Current polished reference-provider example:
- `Mirage.Provider.Fluent` maps Mirage control nodes to Fluent UI Web Components where there is a natural fit.
- Provider-specific implementation details stay inside the provider project and do not leak into `Mirage.Core`, `Mirage.Maui`, or `Mirage.Web`.

Phase-1 control kinds:
- `Root`
- `ContentContainer`
- `VerticalStack`
- `HorizontalStack`
- `Grid`
- `Map`
- `Label`
- `Entry`
- `Button`

Control descriptors provide:
- `ElementTagName`
- optional `ValueElementTagName`
- default attributes
- property mappings
- event mappings

### Built-in Map provider seam

`Map` is Mirage-owned control semantics and stays on the built-in control-node path. The browser map engine itself is provider-owned and plugs in through map-provider contracts, not `ResolveWebNode(...)`.

- Mirage core semantic source of truth is region/span (`center + latitude/longitude span`) plus MAUI map option flags, identity-keyed pins, and identity-keyed map elements.
- Provider implementations receive map projection updates through `IMirageMapProvider`/`IMirageMapAdapter` and are responsible for engine initialization, option projection, region/pin/element projection, interaction callbacks, and deterministic disposal.
- Ordinary map property changes must update an existing map instance; they must not recreate the underlying engine host.
- Providers must declare capability truth in `IMirageMapProvider.Capabilities`; unsupported features must produce explicit diagnostics.
- Tile source defaults/policy (for example, Leaflet + OSM) are provider defaults, not Mirage core semantics.
- Missing provider/assets must produce explicit diagnostics and visible failure state, not silent blank fallback.
- If a provider exposes local-asset mode, local mode must require explicit local asset URLs and fail fast with clear diagnostics when configuration is invalid.
- Runtime parity contract and divergence records are kept in `docs/parity/map-parity-contract.json` and `docs/parity/map-parity-divergences.json`; provider changes should update both when behavior contracts shift.

### Layout and visual realization expectations

Providers must preserve control-node layout/container identity and allow Mirage-authored styles to flow through without reinterpretation.

- Stack kinds (`VerticalStack`, `HorizontalStack`) are realized as control-node layout containers and must honor style mutations for `display`, `flexDirection`, `gap`, alignment, spacing, visibility, and sizing.
- `Grid` is realized as a control-node layout container and must honor style mutations for CSS grid templates, row/column gaps, placement, span, and cell alignment (`gridRow*`, `gridColumn*`, `justifySelf`, `alignSelf`).
- Shared visual projection from handlers includes background/opacity/size/visibility/margin/padding styles. Providers must not suppress these updates for control-node containers.
- Background support currently required by Mirage wave policy is solid + linear-gradient output; unsupported paint semantics are deferred and should remain deterministic (clear/skip instead of provider-specific reinterpretation).

## Web-node realization (`WebNode`)

`ResolveWebNode` is required for web-node rendering and must preserve element identity.

Example:
- request: `"vendor-widget"`
- descriptor must keep `ElementName == "vendor-widget"`

`ResolveWebNode(...)` is capability/validation-oriented, not semantic remapping.
Providers must not reinterpret the requested web-node element into a different identity.

Generic descriptors declare support/capability for:
- attributes
- properties (typed `MirageValue`)
- styles
- classes
- events

### Web-node event payload policy (current)

- Default generic events are name-only (`EventPayloadKind.None`).
- Use `EventPayloadKind.InputValue` only for explicit input-like mappings.
- Structured web-node payload extraction is currently deferred.
- Providers should advertise payload extraction support through provider capabilities (`ProviderFeatureSet.EventPayloadSupport`).

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
- Do not reinterpret web-node element identity into provider-specific control identity.
- `Mirage.Web` remains provider-neutral runtime infrastructure.
