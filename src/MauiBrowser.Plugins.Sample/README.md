# MauiBrowser.Plugins.Sample

Sample plugin package that demonstrates how a third-party library can override
core MAUI renderers via the runtime registry priority model.

## Included override

- `SamplePluginButtonRendererFactory`: overrides `Microsoft.Maui.Controls.Button`
  rendering when registered with a higher priority than the default runtime button renderer.

## Usage

```csharp
builder.Services.AddMauiBrowserRuntime();
builder.Services.AddMauiBrowserSampleButtonPlugin();

var host = builder.Build();
host.Services.UseMauiBrowserSampleButtonPlugin();
await host.RunAsync();
```
