using Microsoft.Playwright;
using Xunit;

namespace Maui.Browser.E2E.Tests;

public sealed partial class BrowserSmokeTests
{
    [Fact]
    public async Task NavigationAndScrollFlowWorks()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 520);
        var page = await OpenDemoAsync(context);

        await page.GetByPlaceholder("Type here...").FillAsync("Navigation state survives");
        await page.Locator("[data-automation-id='nav-open-button']").ClickAsync();

        await page.WaitForSelectorAsync("[data-automation-id='nav-detail-heading']");
        await page.WaitForFunctionAsync(
            @"() => {
                const title = document.querySelector('[data-automation-id=""navigation-title""]');
                return !!title && title.textContent?.includes('Navigation Detail') === true;
            }");

        var overflowY = await ReadComputedStylePropertyAsync(page, "[data-automation-id='nav-scroll-view']", "overflowY");
        Assert.True(overflowY is "auto" or "scroll");

        var hasScrollableHost = await page.EvaluateAsync<bool>(
            @"() => {
                const root = document.querySelector('[data-automation-id=""nav-scroll-view""]');
                if (!root) {
                    return false;
                }

                const nodes = [root, ...root.querySelectorAll('*')];
                return nodes.some((node) => node.scrollHeight > node.clientHeight + 1);
            }");

        if (hasScrollableHost)
        {
            await page.EvaluateAsync(
                @"() => {
                    const root = document.querySelector('[data-automation-id=""nav-scroll-view""]');
                    if (!root) {
                        return;
                    }

                    const nodes = [root, ...root.querySelectorAll('*')];
                    const scroll = nodes.find((node) => node.scrollHeight > node.clientHeight + 1);
                    if (!scroll) {
                        return;
                    }

                    scroll.scrollTop = 10000;
                    scroll.dispatchEvent(new Event('scroll', { bubbles: true }));
                }");

            await page.WaitForFunctionAsync(
                @"() => {
                    const root = document.querySelector('[data-automation-id=""nav-scroll-view""]');
                    if (!root) {
                        return false;
                    }

                    const nodes = [root, ...root.querySelectorAll('*')];
                    const scroll = nodes.find((node) => node.scrollHeight > node.clientHeight + 1);
                    return !!scroll && scroll.scrollTop > 100;
                }");
        }

        await page.GetByRole(AriaRole.Button, new() { Name = "Increment detail counter" }).ClickAsync();
        await page.WaitForSelectorAsync("text=detail count: 1");

        await page.Locator("[data-automation-id='navigation-back-button']").ClickAsync();
        await page.WaitForSelectorAsync("text=Live text: Navigation state survives");
        await page.WaitForSelectorAsync("text=Navigation pushes: 1, pops: 1, detail count: 1");
    }

    [Fact]
    public async Task NavigationPushPopLoopReturnsDiagnosticsNearBaseline()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenDemoAsync(context);

        var before = await ReadDiagnosticsAsync(page);
        var openNavigationButton = page.Locator("[data-automation-id='nav-open-button']");

        for (var i = 1; i <= 4; i++)
        {
            await openNavigationButton.ClickAsync();
            await page.WaitForSelectorAsync("[data-automation-id='nav-detail-heading']");
            await page.Locator("[data-automation-id='navigation-back-button']").ClickAsync();
            await page.WaitForSelectorAsync($"text=Navigation pushes: {i}, pops: {i}");
        }

        var after = await ReadDiagnosticsAsync(page);

        Assert.True(
            after.ElementHandleCount <= before.ElementHandleCount + 20,
            $"Expected navigation element handles near baseline. Before={before.ElementHandleCount}, After={after.ElementHandleCount}");

        Assert.True(
            after.EventSubscriptionCount <= before.EventSubscriptionCount + 6,
            $"Expected navigation subscriptions near baseline. Before={before.EventSubscriptionCount}, After={after.EventSubscriptionCount}");

        Assert.True(
            after.ResizeSubscriptionCount <= before.ResizeSubscriptionCount + 1,
            $"Expected resize subscriptions near baseline. Before={before.ResizeSubscriptionCount}, After={after.ResizeSubscriptionCount}");
    }
}
