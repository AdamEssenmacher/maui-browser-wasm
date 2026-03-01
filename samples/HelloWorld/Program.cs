using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MauiBrowser.Plugins.MudBlazor.DependencyInjection;
using MauiBrowser.Runtime.DependencyInjection;
using HelloWorld;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddMauiBrowserRuntime();
builder.Services.AddMauiBrowserMudBlazorPlugin();

var host = builder.Build();
host.Services.UseMauiBrowserMudBlazorPlugin(priority: 100);

await host.RunAsync();
