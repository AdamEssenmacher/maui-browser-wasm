# Decisions Log

Keep this short. Append new entries with date and a 2–5 sentence rationale.

## 2026-03-04
- DOM is the platform UI for the Browser backend. No canvas renderer.
- No MAUI fork initially; build an external Browser platform package using handlers + DI.
- Host remains a plain `net10.0-browser` app (no Razor required).

## 2026-03-04 (Vertical Slice 1)
- Bootstrap choice: root-view-first host (`BrowserAppHost.RunAsync(MauiApp, IView, ...)`) instead of full MAUI `Application -> Window` wiring in this slice.
- Dependency alignment: explicit MAUI NuGet package references (`Microsoft.Maui.Core` + `Microsoft.Maui.Controls` pinned via `MauiPackageVersion`) because `UseMaui` on `net10.0-browser` did not reliably provide compile-time references in this setup.
- Layout projection: apply MAUI-arranged frames directly to DOM via `position:absolute` + `left/top/width/height` for deterministic correctness first.
- Measurement scope: minimal `BrowserTextMeasurer` backed by canvas `measureText`; deferred full async font readiness pipeline to a later slice.
- Interop contract: keep `DomApi` signatures on primitive `nint` handles and maintain a JS-side handle registry (`number -> Element`) in `dom.js` rather than passing JS object references across the JSImport boundary.
- Dispatcher bootstrap: install a Browser `IDispatcherProvider` in `UseMauiBrowser()` so MAUI bindings and `BindableObject.Dispatcher` can operate without a native windowing platform.

## 2026-03-04 (Vertical Slice 2)
- Bootstrap moved to `Application -> Window` first (`BrowserAppHost.RunAsync(MauiApp, ...)`) with the root-view API retained as a compatibility overload to avoid breaking existing slice-1 callers.
- Browser window lifecycle is encapsulated behind `IBrowserWindowHost` so attach/detach/resize concerns are explicit and testable.
- Added dedicated `Window` + `ContentPage` handlers as minimal DOM container projections, keeping all platform mapping in `Maui.Browser` rather than sample composition.
- Added Playwright-based E2E smoke + stress checks as proof gates, and introduced debug diagnostics (`getDiagnostics`) in DOM interop to validate that dynamic add/remove operations return near baseline handle/subscription counts.
- Multi-window support is intentionally deferred; current Browser host explicitly tracks and uses a single active window.
- MAUI Controls in this slice still expose `IsVisible` flow for probe interactions (collapsed semantics); explicit `Visibility.Hidden` authoring from controls is deferred and remains a known gap for a later slice.
- Sample app now includes a real `.xaml` `ContentPage` rendered through Browser handlers; for this prototype the page is loaded from embedded XAML via `LoadFromXaml(typeof(...))` to keep startup deterministic in the `net10.0-browser` host without introducing additional platform tooling seams.
- `Window.Page` remaps now trigger an immediate measure/arrange pass using the last known window frame (or current DOM client size fallback) so runtime root-page swaps do not wait for browser resize to become visible.

## 2026-03-04 (Hot Reload Proof Slice)
- Added a dedicated ASP.NET Core dev host (`Maui.Browser.Sample.DevHost`) for `dotnet watch` development so browser-refresh tooling can attach without changing the existing WasmAppHost runtime path.
- Capability gate is explicit in-sample: UI now reports `Hot reload mode: metadata` or `refresh-only` using `MetadataUpdater.IsSupported` plus runtime env flags (`DOTNET_MODIFIABLE_ASSEMBLIES`, `__ASPNETCORE_BROWSER_TOOLS`).
- Debug sample builds now opt in to hot reload intent via `WasmEnableHotReload=true` and `MetadataUpdaterSupport=true`; a Debug-only reference to `Microsoft.DotNet.HotReload.WebAssembly.Browser` is included so browser initializer assets are available when the runtime supports metadata deltas.
- A lightweight prerequisite check script validates Debug runtime config flags and hot reload JS asset presence; this runs in CI as a proof guard.
- Current XAML behavior uses fallback full-page refresh: DevHost watches `*.xaml`, rebuilds the sample app, and client-side diagnostics poll a version endpoint to force reload when the rebuilt output changes.

## 2026-03-05 (Hot Reload UX v2)
- Replaced polling-based XAML refresh with server push (`/_dev/events` SSE) and managed in-place `LoadFromXaml(string)` re-apply in the sample page to remove full-page reboot flashes for supported XAML edits.
- Kept this slice sample/devhost scoped: no `Maui.Browser` public API changes, while adding dev-only endpoints for XAML source retrieval and synthetic event injection to support deterministic E2E coverage.
- Added explicit managed metadata hot reload hooks via `MetadataUpdateHandler` and surfaced richer status (`mode`, `last update kind`, metadata delta count) in the sample UI.
- Failure policy is fail-soft: live XAML apply errors preserve the previous visual tree, surface a non-fatal error banner, and provide a manual `Force full reload` action.
- Non-goal remains full MAUI IDE XAML Hot Reload parity; this slice targets a practical `dotnet watch` loop for a single page (`Demo/XamlDemoPage.xaml`) only.
