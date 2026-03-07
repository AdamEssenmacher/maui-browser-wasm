using Xunit;

namespace Maui.Browser.E2E.Tests;

public sealed partial class BrowserSmokeTests
{
    [Fact]
    public async Task ShowcaseHomeListsAllMappedControlTypes()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenHomeAsync(context);

        var controlRowCount = await page.EvaluateAsync<int>(
            @"() => Array.from(document.querySelectorAll('[data-automation-id]'))
                .map((node) => node.getAttribute('data-automation-id') ?? '')
                .filter((id) => id.startsWith('showcase-control-list-row-'))
                .length");

        Assert.Equal(34, controlRowCount);

        var labelRow = await page.Locator("[data-automation-id='showcase-control-list-row-label']").InnerTextAsync();
        Assert.Contains("Label", labelRow, StringComparison.Ordinal);

        var missingRow = await page.Locator("[data-automation-id='showcase-control-list-row-graphicsview']").InnerTextAsync();
        Assert.Contains("not implemented yet", missingRow, StringComparison.OrdinalIgnoreCase);

        await page.Locator("[data-automation-id='showcase-open-control-label']").ClickAsync();
        await page.WaitForSelectorAsync("[data-automation-id='showcase-page-label-depth']");
        await page.WaitForSelectorAsync("[data-automation-id='parity-label-live-preview']");
        await page.CloseAsync();

        page = await OpenHomeAsync(context);

        await page.Locator("[data-automation-id='showcase-open-control-graphicsview']").ClickAsync();
        await page.WaitForSelectorAsync("[data-automation-id='showcase-page-control-graphicsview']");

        var missingNotice = await page.Locator("[data-automation-id='showcase-control-missing-notice']").InnerTextAsync();
        Assert.Contains("not implemented yet", missingNotice, StringComparison.OrdinalIgnoreCase);
    }
}
