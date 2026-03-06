using Microsoft.Playwright;
using Xunit;

namespace Maui.Browser.E2E.Tests;

public sealed partial class BrowserSmokeTests
{
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
}
