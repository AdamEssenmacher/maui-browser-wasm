using Mirage.Core.Providers;
using Mirage.Maui.HotReload;
using Mirage.Maui.Handlers;
using Mirage.Web.Dom;
using Mirage.Web.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using System.Reflection;

namespace Mirage.Maui.Hosting;

public static class MirageBrowserAppHost
{
    private const string MetadataUpdateHandlerAttributeName = "System.Reflection.Metadata.MetadataUpdateHandlerAttribute";

    private static int s_resizeSubscriptionId;
    private static IElementHandler? s_windowHandler;
    private static IMirageRenderCoordinator? s_renderCoordinator;
    private static IMirageRuntime? s_runtime;
    private static IDispatcher? s_dispatcher;
    private static MirageXamlHotReloadService? s_hotReloadService;
    private static BrowserPageNavigator? s_pageNavigator;
    private static Application? s_controlsApplication;
    private static Window? s_primaryWindow;

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
        var dispatcher = mauiApp.Services.GetRequiredService<IDispatcher>();
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
        s_dispatcher = dispatcher;
        s_pageNavigator = pageNavigator;
        s_windowHandler = windowHandler;

        if (application is Application controlsApplication)
        {
            s_controlsApplication = controlsApplication;
            s_primaryWindow = window;
            EnsureHotReloadServiceStarted();
        }

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
        EnsureHotReloadServiceStarted();

        var dispatcher = s_dispatcher;
        if (dispatcher is null)
        {
            s_renderCoordinator?.RequestRender();
            s_hotReloadService?.RequestImmediateScan();
            return;
        }

        void Notify()
        {
            s_renderCoordinator?.RequestRender();
            s_hotReloadService?.RequestImmediateScan();
        }

        if (dispatcher.IsDispatchRequired)
        {
            if (!dispatcher.Dispatch(Notify))
            {
                Notify();
            }
            return;
        }

        Notify();
    }

    private static void EnsureHotReloadServiceStarted()
    {
        if (s_hotReloadService is not null
            || !IsMirageHotReloadConfigured()
            || s_controlsApplication is null
            || s_primaryWindow is null
            || s_dispatcher is null
            || s_renderCoordinator is null)
        {
            return;
        }

        s_hotReloadService = new MirageXamlHotReloadService(s_controlsApplication, s_primaryWindow, s_dispatcher, s_renderCoordinator);
        s_hotReloadService.Start();
    }

    private static bool IsMirageHotReloadConfigured()
    {
        var expectedHandlerType = typeof(MirageHotReloadMetadataUpdateHandler);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            IList<CustomAttributeData> attributes;
            try
            {
                attributes = assembly.GetCustomAttributesData();
            }
            catch
            {
                continue;
            }

            foreach (var attribute in attributes)
            {
                if (!string.Equals(attribute.AttributeType.FullName, MetadataUpdateHandlerAttributeName, StringComparison.Ordinal)
                    || attribute.ConstructorArguments.Count != 1)
                {
                    continue;
                }

                if (attribute.ConstructorArguments[0].Value is Type handlerType
                    && handlerType == expectedHandlerType)
                {
                    return true;
                }
            }
        }

        return false;
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

        s_hotReloadService?.Stop();
        s_hotReloadService = null;

        s_windowHandler?.DisconnectHandler();
        s_windowHandler = null;

        s_pageNavigator?.DetachRoot();
        s_pageNavigator = null;

        s_renderCoordinator?.DetachRuntime();
        s_renderCoordinator = null;

        s_runtime?.Dispose();
        s_runtime = null;

        s_dispatcher = null;
        s_controlsApplication = null;
        s_primaryWindow = null;

        rootDomElement.ClearChildren();
    }

}
