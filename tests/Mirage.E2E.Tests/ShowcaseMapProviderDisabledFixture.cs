namespace Mirage.E2E.Tests;

public sealed class ShowcaseMapProviderDisabledFixture : ShowcaseHostFixture
{
    protected override Uri ConfigureAppUrl(Uri appUrl)
    {
        return ShowcaseFixtureUrlQuery.Append(
            appUrl,
            ("mirageMapProvider", "none"));
    }
}
