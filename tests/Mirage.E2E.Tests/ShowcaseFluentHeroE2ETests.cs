using System.Collections.Concurrent;
using System.Text;
using Microsoft.Playwright;
using Xunit;

namespace Mirage.E2E.Tests;

public sealed class ShowcaseFluentHeroE2ETests : IClassFixture<ShowcaseHostFixture>
{
    private readonly ShowcaseHostFixture _fixture;

    public ShowcaseFluentHeroE2ETests(ShowcaseHostFixture fixture)
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
        Assert.Contains("fluent provider", runtimeBadge, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("How Mirage Works", scrollCue, StringComparison.OrdinalIgnoreCase);
        Assert.True(hasDemoSectionId > 0, $"Expected demo section id. IDs: {string.Join(',', automationIds)}");
        Assert.True(hasArchitectureSectionId > 0, $"Expected architecture section id. IDs: {string.Join(',', automationIds)}");
        Assert.True(hasProvidersSectionId > 0, $"Expected providers section id. IDs: {string.Join(',', automationIds)}");
    }

    [Fact]
    public async Task HeroStyles_AreProjectedFromMauiResourceStyles()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        var heroBackground = await ReadCssPropertyAsync(page, "[data-automation-id='hero']", "background-color");
        var headlineColor = await ReadCssPropertyAsync(page, "[data-automation-id='hero-headline']", "color");
        var subheadlineColor = await ReadCssPropertyAsync(page, "[data-automation-id='hero-subheadline']", "color");
        var ctaBorderRadius = await ReadCssPropertyAsync(page, "[data-automation-id='hero-cta-demo']", "border-radius");
        var ctaBorderWidth = await ReadCssPropertyAsync(page, "[data-automation-id='hero-cta-demo']", "border-top-width");

        Assert.NotEqual("rgba(0, 0, 0, 0)", heroBackground);
        Assert.Contains("15, 23, 42", headlineColor, StringComparison.Ordinal);
        Assert.Contains("51, 65, 85", subheadlineColor, StringComparison.Ordinal);
        Assert.Equal("12px", ctaBorderRadius);
        Assert.Equal("1px", ctaBorderWidth);
    }

    [Fact]
    public async Task LiveDemoWidgetBindsInputAndCommand()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        await SetFluentTextFieldValueAsync(page, "[data-automation-id='showcase-demo-entry']", "Sam");
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

        await SetFluentTextFieldValueAsync(page, "[data-automation-id='foundations-label-input']", "Foundations state");
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
    public async Task HeroRendersFluentDomElementsAndNoCanvasSurface()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        var fluentButtonCount = await page.Locator("fluent-button").CountAsync();
        var fluentTextFieldCount = await page.Locator("fluent-text-field").CountAsync();
        var genericElementCount = await page.Locator("vendor-pill").CountAsync();
        var canvasCount = await page.Locator("canvas").CountAsync();

        Assert.True(fluentButtonCount > 0, "Expected Fluent buttons in the hero page DOM.");
        Assert.True(fluentTextFieldCount > 0, "Expected Fluent text fields in the hero page DOM.");
        Assert.True(genericElementCount > 0, "Expected generic custom element (vendor-pill) in the hero page DOM.");
        Assert.Equal(0, canvasCount);
    }

    [Fact]
    public async Task GenericWebNodeIsInteractiveAndInspectable()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        await page.Locator("[data-automation-id='showcase-generic-widget']").ClickAsync();
        await page.WaitForTimeoutAsync(120);
        var counter = await page.Locator("[data-automation-id='showcase-generic-widget-count']").InnerTextAsync();

        Assert.Contains("Generic widget clicks: 1", counter, StringComparison.Ordinal);
        var nodeType = await page.Locator("[data-automation-id='showcase-generic-widget']").GetAttributeAsync("data-mirage-node-type");
        Assert.Equal("web", nodeType);
        var elementName = await page.Locator("[data-automation-id='showcase-generic-widget']").GetAttributeAsync("data-mirage-element-name");
        Assert.Equal("vendor-pill", elementName);
    }

    [Fact]
    public async Task BuiltInHeroControlsRemainControlNodes()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        var buttonNodeType = await page.Locator("[data-automation-id='showcase-demo-button']").GetAttributeAsync("data-mirage-node-type");
        var entryNodeType = await page.Locator("[data-automation-id='showcase-demo-entry']").GetAttributeAsync("data-mirage-node-type");
        var labelNodeType = await page.Locator("[data-automation-id='showcase-demo-greeting']").GetAttributeAsync("data-mirage-node-type");
        var containerNodeType = await page.Locator("[data-automation-id='demo']").GetAttributeAsync("data-mirage-node-type");

        Assert.Equal("control", buttonNodeType);
        Assert.Equal("control", entryNodeType);
        Assert.Equal("control", labelNodeType);
        Assert.Equal("control", containerNodeType);
    }

    [Fact]
    public async Task BacklogTable_RendersGridStructure()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        await page.WaitForSelectorAsync("[data-automation-id='showcase-backlog-table-host']");
        await page.WaitForSelectorAsync("[data-automation-id='showcase-backlog-table']");
        var display = await ReadCssPropertyAsync(page, "[data-automation-id='showcase-backlog-table']", "display");
        Assert.Equal("grid", display);
    }

    [Fact]
    public async Task BacklogTable_ContainsExpectedRows()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        Assert.True(await page.Locator("[data-automation-id='showcase-backlog-row-label']").CountAsync() > 0);
        Assert.True(await page.Locator("[data-automation-id='showcase-backlog-row-content-view']").CountAsync() > 0);
        Assert.True(await page.Locator("[data-automation-id='showcase-backlog-row-check-box']").CountAsync() > 0);
        Assert.True(await page.Locator("[data-automation-id='showcase-backlog-row-grid']").CountAsync() > 0);
        Assert.True(await page.Locator("[data-automation-id='showcase-backlog-row-horizontal-stack-layout']").CountAsync() > 0);
        Assert.True(await page.Locator("[data-automation-id='showcase-backlog-row-web-view']").CountAsync() > 0);
        Assert.True(await page.Locator("[data-automation-id='showcase-backlog-row-element']").CountAsync() > 0);
        Assert.True(await page.Locator("[data-automation-id='showcase-backlog-row-image-element']").CountAsync() > 0);
    }

    [Fact]
    public async Task BacklogTable_StatusLabelsMatchPolicy()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        var complete = await page.Locator("[data-automation-id='showcase-backlog-cell-button-status-complete']").InnerTextAsync();
        var started = await page.Locator("[data-automation-id='showcase-backlog-cell-label-status-started']").InnerTextAsync();
        var notStarted = await page.Locator("[data-automation-id='showcase-backlog-cell-web-view-status-not-started']").InnerTextAsync();

        Assert.Equal("Fully Implemented (Complete)", complete);
        Assert.Equal("Partially Implemented (Started)", started);
        Assert.Equal("Not Implemented (Not Started)", notStarted);
    }

    [Fact]
    public async Task BacklogTable_HasMinimumRowCount()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        var rows = await page.Locator("[data-automation-id^='showcase-backlog-row-']").CountAsync();
        Assert.Equal(40, rows);
    }

    [Fact]
    public async Task LayoutLab_RendersAllGroupsAndScenarioCardCount()
    {
        await using var context = await _fixture.NewContextAsync(width: 1400, height: 960);
        var page = await OpenAsync(context);

        await page.Locator("[data-automation-id='showcase-nav-layout-lab']").ClickAsync();
        await page.WaitForTimeoutAsync(700);
        var sectionTop = await ReadTopAsync(page, "[data-automation-id='layout-lab']");
        Assert.True(sectionTop < 320, $"Expected layout-lab section near top after nav click, got {sectionTop}.");

        var groupCount = await page.Locator(
            "[data-automation-id='layout-lab-group-content-view'],"
            + "[data-automation-id='layout-lab-group-vertical-stack'],"
            + "[data-automation-id='layout-lab-group-horizontal-stack'],"
            + "[data-automation-id='layout-lab-group-grid'],"
            + "[data-automation-id='layout-lab-group-nested']").CountAsync();
        var cardCount = await page.Locator("[data-automation-id^='layout-lab-card-']").CountAsync();
        var expectedCount = await page.Locator("[data-automation-id^='layout-lab-expected-']").CountAsync();
        var canvasCount = await page.Locator("canvas").CountAsync();

        Assert.Equal(5, groupCount);
        Assert.Equal(25, cardCount);
        Assert.Equal(25, expectedCount);
        Assert.Equal(0, canvasCount);
    }

    [Fact]
    public async Task ContentPage_DefaultSurface_IsNotDocumentScrollable()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 900);
        var page = await OpenAsync(context);

        var rootOverflowY = await ReadCssPropertyAsync(page, "[data-automation-id='showcase-root']", "overflow-y");
        var scrollRootOverflowY = await ReadCssPropertyAsync(page, "[data-automation-id='showcase-scroll-root']", "overflow-y");
        var metrics = await ReadScrollGeometryAsync(page, "[data-automation-id='showcase-scroll-root']");
        var docMetrics = await ReadDocumentScrollGeometryAsync(page);

        Assert.Equal("hidden", rootOverflowY);
        Assert.True(scrollRootOverflowY is "auto" or "scroll");
        Assert.True(metrics.ScrollHeight > metrics.ClientHeight + 120, $"Expected explicit ScrollView overflow. Height={metrics.ScrollHeight}, Client={metrics.ClientHeight}");
        Assert.True(docMetrics.DocumentScrollHeight <= docMetrics.DocumentClientHeight + 2, $"Expected document to remain non-scrollable. ScrollHeight={docMetrics.DocumentScrollHeight}, ClientHeight={docMetrics.DocumentClientHeight}");
        Assert.True(docMetrics.BodyScrollHeight <= docMetrics.BodyClientHeight + 2, $"Expected body to remain non-scrollable. ScrollHeight={docMetrics.BodyScrollHeight}, ClientHeight={docMetrics.BodyClientHeight}");

        await page.EvaluateAsync("() => window.scrollTo(0, 2000)");
        await page.WaitForTimeoutAsync(120);
        var windowScrollY = await ReadWindowScrollYAsync(page);
        Assert.True(windowScrollY <= 1, $"Expected window scroll position to remain near zero. Actual={windowScrollY}");
    }

    [Fact]
    public async Task Showcase_ExplicitRootScrollView_IsScrollable()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 900);
        var page = await OpenAsync(context);

        await SetElementScrollTopAsync(page, "[data-automation-id='showcase-scroll-root']", 0);
        var initial = await ReadScrollGeometryAsync(page, "[data-automation-id='showcase-scroll-root']");
        Assert.True(initial.ScrollHeight > initial.ClientHeight + 120, $"Expected scrollable root geometry. Height={initial.ScrollHeight}, Client={initial.ClientHeight}");

        await SetElementScrollTopAsync(page, "[data-automation-id='showcase-scroll-root']", 700);
        await page.WaitForTimeoutAsync(120);

        var scrolled = await ReadScrollGeometryAsync(page, "[data-automation-id='showcase-scroll-root']");
        Assert.True(scrolled.ScrollTop > 120, $"Expected scroll root top to change. Actual={scrolled.ScrollTop}");
    }

    [Fact]
    public async Task Showcase_NavScroll_UsesExplicitScrollSurface()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 900);
        var page = await OpenAsync(context);

        await SetElementScrollTopAsync(page, "[data-automation-id='showcase-scroll-root']", 0);
        await page.Locator("[data-automation-id='showcase-nav-layout-lab']").ClickAsync();
        await page.WaitForFunctionAsync(
            @"() => {
                const root = document.querySelector('[data-automation-id=""showcase-scroll-root""]');
                return !!root && root.scrollTop > 120;
            }");

        var scrolled = await ReadScrollGeometryAsync(page, "[data-automation-id='showcase-scroll-root']");
        var windowScrollY = await ReadWindowScrollYAsync(page);
        Assert.True(scrolled.ScrollTop > 120, $"Expected nav click to scroll explicit root. Actual={scrolled.ScrollTop}");
        Assert.True(windowScrollY <= 1, $"Expected document scroll to remain near zero. Actual={windowScrollY}");
    }

    [Fact]
    public async Task Showcase_RequestScrollTo_CommandPath_IsInvoked()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 900);
        var page = await OpenAsync(context);

        await SetElementScrollTopAsync(page, "[data-automation-id='showcase-scroll-root']", 0);
        await page.Locator("[data-automation-id='layout-lab-scroll-command-top']").ScrollIntoViewIfNeededAsync();
        var before = await ReadScrollGeometryAsync(page, "[data-automation-id='showcase-scroll-root']");
        Assert.True(before.ScrollTop > 120, $"Expected command probe to require scrolling first. Actual={before.ScrollTop}");
        await page.Locator("[data-automation-id='layout-lab-scroll-command-top']").ClickAsync();
        await page.WaitForFunctionAsync(
            @"() => {
                const counter = document.querySelector('[data-automation-id=""layout-lab-scroll-command-count""]');
                return !!counter && (counter.textContent ?? '').includes('1');
            }");

        var after = await ReadScrollGeometryAsync(page, "[data-automation-id='showcase-scroll-root']");
        var afterCounter = await page.Locator("[data-automation-id='layout-lab-scroll-command-count']").InnerTextAsync();
        var windowScrollY = await ReadWindowScrollYAsync(page);

        Assert.Contains("1", afterCounter, StringComparison.Ordinal);
        Assert.True(after.ScrollTop <= before.ScrollTop + 5, $"Expected command invocation to avoid downward root scroll drift. Before={before.ScrollTop}, After={after.ScrollTop}");
        Assert.True(windowScrollY <= 1, $"Expected document scroll to remain near zero. Actual={windowScrollY}");
    }

    [Fact]
    public async Task LayoutLab_ContentViewScenarios_ShowCenteredAndStretchBehavior()
    {
        await using var context = await _fixture.NewContextAsync(width: 1400, height: 960);
        var page = await OpenAsync(context);

        await page.Locator("[data-automation-id='showcase-nav-layout-lab']").ClickAsync();
        await page.WaitForTimeoutAsync(700);

        var parentLeft = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-parent-content-view-fixed-centered']", "left");
        var parentTop = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-parent-content-view-fixed-centered']", "top");
        var parentWidth = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-parent-content-view-fixed-centered']", "width");
        var parentHeight = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-parent-content-view-fixed-centered']", "height");

        var childLeft = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-child-content-view-fixed-centered-primary']", "left");
        var childTop = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-child-content-view-fixed-centered-primary']", "top");
        var childWidth = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-child-content-view-fixed-centered-primary']", "width");
        var childHeight = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-child-content-view-fixed-centered-primary']", "height");

        var parentCenterX = parentLeft + (parentWidth / 2.0);
        var parentCenterY = parentTop + (parentHeight / 2.0);
        var childCenterX = childLeft + (childWidth / 2.0);
        var childCenterY = childTop + (childHeight / 2.0);
        var centerYTolerance = Math.Max(6, childHeight * 0.35);

        Assert.True(Math.Abs(parentCenterX - childCenterX) <= 3.5, $"Expected centered X alignment. Parent={parentCenterX}, Child={childCenterX}");
        Assert.True(Math.Abs(parentCenterY - childCenterY) <= centerYTolerance, $"Expected centered Y alignment. Parent={parentCenterY}, Child={childCenterY}, Tolerance={centerYTolerance}");

        var stretchWidth = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-child-content-view-fixed-stretch-primary']", "width");
        var stretchHeight = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-child-content-view-fixed-stretch-primary']", "height");
        Assert.True(stretchWidth >= 260, $"Expected stretched child width. Got {stretchWidth}");
        Assert.True(stretchHeight >= 120, $"Expected stretched child height. Got {stretchHeight}");
    }

    [Fact]
    public async Task LayoutLab_ContentViewExplicitSizeScenario_PreservesChildMinHeightBehavior()
    {
        await using var context = await _fixture.NewContextAsync(width: 1400, height: 960);
        var page = await OpenAsync(context);

        await page.Locator("[data-automation-id='showcase-nav-layout-lab']").ClickAsync();
        await page.WaitForTimeoutAsync(700);

        var minHeight = await ReadCssPropertyAsync(page, "[data-automation-id='layout-lab-child-content-view-explicit-size-request-primary']", "min-height");
        Assert.EndsWith("px", minHeight, StringComparison.Ordinal);
        Assert.NotEqual("0px", minHeight);

        var minHeightValue = ParsePx(minHeight);
        Assert.True(minHeightValue >= 30, $"Expected handler-driven non-zero min-height for content child. Actual={minHeight}");
    }

    [Fact]
    public async Task ShowcaseRootContentPageChild_IsNotForcedToZeroMinHeight()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 900);
        var page = await OpenAsync(context);

        var minHeight = await ReadCssPropertyAsync(page, "[data-automation-id='showcase-page-grid']", "min-height");
        Assert.NotEqual("0px", minHeight);

        if (minHeight.EndsWith("px", StringComparison.Ordinal))
        {
            var minHeightValue = ParsePx(minHeight);
            Assert.True(minHeightValue > 0, $"Expected non-zero min-height when expressed as px. Actual={minHeight}");
        }
    }

    [Fact]
    public async Task LayoutLab_VerticalStackScenarios_ShowSpacingAndHiddenCollapse()
    {
        await using var context = await _fixture.NewContextAsync(width: 1400, height: 960);
        var page = await OpenAsync(context);

        await page.Locator("[data-automation-id='showcase-nav-layout-lab']").ClickAsync();
        await page.WaitForTimeoutAsync(700);

        var stackGap = await ReadTopDeltaAsync(
            page,
            "[data-automation-id='layout-lab-child-vertical-stack-multi-child-spacing-primary']",
            "[data-automation-id='layout-lab-child-vertical-stack-multi-child-spacing-secondary']");
        Assert.True(stackGap >= 25, $"Expected visible vertical stack spacing gap. Got {stackGap}");

        var hiddenDisplay = await ReadCssPropertyAsync(page, "[data-automation-id='layout-lab-child-vertical-stack-hidden-child-collapse-primary']", "display");
        Assert.Equal("none", hiddenDisplay);

        var firstTop = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-child-vertical-stack-hidden-child-collapse-first']", "top");
        var thirdTop = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-child-vertical-stack-hidden-child-collapse-third']", "top");
        var collapsedDelta = thirdTop - firstTop;
        Assert.True(collapsedDelta < 90, $"Expected collapsed layout delta under hidden middle child. Delta={collapsedDelta}");
    }

    [Fact]
    public async Task LayoutLab_HorizontalStackScenarios_ShowConstraintAndAlignment()
    {
        await using var context = await _fixture.NewContextAsync(width: 1400, height: 960);
        var page = await OpenAsync(context);

        await page.Locator("[data-automation-id='showcase-nav-layout-lab']").ClickAsync();
        await page.WaitForTimeoutAsync(700);

        var overflowHostLeft = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-parent-horizontal-stack-constrained-width-overflow']", "left");
        var overflowHostWidth = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-parent-horizontal-stack-constrained-width-overflow']", "width");
        var overflowHostRight = overflowHostLeft + overflowHostWidth;
        var overflowSecondLeft = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-child-horizontal-stack-constrained-width-overflow-secondary']", "left");
        var overflowSecondWidth = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-child-horizontal-stack-constrained-width-overflow-secondary']", "width");
        var overflowSecondRight = overflowSecondLeft + overflowSecondWidth;
        Assert.True(overflowSecondRight > overflowHostRight, $"Expected constrained-width overflow beyond host edge. HostRight={overflowHostRight}, ChildRight={overflowSecondRight}");

        var clippedHostLeft = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-parent-horizontal-stack-constrained-width-overflow-clipped']", "left");
        var clippedHostWidth = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-parent-horizontal-stack-constrained-width-overflow-clipped']", "width");
        var clippedHostRight = clippedHostLeft + clippedHostWidth;
        var clippedChildLeft = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-child-horizontal-stack-constrained-width-overflow-clipped-secondary']", "left");
        var clippedChildWidth = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-child-horizontal-stack-constrained-width-overflow-clipped-secondary']", "width");
        var clippedChildRight = clippedChildLeft + clippedChildWidth;
        Assert.True(clippedChildRight > clippedHostRight, $"Expected clipped host scenario to still generate overflow geometry. HostRight={clippedHostRight}, ChildRight={clippedChildRight}");

        var centerParentTop = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-parent-horizontal-stack-centered-child']", "top");
        var centerParentHeight = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-parent-horizontal-stack-centered-child']", "height");
        var centerChildTop = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-child-horizontal-stack-centered-child-primary']", "top");
        var centerChildHeight = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-child-horizontal-stack-centered-child-primary']", "height");
        var centerParentY = centerParentTop + (centerParentHeight / 2.0);
        var centerChildY = centerChildTop + (centerChildHeight / 2.0);
        Assert.True(Math.Abs(centerParentY - centerChildY) <= 14, $"Expected roughly centered child Y in row host. Parent={centerParentY}, Child={centerChildY}");

        var flexShrink = await ReadCssPropertyAsync(page, "[data-automation-id='layout-lab-child-horizontal-stack-constrained-width-overflow-secondary']", "flex-shrink");
        Assert.Equal("0", flexShrink);

        var unclippedOverflow = await ReadCssPropertyAsync(page, "[data-automation-id='layout-lab-overflow-host-horizontal-unclipped']", "overflow");
        var clippedOverflow = await ReadCssPropertyAsync(page, "[data-automation-id='layout-lab-overflow-host-horizontal-clipped']", "overflow");
        Assert.Equal("visible", unclippedOverflow);
        Assert.Equal("hidden", clippedOverflow);

        var unclippedClipPath = await ReadCssPropertyAsync(page, "[data-automation-id='layout-lab-overflow-host-horizontal-unclipped']", "clip-path");
        var clippedClipPath = await ReadCssPropertyAsync(page, "[data-automation-id='layout-lab-overflow-host-horizontal-clipped']", "clip-path");
        Assert.True(unclippedClipPath is "" or "none");
        Assert.NotEqual("none", clippedClipPath);
    }

    [Fact]
    public async Task LayoutLab_GridScenarios_ShowPlacementSpanAndTrackBehavior()
    {
        await using var context = await _fixture.NewContextAsync(width: 1400, height: 960);
        var page = await OpenAsync(context);

        await page.Locator("[data-automation-id='showcase-nav-layout-lab']").ClickAsync();
        await page.WaitForTimeoutAsync(700);

        var r0c0Left = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-child-grid-placement-rows-columns-primary']", "left");
        var r0c1Left = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-child-grid-placement-rows-columns-r0c1']", "left");
        var r1c0Top = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-child-grid-placement-rows-columns-r1c0']", "top");
        var r0c0Top = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-child-grid-placement-rows-columns-primary']", "top");
        Assert.True(r0c1Left > r0c0Left, "Expected row/column placement to move second cell to the right.");
        Assert.True(r1c0Top > r0c0Top, "Expected second-row probe to be below first-row probe.");

        var spanWidth = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-child-grid-row-column-span-primary']", "width");
        var singleWidth = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-child-grid-row-column-span-peer']", "width");
        Assert.True(spanWidth > singleWidth, $"Expected span probe width to exceed single-cell peer width. Span={spanWidth}, Peer={singleWidth}");

        var absoluteTrackWidth = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-grid-absolute-track-probe']", "width");
        Assert.InRange(absoluteTrackWidth, 115, 125);
    }

    [Fact]
    public async Task LayoutLab_NestedScenarios_ShowConstraintPropagation()
    {
        await using var context = await _fixture.NewContextAsync(width: 1400, height: 960);
        var page = await OpenAsync(context);

        await page.Locator("[data-automation-id='showcase-nav-layout-lab']").ClickAsync();
        await page.WaitForTimeoutAsync(700);

        var boundedParentWidth = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-parent-nested-fixed-nested-in-bounded']", "width");
        var boundedChildWidth = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-child-nested-fixed-nested-in-bounded-primary']", "width");
        Assert.True(boundedChildWidth < boundedParentWidth, $"Expected fixed nested child to remain bounded by parent. Parent={boundedParentWidth}, Child={boundedChildWidth}");

        var autoParentWidth = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-parent-nested-auto-nested-with-siblings']", "width");
        var autoPrimaryWidth = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-child-nested-auto-nested-with-siblings-primary']", "width");
        var autoParentTop = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-parent-nested-auto-nested-with-siblings']", "top");
        var autoPrimaryTop = await ReadRectValueAsync(page, "[data-automation-id='layout-lab-child-nested-auto-nested-with-siblings-primary']", "top");

        Assert.True(autoPrimaryWidth <= autoParentWidth, $"Expected auto nested child to stay within parent width. Parent={autoParentWidth}, Child={autoPrimaryWidth}");
        Assert.True(autoPrimaryTop >= autoParentTop, $"Expected auto nested child to be positioned within parent bounds. ParentTop={autoParentTop}, ChildTop={autoPrimaryTop}");
    }

    private async Task<IPage> OpenAsync(IBrowserContext context)
    {
        if (_fixture.HttpAppUrl is null)
        {
            throw new InvalidOperationException("Fixture app URL is not initialized.");
        }

        var page = await context.NewPageAsync();
        var diagnostics = new ConcurrentQueue<string>();
        AttachDiagnostics(page, diagnostics);
        await page.GotoAsync(_fixture.HttpAppUrl.ToString(), new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        try
        {
            await page.WaitForSelectorAsync("[data-automation-id='hero-headline']");
        }
        catch (TimeoutException timeoutException)
        {
            var bodyText = await page.TextContentAsync("body");
            var hostLogs = _fixture.GetHostLogsSnapshot();
            var diagnosticsSnapshot = RenderDiagnostics(diagnostics);
            throw new TimeoutException(
                $"{timeoutException.Message}\nBody text snapshot: {bodyText}\nHost logs:\n{hostLogs}\nPage diagnostics:\n{diagnosticsSnapshot}",
                timeoutException);
        }

        return page;
    }

    private static void AttachDiagnostics(IPage page, ConcurrentQueue<string> diagnostics)
    {
        page.Console += (_, message) =>
        {
            diagnostics.Enqueue($"[console:{message.Type}] {message.Text}");
        };

        page.PageError += (_, message) =>
        {
            diagnostics.Enqueue($"[pageerror] {message}");
        };

        page.RequestFailed += (_, request) =>
        {
            diagnostics.Enqueue($"[requestfailed] {request.Method} {request.Url} ({request.Failure ?? "unknown"})");
        };
    }

    private static string RenderDiagnostics(ConcurrentQueue<string> diagnostics)
    {
        var sb = new StringBuilder();
        foreach (var line in diagnostics.TakeLast(80))
        {
            sb.AppendLine(line);
        }

        return sb.ToString();
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

    private static async Task<(double ScrollTop, double ScrollHeight, double ClientHeight)> ReadScrollGeometryAsync(IPage page, string selector)
    {
        var values = await page.EvaluateAsync<double[]>(
            @"(selector) => {
                const element = document.querySelector(selector);
                if (!element) {
                    return [Number.NaN, Number.NaN, Number.NaN];
                }

                return [element.scrollTop, element.scrollHeight, element.clientHeight];
            }",
            selector);

        return (
            values.Length > 0 ? values[0] : double.NaN,
            values.Length > 1 ? values[1] : double.NaN,
            values.Length > 2 ? values[2] : double.NaN);
    }

    private static async Task<(double DocumentScrollHeight, double DocumentClientHeight, double BodyScrollHeight, double BodyClientHeight)> ReadDocumentScrollGeometryAsync(IPage page)
    {
        var values = await page.EvaluateAsync<double[]>(
            @"() => {
                const doc = document.documentElement;
                const body = document.body;
                return [
                    doc?.scrollHeight ?? Number.NaN,
                    doc?.clientHeight ?? Number.NaN,
                    body?.scrollHeight ?? Number.NaN,
                    body?.clientHeight ?? Number.NaN
                ];
            }");

        return (
            values.Length > 0 ? values[0] : double.NaN,
            values.Length > 1 ? values[1] : double.NaN,
            values.Length > 2 ? values[2] : double.NaN,
            values.Length > 3 ? values[3] : double.NaN);
    }

    private static async Task SetElementScrollTopAsync(IPage page, string selector, double top)
    {
        _ = await page.EvaluateAsync<bool>(
            @"({ selector, top }) => {
                const element = document.querySelector(selector);
                if (!element) {
                    return false;
                }

                element.scrollTop = Number.isFinite(top) ? Math.max(0, top) : 0;
                element.dispatchEvent(new Event('scroll', { bubbles: true }));
                return true;
            }",
            new { selector, top });
    }

    private static async Task<double> ReadWindowScrollYAsync(IPage page)
    {
        return await page.EvaluateAsync<double>("() => window.scrollY ?? 0");
    }

    private static double ParsePx(string value)
    {
        if (!value.EndsWith("px", StringComparison.Ordinal))
        {
            return double.NaN;
        }

        var numeric = value[..^2];
        return double.Parse(numeric, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task SetFluentTextFieldValueAsync(IPage page, string selector, string value)
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

        Assert.True(applied, $"Failed to set Fluent text field value for selector '{selector}'.");
    }
}
