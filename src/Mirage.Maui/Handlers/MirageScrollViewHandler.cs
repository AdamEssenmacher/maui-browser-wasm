using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Mirage.Maui.Handlers;

internal sealed class MirageScrollViewHandler : MirageSingleContentHostHandler<IScrollView>, IScrollViewHandler, IMirageRuntimeEventTarget
{
    private const int AnimatedScrollCompletionTimeoutMs = 1200;
    private const double ScrollCompletionEpsilon = 0.01;

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
    private bool _awaitingAnimatedScrollCompletion;
    private double _awaitedAnimatedHorizontalOffset;
    private double _awaitedAnimatedVerticalOffset;
    private int _animatedScrollCompletionRequestVersion;
    private CancellationTokenSource? _animatedScrollCompletionTimeoutCts;

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
        if (_pendingScrollToRequest is not null || _awaitingAnimatedScrollCompletion)
        {
            _pendingScrollToRequest = null;
            if (_awaitingAnimatedScrollCompletion)
            {
                CompleteAnimatedScrollRequest();
            }
            else
            {
                VirtualView.ScrollFinished();
            }
        }

        DisconnectCurrentPresentedContent();
        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        var padding = ResolvePadding(VirtualView);
        var content = ResolvePresentedContent(VirtualView);
        var measuredWidth = Math.Max(0, padding.HorizontalThickness);
        var measuredHeight = Math.Max(0, padding.VerticalThickness);
        if (content is null)
        {
            return ResolveMeasuredSizeWithConstraints(VirtualView, widthConstraint, heightConstraint, measuredWidth, measuredHeight);
        }

        var orientation = VirtualView.Orientation;
        var innerWidthConstraint = ResolveInnerViewportConstraint(widthConstraint, padding.HorizontalThickness);
        var innerHeightConstraint = ResolveInnerViewportConstraint(heightConstraint, padding.VerticalThickness);
        var horizontalConstraint = orientation is ScrollOrientation.Horizontal or ScrollOrientation.Both
            ? double.PositiveInfinity
            : innerWidthConstraint;
        var verticalConstraint = orientation is ScrollOrientation.Vertical or ScrollOrientation.Both
            ? double.PositiveInfinity
            : innerHeightConstraint;

        var contentSize = content.Measure(horizontalConstraint, verticalConstraint);
        measuredWidth = contentSize.Width + padding.HorizontalThickness;
        measuredHeight = contentSize.Height + padding.VerticalThickness;
        return ResolveMeasuredSizeWithConstraints(VirtualView, widthConstraint, heightConstraint, measuredWidth, measuredHeight);
    }

    public override void PlatformArrange(Rect frame)
    {
        base.PlatformArrange(frame);

        var padding = ResolvePadding(VirtualView);
        var width = Math.Max(0, frame.Width - padding.HorizontalThickness);
        var height = Math.Max(0, frame.Height - padding.VerticalThickness);
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
        if (_awaitingAnimatedScrollCompletion && HasReachedAnimatedScrollTarget(left, top))
        {
            CompleteAnimatedScrollRequest();
        }
    }

    public static void MapContent(MirageScrollViewHandler handler, IScrollView scrollView)
    {
        handler.SetPresentedContent(ResolvePresentedContent(scrollView));
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
        if (_awaitingAnimatedScrollCompletion)
        {
            // A new request supersedes any in-flight animation completion for prior requests.
            // The current request must complete only when its own runtime signal arrives.
            CancelAnimatedScrollCompletionTimeout();
            _awaitingAnimatedScrollCompletion = false;
        }

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
        var previousHorizontalOffset = scrollView.HorizontalOffset;
        var previousVerticalOffset = scrollView.VerticalOffset;
        var appliedViaCoordinator = false;

        if (Coordinator is not null
            && Coordinator.TrySetNodeScrollPosition(
                PlatformView.NodeId,
                horizontalOffset,
                verticalOffset,
                out var realizedLeft,
                out var realizedTop))
        {
            appliedViaCoordinator = true;
            actualHorizontalOffset = realizedLeft;
            actualVerticalOffset = realizedTop;
        }

        scrollView.HorizontalOffset = actualHorizontalOffset;
        scrollView.VerticalOffset = actualVerticalOffset;
        if (request.Instant || ShouldCompleteScrollRequestImmediately(scrollView, appliedViaCoordinator, previousHorizontalOffset, previousVerticalOffset, actualHorizontalOffset, actualVerticalOffset))
        {
            scrollView.ScrollFinished();
            return;
        }

        BeginAwaitingAnimatedScrollCompletion(actualHorizontalOffset, actualVerticalOffset);
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

    private static Size ResolveMeasuredSizeWithConstraints(
        IScrollView scrollView,
        double widthConstraint,
        double heightConstraint,
        double measuredWidth,
        double measuredHeight)
    {
        var clampedWidth = ResolveDesiredDimension(widthConstraint, measuredWidth);
        var clampedHeight = ResolveDesiredDimension(heightConstraint, measuredHeight);
        return new Size(
            ResolveConstraints(clampedWidth, scrollView.Width, scrollView.MinimumWidth, scrollView.MaximumWidth),
            ResolveConstraints(clampedHeight, scrollView.Height, scrollView.MinimumHeight, scrollView.MaximumHeight));
    }

    private static double ResolveConstraints(double measured, double exact, double min, double max)
    {
        var resolved = measured;
        var resolvedMin = ResolveMinimum(min);

        if (IsExplicitSet(exact))
        {
            resolved = exact;
        }

        if (resolved > max)
        {
            resolved = max;
        }

        if (resolved < resolvedMin)
        {
            resolved = resolvedMin;
        }

        return resolved;
    }

    private static double ResolveMinimum(double value)
    {
        return IsExplicitSet(value)
            ? value
            : 0;
    }

    private static bool IsExplicitSet(double value)
    {
        return !double.IsNaN(value);
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

    private static Thickness ResolvePadding(IScrollView scrollView)
    {
        return scrollView is IPadding paddingView
            ? paddingView.Padding
            : Thickness.Zero;
    }

    private static double ResolveInnerViewportConstraint(double outerConstraint, double paddingThickness)
    {
        if (double.IsNaN(outerConstraint) || double.IsInfinity(outerConstraint))
        {
            return outerConstraint;
        }

        return Math.Max(0, outerConstraint - Math.Max(0, paddingThickness));
    }

    private static bool ShouldCompleteScrollRequestImmediately(
        IScrollView scrollView,
        bool appliedViaCoordinator,
        double previousHorizontalOffset,
        double previousVerticalOffset,
        double actualHorizontalOffset,
        double actualVerticalOffset)
    {
        if (!appliedViaCoordinator || ResolvePresentedContent(scrollView) is null)
        {
            return true;
        }

        const double epsilon = 0.001;
        return Math.Abs(actualHorizontalOffset - previousHorizontalOffset) < epsilon
               && Math.Abs(actualVerticalOffset - previousVerticalOffset) < epsilon;
    }

    private bool HasReachedAnimatedScrollTarget(double horizontalOffset, double verticalOffset)
    {
        return Math.Abs(horizontalOffset - _awaitedAnimatedHorizontalOffset) <= ScrollCompletionEpsilon
               && Math.Abs(verticalOffset - _awaitedAnimatedVerticalOffset) <= ScrollCompletionEpsilon;
    }

    private void BeginAwaitingAnimatedScrollCompletion(double horizontalOffset, double verticalOffset)
    {
        _awaitedAnimatedHorizontalOffset = horizontalOffset;
        _awaitedAnimatedVerticalOffset = verticalOffset;
        _awaitingAnimatedScrollCompletion = true;
        var requestVersion = unchecked(++_animatedScrollCompletionRequestVersion);
        ArmAnimatedScrollCompletionTimeout(requestVersion);
    }

    private void CompleteAnimatedScrollRequest()
    {
        CancelAnimatedScrollCompletionTimeout();
        if (!_awaitingAnimatedScrollCompletion)
        {
            return;
        }

        _awaitingAnimatedScrollCompletion = false;
        VirtualView.ScrollFinished();
    }

    private void ArmAnimatedScrollCompletionTimeout(int requestVersion)
    {
        CancelAnimatedScrollCompletionTimeout();
        var timeoutCts = new CancellationTokenSource();
        _animatedScrollCompletionTimeoutCts = timeoutCts;
        _ = CompleteAnimatedScrollRequestOnTimeoutAsync(requestVersion, timeoutCts.Token);
    }

    private void CancelAnimatedScrollCompletionTimeout()
    {
        if (_animatedScrollCompletionTimeoutCts is not CancellationTokenSource timeoutCts)
        {
            return;
        }

        _animatedScrollCompletionTimeoutCts = null;
        timeoutCts.Cancel();
        timeoutCts.Dispose();
    }

    private async Task CompleteAnimatedScrollRequestOnTimeoutAsync(int requestVersion, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(AnimatedScrollCompletionTimeoutMs, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        DispatchOnUiThreadIfRequired(() =>
        {
            if (cancellationToken.IsCancellationRequested
                || !_awaitingAnimatedScrollCompletion
                || requestVersion != _animatedScrollCompletionRequestVersion)
            {
                return;
            }

            CompleteAnimatedScrollRequest();
        });
    }

    private void DispatchOnUiThreadIfRequired(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (ResolveDispatcher() is not IDispatcher dispatcher || !dispatcher.IsDispatchRequired)
        {
            action();
            return;
        }

        if (!dispatcher.Dispatch(action))
        {
            action();
        }
    }

    private IDispatcher? ResolveDispatcher()
    {
        return MauiContext?.Services.GetService(typeof(IDispatcher)) as IDispatcher;
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
