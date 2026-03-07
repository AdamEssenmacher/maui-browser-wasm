using Microsoft.Playwright;
using Xunit;

namespace Maui.Browser.E2E.Tests;

public sealed partial class BrowserSmokeTests
{
    private const string CollectionEmptyTemplateText = "Collection is empty via template binding.";

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
        var emptyViewText = (await page.Locator("[data-automation-id='collection-empty-view']").TextContentAsync())?.Trim();
        Assert.Equal(CollectionEmptyTemplateText, emptyViewText);
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

    [Fact]
    public async Task CollectionViewParityDepthWorks()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenFamilyPageAsync(context, "parity-open-collection-templated", "parity-page-collection-templated");

        const string collectionSelector = "[data-automation-id='parity-collection-view']";
        const string itemsHostSelector = "[data-automation-id='parity-collection-view'] [data-maui-cv-items-host='true']";
        const string scrollHostSelector = "[data-automation-id='parity-collection-view'] [data-maui-cv-scroll-host='true']";

        await page.WaitForSelectorAsync(collectionSelector, new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Attached
        });

        await ScrollIntoViewAsync(page, collectionSelector);
        Assert.Equal(12, await ReadElementCountAsync(page, "[data-automation-id='parity-collection-item-row']"));
        await page.WaitForSelectorAsync("[data-automation-id='parity-collection-header-template-label']");
        await page.WaitForSelectorAsync("[data-automation-id='parity-collection-footer-template-label']");

        var beforeDiagnostics = await ReadDiagnosticsAsync(page);

        await page.Locator("[data-automation-id='parity-collection-toggle-selection-mode-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Selection mode: Single");
        await page.Locator("[data-automation-id='parity-collection-item-row']").Nth(1).ClickAsync();
        await page.WaitForSelectorAsync("text=selected item: Beta row");
        Assert.Equal(1, await ReadElementCountAsync(page, "[data-maui-cv-selected='true']"));

        await page.Locator("[data-automation-id='parity-collection-item-row']").Nth(11).ClickAsync();
        await page.WaitForSelectorAsync("text=selected item: Mu row");
        await page.Locator("[data-automation-id='parity-collection-remove-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=last mutation: remove");
        await page.WaitForSelectorAsync("text=selected item: <none>");
        Assert.Equal(0, await ReadElementCountAsync(page, "[data-maui-cv-selected='true']"));
        await page.Locator("[data-automation-id='parity-collection-add-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=last mutation: add");

        await page.Locator("[data-automation-id='parity-collection-apply-programmatic-selection-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=selected item: Delta row");
        Assert.Equal(1, await ReadElementCountAsync(page, "[data-maui-cv-selected='true']"));

        await page.Locator("[data-automation-id='parity-collection-toggle-selection-mode-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Selection mode: Multiple");
        await page.Locator("[data-automation-id='parity-collection-apply-programmatic-selection-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=selected count: 2");
        Assert.Equal(2, await ReadElementCountAsync(page, "[data-maui-cv-selected='true']"));

        await page.Locator("[data-automation-id='parity-collection-toggle-selection-mode-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Selection mode: None");
        Assert.Equal(0, await ReadElementCountAsync(page, "[data-maui-cv-selected='true']"));

        var titlesBeforeMove = await ReadTextContentListAsync(page, "[data-automation-id='parity-collection-item-title']");
        await page.Locator("[data-automation-id='parity-collection-move-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=last mutation: move");
        var titlesAfterMove = await ReadTextContentListAsync(page, "[data-automation-id='parity-collection-item-title']");
        Assert.NotEmpty(titlesBeforeMove);
        Assert.NotEmpty(titlesAfterMove);
        Assert.Equal(titlesBeforeMove[0], titlesAfterMove[^1]);

        await page.Locator("[data-automation-id='parity-collection-toggle-selection-mode-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Selection mode: Single");
        await page.Locator("[data-automation-id='parity-collection-item-row']").Nth(1).ClickAsync();
        await page.WaitForSelectorAsync("text=selected item: Gamma row");
        await page.Locator("[data-automation-id='parity-collection-replace-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=last mutation: replace");
        await page.WaitForSelectorAsync("text=selected item: <none>");
        Assert.Equal(0, await ReadElementCountAsync(page, "[data-maui-cv-selected='true']"));
        var titlesAfterReplace = await ReadTextContentListAsync(page, "[data-automation-id='parity-collection-item-title']");
        Assert.Contains(titlesAfterReplace, title => title.StartsWith("Replaced", StringComparison.Ordinal));

        await page.Locator("[data-automation-id='parity-collection-toggle-header-footer-mode-button']").ClickAsync();
        await page.WaitForSelectorAsync("[data-automation-id='parity-collection-header-object-view']");
        await page.WaitForSelectorAsync("[data-automation-id='parity-collection-footer-object-view']");
        await page.Locator("[data-automation-id='parity-collection-toggle-header-footer-mode-button']").ClickAsync();
        await page.WaitForSelectorAsync("[data-automation-id='parity-collection-header-template-label']");
        await page.WaitForSelectorAsync("[data-automation-id='parity-collection-footer-template-label']");

        await page.Locator("[data-automation-id='parity-collection-scroll-end-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Last scroll action: end index");
        var endScrollTop = await ReadElementScrollTopAsync(page, scrollHostSelector);
        Assert.True(endScrollTop > 0, $"Expected non-zero scrollTop after end scroll, got {endScrollTop}.");

        await page.Locator("[data-automation-id='parity-collection-scroll-start-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Last scroll action: start index 0");
        var startScrollTop = await ReadElementScrollTopAsync(page, scrollHostSelector);
        Assert.True(startScrollTop <= endScrollTop, $"Expected start scrollTop <= end scrollTop. start={startScrollTop}, end={endScrollTop}.");

        await page.Locator("[data-automation-id='parity-collection-clear-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=last mutation: clear");
        await page.WaitForSelectorAsync("[data-automation-id='parity-collection-empty-view']");

        await page.Locator("[data-automation-id='parity-collection-reset-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=last mutation: reset");
        Assert.Equal("flex", await ReadComputedStylePropertyAsync(page, itemsHostSelector, "display"));
        Assert.Equal(12, await ReadElementCountAsync(page, "[data-automation-id='parity-collection-item-row']"));

        for (var i = 0; i < 3; i++)
        {
            await page.Locator("[data-automation-id='parity-collection-toggle-selection-mode-button']").ClickAsync();
            await page.Locator("[data-automation-id='parity-collection-apply-programmatic-selection-button']").ClickAsync();
            await page.Locator("[data-automation-id='parity-collection-move-button']").ClickAsync();
            await page.Locator("[data-automation-id='parity-collection-replace-button']").ClickAsync();
            await page.Locator("[data-automation-id='parity-collection-add-button']").ClickAsync();
            await page.Locator("[data-automation-id='parity-collection-remove-button']").ClickAsync();
        }

        var afterDiagnostics = await ReadDiagnosticsAsync(page);
        Assert.True(
            afterDiagnostics.ElementHandleCount <= beforeDiagnostics.ElementHandleCount + 40,
            $"Expected collection parity handles near baseline. Before={beforeDiagnostics.ElementHandleCount}, After={afterDiagnostics.ElementHandleCount}");
        Assert.True(
            afterDiagnostics.EventSubscriptionCount <= beforeDiagnostics.EventSubscriptionCount + 6,
            $"Expected collection parity subscriptions near baseline. Before={beforeDiagnostics.EventSubscriptionCount}, After={afterDiagnostics.EventSubscriptionCount}");
        Assert.True(
            afterDiagnostics.ResizeSubscriptionCount <= beforeDiagnostics.ResizeSubscriptionCount + 1,
            $"Expected collection parity resize subscriptions near baseline. Before={beforeDiagnostics.ResizeSubscriptionCount}, After={afterDiagnostics.ResizeSubscriptionCount}");
    }

    [Fact]
    public async Task RefreshViewCollectionIntegrationWorks()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenFamilyPageAsync(context, "parity-open-collection-templated", "parity-page-collection-templated");

        const string refreshSelector = "[data-automation-id='parity-refresh-view']";
        const string refreshTriggerSelector = "[data-automation-id='parity-refresh-view'] [data-maui-rv-trigger='true']";
        const string refreshSpinnerSelector = "[data-automation-id='parity-refresh-view'] [data-maui-rv-spinner='true']";
        const string refreshSummarySelector = "[data-automation-id='parity-refresh-summary']";
        const string refreshCompleteButtonSelector = "[data-automation-id='parity-refresh-complete-button']";
        const string refreshToggleEnabledButtonSelector = "[data-automation-id='parity-refresh-toggle-enabled-button']";
        async Task DispatchClickAsync(string selector)
        {
            await page.EvaluateAsync(
                @"(selector) => {
                    const element = document.querySelector(selector);
                    if (!element) {
                        return;
                    }

                    element.dispatchEvent(new Event('click', { bubbles: true }));
                }",
                selector);
        }
        async Task AssertRefreshInFlowAsync(string reason)
        {
            var metrics = await page.EvaluateAsync<double[]>(
                @"() => {
                    const refresh = document.querySelector('[data-automation-id=""parity-refresh-view""]');
                    const below = document.querySelector('[data-automation-id=""parity-collection-state-summary""]');
                    if (!refresh || !below) {
                        return [Number.NaN, Number.NaN];
                    }

                    const refreshRect = refresh.getBoundingClientRect();
                    const belowRect = below.getBoundingClientRect();
                    return [refreshRect.bottom, belowRect.top];
                }");

            Assert.Equal(2, metrics.Length);
            Assert.True(
                !double.IsNaN(metrics[0]) && !double.IsNaN(metrics[1]) && metrics[1] + 1 >= metrics[0],
                $"RefreshView should remain in normal flow ({reason}). refreshBottom={metrics[0]}, nextTop={metrics[1]}");
        }

        await page.WaitForSelectorAsync(refreshSelector, new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Attached
        });
        await ScrollIntoViewAsync(page, refreshSelector);

        await page.WaitForSelectorAsync("text=Refresh enabled: on, refreshing: false, command count: 0");
        Assert.Equal("false", await ReadAttributeAsync(page, refreshSelector, "data-maui-rv-refreshing"));
        Assert.Equal("true", await ReadAttributeAsync(page, refreshSelector, "data-maui-rv-enabled"));
        Assert.Equal("false", await ReadAttributeAsync(page, refreshSpinnerSelector, "data-maui-rv-spinner-active"));
        Assert.Equal("visible", await ReadComputedStylePropertyAsync(page, refreshSpinnerSelector, "visibility"));
        Assert.Equal("paused", await ReadComputedStylePropertyAsync(page, refreshSpinnerSelector, "animationPlayState"));
        await AssertRefreshInFlowAsync("initial");

        await DispatchClickAsync(refreshTriggerSelector);
        await page.WaitForSelectorAsync("text=Refresh enabled: on, refreshing: true, command count: 1");
        Assert.Equal("true", await ReadAttributeAsync(page, refreshSelector, "data-maui-rv-refreshing"));
        Assert.Equal("true", await ReadAttributeAsync(page, refreshSpinnerSelector, "data-maui-rv-spinner-active"));
        Assert.Equal("visible", await ReadComputedStylePropertyAsync(page, refreshSpinnerSelector, "visibility"));
        Assert.Equal("running", await ReadComputedStylePropertyAsync(page, refreshSpinnerSelector, "animationPlayState"));

        await DispatchClickAsync(refreshTriggerSelector);

        var summaryAfterBlockedTrigger = (await page.Locator(refreshSummarySelector).TextContentAsync())?.Trim() ?? string.Empty;
        Assert.Contains("refreshing: true", summaryAfterBlockedTrigger);
        Assert.Contains("command count: 1", summaryAfterBlockedTrigger);

        await DispatchClickAsync(refreshCompleteButtonSelector);
        await page.WaitForSelectorAsync("text=Refresh enabled: on, refreshing: false, command count: 1");
        Assert.Equal("false", await ReadAttributeAsync(page, refreshSelector, "data-maui-rv-refreshing"));
        Assert.Equal("false", await ReadAttributeAsync(page, refreshSpinnerSelector, "data-maui-rv-spinner-active"));
        Assert.Equal("visible", await ReadComputedStylePropertyAsync(page, refreshSpinnerSelector, "visibility"));
        Assert.Equal("paused", await ReadComputedStylePropertyAsync(page, refreshSpinnerSelector, "animationPlayState"));
        await AssertRefreshInFlowAsync("after-complete");

        await DispatchClickAsync(refreshToggleEnabledButtonSelector);
        await page.WaitForSelectorAsync("text=Refresh enabled: off, refreshing: false, command count: 1");
        Assert.Equal("false", await ReadAttributeAsync(page, refreshSelector, "data-maui-rv-enabled"));
        Assert.Equal("false", await ReadAttributeAsync(page, refreshSpinnerSelector, "data-maui-rv-spinner-active"));
        await AssertRefreshInFlowAsync("after-disable");

        await DispatchClickAsync(refreshTriggerSelector);
        var summaryWhileDisabled = (await page.Locator(refreshSummarySelector).TextContentAsync())?.Trim() ?? string.Empty;
        Assert.Contains("refreshing: false", summaryWhileDisabled);
        Assert.Contains("command count: 1", summaryWhileDisabled);

        await DispatchClickAsync(refreshToggleEnabledButtonSelector);
        await page.WaitForSelectorAsync("text=Refresh enabled: on, refreshing: false, command count: 1");
        await AssertRefreshInFlowAsync("after-enable");

        await DispatchClickAsync(refreshTriggerSelector);
        await page.WaitForSelectorAsync("text=Refresh enabled: on, refreshing: true, command count: 2");
        await DispatchClickAsync(refreshCompleteButtonSelector);
        await page.WaitForSelectorAsync("text=Refresh enabled: on, refreshing: false, command count: 2");

        await DispatchClickAsync("[data-automation-id='parity-collection-toggle-selection-mode-button']");
        await page.WaitForSelectorAsync("text=Selection mode: Single");
        await page.EvaluateAsync(
            @"({ selector, index }) => {
                const rows = document.querySelectorAll(selector);
                const row = rows[index];
                if (!row) {
                    return;
                }

                row.dispatchEvent(new Event('click', { bubbles: true }));
            }",
            new { selector = "[data-automation-id='parity-collection-item-row']", index = 1 });
        await page.WaitForSelectorAsync("text=selected item: Beta row");

        await DispatchClickAsync("[data-automation-id='parity-collection-add-button']");
        await page.WaitForSelectorAsync("text=Collection items: 13, last mutation: add");

        await DispatchClickAsync("[data-automation-id='parity-collection-clear-button']");
        await page.WaitForSelectorAsync("text=Collection items: 0, last mutation: clear");
        await page.WaitForSelectorAsync("[data-automation-id='parity-collection-empty-view']");

        await DispatchClickAsync("[data-automation-id='parity-collection-reset-button']");
        await page.WaitForSelectorAsync("text=Collection items: 12, last mutation: reset");
        Assert.Equal(12, await ReadElementCountAsync(page, "[data-automation-id='parity-collection-item-row']"));
    }
}
