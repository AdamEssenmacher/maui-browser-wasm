using Microsoft.Playwright;
using Xunit;

namespace Mirage.E2E.Tests;

public sealed class CounterFastE2ETests : IClassFixture<BrowserHostFixture>
{
    private readonly BrowserHostFixture _fixture;

    public CounterFastE2ETests(BrowserHostFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task EntryTypingUpdatesLiveLabel()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        await page.Locator("[data-automation-id='counter-entry'] input").FillAsync("Mirage input");
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
    public async Task RendersFastElementsInDom()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        var fastButtonCount = await page.Locator("fast-button").CountAsync();
        var fastTextFieldCount = await page.Locator("fast-text-field").CountAsync();

        Assert.True(fastButtonCount > 0, "Expected at least one <fast-button> element.");
        Assert.True(fastTextFieldCount > 0, "Expected at least one <fast-text-field> element.");
    }

    private async Task<IPage> OpenAsync(IBrowserContext context)
    {
        if (_fixture.HttpAppUrl is null)
        {
            throw new InvalidOperationException("Fixture app URL is not initialized.");
        }

        var page = await context.NewPageAsync();
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
            throw new TimeoutException(
                $"{timeoutException.Message}\nBody text snapshot: {bodyText}",
                timeoutException);
        }

        return page;
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
}
