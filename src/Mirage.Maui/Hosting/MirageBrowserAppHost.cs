using Mirage.Core.Providers;
using Mirage.Maui.Handlers;
using Mirage.Web.Dom;
using Mirage.Web.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;

namespace Mirage.Maui.Hosting;

public static class MirageBrowserAppHost
{
    private static int s_resizeSubscriptionId;
    private static IElementHandler? s_windowHandler;
    private static IMirageRenderCoordinator? s_renderCoordinator;
    private static IMirageRuntime? s_runtime;
    private static BrowserPageNavigator? s_pageNavigator;

    public static async Task RunAsync(MauiApp mauiApp, string rootElementId = "app")
    {
        ArgumentNullException.ThrowIfNull(mauiApp);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootElementId);

        await DomElement.EnsureInteropReadyAsync();

        var provider = MirageProviderValidation.GetRequiredProvider(mauiApp.Services);
        var rootDomElement = DomElement.RootById(rootElementId);

        ClearActiveHostState(rootDomElement);
        rootDomElement.ClearChildren();

        var runtime = new MirageRuntime(rootDomElement, provider);
        var renderCoordinator = mauiApp.Services.GetRequiredService<IMirageRenderCoordinator>();
        var pageNavigator = mauiApp.Services.GetService<BrowserPageNavigator>();

        renderCoordinator.AttachRuntime(runtime);
        pageNavigator?.AttachRoot(rootDomElement);

        var application = mauiApp.Services.GetRequiredService<IApplication>();
        var window = ResolvePrimaryWindow(application);

        var mauiContext = new MauiContext(mauiApp.Services);
        var windowHandler = window.ToHandler(mauiContext);
        if (windowHandler is not MirageWindowHandler)
        {
            throw new InvalidOperationException("MirageBrowserAppHost requires Window to resolve to MirageWindowHandler.");
        }

        s_runtime = runtime;
        s_renderCoordinator = renderCoordinator;
        s_pageNavigator = pageNavigator;
        s_windowHandler = windowHandler;

        void ArrangeAndRender()
        {
            var viewport = rootDomElement.ClientSize();
            var width = viewport.Width > 0 ? viewport.Width : 1024;
            var height = viewport.Height > 0 ? viewport.Height : 768;

            runtime.SetViewport(width, height);
            if (windowHandler is MirageWindowHandler mirageWindowHandler)
            {
                mirageWindowHandler.ArrangeWindow(width, height);
            }

            renderCoordinator.RequestRender();
        }

        ArrangeAndRender();
        s_resizeSubscriptionId = DomElement.SubscribeWindowResize(ArrangeAndRender);
    }

    public static void NotifyHotReloadApplied()
    {
        s_renderCoordinator?.RequestRender();
    }

    private static Window ResolvePrimaryWindow(IApplication application)
    {
        if (application.Windows.Count > 0)
        {
            if (application.Windows[0] is Window existingWindow)
            {
                return existingWindow;
            }

            throw new InvalidOperationException("MirageBrowserAppHost requires Microsoft.Maui.Controls.Window.");
        }

        var created = application.CreateWindow(null);
        if (created is null)
        {
            throw new InvalidOperationException("IApplication.CreateWindow returned null.");
        }

        if (created is Window createdWindow)
        {
            return createdWindow;
        }

        throw new InvalidOperationException("MirageBrowserAppHost requires Microsoft.Maui.Controls.Window.");
    }

    private static void ClearActiveHostState(DomElement rootDomElement)
    {
        if (s_resizeSubscriptionId != 0)
        {
            DomElement.UnsubscribeWindowResize(s_resizeSubscriptionId);
            s_resizeSubscriptionId = 0;
        }

        s_windowHandler?.DisconnectHandler();
        s_windowHandler = null;

        s_pageNavigator?.DetachRoot();
        s_pageNavigator = null;

        s_renderCoordinator?.DetachRuntime();
        s_renderCoordinator = null;

        s_runtime?.Dispose();
        s_runtime = null;

        rootDomElement.ClearChildren();
    }
}
