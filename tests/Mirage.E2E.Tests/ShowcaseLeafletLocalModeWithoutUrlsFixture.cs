namespace Mirage.E2E.Tests;

public sealed class ShowcaseLeafletLocalModeWithoutUrlsFixture : ShowcaseHostFixture
{
    protected override Uri ConfigureAppUrl(Uri appUrl)
    {
        return ShowcaseFixtureUrlQuery.Append(
            appUrl,
            ("mirageLeafletAssetMode", "local"));
    }
}
