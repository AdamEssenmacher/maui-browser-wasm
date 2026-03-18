using Microsoft.Playwright;
using Xunit;

namespace Mirage.E2E.Tests;

public sealed class ShowcaseFluentMapE2ETests : IClassFixture<ShowcaseHostFixture>
{
    private readonly ShowcaseHostFixture _fixture;

    public ShowcaseFluentMapE2ETests(ShowcaseHostFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MapLab_RendersBuiltInMapControlNode_AndInitializesLeafletHost()
    {
        await using var context = await _fixture.NewContextAsync(width: 1320, height: 860);
        var page = await OpenMapLabAsync(context);

        var nodeType = await page.Locator("[data-automation-id='showcase-map']").GetAttributeAsync("data-mirage-node-type");
        var nodeKind = await page.Locator("[data-automation-id='showcase-map']").GetAttributeAsync("data-mirage-kind");
        await page.WaitForFunctionAsync(
            @"() => {
                const map = document.querySelector('[data-automation-id=""showcase-map""]');
                return !!map && map.getAttribute('data-mirage-map-provider') === 'leaflet';
            }");
        var leafletContainerCount = await page.Locator("[data-automation-id='showcase-map'].leaflet-container").CountAsync();
        var markerCount = await page.Locator("[data-automation-id='showcase-map'] .leaflet-marker-icon").CountAsync();

        Assert.Equal("control", nodeType);
        Assert.Equal("Map", nodeKind);
        Assert.True(leafletContainerCount > 0, "Expected the built-in map host element to be initialized as a Leaflet container.");
        Assert.True(markerCount >= 2, $"Expected at least 2 projected pin markers. Actual marker count: {markerCount}");
    }

    [Fact]
    public async Task MapLab_MoveRegionCommand_UpdatesVisibleRegionTelemetry()
    {
        await using var context = await _fixture.NewContextAsync(width: 1320, height: 860);
        var page = await OpenMapLabAsync(context);

        var mapSelector = "[data-automation-id='showcase-map']";
        await page.WaitForFunctionAsync(
            @"() => {
                const map = document.querySelector('[data-automation-id=""showcase-map""]');
                return !!map && map.getAttribute('data-mirage-map-center-lat') !== null;
            }");
        var initialLatitude = await page.Locator(mapSelector).GetAttributeAsync("data-mirage-map-center-lat");

        await page.Locator("[data-automation-id='showcase-map-move-button']").ClickAsync();
        await page.WaitForFunctionAsync(
            @"(initialLat) => {
                const map = document.querySelector('[data-automation-id=""showcase-map""]');
                if (!map) {
                    return false;
                }

                const nextLat = map.getAttribute('data-mirage-map-center-lat');
                return !!nextLat && nextLat !== initialLat;
            }",
            initialLatitude);

        var updatedLatitude = await page.Locator(mapSelector).GetAttributeAsync("data-mirage-map-center-lat");
        var regionLabel = await page.Locator("[data-automation-id='showcase-map-region-label']").InnerTextAsync();

        Assert.NotEqual(initialLatitude, updatedLatitude);
        Assert.Contains("Seattle", regionLabel, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<IPage> OpenMapLabAsync(IBrowserContext context)
    {
        if (_fixture.HttpAppUrl is null)
        {
            throw new InvalidOperationException("Fixture app URL is not initialized.");
        }

        var page = await context.NewPageAsync();
        var mapLabBuilder = new UriBuilder(_fixture.HttpAppUrl)
        {
            Path = "/map-lab"
        };
        var mapLabUrl = mapLabBuilder.Uri;
        await page.GotoAsync(mapLabUrl.ToString(), new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await page.WaitForSelectorAsync("[data-automation-id='showcase-page-map-lab']");
        return page;
    }
}
