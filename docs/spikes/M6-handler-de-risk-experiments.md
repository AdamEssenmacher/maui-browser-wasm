# M6 De-Risk Experiments for Handler Bridge

This document defines probe-first experiments for the handler-path transition. Each experiment is intentionally small and designed to answer one high-risk unknown.

Related docs:
- [M6 handler feasibility dossier](M6-handler-platform-feasibility.md)
- [M6 platform service inventory](../checklists/M6-platform-service-inventory.md)
- [Architecture overview](../ARCHITECTURE.md)

## Experiment Catalog

## E1: Minimal Handler Activation (Label)
- Hypothesis:
  - A browser runtime can activate a MAUI handler for one simple control using `IMauiContext` + `IMauiHandlersFactory` without breaking the existing render entrypoint.
- Minimal implementation/probe shape:
  - Add a throwaway probe path that resolves a handler for a `Label`-like view and runs `SetMauiContext` + `SetVirtualView` once.
  - Keep existing renderer path as fallback.
- Observable pass/fail criteria:
  - Pass: handler instance is created and connected with non-null context and virtual view.
  - Fail: handler resolution/activation throws or requires unavailable platform services.
- Timebox:
  - 1 day.
- Follow-up action if failed:
  - Add explicit adapter layer before handler activation and narrow to mock/fake platform view contract.

## E2: Property Mapper Update Loop in Browser Render Cycle
- Hypothesis:
  - Mapper-driven updates (`UpdateProperties`/`UpdateValue`) can be translated into stable DOM updates without over-rendering.
- Minimal implementation/probe shape:
  - Attach one handler-backed control to property change bursts and record render count + update order.
- Observable pass/fail criteria:
  - Pass: property updates are deterministic and rerender count is bounded.
  - Fail: update oscillation, duplicated updates, or unbounded rerenders.
- Timebox:
  - 1 day.
- Follow-up action if failed:
  - Add batch window/debounce policy in handler adapter and reevaluate.

## E3: Connect/Disconnect and Disposal Semantics
- Hypothesis:
  - Handler connect/disconnect can map cleanly to Blazor component lifetime without leaks.
- Minimal implementation/probe shape:
  - Instrument handler lifecycle for mount/unmount/rebind cycles in sample route transitions.
- Observable pass/fail criteria:
  - Pass: each detached instance receives deterministic disconnect and no stale references remain.
  - Fail: leaked handlers, double-connect without disconnect, or stale event hookups.
- Timebox:
  - 1 day.
- Follow-up action if failed:
  - Introduce explicit lifecycle coordinator that owns handler retention and disposal.

## E4: Dispatcher and Thread-Affinity Assumptions
- Hypothesis:
  - Browser-host dispatching can satisfy MAUI dispatcher assumptions for ordering and non-reentrant UI updates.
- Minimal implementation/probe shape:
  - Create a tiny test matrix for sync dispatch, delayed dispatch, and nested dispatch scenarios.
- Observable pass/fail criteria:
  - Pass: action ordering is stable and deadlock/reentrancy hazards are absent in tested flows.
  - Fail: out-of-order updates or reentrancy bugs.
- Timebox:
  - 1 day.
- Follow-up action if failed:
  - Restrict supported dispatch patterns and add guardrails in runtime contract.

## E5: Text Input + IME Event Compatibility
- Hypothesis:
  - Handler-path text updates can preserve composition semantics (start/update/end) without committing intermediate values.
- Minimal implementation/probe shape:
  - Extend current text-input planning probe with handler-path event wiring for one text control.
- Observable pass/fail criteria:
  - Pass: committed value changes only on non-composition input or composition end.
  - Fail: premature commits, lost commits, or value thrashing.
- Timebox:
  - 2 days.
- Follow-up action if failed:
  - Move text input to a dedicated adapter path with explicit composition state machine.

## E6: Navigation and Window Lifecycle Coordination
- Hypothesis:
  - Browser route/document lifecycle can support a practical subset of `IApplication`/`IWindow` expectations.
- Minimal implementation/probe shape:
  - Build a thin lifecycle probe mapping route activation/deactivation/back navigation to window/application callbacks subset.
- Observable pass/fail criteria:
  - Pass: defined subset behaves predictably and is documentable.
  - Fail: lifecycle cannot be made deterministic under browser events.
- Timebox:
  - 2 days.
- Follow-up action if failed:
  - Explicitly narrow supported lifecycle contract and mark non-supported APIs as blocked.

## E7: Accessibility Semantics Parity on Handler Path
- Hypothesis:
  - Handler-backed controls can preserve baseline semantics parity with current renderer path (`role`, labels, disabled state, keyboard).
- Minimal implementation/probe shape:
  - Run focused a11y checks on one handler-backed interactive control and one text input control.
- Observable pass/fail criteria:
  - Pass: baseline checklist items match existing renderer behavior.
  - Fail: regressions in names/roles/disabled/focus/keyboard behavior.
- Timebox:
  - 1 day.
- Follow-up action if failed:
  - Add required semantics policy layer before enabling handler-backed control by default.

## E8: Plugin Override Compatibility in Mixed Pipeline
- Hypothesis:
  - Existing plugin precedence model can coexist with handler-backed controls without ambiguous resolution.
- Minimal implementation/probe shape:
  - Define and test precedence for renderer override vs handler-backed path for one control family.
- Observable pass/fail criteria:
  - Pass: one deterministic precedence rule works across core + plugin registrations.
  - Fail: ambiguous/unstable resolution order.
- Timebox:
  - 1 day.
- Follow-up action if failed:
  - Introduce explicit bridge precedence policy and migration flags per control type.

## Execution Order Recommendation
1. E1
2. E3
3. E2
4. E4
5. E5
6. E7
7. E6
8. E8

This order validates activation/lifecycle first, then update semantics, then high-risk behavior (dispatch/IME/a11y), and finally compatibility/policy.
