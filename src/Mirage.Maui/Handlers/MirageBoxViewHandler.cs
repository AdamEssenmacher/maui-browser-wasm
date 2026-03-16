using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageBoxViewHandler : MirageViewHandler<BoxView>
{
    private const string DefaultBoxSizePx = "40px";

    public static IPropertyMapper<BoxView, MirageBoxViewHandler> Mapper =
        new PropertyMapper<BoxView, MirageBoxViewHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(BoxView.Color)] = MapColor,
            [nameof(BoxView.CornerRadius)] = MapCornerRadius,
            [nameof(VisualElement.WidthRequest)] = MapSizeRequests,
            [nameof(VisualElement.HeightRequest)] = MapSizeRequests,
            [nameof(VisualElement.MinimumWidthRequest)] = MapSizeRequests,
            [nameof(VisualElement.MinimumHeightRequest)] = MapSizeRequests,
            [nameof(VisualElement.MaximumWidthRequest)] = MapSizeRequests,
            [nameof(VisualElement.MaximumHeightRequest)] = MapSizeRequests
        };

    public MirageBoxViewHandler()
        : base(ComponentKind.BoxView, Mapper)
    {
        DefaultDisplayStyle = "block";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        var changed = SetStyleIfChanged("display", "block");
        base.ConnectHandler(platformView);
        changed |= UpdateColorStyles(VirtualView);
        changed |= UpdateCornerRadiusStyles(VirtualView);
        changed |= UpdateDefaultSizeStyles(VirtualView);
        if (changed)
        {
            RequestRender();
        }
    }

    public static void MapColor(MirageBoxViewHandler handler, BoxView boxView)
    {
        if (!handler.UpdateColorStyles(boxView))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapCornerRadius(MirageBoxViewHandler handler, BoxView boxView)
    {
        if (!handler.UpdateCornerRadiusStyles(boxView))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapSizeRequests(MirageBoxViewHandler handler, BoxView boxView)
    {
        var changed = MirageCommonViewProjection.MapSizeRequests(handler, boxView);
        changed |= handler.UpdateDefaultSizeStyles(boxView);
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
    }

    private bool UpdateColorStyles(BoxView boxView)
    {
        var changed = MirageCommonViewProjection.MapBackground(this, boxView);

        if (boxView.IsSet(BoxView.ColorProperty))
        {
            var cssColor = MirageCommonViewProjection.ToCssColor(boxView.Color);
            changed |= SetStyleIfChanged("backgroundColor", cssColor);
            changed |= SetStyleIfChanged("backgroundImage", null);
            changed |= SetStyleIfChanged("backgroundSize", null);
            changed |= SetStyleIfChanged("backgroundRepeat", null);
            changed |= SetStyleIfChanged("backgroundPosition", null);
        }

        return changed;
    }

    private bool UpdateCornerRadiusStyles(BoxView boxView)
    {
        var cornerRadius = boxView.CornerRadius;
        var topLeft = Math.Max(0d, NormalizeFinite(cornerRadius.TopLeft));
        var topRight = Math.Max(0d, NormalizeFinite(cornerRadius.TopRight));
        var bottomRight = Math.Max(0d, NormalizeFinite(cornerRadius.BottomRight));
        var bottomLeft = Math.Max(0d, NormalizeFinite(cornerRadius.BottomLeft));

        if (topLeft <= 0d && topRight <= 0d && bottomRight <= 0d && bottomLeft <= 0d)
        {
            return SetStyleIfChanged("borderRadius", null);
        }

        var borderRadiusStyle =
            $"{MirageViewMapper.ToPx(topLeft)} {MirageViewMapper.ToPx(topRight)} {MirageViewMapper.ToPx(bottomRight)} {MirageViewMapper.ToPx(bottomLeft)}";
        return SetStyleIfChanged("borderRadius", borderRadiusStyle);
    }

    private bool UpdateDefaultSizeStyles(BoxView boxView)
    {
        var hasExplicitMinWidth = HasExplicitLength(boxView, VisualElement.MinimumWidthRequestProperty);
        var hasExplicitMinHeight = HasExplicitLength(boxView, VisualElement.MinimumHeightRequestProperty);

        var applyDefaultMinWidth = ShouldApplyDefaultMinLength(
            boxView,
            VisualElement.WidthRequestProperty,
            VisualElement.MinimumWidthRequestProperty,
            VisualElement.MaximumWidthRequestProperty);

        var applyDefaultMinHeight = ShouldApplyDefaultMinLength(
            boxView,
            VisualElement.HeightRequestProperty,
            VisualElement.MinimumHeightRequestProperty,
            VisualElement.MaximumHeightRequestProperty);

        var changed = false;
        if (applyDefaultMinWidth)
        {
            changed |= SetStyleIfChanged("minWidth", DefaultBoxSizePx);
        }
        else if (!hasExplicitMinWidth)
        {
            changed |= SetStyleIfChanged("minWidth", null);
        }

        if (applyDefaultMinHeight)
        {
            changed |= SetStyleIfChanged("minHeight", DefaultBoxSizePx);
        }
        else if (!hasExplicitMinHeight)
        {
            changed |= SetStyleIfChanged("minHeight", null);
        }

        return changed;
    }

    private static bool ShouldApplyDefaultMinLength(
        BoxView boxView,
        BindableProperty requestProperty,
        BindableProperty minimumRequestProperty,
        BindableProperty maximumRequestProperty)
    {
        if (HasExplicitLength(boxView, requestProperty))
        {
            return false;
        }

        if (HasExplicitLength(boxView, minimumRequestProperty))
        {
            return false;
        }

        return !HasExplicitLength(boxView, maximumRequestProperty);
    }

    private static bool HasExplicitLength(BoxView boxView, BindableProperty property)
    {
        if (!boxView.IsSet(property))
        {
            return false;
        }

        if (boxView.GetValue(property) is not double value)
        {
            return false;
        }

        return double.IsFinite(value) && value >= 0d;
    }

    private static double NormalizeFinite(double value)
    {
        return double.IsFinite(value) ? value : 0d;
    }

    private bool SetStyleIfChanged(string name, string? value)
    {
        if (string.Equals(PlatformView.GetStyleValue(name), value, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetStyle(name, value);
        return true;
    }
}
