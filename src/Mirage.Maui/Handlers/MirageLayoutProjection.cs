using System.Globalization;
using System.Reflection;
using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;

namespace Mirage.Maui.Handlers;

internal static class MirageLayoutProjection
{
    private static readonly MethodInfo? FlexBasisIsRelativeGetter = typeof(FlexBasis)
        .GetProperty("IsRelative", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?
        .GetMethod;

    public static bool ApplyChildProjectionStyles(
        MiragePlatformView childPlatformView,
        IView childView,
        ComponentKind parentKind)
    {
        var horizontalAlignment = MirageCommonViewProjection.ResolveHorizontalAlignment(childView);
        var verticalAlignment = MirageCommonViewProjection.ResolveVerticalAlignment(childView);

        var changed = false;
        var zIndex = childView is VisualElement visualElement
            ? visualElement.ZIndex
            : childView.ZIndex;
        var zIndexStyle = zIndex == 0
            ? null
            : zIndex.ToString(CultureInfo.InvariantCulture);
        changed |= SetChildStyleIfChanged(childPlatformView, "zIndex", zIndexStyle);
        changed |= SetChildStyleIfChanged(childPlatformView, "position", "relative");
        changed |= SetChildStyleIfChanged(childPlatformView, "left", null);
        changed |= SetChildStyleIfChanged(childPlatformView, "top", null);
        changed |= SetChildStyleIfChanged(childPlatformView, "right", null);
        changed |= SetChildStyleIfChanged(childPlatformView, "bottom", null);

        switch (parentKind)
        {
            case ComponentKind.VerticalStack:
                changed |= SetChildStyleIfChanged(childPlatformView, "flexGrow", "0");
                changed |= SetChildStyleIfChanged(childPlatformView, "flexShrink", "0");
                changed |= SetChildStyleIfChanged(childPlatformView, "flexBasis", "auto");
                changed |= SetChildStyleIfChanged(childPlatformView, "order", null);
                changed |= MirageCommonViewProjection.SetLayoutTransformFragment(childPlatformView, null);
                changed |= SetChildStyleIfChanged(childPlatformView, "gridRowStart", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "gridRowEnd", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "gridColumnStart", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "gridColumnEnd", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "justifySelf", null);
                changed |= SetChildStyleIfChanged(
                    childPlatformView,
                    "alignSelf",
                    MirageCommonViewProjection.ResolveStackCrossAxisAlignment(
                        MirageCommonViewProjection.ResolveEffectiveStackCrossAxisAlignment(
                            childView,
                            parentIsVerticalStack: true)));
                break;
            case ComponentKind.HorizontalStack:
                changed |= SetChildStyleIfChanged(childPlatformView, "flexGrow", "0");
                changed |= SetChildStyleIfChanged(childPlatformView, "flexShrink", "0");
                changed |= SetChildStyleIfChanged(childPlatformView, "flexBasis", "auto");
                changed |= SetChildStyleIfChanged(childPlatformView, "order", null);
                changed |= MirageCommonViewProjection.SetLayoutTransformFragment(childPlatformView, null);
                changed |= SetChildStyleIfChanged(childPlatformView, "gridRowStart", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "gridRowEnd", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "gridColumnStart", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "gridColumnEnd", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "justifySelf", null);
                changed |= SetChildStyleIfChanged(
                    childPlatformView,
                    "alignSelf",
                    MirageCommonViewProjection.ResolveStackCrossAxisAlignment(
                        MirageCommonViewProjection.ResolveEffectiveStackCrossAxisAlignment(
                            childView,
                            parentIsVerticalStack: false)));
                break;
            case ComponentKind.Grid:
                changed |= SetChildStyleIfChanged(childPlatformView, "flexGrow", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "flexShrink", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "flexBasis", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "order", null);
                changed |= MirageCommonViewProjection.SetLayoutTransformFragment(childPlatformView, null);
                var (row, column, rowSpan, columnSpan) = GetGridPlacement(childView);
                changed |= SetChildStyleIfChanged(childPlatformView, "gridRowStart", (row + 1).ToString(CultureInfo.InvariantCulture));
                changed |= SetChildStyleIfChanged(childPlatformView, "gridRowEnd", $"span {rowSpan.ToString(CultureInfo.InvariantCulture)}");
                changed |= SetChildStyleIfChanged(childPlatformView, "gridColumnStart", (column + 1).ToString(CultureInfo.InvariantCulture));
                changed |= SetChildStyleIfChanged(childPlatformView, "gridColumnEnd", $"span {columnSpan.ToString(CultureInfo.InvariantCulture)}");
                changed |= SetChildStyleIfChanged(childPlatformView, "justifySelf", MirageCommonViewProjection.ResolveGridSelfAlignment(horizontalAlignment));
                changed |= SetChildStyleIfChanged(childPlatformView, "alignSelf", MirageCommonViewProjection.ResolveGridSelfAlignment(verticalAlignment));
                break;
            case ComponentKind.AbsoluteLayout:
                changed |= SetChildStyleIfChanged(childPlatformView, "flexGrow", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "flexShrink", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "flexBasis", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "order", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "gridRowStart", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "gridRowEnd", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "gridColumnStart", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "gridColumnEnd", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "justifySelf", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "alignSelf", null);
                changed |= ApplyAbsoluteLayoutChildStyles(childPlatformView, childView);
                break;
            case ComponentKind.Layout:
                if (IsFlexLayoutParent(childView))
                {
                    changed |= SetChildStyleIfChanged(childPlatformView, "gridRowStart", null);
                    changed |= SetChildStyleIfChanged(childPlatformView, "gridRowEnd", null);
                    changed |= SetChildStyleIfChanged(childPlatformView, "gridColumnStart", null);
                    changed |= SetChildStyleIfChanged(childPlatformView, "gridColumnEnd", null);
                    changed |= SetChildStyleIfChanged(childPlatformView, "justifySelf", null);
                    changed |= ApplyFlexLayoutChildStyles(childPlatformView, childView);
                    break;
                }

                changed |= SetChildStyleIfChanged(childPlatformView, "flexGrow", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "flexShrink", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "flexBasis", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "order", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "gridRowStart", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "gridRowEnd", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "gridColumnStart", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "gridColumnEnd", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "justifySelf", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "alignSelf", null);
                changed |= ApplyCustomLayoutChildStyles(childPlatformView, childView);
                break;
            default:
                changed |= SetChildStyleIfChanged(childPlatformView, "flexGrow", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "flexShrink", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "flexBasis", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "order", null);
                changed |= MirageCommonViewProjection.SetLayoutTransformFragment(childPlatformView, null);
                break;
        }

        return changed;
    }

    public static bool ApplyAbsoluteLayoutArrangedChildStyles(MiragePlatformView childPlatformView, IView childView)
    {
        var frame = childView.Frame;
        var parentPadding = ResolveParentPadding(childView);
        var x = NormalizeFinite(frame.X - parentPadding.Left, 0);
        var y = NormalizeFinite(frame.Y - parentPadding.Top, 0);
        var width = Math.Max(0, NormalizeFinite(frame.Width, 0));
        var height = Math.Max(0, NormalizeFinite(frame.Height, 0));

        string? widthStyle = MirageViewMapper.ToPx(width);
        string? heightStyle = MirageViewMapper.ToPx(height);
        if (childView is BindableObject bindable)
        {
            var bounds = AbsoluteLayout.GetLayoutBounds(bindable);
            var flags = AbsoluteLayout.GetLayoutFlags(bindable);
            var widthProportional = IsAbsoluteLayoutFlagSet(flags, AbsoluteLayoutFlags.SizeProportional)
                                    || IsAbsoluteLayoutFlagSet(flags, AbsoluteLayoutFlags.WidthProportional);
            var heightProportional = IsAbsoluteLayoutFlagSet(flags, AbsoluteLayoutFlags.SizeProportional)
                                     || IsAbsoluteLayoutFlagSet(flags, AbsoluteLayoutFlags.HeightProportional);

            if (width <= 0 && bounds.Width.Equals(AbsoluteLayout.AutoSize))
            {
                widthStyle = ResolveAbsoluteLayoutSize(childView, bounds.Width, widthProportional, isWidth: true);
            }

            if (height <= 0 && bounds.Height.Equals(AbsoluteLayout.AutoSize))
            {
                heightStyle = ResolveAbsoluteLayoutSize(childView, bounds.Height, heightProportional, isWidth: false);
            }
        }

        var changed = SetChildStyleIfChanged(childPlatformView, "position", "absolute");
        changed |= SetChildStyleIfChanged(childPlatformView, "left", MirageCommonViewProjection.ToPxNoClamp(x));
        changed |= SetChildStyleIfChanged(childPlatformView, "top", MirageCommonViewProjection.ToPxNoClamp(y));
        changed |= SetChildStyleIfChanged(childPlatformView, "right", null);
        changed |= SetChildStyleIfChanged(childPlatformView, "bottom", null);
        changed |= SetChildStyleIfChanged(childPlatformView, "width", widthStyle);
        changed |= SetChildStyleIfChanged(childPlatformView, "height", heightStyle);
        changed |= MirageCommonViewProjection.SetLayoutTransformFragment(childPlatformView, null);
        return changed;
    }

    public static bool ApplyAbsoluteLayoutChildStyles(MiragePlatformView childPlatformView, IView childView)
    {
        var changed = SetChildStyleIfChanged(childPlatformView, "position", "absolute");

        if (childView is not BindableObject bindable)
        {
            changed |= SetChildStyleIfChanged(childPlatformView, "left", "0px");
            changed |= SetChildStyleIfChanged(childPlatformView, "top", "0px");
            changed |= SetChildStyleIfChanged(childPlatformView, "right", null);
            changed |= SetChildStyleIfChanged(childPlatformView, "bottom", null);
            changed |= SetChildStyleIfChanged(childPlatformView, "width", null);
            changed |= SetChildStyleIfChanged(childPlatformView, "height", null);
            changed |= MirageCommonViewProjection.SetLayoutTransformFragment(childPlatformView, null);
            return changed;
        }

        var bounds = AbsoluteLayout.GetLayoutBounds(bindable);
        var flags = AbsoluteLayout.GetLayoutFlags(bindable);

        var xProportional = IsAbsoluteLayoutFlagSet(flags, AbsoluteLayoutFlags.PositionProportional)
                            || IsAbsoluteLayoutFlagSet(flags, AbsoluteLayoutFlags.XProportional);
        var yProportional = IsAbsoluteLayoutFlagSet(flags, AbsoluteLayoutFlags.PositionProportional)
                            || IsAbsoluteLayoutFlagSet(flags, AbsoluteLayoutFlags.YProportional);
        var widthProportional = IsAbsoluteLayoutFlagSet(flags, AbsoluteLayoutFlags.SizeProportional)
                                || IsAbsoluteLayoutFlagSet(flags, AbsoluteLayoutFlags.WidthProportional);
        var heightProportional = IsAbsoluteLayoutFlagSet(flags, AbsoluteLayoutFlags.SizeProportional)
                                 || IsAbsoluteLayoutFlagSet(flags, AbsoluteLayoutFlags.HeightProportional);

        var x = NormalizeFinite(bounds.X, 0);
        var y = NormalizeFinite(bounds.Y, 0);

        changed |= SetChildStyleIfChanged(childPlatformView, "left", xProportional ? ToPercent(x) : MirageCommonViewProjection.ToPxNoClamp(x));
        changed |= SetChildStyleIfChanged(childPlatformView, "top", yProportional ? ToPercent(y) : MirageCommonViewProjection.ToPxNoClamp(y));
        changed |= SetChildStyleIfChanged(childPlatformView, "right", null);
        changed |= SetChildStyleIfChanged(childPlatformView, "bottom", null);

        changed |= SetChildStyleIfChanged(childPlatformView, "width", ResolveAbsoluteLayoutSize(childView, bounds.Width, widthProportional, isWidth: true));
        changed |= SetChildStyleIfChanged(childPlatformView, "height", ResolveAbsoluteLayoutSize(childView, bounds.Height, heightProportional, isWidth: false));

        changed |= MirageCommonViewProjection.SetLayoutTransformFragment(
            childPlatformView,
            ResolveAbsoluteLayoutTranslate(xProportional ? x : null, yProportional ? y : null));
        return changed;
    }

    public static bool ApplyCustomLayoutChildStyles(MiragePlatformView childPlatformView, IView childView)
    {
        var frame = childView.Frame;
        var parentPadding = ResolveParentPadding(childView);
        var x = NormalizeFinite(frame.X - parentPadding.Left, 0);
        var y = NormalizeFinite(frame.Y - parentPadding.Top, 0);
        var width = Math.Max(0, NormalizeFinite(frame.Width, 0));
        var height = Math.Max(0, NormalizeFinite(frame.Height, 0));

        var changed = SetChildStyleIfChanged(childPlatformView, "position", "absolute");
        changed |= SetChildStyleIfChanged(childPlatformView, "left", MirageCommonViewProjection.ToPxNoClamp(x));
        changed |= SetChildStyleIfChanged(childPlatformView, "top", MirageCommonViewProjection.ToPxNoClamp(y));
        changed |= SetChildStyleIfChanged(childPlatformView, "right", null);
        changed |= SetChildStyleIfChanged(childPlatformView, "bottom", null);
        changed |= SetChildStyleIfChanged(childPlatformView, "width", MirageViewMapper.ToPx(width));
        changed |= SetChildStyleIfChanged(childPlatformView, "height", MirageViewMapper.ToPx(height));
        changed |= MirageCommonViewProjection.SetLayoutTransformFragment(childPlatformView, null);
        return changed;
    }

    private static bool IsFlexLayoutParent(IView childView)
    {
        return childView is IElement { Parent: FlexLayout };
    }

    private static bool ApplyFlexLayoutChildStyles(MiragePlatformView childPlatformView, IView childView)
    {
        if (IsAbsoluteFlexLayoutChild(childView))
        {
            return ApplyAbsoluteFlexLayoutChildStyles(childPlatformView, childView);
        }

        var changed = SetChildStyleIfChanged(childPlatformView, "position", "relative");
        changed |= SetChildStyleIfChanged(childPlatformView, "left", null);
        changed |= SetChildStyleIfChanged(childPlatformView, "top", null);
        changed |= SetChildStyleIfChanged(childPlatformView, "right", null);
        changed |= SetChildStyleIfChanged(childPlatformView, "bottom", null);
        changed |= SetChildStyleIfChanged(childPlatformView, "margin", MirageCommonViewProjection.ToMarginThickness(childView.Margin));

        if (childView is not BindableObject bindableObject)
        {
            changed |= SetChildStyleIfChanged(childPlatformView, "alignSelf", null);
            changed |= SetChildStyleIfChanged(childPlatformView, "order", "0");
            changed |= SetChildStyleIfChanged(childPlatformView, "flexGrow", "0");
            changed |= SetChildStyleIfChanged(childPlatformView, "flexShrink", "1");
            changed |= SetChildStyleIfChanged(childPlatformView, "flexBasis", "auto");
            changed |= MirageCommonViewProjection.SetLayoutTransformFragment(childPlatformView, null);
            return changed;
        }

        var order = FlexLayout.GetOrder(bindableObject);
        var grow = NormalizeNonNegative(FlexLayout.GetGrow(bindableObject));
        var shrink = NormalizeNonNegative(FlexLayout.GetShrink(bindableObject), fallback: 1);
        var alignSelf = ResolveFlexAlignSelf(FlexLayout.GetAlignSelf(bindableObject));
        var basis = ResolveFlexBasis(FlexLayout.GetBasis(bindableObject));

        changed |= SetChildStyleIfChanged(childPlatformView, "alignSelf", alignSelf);
        changed |= SetChildStyleIfChanged(childPlatformView, "order", order.ToString(CultureInfo.InvariantCulture));
        changed |= SetChildStyleIfChanged(childPlatformView, "flexGrow", grow.ToString("0.###", CultureInfo.InvariantCulture));
        changed |= SetChildStyleIfChanged(childPlatformView, "flexShrink", shrink.ToString("0.###", CultureInfo.InvariantCulture));
        changed |= SetChildStyleIfChanged(childPlatformView, "flexBasis", basis);
        if (childView is IFlexLayout && childView is VisualElement visualElement)
        {
            changed |= SetChildStyleIfChanged(childPlatformView, "width", ResolveRequestedLength(visualElement.WidthRequest));
            changed |= SetChildStyleIfChanged(childPlatformView, "height", ResolveRequestedLength(visualElement.HeightRequest));
        }

        changed |= MirageCommonViewProjection.SetLayoutTransformFragment(childPlatformView, null);
        return changed;
    }

    public static bool ApplyAbsoluteFlexLayoutChildStyles(MiragePlatformView childPlatformView, IView childView)
    {
        var changed = SetChildStyleIfChanged(childPlatformView, "margin", null);
        changed |= SetChildStyleIfChanged(childPlatformView, "alignSelf", null);
        changed |= SetChildStyleIfChanged(childPlatformView, "order", null);
        changed |= SetChildStyleIfChanged(childPlatformView, "flexGrow", null);
        changed |= SetChildStyleIfChanged(childPlatformView, "flexShrink", null);
        changed |= SetChildStyleIfChanged(childPlatformView, "flexBasis", null);
        changed |= ApplyCustomLayoutChildStyles(childPlatformView, childView);
        return changed;
    }

    private static bool IsAbsoluteFlexLayoutChild(IView childView)
    {
        // Upstream MAUI FlexLayout.Position is an instance property on IFlexLayout children.
        return childView is IFlexLayout { Position: FlexPosition.Absolute };
    }

    private static (int Row, int Column, int RowSpan, int ColumnSpan) GetGridPlacement(IView childView)
    {
        if (childView is not BindableObject bindable)
        {
            return (0, 0, 1, 1);
        }

        var row = Math.Max(0, Grid.GetRow(bindable));
        var column = Math.Max(0, Grid.GetColumn(bindable));
        var rowSpan = Math.Max(1, Grid.GetRowSpan(bindable));
        var columnSpan = Math.Max(1, Grid.GetColumnSpan(bindable));
        return (row, column, rowSpan, columnSpan);
    }

    private static Thickness ResolveParentPadding(IView childView)
    {
        if (childView is not IElement { Parent: Layout parentLayout })
        {
            return Thickness.Zero;
        }

        return parentLayout.Padding;
    }

    private static bool IsAbsoluteLayoutFlagSet(AbsoluteLayoutFlags flags, AbsoluteLayoutFlags flag)
    {
        return (flags & flag) == flag;
    }

    private static string? ResolveAbsoluteLayoutSize(IView childView, double value, bool proportional, bool isWidth)
    {
        if (value.Equals(AbsoluteLayout.AutoSize))
        {
            return ResolveAbsoluteLayoutAutoSizeLength(childView, isWidth);
        }

        var normalized = NormalizeFinite(value, 0);
        if (proportional)
        {
            return ToPercent(Math.Max(0, normalized));
        }

        return MirageViewMapper.ToPx(Math.Max(0, normalized));
    }

    private static string? ResolveAbsoluteLayoutAutoSizeLength(IView childView, bool isWidth)
    {
        var desiredLength = isWidth ? childView.DesiredSize.Width : childView.DesiredSize.Height;
        if (double.IsFinite(desiredLength) && desiredLength > 0)
        {
            return MirageViewMapper.ToPx(desiredLength);
        }

        if (childView is not VisualElement visualElement)
        {
            return null;
        }

        var requestedLength = isWidth ? visualElement.WidthRequest : visualElement.HeightRequest;
        if (!double.IsFinite(requestedLength) || requestedLength < 0)
        {
            return null;
        }

        return MirageViewMapper.ToPx(requestedLength);
    }

    private static string? ResolveAbsoluteLayoutTranslate(double? proportionalX, double? proportionalY)
    {
        if (proportionalX is null && proportionalY is null)
        {
            return null;
        }

        if (proportionalX is null && proportionalY is double yOnly)
        {
            return $"translateY({ToTranslatePercent(yOnly)})";
        }

        if (proportionalY is null && proportionalX is double xOnly)
        {
            return $"translateX({ToTranslatePercent(xOnly)})";
        }

        return $"translate({ToTranslatePercent(proportionalX ?? 0)}, {ToTranslatePercent(proportionalY ?? 0)})";
    }

    private static string ToTranslatePercent(double proportionalValue)
    {
        var value = NormalizeFinite(proportionalValue, 0) * -100d;
        return $"{value.ToString("0.###", CultureInfo.InvariantCulture)}%";
    }

    private static string ToPercent(double value)
    {
        var percentValue = NormalizeFinite(value, 0) * 100d;
        return $"{percentValue.ToString("0.###", CultureInfo.InvariantCulture)}%";
    }

    private static double NormalizeNonNegative(float value, double fallback = 0)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            return fallback;
        }

        return Math.Max(0, value);
    }

    private static string? ResolveFlexAlignSelf(FlexAlignSelf alignSelf)
    {
        return alignSelf switch
        {
            FlexAlignSelf.Start => "flex-start",
            FlexAlignSelf.Center => "center",
            FlexAlignSelf.End => "flex-end",
            FlexAlignSelf.Stretch => "stretch",
            _ => null
        };
    }

    private static string ResolveFlexBasis(FlexBasis basis)
    {
        if (basis == FlexBasis.Auto)
        {
            return "auto";
        }

        if (TryResolveRelativeFlexBasisPercent(basis, out var percent))
        {
            return $"{percent.ToString("0.###", CultureInfo.InvariantCulture)}%";
        }

        var length = (double)basis.Length;
        if (!double.IsFinite(length))
        {
            return "auto";
        }

        return MirageViewMapper.ToPx(Math.Max(0, length));
    }

    private static bool TryResolveRelativeFlexBasisPercent(FlexBasis basis, out double percent)
    {
        percent = 0;

        if (FlexBasisIsRelativeGetter?.Invoke(basis, null) is not bool isRelative || !isRelative)
        {
            return false;
        }

        var relativeLength = (double)basis.Length;
        if (!double.IsFinite(relativeLength))
        {
            return false;
        }

        percent = Math.Max(0, relativeLength * 100d);
        return true;
    }

    private static double NormalizeFinite(double value, double fallback)
    {
        if (!double.IsFinite(value))
        {
            return fallback;
        }

        return value;
    }

    private static string? ResolveRequestedLength(double requestedLength)
    {
        if (!double.IsFinite(requestedLength) || requestedLength < 0)
        {
            return null;
        }

        return MirageViewMapper.ToPx(requestedLength);
    }

    private static bool SetChildStyleIfChanged(MiragePlatformView childPlatformView, string styleName, string? value)
    {
        if (string.Equals(childPlatformView.GetStyleValue(styleName), value, StringComparison.Ordinal))
        {
            return false;
        }

        childPlatformView.SetStyle(styleName, value);
        return true;
    }
}
