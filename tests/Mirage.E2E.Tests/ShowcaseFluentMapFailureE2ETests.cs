using Microsoft.Playwright;
using System.Collections.Concurrent;
using System.Text;
using Xunit;

namespace Mirage.E2E.Tests;

public sealed class ShowcaseFluentMapProviderMissingE2ETests : IClassFixture<ShowcaseMapProviderDisabledFixture>
{
    private readonly ShowcaseMapProviderDisabledFixture _fixture;

    public ShowcaseFluentMapProviderMissingE2ETests(ShowcaseMapProviderDisabledFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MapLab_WithoutRegisteredProvider_ShowsDeterministicErrorState()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 860);
        var page = await ShowcaseMapE2ETestHelpers.OpenMapLabAsync(context, _fixture.HttpAppUrl, _fixture.GetHostLogsSnapshot);
        var map = page.Locator("[data-automation-id='showcase-map']");

        var status = await ShowcaseMapE2ETestHelpers.WaitForErrorStatusAsync(page, map, timeoutMs: 8000);
        var provider = await map.GetAttributeAsync("data-mirage-map-provider");
        var nodeKind = await map.GetAttributeAsync("data-mirage-kind");

        var errorCode = await map.GetAttributeAsync("data-mirage-map-error-code");
        var errorMessage = await map.GetAttributeAsync("data-mirage-map-error");

        Assert.True(
            string.Equals(status, "error", StringComparison.Ordinal),
            $"Expected map status=error. status={status ?? "<null>"}, provider={provider ?? "<null>"}, kind={nodeKind ?? "<null>"}, errorCode={errorCode ?? "<null>"}, errorMessage={errorMessage ?? "<null>"}");
        Assert.Equal("provider-not-registered", errorCode);
        Assert.Contains("UseMirageLeafletMaps", errorMessage ?? string.Empty, StringComparison.Ordinal);
    }
}

public sealed class ShowcaseFluentMapAssetFailureE2ETests : IClassFixture<ShowcaseLeafletBrokenAssetsFixture>
{
    private readonly ShowcaseLeafletBrokenAssetsFixture _fixture;

    public ShowcaseFluentMapAssetFailureE2ETests(ShowcaseLeafletBrokenAssetsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MapLab_WithBrokenCdnAssets_ShowsPredictableErrorState()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 860);
        var page = await ShowcaseMapE2ETestHelpers.OpenMapLabAsync(context, _fixture.HttpAppUrl, _fixture.GetHostLogsSnapshot);
        var map = page.Locator("[data-automation-id='showcase-map']");

        var status = await ShowcaseMapE2ETestHelpers.WaitForErrorStatusAsync(page, map, timeoutMs: 12000);
        var provider = await map.GetAttributeAsync("data-mirage-map-provider");

        var errorCode = await map.GetAttributeAsync("data-mirage-map-error-code");
        var errorMessage = await map.GetAttributeAsync("data-mirage-map-error");

        Assert.True(
            string.Equals(status, "error", StringComparison.Ordinal),
            $"Expected map status=error under broken CDN assets. status={status ?? "<null>"}, provider={provider ?? "<null>"}, errorCode={errorCode ?? "<null>"}, errorMessage={errorMessage ?? "<null>"}");
        Assert.Contains(
            errorCode ?? string.Empty,
            new[]
            {
                "css-load-failed",
                "css-timeout",
                "js-load-failed",
                "js-timeout",
                "leaflet-global-missing",
                "initialization-failed",
                "provider-not-ready"
            });
        Assert.False(string.IsNullOrWhiteSpace(errorMessage));
    }
}

public sealed class ShowcaseFluentMapLocalAssetModeValidationE2ETests : IClassFixture<ShowcaseLeafletLocalModeWithoutUrlsFixture>
{
    private readonly ShowcaseLeafletLocalModeWithoutUrlsFixture _fixture;

    public ShowcaseFluentMapLocalAssetModeValidationE2ETests(ShowcaseLeafletLocalModeWithoutUrlsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MapLab_LocalAssetModeWithoutExplicitLocalUrls_ShowsConfigurationError()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 860);
        var page = await ShowcaseMapE2ETestHelpers.OpenMapLabAsync(context, _fixture.HttpAppUrl, _fixture.GetHostLogsSnapshot);
        var map = page.Locator("[data-automation-id='showcase-map']");

        var status = await ShowcaseMapE2ETestHelpers.WaitForErrorStatusAsync(page, map, timeoutMs: 8000);
        var errorCode = await map.GetAttributeAsync("data-mirage-map-error-code");
        var errorMessage = await map.GetAttributeAsync("data-mirage-map-error");

        Assert.Equal("error", status);
        Assert.Equal("local-assets-not-configured", errorCode);
        Assert.Contains("local", errorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class ShowcaseFluentMapLocalAssetModeInvalidUrlE2ETests : IClassFixture<ShowcaseLeafletLocalModeInvalidUrlsFixture>
{
    private readonly ShowcaseLeafletLocalModeInvalidUrlsFixture _fixture;

    public ShowcaseFluentMapLocalAssetModeInvalidUrlE2ETests(ShowcaseLeafletLocalModeInvalidUrlsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MapLab_LocalAssetModeWithProtocolRelativeUrl_ShowsConfigurationError()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 860);
        var page = await ShowcaseMapE2ETestHelpers.OpenMapLabAsync(context, _fixture.HttpAppUrl, _fixture.GetHostLogsSnapshot);
        var map = page.Locator("[data-automation-id='showcase-map']");

        var status = await ShowcaseMapE2ETestHelpers.WaitForErrorStatusAsync(page, map, timeoutMs: 8000);
        var errorCode = await map.GetAttributeAsync("data-mirage-map-error-code");
        var errorMessage = await map.GetAttributeAsync("data-mirage-map-error");

        Assert.Equal("error", status);
        Assert.Equal("local-assets-invalid-url", errorCode);
        Assert.Contains("local", errorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}

internal static class ShowcaseMapE2ETestHelpers
{
    internal static async Task<IPage> OpenMapLabAsync(IBrowserContext context, Uri? baseUrl, Func<string>? hostLogsProvider = null)
    {
        if (baseUrl is null)
        {
            throw new InvalidOperationException("Fixture app URL is not initialized.");
        }

        var page = await context.NewPageAsync();
        var diagnostics = new ConcurrentQueue<string>();
        page.Console += (_, message) => diagnostics.Enqueue($"[console:{message.Type}] {message.Text}");
        page.PageError += (_, message) => diagnostics.Enqueue($"[pageerror] {message}");
        page.RequestFailed += (_, request) => diagnostics.Enqueue($"[requestfailed] {request.Method} {request.Url} ({request.Failure ?? "unknown"})");
        var mapLabBuilder = new UriBuilder(baseUrl)
        {
            Path = "/map-lab"
        };
        var mapLabUrl = mapLabBuilder.Uri;
        await page.GotoAsync(mapLabUrl.ToString(), new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        try
        {
            await page.WaitForSelectorAsync("[data-automation-id='showcase-page-map-lab']");
        }
        catch (TimeoutException timeoutException)
        {
            var bodyText = await page.TextContentAsync("body");
            var hostLogs = hostLogsProvider is null ? string.Empty : hostLogsProvider();
            var diagnosticsSnapshot = RenderDiagnostics(diagnostics);
            throw new TimeoutException(
                $"{timeoutException.Message}\nMapLab URL: {mapLabUrl}\nBody text snapshot: {bodyText}\nHost logs:\n{hostLogs}\nPage diagnostics:\n{diagnosticsSnapshot}",
                timeoutException);
        }

        return page;
    }

    private static string RenderDiagnostics(ConcurrentQueue<string> diagnostics)
    {
        var sb = new StringBuilder();
        foreach (var line in diagnostics.TakeLast(120))
        {
            sb.AppendLine(line);
        }

        return sb.ToString();
    }

    internal static async Task<string?> WaitForErrorStatusAsync(IPage page, ILocator map, int timeoutMs)
    {
        const int pollIntervalMs = 120;
        var attempts = Math.Max(1, timeoutMs / pollIntervalMs);
        for (var index = 0; index < attempts; index++)
        {
            var status = await map.GetAttributeAsync("data-mirage-map-status");
            if (string.Equals(status, "error", StringComparison.Ordinal))
            {
                return status;
            }

            await page.WaitForTimeoutAsync(pollIntervalMs);
        }

        return await map.GetAttributeAsync("data-mirage-map-status");
    }
}
