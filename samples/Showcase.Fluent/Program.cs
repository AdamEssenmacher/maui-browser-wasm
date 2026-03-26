using Mirage.Maui.Hosting;
using Mirage.Provider.LeafletMaps;
using Mirage.Provider.Fluent;
using Mirage.Web.Dom;
using Mirage.Web.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Hosting;
using Showcase.Fluent.App;
using Showcase.Shared.App;

public static class Program
{
    public static async Task Main()
    {
        if (!OperatingSystem.IsBrowser())
        {
            Console.Error.WriteLine("Showcase.Fluent must run inside browser WebAssembly runtime.");
            Console.Error.WriteLine("Use: dotnet run --project samples/Showcase.Fluent/Showcase.Fluent.csproj -r browser-wasm");
            return;
        }

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<ShowcaseApp>();
        builder.ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        });
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler(typeof(ShowcaseXamlEditorView), typeof(ShowcaseMonacoEditorHandler));
        });
        builder.UseMirageBrowserPlatform();
        builder.UseMirageNavigation(routes =>
        {
            routes.Map<ShowcaseHeroPage>("/");
            routes.Map<ShowcaseEssentialsLabPage>("/essentials-lab");
            routes.Map<ShowcaseLayoutLabPage>("/layout-lab");
            routes.Map<ShowcaseXamlLabPage>("/xaml-lab");
            routes.Map<ShowcaseNavigationLabPage>("/navigation-lab");
            routes.Map<ShowcaseMapLabPage>("/map-lab");
            routes.Map<ShowcaseProjectPage>("/projects/{projectId}");
            routes.SetFallback<ShowcaseHeroPage>();
            routes.SetDefaultTitle("Mirage Showcase");
        });
        builder.UseMirageComponentProvider<FluentMirageComponentProvider>();
        await DomElement.EnsureInteropReadyAsync();
        var startupOptions = ReadStartupMapOptions();
        if (!startupOptions.DisableMapProvider)
        {
            builder.UseMirageLeafletMaps(options =>
            {
                if (startupOptions.LeafletJavaScriptUrl is string javascriptUrl)
                {
                    options.JavaScriptUrl = javascriptUrl;
                }

                if (startupOptions.LeafletStylesheetUrl is string stylesheetUrl)
                {
                    options.StylesheetUrl = stylesheetUrl;
                }

                if (startupOptions.LeafletTileUrlTemplate is string tileUrl)
                {
                    options.TileLayerUrlTemplate = tileUrl;
                }

                if (startupOptions.LeafletTileAttribution is string attribution)
                {
                    options.TileLayerAttribution = attribution;
                }

                if (startupOptions.LeafletAssetMode is string assetMode
                    && string.Equals(assetMode, "local", StringComparison.OrdinalIgnoreCase))
                {
                    options.AssetMode = LeafletAssetMode.Local;
                }
            });
        }

        var app = builder.Build();
        await MirageBrowserAppHost.RunAsync(app, rootElementId: "app");

        await Task.Delay(Timeout.InfiniteTimeSpan);
    }

    private static StartupMapOptions ReadStartupMapOptions()
    {
        var queryOptions = ReadQueryOptions();

        var disableMapProvider = queryOptions.TryGetValue("mirageMapProvider", out var providerMode)
            ? string.Equals(providerMode, "none", StringComparison.OrdinalIgnoreCase)
            : IsDisabled("MIRAGE_DISABLE_MAP_PROVIDER");

        var assetMode = queryOptions.TryGetValue("mirageLeafletAssetMode", out var assetModeFromQuery)
            ? assetModeFromQuery
            : GetEnvironmentOrNull("MIRAGE_LEAFLET_ASSET_MODE");

        var localAssetModeRequested = string.Equals(assetMode, "local", StringComparison.OrdinalIgnoreCase);
        var javaScriptUrl = ResolveStartupAssetUrl(
            queryOptions,
            queryName: "mirageLeafletJsUrl",
            environmentName: "MIRAGE_LEAFLET_JS_URL",
            allowUnvalidatedQueryForLocalMode: localAssetModeRequested);
        var stylesheetUrl = ResolveStartupAssetUrl(
            queryOptions,
            queryName: "mirageLeafletCssUrl",
            environmentName: "MIRAGE_LEAFLET_CSS_URL",
            allowUnvalidatedQueryForLocalMode: localAssetModeRequested);

        var tileUrl = queryOptions.TryGetValue("mirageLeafletTileUrl", out var tileFromQuery)
            ? tileFromQuery
            : GetEnvironmentOrNull("MIRAGE_LEAFLET_TILE_URL");

        // Tile attribution can render HTML in Leaflet controls, so do not trust URL query overrides.
        var tileAttribution = GetEnvironmentOrNull("MIRAGE_LEAFLET_TILE_ATTRIBUTION");

        return new StartupMapOptions(
            DisableMapProvider: disableMapProvider,
            LeafletJavaScriptUrl: javaScriptUrl,
            LeafletStylesheetUrl: stylesheetUrl,
            LeafletTileUrlTemplate: tileUrl,
            LeafletTileAttribution: tileAttribution,
            LeafletAssetMode: assetMode);
    }

    private static string? ResolveStartupAssetUrl(
        IReadOnlyDictionary<string, string> queryOptions,
        string queryName,
        string environmentName,
        bool allowUnvalidatedQueryForLocalMode)
    {
        if (queryOptions.TryGetValue(queryName, out var queryValue))
        {
            var normalized = queryValue.Trim();
            if (!string.IsNullOrWhiteSpace(normalized)
                && (allowUnvalidatedQueryForLocalMode || IsSafeQueryAssetUrl(normalized)))
            {
                return normalized;
            }
        }

        return GetEnvironmentOrNull(environmentName);
    }

    private static bool IsSafeQueryAssetUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        if (value.StartsWith("/", StringComparison.Ordinal))
        {
            return true;
        }

        return !Uri.TryCreate(value, UriKind.Absolute, out _);
    }

    private static Dictionary<string, string> ReadQueryOptions()
    {
        var query = GetStartupQuery();
        if (string.IsNullOrWhiteSpace(query))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var name = Uri.UnescapeDataString(pair[..separator]);
            var value = Uri.UnescapeDataString(pair[(separator + 1)..]);
            if (!string.IsNullOrWhiteSpace(name))
            {
                options[name] = value;
            }
        }

        return options;
    }

    private static string GetStartupQuery()
    {
        var location = DomElement.GetLocationPathAndQueryAndHash();
        if (string.IsNullOrWhiteSpace(location))
        {
            return string.Empty;
        }

        var queryStart = location.IndexOf('?', StringComparison.Ordinal);
        if (queryStart < 0 || queryStart == location.Length - 1)
        {
            return string.Empty;
        }

        var fragmentStart = location.IndexOf('#', queryStart + 1);
        return fragmentStart >= 0
            ? location[(queryStart + 1)..fragmentStart]
            : location[(queryStart + 1)..];
    }

    private static bool TryGetNonEmptyEnvironment(string name, out string value)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(raw))
        {
            value = string.Empty;
            return false;
        }

        value = raw.Trim();
        return true;
    }

    private static bool IsDisabled(string name)
    {
        if (!TryGetNonEmptyEnvironment(name, out var value))
        {
            return false;
        }

        return value switch
        {
            "1" => true,
            "true" => true,
            "TRUE" => true,
            "yes" => true,
            "YES" => true,
            "on" => true,
            "ON" => true,
            _ => false
        };
    }

    private static string? GetEnvironmentOrNull(string name)
    {
        return TryGetNonEmptyEnvironment(name, out var value) ? value : null;
    }

    private sealed record StartupMapOptions(
        bool DisableMapProvider,
        string? LeafletJavaScriptUrl,
        string? LeafletStylesheetUrl,
        string? LeafletTileUrlTemplate,
        string? LeafletTileAttribution,
        string? LeafletAssetMode);
}

internal sealed class ShowcaseApp : Application
{
    public ShowcaseApp()
    {
        Resources ??= new ResourceDictionary();
        Resources.MergedDictionaries.Add(new ShowcaseMauiTemplateColors());
        Resources.MergedDictionaries.Add(new ShowcaseMauiTemplateStyles());
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        _ = activationState;
        return new Window(new NavigationPage(new ShowcaseHeroPage())
        {
            AutomationId = "showcase-navigation-root"
        });
    }
}
