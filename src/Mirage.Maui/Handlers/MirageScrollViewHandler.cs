using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Mirage.Maui.Handlers;

internal sealed class MirageScrollViewHandler : MirageViewHandler<IScrollView>, IScrollViewHandler, IMirageRuntimeEventTarget
{
    public static IPropertyMapper<IScrollView, MirageScrollViewHandler> Mapper =
        new PropertyMapper<IScrollView, MirageScrollViewHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(IContentView.Content)] = MapContent,
            [nameof(IScrollView.Orientation)] = MapOrientation,
            [nameof(IScrollView.HorizontalScrollBarVisibility)] = MapHorizontalScrollBarVisibility,
            [nameof(IScrollView.VerticalScrollBarVisibility)] = MapVerticalScrollBarVisibility,
            [nameof(IScrollView.ContentSize)] = MapContentSize
        };

    public static CommandMapper<IScrollView, MirageScrollViewHandler> CommandMapper =
        new(MirageViewMapper.CommonViewCommandMapper)
        {
            [nameof(IScrollView.RequestScrollTo)] = MapRequestScrollTo
        };

    private IView? _currentContent;
    private IElementHandler? _currentContentHandler;

    public MirageScrollViewHandler()
        : base(ComponentKind.ContentContainer, Mapper, CommandMapper)
    {
        DefaultDisplayStyle = "block";
    }

    IScrollView IScrollViewHandler.VirtualView => VirtualView;

    object IScrollViewHandler.PlatformView => PlatformView;

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        base.ConnectHandler(platformView);
        PlatformView.SetStyle("display", "block");
        PlatformView.SetStyle("boxSizing", "border-box");
        PlatformView.SetEvent("scroll", enabled: true);
        SetContent(ResolvePresentedContent(VirtualView));
        ApplyScrollStyles(VirtualView);
        RequestRender();
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        PlatformView.SetEvent("scroll", enabled: false);
        DisconnectCurrentContent();
        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        var content = ResolvePresentedContent(VirtualView);
        if (content is null)
        {
            return base.GetDesiredSize(widthConstraint, heightConstraint);
        }

        var contentSize = content.Measure(widthConstraint, heightConstraint);
        return new Size(
            ResolveDesiredDimension(widthConstraint, contentSize.Width),
            ResolveDesiredDimension(heightConstraint, contentSize.Height));
    }

    public override void PlatformArrange(Rect frame)
    {
        base.PlatformArrange(frame);

        var width = Math.Max(0, frame.Width);
        var height = Math.Max(0, frame.Height);
        if (ResolvePresentedContent(VirtualView) is not IView content
            || _currentContentHandler?.PlatformView is not MiragePlatformView contentPlatformView)
        {
            return;
        }

        var orientation = VirtualView.Orientation;
        var horizontalConstraint = orientation is ScrollOrientation.Horizontal or ScrollOrientation.Both
            ? double.PositiveInfinity
            : width;
        var verticalConstraint = orientation is ScrollOrientation.Vertical or ScrollOrientation.Both
            ? double.PositiveInfinity
            : height;

        var desiredSize = content.Measure(horizontalConstraint, verticalConstraint);
        var arrangedWidth = orientation is ScrollOrientation.Horizontal or ScrollOrientation.Both
            ? Math.Max(width, desiredSize.Width)
            : width;
        var arrangedHeight = orientation is ScrollOrientation.Vertical or ScrollOrientation.Both
            ? Math.Max(height, desiredSize.Height)
            : height;

        content.Arrange(new Rect(0, 0, arrangedWidth, arrangedHeight));
        NormalizeContentPosition(contentPlatformView, arrangedWidth, arrangedHeight);
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        _ = payload;
        if (!string.Equals(eventName, "scroll", StringComparison.Ordinal))
        {
            return;
        }

        if (Coordinator is null)
        {
            return;
        }

        if (!Coordinator.TryGetNodeScrollPosition(PlatformView.NodeId, out var left, out var top))
        {
            return;
        }

        if (VirtualView is ScrollView controlsScrollView)
        {
            controlsScrollView.SetScrolledPosition(left, top);
        }
    }

    public static void MapContent(MirageScrollViewHandler handler, IScrollView scrollView)
    {
        handler.SetContent(ResolvePresentedContent(scrollView));
    }

    public static void MapHorizontalScrollBarVisibility(MirageScrollViewHandler handler, IScrollView scrollView)
    {
        handler.ApplyScrollStyles(scrollView);
    }

    public static void MapVerticalScrollBarVisibility(MirageScrollViewHandler handler, IScrollView scrollView)
    {
        handler.ApplyScrollStyles(scrollView);
    }

    public static void MapOrientation(MirageScrollViewHandler handler, IScrollView scrollView)
    {
        handler.ApplyScrollStyles(scrollView);
    }

    public static void MapContentSize(MirageScrollViewHandler handler, IScrollView scrollView)
    {
        _ = handler;
        _ = scrollView;
    }

    public static void MapRequestScrollTo(MirageScrollViewHandler handler, IScrollView scrollView, object? args)
    {
        if (!TryResolveScrollRequest(scrollView, args, out var horizontalOffset, out var verticalOffset))
        {
            return;
        }

        horizontalOffset = NormalizeOffset(horizontalOffset);
        verticalOffset = NormalizeOffset(verticalOffset);
        var actualHorizontalOffset = horizontalOffset;
        var actualVerticalOffset = verticalOffset;

        if (handler.Coordinator is not null
            && handler.Coordinator.TrySetNodeScrollPosition(
                handler.PlatformView.NodeId,
                horizontalOffset,
                verticalOffset,
                out var realizedLeft,
                out var realizedTop))
        {
            actualHorizontalOffset = realizedLeft;
            actualVerticalOffset = realizedTop;
        }

        if (scrollView is ScrollView controlsScrollView)
        {
            controlsScrollView.SetScrolledPosition(actualHorizontalOffset, actualVerticalOffset);
            controlsScrollView.SendScrollFinished();
        }
    }

    private static bool TryResolveScrollRequest(
        IScrollView scrollView,
        object? args,
        out double horizontalOffset,
        out double verticalOffset)
    {
        switch (args)
        {
            case ScrollToRequest request:
                horizontalOffset = request.HorizontalOffset;
                verticalOffset = request.VerticalOffset;
                return true;
            case ScrollToRequestedEventArgs requestedEventArgs:
                if (requestedEventArgs.Mode == ScrollToMode.Element
                    && requestedEventArgs.Element is Element targetElement)
                {
                    if (TryComputeTargetOffsets(scrollView, targetElement, out horizontalOffset, out verticalOffset))
                    {
                        return true;
                    }
                }

                horizontalOffset = requestedEventArgs.ScrollX;
                verticalOffset = requestedEventArgs.ScrollY;
                return true;
            default:
                horizontalOffset = 0;
                verticalOffset = 0;
                return false;
        }
    }

    private static bool TryComputeTargetOffsets(IScrollView scrollView, Element targetElement, out double horizontalOffset, out double verticalOffset)
    {
        horizontalOffset = 0;
        verticalOffset = 0;

        if (ResolvePresentedContent(scrollView) is not Element contentRoot)
        {
            return false;
        }

        var offsetX = 0d;
        var offsetY = 0d;
        Element? current = targetElement;
        while (current is not null && !ReferenceEquals(current, contentRoot))
        {
            if (current is VisualElement visualElement)
            {
                if (!double.IsFinite(visualElement.X) || !double.IsFinite(visualElement.Y))
                {
                    return false;
                }

                offsetX += visualElement.X;
                offsetY += visualElement.Y;
            }

            current = current.Parent;
        }

        if (!ReferenceEquals(current, contentRoot) || !double.IsFinite(offsetX) || !double.IsFinite(offsetY))
        {
            return false;
        }

        // In current browser-hosted paths, MAUI may surface element-mode requests before meaningful
        // arranged offsets are reflected in target X/Y, yielding a false "0,0" location for non-root
        // section targets. Return false so caller can avoid applying this offset.
        if (!ReferenceEquals(targetElement, contentRoot)
            && Math.Abs(offsetX) < 0.5
            && Math.Abs(offsetY) < 0.5)
        {
            return false;
        }

        horizontalOffset = Math.Max(0, offsetX);
        verticalOffset = Math.Max(0, offsetY);
        return true;
    }

    private static double NormalizeOffset(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        return Math.Max(0, value);
    }

    private static double ResolveDesiredDimension(double constraint, double contentDimension)
    {
        if (double.IsNaN(constraint) || double.IsInfinity(constraint))
        {
            return Math.Max(0, contentDimension);
        }

        return Math.Max(0, Math.Min(constraint, contentDimension));
    }

    private static IView? ResolvePresentedContent(IScrollView scrollView)
    {
        return scrollView is IContentView contentView ? contentView.PresentedContent : null;
    }

    private void SetContent(IView? content)
    {
        if (_currentContentHandler is not null && !ReferenceEquals(_currentContent, content))
        {
            DisconnectCurrentContent();
        }

        PlatformView.ClearChildren();

        if (content is null || MauiContext is null)
        {
            RequestRender();
            return;
        }

        var contentHandler = content.ToHandler(MauiContext);
        if (contentHandler.PlatformView is not MiragePlatformView contentPlatformView)
        {
            throw new InvalidOperationException("MirageScrollViewHandler requires child handlers to expose MiragePlatformView.");
        }

        PlatformView.AddChild(contentPlatformView);
        if (contentHandler is IViewHandler viewHandler && content is IView childView)
        {
            MirageViewMapper.MapLayoutAlignment(viewHandler, childView);
        }

        _currentContent = content;
        _currentContentHandler = contentHandler;
        RequestRender();
    }

    private void DisconnectCurrentContent()
    {
        if (_currentContentHandler is not null)
        {
            _currentContentHandler.DisconnectHandler();
        }

        _currentContentHandler = null;
        _currentContent = null;
        PlatformView.ClearChildren();
        RequestRender();
    }

    private void ApplyScrollStyles(IScrollView scrollView)
    {
        var orientation = scrollView.Orientation;
        var allowHorizontal = orientation is ScrollOrientation.Horizontal or ScrollOrientation.Both;
        var allowVertical = orientation is ScrollOrientation.Vertical or ScrollOrientation.Both;

        var changed = SetStyleIfChanged("overflowX", ResolveOverflowCss(allowHorizontal, scrollView.HorizontalScrollBarVisibility));
        changed |= SetStyleIfChanged("overflowY", ResolveOverflowCss(allowVertical, scrollView.VerticalScrollBarVisibility));
        changed |= SetStyleIfChanged("overflow", null);
        if (changed)
        {
            RequestRender();
        }
    }

    private bool SetStyleIfChanged(string styleName, string? value)
    {
        var current = PlatformView.GetStyleValue(styleName);
        if (string.Equals(current, value, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetStyle(styleName, value);
        return true;
    }

    private static string ResolveOverflowCss(bool axisEnabled, ScrollBarVisibility visibility)
    {
        if (!axisEnabled)
        {
            return "hidden";
        }

        return visibility switch
        {
            ScrollBarVisibility.Never => "hidden",
            ScrollBarVisibility.Always => "scroll",
            _ => "auto"
        };
    }

    private static void NormalizeContentPosition(MiragePlatformView contentPlatformView, double arrangedWidth, double arrangedHeight)
    {
        contentPlatformView.SetStyle("position", "relative");
        contentPlatformView.SetStyle("left", null);
        contentPlatformView.SetStyle("top", null);
        contentPlatformView.SetStyle("right", null);
        contentPlatformView.SetStyle("bottom", null);
        contentPlatformView.SetStyle("width", MirageViewMapper.ToPx(Math.Max(0, arrangedWidth)));
        contentPlatformView.SetStyle("height", MirageViewMapper.ToPx(Math.Max(0, arrangedHeight)));
    }
}
