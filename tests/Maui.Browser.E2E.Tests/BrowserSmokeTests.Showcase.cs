using Microsoft.Playwright;
using Xunit;

namespace Maui.Browser.E2E.Tests;

public sealed partial class BrowserSmokeTests
{
    [Fact]
    public async Task ShowcaseThemeAndStyleLabWork()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenFamilyPageAsync(context, "showcase-open-home", "showcase-page-home");

        const string dynamicBlockSelector = "[data-automation-id='showcase-style-dynamic-block']";
        const string stateButtonSelector = "[data-automation-id='showcase-style-state-button']";
        const string focusEntrySelector = "[data-automation-id='showcase-style-focus-entry']";

        var themeSummaryBefore = await page.Locator("[data-automation-id='showcase-theme-summary']").InnerTextAsync();
        var dynamicBlockColorBefore = await ReadComputedStylePropertyAsync(page, dynamicBlockSelector, "backgroundColor");

        await page.Locator(focusEntrySelector).FillAsync("state is preserved");
        await page.Locator("[data-automation-id='showcase-theme-toggle-button']").ClickAsync();

        await page.WaitForFunctionAsync(
            @"(previousText) => {
                const node = document.querySelector('[data-automation-id=""showcase-theme-summary""]');
                return !!node && (node.textContent ?? '').trim() !== previousText;
            }",
            themeSummaryBefore.Trim());

        var dynamicBlockColorAfter = await ReadComputedStylePropertyAsync(page, dynamicBlockSelector, "backgroundColor");
        Assert.NotEqual(dynamicBlockColorBefore.Trim(), dynamicBlockColorAfter.Trim());

        Assert.Equal("state is preserved", await ReadInputValueAsync(page, focusEntrySelector));

        var implicitLabelColor = await ReadComputedStylePropertyAsync(page, "[data-automation-id='showcase-style-implicit-label']", "color");
        var explicitLabelBackground = await ReadComputedStylePropertyAsync(page, "[data-automation-id='showcase-style-explicit-label']", "backgroundColor");
        var styleClassLetterSpacing = await ReadComputedStylePropertyAsync(page, "[data-automation-id='showcase-style-class-label']", "letterSpacing");

        Assert.False(string.IsNullOrWhiteSpace(implicitLabelColor));
        Assert.False(string.IsNullOrWhiteSpace(explicitLabelBackground));
        Assert.False(string.IsNullOrWhiteSpace(styleClassLetterSpacing));

        var buttonBorderWidth = await ReadInlineStylePropertyAsync(page, stateButtonSelector, "borderWidth");
        var buttonBorderRadius = await ReadInlineStylePropertyAsync(page, stateButtonSelector, "borderRadius");
        Assert.Equal("1.5px", buttonBorderWidth);
        Assert.Equal("12px", buttonBorderRadius);

        await page.Locator(stateButtonSelector).DispatchEventAsync("pointerdown");
        Assert.Equal("pressed", await ReadAttributeAsync(page, stateButtonSelector, "data-maui-button-state"));

        await page.EvaluateAsync(
            @"() => {
                document.body.dispatchEvent(new PointerEvent('pointerup', { bubbles: true, cancelable: true }));
            }");

        await page.WaitForFunctionAsync(
            @"() => {
                const button = document.querySelector('[data-automation-id=""showcase-style-state-button""]');
                return button?.getAttribute('data-maui-button-state') === null;
            }");

        Assert.Equal("disabled", await ReadAttributeAsync(page, "[data-automation-id='showcase-style-disabled-button']", "disabled"));
    }

    [Fact]
    public async Task ShowcasePageFlowWorks()
    {
        await using var context = await _fixture.NewContextAsync();

        var textPage = await OpenFamilyPageAsync(context, "showcase-open-text-input", "showcase-page-text-input");
        await textPage.Locator("[data-automation-id='showcase-text-entry']").FillAsync("launch polish");
        await textPage.WaitForSelectorAsync("text=Entry: launch polish | completed: 0");
        await textPage.Locator("[data-automation-id='showcase-text-entry']").PressAsync("Enter");
        await textPage.WaitForSelectorAsync("text=Entry: launch polish | completed: 1");

        await textPage.Locator("[data-automation-id='showcase-text-open-form-media']").ClickAsync();
        await textPage.WaitForSelectorAsync("[data-automation-id='showcase-page-form-media']");

        await textPage.Locator("[data-automation-id='showcase-form-apply-programmatic']").ClickAsync();
        await textPage.WaitForSelectorAsync("text=Switch:");
        Assert.False(string.IsNullOrWhiteSpace(await ReadAttributeAsync(textPage, "[data-automation-id='showcase-media-image']", "src")));

        await textPage.Locator("[data-automation-id='showcase-form-open-layout-collection']").ClickAsync();
        await textPage.WaitForSelectorAsync("[data-automation-id='showcase-page-layout-collection']");

        var flexDisplay = await ReadComputedStylePropertyAsync(textPage, "[data-automation-id='showcase-layout-flex']", "display");
        Assert.Equal("flex", flexDisplay);

        var rowsBefore = await ReadElementCountAsync(textPage, "[data-automation-id='showcase-layout-item-row']");
        await textPage.Locator("[data-automation-id='showcase-layout-add-item']").ClickAsync();
        await textPage.WaitForFunctionAsync(
            @"(before) => document.querySelectorAll('[data-automation-id=""showcase-layout-item-row""]').length > before",
            rowsBefore);

        await textPage.Locator("[data-automation-id='showcase-layout-start-refresh']").ClickAsync();
        await textPage.WaitForFunctionAsync(
            @"() => {
                const summary = document.querySelector('[data-automation-id=""showcase-layout-refresh-summary""]');
                return !!summary && (summary.textContent ?? '').includes('refreshing: true');
            }");

        await textPage.Locator("[data-automation-id='showcase-layout-complete-refresh']").ClickAsync();
        await textPage.WaitForFunctionAsync(
            @"() => {
                const summary = document.querySelector('[data-automation-id=""showcase-layout-refresh-summary""]');
                return !!summary && (summary.textContent ?? '').includes('refreshing: false');
            }");
    }
}
