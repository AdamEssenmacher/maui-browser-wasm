# MauiBrowser.Plugins.MudBlazor

Plugin package that demonstrates third-party renderer overrides using
[MudBlazor](https://mudblazor.com/).

## Included overrides

- `MudBlazorButtonRendererFactory` for `Microsoft.Maui.Controls.Button`
- `MudBlazorEntryRendererFactory` for `Microsoft.Maui.Controls.Entry`

## Usage

```csharp
builder.Services.AddMauiBrowserRuntime();
builder.Services.AddMauiBrowserMudBlazorPlugin();

var host = builder.Build();
host.Services.UseMauiBrowserMudBlazorPlugin();
await host.RunAsync();
```
