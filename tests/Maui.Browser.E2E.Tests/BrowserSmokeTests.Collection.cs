using Microsoft.Playwright;
using Xunit;

namespace Maui.Browser.E2E.Tests;

public sealed partial class BrowserSmokeTests
{
    [Fact]
    public async Task CollectionViewViabilityWorks()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenDemoAsync(context);

        const string collectionSelector = "[data-automation-id='collection-view']";
        const string itemsHostSelector = "[data-automation-id='collection-view'] [data-maui-cv-items-host='true']";
        const string scrollHostSelector = "[data-automation-id='collection-view'] [data-maui-cv-scroll-host='true']";
        const string emptyHostSelector = "[data-automation-id='collection-view'] [data-maui-cv-empty-host='true']";

        await page.WaitForSelectorAsync(collectionSelector, new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Attached
        });
        await ScrollIntoViewAsync(page, collectionSelector);

        var scrollOverflowY = await ReadComputedStylePropertyAsync(page, scrollHostSelector, "overflowY");
        Assert.True(scrollOverflowY is "auto" or "scroll");

        var itemsDisplay = await ReadComputedStylePropertyAsync(page, itemsHostSelector, "display");
        var itemsDirection = await ReadComputedStylePropertyAsync(page, itemsHostSelector, "flexDirection");
        Assert.Equal("flex", itemsDisplay);
        Assert.Equal("column", itemsDirection);

        var initialTitles = await ReadTextContentListAsync(page, "[data-automation-id='collection-item-title']");
        Assert.Equal(["Alpha row", "Beta row", "Gamma row", "Delta row"], initialTitles);
        Assert.Equal(4, await ReadElementCountAsync(page, "[data-automation-id='collection-item-row']"));

        var beforeDiagnostics = await ReadDiagnosticsAsync(page);

        await page.Locator("[data-automation-id='collection-add-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Collection items: 5, last mutation: add");
        Assert.Equal(5, await ReadElementCountAsync(page, "[data-automation-id='collection-item-row']"));

        await page.Locator("[data-automation-id='collection-remove-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Collection items: 4, last mutation: remove");
        Assert.Equal(4, await ReadElementCountAsync(page, "[data-automation-id='collection-item-row']"));

        await page.Locator("[data-automation-id='collection-clear-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Collection items: 0, last mutation: clear");
        await page.WaitForSelectorAsync("[data-automation-id='collection-empty-view']");
        Assert.Equal("none", await ReadComputedStylePropertyAsync(page, scrollHostSelector, "display"));
        Assert.NotEqual("none", await ReadComputedStylePropertyAsync(page, emptyHostSelector, "display"));

        await page.Locator("[data-automation-id='collection-reset-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Collection items: 4, last mutation: reset");
        Assert.Equal(4, await ReadElementCountAsync(page, "[data-automation-id='collection-item-row']"));
        Assert.Equal("none", await ReadComputedStylePropertyAsync(page, emptyHostSelector, "display"));

        for (var i = 0; i < 3; i++)
        {
            await page.Locator("[data-automation-id='collection-add-button']").ClickAsync();
            await page.Locator("[data-automation-id='collection-remove-button']").ClickAsync();
            await page.Locator("[data-automation-id='collection-clear-button']").ClickAsync();
            await page.Locator("[data-automation-id='collection-reset-button']").ClickAsync();
        }

        await page.WaitForSelectorAsync("text=Collection items: 4, last mutation: reset");
        var afterDiagnostics = await ReadDiagnosticsAsync(page);
        Assert.True(
            afterDiagnostics.ElementHandleCount <= beforeDiagnostics.ElementHandleCount + 30,
            $"Expected collection mutation handles near baseline. Before={beforeDiagnostics.ElementHandleCount}, After={afterDiagnostics.ElementHandleCount}");
        Assert.True(
            afterDiagnostics.EventSubscriptionCount <= beforeDiagnostics.EventSubscriptionCount + 4,
            $"Expected collection mutation subscriptions near baseline. Before={beforeDiagnostics.EventSubscriptionCount}, After={afterDiagnostics.EventSubscriptionCount}");
        Assert.True(
            afterDiagnostics.ResizeSubscriptionCount <= beforeDiagnostics.ResizeSubscriptionCount + 1,
            $"Expected collection mutation resize subscriptions near baseline. Before={beforeDiagnostics.ResizeSubscriptionCount}, After={afterDiagnostics.ResizeSubscriptionCount}");
    }
}
