using Microsoft.Playwright;
using Xunit;

namespace Maui.Browser.E2E.Tests;

public sealed partial class BrowserSmokeTests
{
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
}
