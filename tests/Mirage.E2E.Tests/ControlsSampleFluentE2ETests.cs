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
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-boxviewpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-buttonpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-checkboxpage']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-automation-id='controls-sample-open-sliderpage']").CountAsync());
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
    public async Task HorizontalStackLayoutPage_IsRunnable_AndRendersHorizontalChildFlow()
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
                    return ["__missing__", "0", "0", "0", "0", "0", "0", "0"];
                }

                const label = stack.querySelector("[data-mirage-kind='Label']");
                const labelText = (label?.textContent ?? "").trim();
                const boxViews = Array.from(stack.querySelectorAll("[data-mirage-kind='BoxView']"));
                const first = boxViews[0];
                const last = boxViews[boxViews.length - 1];

                const firstRect = first?.getBoundingClientRect();
                const secondRect = boxViews[1]?.getBoundingClientRect();
                const lastRect = last?.getBoundingClientRect();

                return [
                    "ok",
                    labelText,
                    boxViews.length.toString(),
                    (firstRect?.width ?? 0).toString(),
                    (firstRect?.height ?? 0).toString(),
                    (firstRect?.left ?? 0).toString(),
                    (secondRect?.left ?? 0).toString(),
                    (lastRect?.left ?? 0).toString(),
                    getComputedStyle(first ?? stack).backgroundColor ?? "",
                    getComputedStyle(last ?? stack).backgroundColor ?? ""
                ];
            }
            """);

        Assert.Equal("ok", layoutSnapshot[0]);
        Assert.Equal("HorizontalStackLayout", layoutSnapshot[1]);
        Assert.Equal("6", layoutSnapshot[2]);
        Assert.True(double.Parse(layoutSnapshot[3], CultureInfo.InvariantCulture) > 0d);
        Assert.True(double.Parse(layoutSnapshot[4], CultureInfo.InvariantCulture) > 0d);
        Assert.True(double.Parse(layoutSnapshot[6], CultureInfo.InvariantCulture) > double.Parse(layoutSnapshot[5], CultureInfo.InvariantCulture));
        Assert.True(double.Parse(layoutSnapshot[7], CultureInfo.InvariantCulture) > double.Parse(layoutSnapshot[6], CultureInfo.InvariantCulture));
        Assert.Contains("255, 0, 0", layoutSnapshot[8], StringComparison.Ordinal);
        Assert.Contains("128, 0, 128", layoutSnapshot[9], StringComparison.Ordinal);
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
}
