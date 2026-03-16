using System.Collections.Concurrent;
using System.Text;
using Microsoft.Playwright;
using Xunit;

namespace Mirage.E2E.Tests;

public sealed class CounterFluentE2ETests : IClassFixture<BrowserHostFixture>
{
    private static readonly string? s_diagnosticsLogFile = Environment.GetEnvironmentVariable("MIRAGE_DIAGNOSTICS_LOG_FILE");
    private static readonly object s_diagnosticsLogSync = new();
    private readonly BrowserHostFixture _fixture;

    public CounterFluentE2ETests(BrowserHostFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task EntryTypingUpdatesLiveLabel()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        await SetFluentTextFieldValueAsync(page, "[data-automation-id='counter-entry']", "Mirage input");
        await page.WaitForTimeoutAsync(120);

        var labelText = await page.Locator("[data-automation-id='counter-live-label']").InnerTextAsync();
        Assert.Contains("Mirage input", labelText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ButtonClickUpdatesCount()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        await page.Locator("[data-automation-id='counter-button']").ClickAsync();
        await page.WaitForTimeoutAsync(120);

        var countText = await page.Locator("[data-automation-id='counter-count-label']").InnerTextAsync();
        Assert.Contains("Count: 1", countText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LayoutRespondsToResize()
    {
        await using var context = await _fixture.NewContextAsync(width: 900, height: 700);
        var page = await OpenAsync(context);

        var initialWidth = await ReadElementWidthAsync(page, "[data-automation-id='counter-entry']");
        await page.SetViewportSizeAsync(500, 700);
        await page.WaitForTimeoutAsync(200);
        var resizedWidth = await ReadElementWidthAsync(page, "[data-automation-id='counter-entry']");

        Assert.True(resizedWidth < initialWidth, $"Expected resized width to shrink. Initial={initialWidth}, Resized={resizedWidth}");
    }

    [Fact]
    public async Task RendersExpectedElementsInDom()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        var buttonCount = await page.Locator("button[data-mirage-kind='Button']").CountAsync();
        var fluentTextFieldCount = await page.Locator("fluent-text-field").CountAsync();

        Assert.True(buttonCount > 0, "Expected at least one Mirage Button host element.");
        Assert.True(fluentTextFieldCount > 0, "Expected at least one <fluent-text-field> element.");
    }

    private async Task<IPage> OpenAsync(IBrowserContext context)
    {
        if (_fixture.HttpAppUrl is null)
        {
            throw new InvalidOperationException("Fixture app URL is not initialized.");
        }

        var page = await context.NewPageAsync();
        var diagnostics = new ConcurrentQueue<string>();
        AttachDiagnostics(page, diagnostics);
        await page.GotoAsync(_fixture.HttpAppUrl.ToString(), new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        try
        {
            await page.WaitForSelectorAsync("[data-automation-id='counter-title']");
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

        return page;
    }

    private static void AttachDiagnostics(IPage page, ConcurrentQueue<string> diagnostics)
    {
        page.Console += (_, message) =>
        {
            diagnostics.Enqueue($"[console:{message.Type}] {message.Text}");
            AppendDiagnosticsLine(message.Text);
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

    private static void AppendDiagnosticsLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)
            || string.IsNullOrWhiteSpace(s_diagnosticsLogFile)
            || !line.Contains("[Mirage.Diagnostics]", StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            lock (s_diagnosticsLogSync)
            {
                File.AppendAllText(s_diagnosticsLogFile, line + Environment.NewLine);
            }
        }
        catch
        {
            // Diagnostics file output is best-effort and must never fail the test.
        }
    }

    private static async Task<double> ReadElementWidthAsync(IPage page, string selector)
    {
        return await page.EvaluateAsync<double>(
            @"(selector) => {
                const element = document.querySelector(selector);
                if (!element) {
                    return Number.NaN;
                }

                return element.getBoundingClientRect().width;
            }",
            selector);
    }

    private static async Task SetFluentTextFieldValueAsync(IPage page, string selector, string value)
    {
        var applied = await page.EvaluateAsync<bool>(
            @"({ selector, value }) => {
                const element = document.querySelector(selector);
                if (!element) {
                    return false;
                }

                element.value = value ?? '';
                let inputEvent;
                try {
                    inputEvent = new InputEvent('input', { bubbles: true, composed: true });
                } catch {
                    inputEvent = new Event('input', { bubbles: true, composed: true });
                }

                element.dispatchEvent(inputEvent);
                return true;
            }",
            new { selector, value });

        Assert.True(applied, $"Failed to set Fluent text field value for selector '{selector}'.");
    }
}
