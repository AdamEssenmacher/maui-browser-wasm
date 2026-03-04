# MAUI -> WASM (DOM) Vertical Slice

This repository hosts a DOM-backed Browser platform experiment for .NET MAUI:

MAUI virtual views -> MAUI handlers -> DOM elements + CSS + JS interop.

Current slice status:
- Root-view-first browser host bootstrap is implemented (`BrowserAppHost`).
- DOM platform view wrapper is `DomElement`; all JS interop is isolated in `DomApi`.
- First handlers are implemented: `Label`, `Button`, `Entry`, `Layout`, `ContentView`.
- Sample app is pure C# (no Razor, no XAML) and demonstrates live updates, click handling, resize, visibility, and opacity.

## Repo layout

- `src/Maui.Browser/`
  Browser backend: DOM interop, handlers, hosting, and measurement services.
- `src/Maui.Browser.Sample/`
  `net10.0-browser` sample host app that mounts to `#app`.
- `docs/DECISIONS.md`
  Short architecture and implementation decision log.

## Prerequisites

1. .NET SDK `10.0.102` (or compatible .NET 10 SDK).
2. MAUI assemblies are resolved via explicit NuGet package references (`Microsoft.Maui.Core`, `Microsoft.Maui.Controls`) pinned in `Directory.Build.props`.
3. Required WebAssembly tooling:
   - `dotnet workload install maui wasm-tools`

## Build and run

1. Build solution:
   - `dotnet build MauiDomWasm.sln`
2. Run sample:
   - `dotnet run --project src/Maui.Browser.Sample/Maui.Browser.Sample.csproj`
3. Open the launched browser URL (default from launch profile: `http://localhost:5057`).

Important:
- Do not execute `src/Maui.Browser.Sample/bin/.../Maui.Browser.Sample` directly. That runs as a native host process and `JSImport` will throw `PlatformNotSupportedException`.
- If `WasmAppHost` reports `no perHostConfigs found`, run:
  - `dotnet clean src/Maui.Browser.Sample/Maui.Browser.Sample.csproj`
  - `dotnet build src/Maui.Browser.Sample/Maui.Browser.Sample.csproj -t:Rebuild`

## Smoke checklist

- `Label` text updates as you type in `Entry`.
- `Button` click increments a counter label.
- Resizing the browser updates the viewport/layout status label.
- Probe label responds to visibility toggle and opacity cycle buttons.

## Notes

- Layout uses absolute frame projection for correctness-first behavior.
- Text measurement uses a minimal canvas `measureText` service.
- Font-load synchronization is intentionally deferred for this first slice.

See `AGENTS.md` for project guardrails.
