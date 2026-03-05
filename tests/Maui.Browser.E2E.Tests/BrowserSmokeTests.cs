using Microsoft.Playwright;
using Xunit;

namespace Maui.Browser.E2E.Tests;

public sealed class BrowserSmokeTests : IClassFixture<BrowserHostFixture>
{
    private readonly BrowserHostFixture _fixture;

    public BrowserSmokeTests(BrowserHostFixture fixture)
    {
        _fixture = fixture;
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

    [Fact]
    public async Task FormInputBreadthFlowWorks()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenDemoAsync(context);

        var switchControl = page.GetByRole(AriaRole.Switch).First;
        Assert.InRange(await ReadElementWidthAsync(page, "[role='switch']"), 44, 60);

        await switchControl.ClickAsync();
        await page.WaitForSelectorAsync("text=Switch is off");
        Assert.False(await ReadAriaCheckedAsync(page, "[role='switch']"));

        await switchControl.ClickAsync();
        await page.WaitForSelectorAsync("text=Switch is on");
        Assert.True(await ReadAriaCheckedAsync(page, "[role='switch']"));
        await AssertSwitchTrackStylePresentAsync(page);

        var checkBoxControl = page.GetByRole(AriaRole.Checkbox).First;
        await checkBoxControl.ClickAsync();
        await page.WaitForSelectorAsync("text=Checkbox is checked");
        await checkBoxControl.ClickAsync();
        await page.WaitForSelectorAsync("text=Checkbox is unchecked");

        await SetRangeValueAsync(page, 0, 64);
        await page.WaitForSelectorAsync("text=Slider value: 64");

        var pickerControl = page.Locator("select").First;
        await pickerControl.SelectOptionAsync(new[] { new SelectOptionValue { Index = 3 } });
        await page.WaitForSelectorAsync("text=Picker selection: 3 (Delta)");

        await page.GetByRole(AriaRole.Button, new() { Name = "Apply programmatic form update" }).ClickAsync();
        await page.WaitForSelectorAsync("text=Switch is off");
        await page.WaitForSelectorAsync("text=Checkbox is checked");
        await page.WaitForSelectorAsync("text=Slider value: 82");
        await page.WaitForSelectorAsync("text=Picker selection: 2 (Gamma)");

        Assert.False(await ReadAriaCheckedAsync(page, "[role='switch']"));
        Assert.True(await checkBoxControl.IsCheckedAsync());
        Assert.InRange(await ReadInputNumericValueAsync(page, "input[type='range']", 0), 81.9, 82.1);

        var selectedIndex = await page.EvaluateAsync<int>(
            "() => document.querySelector('select')?.selectedIndex ?? -1");
        Assert.Equal(2, selectedIndex);

        await AssertAccentColorPresentAsync(page, "input[type='checkbox']", 0, "checkbox");
        await AssertAccentColorPresentAsync(page, "input[type='range']", 0, "slider");
    }

    [Fact]
    public async Task StressMutationRunReturnsDiagnosticsNearBaseline()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenDemoAsync(context);

        var before = await ReadDiagnosticsAsync(page);

        var stressButton = page.GetByRole(AriaRole.Button, new() { Name = "Run leak stress" });
        for (var i = 1; i <= 3; i++)
        {
            await stressButton.ClickAsync();
            await page.WaitForSelectorAsync($"text=Stress runs: {i}");
        }

        var after = await ReadDiagnosticsAsync(page);

        Assert.True(
            after.ElementHandleCount <= before.ElementHandleCount + 8,
            $"Expected element handles near baseline. Before={before.ElementHandleCount}, After={after.ElementHandleCount}");

        Assert.True(
            after.EventSubscriptionCount <= before.EventSubscriptionCount + 3,
            $"Expected event subscriptions near baseline. Before={before.EventSubscriptionCount}, After={after.EventSubscriptionCount}");

        Assert.True(
            after.ResizeSubscriptionCount <= before.ResizeSubscriptionCount + 1,
            $"Expected resize subscriptions near baseline. Before={before.ResizeSubscriptionCount}, After={after.ResizeSubscriptionCount}");
    }

    private async Task<IPage> OpenDemoAsync(IBrowserContext context)
    {
        if (_fixture.HttpAppUrl is null)
        {
            throw new InvalidOperationException("Fixture app URL is not initialized.");
        }

        var page = await context.NewPageAsync();
        await page.GotoAsync(_fixture.HttpAppUrl.ToString(), new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await page.WaitForSelectorAsync("text=Live text:");
        return page;
    }

    private static async Task<string> ReadViewportLabelAsync(IPage page)
    {
        return await page.EvaluateAsync<string>(
            "() => Array.from(document.querySelectorAll('*')).find(el => el.textContent?.startsWith('Viewport/layout size:'))?.textContent ?? ''");
    }

    private static async Task<DomDiagnosticSnapshot> ReadDiagnosticsAsync(IPage page)
    {
        var values = await page.EvaluateAsync<int[]>(@"async () => {
            const domModule = await import('./_framework/dom.js');
            return domModule.getDiagnostics();
        }");

        return new DomDiagnosticSnapshot(
            values.Length > 0 ? values[0] : 0,
            values.Length > 1 ? values[1] : 0,
            values.Length > 2 ? values[2] : 0);
    }

    private static async Task SetRangeValueAsync(IPage page, int index, double value)
    {
        await page.EvaluateAsync(
            @"({ index, value }) => {
                const ranges = document.querySelectorAll('input[type=""range""]');
                const slider = ranges[index];
                if (!slider) {
                    return;
                }

                slider.value = String(value);
                slider.dispatchEvent(new Event('input', { bubbles: true }));
                slider.dispatchEvent(new Event('change', { bubbles: true }));
            }",
            new { index, value });
    }

    private static async Task<double> ReadInputNumericValueAsync(IPage page, string selector, int index)
    {
        return await page.EvaluateAsync<double>(
            @"({ selector, index }) => {
                const elements = document.querySelectorAll(selector);
                const input = elements[index];
                if (!input) {
                    return Number.NaN;
                }

                return Number.parseFloat(input.value ?? '');
            }",
            new { selector, index });
    }

    private static async Task<double> ReadElementWidthAsync(IPage page, string selector)
    {
        return await page.EvaluateAsync<double>(
            @"(selector) => {
                const element = document.querySelector(selector);
                if (!element) {
                    return Number.NaN;
                }

                return element.getBoundingClientRect().width;
            }",
            selector);
    }

    private static async Task<bool> ReadAriaCheckedAsync(IPage page, string selector)
    {
        return await page.EvaluateAsync<bool>(
            @"(selector) => {
                const element = document.querySelector(selector);
                return element?.getAttribute('aria-checked') === 'true';
            }",
            selector);
    }

    private static async Task AssertSwitchTrackStylePresentAsync(IPage page)
    {
        var trackColor = await page.EvaluateAsync<string>(
            @"() => {
                const switchElement = document.querySelector('[role=""switch""]');
                const track = switchElement?.querySelector('span');
                if (!track) {
                    return '';
                }

                return getComputedStyle(track).backgroundColor ?? '';
            }");

        Assert.False(string.IsNullOrWhiteSpace(trackColor), "Expected switch track to have computed background color.");
    }

    private static async Task AssertAccentColorPresentAsync(IPage page, string selector, int index, string controlKind)
    {
        var accentColor = await page.EvaluateAsync<string>(
            @"({ selector, index }) => {
                const elements = document.querySelectorAll(selector);
                const element = elements[index];
                if (!element) {
                    return '';
                }

                return element.style.getPropertyValue('accent-color') ?? '';
            }",
            new { selector, index });

        Assert.False(
            string.IsNullOrWhiteSpace(accentColor),
            $"Expected {controlKind} to have inline accent-color style.");
    }

    private readonly record struct DomDiagnosticSnapshot(
        int ElementHandleCount,
        int EventSubscriptionCount,
        int ResizeSubscriptionCount);
}
