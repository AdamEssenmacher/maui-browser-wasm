# MAUI Browser (DOM via Blazor WASM) — Scaffold

Third-party Browser/WASM DOM backend for .NET MAUI, hosted inside a Blazor WebAssembly app.

This repo is intentionally incremental: every milestone must produce a runnable sample under `samples/`.

## Quickstart

Prereqs:
- .NET SDK 10.0.102 (pinned via `global.json`)

Build:
```bash
dotnet build
```

Run sample:
```bash
dotnet run --project samples/HelloWorld/HelloWorld.csproj
```

> Current sample includes M1-M5 slice 1: open `/maui` to view MAUI `Label`, `StackLayout`, `Grid`, `CollectionView` prototype, `Button`, `Entry`, and route-synced `NavigationPage` rendering (MudBlazor plugin overrides `Button` and `Entry`).

## Current Maturity

The repo currently demonstrates MAUI control rendering in Blazor WASM with a pluggable renderer pipeline.  
Next milestone focus is M6 handler-path feasibility and platform discovery:
- `docs/spikes/M6-handler-platform-feasibility.md`
- `docs/checklists/M6-platform-service-inventory.md`
- `docs/spikes/M6-handler-de-risk-experiments.md`

## Repo layout

- `src/` Production packages (runtime + SDK + interop)
- `samples/` Runnable end-to-end samples
- `docs/` Architecture, roadmap, decisions (ADRs)
- `.agents/` Agent skills/playbooks (Codex-friendly)
- `tools/` Optional tooling (devserver wrappers, etc.)

## Roadmap

See `docs/ROADMAP.md`.

## License

MIT (placeholder — update as needed).
