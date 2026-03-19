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
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-customlayoutpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-gridpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-horizontalstacklayoutpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-stacklayoutpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-verticalstacklayoutpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-scrollviewpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-boxviewpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-buttonpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-checkboxpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-sliderpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-stepperpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-transformationspage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-borderpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-tabbedpagegallerymainpage']").CountAsync());
        Assert.Equal(0, await page.Locator("[data-automation-id='controls-sample-open-layoutisenabledpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-scrollviewpage']").CountAsync());
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

        var iconHostShapeStyles = await page.EvaluateAsync<string[]>(
            """
            () => {
                const image = document.querySelector("[data-automation-id='controls-sample-page-contentviewpage'] img[src*='/_mirage/assets/']");
                const iconHost = image?.parentElement;
                if (!iconHost) {
                    return ["__missing__", "__missing__"];
                }

                const computed = getComputedStyle(iconHost);
                return [computed.borderRadius ?? "", computed.clipPath ?? ""];
            }
            """);

        Assert.True(
            !string.Equals(iconHostShapeStyles[0], "0px", StringComparison.Ordinal)
            || !string.Equals(iconHostShapeStyles[1], "none", StringComparison.OrdinalIgnoreCase),
            $"Expected avatar host to project clipping geometry via borderRadius or clipPath, but got borderRadius='{iconHostShapeStyles[0]}' clipPath='{iconHostShapeStyles[1]}'.");

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
    public async Task CustomLayoutPage_IsRunnable_AndAppliesDockedButtonArrangement()
    {
        await using var context = await _fixture.NewContextAsync(width: 1000, height: 760);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-open-customlayoutpage']").ClickAsync();
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
        await using var context = await _fixture.NewContextAsync(width: 960, height: 760);
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
                    return ["__missing__", "0", "0", "0", "0", "0", "0", "0"];
                }

                const labels = Array
                    .from(host.querySelectorAll("[data-mirage-kind='Label']"))
                    .map(label => (label.textContent ?? "").trim());
                const stackNodes = Array
                    .from(host.querySelectorAll("[data-mirage-kind='VerticalStack'], [data-mirage-kind='HorizontalStack']"));
                const sectionStacks = stackNodes
                    .map(stack => ({
                        kind: stack.getAttribute("data-mirage-kind") ?? "",
                        boxes: Array.from(stack.children).filter(child => child.getAttribute?.("data-mirage-kind") === "BoxView")
                    }))
                    .filter(section => section.boxes.length > 0);

                const verticalBoxes = sectionStacks.find(section => section.kind === "VerticalStack")?.boxes ?? [];
                const horizontalBoxes = sectionStacks.find(section => section.kind === "HorizontalStack")?.boxes ?? [];
                const allBoxes = sectionStacks.flatMap(section => section.boxes);
                const boxRects = allBoxes.map(box => box.getBoundingClientRect());
                const collapsedCount = boxRects.filter(rect => rect.width <= 1 || rect.height <= 1 || (rect.width * rect.height) <= 1).length;
                const maxArea = boxRects.reduce((current, rect) => Math.max(current, rect.width * rect.height), 0);

                return [
                    "ok",
                    labels.includes("Vertical") ? "1" : "0",
                    labels.includes("Horizontal") ? "1" : "0",
                    sectionStacks.length.toString(),
                    verticalBoxes.length.toString(),
                    horizontalBoxes.length.toString(),
                    collapsedCount.toString(),
                    maxArea.toString()
                ];
            }
            """);

        Assert.Equal("ok", layoutSnapshot[0]);
        Assert.Equal("1", layoutSnapshot[1]);
        Assert.Equal("1", layoutSnapshot[2]);
        Assert.Equal("2", layoutSnapshot[3]);
        Assert.Equal("6", layoutSnapshot[4]);
        Assert.Equal("6", layoutSnapshot[5]);
        Assert.Equal("12", layoutSnapshot[6]);
        Assert.True(double.Parse(layoutSnapshot[7], CultureInfo.InvariantCulture) <= 1d);
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
                await page.Locator("[data-automation-id='controls-sample-open-scrollviewpage']").ClickAsync();
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
    public async Task BorderPage_IsRunnable_AndExercisesBorderGalleryScenarios()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 1600);
        var (page, diagnostics) = await OpenAsync(context);

        await NavigateWithDiagnosticsAsync(
            page,
            diagnostics,
            async () =>
            {
                await page.Locator("[data-automation-id='controls-sample-open-borderpage']").ClickAsync();
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

    private static async Task SetSelectIndexAsync(IPage page, string selector, int selectedIndex)
    {
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
                return control.value;
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
        var result = await page.EvaluateAsync<string>(
            """
            (payload) => {
                const control = document.querySelector(payload.selector);
                if (!(control instanceof Element)) {
                    return "__missing__";
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
