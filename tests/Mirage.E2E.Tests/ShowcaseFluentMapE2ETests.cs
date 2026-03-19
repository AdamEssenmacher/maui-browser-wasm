using Microsoft.Playwright;
using Xunit;

namespace Mirage.E2E.Tests;

[Collection("MapE2ESerial")]
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
        var elementCount = await page.Locator("[data-automation-id='showcase-map'] path").CountAsync();

        Assert.Equal("control", nodeType);
        Assert.Equal("Map", nodeKind);
        Assert.True(leafletContainerCount > 0, "Expected the built-in map host element to be initialized as a Leaflet container.");
        Assert.True(markerCount >= 2, $"Expected at least 2 projected pin markers. Actual marker count: {markerCount}");
        Assert.True(elementCount >= 2, $"Expected projected map elements (polyline/polygon/circle). Actual SVG path count: {elementCount}");
    }

    [Fact]
    public async Task MapLab_MoveRegionCommand_UpdatesVisibleRegionTelemetry()
    {
        await using var context = await _fixture.NewContextAsync(width: 1320, height: 860);
        var page = await OpenMapLabAsync(context);
        await WaitForMapReadyAsync(page);

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

    [Fact]
    public async Task MapLab_TrafficToggle_DoesNotReapplyRegionWithoutMoveRequest()
    {
        await using var context = await _fixture.NewContextAsync(width: 1320, height: 860);
        var page = await OpenMapLabAsync(context);
        await WaitForMapReadyAsync(page);

        await page.EvaluateAsync(
            @"() => {
                const proto = window.L && window.L.Map && window.L.Map.prototype;
                if (!proto || typeof proto.fitBounds !== 'function') {
                    return;
                }

                if (!proto.__mirageOriginalFitBounds) {
                    proto.__mirageOriginalFitBounds = proto.fitBounds;
                    proto.fitBounds = function(bounds, options) {
                        window.__mirageFitBoundsCallCount = (window.__mirageFitBoundsCallCount || 0) + 1;
                        return proto.__mirageOriginalFitBounds.call(this, bounds, options);
                    };
                }

                window.__mirageFitBoundsCallCount = 0;
            }");

        await page.Locator("[data-automation-id='showcase-map-traffic-button']").ClickAsync();
        await page.WaitForFunctionAsync(
            @"() => {
                const summary = document.querySelector('[data-automation-id=""showcase-map-options-label""]');
                return !!summary && (summary.textContent || '').includes('Traffic=True');
            }",
            null,
            new PageWaitForFunctionOptions
            {
                Timeout = 10000
            });

        await page.WaitForTimeoutAsync(250);
        var fitBoundsCallCount = await page.EvaluateAsync<int>("() => Number(window.__mirageFitBoundsCallCount || 0)");
        Assert.Equal(0, fitBoundsCallCount);
    }

    [Fact]
    public async Task MapLab_TrafficToggle_ReportsUnsupportedDiagnostic()
    {
        await using var context = await _fixture.NewContextAsync(width: 1320, height: 860);
        var page = await OpenMapLabAsync(context);
        await WaitForMapReadyAsync(page);

        var map = page.Locator("[data-automation-id='showcase-map']");
        await page.Locator("[data-automation-id='showcase-map-traffic-button']").ClickAsync();
        await page.WaitForFunctionAsync(
            @"() => {
                const summary = document.querySelector('[data-automation-id=""showcase-map-options-label""]');
                return !!summary && (summary.textContent || '').includes('Traffic=True');
            }",
            null,
            new PageWaitForFunctionOptions
            {
                Timeout = 10000
            });

        var mapStatus = await map.GetAttributeAsync("data-mirage-map-status");
        Assert.Equal("ready", mapStatus);
    }

    [Fact]
    public async Task MapLab_ToggleUserLocation_EmitsDeterministicStatus()
    {
        await using var context = await _fixture.NewContextAsync(width: 1320, height: 860);
        var page = await OpenMapLabAsync(context);
        await WaitForMapReadyAsync(page);

        var map = page.Locator("[data-automation-id='showcase-map']");
        await page.Locator("[data-automation-id='showcase-map-user-button']").ClickAsync();

        await page.WaitForFunctionAsync(
            @"() => {
                const summary = document.querySelector('[data-automation-id=""showcase-map-options-label""]');
                return !!summary && (summary.textContent || '').includes('User=True');
            }",
            null,
            new PageWaitForFunctionOptions
            {
                Timeout = 10000
            });

        var status = await map.GetAttributeAsync("data-mirage-map-geolocation-status");
        Assert.Contains(status ?? "missing", new[] { "missing", "pending", "ready", "error", "unavailable", "disabled" });
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

    private static async Task WaitForMapReadyAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            @"() => {
                const map = document.querySelector('[data-automation-id=""showcase-map""]');
                if (!map) {
                    return false;
                }

                return map.getAttribute('data-mirage-map-provider') === 'leaflet'
                    && map.getAttribute('data-mirage-map-status') === 'ready';
            }",
            null,
            new PageWaitForFunctionOptions
            {
                Timeout = 12000
            });
    }
}
