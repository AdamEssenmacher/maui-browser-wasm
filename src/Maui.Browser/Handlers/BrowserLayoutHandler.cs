using System.Globalization;
using Maui.Browser.Dom;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using MauiLayout = Microsoft.Maui.ILayout;

namespace Maui.Browser.Handlers;

public sealed class BrowserLayoutHandler : BrowserViewHandler<MauiLayout>, ILayoutHandler
{
    public static IPropertyMapper<MauiLayout, BrowserLayoutHandler> Mapper = new PropertyMapper<MauiLayout, BrowserLayoutHandler>(ViewHandler.ViewMapper)
    {
        [nameof(MauiLayout.ClipsToBounds)] = MapClipsToBounds,
        [nameof(MauiLayout.Padding)] = MapPadding,
        [nameof(IView.Visibility)] = MapVisibility,
        [nameof(IView.Opacity)] = MapOpacity
    };

    public static CommandMapper<MauiLayout, BrowserLayoutHandler> CommandMapper = new(ViewHandler.ViewCommandMapper)
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
    }

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Style("overflow", "visible");
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
    }

    public void Remove(IView view)
    {
        DisconnectChild(view, removeFromDom: true);
    }

    public void Clear()
    {
        DisconnectAllChildren();
        PlatformView.ClearChildren();
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

    public static void MapPadding(BrowserLayoutHandler handler, MauiLayout layout)
    {
        BrowserViewMapper.MapPadding(layout, handler.PlatformView);
    }

    public static void MapVisibility(BrowserLayoutHandler handler, MauiLayout layout)
    {
        BrowserViewMapper.MapVisibility(layout, handler.PlatformView);
    }

    public static void MapOpacity(BrowserLayoutHandler handler, MauiLayout layout)
    {
        BrowserViewMapper.MapOpacity(layout, handler.PlatformView);
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
        }
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
}
