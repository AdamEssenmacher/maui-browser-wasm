using Microsoft.Playwright;
using Xunit;

namespace Maui.Browser.E2E.Tests;

public sealed partial class BrowserSmokeTests : IClassFixture<BrowserHostFixture>
{
    private readonly BrowserHostFixture _fixture;

    public BrowserSmokeTests(BrowserHostFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<IPage> OpenDemoAsync(IBrowserContext context)
    {
        var page = await OpenCatalogAsync(context);
        await page.Locator("[data-automation-id='parity-open-consolidated-demo']").ClickAsync();
        await page.WaitForSelectorAsync("text=Live text:");
        return page;
    }

    private async Task<IPage> OpenFamilyPageAsync(IBrowserContext context, string openButtonAutomationId, string expectedPageAutomationId)
    {
        var page = await OpenCatalogAsync(context);
        await page.Locator($"[data-automation-id='{openButtonAutomationId}']").ClickAsync();
        await page.WaitForSelectorAsync($"[data-automation-id='{expectedPageAutomationId}']");
        return page;
    }

    private async Task<IPage> OpenCatalogAsync(IBrowserContext context)
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

        await page.WaitForSelectorAsync("[data-automation-id='parity-page-catalog']");
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
                const directValue = styles[propertyName];
                if (typeof directValue === 'string' && directValue.length > 0) {
                    return directValue;
                }

                const kebabCaseName = propertyName.includes('-')
                    ? propertyName
                    : propertyName.replace(/[A-Z]/g, (match) => `-${match.toLowerCase()}`);
                return styles.getPropertyValue(kebabCaseName) ?? '';
            }",
            new { selector, propertyName });
    }

    private static async Task<string> ReadInlineStylePropertyAsync(IPage page, string selector, string propertyName)
    {
        return await page.EvaluateAsync<string>(
            @"({ selector, propertyName }) => {
                const element = document.querySelector(selector);
                if (!element) {
                    return '';
                }

                const directValue = element.style[propertyName];
                if (typeof directValue === 'string' && directValue.length > 0) {
                    return directValue;
                }

                const kebabCaseName = propertyName.includes('-')
                    ? propertyName
                    : propertyName.replace(/[A-Z]/g, (match) => `-${match.toLowerCase()}`);
                return element.style.getPropertyValue(kebabCaseName) ?? '';
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

    private static async Task<string> ReadInputValueAsync(IPage page, string selector)
    {
        return await page.EvaluateAsync<string>(
            @"(selector) => {
                const element = document.querySelector(selector);
                if (!element) {
                    return '';
                }

                return element.value ?? '';
            }",
            selector);
    }

    private static async Task<string?> ReadAttributeAsync(IPage page, string selector, string attributeName)
    {
        return await page.EvaluateAsync<string?>(
            @"({ selector, attributeName }) => {
                const element = document.querySelector(selector);
                return element?.getAttribute(attributeName);
            }",
            new { selector, attributeName });
    }

    private static async Task SetInputValueAndDispatchAsync(IPage page, string selector, string value)
    {
        await page.EvaluateAsync(
            @"({ selector, value }) => {
                const element = document.querySelector(selector);
                if (!element) {
                    return;
                }

                element.value = value;
                element.dispatchEvent(new Event('input', { bubbles: true }));
                element.dispatchEvent(new Event('change', { bubbles: true }));
            }",
            new { selector, value });
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

    private static async Task<int> ReadElementCountAsync(IPage page, string selector)
    {
        return await page.EvaluateAsync<int>(
            @"(selector) => document.querySelectorAll(selector).length",
            selector);
    }

    private static async Task<string[]> ReadTextContentListAsync(IPage page, string selector)
    {
        return await page.EvaluateAsync<string[]>(
            @"(selector) =>
                Array.from(document.querySelectorAll(selector))
                    .map((element) => (element.textContent ?? '').trim())",
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

    private static async Task<double> ReadSwitchThumbLeftAsync(IPage page, string selector)
    {
        return await page.EvaluateAsync<double>(
            @"(selector) => {
                const switchElement = document.querySelector(selector);
                if (!switchElement) {
                    return Number.NaN;
                }

                const spans = switchElement.querySelectorAll('span');
                if (spans.length < 2) {
                    return Number.NaN;
                }

                const thumb = spans[1];
                const inlineLeft = Number.parseFloat(thumb.style.left ?? '');
                if (Number.isFinite(inlineLeft)) {
                    return inlineLeft;
                }

                const computedLeft = Number.parseFloat(getComputedStyle(thumb).left ?? '');
                return Number.isFinite(computedLeft) ? computedLeft : Number.NaN;
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
