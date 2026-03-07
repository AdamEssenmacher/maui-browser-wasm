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
        await page.WaitForSelectorAsync("[data-automation-id='showcase-page-control-label']");
        await page.WaitForSelectorAsync("[data-automation-id='showcase-control-probe-label']");

        await page.Locator("[data-automation-id='showcase-control-back']").ClickAsync();
        await page.WaitForSelectorAsync("[data-automation-id='showcase-page-home']");

        await page.Locator("[data-automation-id='showcase-open-control-graphicsview']").ClickAsync();
        await page.WaitForSelectorAsync("[data-automation-id='showcase-page-control-graphicsview']");

        var missingNotice = await page.Locator("[data-automation-id='showcase-control-missing-notice']").InnerTextAsync();
        Assert.Contains("not implemented yet", missingNotice, StringComparison.OrdinalIgnoreCase);
    }
}
