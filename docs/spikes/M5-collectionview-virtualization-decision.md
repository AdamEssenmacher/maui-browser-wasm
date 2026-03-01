# M5 Spike: CollectionView Prototype and Virtualization Decision

## Goal
Close M5 item "CollectionView prototype + virtualization decision" with a runnable proof and an explicit v0 decision.

## Prototype Implemented
- Runtime includes `MauiCollectionViewRendererFactory`.
- `CollectionView` renders through the existing pluggable renderer registry.
- `ItemsSource` is enumerated and materialized into DOM list items.
- `ItemTemplate` is supported when it creates a MAUI `View`; each templated view is resolved through `IRendererRegistry`.
- Fallback when no `ItemTemplate` is provided: render `item.ToString()`.
- Safety cap: first 200 items are rendered, then a truncation message is emitted.

## Virtualization Decision (v0)
Decision: **do not implement DOM virtualization in M5 v0**.

Rationale:
- The objective of this slice is to validate end-to-end renderer composition for repeatable item controls, not ship production-scale scrolling behavior.
- Existing milestone constraints prioritize incremental, runnable slices with clear behavior over broad incomplete infrastructure.
- A non-virtualized prototype with a hard cap is deterministic, testable, and keeps failure modes explicit.

Trade-offs accepted:
- Large lists are truncated at 200 items in v0.
- Scrolling performance for long datasets is not representative of final design.

## Follow-up Criteria for Virtualization Work
Virtualization moves from deferred to required when at least one of these applies:
- We need to render >200 items in sample/runtime scenarios.
- We add controls or demos where smooth scrolling and memory usage are core acceptance criteria.
- We introduce selection/state features that require stable item identity across recycled views.

## Next Design Questions (Future Slice)
- View recycling strategy: Blazor keying + MAUI virtual view identity mapping.
- Windowing trigger: scroll container observation and overscan policy.
- Templated item lifecycle: create/bind/unbind/reuse contracts.
- Measurement strategy for variable-height items.
