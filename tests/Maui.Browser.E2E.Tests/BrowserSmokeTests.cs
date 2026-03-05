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
        Assert.Equal("16px", rootPaddingTop);
        Assert.Equal("16px", rootPaddingLeft);

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

        var fallbackDisplay = await ReadComputedStylePropertyAsync(page, "[data-automation-id='layout-fallback-grid']", "display");
        Assert.NotEqual("flex", fallbackDisplay);

        await page.GetByRole(AriaRole.Button, new() { Name = "Click to increment count" }).ClickAsync();
        await page.WaitForSelectorAsync("text=Button clicks: 1");
    }

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

        await switchControl.ClickAsync();
        await page.WaitForSelectorAsync("text=Switch is on");
        Assert.True(await ReadAriaCheckedAsync(page, switchSelector));
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

    [Fact]
    public async Task IViewCoreAndTransformFlowWorks()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenDemoAsync(context);

        var hasAutomationIds = await page.EvaluateAsync<bool>(
            @"() => {
                return !!document.querySelector('[data-automation-id=""live-input-entry""]')
                    && !!document.querySelector('[data-automation-id=""core-view-container""]')
                    && !!document.querySelector('[data-automation-id=""core-transform-probe""]');
            }");
        Assert.True(hasAutomationIds);

        var directionBefore = await page.EvaluateAsync<string>(
            @"() => getComputedStyle(document.querySelector('[data-automation-id=""core-view-container""]')).direction");
        await page.GetByRole(AriaRole.Button, new() { Name = "Toggle core flow direction" }).ClickAsync();
        await page.WaitForSelectorAsync("text=Core flow direction: rtl");
        var directionAfter = await page.EvaluateAsync<string>(
            @"() => getComputedStyle(document.querySelector('[data-automation-id=""core-view-container""]')).direction");
        Assert.NotEqual(directionBefore, directionAfter);
        Assert.Equal("rtl", directionAfter);

        await page.GetByRole(AriaRole.Button, new() { Name = "Core input transparency probe" }).ClickAsync();
        await page.WaitForSelectorAsync("text=Core probe clicks: 1");

        await page.GetByRole(AriaRole.Button, new() { Name = "Toggle core input transparency" }).ClickAsync();
        await page.WaitForSelectorAsync("text=Core input transparent: on");

        var pointerEvents = await page.EvaluateAsync<string>(
            @"() => getComputedStyle(document.querySelector('[data-automation-id=""core-input-transparent-probe-button""]')).pointerEvents");
        Assert.Equal("none", pointerEvents);

        var probeButton = page.Locator("[data-automation-id='core-input-transparent-probe-button']");
        var probeBox = await probeButton.BoundingBoxAsync();
        Assert.NotNull(probeBox);
        await page.Mouse.ClickAsync(probeBox!.X + (probeBox.Width / 2), probeBox.Y + (probeBox.Height / 2));
        await page.WaitForTimeoutAsync(200);
        await page.WaitForSelectorAsync("text=Core probe clicks: 1");

        await page.Locator("[data-automation-id='core-focus-button']").ClickAsync();
        await page.WaitForFunctionAsync(
            @"() => document.activeElement?.getAttribute('data-automation-id') === 'core-focus-entry'");

        await page.Locator("[data-automation-id='core-unfocus-button']").ClickAsync();
        await page.WaitForFunctionAsync(
            @"() => document.activeElement?.getAttribute('data-automation-id') !== 'core-focus-entry'");

        await page.GetByRole(AriaRole.Button, new() { Name = "Toggle core transform preset" }).ClickAsync();
        await page.WaitForFunctionAsync(
            @"() => {
                const probe = document.querySelector('[data-automation-id=""core-transform-probe""]');
                return !!probe
                    && probe.style.transform.includes('translate(')
                    && probe.style.transform.includes('rotate(')
                    && probe.style.transform.includes('scale(');
            }");

        var backgroundBefore = await page.EvaluateAsync<string>(
            @"() => getComputedStyle(document.querySelector('[data-automation-id=""core-transform-probe""]')).backgroundColor");
        await page.GetByRole(AriaRole.Button, new() { Name = "Toggle core background" }).ClickAsync();
        await page.WaitForFunctionAsync(
            @"(beforeColor) => {
                const probe = document.querySelector('[data-automation-id=""core-transform-probe""]');
                return !!probe && getComputedStyle(probe).backgroundColor !== beforeColor;
            }",
            backgroundBefore);

        await page.GetByRole(AriaRole.Button, new() { Name = "Update core semantics" }).ClickAsync();
        await page.WaitForSelectorAsync("text=Core semantics version: 2");

        var semanticsValues = await page.EvaluateAsync<string[]>(
            @"() => {
                const probe = document.querySelector('[data-automation-id=""core-transform-probe""]');
                if (!probe) {
                    return ['', '', '', ''];
                }

                return [
                    probe.getAttribute('aria-label') ?? '',
                    probe.getAttribute('aria-description') ?? '',
                    probe.getAttribute('role') ?? '',
                    probe.getAttribute('aria-level') ?? ''
                ];
            }");

        Assert.Contains("v2", semanticsValues[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("v2", semanticsValues[1], StringComparison.OrdinalIgnoreCase);
        Assert.Equal("heading", semanticsValues[2]);
        Assert.Equal("2", semanticsValues[3]);
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

    private static async Task<string> ReadComputedStylePropertyAsync(IPage page, string selector, string propertyName)
    {
        return await page.EvaluateAsync<string>(
            @"({ selector, propertyName }) => {
                const element = document.querySelector(selector);
                if (!element) {
                    return '';
                }

                const styles = getComputedStyle(element);
                return styles[propertyName] ?? '';
            }",
            new { selector, propertyName });
    }

    private static async Task<double[]> ReadBoundingTopsAsync(IPage page, string[] selectors)
    {
        return await page.EvaluateAsync<double[]>(
            @"(selectors) => selectors.map((selector) => {
                const element = document.querySelector(selector);
                if (!element) {
                    return Number.NaN;
                }

                return element.getBoundingClientRect().top;
            })",
            selectors);
    }

    private static async Task<double[]> ReadBoundingLeftsAsync(IPage page, string[] selectors)
    {
        return await page.EvaluateAsync<double[]>(
            @"(selectors) => selectors.map((selector) => {
                const element = document.querySelector(selector);
                if (!element) {
                    return Number.NaN;
                }

                return element.getBoundingClientRect().left;
            })",
            selectors);
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

    private static async Task SetSelectIndexAsync(IPage page, int index, int selectedIndex)
    {
        await page.EvaluateAsync(
            @"({ index, selectedIndex }) => {
                const selects = document.querySelectorAll('select');
                const select = selects[index];
                if (!select) {
                    return;
                }

                select.selectedIndex = selectedIndex;
                select.dispatchEvent(new Event('change', { bubbles: true }));
            }",
            new { index, selectedIndex });
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

    private static async Task<double> ReadElementHeightAsync(IPage page, string selector)
    {
        return await page.EvaluateAsync<double>(
            @"(selector) => {
                const element = document.querySelector(selector);
                if (!element) {
                    return Number.NaN;
                }

                return element.getBoundingClientRect().height;
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

    private static async Task ScrollIntoViewAsync(IPage page, string selector)
    {
        await page.EvaluateAsync(
            @"(selector) => {
                const element = document.querySelector(selector);
                if (!element) {
                    return;
                }

                element.scrollIntoView({ block: 'center', inline: 'center' });
            }",
            selector);
    }

    private static async Task AssertSwitchTrackStylePresentAsync(IPage page)
    {
        var trackColor = await page.EvaluateAsync<string>(
            @"() => {
                const switchElement = document.querySelector('[data-automation-id=""form-switch""]');
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
