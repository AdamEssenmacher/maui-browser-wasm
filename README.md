# Mirage

Mirage is an experimental MAUI handler-backed browser platform for `net10.0-browser`, using the DOM as the platform UI instead of a canvas renderer or a parallel component model.

> [!IMPORTANT]
> Mirage is still experimental. The current happy path is to build and run it from this repository. This README documents the active source-first workflow, not a stable package-install story.

## Table of Contents

- [Why Mirage](#why-mirage)
- [Try Mirage](#try-mirage)
- [Build a Tiny Mirage App from Source](#build-a-tiny-mirage-app-from-source)
- [What Exists Today](#what-exists-today)
- [How Mirage Is Structured](#how-mirage-is-structured)
- [Limits and Non-Goals](#limits-and-non-goals)
- [Where to Go Next](#where-to-go-next)

## Why Mirage

- The DOM is the platform UI. Platform views become DOM nodes, styling flows through CSS, input comes from browser events, and accessibility maps to web semantics.
- Mirage uses MAUI's handler architecture instead of a separate "MAUI-like" component system. The goal is a real browser backend for MAUI, not a Blazor component veneer.
- The current reference provider path uses `Mirage.Provider.Fluent` for built-in control realization, with a narrower raw custom-element hosting seam for direct DOM/custom-element hosting.
- `.razor` is optional, not required. Mirage hosts are plain `net10.0-browser` apps that run ordinary MAUI views, layouts, code-behind, and XAML.

## Try Mirage

### Prerequisites

- .NET SDK `10.0.102`
- Linux workloads: `dotnet workload install wasm-tools`
- macOS/Windows workloads: `dotnet workload install maui wasm-tools`

### Build the repo

```bash
dotnet restore Mirage.sln
dotnet build Mirage.sln
```

The browser reference samples load Fluent UI Web Components from `unpkg` by default, so the page itself still expects network access when it boots.

### Run the browser samples

```bash
dotnet run --project samples/Counter.Fluent/Counter.Fluent.csproj
dotnet run --project samples/Showcase.Fluent/Showcase.Fluent.csproj
dotnet run --project samples/ControlsSample.Fluent/ControlsSample.Fluent.csproj
```

- `Counter.Fluent` proves the minimum host wiring and the core `Label` / `Entry` / `Button` flow.
- `Showcase.Fluent` is the feature tour: navigation projection, layout labs, XAML lab, explicit scrolling, and an optional Leaflet-backed `Map` lane.
- `ControlsSample.Fluent` is the parity-breadth lane, using repo wrappers around upstream `dotnet/maui` Controls.Sample pages.

Each browser sample has a paired native MAUI host under `samples/*.Maui` for side-by-side comparison. Those native hosts are intentionally not part of `Mirage.sln`.

## Build a Tiny Mirage App from Source

If you want the smallest moving parts without reading an entire sample, this is the minimal shape Mirage uses today. It is source-first on purpose: the app references the local Mirage projects directly.

### 1. Create a browser host project

```xml
<Project Sdk="Microsoft.NET.Sdk.WebAssembly">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <MirageBrowserApp>true</MirageBrowserApp>
    <TargetFramework>net10.0-browser</TargetFramework>
    <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
    <DefaultWasmHostConfig>browser</DefaultWasmHostConfig>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <WasmEnableJSImport>true</WasmEnableJSImport>
    <WasmBuildAppBundle>true</WasmBuildAppBundle>
  </PropertyGroup>

  <ItemGroup>
    <HostConfig Include="browser">
      <host>browser</host>
      <html-path>index.html</html-path>
    </HostConfig>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Mirage.Maui\Mirage.Maui.csproj" />
    <ProjectReference Include="..\..\src\Mirage.Provider.Fluent\Mirage.Provider.Fluent.csproj" />
    <ProjectReference Include="..\..\src\Mirage.Web\Mirage.Web.csproj" />
    <ProjectReference Include="..\..\src\Mirage.Core\Mirage.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Maui.Core" Version="10.0.20" />
    <PackageReference Include="Microsoft.Maui.Controls" Version="10.0.20" />
  </ItemGroup>

  <ItemGroup>
    <None Include="wwwroot\**\*.*" CopyToOutputDirectory="PreserveNewest" />
    <WasmExtraFilesToDeploy Include="wwwroot\**\*.*">
      <TargetPath>%(RecursiveDir)%(Filename)%(Extension)</TargetPath>
    </WasmExtraFilesToDeploy>
  </ItemGroup>
</Project>
```

### 2. Wire Mirage into `Program.cs`

```csharp
using Mirage.Maui.Hosting;
using Mirage.Provider.Fluent;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Hosting;

public static class Program
{
    public static async Task Main()
    {
        if (!OperatingSystem.IsBrowser())
        {
            Console.Error.WriteLine("This host must run inside browser WebAssembly runtime.");
            return;
        }

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        builder.UseMirageBrowserPlatform();
        builder.UseMirageComponentProvider<FluentMirageComponentProvider>();

        var app = builder.Build();
        await MirageBrowserAppHost.RunAsync(app, rootElementId: "app");

        await Task.Delay(Timeout.InfiniteTimeSpan);
    }
}

internal sealed class App : Application
{
    protected override Window CreateWindow(IActivationState? activationState)
    {
        _ = activationState;
        return new Window(new MainPage());
    }
}

internal sealed class MainPage : ContentPage
{
    private int _count;
    private readonly Label _countLabel;

    public MainPage()
    {
        Title = "Mirage Counter";

        var entry = new Entry
        {
            Placeholder = "Type here"
        };

        _countLabel = new Label
        {
            Text = "Count: 0"
        };

        var button = new Button
        {
            Text = "Increment"
        };
        button.Clicked += (_, _) =>
        {
            _count++;
            _countLabel.Text = $"Count: {_count}";
        };

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 16,
            Children =
            {
                new Label { Text = "Hello from Mirage" },
                entry,
                button,
                _countLabel
            }
        };
    }
}
```

### 3. Create `wwwroot/main.mjs`

This is the small JavaScript bootstrap module that starts the browser-side .NET runtime. The SDK generates `./_framework/dotnet.js`; your host provides `main.mjs` and calls `runMain()`.

```js
import { dotnet } from "./_framework/dotnet.js";

async function start() {
  const appRoot = document.getElementById("app");

  try {
    const runtime = await dotnet
      .withDiagnosticTracing(false)
      .create();

    await runtime.runMain();
  } catch (error) {
    console.error("Failed to start .NET runtime:", error);

    if (appRoot) {
      appRoot.textContent = "Startup failed. Check browser console for details.";
    }
  }
}

await start();
```

### 4. Add an HTML shell

```html
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Mirage App</title>
  <style>
    html, body, #app {
      margin: 0;
      width: 100%;
      height: 100%;
    }
  </style>
</head>
<body>
  <div id="app"></div>
  <script type="module" src="https://unpkg.com/@fluentui/web-components@2.6.1"></script>
  <script type="module" src="main.mjs"></script>
</body>
</html>
```

### 5. Run the host

```bash
dotnet run --project path/to/YourApp.csproj
```

This walkthrough uses C# UI for brevity, but Mirage also runs MAUI XAML without `.razor`. Start with `samples/Counter.Fluent` if you want the smallest real host to copy, or `samples/Showcase.Fluent` if you want navigation, maps, and XAML hot-reload seams.

Optional follow-ups:

- `UseMirageNavigation(...)` projects `NavigationPage` state into browser URL and history while keeping MAUI stack semantics as the source of truth.
- `UseMirageLeafletMaps(...)` registers the current reference map provider for built-in `Map` semantics.

## What Exists Today

- Mirage already runs a meaningful subset of MAUI pages, layouts, text and input controls, selection and media controls, shapes, collection-style controls, and maps in the browser.
- The browser-facing samples cover minimal apps (`Counter`), product-style exploration (`Showcase`), and broader parity smoke via wrapped `Controls.Sample`.
- The browser stack includes source-based XAML support, bundled image publishing, `NavigationPage` to URL projection, optional XAML hot reload, and built-in diagnostics and performance instrumentation.
- Exact control coverage and intentional divergences live in the [Parity Matrix](docs/PARITY_MATRIX.md), [Handler Parity Matrix](docs/HANDLER_PARITY_MATRIX.md), and [Parity Gaps](docs/PARITY_GAPS.md).

## How Mirage Is Structured

`MAUI virtual view -> Mirage MAUI handlers -> Mirage semantic tree + provider descriptors -> browser runtime -> DOM/custom elements`

- `Mirage.Core` owns browser-side semantics, typed values, diffing, and provider contracts.
- `Mirage.Web` owns browser runtime services, DOM interop, patch application, and event plumbing.
- `Mirage.Maui` owns the MAUI-facing browser handlers and host APIs such as `UseMirageBrowserPlatform()` and `MirageBrowserAppHost.RunAsync(...)`.

Reference providers currently live in `Mirage.Provider.Fluent` and `Mirage.Provider.LeafletMaps`.

## Limits and Non-Goals

- Mirage is experimental and under active change.
- MAUI parity is meaningful but still partial. Use the parity docs as the source of truth for current support and intentional divergences.
- The project is source-first today. This README does not describe a stable NuGet package workflow.
- `Shell` is not the current navigation story. Mirage currently centers `Window` and `NavigationPage`, with optional URL projection.
- Mirage is not trying to rebuild MAUI as Blazor components, and it is not a canvas-based renderer.
- The provider abstraction is intentionally narrow today. Built-in controls follow Mirage's control-node path, while generic DOM and custom-element hosting remains a thinner extensibility seam than a full arbitrary component-library adapter.

## Where to Go Next

- For current control coverage and gaps: [Parity Matrix](docs/PARITY_MATRIX.md), [Handler Parity Matrix](docs/HANDLER_PARITY_MATRIX.md), [Parity Gaps](docs/PARITY_GAPS.md)
- For architecture guardrails: [Architecture Guardrails](docs/ARCHITECTURE_GUARDRAILS.md)
- For provider work: [Provider Authoring](docs/PROVIDER_AUTHORING.md)
- For project decisions and reframes: [Decisions Log](docs/DECISIONS.md)
- For the wrapped upstream sample lane: [Controls Sample Page Status](docs/CONTROLS_SAMPLE_PAGE_STATUS.md)

<details>
<summary>Optional validation and diagnostics</summary>

Install the Playwright browser for E2E:

```bash
dotnet build Mirage.CI.slnf --configuration Release
pwsh tests/Mirage.E2E.Tests/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
```

Run the test suites:

```bash
dotnet test tests/Mirage.Core.Tests/Mirage.Core.Tests.csproj --configuration Release --no-build
dotnet test tests/Mirage.Integration.Tests/Mirage.Integration.Tests.csproj --configuration Release --no-build
MIRAGE_E2E_HOST_NO_BUILD=1 dotnet test tests/Mirage.E2E.Tests/Mirage.E2E.Tests.csproj --configuration Release --no-build
```

Run the local slowness matrix:

```bash
MIRAGE_MATRIX_RUNS=1 \
MIRAGE_PERF_BUDGET_FILE=tools/diagnostics/perf-budgets-balanced.cfg \
tools/diagnostics/run-slowness-matrix.sh
```

</details>

The matrix writes machine-readable summaries under a timestamped output directory and exits non-zero when the configured hot-reload-off budget profile fails.

## Runtime invariant

Mirage startup requires `IMirageComponentProvider` registration.

If missing, startup fails with:

`MirageRuntime requires IMirageComponentProvider. Register one with UseMirageComponentProvider<TProvider>().`

## Map Support (Provider Model)

- `Map` is a built-in Mirage control-node path (`ComponentKind.Map`), not a generic `WebNode`.
- Mirage core semantics are region-first (`center + span`) with MAUI-shaped map options (`MapType`, `IsShowingUser`, `IsScrollEnabled`, `IsZoomEnabled`, `IsTrafficEnabled`), identity-based pins, and identity-based map elements (`Circle`/`Polyline`/`Polygon`).
- Browser map realization is provider-owned through `IMirageMapProvider`/`IMirageMapAdapter`; Mirage core/runtime stays provider-neutral.
- First browser map provider is Leaflet (`UseMirageLeafletMaps(...)`) in `src/Mirage.Provider.LeafletMaps`.
- Leaflet asset policy defaults to CDN JS/CSS URLs (configurable), with explicit initialization diagnostics and visible error state on provider/asset failures.
- `LeafletAssetMode.Local` requires explicit local JS/CSS URLs; Mirage fails fast with diagnostics when local mode is enabled without local URLs.
- Leaflet default tile source is OpenStreetMap with attribution as a provider default policy; tile URL/attribution are configurable and not Mirage core semantics.
- Runtime parity contract and CI guard live at `docs/parity/map-parity-contract.json` and `tests/Mirage.Integration.Tests/MapParityContractTests.cs`.
- Explicit non-parity/constraint entries are tracked in `docs/parity/map-parity-divergences.json` with test references.
- Upstream MAUI shared-code risks for unofficial/browser targets are tracked separately in `docs/UPSTREAM_TARGET_RISKS.md`.

## Notes

- `Mirage.Web` is provider-neutral browser infrastructure.
- MAUI framework/logical semantics remain MAUI-side; Mirage models platform realization only.
- `VisualNodeBase` is a shared platform-side visual/layout projection layer, not a duplicate MAUI framework model.
- Provider-specific mappings (Fluent, UI5, custom) live only in provider projects.
- Current provider abstraction is intentionally narrow: deep portability across arbitrary web-component libraries is deferred until Mirage grows slots/child projection, structured payloads, overlay hosting, provider-owned lifecycle hooks, and shadow/part-aware targeting.
- Navigation is `NavigationPage`-first: MAUI stack changes are the source of truth and Mirage projects them into browser URL/history through `UseMirageNavigation(...)`.
- `NavigationPage` is realized as a built-in navigation shell (nav bar, back button, title slot, toolbar slot, bounded content host) on the control-node path.
- Dependency direction for samples is locked: `*.Fluent -> *.Shared` and `*.Maui -> *.Shared`.
- Fluent UI Web Components are built on FAST foundations, but Mirage’s public reference-provider story is Fluent-first.
- Providers must support control-node and web-node realization for full Mirage behavior.
- Mirage preserves MAUI’s explicit scrolling model: `ContentPage` is non-scrollable by default and scrolling is introduced explicitly with `ScrollView`.
- Shared sample projects (`Counter.Shared`, `Showcase.Shared`) are included in `Mirage.sln`; native MAUI host projects are not.
- `ControlsSample` wrappers (`ControlsSample.Shared`, `ControlsSample.Fluent`, `ControlsSample.Maui`) provide a subtree-synced integration lane for upstream `dotnet/maui` Controls.Sample.
- Mirage browser hosts automatically collect bundled `MauiImage` assets (including transitive `ProjectReference` projects), publish them to `/_mirage/assets/...`, and resolve MAUI image source strings through a shared runtime resolver + manifest (`/_mirage/assets/images.manifest.v1.json`).
- Bundled-image wave scope is file-based images only (`.png`, `.jpg`, `.jpeg`, `.gif`, `.webp`, `.svg`); stream/font/full MAUI image pipeline parity is explicitly deferred.
- Upstream license compliance for the vendored Controls.Sample lane is tracked in `docs/THIRD_PARTY_NOTICES.md`.
- The old `Maui.Browser*` showcase/parity stack is not part of the active solution wiring.
- Architecture benchmark harness: `tools/benchmarks/Mirage.ArchitectureBenchmarks`.
- Current rendering wave focus is correctness-first (shared visual projection, stack/grid determinism, and DOM-backed surface rendering) before richer control breadth.

## Early-lock architecture guardrails

- Built-in Mirage handlers must continue to emit control nodes.
- `WebNode` is for extensibility (third-party/custom/BYO components), not built-in fallback.
- `Mirage.Web` stays provider-neutral and must not contain provider mappings.
- Web-node realization must preserve requested element identity end-to-end.
- See [Architecture Guardrails](docs/ARCHITECTURE_GUARDRAILS.md) and [Provider Authoring](docs/PROVIDER_AUTHORING.md).
