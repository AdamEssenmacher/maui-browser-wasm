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
