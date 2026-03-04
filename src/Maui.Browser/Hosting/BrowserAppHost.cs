using Maui.Browser.Dom;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;

namespace Maui.Browser.Hosting;

/// <summary>
/// Browser host bootstrap for root-view-first startup.
/// - Creates a MAUI context from <see cref="MauiApp.Services"/>
/// - Creates/attaches the root handler under a DOM element id
/// - Runs MAUI measure/arrange against current viewport size
/// - Re-runs layout on browser resize
/// </summary>
public static class BrowserAppHost
{
    private static readonly List<Action> s_resizeCallbacks = [];
    private static readonly List<int> s_resizeSubscriptions = [];
    private static IElementHandler? s_rootHandler;

    public static async Task RunAsync(MauiApp mauiApp, IView rootView, string rootElementId = "app")
    {
        ArgumentNullException.ThrowIfNull(mauiApp);
        ArgumentNullException.ThrowIfNull(rootView);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootElementId);

        await DomElement.EnsureInteropReadyAsync();

        var rootDomElement = DomElement.RootById(rootElementId);
        rootDomElement.ClearChildren();

        foreach (var subscriptionId in s_resizeSubscriptions)
        {
            DomElement.UnsubscribeWindowResize(subscriptionId);
        }

        s_resizeSubscriptions.Clear();
        s_resizeCallbacks.Clear();

        if (s_rootHandler is not null)
        {
            s_rootHandler.DisconnectHandler();
            s_rootHandler = null;
        }

        var mauiContext = new MauiContext(mauiApp.Services);
        var rootHandler = rootView.ToHandler(mauiContext);
        s_rootHandler = rootHandler;

        if (rootHandler.PlatformView is not DomElement rootPlatformView)
        {
            throw new InvalidOperationException(
                $"Root handler platform view must be {nameof(DomElement)}. Actual: {rootHandler.PlatformView?.GetType().FullName ?? "<null>"}");
        }

        rootDomElement.Append(rootPlatformView);

        void ArrangeRoot()
        {
            var viewport = rootDomElement.ClientSize();
            var width = viewport.Width > 0 ? viewport.Width : 1024;
            var height = viewport.Height > 0 ? viewport.Height : 768;

            rootView.Measure(width, height);
            rootView.Arrange(new Rect(0, 0, width, height));
        }

        ArrangeRoot();

        Action resizeCallback = ArrangeRoot;
        var resizeSubscriptionId = DomElement.SubscribeWindowResize(resizeCallback);
        s_resizeCallbacks.Add(resizeCallback);
        s_resizeSubscriptions.Add(resizeSubscriptionId);
    }
}
