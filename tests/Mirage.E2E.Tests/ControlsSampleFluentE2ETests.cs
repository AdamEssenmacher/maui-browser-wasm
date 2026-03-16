using System.Collections.Concurrent;
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

        var contentPageImages = page.Locator("[data-automation-id='controls-sample-page-contentviewpage'] img[src$='dotnet_bot.png']");
        Assert.True(
            await contentPageImages.CountAsync() > 0,
            "Expected ContentViewPage to render card images with src ending in dotnet_bot.png.");

        var dotnetBotStatus = await page.EvaluateAsync<int>(
            "async () => { const response = await fetch('/dotnet_bot.png', { cache: 'no-store' }); return response.status; }");
        Assert.Equal(200, dotnetBotStatus);

        var dotnetBotPngSignature = await page.EvaluateAsync<string>(
            "async () => { const response = await fetch('/dotnet_bot.png', { cache: 'no-store' }); const bytes = new Uint8Array(await response.arrayBuffer()); return Array.from(bytes.slice(0, 4)).map(b => b.toString(16).padStart(2, '0')).join(''); }");
        Assert.Equal("89504e47", dotnetBotPngSignature);
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
