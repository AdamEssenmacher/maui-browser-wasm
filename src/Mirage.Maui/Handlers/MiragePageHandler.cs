using Mirage.Core.Semantics;
using Mirage.Maui.Navigation;
using Mirage.Web.Runtime;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MiragePageHandler : MirageViewHandler<Page>, IMiragePageTitleProjectionHost
{
    internal static readonly string[] ParityPropertyKeys =
    [
        nameof(ITitledElement.Title)
    ];

    internal static readonly string[] ParityCommandKeys = [];

    public static IPropertyMapper<Page, MiragePageHandler> Mapper =
        new PropertyMapper<Page, MiragePageHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(ITitledElement.Title)] = MapTitle
        };

    public static CommandMapper<Page, MiragePageHandler> CommandMapper =
        new(MirageViewMapper.CommonViewCommandMapper);

    public MiragePageHandler()
        : base(ComponentKind.ContentContainer, Mapper, CommandMapper)
    {
        DefaultDisplayStyle = "grid";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        EnsureSupportedPageType(VirtualView);
        platformView.SetStyle("display", "grid");
        base.ConnectHandler(platformView);
        TryProjectActivePageTitle();
    }

    public static void MapTitle(MiragePageHandler handler, Page page)
    {
        _ = page;
        handler.TryProjectActivePageTitle();
    }

    internal bool TryProjectActivePageTitle()
    {
        if (!TryGetPlatformView(out var platformView))
        {
            return false;
        }

        return TryProjectActivePageTitle(MauiContext, platformView, VirtualView);
    }

    bool IMiragePageTitleProjectionHost.TryProjectActivePageTitle() => TryProjectActivePageTitle();

    internal static bool TryProjectActivePageTitle(IElementHandler? handler)
    {
        return handler is IMiragePageTitleProjectionHost titleProjectionHost
            && titleProjectionHost.TryProjectActivePageTitle();
    }

    internal static bool TryProjectActivePageTitle(IMauiContext? mauiContext, MiragePlatformView? platformView, Page page)
    {
        if (mauiContext?.Services is not { } services
            || platformView is null
            || string.IsNullOrWhiteSpace(platformView.NodeId)
            || !IsAttachedToRoot(platformView)
            || IsNavigationOwned(page))
        {
            return false;
        }

        if (page.Handler?.PlatformView is not MiragePlatformView currentPlatformView
            || !ReferenceEquals(currentPlatformView, platformView))
        {
            return false;
        }

        if (services.GetService(typeof(IMirageBrowserHistory)) is not IMirageBrowserHistory browserHistory
            || services.GetService(typeof(INavigationTitlePolicy)) is not INavigationTitlePolicy titlePolicy)
        {
            return false;
        }

        browserHistory.SetDocumentTitle(titlePolicy.ResolveTitle(page, routePath: null));
        return true;
    }

    private static bool IsAttachedToRoot(MiragePlatformView platformView)
    {
        var current = platformView;
        while (current.Parent is not null)
        {
            current = current.Parent;
        }

        return current.ControlKind == ComponentKind.Root
            && !string.IsNullOrWhiteSpace(current.NodeId);
    }

    private static bool IsNavigationOwned(Page page)
    {
        for (Element? current = page.Parent; current is not null; current = current.Parent)
        {
            if (current is NavigationPage)
            {
                return true;
            }
        }

        return false;
    }

    private static void EnsureSupportedPageType(Page page)
    {
        if (page.GetType() == typeof(Page))
        {
            return;
        }

        throw new NotSupportedException(
            $"MiragePageHandler does not support '{page.GetType().FullName}'. " +
            "Register a dedicated handler for this Page subtype instead of relying on Page fallback mapping.");
    }
}
