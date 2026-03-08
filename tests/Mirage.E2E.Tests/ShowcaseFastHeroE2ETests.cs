using Microsoft.Playwright;
using Xunit;

namespace Mirage.E2E.Tests;

public sealed class ShowcaseFastHeroE2ETests : IClassFixture<ShowcaseHostFixture>
{
    private readonly ShowcaseHostFixture _fixture;

    public ShowcaseFastHeroE2ETests(ShowcaseHostFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task HeroStructureRendersExpectedMessaging()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        var headline = await page.Locator("[data-automation-id='hero-headline']").InnerTextAsync();
        var pipeline = await page.Locator("[data-automation-id='architecture-pipeline']").InnerTextAsync();
        var contrast = await page.Locator("[data-automation-id='hero-why-contrast']").InnerTextAsync();
        var runtimeBadge = await page.Locator("[data-automation-id='showcase-runtime-badge']").InnerTextAsync();
        var scrollCue = await page.Locator("[data-automation-id='hero-scroll-cue']").InnerTextAsync();
        var hasDemoSectionId = await page.Locator("[data-automation-id='demo']").CountAsync();
        var hasArchitectureSectionId = await page.Locator("[data-automation-id='architecture']").CountAsync();
        var hasProvidersSectionId = await page.Locator("[data-automation-id='providers']").CountAsync();
        var automationIds = await page.EvaluateAsync<string[]>(
            @"() => Array.from(document.querySelectorAll('[data-automation-id]'))
                .map((node) => node.getAttribute('data-automation-id') ?? '')
                .filter((id) => id.length > 0)");

        Assert.Contains("real DOM UI", headline, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DOM + Web Components", pipeline, StringComparison.Ordinal);
        Assert.Contains("canvas-based", contrast, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FAST provider", runtimeBadge, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("How Mirage Works", scrollCue, StringComparison.OrdinalIgnoreCase);
        Assert.True(hasDemoSectionId > 0, $"Expected demo section id. IDs: {string.Join(',', automationIds)}");
        Assert.True(hasArchitectureSectionId > 0, $"Expected architecture section id. IDs: {string.Join(',', automationIds)}");
        Assert.True(hasProvidersSectionId > 0, $"Expected providers section id. IDs: {string.Join(',', automationIds)}");
    }

    [Fact]
    public async Task LiveDemoWidgetBindsInputAndCommand()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        await SetFastTextFieldValueAsync(page, "[data-automation-id='showcase-demo-entry']", "Sam");
        await page.WaitForTimeoutAsync(140);
        var greeting = await page.Locator("[data-automation-id='showcase-demo-greeting']").InnerTextAsync();
        Assert.Contains("Hello, Sam", greeting, StringComparison.Ordinal);

        await page.Locator("[data-automation-id='showcase-demo-button']").ClickAsync();
        await page.WaitForTimeoutAsync(140);
        var countText = await page.Locator("[data-automation-id='showcase-demo-count']").InnerTextAsync();
        Assert.Contains("Click count: 1", countText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FoundationsLab_LabelInputUpdatesBoundOutput()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        await SetFastTextFieldValueAsync(page, "[data-automation-id='foundations-label-input']", "Foundations state");
        await page.WaitForTimeoutAsync(120);
        var text = await page.Locator("[data-automation-id='foundations-label-output']").InnerTextAsync();
        Assert.Contains("Foundations state", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FoundationsLab_VisibilityToggleSwitchesDisplay()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        await page.Locator("[data-automation-id='foundations-visibility-toggle']").ClickAsync();
        await page.WaitForTimeoutAsync(120);
        var hiddenDisplay = await ReadCssPropertyAsync(page, "[data-automation-id='foundations-visibility-target']", "display");
        Assert.Equal("none", hiddenDisplay);

        await page.Locator("[data-automation-id='foundations-visibility-toggle']").ClickAsync();
        await page.WaitForTimeoutAsync(120);
        var shownDisplay = await ReadCssPropertyAsync(page, "[data-automation-id='foundations-visibility-target']", "display");
        Assert.NotEqual("none", shownDisplay);
    }

    [Fact]
    public async Task FoundationsLab_MarginToggleChangesComputedMargins()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        var initialTop = await ReadCssPropertyAsync(page, "[data-automation-id='foundations-margin-target']", "margin-top");
        var initialBottom = await ReadCssPropertyAsync(page, "[data-automation-id='foundations-margin-target']", "margin-bottom");

        await page.Locator("[data-automation-id='foundations-margin-toggle']").ClickAsync();
        await page.WaitForTimeoutAsync(120);

        var toggledTop = await ReadCssPropertyAsync(page, "[data-automation-id='foundations-margin-target']", "margin-top");
        var toggledBottom = await ReadCssPropertyAsync(page, "[data-automation-id='foundations-margin-target']", "margin-bottom");

        Assert.NotEqual(initialTop, toggledTop);
        Assert.NotEqual(initialBottom, toggledBottom);
        Assert.EndsWith("px", toggledTop, StringComparison.Ordinal);
        Assert.EndsWith("px", toggledBottom, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FoundationsLab_VerticalStackSpacingToggleChangesGeometry()
    {
        await using var context = await _fixture.NewContextAsync(width: 1180, height: 800);
        var page = await OpenAsync(context);

        var initialGap = await ReadTopDeltaAsync(
            page,
            "[data-automation-id='foundations-stack-probe-a']",
            "[data-automation-id='foundations-stack-probe-b']");

        await page.Locator("[data-automation-id='foundations-stack-spacing-toggle']").ClickAsync();
        await page.WaitForTimeoutAsync(150);

        var toggledGap = await ReadTopDeltaAsync(
            page,
            "[data-automation-id='foundations-stack-probe-a']",
            "[data-automation-id='foundations-stack-probe-b']");

        Assert.True(toggledGap > initialGap, $"Expected spacing gap to grow. Initial={initialGap}, Toggled={toggledGap}");
    }

    [Fact]
    public async Task FoundationsLab_HorizontalStackSpacingToggleChangesGeometry()
    {
        await using var context = await _fixture.NewContextAsync(width: 1180, height: 800);
        var page = await OpenAsync(context);

        var initialGap = await ReadLeftDeltaAsync(
            page,
            "[data-automation-id='foundations-horizontal-probe-a']",
            "[data-automation-id='foundations-horizontal-probe-b']");

        await page.Locator("[data-automation-id='foundations-horizontal-spacing-toggle']").ClickAsync();
        await page.WaitForTimeoutAsync(150);

        var toggledGap = await ReadLeftDeltaAsync(
            page,
            "[data-automation-id='foundations-horizontal-probe-a']",
            "[data-automation-id='foundations-horizontal-probe-b']");

        Assert.True(toggledGap > initialGap, $"Expected horizontal spacing to grow. Initial={initialGap}, Toggled={toggledGap}");
    }

    [Fact]
    public async Task FoundationsLab_GridPlacementAndSpanAreRendered()
    {
        await using var context = await _fixture.NewContextAsync(width: 1180, height: 800);
        var page = await OpenAsync(context);

        var r0c0Left = await ReadRectValueAsync(page, "[data-automation-id='foundations-grid-cell-r0c0']", "left");
        var r0c1Left = await ReadRectValueAsync(page, "[data-automation-id='foundations-grid-cell-r0c1']", "left");
        var r1c2Top = await ReadRectValueAsync(page, "[data-automation-id='foundations-grid-cell-r1c2']", "top");
        var r0c2Top = await ReadRectValueAsync(page, "[data-automation-id='foundations-grid-cell-r0c2']", "top");
        var spanWidth = await ReadRectValueAsync(page, "[data-automation-id='foundations-grid-cell-span']", "width");
        var c0Width = await ReadRectValueAsync(page, "[data-automation-id='foundations-grid-cell-r0c0']", "width");

        Assert.True(r0c1Left > r0c0Left, "Expected r0c1 to be positioned to the right of r0c0.");
        Assert.True(r1c2Top > r0c2Top, "Expected second-row cell to be below first-row cell.");
        Assert.True(spanWidth > c0Width, "Expected span cell width to exceed single-column cell width.");
    }

    [Fact]
    public async Task FoundationsLab_BackgroundAndVisualSizingAreApplied()
    {
        await using var context = await _fixture.NewContextAsync(width: 1180, height: 800);
        var page = await OpenAsync(context);

        var solidBg = await ReadCssPropertyAsync(page, "[data-automation-id='foundations-bg-solid']", "background-color");
        var linearBgImage = await ReadCssPropertyAsync(page, "[data-automation-id='foundations-bg-linear']", "background-image");
        var opacity = await ReadCssPropertyAsync(page, "[data-automation-id='foundations-opacity-target']", "opacity");
        var sizeTargetWidth = await ReadRectValueAsync(page, "[data-automation-id='foundations-size-target']", "width");

        Assert.NotEqual("rgba(0, 0, 0, 0)", solidBg);
        Assert.NotEqual("none", linearBgImage);
        Assert.Equal("0.58", opacity);
        Assert.InRange(sizeTargetWidth, 160, 260);
    }

    [Fact]
    public async Task NavAndCtaButtonsSmoothScrollToSections()
    {
        await using var context = await _fixture.NewContextAsync(width: 1180, height: 760);
        var page = await OpenAsync(context);

        await page.WaitForSelectorAsync("[data-automation-id='providers']");
        await page.WaitForSelectorAsync("[data-automation-id='architecture']");
        await page.WaitForSelectorAsync("[data-automation-id='demo']");

        await page.Locator("[data-automation-id='showcase-nav-providers']").ClickAsync();
        await page.WaitForTimeoutAsync(700);
        var providersTop = await ReadTopAsync(page, "[data-automation-id='providers']");
        Assert.True(providersTop < 320, $"Expected providers section near top after nav scroll, got {providersTop}.");

        await page.Locator("[data-automation-id='hero-cta-architecture']").ClickAsync();
        await page.WaitForTimeoutAsync(700);
        var architectureTop = await ReadTopAsync(page, "[data-automation-id='architecture']");
        Assert.True(architectureTop < 320, $"Expected architecture section near top after CTA scroll, got {architectureTop}.");

        await page.Locator("[data-automation-id='hero-cta-demo']").ClickAsync();
        await page.WaitForTimeoutAsync(700);
        var demoTop = await ReadTopAsync(page, "[data-automation-id='demo']");
        Assert.True(demoTop < 320, $"Expected demo section near top after CTA scroll, got {demoTop}.");
    }

    [Fact]
    public async Task HeroRendersFastDomElementsAndNoCanvasSurface()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        var fastButtonCount = await page.Locator("fast-button").CountAsync();
        var fastTextFieldCount = await page.Locator("fast-text-field").CountAsync();
        var genericElementCount = await page.Locator("vendor-pill").CountAsync();
        var canvasCount = await page.Locator("canvas").CountAsync();

        Assert.True(fastButtonCount > 0, "Expected FAST buttons in the hero page DOM.");
        Assert.True(fastTextFieldCount > 0, "Expected FAST text fields in the hero page DOM.");
        Assert.True(genericElementCount > 0, "Expected generic custom element (vendor-pill) in the hero page DOM.");
        Assert.Equal(0, canvasCount);
    }

    [Fact]
    public async Task GenericElementNodeIsInteractiveAndInspectable()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        await page.Locator("[data-automation-id='showcase-generic-widget']").ClickAsync();
        await page.WaitForTimeoutAsync(120);
        var counter = await page.Locator("[data-automation-id='showcase-generic-widget-count']").InnerTextAsync();

        Assert.Contains("Generic widget clicks: 1", counter, StringComparison.Ordinal);
        var nodeType = await page.Locator("[data-automation-id='showcase-generic-widget']").GetAttributeAsync("data-mirage-node-type");
        Assert.Equal("element", nodeType);
        var elementName = await page.Locator("[data-automation-id='showcase-generic-widget']").GetAttributeAsync("data-mirage-element-name");
        Assert.Equal("vendor-pill", elementName);
    }

    [Fact]
    public async Task BuiltInHeroControlsRemainTypedNodes()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        var buttonNodeType = await page.Locator("[data-automation-id='showcase-demo-button']").GetAttributeAsync("data-mirage-node-type");
        var entryNodeType = await page.Locator("[data-automation-id='showcase-demo-entry']").GetAttributeAsync("data-mirage-node-type");
        var labelNodeType = await page.Locator("[data-automation-id='showcase-demo-greeting']").GetAttributeAsync("data-mirage-node-type");
        var containerNodeType = await page.Locator("[data-automation-id='demo']").GetAttributeAsync("data-mirage-node-type");

        Assert.Equal("typed", buttonNodeType);
        Assert.Equal("typed", entryNodeType);
        Assert.Equal("typed", labelNodeType);
        Assert.Equal("typed", containerNodeType);
    }

    private async Task<IPage> OpenAsync(IBrowserContext context)
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

        await page.WaitForSelectorAsync("[data-automation-id='hero-headline']");
        return page;
    }

    private static async Task<double> ReadTopAsync(IPage page, string selector)
    {
        return await page.EvaluateAsync<double>(
            @"(selector) => {
                const element = document.querySelector(selector);
                if (!element) {
                    return Number.POSITIVE_INFINITY;
                }

                return element.getBoundingClientRect().top;
            }",
            selector);
    }

    private static async Task<string> ReadCssPropertyAsync(IPage page, string selector, string propertyName)
    {
        return await page.EvaluateAsync<string>(
            @"({ selector, propertyName }) => {
                const element = document.querySelector(selector);
                if (!element) {
                    return '';
                }

                return window.getComputedStyle(element).getPropertyValue(propertyName).trim();
            }",
            new { selector, propertyName });
    }

    private static async Task<double> ReadTopDeltaAsync(IPage page, string firstSelector, string secondSelector)
    {
        return await page.EvaluateAsync<double>(
            @"({ firstSelector, secondSelector }) => {
                const first = document.querySelector(firstSelector);
                const second = document.querySelector(secondSelector);
                if (!first || !second) {
                    return Number.NaN;
                }

                return second.getBoundingClientRect().top - first.getBoundingClientRect().top;
            }",
            new { firstSelector, secondSelector });
    }

    private static async Task<double> ReadLeftDeltaAsync(IPage page, string firstSelector, string secondSelector)
    {
        return await page.EvaluateAsync<double>(
            @"({ firstSelector, secondSelector }) => {
                const first = document.querySelector(firstSelector);
                const second = document.querySelector(secondSelector);
                if (!first || !second) {
                    return Number.NaN;
                }

                return second.getBoundingClientRect().left - first.getBoundingClientRect().left;
            }",
            new { firstSelector, secondSelector });
    }

    private static async Task<double> ReadRectValueAsync(IPage page, string selector, string key)
    {
        return await page.EvaluateAsync<double>(
            @"({ selector, key }) => {
                const element = document.querySelector(selector);
                if (!element) {
                    return Number.NaN;
                }

                const rect = element.getBoundingClientRect();
                return key === 'left'
                    ? rect.left
                    : key === 'top'
                        ? rect.top
                        : key === 'width'
                            ? rect.width
                            : key === 'height'
                                ? rect.height
                                : Number.NaN;
            }",
            new { selector, key });
    }

    private static async Task SetFastTextFieldValueAsync(IPage page, string selector, string value)
    {
        var applied = await page.EvaluateAsync<bool>(
            @"({ selector, value }) => {
                const element = document.querySelector(selector);
                if (!element) {
                    return false;
                }

                element.value = value ?? '';
                let inputEvent;
                try {
                    inputEvent = new InputEvent('input', { bubbles: true, composed: true });
                } catch {
                    inputEvent = new Event('input', { bubbles: true, composed: true });
                }

                element.dispatchEvent(inputEvent);
                return true;
            }",
            new { selector, value });

        Assert.True(applied, $"Failed to set FAST text field value for selector '{selector}'.");
    }
}
