using Maui.Browser.Dom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;

namespace Maui.Browser.Hosting;

/// <summary>
/// Browser host bootstrap with two startup modes:
/// - Application/window-first startup via <see cref="RunAsync(MauiApp,string)"/>
/// - Root-view compatibility startup via <see cref="RunAsync(MauiApp,IView,string)"/>
/// </summary>
public static class BrowserAppHost
{
    private static Action? s_resizeCallback;
    private static int s_resizeSubscriptionId;
    private static IElementHandler? s_rootHandler;
    private static IBrowserWindowHost? s_windowHost;

    public static async Task RunAsync(MauiApp mauiApp, string rootElementId = "app")
    {
        ArgumentNullException.ThrowIfNull(mauiApp);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootElementId);

        await DomElement.EnsureInteropReadyAsync();

        var rootDomElement = DomElement.RootById(rootElementId);
        ClearActiveHostState(rootDomElement);

        var application = mauiApp.Services.GetRequiredService<IApplication>();
        var window = ResolvePrimaryWindow(application);

        var mauiContext = new MauiContext(mauiApp.Services);
        var windowHost = mauiApp.Services.GetService<IBrowserWindowHost>() ?? new BrowserWindowHost();
        s_windowHost = windowHost;
        windowHost.AttachWindow(window, mauiContext, rootDomElement);

        void ArrangeWindow()
        {
            var viewport = ResolveViewport(rootDomElement);
            windowHost.Arrange(viewport.Width, viewport.Height);
        }

        ArrangeWindow();
        SubscribeResize(ArrangeWindow);
    }

    public static async Task RunAsync(MauiApp mauiApp, IView rootView, string rootElementId = "app")
    {
        ArgumentNullException.ThrowIfNull(mauiApp);
        ArgumentNullException.ThrowIfNull(rootView);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootElementId);

        await DomElement.EnsureInteropReadyAsync();

        var rootDomElement = DomElement.RootById(rootElementId);
        ClearActiveHostState(rootDomElement);
        s_windowHost = null;

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
            var viewport = ResolveViewport(rootDomElement);
            rootView.Measure(viewport.Width, viewport.Height);
            rootView.Arrange(new Rect(0, 0, viewport.Width, viewport.Height));
        }

        ArrangeRoot();
        SubscribeResize(ArrangeRoot);
    }

    private static IWindow ResolvePrimaryWindow(IApplication application)
    {
        if (application.Windows.Count > 0)
        {
            if (application.Windows.Count > 1)
            {
                Console.Error.WriteLine(
                    $"BrowserAppHost currently supports a single active window. Using the first of {application.Windows.Count} windows.");
            }

            return application.Windows[0];
        }

        var created = application.CreateWindow(null);
        if (created is null)
        {
            throw new InvalidOperationException("IApplication.CreateWindow returned null.");
        }

        if (application.Windows.Count > 1)
        {
            Console.Error.WriteLine(
                $"BrowserAppHost currently supports a single active window. Using the first of {application.Windows.Count} windows.");
            return application.Windows[0];
        }

        return created;
    }

    private static void ClearActiveHostState(DomElement rootDomElement)
    {
        UnsubscribeResize();
        s_windowHost?.DetachWindow();
        s_rootHandler?.DisconnectHandler();
        s_rootHandler = null;
        rootDomElement.ClearChildren();
    }

    private static void SubscribeResize(Action callback)
    {
        UnsubscribeResize();
        s_resizeCallback = callback;
        s_resizeSubscriptionId = DomElement.SubscribeWindowResize(callback);
    }

    private static void UnsubscribeResize()
    {
        if (s_resizeSubscriptionId != 0)
        {
            DomElement.UnsubscribeWindowResize(s_resizeSubscriptionId);
            s_resizeSubscriptionId = 0;
        }

        s_resizeCallback = null;
    }

    private static (double Width, double Height) ResolveViewport(DomElement rootDomElement)
    {
        var viewport = rootDomElement.ClientSize();
        var width = viewport.Width > 0 ? viewport.Width : 1024;
        var height = viewport.Height > 0 ? viewport.Height : 768;
        return (width, height);
    }
}
