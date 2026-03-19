using Microsoft.Playwright;
using Xunit;

namespace Mirage.E2E.Tests;

[Collection("MapE2ESerial")]
public sealed class ShowcaseFluentMapSecurityE2ETests : IClassFixture<ShowcaseLeafletQueryAttributionOverrideFixture>
{
    private readonly ShowcaseLeafletQueryAttributionOverrideFixture _fixture;

    public ShowcaseFluentMapSecurityE2ETests(ShowcaseLeafletQueryAttributionOverrideFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MapLab_QueryTileAttributionOverride_IsIgnored()
    {
        await using var context = await _fixture.NewContextAsync(width: 1280, height: 860);
        var page = await ShowcaseMapE2ETestHelpers.OpenMapLabAsync(context, _fixture.HttpAppUrl, _fixture.GetHostLogsSnapshot);
        var map = page.Locator("[data-automation-id='showcase-map']");

        await page.WaitForFunctionAsync(
            @"() => {
                const map = document.querySelector('[data-automation-id=""showcase-map""]');
                return !!map
                    && map.getAttribute('data-mirage-map-provider') === 'leaflet'
                    && map.getAttribute('data-mirage-map-status') === 'ready'
                    && !!document.querySelector('[data-automation-id=""showcase-map""] .leaflet-control-attribution');
            }",
            null,
            new PageWaitForFunctionOptions
            {
                Timeout = 12000
            });

        var attributionHtml = await page.EvaluateAsync<string>(
            @"() => {
                const attribution = document.querySelector('[data-automation-id=""showcase-map""] .leaflet-control-attribution');
                return attribution ? (attribution.innerHTML || '') : '';
            }");
        var status = await map.GetAttributeAsync("data-mirage-map-status");

        Assert.Equal("ready", status);
        Assert.DoesNotContain("MirageInjectedAttribution", attributionHtml ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains("OpenStreetMap", attributionHtml ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
