# HelloWorld sample

Run:
```bash
dotnet run --project samples/HelloWorld/HelloWorld.csproj
```

This sample is hosted in Blazor WebAssembly.

Current route highlights:
- open `/maui` to see `Label`, `StackLayout`, `Grid`, and `Button` rendered through the runtime pipeline with styling bridge v0 (`StyleTokens` -> CSS variables).
- the MudBlazor plugin package now overrides MAUI `Button` and `Entry` via renderer priority (`maui-mudblazor-button`, `maui-mudblazor-entry` classes).
- open `/maui/details/settings/profile` to deep-link into the M3 `NavigationPage` route-synced stack demo.
- enter an unknown segment such as `/maui/details/unknown` to view the MAUI-rendered NotFound state (URL unchanged).
