using System.Globalization;
using Microsoft.Playwright;
using Xunit;

namespace Mirage.E2E.Tests;

public sealed class WebViewParityProbeE2ETests : IClassFixture<ControlsSampleHostFixture>
{
    private readonly ControlsSampleHostFixture _fixture;

    public WebViewParityProbeE2ETests(ControlsSampleHostFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task WebViewParityProbe_AllowsPageHandlersBeforeInterceptingNavigation()
    {
        await using var context = await _fixture.NewContextAsync(width: 1220, height: 940);
        var page = await OpenWebViewParityProbeAsync(context);

        const string hostSelector = "[data-automation-id='controls-sample-webview-parity-host']";
        const string loadPageASelector = "[data-automation-id='controls-sample-webview-parity-load-page-a']";
        const string navigationStateSelector = "[data-automation-id='controls-sample-webview-parity-navigation-state']";
        const string cancelLinkSelector = "[data-automation-id='controls-sample-webview-probe-link-page-a-script-cancel']";
        const string mutateLinkSelector = "[data-automation-id='controls-sample-webview-probe-link-page-a-script-mutate']";
        const string mutateSubmitSelector = "[data-automation-id='controls-sample-webview-probe-submit-script-mutate-page-a-to-page-b']";

        async Task WaitForHostUrlContainingAsync(string expectedSubstring)
        {
            await page.WaitForFunctionAsync(
                """
                (args) => {
                    const host = document.querySelector(args.selector);
                    const value = host?.getAttribute("data-maui-wv-url") ?? "";
                    return value.includes(args.expectedSubstring);
                }
                """,
                new
                {
                    selector = hostSelector,
                    expectedSubstring
                });
        }

        async Task<int> ReadNavigationCounterAsync(string counterName)
        {
            var text = await page.Locator(navigationStateSelector).InnerTextAsync();
            var token = $"{counterName}=";
            var start = text.IndexOf(token, StringComparison.Ordinal);
            Assert.True(start >= 0, $"Navigation state label did not contain counter token '{token}'. Text: {text}");
            start += token.Length;
            var end = text.IndexOf(' ', start);
            var valueText = end >= 0 ? text[start..end] : text[start..];
            return int.Parse(valueText, CultureInfo.InvariantCulture);
        }

        await page.Locator(loadPageASelector).ClickAsync();
        await WaitForHostUrlContainingAsync("/webview-probe/page-a.html");
        var frame = page.FrameLocator(hostSelector);

        var canceledNavigatingBaseline = await ReadNavigationCounterAsync("navigating");
        await frame.Locator(cancelLinkSelector).ClickAsync();
        await ExpectFrameBodyAttributeAsync(frame, "data-script-cancel-ran", "true");
        await page.WaitForTimeoutAsync(150);
        Assert.Equal(canceledNavigatingBaseline, await ReadNavigationCounterAsync("navigating"));
        await WaitForHostUrlContainingAsync("/webview-probe/page-a.html");

        await frame.Locator(mutateLinkSelector).ClickAsync();
        await WaitForHostUrlContainingAsync("/webview-probe/page-b.html?script=mutated");

        await page.Locator(loadPageASelector).ClickAsync();
        await WaitForHostUrlContainingAsync("/webview-probe/page-a.html");
        await frame.Locator(mutateSubmitSelector).ClickAsync();
        await WaitForHostUrlContainingAsync("/webview-probe/page-b.html?form=mutated");
        await WaitForHostUrlContainingAsync("probe=page-a-form-script");
    }

    private async Task<IPage> OpenWebViewParityProbeAsync(IBrowserContext context)
    {
        var appUrl = _fixture.HttpAppUrl ?? throw new InvalidOperationException("Host URL was not initialized.");
        var page = await context.NewPageAsync();
        await page.GotoAsync(appUrl.ToString(), new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await page.WaitForSelectorAsync("[data-automation-id='controls-sample-home']");
        await page.Locator("[data-automation-id='controls-sample-open-category-parityprobes']").ClickAsync();
        await page.WaitForSelectorAsync("[data-automation-id='controls-sample-category-parityprobes']");
        await page.Locator("[data-automation-id='controls-sample-open-webviewparitypage']").ClickAsync();
        await page.WaitForSelectorAsync("[data-automation-id='controls-sample-page-webviewparitypage']");
        await page.WaitForSelectorAsync("[data-automation-id='controls-sample-webview-parity-host']");
        return page;
    }

    private static async Task ExpectFrameBodyAttributeAsync(IFrameLocator frame, string attributeName, string expectedValue)
    {
        await frame.Locator("body").WaitForAsync();
        await Assertions.Expect(frame.Locator("body")).ToHaveAttributeAsync(attributeName, expectedValue);
    }
}
