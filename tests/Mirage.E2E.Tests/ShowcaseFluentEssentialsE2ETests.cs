using System.Collections.Concurrent;
using System.Text;
using Microsoft.Playwright;
using Xunit;

namespace Mirage.E2E.Tests;

public sealed class ShowcaseFluentEssentialsE2ETests : IClassFixture<ShowcaseHostFixture>
{
    private readonly ShowcaseHostFixture _fixture;

    public ShowcaseFluentEssentialsE2ETests(ShowcaseHostFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task HeroCta_NavigatesToEssentialsLab()
    {
        await using var context = await _fixture.NewContextAsync(width: 1180, height: 760);
        var page = await OpenAsync(context, "/", "[data-automation-id='showcase-root']");

        await page.Locator("[data-automation-id='hero-cta-command-essentials-lab']").ClickAsync();
        await page.WaitForFunctionAsync("() => window.location.pathname === '/essentials-lab'");
        await page.WaitForSelectorAsync("[data-automation-id='showcase-page-essentials-lab']");

        var pathname = await page.EvaluateAsync<string>("() => window.location.pathname");
        Assert.Equal("/essentials-lab", pathname);
    }

    [Fact]
    public async Task EssentialsLab_ShowsPatchedServices_AndPreferencesPersistAcrossReload()
    {
        await using var context = await _fixture.NewContextAsync(width: 1260, height: 860);
        var diagnostics = new ConcurrentQueue<string>();
        var page = await OpenAsync(context, "/essentials-lab", "[data-automation-id='showcase-page-essentials-lab']");
        AttachDiagnostics(page, diagnostics);

        await page.WaitForFunctionAsync(
            @"() => {
                const prefs = document.querySelector('[data-automation-id=""showcase-essentials-lab-preferences-patched""]');
                const appInfo = document.querySelector('[data-automation-id=""showcase-essentials-lab-appinfo-patched""]');
                return !!prefs && !!appInfo
                    && (prefs.textContent || '').trim().endsWith('true')
                    && (appInfo.textContent || '').trim().endsWith('true');
            }");

        await page.Locator("[data-automation-id='showcase-essentials-lab-reset']").ClickAsync();
        await WaitForCounterAsync(page, "0", diagnostics);

        await page.Locator("[data-automation-id='showcase-essentials-lab-increment']").ClickAsync();
        await WaitForCounterAsync(page, "1", diagnostics);

        var staticSummary = await page.Locator("[data-automation-id='showcase-essentials-lab-appinfo-static']").InnerTextAsync();
        var injectedSummary = await page.Locator("[data-automation-id='showcase-essentials-lab-appinfo-di']").InnerTextAsync();

        Assert.False(string.IsNullOrWhiteSpace(staticSummary));
        Assert.Equal(staticSummary, injectedSummary);

        await page.ReloadAsync();
        await page.WaitForSelectorAsync("[data-automation-id='showcase-page-essentials-lab']");
        await page.WaitForFunctionAsync(
            @"() => {
                const prefs = document.querySelector('[data-automation-id=""showcase-essentials-lab-preferences-patched""]');
                return !!prefs && (prefs.textContent || '').trim().endsWith('true');
            }");
        await WaitForCounterAsync(page, "1", diagnostics);
    }

    private async Task<IPage> OpenAsync(IBrowserContext context, string path, string waitForSelector)
    {
        if (_fixture.HttpAppUrl is null)
        {
            throw new InvalidOperationException("Showcase host URL is not available.");
        }

        var page = await context.NewPageAsync();
        var targetUri = new Uri(_fixture.HttpAppUrl, path);
        await page.GotoAsync(targetUri.ToString(), new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await page.WaitForSelectorAsync(waitForSelector);
        return page;
    }

    private static void AttachDiagnostics(IPage page, ConcurrentQueue<string> diagnostics)
    {
        page.Console += (_, message) => diagnostics.Enqueue($"[console:{message.Type}] {message.Text}");
        page.PageError += (_, message) => diagnostics.Enqueue($"[pageerror] {message}");
        page.RequestFailed += (_, request) =>
            diagnostics.Enqueue($"[requestfailed] {request.Method} {request.Url} ({request.Failure ?? "unknown"})");
    }

    private static async Task WaitForCounterAsync(IPage page, string expectedValue, ConcurrentQueue<string> diagnostics)
    {
        try
        {
            await page.WaitForFunctionAsync(
                @"(expectedValue) => {
                    const staticLabel = document.querySelector('[data-automation-id=""showcase-essentials-lab-counter-static""]');
                    const injectedLabel = document.querySelector('[data-automation-id=""showcase-essentials-lab-counter-di""]');
                    if (!staticLabel || !injectedLabel) {
                        return false;
                    }

                    return (staticLabel.textContent || '').trim() === expectedValue
                        && (injectedLabel.textContent || '').trim() === expectedValue;
                }",
                expectedValue);
        }
        catch (TimeoutException error)
        {
            var staticValue = await SafeReadTextAsync(page, "[data-automation-id='showcase-essentials-lab-counter-static']");
            var injectedValue = await SafeReadTextAsync(page, "[data-automation-id='showcase-essentials-lab-counter-di']");
            throw new TimeoutException(
                $"Timed out waiting for counter value '{expectedValue}'. static='{staticValue}' injected='{injectedValue}' diagnostics={RenderDiagnostics(diagnostics)}",
                error);
        }
    }

    private static async Task<string> SafeReadTextAsync(IPage page, string selector)
    {
        var count = await page.Locator(selector).CountAsync();
        if (count == 0)
        {
            return "__missing__";
        }

        return (await page.Locator(selector).InnerTextAsync()).Trim();
    }

    private static string RenderDiagnostics(ConcurrentQueue<string> diagnostics)
    {
        if (diagnostics.IsEmpty)
        {
            return "__none__";
        }

        var builder = new StringBuilder();
        foreach (var line in diagnostics.TakeLast(20))
        {
            builder.Append('[').Append(line).Append(']');
        }

        return builder.ToString();
    }
}
