using Microsoft.Playwright;
using Xunit;

namespace Maui.Browser.E2E.Tests;

public sealed partial class BrowserSmokeTests
{
    [Fact]
    public async Task MediaVisualParityWorks()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenFamilyPageAsync(context, "parity-open-media-visual", "parity-page-media-visual");

        const string imageSelector = "[data-automation-id='parity-media-image']";
        const string imageButtonSelector = "[data-automation-id='parity-media-image-button']";
        const string borderSelector = "[data-automation-id='parity-media-border']";

        await page.WaitForSelectorAsync(imageSelector);
        await ScrollIntoViewAsync(page, imageSelector);

        await page.WaitForSelectorAsync("text=Image source mode: uri");
        var uriSource = await ReadAttributeAsync(page, imageSelector, "src");
        Assert.False(string.IsNullOrWhiteSpace(uriSource));
        Assert.Equal("uri", await ReadAttributeAsync(page, imageSelector, "data-maui-image-source-kind"));
        await page.WaitForFunctionAsync(
            @"(selector) => {
                const element = document.querySelector(selector);
                return element?.getAttribute('data-maui-image-state') === 'loaded';
            }",
            imageSelector);

        Assert.Equal("contain", await ReadComputedStylePropertyAsync(page, imageSelector, "objectFit"));
        await page.Locator("[data-automation-id='parity-media-toggle-aspect-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Image aspect: aspect-fill");
        Assert.Equal("cover", await ReadComputedStylePropertyAsync(page, imageSelector, "objectFit"));

        await page.Locator("[data-automation-id='parity-media-cycle-source-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Image source mode: file");
        Assert.Contains("media-file.svg", await ReadAttributeAsync(page, imageSelector, "src"), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("file", await ReadAttributeAsync(page, imageSelector, "data-maui-image-source-kind"));
        await page.WaitForFunctionAsync(
            @"(selector) => {
                const element = document.querySelector(selector);
                return element?.getAttribute('data-maui-image-state') === 'loaded';
            }",
            imageSelector);

        await page.Locator("[data-automation-id='parity-media-cycle-source-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Image source mode: font");
        var fontSource = await ReadAttributeAsync(page, imageSelector, "src");
        Assert.StartsWith("data:image/svg+xml;base64,", fontSource, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("font", await ReadAttributeAsync(page, imageSelector, "data-maui-image-source-kind"));

        var clicksBefore = await ReadSummaryNumberAsync(page, "[data-automation-id='parity-media-imagebutton-clicks-summary']");
        var pressedBefore = await ReadSummaryNumberAsync(page, "[data-automation-id='parity-media-imagebutton-pressed-summary']");
        var releasedBefore = await ReadSummaryNumberAsync(page, "[data-automation-id='parity-media-imagebutton-released-summary']");

        await page.Locator(imageButtonSelector).DispatchEventAsync("pointerdown");
        await page.Locator(imageButtonSelector).DispatchEventAsync("pointerup");
        await page.Locator(imageButtonSelector).ClickAsync();

        var clicksAfter = await ReadSummaryNumberAsync(page, "[data-automation-id='parity-media-imagebutton-clicks-summary']");
        var pressedAfter = await ReadSummaryNumberAsync(page, "[data-automation-id='parity-media-imagebutton-pressed-summary']");
        var releasedAfter = await ReadSummaryNumberAsync(page, "[data-automation-id='parity-media-imagebutton-released-summary']");

        Assert.True(clicksAfter > clicksBefore, $"Expected click count to increase. Before={clicksBefore}, After={clicksAfter}");
        Assert.True(pressedAfter > pressedBefore, $"Expected pressed count to increase. Before={pressedBefore}, After={pressedAfter}");
        Assert.True(releasedAfter > releasedBefore, $"Expected released count to increase. Before={releasedBefore}, After={releasedAfter}");

        Assert.Equal("3px", await ReadInlineStylePropertyAsync(page, borderSelector, "borderWidth"));
        Assert.Equal("solid", await ReadInlineStylePropertyAsync(page, borderSelector, "borderStyle"));
        Assert.Contains("14px", await ReadInlineStylePropertyAsync(page, borderSelector, "borderRadius"), StringComparison.Ordinal);

        await page.Locator("[data-automation-id='parity-media-toggle-border-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Border mode: dashed-rectangle");
        Assert.Equal("dashed", await ReadInlineStylePropertyAsync(page, borderSelector, "borderStyle"));
        Assert.Equal("4px", await ReadInlineStylePropertyAsync(page, borderSelector, "borderWidth"));

        await page.Locator("[data-automation-id='parity-media-toggle-border-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Border mode: unsupported-gradient");
        Assert.Equal("none", await ReadInlineStylePropertyAsync(page, borderSelector, "borderStyle"));

        await page.Locator("[data-automation-id='parity-media-unsupported-source-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Image source mode: unsupported-stream");
        Assert.Equal("unsupported", await ReadAttributeAsync(page, imageSelector, "data-maui-image-state"));
        Assert.Equal("unsupported", await ReadAttributeAsync(page, imageSelector, "data-maui-image-source-kind"));

        await page.Locator("[data-automation-id='parity-media-cycle-source-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Image source mode: uri");
    }

    private static async Task<int> ReadSummaryNumberAsync(IPage page, string selector)
    {
        return await page.EvaluateAsync<int>(
            @"(selector) => {
                const text = (document.querySelector(selector)?.textContent ?? '').trim();
                const match = text.match(/-?\d+/);
                if (!match) {
                    return -1;
                }

                return Number.parseInt(match[0], 10);
            }",
            selector);
    }
}
