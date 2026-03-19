namespace Mirage.E2E.Tests;

public sealed class ShowcaseLeafletQueryAttributionOverrideFixture : ShowcaseHostFixture
{
    protected override Uri ConfigureAppUrl(Uri appUrl)
    {
        return ShowcaseFixtureUrlQuery.Append(
            appUrl,
            ("mirageLeafletTileAttribution", "<img src=x onerror=alert(1)>MirageInjectedAttribution"));
    }
}
