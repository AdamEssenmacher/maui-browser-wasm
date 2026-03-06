using Xunit;

namespace Maui.Browser.E2E.Tests;

public sealed partial class BrowserSmokeTests
{
    [Fact]
    public async Task PickersDateTimeAndNumericFamilyParityWorks()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenFamilyPageAsync(context, "parity-open-pickers-datetime", "parity-page-pickers-datetime");

        await SetInputValueAndDispatchAsync(page, "[data-automation-id='parity-datetime-date-picker']", "2026-04-18");
        await page.WaitForSelectorAsync("text=Date value: 2026-04-18");

        await SetInputValueAndDispatchAsync(page, "[data-automation-id='parity-datetime-time-picker']", "11:42");
        await page.WaitForSelectorAsync("text=Time value: 11:42");

        await page.Locator("[data-automation-id='parity-datetime-stepper-increment-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Stepper value: 2.5");

        await page.Locator("[data-automation-id='parity-datetime-progress-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Progress value: 0.45");

        await page.Locator("[data-automation-id='parity-datetime-toggle-activity-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Activity running: off");
        Assert.Equal(
            "none",
            await ReadComputedStylePropertyAsync(page, "[data-automation-id='parity-datetime-activity-indicator']", "display"));

        await page.Locator("[data-automation-id='parity-datetime-programmatic-update-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Date value: 2026-12-24");
        await page.WaitForSelectorAsync("text=Time value: 14:45");
        await page.WaitForSelectorAsync("text=Stepper value: 6.5");
        await page.WaitForSelectorAsync("text=Progress value: 0.82");
        await page.WaitForSelectorAsync("text=Activity running: off");

        Assert.Equal(
            "2026-12-24",
            await ReadInputValueAsync(page, "[data-automation-id='parity-datetime-date-picker']"));
        Assert.Equal(
            "14:45",
            await ReadInputValueAsync(page, "[data-automation-id='parity-datetime-time-picker']"));

        Assert.Equal(
            "6.5",
            (await page.Locator("[data-automation-id='parity-datetime-stepper-value-label']").InnerTextAsync()).Trim());
        Assert.Equal(
            "flex",
            await ReadComputedStylePropertyAsync(page, "[data-automation-id='parity-datetime-stepper']", "display"));
        Assert.Equal(
            "8px",
            await ReadComputedStylePropertyAsync(page, "[data-automation-id='parity-datetime-stepper']", "gap"));

        var progressValue = await ReadAttributeAsync(page, "[data-automation-id='parity-datetime-progress-bar']", "value");
        Assert.Equal("0.82", progressValue);
    }
}
