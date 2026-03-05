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
        FlexRow
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
        ApplyChildProjectionStyles(childPlatformView, _projectionMode);
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
        // Child frames are already arranged with MAUI padding applied, so adding CSS padding
        // here would double-inset absolutely-positioned children.
        handler.PlatformView.Style("padding", string.Empty);
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
            ApplyChildProjectionStyles(childPlatformView, _projectionMode);
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

        foreach (var handler in _childHandlers.Values)
        {
            if (handler.PlatformView is not DomElement childPlatformView)
            {
                continue;
            }

            ApplyChildProjectionStyles(childPlatformView, projectionMode);
        }
    }

    internal static BrowserLayoutProjectionMode ResolveProjectionMode(MauiLayout layout)
    {
        return layout switch
        {
            VerticalStackLayout => BrowserLayoutProjectionMode.FlexColumn,
            HorizontalStackLayout => BrowserLayoutProjectionMode.FlexRow,
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
            return;
        }

        var spacing = GetLayoutSpacing(layout);
        platformView.Style("display", "flex");
        platformView.Style("flexDirection", projectionMode == BrowserLayoutProjectionMode.FlexRow ? "row" : "column");
        platformView.Style("alignItems", "stretch");
        platformView.Style("justifyContent", "flex-start");
        platformView.Style("gap", CssUtil.Px(spacing));
    }

    internal static void ApplyChildProjectionStyles(DomElement childPlatformView, BrowserLayoutProjectionMode projectionMode)
    {
        if (projectionMode == BrowserLayoutProjectionMode.Absolute)
        {
            return;
        }

        childPlatformView.Style("position", "relative");
        childPlatformView.Style("left", null);
        childPlatformView.Style("top", null);
        childPlatformView.Style("right", null);
        childPlatformView.Style("bottom", null);
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
}
