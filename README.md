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

> Current sample includes M1 slice 1: open `/maui` to view a MAUI `Label` rendered through the runtime pipeline.

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
