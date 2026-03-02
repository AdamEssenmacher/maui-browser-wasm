# M6 Feasibility Dossier: Handler-Path Bridge for Browser Runtime

## Summary
This document defines a runtime-first, hybrid bridge path from the current renderer-centric implementation to a handler-capable browser platform architecture.

Scope in this slice is discovery and decision-making only. No runtime behavior changes are introduced.

Related docs:
- [M6 platform service inventory](../checklists/M6-platform-service-inventory.md)
- [M6 de-risk experiments](M6-handler-de-risk-experiments.md)
- [Architecture overview](../ARCHITECTURE.md)
- [Roadmap](../ROADMAP.md)

## Baseline: What Exists Today
Current runtime is renderer-centric, not handler-driven:
- View resolution is via [`IRendererRegistry`](../../src/MauiBrowser.Runtime/Rendering/IRendererRegistry.cs) and [`IRendererFactory`](../../src/MauiBrowser.Runtime/Rendering/IRendererFactory.cs).
- Rendering entrypoint is [`IMauiViewRenderer` / `MauiViewRenderer`](../../src/MauiBrowser.Runtime/Rendering/MauiViewRenderer.cs).
- Default registrations are in [`AddMauiBrowserRuntime(IServiceCollection)`](../../src/MauiBrowser.Runtime/DependencyInjection/MauiBrowserRuntimeServiceCollectionExtensions.cs).

## Source Basis (Pinned)
MAUI source references in this dossier are pinned to `dotnet/maui` commit `1929f07799a9002a3f444d4d3f5b8a28167f91c5` (release tag `10.0.40`):
- `IElementHandler`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Handlers/IElementHandler.cs>
- `IViewHandler`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Handlers/IViewHandler.cs>
- `ElementHandler`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Handlers/Element/ElementHandler.cs>
- `ViewHandler`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Handlers/View/ViewHandler.cs>
- `IMauiContext` and `MauiContext`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/IMauiContext.cs>, <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/MauiContext.cs>
- Handler registration (`ConfigureMauiHandlers`): <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Hosting/HandlerMauiAppBuilderExtensions.cs>
- Mapper infrastructure (`PropertyMapper`, `CommandMapper`): <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/PropertyMapper.cs>, <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/CommandMapper.cs>
- Dispatching (`IDispatcher`, `IDispatcherProvider`, dispatch setup): <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Dispatching/IDispatcher.cs>, <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Dispatching/IDispatcherProvider.cs>, <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Hosting/Dispatching/AppHostBuilderExtensions.cs>
- App/window contracts: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Core/IApplication.cs>, <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Core/IWindow.cs>, <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Handlers/Window/IWindowHandler.cs>
- Text input contracts: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Core/ITextInput.cs>, <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Handlers/Entry/IEntryHandler.cs>, <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Handlers/Entry/EntryHandler.cs>

## Target Architecture (Hybrid Bridge)
Target render path:

`VirtualView -> Handler Adapter -> DOM Renderer`

Rules:
1. Keep existing renderer registry as fallback and compatibility layer.
2. Add handler-backed rendering incrementally (control-by-control).
3. No big-bang replacement of current `IRendererFactory` pipeline.

Planned shape:
1. Browser `IMauiContext` adapter creates handler execution scope.
2. Handler adapter resolves/creates `IElementHandler` and applies mapper updates.
3. DOM renderer consumes handler/virtual view state and emits `RenderFragment`.
4. Existing renderer factories remain valid until each control is migrated.

## Handler Integration Seams to Define
1. Browser MAUI context adapter:
   - Implement browser-safe `IMauiContext` backing `Services` and `IMauiHandlersFactory`.
   - Scope lifecycle for app/window-like contexts, even in single-document runtime.
2. Handler activation strategy:
   - Resolve handler type through `IMauiHandlersFactory` and attach with `SetMauiContext` + `SetVirtualView`.
   - Cache/retain handler identity to avoid churn in frequent re-renders.
3. Mapper/property update flow:
   - Use MAUI property/command mapper model (`UpdateValue`, `Invoke`) for handler-compatible controls.
   - Bridge mapper-driven updates to DOM output without reintroducing event loops.
4. Lifecycle and disposal:
   - Align handler connect/disconnect semantics (`DisconnectHandler`) with Blazor component disposal/reconciliation.
   - Guarantee deterministic cleanup for timers/events/interop handles.

## DI Shadowing Model
### Shadow now (low-risk service overrides)
These can be replaced with browser implementations without changing MAUI contracts:
- `IMauiHandlersFactory` registration strategy for browser handler types.
- `IMauiHandlersCollection` composition and default registrations.
- `IDispatcherProvider` + app/window dispatch initialization model.

### Adapter required (medium-risk abstractions)
These require an adapter layer between MAUI assumptions and browser runtime:
- `IMauiContext` scope semantics (services + handler factory lifetime).
- `IElementHandler` activation lifecycle under Blazor render loop.
- Mapper-driven property updates to DOM fragments.

### Platform-hard (high-risk browser constraints)
These are likely hard blockers or partial-parity only:
- Full `IWindow` lifecycle parity in a single-tab web host.
- Native-like text/IME fidelity across browsers and virtual keyboards.
- High-volume virtualization with stable handler identity and low GC churn.

## Top 10 Gotchas Register
| Rank | Gotcha | Severity | Confidence | Impact surface | Why hard/easy/impossible | Mitigation experiment | Exit signal |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Handler lifecycle vs Blazor diff lifecycle mismatch | Critical | High | All handler-backed controls | `ElementHandler` assumes connect/disconnect ownership that is not identical to diff-based reconciliation. | E1 + E3 | Deterministic connect/disconnect events per visual attach/detach with no leaked handlers. |
| 2 | Missing browser `IMauiContext` window-scope equivalent | Critical | High | Handler activation, scoped services | `MauiContext` supports scope and service overrides; browser runtime must emulate scope boundaries. | E1 + E4 | Handler activation succeeds with scoped services and no null context paths. |
| 3 | Mapper update cadence can over-render | High | High | Property-heavy controls, performance | `PropertyMapper.UpdateProperties/UpdateProperty` can fire many operations; naive DOM bridge can thrash. | E2 | Measured bounded rerender count per property burst. |
| 4 | Dispatcher/thread affinity assumptions | High | Medium | Commands, timers, async UI updates | MAUI dispatch model assumes UI-thread semantics that are different in WASM/browser execution models. | E4 | Documented dispatch contract with passing tests for ordering and reentrancy. |
| 5 | IME composition/final commit semantics | High | High | Entry/Editor/input UX | Intermediate composition states must not be treated as committed values across all target browsers. | E5 | Composition tests pass for start/update/end and committed text invariants. |
| 6 | App/window lifecycle parity gaps | High | Medium | Navigation, startup/teardown, window APIs | `IApplication`/`IWindow` include lifecycle and multi-window concepts that are partial in browser host. | E6 | Explicit supported subset documented and validated by sample probes. |
| 7 | Accessibility semantics parity under handler path | High | Medium | Screen readers, keyboard, aria | Handler path may diverge from existing semantics projection unless parity rules are enforced. | E7 | A11y checklist pass for migrated handler-backed controls. |
| 8 | Plugin override compatibility across two pipelines | Medium | Medium | Extensibility model | Existing plugins override renderers; mixed handler/renderer world needs deterministic precedence rules. | E8 | Plugin precedence contract documented and validated by tests. |
| 9 | Virtualization + handler identity stability | Medium | Medium | CollectionView-scale controls | Recycled item views can break handler identity/state if keying and disposal are inconsistent. | E8 | Stable item identity with no duplicate/disposed-handler errors under churn test. |
| 10 | Browser-host gaps for platform APIs (dialogs, sensors, windowing) | Medium | Medium | Future platform services | Some APIs need web alternatives; some remain unsupported in browser sandbox. | E6 | Service inventory marks each API path as verified/assumed/blocked with fallbacks. |

## Major-Area Feasibility Conclusions
| Area | Conclusion | Notes |
| --- | --- | --- |
| Handlers bootstrapping | Possible | Contract surface (`IElementHandler`, `IMauiContext`, handler factory) is adapter-friendly for browser runtime. |
| Input/IME | Hard | Feasible for baseline, but high parity risk across browsers and input methods. |
| Navigation/windowing lifecycle | Hard | Browser can support a subset; full window lifecycle parity is unlikely in v1. |
| Threading/dispatch model | Hard | Requires explicit runtime contract and guardrails around dispatcher assumptions. |
| Accessibility parity | Hard | Achievable baseline is likely; full parity depends on control-by-control mapping quality. |
| Virtualization/perf | Likely impossible for now (full parity) | Practical v1 should target bounded/prototype virtualization, not native-equivalent performance. |

## Decision Defaults for Next Slice
1. Keep current renderer pipeline as baseline compatibility path.
2. Start handler bridge with a single simple control in a probe-first PR.
3. Treat dispatching + IME as first-class risk streams, not follow-up cleanup.
