using System.Collections.Concurrent;
using System.Text;
using Microsoft.Playwright;
using Xunit;

namespace Mirage.E2E.Tests;

public sealed class ShowcaseFluentHeroE2ETests : IClassFixture<ShowcaseHostFixture>
{
    private static readonly string? s_diagnosticsLogFile = Environment.GetEnvironmentVariable("MIRAGE_DIAGNOSTICS_LOG_FILE");
    private static readonly object s_diagnosticsLogSync = new();
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
        var runtimeBadge = await page.Locator("[data-automation-id='showcase-runtime-badge']").InnerTextAsync();
        var hasDemoSectionId = await page.Locator("[data-automation-id='demo']").CountAsync();
        var hasProvidersSectionId = await page.Locator("[data-automation-id='providers']").CountAsync();
        var automationIds = await page.EvaluateAsync<string[]>(
            @"() => Array.from(document.querySelectorAll('[data-automation-id]'))
                .map((node) => node.getAttribute('data-automation-id') ?? '')
                .filter((id) => id.length > 0)");

        Assert.Contains("real DOM UI", headline, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fluent provider", runtimeBadge, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, hasDemoSectionId);
        Assert.True(hasProvidersSectionId > 0, $"Expected providers section id. IDs: {string.Join(',', automationIds)}");
    }

    [Fact]
    public async Task HeroStyles_AreProjectedFromMauiResourceStyles()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        var pageBackground = await ReadCssPropertyAsync(page, "[data-automation-id='showcase-root']", "background-color");
        var heroBackground = await ReadCssPropertyAsync(page, "[data-automation-id='hero']", "background-color");
        var headlineColor = await ReadCssPropertyAsync(page, "[data-automation-id='hero-headline']", "color");
        var subheadlineColor = await ReadCssPropertyAsync(page, "[data-automation-id='hero-subheadline']", "color");
        var headlineFontSize = await ReadCssPropertyAsync(page, "[data-automation-id='hero-headline']", "font-size");
        var subheadlineFontSize = await ReadCssPropertyAsync(page, "[data-automation-id='hero-subheadline']", "font-size");
        var ctaBackground = await ReadCssPropertyAsync(page, "[data-automation-id='hero-cta-command-layout-lab']", "background-color");
        var ctaAccentFill = await ReadCssPropertyAsync(page, "[data-automation-id='hero-cta-command-layout-lab']", "--accent-fill-rest");
        var ctaCornerToken = await ReadCssPropertyAsync(page, "[data-automation-id='hero-cta-command-layout-lab']", "--control-corner-radius");
        var ctaBorderRadius = await ReadCssPropertyAsync(page, "[data-automation-id='hero-cta-command-layout-lab']", "border-radius");
        var ctaBorderWidth = await ReadCssPropertyAsync(page, "[data-automation-id='hero-cta-command-layout-lab']", "border-top-width");

        Assert.NotEqual("rgba(0, 0, 0, 0)", pageBackground);
        Assert.Equal("rgba(0, 0, 0, 0)", heroBackground);
        Assert.True(
            headlineColor.Contains("25, 6, 73", StringComparison.Ordinal)
            || headlineColor.Contains("255, 255, 255", StringComparison.Ordinal),
            $"Expected themed Headline color, got '{headlineColor}'.");
        Assert.True(
            subheadlineColor.Contains("25, 6, 73", StringComparison.Ordinal)
            || subheadlineColor.Contains("255, 255, 255", StringComparison.Ordinal),
            $"Expected themed SubHeadline color, got '{subheadlineColor}'.");
        Assert.Equal("32px", headlineFontSize);
        Assert.Equal("24px", subheadlineFontSize);
        var normalizedAccentToken = ctaAccentFill.Replace(" ", string.Empty, StringComparison.Ordinal);
        var normalizedBackground = ctaBackground.Replace(" ", string.Empty, StringComparison.Ordinal);
        var hasAccentToken = normalizedAccentToken.Contains("81,43,212", StringComparison.Ordinal);
        var hasAccentBackground = normalizedBackground.Contains("81,43,212", StringComparison.Ordinal);
        Assert.True(
            hasAccentToken || hasAccentBackground,
            $"Expected CTA accent via token or direct background color, got token='{ctaAccentFill}', background='{ctaBackground}'.");
        Assert.True(
            string.Equals(ctaCornerToken, "8px", StringComparison.Ordinal)
            || string.Equals(ctaBorderRadius, "8px", StringComparison.Ordinal),
            $"Expected CTA corner radius to be 8px from token or direct style, got token='{ctaCornerToken}', borderRadius='{ctaBorderRadius}'.");
        Assert.EndsWith("px", ctaBorderWidth, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AppThemeBinding_TracksBrowserColorSchemeChanges()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        await page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });
        await page.WaitForTimeoutAsync(140);
        var lightHeadlineColor = await ReadCssPropertyAsync(page, "[data-automation-id='hero-headline']", "color");

        await page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await page.WaitForTimeoutAsync(140);
        var darkHeadlineColor = await ReadCssPropertyAsync(page, "[data-automation-id='hero-headline']", "color");

        await page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });
        await page.WaitForTimeoutAsync(140);
        var lightHeadlineColorAfter = await ReadCssPropertyAsync(page, "[data-automation-id='hero-headline']", "color");

        Assert.NotEqual(lightHeadlineColor, darkHeadlineColor);
        Assert.Equal(lightHeadlineColor, lightHeadlineColorAfter);
    }

    [Fact]
    public async Task HeaderNav_IsNotRendered()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        Assert.Equal(0, await page.Locator("[data-automation-id='showcase-top-nav']").CountAsync());
        Assert.Equal(0, await page.Locator("[data-automation-id^='showcase-nav-']").CountAsync());
    }

    [Fact]
    public async Task BacklogSection_IsNotRendered()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        Assert.Equal(0, await page.Locator("[data-automation-id='showcase-backlog']").CountAsync());
        Assert.Equal(0, await page.Locator("[data-automation-id='showcase-backlog-table']").CountAsync());
        Assert.Equal(0, await page.Locator("[data-automation-id='showcase-backlog-table-host']").CountAsync());
    }

    [Fact]
    public async Task FoundationsSection_IsNotRendered()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        Assert.Equal(0, await page.Locator("[data-automation-id='showcase-foundations-lab']").CountAsync());
        Assert.Equal(0, await page.Locator("[data-automation-id^='foundations-']").CountAsync());
    }

    [Fact]
    public async Task WhyMattersAndGithubPlaceholder_AreNotRendered()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        Assert.Equal(0, await page.Locator("[data-automation-id='hero-why-matters']").CountAsync());
        Assert.Equal(0, await page.Locator("[data-automation-id='hero-why-contrast']").CountAsync());
        Assert.Equal(0, await page.Locator("[data-automation-id='hero-cta-github-placeholder']").CountAsync());
        Assert.Equal(0, await page.Locator("[data-automation-id='hero-scroll-cue']").CountAsync());
    }

    [Fact]
    public async Task LiveDemoAndGenericWidget_AreNotRendered()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        Assert.Equal(0, await page.Locator("[data-automation-id='demo']").CountAsync());
        Assert.Equal(0, await page.Locator("[data-automation-id^='showcase-demo-']").CountAsync());
        Assert.Equal(0, await page.Locator("[data-automation-id='showcase-generic-widget']").CountAsync());
        Assert.Equal(0, await page.Locator("vendor-pill").CountAsync());
    }

    [Fact]
    public async Task CtaButton_NavigatesToLayoutLab()
    {
        await using var context = await _fixture.NewContextAsync(width: 1180, height: 760);
        var page = await OpenAsync(context);

        await page.Locator("[data-automation-id='hero-cta-command-layout-lab']").ClickAsync();
        await page.WaitForFunctionAsync("() => window.location.pathname === '/layout-lab'");
        await page.WaitForSelectorAsync("[data-automation-id='showcase-page-layout-lab']");
        var layoutLabPath = await page.EvaluateAsync<string>("() => window.location.pathname");
        Assert.Equal("/layout-lab", layoutLabPath);
    }

    [Fact]
    public async Task CtaButton_NavigatesToXamlLab()
    {
        await using var context = await _fixture.NewContextAsync(width: 1180, height: 760);
        var page = await OpenAsync(context);

        await page.Locator("[data-automation-id='hero-cta-command-xaml-lab']").ClickAsync();
        await page.WaitForFunctionAsync("() => window.location.pathname === '/xaml-lab'");
        await page.WaitForSelectorAsync("[data-automation-id='showcase-page-xaml-lab']");
        var xamlLabPath = await page.EvaluateAsync<string>("() => window.location.pathname");
        Assert.Equal("/xaml-lab", xamlLabPath);
    }

    [Fact]
    public async Task HeroRendersExpectedDomElementsAndNoCanvasSurface()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        var buttonCount = await page.Locator("button[data-mirage-kind='Button']").CountAsync();
        var fluentTextFieldCount = await page.Locator("fluent-text-field").CountAsync();
        var genericElementCount = await page.Locator("vendor-pill").CountAsync();
        var canvasCount = await page.Locator("canvas").CountAsync();

        Assert.True(buttonCount > 0, "Expected Mirage button host elements in the hero page DOM.");
        Assert.Equal(0, fluentTextFieldCount);
        Assert.Equal(0, genericElementCount);
        Assert.Equal(0, canvasCount);
    }

    [Fact]
    public async Task GenericWebNodeDemo_IsNotRendered()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        Assert.Equal(0, await page.Locator("[data-automation-id='showcase-generic-widget']").CountAsync());
        Assert.Equal(0, await page.Locator("[data-automation-id='showcase-generic-widget-host']").CountAsync());
    }

    [Fact]
    public async Task BuiltInHeroControlsRemainControlNodes()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenAsync(context);

        var buttonNodeType = await page.Locator("[data-automation-id='hero-cta-command-layout-lab']").GetAttributeAsync("data-mirage-node-type");
        var labelNodeType = await page.Locator("[data-automation-id='hero-headline']").GetAttributeAsync("data-mirage-node-type");
        var containerNodeType = await page.Locator("[data-automation-id='providers']").GetAttributeAsync("data-mirage-node-type");

        Assert.Equal("control", buttonNodeType);
        Assert.Equal("control", labelNodeType);
        Assert.Equal("control", containerNodeType);
    }

    [Fact]
    public async Task LayoutLab_RendersAllGroupsAndScenarioCardCount()
    {
        await using var context = await _fixture.NewContextAsync(width: 1400, height: 960);
        var page = await OpenAsync(
            context,
            path: "/layout-lab",
            waitForSelector: "[data-automation-id='showcase-page-layout-lab']");

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
    public async Task LayoutLab_SelfWidthBindingPatterns_ProjectNonZeroHeights()
    {
        await using var context = await _fixture.NewContextAsync(width: 1400, height: 960);
        var page = await OpenAsync(
            context,
            path: "/layout-lab",
            waitForSelector: "[data-automation-id='showcase-page-layout-lab']");

        var relativeSelfHeight = await ReadCssPropertyAsync(page, "[data-automation-id='layout-lab-square-probe']", "height");
        var xReferenceHeight = await ReadCssPropertyAsync(page, "[data-automation-id='layout-lab-square-probe-xreference']", "height");

        Assert.EndsWith("px", relativeSelfHeight, StringComparison.Ordinal);
        Assert.EndsWith("px", xReferenceHeight, StringComparison.Ordinal);

        var relativeSelfHeightPx = ParsePx(relativeSelfHeight);
        var xReferenceHeightPx = ParsePx(xReferenceHeight);
        Assert.True(relativeSelfHeightPx > 0, $"Expected RelativeSource Self binding to project a non-zero height. Actual={relativeSelfHeight}");
        Assert.True(xReferenceHeightPx > 0, $"Expected x:Reference binding to project a non-zero height. Actual={xReferenceHeight}");
    }

    [Fact]
    public async Task ContentPage_DefaultSurface_IsNotDocumentScrollable()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 900);
        var page = await OpenAsync(
            context,
            path: "/layout-lab",
            waitForSelector: "[data-automation-id='showcase-page-layout-lab']");

        var rootOverflowY = await ReadCssPropertyAsync(page, "[data-automation-id='showcase-page-layout-lab']", "overflow-y");
        var scrollRootOverflowY = await ReadCssPropertyAsync(page, "[data-automation-id='layout-lab-scroll-root']", "overflow-y");
        var metrics = await ReadScrollGeometryAsync(page, "[data-automation-id='layout-lab-scroll-root']");
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
        var page = await OpenAsync(
            context,
            path: "/layout-lab",
            waitForSelector: "[data-automation-id='showcase-page-layout-lab']");

        await SetElementScrollTopAsync(page, "[data-automation-id='layout-lab-scroll-root']", 0);
        var initial = await ReadScrollGeometryAsync(page, "[data-automation-id='layout-lab-scroll-root']");
        Assert.True(initial.ScrollHeight > initial.ClientHeight + 120, $"Expected scrollable root geometry. Height={initial.ScrollHeight}, Client={initial.ClientHeight}");

        await SetElementScrollTopAsync(page, "[data-automation-id='layout-lab-scroll-root']", 700);
        await page.WaitForTimeoutAsync(120);

        var scrolled = await ReadScrollGeometryAsync(page, "[data-automation-id='layout-lab-scroll-root']");
        Assert.True(scrolled.ScrollTop > 120, $"Expected scroll root top to change. Actual={scrolled.ScrollTop}");
    }

    [Fact]
    public async Task Showcase_CtaLayoutLab_NavigatesWithoutDocumentScroll()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 900);
        var page = await OpenAsync(context);

        await SetElementScrollTopAsync(page, "[data-automation-id='showcase-scroll-root']", 0);
        await page.Locator("[data-automation-id='hero-cta-command-layout-lab']").ClickAsync();
        await page.WaitForFunctionAsync("() => window.location.pathname === '/layout-lab'");
        await page.WaitForSelectorAsync("[data-automation-id='showcase-page-layout-lab']");

        var pathname = await page.EvaluateAsync<string>("() => window.location.pathname");
        var layoutLabScrollRoot = await ReadScrollGeometryAsync(page, "[data-automation-id='layout-lab-scroll-root']");
        var windowScrollY = await ReadWindowScrollYAsync(page);
        Assert.Equal("/layout-lab", pathname);
        Assert.True(layoutLabScrollRoot.ScrollTop <= 1, $"Expected dedicated layout-lab scroll root to start at top. Actual={layoutLabScrollRoot.ScrollTop}");
        Assert.True(windowScrollY <= 1, $"Expected document scroll to remain near zero. Actual={windowScrollY}");
    }

    [Fact]
    public async Task Showcase_RequestScrollTo_CommandPath_IsInvoked()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 900);
        var page = await OpenAsync(
            context,
            path: "/layout-lab",
            waitForSelector: "[data-automation-id='showcase-page-layout-lab']");

        await SetElementScrollTopAsync(page, "[data-automation-id='layout-lab-scroll-root']", 700);
        var before = await ReadScrollGeometryAsync(page, "[data-automation-id='layout-lab-scroll-root']");
        Assert.True(before.ScrollTop > 120, $"Expected command probe to require scrolling first. Actual={before.ScrollTop}");
        await page.Locator("[data-automation-id='layout-lab-scroll-command-top']").ClickAsync();
        await page.WaitForFunctionAsync(
            @"() => {
                const counter = document.querySelector('[data-automation-id=""layout-lab-scroll-command-count""]');
                return !!counter && (counter.textContent ?? '').includes('1');
            }");

        var after = await ReadScrollGeometryAsync(page, "[data-automation-id='layout-lab-scroll-root']");
        var afterCounter = await page.Locator("[data-automation-id='layout-lab-scroll-command-count']").InnerTextAsync();
        var windowScrollY = await ReadWindowScrollYAsync(page);

        Assert.Contains("1", afterCounter, StringComparison.Ordinal);
        Assert.True(after.ScrollTop < 20, $"Expected command invocation to scroll layout lab root to top. Before={before.ScrollTop}, After={after.ScrollTop}");
        Assert.True(windowScrollY <= 1, $"Expected document scroll to remain near zero. Actual={windowScrollY}");
    }

    [Fact]
    public async Task NavigationLab_DeepLink_OpensCorrectPage()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 900);
        var page = await OpenAsync(
            context,
            path: "/navigation-lab",
            waitForSelector: "[data-automation-id='showcase-page-navigation-lab']");

        var routeLabel = await page.Locator("[data-automation-id='showcase-navigation-lab-route']").InnerTextAsync();
        var pathname = await page.EvaluateAsync<string>("() => window.location.pathname");

        Assert.Contains("/navigation-lab", routeLabel, StringComparison.Ordinal);
        Assert.Equal("/navigation-lab", pathname);
    }

    [Fact]
    public async Task XamlLab_RendersEditorAsWebNode_AndInitialPreview()
    {
        await using var context = await _fixture.NewContextAsync(width: 1360, height: 900);
        var page = await OpenAsync(
            context,
            path: "/xaml-lab",
            waitForSelector: "[data-automation-id='showcase-page-xaml-lab']");

        await page.WaitForSelectorAsync("[data-automation-id='xaml-lab-editor']");
        await page.WaitForSelectorAsync("[data-automation-id='xaml-lab-preview-host']");

        var editorTag = await page.EvaluateAsync<string>(
            "() => (document.querySelector('[data-automation-id=\"xaml-lab-editor\"]')?.tagName ?? '').toLowerCase()");
        var editorNodeType = await page.Locator("[data-automation-id='xaml-lab-editor']")
            .GetAttributeAsync("data-mirage-node-type");
        var customElementDefined = await page.EvaluateAsync<bool>(
            "() => typeof customElements.get('mirage-monaco-editor') === 'function'");
        var editorBackend = await page.Locator("[data-automation-id='xaml-lab-editor']")
            .GetAttributeAsync("data-editor-backend");
        var editorGeometry = await page.EvaluateAsync<double[]>(
            @"() => {
                const editor = document.querySelector('[data-automation-id=""xaml-lab-editor""]');
                if (!editor) {
                    return [0, 0];
                }

                const rect = editor.getBoundingClientRect();
                return [rect.width, rect.height];
            }");
        var editorInternalsJson = await page.EvaluateAsync<string>(
            @"() => {
                const editor = document.querySelector('[data-automation-id=""xaml-lab-editor""]');
                if (!editor) {
                    return JSON.stringify({ visible: false, reason: 'editor-missing' });
                }

                const root = editor.shadowRoot ?? editor;
                if (!root) {
                    return JSON.stringify({
                        visible: false,
                        reason: 'editor-root-missing',
                        childCount: editor.childElementCount
                    });
                }

                const textarea = root.querySelector('textarea');
                if (textarea && getComputedStyle(textarea).display !== 'none') {
                    const rect = textarea.getBoundingClientRect();
                    return JSON.stringify({
                        visible: rect.width > 100 && rect.height > 100,
                        reason: 'textarea-visible',
                        width: rect.width,
                        height: rect.height
                    });
                }

                const monacoHost = root.querySelector('.monaco-host');
                if (monacoHost && getComputedStyle(monacoHost).display !== 'none') {
                    const rect = monacoHost.getBoundingClientRect();
                    return JSON.stringify({
                        visible: rect.width > 100 && rect.height > 100,
                        reason: 'monaco-host-visible',
                        width: rect.width,
                        height: rect.height
                    });
                }

                return JSON.stringify({
                    visible: false,
                    reason: 'no-visible-editor-surface',
                    mode: editor.shadowRoot ? 'shadow' : 'light',
                    shadowChildren: root.childElementCount
                });
            }");

        Assert.Equal("mirage-monaco-editor", editorTag);
        Assert.Equal("web", editorNodeType);
        Assert.True(customElementDefined, "Expected mirage-monaco-editor custom element to be defined.");
        Assert.True(editorBackend is "monaco" or "textarea", $"Expected editor backend marker to be set. Actual='{editorBackend ?? "(null)"}'.");
        Assert.True(editorGeometry.Length == 2 && editorGeometry[0] > 200 && editorGeometry[1] > 200,
            $"Expected visible editor geometry. Width={editorGeometry.ElementAtOrDefault(0)}, Height={editorGeometry.ElementAtOrDefault(1)}");
        using var editorInternalsDocument = System.Text.Json.JsonDocument.Parse(editorInternalsJson);
        var editorInternalsVisible = editorInternalsDocument.RootElement.TryGetProperty("visible", out var visibleValue)
            && visibleValue.ValueKind == System.Text.Json.JsonValueKind.True;
        Assert.True(
            editorInternalsVisible,
            $"Expected textarea or Monaco host to be visibly rendered inside editor host. Details={editorInternalsJson}");

        await page.WaitForFunctionAsync(
            @"() => (document.querySelector('[data-automation-id=""xaml-lab-preview-host""]')?.textContent ?? '')
                .includes('Hello Mirage XAML Lab')");

        var previewCanvasCount = await page.Locator("[data-automation-id='xaml-lab-preview-host'] canvas").CountAsync();
        Assert.Equal(0, previewCanvasCount);
    }

    [Fact]
    public async Task XamlLab_EditorWidth_RemainsStableAfterInitialRender()
    {
        await using var context = await _fixture.NewContextAsync(width: 1360, height: 900);
        var page = await OpenAsync(
            context,
            path: "/xaml-lab",
            waitForSelector: "[data-automation-id='showcase-page-xaml-lab']");

        await page.WaitForSelectorAsync("[data-automation-id='xaml-lab-editor']");

        var samples = new List<double>(capacity: 20);
        for (var index = 0; index < 20; index++)
        {
            var width = await page.EvaluateAsync<double>(
                @"() => {
                    const editor = document.querySelector('[data-automation-id=""xaml-lab-editor""]');
                    if (!editor) {
                        return Number.NaN;
                    }

                    return editor.getBoundingClientRect().width;
                }");

            samples.Add(width);
            await page.WaitForTimeoutAsync(400);
        }

        var validSamples = samples.Where(double.IsFinite).ToArray();
        Assert.True(validSamples.Length >= 16, $"Expected valid editor width samples. Raw={string.Join(", ", samples)}");

        var settledWindow = validSamples.Skip(Math.Max(0, validSamples.Length - 8)).ToArray();
        var maxWidth = settledWindow.Max();
        var minWidth = settledWindow.Min();
        var drift = maxWidth - minWidth;
        Assert.True(
            drift <= 4,
            $"Expected settled editor width drift <= 4px. Drift={drift:0.###}; Settled={string.Join(", ", settledWindow.Select(v => v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)))}; All={string.Join(", ", validSamples.Select(v => v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)))}");
    }

    [Fact]
    public async Task XamlLab_EditorUpdatesPreviewAfterDebounce()
    {
        await using var context = await _fixture.NewContextAsync(width: 1360, height: 900);
        var page = await OpenAsync(
            context,
            path: "/xaml-lab",
            waitForSelector: "[data-automation-id='showcase-page-xaml-lab']");

        await page.EvaluateAsync(
            """
            () => {
                const editor = document.querySelector('[data-automation-id="xaml-lab-editor"]');
                if (!editor) {
                    throw new Error("xaml lab editor element is unavailable");
                }

                editor.value = `<VerticalStackLayout Padding="10" Spacing="8">
                    <Label Text="Live update marker" />
                    <Button Text="Apply marker button" />
                </VerticalStackLayout>`;
                editor.dispatchEvent(new Event("input", { bubbles: true, composed: true }));
            }
            """);

        await page.WaitForFunctionAsync(
            @"() => (document.querySelector('[data-automation-id=""xaml-lab-preview-host""]')?.textContent ?? '')
                .includes('Live update marker')");
    }

    [Fact]
    public async Task XamlLab_InvalidFragment_ShowsErrorAndPreservesLastGoodPreview()
    {
        await using var context = await _fixture.NewContextAsync(width: 1360, height: 900);
        var page = await OpenAsync(
            context,
            path: "/xaml-lab",
            waitForSelector: "[data-automation-id='showcase-page-xaml-lab']");

        await page.EvaluateAsync(
            """
            () => {
                const editor = document.querySelector('[data-automation-id="xaml-lab-editor"]');
                if (!editor) {
                    throw new Error("xaml lab editor element is unavailable");
                }

                editor.value = `<VerticalStackLayout Padding="8">
                    <Label Text="Stable preview marker" />
                </VerticalStackLayout>`;
                editor.dispatchEvent(new Event("input", { bubbles: true, composed: true }));
            }
            """);

        await page.WaitForFunctionAsync(
            @"() => (document.querySelector('[data-automation-id=""xaml-lab-preview-host""]')?.textContent ?? '')
                .includes('Stable preview marker')");

        await page.EvaluateAsync(
            """
            () => {
                const editor = document.querySelector('[data-automation-id="xaml-lab-editor"]');
                if (!editor) {
                    throw new Error("xaml lab editor element is unavailable");
                }

                editor.value = `<VerticalStackLayout><Label Text="Broken marker"></VerticalStackLayout>`;
                editor.dispatchEvent(new Event("input", { bubbles: true, composed: true }));
            }
            """);

        await page.WaitForFunctionAsync(
            @"() => {
                const panel = document.querySelector('[data-automation-id=""xaml-lab-error-panel""]');
                return !!panel && getComputedStyle(panel).display !== 'none';
            }");

        var previewTextAfterInvalid = await page.Locator("[data-automation-id='xaml-lab-preview-host']").InnerTextAsync();
        Assert.Contains("Stable preview marker", previewTextAfterInvalid, StringComparison.Ordinal);
    }

    [Fact]
    public async Task XamlLab_EntryMinHeight_KeepsFluentChromeAligned()
    {
        await using var context = await _fixture.NewContextAsync(width: 1360, height: 900);
        var page = await OpenAsync(
            context,
            path: "/xaml-lab",
            waitForSelector: "[data-automation-id='showcase-page-xaml-lab']");

        await page.EvaluateAsync(
            """
            () => {
                const editor = document.querySelector('[data-automation-id="xaml-lab-editor"]');
                if (!editor) {
                    throw new Error("xaml lab editor element is unavailable");
                }

                editor.value = `<Grid Padding="12" ColumnDefinitions="Auto,*" RowDefinitions="Auto,Auto,Auto" RowSpacing="10" ColumnSpacing="10">
                    <Label Text="Name" Grid.Row="0" Grid.Column="0" VerticalOptions="Center" />
                    <Entry Grid.Row="0" Grid.Column="1" Placeholder="Ada Lovelace" />
                    <Label Text="Role" Grid.Row="1" Grid.Column="0" VerticalOptions="Center" />
                    <Entry Grid.Row="1" Grid.Column="1" Placeholder=".NET Architect" />
                    <Button Grid.Row="2" Grid.Column="1" HorizontalOptions="End" Text="Save" />
                </Grid>`;
                editor.dispatchEvent(new Event("input", { bubbles: true, composed: true }));
            }
            """);

        await page.WaitForSelectorAsync("[data-mirage-kind='Entry']");
        await page.EvaluateAsync(
            """
            () => {
                const entry = document.querySelectorAll('[data-mirage-kind="Entry"]')[0];
                if (entry) {
                    entry.focus();
                }
            }
            """);

        var metricsJson = await page.EvaluateAsync<string>(
            """
            () => {
                const entry = document.querySelectorAll('[data-mirage-kind="Entry"]')[0];
                if (!entry) {
                    return JSON.stringify({ hasEntry: false });
                }

                const hostRect = entry.getBoundingClientRect();
                const hostStyle = getComputedStyle(entry);
                const shadow = entry.shadowRoot;
                const root = shadow?.querySelector('.root, [part="root"]');
                const control = shadow?.querySelector('[part="control"]');
                const rootRect = root?.getBoundingClientRect() ?? null;
                const controlRect = control?.getBoundingClientRect() ?? null;

                return JSON.stringify({
                    hasEntry: true,
                    hostHeight: hostRect.height,
                    rootHeight: rootRect?.height ?? null,
                    controlHeight: controlRect?.height ?? null,
                    hostBottom: hostRect.bottom,
                    rootBottom: rootRect?.bottom ?? null,
                    baseHeightMultiplier: hostStyle.getPropertyValue('--base-height-multiplier').trim(),
                    minHeight: hostStyle.minHeight
                });
            }
            """);

        using var metricsDocument = System.Text.Json.JsonDocument.Parse(metricsJson);
        var root = metricsDocument.RootElement;
        Assert.True(root.GetProperty("hasEntry").GetBoolean(), $"Expected entry metrics payload. Details={metricsJson}");

        var hostHeight = root.GetProperty("hostHeight").GetDouble();
        var rootHeight = root.GetProperty("rootHeight").GetDouble();
        var hostBottom = root.GetProperty("hostBottom").GetDouble();
        var rootBottom = root.GetProperty("rootBottom").GetDouble();
        var baseHeightMultiplier = root.GetProperty("baseHeightMultiplier").GetString();
        var minHeight = root.GetProperty("minHeight").GetString();

        Assert.Equal("44px", minHeight);
        Assert.Equal("11", baseHeightMultiplier);
        Assert.True(Math.Abs(hostHeight - rootHeight) <= 2, $"Expected host/root heights to align. Metrics={metricsJson}");
        Assert.True(Math.Abs(hostBottom - rootBottom) <= 2, $"Expected host/root bottoms to align. Metrics={metricsJson}");
    }

    [Fact]
    public async Task NavigationChrome_TitleAndBackButton_FollowNavigationStack()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 900);
        var page = await OpenAsync(context);

        await page.WaitForSelectorAsync("[data-automation-id='mirage-navigation-title']");
        var rootTitle = await page.Locator("[data-automation-id='mirage-navigation-title']").InnerTextAsync();
        Assert.Contains("Mirage Showcase", rootTitle, StringComparison.Ordinal);

        var rootBackDisplay = await ReadCssPropertyAsync(page, "[data-automation-id='mirage-navigation-back-button']", "display");
        Assert.Equal("none", rootBackDisplay);

        await page.Locator("[data-automation-id='showcase-open-navigation-lab']").ClickAsync();
        await page.WaitForFunctionAsync("() => window.location.pathname === '/navigation-lab'");
        await page.WaitForSelectorAsync("[data-automation-id='showcase-page-navigation-lab']");

        var labTitle = await page.Locator("[data-automation-id='mirage-navigation-title']").InnerTextAsync();
        Assert.Contains("Navigation Lab", labTitle, StringComparison.Ordinal);

        var labBackDisplay = await ReadCssPropertyAsync(page, "[data-automation-id='mirage-navigation-back-button']", "display");
        Assert.NotEqual("none", labBackDisplay);

        await page.EvaluateAsync(
            "() => (document.querySelector('[data-automation-id=\"mirage-navigation-back-button\"]'))?.click()");
        await page.WaitForFunctionAsync("() => window.location.pathname === '/'");
        await page.WaitForSelectorAsync("[data-automation-id='showcase-root']");

        var returnedTitle = await page.Locator("[data-automation-id='mirage-navigation-title']").InnerTextAsync();
        Assert.Contains("Mirage Showcase", returnedTitle, StringComparison.Ordinal);
        var returnedBackDisplay = await ReadCssPropertyAsync(page, "[data-automation-id='mirage-navigation-back-button']", "display");
        Assert.Equal("none", returnedBackDisplay);
    }

    [Fact]
    public async Task NavigationChrome_ToolbarItems_RenderDeterministically_AndNavChromeTogglesWork()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 900);
        var page = await OpenAsync(context);

        await page.Locator("[data-automation-id='showcase-open-navigation-lab']").ClickAsync();
        await page.WaitForFunctionAsync("() => window.location.pathname === '/navigation-lab'");
        await page.WaitForSelectorAsync("[data-automation-id='showcase-page-navigation-lab']");
        await page.WaitForSelectorAsync("[data-automation-id='mirage-navigation-toolbar-item-1']");
        await page.WaitForSelectorAsync("[data-automation-id='mirage-navigation-toolbar-item-2']");
        await page.WaitForSelectorAsync("[data-automation-id='mirage-navigation-toolbar-item-3']");

        var toolbarItemOneText = await page.Locator("[data-automation-id='mirage-navigation-toolbar-item-1']").InnerTextAsync();
        var toolbarItemTwoText = await page.Locator("[data-automation-id='mirage-navigation-toolbar-item-2']").InnerTextAsync();
        var toolbarItemThreeText = await page.Locator("[data-automation-id='mirage-navigation-toolbar-item-3']").InnerTextAsync();
        Assert.Contains("Toolbar +1", toolbarItemOneText, StringComparison.Ordinal);
        Assert.Contains("Toggle Bar", toolbarItemTwoText, StringComparison.Ordinal);
        Assert.Contains("Toggle Back", toolbarItemThreeText, StringComparison.Ordinal);

        await page.EvaluateAsync(
            "() => document.querySelector('[data-automation-id=\"navigation-lab-toggle-nav-bar\"]')?.dispatchEvent(new MouseEvent('click', { bubbles: true }))");
        await page.WaitForFunctionAsync(
            "() => getComputedStyle(document.querySelector('[data-automation-id=\"mirage-navigation-bar\"]')).display === 'none'");

        await page.EvaluateAsync(
            "() => document.querySelector('[data-automation-id=\"navigation-lab-toggle-nav-bar\"]')?.dispatchEvent(new MouseEvent('click', { bubbles: true }))");
        await page.WaitForFunctionAsync(
            "() => getComputedStyle(document.querySelector('[data-automation-id=\"mirage-navigation-bar\"]')).display !== 'none'");

        var backDisplayBeforeToggle = await ReadCssPropertyAsync(page, "[data-automation-id='mirage-navigation-back-button']", "display");
        Assert.NotEqual("none", backDisplayBeforeToggle);

        await page.EvaluateAsync(
            "() => document.querySelector('[data-automation-id=\"navigation-lab-toggle-back-button\"]')?.dispatchEvent(new MouseEvent('click', { bubbles: true }))");
        await page.WaitForFunctionAsync(
            "() => getComputedStyle(document.querySelector('[data-automation-id=\"mirage-navigation-back-button\"]')).display === 'none'");

        await page.EvaluateAsync(
            "() => document.querySelector('[data-automation-id=\"navigation-lab-bump-title\"]')?.dispatchEvent(new MouseEvent('click', { bubbles: true }))");
        await page.WaitForFunctionAsync(
            "() => (document.querySelector('[data-automation-id=\"mirage-navigation-title\"]')?.textContent ?? '').includes('#1')");
    }

    [Fact]
    public async Task NavigationLab_Refresh_PreservesCurrentVisiblePage()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 900);
        var page = await OpenAsync(
            context,
            path: "/projects/77?mode=details",
            waitForSelector: "[data-automation-id='showcase-page-project']");

        var beforePath = await page.EvaluateAsync<string>("() => window.location.pathname");
        Assert.Equal("/projects/77", beforePath);

        await page.ReloadAsync(new PageReloadOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await page.WaitForSelectorAsync("[data-automation-id='showcase-page-project']");

        var pathname = await page.EvaluateAsync<string>("() => window.location.pathname");
        Assert.Equal("/projects/77", pathname);
    }

    [Fact]
    public async Task NavigationLab_BrowserBackForward_SynchronizesVisiblePage()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 900);
        var page = await OpenAsync(context);
        var historyLengthBeforeOpen = await page.EvaluateAsync<int>("() => window.history.length");

        await page.EvaluateAsync(
            "() => document.querySelector('[data-automation-id=\"showcase-open-navigation-lab\"]')?.dispatchEvent(new MouseEvent('click', { bubbles: true }))");
        await page.WaitForSelectorAsync("[data-automation-id='showcase-page-navigation-lab']");
        var labPath = await page.EvaluateAsync<string>("() => window.location.pathname");
        Assert.Equal("/navigation-lab", labPath);
        var historyLengthAfterOpen = await page.EvaluateAsync<int>("() => window.history.length");
        Assert.True(historyLengthAfterOpen > historyLengthBeforeOpen, $"Expected pushState on navigation open. Before={historyLengthBeforeOpen}, After={historyLengthAfterOpen}");
        var historyLengthBeforeBack = await page.EvaluateAsync<int>("() => window.history.length");

        await page.GoBackAsync();
        await page.WaitForTimeoutAsync(500);
        var homePath = await page.EvaluateAsync<string>("() => window.location.pathname");
        Assert.Equal("/", homePath);
        await page.WaitForSelectorAsync("[data-automation-id='showcase-open-navigation-lab']");
        var historyLengthAfterBack = await page.EvaluateAsync<int>("() => window.history.length");
        Assert.True(historyLengthAfterBack >= historyLengthBeforeBack - 1);

        await page.GoForwardAsync();
        await page.WaitForFunctionAsync("() => window.location.pathname === '/navigation-lab'");
        await page.WaitForSelectorAsync("[data-automation-id='showcase-page-navigation-lab']");
        var forwardPath = await page.EvaluateAsync<string>("() => window.location.pathname");
        Assert.Equal("/navigation-lab", forwardPath);
    }

    [Fact]
    public async Task NavigationLab_MauiNavigation_UpdatesUrlWithoutFullReload()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 900);
        var page = await OpenAsync(context);

        var navigationEntriesBefore = await page.EvaluateAsync<int>(
            "(() => performance.getEntriesByType('navigation').length)");

        await page.EvaluateAsync(
            "() => document.querySelector('[data-automation-id=\"showcase-open-navigation-lab\"]')?.dispatchEvent(new MouseEvent('click', { bubbles: true }))");
        await page.WaitForSelectorAsync("[data-automation-id='showcase-page-navigation-lab']");
        await page.WaitForFunctionAsync("() => window.location.pathname === '/navigation-lab'");

        var navigationEntriesAfter = await page.EvaluateAsync<int>(
            "(() => performance.getEntriesByType('navigation').length)");
        Assert.Equal(navigationEntriesBefore, navigationEntriesAfter);
    }

    [Fact]
    public async Task NavigationLab_RapidBrowserBack_DoesNotDropPendingPopState()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 900);
        var page = await OpenAsync(context);

        await page.EvaluateAsync(
            "() => document.querySelector('[data-automation-id=\"showcase-open-navigation-lab\"]')?.dispatchEvent(new MouseEvent('click', { bubbles: true }))");
        await page.WaitForFunctionAsync("() => window.location.pathname === '/navigation-lab'");
        await page.WaitForSelectorAsync("[data-automation-id='showcase-page-navigation-lab']");

        await page.Locator("[data-automation-id='navigation-lab-push-project-101']").ClickAsync();
        await page.WaitForFunctionAsync("() => window.location.pathname === '/projects/101'");
        await page.WaitForSelectorAsync("[data-automation-id='showcase-page-project']");

        await page.Locator("[data-automation-id='showcase-project-push-next']").ClickAsync();
        await page.WaitForFunctionAsync("() => window.location.pathname === '/projects/102'");

        await page.EvaluateAsync("() => { history.back(); history.back(); }");
        await page.WaitForFunctionAsync("() => window.location.pathname === '/navigation-lab'");
    }

    [Fact]
    public async Task LayoutLab_ContentViewScenarios_ShowCenteredAndStretchBehavior()
    {
        await using var context = await _fixture.NewContextAsync(width: 1400, height: 960);
        var page = await OpenLayoutLabAsync(context);

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
        var page = await OpenLayoutLabAsync(context);

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
        var page = await OpenLayoutLabAsync(context);

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
        var page = await OpenLayoutLabAsync(context);

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
        var page = await OpenLayoutLabAsync(context);

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
        var page = await OpenLayoutLabAsync(context);

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

    private async Task<IPage> OpenAsync(
        IBrowserContext context,
        string? path = null,
        string waitForSelector = "[data-automation-id='hero-headline']")
    {
        if (_fixture.HttpAppUrl is null)
        {
            throw new InvalidOperationException("Fixture app URL is not initialized.");
        }

        var page = await context.NewPageAsync();
        var diagnostics = new ConcurrentQueue<string>();
        AttachDiagnostics(page, diagnostics);
        var targetUrl = path is null
            ? _fixture.HttpAppUrl.ToString()
            : new Uri(_fixture.HttpAppUrl, path).ToString();

        await page.GotoAsync(targetUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        try
        {
            await page.WaitForSelectorAsync(waitForSelector);
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

    private Task<IPage> OpenLayoutLabAsync(IBrowserContext context)
    {
        return OpenAsync(
            context,
            path: "/layout-lab",
            waitForSelector: "[data-automation-id='showcase-page-layout-lab']");
    }

    private static void AttachDiagnostics(IPage page, ConcurrentQueue<string> diagnostics)
    {
        page.Console += (_, message) =>
        {
            diagnostics.Enqueue($"[console:{message.Type}] {message.Text}");
            AppendDiagnosticsLine(message.Text);
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

    private static void AppendDiagnosticsLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)
            || string.IsNullOrWhiteSpace(s_diagnosticsLogFile)
            || !line.Contains("[Mirage.Diagnostics]", StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            lock (s_diagnosticsLogSync)
            {
                File.AppendAllText(s_diagnosticsLogFile, line + Environment.NewLine);
            }
        }
        catch
        {
            // Diagnostics file output is best-effort and must never fail the test.
        }
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

}
