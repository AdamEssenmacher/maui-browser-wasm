# MAUI -> WASM (DOM) Prototype

This repository is an experiment for running .NET MAUI on `net10.0-browser` with the DOM as the platform UI:

MAUI virtual views -> MAUI handlers -> DOM elements + CSS + JS interop.

Current state:
- App startup supports real MAUI `Application -> Window` bootstrap (`BrowserAppHost.RunAsync(MauiApp, ...)`).
- Root-view bootstrap (`BrowserAppHost.RunAsync(MauiApp, IView, ...)`) remains as a compatibility path.
- DOM platform view wrapper is `DomElement`; JSImport entrypoints are centralized in `DomApi`.
- Implemented handlers: `Window`, `ContentPage`, `Label`, `Button`, `Entry`, `Layout`, `ContentView`.
- Sample app demonstrates a real `.xaml` `ContentPage` (with code-behind) and covers binding, input, click, resize, visibility, opacity, and stress add/remove cleanup.

## Repo layout

- `src/Maui.Browser/`
  Browser backend: DOM interop, handlers, hosting, measurement.
- `src/Maui.Browser.Sample/`
  `net10.0-browser` host app mounted under `#app`.
- `src/Maui.Browser.Sample.DevHost/`
  ASP.NET Core dev-only host used for `dotnet watch` browser refresh + hot reload plumbing.
- `tests/Maui.Browser.E2E.Tests/`
  Playwright smoke and stress validation.
- `scripts/check-hotreload-prereqs.sh`
  Build-time check for hot reload prerequisites in Debug output.
- `docs/DECISIONS.md`
  Architecture/implementation decision log.

## Prerequisites

1. .NET SDK `10.0.102` (or compatible .NET 10 SDK).
2. Required workloads:
   - `dotnet workload install maui wasm-tools`
3. Playwright browser runtime for E2E tests:
   - `pwsh tests/Maui.Browser.E2E.Tests/bin/Release/net10.0/playwright.ps1 install --with-deps chromium`

## Build and run sample

1. Build:
   - `dotnet build MauiDomWasm.sln`
2. Run sample:
   - `dotnet run --project src/Maui.Browser.Sample/Maui.Browser.Sample.csproj`
3. Open the launched app URL (default launch profile uses `http://localhost:5057`).

Important:
- Do not execute `src/Maui.Browser.Sample/bin/.../Maui.Browser.Sample` directly. That runs as a native host process and `JSImport` throws `PlatformNotSupportedException`.
- If `WasmAppHost` reports `no perHostConfigs found`, run:
  - `dotnet clean src/Maui.Browser.Sample/Maui.Browser.Sample.csproj`
  - `dotnet build src/Maui.Browser.Sample/Maui.Browser.Sample.csproj -t:Rebuild`

## Hot reload dev loop

1. Start the dev host with watch:
   - `dotnet watch --project src/Maui.Browser.Sample.DevHost/Maui.Browser.Sample.DevHost.csproj run`
2. Open `http://localhost:5173`.
3. Confirm the page shows a `Hot reload mode: ...` status label.
4. Edit `src/Maui.Browser.Sample/Demo/DemoState.cs` method bodies and `src/Maui.Browser.Sample/Demo/XamlDemoPage.xaml` text/properties.
5. Observe:
   - `metadata` mode: method-body edits apply without full page reload when supported.
   - `refresh-only` mode: C# edits still fall back to browser refresh behavior.
   - `XamlDemoPage.xaml` text/property edits are pushed over SSE and applied in place (no `Bootstrapping app...` flash).
   - status label includes `last update: ...` (`metadata-delta`, `xaml-live-apply`, `xaml-apply-failed`).
   - when live XAML apply fails, the prior UI remains active and the page shows a non-fatal error with a `Force full reload` button.

Diagnostics:
- Browser console logs include:
  - page boot count (`[hot-reload] page boot #...`)
  - runtime env flags (`DOTNET_MODIFIABLE_ASSEMBLIES`, `__ASPNETCORE_BROWSER_TOOLS`)
  - delta apply callback count when available.
- Dev host exposes:
  - `/_dev/events` (SSE stream for XAML change events)
  - `/_dev/xaml?path=Demo/XamlDemoPage.xaml` (current XAML source payload)
  - `/_dev/emit-xaml-change` (test-only synthetic change trigger used by E2E tests)

Known live-XAML limits in this slice:
- scoped to `Demo/XamlDemoPage.xaml` only.
- structural edits that require new generated members or new code-behind methods are out of scope for in-place apply and still require rebuild/restart.
- malformed XAML does not crash the app, but surfaces `xaml-apply-failed` and keeps the previous visual tree.

## Automated smoke checks

1. Build release artifacts:
   - `dotnet build MauiDomWasm.sln --configuration Release`
2. Build Debug sample artifacts for hot reload proof:
   - `dotnet build src/Maui.Browser.Sample/Maui.Browser.Sample.csproj --configuration Debug`
3. Verify hot reload prerequisites (Debug):
   - `./scripts/check-hotreload-prereqs.sh`
4. Install Playwright browser:
   - `pwsh tests/Maui.Browser.E2E.Tests/bin/Release/net10.0/playwright.ps1 install --with-deps chromium`
5. Run smoke tests:
   - `dotnet test tests/Maui.Browser.E2E.Tests/Maui.Browser.E2E.Tests.csproj --configuration Release --no-build`

Expected pass signals:
- All E2E tests pass.
- Entry -> label binding works.
- Button click updates count.
- Resize updates viewport text.
- Visibility/opacity transitions apply correctly.
- Stress mutation test keeps DOM handle/subscription counts near baseline.

## Manual runtime checklist

- Page renders under `#app`.
- `Hot reload mode: ...` label appears and reports `metadata` or `refresh-only`.
- Typing into `Entry` updates the live text label.
- Clicking increment button updates click summary.
- Resizing browser updates viewport/layout status text.
- Toggling visibility moves probe through `visible <-> collapsed`.
- Cycling opacity updates probe opacity summary.
- Running leak stress increments stress run count and remains interactive.

## Prototype proof checklist

- Lifecycle proof: app boots from `UseMauiApp<App>()` via `Application -> Window` host path.
- Handler proof: controls/layout/page/window mapping stays in `Maui.Browser` handlers.
- Automated proof: Playwright smoke runs in CI alongside solution build.
- Leak-resistance proof: stress scenario validates handle/subscription cleanup near baseline.

## Notes

- Layout uses absolute frame projection for correctness-first behavior.
- Text measurement is currently canvas `measureText` based.
- Font-load synchronization remains deferred.

See `AGENTS.md` for repository guardrails.
