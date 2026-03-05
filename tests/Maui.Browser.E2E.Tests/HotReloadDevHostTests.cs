using Microsoft.Playwright;
using Xunit;

namespace Maui.Browser.E2E.Tests;

public sealed class HotReloadDevHostTests : IClassFixture<DevHostFixture>
{
    private const string TargetXamlPath = "Demo/XamlDemoPage.xaml";

    private readonly DevHostFixture _fixture;

    public HotReloadDevHostTests(DevHostFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SyntheticXamlChangeUpdatesUiWithoutPageReload()
    {
        await _fixture.EmitSyntheticXamlChangeAsync(TargetXamlPath, xamlOverride: null);

        await using var context = await _fixture.NewContextAsync();
        var page = await OpenDemoAsync(context);
        var pageBootBefore = await ReadPageBootCountAsync(page);

        var baselineXaml = await _fixture.GetXamlAsync(TargetXamlPath);
        var buttonSuffix = Guid.NewGuid().ToString("N")[..6];
        var updatedButtonText = $"Click to increment count [{buttonSuffix}]";
        var updatedXaml = baselineXaml.Replace(
            "Click to increment count",
            updatedButtonText,
            StringComparison.Ordinal);

        await _fixture.EmitSyntheticXamlChangeAsync(TargetXamlPath, updatedXaml);

        await page.GetByRole(AriaRole.Button, new() { Name = updatedButtonText }).WaitForAsync();
        await page.WaitForSelectorAsync("text=last update: xaml-live-apply");

        var pageBootAfter = await ReadPageBootCountAsync(page);
        Assert.Equal(pageBootBefore, pageBootAfter);
    }

    [Fact]
    public async Task SyntheticXamlChangePreservesViewModelState()
    {
        await _fixture.EmitSyntheticXamlChangeAsync(TargetXamlPath, xamlOverride: null);

        await using var context = await _fixture.NewContextAsync();
        var page = await OpenDemoAsync(context);

        await page.GetByPlaceholder("Type here...").FillAsync("State should persist");
        await page.GetByRole(AriaRole.Button, new() { Name = "Click to increment count" }).ClickAsync();
        await page.WaitForSelectorAsync("text=Button clicks: 1");

        var baselineXaml = await _fixture.GetXamlAsync(TargetXamlPath);
        var probeSuffix = Guid.NewGuid().ToString("N")[..6];
        var updatedProbeText = $"Visibility and opacity probe (live-{probeSuffix})";
        var updatedXaml = baselineXaml.Replace(
            "Visibility and opacity probe",
            updatedProbeText,
            StringComparison.Ordinal);

        await _fixture.EmitSyntheticXamlChangeAsync(TargetXamlPath, updatedXaml);

        await page.WaitForSelectorAsync($"text={updatedProbeText}");
        await page.WaitForSelectorAsync("text=Live text: State should persist");
        await page.WaitForSelectorAsync("text=Button clicks: 1");
    }

    [Fact]
    public async Task InvalidSyntheticXamlShowsErrorWithoutCrashingUi()
    {
        await _fixture.EmitSyntheticXamlChangeAsync(TargetXamlPath, xamlOverride: null);

        await using var context = await _fixture.NewContextAsync();
        var page = await OpenDemoAsync(context);
        var pageBootBefore = await ReadPageBootCountAsync(page);

        var baselineXaml = await _fixture.GetXamlAsync(TargetXamlPath);
        var invalidXaml = baselineXaml.Replace("</ContentPage>", string.Empty, StringComparison.Ordinal);

        await _fixture.EmitSyntheticXamlChangeAsync(TargetXamlPath, invalidXaml);

        await page.WaitForSelectorAsync("text=last update: xaml-apply-failed");
        await page.WaitForSelectorAsync("text=Live XAML apply failed:");
        await page.GetByRole(AriaRole.Button, new() { Name = "Force full reload" }).WaitForAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Click to increment count" }).ClickAsync();
        await page.WaitForSelectorAsync("text=Button clicks: 1");

        var pageBootAfter = await ReadPageBootCountAsync(page);
        Assert.Equal(pageBootBefore, pageBootAfter);
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

    private static Task<int> ReadPageBootCountAsync(IPage page)
    {
        return page.EvaluateAsync<int>("() => globalThis.__mauiBrowserHotReload?.pageReloadCount ?? -1");
    }
}
