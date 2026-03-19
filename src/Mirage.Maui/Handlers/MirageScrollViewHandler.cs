using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Mirage.Maui.Handlers;

internal sealed class MirageScrollViewHandler : MirageSingleContentHostHandler<IScrollView>, IScrollViewHandler, IMirageRuntimeEventTarget
{
    internal static readonly string[] ParityPropertyKeys =
    {
        nameof(IContentView.Content),
        nameof(IScrollView.HorizontalScrollBarVisibility),
        nameof(IScrollView.VerticalScrollBarVisibility),
        nameof(IScrollView.Orientation)
    };

    internal static readonly string[] ParityExtensionPropertyKeys =
    {
        nameof(ScrollView.Padding)
    };

    internal static readonly string[] ParityCommandKeys =
    {
        nameof(IScrollView.RequestScrollTo)
    };

    public static IPropertyMapper<IScrollView, MirageScrollViewHandler> Mapper =
        new PropertyMapper<IScrollView, MirageScrollViewHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(IContentView.Content)] = MapContent,
            [nameof(IScrollView.Orientation)] = MapOrientation,
            [nameof(IScrollView.HorizontalScrollBarVisibility)] = MapHorizontalScrollBarVisibility,
            [nameof(IScrollView.VerticalScrollBarVisibility)] = MapVerticalScrollBarVisibility,
            [nameof(ScrollView.Padding)] = MapPadding
        };

    public static CommandMapper<IScrollView, MirageScrollViewHandler> CommandMapper =
        new(MirageViewMapper.CommonViewCommandMapper)
        {
            [nameof(IScrollView.RequestScrollTo)] = MapRequestScrollTo
        };

    private ScrollToRequest? _pendingScrollToRequest;

    public MirageScrollViewHandler()
        : this(Mapper, CommandMapper)
    {
    }

    public MirageScrollViewHandler(IPropertyMapper? mapper)
        : this(mapper, CommandMapper)
    {
    }

    public MirageScrollViewHandler(IPropertyMapper? mapper, CommandMapper? commandMapper)
        : base(ComponentKind.ContentContainer, mapper ?? Mapper, commandMapper ?? CommandMapper)
    {
        DefaultDisplayStyle = "block";
    }

    IScrollView IScrollViewHandler.VirtualView => VirtualView;

    object IScrollViewHandler.PlatformView => PlatformView;

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        base.ConnectHandler(platformView);
        platformView.SetStyle("display", "block");
        platformView.SetStyle("boxSizing", "border-box");
        platformView.SetEvent("scroll", enabled: true);
        SetPresentedContent(ResolvePresentedContent(VirtualView));
        MapPadding(this, VirtualView);
        ApplyScrollStyles(VirtualView);
        RequestRender();
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        platformView.SetEvent("scroll", enabled: false);
        _pendingScrollToRequest = null;
        DisconnectCurrentPresentedContent();
        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        var content = ResolvePresentedContent(VirtualView);
        if (content is null)
        {
            return base.GetDesiredSize(widthConstraint, heightConstraint);
        }

        var orientation = VirtualView.Orientation;
        var horizontalConstraint = orientation is ScrollOrientation.Horizontal or ScrollOrientation.Both
            ? double.PositiveInfinity
            : widthConstraint;
        var verticalConstraint = orientation is ScrollOrientation.Vertical or ScrollOrientation.Both
            ? double.PositiveInfinity
            : heightConstraint;

        var contentSize = content.Measure(horizontalConstraint, verticalConstraint);
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
            || !TryGetPresentedContentPlatformView(out var contentPlatformView))
        {
            // Ensure pending command tasks cannot hang if content is absent/unrealized during arrange.
            TryApplyPendingScrollToRequest(VirtualView);
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
        NormalizeHostedContentFlowStyles(contentPlatformView);
        TryApplyPendingScrollToRequest(VirtualView);
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

        VirtualView.HorizontalOffset = left;
        VirtualView.VerticalOffset = top;
    }

    public static void MapContent(MirageScrollViewHandler handler, IScrollView scrollView)
    {
        handler.SetPresentedContent(ResolvePresentedContent(scrollView));
        handler.TryApplyPendingScrollToRequest(scrollView);
    }

    public static void MapPadding(MirageScrollViewHandler handler, IScrollView scrollView)
    {
        if (scrollView is not IPadding paddingView)
        {
            return;
        }

        MirageViewMapper.MapPadding(handler, paddingView);
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

    public static void MapRequestScrollTo(MirageScrollViewHandler handler, IScrollView scrollView, object? args)
    {
        if (args is not ScrollToRequest request)
        {
            return;
        }

        handler.ApplyScrollRequest(scrollView, request, allowDeferral: true);
    }

    private void ApplyScrollRequest(IScrollView scrollView, ScrollToRequest request, bool allowDeferral)
    {
        if (allowDeferral && ShouldDeferScrollRequest(scrollView))
        {
            _pendingScrollToRequest = request;
            return;
        }

        _pendingScrollToRequest = null;

        var horizontalOffset = NormalizeOffset(request.HorizontalOffset);
        var verticalOffset = NormalizeOffset(request.VerticalOffset);
        var actualHorizontalOffset = horizontalOffset;
        var actualVerticalOffset = verticalOffset;

        if (Coordinator is not null
            && Coordinator.TrySetNodeScrollPosition(
                PlatformView.NodeId,
                horizontalOffset,
                verticalOffset,
                out var realizedLeft,
                out var realizedTop))
        {
            actualHorizontalOffset = realizedLeft;
            actualVerticalOffset = realizedTop;
        }

        scrollView.HorizontalOffset = actualHorizontalOffset;
        scrollView.VerticalOffset = actualVerticalOffset;
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

    private void TryApplyPendingScrollToRequest(IScrollView scrollView)
    {
        if (_pendingScrollToRequest is not ScrollToRequest pendingRequest)
        {
            return;
        }

        ApplyScrollRequest(scrollView, pendingRequest, allowDeferral: false);
    }

    private static bool ShouldDeferScrollRequest(IScrollView scrollView)
    {
        if (ResolvePresentedContent(scrollView) is not IView content)
        {
            return false;
        }

        var hasWidth = IsFinitePositive(content.Frame.Width);
        var hasHeight = IsFinitePositive(content.Frame.Height);
        return scrollView.Orientation switch
        {
            ScrollOrientation.Horizontal => !hasWidth,
            ScrollOrientation.Vertical => !hasHeight,
            ScrollOrientation.Both => !hasWidth || !hasHeight,
            _ => false
        };
    }

    private static bool IsFinitePositive(double value)
    {
        return double.IsFinite(value) && value > 0;
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

}
