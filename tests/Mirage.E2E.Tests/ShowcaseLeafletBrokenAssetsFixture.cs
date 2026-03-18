namespace Mirage.E2E.Tests;

public sealed class ShowcaseLeafletBrokenAssetsFixture : ShowcaseHostFixture
{
    protected override Uri ConfigureAppUrl(Uri appUrl)
    {
        return ShowcaseFixtureUrlQuery.Append(
            appUrl,
            ("mirageLeafletJsUrl", "https://invalid.localhost.example/leaflet.js"),
            ("mirageLeafletCssUrl", "https://invalid.localhost.example/leaflet.css"));
    }
}
