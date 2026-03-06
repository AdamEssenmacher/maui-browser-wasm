using Microsoft.Playwright;
using Xunit;

namespace Maui.Browser.E2E.Tests;

public sealed partial class BrowserSmokeTests
{
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
}
