namespace Mirage.E2E.Tests;

public sealed class ShowcaseLeafletBrokenAssetsFixture : ShowcaseHostFixture
{
    protected override Uri ConfigureAppUrl(Uri appUrl)
    {
        return ShowcaseFixtureUrlQuery.Append(
            appUrl,
            ("mirageLeafletJsUrl", "/__missing__/leaflet.js"),
            ("mirageLeafletCssUrl", "/__missing__/leaflet.css"));
    }
}
