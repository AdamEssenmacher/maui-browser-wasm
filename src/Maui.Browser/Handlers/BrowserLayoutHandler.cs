using System.Globalization;
using Maui.Browser.Dom;
using Maui.Browser.Utils;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using MauiLayout = Microsoft.Maui.ILayout;

namespace Maui.Browser.Handlers;

public sealed class BrowserLayoutHandler : BrowserViewHandler<MauiLayout>, ILayoutHandler
{
    internal enum BrowserLayoutProjectionMode
    {
        Absolute,
        FlexColumn,
        FlexRow,
        CssGrid
    }

    public static IPropertyMapper<MauiLayout, BrowserLayoutHandler> Mapper = new PropertyMapper<MauiLayout, BrowserLayoutHandler>(BrowserViewMapper.CommonViewMapper)
    {
        [nameof(IView.Visibility)] = MapVisibility,
        [nameof(MauiLayout.ClipsToBounds)] = MapClipsToBounds,
        [nameof(MauiLayout.Padding)] = MapPadding
    };

    public static CommandMapper<MauiLayout, BrowserLayoutHandler> CommandMapper = new(BrowserViewMapper.CommonViewCommandMapper)
    {
        [nameof(ILayoutHandler.Add)] = MapAdd,
        [nameof(ILayoutHandler.Remove)] = MapRemove,
        [nameof(ILayoutHandler.Clear)] = MapClear,
        [nameof(ILayoutHandler.Insert)] = MapInsert,
        [nameof(ILayoutHandler.Update)] = MapUpdate,
        [nameof(ILayoutHandler.UpdateZIndex)] = MapUpdateZIndex
    };

    public BrowserLayoutHandler()
        : base(Mapper, CommandMapper)
    {
    }

    private readonly Dictionary<IView, IElementHandler> _childHandlers = new();
    private BrowserLayoutProjectionMode _projectionMode;

    MauiLayout ILayoutHandler.VirtualView => VirtualView;

    object ILayoutHandler.PlatformView => PlatformView;

    protected override DomElement CreatePlatformView() => DomElement.Create("div");

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        return VirtualView.CrossPlatformMeasure(widthConstraint, heightConstraint);
    }

    public override void PlatformArrange(Rect frame)
    {
        base.PlatformArrange(frame);
        VirtualView.CrossPlatformArrange(new Rect(0, 0, frame.Width, frame.Height));
        ApplyProjectionStyles(ResolveProjectionMode(VirtualView));
    }

    protected override void ConnectHandler(DomElement platformView)
    {
        _projectionMode = ResolveProjectionMode(VirtualView);
        platformView.Style("overflow", "visible");
        ApplyContainerProjectionStyles(platformView, VirtualView, _projectionMode);
        RebuildChildren();
        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(DomElement platformView)
    {
        DisconnectAllChildren();
        base.DisconnectHandler(platformView);
    }

    public void Add(IView view)
    {
        if (!TryGetChild(view, out var childHandler, out var childPlatformView))
        {
            return;
        }

        _childHandlers[view] = childHandler;
        PlatformView.Append(childPlatformView);
        UpdateZIndex(view);
        ApplyChildProjectionStyles(childPlatformView, view, _projectionMode);
    }

    public void Remove(IView view)
    {
        DisconnectChild(view, removeFromDom: true);
        ApplyProjectionStyles(_projectionMode);
    }

    public void Clear()
    {
        DisconnectAllChildren();
        PlatformView.ClearChildren();
        ApplyProjectionStyles(_projectionMode);
    }

    public void Insert(int index, IView view)
    {
        RebuildChildren();
    }

    public void Update(int index, IView view)
    {
        RebuildChildren();
    }

    public void UpdateZIndex(IView view)
    {
        if (!TryGetChild(view, out _, out var childPlatformView))
        {
            return;
        }

        childPlatformView.Style("zIndex", view.ZIndex.ToString(CultureInfo.InvariantCulture));
    }

    public static void MapClipsToBounds(BrowserLayoutHandler handler, MauiLayout layout)
    {
        handler.PlatformView.Style("overflow", layout.ClipsToBounds ? "hidden" : "visible");
    }

    public static void MapVisibility(BrowserLayoutHandler handler, MauiLayout layout)
    {
        BrowserViewMapper.MapVisibility(layout, handler.PlatformView);
        if (layout.Visibility == Visibility.Collapsed)
        {
            return;
        }

        handler.ApplyProjectionStyles(ResolveProjectionMode(layout));
    }

    public static void MapPadding(BrowserLayoutHandler handler, MauiLayout layout)
    {
        handler.ApplyPaddingStyle(layout, ResolveProjectionMode(layout));
    }

    public static void MapAdd(BrowserLayoutHandler handler, MauiLayout layout, object? arg)
    {
        if (arg is LayoutHandlerUpdate update)
        {
            handler.Add(update.View);
            return;
        }

        handler.RebuildChildren();
    }

    public static void MapRemove(BrowserLayoutHandler handler, MauiLayout layout, object? arg)
    {
        if (arg is LayoutHandlerUpdate update)
        {
            handler.Remove(update.View);
            return;
        }

        handler.RebuildChildren();
    }

    public static void MapClear(BrowserLayoutHandler handler, MauiLayout layout, object? arg)
    {
        handler.Clear();
    }

    public static void MapInsert(BrowserLayoutHandler handler, MauiLayout layout, object? arg)
    {
        if (arg is LayoutHandlerUpdate update)
        {
            handler.Insert(update.Index, update.View);
            return;
        }

        handler.RebuildChildren();
    }

    public static void MapUpdate(BrowserLayoutHandler handler, MauiLayout layout, object? arg)
    {
        if (arg is LayoutHandlerUpdate update)
        {
            handler.Update(update.Index, update.View);
            return;
        }

        handler.RebuildChildren();
    }

    public static void MapUpdateZIndex(BrowserLayoutHandler handler, MauiLayout layout, object? arg)
    {
        if (arg is IView view)
        {
            handler.UpdateZIndex(view);
        }
    }

    private void RebuildChildren()
    {
        var currentChildren = new HashSet<IView>(VirtualView);
        var removedChildren = _childHandlers.Keys.Where(child => !currentChildren.Contains(child)).ToArray();
        foreach (var removedChild in removedChildren)
        {
            DisconnectChild(removedChild, removeFromDom: false);
        }

        PlatformView.ClearChildren();

        foreach (var child in VirtualView)
        {
            if (!TryGetChild(child, out var childHandler, out var childPlatformView))
            {
                continue;
            }

            _childHandlers[child] = childHandler;
            PlatformView.Append(childPlatformView);
            UpdateZIndex(child);
            ApplyChildProjectionStyles(childPlatformView, child, _projectionMode);
        }

        ApplyProjectionStyles(_projectionMode);
    }

    private bool TryGetChild(IView view, out IElementHandler childHandler, out DomElement childPlatformView)
    {
        if (MauiContext is null)
        {
            childHandler = null!;
            childPlatformView = null!;
            return false;
        }

        childHandler = view.ToHandler(MauiContext);
        if (childHandler.PlatformView is DomElement domElement)
        {
            childPlatformView = domElement;
            return true;
        }

        childHandler = null!;
        childPlatformView = null!;
        return false;
    }

    private void DisconnectAllChildren()
    {
        foreach (var child in _childHandlers.Keys.ToArray())
        {
            DisconnectChild(child, removeFromDom: false);
        }

        _childHandlers.Clear();
    }

    private void DisconnectChild(IView child, bool removeFromDom)
    {
        if (!_childHandlers.TryGetValue(child, out var childHandler))
        {
            return;
        }

        if (removeFromDom && childHandler.PlatformView is DomElement childPlatformView)
        {
            try
            {
                PlatformView.Remove(childPlatformView);
            }
            catch
            {
                // Child may already be detached; continue with handler cleanup.
            }
        }

        childHandler.DisconnectHandler();
        _childHandlers.Remove(child);
    }

    private void ApplyProjectionStyles(BrowserLayoutProjectionMode projectionMode)
    {
        _projectionMode = projectionMode;
        ApplyContainerProjectionStyles(PlatformView, VirtualView, projectionMode);
        ApplyPaddingStyle(VirtualView, projectionMode);

        foreach (var entry in _childHandlers)
        {
            var childView = entry.Key;
            var handler = entry.Value;
            if (handler.PlatformView is not DomElement childPlatformView)
            {
                continue;
            }

            ApplyChildProjectionStyles(childPlatformView, childView, projectionMode);
        }
    }

    internal static BrowserLayoutProjectionMode ResolveProjectionMode(MauiLayout layout)
    {
        return layout switch
        {
            VerticalStackLayout => BrowserLayoutProjectionMode.FlexColumn,
            HorizontalStackLayout => BrowserLayoutProjectionMode.FlexRow,
            Grid grid when IsGridMetadataUsable(grid) => BrowserLayoutProjectionMode.CssGrid,
            _ => BrowserLayoutProjectionMode.Absolute
        };
    }

    internal static void ApplyContainerProjectionStyles(
        DomElement platformView,
        MauiLayout layout,
        BrowserLayoutProjectionMode projectionMode)
    {
        if (layout.Visibility == Visibility.Collapsed)
        {
            platformView.Style("display", "none");
            return;
        }

        if (projectionMode == BrowserLayoutProjectionMode.Absolute)
        {
            platformView.Style("display", null);
            platformView.Style("flexDirection", null);
            platformView.Style("alignItems", null);
            platformView.Style("justifyContent", null);
            platformView.Style("gap", null);
            platformView.Style("gridTemplateColumns", null);
            platformView.Style("gridTemplateRows", null);
            platformView.Style("columnGap", null);
            platformView.Style("rowGap", null);
            return;
        }

        if (projectionMode == BrowserLayoutProjectionMode.CssGrid)
        {
            if (layout is not Grid grid
                || !TryBuildGridTrackTemplate(grid.ColumnDefinitions.Select(definition => definition.Width), out var columnsTemplate)
                || !TryBuildGridTrackTemplate(grid.RowDefinitions.Select(definition => definition.Height), out var rowsTemplate))
            {
                platformView.Style("display", null);
                platformView.Style("gridTemplateColumns", null);
                platformView.Style("gridTemplateRows", null);
                platformView.Style("columnGap", null);
                platformView.Style("rowGap", null);
                return;
            }

            platformView.Style("display", "grid");
            platformView.Style("gridTemplateColumns", columnsTemplate);
            platformView.Style("gridTemplateRows", rowsTemplate);
            platformView.Style("columnGap", CssUtil.Px(grid.ColumnSpacing));
            platformView.Style("rowGap", CssUtil.Px(grid.RowSpacing));
            platformView.Style("gap", $"{CssUtil.Px(grid.RowSpacing)} {CssUtil.Px(grid.ColumnSpacing)}");
            platformView.Style("flexDirection", null);
            platformView.Style("alignItems", null);
            platformView.Style("justifyContent", null);
            return;
        }

        var spacing = GetLayoutSpacing(layout);
        platformView.Style("display", "flex");
        platformView.Style("flexDirection", projectionMode == BrowserLayoutProjectionMode.FlexRow ? "row" : "column");
        platformView.Style("alignItems", "stretch");
        platformView.Style("justifyContent", "flex-start");
        platformView.Style("gap", CssUtil.Px(spacing));
        platformView.Style("gridTemplateColumns", null);
        platformView.Style("gridTemplateRows", null);
        platformView.Style("columnGap", null);
        platformView.Style("rowGap", null);
    }

    internal static void ApplyChildProjectionStyles(DomElement childPlatformView, IView childView, BrowserLayoutProjectionMode projectionMode)
    {
        if (projectionMode == BrowserLayoutProjectionMode.Absolute)
        {
            childPlatformView.Style("alignSelf", null);
            childPlatformView.Style("justifySelf", null);
            childPlatformView.Style("margin", null);
            childPlatformView.Style("gridRowStart", null);
            childPlatformView.Style("gridRowEnd", null);
            childPlatformView.Style("gridColumnStart", null);
            childPlatformView.Style("gridColumnEnd", null);
            return;
        }

        if (projectionMode == BrowserLayoutProjectionMode.CssGrid)
        {
            var (row, column, rowSpan, columnSpan) = GetGridPlacement(childView);
            childPlatformView.Style("position", "relative");
            childPlatformView.Style("left", null);
            childPlatformView.Style("top", null);
            childPlatformView.Style("right", null);
            childPlatformView.Style("bottom", null);
            childPlatformView.Style("margin", ToCssThickness(childView.Margin));
            childPlatformView.Style("gridRowStart", (row + 1).ToString(CultureInfo.InvariantCulture));
            childPlatformView.Style("gridRowEnd", $"span {rowSpan.ToString(CultureInfo.InvariantCulture)}");
            childPlatformView.Style("gridColumnStart", (column + 1).ToString(CultureInfo.InvariantCulture));
            childPlatformView.Style("gridColumnEnd", $"span {columnSpan.ToString(CultureInfo.InvariantCulture)}");
            childPlatformView.Style("justifySelf", ResolveGridSelfAlignment(childView.HorizontalLayoutAlignment));
            childPlatformView.Style("alignSelf", ResolveGridSelfAlignment(childView.VerticalLayoutAlignment));
            return;
        }

        childPlatformView.Style("position", "relative");
        childPlatformView.Style("left", null);
        childPlatformView.Style("top", null);
        childPlatformView.Style("right", null);
        childPlatformView.Style("bottom", null);
        childPlatformView.Style("gridRowStart", null);
        childPlatformView.Style("gridRowEnd", null);
        childPlatformView.Style("gridColumnStart", null);
        childPlatformView.Style("gridColumnEnd", null);
        childPlatformView.Style("justifySelf", null);
        childPlatformView.Style("alignSelf", ResolveChildAlignSelf(childView, projectionMode));
        childPlatformView.Style("margin", ToCssThickness(childView.Margin));
    }

    private static double GetLayoutSpacing(MauiLayout layout)
    {
        var spacing = layout switch
        {
            VerticalStackLayout verticalStackLayout => verticalStackLayout.Spacing,
            HorizontalStackLayout horizontalStackLayout => horizontalStackLayout.Spacing,
            _ => 0
        };

        if (double.IsNaN(spacing) || double.IsInfinity(spacing))
        {
            return 0;
        }

        return Math.Max(0, spacing);
    }

    private void ApplyPaddingStyle(MauiLayout layout, BrowserLayoutProjectionMode projectionMode)
    {
        if (projectionMode == BrowserLayoutProjectionMode.Absolute)
        {
            // Child frames are already arranged with MAUI padding applied, so adding CSS padding
            // here would double-inset absolutely-positioned children.
            PlatformView.Style("padding", string.Empty);
            return;
        }

        var padding = layout.Padding;
        var left = NormalizePaddingValue(padding.Left);
        var top = NormalizePaddingValue(padding.Top);
        var right = NormalizePaddingValue(padding.Right);
        var bottom = NormalizePaddingValue(padding.Bottom);
        PlatformView.Style("padding", $"{CssUtil.Px(top)} {CssUtil.Px(right)} {CssUtil.Px(bottom)} {CssUtil.Px(left)}");
    }

    private static double NormalizePaddingValue(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        return Math.Max(0, value);
    }

    private static string ResolveChildAlignSelf(IView childView, BrowserLayoutProjectionMode projectionMode)
    {
        var alignment = projectionMode == BrowserLayoutProjectionMode.FlexColumn
            ? childView.HorizontalLayoutAlignment
            : childView.VerticalLayoutAlignment;

        return alignment switch
        {
            Microsoft.Maui.Primitives.LayoutAlignment.Center => "center",
            Microsoft.Maui.Primitives.LayoutAlignment.End => "flex-end",
            Microsoft.Maui.Primitives.LayoutAlignment.Start => "flex-start",
            _ => "stretch"
        };
    }

    private static string ToCssThickness(Thickness thickness)
    {
        var left = NormalizeThicknessValue(thickness.Left);
        var top = NormalizeThicknessValue(thickness.Top);
        var right = NormalizeThicknessValue(thickness.Right);
        var bottom = NormalizeThicknessValue(thickness.Bottom);
        return $"{CssUtil.Px(top)} {CssUtil.Px(right)} {CssUtil.Px(bottom)} {CssUtil.Px(left)}";
    }

    private static double NormalizeThicknessValue(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        return value;
    }

    private static bool IsGridMetadataUsable(Grid grid)
    {
        if (!IsValidGridSpacing(grid.ColumnSpacing) || !IsValidGridSpacing(grid.RowSpacing))
        {
            return false;
        }

        return TryBuildGridTrackTemplate(grid.ColumnDefinitions.Select(definition => definition.Width), out _)
            && TryBuildGridTrackTemplate(grid.RowDefinitions.Select(definition => definition.Height), out _);
    }

    private static bool IsValidGridSpacing(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return false;
        }

        return value >= 0;
    }

    private static bool TryBuildGridTrackTemplate(IEnumerable<GridLength> trackLengths, out string? template)
    {
        var parts = new List<string>();
        foreach (var trackLength in trackLengths)
        {
            if (!TryConvertGridTrack(trackLength, out var trackPart))
            {
                template = null;
                return false;
            }

            parts.Add(trackPart);
        }

        // MAUI Grid with no explicit definitions behaves as a single star track.
        template = parts.Count > 0 ? string.Join(" ", parts) : "1fr";
        return true;
    }

    private static bool TryConvertGridTrack(GridLength trackLength, out string cssTrack)
    {
        if (trackLength.IsAuto)
        {
            cssTrack = "auto";
            return true;
        }

        if (trackLength.IsStar)
        {
            var starValue = trackLength.Value;
            if (double.IsNaN(starValue) || double.IsInfinity(starValue) || starValue <= 0)
            {
                starValue = 1;
            }

            cssTrack = $"{starValue.ToString("0.###", CultureInfo.InvariantCulture)}fr";
            return true;
        }

        if (trackLength.IsAbsolute)
        {
            var absoluteValue = trackLength.Value;
            if (double.IsNaN(absoluteValue) || double.IsInfinity(absoluteValue) || absoluteValue < 0)
            {
                cssTrack = string.Empty;
                return false;
            }

            cssTrack = CssUtil.Px(absoluteValue);
            return true;
        }

        cssTrack = string.Empty;
        return false;
    }

    private static (int Row, int Column, int RowSpan, int ColumnSpan) GetGridPlacement(IView childView)
    {
        if (childView is not BindableObject bindableObject)
        {
            return (0, 0, 1, 1);
        }

        var row = Math.Max(0, Grid.GetRow(bindableObject));
        var column = Math.Max(0, Grid.GetColumn(bindableObject));
        var rowSpan = Math.Max(1, Grid.GetRowSpan(bindableObject));
        var columnSpan = Math.Max(1, Grid.GetColumnSpan(bindableObject));
        return (row, column, rowSpan, columnSpan);
    }

    private static string ResolveGridSelfAlignment(Microsoft.Maui.Primitives.LayoutAlignment alignment)
    {
        return alignment switch
        {
            Microsoft.Maui.Primitives.LayoutAlignment.Start => "start",
            Microsoft.Maui.Primitives.LayoutAlignment.Center => "center",
            Microsoft.Maui.Primitives.LayoutAlignment.End => "end",
            _ => "stretch"
        };
    }
}
