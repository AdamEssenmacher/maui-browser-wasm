using Mirage.Core.Providers;
using Mirage.Core.Diagnostics;
using Mirage.Maui.HotReload;
using Mirage.Maui.Handlers;
using Mirage.Maui.Navigation;
using Mirage.Web.Dom;
using Mirage.Web.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;

namespace Mirage.Maui.Hosting;

public static class MirageBrowserAppHost
{
    private static int s_resizeSubscriptionId;
    private static int s_themeSubscriptionId;
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
        MirageDiagnostics.EnableIf(DomElement.IsDiagnosticsEnabled());

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
        if (mauiApp.Services.GetService<IMirageNavigationStartupResolver>() is { } startupResolver)
        {
            await startupResolver.ApplyInitialRouteAsync(window, mauiApp.Services);
        }

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
            InitializeBrowserThemeBridge(controlsApplication);
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
            || s_controlsApplication is null
            || s_primaryWindow is null
            || s_dispatcher is null
            || s_renderCoordinator is null)
        {
            return;
        }

        if (!IsHotReloadEnabled())
        {
            MirageDiagnostics.Log("hotreload", "disabled via MIRAGE_HOT_RELOAD");
            return;
        }

        if (!MirageHotReloadMetadataUpdateHandler.IsEnabledForAssembly(s_controlsApplication.GetType().Assembly))
        {
            return;
        }

        s_hotReloadService = new MirageXamlHotReloadService(s_controlsApplication, s_primaryWindow, s_dispatcher, s_renderCoordinator);
        s_hotReloadService.Start();
    }

    private static bool IsHotReloadEnabled()
    {
        var raw = Environment.GetEnvironmentVariable("MIRAGE_HOT_RELOAD");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        return raw.Trim() switch
        {
            "0" => false,
            "false" => false,
            "FALSE" => false,
            "no" => false,
            "NO" => false,
            "off" => false,
            "OFF" => false,
            _ => true
        };
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

        if (s_themeSubscriptionId != 0)
        {
            DomElement.UnsubscribePreferredColorSchemeChanged(s_themeSubscriptionId);
            s_themeSubscriptionId = 0;
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

    private static void InitializeBrowserThemeBridge(Application controlsApplication)
    {
        if (s_themeSubscriptionId != 0)
        {
            DomElement.UnsubscribePreferredColorSchemeChanged(s_themeSubscriptionId);
            s_themeSubscriptionId = 0;
        }

        ApplyBrowserTheme(controlsApplication);

        s_themeSubscriptionId = DomElement.SubscribePreferredColorSchemeChanged(() =>
        {
            var dispatcher = s_dispatcher;
            if (dispatcher is not null && dispatcher.IsDispatchRequired)
            {
                if (dispatcher.Dispatch(() => ApplyBrowserTheme(controlsApplication)))
                {
                    return;
                }
            }

            ApplyBrowserTheme(controlsApplication);
        });
    }

    private static void ApplyBrowserTheme(Application controlsApplication)
    {
        var browserTheme = ParseBrowserTheme(DomElement.GetPreferredColorScheme());
        if (browserTheme is null || controlsApplication.UserAppTheme == browserTheme.Value)
        {
            return;
        }

        controlsApplication.UserAppTheme = browserTheme.Value;
        if (MirageDiagnostics.IsEnabled)
        {
            MirageDiagnostics.Log("theme.bridge", $"theme={browserTheme.Value}");
        }
    }

    private static AppTheme? ParseBrowserTheme(string? colorScheme)
    {
        if (string.IsNullOrWhiteSpace(colorScheme))
        {
            return null;
        }

        return colorScheme.Trim().ToLowerInvariant() switch
        {
            "light" => AppTheme.Light,
            "dark" => AppTheme.Dark,
            _ => null
        };
    }

}
