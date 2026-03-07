using Microsoft.Playwright;
using Xunit;

namespace Maui.Browser.E2E.Tests;

public sealed partial class BrowserSmokeTests
{
    [Fact]
    public async Task StackLayoutSemanticProjectionWorks()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenDemoAsync(context);

        var rootDisplay = await ReadComputedStylePropertyAsync(page, "[data-automation-id='root-stack-container']", "display");
        var rootDirection = await ReadComputedStylePropertyAsync(page, "[data-automation-id='root-stack-container']", "flexDirection");
        var rootPaddingTop = await ReadComputedStylePropertyAsync(page, "[data-automation-id='root-stack-container']", "paddingTop");
        var rootPaddingLeft = await ReadComputedStylePropertyAsync(page, "[data-automation-id='root-stack-container']", "paddingLeft");
        Assert.Equal("flex", rootDisplay);
        Assert.Equal("column", rootDirection);
        Assert.Equal("20px", rootPaddingTop);
        Assert.Equal("24px", rootPaddingLeft);

        var rowDisplay = await ReadComputedStylePropertyAsync(page, "[data-automation-id='core-focus-row']", "display");
        var rowDirection = await ReadComputedStylePropertyAsync(page, "[data-automation-id='core-focus-row']", "flexDirection");
        Assert.Equal("flex", rowDisplay);
        Assert.Equal("row", rowDirection);

        var centeredAlignSelf = await ReadComputedStylePropertyAsync(page, "[data-automation-id='stack-center-alignment-probe']", "alignSelf");
        var centeredMarginLeft = await ReadComputedStylePropertyAsync(page, "[data-automation-id='stack-center-alignment-probe']", "marginLeft");
        Assert.Equal("center", centeredAlignSelf);
        Assert.Equal("24px", centeredMarginLeft);

        var rowEndAlignSelf = await ReadComputedStylePropertyAsync(page, "[data-automation-id='core-unfocus-button']", "alignSelf");
        Assert.Equal("flex-end", rowEndAlignSelf);

        var verticalTops = await ReadBoundingTopsAsync(page, [
            "[data-automation-id='live-text-label']",
            "[data-automation-id='live-input-entry']",
            "[data-automation-id='click-button']"
        ]);
        Assert.True(verticalTops[0] < verticalTops[1] && verticalTops[1] < verticalTops[2], "Expected root stack items to remain in top-to-bottom order.");

        var rowLefts = await ReadBoundingLeftsAsync(page, [
            "[data-automation-id='core-focus-button']",
            "[data-automation-id='core-unfocus-button']"
        ]);
        Assert.True(rowLefts[0] < rowLefts[1], "Expected horizontal stack items to remain in left-to-right order.");

        var fallbackDisplay = await ReadComputedStylePropertyAsync(page, "[data-automation-id='layout-fallback-absolute']", "display");
        Assert.NotEqual("flex", fallbackDisplay);

        await page.GetByRole(AriaRole.Button, new() { Name = "Click to increment count" }).ClickAsync();
        await page.WaitForSelectorAsync("text=Button clicks: 1");
    }

    [Fact]
    public async Task GridSemanticProjectionWorks()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenDemoAsync(context);

        const string gridSelector = "[data-automation-id='grid-semantic-container']";
        var gridDisplay = await ReadComputedStylePropertyAsync(page, gridSelector, "display");
        var gridGap = await ReadInlineStylePropertyAsync(page, gridSelector, "gap");
        Assert.Equal("grid", gridDisplay);
        Assert.Equal("8px 10px", gridGap);

        var gridColumns = await ReadInlineStylePropertyAsync(page, gridSelector, "gridTemplateColumns");
        var gridRows = await ReadInlineStylePropertyAsync(page, gridSelector, "gridTemplateRows");
        Assert.Equal("90px 1fr 2fr", gridColumns);
        Assert.Equal("auto 52px auto", gridRows);

        Assert.Equal("2", await ReadInlineStylePropertyAsync(page, "[data-automation-id='grid-r0-c1']", "gridColumnStart"));
        Assert.Equal("1", await ReadInlineStylePropertyAsync(page, "[data-automation-id='grid-r0-c1']", "gridRowStart"));
        Assert.Equal("1", await ReadInlineStylePropertyAsync(page, "[data-automation-id='grid-span-center']", "gridColumnStart"));
        Assert.Equal("2", await ReadInlineStylePropertyAsync(page, "[data-automation-id='grid-span-center']", "gridRowStart"));
        Assert.Equal("span 2", await ReadInlineStylePropertyAsync(page, "[data-automation-id='grid-span-center']", "gridColumnEnd"));

        Assert.Equal("center", await ReadInlineStylePropertyAsync(page, "[data-automation-id='grid-span-center']", "justifySelf"));
        Assert.Equal("end", await ReadInlineStylePropertyAsync(page, "[data-automation-id='grid-align-end']", "justifySelf"));
        Assert.Equal("end", await ReadInlineStylePropertyAsync(page, "[data-automation-id='grid-align-end']", "alignSelf"));

        var gridRowTops = await ReadBoundingTopsAsync(page, [
            "[data-automation-id='grid-r0-c0']",
            "[data-automation-id='grid-span-center']",
            "[data-automation-id='grid-r2-c0']"
        ]);
        Assert.True(gridRowTops[0] < gridRowTops[1] && gridRowTops[1] < gridRowTops[2], "Expected grid row probes to remain top-to-bottom.");

        var gridColumnLefts = await ReadBoundingLeftsAsync(page, [
            "[data-automation-id='grid-r0-c0']",
            "[data-automation-id='grid-r0-c1']",
            "[data-automation-id='grid-r0-c2']"
        ]);
        Assert.True(gridColumnLefts[0] < gridColumnLefts[1] && gridColumnLefts[1] < gridColumnLefts[2], "Expected grid column probes to remain left-to-right.");

        var absoluteFallbackDisplay = await ReadComputedStylePropertyAsync(page, "[data-automation-id='layout-fallback-absolute']", "display");
        var absoluteFallbackChildLeft = await ReadInlineStylePropertyAsync(page, "[data-automation-id='layout-fallback-absolute-child']", "left");
        Assert.NotEqual("grid", absoluteFallbackDisplay);
        Assert.EndsWith("px", absoluteFallbackChildLeft, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FlexLayoutSemanticProjectionWorks()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenFamilyPageAsync(context, "showcase-open-containers-layout-depth", "showcase-page-containers-layout-depth");

        const string flexContainerSelector = "[data-automation-id='parity-flex-container']";
        const string itemOneSelector = "[data-automation-id='parity-flex-item-1']";
        const string itemTwoSelector = "[data-automation-id='parity-flex-item-2']";
        const string itemThreeSelector = "[data-automation-id='parity-flex-item-3']";

        var beforeDiagnostics = await ReadDiagnosticsAsync(page);

        Assert.Equal("flex", await ReadComputedStylePropertyAsync(page, flexContainerSelector, "display"));
        Assert.Equal("row", await ReadComputedStylePropertyAsync(page, flexContainerSelector, "flexDirection"));
        Assert.Equal("nowrap", await ReadComputedStylePropertyAsync(page, flexContainerSelector, "flexWrap"));
        Assert.Equal("flex-start", await ReadComputedStylePropertyAsync(page, flexContainerSelector, "justifyContent"));
        Assert.Equal("stretch", await ReadComputedStylePropertyAsync(page, flexContainerSelector, "alignItems"));
        Assert.Equal("flex-start", await ReadComputedStylePropertyAsync(page, flexContainerSelector, "alignContent"));

        await page.Locator("[data-automation-id='parity-flex-toggle-direction-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Flex direction: column");
        Assert.Equal("column", await ReadComputedStylePropertyAsync(page, flexContainerSelector, "flexDirection"));

        await page.Locator("[data-automation-id='parity-flex-toggle-wrap-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Flex wrap: wrap");
        Assert.Equal("wrap", await ReadComputedStylePropertyAsync(page, flexContainerSelector, "flexWrap"));

        await page.Locator("[data-automation-id='parity-flex-toggle-justify-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Flex justify: space-between");
        Assert.Equal("space-between", await ReadComputedStylePropertyAsync(page, flexContainerSelector, "justifyContent"));

        await page.Locator("[data-automation-id='parity-flex-toggle-align-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Flex align: items=center, content=space-around");
        Assert.Equal("center", await ReadComputedStylePropertyAsync(page, flexContainerSelector, "alignItems"));
        Assert.Equal("space-around", await ReadComputedStylePropertyAsync(page, flexContainerSelector, "alignContent"));

        await page.Locator("[data-automation-id='parity-flex-toggle-direction-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Flex direction: row");
        Assert.Equal("row", await ReadComputedStylePropertyAsync(page, flexContainerSelector, "flexDirection"));

        var leftsBeforeItemPreset = await ReadBoundingLeftsAsync(page, [itemOneSelector, itemTwoSelector, itemThreeSelector]);
        Assert.True(
            leftsBeforeItemPreset[0] < leftsBeforeItemPreset[1] && leftsBeforeItemPreset[1] < leftsBeforeItemPreset[2],
            "Expected default item order to be 1,2,3.");

        await page.Locator("[data-automation-id='parity-flex-toggle-item-preset-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Flex item preset: emphasis (grow/shrink/order/basis/align-self)");

        Assert.Equal("0", await ReadInlineStylePropertyAsync(page, itemTwoSelector, "order"));
        Assert.Equal("1", await ReadInlineStylePropertyAsync(page, itemTwoSelector, "flexGrow"));
        Assert.Equal("1", await ReadInlineStylePropertyAsync(page, itemTwoSelector, "flexShrink"));
        Assert.Equal("50%", await ReadInlineStylePropertyAsync(page, itemTwoSelector, "flexBasis"));
        Assert.Equal("flex-end", await ReadInlineStylePropertyAsync(page, itemTwoSelector, "alignSelf"));

        var leftsAfterItemPreset = await ReadBoundingLeftsAsync(page, [itemOneSelector, itemTwoSelector, itemThreeSelector]);
        Assert.True(
            leftsAfterItemPreset[1] < leftsAfterItemPreset[0] && leftsAfterItemPreset[0] < leftsAfterItemPreset[2],
            "Expected item 2 to move ahead of item 1 after order preset.");

        for (var i = 0; i < 4; i++)
        {
            await page.Locator("[data-automation-id='parity-flex-toggle-direction-button']").ClickAsync();
            await page.Locator("[data-automation-id='parity-flex-toggle-wrap-button']").ClickAsync();
            await page.Locator("[data-automation-id='parity-flex-toggle-justify-button']").ClickAsync();
            await page.Locator("[data-automation-id='parity-flex-toggle-align-button']").ClickAsync();
            await page.Locator("[data-automation-id='parity-flex-toggle-item-preset-button']").ClickAsync();
        }

        var afterDiagnostics = await ReadDiagnosticsAsync(page);
        Assert.True(
            afterDiagnostics.ElementHandleCount <= beforeDiagnostics.ElementHandleCount + 20,
            $"Expected flex probe element handles near baseline. Before={beforeDiagnostics.ElementHandleCount}, After={afterDiagnostics.ElementHandleCount}");
        Assert.True(
            afterDiagnostics.EventSubscriptionCount <= beforeDiagnostics.EventSubscriptionCount + 4,
            $"Expected flex probe subscriptions near baseline. Before={beforeDiagnostics.EventSubscriptionCount}, After={afterDiagnostics.EventSubscriptionCount}");
        Assert.True(
            afterDiagnostics.ResizeSubscriptionCount <= beforeDiagnostics.ResizeSubscriptionCount + 1,
            $"Expected flex probe resize subscriptions near baseline. Before={beforeDiagnostics.ResizeSubscriptionCount}, After={afterDiagnostics.ResizeSubscriptionCount}");
    }
}
