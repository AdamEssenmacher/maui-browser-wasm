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

### 3. Add an HTML shell

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

### 4. Run the host

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
pwsh tests/Mirage.E2E.Tests/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
```

Run the test suites:

```bash
dotnet test tests/Mirage.Core.Tests/Mirage.Core.Tests.csproj
dotnet test tests/Mirage.Integration.Tests/Mirage.Integration.Tests.csproj
dotnet test tests/Mirage.E2E.Tests/Mirage.E2E.Tests.csproj
```

Run the local slowness matrix:

```bash
MIRAGE_MATRIX_RUNS=1 \
MIRAGE_PERF_BUDGET_FILE=tools/diagnostics/perf-budgets-balanced.cfg \
tools/diagnostics/run-slowness-matrix.sh
```

</details>
