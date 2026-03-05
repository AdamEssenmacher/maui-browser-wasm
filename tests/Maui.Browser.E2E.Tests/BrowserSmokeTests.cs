using Microsoft.Playwright;
using Xunit;

namespace Maui.Browser.E2E.Tests;

public sealed class BrowserSmokeTests : IClassFixture<BrowserHostFixture>
{
    private readonly BrowserHostFixture _fixture;

    public BrowserSmokeTests(BrowserHostFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task EntryAndButtonFlowWork()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenDemoAsync(context);

        await page.GetByPlaceholder("Type here...").FillAsync("E2E typing check");
        await page.WaitForSelectorAsync("text=Live text: E2E typing check");

        await page.GetByRole(AriaRole.Button, new() { Name = "Click to increment count" }).ClickAsync();
        await page.WaitForSelectorAsync("text=Button clicks: 1");
    }

    [Fact]
    public async Task ResizeAndVisibilityOpacityFlowWork()
    {
        await using var context = await _fixture.NewContextAsync(width: 1200, height: 900);
        var page = await OpenDemoAsync(context);

        var viewportLabelBefore = await ReadViewportLabelAsync(page);

        await page.SetViewportSizeAsync(900, 620);
        await page.WaitForFunctionAsync(
            "(before) => { const match = Array.from(document.querySelectorAll('*')).find(el => el.textContent?.startsWith('Viewport/layout size:')); return !!match && match.textContent !== before; }",
            viewportLabelBefore);

        var visibilityButton = page.GetByRole(AriaRole.Button, new() { Name = "Toggle probe visibility" });
        await visibilityButton.ClickAsync();
        await page.WaitForSelectorAsync("text=Probe visibility: collapsed");

        await visibilityButton.ClickAsync();
        await page.WaitForSelectorAsync("text=Probe visibility: visible");

        var opacityButton = page.GetByRole(AriaRole.Button, new() { Name = "Cycle probe opacity" });
        await opacityButton.ClickAsync();
        await page.WaitForSelectorAsync("text=Probe opacity: 0.60");
    }

    [Fact]
    public async Task StressMutationRunReturnsDiagnosticsNearBaseline()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await OpenDemoAsync(context);

        var before = await ReadDiagnosticsAsync(page);

        var stressButton = page.GetByRole(AriaRole.Button, new() { Name = "Run leak stress" });
        for (var i = 1; i <= 3; i++)
        {
            await stressButton.ClickAsync();
            await page.WaitForSelectorAsync($"text=Stress runs: {i}");
        }

        var after = await ReadDiagnosticsAsync(page);

        Assert.True(
            after.ElementHandleCount <= before.ElementHandleCount + 8,
            $"Expected element handles near baseline. Before={before.ElementHandleCount}, After={after.ElementHandleCount}");

        Assert.True(
            after.EventSubscriptionCount <= before.EventSubscriptionCount + 3,
            $"Expected event subscriptions near baseline. Before={before.EventSubscriptionCount}, After={after.EventSubscriptionCount}");

        Assert.True(
            after.ResizeSubscriptionCount <= before.ResizeSubscriptionCount + 1,
            $"Expected resize subscriptions near baseline. Before={before.ResizeSubscriptionCount}, After={after.ResizeSubscriptionCount}");
    }

    private async Task<IPage> OpenDemoAsync(IBrowserContext context)
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

        await page.WaitForSelectorAsync("text=Hot reload mode:");
        await page.WaitForSelectorAsync("text=Live text:");
        return page;
    }

    private static async Task<string> ReadViewportLabelAsync(IPage page)
    {
        return await page.EvaluateAsync<string>(
            "() => Array.from(document.querySelectorAll('*')).find(el => el.textContent?.startsWith('Viewport/layout size:'))?.textContent ?? ''");
    }

    private static async Task<DomDiagnosticSnapshot> ReadDiagnosticsAsync(IPage page)
    {
        var values = await page.EvaluateAsync<int[]>(@"async () => {
            const domModule = await import('./_framework/dom.js');
            return domModule.getDiagnostics();
        }");

        return new DomDiagnosticSnapshot(
            values.Length > 0 ? values[0] : 0,
            values.Length > 1 ? values[1] : 0,
            values.Length > 2 ? values[2] : 0);
    }

    private readonly record struct DomDiagnosticSnapshot(
        int ElementHandleCount,
        int EventSubscriptionCount,
        int ResizeSubscriptionCount);
}
