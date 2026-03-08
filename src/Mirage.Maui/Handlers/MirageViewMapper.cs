using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

public static class MirageViewMapper
{
    public static IPropertyMapper<IView, IViewHandler> CommonViewMapper = new PropertyMapper<IView, IViewHandler>(ViewHandler.ViewMapper)
    {
        [nameof(IView.AutomationId)] = MapAutomationId,
        [nameof(IView.Margin)] = MapMargin,
        [nameof(IView.Visibility)] = MapVisibility,
        [nameof(IView.Opacity)] = MapOpacity,
        [nameof(IView.Background)] = MapBackground,
        [nameof(VisualElement.WidthRequest)] = MapSizeRequests,
        [nameof(VisualElement.HeightRequest)] = MapSizeRequests,
        [nameof(VisualElement.MinimumWidthRequest)] = MapSizeRequests,
        [nameof(VisualElement.MinimumHeightRequest)] = MapSizeRequests,
        [nameof(VisualElement.MaximumWidthRequest)] = MapSizeRequests,
        [nameof(VisualElement.MaximumHeightRequest)] = MapSizeRequests,
        [nameof(IView.HorizontalLayoutAlignment)] = MapLayoutAlignment,
        [nameof(IView.VerticalLayoutAlignment)] = MapLayoutAlignment
    };

    public static CommandMapper<IView, IViewHandler> CommonViewCommandMapper = new(ViewHandler.ViewCommandMapper);

    public static void MapAutomationId(IViewHandler handler, IView view)
    {
        if (!MirageCommonViewProjection.MapAutomationId(handler, view))
        {
            return;
        }

        ((IMiragePlatformHandler)handler).RequestRender();
    }

    public static void MapMargin(IViewHandler handler, IView view)
    {
        if (!MirageCommonViewProjection.MapMargin(handler, view))
        {
            return;
        }

        ((IMiragePlatformHandler)handler).RequestRender();
    }

    public static void MapVisibility(IViewHandler handler, IView view)
    {
        if (!MirageCommonViewProjection.MapVisibility(handler, view))
        {
            return;
        }

        ((IMiragePlatformHandler)handler).RequestRender();
    }

    public static void MapPadding(IViewHandler handler, IPadding paddingView)
    {
        if (!MirageCommonViewProjection.MapPadding(handler, paddingView))
        {
            return;
        }

        ((IMiragePlatformHandler)handler).RequestRender();
    }

    public static void MapOpacity(IViewHandler handler, IView view)
    {
        if (!MirageCommonViewProjection.MapOpacity(handler, view))
        {
            return;
        }

        ((IMiragePlatformHandler)handler).RequestRender();
    }

    public static void MapBackground(IViewHandler handler, IView view)
    {
        if (!MirageCommonViewProjection.MapBackground(handler, view))
        {
            return;
        }

        ((IMiragePlatformHandler)handler).RequestRender();
    }

    public static void MapSizeRequests(IViewHandler handler, IView view)
    {
        if (!MirageCommonViewProjection.MapSizeRequests(handler, view))
        {
            return;
        }

        ((IMiragePlatformHandler)handler).RequestRender();
    }

    public static void MapLayoutAlignment(IViewHandler handler, IView view)
    {
        if (!MirageCommonViewProjection.MapLayoutAlignment(handler, view))
        {
            return;
        }

        ((IMiragePlatformHandler)handler).RequestRender();
    }

    public static string ToPx(double value) => MirageCommonViewProjection.ToPx(value);
}
