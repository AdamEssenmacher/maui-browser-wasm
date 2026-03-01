using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MauiBrowser.Plugins.Sample.DependencyInjection;
using MauiBrowser.Runtime.DependencyInjection;
using HelloWorld;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddMauiBrowserRuntime();
builder.Services.AddMauiBrowserSampleButtonPlugin();

var host = builder.Build();
host.Services.UseMauiBrowserSampleButtonPlugin(priority: 100);

await host.RunAsync();
