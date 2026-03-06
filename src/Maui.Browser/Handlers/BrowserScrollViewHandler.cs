using Maui.Browser.Dom;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Maui.Browser.Handlers;

public sealed class BrowserScrollViewHandler : BrowserViewHandler<IScrollView>, IScrollViewHandler
{
    public static IPropertyMapper<IScrollView, BrowserScrollViewHandler> Mapper =
        new PropertyMapper<IScrollView, BrowserScrollViewHandler>(BrowserViewMapper.CommonViewMapper)
        {
            [nameof(IContentView.Content)] = MapContent,
            [nameof(IScrollView.Orientation)] = MapOrientation,
            [nameof(IScrollView.HorizontalScrollBarVisibility)] = MapHorizontalScrollBarVisibility,
            [nameof(IScrollView.VerticalScrollBarVisibility)] = MapVerticalScrollBarVisibility,
            [nameof(IScrollView.ContentSize)] = MapContentSize
        };

    public static CommandMapper<IScrollView, BrowserScrollViewHandler> CommandMapper =
        new(BrowserViewMapper.CommonViewCommandMapper)
        {
            [nameof(IScrollView.RequestScrollTo)] = MapRequestScrollTo
        };

    private IView? _currentContent;
    private IElementHandler? _currentContentHandler;
    private Action? _scrollCallback;
    private int _scrollSubscriptionId;

    public BrowserScrollViewHandler()
        : base(Mapper, CommandMapper)
    {
    }

    IScrollView IScrollViewHandler.VirtualView => VirtualView;

    object IScrollViewHandler.PlatformView => PlatformView;

    protected override DomElement CreatePlatformView() => DomElement.Create("div");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Style("boxSizing", "border-box");
        platformView.Style("overflow", "auto");

        _scrollCallback = OnScroll;
        _scrollSubscriptionId = platformView.On("scroll", _scrollCallback);

        SetContent(ResolvePresentedContent(VirtualView));
        ApplyScrollStyles(VirtualView);
        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(DomElement platformView)
    {
        if (_scrollSubscriptionId != 0)
        {
            DomElement.Off(_scrollSubscriptionId);
            _scrollSubscriptionId = 0;
        }

        _scrollCallback = null;
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
        if (ResolvePresentedContent(VirtualView) is IView content)
        {
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
        }
    }

    private void OnScroll()
    {
        var (left, top) = PlatformView.GetScrollPosition();
        if (VirtualView is ScrollView controlsScrollView)
        {
            controlsScrollView.SetScrolledPosition(left, top);
        }
    }

    public static void MapContent(BrowserScrollViewHandler handler, IScrollView scrollView)
    {
        handler.SetContent(ResolvePresentedContent(scrollView));
    }

    public static void MapHorizontalScrollBarVisibility(BrowserScrollViewHandler handler, IScrollView scrollView)
    {
        handler.ApplyScrollStyles(scrollView);
    }

    public static void MapVerticalScrollBarVisibility(BrowserScrollViewHandler handler, IScrollView scrollView)
    {
        handler.ApplyScrollStyles(scrollView);
    }

    public static void MapOrientation(BrowserScrollViewHandler handler, IScrollView scrollView)
    {
        handler.ApplyScrollStyles(scrollView);
    }

    public static void MapContentSize(BrowserScrollViewHandler handler, IScrollView scrollView)
    {
        // Content measurement/arrange drives effective scroll content size.
    }

    public static void MapRequestScrollTo(BrowserScrollViewHandler handler, IScrollView scrollView, object? args)
    {
        if (args is not ScrollToRequest request)
        {
            return;
        }

        var horizontalOffset = NormalizeOffset(request.HorizontalOffset);
        var verticalOffset = NormalizeOffset(request.VerticalOffset);

        handler.PlatformView.SetScrollPosition(horizontalOffset, verticalOffset);
        var (actualHorizontalOffset, actualVerticalOffset) = handler.PlatformView.GetScrollPosition();
        if (scrollView is ScrollView controlsScrollView)
        {
            controlsScrollView.SetScrolledPosition(actualHorizontalOffset, actualVerticalOffset);
        }

        scrollView.ScrollFinished();
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
            return;
        }

        var contentHandler = content.ToHandler(MauiContext);
        if (contentHandler.PlatformView is not DomElement contentElement)
        {
            return;
        }

        PlatformView.Append(contentElement);
        _currentContent = content;
        _currentContentHandler = contentHandler;
    }

    private void DisconnectCurrentContent()
    {
        if (_currentContentHandler is null)
        {
            _currentContent = null;
            return;
        }

        _currentContentHandler.DisconnectHandler();
        _currentContentHandler = null;
        _currentContent = null;
    }

    private void ApplyScrollStyles(IScrollView scrollView)
    {
        var orientation = scrollView.Orientation;
        var allowHorizontal = orientation is ScrollOrientation.Horizontal or ScrollOrientation.Both;
        var allowVertical = orientation is ScrollOrientation.Vertical or ScrollOrientation.Both;

        PlatformView.Style("overflowX", ResolveOverflowCss(allowHorizontal, scrollView.HorizontalScrollBarVisibility));
        PlatformView.Style("overflowY", ResolveOverflowCss(allowVertical, scrollView.VerticalScrollBarVisibility));
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
}
