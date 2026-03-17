using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Microsoft.Playwright;
using Xunit;

namespace Mirage.E2E.Tests;

public sealed class ControlsSampleFluentE2ETests : IClassFixture<ControlsSampleHostFixture>
{
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

        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-absolutelayoutpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-clippingpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-contentviewpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-gridpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-horizontalstacklayoutpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-stacklayoutpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-verticalstacklayoutpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-boxviewpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-buttonpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-checkboxpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-sliderpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-stepperpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-transformationspage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-tabbedpagegallerymainpage']").CountAsync());
        Assert.Equal(0, await page.Locator("[data-automation-id='controls-sample-open-layoutisenabledpage']").CountAsync());
        Assert.Equal(0, await page.Locator("[data-automation-id='controls-sample-open-blazorpage']").CountAsync());

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-open-gridpage']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-gridpage']");
            });

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='mirage-navigation-back-button']").ClickAsync();
                await page.WaitForSelectorAsync("[data-automation-id='controls-sample-home']");
            });

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-open-contentviewpage']").ClickAsync();
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
    public async Task HorizontalStackLayoutPage_IsRunnable_AndAllowsUnsizedBoxViewsToCollapse()
    {
        await using var context = await _fixture.NewContextAsync(width: 900, height: 700);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-open-horizontalstacklayoutpage']").ClickAsync();
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
    public async Task VerticalStackLayoutPage_IsRunnable_AndAllowsUnsizedBoxViewsToCollapse()
    {
        await using var context = await _fixture.NewContextAsync(width: 900, height: 700);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-open-verticalstacklayoutpage']").ClickAsync();
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
                await page.Locator("[data-automation-id='controls-sample-open-stacklayoutpage']").ClickAsync();
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
    public async Task ButtonPage_IsRunnable_AndExercisesCoreScenarios()
    {
        await using var context = await _fixture.NewContextAsync(width: 760, height: 980);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-open-buttonpage']").ClickAsync();
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
                await page.Locator("[data-automation-id='controls-sample-open-boxviewpage']").ClickAsync();
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
                await page.Locator("[data-automation-id='controls-sample-open-checkboxpage']").ClickAsync();
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
                await page.Locator("[data-automation-id='controls-sample-open-sliderpage']").ClickAsync();
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
    public async Task TransformationsPage_IsRunnable_AndAppliesSliderAndStepperTransforms()
    {
        await using var context = await _fixture.NewContextAsync(width: 1000, height: 1200);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-open-transformationspage']").ClickAsync();
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
                await page.Locator("[data-automation-id='controls-sample-open-stepperpage']").ClickAsync();
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
    public async Task ClippingPage_GridFrame_RendersAtExpectedSize_AndIsHorizontallyCentered()
    {
        await using var context = await _fixture.NewContextAsync();
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-open-clippingpage']").ClickAsync();
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
}
