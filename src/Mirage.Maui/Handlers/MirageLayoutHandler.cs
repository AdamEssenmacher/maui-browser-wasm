using System.Globalization;
using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Platform;
using MauiLayout = Microsoft.Maui.ILayout;

namespace Mirage.Maui.Handlers;

internal sealed class MirageLayoutHandler : MirageViewHandler<MauiLayout>, ILayoutHandler
{
    public static IPropertyMapper<MauiLayout, MirageLayoutHandler> Mapper =
        new PropertyMapper<MauiLayout, MirageLayoutHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(MauiLayout.Padding)] = MapPadding,
            [nameof(MauiLayout.ClipsToBounds)] = MapClipsToBounds,
            ["IsClippedToBounds"] = MapClipsToBounds,
            [nameof(StackLayout.Orientation)] = MapStackLayoutOrientation,
            [nameof(StackLayout.Spacing)] = MapSpacing,
            [nameof(VerticalStackLayout.Spacing)] = MapSpacing,
            [nameof(HorizontalStackLayout.Spacing)] = MapSpacing,
            [nameof(Grid.RowSpacing)] = MapGridStructure,
            [nameof(Grid.ColumnSpacing)] = MapGridStructure,
            [nameof(Grid.RowDefinitions)] = MapGridStructure,
            [nameof(Grid.ColumnDefinitions)] = MapGridStructure
        };

    public static CommandMapper<MauiLayout, MirageLayoutHandler> CommandMapper = new(MirageViewMapper.CommonViewCommandMapper)
    {
        [nameof(ILayoutHandler.Add)] = MapAdd,
        [nameof(ILayoutHandler.Remove)] = MapRemove,
        [nameof(ILayoutHandler.Clear)] = MapClear,
        [nameof(ILayoutHandler.Insert)] = MapInsert,
        [nameof(ILayoutHandler.Update)] = MapUpdate,
        [nameof(ILayoutHandler.UpdateZIndex)] = MapUpdateZIndex
    };

    private readonly Dictionary<IView, IElementHandler> _childHandlers = new(ReferenceEqualityComparer.Instance);

    public MirageLayoutHandler()
        : base(ComponentKind.VerticalStack, Mapper, CommandMapper)
    {
    }

    MauiLayout ILayoutHandler.VirtualView => VirtualView;

    object ILayoutHandler.PlatformView => PlatformView;

    protected override MiragePlatformView CreatePlatformView()
    {
        return ResolveComponentKind(VirtualView) switch
        {
            ComponentKind.VerticalStack => new MiragePlatformView(ComponentKind.VerticalStack),
            ComponentKind.HorizontalStack => new MiragePlatformView(ComponentKind.HorizontalStack),
            ComponentKind.Grid => new MiragePlatformView(ComponentKind.Grid),
            ComponentKind.AbsoluteLayout => new MiragePlatformView(ComponentKind.AbsoluteLayout),
            ComponentKind.Layout => new MiragePlatformView(ComponentKind.Layout),
            _ => throw new InvalidOperationException("MirageLayoutHandler supports stack, vertical stack, horizontal stack, grid, absolute layout, and custom Layout-derived views.")
        };
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        ApplyBaseStyles();
        RebuildChildren();
        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        DisconnectAllChildren(platformView);
        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        return VirtualView.CrossPlatformMeasure(widthConstraint, heightConstraint);
    }

    public override void PlatformArrange(Rect frame)
    {
        base.PlatformArrange(frame);
        VirtualView.CrossPlatformArrange(new Rect(0, 0, frame.Width, frame.Height));

        if (ResolveComponentKind(VirtualView) == ComponentKind.Layout)
        {
            ApplyCustomLayoutArrangedFrames();
        }
    }

    public void Add(IView view) => RebuildChildren();

    public void Remove(IView view) => RebuildChildren();

    public void Clear() => RebuildChildren();

    public void Insert(int index, IView view) => RebuildChildren();

    public void Update(int index, IView view) => RebuildChildren();

    public void UpdateZIndex(IView view) => RebuildChildren();

    public static void MapPadding(MirageLayoutHandler handler, MauiLayout layout)
    {
        MirageViewMapper.MapPadding(handler, layout);
    }

    public static void MapClipsToBounds(MirageLayoutHandler handler, MauiLayout layout)
    {
        if (!MirageCommonViewProjection.MapLayoutClipsToBounds(handler, layout))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapSpacing(MirageLayoutHandler handler, MauiLayout layout)
    {
        if (!handler.ApplySpacingStyles(layout))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapStackLayoutOrientation(MirageLayoutHandler handler, MauiLayout layout)
    {
        if (layout is not StackLayout)
        {
            return;
        }

        handler.ApplyBaseStyles();
        handler.RebuildChildren();
    }

    public static void MapGridStructure(MirageLayoutHandler handler, MauiLayout layout)
    {
        if (layout is not Grid grid)
        {
            return;
        }

        handler.ApplyGridStyles(grid);
        handler.RebuildChildren();
    }

    public static void MapAdd(MirageLayoutHandler handler, MauiLayout layout, object? arg)
    {
        _ = layout;
        _ = arg;
        handler.RebuildChildren();
    }

    public static void MapRemove(MirageLayoutHandler handler, MauiLayout layout, object? arg)
    {
        _ = layout;
        _ = arg;
        handler.RebuildChildren();
    }

    public static void MapClear(MirageLayoutHandler handler, MauiLayout layout, object? arg)
    {
        _ = layout;
        _ = arg;
        handler.RebuildChildren();
    }

    public static void MapInsert(MirageLayoutHandler handler, MauiLayout layout, object? arg)
    {
        _ = layout;
        _ = arg;
        handler.RebuildChildren();
    }

    public static void MapUpdate(MirageLayoutHandler handler, MauiLayout layout, object? arg)
    {
        _ = layout;
        _ = arg;
        handler.RebuildChildren();
    }

    public static void MapUpdateZIndex(MirageLayoutHandler handler, MauiLayout layout, object? arg)
    {
        _ = layout;
        _ = arg;
        handler.RebuildChildren();
    }

    private void RebuildChildren()
    {
        var kind = ResolveComponentKind(VirtualView);
        var currentChildren = VirtualView.ToArray();
        var currentSet = new HashSet<IView>(currentChildren, ReferenceEqualityComparer.Instance);

        foreach (var existing in _childHandlers.Keys.Where(child => !currentSet.Contains(child)).ToArray())
        {
            DisconnectChild(existing);
        }

        PlatformView.ClearChildren();
        var changed = false;
        foreach (var child in currentChildren)
        {
            if (!TryGetChild(child, out var childHandler, out var childPlatformView))
            {
                throw new InvalidOperationException($"MirageLayoutHandler expected child handler for '{child.GetType().Name}'.");
            }

            _childHandlers[child] = childHandler;
            PlatformView.AddChild(childPlatformView);
            changed |= ApplyChildProjectionStyles(childPlatformView, child, kind);
            changed = true;
        }

        if (changed)
        {
            RequestRender();
        }
    }

    private void DisconnectChild(IView child)
    {
        if (_childHandlers.Remove(child, out var handler))
        {
            MirageHandlerDisconnect.SafeDisconnect(handler);
        }
    }

    private void DisconnectAllChildren(MiragePlatformView platformView)
    {
        foreach (var handler in _childHandlers.Values.ToArray())
        {
            MirageHandlerDisconnect.SafeDisconnect(handler);
        }

        _childHandlers.Clear();
        platformView.ClearChildren();
    }

    private void ApplyBaseStyles()
    {
        var kind = ResolveComponentKind(VirtualView);
        var changed = false;

        switch (kind)
        {
            case ComponentKind.VerticalStack:
                DefaultDisplayStyle = "flex";
                changed |= SetStyleIfChanged("display", "flex");
                changed |= SetStyleIfChanged("flexDirection", "column");
                changed |= SetStyleIfChanged("flexWrap", "nowrap");
                changed |= SetStyleIfChanged("justifyContent", "flex-start");
                changed |= SetStyleIfChanged("alignItems", "stretch");
                changed |= ClearGridContainerStyles();
                break;
            case ComponentKind.HorizontalStack:
                DefaultDisplayStyle = "flex";
                changed |= SetStyleIfChanged("display", "flex");
                changed |= SetStyleIfChanged("flexDirection", "row");
                changed |= SetStyleIfChanged("flexWrap", "nowrap");
                changed |= SetStyleIfChanged("justifyContent", "flex-start");
                changed |= SetStyleIfChanged("alignItems", "stretch");
                changed |= ClearGridContainerStyles();
                break;
            case ComponentKind.Grid:
                DefaultDisplayStyle = "grid";
                changed |= SetStyleIfChanged("display", "grid");
                changed |= SetStyleIfChanged("justifyItems", "stretch");
                changed |= SetStyleIfChanged("alignItems", "stretch");
                changed |= SetStyleIfChanged("justifyContent", "start");
                changed |= SetStyleIfChanged("alignContent", "start");
                changed |= ClearFlexContainerStyles();
                changed |= ApplyGridStyles((Grid)VirtualView);
                break;
            case ComponentKind.AbsoluteLayout:
                DefaultDisplayStyle = "block";
                changed |= SetStyleIfChanged("display", "block");
                changed |= SetStyleIfChanged("position", "relative");
                changed |= ClearFlexContainerStyles();
                changed |= ClearGridContainerStyles();
                changed |= SetStyleIfChanged("gap", null);
                break;
            case ComponentKind.Layout:
                DefaultDisplayStyle = "block";
                changed |= SetStyleIfChanged("display", "block");
                changed |= SetStyleIfChanged("position", "relative");
                changed |= ClearFlexContainerStyles();
                changed |= ClearGridContainerStyles();
                changed |= SetStyleIfChanged("gap", null);
                break;
            default:
                throw new InvalidOperationException($"Unsupported layout kind '{kind}'.");
        }

        changed |= ApplySpacingStyles(VirtualView);
        changed |= MirageCommonViewProjection.MapLayoutClipsToBounds(this, VirtualView);
        if (changed)
        {
            RequestRender();
        }

        MirageViewMapper.MapPadding(this, VirtualView);
    }

    private bool ApplySpacingStyles(MauiLayout layout)
    {
        return layout switch
        {
            StackLayout stack => SetStyleIfChanged("gap", MirageViewMapper.ToPx(NormalizeSpacing(stack.Spacing))),
            VerticalStackLayout vertical => SetStyleIfChanged("gap", MirageViewMapper.ToPx(NormalizeSpacing(vertical.Spacing))),
            HorizontalStackLayout horizontal => SetStyleIfChanged("gap", MirageViewMapper.ToPx(NormalizeSpacing(horizontal.Spacing))),
            Grid grid => ApplyGridGapStyles(grid),
            _ => false
        };
    }

    private bool ApplyGridStyles(Grid grid)
    {
        var changed = false;
        changed |= SetStyleIfChanged("display", "grid");
        changed |= SetStyleIfChanged("justifyItems", "stretch");
        changed |= SetStyleIfChanged("alignItems", "stretch");
        changed |= SetStyleIfChanged("justifyContent", "start");
        changed |= SetStyleIfChanged("alignContent", "start");

        if (!TryBuildGridTrackTemplate(grid.ColumnDefinitions.Select(definition => definition.Width), out var columnsTemplate))
        {
            columnsTemplate = "1fr";
        }

        if (!TryBuildGridTrackTemplate(grid.RowDefinitions.Select(definition => definition.Height), out var rowsTemplate))
        {
            rowsTemplate = "1fr";
        }

        changed |= SetStyleIfChanged("gridTemplateColumns", columnsTemplate);
        changed |= SetStyleIfChanged("gridTemplateRows", rowsTemplate);
        changed |= ApplyGridGapStyles(grid);
        return changed;
    }

    private bool ApplyGridGapStyles(Grid grid)
    {
        var rowSpacing = NormalizeSpacing(grid.RowSpacing);
        var columnSpacing = NormalizeSpacing(grid.ColumnSpacing);
        var rowCss = MirageViewMapper.ToPx(rowSpacing);
        var columnCss = MirageViewMapper.ToPx(columnSpacing);

        var changed = SetStyleIfChanged("rowGap", rowCss);
        changed |= SetStyleIfChanged("columnGap", columnCss);
        changed |= SetStyleIfChanged("gap", $"{rowCss} {columnCss}");
        return changed;
    }

    private bool ClearFlexContainerStyles()
    {
        var changed = SetStyleIfChanged("flexDirection", null);
        changed |= SetStyleIfChanged("flexWrap", null);
        changed |= SetStyleIfChanged("justifyContent", null);
        return changed;
    }

    private bool ClearGridContainerStyles()
    {
        var changed = SetStyleIfChanged("justifyItems", null);
        changed |= SetStyleIfChanged("gridTemplateColumns", null);
        changed |= SetStyleIfChanged("gridTemplateRows", null);
        changed |= SetStyleIfChanged("rowGap", null);
        changed |= SetStyleIfChanged("columnGap", null);
        return changed;
    }

    private static double NormalizeSpacing(double spacing)
    {
        if (!double.IsFinite(spacing))
        {
            return 0;
        }

        return Math.Max(0, spacing);
    }

    private static bool ApplyChildProjectionStyles(
        MiragePlatformView childPlatformView,
        IView childView,
        ComponentKind parentKind)
    {
        var horizontalAlignment = MirageCommonViewProjection.ResolveHorizontalAlignment(childView);
        var verticalAlignment = MirageCommonViewProjection.ResolveVerticalAlignment(childView);

        var changed = false;
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
                changed |= SetChildStyleIfChanged(childPlatformView, "gridRowStart", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "gridRowEnd", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "gridColumnStart", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "gridColumnEnd", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "justifySelf", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "alignSelf", null);
                changed |= ApplyAbsoluteLayoutChildStyles(childPlatformView, childView);
                break;
            case ComponentKind.Layout:
                changed |= SetChildStyleIfChanged(childPlatformView, "flexGrow", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "flexShrink", null);
                changed |= SetChildStyleIfChanged(childPlatformView, "flexBasis", null);
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
                changed |= MirageCommonViewProjection.SetLayoutTransformFragment(childPlatformView, null);
                break;
        }

        return changed;
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

    private static bool ApplyAbsoluteLayoutChildStyles(MiragePlatformView childPlatformView, IView childView)
    {
        var changed = SetChildStyleIfChanged(childPlatformView, "position", "absolute");

        if (childView is not BindableObject bindable)
        {
            changed |= SetChildStyleIfChanged(childPlatformView, "left", "0px");
            changed |= SetChildStyleIfChanged(childPlatformView, "top", "0px");
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

        changed |= SetChildStyleIfChanged(childPlatformView, "width", ResolveAbsoluteLayoutSize(childView, bounds.Width, widthProportional, isWidth: true));
        changed |= SetChildStyleIfChanged(childPlatformView, "height", ResolveAbsoluteLayoutSize(childView, bounds.Height, heightProportional, isWidth: false));

        changed |= MirageCommonViewProjection.SetLayoutTransformFragment(
            childPlatformView,
            ResolveAbsoluteLayoutTranslate(xProportional ? x : null, yProportional ? y : null));
        return changed;
    }

    private static bool ApplyCustomLayoutChildStyles(MiragePlatformView childPlatformView, IView childView)
    {
        var frame = childView.Frame;
        var parentPadding = ResolveCustomLayoutParentPadding(childView);
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

    private static Thickness ResolveCustomLayoutParentPadding(IView childView)
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
            return ResolveAbsoluteLayoutAutoSizeRequestedLength(childView, isWidth);
        }

        var normalized = NormalizeFinite(value, 0);
        if (proportional)
        {
            return ToPercent(Math.Max(0, normalized));
        }

        return MirageViewMapper.ToPx(Math.Max(0, normalized));
    }

    private static string? ResolveAbsoluteLayoutAutoSizeRequestedLength(IView childView, bool isWidth)
    {
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

    private static double NormalizeFinite(double value, double fallback)
    {
        if (!double.IsFinite(value))
        {
            return fallback;
        }

        return value;
    }

    private static bool TryBuildGridTrackTemplate(IEnumerable<GridLength> trackLengths, out string template)
    {
        var parts = new List<string>();
        foreach (var trackLength in trackLengths)
        {
            if (!TryConvertGridTrack(trackLength, out var trackPart))
            {
                template = "1fr";
                return false;
            }

            parts.Add(trackPart);
        }

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
            if (!double.IsFinite(starValue) || starValue <= 0)
            {
                starValue = 1;
            }

            cssTrack = $"{starValue.ToString("0.###", CultureInfo.InvariantCulture)}fr";
            return true;
        }

        if (trackLength.IsAbsolute)
        {
            var absoluteValue = trackLength.Value;
            if (!double.IsFinite(absoluteValue) || absoluteValue < 0)
            {
                cssTrack = string.Empty;
                return false;
            }

            cssTrack = MirageViewMapper.ToPx(absoluteValue);
            return true;
        }

        cssTrack = string.Empty;
        return false;
    }

    private static ComponentKind ResolveComponentKind(MauiLayout layout)
    {
        return layout switch
        {
            StackLayout stack => stack.Orientation == StackOrientation.Horizontal
                ? ComponentKind.HorizontalStack
                : ComponentKind.VerticalStack,
            VerticalStackLayout => ComponentKind.VerticalStack,
            HorizontalStackLayout => ComponentKind.HorizontalStack,
            Grid => ComponentKind.Grid,
            AbsoluteLayout => ComponentKind.AbsoluteLayout,
            _ => ComponentKind.Layout
        };
    }

    private void ApplyCustomLayoutArrangedFrames()
    {
        var changed = false;
        foreach (var child in VirtualView)
        {
            if (!_childHandlers.TryGetValue(child, out var childHandler)
                || childHandler.PlatformView is not MiragePlatformView childPlatformView)
            {
                continue;
            }

            changed |= ApplyCustomLayoutChildStyles(childPlatformView, child);
        }

        if (changed)
        {
            RequestRender();
        }
    }

    private bool SetStyleIfChanged(string styleName, string? value)
    {
        if (string.Equals(PlatformView.GetStyleValue(styleName), value, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetStyle(styleName, value);
        return true;
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
