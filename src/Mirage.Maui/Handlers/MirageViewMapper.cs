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
        ["IsVisible"] = MapVisibility,
        ["Style"] = MapStyle,
        [nameof(ITextStyle.TextColor)] = MapTextColor,
        [nameof(ITextStyle.Font)] = MapFont,
        ["FontFamily"] = MapFont,
        ["FontSize"] = MapFont,
        ["FontAttributes"] = MapFont,
        ["FontAutoScalingEnabled"] = MapFont,
        [nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
        [nameof(IView.Opacity)] = MapOpacity,
        [nameof(IView.Background)] = MapBackground,
        ["BackgroundColor"] = MapBackground,
        [nameof(IView.Clip)] = MapClip,
        [nameof(VisualElement.WidthRequest)] = MapSizeRequestsAndLayoutAlignment,
        [nameof(VisualElement.HeightRequest)] = MapSizeRequestsAndLayoutAlignment,
        [nameof(VisualElement.MinimumWidthRequest)] = MapSizeRequests,
        [nameof(VisualElement.MinimumHeightRequest)] = MapSizeRequests,
        [nameof(VisualElement.MaximumWidthRequest)] = MapSizeRequests,
        [nameof(VisualElement.MaximumHeightRequest)] = MapSizeRequests,
        [nameof(View.HorizontalOptions)] = MapLayoutAlignment,
        [nameof(View.VerticalOptions)] = MapLayoutAlignment,
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

    public static void MapTextColor(IViewHandler handler, IView view)
    {
        if (!MirageCommonViewProjection.MapTextColor(handler, view))
        {
            return;
        }

        ((IMiragePlatformHandler)handler).RequestRender();
    }

    public static void MapFont(IViewHandler handler, IView view)
    {
        if (!MirageCommonViewProjection.MapFont(handler, view))
        {
            return;
        }

        ((IMiragePlatformHandler)handler).RequestRender();
    }

    public static void MapCharacterSpacing(IViewHandler handler, IView view)
    {
        if (!MirageCommonViewProjection.MapCharacterSpacing(handler, view))
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

    public static void MapClip(IViewHandler handler, IView view)
    {
        if (!MirageCommonViewProjection.MapClip(handler, view))
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

    public static void MapSizeRequestsAndLayoutAlignment(IViewHandler handler, IView view)
    {
        var changed = MirageCommonViewProjection.MapSizeRequests(handler, view);
        changed |= MirageCommonViewProjection.MapLayoutAlignment(handler, view);
        if (!changed)
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

    public static void MapStyle(IViewHandler handler, IView view)
    {
        _ = view;
        handler.UpdateValue(nameof(IView.Background));
        handler.UpdateValue("BackgroundColor");
        handler.UpdateValue(nameof(IView.Margin));
        handler.UpdateValue(nameof(IView.Visibility));
        handler.UpdateValue("IsVisible");
        handler.UpdateValue(nameof(IView.Opacity));
        handler.UpdateValue(nameof(VisualElement.WidthRequest));
        handler.UpdateValue(nameof(VisualElement.HeightRequest));
        handler.UpdateValue(nameof(VisualElement.MinimumWidthRequest));
        handler.UpdateValue(nameof(VisualElement.MinimumHeightRequest));
        handler.UpdateValue(nameof(VisualElement.MaximumWidthRequest));
        handler.UpdateValue(nameof(VisualElement.MaximumHeightRequest));
        handler.UpdateValue(nameof(View.HorizontalOptions));
        handler.UpdateValue(nameof(View.VerticalOptions));

        if (view is ITextStyle)
        {
            handler.UpdateValue(nameof(ITextStyle.TextColor));
            handler.UpdateValue("TextColor");
            handler.UpdateValue(nameof(ITextStyle.Font));
            handler.UpdateValue("FontFamily");
            handler.UpdateValue("FontSize");
            handler.UpdateValue("FontAttributes");
            handler.UpdateValue("FontAutoScalingEnabled");
            handler.UpdateValue(nameof(ITextStyle.CharacterSpacing));
        }

        if (view is IPadding)
        {
            handler.UpdateValue("Padding");
        }
    }

    public static string ToPx(double value) => MirageCommonViewProjection.ToPx(value);
}
