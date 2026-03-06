using Microsoft.Playwright;
using Xunit;

namespace Maui.Browser.E2E.Tests;

public sealed partial class BrowserSmokeTests
{
    [Fact]
    public async Task FormInputBreadthFlowWorks()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenDemoAsync(context);

        const string switchSelector = "[data-automation-id='form-switch']";
        var switchControl = page.Locator(switchSelector).First;
        await ScrollIntoViewAsync(page, switchSelector);
        Assert.InRange(await ReadElementWidthAsync(page, switchSelector), 44, 60);
        Assert.InRange(await ReadElementHeightAsync(page, switchSelector), 24, 40);

        await switchControl.ClickAsync();
        await page.WaitForSelectorAsync("text=Switch is off");
        Assert.False(await ReadAriaCheckedAsync(page, switchSelector));
        var switchThumbLeftOff = await ReadSwitchThumbLeftAsync(page, switchSelector);

        await switchControl.ClickAsync();
        await page.WaitForSelectorAsync("text=Switch is on");
        Assert.True(await ReadAriaCheckedAsync(page, switchSelector));
        var switchThumbLeftOn = await ReadSwitchThumbLeftAsync(page, switchSelector);
        Assert.True(switchThumbLeftOn > switchThumbLeftOff + 8, "Expected switch thumb to visibly move when toggled on.");
        await AssertSwitchTrackStylePresentAsync(page);

        const string checkBoxSelector = "[data-automation-id='form-checkbox']";
        var checkBoxControl = page.Locator(checkBoxSelector).First;
        await ScrollIntoViewAsync(page, checkBoxSelector);
        await checkBoxControl.ClickAsync();
        await page.WaitForSelectorAsync("text=Checkbox is checked");
        await checkBoxControl.ClickAsync();
        await page.WaitForSelectorAsync("text=Checkbox is unchecked");

        await SetRangeValueAsync(page, 0, 64.3756);
        await page.WaitForSelectorAsync("text=Slider value: 64.38");
        Assert.InRange(await ReadInputNumericValueAsync(page, "input[type='range']", 0), 64.3755, 64.3757);

        var pickerControl = page.Locator("select").First;
        await page.WaitForFunctionAsync(
            @"() => {
                const select = document.querySelector('select');
                if (!select) {
                    return false;
                }

                const first = select.options[0];
                return !!first && first.text === 'Pick an item' && first.disabled === true;
            }");
        await pickerControl.SelectOptionAsync(new[] { new SelectOptionValue { Index = 4 } });
        await page.WaitForSelectorAsync("text=Picker selection: 3 (Delta)");

        await SetSelectIndexAsync(page, 0, 0);
        await page.WaitForSelectorAsync("text=Picker selection: -1 (<none>)");

        await page.GetByRole(AriaRole.Button, new() { Name = "Apply programmatic form update" }).ClickAsync();
        await page.WaitForSelectorAsync("text=Switch is off");
        await page.WaitForSelectorAsync("text=Checkbox is checked");
        await page.WaitForSelectorAsync("text=Slider value: 82");
        await page.WaitForSelectorAsync("text=Picker selection: 2 (Gamma)");

        Assert.False(await ReadAriaCheckedAsync(page, switchSelector));
        Assert.True(await checkBoxControl.IsCheckedAsync());
        Assert.InRange(await ReadInputNumericValueAsync(page, "input[type='range']", 0), 81.9, 82.1);

        var selectedIndex = await page.EvaluateAsync<int>(
            "() => document.querySelector('select')?.selectedIndex ?? -1");
        Assert.Equal(3, selectedIndex);

        await AssertAccentColorPresentAsync(page, "input[type='checkbox']", 0, "checkbox");
        await AssertAccentColorPresentAsync(page, "input[type='range']", 0, "slider");
    }
}
