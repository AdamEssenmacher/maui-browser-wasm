using System.ComponentModel;
using System.Globalization;
using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using MauiLayout = Microsoft.Maui.ILayout;

namespace Mirage.Maui.Handlers;

internal sealed class MirageLayoutHandler : MirageViewHandler<MauiLayout>, ILayoutHandler
{
    internal static readonly string[] ParityPropertyKeys =
    {
        nameof(MauiLayout.Padding),
        nameof(MauiLayout.ClipsToBounds),
        "IsClippedToBounds",
        nameof(IView.InputTransparent),
        nameof(StackLayout.Orientation),
        nameof(StackLayout.Spacing),
        nameof(Grid.RowSpacing),
        nameof(Grid.ColumnSpacing),
        nameof(Grid.RowDefinitions),
        nameof(Grid.ColumnDefinitions)
    };

    internal static readonly string[] ParityExtensionPropertyKeys =
    {
        "IsClippedToBounds",
        nameof(IView.InputTransparent),
        nameof(StackLayout.Orientation),
        nameof(StackLayout.Spacing),
        nameof(Grid.RowSpacing),
        nameof(Grid.ColumnSpacing),
        nameof(Grid.RowDefinitions),
        nameof(Grid.ColumnDefinitions)
    };

    internal static readonly string[] ParityCommandKeys =
    {
        nameof(ILayoutHandler.Add),
        nameof(ILayoutHandler.Remove),
        nameof(ILayoutHandler.Clear),
        nameof(ILayoutHandler.Insert),
        nameof(ILayoutHandler.Update),
        nameof(ILayoutHandler.UpdateZIndex)
    };

    public static IPropertyMapper<MauiLayout, MirageLayoutHandler> Mapper =
        new PropertyMapper<MauiLayout, MirageLayoutHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(MauiLayout.Padding)] = MapPadding,
            [nameof(MauiLayout.ClipsToBounds)] = MapClipsToBounds,
            ["IsClippedToBounds"] = MapClipsToBounds,
            [nameof(IView.InputTransparent)] = MapInputTransparent,
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
    private readonly Dictionary<IView, PropertyChangedEventHandler> _childPropertyChangedHandlers = new(ReferenceEqualityComparer.Instance);

    public MirageLayoutHandler()
        : base(ComponentKind.VerticalStack, Mapper, CommandMapper)
    {
    }

    MauiLayout ILayoutHandler.VirtualView => VirtualView;

    object ILayoutHandler.PlatformView => PlatformView;

    public override void SetVirtualView(IView view)
    {
        base.SetVirtualView(view);

        if (!TryGetPlatformView(out _))
        {
            return;
        }

        ApplyBaseStyles();
        SyncChildrenFromVirtualView();
    }

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

        switch (ResolveComponentKind(VirtualView))
        {
            case ComponentKind.Layout:
                ApplyCustomLayoutArrangedFrames();
                break;
            case ComponentKind.AbsoluteLayout:
                ApplyAbsoluteLayoutArrangedFrames();
                break;
        }
    }

    public void Add(IView view) => AddOrInsertChild(view, fallbackIndex: null);

    public void Remove(IView view) => RemoveChild(view);

    public void Clear() => ClearChildren();

    public void Insert(int index, IView view) => AddOrInsertChild(view, index);

    public void Update(int index, IView view) => UpdateChild(index, view);

    public void UpdateZIndex(IView view) => UpdateChildZIndex(view);

    public static void MapPadding(MirageLayoutHandler handler, MauiLayout layout)
    {
        _ = layout;
        MirageViewMapper.MapPadding(handler, handler.VirtualView);
    }

    public static void MapClipsToBounds(MirageLayoutHandler handler, MauiLayout layout)
    {
        if (!MirageCommonViewProjection.MapLayoutClipsToBounds(handler, layout))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapInputTransparent(MirageLayoutHandler handler, MauiLayout layout)
    {
        if (!handler.ApplyInputTransparent(layout))
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
        handler.RefreshAllChildProjectionStyles(updateOrder: true);
    }

    public static void MapGridStructure(MirageLayoutHandler handler, MauiLayout layout)
    {
        if (layout is not Grid grid)
        {
            return;
        }

        var changed = handler.ApplyGridStyles(grid);
        changed |= handler.RefreshAllChildProjectionStyles(updateOrder: false);
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapAdd(MirageLayoutHandler handler, MauiLayout layout, object? arg)
    {
        _ = layout;

        if (!TryResolveViewAndIndex(arg, out var view, out _))
        {
            handler.SyncChildrenFromVirtualView();
            return;
        }

        handler.Add(view);
    }

    public static void MapRemove(MirageLayoutHandler handler, MauiLayout layout, object? arg)
    {
        _ = layout;

        if (!TryResolveViewAndIndex(arg, out var view, out _))
        {
            handler.SyncChildrenFromVirtualView();
            return;
        }

        handler.Remove(view);
    }

    public static void MapClear(MirageLayoutHandler handler, MauiLayout layout, object? arg)
    {
        _ = layout;
        _ = arg;
        handler.Clear();
    }

    public static void MapInsert(MirageLayoutHandler handler, MauiLayout layout, object? arg)
    {
        _ = layout;

        if (!TryResolveViewAndIndex(arg, out var view, out var index))
        {
            handler.SyncChildrenFromVirtualView();
            return;
        }

        handler.Insert(index, view);
    }

    public static void MapUpdate(MirageLayoutHandler handler, MauiLayout layout, object? arg)
    {
        _ = layout;

        if (!TryResolveViewAndIndex(arg, out var view, out var index))
        {
            handler.SyncChildrenFromVirtualView();
            return;
        }

        handler.Update(index, view);
    }

    public static void MapUpdateZIndex(MirageLayoutHandler handler, MauiLayout layout, object? arg)
    {
        _ = layout;

        if (!TryResolveViewAndIndex(arg, out var view, out _))
        {
            handler.SyncChildrenFromVirtualView();
            return;
        }

        handler.UpdateZIndex(view);
    }

    private static bool TryResolveViewAndIndex(object? arg, out IView view, out int index)
    {
        if (arg is LayoutHandlerUpdate update)
        {
            view = update.View;
            index = update.Index;
            return true;
        }

        if (arg is IView inputView)
        {
            view = inputView;
            index = -1;
            return true;
        }

        view = null!;
        index = -1;
        return false;
    }

    private void SyncChildrenFromVirtualView()
    {
        if (!TryGetPlatformView(out var platformView))
        {
            return;
        }

        var orderedChildren = MirageLayoutHandlerOrdering.OrderByZIndex(VirtualView);
        var desiredChildren = new HashSet<IView>(orderedChildren, ReferenceEqualityComparer.Instance);
        foreach (var existing in _childHandlers.Keys.Where(child => !desiredChildren.Contains(child)).ToArray())
        {
            DisconnectChild(existing, removeFromPlatform: false);
        }

        platformView.ClearChildren();

        var kind = ResolveComponentKind(VirtualView);
        foreach (var child in orderedChildren)
        {
            if (!TryGetOrCreateChild(child, out var childHandler, out var childPlatformView))
            {
                throw new InvalidOperationException($"MirageLayoutHandler expected child handler for '{child.GetType().Name}'.");
            }

            _childHandlers[child] = childHandler;
            EnsureChildPropertyTracking(child);
            platformView.AddChild(childPlatformView);
            _ = ApplyChildProjectionStyles(childPlatformView, child, kind);
        }

        RequestRender();
    }

    private void AddOrInsertChild(IView view, int? fallbackIndex)
    {
        if (!TryGetOrCreateChild(view, out var childHandler, out var childPlatformView))
        {
            SyncChildrenFromVirtualView();
            return;
        }

        _childHandlers[view] = childHandler;
        EnsureChildPropertyTracking(view);
        var changed = false;
        changed |= InsertOrMoveChild(view, childPlatformView, fallbackIndex);
        changed |= ApplyChildProjectionStyles(childPlatformView, view, ResolveComponentKind(VirtualView));
        if (!changed)
        {
            return;
        }

        RequestRender();
    }

    private void RemoveChild(IView view)
    {
        if (!DisconnectChild(view, removeFromPlatform: true))
        {
            return;
        }

        RequestRender();
    }

    private void ClearChildren()
    {
        if (!TryGetPlatformView(out var platformView))
        {
            return;
        }

        if (_childHandlers.Count == 0 && platformView.Children.Count == 0)
        {
            return;
        }

        DisconnectAllChildren(platformView);
        RequestRender();
    }

    private void UpdateChild(int fallbackIndex, IView view)
    {
        if (!_childHandlers.TryGetValue(view, out var childHandler)
            || childHandler.PlatformView is not MiragePlatformView childPlatformView)
        {
            AddOrInsertChild(view, fallbackIndex);
            return;
        }

        var changed = false;
        changed |= InsertOrMoveChild(view, childPlatformView, fallbackIndex);
        changed |= ApplyChildProjectionStyles(childPlatformView, view, ResolveComponentKind(VirtualView));
        if (!changed)
        {
            return;
        }

        RequestRender();
    }

    private void UpdateChildZIndex(IView view)
    {
        if (!_childHandlers.TryGetValue(view, out var childHandler)
            || childHandler.PlatformView is not MiragePlatformView childPlatformView)
        {
            AddOrInsertChild(view, fallbackIndex: null);
            return;
        }

        var changed = false;
        changed |= InsertOrMoveChild(view, childPlatformView, fallbackIndex: null);
        changed |= ApplyChildProjectionStyles(childPlatformView, view, ResolveComponentKind(VirtualView));
        if (!changed)
        {
            return;
        }

        RequestRender();
    }

    private bool RefreshAllChildProjectionStyles(bool updateOrder)
    {
        var kind = ResolveComponentKind(VirtualView);
        var changed = false;

        var orderedChildren = updateOrder
            ? MirageLayoutHandlerOrdering.OrderByZIndex(VirtualView)
            : VirtualView.ToArray();

        foreach (var child in orderedChildren)
        {
            if (!_childHandlers.TryGetValue(child, out var childHandler)
                || childHandler.PlatformView is not MiragePlatformView childPlatformView)
            {
                SyncChildrenFromVirtualView();
                return true;
            }

            changed |= ApplyChildProjectionStyles(childPlatformView, child, kind);
            if (updateOrder)
            {
                changed |= InsertOrMoveChild(child, childPlatformView, fallbackIndex: null);
            }
        }

        return changed;
    }

    private bool InsertOrMoveChild(IView view, MiragePlatformView childPlatformView, int? fallbackIndex)
    {
        var targetIndex = ResolveChildIndex(view, fallbackIndex);
        return InsertOrMoveChild(targetIndex, childPlatformView);
    }

    private bool InsertOrMoveChild(int targetIndex, MiragePlatformView childPlatformView)
    {
        var normalizedTargetIndex = Math.Clamp(targetIndex, 0, PlatformView.Children.Count);
        var currentIndex = GetCurrentChildIndex(childPlatformView);
        if (currentIndex == normalizedTargetIndex)
        {
            return false;
        }

        if (currentIndex >= 0)
        {
            PlatformView.RemoveChild(childPlatformView);
        }

        PlatformView.InsertChild(normalizedTargetIndex, childPlatformView);
        return true;
    }

    private int ResolveChildIndex(IView view, int? fallbackIndex)
    {
        if (ContainsVirtualChild(view))
        {
            return MirageLayoutHandlerOrdering.GetLayoutHandlerIndex(VirtualView, view);
        }

        return Math.Clamp(fallbackIndex ?? PlatformView.Children.Count, 0, PlatformView.Children.Count);
    }

    private bool ContainsVirtualChild(IView view)
    {
        return VirtualView.Any(child => ReferenceEquals(child, view));
    }

    private int GetCurrentChildIndex(MiragePlatformView childPlatformView)
    {
        for (var i = 0; i < PlatformView.Children.Count; i++)
        {
            if (ReferenceEquals(PlatformView.Children[i], childPlatformView))
            {
                return i;
            }
        }

        return -1;
    }

    private bool TryGetOrCreateChild(IView child, out IElementHandler childHandler, out MiragePlatformView childPlatformView)
    {
        if (_childHandlers.TryGetValue(child, out var existingHandler)
            && existingHandler.PlatformView is MiragePlatformView existingPlatformView)
        {
            childHandler = existingHandler;
            childPlatformView = existingPlatformView;
            return true;
        }

        if (_childHandlers.Remove(child, out var staleHandler))
        {
            MirageHandlerDisconnect.SafeDisconnect(staleHandler);
        }

        return TryGetChild(child, out childHandler, out childPlatformView);
    }

    private bool DisconnectChild(IView child, bool removeFromPlatform)
    {
        if (!_childHandlers.Remove(child, out var handler))
        {
            return false;
        }

        if (removeFromPlatform && handler.PlatformView is MiragePlatformView childPlatformView)
        {
            PlatformView.RemoveChild(childPlatformView);
        }

        RemoveChildPropertyTracking(child);
        MirageHandlerDisconnect.SafeDisconnect(handler);
        return true;
    }

    private void DisconnectAllChildren(MiragePlatformView platformView)
    {
        foreach (var handler in _childHandlers.Values.ToArray())
        {
            MirageHandlerDisconnect.SafeDisconnect(handler);
        }

        foreach (var child in _childPropertyChangedHandlers.Keys.ToArray())
        {
            RemoveChildPropertyTracking(child);
        }

        _childHandlers.Clear();
        platformView.ClearChildren();
    }

    private void EnsureChildPropertyTracking(IView child)
    {
        if (child is not BindableObject bindable || _childPropertyChangedHandlers.ContainsKey(child))
        {
            return;
        }

        PropertyChangedEventHandler handler = (_, args) => OnChildPropertyChanged(child, args.PropertyName);
        bindable.PropertyChanged += handler;
        _childPropertyChangedHandlers[child] = handler;
    }

    private void RemoveChildPropertyTracking(IView child)
    {
        if (child is not BindableObject bindable || !_childPropertyChangedHandlers.Remove(child, out var handler))
        {
            return;
        }

        bindable.PropertyChanged -= handler;
    }

    private void OnChildPropertyChanged(IView child, string? propertyName)
    {
        if (!ContainsVirtualChild(child))
        {
            return;
        }

        if (IsZIndexProperty(propertyName))
        {
            UpdateChildZIndex(child);
            return;
        }

        var kind = ResolveComponentKind(VirtualView);
        if (kind == ComponentKind.AbsoluteLayout && IsAbsoluteLayoutProjectionProperty(propertyName))
        {
            UpdateChild(fallbackIndex: -1, child);
            return;
        }

        if (kind == ComponentKind.Grid && IsGridPlacementProperty(propertyName))
        {
            UpdateChild(fallbackIndex: -1, child);
        }
    }

    private static bool IsZIndexProperty(string? propertyName)
    {
        return string.Equals(propertyName, nameof(VisualElement.ZIndex), StringComparison.Ordinal);
    }

    private static bool IsAbsoluteLayoutProjectionProperty(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return false;
        }

        return string.Equals(propertyName, AbsoluteLayout.LayoutBoundsProperty.PropertyName, StringComparison.Ordinal)
               || string.Equals(propertyName, AbsoluteLayout.LayoutFlagsProperty.PropertyName, StringComparison.Ordinal);
    }

    private static bool IsGridPlacementProperty(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return false;
        }

        return string.Equals(propertyName, Grid.RowProperty.PropertyName, StringComparison.Ordinal)
               || string.Equals(propertyName, Grid.ColumnProperty.PropertyName, StringComparison.Ordinal)
               || string.Equals(propertyName, Grid.RowSpanProperty.PropertyName, StringComparison.Ordinal)
               || string.Equals(propertyName, Grid.ColumnSpanProperty.PropertyName, StringComparison.Ordinal);
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
        changed |= ApplyInputTransparent(VirtualView);
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

    private bool ApplyInputTransparent(MauiLayout layout)
    {
        return SetStyleIfChanged("pointerEvents", layout.InputTransparent ? "none" : null);
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
        return MirageLayoutProjection.ApplyChildProjectionStyles(childPlatformView, childView, parentKind);
    }

    private static bool ApplyAbsoluteLayoutChildStyles(MiragePlatformView childPlatformView, IView childView)
    {
        return MirageLayoutProjection.ApplyAbsoluteLayoutChildStyles(childPlatformView, childView);
    }

    private static bool ApplyCustomLayoutChildStyles(MiragePlatformView childPlatformView, IView childView)
    {
        return MirageLayoutProjection.ApplyCustomLayoutChildStyles(childPlatformView, childView);
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

    private void ApplyAbsoluteLayoutArrangedFrames()
    {
        var changed = false;
        foreach (var child in VirtualView)
        {
            if (!_childHandlers.TryGetValue(child, out var childHandler)
                || childHandler.PlatformView is not MiragePlatformView childPlatformView)
            {
                continue;
            }

            changed |= MirageLayoutProjection.ApplyAbsoluteLayoutArrangedChildStyles(childPlatformView, child);
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
}
