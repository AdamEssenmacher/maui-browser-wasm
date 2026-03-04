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

    public void Add(IView view)
    {
        if (!TryGetChildPlatformView(view, out var childPlatformView))
        {
            return;
        }

        PlatformView.Append(childPlatformView);
        UpdateZIndex(view);
    }

    public void Remove(IView view)
    {
        if (!TryGetChildPlatformView(view, out var childPlatformView))
        {
            return;
        }

        PlatformView.Remove(childPlatformView);
    }

    public void Clear()
    {
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
        if (!TryGetChildPlatformView(view, out var childPlatformView))
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
        PlatformView.ClearChildren();

        foreach (var child in VirtualView)
        {
            if (!TryGetChildPlatformView(child, out var childPlatformView))
            {
                continue;
            }

            PlatformView.Append(childPlatformView);
            UpdateZIndex(child);
        }
    }

    private bool TryGetChildPlatformView(IView view, out DomElement childPlatformView)
    {
        if (MauiContext is null)
        {
            childPlatformView = null!;
            return false;
        }

        var childHandler = view.ToHandler(MauiContext);
        if (childHandler.PlatformView is DomElement domElement)
        {
            childPlatformView = domElement;
            return true;
        }

        childPlatformView = null!;
        return false;
    }
}
