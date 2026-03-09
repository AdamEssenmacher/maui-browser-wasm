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
    public async Task ContentViewDemoPage_VisualAndPositioningProbesWork()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 900);
        var page = await OpenAsync(context);

        await page.Locator("[data-automation-id='showcase-nav-content-view']").ClickAsync();
        await page.WaitForTimeoutAsync(700);
        var sectionTop = await ReadTopAsync(page, "[data-automation-id='content-view-demo-page']");
        Assert.True(sectionTop < 320, $"Expected content-view demo section near top after nav click, got {sectionTop}.");

        var automationIds = await page.EvaluateAsync<string[]>(
            @"() => Array.from(document.querySelectorAll('[data-automation-id]'))
                .map((node) => node.getAttribute('data-automation-id') ?? '')
                .filter((id) => id.length > 0)");

        Assert.Contains("content-view-demo-title", automationIds);
        Assert.Contains("content-view-visual-target", automationIds);

        var initialOpacity = await ReadCssPropertyAsync(page, "[data-automation-id='content-view-visual-target']", "opacity");
        await page.Locator("[data-automation-id='content-view-opacity-toggle']").ClickAsync();
        await page.WaitForTimeoutAsync(140);
        var toggledOpacity = await ReadCssPropertyAsync(page, "[data-automation-id='content-view-visual-target']", "opacity");
        Assert.NotEqual(initialOpacity, toggledOpacity);

        var initialBackground = await ReadCssPropertyAsync(page, "[data-automation-id='content-view-visual-target']", "background-color");
        await page.Locator("[data-automation-id='content-view-background-toggle']").ClickAsync();
        await page.WaitForTimeoutAsync(140);
        var toggledBackground = await ReadCssPropertyAsync(page, "[data-automation-id='content-view-visual-target']", "background-color");
        Assert.NotEqual(initialBackground, toggledBackground);

        await page.Locator("[data-automation-id='content-view-visibility-toggle']").ClickAsync();
        await page.WaitForTimeoutAsync(120);
        var hiddenDisplay = await ReadCssPropertyAsync(page, "[data-automation-id='content-view-visual-target']", "display");
        Assert.Equal("none", hiddenDisplay);

        await page.Locator("[data-automation-id='content-view-visibility-toggle']").ClickAsync();
        await page.WaitForTimeoutAsync(120);
        var shownDisplay = await ReadCssPropertyAsync(page, "[data-automation-id='content-view-visual-target']", "display");
        Assert.NotEqual("none", shownDisplay);

        var initialMarginTop = await ReadCssPropertyAsync(page, "[data-automation-id='content-view-visual-target']", "margin-top");
        await page.Locator("[data-automation-id='content-view-margin-toggle']").ClickAsync();
        await page.WaitForTimeoutAsync(120);
        var toggledMarginTop = await ReadCssPropertyAsync(page, "[data-automation-id='content-view-visual-target']", "margin-top");
        Assert.NotEqual(initialMarginTop, toggledMarginTop);

        var scaffoldCount = await page.Locator("text=Host filler row").CountAsync();
        Assert.Equal(0, scaffoldCount);

        var startLeft = await ReadRectValueAsync(page, "[data-automation-id='content-view-horizontal-position-start']", "left");
        var centerLeft = await ReadRectValueAsync(page, "[data-automation-id='content-view-horizontal-position-center']", "left");
        var endLeft = await ReadRectValueAsync(page, "[data-automation-id='content-view-horizontal-position-end']", "left");

        Assert.True(centerLeft > startLeft, $"Expected HorizontalOptions.Center probe to be right of Start. Start={startLeft}, Center={centerLeft}");
        Assert.True(endLeft > centerLeft, $"Expected HorizontalOptions.End probe to be right of Center. Center={centerLeft}, End={endLeft}");

        var startTop = await ReadRectValueAsync(page, "[data-automation-id='content-view-vertical-position-start']", "top");
        var centerTop = await ReadRectValueAsync(page, "[data-automation-id='content-view-vertical-position-center']", "top");
        var endTop = await ReadRectValueAsync(page, "[data-automation-id='content-view-vertical-position-end']", "top");

        Assert.True(centerTop > startTop, $"Expected VerticalOptions.Center probe to be below Start. Start={startTop}, Center={centerTop}");
        Assert.True(endTop > centerTop, $"Expected VerticalOptions.End probe to be below Center. Center={centerTop}, End={endTop}");

        var horizontalHeight = await ReadRectValueAsync(page, "[data-automation-id='content-view-horizontal-position-start']", "height");
        var verticalHeight = await ReadRectValueAsync(page, "[data-automation-id='content-view-vertical-position-start']", "height");
        Assert.True(
            Math.Abs(horizontalHeight - verticalHeight) <= 1.5,
            $"Expected horizontal/vertical positioning probes to have equal height. Horizontal={horizontalHeight}, Vertical={verticalHeight}");

        var canvasCount = await page.Locator("canvas").CountAsync();
        Assert.Equal(0, canvasCount);
    }

    [Fact]
    public async Task ContentViewSizeOverflowLab_StaticRowsMatchExpectedGeometryAndClipping()
    {
        await using var context = await _fixture.NewContextAsync(width: 1400, height: 960);
        var page = await OpenAsync(context);

        await page.Locator("[data-automation-id='showcase-nav-content-view']").ClickAsync();
        await page.WaitForTimeoutAsync(700);

        var widthBaselineStartLeft = await ReadRectValueAsync(page, "[data-automation-id='content-view-size-width-fit-start']", "left");
        var widthBaselineCenterLeft = await ReadRectValueAsync(page, "[data-automation-id='content-view-size-width-fit-center']", "left");
        var widthBaselineEndLeft = await ReadRectValueAsync(page, "[data-automation-id='content-view-size-width-fit-end']", "left");
        Assert.True(widthBaselineCenterLeft > widthBaselineStartLeft);
        Assert.True(widthBaselineEndLeft > widthBaselineCenterLeft);

        var heightBaselineStartTop = await ReadRectValueAsync(page, "[data-automation-id='content-view-size-height-fit-start']", "top");
        var heightBaselineCenterTop = await ReadRectValueAsync(page, "[data-automation-id='content-view-size-height-fit-center']", "top");
        var heightBaselineEndTop = await ReadRectValueAsync(page, "[data-automation-id='content-view-size-height-fit-end']", "top");
        Assert.True(heightBaselineCenterTop > heightBaselineStartTop);
        Assert.True(heightBaselineEndTop > heightBaselineCenterTop);

        var widthOverflowUnclippedProbeWidth = await ReadRectValueAsync(page, "[data-automation-id='content-view-size-width-overflow-unclipped-probe']", "width");
        var widthOverflowUnclippedHostWidth = await ReadRectValueAsync(page, "[data-automation-id='content-view-size-width-overflow-unclipped-host']", "width");
        var widthOverflowClippedProbeWidth = await ReadRectValueAsync(page, "[data-automation-id='content-view-size-width-overflow-clipped-probe']", "width");
        var widthOverflowClippedHostWidth = await ReadRectValueAsync(page, "[data-automation-id='content-view-size-width-overflow-clipped-host']", "width");
        Assert.True(widthOverflowUnclippedProbeWidth > widthOverflowUnclippedHostWidth);
        Assert.True(widthOverflowClippedProbeWidth > widthOverflowClippedHostWidth);

        var heightOverflowUnclippedProbeHeight = await ReadRectValueAsync(page, "[data-automation-id='content-view-size-height-overflow-unclipped-probe']", "height");
        var heightOverflowUnclippedHostHeight = await ReadRectValueAsync(page, "[data-automation-id='content-view-size-height-overflow-unclipped-host']", "height");
        var heightOverflowClippedProbeHeight = await ReadRectValueAsync(page, "[data-automation-id='content-view-size-height-overflow-clipped-probe']", "height");
        var heightOverflowClippedHostHeight = await ReadRectValueAsync(page, "[data-automation-id='content-view-size-height-overflow-clipped-host']", "height");
        Assert.True(heightOverflowUnclippedProbeHeight > heightOverflowUnclippedHostHeight);
        Assert.True(heightOverflowClippedProbeHeight > heightOverflowClippedHostHeight);

        var unclippedWidthOverflow = await ReadCssPropertyAsync(page, "[data-automation-id='content-view-size-width-overflow-unclipped-host']", "overflow");
        var clippedWidthOverflow = await ReadCssPropertyAsync(page, "[data-automation-id='content-view-size-width-overflow-clipped-host']", "overflow");
        Assert.Equal("visible", unclippedWidthOverflow);
        Assert.Equal("hidden", clippedWidthOverflow);

        var unclippedHeightOverflow = await ReadCssPropertyAsync(page, "[data-automation-id='content-view-size-height-overflow-unclipped-host']", "overflow");
        var clippedHeightOverflow = await ReadCssPropertyAsync(page, "[data-automation-id='content-view-size-height-overflow-clipped-host']", "overflow");
        Assert.Equal("visible", unclippedHeightOverflow);
        Assert.Equal("hidden", clippedHeightOverflow);

        var unclippedWidthClipPath = await ReadCssPropertyAsync(page, "[data-automation-id='content-view-size-width-overflow-unclipped-host']", "clip-path");
        var clippedWidthClipPath = await ReadCssPropertyAsync(page, "[data-automation-id='content-view-size-width-overflow-clipped-host']", "clip-path");
        Assert.True(unclippedWidthClipPath is "" or "none");
        Assert.NotEqual("none", clippedWidthClipPath);

        var unclippedHeightClipPath = await ReadCssPropertyAsync(page, "[data-automation-id='content-view-size-height-overflow-unclipped-host']", "clip-path");
        var clippedHeightClipPath = await ReadCssPropertyAsync(page, "[data-automation-id='content-view-size-height-overflow-clipped-host']", "clip-path");
        Assert.True(unclippedHeightClipPath is "" or "none");
        Assert.NotEqual("none", clippedHeightClipPath);
    }

    [Fact]
    public async Task ContentViewSizeOverflowLab_InteractiveProbeTogglesAreDeterministic()
    {
        await using var context = await _fixture.NewContextAsync(width: 1400, height: 960);
        var page = await OpenAsync(context);

        await page.Locator("[data-automation-id='showcase-nav-content-view']").ClickAsync();
        await page.WaitForTimeoutAsync(700);

        var widthHostWidth = await ReadRectValueAsync(page, "[data-automation-id='content-view-size-interactive-width-host']", "width");
        var widthFit = await ReadRectValueAsync(page, "[data-automation-id='content-view-size-interactive-width-probe']", "width");
        Assert.True(widthFit < widthHostWidth);

        await page.Locator("[data-automation-id='content-view-size-interactive-width-mode-toggle']").ClickAsync();
        await page.WaitForTimeoutAsync(120);
        var widthOverflow = await ReadRectValueAsync(page, "[data-automation-id='content-view-size-interactive-width-probe']", "width");
        Assert.True(widthOverflow > widthHostWidth);

        var initialWidthOptionText = await page.Locator("[data-automation-id='content-view-size-interactive-horizontal-option-toggle']").InnerTextAsync();
        await page.Locator("[data-automation-id='content-view-size-interactive-horizontal-option-toggle']").ClickAsync();
        await page.WaitForTimeoutAsync(120);
        var centeredWidthOptionText = await page.Locator("[data-automation-id='content-view-size-interactive-horizontal-option-toggle']").InnerTextAsync();
        Assert.NotEqual(initialWidthOptionText, centeredWidthOptionText);

        var widthClipBefore = await ReadCssPropertyAsync(page, "[data-automation-id='content-view-size-interactive-width-probe']", "clip-path");
        await page.Locator("[data-automation-id='content-view-size-interactive-width-clip-toggle']").ClickAsync();
        await page.WaitForTimeoutAsync(120);
        var widthClipAfter = await ReadCssPropertyAsync(page, "[data-automation-id='content-view-size-interactive-width-probe']", "clip-path");
        Assert.True(widthClipBefore is "" or "none");
        Assert.NotEqual("none", widthClipAfter);

        var heightHostHeight = await ReadRectValueAsync(page, "[data-automation-id='content-view-size-interactive-height-host']", "height");
        var heightFit = await ReadRectValueAsync(page, "[data-automation-id='content-view-size-interactive-height-probe']", "height");
        Assert.True(heightFit < heightHostHeight);

        await page.Locator("[data-automation-id='content-view-size-interactive-height-mode-toggle']").ClickAsync();
        await page.WaitForTimeoutAsync(120);
        var heightOverflow = await ReadRectValueAsync(page, "[data-automation-id='content-view-size-interactive-height-probe']", "height");
        Assert.True(heightOverflow > heightHostHeight);

        var initialHeightOptionText = await page.Locator("[data-automation-id='content-view-size-interactive-vertical-option-toggle']").InnerTextAsync();
        await page.Locator("[data-automation-id='content-view-size-interactive-vertical-option-toggle']").ClickAsync();
        await page.WaitForTimeoutAsync(120);
        var centeredHeightOptionText = await page.Locator("[data-automation-id='content-view-size-interactive-vertical-option-toggle']").InnerTextAsync();
        Assert.NotEqual(initialHeightOptionText, centeredHeightOptionText);

        var heightClipBefore = await ReadCssPropertyAsync(page, "[data-automation-id='content-view-size-interactive-height-probe']", "clip-path");
        await page.Locator("[data-automation-id='content-view-size-interactive-height-clip-toggle']").ClickAsync();
        await page.WaitForTimeoutAsync(120);
        var heightClipAfter = await ReadCssPropertyAsync(page, "[data-automation-id='content-view-size-interactive-height-probe']", "clip-path");
        Assert.True(heightClipBefore is "" or "none");
        Assert.NotEqual("none", heightClipAfter);
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
