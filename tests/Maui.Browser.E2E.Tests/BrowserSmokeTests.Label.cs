using Microsoft.Playwright;
using Xunit;

namespace Maui.Browser.E2E.Tests;

public sealed partial class BrowserSmokeTests
{
    [Fact]
    public async Task LabelDepthInteractiveParityWorks()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenHomeAsync(context);

        await page.Locator("[data-automation-id='showcase-open-control-label']").ClickAsync();
        await page.WaitForSelectorAsync("[data-automation-id='showcase-page-label-depth']");

        const string previewSelector = "[data-automation-id='parity-label-live-preview']";

        await page.Locator("[data-automation-id='parity-label-editor-text-entry']").FillAsync("Label depth live text");
        await page.WaitForFunctionAsync(
            @"() => {
                const probe = document.querySelector('[data-automation-id=""parity-label-live-preview""]');
                return !!probe && (probe.textContent ?? '').includes('Label depth live text');
            }");

        var colorBefore = await ReadComputedStylePropertyAsync(page, previewSelector, "color");
        await SetPickerSelectedIndexAsync(page, "[data-automation-id='parity-label-editor-color-picker']", 3);
        await page.WaitForFunctionAsync(
            @"(beforeColor) => {
                const probe = document.querySelector('[data-automation-id=""parity-label-live-preview""]');
                return !!probe && getComputedStyle(probe).color !== beforeColor;
            }",
            colorBefore);

        await SetInputValueAndDispatchAsync(page, "[data-automation-id='parity-label-editor-font-size-slider']", "30");
        var fontInline = await ReadInlineStylePropertyAsync(page, previewSelector, "font");
        Assert.Contains("30px", fontInline, StringComparison.Ordinal);

        await SetInputValueAndDispatchAsync(page, "[data-automation-id='parity-label-editor-character-spacing-slider']", "4");
        Assert.Equal("4px", await ReadInlineStylePropertyAsync(page, previewSelector, "letterSpacing"));

        await SetInputValueAndDispatchAsync(page, "[data-automation-id='parity-label-editor-line-height-slider']", "1.8");
        var lineHeightInline = await ReadInlineStylePropertyAsync(page, previewSelector, "lineHeight");
        Assert.Contains("1.8", lineHeightInline, StringComparison.Ordinal);

        await SetPickerSelectedIndexAsync(page, "[data-automation-id='parity-label-editor-horizontal-align-picker']", 1);
        Assert.Equal("center", await ReadInlineStylePropertyAsync(page, previewSelector, "textAlign"));

        await SetInputValueAndDispatchAsync(page, "[data-automation-id='parity-label-editor-padding-slider']", "18");
        Assert.Contains("18px", await ReadInlineStylePropertyAsync(page, previewSelector, "padding"), StringComparison.Ordinal);

        await SetPickerSelectedIndexAsync(page, "[data-automation-id='parity-label-editor-line-break-picker']", 5);
        await page.Locator("[data-automation-id='parity-label-editor-max-lines-stepper-decrement-button']").ClickAsync();
        await page.Locator("[data-automation-id='parity-label-editor-max-lines-stepper-decrement-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=maxLines=1");

        Assert.Equal("ellipsis", await ReadInlineStylePropertyAsync(page, previewSelector, "textOverflow"));
        Assert.Equal("nowrap", await ReadInlineStylePropertyAsync(page, previewSelector, "whiteSpace"));

        await SetPickerSelectedIndexAsync(page, "[data-automation-id='parity-label-editor-flow-direction-picker']", 1);
        Assert.Equal("rtl", await ReadComputedStylePropertyAsync(page, previewSelector, "direction"));

        await SetPickerSelectedIndexAsync(page, "[data-automation-id='parity-label-editor-visibility-picker']", 2);
        Assert.Equal("none", await ReadComputedStylePropertyAsync(page, previewSelector, "display"));

        await SetPickerSelectedIndexAsync(page, "[data-automation-id='parity-label-editor-visibility-picker']", 0);
        await page.WaitForFunctionAsync(
            @"() => {
                const probe = document.querySelector('[data-automation-id=""parity-label-live-preview""]');
                return !!probe && getComputedStyle(probe).display !== 'none';
            }");

        await SetInputValueAndDispatchAsync(page, "[data-automation-id='parity-label-editor-opacity-slider']", "0.4");
        Assert.Equal("0.4", await ReadComputedStylePropertyAsync(page, previewSelector, "opacity"));

        var roleBefore = await ReadAttributeAsync(page, previewSelector, "role");
        Assert.Equal("heading", roleBefore);
        await page.Locator("[data-automation-id='parity-label-editor-heading-enabled-switch']").ClickAsync();
        await page.WaitForFunctionAsync(
            @"() => {
                const probe = document.querySelector('[data-automation-id=""parity-label-live-preview""]');
                return !!probe && probe.getAttribute('role') === null;
            }");
    }

    [Fact]
    public async Task LabelDepthGapBehaviorIsDeterministic()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenHomeAsync(context);

        await page.Locator("[data-automation-id='showcase-open-control-label']").ClickAsync();
        await page.WaitForSelectorAsync("[data-automation-id='showcase-page-label-depth']");

        await page.Locator("[data-automation-id='parity-label-gap-formatted-toggle-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=gap-documented");
        var formattedText = await page.Locator("[data-automation-id='parity-label-gap-formatted-probe']").InnerTextAsync();
        Assert.Equal(string.Empty, formattedText.Trim());

        await page.Locator("[data-automation-id='parity-label-gap-html-toggle-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=raw markup text remains visible");
        var htmlProbeText = await page.Locator("[data-automation-id='parity-label-gap-html-probe']").InnerTextAsync();
        Assert.Contains("<b>Bold HTML</b>", htmlProbeText, StringComparison.Ordinal);

        await page.Locator("[data-automation-id='parity-label-gap-transform-cycle-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=TextTransform=Uppercase");
        var transformProbeText = await page.Locator("[data-automation-id='parity-label-gap-transform-probe']").InnerTextAsync();
        Assert.Equal("TextTransform probe baseline", transformProbeText.Trim());
    }

    private static async Task SetPickerSelectedIndexAsync(IPage page, string selector, int selectedIndex)
    {
        await page.EvaluateAsync(
            @"({ selector, selectedIndex }) => {
                const select = document.querySelector(selector);
                if (!select) {
                    return;
                }

                select.selectedIndex = selectedIndex;
                select.dispatchEvent(new Event('change', { bubbles: true }));
            }",
            new { selector, selectedIndex });
    }
}
