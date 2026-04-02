using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Microsoft.Playwright;
using Xunit;

namespace Mirage.E2E.Tests;

public sealed class ControlsSampleFluentE2ETests : IClassFixture<ControlsSampleHostFixture>
{
    private static readonly string[] ExpectedCollectionViewReachablePageHosts =
    [
        "controls-sample-page-adaptivecollectionview",
        "controls-sample-page-alternatelayoutgallery",
        "controls-sample-page-basicgrouping",
        "controls-sample-page-chatexample",
        "controls-sample-page-collectionviewgallery",
        "controls-sample-page-datatemplategallery",
        "controls-sample-page-defaulttextgallery",
        "controls-sample-page-dynamicitemsizegallery",
        "controls-sample-page-emptyviewgallery",
        "controls-sample-page-emptyviewloadsimulategallery",
        "controls-sample-page-emptyviewnullgallery",
        "controls-sample-page-footeronlystring",
        "controls-sample-page-gridgrouping",
        "controls-sample-page-groupedreorderinggallery",
        "controls-sample-page-groupinggallery",
        "controls-sample-page-groupingnotemplates",
        "controls-sample-page-groupingplusselection",
        "controls-sample-page-headerfootergallery",
        "controls-sample-page-headerfootergrid",
        "controls-sample-page-headerfootergridhorizontal",
        "controls-sample-page-headerfooterstring",
        "controls-sample-page-headerfootertemplate",
        "controls-sample-page-headerfooterview",
        "controls-sample-page-itemssizegallery",
        "controls-sample-page-itemsspacinggallery",
        "controls-sample-page-itemsupdatingscrollmodegallery",
        "controls-sample-page-multipleboundselection",
        "controls-sample-page-nestedcollectionviewgallery",
        "controls-sample-page-observablecodecollectionviewgallery",
        "controls-sample-page-observablecollectiongallery",
        "controls-sample-page-observablecollectionresetgallery",
        "controls-sample-page-observablemultiitemcollectionviewgallery",
        "controls-sample-page-onlineimages",
        "controls-sample-page-preselecteditemgallery",
        "controls-sample-page-preselecteditemsgallery",
        "controls-sample-page-propagatecodegallery",
        "controls-sample-page-propagationgallery",
        "controls-sample-page-reorderinggallery",
        "controls-sample-page-scrollmodegallery",
        "controls-sample-page-scrollmodetestgallery",
        "controls-sample-page-scrolltocodegallery",
        "controls-sample-page-scrolltogallery",
        "controls-sample-page-scrolltogroup",
        "controls-sample-page-selectionchangedcommandparameter",
        "controls-sample-page-selectiongallery",
        "controls-sample-page-selectionmodegallery",
        "controls-sample-page-selectionsynchronization",
        "controls-sample-page-singleboundselection",
        "controls-sample-page-snappointscodegallery",
        "controls-sample-page-snappointsgallery",
        "controls-sample-page-someemptygroups",
        "controls-sample-page-spacinggallery",
        "controls-sample-page-switchgrouping",
        "controls-sample-page-templatecodecollectionviewgallery",
        "controls-sample-page-templatecodecollectionviewgridgallery",
        "controls-sample-page-textcodecollectionviewgallery",
        "controls-sample-page-textcodecollectionviewgridgallery",
        "controls-sample-page-ungroupedreorderinggallery",
        "controls-sample-page-variablesizetemplategridgallery",
        "controls-sample-page-variedsizedatatemplateselectorgallery",
        "controls-sample-page-visualstatesgallery"
    ];

    private static readonly string[] DeferredCollectionViewButtonAutomationIds =
    [
        "controls-sample-collectionviewgallery-open-carouselview-galleries",
        "controls-sample-collectionviewgallery-open-datatemplateselector",
        "controls-sample-collectionviewgallery-open-filter-items",
        "controls-sample-collectionviewgallery-open-emptyview-string",
        "controls-sample-collectionviewgallery-open-emptyview-view",
        "controls-sample-collectionviewgallery-open-emptyview-template-view",
        "controls-sample-collectionviewgallery-open-emptyview-swap-emptyview",
        "controls-sample-collectionviewgallery-open-emptyview-data-template-selector",
        "controls-sample-collectionviewgallery-open-emptyview-rtl",
        "controls-sample-collectionviewgallery-open-filterable-single-selection"
    ];

    private static readonly string[] ExpectedHomeCategoryAutomationIds =
    [
        "controls-sample-open-category-layouts",
        "controls-sample-open-category-controls",
        "controls-sample-open-category-core",
        "controls-sample-open-category-parityprobes",
        "controls-sample-open-category-compatibility"
    ];

    private static readonly string[] KnownCategoryIds =
    [
        "layouts",
        "controls",
        "core",
        "parityprobes",
        "compatibility"
    ];

    private static readonly IReadOnlyDictionary<string, string> EntryCategoryById =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["absolutelayoutpage"] = "layouts",
            ["layoutspage"] = "layouts",
            ["gridpage"] = "layouts",
            ["layoutupdatespage"] = "layouts",
            ["relativelayoutpage"] = "layouts",
            ["horizontalstacklayoutpage"] = "layouts",
            ["stacklayoutpage"] = "layouts",
            ["verticalstacklayoutpage"] = "layouts",
            ["templatedviewpage"] = "layouts",
            ["zindexpage"] = "layouts",
            ["scrollviewpage"] = "layouts",
            ["clippingpage"] = "layouts",
            ["contentviewpage"] = "layouts",
            ["customlayoutpage"] = "layouts",
            ["flexlayoutpage"] = "layouts",
            ["layoutisenabledpage"] = "layouts",

            ["activityindicatorpage"] = "controls",
            ["boxviewpage"] = "controls",
            ["buttonpage"] = "controls",
            ["checkboxpage"] = "controls",
            ["editorpage"] = "controls",
            ["entrypage"] = "controls",
            ["imagepage"] = "controls",
            ["sliderpage"] = "controls",
            ["stepperpage"] = "controls",
            ["mapsgallery"] = "controls",
            ["collectionviewgallery"] = "controls",

            ["clippage"] = "core",
            ["alertspage"] = "core",
            ["brushespage"] = "core",
            ["devicepage"] = "core",
            ["contentpagegallery"] = "core",
            ["transformationspage"] = "core",
            ["borderpage"] = "core",

            ["absolutelayoutparitypage"] = "parityprobes",
            ["carouselviewparitypage"] = "parityprobes",
            ["imagebuttonparitypage"] = "parityprobes",
            ["indicatorviewparitypage"] = "parityprobes",
            ["shapesparitypage"] = "parityprobes",
            ["editorparitypage"] = "parityprobes",
            ["radiobuttonparitypage"] = "parityprobes",
            ["pickerparitypage"] = "parityprobes",
            ["progressbarparitypage"] = "parityprobes",
            ["graphicsviewparitypage"] = "parityprobes",
            ["refreshviewparitypage"] = "parityprobes",
            ["timepickerparitypage"] = "parityprobes",
            ["datepickerparitypage"] = "parityprobes",

            ["tabbedpagegallerymainpage"] = "compatibility"
        };

    private readonly ControlsSampleHostFixture _fixture;

    public ControlsSampleFluentE2ETests(ControlsSampleHostFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task BrowserCatalog_ShowsRunnableImportedPagesOnly_AndNavigates()
    {
        await using var context = await _fixture.NewContextAsync();
        var (page, diagnostics) = await OpenAsync(context);

        foreach (var categoryAutomationId in ExpectedHomeCategoryAutomationIds)
        {
            Assert.Equal(1, await page.Locator($"[data-automation-id='{categoryAutomationId}']").CountAsync());
        }

        foreach (var entryId in EntryCategoryById.Keys)
        {
            Assert.Equal(0, await page.Locator($"[data-automation-id='controls-sample-open-{entryId}']").CountAsync());
        }
        Assert.Equal(0, await page.Locator("[data-automation-id='controls-sample-open-contentpagebackgroundgallery']").CountAsync());
        Assert.Equal(0, await page.Locator("[data-automation-id='controls-sample-open-blazorpage']").CountAsync());

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "gridpage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-gridpage']");
            });

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='mirage-navigation-back-button']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-category-layouts']");
                await page.Locator("[data-automation-id='mirage-navigation-back-button']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-home']");
            });

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "contentviewpage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-contentviewpage']");
            });

        var manifestStatus = await page.EvaluateAsync<int>(
            "async () => { const response = await fetch('/_mirage/assets/images.manifest.v1.json', { cache: 'no-store' }); return response.status; }");
        Assert.Equal(200, manifestStatus);

        var manifestLookup = await page.EvaluateAsync<string>(
            """
            async () => {
                const response = await fetch('/_mirage/assets/images.manifest.v1.json', { cache: 'no-store' });
                const json = await response.json();
                const key = 'ControlsSample.Shared:dotnet_bot.png';
                const imageUrl = json?.images?.[key] ?? '';
                return imageUrl;
            }
            """);

        Assert.False(string.IsNullOrWhiteSpace(manifestLookup));
        Assert.StartsWith("/_mirage/assets/ControlsSample.Shared/", manifestLookup, StringComparison.Ordinal);

        var dotnetBotStatus = await page.EvaluateAsync<int>(
            "async (url) => { const response = await fetch(url, { cache: 'no-store' }); return response.status; }",
            manifestLookup);
        Assert.Equal(200, dotnetBotStatus);

        var dotnetBotPngSignature = await page.EvaluateAsync<string>(
            "async (url) => { const response = await fetch(url, { cache: 'no-store' }); const bytes = new Uint8Array(await response.arrayBuffer()); return Array.from(bytes.slice(0, 4)).map(b => b.toString(16).padStart(2, '0')).join(''); }",
            manifestLookup);
        Assert.Equal("89504e47", dotnetBotPngSignature);

        var contentImageSources = await page.EvaluateAsync<string[]>(
            """
            () => Array
                .from(document.querySelectorAll("[data-automation-id='controls-sample-page-contentviewpage'] img"))
                .map(img => img.getAttribute("src") ?? "")
            """);
        var contentPageImages = page.Locator("[data-automation-id='controls-sample-page-contentviewpage'] img[src*='/_mirage/assets/']");
        var hostLogs = _fixture.GetHostLogsSnapshot();
        var diagnosticsSnapshot = RenderDiagnostics(diagnostics);
        Assert.True(
            await contentPageImages.CountAsync() > 0,
            $"Expected ContentViewPage to render card images from the Mirage bundled-image asset path. " +
            $"Found sources: [{string.Join(", ", contentImageSources)}]\nHost logs:\n{hostLogs}\nPage diagnostics:\n{diagnosticsSnapshot}");

        var iconHostOverflow = await page.EvaluateAsync<string>(
            """
            () => {
                const image = document.querySelector("[data-automation-id='controls-sample-page-contentviewpage'] img[src*='/_mirage/assets/']");
                const iconHost = image?.parentElement;
                if (!iconHost) {
                    return "__missing__";
                }

                return getComputedStyle(iconHost).overflow;
            }
            """);

        Assert.Equal(
            "hidden",
            iconHostOverflow);

        var iconHostBorderRadius = await page.EvaluateAsync<string>(
            """
            () => {
                const image = document.querySelector("[data-automation-id='controls-sample-page-contentviewpage'] img[src*='/_mirage/assets/']");
                const iconHost = image?.parentElement;
                if (!iconHost) {
                    return "__missing__";
                }

                return getComputedStyle(iconHost).borderRadius;
            }
            """);

        Assert.NotEqual("0px", iconHostBorderRadius);

        var overlapProbeTagName = await page.EvaluateAsync<string>(
            """
            () => {
                const description = Array
                    .from(document.querySelectorAll("[data-automation-id='controls-sample-page-contentviewpage'] span"))
                    .find(span => (span.textContent ?? "").includes("Lorem ipsum"));
                if (!description) {
                    return "__missing_description__";
                }

                const rect = description.getBoundingClientRect();
                const probeX = rect.left + Math.min(12, Math.max(1, rect.width * 0.15));
                const probeY = rect.top + Math.min(12, Math.max(1, rect.height * 0.35));
                const topElement = document.elementFromPoint(probeX, probeY);
                return topElement?.tagName ?? "__missing_probe__";
            }
            """);

        Assert.NotEqual(
            "IMG",
            overlapProbeTagName);

        var avatarAndTitleMetrics = await page.EvaluateAsync<string[]>(
            """
            () => {
                const image = document.querySelector("[data-automation-id='controls-sample-page-contentviewpage'] img[src*='/_mirage/assets/']");
                const iconHost = image?.parentElement;
                const grid = iconHost?.parentElement;
                const title = grid?.children?.item(1);
                if (!image || !iconHost || !title) {
                    return ["__missing__", "0", "0", "0", "0", "0", "0"];
                }

                const imageRect = image.getBoundingClientRect();
                const iconRect = iconHost.getBoundingClientRect();
                const titleRect = title.getBoundingClientRect();
                return [
                    "ok",
                    imageRect.width.toString(),
                    imageRect.height.toString(),
                    (iconRect.top + (iconRect.height / 2)).toString(),
                    (titleRect.top + (titleRect.height / 2)).toString(),
                    getComputedStyle(image).marginLeft,
                    getComputedStyle(image).marginTop
                ];
            }
            """);

        Assert.Equal("ok", avatarAndTitleMetrics[0]);
        Assert.True(double.Parse(avatarAndTitleMetrics[1], CultureInfo.InvariantCulture) >= 95d);
        Assert.True(double.Parse(avatarAndTitleMetrics[2], CultureInfo.InvariantCulture) >= 95d);
        Assert.True(
            Math.Abs(
                double.Parse(avatarAndTitleMetrics[3], CultureInfo.InvariantCulture)
                - double.Parse(avatarAndTitleMetrics[4], CultureInfo.InvariantCulture)) <= 8d);
        Assert.Equal("-20px", avatarAndTitleMetrics[5]);
        Assert.Equal("-20px", avatarAndTitleMetrics[6]);

        var dividerMetrics = await page.EvaluateAsync<string[]>(
            """
            () => {
                const image = document.querySelector("[data-automation-id='controls-sample-page-contentviewpage'] img[src*='/_mirage/assets/']");
                const iconHost = image?.parentElement;
                const grid = iconHost?.parentElement;
                const divider = grid?.children?.item(2);
                if (!divider) {
                    return ["__missing__", "", "", "0", "0"];
                }

                const style = getComputedStyle(divider);
                const rect = divider.getBoundingClientRect();
                return [
                    divider.tagName,
                    style.backgroundColor ?? "",
                    style.display ?? "",
                    rect.height.toString(),
                    rect.width.toString()
                ];
            }
            """);

        Assert.NotEqual("__missing__", dividerMetrics[0]);
        Assert.NotEqual("rgba(0, 0, 0, 0)", dividerMetrics[1]);
        Assert.NotEqual("none", dividerMetrics[2]);
        Assert.True(double.Parse(dividerMetrics[3], CultureInfo.InvariantCulture) >= 1d);
        Assert.True(double.Parse(dividerMetrics[4], CultureInfo.InvariantCulture) >= 120d);
    }

    [Fact]
    public async Task BrowserCatalog_GroupsEntriesIntoCategoryPages()
    {
        await using var context = await _fixture.NewContextAsync();
        var (page, diagnostics) = await OpenAsync(context);

        var categoryMatrix = new (string CategoryId, string[] PresentEntryIds, string[] AbsentEntryIds)[]
        {
            ("layouts", ["gridpage", "scrollviewpage"], ["buttonpage", "indicatorviewparitypage"]),
            ("controls", ["buttonpage", "mapsgallery"], ["gridpage", "borderpage"]),
            ("core", ["brushespage", "devicepage", "borderpage"], ["sliderpage", "editorparitypage"]),
            ("parityprobes", ["editorparitypage", "pickerparitypage", "progressbarparitypage", "graphicsviewparitypage", "refreshviewparitypage", "timepickerparitypage", "carouselviewparitypage", "datepickerparitypage"], ["gridpage", "boxviewpage"]),
            ("compatibility", ["tabbedpagegallerymainpage"], ["gridpage", "shapesparitypage"])
        };

        foreach (var (categoryId, presentEntryIds, absentEntryIds) in categoryMatrix)
        {
            await OpenCatalogCategoryAsync(page, diagnostics, categoryId);

            foreach (var entryId in presentEntryIds)
            {
                Assert.Equal(1, await page.Locator($"[data-automation-id='controls-sample-open-{entryId}']").CountAsync());
            }

            foreach (var entryId in absentEntryIds)
            {
                Assert.Equal(0, await page.Locator($"[data-automation-id='controls-sample-open-{entryId}']").CountAsync());
            }

            await NavigateWithDiagnosticsAsync(
                page,
                diagnostics,
                async () =>
                {
                    await page.Locator("[data-automation-id='mirage-navigation-back-button']").ClickAsync();
                    await page.WaitForSelectorAsync("[data-automation-id='controls-sample-home']");
                });
        }
    }

    [Fact]
    public async Task AbsoluteLayoutParityProbe_UpdatesBoundsFlagsAndZOrderAtRuntime()
    {
        await using var context = await _fixture.NewContextAsync(width: 1180, height: 860);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "absolutelayoutparitypage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-absolutelayoutparitypage']");
            });

        const string primarySelector = "[data-automation-id='controls-sample-absolutelayout-parity-a']";
        var initialPrimaryRect = await ReadBoundingClientRectAsync(page, primarySelector);

        await page.Locator("[data-automation-id='controls-sample-absolutelayout-parity-toggle-bounds']").ClickAsync();
        var updatedPrimaryRect = await WaitForBoundingClientRectChangeAsync(page, primarySelector, initialPrimaryRect);

        var boundsChanged = AnyBoundingClientRectDimensionChanged(initialPrimaryRect, updatedPrimaryRect);
        Assert.True(boundsChanged);

        await page.Locator("[data-automation-id='controls-sample-absolutelayout-parity-toggle-flags']").ClickAsync();
        var proportionalPrimaryRect = await WaitForBoundingClientRectChangeAsync(page, primarySelector, updatedPrimaryRect);

        var flagsChangedGeometry = AnyBoundingClientRectDimensionChanged(updatedPrimaryRect, proportionalPrimaryRect);
        Assert.True(flagsChangedGeometry);

        var topBeforeToggle = await ReadParityProbeOverlapTopAutomationIdAsync(page);

        await page.Locator("[data-automation-id='controls-sample-absolutelayout-parity-toggle-zindex']").ClickAsync();
        var topAfterToggle = await WaitForParityProbeOverlapTopAutomationIdChangeAsync(page, topBeforeToggle);

        Assert.Equal("controls-sample-absolutelayout-parity-a", topAfterToggle);
        Assert.NotEqual(topBeforeToggle, topAfterToggle);
    }

    [Fact]
    public async Task IndicatorViewParityProbe_ProjectsDotsAndInteractions()
    {
        await using var context = await _fixture.NewContextAsync(width: 1120, height: 860);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "indicatorviewparitypage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-indicatorviewparitypage']");
            });

        const string hostSelector = "[data-automation-id='controls-sample-indicatorview-parity-host']";
        await page.WaitForSelectorAsync(hostSelector);
        Assert.Equal("IndicatorView", await page.GetAttributeAsync(hostSelector, "data-mirage-kind"));

        Assert.Equal(5, await CountIndicatorDotsAsync(page, hostSelector));

        await page.Locator("[data-automation-id='controls-sample-indicatorview-parity-toggle-max-visible']").ClickAsync();
        await WaitForIndicatorDotCountAsync(page, hostSelector, expectedCount: 3);

        await page.Locator($"{hostSelector} .mirage-indicator-dot").Nth(2).ClickAsync();
        await WaitForLabelTextContainsAsync(
            page,
            "[data-automation-id='controls-sample-indicatorview-parity-position']",
            "position=2");

        await page.Locator("[data-automation-id='controls-sample-indicatorview-parity-set-count-one']").ClickAsync();
        await WaitForIndicatorDotCountAsync(page, hostSelector, expectedCount: 1);

        await page.Locator("[data-automation-id='controls-sample-indicatorview-parity-toggle-hide-single']").ClickAsync();
        await WaitForIndicatorDotCountAsync(page, hostSelector, expectedCount: 0);

        await page.Locator("[data-automation-id='controls-sample-indicatorview-parity-toggle-hide-single']").ClickAsync();
        await WaitForIndicatorDotCountAsync(page, hostSelector, expectedCount: 1);

        await page.Locator("[data-automation-id='controls-sample-indicatorview-parity-toggle-template']").ClickAsync();
        await WaitForLabelTextContainsAsync(
            page,
            "[data-automation-id='controls-sample-indicatorview-parity-mode']",
            "template=on");
        await WaitForIndicatorDotCountAsync(page, hostSelector, expectedCount: 0);

        await page.Locator("[data-automation-id='controls-sample-indicatorview-parity-toggle-template']").ClickAsync();
        await WaitForLabelTextContainsAsync(
            page,
            "[data-automation-id='controls-sample-indicatorview-parity-mode']",
            "template=off");
        await WaitForIndicatorDotCountAsync(page, hostSelector, expectedCount: 1);
    }

    [Fact]
    public async Task CarouselViewParityProbe_ProjectsSnapStateIndicatorAndLayoutUpdates()
    {
        await using var context = await _fixture.NewContextAsync(width: 1240, height: 900);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "carouselviewparitypage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-carouselviewparitypage']");
            });

        const string hostSelector = "[data-automation-id='controls-sample-carouselview-parity-host']";
        const string indicatorSelector = "[data-automation-id='controls-sample-carouselview-parity-indicator']";
        const string positionSelector = "[data-automation-id='controls-sample-carouselview-parity-position']";
        const string modeSelector = "[data-automation-id='controls-sample-carouselview-parity-mode']";

        await page.WaitForSelectorAsync(hostSelector);
        Assert.Equal("ContentContainer", await page.GetAttributeAsync(hostSelector, "data-mirage-kind"));
        await WaitForLabelTextContainsAsync(page, positionSelector, "position=0 current=Alpha");
        await WaitForIndicatorDotCountAsync(page, indicatorSelector, expectedCount: 4);

        await page.Locator("[data-automation-id='controls-sample-carouselview-parity-toggle-peek']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, modeSelector, "peek=12,0,36,0");
        Assert.Equal(
            "12px",
            await page.EvaluateAsync<string>(
                """
                (selector) => getComputedStyle(document.querySelector(selector)).scrollPaddingLeft
                """,
                hostSelector));
        Assert.Equal(
            "36px",
            await page.EvaluateAsync<string>(
                """
                (selector) => getComputedStyle(document.querySelector(selector)).scrollPaddingRight
                """,
                hostSelector));

        await page.Locator("[data-automation-id='controls-sample-carouselview-parity-animate-gamma']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, positionSelector, "position=2 current=Gamma");
        await page.WaitForFunctionAsync(
            """
            (args) => {
                const host = document.querySelector(args.selector);
                if (!(host instanceof HTMLElement)) {
                    return false;
                }

                const items = Array.from(host.querySelectorAll("*"))
                    .filter((child) => child instanceof HTMLElement && getComputedStyle(child).scrollSnapAlign === "center")
                    .map((child) => child);
                const target = items[args.targetIndex];
                if (!(target instanceof HTMLElement)) {
                    return false;
                }

                const hostRect = host.getBoundingClientRect();
                const targetRect = target.getBoundingClientRect();
                const leftPadding = Number.parseFloat(getComputedStyle(host).scrollPaddingLeft || "0") || 0;
                const rightPadding = Number.parseFloat(getComputedStyle(host).scrollPaddingRight || "0") || 0;
                const leadingInset = targetRect.left - hostRect.left;
                const trailingInset = hostRect.right - targetRect.right;
                return Math.abs(leadingInset - leftPadding) <= 2
                    && Math.abs(trailingInset - rightPadding) <= 2;
            }
            """,
            new { selector = hostSelector, targetIndex = 2 });
        var animatedGammaInsets = await page.EvaluateAsync<double[]>(
            """
            (args) => {
                const host = document.querySelector(args.selector);
                if (!(host instanceof HTMLElement)) {
                    return [-1, -1, -1, -1];
                }

                const items = Array.from(host.querySelectorAll("*"))
                    .filter((child) => child instanceof HTMLElement && getComputedStyle(child).scrollSnapAlign === "center")
                    .map((child) => child);
                const target = items[args.targetIndex];
                if (!(target instanceof HTMLElement)) {
                    return [-1, -1, -1, -1];
                }

                const hostRect = host.getBoundingClientRect();
                const targetRect = target.getBoundingClientRect();
                const leftPadding = Number.parseFloat(getComputedStyle(host).scrollPaddingLeft || "0") || 0;
                const rightPadding = Number.parseFloat(getComputedStyle(host).scrollPaddingRight || "0") || 0;
                const leadingInset = targetRect.left - hostRect.left;
                const trailingInset = hostRect.right - targetRect.right;
                return [leadingInset, trailingInset, leftPadding, rightPadding];
            }
            """,
            new { selector = hostSelector, targetIndex = 2 });
        Assert.InRange(animatedGammaInsets[0], animatedGammaInsets[2] - 2, animatedGammaInsets[2] + 2);
        Assert.InRange(animatedGammaInsets[1], animatedGammaInsets[3] - 2, animatedGammaInsets[3] + 2);

        await page.Locator($"{indicatorSelector} .mirage-indicator-dot").Nth(1).ClickAsync();
        await WaitForLabelTextContainsAsync(page, positionSelector, "position=1 current=Beta");

        await page.Locator("[data-automation-id='controls-sample-carouselview-parity-toggle-swipe']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, modeSelector, "swipe=False");
        await page.Locator("[data-automation-id='controls-sample-carouselview-parity-next-position']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, positionSelector, "position=2 current=Gamma");

        await page.Locator("[data-automation-id='controls-sample-carouselview-parity-toggle-swipe']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, modeSelector, "swipe=True");
        await page.Locator("[data-automation-id='controls-sample-carouselview-parity-toggle-orientation']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, modeSelector, "orientation=Vertical");
        await page.Locator("[data-automation-id='controls-sample-carouselview-parity-reset-alpha']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, positionSelector, "position=0 current=Alpha");
        var verticalScrollHeight = await page.EvaluateAsync<int>(
            """
            (selector) => {
                const host = document.querySelector(selector);
                if (!(host instanceof HTMLElement)) {
                    return 0;
                }

                return host.scrollHeight - host.clientHeight;
            }
            """,
            hostSelector);
        Assert.True(verticalScrollHeight > 0);

        await page.EvaluateAsync(
            """
            (selector) => {
                const host = document.querySelector(selector);
                if (!(host instanceof HTMLElement)) {
                    return;
                }

                host.scrollTop = host.clientHeight;
                host.dispatchEvent(new Event("scroll"));
            }
            """,
            hostSelector);
        await WaitForLabelTextContainsAsync(page, positionSelector, "position=1 current=Beta");
    }

    [Fact]
    public async Task ShapesParityProbe_ProjectsGeometryPaintAndAspectUpdates()
    {
        await using var context = await _fixture.NewContextAsync(width: 1260, height: 900);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "shapesparitypage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-shapesparitypage']");
            });

        var shapeSelector = ShapeSelectorFor("ellipse");
        await page.WaitForSelectorAsync(shapeSelector);
        Assert.Equal("ShapeView", await page.GetAttributeAsync(shapeSelector, "data-mirage-kind"));

        var aspectBefore = await ReadShapePathAttributeAsync(page, shapeSelector, "preserveAspectRatio");
        await page.Locator("[data-automation-id='controls-sample-shapes-parity-toggle-aspect']").ClickAsync();
        var aspectAfter = await WaitForShapePathAttributeChangeAsync(page, shapeSelector, "preserveAspectRatio", aspectBefore);
        Assert.NotEqual(aspectBefore, aspectAfter);

        var fillBefore = await ReadShapePathAttributeAsync(page, shapeSelector, "fill");
        await page.Locator("[data-automation-id='controls-sample-shapes-parity-toggle-fill']").ClickAsync();
        var fillValue = await WaitForShapePathAttributeChangeAsync(page, shapeSelector, "fill", fillBefore);
        Assert.StartsWith("url(#", fillValue, StringComparison.Ordinal);

        var strokeBefore = await ReadShapePathAttributeAsync(page, shapeSelector, "stroke");
        await page.Locator("[data-automation-id='controls-sample-shapes-parity-toggle-stroke']").ClickAsync();
        var strokeValue = await WaitForShapePathAttributeChangeAsync(page, shapeSelector, "stroke", strokeBefore);
        Assert.StartsWith("url(#", strokeValue, StringComparison.Ordinal);

        var dashBefore = await ReadShapePathAttributeAsync(page, shapeSelector, "stroke-dasharray");
        await page.Locator("[data-automation-id='controls-sample-shapes-parity-toggle-dash']").ClickAsync();
        var dashValue = await WaitForShapePathAttributeChangeAsync(page, shapeSelector, "stroke-dasharray", dashBefore);
        Assert.False(string.IsNullOrWhiteSpace(dashValue));

        await NavigateToShapeKindAsync(page, "Polygon");
        shapeSelector = ShapeSelectorFor("polygon");
        await page.WaitForSelectorAsync(shapeSelector);
        var polygonPathBefore = await WaitForShapePathAttributeValueAsync(page, shapeSelector, "d", timeoutMs: 10000);
        await page.Locator("[data-automation-id='controls-sample-shapes-parity-mutate-points']").ClickAsync();
        var polygonPathAfter = await WaitForShapePathAttributeChangeAsync(
            page,
            shapeSelector,
            "d",
            polygonPathBefore,
            timeoutMs: 10000,
            requireNonEmpty: true);
        Assert.False(string.IsNullOrWhiteSpace(polygonPathAfter));
        Assert.NotEqual(polygonPathBefore, polygonPathAfter);
        await WaitForLabelTextContainsAsync(
            page,
            "[data-automation-id='controls-sample-shapes-parity-geometry-state']",
            "points=alt");

        await NavigateToShapeKindAsync(page, "Path");
        shapeSelector = ShapeSelectorFor("path");
        await page.WaitForSelectorAsync(shapeSelector);
        var pathDataBefore = await WaitForShapePathAttributeValueAsync(page, shapeSelector, "d", timeoutMs: 10000);
        await page.Locator("[data-automation-id='controls-sample-shapes-parity-mutate-path-data']").ClickAsync();
        var pathDataAfter = await WaitForShapePathAttributeChangeAsync(
            page,
            shapeSelector,
            "d",
            pathDataBefore,
            timeoutMs: 10000,
            requireNonEmpty: true);
        Assert.False(string.IsNullOrWhiteSpace(pathDataAfter));
        Assert.NotEqual(pathDataBefore, pathDataAfter);
        await WaitForLabelTextContainsAsync(
            page,
            "[data-automation-id='controls-sample-shapes-parity-geometry-state']",
            "pathData=alt");

        var pathTransformBefore = await ReadShapePathAttributeAsync(page, shapeSelector, "transform");
        await page.Locator("[data-automation-id='controls-sample-shapes-parity-mutate-path-transform']").ClickAsync();
        var pathTransformAfter = await WaitForShapePathAttributeChangeAsync(
            page,
            shapeSelector,
            "transform",
            pathTransformBefore,
            timeoutMs: 10000,
            requireNonEmpty: true);
        Assert.Contains("translate(", pathTransformAfter, StringComparison.Ordinal);
        await WaitForLabelTextContainsAsync(
            page,
            "[data-automation-id='controls-sample-shapes-parity-geometry-state']",
            "pathTransform=on");
    }

    [Fact]
    public async Task EditorParityProbe_ProjectsTextSelectionAndCompletionBehavior()
    {
        await using var context = await _fixture.NewContextAsync(width: 1160, height: 860);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "editorparitypage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-editorparitypage']");
            });

        const string hostSelector = "[data-automation-id='controls-sample-editor-parity-host']";
        const string inputSelector = "[data-automation-id='controls-sample-editor-parity-host'] textarea.mirage-editor-input";

        await page.WaitForSelectorAsync(hostSelector);
        await page.WaitForSelectorAsync(inputSelector);
        Assert.Equal("Editor", await page.GetAttributeAsync(hostSelector, "data-mirage-kind"));

        await page.Locator("[data-automation-id='controls-sample-editor-parity-set-text-b']").ClickAsync();
        await WaitForEditorValueContainsAsync(page, inputSelector, "second sample text");

        await page.Locator("[data-automation-id='controls-sample-editor-parity-toggle-transform']").ClickAsync();
        await WaitForEditorValueContainsAsync(page, inputSelector, "SECOND SAMPLE TEXT");
        await WaitForLabelTextContainsAsync(
            page,
            "[data-automation-id='controls-sample-editor-parity-state']",
            "transform=Uppercase");

        await page.Locator("[data-automation-id='controls-sample-editor-parity-set-selection']").ClickAsync();
        var selection = await ReadSelectionRangeAsync(page, inputSelector);
        Assert.Equal(1, selection.start);
        Assert.Equal(5, selection.end);

        await page.Locator("[data-automation-id='controls-sample-editor-parity-toggle-maxlength']").ClickAsync();
        await WaitForLabelTextContainsAsync(
            page,
            "[data-automation-id='controls-sample-editor-parity-state']",
            "max=8");

        await page.Locator("[data-automation-id='controls-sample-editor-parity-toggle-enabled']").ClickAsync();
        await WaitForLabelTextContainsAsync(
            page,
            "[data-automation-id='controls-sample-editor-parity-state']",
            "enabled=False");
        Assert.Equal("disabled", await page.GetAttributeAsync(inputSelector, "disabled"));

        await page.Locator("[data-automation-id='controls-sample-editor-parity-toggle-enabled']").ClickAsync();
        await WaitForLabelTextContainsAsync(
            page,
            "[data-automation-id='controls-sample-editor-parity-state']",
            "enabled=True");

        await page.Locator("[data-automation-id='controls-sample-editor-parity-focus-editor']").ClickAsync();
        await page.Locator("[data-automation-id='controls-sample-editor-parity-blur-target']").ClickAsync();
        await WaitForLabelTextContainsAsync(
            page,
            "[data-automation-id='controls-sample-editor-parity-completed']",
            "completed=1");
    }

    [Fact]
    public async Task RadioButtonParityProbe_ProjectsGroupSelectionContentStyleAndEnabledGating()
    {
        await using var context = await _fixture.NewContextAsync(width: 1160, height: 860);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "radiobuttonparitypage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-radiobuttonparitypage']");
            });

        const string namedMercuryHost = "[data-automation-id='controls-sample-radiobutton-parity-named-mercury']";
        const string namedVenusHost = "[data-automation-id='controls-sample-radiobutton-parity-named-venus']";
        const string namedEarthHost = "[data-automation-id='controls-sample-radiobutton-parity-named-earth']";
        const string parentBetaHost = "[data-automation-id='controls-sample-radiobutton-parity-parent-beta']";
        const string selectedValueLabel = "[data-automation-id='controls-sample-radiobutton-parity-selected-value']";
        const string parentSelectedLabel = "[data-automation-id='controls-sample-radiobutton-parity-parent-selected']";
        const string styleLabel = "[data-automation-id='controls-sample-radiobutton-parity-style-state']";

        await page.WaitForSelectorAsync(namedMercuryHost);
        Assert.Equal("RadioButton", await page.GetAttributeAsync(namedMercuryHost, "data-mirage-kind"));

        await WaitForLabelTextContainsAsync(page, selectedValueLabel, "namedSelected=venus");

        await page.Locator($"{namedMercuryHost} input.mirage-radiobutton-input").ClickAsync();
        await WaitForLabelTextContainsAsync(page, selectedValueLabel, "namedSelected=mercury");

        await page.Locator($"{namedEarthHost} input.mirage-radiobutton-input").ClickAsync();
        await WaitForLabelTextContainsAsync(page, selectedValueLabel, "namedSelected=earth");

        await page.Locator($"{parentBetaHost} input.mirage-radiobutton-input").ClickAsync();
        await WaitForLabelTextContainsAsync(page, parentSelectedLabel, "parentSelected=beta");

        await page.Locator("[data-automation-id='controls-sample-radiobutton-parity-toggle-content']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, styleLabel, "contentMode=view");

        await page.Locator("[data-automation-id='controls-sample-radiobutton-parity-toggle-style']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, styleLabel, "style=alt");

        await page.Locator("[data-automation-id='controls-sample-radiobutton-parity-toggle-enabled']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, styleLabel, "enabled=False");

        await page.EvaluateAsync(
            "selector => { document.querySelector(selector)?.click(); }",
            $"{namedVenusHost} input.mirage-radiobutton-input");
        await WaitForLabelTextContainsAsync(page, selectedValueLabel, "namedSelected=earth");

        await page.Locator("[data-automation-id='controls-sample-radiobutton-parity-toggle-enabled']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, styleLabel, "enabled=True");
        await page.Locator($"{namedVenusHost} input.mirage-radiobutton-input").ClickAsync();
        await WaitForLabelTextContainsAsync(page, selectedValueLabel, "namedSelected=venus");
    }

    [Fact]
    public async Task PickerParityProbe_ProjectsSelectionPlaceholderItemsSourceAndOpenState()
    {
        await using var context = await _fixture.NewContextAsync(width: 1160, height: 860);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "pickerparitypage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-pickerparitypage']");
            });

        const string hostSelector = "[data-automation-id='controls-sample-picker-parity-host']";
        const string itemsHostSelector = "[data-automation-id='controls-sample-picker-parity-items-host']";
        const string selectionStateSelector = "[data-automation-id='controls-sample-picker-parity-selection-state']";
        const string openStateSelector = "[data-automation-id='controls-sample-picker-parity-open-state']";
        const string styleStateSelector = "[data-automation-id='controls-sample-picker-parity-style-state']";
        const string itemsStateSelector = "[data-automation-id='controls-sample-picker-parity-items-state']";

        await page.WaitForSelectorAsync(hostSelector);
        Assert.Equal("Picker", await page.GetAttributeAsync(hostSelector, "data-mirage-kind"));
        Assert.Equal("Picker", await page.GetAttributeAsync(itemsHostSelector, "data-mirage-kind"));
        Assert.Equal("Choose planet", await page.GetAttributeAsync(hostSelector, "aria-label"));

        var initialPrimaryOptions = await page.EvaluateAsync<string[]>(
            """
            (selector) => {
                const select = document.querySelector(selector);
                if (!(select instanceof HTMLSelectElement)) {
                    return [];
                }

                return Array.from(select.options)
                    .map(option => `${option.text}|${option.value}|${option.disabled}|${option.selected}`);
            }
            """,
            hostSelector);
        Assert.Equal("Choose planet||true|true", initialPrimaryOptions[0]);
        Assert.Equal("Mercury|0|false|false", initialPrimaryOptions[1]);
        Assert.Equal("Venus|1|false|false", initialPrimaryOptions[2]);
        Assert.Equal("Earth|2|false|false", initialPrimaryOptions[3]);

        var initialPrimaryColor = await page.EvaluateAsync<string>(
            "selector => getComputedStyle(document.querySelector(selector)).color",
            hostSelector);
        Assert.Contains("192", initialPrimaryColor, StringComparison.Ordinal);

        await page.Locator("[data-automation-id='controls-sample-picker-parity-select-earth']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, selectionStateSelector, "selectedIndex=2 selectedItem=Earth");
        await WaitForInputValueContainsAsync(page, hostSelector, "2");

        var selectedPrimaryColor = await page.EvaluateAsync<string>(
            "selector => getComputedStyle(document.querySelector(selector)).color",
            hostSelector);
        Assert.Contains("17", selectedPrimaryColor, StringComparison.Ordinal);

        await page.Locator("[data-automation-id='controls-sample-picker-parity-open']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, openStateSelector, "isOpen=True");

        await page.Locator("[data-automation-id='controls-sample-picker-parity-close']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, openStateSelector, "isOpen=False");

        await page.Locator("[data-automation-id='controls-sample-picker-parity-toggle-enabled']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, openStateSelector, "enabled=False");
        Assert.Equal("disabled", await page.GetAttributeAsync(hostSelector, "disabled"));

        await page.EvaluateAsync(
            """
            (selector) => {
                const select = document.querySelector(selector);
                if (!(select instanceof HTMLSelectElement)) {
                    return;
                }

                select.value = "1";
                select.dispatchEvent(new Event("change", { bubbles: true }));
            }
            """,
            hostSelector);
        await WaitForLabelTextContainsAsync(page, selectionStateSelector, "selectedIndex=2 selectedItem=Earth");

        await page.Locator("[data-automation-id='controls-sample-picker-parity-toggle-enabled']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, openStateSelector, "enabled=True");
        await page.EvaluateAsync(
            """
            (selector) => {
                const select = document.querySelector(selector);
                if (!(select instanceof HTMLSelectElement)) {
                    return;
                }

                select.value = "1";
                select.dispatchEvent(new Event("change", { bubbles: true }));
            }
            """,
            hostSelector);
        await WaitForLabelTextContainsAsync(page, selectionStateSelector, "selectedIndex=1 selectedItem=Venus");
        await WaitForInputValueContainsAsync(page, hostSelector, "1");

        await page.Locator("[data-automation-id='controls-sample-picker-parity-toggle-style']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, styleStateSelector, "style=alt");
        await WaitForLabelTextContainsAsync(page, styleStateSelector, "hAlign=End");
        await WaitForLabelTextContainsAsync(page, styleStateSelector, "vAlign=End");

        var styledTextAlign = await page.EvaluateAsync<string>(
            "selector => document.querySelector(selector)?.style.textAlign ?? ''",
            hostSelector);
        Assert.Equal("end", styledTextAlign);
        var styledBackground = await page.EvaluateAsync<string>(
            "selector => getComputedStyle(document.querySelector(selector)).backgroundColor",
            hostSelector);
        Assert.Contains("219", styledBackground, StringComparison.Ordinal);

        var initialItemsOptions = await page.EvaluateAsync<string[]>(
            """
            (selector) => {
                const select = document.querySelector(selector);
                if (!(select instanceof HTMLSelectElement)) {
                    return [];
                }

                return Array.from(select.options).map(option => option.text);
            }
            """,
            itemsHostSelector);
        Assert.Equal("Choose object planet", initialItemsOptions[0]);
        Assert.Equal("Planet Mercury", initialItemsOptions[1]);
        Assert.Equal("Planet Venus", initialItemsOptions[2]);
        Assert.Equal("Planet Earth", initialItemsOptions[3]);
        await WaitForLabelTextContainsAsync(page, itemsStateSelector, "binding=Display");
        await WaitForLabelTextContainsAsync(page, itemsStateSelector, "selectedItem=Venus");

        await page.Locator("[data-automation-id='controls-sample-picker-parity-toggle-binding']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, itemsStateSelector, "binding=Name");

        var reboundOptions = await page.EvaluateAsync<string[]>(
            """
            (selector) => {
                const select = document.querySelector(selector);
                if (!(select instanceof HTMLSelectElement)) {
                    return [];
                }

                return Array.from(select.options).map(option => option.text);
            }
            """,
            itemsHostSelector);
        Assert.Equal("Mercury", reboundOptions[1]);
        Assert.Equal("Venus", reboundOptions[2]);
        Assert.Equal("Earth", reboundOptions[3]);

        await page.EvaluateAsync(
            """
            (selector) => {
                const select = document.querySelector(selector);
                if (!(select instanceof HTMLSelectElement)) {
                    return;
                }

                select.value = "2";
                select.dispatchEvent(new Event("change", { bubbles: true }));
            }
            """,
            itemsHostSelector);
        await WaitForLabelTextContainsAsync(page, itemsStateSelector, "selectedItem=Earth");

        await page.Locator("[data-automation-id='controls-sample-picker-parity-replace-source']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, itemsStateSelector, "source=alt");
        await WaitForLabelTextContainsAsync(page, itemsStateSelector, "selectedItem=Jupiter");

        var replacedOptions = await page.EvaluateAsync<string[]>(
            """
            (selector) => {
                const select = document.querySelector(selector);
                if (!(select instanceof HTMLSelectElement)) {
                    return [];
                }

                return Array.from(select.options).map(option => option.text);
            }
            """,
            itemsHostSelector);
        Assert.Equal("Mars", replacedOptions[1]);
        Assert.Equal("Jupiter", replacedOptions[2]);
        Assert.Equal("Saturn", replacedOptions[3]);

        await page.Locator("[data-automation-id='controls-sample-picker-parity-select-last-item']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, itemsStateSelector, "selectedItem=Saturn");
        await WaitForInputValueContainsAsync(page, itemsHostSelector, "2");
    }

    [Fact]
    public async Task TimePickerParityProbe_ProjectsTimeFormatOpenAndEnabledState()
    {
        await using var context = await _fixture.NewContextAsync(width: 1160, height: 860);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "timepickerparitypage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-timepickerparitypage']");
            });

        const string hostSelector = "[data-automation-id='controls-sample-timepicker-parity-host']";
        const string timeStateSelector = "[data-automation-id='controls-sample-timepicker-parity-time-state']";
        const string formatStateSelector = "[data-automation-id='controls-sample-timepicker-parity-format-state']";
        const string openStateSelector = "[data-automation-id='controls-sample-timepicker-parity-open-state']";

        await page.WaitForSelectorAsync(hostSelector);
        Assert.Equal("TimePicker", await page.GetAttributeAsync(hostSelector, "data-mirage-kind"));

        await WaitForLabelTextContainsAsync(page, formatStateSelector, "step=60");
        await page.Locator("[data-automation-id='controls-sample-timepicker-parity-toggle-seconds']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, formatStateSelector, "step=1");
        await WaitForInputValueContainsAsync(page, hostSelector, "09:30:00");

        await page.Locator("[data-automation-id='controls-sample-timepicker-parity-set-144520']").ClickAsync();
        await WaitForInputValueContainsAsync(page, hostSelector, "14:45:20");
        await WaitForLabelTextContainsAsync(page, timeStateSelector, "time=14:45:20");

        await page.Locator("[data-automation-id='controls-sample-timepicker-parity-open']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, openStateSelector, "isOpen=True");

        await page.Locator("[data-automation-id='controls-sample-timepicker-parity-close']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, openStateSelector, "isOpen=False");

        await page.Locator("[data-automation-id='controls-sample-timepicker-parity-toggle-enabled']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, openStateSelector, "enabled=False");
        Assert.Equal("disabled", await page.GetAttributeAsync(hostSelector, "disabled"));

        await page.EvaluateAsync(
            """
            (selector) => {
                const input = document.querySelector(selector);
                if (!(input instanceof HTMLInputElement)) {
                    return;
                }

                input.value = "07:11:12";
                input.dispatchEvent(new Event("change", { bubbles: true }));
            }
            """,
            hostSelector);
        await WaitForLabelTextContainsAsync(page, timeStateSelector, "time=14:45:20");

        await page.Locator("[data-automation-id='controls-sample-timepicker-parity-toggle-enabled']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, openStateSelector, "enabled=True");
        await page.EvaluateAsync(
            """
            (selector) => {
                const input = document.querySelector(selector);
                if (!(input instanceof HTMLInputElement)) {
                    return;
                }

                input.value = "07:11:12";
                input.dispatchEvent(new Event("change", { bubbles: true }));
            }
            """,
            hostSelector);
        await WaitForLabelTextContainsAsync(page, timeStateSelector, "time=07:11:12");
    }

    [Fact]
    public async Task DatePickerParityProbe_ProjectsDateBoundsFormatOpenAndEnabledState()
    {
        await using var context = await _fixture.NewContextAsync(width: 1160, height: 860);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "datepickerparitypage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-datepickerparitypage']");
            });

        const string hostSelector = "[data-automation-id='controls-sample-datepicker-parity-host']";
        const string dateStateSelector = "[data-automation-id='controls-sample-datepicker-parity-date-state']";
        const string boundsStateSelector = "[data-automation-id='controls-sample-datepicker-parity-bounds-state']";
        const string formatStateSelector = "[data-automation-id='controls-sample-datepicker-parity-format-state']";
        const string openStateSelector = "[data-automation-id='controls-sample-datepicker-parity-open-state']";

        await page.WaitForSelectorAsync(hostSelector);
        Assert.Equal("DatePicker", await page.GetAttributeAsync(hostSelector, "data-mirage-kind"));
        await WaitForInputValueContainsAsync(page, hostSelector, "2026-04-02");

        await page.Locator("[data-automation-id='controls-sample-datepicker-parity-set-20260412']").ClickAsync();
        await WaitForInputValueContainsAsync(page, hostSelector, "2026-04-12");
        await WaitForLabelTextContainsAsync(page, dateStateSelector, "date=2026-04-12");

        await page.Locator("[data-automation-id='controls-sample-datepicker-parity-clear-date']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, dateStateSelector, "date=null");
        Assert.Equal(string.Empty, await page.InputValueAsync(hostSelector));

        await page.Locator("[data-automation-id='controls-sample-datepicker-parity-toggle-bounds']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, boundsStateSelector, "min=2026-04-05");
        await WaitForLabelTextContainsAsync(page, boundsStateSelector, "max=2026-04-20");
        Assert.Equal("2026-04-05", await page.GetAttributeAsync(hostSelector, "min"));
        Assert.Equal("2026-04-20", await page.GetAttributeAsync(hostSelector, "max"));

        await page.Locator("[data-automation-id='controls-sample-datepicker-parity-toggle-format']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, formatStateSelector, "format=D");
        Assert.Equal("D", await page.GetAttributeAsync(hostSelector, "data-maui-date-format"));

        await page.Locator("[data-automation-id='controls-sample-datepicker-parity-open']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, openStateSelector, "isOpen=True");

        await page.Locator("[data-automation-id='controls-sample-datepicker-parity-close']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, openStateSelector, "isOpen=False");

        await page.Locator("[data-automation-id='controls-sample-datepicker-parity-toggle-enabled']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, openStateSelector, "enabled=False");
        Assert.Equal("disabled", await page.GetAttributeAsync(hostSelector, "disabled"));

        await page.EvaluateAsync(
            """
            (selector) => {
                const input = document.querySelector(selector);
                if (!(input instanceof HTMLInputElement)) {
                    return;
                }

                input.value = "2026-04-18";
                input.dispatchEvent(new Event("change", { bubbles: true }));
            }
            """,
            hostSelector);
        await WaitForLabelTextContainsAsync(page, dateStateSelector, "date=null");

        await page.Locator("[data-automation-id='controls-sample-datepicker-parity-toggle-enabled']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, openStateSelector, "enabled=True");
        await page.EvaluateAsync(
            """
            (selector) => {
                const input = document.querySelector(selector);
                if (!(input instanceof HTMLInputElement)) {
                    return;
                }

                input.value = "2026-04-18";
                input.dispatchEvent(new Event("change", { bubbles: true }));
            }
            """,
            hostSelector);
        await WaitForLabelTextContainsAsync(page, dateStateSelector, "date=2026-04-18");
        await WaitForLabelTextContainsAsync(page, openStateSelector, "isOpen=False");
    }

    [Fact]
    public async Task ProgressBarParityProbe_ProjectsClampedValueAndStyleUpdates()
    {
        await using var context = await _fixture.NewContextAsync(width: 1160, height: 860);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "progressbarparitypage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-progressbarparitypage']");
            });

        const string hostSelector = "[data-automation-id='controls-sample-progressbar-parity-host']";
        const string progressStateSelector = "[data-automation-id='controls-sample-progressbar-parity-progress-state']";
        const string styleStateSelector = "[data-automation-id='controls-sample-progressbar-parity-style-state']";

        await page.WaitForSelectorAsync(hostSelector);
        Assert.Equal("ProgressBar", await page.GetAttributeAsync(hostSelector, "data-mirage-kind"));

        await WaitForLabelTextContainsAsync(page, progressStateSelector, "progress=0.25");
        var initialProgressValue = await page.EvaluateAsync<double>(
            """
            (selector) => {
                const progress = document.querySelector(selector);
                return progress instanceof HTMLProgressElement ? progress.value : -1;
            }
            """,
            hostSelector);
        Assert.Equal(0.25, initialProgressValue, 3);

        await page.Locator("[data-automation-id='controls-sample-progressbar-parity-increment']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, progressStateSelector, "progress=0.45");
        var incrementedProgressValue = await page.EvaluateAsync<double>(
            """
            (selector) => {
                const progress = document.querySelector(selector);
                return progress instanceof HTMLProgressElement ? progress.value : -1;
            }
            """,
            hostSelector);
        Assert.Equal(0.45, incrementedProgressValue, 3);

        await page.Locator("[data-automation-id='controls-sample-progressbar-parity-overshoot']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, progressStateSelector, "progress=1");
        var clampedProgressValue = await page.EvaluateAsync<double>(
            """
            (selector) => {
                const progress = document.querySelector(selector);
                return progress instanceof HTMLProgressElement ? progress.value : -1;
            }
            """,
            hostSelector);
        Assert.Equal(1, clampedProgressValue, 3);

        var baselineAccentColor = await page.EvaluateAsync<string>(
            "selector => document.querySelector(selector)?.style.accentColor ?? ''",
            hostSelector);

        await page.Locator("[data-automation-id='controls-sample-progressbar-parity-toggle-style']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, styleStateSelector, "style=alt");

        var alternateAccentColor = await page.EvaluateAsync<string>(
            "selector => document.querySelector(selector)?.style.accentColor ?? ''",
            hostSelector);
        Assert.NotEqual(baselineAccentColor, alternateAccentColor);

        await page.Locator("[data-automation-id='controls-sample-progressbar-parity-reset']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, progressStateSelector, "progress=0.25");
    }

    [Fact]
    public async Task GraphicsViewParityProbe_ProjectsCanvasDrawingAndInteractionState()
    {
        await using var context = await _fixture.NewContextAsync(width: 1160, height: 860);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "graphicsviewparitypage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-graphicsviewparitypage']");
            });

        const string hostSelector = "[data-automation-id='controls-sample-graphicsview-parity-host']";
        const string drawStateSelector = "[data-automation-id='controls-sample-graphicsview-parity-draw-state']";
        const string interactionStateSelector = "[data-automation-id='controls-sample-graphicsview-parity-interaction-state']";
        const string styleStateSelector = "[data-automation-id='controls-sample-graphicsview-parity-style-state']";
        const string sizeStateSelector = "[data-automation-id='controls-sample-graphicsview-parity-size-state']";

        static string CanvasPixelReadyScript() =>
            """
            (args) => {
                const canvas = document.querySelector(args.selector);
                if (!(canvas instanceof HTMLCanvasElement) || canvas.width <= 0 || canvas.height <= 0) {
                    return false;
                }

                const context = canvas.getContext("2d");
                if (!context) {
                    return false;
                }

                const x = Math.max(0, Math.min(canvas.width - 1, Math.round(canvas.width * args.xRatio)));
                const y = Math.max(0, Math.min(canvas.height - 1, Math.round(canvas.height * args.yRatio)));
                const pixel = context.getImageData(x, y, 1, 1).data;
                return pixel[3] > 0;
            }
            """;

        async Task<int[]> SampleCanvasPixelAsync(double xRatio, double yRatio)
        {
            return await page.EvaluateAsync<int[]>(
                """
                (args) => {
                    const canvas = document.querySelector(args.selector);
                    if (!(canvas instanceof HTMLCanvasElement) || canvas.width <= 0 || canvas.height <= 0) {
                        return [-1, -1, -1, -1];
                    }

                    const context = canvas.getContext("2d");
                    if (!context) {
                        return [-1, -1, -1, -1];
                    }

                    const x = Math.max(0, Math.min(canvas.width - 1, Math.round(canvas.width * args.xRatio)));
                    const y = Math.max(0, Math.min(canvas.height - 1, Math.round(canvas.height * args.yRatio)));
                    return Array.from(context.getImageData(x, y, 1, 1).data);
                }
                """,
                new
                {
                    selector = hostSelector,
                    xRatio,
                    yRatio
                });
        }

        async Task<int> GetCanvasRenderCountAsync()
        {
            return await page.EvaluateAsync<int>(
                """
                (selector) => {
                    const canvas = document.querySelector(selector);
                    if (!(canvas instanceof HTMLCanvasElement)) {
                        return -1;
                    }

                    const value = Number.parseInt(canvas.getAttribute("data-mirage-canvas-renders") ?? "0", 10);
                    return Number.isFinite(value) ? value : -1;
                }
                """,
                hostSelector);
        }

        async Task WaitForOpaqueCanvasRegionAsync(double leftRatio, double topRatio, double rightRatio, double bottomRatio)
        {
            await page.WaitForFunctionAsync(
                """
                (args) => {
                    const canvas = document.querySelector(args.selector);
                    if (!(canvas instanceof HTMLCanvasElement) || canvas.width <= 0 || canvas.height <= 0) {
                        return false;
                    }

                    const context = canvas.getContext("2d");
                    if (!context) {
                        return false;
                    }

                    const startX = Math.max(0, Math.min(canvas.width - 1, Math.floor(canvas.width * args.leftRatio)));
                    const endX = Math.max(startX, Math.min(canvas.width - 1, Math.ceil(canvas.width * args.rightRatio)));
                    const startY = Math.max(0, Math.min(canvas.height - 1, Math.floor(canvas.height * args.topRatio)));
                    const endY = Math.max(startY, Math.min(canvas.height - 1, Math.ceil(canvas.height * args.bottomRatio)));

                    for (let y = startY; y <= endY; y++) {
                        for (let x = startX; x <= endX; x++) {
                            if (context.getImageData(x, y, 1, 1).data[3] > 0) {
                                return true;
                            }
                        }
                    }

                    return false;
                }
                """,
                new
                {
                    selector = hostSelector,
                    leftRatio,
                    topRatio,
                    rightRatio,
                    bottomRatio
                });
        }

        await page.WaitForSelectorAsync(hostSelector);
        Assert.Equal("GraphicsView", await page.GetAttributeAsync(hostSelector, "data-mirage-kind"));
        await page.WaitForFunctionAsync(
            """
            (selector) => {
                const canvas = document.querySelector(selector);
                if (!(canvas instanceof HTMLCanvasElement)) {
                    return false;
                }

                const width = Number(canvas.getAttribute("data-mirage-canvas-width") ?? "");
                const height = Number(canvas.getAttribute("data-mirage-canvas-height") ?? "");
                return Number.isFinite(width) && width > 0 && Number.isFinite(height) && height > 0;
            }
            """,
            hostSelector);
        var initialCanvasMetrics = await page.EvaluateAsync<double[]>(
            """
            (selector) => {
                const canvas = document.querySelector(selector);
                if (!(canvas instanceof HTMLCanvasElement)) {
                    return [-1, -1];
                }

                return [
                    Number(canvas.getAttribute("data-mirage-canvas-width") ?? ""),
                    Number(canvas.getAttribute("data-mirage-canvas-height") ?? "")
                ];
            }
            """,
            hostSelector);
        Assert.True(initialCanvasMetrics[0] > 0);
        Assert.True(initialCanvasMetrics[1] > 0);
        await WaitForLabelTextContainsAsync(page, drawStateSelector, "mode=Square");
        await page.WaitForFunctionAsync(CanvasPixelReadyScript(), new
        {
            selector = hostSelector,
            xRatio = 0.30,
            yRatio = 0.58
        });

        var initialPixel = await SampleCanvasPixelAsync(0.30, 0.58);
        Assert.True(initialPixel[0] > initialPixel[2], "Expected the default square sample to be red-dominant.");
        var initialRenderCount = await GetCanvasRenderCountAsync();
        Assert.True(initialRenderCount >= 1);

        await page.Locator("[data-automation-id='controls-sample-graphicsview-parity-toggle-color']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, drawStateSelector, "2563EB");
        await page.WaitForFunctionAsync(
            """
            (args) => {
                const canvas = document.querySelector(args.selector);
                if (!(canvas instanceof HTMLCanvasElement)) {
                    return false;
                }

                const renderCount = Number.parseInt(canvas.getAttribute("data-mirage-canvas-renders") ?? "0", 10);
                return Number.isFinite(renderCount) && renderCount > args.previousRenderCount;
            }
            """,
            new
            {
                selector = hostSelector,
                previousRenderCount = initialRenderCount
            });

        var recoloredPixel = await SampleCanvasPixelAsync(0.30, 0.58);
        Assert.True(recoloredPixel[2] > recoloredPixel[0], "Expected the recolored square sample to be blue-dominant.");

        await page.Locator("[data-automation-id='controls-sample-graphicsview-parity-triangle']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, drawStateSelector, "mode=Triangle");
        await page.WaitForFunctionAsync(
            """
            (args) => {
                const canvas = document.querySelector(args.selector);
                if (!(canvas instanceof HTMLCanvasElement) || canvas.width <= 0 || canvas.height <= 0) {
                    return false;
                }

                const context = canvas.getContext("2d");
                if (!context) {
                    return false;
                }

                const x = Math.max(0, Math.min(canvas.width - 1, Math.round(canvas.width * args.xRatio)));
                const y = Math.max(0, Math.min(canvas.height - 1, Math.round(canvas.height * args.yRatio)));
                const pixel = context.getImageData(x, y, 1, 1).data;
                return pixel[3] === 0;
            }
            """,
            new
            {
                selector = hostSelector,
                xRatio = 0.30,
                yRatio = 0.58
            });

        await page.Locator("[data-automation-id='controls-sample-graphicsview-parity-text']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, drawStateSelector, "mode=Text");
        await WaitForOpaqueCanvasRegionAsync(0.32, 0.35, 0.68, 0.70);

        await page.Locator("[data-automation-id='controls-sample-graphicsview-parity-toggle-background']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, styleStateSelector, "background=alt");
        var alternateBackground = await page.EvaluateAsync<string>(
            "selector => getComputedStyle(document.querySelector(selector)).backgroundColor",
            hostSelector);
        Assert.Contains("254", alternateBackground, StringComparison.Ordinal);

        await page.Locator("[data-automation-id='controls-sample-graphicsview-parity-toggle-flow']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, styleStateSelector, "flow=RightToLeft");
        var direction = await page.EvaluateAsync<string>(
            "selector => getComputedStyle(document.querySelector(selector)).direction",
            hostSelector);
        Assert.Equal("rtl", direction);

        var preResizeRenderCount = await GetCanvasRenderCountAsync();
        await page.Locator("[data-automation-id='controls-sample-graphicsview-parity-toggle-size']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, sizeStateSelector, "request=160x120");
        await page.WaitForFunctionAsync(
            """
            (args) => {
                const canvas = document.querySelector(args.selector);
                if (!(canvas instanceof HTMLCanvasElement)) {
                    return false;
                }

                const renderCount = Number.parseInt(canvas.getAttribute("data-mirage-canvas-renders") ?? "0", 10);
                return Number.isFinite(renderCount) && renderCount > args.previousRenderCount;
            }
            """,
            new
            {
                selector = hostSelector,
                previousRenderCount = preResizeRenderCount
            });
        var compactCanvasMetrics = await page.EvaluateAsync<double[]>(
            """
            (selector) => {
                const canvas = document.querySelector(selector);
                if (!(canvas instanceof HTMLCanvasElement)) {
                    return [-1, -1];
                }

                return [
                    Number(canvas.getAttribute("data-mirage-canvas-width") ?? ""),
                    Number(canvas.getAttribute("data-mirage-canvas-height") ?? "")
                ];
            }
            """,
            hostSelector);
        Assert.True(compactCanvasMetrics[0] > 0);
        Assert.True(compactCanvasMetrics[1] > 0);
        Assert.False(string.IsNullOrWhiteSpace(await page.GetAttributeAsync(hostSelector, "data-mirage-canvas-dpr")));

        var canvasBox = await page.Locator(hostSelector).BoundingBoxAsync();
        Assert.NotNull(canvasBox);
        var insideX = canvasBox!.X + 24;
        var insideY = canvasBox.Y + 24;

        async Task WaitForHoverStateAsync()
        {
            await page.WaitForFunctionAsync(
                """
                (selector) => {
                    const label = document.querySelector(selector);
                    if (!label) {
                        return false;
                    }

                    const text = label.textContent ?? "";
                    return text.includes("interaction=hover-start") || text.includes("interaction=hover-move");
                }
                """,
                interactionStateSelector);
        }

        await page.Mouse.MoveAsync(Math.Max(0, canvasBox.X - 24), Math.Max(0, canvasBox.Y - 24));
        await page.Mouse.MoveAsync(insideX, insideY);
        await WaitForHoverStateAsync();

        await page.Mouse.MoveAsync(canvasBox.X + 56, canvasBox.Y + 44);
        await WaitForLabelTextContainsAsync(page, interactionStateSelector, "interaction=hover-move");

        await page.Mouse.DownAsync();
        await WaitForLabelTextContainsAsync(page, interactionStateSelector, "interaction=start");

        await page.Mouse.MoveAsync(canvasBox.X + 96, canvasBox.Y + 72);
        await WaitForLabelTextContainsAsync(page, interactionStateSelector, "interaction=drag");

        await page.Mouse.UpAsync();
        await WaitForLabelTextContainsAsync(page, interactionStateSelector, "interaction=end inside=True");

        await page.Mouse.MoveAsync(canvasBox.X + 40, canvasBox.Y + 36);
        await WaitForHoverStateAsync();

        await page.Mouse.MoveAsync(canvasBox.X - 10, canvasBox.Y - 10);
        await WaitForLabelTextContainsAsync(page, interactionStateSelector, "interaction=hover-end");

        await page.Mouse.MoveAsync(canvasBox.X + 48, canvasBox.Y + 40);
        await page.Mouse.DownAsync();
        await WaitForLabelTextContainsAsync(page, interactionStateSelector, "interaction=start");

        await page.EvaluateAsync(
            """
            (args) => {
                const canvas = document.querySelector(args.selector);
                if (!(canvas instanceof HTMLCanvasElement)) {
                    return;
                }

                const rect = canvas.getBoundingClientRect();
                canvas.dispatchEvent(new PointerEvent("pointercancel", {
                    bubbles: true,
                    pointerId: 1,
                    pointerType: "mouse",
                    clientX: rect.left + args.offsetX,
                    clientY: rect.top + args.offsetY
                }));
            }
            """,
            new
            {
                selector = hostSelector,
                offsetX = 48,
                offsetY = 40
            });
        await WaitForLabelTextContainsAsync(page, interactionStateSelector, "interaction=cancel");
        await page.Mouse.UpAsync();
    }

    [Fact]
    public async Task RefreshViewParityProbe_ProjectsTriggerAndPullRefreshAcrossContentModes()
    {
        await using var context = await _fixture.NewContextAsync(width: 1180, height: 900);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "refreshviewparitypage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-refreshviewparitypage']");
            });

        const string refreshSelector = "[data-automation-id='controls-sample-refreshview-parity-host']";
        const string triggerSelector = "[data-automation-id='controls-sample-refreshview-parity-host'] [data-maui-rv-trigger='true']";
        const string spinnerSelector = "[data-automation-id='controls-sample-refreshview-parity-host'] [data-maui-rv-spinner='true']";
        const string collectionScrollHostSelector = "[data-automation-id='controls-sample-refreshview-parity-host'] [data-maui-cv-host-role='scroll']";
        const string plainScrollHostSelector = "[data-automation-id='controls-sample-refreshview-parity-host'] [data-maui-rv-content-host='true']";
        const string refreshStateSelector = "[data-automation-id='controls-sample-refreshview-parity-refresh-state']";
        const string enabledStateSelector = "[data-automation-id='controls-sample-refreshview-parity-enabled-state']";
        const string modeStateSelector = "[data-automation-id='controls-sample-refreshview-parity-mode-state']";
        const string styleStateSelector = "[data-automation-id='controls-sample-refreshview-parity-style-state']";
        const string completeRefreshSelector = "[data-automation-id='controls-sample-refreshview-parity-complete-refresh']";
        const string toggleEnabledSelector = "[data-automation-id='controls-sample-refreshview-parity-toggle-enabled']";
        const string toggleRefreshEnabledSelector = "[data-automation-id='controls-sample-refreshview-parity-toggle-refresh-enabled']";
        const string toggleModeSelector = "[data-automation-id='controls-sample-refreshview-parity-toggle-mode']";
        const string toggleColorSelector = "[data-automation-id='controls-sample-refreshview-parity-toggle-color']";

        async Task DispatchClickAsync(string selector)
        {
            await page.EvaluateAsync(
                """
                (selector) => {
                    const element = document.querySelector(selector);
                    if (!element) {
                        return;
                    }

                    element.dispatchEvent(new Event("click", { bubbles: true }));
                }
                """,
                selector);
        }

        async Task<string?> ReadAttributeValueAsync(string selector, string attributeName)
        {
            return await page.GetAttributeAsync(selector, attributeName);
        }

        async Task<string> ReadComputedStyleAsync(string selector, string propertyName)
        {
            return await page.EvaluateAsync<string>(
                """
                (args) => {
                    const element = document.querySelector(args.selector);
                    if (!(element instanceof HTMLElement)) {
                        return "";
                    }

                    return getComputedStyle(element)[args.propertyName] ?? "";
                }
                """,
                new
                {
                    selector,
                    propertyName
                });
        }

        async Task PullDownRefreshAsync(string selector, double distance, bool resetScrollTop = true)
        {
            var bounds = await page.EvaluateAsync<double[]>(
                """
                (args) => {
                    const host = document.querySelector(args.selector);
                    if (!(host instanceof HTMLElement)) {
                        return [Number.NaN, Number.NaN, Number.NaN, Number.NaN];
                    }

                    if (args.resetScrollTop) {
                        host.scrollTop = 0;
                    }

                    const rect = host.getBoundingClientRect();
                    return [rect.x, rect.y, rect.width, rect.height];
                }
                """,
                new
                {
                    selector,
                    resetScrollTop
                });

            Assert.Equal(4, bounds.Length);
            Assert.True(
                !double.IsNaN(bounds[0]) &&
                !double.IsNaN(bounds[1]) &&
                !double.IsNaN(bounds[2]) &&
                !double.IsNaN(bounds[3]) &&
                bounds[2] > 0 &&
                bounds[3] > 0,
                $"Expected valid pull host bounds for selector {selector}.");

            var startX = (float)(bounds[0] + (bounds[2] / 2));
            var startY = (float)(bounds[1] + Math.Min(36, Math.Max(18, bounds[3] * 0.2)));
            await page.Mouse.MoveAsync(startX, startY);
            await page.Mouse.DownAsync();
            await page.Mouse.MoveAsync(
                startX,
                (float)(startY + Math.Max(20, distance)),
                new MouseMoveOptions { Steps = 14 });
        }

        async Task AssertCanScrollVerticallyAsync(string selector, string reason)
        {
            var metrics = await page.EvaluateAsync<double[]>(
                """
                (selector) => {
                    const host = document.querySelector(selector);
                    if (!(host instanceof HTMLElement)) {
                        return [Number.NaN, Number.NaN, Number.NaN];
                    }

                    host.scrollTop = 0;
                    const before = host.scrollTop;
                    host.scrollTop = 64;
                    const after = host.scrollTop;
                    return [host.clientHeight, host.scrollHeight, after - before];
                }
                """,
                selector);

            Assert.Equal(3, metrics.Length);
            Assert.True(
                !double.IsNaN(metrics[0]) &&
                !double.IsNaN(metrics[1]) &&
                !double.IsNaN(metrics[2]) &&
                metrics[1] > metrics[0] &&
                metrics[2] > 0,
                $"Expected vertical scrolling for {reason}. clientHeight={metrics[0]}, scrollHeight={metrics[1]}, delta={metrics[2]}");
        }

        async Task AssertRefreshInFlowAsync(string reason)
        {
            var metrics = await page.EvaluateAsync<double[]>(
                """
                () => {
                    const refresh = document.querySelector("[data-automation-id='controls-sample-refreshview-parity-host']");
                    const below = document.querySelector("[data-automation-id='controls-sample-refreshview-parity-refresh-state']");
                    if (!(refresh instanceof HTMLElement) || !(below instanceof HTMLElement)) {
                        return [Number.NaN, Number.NaN];
                    }

                    const refreshRect = refresh.getBoundingClientRect();
                    const belowRect = below.getBoundingClientRect();
                    return [refreshRect.bottom, belowRect.top];
                }
                """);

            Assert.Equal(2, metrics.Length);
            Assert.True(
                !double.IsNaN(metrics[0]) && !double.IsNaN(metrics[1]) && metrics[1] + 1 >= metrics[0],
                $"RefreshView should remain in normal flow ({reason}). refreshBottom={metrics[0]}, nextTop={metrics[1]}");
        }

        await page.WaitForSelectorAsync(refreshSelector);
        await page.Locator(refreshSelector).ScrollIntoViewIfNeededAsync();

        Assert.Equal("RefreshView", await page.GetAttributeAsync(refreshSelector, "data-mirage-kind"));
        await WaitForLabelTextContainsAsync(page, refreshStateSelector, "isRefreshing=False commands=0");
        await WaitForLabelTextContainsAsync(page, enabledStateSelector, "enabled=True refreshEnabled=True");
        await WaitForLabelTextContainsAsync(page, modeStateSelector, "mode=collection");
        Assert.Equal("false", await ReadAttributeValueAsync(refreshSelector, "data-maui-rv-refreshing"));
        Assert.Equal("true", await ReadAttributeValueAsync(refreshSelector, "data-maui-rv-enabled"));
        Assert.Equal("false", await ReadAttributeValueAsync(spinnerSelector, "data-maui-rv-spinner-active"));
        await AssertRefreshInFlowAsync("initial");

        var initialTriggerColor = await ReadComputedStyleAsync(triggerSelector, "color");

        await DispatchClickAsync(triggerSelector);
        await WaitForLabelTextContainsAsync(page, refreshStateSelector, "isRefreshing=True commands=1 lastParameter=collection");
        Assert.Equal("true", await ReadAttributeValueAsync(refreshSelector, "data-maui-rv-refreshing"));
        Assert.Equal("true", await ReadAttributeValueAsync(spinnerSelector, "data-maui-rv-spinner-active"));
        Assert.Equal("running", await ReadComputedStyleAsync(spinnerSelector, "animationPlayState"));

        await DispatchClickAsync(triggerSelector);
        var blockedWhileRefreshing = (await page.Locator(refreshStateSelector).TextContentAsync())?.Trim() ?? string.Empty;
        Assert.Contains("commands=1", blockedWhileRefreshing, StringComparison.Ordinal);

        await page.Locator(completeRefreshSelector).ClickAsync();
        await WaitForLabelTextContainsAsync(page, refreshStateSelector, "isRefreshing=False commands=1");
        Assert.Equal("false", await ReadAttributeValueAsync(refreshSelector, "data-maui-rv-refreshing"));
        Assert.Equal("paused", await ReadComputedStyleAsync(spinnerSelector, "animationPlayState"));

        await PullDownRefreshAsync(collectionScrollHostSelector, 132);
        Assert.Equal("true", await ReadAttributeValueAsync(refreshSelector, "data-maui-rv-pull-active"));
        Assert.Equal("true", await ReadAttributeValueAsync(refreshSelector, "data-maui-rv-pull-armed"));
        Assert.NotEqual("0", await ReadAttributeValueAsync(refreshSelector, "data-maui-rv-pull-distance"));
        await page.Mouse.UpAsync();
        await WaitForLabelTextContainsAsync(page, refreshStateSelector, "isRefreshing=True commands=2 lastParameter=collection");
        await page.Locator(completeRefreshSelector).ClickAsync();
        await WaitForLabelTextContainsAsync(page, refreshStateSelector, "isRefreshing=False commands=2");

        await page.Locator(toggleModeSelector).ClickAsync();
        await WaitForLabelTextContainsAsync(page, modeStateSelector, "mode=plain");
        await AssertCanScrollVerticallyAsync(plainScrollHostSelector, "plain RefreshView content viewport");
        await PullDownRefreshAsync(plainScrollHostSelector, 132);
        Assert.Equal("true", await ReadAttributeValueAsync(refreshSelector, "data-maui-rv-pull-active"));
        Assert.Equal("true", await ReadAttributeValueAsync(refreshSelector, "data-maui-rv-pull-armed"));
        await page.Mouse.UpAsync();
        await WaitForLabelTextContainsAsync(page, refreshStateSelector, "isRefreshing=True commands=3 lastParameter=plain");
        await page.Locator(completeRefreshSelector).ClickAsync();
        await WaitForLabelTextContainsAsync(page, refreshStateSelector, "isRefreshing=False commands=3");

        await page.Locator(toggleEnabledSelector).ClickAsync();
        await WaitForLabelTextContainsAsync(page, enabledStateSelector, "enabled=False refreshEnabled=True");
        Assert.Equal("false", await ReadAttributeValueAsync(refreshSelector, "data-maui-rv-enabled"));
        await DispatchClickAsync(triggerSelector);
        await PullDownRefreshAsync(plainScrollHostSelector, 132);
        Assert.Equal("false", await ReadAttributeValueAsync(refreshSelector, "data-maui-rv-pull-active"));
        Assert.Equal("false", await ReadAttributeValueAsync(refreshSelector, "data-maui-rv-pull-armed"));
        Assert.Equal("0", await ReadAttributeValueAsync(refreshSelector, "data-maui-rv-pull-distance"));
        await page.Mouse.UpAsync();
        var disabledState = (await page.Locator(refreshStateSelector).TextContentAsync())?.Trim() ?? string.Empty;
        Assert.Contains("commands=3", disabledState, StringComparison.Ordinal);

        await page.Locator(toggleEnabledSelector).ClickAsync();
        await WaitForLabelTextContainsAsync(page, enabledStateSelector, "enabled=True refreshEnabled=True");

        await page.Locator(toggleRefreshEnabledSelector).ClickAsync();
        await WaitForLabelTextContainsAsync(page, enabledStateSelector, "enabled=True refreshEnabled=False");
        Assert.Equal("false", await ReadAttributeValueAsync(refreshSelector, "data-maui-rv-enabled"));
        await DispatchClickAsync(triggerSelector);
        await PullDownRefreshAsync(plainScrollHostSelector, 132);
        Assert.Equal("false", await ReadAttributeValueAsync(refreshSelector, "data-maui-rv-pull-active"));
        Assert.Equal("false", await ReadAttributeValueAsync(refreshSelector, "data-maui-rv-pull-armed"));
        await page.Mouse.UpAsync();
        var refreshDisabledState = (await page.Locator(refreshStateSelector).TextContentAsync())?.Trim() ?? string.Empty;
        Assert.Contains("commands=3", refreshDisabledState, StringComparison.Ordinal);

        await page.Locator(toggleRefreshEnabledSelector).ClickAsync();
        await WaitForLabelTextContainsAsync(page, enabledStateSelector, "enabled=True refreshEnabled=True");

        await page.Locator(toggleColorSelector).ClickAsync();
        await WaitForLabelTextContainsAsync(page, styleStateSelector, "color=alt");
        var alternateTriggerColor = await ReadComputedStyleAsync(triggerSelector, "color");
        Assert.NotEqual(initialTriggerColor, alternateTriggerColor);

        await AssertRefreshInFlowAsync("final");
    }

    [Fact]
    public async Task ImageButtonParityProbe_ProjectsInteractionSourceAndStyleState()
    {
        await using var context = await _fixture.NewContextAsync(width: 1160, height: 860);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "imagebuttonparitypage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-imagebuttonparitypage']");
            });

        const string hostSelector = "[data-automation-id='controls-sample-imagebutton-parity-host']";
        const string eventsSelector = "[data-automation-id='controls-sample-imagebutton-parity-events']";
        const string projectionSelector = "[data-automation-id='controls-sample-imagebutton-parity-projection']";
        const string styleSelector = "[data-automation-id='controls-sample-imagebutton-parity-style']";

        await page.WaitForSelectorAsync(hostSelector);
        Assert.Equal("ImageButton", await page.GetAttributeAsync(hostSelector, "data-mirage-kind"));
        Assert.Equal("file", await page.GetAttributeAsync(hostSelector, "data-maui-image-source-kind"));
        Assert.Equal("Image button parity probe", await page.GetAttributeAsync(hostSelector, "aria-label"));
        Assert.Equal("Activates the parity probe image button", await page.GetAttributeAsync(hostSelector, "aria-description"));
        Assert.Equal(string.Empty, await page.Locator($"{hostSelector} img").GetAttributeAsync("alt"));
        await page.WaitForFunctionAsync(
            "(selector) => (document.querySelector(selector)?.getAttribute('data-maui-image-state') ?? '') === 'loaded'",
            hostSelector);

        var initialSource = await page.Locator($"{hostSelector} img").GetAttributeAsync("src");
        Assert.False(string.IsNullOrWhiteSpace(initialSource));
        await WaitForLabelTextContainsAsync(page, eventsSelector, "clicked=0");

        await page.Locator(hostSelector).ClickAsync();
        await WaitForLabelTextContainsAsync(page, eventsSelector, "pressed=1");
        await WaitForLabelTextContainsAsync(page, eventsSelector, "released=1");
        await WaitForLabelTextContainsAsync(page, eventsSelector, "clicked=1");

        await page.Locator("[data-automation-id='controls-sample-imagebutton-parity-toggle-source']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, projectionSelector, "source=dotnet_bot.png");
        await page.WaitForFunctionAsync(
            """
            (args) => {
                const current = document.querySelector(args.selector + " img")?.getAttribute("src") ?? "";
                return current !== "" && current !== args.previous;
            }
            """,
            new
            {
                selector = hostSelector,
                previous = initialSource!
            });
        await page.WaitForFunctionAsync(
            "(selector) => (document.querySelector(selector)?.getAttribute('data-maui-image-state') ?? '') === 'loaded'",
            hostSelector);

        await page.Locator("[data-automation-id='controls-sample-imagebutton-parity-toggle-aspect']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, projectionSelector, "aspect=AspectFill");
        var objectFit = await page.EvaluateAsync<string>(
            """
            (selector) => getComputedStyle(document.querySelector(selector + " img")).objectFit
            """,
            hostSelector);
        Assert.Equal("cover", objectFit);

        await page.Locator("[data-automation-id='controls-sample-imagebutton-parity-toggle-style']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, styleSelector, "borderWidth=4");
        var hostChrome = await page.EvaluateAsync<string[]>(
            """
            (selector) => {
                const style = getComputedStyle(document.querySelector(selector));
                return [style.borderWidth, style.borderRadius, style.padding];
            }
            """,
            hostSelector);
        Assert.Equal("4px", hostChrome[0]);
        Assert.Equal("30px", hostChrome[1]);
        Assert.Equal("10px 14px 18px 6px", hostChrome[2]);

        await page.Locator("[data-automation-id='controls-sample-imagebutton-parity-toggle-enabled']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, projectionSelector, "enabled=False");
        Assert.Equal("disabled", await page.GetAttributeAsync(hostSelector, "disabled"));
        await page.EvaluateAsync(
            "selector => { document.querySelector(selector)?.click(); }",
            hostSelector);
        await WaitForLabelTextContainsAsync(page, eventsSelector, "clicked=1");

        await page.Locator("[data-automation-id='controls-sample-imagebutton-parity-toggle-enabled']").ClickAsync();
        await WaitForLabelTextContainsAsync(page, projectionSelector, "enabled=True");
        await page.Locator(hostSelector).ClickAsync();
        await WaitForLabelTextContainsAsync(page, eventsSelector, "clicked=2");
    }

    [Fact]
    public async Task CustomLayoutPage_IsRunnable_AndAppliesDockedButtonArrangement()
    {
        await using var context = await _fixture.NewContextAsync(width: 1000, height: 760);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "customlayoutpage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-customlayoutpage']");
            });

        var layoutSnapshot = await page.EvaluateAsync<string[]>(
            """
            () => {
                const host = document.querySelector("[data-automation-id='controls-sample-page-customlayoutpage']");
                if (!host) {
                    return ["__missing__", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0"];
                }

                const buttons = Array.from(host.querySelectorAll("[data-mirage-kind='Button']"));
                const byText = text => buttons.filter(button => (button.textContent ?? "").trim() === text);
                const top = byText("Top")[0];
                const bottom = byText("Bottom")[0];
                const leftButtons = byText("Left");
                const rightButtons = byText("Right");
                if (!top || !bottom || leftButtons.length === 0 || rightButtons.length === 0) {
                    return ["__missing_buttons__", buttons.length.toString(), "0", "0", leftButtons.length.toString(), rightButtons.length.toString(), "0", "0", "0", "0", "0", "0", "0"];
                }

                const topRect = top.getBoundingClientRect();
                const bottomRect = bottom.getBoundingClientRect();
                const leftRect = leftButtons[0].getBoundingClientRect();
                const rightRect = rightButtons[0].getBoundingClientRect();

                return [
                    "ok",
                    buttons.length.toString(),
                    topRect.height.toString(),
                    bottomRect.height.toString(),
                    leftButtons.length.toString(),
                    rightButtons.length.toString(),
                    leftRect.x.toString(),
                    rightRect.x.toString(),
                    topRect.y.toString(),
                    leftRect.y.toString(),
                    bottomRect.y.toString(),
                    leftRect.width.toString(),
                    rightRect.width.toString()
                ];
            }
            """);

        Assert.Equal("ok", layoutSnapshot[0]);
        Assert.Equal("6", layoutSnapshot[1]);
        Assert.Equal("2", layoutSnapshot[4]);
        Assert.Equal("2", layoutSnapshot[5]);
        Assert.True(double.Parse(layoutSnapshot[2], CultureInfo.InvariantCulture) >= 0d);
        Assert.True(double.Parse(layoutSnapshot[3], CultureInfo.InvariantCulture) >= 0d);
        Assert.True(double.Parse(layoutSnapshot[6], CultureInfo.InvariantCulture) < double.Parse(layoutSnapshot[7], CultureInfo.InvariantCulture));
        Assert.True(double.Parse(layoutSnapshot[8], CultureInfo.InvariantCulture) <= double.Parse(layoutSnapshot[9], CultureInfo.InvariantCulture));
        Assert.True(double.Parse(layoutSnapshot[10], CultureInfo.InvariantCulture) >= double.Parse(layoutSnapshot[9], CultureInfo.InvariantCulture));
        Assert.True(double.Parse(layoutSnapshot[11], CultureInfo.InvariantCulture) >= 0d);
        Assert.True(double.Parse(layoutSnapshot[12], CultureInfo.InvariantCulture) >= 0d);
    }

    [Fact]
    public async Task HorizontalStackLayoutPage_IsRunnable_AndAllowsUnsizedBoxViewsToCollapse()
    {
        await using var context = await _fixture.NewContextAsync(width: 900, height: 700);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "horizontalstacklayoutpage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-horizontalstacklayoutpage']");
            });

        var layoutSnapshot = await page.EvaluateAsync<string[]>(
            """
            () => {
                const host = document.querySelector("[data-automation-id='controls-sample-page-horizontalstacklayoutpage']");
                const stack = host?.querySelector("[data-mirage-kind='HorizontalStack']");
                if (!host || !stack) {
                    return ["__missing__", "0", "0", "0", "0", "0"];
                }

                const label = stack.querySelector("[data-mirage-kind='Label']");
                const labelText = (label?.textContent ?? "").trim();
                const boxViews = Array.from(stack.querySelectorAll("[data-mirage-kind='BoxView']"));
                const boxRects = boxViews.map(box => box.getBoundingClientRect());
                const collapsedCount = boxRects.filter(rect => rect.width <= 1 || rect.height <= 1 || (rect.width * rect.height) <= 1).length;
                const maxHeight = boxRects.reduce((current, rect) => Math.max(current, rect.height), 0);
                const maxArea = boxRects.reduce((current, rect) => Math.max(current, rect.width * rect.height), 0);

                return [
                    "ok",
                    labelText,
                    boxViews.length.toString(),
                    collapsedCount.toString(),
                    maxHeight.toString(),
                    maxArea.toString()
                ];
            }
            """);

        Assert.Equal("ok", layoutSnapshot[0]);
        Assert.Equal("HorizontalStackLayout", layoutSnapshot[1]);
        Assert.Equal("6", layoutSnapshot[2]);
        Assert.Equal("6", layoutSnapshot[3]);
    }

    [Fact]
    public async Task FlexLayoutPage_IsRunnable_AndProjectsOrderGrowAndBasisSemantics()
    {
        await using var context = await _fixture.NewContextAsync(width: 1100, height: 800);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "flexlayoutpage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-flexlayoutpage']");
            });

        var layoutSnapshot = await page.EvaluateAsync<string[]>(
            """
            () => {
                const host = document.querySelector("[data-automation-id='controls-sample-page-flexlayoutpage']");
                const root = host?.querySelector(":scope > [data-mirage-kind='Layout']");
                if (!host || !root) {
                    return ["__missing__", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0"];
                }

                const header = Array.from(root.querySelectorAll(":scope > [data-mirage-kind='Label']"))
                    .find(label => (label.textContent ?? "").trim() === "HEADER");
                const footer = Array.from(root.querySelectorAll(":scope > [data-mirage-kind='Label']"))
                    .find(label => (label.textContent ?? "").trim() === "FOOTER");
                const body = root.querySelector(":scope > [data-mirage-kind='Layout']");
                const content = body
                    ? Array.from(body.querySelectorAll(":scope > [data-mirage-kind='Label']"))
                        .find(label => (label.textContent ?? "").trim() === "CONTENT")
                    : null;

                const bodyChildren = body ? Array.from(body.querySelectorAll(":scope > *")) : [];
                const boxViews = bodyChildren.filter(element => element.getAttribute("data-mirage-kind") === "BoxView");
                const sortedBoxes = boxViews
                    .map(element => ({
                        element,
                        rect: element.getBoundingClientRect(),
                        color: getComputedStyle(element).backgroundColor
                    }))
                    .sort((a, b) => a.rect.left - b.rect.left);

                if (!header || !footer || !body || !content || sortedBoxes.length < 2) {
                    return ["__missing_expected_nodes__", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0"];
                }

                const rootStyle = getComputedStyle(root);
                const bodyStyle = getComputedStyle(body);
                const contentRect = content.getBoundingClientRect();
                const firstBoxRect = sortedBoxes[0].rect;
                const secondBoxRect = sortedBoxes[1].rect;
                const contentCenterX = contentRect.left + (contentRect.width / 2);
                const bodyCenterY = contentRect.top + (contentRect.height / 2);

                return [
                    "ok",
                    rootStyle.flexDirection,
                    bodyStyle.flexDirection,
                    boxViews.length.toString(),
                    firstBoxRect.width.toString(),
                    firstBoxRect.height.toString(),
                    secondBoxRect.width.toString(),
                    secondBoxRect.height.toString(),
                    contentRect.width.toString(),
                    (firstBoxRect.left < contentCenterX).toString(),
                    (secondBoxRect.left > contentCenterX).toString(),
                    sortedBoxes[0].color,
                    sortedBoxes[1].color,
                    (Math.abs(firstBoxRect.top - bodyCenterY) < contentRect.height).toString()
                ];
            }
            """);

        Assert.Equal("ok", layoutSnapshot[0]);
        Assert.Equal("column", layoutSnapshot[1]);
        Assert.Equal("row", layoutSnapshot[2]);
        Assert.Equal("2", layoutSnapshot[3]);

        var firstWidth = double.Parse(layoutSnapshot[4], CultureInfo.InvariantCulture);
        var firstHeight = double.Parse(layoutSnapshot[5], CultureInfo.InvariantCulture);
        var secondWidth = double.Parse(layoutSnapshot[6], CultureInfo.InvariantCulture);
        var secondHeight = double.Parse(layoutSnapshot[7], CultureInfo.InvariantCulture);
        var contentWidth = double.Parse(layoutSnapshot[8], CultureInfo.InvariantCulture);

        Assert.InRange(firstWidth, 40d, 70d);
        Assert.InRange(secondWidth, 40d, 70d);
        Assert.True(firstHeight > 30d);
        Assert.True(secondHeight > 30d);
        Assert.True(contentWidth > 150d);
        Assert.Equal("true", layoutSnapshot[9]);
        Assert.Equal("true", layoutSnapshot[10]);
        Assert.Equal("true", layoutSnapshot[13]);
        Assert.Contains("0, 0, 255", layoutSnapshot[11], StringComparison.Ordinal);
        Assert.Contains("0, 128, 0", layoutSnapshot[12], StringComparison.Ordinal);
    }

    [Fact]
    public async Task VerticalStackLayoutPage_IsRunnable_AndAllowsUnsizedBoxViewsToCollapse()
    {
        await using var context = await _fixture.NewContextAsync(width: 900, height: 700);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "verticalstacklayoutpage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-verticalstacklayoutpage']");
            });

        var layoutSnapshot = await page.EvaluateAsync<string[]>(
            """
            () => {
                const host = document.querySelector("[data-automation-id='controls-sample-page-verticalstacklayoutpage']");
                const stack = host?.querySelector("[data-mirage-kind='VerticalStack']");
                if (!host || !stack) {
                    return ["__missing__", "0", "0", "0", "0", "0"];
                }

                const label = stack.querySelector("[data-mirage-kind='Label']");
                const labelText = (label?.textContent ?? "").trim();
                const boxViews = Array.from(stack.querySelectorAll("[data-mirage-kind='BoxView']"));
                const boxRects = boxViews.map(box => box.getBoundingClientRect());
                const collapsedCount = boxRects.filter(rect => rect.width <= 1 || rect.height <= 1 || (rect.width * rect.height) <= 1).length;
                const maxWidth = boxRects.reduce((current, rect) => Math.max(current, rect.width), 0);
                const maxArea = boxRects.reduce((current, rect) => Math.max(current, rect.width * rect.height), 0);

                return [
                    "ok",
                    labelText,
                    boxViews.length.toString(),
                    collapsedCount.toString(),
                    maxWidth.toString(),
                    maxArea.toString()
                ];
            }
            """);

        Assert.Equal("ok", layoutSnapshot[0]);
        Assert.Equal("VerticalStackLayout", layoutSnapshot[1]);
        Assert.Equal("6", layoutSnapshot[2]);
        Assert.Equal("6", layoutSnapshot[3]);
    }

    [Fact]
    public async Task StackLayoutPage_IsRunnable_AndAllowsUnsizedBoxViewsToCollapse()
    {
        await using var context = await _fixture.NewContextAsync(width: 900, height: 700);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "stacklayoutpage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-stacklayoutpage']");
            });

        var layoutSnapshot = await page.EvaluateAsync<string[]>(
            """
            () => {
                const host = document.querySelector("[data-automation-id='controls-sample-page-stacklayoutpage']");
                if (!host) {
                    return ["__missing__", "0", "0", "0", "0", "0", "0"];
                }

                const labels = Array.from(host.querySelectorAll("[data-mirage-kind='Label']"));
                const verticalLabel = labels.find(label => (label.textContent ?? "").trim() === "Vertical");
                const horizontalLabel = labels.find(label => (label.textContent ?? "").trim() === "Horizontal");
                const stackKinds = new Set(["StackLayout", "VerticalStack", "HorizontalStack"]);

                const findFollowingStack = (label) => {
                    let current = label?.nextElementSibling ?? null;
                    while (current) {
                        const kind = current.getAttribute("data-mirage-kind");
                        if (kind && stackKinds.has(kind)) {
                            return current;
                        }

                        current = current.nextElementSibling;
                    }

                    return null;
                };

                const verticalStack = findFollowingStack(verticalLabel);
                const horizontalStack = findFollowingStack(horizontalLabel);
                if (!verticalStack || !horizontalStack) {
                    return ["__missing_sections__", "0", "0", "0", "0", "0", "0"];
                }

                const collect = (stack) => {
                    const boxes = Array.from(stack.querySelectorAll(":scope > [data-mirage-kind='BoxView']"));
                    const rects = boxes.map(box => box.getBoundingClientRect());
                    const collapsed = rects.filter(rect => rect.width <= 1 || rect.height <= 1 || (rect.width * rect.height) <= 1).length;
                    return [boxes.length, collapsed];
                };

                const [verticalCount, verticalCollapsed] = collect(verticalStack);
                const [horizontalCount, horizontalCollapsed] = collect(horizontalStack);

                return [
                    "ok",
                    verticalLabel ? "1" : "0",
                    horizontalLabel ? "1" : "0",
                    verticalCount.toString(),
                    horizontalCount.toString(),
                    verticalCollapsed.toString(),
                    horizontalCollapsed.toString()
                ];
            }
            """);

        Assert.Equal("ok", layoutSnapshot[0]);
        Assert.Equal("1", layoutSnapshot[1]);
        Assert.Equal("1", layoutSnapshot[2]);
        Assert.Equal("6", layoutSnapshot[3]);
        Assert.Equal("6", layoutSnapshot[4]);
        Assert.Equal("6", layoutSnapshot[5]);
        Assert.Equal("6", layoutSnapshot[6]);
    }

    [Fact]
    public async Task LayoutsPage_IsRunnable_AndNavigatesToRemainingLayoutSubPages()
    {
        await using var context = await _fixture.NewContextAsync(width: 1024, height: 900);
        var (page, diagnostics) = await OpenAsync(context);

        const string layoutsHostSelector = "[data-automation-id='controls-sample-page-layoutspage']";
        const string sectionsSelector = "[data-automation-id='controls-sample-layoutspage-sections']";

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "layoutspage");
                await page.WaitForSelectorAsync(layoutsHostSelector);
                await page.WaitForSelectorAsync(sectionsSelector);
            });

        var expectedSectionTitles = new[]
        {
            "RelativeLayout",
            "Layout Updates",
            "TemplatedView",
            "Z-Index",
            "Layout IsEnabled"
        };

        foreach (var title in expectedSectionTitles)
        {
            var count = await page.Locator($"{sectionsSelector} span").Filter(new() { HasTextString = title }).CountAsync();
            Assert.True(count > 0, $"Expected LayoutsPage section '{title}' to render.");
        }

        var navigationMatrix = new (string Title, string HostId)[]
        {
            ("RelativeLayout", "controls-sample-page-relativelayoutpage"),
            ("Layout Updates", "controls-sample-page-layoutupdatespage"),
            ("TemplatedView", "controls-sample-page-templatedviewpage"),
            ("Z-Index", "controls-sample-page-zindexpage"),
            ("Layout IsEnabled", "controls-sample-page-layoutisenabledpage")
        };

        foreach (var (title, hostId) in navigationMatrix)
        {
            await NavigateWithDiagnosticsAsync(
                page,
                diagnostics,
                async () =>
                {
                    await page.Locator($"{sectionsSelector} span").Filter(new() { HasTextString = title }).First.ClickAsync();
                    await page.WaitForSelectorAsync($"[data-automation-id='{hostId}']");
                });

            await NavigateWithDiagnosticsAsync(
                page,
                diagnostics,
                async () =>
                {
                    await page.Locator("[data-automation-id='mirage-navigation-back-button']").ClickAsync();
                    await page.WaitForSelectorAsync(layoutsHostSelector);
                });
        }
    }

    [Fact]
    public async Task TemplatedViewPage_IsRunnable_FromCatalog()
    {
        await using var context = await _fixture.NewContextAsync(width: 1024, height: 900);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogCategoryAsync(page, diagnostics, "layouts");
                var button = page.Locator("[data-automation-id='controls-sample-open-templatedviewpage']");
                await button.ScrollIntoViewIfNeededAsync();
                await button.ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-templatedviewpage']");
            });
    }

    [Fact]
    public async Task ScrollViewPage_IsRunnable_AndNavigatesToConstructorScrollScenario()
    {
        await using var context = await _fixture.NewContextAsync(width: 1000, height: 1400);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "scrollviewpage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-scrollviewpage']");
            });

        Assert.True(await page.Locator("[data-automation-id='controls-sample-scrollviewpage-sections']").CountAsync() > 0);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page
                    .Locator("[data-automation-id='controls-sample-scrollviewpage-sections'] span")
                    .Filter(new() { HasTextString = "Scroll To from constructor" })
                    .First
                    .ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-scrolltofromconstructorpage']");
            });

        var widths = await page.EvaluateAsync<double[]>(
            """
            () => Array
                .from(document.querySelectorAll("[data-automation-id='controls-sample-page-scrolltofromconstructorpage'] [data-mirage-kind='BoxView']"))
                .map(box => box.getBoundingClientRect().width)
            """);

        Assert.Equal(10, widths.Length);
        for (var i = 1; i < widths.Length; i++)
        {
            Assert.True(
                widths[i] <= widths[i - 1] - 0.5,
                $"Expected descending widths from ScaleX projection. Previous={widths[i - 1]:0.###}, current={widths[i]:0.###}, index={i}.");
        }
    }

    [Fact]
    public async Task ScrollViewPage_OrientationScenario_UpdatesOverflowAxes()
    {
        await using var context = await _fixture.NewContextAsync(width: 1000, height: 1400);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "scrollviewpage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-scrollviewpage']");
            });

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page
                    .Locator("[data-automation-id='controls-sample-scrollviewpage-sections'] span")
                    .Filter(new() { HasTextString = "Orientation" })
                    .First
                    .ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-scrollvieworientationpage']");
            });

        const string orientationPicker = "[data-automation-id='controls-sample-scrollvieworientationpage-orientation-picker']";
        const string scrollViewer = "[data-automation-id='controls-sample-scrollvieworientationpage-scrollview']";

        Assert.Equal(1, await page.Locator(orientationPicker).CountAsync());
        Assert.Equal(1, await page.Locator(scrollViewer).CountAsync());

        async Task<(string X, string Y)> ReadOverflowAxesAsync()
        {
            var overflow = await page.EvaluateAsync<string[]>(
                """
                (selector) => {
                    const node = document.querySelector(selector);
                    if (!(node instanceof HTMLElement)) {
                        return ["__missing__", "__missing__"];
                    }

                    const style = getComputedStyle(node);
                    return [style.overflowX ?? "", style.overflowY ?? ""];
                }
                """,
                scrollViewer);

            return (overflow[0], overflow[1]);
        }

        async Task SelectOrientationByTextAsync(string label)
        {
            var result = await page.EvaluateAsync<string>(
                """
                (payload) => {
                    const control = document.querySelector(payload.selector);
                    if (!(control instanceof HTMLSelectElement)) {
                        return "__missing__";
                    }

                    const targetIndex = Array.from(control.options)
                        .findIndex(option => (option.textContent ?? "").trim() === payload.label);
                    if (targetIndex < 0) {
                        return "__not_found__";
                    }

                    const targetValue = control.options[targetIndex]?.value ?? "";
                    control.value = targetValue;
                    control.selectedIndex = targetIndex;
                    control.dispatchEvent(new Event("input", { bubbles: true, composed: true }));
                    control.dispatchEvent(new Event("change", { bubbles: true, composed: true }));
                    return control.value ?? "";
                }
                """,
                new
                {
                    selector = orientationPicker,
                    label
                });

            Assert.NotEqual("__missing__", result);
            Assert.NotEqual("__not_found__", result);
        }

        async Task AssertOrientationOverflowAsync(string label, string expectedX, string expectedY)
        {
            await SelectOrientationByTextAsync(label);

            const int timeoutMs = 2000;
            const int pollMs = 50;
            var elapsedMs = 0;
            (string X, string Y) observed = ("", "");

            while (elapsedMs <= timeoutMs)
            {
                observed = await ReadOverflowAxesAsync();
                if (string.Equals(observed.X, expectedX, StringComparison.Ordinal)
                    && string.Equals(observed.Y, expectedY, StringComparison.Ordinal))
                {
                    return;
                }

                await page.WaitForTimeoutAsync(pollMs);
                elapsedMs += pollMs;
            }

            Assert.Equal(expectedX, observed.X);
            Assert.Equal(expectedY, observed.Y);
        }

        await AssertOrientationOverflowAsync("Vertical", "hidden", "auto");
        await AssertOrientationOverflowAsync("Horizontal", "auto", "hidden");
        await AssertOrientationOverflowAsync("Both", "auto", "auto");
        await AssertOrientationOverflowAsync("Neither", "hidden", "hidden");
    }

    [Fact]
    public async Task ButtonPage_IsRunnable_AndExercisesCoreScenarios()
    {
        await using var context = await _fixture.NewContextAsync(width: 760, height: 980);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "buttonpage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-buttonpage']");
            });

        Assert.True(await page.Locator("[data-automation-id='controls-sample-page-buttonpage'] input[type='checkbox']").CountAsync() > 0);
        Assert.True(await page.Locator("[data-automation-id='controls-sample-page-buttonpage'] input[type='range']").CountAsync() > 0);

        var imageToggleSelector = "[data-automation-id='controls-sample-buttonpage-image-source-toggle']";
        Assert.Equal(1, await page.Locator(imageToggleSelector).CountAsync());
        var imageBefore = await page.Locator($"{imageToggleSelector} img").GetAttributeAsync("src");
        Assert.False(string.IsNullOrWhiteSpace(imageBefore));
        await page.Locator(imageToggleSelector).ClickAsync();
        await page.WaitForTimeoutAsync(120);
        var imageAfter = await page.Locator($"{imageToggleSelector} img").GetAttributeAsync("src");
        Assert.True(string.IsNullOrWhiteSpace(imageAfter));

        var positionSelector = "[data-automation-id='controls-sample-buttonpage-position-toggle']";
        var decreaseSpacingSelector = "[data-automation-id='controls-sample-buttonpage-decrease-spacing']";
        Assert.Equal(1, await page.Locator(positionSelector).CountAsync());
        Assert.Equal(1, await page.Locator(decreaseSpacingSelector).CountAsync());

        var initialFlexDirection = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).flexDirection",
            positionSelector);
        await page.Locator(positionSelector).ClickAsync();
        await page.WaitForTimeoutAsync(120);
        var changedFlexDirection = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).flexDirection",
            positionSelector);
        Assert.NotEqual(initialFlexDirection, changedFlexDirection);

        var marginBefore = await page.EvaluateAsync<string>(
            """
            (selector) => {
                const text = document.querySelector(selector)?.querySelector("[data-mirage-value-element='true']");
                if (!text) {
                    return "__missing__";
                }

                return [text.style.marginTop, text.style.marginRight, text.style.marginBottom, text.style.marginLeft].join("|");
            }
            """,
            positionSelector);

        await page.Locator(decreaseSpacingSelector).ClickAsync();
        await page.WaitForTimeoutAsync(120);

        var marginAfter = await page.EvaluateAsync<string>(
            """
            (selector) => {
                const text = document.querySelector(selector)?.querySelector("[data-mirage-value-element='true']");
                if (!text) {
                    return "__missing__";
                }

                return [text.style.marginTop, text.style.marginRight, text.style.marginBottom, text.style.marginLeft].join("|");
            }
            """,
            positionSelector);

        Assert.NotEqual("__missing__", marginBefore);
        Assert.NotEqual("__missing__", marginAfter);
        Assert.NotEqual(marginBefore, marginAfter);

        var backgroundConverterPairLocated = await page.EvaluateAsync<bool>(
            """
            () => {
                const root = document.querySelector("[data-automation-id='controls-sample-page-buttonpage']");
                if (!root) {
                    return false;
                }

                const checkbox = root.querySelector("input[type='checkbox']");
                if (!checkbox) {
                    return false;
                }

                const mappedButton = checkbox.nextElementSibling;
                if (!(mappedButton instanceof HTMLButtonElement)) {
                    return false;
                }

                checkbox.setAttribute("data-e2e-buttonpage-color-checkbox", "true");
                mappedButton.setAttribute("data-e2e-buttonpage-color-target", "true");
                return true;
            }
            """);

        Assert.True(backgroundConverterPairLocated);
        var colorTargetSelector = "[data-e2e-buttonpage-color-target='true']";
        var converterBackgroundBefore = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).backgroundColor",
            colorTargetSelector);
        await page.Locator("[data-e2e-buttonpage-color-checkbox='true']").ClickAsync();
        await page.WaitForTimeoutAsync(120);
        var converterBackgroundAfter = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).backgroundColor",
            colorTargetSelector);
        Assert.NotEqual(converterBackgroundBefore, converterBackgroundAfter);

        var borderButtonSelector = "[data-automation-id='controls-sample-buttonpage-border-button']";
        var borderSliderSelector = "[data-automation-id='controls-sample-buttonpage-border-slider']";
        Assert.Equal(1, await page.Locator(borderButtonSelector).CountAsync());
        Assert.Equal(1, await page.Locator(borderSliderSelector).CountAsync());
        var borderWidthBefore = await page.EvaluateAsync<double>(
            "(selector) => parseFloat(getComputedStyle(document.querySelector(selector)).borderTopWidth || '0')",
            borderButtonSelector);
        await page.EvaluateAsync(
            """
            (selector) => {
                const slider = document.querySelector(selector);
                if (!(slider instanceof HTMLInputElement)) {
                    return;
                }

                slider.value = "25";
                slider.dispatchEvent(new Event("input", { bubbles: true, composed: true }));
                slider.dispatchEvent(new Event("change", { bubbles: true, composed: true }));
            }
            """,
            borderSliderSelector);
        await page.WaitForTimeoutAsync(120);
        var borderWidthAfter = await page.EvaluateAsync<double>(
            "(selector) => parseFloat(getComputedStyle(document.querySelector(selector)).borderTopWidth || '0')",
            borderButtonSelector);
        Assert.True(borderWidthAfter > borderWidthBefore);

        var lineBreakUpdateSelector = "[data-automation-id='controls-sample-buttonpage-linebreak-update']";
        var lineBreakTargetSelector = "[data-automation-id='controls-sample-buttonpage-linebreak-target']";
        Assert.Equal(1, await page.Locator(lineBreakUpdateSelector).CountAsync());
        Assert.Equal(1, await page.Locator(lineBreakTargetSelector).CountAsync());

        await page.Locator(lineBreakUpdateSelector).ClickAsync();
        await page.WaitForTimeoutAsync(80);
        await page.Locator(lineBreakUpdateSelector).ClickAsync();
        await page.WaitForTimeoutAsync(80);
        var headText = await page.EvaluateAsync<string>(
            "(selector) => (document.querySelector(selector)?.querySelector('[data-mirage-value-element=\"true\"]')?.textContent ?? '').trim()",
            lineBreakTargetSelector);
        if (headText.Contains('…'))
        {
            Assert.StartsWith("…", headText, StringComparison.Ordinal);
        }

        await page.Locator(lineBreakUpdateSelector).ClickAsync();
        await page.WaitForTimeoutAsync(80);
        var middleText = await page.EvaluateAsync<string>(
            "(selector) => (document.querySelector(selector)?.querySelector('[data-mirage-value-element=\"true\"]')?.textContent ?? '').trim()",
            lineBreakTargetSelector);
        if (middleText.Contains('…'))
        {
            Assert.False(middleText.StartsWith("…", StringComparison.Ordinal));
            Assert.False(middleText.EndsWith("…", StringComparison.Ordinal));
        }

        await page.Locator(lineBreakUpdateSelector).ClickAsync();
        await page.WaitForTimeoutAsync(80);
        await page.Locator(lineBreakUpdateSelector).ClickAsync();
        await page.WaitForTimeoutAsync(80);
        var tailText = await page.EvaluateAsync<string>(
            "(selector) => (document.querySelector(selector)?.querySelector('[data-mirage-value-element=\"true\"]')?.textContent ?? '').trim()",
            lineBreakTargetSelector);
        if (tailText.Contains('…'))
        {
            Assert.EndsWith("…", tailText, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task BoxViewPage_IsRunnable_AndExercisesCoreProjectionScenarios()
    {
        await using var context = await _fixture.NewContextAsync(width: 900, height: 1100);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "boxviewpage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-boxviewpage']");
            });

        var boxIds = new[]
        {
            "controls-sample-boxviewpage-default",
            "controls-sample-boxviewpage-color",
            "controls-sample-boxviewpage-background",
            "controls-sample-boxviewpage-corner-radius",
            "controls-sample-boxviewpage-complex-corner-radius",
            "controls-sample-boxviewpage-opacity",
            "controls-sample-boxviewpage-clip",
            "controls-sample-boxviewpage-shadow"
        };

        foreach (var boxId in boxIds)
        {
            Assert.Equal(1, await page.Locator($"[data-automation-id='{boxId}']").CountAsync());
        }

        var defaultBackground = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).backgroundColor",
            "[data-automation-id='controls-sample-boxviewpage-default']");
        Assert.Contains("100, 149, 237", defaultBackground, StringComparison.Ordinal);

        var colorBackground = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).backgroundColor",
            "[data-automation-id='controls-sample-boxviewpage-color']");
        Assert.Contains("128, 0, 128", colorBackground, StringComparison.Ordinal);

        var brushBackground = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).backgroundImage",
            "[data-automation-id='controls-sample-boxviewpage-background']");
        Assert.True(
            !string.IsNullOrWhiteSpace(brushBackground)
            && !string.Equals(brushBackground, "none", StringComparison.OrdinalIgnoreCase),
            "Expected brush-based background projection for BoxView background scenario.");

        var cornerRadius = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).borderRadius",
            "[data-automation-id='controls-sample-boxviewpage-corner-radius']");
        Assert.Equal("10px", cornerRadius);

        var complexCornerRadius = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).borderRadius",
            "[data-automation-id='controls-sample-boxviewpage-complex-corner-radius']");
        Assert.Equal("10px 0px 20px 5px", complexCornerRadius);

        var opacity = await page.EvaluateAsync<double>(
            "(selector) => parseFloat(getComputedStyle(document.querySelector(selector)).opacity || '1')",
            "[data-automation-id='controls-sample-boxviewpage-opacity']");
        Assert.InRange(opacity, 0.45d, 0.55d);

        var clipPath = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).clipPath",
            "[data-automation-id='controls-sample-boxviewpage-clip']");
        Assert.True(
            !string.IsNullOrWhiteSpace(clipPath)
            && !string.Equals(clipPath, "none", StringComparison.OrdinalIgnoreCase),
            $"Expected non-empty clip-path for BoxView clip scenario but found '{clipPath}'.");

        var shadow = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).boxShadow",
            "[data-automation-id='controls-sample-boxviewpage-shadow']");
        Assert.True(
            !string.IsNullOrWhiteSpace(shadow)
            && !string.Equals(shadow, "none", StringComparison.OrdinalIgnoreCase),
            $"Expected non-empty box-shadow for BoxView shadow scenario but found '{shadow}'.");
    }

    [Fact]
    public async Task CheckBoxPage_IsRunnable_AndExercisesColorAndCheckedScenarios()
    {
        await using var context = await _fixture.NewContextAsync(width: 760, height: 980);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "checkboxpage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-checkboxpage']");
            });

        var coloredAccent = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).accentColor",
            "[data-automation-id='controls-sample-checkboxpage-colored']");
        Assert.Contains("128", coloredAccent, StringComparison.Ordinal);

        var disabledAttribute = await page.EvaluateAsync<string?>(
            "(selector) => document.querySelector(selector)?.getAttribute('disabled')",
            "[data-automation-id='controls-sample-checkboxpage-disabled']");
        Assert.Equal("disabled", disabledAttribute);

        var dynamicAccentBefore = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).accentColor",
            "[data-automation-id='controls-sample-checkboxpage-change-checkbox']");

        await page.Locator("[data-automation-id='controls-sample-checkboxpage-change-button']").ClickAsync();
        await page.WaitForTimeoutAsync(100);

        var dynamicAccentAfter = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).accentColor",
            "[data-automation-id='controls-sample-checkboxpage-change-checkbox']");
        Assert.NotEqual(dynamicAccentBefore, dynamicAccentAfter);

        var dynamicButtonText = await page.Locator("[data-automation-id='controls-sample-checkboxpage-change-button']").InnerTextAsync();
        Assert.Contains("Is green? True", dynamicButtonText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SliderPage_IsRunnable_AndExercisesThumbImageAndDynamicRangeScenarios()
    {
        await using var context = await _fixture.NewContextAsync(width: 900, height: 1080);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "sliderpage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-sliderpage']");
            });

        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-sliderpage-image-slider']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-sliderpage-dynamic-slider']").CountAsync());

        var defaultSliderHostBackground = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).backgroundColor",
            "[data-automation-id='controls-sample-sliderpage-default-slider']");
        Assert.True(
            string.Equals(defaultSliderHostBackground, "rgba(0, 0, 0, 0)", StringComparison.OrdinalIgnoreCase)
            || string.Equals(defaultSliderHostBackground, "transparent", StringComparison.OrdinalIgnoreCase),
            $"Expected transparent slider host background but found '{defaultSliderHostBackground}'.");

        var defaultSliderHostBackgroundImage = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).backgroundImage",
            "[data-automation-id='controls-sample-sliderpage-default-slider']");
        Assert.True(
            string.IsNullOrWhiteSpace(defaultSliderHostBackgroundImage)
            || string.Equals(defaultSliderHostBackgroundImage, "none", StringComparison.OrdinalIgnoreCase),
            $"Expected no host background image but found '{defaultSliderHostBackgroundImage}'.");

        var backgroundColorHost = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).backgroundColor",
            "[data-automation-id='controls-sample-sliderpage-background-color-slider']");
        Assert.Contains("0, 0, 255", backgroundColorHost, StringComparison.Ordinal);

        var backgroundBrushHost = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).backgroundImage",
            "[data-automation-id='controls-sample-sliderpage-background-brush-slider']");
        Assert.True(
            !string.IsNullOrWhiteSpace(backgroundBrushHost)
            && !string.Equals(backgroundBrushHost, "none", StringComparison.OrdinalIgnoreCase),
            "Expected a brush background image to be projected on the slider host.");

        var maximumTrackGradient = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).getPropertyValue('--mirage-slider-track-gradient').trim()",
            "[data-automation-id='controls-sample-sliderpage-maximum-track-slider']");
        Assert.Contains("255,192,203", maximumTrackGradient.Replace(" ", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);

        var customTrackGradient = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).getPropertyValue('--mirage-slider-track-gradient').trim()",
            "[data-automation-id='controls-sample-sliderpage-custom-slider']");
        var compactCustomGradient = customTrackGradient.Replace(" ", string.Empty, StringComparison.Ordinal);
        Assert.Contains("255,0,0", compactCustomGradient, StringComparison.Ordinal);
        Assert.Contains("0,128,0", compactCustomGradient, StringComparison.Ordinal);

        var thumbImageBefore = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).getPropertyValue('--mirage-slider-thumb-image').trim()",
            "[data-automation-id='controls-sample-sliderpage-image-slider']");
        Assert.Contains("/_mirage/assets/", thumbImageBefore, StringComparison.Ordinal);

        var thumbImageStateBefore = await page.EvaluateAsync<string?>(
            "(selector) => document.querySelector(selector)?.getAttribute('data-maui-slider-thumb-image-state')",
            "[data-automation-id='controls-sample-sliderpage-image-slider']");
        Assert.Equal("resolved", thumbImageStateBefore);

        var thumbImageBorderRadiusBefore = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector), '::-webkit-slider-thumb').borderRadius",
            "[data-automation-id='controls-sample-sliderpage-image-slider']");
        Assert.NotEqual("50%", thumbImageBorderRadiusBefore);

        await page.Locator("[data-automation-id='controls-sample-sliderpage-toggle-image']").ClickAsync();
        await page.WaitForTimeoutAsync(100);
        var thumbImageRemoved = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).getPropertyValue('--mirage-slider-thumb-image').trim()",
            "[data-automation-id='controls-sample-sliderpage-image-slider']");
        Assert.True(string.IsNullOrWhiteSpace(thumbImageRemoved) || string.Equals(thumbImageRemoved, "none", StringComparison.OrdinalIgnoreCase));

        await page.Locator("[data-automation-id='controls-sample-sliderpage-toggle-image']").ClickAsync();
        await page.WaitForTimeoutAsync(100);
        var thumbImageRestored = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).getPropertyValue('--mirage-slider-thumb-image').trim()",
            "[data-automation-id='controls-sample-sliderpage-image-slider']");
        Assert.Contains("/_mirage/assets/", thumbImageRestored, StringComparison.Ordinal);

        var thumbImageStateRestored = await page.EvaluateAsync<string?>(
            "(selector) => document.querySelector(selector)?.getAttribute('data-maui-slider-thumb-image-state')",
            "[data-automation-id='controls-sample-sliderpage-image-slider']");
        Assert.Equal("resolved", thumbImageStateRestored);

        var dynamicInfoBefore = await page.Locator("[data-automation-id='controls-sample-sliderpage-dynamic-info']").InnerTextAsync();
        Assert.Contains("Minimum: 0", dynamicInfoBefore, StringComparison.Ordinal);
        Assert.Contains("Maximum: 10", dynamicInfoBefore, StringComparison.Ordinal);

        await page.Locator("[data-automation-id='controls-sample-sliderpage-update-minimum']").ClickAsync();
        await page.Locator("[data-automation-id='controls-sample-sliderpage-update-maximum']").ClickAsync();
        await page.WaitForTimeoutAsync(100);

        var dynamicInfoAfter = await page.Locator("[data-automation-id='controls-sample-sliderpage-dynamic-info']").InnerTextAsync();
        Assert.Contains("Minimum: 4", dynamicInfoAfter, StringComparison.Ordinal);
        Assert.Contains("Maximum: 8", dynamicInfoAfter, StringComparison.Ordinal);

        var disabledSliderAttribute = await page.EvaluateAsync<string?>(
            "(selector) => document.querySelector(selector)?.getAttribute('disabled')",
            "[data-automation-id='controls-sample-sliderpage-disabled-slider']");
        Assert.Equal("disabled", disabledSliderAttribute);

        var disabledSliderOpacity = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).opacity",
            "[data-automation-id='controls-sample-sliderpage-disabled-slider']");
        Assert.Equal("1", disabledSliderOpacity);

        var disabledThumbColor = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).getPropertyValue('--mirage-slider-thumb-color').trim()",
            "[data-automation-id='controls-sample-sliderpage-disabled-slider']");
        Assert.Contains("255,255,255", disabledThumbColor.Replace(" ", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
    }

    [Fact]
    public async Task EntryPage_IsRunnable_AndExercisesTextInputInteractionsAndDialogs()
    {
        await using var context = await _fixture.NewContextAsync(width: 1000, height: 1600);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "entrypage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-entrypage']");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-entrypage-placeholder-entry']");
            });

        const string placeholderEntrySelector = "[data-automation-id='controls-sample-entrypage-placeholder-entry']";
        const string showSoftInputSelector = "[data-automation-id='controls-sample-entrypage-show-soft-input']";
        const string hideSoftInputSelector = "[data-automation-id='controls-sample-entrypage-hide-soft-input']";

        await page.Locator(showSoftInputSelector).ClickAsync();
        await WaitForResolvedTextInputFocusAsync(page, placeholderEntrySelector, expectedFocused: true);

        await page.Locator(hideSoftInputSelector).ClickAsync();
        await WaitForResolvedTextInputFocusAsync(page, placeholderEntrySelector, expectedFocused: false);

        const string passwordEntrySelector = "[data-automation-id='controls-sample-entrypage-password-entry']";
        Assert.Equal("password", await ReadResolvedTextInputAttributeAsync(page, passwordEntrySelector, "type"));

        await page.Locator("[data-automation-id='controls-sample-entrypage-password-checkbox']").ClickAsync();
        await page.WaitForTimeoutAsync(80);
        Assert.Equal("text", await ReadResolvedTextInputAttributeAsync(page, passwordEntrySelector, "type"));

        const string clearButtonEntrySelector = "[data-automation-id='controls-sample-entrypage-clear-button-entry']";
        await FocusResolvedTextInputAsync(page, clearButtonEntrySelector);
        Assert.True(await ClickNestedElementAsync(page, clearButtonEntrySelector, ".mirage-entry-clear"));
        await WaitForInputValueEqualsAsync(page, clearButtonEntrySelector, string.Empty);
        await WaitForResolvedTextInputFocusAsync(page, clearButtonEntrySelector, expectedFocused: true);

        const string backgroundColorEntrySelector = "[data-automation-id='controls-sample-entrypage-background-color-entry']";
        var backgroundColorBeforeClear = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).backgroundColor",
            backgroundColorEntrySelector);
        Assert.False(IsTransparent(backgroundColorBeforeClear), $"Expected Entry background color to be initialized, but found '{backgroundColorBeforeClear}'.");

        await page.Locator("[data-automation-id='controls-sample-entrypage-clear-background-color']").ClickAsync();
        await page.WaitForTimeoutAsync(80);
        var backgroundColorAfterClear = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).backgroundColor",
            backgroundColorEntrySelector);
        Assert.True(IsTransparent(backgroundColorAfterClear), $"Expected Entry background color to clear, but found '{backgroundColorAfterClear}'.");

        await page.Locator("[data-automation-id='controls-sample-entrypage-update-background-color']").ClickAsync();
        await page.WaitForTimeoutAsync(80);
        var backgroundColorAfterUpdate = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).backgroundColor",
            backgroundColorEntrySelector);
        Assert.False(IsTransparent(backgroundColorAfterUpdate), $"Expected Entry background color update to project a visible color, but found '{backgroundColorAfterUpdate}'.");

        const string backgroundBrushEntrySelector = "[data-automation-id='controls-sample-entrypage-background-entry']";
        var backgroundImageBeforeClear = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).backgroundImage",
            backgroundBrushEntrySelector);
        Assert.True(
            !string.IsNullOrWhiteSpace(backgroundImageBeforeClear)
            && !string.Equals(backgroundImageBeforeClear, "none", StringComparison.OrdinalIgnoreCase),
            "Expected Entry background brush to be initialized.");

        await page.Locator("[data-automation-id='controls-sample-entrypage-clear-background']").ClickAsync();
        await page.WaitForTimeoutAsync(80);
        var backgroundImageAfterClear = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).backgroundImage",
            backgroundBrushEntrySelector);
        Assert.True(
            string.IsNullOrWhiteSpace(backgroundImageAfterClear)
            || string.Equals(backgroundImageAfterClear, "none", StringComparison.OrdinalIgnoreCase),
            $"Expected Entry background brush to clear, but found '{backgroundImageAfterClear}'.");

        await page.Locator("[data-automation-id='controls-sample-entrypage-update-background']").ClickAsync();
        await page.WaitForTimeoutAsync(80);
        var backgroundImageAfterUpdate = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).backgroundImage",
            backgroundBrushEntrySelector);
        Assert.True(
            !string.IsNullOrWhiteSpace(backgroundImageAfterUpdate)
            && !string.Equals(backgroundImageAfterUpdate, "none", StringComparison.OrdinalIgnoreCase),
            "Expected Entry background brush update to project a gradient.");

        await SetInputValueAsync(page, "[data-automation-id='controls-sample-entrypage-cursor-slider']", 3);
        await page.WaitForTimeoutAsync(80);
        var cursorSelection = await ReadSelectionRangeAsync(page, "[data-automation-id='controls-sample-entrypage-cursor-entry']");
        Assert.Equal(3, cursorSelection.start);
        Assert.Equal(3, cursorSelection.end);

        await SetInputValueAsync(page, "[data-automation-id='controls-sample-entrypage-selection-slider']", 2);
        await page.WaitForTimeoutAsync(80);
        var selectionRange = await ReadSelectionRangeAsync(page, "[data-automation-id='controls-sample-entrypage-selection-entry']");
        Assert.Equal(2, selectionRange.start);
        Assert.Equal(4, selectionRange.end);

        await FocusResolvedTextInputAsync(page, "[data-automation-id='controls-sample-entrypage-focus-entry']");
        await ExpectDialogAsync(page, "Focused", string.Empty, "modal");
        await page.Locator("[data-automation-id='mirage-dialog-accept']").ClickAsync();
        await page.WaitForSelectorAsync("[data-automation-id='mirage-dialog-overlay']", new() { State = WaitForSelectorState.Detached });

        await FocusResolvedTextInputAsync(page, "[data-automation-id='controls-sample-entrypage-unfocus-entry']");
        await FocusResolvedTextInputAsync(page, placeholderEntrySelector);
        await ExpectDialogAsync(page, "Unfocused", string.Empty, "modal");
        await page.Locator("[data-automation-id='mirage-dialog-accept']").ClickAsync();
        await page.WaitForSelectorAsync("[data-automation-id='mirage-dialog-overlay']", new() { State = WaitForSelectorState.Detached });

        const string completedEntrySelector = "[data-automation-id='controls-sample-entrypage-completed-entry']";
        await SetInputTextAsync(page, completedEntrySelector, "Done");
        await FocusResolvedTextInputAsync(page, completedEntrySelector);
        await page.Locator($"{completedEntrySelector} input").PressAsync("Enter");
        await ExpectDialogAsync(page, "Completed", "Done", "modal");
        await page.Locator("[data-automation-id='mirage-dialog-accept']").ClickAsync();
        await page.WaitForSelectorAsync("[data-automation-id='mirage-dialog-overlay']", new() { State = WaitForSelectorState.Detached });
    }

    [Fact]
    public async Task EditorPage_IsRunnable_AndProjectsBackgroundKeyboardCursorDialogsAndAutoSize()
    {
        await using var context = await _fixture.NewContextAsync(width: 1000, height: 1600);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "editorpage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-editorpage']");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-editorpage-background-editor']");
            });

        const string numericEditorSelector = "[data-automation-id='controls-sample-editorpage-numeric-editor']";
        Assert.Equal("decimal", await ReadResolvedTextInputAttributeAsync(page, numericEditorSelector, "inputmode"));

        const string backgroundEditorSelector = "[data-automation-id='controls-sample-editorpage-background-editor']";
        var backgroundImageBeforeClear = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).backgroundImage",
            backgroundEditorSelector);
        Assert.True(
            !string.IsNullOrWhiteSpace(backgroundImageBeforeClear)
            && !string.Equals(backgroundImageBeforeClear, "none", StringComparison.OrdinalIgnoreCase),
            "Expected Editor background brush to be initialized.");

        await page.Locator("[data-automation-id='controls-sample-editorpage-clear-background']").ClickAsync();
        await page.WaitForTimeoutAsync(80);
        var backgroundImageAfterClear = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).backgroundImage",
            backgroundEditorSelector);
        Assert.True(
            string.IsNullOrWhiteSpace(backgroundImageAfterClear)
            || string.Equals(backgroundImageAfterClear, "none", StringComparison.OrdinalIgnoreCase),
            $"Expected Editor background brush to clear, but found '{backgroundImageAfterClear}'.");

        await page.Locator("[data-automation-id='controls-sample-editorpage-update-background']").ClickAsync();
        await page.WaitForTimeoutAsync(80);
        var backgroundImageAfterUpdate = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).backgroundImage",
            backgroundEditorSelector);
        Assert.True(
            !string.IsNullOrWhiteSpace(backgroundImageAfterUpdate)
            && !string.Equals(backgroundImageAfterUpdate, "none", StringComparison.OrdinalIgnoreCase),
            "Expected Editor background brush update to project a gradient.");

        var cursorSelection = await ReadSelectionRangeAsync(page, "[data-automation-id='controls-sample-editorpage-cursor-editor']");
        Assert.Equal(2, cursorSelection.start);
        Assert.Equal(2, cursorSelection.end);

        await FocusResolvedTextInputAsync(page, "[data-automation-id='controls-sample-editorpage-focus-editor']");
        await ExpectDialogAsync(page, "Focused", string.Empty, "modal");
        await page.Locator("[data-automation-id='mirage-dialog-accept']").ClickAsync();
        await page.WaitForSelectorAsync("[data-automation-id='mirage-dialog-overlay']", new() { State = WaitForSelectorState.Detached });

        await FocusResolvedTextInputAsync(page, "[data-automation-id='controls-sample-editorpage-unfocus-editor']");
        await FocusResolvedTextInputAsync(page, numericEditorSelector);
        await ExpectDialogAsync(page, "Unfocused", string.Empty, "modal");
        await page.Locator("[data-automation-id='mirage-dialog-accept']").ClickAsync();
        await page.WaitForSelectorAsync("[data-automation-id='mirage-dialog-overlay']", new() { State = WaitForSelectorState.Detached });

        const string completedEditorSelector = "[data-automation-id='controls-sample-editorpage-completed-editor']";
        await SetInputTextAsync(page, completedEditorSelector, "Editor done");
        await FocusResolvedTextInputAsync(page, completedEditorSelector);
        await FocusResolvedTextInputAsync(page, numericEditorSelector);
        await ExpectDialogAsync(page, "Completed", "Editor done", "modal");
        await page.Locator("[data-automation-id='mirage-dialog-accept']").ClickAsync();
        await page.WaitForSelectorAsync("[data-automation-id='mirage-dialog-overlay']", new() { State = WaitForSelectorState.Detached });

        const string autoSizeEditorSelector = "[data-automation-id='controls-sample-editorpage-autosize-editor']";
        var baselineRect = await ReadBoundingClientRectAsync(page, autoSizeEditorSelector);
        await SetInputTextAsync(page, autoSizeEditorSelector, string.Join('\n', Enumerable.Range(1, 12).Select(static line => $"Line {line}")));
        var grownRect = await WaitForBoundingClientRectChangeAsync(page, autoSizeEditorSelector, baselineRect);
        Assert.True(grownRect[3] > baselineRect[3] + 10d, $"Expected autosize editor to grow. Before={baselineRect[3]:0.###}, after={grownRect[3]:0.###}.");

        await SetInputTextAsync(page, autoSizeEditorSelector, "Short");
        var shrunkRect = await WaitForBoundingClientRectChangeAsync(page, autoSizeEditorSelector, grownRect);
        Assert.True(shrunkRect[3] < grownRect[3] - 10d, $"Expected autosize editor to shrink. Before={grownRect[3]:0.###}, after={shrunkRect[3]:0.###}.");
    }

    [Fact]
    public async Task TransformationsPage_IsRunnable_AndAppliesSliderAndStepperTransforms()
    {
        await using var context = await _fixture.NewContextAsync(width: 1000, height: 1200);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "transformationspage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-transformationspage']");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-transformationspage-target']");
            });

        await SetInputValueAsync(page, "[data-automation-id='controls-sample-transformationspage-scale-slider']", 2);
        await SetInputValueAsync(page, "[data-automation-id='controls-sample-transformationspage-scalex-slider']", 1.5);
        await SetInputValueAsync(page, "[data-automation-id='controls-sample-transformationspage-scaley-slider']", 0.5);
        await SetInputValueAsync(page, "[data-automation-id='controls-sample-transformationspage-rotation-slider']", 90);
        await page.Locator("[data-automation-id='controls-sample-transformationspage-anchorx-stepper'] [data-mirage-secondary-element='true']").ClickAsync();
        await page.Locator("[data-automation-id='controls-sample-transformationspage-anchory-stepper'] [data-mirage-value-element='true']").ClickAsync();
        await page.WaitForTimeoutAsync(120);

        var targetTransformSnapshot = await page.EvaluateAsync<string[]>(
            """
            () => {
                const target = document.querySelector("[data-automation-id='controls-sample-transformationspage-target']");
                if (!(target instanceof HTMLElement)) {
                    return ["__missing__", "", ""];
                }

                return [
                    "ok",
                    target.style.transform ?? "",
                    target.style.transformOrigin ?? ""
                ];
            }
            """);

        Assert.Equal("ok", targetTransformSnapshot[0]);
        Assert.Contains("rotate(90deg)", targetTransformSnapshot[1], StringComparison.Ordinal);
        Assert.Contains("scale(3, 1)", targetTransformSnapshot[1], StringComparison.Ordinal);
        Assert.Equal("0% 100%", targetTransformSnapshot[2]);
    }

    [Fact]
    public async Task StepperPage_IsRunnable_AndSupportsEnablementValueAndRangeScenarios()
    {
        await using var context = await _fixture.NewContextAsync(width: 900, height: 1080);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "stepperpage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-stepperpage']");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-stepperpage-enable-stepper']");
            });

        var numberInputCount = await page.Locator("[data-automation-id='controls-sample-page-stepperpage'] input[type='number']").CountAsync();
        Assert.Equal(0, numberInputCount);

        var enableStepperSelector = "[data-automation-id='controls-sample-stepperpage-enable-stepper']";
        var enableButtonSelector = "[data-automation-id='controls-sample-stepperpage-enable-button']";
        var enableStepperDecrementSelector = $"{enableStepperSelector} [data-mirage-secondary-element='true']";
        var enableStepperIncrementSelector = $"{enableStepperSelector} [data-mirage-value-element='true']";

        Assert.Equal("-", (await page.Locator(enableStepperDecrementSelector).InnerTextAsync()).Trim());
        Assert.Equal("+", (await page.Locator(enableStepperIncrementSelector).InnerTextAsync()).Trim());
        Assert.Equal("button", await page.Locator(enableStepperDecrementSelector).GetAttributeAsync("type"));
        Assert.Equal("button", await page.Locator(enableStepperIncrementSelector).GetAttributeAsync("type"));

        Assert.True(await HasDisabledAttributeAsync(page, enableStepperDecrementSelector));
        Assert.True(await HasDisabledAttributeAsync(page, enableStepperIncrementSelector));

        await page.Locator(enableButtonSelector).ClickAsync();
        await page.WaitForTimeoutAsync(80);

        Assert.True(await HasDisabledAttributeAsync(page, enableStepperDecrementSelector));
        Assert.False(await HasDisabledAttributeAsync(page, enableStepperIncrementSelector));

        var buttonTextAfterEnable = await page.Locator(enableButtonSelector).InnerTextAsync();
        Assert.Contains("Disable Stepper", buttonTextAfterEnable, StringComparison.Ordinal);

        await page.Locator(enableButtonSelector).ClickAsync();
        await page.WaitForTimeoutAsync(80);

        Assert.True(await HasDisabledAttributeAsync(page, enableStepperDecrementSelector));
        Assert.True(await HasDisabledAttributeAsync(page, enableStepperIncrementSelector));

        var minStepperSelector = "[data-automation-id='controls-sample-stepperpage-minmax']";
        Assert.Equal("5", await page.Locator(minStepperSelector).GetAttributeAsync("data-maui-stepper-min"));
        Assert.Equal("25", await page.Locator(minStepperSelector).GetAttributeAsync("data-maui-stepper-max"));
        var minStepperDecrementSelector = $"{minStepperSelector} [data-mirage-secondary-element='true']";
        var minStepperIncrementSelector = $"{minStepperSelector} [data-mirage-value-element='true']";
        Assert.True(await HasDisabledAttributeAsync(page, minStepperDecrementSelector));
        Assert.False(await HasDisabledAttributeAsync(page, minStepperIncrementSelector));

        Assert.True(await TryClickButtonIfEnabledAsync(page, minStepperIncrementSelector));
        await page.WaitForTimeoutAsync(80);
        Assert.False(await HasDisabledAttributeAsync(page, minStepperDecrementSelector));

        for (var i = 0; i < 32; i++)
        {
            if (!await TryClickButtonIfEnabledAsync(page, minStepperIncrementSelector))
            {
                break;
            }
        }

        await page.WaitForTimeoutAsync(100);
        Assert.True(await HasDisabledAttributeAsync(page, minStepperIncrementSelector));

        var incrementStepperSelector = "[data-automation-id='controls-sample-stepperpage-increment']";
        Assert.Equal("2", await page.Locator(incrementStepperSelector).GetAttributeAsync("data-maui-stepper-step"));

        var backgroundColorHost = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).backgroundColor",
            "[data-automation-id='controls-sample-stepperpage-background-color']");
        Assert.Contains("255,0,0", backgroundColorHost.Replace(" ", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);

        var backgroundBrushHost = await page.EvaluateAsync<string>(
            "(selector) => getComputedStyle(document.querySelector(selector)).backgroundImage",
            "[data-automation-id='controls-sample-stepperpage-background-brush']");
        Assert.True(
            !string.IsNullOrWhiteSpace(backgroundBrushHost)
            && !string.Equals(backgroundBrushHost, "none", StringComparison.OrdinalIgnoreCase),
            "Expected brush background image projection on Stepper host.");

        var valueChangedSelector = "[data-automation-id='controls-sample-stepperpage-valuechanged-stepper']";
        var valueChangedIncrementSelector = $"{valueChangedSelector} [data-mirage-value-element='true']";
        var valueChangedDecrementSelector = $"{valueChangedSelector} [data-mirage-secondary-element='true']";
        Assert.True(await TryClickButtonIfEnabledAsync(page, valueChangedIncrementSelector));
        await page.WaitForTimeoutAsync(100);

        var valueLabelText = await page.Locator("[data-automation-id='controls-sample-stepperpage-value-label']").InnerTextAsync();
        Assert.Contains("1", valueLabelText, StringComparison.Ordinal);

        Assert.True(await TryClickButtonIfEnabledAsync(page, valueChangedDecrementSelector));
        await page.WaitForTimeoutAsync(100);
        var valueLabelAfterDecrement = await page.Locator("[data-automation-id='controls-sample-stepperpage-value-label']").InnerTextAsync();
        Assert.Contains("0", valueLabelAfterDecrement, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ActivityIndicatorPage_IsRunnable_AndProjectsRunningColorSizingAndStoppedState()
    {
        await using var context = await _fixture.NewContextAsync(width: 1200, height: 1200);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "activityindicatorpage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-activityindicatorpage']");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-activityindicatorpage-default']");
            });

        var snapshot = await page.EvaluateAsync<string[]>(
            """
            () => {
                const host = document.querySelector("[data-automation-id='controls-sample-page-activityindicatorpage']");
                const ids = [
                    "controls-sample-activityindicatorpage-default",
                    "controls-sample-activityindicatorpage-themed",
                    "controls-sample-activityindicatorpage-background",
                    "controls-sample-activityindicatorpage-large",
                    "controls-sample-activityindicatorpage-small",
                    "controls-sample-activityindicatorpage-stopped"
                ];

                const elements = ids.map(id => document.querySelector(`[data-automation-id='${id}']`));
                if (!(host instanceof HTMLElement) || elements.some(element => !(element instanceof HTMLElement))) {
                    return ["__missing__", "0", "", "", "", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "", "", "0", "", ""];
                }

                const themed = elements[1];
                const background = elements[2];
                const large = elements[3];
                const small = elements[4];
                const stopped = elements[5];
                const defaultSpinner = elements[0];

                const hostRect = host.getBoundingClientRect();
                const defaultRect = defaultSpinner.getBoundingClientRect();
                const backgroundRect = background.getBoundingClientRect();
                const largeRect = large.getBoundingClientRect();
                const smallRect = small.getBoundingClientRect();
                const hostCenterX = hostRect.left + (hostRect.width / 2);
                const smallCenterX = smallRect.left + (smallRect.width / 2);
                const smallCenterDelta = Math.abs(smallCenterX - hostCenterX);
                const defaultPseudo = getComputedStyle(defaultSpinner, "::before");
                const stoppedStyle = getComputedStyle(stopped);

                return [
                    "ok",
                    elements.length.toString(),
                    defaultSpinner.getAttribute("data-maui-activity-running") ?? "",
                    defaultSpinner.getAttribute("aria-busy") ?? "",
                    getComputedStyle(themed).getPropertyValue("--mirage-activity-color").trim(),
                    defaultRect.width.toString(),
                    defaultRect.height.toString(),
                    backgroundRect.width.toString(),
                    defaultPseudo.width ?? "",
                    defaultPseudo.height ?? "",
                    defaultPseudo.opacity ?? "",
                    largeRect.width.toString(),
                    largeRect.height.toString(),
                    smallRect.width.toString(),
                    smallRect.height.toString(),
                    smallCenterDelta.toString(),
                    stopped.getAttribute("data-maui-activity-running") ?? "",
                    stopped.getAttribute("aria-busy") ?? "",
                    stoppedStyle.display ?? "",
                    stoppedStyle.visibility ?? "",
                    getComputedStyle(background).backgroundColor
                ];
            }
            """);

        Assert.Equal("ok", snapshot[0]);
        Assert.Equal("6", snapshot[1]);
        Assert.Equal("true", snapshot[2]);
        Assert.Equal("true", snapshot[3]);
        Assert.False(string.IsNullOrWhiteSpace(snapshot[4]));

        var defaultWidth = double.Parse(snapshot[5], CultureInfo.InvariantCulture);
        var defaultHeight = double.Parse(snapshot[6], CultureInfo.InvariantCulture);
        var backgroundWidth = double.Parse(snapshot[7], CultureInfo.InvariantCulture);
        var defaultPseudoWidth = double.Parse(snapshot[8].Replace("px", string.Empty, StringComparison.Ordinal), CultureInfo.InvariantCulture);
        var defaultPseudoHeight = double.Parse(snapshot[9].Replace("px", string.Empty, StringComparison.Ordinal), CultureInfo.InvariantCulture);
        var defaultPseudoOpacity = double.Parse(snapshot[10], CultureInfo.InvariantCulture);
        Assert.True(defaultWidth >= 400d);
        Assert.InRange(defaultHeight, 20d, 32d);
        Assert.True(backgroundWidth >= 400d);
        Assert.InRange(defaultPseudoWidth, 18d, 30d);
        Assert.InRange(defaultPseudoHeight, 18d, 30d);
        Assert.True(defaultPseudoOpacity > 0.1d);

        var largeWidth = double.Parse(snapshot[11], CultureInfo.InvariantCulture);
        var largeHeight = double.Parse(snapshot[12], CultureInfo.InvariantCulture);
        Assert.InRange(largeWidth, 148d, 152d);
        Assert.InRange(largeHeight, 148d, 152d);

        var smallWidth = double.Parse(snapshot[13], CultureInfo.InvariantCulture);
        var smallHeight = double.Parse(snapshot[14], CultureInfo.InvariantCulture);
        Assert.InRange(smallWidth, 9d, 12d);
        Assert.InRange(smallHeight, 9d, 12d);

        var smallCenterDelta = double.Parse(snapshot[15], CultureInfo.InvariantCulture);
        Assert.True(smallCenterDelta <= 24d, $"Expected centered small spinner; center delta was {smallCenterDelta:0.###}.");

        Assert.Equal("false", snapshot[16]);
        Assert.Equal("false", snapshot[17]);
        Assert.Equal("none", snapshot[18]);
        Assert.Equal("hidden", snapshot[19]);

        Assert.Contains("255,255,0", snapshot[20].Replace(" ", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AlertsPage_IsRunnable_AndExercisesSharedBrowserDialogs()
    {
        await using var context = await _fixture.NewContextAsync(width: 1200, height: 1200);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "alertspage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-alertspage']");
                await page.WaitForSelectorAsync("[data-automation-id='mirage-dialog-overlay']");
            });

        await ExpectDialogAsync(page, "Alert", "Welcome to the Alerts Page", "modal");
        await page.Locator("[data-automation-id='mirage-dialog-accept']").ClickAsync();
        await page.WaitForSelectorAsync("[data-automation-id='mirage-dialog-overlay']", new() { State = WaitForSelectorState.Detached });

        await page.Locator("[data-automation-id='controls-sample-alertspage-alert-simple']").ClickAsync();
        await ExpectDialogAsync(page, "Alert", "You have been alerted", "modal");
        Assert.Equal("OK", await page.Locator("[data-automation-id='mirage-dialog-accept']").InnerTextAsync());
        Assert.Equal(0, await page.Locator("[data-automation-id='mirage-dialog-cancel']").CountAsync());
        await page.Locator("[data-automation-id='mirage-dialog-accept']").ClickAsync();
        await page.WaitForSelectorAsync("[data-automation-id='mirage-dialog-overlay']", new() { State = WaitForSelectorState.Detached });

        await page.Locator("[data-automation-id='controls-sample-alertspage-alert-yesno']").ClickAsync();
        await ExpectDialogAsync(page, "Question?", "Would you like to play a game", "modal");
        Assert.Equal("Yes", await page.Locator("[data-automation-id='mirage-dialog-accept']").InnerTextAsync());
        Assert.Equal("No", await page.Locator("[data-automation-id='mirage-dialog-cancel']").InnerTextAsync());
        await page.Locator("[data-automation-id='mirage-dialog-cancel']").ClickAsync();
        await page.WaitForSelectorAsync("[data-automation-id='mirage-dialog-overlay']", new() { State = WaitForSelectorState.Detached });

        await page.Locator("[data-automation-id='controls-sample-alertspage-actionsheet-simple']").ClickAsync();
        await ExpectDialogAsync(page, "ActionSheet: Send to?", null, "sheet");
        Assert.Equal("Email", await page.Locator("[data-automation-id='mirage-dialog-option-0']").InnerTextAsync());
        Assert.Equal("Twitter", await page.Locator("[data-automation-id='mirage-dialog-option-1']").InnerTextAsync());
        Assert.Equal("Facebook", await page.Locator("[data-automation-id='mirage-dialog-option-2']").InnerTextAsync());
        Assert.Equal("Cancel", await page.Locator("[data-automation-id='mirage-dialog-cancel']").InnerTextAsync());
        await page.Locator("[data-automation-id='mirage-dialog-option-1']").ClickAsync();
        await page.WaitForSelectorAsync("[data-automation-id='mirage-dialog-overlay']", new() { State = WaitForSelectorState.Detached });

        await page.Locator("[data-automation-id='controls-sample-alertspage-actionsheet-cancel-delete']").ClickAsync();
        await ExpectDialogAsync(page, "ActionSheet: SavePhoto?", null, "sheet");
        Assert.Equal("Delete", await page.Locator("[data-automation-id='mirage-dialog-destruction']").InnerTextAsync());
        await page.Locator("[data-automation-id='mirage-dialog-destruction']").ClickAsync();
        await page.WaitForSelectorAsync("[data-automation-id='mirage-dialog-overlay']", new() { State = WaitForSelectorState.Detached });

        await page.Locator("[data-automation-id='controls-sample-alertspage-question-1']").ClickAsync();
        await ExpectDialogAsync(page, "Question 1", "What's your name?", "modal");
        await page.Locator("[data-automation-id='mirage-dialog-input']").FillAsync("Ada");
        await page.Locator("[data-automation-id='mirage-dialog-accept']").ClickAsync();
        await page.WaitForSelectorAsync("[data-automation-id='mirage-dialog-overlay']", new() { State = WaitForSelectorState.Detached });
        Assert.Equal("Hello Ada.", await page.Locator("[data-automation-id='controls-sample-alertspage-question1-result']").InnerTextAsync());

        await page.Locator("[data-automation-id='controls-sample-alertspage-question-2']").ClickAsync();
        await ExpectDialogAsync(page, "Question 2", "What's 5 + 5?", "modal");
        Assert.Equal("numeric", await page.Locator("[data-automation-id='mirage-dialog-input']").GetAttributeAsync("inputmode"));
        Assert.Equal("2", await page.Locator("[data-automation-id='mirage-dialog-input']").GetAttributeAsync("maxlength"));
        await page.Locator("[data-automation-id='mirage-dialog-input']").FillAsync("10");
        await page.Locator("[data-automation-id='mirage-dialog-accept']").ClickAsync();
        await page.WaitForSelectorAsync("[data-automation-id='mirage-dialog-overlay']", new() { State = WaitForSelectorState.Detached });
        Assert.Equal("Correct.", await page.Locator("[data-automation-id='controls-sample-alertspage-question2-result']").InnerTextAsync());
    }

    [Fact]
    public async Task DevicePage_IsRunnable_AndUsesDefaultPlatformPlusBrowserIdiom()
    {
        await using var context = await _fixture.NewContextAsync();
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "devicepage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-devicepage']");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-devicepage-platform-label']");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-devicepage-idiom-label']");
            });

        Assert.Equal(
            "Default Platform",
            await page.Locator("[data-automation-id='controls-sample-devicepage-platform-label']").InnerTextAsync());
        Assert.Equal(
            "Desktop",
            await page.Locator("[data-automation-id='controls-sample-devicepage-idiom-label']").InnerTextAsync());
    }

    [Fact]
    public async Task ImagePage_IsRunnable_AndExercisesResolverBackedSourcesStreamAndGifPlayback()
    {
        await using var context = await _fixture.NewContextAsync(width: 1200, height: 1400);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "imagepage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-imagepage']");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-imagepage-uri']");
            });

        const string uriSelector = "[data-automation-id='controls-sample-imagepage-uri']";
        const string fileSelector = "[data-automation-id='controls-sample-imagepage-file']";
        const string fontSelector = "[data-automation-id='controls-sample-imagepage-font']";
        const string streamSelector = "[data-automation-id='controls-sample-imagepage-stream']";
        const string switchBoundGifSelector = "[data-automation-id='controls-sample-imagepage-gif-switch-bound']";
        const string animatedGifSelector = "[data-automation-id='controls-sample-imagepage-animated-gif']";

        var uriSource = await page.Locator(uriSelector).GetAttributeAsync("src");
        Assert.StartsWith("https://", uriSource, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("async", await page.Locator(uriSelector).GetAttributeAsync("decoding"));
        Assert.Equal("eager", await page.Locator(uriSelector).GetAttributeAsync("loading"));

        var fileSource = await page.Locator(fileSelector).GetAttributeAsync("src");
        Assert.StartsWith("/_mirage/assets/", fileSource, StringComparison.Ordinal);

        var fontSource = await page.Locator(fontSelector).GetAttributeAsync("src");
        Assert.StartsWith("data:image/svg+xml;base64,", fontSource, StringComparison.Ordinal);

        await page.WaitForFunctionAsync(
            "(selector) => { const element = document.querySelector(selector); const src = element?.getAttribute('src') ?? ''; return src.startsWith('blob:'); }",
            streamSelector);
        var streamSource = await page.Locator(streamSelector).GetAttributeAsync("src");
        Assert.StartsWith("blob:", streamSource, StringComparison.Ordinal);

        var initialSwitchGifSource = await page.Locator(switchBoundGifSelector).GetAttributeAsync("src");
        Assert.False(string.IsNullOrWhiteSpace(initialSwitchGifSource));

        await page.Locator("[data-automation-id='controls-sample-imagepage-animation-switch']").ClickAsync();
        await page.WaitForFunctionAsync(
            "(selector) => { const element = document.querySelector(selector); return !element || !element.hasAttribute('src'); }",
            switchBoundGifSelector);
        Assert.Null(await page.Locator(switchBoundGifSelector).GetAttributeAsync("src"));

        await page.Locator("[data-automation-id='controls-sample-imagepage-animation-switch']").ClickAsync();
        await page.WaitForFunctionAsync(
            "(selector) => { const element = document.querySelector(selector); const src = element?.getAttribute('src') ?? ''; return src.length > 0; }",
            switchBoundGifSelector);
        Assert.Equal(initialSwitchGifSource, await page.Locator(switchBoundGifSelector).GetAttributeAsync("src"));

        await page.Locator("[data-automation-id='controls-sample-imagepage-start-stop']").ClickAsync();
        await page.WaitForFunctionAsync(
            "(selector) => { const element = document.querySelector(selector); return !element || !element.hasAttribute('src'); }",
            animatedGifSelector);
        Assert.Null(await page.Locator(animatedGifSelector).GetAttributeAsync("src"));
        Assert.Equal("loaded", await page.Locator(animatedGifSelector).GetAttributeAsync("data-maui-image-state"));

        await page.Locator("[data-automation-id='controls-sample-imagepage-start-stop']").ClickAsync();
        await page.WaitForFunctionAsync(
            "(selector) => { const element = document.querySelector(selector); const src = element?.getAttribute('src') ?? ''; return src.length > 0; }",
            animatedGifSelector);

        await page.EvaluateAsync(
            "(selector) => { const element = document.querySelector(selector); if (element) { element.dispatchEvent(new Event('error')); } }",
            animatedGifSelector);
        await page.WaitForFunctionAsync(
            "(selector) => (document.querySelector(selector)?.getAttribute('data-maui-image-state') ?? '') === 'error'",
            animatedGifSelector);
        Assert.Equal("error", await page.Locator(animatedGifSelector).GetAttributeAsync("data-maui-image-state"));
    }

    [Fact]
    public async Task BrushesPage_IsRunnable_AndRendersDefaultTabBrushScenarios()
    {
        await using var context = await _fixture.NewContextAsync(width: 1100, height: 1300);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "brushespage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-brushespage']");
            });

        var brushesSnapshot = await page.EvaluateAsync<string[]>(
            """
            () => {
                const host = document.querySelector("[data-automation-id='controls-sample-page-brushespage']");
                if (!(host instanceof HTMLElement)) {
                    return ["__missing__", "0", "0"];
                }

                const labels = Array
                    .from(host.querySelectorAll("[data-mirage-kind='Label']"))
                    .map(label => (label.textContent ?? "").trim());

                const hasSolidHeadline = labels.some(text => text.includes("SolidColorBrush (Predefined Brush)"));
                const swatchCount = Array
                    .from(host.querySelectorAll("[data-mirage-kind='ContentContainer'], [data-mirage-kind='Border']"))
                    .filter(node => {
                        if (!(node instanceof HTMLElement)) {
                            return false;
                        }

                        const style = window.getComputedStyle(node);
                        return parseFloat(style.width || "0") >= 100 && parseFloat(style.height || "0") >= 100;
                    })
                    .length;

                return ["ok", hasSolidHeadline ? "1" : "0", swatchCount.toString()];
            }
            """);

        Assert.Equal("ok", brushesSnapshot[0]);
        Assert.Equal("1", brushesSnapshot[1]);
        Assert.True(int.Parse(brushesSnapshot[2], CultureInfo.InvariantCulture) >= 1);
    }

    [Fact]
    public async Task ContentPageGallery_IsRunnable_AndNavigatesBackgroundSubpages()
    {
        await using var context = await _fixture.NewContextAsync(width: 1100, height: 900);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "contentpagegallery");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-contentpagegallery']");
            });

        const string galleryHostSelector = "[data-automation-id='controls-sample-page-contentpagegallery']";
        const string backgroundButtonSelector = "[data-automation-id='controls-sample-gallery-open-contentpage-background']";
        const string backgroundImageButtonSelector = "[data-automation-id='controls-sample-gallery-open-contentpage-backgroundimage']";
        Assert.Equal(1, await page.Locator(galleryHostSelector).CountAsync());
        Assert.Equal(1, await page.Locator(backgroundButtonSelector).CountAsync());
        Assert.Equal(1, await page.Locator(backgroundImageButtonSelector).CountAsync());

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator(backgroundButtonSelector).ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-contentpagebackgroundgallery']");
            });

        const string hostSelector = "[data-automation-id='controls-sample-page-contentpagebackgroundgallery']";
        Assert.Equal(1, await page.Locator(hostSelector).CountAsync());

        var labelCount = await page.Locator($"{hostSelector} [data-mirage-kind='Label']").Filter(new() { HasTextString = "ContentPage" }).CountAsync();
        Assert.True(labelCount > 0);

        var backgroundSnapshot = await page.EvaluateAsync<string[]>(
            """
            (selector) => {
                const host = document.querySelector(selector);
                if (!(host instanceof HTMLElement)) {
                    return ["__missing__", "", ""];
                }

                const style = getComputedStyle(host);
                const backgroundImage = style.backgroundImage ?? "";
                let decodedGradientSvg = "";
                const prefix = 'url("data:image/svg+xml;base64,';
                if (backgroundImage.startsWith(prefix) && backgroundImage.endsWith('")')) {
                    const base64 = backgroundImage.slice(prefix.length, -2);
                    try {
                        decodedGradientSvg = atob(base64);
                    } catch {
                        decodedGradientSvg = "";
                    }
                }

                return [backgroundImage, decodedGradientSvg, style.backgroundColor ?? ""];
            }
            """,
            hostSelector);

        Assert.NotEqual("__missing__", backgroundSnapshot[0]);
        Assert.StartsWith("url(\"data:image/svg+xml;base64,", backgroundSnapshot[0], StringComparison.Ordinal);
        Assert.Contains("linearGradient", backgroundSnapshot[1], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rgba(255,255,0,1)", backgroundSnapshot[1], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rgba(0,128,0,1)", backgroundSnapshot[1], StringComparison.OrdinalIgnoreCase);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='mirage-navigation-back-button']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-contentpagegallery']");
            });

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator(backgroundImageButtonSelector).ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-contentpagebackgroundimagegallery']");
            });

        const string backgroundImageHostSelector = "[data-automation-id='controls-sample-page-contentpagebackgroundimagegallery']";
        Assert.Equal(1, await page.Locator(backgroundImageHostSelector).CountAsync());

        var backgroundImageLabelCount = await page
            .Locator($"{backgroundImageHostSelector} [data-mirage-kind='Label']")
            .Filter(new() { HasTextString = "ContentPage" })
            .CountAsync();
        Assert.True(backgroundImageLabelCount > 0);

        var backgroundImageSnapshot = await page.EvaluateAsync<string[]>(
            """
            (selector) => {
                const host = document.querySelector(selector);
                if (!(host instanceof HTMLElement)) {
                    return ["__missing__", "", "", ""];
                }

                const style = getComputedStyle(host);
                return [
                    style.backgroundImage ?? "",
                    style.backgroundRepeat ?? "",
                    style.backgroundSize ?? "",
                    style.backgroundPosition ?? ""
                ];
            }
            """,
            backgroundImageHostSelector);

        Assert.NotEqual("__missing__", backgroundImageSnapshot[0]);
        Assert.StartsWith("url(\"", backgroundImageSnapshot[0], StringComparison.Ordinal);
        Assert.Contains("/_mirage/assets/", backgroundImageSnapshot[0], StringComparison.Ordinal);
        Assert.Equal("no-repeat", backgroundImageSnapshot[1]);
        Assert.Equal("100% 100%", backgroundImageSnapshot[2]);
    }

    [Fact]
    public async Task BorderPage_IsRunnable_AndExercisesBorderGalleryScenarios()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 1600);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "borderpage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-borderpage']");
            });

        var navButtonIds = new[]
        {
            "controls-sample-borderpage-open-border-playground",
            "controls-sample-borderpage-open-border-clipping-playground",
            "controls-sample-borderpage-open-border-using-styles",
            "controls-sample-borderpage-open-border-using-content-layout",
            "controls-sample-borderpage-open-border-stroke-options",
            "controls-sample-borderpage-open-border-without-stroke",
            "controls-sample-borderpage-open-border-resize-content",
            "controls-sample-borderpage-open-border-alignment"
        };

        foreach (var navButtonId in navButtonIds)
        {
            Assert.Equal(1, await page.Locator($"[data-automation-id='{navButtonId}']").CountAsync());
        }

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-borderpage-open-border-playground']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-borderplayground']");
            });

        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-borderplayground-border']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-borderplayground-content-picker']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-borderplayground-shape-picker']").CountAsync());

        var defaultShapeSelection = await page.EvaluateAsync<string[]>(
            """
            () => {
                const picker = document.querySelector("[data-automation-id='controls-sample-borderplayground-shape-picker']");
                if (!(picker instanceof HTMLSelectElement)) {
                    return ["__missing__", "__missing__"];
                }

                const selectedText = picker.selectedOptions.length > 0
                    ? (picker.selectedOptions[0].textContent ?? "").trim()
                    : "";
                return [picker.value ?? "", selectedText];
            }
            """);

        Assert.Equal("1", defaultShapeSelection[0]);
        Assert.Equal("RoundRectangle", defaultShapeSelection[1]);

        var borderPlaygroundDefaultSnapshot = await page.EvaluateAsync<string[]>(
            """
            () => {
                const host = document.querySelector("[data-automation-id='controls-sample-borderplayground-border']");
                const label = host?.querySelector("[data-mirage-kind='Label']");
                const path = host?.querySelector("[data-maui-border-part='stroke-path']");
                if (!(host instanceof HTMLElement)) {
                    return ["__missing__", "", "", "", "", "0", "0"];
                }

                const hostRect = host.getBoundingClientRect();
                const pathRect = path?.getBoundingClientRect();
                const widthRatio = pathRect && hostRect.width > 0 ? (pathRect.width / hostRect.width) : 0;
                const heightRatio = pathRect && hostRect.height > 0 ? (pathRect.height / hostRect.height) : 0;

                return [
                    "ok",
                    (label?.textContent ?? "").trim(),
                    getComputedStyle(host).paddingLeft ?? "",
                    path?.getAttribute("stroke-dasharray") ?? "",
                    path?.getAttribute("stroke-dashoffset") ?? "",
                    widthRatio.toString(),
                    heightRatio.toString()
                ];
            }
            """);

        Assert.Equal("ok", borderPlaygroundDefaultSnapshot[0]);
        Assert.Equal("Just a Label", borderPlaygroundDefaultSnapshot[1]);
        var defaultPaddingLeft = double.Parse(borderPlaygroundDefaultSnapshot[2].Replace("px", string.Empty, StringComparison.Ordinal), CultureInfo.InvariantCulture);
        Assert.True(defaultPaddingLeft >= 4.5d);
        Assert.Equal("5 5", borderPlaygroundDefaultSnapshot[3]);
        Assert.Equal("5", borderPlaygroundDefaultSnapshot[4]);
        Assert.True(double.Parse(borderPlaygroundDefaultSnapshot[5], CultureInfo.InvariantCulture) >= 0.9d);
        Assert.True(double.Parse(borderPlaygroundDefaultSnapshot[6], CultureInfo.InvariantCulture) >= 0.9d);

        await SetSelectIndexAsync(page, "[data-automation-id='controls-sample-borderplayground-content-picker']", 1);
        await SetInputValueAsync(page, "[data-automation-id='controls-sample-borderplayground-corner-top-left']", 60);
        await page.WaitForTimeoutAsync(120);
        var borderTopLeftCornerHitSnapshot = await page.EvaluateAsync<string[]>(
            """
            () => {
                const host = document.querySelector("[data-automation-id='controls-sample-borderplayground-border']");
                const image = host?.querySelector("img");
                if (!(host instanceof HTMLElement) || !(image instanceof HTMLImageElement)) {
                    return ["__missing__", "", "0"];
                }

                const rect = host.getBoundingClientRect();
                const probeX = rect.left + 8;
                const probeY = rect.top + 8;
                const hit = document.elementFromPoint(probeX, probeY);
                const hitIsImage = hit === image || (hit instanceof Node && image.contains(hit));
                const hostStyle = getComputedStyle(host);
                const imageStyle = getComputedStyle(image);
                return [
                    "ok",
                    hit?.tagName ?? "",
                    hitIsImage ? "1" : "0",
                    hostStyle.clipPath ?? "",
                    hostStyle.borderRadius ?? "",
                    hostStyle.overflow ?? "",
                    imageStyle.clipPath ?? "",
                    imageStyle.borderRadius ?? "",
                    imageStyle.overflow ?? ""
                ];
            }
            """);
        Assert.Equal("ok", borderTopLeftCornerHitSnapshot[0]);
        Assert.True(
            string.Equals(borderTopLeftCornerHitSnapshot[2], "0", StringComparison.Ordinal),
            $"Expected clipped top-left corner probe to avoid image hit but got tag={borderTopLeftCornerHitSnapshot[1]} hitFlag={borderTopLeftCornerHitSnapshot[2]} " +
            $"hostClipPath='{borderTopLeftCornerHitSnapshot[3]}' hostBorderRadius='{borderTopLeftCornerHitSnapshot[4]}' hostOverflow='{borderTopLeftCornerHitSnapshot[5]}' " +
            $"imageClipPath='{borderTopLeftCornerHitSnapshot[6]}' imageBorderRadius='{borderTopLeftCornerHitSnapshot[7]}' imageOverflow='{borderTopLeftCornerHitSnapshot[8]}'.");

        await SetInputValueAsync(page, "[data-automation-id='controls-sample-borderplayground-corner-top-left']", 20);
        await SetInputValueAsync(page, "[data-automation-id='controls-sample-borderplayground-corner-bottom-left']", 18);
        await SetInputValueAsync(page, "[data-automation-id='controls-sample-borderplayground-corner-bottom-right']", 4);
        await page.WaitForTimeoutAsync(120);

        var borderCornerSnapshot = await page.EvaluateAsync<string[]>(
            """
            () => {
                const host = document.querySelector("[data-automation-id='controls-sample-borderplayground-border']");
                if (!(host instanceof HTMLElement)) {
                    return ["__missing__", ""];
                }

                return [
                    "ok",
                    host.style.borderRadius ?? ""
                ];
            }
            """);
        Assert.Equal("ok", borderCornerSnapshot[0]);
        Assert.Equal("20px 0px 4px 18px", borderCornerSnapshot[1]);

        await SetSelectIndexAsync(page, "[data-automation-id='controls-sample-borderplayground-content-picker']", 1);
        await SetSelectIndexAsync(page, "[data-automation-id='controls-sample-borderplayground-linejoin-picker']", 2);
        await SetSelectIndexAsync(page, "[data-automation-id='controls-sample-borderplayground-linecap-picker']", 1);
        await SetInputTextAsync(page, "[data-automation-id='controls-sample-borderplayground-dash-entry']", "4, 2");
        await SetInputValueAsync(page, "[data-automation-id='controls-sample-borderplayground-dash-offset-slider']", 1.5);
        await SetInputValueAsync(page, "[data-automation-id='controls-sample-borderplayground-width-slider']", 7);
        await SetSelectIndexAsync(page, "[data-automation-id='controls-sample-borderplayground-shape-picker']", 2);
        await page.WaitForFunctionAsync(
            """
            () => {
                const host = document.querySelector("[data-automation-id='controls-sample-borderplayground-border']");
                if (!(host instanceof HTMLElement)) {
                    return false;
                }

                const clipPath = getComputedStyle(host).clipPath ?? "";
                return clipPath.toLowerCase().includes("ellipse(");
            }
            """,
            options: new()
            {
                Timeout = 5000
            });

        var playgroundSnapshot = await page.EvaluateAsync<string[]>(
            """
            () => {
                const host = document.querySelector("[data-automation-id='controls-sample-borderplayground-border']");
                if (!(host instanceof HTMLElement)) {
                    return ["__missing__", "", "", "", "", "", "", "", ""];
                }

                const image = host.querySelector("img");
                const path = host.querySelector("[data-maui-border-part='stroke-path']");
                const defs = host.querySelector("[data-maui-border-part='stroke-defs']");
                return [
                    "ok",
                    image ? "1" : "0",
                    getComputedStyle(host).clipPath ?? "",
                    path?.getAttribute("stroke") ?? "",
                    path?.getAttribute("stroke-dasharray") ?? "",
                    path?.getAttribute("stroke-dashoffset") ?? "",
                    path?.getAttribute("stroke-linejoin") ?? "",
                    path?.getAttribute("stroke-linecap") ?? "",
                    defs?.innerHTML ?? ""
                ];
            }
            """);

        Assert.Equal("ok", playgroundSnapshot[0]);
        Assert.Equal("1", playgroundSnapshot[1]);
        Assert.Contains("ellipse(", playgroundSnapshot[2], StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("url(#", playgroundSnapshot[3], StringComparison.Ordinal);
        Assert.Equal("28 14", playgroundSnapshot[4]);
        Assert.Equal("10.5", playgroundSnapshot[5]);
        Assert.Equal("bevel", playgroundSnapshot[6]);
        Assert.Equal("round", playgroundSnapshot[7]);
        Assert.Contains("<lineargradient", playgroundSnapshot[8], StringComparison.OrdinalIgnoreCase);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='mirage-navigation-back-button']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-borderpage']");
            });

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-borderpage-open-border-clipping-playground']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-borderclipplayground']");
            });

        await SetSelectIndexAsync(page, "[data-automation-id='controls-sample-borderclipplayground-shape-picker']", 2);
        await SetInputValueAsync(page, "[data-automation-id='controls-sample-borderclipplayground-width-slider']", 12);
        await page.WaitForTimeoutAsync(120);
        var clipSnapshot = await page.EvaluateAsync<string[]>(
            """
            () => {
                const host = document.querySelector("[data-automation-id='controls-sample-borderclipplayground-border']");
                if (!(host instanceof HTMLElement)) {
                    return ["__missing__", "", "", ""];
                }

                const widthLabel = Array
                    .from(document.querySelectorAll("[data-automation-id='controls-sample-page-borderclipplayground'] [data-mirage-kind='Label']"))
                    .map(label => (label.textContent ?? "").trim())
                    .find(text => text.includes("Border Width:")) ?? "";

                return [
                    "ok",
                    getComputedStyle(host).clipPath ?? "",
                    widthLabel,
                    getComputedStyle(host).overflow ?? ""
                ];
            }
            """);
        Assert.Equal("ok", clipSnapshot[0]);
        Assert.Contains("ellipse(", clipSnapshot[1], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("12", clipSnapshot[2], StringComparison.Ordinal);
        Assert.Equal("hidden", clipSnapshot[3]);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='mirage-navigation-back-button']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-borderpage']");
            });

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-borderpage-open-border-using-styles']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-borderstyles']");
            });

        var borderStylesInfoBefore = await page.Locator("[data-automation-id='controls-sample-borderstyles-update-info']").InnerTextAsync();
        await page.Locator("[data-automation-id='controls-sample-borderstyles-increase-corner']").ClickAsync();
        await page.WaitForTimeoutAsync(100);
        var borderStylesInfoAfter = await page.Locator("[data-automation-id='controls-sample-borderstyles-update-info']").InnerTextAsync();
        Assert.NotEqual(borderStylesInfoBefore, borderStylesInfoAfter);
        Assert.Contains("RoundRectangle", borderStylesInfoAfter, StringComparison.Ordinal);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='mirage-navigation-back-button']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-borderpage']");
            });

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-borderpage-open-border-using-content-layout']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-borderlayout']");
            });

        await SetInputValueAsync(page, "[data-automation-id='controls-sample-borderlayout-width-slider']", 18);
        await page.WaitForTimeoutAsync(120);
        var strokeWidths = await page.EvaluateAsync<double[]>(
            """
            () => {
                const paths = Array
                    .from(document.querySelectorAll("[data-automation-id='controls-sample-page-borderlayout'] [data-maui-border-part='stroke-path']"));
                return paths
                    .map(path => parseFloat(path.getAttribute("stroke-width") ?? "NaN"))
                    .filter(width => Number.isFinite(width));
            }
            """);
        Assert.Contains(strokeWidths, width => width >= 17.5d && width <= 18.5d);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='mirage-navigation-back-button']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-borderpage']");
            });

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-borderpage-open-border-stroke-options']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-borderstroke']");
            });

        var strokeThicknessVariants = await page.EvaluateAsync<string[]>(
            """
            () => {
                const paths = Array.from(document.querySelectorAll("[data-automation-id='controls-sample-page-borderstroke'] [data-maui-border-part='stroke-path']"));
                return paths.slice(0, 3).map(path => path.getAttribute("stroke-width") ?? "");
            }
            """);
        Assert.Contains("1", strokeThicknessVariants);
        Assert.Contains("5", strokeThicknessVariants);
        Assert.Contains("10", strokeThicknessVariants);

        var strokeResizeBefore = await page.EvaluateAsync<double>(
            "(selector) => document.querySelector(selector)?.getBoundingClientRect().height ?? 0",
            "[data-automation-id='controls-sample-borderstroke-rectangle-3']");
        await SetInputValueAsync(page, "[data-automation-id='controls-sample-borderstroke-height-slider']", 100);
        await page.WaitForTimeoutAsync(100);
        var strokeResizeAfter = await page.EvaluateAsync<double>(
            "(selector) => document.querySelector(selector)?.getBoundingClientRect().height ?? 0",
            "[data-automation-id='controls-sample-borderstroke-rectangle-3']");
        Assert.True(strokeResizeAfter > strokeResizeBefore);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='mirage-navigation-back-button']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-borderpage']");
            });

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-borderpage-open-border-without-stroke']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-borderless']");
            });

        var borderlessSnapshot = await page.EvaluateAsync<string[]>(
            """
            () => {
                const borders = Array.from(document.querySelectorAll("[data-automation-id='controls-sample-page-borderless'] [data-mirage-kind='Border']"));
                if (borders.length < 2) {
                    return ["0", "", ""];
                }

                const topBackground = getComputedStyle(borders[0]).backgroundColor ?? "";
                const bottomBackground = getComputedStyle(borders[1]).backgroundColor ?? "";
                const hasVisibleStroke = borders.some(border => {
                    const path = border.querySelector("[data-maui-border-part='stroke-path']");
                    const stroke = path?.getAttribute("stroke");
                    const width = path?.getAttribute("stroke-width");
                    return !!stroke && stroke !== "none" && width && width !== "0";
                });

                return [borders.length.toString(), topBackground, bottomBackground, hasVisibleStroke ? "1" : "0"];
            }
            """);
        Assert.Equal("2", borderlessSnapshot[0]);
        Assert.NotEqual("rgba(0, 0, 0, 0)", borderlessSnapshot[1]);
        Assert.NotEqual("rgba(0, 0, 0, 0)", borderlessSnapshot[2]);
        Assert.Equal("0", borderlessSnapshot[3]);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='mirage-navigation-back-button']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-borderpage']");
            });

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-borderpage-open-border-resize-content']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-borderresizecontent']");
            });

        await SetInputTextAsync(page, "[data-automation-id='controls-sample-borderresizecontent-text-entry']", "*");
        await SetInputValueAsync(page, "[data-automation-id='controls-sample-borderresizecontent-font-slider']", 120);
        await SetInputValueAsync(page, "[data-automation-id='controls-sample-borderresizecontent-image-scale-slider']", 3);
        await page.WaitForTimeoutAsync(120);

        var resizeSnapshot = await page.EvaluateAsync<string[]>(
            """
            () => {
                const circleImageBorder = document.querySelector("[data-automation-id='controls-sample-borderresizecontent-circle-image']");
                const triangleImageBorder = document.querySelector("[data-automation-id='controls-sample-borderresizecontent-triangle-image']");
                const hasUpdatedLabel = Array
                    .from(document.querySelectorAll("[data-automation-id='controls-sample-page-borderresizecontent'] [data-mirage-kind='Label']"))
                    .some(label => (label.textContent ?? "").includes("*"));
                const circleImage = circleImageBorder?.querySelector("img");

                return [
                    circleImage ? "1" : "0",
                    triangleImageBorder ? "1" : "0",
                    getComputedStyle(triangleImageBorder ?? document.body).clipPath ?? "",
                    hasUpdatedLabel ? "1" : "0",
                    circleImage ? circleImage.style.transform ?? "" : ""
                ];
            }
            """);
        Assert.Equal("1", resizeSnapshot[0]);
        Assert.Equal("1", resizeSnapshot[1]);
        Assert.Contains("polygon(", resizeSnapshot[2], StringComparison.OrdinalIgnoreCase);
        Assert.Equal("1", resizeSnapshot[3]);
        Assert.Contains("scale(", resizeSnapshot[4], StringComparison.Ordinal);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='mirage-navigation-back-button']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-borderpage']");
            });

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-borderpage-open-border-alignment']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-borderalignment']");
            });

        var alignmentSnapshot = await page.EvaluateAsync<string[]>(
            """
            () => {
                const host = document.querySelector("[data-automation-id='controls-sample-page-borderalignment']");
                const start = document.querySelector("[data-automation-id='controls-sample-borderalignment-border-1']");
                const center = document.querySelector("[data-automation-id='controls-sample-borderalignment-border-2']");
                const end = document.querySelector("[data-automation-id='controls-sample-borderalignment-border-3']");
                const fill = document.querySelector("[data-automation-id='controls-sample-borderalignment-border-4']");
                const startStrokePath = start?.querySelector("[data-maui-border-part='stroke-path']");
                const fillStrokePath = fill?.querySelector("[data-maui-border-part='stroke-path']");
                const startStrokeOverlay = start?.querySelector("[data-maui-border-part='stroke-overlay']");
                const fillStrokeOverlay = fill?.querySelector("[data-maui-border-part='stroke-overlay']");
                if (!(host instanceof HTMLElement) || !(start instanceof HTMLElement) || !(center instanceof HTMLElement) || !(end instanceof HTMLElement) || !(fill instanceof HTMLElement)) {
                    return ["__missing__", "0", "0", "0", "0", "0", "0", "", "", "0", "0", "", "", "", ""];
                }

                const hostRect = host.getBoundingClientRect();
                const startRect = start.getBoundingClientRect();
                const centerRect = center.getBoundingClientRect();
                const endRect = end.getBoundingClientRect();
                const fillRect = fill.getBoundingClientRect();
                const startStrokeRect = startStrokePath?.getBoundingClientRect();
                const fillStrokeRect = fillStrokePath?.getBoundingClientRect();

                const hostCenter = hostRect.left + (hostRect.width / 2);
                const centerCenter = centerRect.left + (centerRect.width / 2);
                const centerDelta = Math.abs(centerCenter - hostCenter);
                const endRightGap = Math.abs(hostRect.right - endRect.right);
                const startStrokeWidthRatio = startStrokeRect && startRect.width > 0 ? (startStrokeRect.width / startRect.width) : 0;
                const fillStrokeWidthRatio = fillStrokeRect && fillRect.width > 0 ? (fillStrokeRect.width / fillRect.width) : 0;

                return [
                    "ok",
                    startRect.left.toString(),
                    centerDelta.toString(),
                    endRightGap.toString(),
                    fillRect.width.toString(),
                    startRect.width.toString(),
                    endRect.width.toString(),
                    startStrokePath?.namespaceURI ?? "",
                    startStrokePath?.getAttribute("stroke") ?? "",
                    startStrokeWidthRatio.toString(),
                    fillStrokeWidthRatio.toString(),
                    startStrokeOverlay?.getAttribute("viewBox") ?? "",
                    fillStrokeOverlay?.getAttribute("viewBox") ?? "",
                    startRect.width.toString(),
                    startStrokeRect?.width?.toString() ?? "0"
                ];
            }
            """);

        Assert.Equal("ok", alignmentSnapshot[0]);
        Assert.True(double.Parse(alignmentSnapshot[2], CultureInfo.InvariantCulture) <= 24d);
        Assert.True(double.Parse(alignmentSnapshot[3], CultureInfo.InvariantCulture) <= 24d);
        Assert.True(
            double.Parse(alignmentSnapshot[4], CultureInfo.InvariantCulture)
            > double.Parse(alignmentSnapshot[5], CultureInfo.InvariantCulture));
        Assert.Equal("http://www.w3.org/2000/svg", alignmentSnapshot[7]);
        Assert.Contains("255,0,0", alignmentSnapshot[8].Replace(" ", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
        var startStrokeWidthRatio = double.Parse(alignmentSnapshot[9], CultureInfo.InvariantCulture);
        var fillStrokeWidthRatio = double.Parse(alignmentSnapshot[10], CultureInfo.InvariantCulture);
        Assert.True(
            startStrokeWidthRatio >= 0.45d,
            $"Expected start stroke ratio >= 0.45 but got {startStrokeWidthRatio:0.###}. startViewBox={alignmentSnapshot[11]} startHostWidth={alignmentSnapshot[13]} startStrokeWidth={alignmentSnapshot[14]}");
        Assert.True(
            fillStrokeWidthRatio >= 0.9d,
            $"Expected fill stroke ratio >= 0.9 but got {fillStrokeWidthRatio:0.###}. fillViewBox={alignmentSnapshot[12]}");
    }

    [Fact]
    public async Task MapsGallery_IsRunnable_AndExercisesAllMapSubPages()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 980);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "mapsgallery");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-mapsgallery']");
            });

        var navButtonIds = new[]
        {
            "controls-sample-mapsgallery-open-basic-map",
            "controls-sample-mapsgallery-open-map-type",
            "controls-sample-mapsgallery-open-map-pins",
            "controls-sample-mapsgallery-open-pins-itemssource",
            "controls-sample-mapsgallery-open-circle",
            "controls-sample-mapsgallery-open-polygon"
        };

        foreach (var navButtonId in navButtonIds)
        {
            Assert.Equal(1, await page.Locator($"[data-automation-id='{navButtonId}']").CountAsync());
        }

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-mapsgallery-open-basic-map']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-basicmapgallery']");
            });

        await WaitForMapReadyAsync(page, "[data-automation-id='controls-sample-basicmapgallery-map']");
        Assert.Equal(4, await page.Locator("[data-automation-id='controls-sample-page-basicmapgallery'] input[data-mirage-kind='Switch']").CountAsync());

        var zoomSwitchSelector = "[data-automation-id='controls-sample-basicmapgallery-zoom-enabled-switch']";
        var zoomBefore = await ReadCheckedAsync(page, zoomSwitchSelector);
        await page.Locator(zoomSwitchSelector).ClickAsync();
        await page.WaitForTimeoutAsync(80);
        var zoomAfter = await ReadCheckedAsync(page, zoomSwitchSelector);
        Assert.NotEqual(zoomBefore, zoomAfter);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='mirage-navigation-back-button']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-mapsgallery']");
            });

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-mapsgallery-open-map-type']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-maptypegallery']");
            });

        await WaitForMapReadyAsync(page, "[data-automation-id='controls-sample-maptypegallery-map']");
        await SetSelectIndexAsync(page, "[data-automation-id='controls-sample-maptypegallery-type-picker']", 1);
        await SetInputValueAsync(page, "[data-automation-id='controls-sample-maptypegallery-zoom-slider']", 14);
        await WaitForMapReadyAsync(page, "[data-automation-id='controls-sample-maptypegallery-map']");

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='mirage-navigation-back-button']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-mapsgallery']");
            });

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-mapsgallery-open-map-pins']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-mappinsgallery']");
            });

        const string mapPinsSelector = "[data-automation-id='controls-sample-mappinsgallery-map']";
        await WaitForMapReadyAsync(page, mapPinsSelector);
        var pinCount = await ReadMapCountAsync(page, mapPinsSelector, "data-mirage-map-pin-count");
        Assert.True(pinCount >= 1);

        await page.Locator("[data-automation-id='controls-sample-mappinsgallery-add-pin']").ClickAsync();
        await WaitForMapCountAsync(page, mapPinsSelector, "data-mirage-map-pin-count", pinCount + 1);

        await page.Locator("[data-automation-id='controls-sample-mappinsgallery-remove-pin']").ClickAsync();
        await WaitForMapCountAsync(page, mapPinsSelector, "data-mirage-map-pin-count", pinCount);

        await page.Locator("[data-automation-id='controls-sample-mappinsgallery-add-ten-pins']").ClickAsync();
        await WaitForMapCountAsync(page, mapPinsSelector, "data-mirage-map-pin-count", pinCount + 11);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='mirage-navigation-back-button']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-mapsgallery']");
            });

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-mapsgallery-open-pins-itemssource']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-pinitemssourcegallery']");
            });

        const string itemsSourceMapSelector = "[data-automation-id='controls-sample-pinitemssourcegallery-map']";
        await WaitForMapReadyAsync(page, itemsSourceMapSelector);
        var initialItemsSourcePinCount = await ReadMapCountAsync(page, itemsSourceMapSelector, "data-mirage-map-pin-count");
        Assert.True(initialItemsSourcePinCount >= 3);

        await page.Locator("[data-automation-id='controls-sample-pinitemssourcegallery-add']").ClickAsync();
        await WaitForMapCountAsync(page, itemsSourceMapSelector, "data-mirage-map-pin-count", initialItemsSourcePinCount + 1);

        await page.Locator("[data-automation-id='controls-sample-pinitemssourcegallery-remove']").ClickAsync();
        await WaitForMapCountAsync(page, itemsSourceMapSelector, "data-mirage-map-pin-count", initialItemsSourcePinCount);

        await page.Locator("[data-automation-id='controls-sample-pinitemssourcegallery-clear']").ClickAsync();
        await WaitForMapCountAsync(page, itemsSourceMapSelector, "data-mirage-map-pin-count", 0);

        await page.Locator("[data-automation-id='controls-sample-pinitemssourcegallery-add']").ClickAsync();
        await WaitForMapCountAsync(page, itemsSourceMapSelector, "data-mirage-map-pin-count", 1);

        await page.Locator("[data-automation-id='controls-sample-pinitemssourcegallery-replace']").ClickAsync();
        await WaitForMapCountAsync(page, itemsSourceMapSelector, "data-mirage-map-pin-count", 1);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='mirage-navigation-back-button']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-mapsgallery']");
            });

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-mapsgallery-open-circle']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-circlegallery']");
            });

        const string circleMapSelector = "[data-automation-id='controls-sample-circlegallery-map']";
        await WaitForMapReadyAsync(page, circleMapSelector);
        var circleElementCount = await ReadMapCountAsync(page, circleMapSelector, "data-mirage-map-element-count");
        Assert.True(circleElementCount >= 1);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='mirage-navigation-back-button']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-mapsgallery']");
            });

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-mapsgallery-open-polygon']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-polygonsgallery']");
            });

        const string polygonMapSelector = "[data-automation-id='controls-sample-polygonsgallery-map']";
        await WaitForMapReadyAsync(page, polygonMapSelector);
        var polygonElementCount = await ReadMapCountAsync(page, polygonMapSelector, "data-mirage-map-element-count");
        Assert.True(polygonElementCount >= 3);

        await page.Locator("[data-automation-id='controls-sample-polygonsgallery-clear-map-elements']").ClickAsync();
        await WaitForMapCountAsync(page, polygonMapSelector, "data-mirage-map-element-count", 0);
    }

    [Fact]
    public async Task CollectionViewGallery_IsRunnable_AndNavigatesAllNonDeferredLeafPages()
    {
        await using var context = await _fixture.NewContextAsync(width: 1440, height: 1600);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "collectionviewgallery");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-collectionviewgallery']");
            });

        var openedPageHosts = new HashSet<string>(StringComparer.Ordinal);
        await TraverseCollectionViewGalleryTreeAsync(page, diagnostics, openedPageHosts, depth: 0, maxDepth: 8);

        var missingHosts = ExpectedCollectionViewReachablePageHosts
            .Where(expectedHost => !openedPageHosts.Contains(expectedHost))
            .ToArray();
        Assert.Empty(missingHosts);
    }

    [Fact]
    public async Task ClippingPage_GridFrame_RendersAtExpectedSize_AndIsHorizontallyCentered()
    {
        await using var context = await _fixture.NewContextAsync();
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "clippingpage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-clippingpage']");
            });

        var gridMetrics = await page.EvaluateAsync<string[]>(
            """
            () => {
                const pageRoot = document.querySelector("[data-automation-id='controls-sample-page-clippingpage']");
                if (!pageRoot) {
                    return ["__missing_page__", "0", "0", "0"];
                }

                const targetGrid = Array
                    .from(pageRoot.querySelectorAll("div"))
                    .find(element => {
                        const inline = element.style;
                        return inline.display === "grid" && inline.width === "400px" && inline.height === "200px";
                    });

                if (!targetGrid) {
                    return ["__missing_grid__", "0", "0", "0"];
                }

                const pageRect = pageRoot.getBoundingClientRect();
                const gridRect = targetGrid.getBoundingClientRect();
                const pageCenterX = pageRect.left + (pageRect.width / 2);
                const gridCenterX = gridRect.left + (gridRect.width / 2);
                const centerDelta = Math.abs(gridCenterX - pageCenterX);

                return [
                    "ok",
                    gridRect.width.toString(),
                    gridRect.height.toString(),
                    centerDelta.toString()
                ];
            }
            """);

        Assert.Equal("ok", gridMetrics[0]);
        var width = double.Parse(gridMetrics[1], CultureInfo.InvariantCulture);
        var height = double.Parse(gridMetrics[2], CultureInfo.InvariantCulture);
        var centerDelta = double.Parse(gridMetrics[3], CultureInfo.InvariantCulture);

        Assert.InRange(width, 398d, 402d);
        Assert.InRange(height, 198d, 202d);
        Assert.True(centerDelta <= 12d, $"Expected clipping grid to be horizontally centered; center delta was {centerDelta:0.###}.");
    }

    [Fact]
    public async Task ClipPage_IsRunnable_AndProjectsExpectedClipScenarios()
    {
        await using var context = await _fixture.NewContextAsync(width: 1024, height: 1400);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await OpenCatalogEntryAsync(page, diagnostics, "clippage");
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-clippage']");
            });

        var imageIds = new[]
        {
            "controls-sample-clippage-image-default",
            "controls-sample-clippage-image-rectangle",
            "controls-sample-clippage-image-ellipse",
            "controls-sample-clippage-image-geometry-group",
            "controls-sample-clippage-image-path"
        };

        foreach (var imageId in imageIds)
        {
            Assert.Equal(1, await page.Locator($"[data-automation-id='{imageId}']").CountAsync());
        }

        var clipSnapshot = await page.EvaluateAsync<string[]>(
            """
            () => {
                const read = (id) => {
                    const node = document.querySelector(`[data-automation-id='${id}']`);
                    if (!(node instanceof HTMLElement)) {
                        return "__missing__";
                    }

                    const inline = node.style.clipPath ?? "";
                    const computed = getComputedStyle(node).clipPath ?? "";
                    return `${inline}|||${computed}`;
                };

                return [
                    read("controls-sample-clippage-image-default"),
                    read("controls-sample-clippage-image-rectangle"),
                    read("controls-sample-clippage-image-ellipse"),
                    read("controls-sample-clippage-image-geometry-group"),
                    read("controls-sample-clippage-image-path")
                ];
            }
            """);

        static (string Inline, string Computed) split(string value)
        {
            var parts = value.Split("|||", StringSplitOptions.None);
            return parts.Length == 2 ? (parts[0], parts[1]) : (value, value);
        }

        static bool IsNoClip(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            return value.Trim().ToLowerInvariant() is "none" or "initial" or "unset" or "revert" or "revert-layer";
        }

        var defaultClip = split(clipSnapshot[0]);
        Assert.True(
            IsNoClip(defaultClip.Inline) && IsNoClip(defaultClip.Computed),
            $"Expected default ClipPage image to be unclipped but got inline='{defaultClip.Inline}' computed='{defaultClip.Computed}'.");

        for (var i = 1; i < clipSnapshot.Length; i++)
        {
            var clip = split(clipSnapshot[i]);
            Assert.True(
                !IsNoClip(clip.Inline) || !IsNoClip(clip.Computed),
                $"Expected clipped image at index {i} to have a clip path but got inline='{clip.Inline}' computed='{clip.Computed}'.");
        }

        var groupClip = split(clipSnapshot[3]);
        Assert.True(
            groupClip.Inline.Contains("evenodd", StringComparison.OrdinalIgnoreCase)
            || groupClip.Computed.Contains("evenodd", StringComparison.OrdinalIgnoreCase),
            $"Expected GeometryGroup clip-path to preserve even-odd fill rule but got inline='{groupClip.Inline}' computed='{groupClip.Computed}'.");
    }

    private async Task<(IPage Page, ConcurrentQueue<string> Diagnostics)> OpenAsync(IBrowserContext context)
    {
        var appUrl = _fixture.HttpAppUrl ?? throw new InvalidOperationException("Host URL was not initialized.");

        var page = await context.NewPageAsync();
        var diagnostics = new ConcurrentQueue<string>();
        AttachDiagnostics(page, diagnostics);
        await page.GotoAsync(appUrl.ToString(), new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        try
        {
            await page.WaitForSelectorAsync("[data-automation-id='controls-sample-home']");
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

        return (page, diagnostics);
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

    private async Task NavigateWithDiagnosticsAsync(
        IPage page,
        ConcurrentQueue<string> diagnostics,
        Func<Task> navigate)
    {
        try
        {
            await navigate();
        }
        catch (Exception ex)
        {
            var bodyText = await page.TextContentAsync("body");
            var hostLogs = _fixture.GetHostLogsSnapshot();
            var diagnosticsSnapshot = RenderDiagnostics(diagnostics);
            throw new TimeoutException(
                $"{ex.Message}\nBody text snapshot: {bodyText}\nHost logs:\n{hostLogs}\nPage diagnostics:\n{diagnosticsSnapshot}",
                ex);
        }
    }

    private async Task OpenCatalogCategoryAsync(
        IPage page,
        ConcurrentQueue<string> diagnostics,
        string categoryId)
    {
        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator($"[data-automation-id='controls-sample-open-category-{categoryId}']").ClickAsync();
                await page.WaitForSelectorAsync($"[data-automation-id='controls-sample-category-{categoryId}']");
            });
    }

    private async Task OpenCatalogEntryAsync(
        IPage page,
        ConcurrentQueue<string> diagnostics,
        string entryId)
    {
        if (EntryCategoryById.TryGetValue(entryId, out var categoryId))
        {
            await OpenCatalogCategoryAsync(page, diagnostics, categoryId);
            if (await page.Locator($"[data-automation-id='controls-sample-open-{entryId}']").CountAsync() > 0)
            {
                await NavigateWithDiagnosticsAsync(
                    page,
                    diagnostics,
                    async () =>
                    {
                        await page.Locator($"[data-automation-id='controls-sample-open-{entryId}']").ClickAsync();
                        await page.WaitForSelectorAsync($"[data-automation-id='controls-sample-page-{entryId}']");
                    });
                return;
            }

            throw new KeyNotFoundException($"Controls sample entry '{entryId}' is mapped to category '{categoryId}', but no matching entry button was found there.");
        }

        foreach (var fallbackCategoryId in KnownCategoryIds)
        {
            await OpenCatalogCategoryAsync(page, diagnostics, fallbackCategoryId);
            if (await page.Locator($"[data-automation-id='controls-sample-open-{entryId}']").CountAsync() == 0)
            {
                await ReturnToCatalogHomeAsync(page, diagnostics);
                continue;
            }

            throw new KeyNotFoundException($"Controls sample entry '{entryId}' is missing from EntryCategoryById. It was discovered under category '{fallbackCategoryId}'.");
        }

        throw new KeyNotFoundException($"Unable to resolve controls sample entry '{entryId}' from the known catalog categories.");
    }

    private async Task ReturnToCatalogHomeAsync(
        IPage page,
        ConcurrentQueue<string> diagnostics)
    {
        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='mirage-navigation-back-button']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-home']");
            });
    }

    private static async Task<double[]> WaitForBoundingClientRectChangeAsync(
        IPage page,
        string selector,
        double[] baselineRect,
        int timeoutMs = 3000,
        int pollMs = 50)
    {
        var elapsedMs = 0;
        while (elapsedMs < timeoutMs)
        {
            var currentRect = await ReadBoundingClientRectAsync(page, selector);
            if (AnyBoundingClientRectDimensionChanged(baselineRect, currentRect))
            {
                return currentRect;
            }

            await page.WaitForTimeoutAsync(pollMs);
            elapsedMs += pollMs;
        }

        return await ReadBoundingClientRectAsync(page, selector);
    }

    private static bool AnyBoundingClientRectDimensionChanged(double[] before, double[] after)
    {
        return before
            .Zip(after, static (previous, current) => Math.Abs(previous - current) > 0.5d)
            .Any(static changed => changed);
    }

    private static async Task<double[]> ReadBoundingClientRectAsync(IPage page, string selector)
    {
        return await page.EvaluateAsync<double[]>(
            """
            (targetSelector) => {
                const target = document.querySelector(targetSelector);
                if (!target) {
                    return [0, 0, 0, 0];
                }

                const rect = target.getBoundingClientRect();
                return [rect.left, rect.top, rect.width, rect.height];
            }
            """,
            selector);
    }

    private static async Task<string> WaitForParityProbeOverlapTopAutomationIdChangeAsync(
        IPage page,
        string baselineTopAutomationId,
        int timeoutMs = 3000,
        int pollMs = 50)
    {
        var elapsedMs = 0;
        while (elapsedMs < timeoutMs)
        {
            var currentTopAutomationId = await ReadParityProbeOverlapTopAutomationIdAsync(page);
            if (!string.Equals(currentTopAutomationId, baselineTopAutomationId, StringComparison.Ordinal))
            {
                return currentTopAutomationId;
            }

            await page.WaitForTimeoutAsync(pollMs);
            elapsedMs += pollMs;
        }

        return await ReadParityProbeOverlapTopAutomationIdAsync(page);
    }

    private static async Task<string> ReadParityProbeOverlapTopAutomationIdAsync(IPage page)
    {
        return await page.EvaluateAsync<string>(
            """
            () => {
                const primary = document.querySelector("[data-automation-id='controls-sample-absolutelayout-parity-a']");
                const secondary = document.querySelector("[data-automation-id='controls-sample-absolutelayout-parity-b']");
                if (!primary || !secondary) {
                    return "__missing__";
                }

                const firstRect = primary.getBoundingClientRect();
                const secondRect = secondary.getBoundingClientRect();
                const left = Math.max(firstRect.left, secondRect.left);
                const top = Math.max(firstRect.top, secondRect.top);
                const right = Math.min(firstRect.right, secondRect.right);
                const bottom = Math.min(firstRect.bottom, secondRect.bottom);
                if (!(right > left && bottom > top)) {
                    return "__no_overlap__";
                }

                const probeX = left + Math.min(6, right - left - 1);
                const probeY = top + Math.min(6, bottom - top - 1);
                const topElement = document.elementFromPoint(probeX, probeY);
                return topElement?.closest("[data-automation-id]")?.getAttribute("data-automation-id") ?? "__none__";
            }
            """);
    }

    private static async Task<int> CountIndicatorDotsAsync(IPage page, string hostSelector)
    {
        return await page.EvaluateAsync<int>(
            """
            (selector) => {
                const host = document.querySelector(selector);
                if (!host) {
                    return -1;
                }

                return host.querySelectorAll(".mirage-indicator-dot").length;
            }
            """,
            hostSelector);
    }

    private static async Task WaitForIndicatorDotCountAsync(
        IPage page,
        string hostSelector,
        int expectedCount,
        int timeoutMs = 3000,
        int pollMs = 50)
    {
        var elapsedMs = 0;
        while (elapsedMs < timeoutMs)
        {
            var count = await CountIndicatorDotsAsync(page, hostSelector);
            if (count == expectedCount)
            {
                return;
            }

            await page.WaitForTimeoutAsync(pollMs);
            elapsedMs += pollMs;
        }

        Assert.Equal(expectedCount, await CountIndicatorDotsAsync(page, hostSelector));
    }

    private static async Task WaitForLabelTextContainsAsync(
        IPage page,
        string selector,
        string expectedSubstring,
        int timeoutMs = 3000,
        int pollMs = 50)
    {
        var elapsedMs = 0;
        while (elapsedMs < timeoutMs)
        {
            var text = await page.Locator(selector).TextContentAsync() ?? string.Empty;
            if (text.Contains(expectedSubstring, StringComparison.Ordinal))
            {
                return;
            }

            await page.WaitForTimeoutAsync(pollMs);
            elapsedMs += pollMs;
        }

        var finalText = await page.Locator(selector).TextContentAsync() ?? string.Empty;
        Assert.Contains(expectedSubstring, finalText, StringComparison.Ordinal);
    }

    private static async Task WaitForEditorValueContainsAsync(
        IPage page,
        string selector,
        string expectedSubstring,
        int timeoutMs = 3000,
        int pollMs = 50)
    {
        var elapsedMs = 0;
        while (elapsedMs < timeoutMs)
        {
            var value = await page.Locator(selector).InputValueAsync();
            if (value.Contains(expectedSubstring, StringComparison.Ordinal))
            {
                return;
            }

            await page.WaitForTimeoutAsync(pollMs);
            elapsedMs += pollMs;
        }

        var finalValue = await page.Locator(selector).InputValueAsync();
        Assert.Contains(expectedSubstring, finalValue, StringComparison.Ordinal);
    }

    private static async Task WaitForInputValueContainsAsync(
        IPage page,
        string selector,
        string expectedSubstring,
        int timeoutMs = 3000,
        int pollMs = 50)
    {
        var elapsedMs = 0;
        while (elapsedMs < timeoutMs)
        {
            var value = await page.Locator(selector).InputValueAsync();
            if (value.Contains(expectedSubstring, StringComparison.Ordinal))
            {
                return;
            }

            await page.WaitForTimeoutAsync(pollMs);
            elapsedMs += pollMs;
        }

        var finalValue = await page.Locator(selector).InputValueAsync();
        Assert.Contains(expectedSubstring, finalValue, StringComparison.Ordinal);
    }

    private static async Task WaitForInputValueEqualsAsync(
        IPage page,
        string selector,
        string expectedValue,
        int timeoutMs = 3000,
        int pollMs = 50)
    {
        var elapsedMs = 0;
        while (elapsedMs < timeoutMs)
        {
            var value = await ReadResolvedTextInputValueAsync(page, selector);
            if (string.Equals(value, expectedValue, StringComparison.Ordinal))
            {
                return;
            }

            await page.WaitForTimeoutAsync(pollMs);
            elapsedMs += pollMs;
        }

        var finalValue = await ReadResolvedTextInputValueAsync(page, selector);
        Assert.Equal(expectedValue, finalValue);
    }

    private static async Task<(int start, int end)> ReadSelectionRangeAsync(IPage page, string selector)
    {
        var values = await page.EvaluateAsync<int[]>(
            """
            (targetSelector) => {
                const root = document.querySelector(targetSelector);
                if (!(root instanceof Element)) {
                    return [-1, -1];
                }

                const element =
                    root instanceof HTMLTextAreaElement || root instanceof HTMLInputElement
                        ? root
                        : root.querySelector("input, textarea");
                if (!(element instanceof HTMLTextAreaElement || element instanceof HTMLInputElement)) {
                    return [-1, -1];
                }

                return [
                    Number.isInteger(element.selectionStart) ? element.selectionStart : -1,
                    Number.isInteger(element.selectionEnd) ? element.selectionEnd : -1
                ];
            }
            """,
            selector);

        Assert.Equal(2, values.Length);
        return (values[0], values[1]);
    }

    private static async Task<string?> ReadResolvedTextInputAttributeAsync(IPage page, string selector, string attributeName)
    {
        return await page.EvaluateAsync<string?>(
            """
            (payload) => {
                const root = document.querySelector(payload.selector);
                if (!(root instanceof Element)) {
                    return "__missing__";
                }

                const control =
                    root instanceof HTMLInputElement || root instanceof HTMLTextAreaElement
                        ? root
                        : root.querySelector("input, textarea");
                if (!(control instanceof HTMLInputElement || control instanceof HTMLTextAreaElement)) {
                    return "__missing__";
                }

                return control.getAttribute(payload.attributeName);
            }
            """,
            new
            {
                selector,
                attributeName
            });
    }

    private static async Task<string> ReadResolvedTextInputValueAsync(IPage page, string selector)
    {
        var value = await page.EvaluateAsync<string>(
            """
            (targetSelector) => {
                const root = document.querySelector(targetSelector);
                if (!(root instanceof Element)) {
                    return "__missing__";
                }

                const control =
                    root instanceof HTMLInputElement || root instanceof HTMLTextAreaElement
                        ? root
                        : root.querySelector("input, textarea");
                if (!(control instanceof HTMLInputElement || control instanceof HTMLTextAreaElement)) {
                    return "__missing__";
                }

                return control.value ?? "";
            }
            """,
            selector);

        Assert.NotEqual("__missing__", value);
        return value;
    }

    private static async Task FocusResolvedTextInputAsync(IPage page, string selector)
    {
        var focused = await page.EvaluateAsync<bool>(
            """
            (targetSelector) => {
                const root = document.querySelector(targetSelector);
                if (!(root instanceof Element)) {
                    return false;
                }

                const control =
                    root instanceof HTMLInputElement || root instanceof HTMLTextAreaElement
                        ? root
                        : root.querySelector("input, textarea");
                if (!(control instanceof HTMLInputElement || control instanceof HTMLTextAreaElement)) {
                    return false;
                }

                control.focus();
                return document.activeElement === control;
            }
            """,
            selector);

        Assert.True(focused, $"Expected to focus resolved text input for selector '{selector}'.");
    }

    private static async Task WaitForResolvedTextInputFocusAsync(
        IPage page,
        string selector,
        bool expectedFocused,
        int timeoutMs = 3000,
        int pollMs = 50)
    {
        var elapsedMs = 0;
        while (elapsedMs < timeoutMs)
        {
            var focused = await page.EvaluateAsync<bool>(
                """
                (targetSelector) => {
                    const root = document.querySelector(targetSelector);
                    if (!(root instanceof Element)) {
                        return false;
                    }

                    const control =
                        root instanceof HTMLInputElement || root instanceof HTMLTextAreaElement
                            ? root
                            : root.querySelector("input, textarea");
                    if (!(control instanceof HTMLInputElement || control instanceof HTMLTextAreaElement)) {
                        return false;
                    }

                    return document.activeElement === control;
                }
                """,
                selector);

            if (focused == expectedFocused)
            {
                return;
            }

            await page.WaitForTimeoutAsync(pollMs);
            elapsedMs += pollMs;
        }

        var finalFocused = await page.EvaluateAsync<bool>(
            """
            (targetSelector) => {
                const root = document.querySelector(targetSelector);
                if (!(root instanceof Element)) {
                    return false;
                }

                const control =
                    root instanceof HTMLInputElement || root instanceof HTMLTextAreaElement
                        ? root
                        : root.querySelector("input, textarea");
                if (!(control instanceof HTMLInputElement || control instanceof HTMLTextAreaElement)) {
                    return false;
                }

                return document.activeElement === control;
            }
            """,
            selector);

        Assert.Equal(expectedFocused, finalFocused);
    }

    private static async Task<bool> ClickNestedElementAsync(IPage page, string selector, string nestedSelector)
    {
        return await page.EvaluateAsync<bool>(
            """
            (payload) => {
                const root = document.querySelector(payload.selector);
                if (!(root instanceof Element)) {
                    return false;
                }

                const target = root.querySelector(payload.nestedSelector);
                if (!(target instanceof HTMLElement)) {
                    return false;
                }

                target.click();
                return true;
            }
            """,
            new
            {
                selector,
                nestedSelector
            });
    }

    private static bool IsTransparent(string cssColor)
    {
        return string.IsNullOrWhiteSpace(cssColor)
            || string.Equals(cssColor, "transparent", StringComparison.OrdinalIgnoreCase)
            || string.Equals(cssColor, "rgba(0, 0, 0, 0)", StringComparison.OrdinalIgnoreCase)
            || string.Equals(cssColor, "rgba(0,0,0,0)", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task NavigateToShapeKindAsync(IPage page, string expectedShapeKind, int maxToggles = 12)
    {
        for (var attempt = 0; attempt < maxToggles; attempt++)
        {
            var current = await page.Locator("[data-automation-id='controls-sample-shapes-parity-shape-kind']").TextContentAsync() ?? string.Empty;
            if (current.Contains($"shape={expectedShapeKind}", StringComparison.Ordinal))
            {
                return;
            }

            await page.Locator("[data-automation-id='controls-sample-shapes-parity-next-shape']").ClickAsync();
            await page.WaitForTimeoutAsync(50);
        }

        var finalState = await page.Locator("[data-automation-id='controls-sample-shapes-parity-shape-kind']").TextContentAsync() ?? string.Empty;
        Assert.Contains($"shape={expectedShapeKind}", finalState, StringComparison.Ordinal);
    }

    private static string ShapeSelectorFor(string shapeKind)
    {
        return $"[data-automation-id='controls-sample-shapes-parity-shape-{shapeKind}']";
    }

    private static async Task<string> WaitForShapePathAttributeChangeAsync(
        IPage page,
        string shapeSelector,
        string attributeName,
        string baselineValue,
        int timeoutMs = 3000,
        int pollMs = 50,
        bool requireNonEmpty = false)
    {
        var elapsedMs = 0;
        while (elapsedMs < timeoutMs)
        {
            var current = await ReadShapePathAttributeAsync(page, shapeSelector, attributeName);
            if (!string.Equals(current, baselineValue, StringComparison.Ordinal)
                && (!requireNonEmpty || !string.IsNullOrWhiteSpace(current)))
            {
                return current;
            }

            await page.WaitForTimeoutAsync(pollMs);
            elapsedMs += pollMs;
        }

        return await ReadShapePathAttributeAsync(page, shapeSelector, attributeName);
    }

    private static async Task<string> WaitForShapePathAttributeValueAsync(
        IPage page,
        string shapeSelector,
        string attributeName,
        int timeoutMs = 10000,
        int pollMs = 50)
    {
        var elapsedMs = 0;
        while (elapsedMs < timeoutMs)
        {
            var current = await ReadShapePathAttributeAsync(page, shapeSelector, attributeName);
            if (!string.IsNullOrWhiteSpace(current)
                && !string.Equals(current, "__missing_shape_host__", StringComparison.Ordinal))
            {
                return current;
            }

            await page.WaitForTimeoutAsync(pollMs);
            elapsedMs += pollMs;
        }

        return await ReadShapePathAttributeAsync(page, shapeSelector, attributeName);
    }

    private static async Task<string> ReadShapePathAttributeAsync(IPage page, string shapeSelector, string attributeName)
    {
        return await page.EvaluateAsync<string>(
            """
            (payload) => {
                const pageHost = document.querySelector("[data-automation-id='controls-sample-page-shapesparitypage']");
                const queryRoot = pageHost ?? document;
                const hosts = Array.from(queryRoot.querySelectorAll(payload.shapeSelector));
                const shapeHost =
                    hosts.slice().reverse().find(host => {
                        if (!(host instanceof HTMLElement) || !host.isConnected) {
                            return false;
                        }

                        const rect = host.getBoundingClientRect();
                        return rect.width > 0 && rect.height > 0;
                    })
                    ?? hosts.at(-1)
                    ?? null;

                if (!shapeHost) {
                    return "__missing_shape_host__";
                }

                const svgHost = shapeHost.querySelector(".mirage-shape-svg");
                const pathNode = shapeHost.querySelector(".mirage-shape-path");

                if (payload.attributeName === "preserveAspectRatio") {
                    return svgHost?.getAttribute("preserveAspectRatio") ?? "";
                }

                return pathNode?.getAttribute(payload.attributeName) ?? "";
            }
            """,
            new
            {
                shapeSelector,
                attributeName
            });
    }

    private static async Task SetSelectIndexAsync(IPage page, string selector, int selectedIndex)
    {
        await page.WaitForSelectorAsync(selector);

        var result = await page.EvaluateAsync<string>(
            """
            (payload) => {
                const control = document.querySelector(payload.selector);
                if (!(control instanceof HTMLSelectElement)) {
                    return "__missing__";
                }

                const targetIndex = Math.max(0, Math.min(payload.selectedIndex, control.options.length - 1));
                const targetValue = control.options[targetIndex]?.value ?? "";
                control.value = targetValue;
                control.selectedIndex = targetIndex;
                control.dispatchEvent(new Event("input", { bubbles: true, composed: true }));
                control.dispatchEvent(new Event("change", { bubbles: true, composed: true }));
                return control.value ?? "";
            }
            """,
            new
            {
                selector,
                selectedIndex
            });

        Assert.NotEqual("__missing__", result);
    }

    private static async Task SetInputTextAsync(IPage page, string selector, string value)
    {
        await page.WaitForSelectorAsync(selector);

        var result = await page.EvaluateAsync<string>(
            """
            (payload) => {
                const root = document.querySelector(payload.selector);
                if (!(root instanceof Element)) {
                    return "__missing__";
                }

                let control = root;
                if (!(control instanceof HTMLInputElement || control instanceof HTMLTextAreaElement)) {
                    const descendantInput = control.querySelector("input, textarea");
                    if (descendantInput instanceof HTMLInputElement || descendantInput instanceof HTMLTextAreaElement) {
                        control = descendantInput;
                    }
                }

                if (control instanceof HTMLInputElement || control instanceof HTMLTextAreaElement) {
                    control.value = payload.value;
                    control.dispatchEvent(new Event("input", { bubbles: true, composed: true }));
                    control.dispatchEvent(new Event("change", { bubbles: true, composed: true }));
                    return control.value;
                }

                if (!("value" in control)) {
                    return "__unsupported__";
                }

                control.value = payload.value;
                control.dispatchEvent(new Event("input", { bubbles: true, composed: true }));
                control.dispatchEvent(new Event("change", { bubbles: true, composed: true }));
                return String(control.value ?? "");
            }
            """,
            new
            {
                selector,
                value
            });

        Assert.NotEqual("__missing__", result);
        Assert.NotEqual("__unsupported__", result);
    }

    private static async Task SetInputValueAsync(IPage page, string selector, double value)
    {
        var result = await page.EvaluateAsync<string>(
            """
            (payload) => {
                const control = document.querySelector(payload.selector);
                if (!(control instanceof HTMLInputElement)) {
                    return "__missing__";
                }

                control.value = payload.value;
                control.dispatchEvent(new Event("input", { bubbles: true, composed: true }));
                control.dispatchEvent(new Event("change", { bubbles: true, composed: true }));
                return control.value;
            }
            """,
            new
            {
                selector,
                value = value.ToString("G17", CultureInfo.InvariantCulture)
            });

        Assert.NotEqual("__missing__", result);
    }

    private static async Task<bool> ReadCheckedAsync(IPage page, string selector)
    {
        return await page.EvaluateAsync<bool>(
            """
            (targetSelector) => {
                const element = document.querySelector(targetSelector);
                if (!(element instanceof HTMLInputElement)) {
                    return false;
                }

                return element.checked;
            }
            """,
            selector);
    }

    private static async Task WaitForMapReadyAsync(IPage page, string mapSelector, float timeoutMs = 20000)
    {
        await page.WaitForFunctionAsync(
            """
            (payload) => {
                const element = document.querySelector(payload.selector);
                if (!(element instanceof HTMLElement)) {
                    return false;
                }

                return element.getAttribute("data-mirage-map-status") === payload.status;
            }
            """,
            new
            {
                selector = mapSelector,
                status = "ready"
            },
            new()
            {
                Timeout = timeoutMs
            });
    }

    private static async Task<int> ReadMapCountAsync(IPage page, string mapSelector, string attributeName)
    {
        return await page.EvaluateAsync<int>(
            """
            (payload) => {
                const element = document.querySelector(payload.selector);
                if (!(element instanceof HTMLElement)) {
                    return -1;
                }

                const raw = element.getAttribute(payload.attributeName);
                const parsed = Number.parseInt(raw ?? "", 10);
                return Number.isFinite(parsed) ? parsed : -1;
            }
            """,
            new
            {
                selector = mapSelector,
                attributeName
            });
    }

    private static async Task WaitForMapCountAsync(IPage page, string mapSelector, string attributeName, int expectedCount, float timeoutMs = 15000)
    {
        await page.WaitForFunctionAsync(
            """
            (payload) => {
                const element = document.querySelector(payload.selector);
                if (!(element instanceof HTMLElement)) {
                    return false;
                }

                const raw = element.getAttribute(payload.attributeName);
                const parsed = Number.parseInt(raw ?? "", 10);
                return Number.isFinite(parsed) && parsed === payload.expectedCount;
            }
            """,
            new
            {
                selector = mapSelector,
                attributeName,
                expectedCount
            },
            new()
            {
                Timeout = timeoutMs
            });
    }

    private static async Task<bool> HasDisabledAttributeAsync(IPage page, string selector)
    {
        return await page.EvaluateAsync<bool>(
            "(targetSelector) => document.querySelector(targetSelector)?.hasAttribute('disabled') === true",
            selector);
    }

    private static async Task<bool> TryClickButtonIfEnabledAsync(IPage page, string selector)
    {
        return await page.EvaluateAsync<bool>(
            """
            (targetSelector) => {
                const element = document.querySelector(targetSelector);
                if (!(element instanceof HTMLButtonElement) || element.disabled) {
                    return false;
                }

                element.click();
                return true;
            }
            """,
            selector);
    }

    private async Task TraverseCollectionViewGalleryTreeAsync(
        IPage page,
        ConcurrentQueue<string> diagnostics,
        HashSet<string> openedPageHosts,
        int depth,
        int maxDepth)
    {
        if (depth > maxDepth)
        {
            throw new InvalidOperationException($"CollectionView gallery traversal exceeded max depth {maxDepth}.");
        }

        await AssertDeferredCollectionButtonsHiddenAsync(page);

        var currentHostAutomationId = await ReadCurrentControlsSamplePageHostAutomationIdAsync(page);
        if (!string.IsNullOrWhiteSpace(currentHostAutomationId))
        {
            openedPageHosts.Add(currentHostAutomationId);
        }

        var navButtonIds = await ReadClickableCollectionViewNavButtonIdsAsync(page);
        foreach (var navButtonId in navButtonIds)
        {
            var parentHostAutomationId = await ReadCurrentControlsSamplePageHostAutomationIdAsync(page);
            Assert.False(string.IsNullOrWhiteSpace(parentHostAutomationId));
            var parentHostNodeId = await ReadCurrentControlsSamplePageHostNodeIdAsync(page);
            var parentHostFingerprint = $"{parentHostAutomationId}|{parentHostNodeId}";

            await NavigateWithDiagnosticsAsync(
                page,
                diagnostics,
                async () =>
                {
                    var domNavButtonSelector = string.IsNullOrWhiteSpace(parentHostNodeId)
                        ? $"[data-automation-id='{navButtonId}']"
                        : $"[data-mirage-node-id='{parentHostNodeId}'] [data-automation-id='{navButtonId}']";
                    var locatorNavButtonSelector = string.IsNullOrWhiteSpace(parentHostNodeId)
                        ? $"{domNavButtonSelector}:visible"
                        : domNavButtonSelector;
                    await ClickCollectionNavButtonAsync(page, locatorNavButtonSelector, domNavButtonSelector);

                    await page.WaitForFunctionAsync(
                        """
                        (previousFingerprint) => {
                            const isVisible = (element) => {
                                if (!(element instanceof HTMLElement) || !element.isConnected) {
                                    return false;
                                }

                                const style = window.getComputedStyle(element);
                                if (style.display === "none" || style.visibility === "hidden" || style.opacity === "0") {
                                    return false;
                                }

                                const rect = element.getBoundingClientRect();
                                return rect.width > 0 && rect.height > 0;
                            };

                            const hosts = Array
                                .from(document.querySelectorAll("[data-automation-id^='controls-sample-page-']"))
                                .filter(isVisible);

                            if (hosts.length === 0) {
                                return false;
                            }

                            const activeHost = hosts.at(-1) ?? null;

                            if (!(activeHost instanceof HTMLElement)) {
                                return false;
                            }

                            const currentAutomationId = activeHost.getAttribute("data-automation-id") ?? "";
                            const currentNodeId = activeHost.getAttribute("data-mirage-node-id") ?? "";
                            if (currentAutomationId.length === 0 || currentNodeId.length === 0) {
                                return false;
                            }

                            return `${currentAutomationId}|${currentNodeId}` !== previousFingerprint;
                        }
                        """,
                        parentHostFingerprint,
                        new()
                        {
                            Timeout = 15000
                        });
                });

            await TraverseCollectionViewGalleryTreeAsync(page, diagnostics, openedPageHosts, depth + 1, maxDepth);

            await NavigateWithDiagnosticsAsync(
                page,
                diagnostics,
                async () =>
                {
                    await page.Locator("[data-automation-id='mirage-navigation-back-button']").ClickAsync();
                    await page.WaitForFunctionAsync(
                        """
                        (expectedAutomationId) => {
                            const isVisible = (element) => {
                                if (!(element instanceof HTMLElement) || !element.isConnected) {
                                    return false;
                                }

                                const style = window.getComputedStyle(element);
                                if (style.display === "none" || style.visibility === "hidden" || style.opacity === "0") {
                                    return false;
                                }

                                const rect = element.getBoundingClientRect();
                                return rect.width > 0 && rect.height > 0;
                            };

                            const hosts = Array
                                .from(document.querySelectorAll("[data-automation-id^='controls-sample-page-']"))
                                .filter(isVisible);

                            if (hosts.length === 0) {
                                return false;
                            }

                            const activeHost = hosts.at(-1) ?? null;

                            if (!(activeHost instanceof HTMLElement)) {
                                return false;
                            }

                            const currentAutomationId = activeHost.getAttribute("data-automation-id") ?? "";
                            return currentAutomationId === expectedAutomationId;
                        }
                        """,
                        parentHostAutomationId,
                        new()
                        {
                            Timeout = 15000
                        });
                });
        }
    }

    private async Task AssertDeferredCollectionButtonsHiddenAsync(IPage page)
    {
        var visibleDeferredButtons = await page.EvaluateAsync<string[]>(
            """
            (deferredIds) => {
                const isVisible = (element) => {
                    if (!(element instanceof HTMLElement) || !element.isConnected) {
                        return false;
                    }

                    const style = window.getComputedStyle(element);
                    if (style.display === "none" || style.visibility === "hidden" || style.opacity === "0") {
                        return false;
                    }

                    const rect = element.getBoundingClientRect();
                    return rect.width > 0 && rect.height > 0;
                };

                const hosts = Array.from(document.querySelectorAll("[data-automation-id^='controls-sample-page-']"))
                    .filter(isVisible);
                if (hosts.length === 0) {
                    return ["__missing_host__"];
                }

                const points = [
                    [Math.max(1, Math.floor(window.innerWidth / 2)), Math.max(1, Math.floor(window.innerHeight / 2))],
                    [Math.max(1, Math.floor(window.innerWidth / 2)), Math.max(1, Math.min(window.innerHeight - 2, 140))],
                    [Math.max(1, Math.min(window.innerWidth - 2, 24)), Math.max(1, Math.min(window.innerHeight - 2, 140))]
                ];

                let activeHost = null;
                for (const [x, y] of points) {
                    const stacked = document.elementsFromPoint(x, y);
                    for (const node of stacked) {
                        if (!(node instanceof Element)) {
                            continue;
                        }

                        const host = node.closest("[data-automation-id^='controls-sample-page-']");
                        if (host instanceof HTMLElement && hosts.includes(host)) {
                            activeHost = host;
                            break;
                        }
                    }

                    if (activeHost) {
                        break;
                    }
                }

                if (!(activeHost instanceof HTMLElement)) {
                    activeHost = hosts.at(-1) ?? null;
                }

                if (!(activeHost instanceof HTMLElement)) {
                    return ["__missing_host__"];
                }

                return deferredIds.filter(id => {
                    const element = activeHost.querySelector(`[data-automation-id='${id}']`);
                    return element instanceof HTMLElement && isVisible(element);
                });
            }
            """,
            DeferredCollectionViewButtonAutomationIds);

        Assert.DoesNotContain("__missing_host__", visibleDeferredButtons);
        Assert.Empty(visibleDeferredButtons);
    }

    private static async Task<string[]> ReadClickableCollectionViewNavButtonIdsAsync(IPage page)
    {
        return await page.EvaluateAsync<string[]>(
            """
            () => {
                const isVisible = (element) => {
                    if (!(element instanceof HTMLElement) || !element.isConnected) {
                        return false;
                    }

                    const style = window.getComputedStyle(element);
                    if (style.display === "none" || style.visibility === "hidden" || style.opacity === "0") {
                        return false;
                    }

                    const rect = element.getBoundingClientRect();
                    return rect.width > 0 && rect.height > 0;
                };

                const hosts = Array.from(document.querySelectorAll("[data-automation-id^='controls-sample-page-']"))
                    .filter(isVisible);
                if (hosts.length === 0) {
                    return [];
                }

                const points = [
                    [Math.max(1, Math.floor(window.innerWidth / 2)), Math.max(1, Math.floor(window.innerHeight / 2))],
                    [Math.max(1, Math.floor(window.innerWidth / 2)), Math.max(1, Math.min(window.innerHeight - 2, 140))],
                    [Math.max(1, Math.min(window.innerWidth - 2, 24)), Math.max(1, Math.min(window.innerHeight - 2, 140))]
                ];

                let activeHost = null;
                for (const [x, y] of points) {
                    const stacked = document.elementsFromPoint(x, y);
                    for (const node of stacked) {
                        if (!(node instanceof Element)) {
                            continue;
                        }

                        const host = node.closest("[data-automation-id^='controls-sample-page-']");
                        if (host instanceof HTMLElement && hosts.includes(host)) {
                            activeHost = host;
                            break;
                        }
                    }

                    if (activeHost) {
                        break;
                    }
                }

                if (!(activeHost instanceof HTMLElement)) {
                    activeHost = hosts.at(-1) ?? null;
                }

                if (!(activeHost instanceof HTMLElement)) {
                    return [];
                }

                const isTopmostHitTarget = (element) => {
                    if (!(element instanceof HTMLElement)) {
                        return false;
                    }

                    const rect = element.getBoundingClientRect();
                    const probeX = Math.max(1, Math.min(window.innerWidth - 2, rect.left + Math.max(1, Math.min(rect.width - 1, rect.width / 2))));
                    const probeY = Math.max(1, Math.min(window.innerHeight - 2, rect.top + Math.max(1, Math.min(rect.height - 1, rect.height / 2))));
                    const topNode = document.elementFromPoint(probeX, probeY);
                    return topNode instanceof Element && (topNode === element || element.contains(topNode));
                };

                const ids = Array.from(activeHost.querySelectorAll("[data-automation-id^='controls-sample-collectionviewgallery-open-']"))
                    .filter(element => element instanceof HTMLButtonElement)
                    .filter(element => isVisible(element))
                    .filter(element => !element.disabled)
                    .filter(element => isTopmostHitTarget(element))
                    .map(element => element.getAttribute("data-automation-id") ?? "")
                    .filter(id => id.length > 0)
                    .sort();

                return [...new Set(ids)];
            }
            """);
    }

    private static async Task<string> ReadCurrentControlsSamplePageHostAutomationIdAsync(IPage page)
    {
        return await page.EvaluateAsync<string>(
            """
            () => {
                const isVisible = (element) => {
                    if (!(element instanceof HTMLElement) || !element.isConnected) {
                        return false;
                    }

                    const style = window.getComputedStyle(element);
                    if (style.display === "none" || style.visibility === "hidden" || style.opacity === "0") {
                        return false;
                    }

                    const rect = element.getBoundingClientRect();
                    return rect.width > 0 && rect.height > 0;
                };

                const hosts = Array.from(document.querySelectorAll("[data-automation-id^='controls-sample-page-']"))
                    .filter(isVisible);
                if (hosts.length === 0) {
                    return "";
                }

                const points = [
                    [Math.max(1, Math.floor(window.innerWidth / 2)), Math.max(1, Math.floor(window.innerHeight / 2))],
                    [Math.max(1, Math.floor(window.innerWidth / 2)), Math.max(1, Math.min(window.innerHeight - 2, 140))],
                    [Math.max(1, Math.min(window.innerWidth - 2, 24)), Math.max(1, Math.min(window.innerHeight - 2, 140))]
                ];

                let host = null;
                for (const [x, y] of points) {
                    const stacked = document.elementsFromPoint(x, y);
                    for (const node of stacked) {
                        if (!(node instanceof Element)) {
                            continue;
                        }

                        const candidate = node.closest("[data-automation-id^='controls-sample-page-']");
                        if (candidate instanceof HTMLElement && hosts.includes(candidate)) {
                            host = candidate;
                            break;
                        }
                    }

                    if (host) {
                        break;
                    }
                }

                if (!(host instanceof HTMLElement)) {
                    host = hosts.at(-1) ?? null;
                }

                if (!(host instanceof HTMLElement)) {
                    return "";
                }

                return host.getAttribute("data-automation-id") ?? "";
            }
            """);
    }

    private static async Task<string> ReadCurrentControlsSamplePageHostNodeIdAsync(IPage page)
    {
        return await page.EvaluateAsync<string>(
            """
            () => {
                const isVisible = (element) => {
                    if (!(element instanceof HTMLElement) || !element.isConnected) {
                        return false;
                    }

                    const style = window.getComputedStyle(element);
                    if (style.display === "none" || style.visibility === "hidden" || style.opacity === "0") {
                        return false;
                    }

                    const rect = element.getBoundingClientRect();
                    return rect.width > 0 && rect.height > 0;
                };

                const hosts = Array.from(document.querySelectorAll("[data-automation-id^='controls-sample-page-']"))
                    .filter(isVisible);
                if (hosts.length === 0) {
                    return "";
                }

                const points = [
                    [Math.max(1, Math.floor(window.innerWidth / 2)), Math.max(1, Math.floor(window.innerHeight / 2))],
                    [Math.max(1, Math.floor(window.innerWidth / 2)), Math.max(1, Math.min(window.innerHeight - 2, 140))],
                    [Math.max(1, Math.min(window.innerWidth - 2, 24)), Math.max(1, Math.min(window.innerHeight - 2, 140))]
                ];

                let activeHost = null;
                for (const [x, y] of points) {
                    const stacked = document.elementsFromPoint(x, y);
                    for (const node of stacked) {
                        if (!(node instanceof Element)) {
                            continue;
                        }

                        const host = node.closest("[data-automation-id^='controls-sample-page-']");
                        if (host instanceof HTMLElement && hosts.includes(host)) {
                            activeHost = host;
                            break;
                        }
                    }

                    if (activeHost) {
                        break;
                    }
                }

                if (!(activeHost instanceof HTMLElement)) {
                    activeHost = hosts.at(-1) ?? null;
                }

                if (!(activeHost instanceof HTMLElement)) {
                    return "";
                }

                return activeHost.getAttribute("data-mirage-node-id") ?? "";
            }
            """);
    }

    private static async Task ExpectDialogAsync(IPage page, string title, string? message, string presentation)
    {
        await page.WaitForSelectorAsync("[data-automation-id='mirage-dialog-overlay']");
        Assert.Equal(
            presentation,
            await page.Locator("[data-automation-id='mirage-dialog-surface']").GetAttributeAsync("data-mirage-dialog-presentation"));
        Assert.Equal(title, await page.Locator("[data-automation-id='mirage-dialog-title']").InnerTextAsync());

        if (string.IsNullOrEmpty(message))
        {
            var messageCount = await page.Locator("[data-automation-id='mirage-dialog-message']").CountAsync();
            if (messageCount == 0)
            {
                return;
            }

            Assert.Equal(string.Empty, (await page.Locator("[data-automation-id='mirage-dialog-message']").TextContentAsync()) ?? string.Empty);
            return;
        }

        Assert.Equal(message, (await page.Locator("[data-automation-id='mirage-dialog-message']").TextContentAsync()) ?? string.Empty);
    }

    private static async Task ClickCollectionNavButtonAsync(IPage page, string locatorSelector, string domSelector)
    {
        try
        {
            var navButtonLocator = page.Locator(locatorSelector).Last;
            await navButtonLocator.WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = 15000
            });
            await navButtonLocator.ScrollIntoViewIfNeededAsync();
            await navButtonLocator.ClickAsync(new()
            {
#pragma warning disable CS0612 // NoWaitAfter is obsolete in Playwright API but still required here to avoid false navigation waits for in-app MAUI routing.
                NoWaitAfter = true
#pragma warning restore CS0612
            });
        }
        catch (TimeoutException)
        {
            var clicked = await page.EvaluateAsync<bool>(
                """
                (selector) => {
                    const elements = Array.from(document.querySelectorAll(selector));
                    const target = elements.at(-1);
                    if (!(target instanceof HTMLElement) || !target.isConnected) {
                        return false;
                    }

                    const style = window.getComputedStyle(target);
                    if (style.display === "none" || style.visibility === "hidden" || style.opacity === "0") {
                        return false;
                    }

                    target.click();
                    return true;
                }
                """,
                domSelector);

            if (clicked)
            {
                return;
            }

            var navButtonLocator = page.Locator(locatorSelector).Last;
            await navButtonLocator.WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = 15000
            });
            await navButtonLocator.ScrollIntoViewIfNeededAsync();
            await navButtonLocator.ClickAsync(new()
            {
                Position = new Position
                {
                    X = 6,
                    Y = 6
                },
#pragma warning disable CS0612 // NoWaitAfter is obsolete in Playwright API but still required here to avoid false navigation waits for in-app MAUI routing.
                NoWaitAfter = true
#pragma warning restore CS0612
            });
        }
    }
}
