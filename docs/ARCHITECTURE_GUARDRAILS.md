# Mirage Architecture Guardrails (Early-lock)

This document locks architectural boundaries before Mirage expands beyond the current built-in Phase-1 surface.

## 1. Built-in control-node purity (hard invariant)

- Mirage-owned built-in handlers must emit `ControlNode`.
- `WebNode` is for extensibility only:
  - third-party controls
  - custom controls
  - arbitrary DOM/Web Components
  - BYO-component scenarios
- Built-ins must not silently fall back to `WebNode` without explicit architectural approval.

## 2. Shared visual/layout contract scope

`VisualNodeBase` is limited to cross-cutting concerns valid for both control and web nodes:

- stable `NodeId`
- `Children`
- `AutomationId`
- `Visibility`
- `Opacity`
- `Margin`
- common sizing/layout participation metadata (`LayoutMetadata`)
- common mutation channels (`Properties`, `Styles`, `Events`)

Classification:

- Shared visual concern:
  - `AutomationId`, visibility, opacity, margin, layout metadata
- Web-node-only concern:
  - arbitrary `Attributes`
  - `Classes`
  - `Slots`
  - web-shaped element metadata
- Deferred concern:
  - full MAUI style/resource/trigger semantics
  - universal padding semantics across all nodes
  - control-specific semantic behavior in shared base

## 3. Generic `WebNode` contract

`WebNode` is an intentional web realization node:

- create this actual DOM/custom element by name
- apply explicit attributes/properties/classes/styles/events
- preserve element identity through provider/runtime

Provider rule:

- `ResolveWebNode(...)` is validation/capability-oriented.
- Providers must not reinterpret element identity (for example, remapping `vendor-widget` to another element name).

## 4. Provider capability contract expectations

Capabilities are treated as structured contract, not free-form metadata only.

Current required capability dimensions:

- `SupportsControlNodes`
- `SupportsWebNodes`

Future capability dimensions should remain explicit (not ad-hoc string parsing):

- advanced input/property semantics
- overlay/dialog hosting
- virtualization
- theming/token support
- structured event payload extraction

## 5. Generic event payload policy (current)

Generic event routing stays node-id based and currently supports:

- Name-only event (`EventPayloadKind.None`)
- Input-value string payload (`EventPayloadKind.InputValue`)

Policy:

- Default to name-only.
- Use input-value payload only when explicitly mapped and needed.
- Structured web-node event payload extraction is deferred until a typed payload contract is introduced.

## 6. `MirageValue.Json` usage constraints

- Allowed shapes: `JsonObject` or `JsonArray` only.
- Primitive JSON nodes are rejected.
- Deep clone behavior is guaranteed on assignment and retrieval.
- Equality is structural (`JsonNode.DeepEquals`).
- Intended for structured payload/config, not high-frequency mutation loops.
- Keep payloads reasonably small; very large JSON payloads should be avoided in hot paths.

## 7. Mirage.Web neutrality boundary

`Mirage.Web` may include:

- DOM interop
- patch application
- node registry
- event bridge
- browser services

`Mirage.Web` must not include:

- provider-specific mappings (Fluent/FAST/UI5/etc.)
- UI5-specific assumptions
- vendor-specific control realization logic

## 8. Showcase role

- `Showcase.Fluent` is a proof and dogfood sample, not a framework-design driver.
- `Counter.Fluent` remains minimal stability harness.
- Architectural decisions must be justified in core runtime/provider/handler layers, not by demo convenience.
