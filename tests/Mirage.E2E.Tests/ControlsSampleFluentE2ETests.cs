using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Microsoft.Playwright;
using Xunit;

namespace Mirage.E2E.Tests;

public sealed class ControlsSampleFluentE2ETests : IClassFixture<ControlsSampleHostFixture>
{
    private readonly ControlsSampleHostFixture _fixture;

    public ControlsSampleFluentE2ETests(ControlsSampleHostFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task BrowserCatalog_ShowsRunnableImportedPagesOnly_AndNavigates()
    {
        await using var context = await _fixture.NewContextAsync();
        var (page, diagnostics) = await OpenAsync(context);

        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-absolutelayoutpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-clippingpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-contentviewpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-gridpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-tabbedpagegallerymainpage']").CountAsync());
        Assert.Equal(0, await page.Locator("[data-automation-id='controls-sample-open-layoutisenabledpage']").CountAsync());
        Assert.Equal(0, await page.Locator("[data-automation-id='controls-sample-open-blazorpage']").CountAsync());

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-open-gridpage']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-gridpage']");
            });

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='mirage-navigation-back-button']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-home']");
            });

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-open-contentviewpage']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-contentviewpage']");
            });

        var manifestStatus = await page.EvaluateAsync<int>(
            "async () => { const response = await fetch('/_mirage/assets/images.manifest.v1.json', { cache: 'no-store' }); return response.status; }");
        Assert.Equal(200, manifestStatus);

        var manifestLookup = await page.EvaluateAsync<string>(
            """
            async () => {
                const response = await fetch('/_mirage/assets/images.manifest.v1.json', { cache: 'no-store' });
                const json = await response.json();
                const key = 'ControlsSample.Shared:dotnet_bot.png';
                const imageUrl = json?.images?.[key] ?? '';
                return imageUrl;
            }
            """);

        Assert.False(string.IsNullOrWhiteSpace(manifestLookup));
        Assert.StartsWith("/_mirage/assets/ControlsSample.Shared/", manifestLookup, StringComparison.Ordinal);

        var dotnetBotStatus = await page.EvaluateAsync<int>(
            "async (url) => { const response = await fetch(url, { cache: 'no-store' }); return response.status; }",
            manifestLookup);
        Assert.Equal(200, dotnetBotStatus);

        var dotnetBotPngSignature = await page.EvaluateAsync<string>(
            "async (url) => { const response = await fetch(url, { cache: 'no-store' }); const bytes = new Uint8Array(await response.arrayBuffer()); return Array.from(bytes.slice(0, 4)).map(b => b.toString(16).padStart(2, '0')).join(''); }",
            manifestLookup);
        Assert.Equal("89504e47", dotnetBotPngSignature);

        var contentImageSources = await page.EvaluateAsync<string[]>(
            """
            () => Array
                .from(document.querySelectorAll("[data-automation-id='controls-sample-page-contentviewpage'] img"))
                .map(img => img.getAttribute("src") ?? "")
            """);
        var contentPageImages = page.Locator("[data-automation-id='controls-sample-page-contentviewpage'] img[src*='/_mirage/assets/']");
        var hostLogs = _fixture.GetHostLogsSnapshot();
        var diagnosticsSnapshot = RenderDiagnostics(diagnostics);
        Assert.True(
            await contentPageImages.CountAsync() > 0,
            $"Expected ContentViewPage to render card images from the Mirage bundled-image asset path. " +
            $"Found sources: [{string.Join(", ", contentImageSources)}]\nHost logs:\n{hostLogs}\nPage diagnostics:\n{diagnosticsSnapshot}");

        var iconHostOverflow = await page.EvaluateAsync<string>(
            """
            () => {
                const image = document.querySelector("[data-automation-id='controls-sample-page-contentviewpage'] img[src*='/_mirage/assets/']");
                const iconHost = image?.parentElement;
                if (!iconHost) {
                    return "__missing__";
                }

                return getComputedStyle(iconHost).overflow;
            }
            """);

        Assert.Equal(
            "hidden",
            iconHostOverflow);

        var iconHostBorderRadius = await page.EvaluateAsync<string>(
            """
            () => {
                const image = document.querySelector("[data-automation-id='controls-sample-page-contentviewpage'] img[src*='/_mirage/assets/']");
                const iconHost = image?.parentElement;
                if (!iconHost) {
                    return "__missing__";
                }

                return getComputedStyle(iconHost).borderRadius;
            }
            """);

        Assert.NotEqual("0px", iconHostBorderRadius);

        var overlapProbeTagName = await page.EvaluateAsync<string>(
            """
            () => {
                const description = Array
                    .from(document.querySelectorAll("[data-automation-id='controls-sample-page-contentviewpage'] span"))
                    .find(span => (span.textContent ?? "").includes("Lorem ipsum"));
                if (!description) {
                    return "__missing_description__";
                }

                const rect = description.getBoundingClientRect();
                const probeX = rect.left + Math.min(12, Math.max(1, rect.width * 0.15));
                const probeY = rect.top + Math.min(12, Math.max(1, rect.height * 0.35));
                const topElement = document.elementFromPoint(probeX, probeY);
                return topElement?.tagName ?? "__missing_probe__";
            }
            """);

        Assert.NotEqual(
            "IMG",
            overlapProbeTagName);

        var avatarAndTitleMetrics = await page.EvaluateAsync<string[]>(
            """
            () => {
                const image = document.querySelector("[data-automation-id='controls-sample-page-contentviewpage'] img[src*='/_mirage/assets/']");
                const iconHost = image?.parentElement;
                const grid = iconHost?.parentElement;
                const title = grid?.children?.item(1);
                if (!image || !iconHost || !title) {
                    return ["__missing__", "0", "0", "0", "0", "0", "0"];
                }

                const imageRect = image.getBoundingClientRect();
                const iconRect = iconHost.getBoundingClientRect();
                const titleRect = title.getBoundingClientRect();
                return [
                    "ok",
                    imageRect.width.toString(),
                    imageRect.height.toString(),
                    (iconRect.top + (iconRect.height / 2)).toString(),
                    (titleRect.top + (titleRect.height / 2)).toString(),
                    getComputedStyle(image).marginLeft,
                    getComputedStyle(image).marginTop
                ];
            }
            """);

        Assert.Equal("ok", avatarAndTitleMetrics[0]);
        Assert.True(double.Parse(avatarAndTitleMetrics[1], CultureInfo.InvariantCulture) >= 95d);
        Assert.True(double.Parse(avatarAndTitleMetrics[2], CultureInfo.InvariantCulture) >= 95d);
        Assert.True(
            Math.Abs(
                double.Parse(avatarAndTitleMetrics[3], CultureInfo.InvariantCulture)
                - double.Parse(avatarAndTitleMetrics[4], CultureInfo.InvariantCulture)) <= 8d);
        Assert.Equal("-20px", avatarAndTitleMetrics[5]);
        Assert.Equal("-20px", avatarAndTitleMetrics[6]);

        var dividerMetrics = await page.EvaluateAsync<string[]>(
            """
            () => {
                const image = document.querySelector("[data-automation-id='controls-sample-page-contentviewpage'] img[src*='/_mirage/assets/']");
                const iconHost = image?.parentElement;
                const grid = iconHost?.parentElement;
                const divider = grid?.children?.item(2);
                if (!divider) {
                    return ["__missing__", "", "", "0", "0"];
                }

                const style = getComputedStyle(divider);
                const rect = divider.getBoundingClientRect();
                return [
                    divider.tagName,
                    style.backgroundColor ?? "",
                    style.display ?? "",
                    rect.height.toString(),
                    rect.width.toString()
                ];
            }
            """);

        Assert.NotEqual("__missing__", dividerMetrics[0]);
        Assert.NotEqual("rgba(0, 0, 0, 0)", dividerMetrics[1]);
        Assert.NotEqual("none", dividerMetrics[2]);
        Assert.True(double.Parse(dividerMetrics[3], CultureInfo.InvariantCulture) >= 1d);
        Assert.True(double.Parse(dividerMetrics[4], CultureInfo.InvariantCulture) >= 120d);
    }

    private async Task<(IPage Page, ConcurrentQueue<string> Diagnostics)> OpenAsync(IBrowserContext context)
    {
        var appUrl = _fixture.HttpAppUrl ?? throw new InvalidOperationException("Host URL was not initialized.");

        var page = await context.NewPageAsync();
        var diagnostics = new ConcurrentQueue<string>();
        AttachDiagnostics(page, diagnostics);
        await page.GotoAsync(appUrl.ToString(), new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        try
        {
            await page.WaitForSelectorAsync("[data-automation-id='controls-sample-home']");
        }
        catch (TimeoutException timeoutException)
        {
            var bodyText = await page.TextContentAsync("body");
            var hostLogs = _fixture.GetHostLogsSnapshot();
            var diagnosticsSnapshot = RenderDiagnostics(diagnostics);
            throw new TimeoutException(
                $"{timeoutException.Message}\nBody text snapshot: {bodyText}\nHost logs:\n{hostLogs}\nPage diagnostics:\n{diagnosticsSnapshot}",
                timeoutException);
        }

        return (page, diagnostics);
    }

    private static void AttachDiagnostics(IPage page, ConcurrentQueue<string> diagnostics)
    {
        page.Console += (_, message) =>
        {
            diagnostics.Enqueue($"[console:{message.Type}] {message.Text}");
        };

        page.PageError += (_, message) =>
        {
            diagnostics.Enqueue($"[pageerror] {message}");
        };

        page.RequestFailed += (_, request) =>
        {
            diagnostics.Enqueue($"[requestfailed] {request.Method} {request.Url} ({request.Failure ?? "unknown"})");
        };
    }

    private static string RenderDiagnostics(ConcurrentQueue<string> diagnostics)
    {
        var sb = new StringBuilder();
        foreach (var line in diagnostics.TakeLast(80))
        {
            sb.AppendLine(line);
        }

        return sb.ToString();
    }

    private async Task NavigateWithDiagnosticsAsync(
        IPage page,
        ConcurrentQueue<string> diagnostics,
        Func<Task> navigate)
    {
        try
        {
            await navigate();
        }
        catch (Exception ex)
        {
            var bodyText = await page.TextContentAsync("body");
            var hostLogs = _fixture.GetHostLogsSnapshot();
            var diagnosticsSnapshot = RenderDiagnostics(diagnostics);
            throw new TimeoutException(
                $"{ex.Message}\nBody text snapshot: {bodyText}\nHost logs:\n{hostLogs}\nPage diagnostics:\n{diagnosticsSnapshot}",
                ex);
        }
    }
}
