using Microsoft.Playwright;
using Xunit;

namespace Maui.Browser.E2E.Tests;

public sealed partial class BrowserSmokeTests
{
    [Fact]
    public async Task TextInputFamilyParityWorks()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenFamilyPageAsync(context, "parity-open-text-input", "parity-page-text-input");

        await page.Locator("[data-automation-id='parity-text-entry']").FillAsync("family-entry");
        await page.WaitForSelectorAsync("text=Entry text: family-entry | completed: 0");

        await page.Locator("[data-automation-id='parity-text-entry']").PressAsync("Enter");
        await page.WaitForSelectorAsync("text=Entry text: family-entry | completed: 1");

        Assert.Equal(
            "password",
            await ReadAttributeAsync(page, "[data-automation-id='parity-text-password-entry']", "type"));

        var placeholderColor = await page.EvaluateAsync<string>(
            @"() => {
                const entry = document.querySelector('[data-automation-id=""parity-text-entry""]');
                if (!entry) {
                    return '';
                }

                return entry.style.getPropertyValue('--maui-placeholder-color') ?? '';
            }");
        Assert.False(string.IsNullOrWhiteSpace(placeholderColor));

        await page.Locator("[data-automation-id='parity-text-label-mode-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Label mode: tail truncation (max-lines=1)");
        var labelOverflow = await ReadInlineStylePropertyAsync(page, "[data-automation-id='parity-text-label-probe']", "textOverflow");
        Assert.Equal("ellipsis", labelOverflow);

        var editorSummaryBefore = await page.Locator("[data-automation-id='parity-text-editor-summary']").InnerTextAsync();
        await SetInputValueAndDispatchAsync(page, "[data-automation-id='parity-text-editor']", "family editor update");
        await page.WaitForFunctionAsync(
            @"(before) => {
                const summary = document.querySelector('[data-automation-id=""parity-text-editor-summary""]');
                return !!summary && (summary.textContent ?? '').trim() !== before;
            }",
            editorSummaryBefore.Trim());

        await page.Locator("[data-automation-id='parity-text-searchbar-input']").FillAsync("browser parity");
        await page.Locator("[data-automation-id='parity-text-searchbar-search-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Search text: browser parity | presses: 1 | last: browser parity");

        await page.Locator("[data-automation-id='parity-text-searchbar-clear-button']").ClickAsync();
        Assert.Equal(string.Empty, await ReadInputValueAsync(page, "[data-automation-id='parity-text-searchbar-input']"));

        await page.Locator("[data-automation-id='parity-text-programmatic-update-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Entry text: programmatic-entry | completed: 1");
        Assert.Equal(
            "programmatic-search",
            await ReadInputValueAsync(page, "[data-automation-id='parity-text-searchbar-input']"));
    }

    [Fact]
    public async Task EntryAndButtonFlowWork()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenDemoAsync(context);

        await page.GetByPlaceholder("Type here...").FillAsync("E2E typing check");
        await page.WaitForSelectorAsync("text=Live text: E2E typing check");

        await page.GetByRole(AriaRole.Button, new() { Name = "Click to increment count" }).ClickAsync();
        await page.WaitForSelectorAsync("text=Button clicks: 1");
    }

    [Fact]
    public async Task ResizeAndVisibilityOpacityFlowWork()
    {
        await using var context = await _fixture.NewContextAsync(width: 1200, height: 900);
        var page = await OpenDemoAsync(context);

        var viewportLabelBefore = await ReadViewportLabelAsync(page);

        await page.SetViewportSizeAsync(900, 620);
        await page.WaitForFunctionAsync(
            "(before) => { const match = Array.from(document.querySelectorAll('*')).find(el => el.textContent?.startsWith('Viewport/layout size:')); return !!match && match.textContent !== before; }",
            viewportLabelBefore);

        var visibilityButton = page.GetByRole(AriaRole.Button, new() { Name = "Toggle probe visibility" });
        await visibilityButton.ClickAsync();
        await page.WaitForSelectorAsync("text=Probe visibility: collapsed");

        await visibilityButton.ClickAsync();
        await page.WaitForSelectorAsync("text=Probe visibility: visible");

        var opacityButton = page.GetByRole(AriaRole.Button, new() { Name = "Cycle probe opacity" });
        await opacityButton.ClickAsync();
        await page.WaitForSelectorAsync("text=Probe opacity: 0.60");
    }
}
