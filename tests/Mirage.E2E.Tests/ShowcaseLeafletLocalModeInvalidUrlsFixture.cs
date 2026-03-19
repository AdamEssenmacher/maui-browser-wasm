namespace Mirage.E2E.Tests;

public sealed class ShowcaseLeafletLocalModeInvalidUrlsFixture : ShowcaseHostFixture
{
    protected override Uri ConfigureAppUrl(Uri appUrl)
    {
        return ShowcaseFixtureUrlQuery.Append(
            appUrl,
            ("mirageLeafletAssetMode", "local"),
            ("mirageLeafletJsUrl", "//invalid.localhost.example/leaflet.js"),
            ("mirageLeafletCssUrl", "/leaflet/leaflet.css"));
    }
}
