# M6 Platform Service Inventory (Runtime-First)

This matrix tracks service/abstraction feasibility for a browser handler bridge.

Status vocabulary:
- `verified`: confirmed in current repo behavior or directly validated from MAUI source contracts.
- `assumed`: contract exists but browser-path behavior is not yet proven in this repo.
- `blocked`: known browser-host mismatch or unresolved platform constraint.

Related docs:
- [M6 handler feasibility dossier](../spikes/M6-handler-platform-feasibility.md)
- [M6 de-risk experiments](../spikes/M6-handler-de-risk-experiments.md)
- [Architecture overview](../ARCHITECTURE.md)

## Service Matrix
| Service/abstraction name | Where it is needed | Browser implementation strategy | DI override strategy | Risk level | Current status | Next proof task |
| --- | --- | --- | --- | --- | --- | --- |
| `IRendererRegistry` (repo) | Existing control rendering and fallback path | Keep as compatibility renderer path during handler rollout | No change in baseline registration; augment with handler adapters | low | verified | E1 |
| `IRendererFactory` (repo) | Existing renderer contracts and plugin overrides | Keep for fallback and plugin continuity | Keep existing registration model; add bridge registration conventions | low | verified | E8 |
| `IMauiViewRenderer` (repo) | Root entrypoint from sample/pages | Keep entrypoint stable; internally route to handler adapter when available | Preserve `AddMauiBrowserRuntime` service registration | low | verified | E1 |
| `IMauiContext` (MAUI) | Handler activation (`SetMauiContext`) | Create browser-safe context with scoped `Services` + `Handlers` | Register browser context factory/scoped provider | high | assumed | E1 |
| `IMauiHandlersFactory` (MAUI) | Resolve handler for virtual view type | Provide browser handler registrations and handler-type resolution | `ConfigureMauiHandlers`-style collection, browser-specific defaults | medium | assumed | E1 |
| `IMauiHandlersCollection` (MAUI) | Handler registration composition | Build additive registrations per control migration slice | Add singleton collection + registration delegates | medium | assumed | E1 |
| `IElementHandler` (MAUI) | Handler lifecycle (`SetVirtualView`, `DisconnectHandler`) | Introduce adapter lifecycle aligned with Blazor component lifetime | Resolve handler instances from factory; scoped lifetime policy | high | assumed | E3 |
| `IViewHandler` (MAUI) | View-specific layout/measure semantics | Start with limited browser interpretations and explicit unsupported paths | Register only handlers that can be mapped to DOM in v1 | high | assumed | E2 |
| `IPropertyMapper` (MAUI) | Property update flow (`UpdateProperty`, `UpdateProperties`) | Reuse mapper semantics; batch/guard updates to avoid DOM thrash | No direct override; invoked through handler adapter | high | assumed | E2 |
| `CommandMapper` (MAUI) | Command invocation path (`Invoke`) | Route command actions via adapter event bridge | No direct override; adapter mediation | medium | assumed | E2 |
| `IDispatcherProvider` + `IDispatcher` (MAUI) | UI-thread/dispatch assumptions, timers, async events | Define browser dispatch policy (main-thread/event-loop consistent behavior) | Register browser dispatcher provider and scoped dispatcher | high | assumed | E4 |
| `IApplication` + application handler contracts (MAUI) | App lifecycle and window operations | Support browser subset (single-app host, constrained window semantics) | Browser application services with unsupported APIs explicitly surfaced | high | assumed | E6 |
| `IWindow` + `IWindowHandler` (MAUI) | Window lifecycle, title/frame/back button model | Map to route/document-level semantics with explicit limits | Register browser window abstractions; unsupported members documented | high | blocked | E6 |
| `ITextInput` + `IEntryHandler` (MAUI) | Entry/editor behavior, IME, selection, keyboard | Build composition-safe DOM input bridge with commit semantics | Browser text service adapters behind handler bridge | high | assumed | E5 |
| `IView.Semantics` mapper path (MAUI `ViewHandler`) | Accessibility mapping under handler path | Preserve/extend aria projection rules from runtime context | Add semantics projection policy in handler adapter layer | high | assumed | E7 |

## Evidence Links
Repo evidence:
- [`IRendererRegistry`](../../src/MauiBrowser.Runtime/Rendering/IRendererRegistry.cs)
- [`IRendererFactory`](../../src/MauiBrowser.Runtime/Rendering/IRendererFactory.cs)
- [`IMauiViewRenderer`](../../src/MauiBrowser.Runtime/Rendering/IMauiViewRenderer.cs)
- [`MauiViewRenderer`](../../src/MauiBrowser.Runtime/Rendering/MauiViewRenderer.cs)
- [`AddMauiBrowserRuntime`](../../src/MauiBrowser.Runtime/DependencyInjection/MauiBrowserRuntimeServiceCollectionExtensions.cs)

MAUI source evidence (commit pinned):
- `IMauiContext`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/IMauiContext.cs>
- `MauiContext`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/MauiContext.cs>
- `IMauiHandlersFactory`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Hosting/IMauiHandlersFactory.cs>
- `IMauiHandlersCollection`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Hosting/IMauiHandlersCollection.cs>
- `ConfigureMauiHandlers`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Hosting/HandlerMauiAppBuilderExtensions.cs>
- `IElementHandler`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Handlers/IElementHandler.cs>
- `IViewHandler`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Handlers/IViewHandler.cs>
- `ElementHandler`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Handlers/Element/ElementHandler.cs>
- `ViewHandler`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Handlers/View/ViewHandler.cs>
- `PropertyMapper`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/PropertyMapper.cs>
- `CommandMapper`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/CommandMapper.cs>
- `IDispatcher`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Dispatching/IDispatcher.cs>
- `IDispatcherProvider`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Dispatching/IDispatcherProvider.cs>
- `ConfigureDispatching`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Hosting/Dispatching/AppHostBuilderExtensions.cs>
- `IApplication`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Core/IApplication.cs>
- `IWindow`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Core/IWindow.cs>
- `IWindowHandler`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Handlers/Window/IWindowHandler.cs>
- `ITextInput`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Core/ITextInput.cs>
- `IEntryHandler`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Handlers/Entry/IEntryHandler.cs>
- `EntryHandler`: <https://github.com/dotnet/maui/blob/1929f07799a9002a3f444d4d3f5b8a28167f91c5/src/Core/src/Handlers/Entry/EntryHandler.cs>
