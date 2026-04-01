using System.ComponentModel;
using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageCarouselViewHandler : MirageItemsViewHandler<CarouselView>
{
    private static readonly TimeSpan AlignmentRetryDelay = TimeSpan.FromMilliseconds(4);
    private static readonly TimeSpan ScrollSettleDelay = TimeSpan.FromMilliseconds(80);

    private PropertyChangedEventHandler? _carouselPropertyChangedHandler;
    private CollectionItemRealization? _leadingLoopClone;
    private CollectionItemRealization? _trailingLoopClone;

    private bool _updatingCurrentItemFromHandler;
    private bool _updatingPositionFromHandler;
    private bool _dispatchingProgrammaticScroll;
    private bool _awaitingProgrammaticScrollRequest;
    private bool _awaitingAnimatedProgrammaticScroll;
    private int? _pendingAnimatedWrapLogicalIndex;
    private double? _pendingAnimatedTargetMainAxisOffset;

    private double _lastArrangedWidth;
    private double _lastArrangedHeight;
    private double _lastScrollLeft;
    private double _lastScrollTop;

    private int _alignmentRetryVersion;
    private int _scrollSettleVersion;

    internal new static readonly string[] ParityPropertyKeys =
    [
        .. MirageItemsViewHandler<CarouselView>.ParityPropertyKeys,
        nameof(CarouselView.ItemsLayout),
        nameof(CarouselView.IsSwipeEnabled),
        nameof(CarouselView.PeekAreaInsets),
        nameof(CarouselView.IsBounceEnabled),
        nameof(CarouselView.Position),
        nameof(CarouselView.CurrentItem)
    ];

    internal new static readonly string[] ParityCommandKeys = [];

    internal new static readonly string[] ParityExtensionPropertyKeys = MirageItemsViewHandler<CarouselView>.ParityExtensionPropertyKeys;

    public static IPropertyMapper<CarouselView, MirageCarouselViewHandler> Mapper =
        new PropertyMapper<CarouselView, MirageCarouselViewHandler>(ItemsViewMapper)
        {
            [nameof(CarouselView.ItemsLayout)] = MapItemsLayout,
            [nameof(CarouselView.IsSwipeEnabled)] = MapIsSwipeEnabled,
            [nameof(CarouselView.PeekAreaInsets)] = MapPeekAreaInsets,
            [nameof(CarouselView.IsBounceEnabled)] = MapIsBounceEnabled,
            [nameof(CarouselView.Position)] = MapPosition,
            [nameof(CarouselView.CurrentItem)] = MapCurrentItem,
            [nameof(IView.IsEnabled)] = MapIsEnabled,
            ["IsEnabled"] = MapIsEnabled
        };

    public static CommandMapper<CarouselView, MirageCarouselViewHandler> CommandMapper =
        new(MirageItemsViewHandler<CarouselView>.ItemsViewCommandMapper);

    public MirageCarouselViewHandler()
        : base(Mapper, CommandMapper)
    {
    }

    protected override void OnConnectVirtualView()
    {
        _carouselPropertyChangedHandler ??= OnCarouselPropertyChanged;
        VirtualView.PropertyChanged += _carouselPropertyChangedHandler;
        VirtualView.SetIsDragging(false);
        base.OnConnectVirtualView();
    }

    protected override void OnDisconnectVirtualView()
    {
        if (_carouselPropertyChangedHandler is not null)
        {
            VirtualView.PropertyChanged -= _carouselPropertyChangedHandler;
        }

        _alignmentRetryVersion++;
        _scrollSettleVersion++;
        ClearLoopClones();
        ClearVisibleViews();
        VirtualView.SetIsDragging(false);
        _lastArrangedWidth = 0;
        _lastArrangedHeight = 0;
        _lastScrollLeft = 0;
        _lastScrollTop = 0;
        _awaitingProgrammaticScrollRequest = false;
        _awaitingAnimatedProgrammaticScroll = false;
        _pendingAnimatedWrapLogicalIndex = null;
        _pendingAnimatedTargetMainAxisOffset = null;
        base.OnDisconnectVirtualView();
    }

    protected override LinearItemsLayout? ResolveLinearItemsLayout() => VirtualView.ItemsLayout;

    protected override void ApplyItemsLayout()
    {
        base.ApplyItemsLayout();

        if (ItemsHost is null)
        {
            return;
        }

        var isHorizontal = ResolveItemsLayoutOrientation() == ItemsLayoutOrientation.Horizontal;
        PlatformView.SetStyle("scrollSnapType", isHorizontal ? "x mandatory" : "y mandatory");
        PlatformView.SetStyle("scrollBehavior", null);

        ItemsHost.SetStyle("display", "flex");
        ItemsHost.SetStyle("flexWrap", "nowrap");
        ItemsHost.SetStyle("alignItems", "stretch");
        ItemsHost.SetStyle("width", isHorizontal ? "max-content" : "100%");
        ItemsHost.SetStyle("height", isHorizontal ? "100%" : "max-content");

        ApplyDisplayedItemProjection();
    }

    protected override void ApplyScrollStyles()
    {
        var isHorizontal = ResolveItemsLayoutOrientation() == ItemsLayoutOrientation.Horizontal;
        var swipeInteractionEnabled = VirtualView.IsEnabled && VirtualView.IsSwipeEnabled;
        var horizontalOverflow = isHorizontal && swipeInteractionEnabled
            ? ResolveScrollOverflowStyle(VirtualView.HorizontalScrollBarVisibility)
            : "hidden";
        var verticalOverflow = !isHorizontal && swipeInteractionEnabled
            ? ResolveScrollOverflowStyle(VirtualView.VerticalScrollBarVisibility)
            : "hidden";

        PlatformView.SetStyle("overflowX", horizontalOverflow);
        PlatformView.SetStyle("overflowY", verticalOverflow);
        PlatformView.SetStyle("touchAction", swipeInteractionEnabled
            ? (isHorizontal ? "pan-x" : "pan-y")
            : "none");
        PlatformView.SetAttribute("aria-disabled", VirtualView.IsEnabled ? null : "true");

        ApplyBounceProjection();
    }

    protected override void OnItemsRealizationsChanged(bool applyMutationScrollMode)
    {
        RebuildLoopClones();
        RebuildDisplayedDomOrder();
        ApplyDisplayedItemProjection();
        NormalizeVirtualStateFromItems();
        UpdateVisibleViewsAndVisualStates(ResolvePreferredLogicalIndex());
        ScheduleEnsureLogicalIndexVisible(ResolvePreferredLogicalIndex(), animate: false);
        base.OnItemsRealizationsChanged(applyMutationScrollMode);
    }

    protected override void OnRuntimeScrollPositionChanged(double left, double top)
    {
        _lastScrollLeft = left;
        _lastScrollTop = top;

        var isProgrammaticScroll = _dispatchingProgrammaticScroll
            || _awaitingProgrammaticScrollRequest
            || _awaitingAnimatedProgrammaticScroll;
        if (!isProgrammaticScroll && (!VirtualView.IsEnabled || !VirtualView.IsSwipeEnabled))
        {
            return;
        }

        base.OnRuntimeScrollPositionChanged(left, top);

        var logicalIndex = ResolveLogicalIndexForOffset(GetMainAxisOffset(left, top), out var displayedIndex);
        if (logicalIndex >= 0)
        {
            UpdateVirtualStateFromLogicalIndex(logicalIndex);
        }

        UpdateVisibleViewsAndVisualStates(logicalIndex);

        if (_awaitingAnimatedProgrammaticScroll
            && TryCompleteAnimatedProgrammaticScroll(GetMainAxisOffset(left, top), logicalIndex))
        {
            return;
        }

        if (_awaitingProgrammaticScrollRequest)
        {
            _awaitingProgrammaticScrollRequest = false;
        }

        if (isProgrammaticScroll)
        {
            return;
        }

        VirtualView.SetIsDragging(true);
        ScheduleScrollSettle(displayedIndex);
    }

    protected override int ResolveScrollToTargetIndex(ScrollToRequestEventArgs request)
    {
        return base.ResolveScrollToTargetIndex(request);
    }

    protected override void SendProgrammaticScrolled(double horizontalOffset, double verticalOffset)
    {
        _awaitingProgrammaticScrollRequest = false;
        _dispatchingProgrammaticScroll = true;
        try
        {
            base.SendProgrammaticScrolled(horizontalOffset, verticalOffset);
        }
        finally
        {
            _dispatchingProgrammaticScroll = false;
        }
    }

    protected override void ApplyScrollToRequest(ScrollToRequestEventArgs request)
    {
        if (ItemRealizations.Count == 0)
        {
            return;
        }

        var targetIndex = ResolveScrollToTargetIndex(request);
        if (targetIndex < 0 || targetIndex >= ItemRealizations.Count)
        {
            return;
        }

        _awaitingProgrammaticScrollRequest = true;
        if (!ScrollLogicalIndexIntoView(targetIndex, request.IsAnimated))
        {
            _awaitingProgrammaticScrollRequest = false;
        }
    }

    public override void PlatformArrange(Rect frame)
    {
        base.PlatformArrange(frame);
        _lastArrangedWidth = Math.Max(0, frame.Width);
        _lastArrangedHeight = Math.Max(0, frame.Height);
        ApplyDisplayedItemProjection();
        UpdateVisibleViewsAndVisualStates(ResolvePreferredLogicalIndex());
        ScheduleEnsureLogicalIndexVisible(ResolvePreferredLogicalIndex(), animate: false);
    }

    public static void MapItemsLayout(MirageCarouselViewHandler handler, CarouselView carouselView)
    {
        _ = carouselView;
        handler.ApplyItemsLayout();
        handler.ApplyScrollStyles();
        handler.UpdateVisibleViewsAndVisualStates(handler.ResolvePreferredLogicalIndex());
        handler.ScheduleEnsureLogicalIndexVisible(handler.ResolvePreferredLogicalIndex(), animate: false);
        handler.RequestRender();
    }

    public static void MapIsSwipeEnabled(MirageCarouselViewHandler handler, CarouselView carouselView)
    {
        _ = carouselView;
        handler.ApplyScrollStyles();
        handler.RequestRender();
    }

    public static void MapPeekAreaInsets(MirageCarouselViewHandler handler, CarouselView carouselView)
    {
        _ = carouselView;
        handler.ApplyDisplayedItemProjection();
        handler.UpdateVisibleViewsAndVisualStates(handler.ResolvePreferredLogicalIndex());
        handler.ScheduleEnsureLogicalIndexVisible(handler.ResolvePreferredLogicalIndex(), animate: false);
        handler.RequestRender();
    }

    public static void MapIsBounceEnabled(MirageCarouselViewHandler handler, CarouselView carouselView)
    {
        _ = carouselView;
        handler.ApplyBounceProjection();
        handler.RequestRender();
    }

    public static void MapIsEnabled(MirageCarouselViewHandler handler, CarouselView carouselView)
    {
        _ = carouselView;
        handler.ApplyScrollStyles();
        handler.RequestRender();
    }

    public static void MapPosition(MirageCarouselViewHandler handler, CarouselView carouselView)
    {
        _ = carouselView;
        if (handler._updatingPositionFromHandler)
        {
            return;
        }

        handler.ApplyMappedPositionChange();
    }

    public static void MapCurrentItem(MirageCarouselViewHandler handler, CarouselView carouselView)
    {
        _ = carouselView;
        if (handler._updatingCurrentItemFromHandler)
        {
            return;
        }

        handler.ApplyMappedCurrentItemChange();
    }

    private void ApplyMappedPositionChange()
    {
        if (ItemRealizations.Count == 0)
        {
            NormalizeVirtualStateFromItems();
            return;
        }

        var targetIndex = Math.Clamp(VirtualView.Position, 0, ItemRealizations.Count - 1);
        UpdateVirtualStateFromLogicalIndex(targetIndex);
        UpdateVisibleViewsAndVisualStates(targetIndex);
        ScrollLogicalIndexIntoView(targetIndex, VirtualView.AnimatePositionChanges);
    }

    private void ApplyMappedCurrentItemChange()
    {
        if (ItemRealizations.Count == 0)
        {
            NormalizeVirtualStateFromItems();
            return;
        }

        var targetIndex = FindFirstIndexForItem(VirtualView.CurrentItem);
        if (targetIndex < 0)
        {
            return;
        }

        UpdateVirtualStateFromLogicalIndex(targetIndex);
        UpdateVisibleViewsAndVisualStates(targetIndex);
        ScrollLogicalIndexIntoView(targetIndex, VirtualView.AnimateCurrentItemChanges);
    }

    private void NormalizeVirtualStateFromItems()
    {
        if (ItemRealizations.Count == 0)
        {
            if (VirtualView.Position != 0)
            {
                _updatingPositionFromHandler = true;
                try
                {
                    VirtualView.Position = 0;
                }
                finally
                {
                    _updatingPositionFromHandler = false;
                }
            }

            if (VirtualView.CurrentItem is not null)
            {
                _updatingCurrentItemFromHandler = true;
                try
                {
                    VirtualView.CurrentItem = null;
                }
                finally
                {
                    _updatingCurrentItemFromHandler = false;
                }
            }

            ClearVisibleViews();
            return;
        }

        UpdateVirtualStateFromLogicalIndex(ResolvePreferredLogicalIndex());
    }

    private int ResolvePreferredLogicalIndex()
    {
        if (ItemRealizations.Count == 0)
        {
            return -1;
        }

        var currentItemIndex = FindFirstIndexForItem(VirtualView.CurrentItem);
        if (currentItemIndex >= 0)
        {
            return currentItemIndex;
        }

        return Math.Clamp(VirtualView.Position, 0, ItemRealizations.Count - 1);
    }

    private void UpdateVirtualStateFromLogicalIndex(int logicalIndex)
    {
        if (logicalIndex < 0 || logicalIndex >= ItemRealizations.Count)
        {
            return;
        }

        var currentItem = ItemsSnapshot[logicalIndex];
        if (VirtualView.Position != logicalIndex)
        {
            _updatingPositionFromHandler = true;
            try
            {
                VirtualView.Position = logicalIndex;
            }
            finally
            {
                _updatingPositionFromHandler = false;
            }
        }

        if (!ReferenceEquals(VirtualView.CurrentItem, currentItem))
        {
            _updatingCurrentItemFromHandler = true;
            try
            {
                VirtualView.CurrentItem = currentItem;
            }
            finally
            {
                _updatingCurrentItemFromHandler = false;
            }
        }
    }

    private void ApplyDisplayedItemProjection()
    {
        if (ItemsHost is null)
        {
            return;
        }

        var displayedItems = GetDisplayedRealizations();
        if (displayedItems.Count == 0)
        {
            ApplyScrollPaddingStyles();
            return;
        }

        var isHorizontal = ResolveItemsLayoutOrientation() == ItemsLayoutOrientation.Horizontal;
        var itemExtent = ResolveMainAxisItemExtent();
        var leadingEdgeSpacing = ShouldUseLoopClones() ? 0 : ResolveLeadingMainAxisPeekInset();
        var trailingEdgeSpacing = ShouldUseLoopClones() ? 0 : ResolveTrailingMainAxisPeekInset();

        ApplyScrollPaddingStyles();

        for (var displayIndex = 0; displayIndex < displayedItems.Count; displayIndex++)
        {
            var node = displayedItems[displayIndex].Node;
            node.SetStyle("flexGrow", "0");
            node.SetStyle("flexShrink", "0");
            node.SetStyle("scrollSnapAlign", "center");
            node.SetStyle("scrollSnapStop", "always");

            if (isHorizontal)
            {
                node.SetStyle("width", itemExtent > 0 ? MirageViewMapper.ToPx(itemExtent) : null);
                node.SetStyle("height", "100%");
                node.SetStyle("marginTop", null);
                node.SetStyle("marginBottom", null);
                node.SetStyle("marginLeft", !ShouldUseLoopClones() && displayIndex == 0 ? MirageViewMapper.ToPx(leadingEdgeSpacing) : null);
                node.SetStyle("marginRight", !ShouldUseLoopClones() && displayIndex == displayedItems.Count - 1 ? MirageViewMapper.ToPx(trailingEdgeSpacing) : null);
            }
            else
            {
                node.SetStyle("height", itemExtent > 0 ? MirageViewMapper.ToPx(itemExtent) : null);
                node.SetStyle("width", "100%");
                node.SetStyle("marginLeft", null);
                node.SetStyle("marginRight", null);
                node.SetStyle("marginTop", !ShouldUseLoopClones() && displayIndex == 0 ? MirageViewMapper.ToPx(leadingEdgeSpacing) : null);
                node.SetStyle("marginBottom", !ShouldUseLoopClones() && displayIndex == displayedItems.Count - 1 ? MirageViewMapper.ToPx(trailingEdgeSpacing) : null);
            }
        }
    }

    private void ApplyScrollPaddingStyles()
    {
        var insets = VirtualView.PeekAreaInsets;
        var isHorizontal = ResolveItemsLayoutOrientation() == ItemsLayoutOrientation.Horizontal;

        PlatformView.SetStyle("scrollPaddingLeft", isHorizontal ? MirageViewMapper.ToPx(Math.Max(0, insets.Left)) : null);
        PlatformView.SetStyle("scrollPaddingRight", isHorizontal ? MirageViewMapper.ToPx(Math.Max(0, insets.Right)) : null);
        PlatformView.SetStyle("scrollPaddingTop", !isHorizontal ? MirageViewMapper.ToPx(Math.Max(0, insets.Top)) : null);
        PlatformView.SetStyle("scrollPaddingBottom", !isHorizontal ? MirageViewMapper.ToPx(Math.Max(0, insets.Bottom)) : null);
    }

    private void ApplyBounceProjection()
    {
        var isHorizontal = ResolveItemsLayoutOrientation() == ItemsLayoutOrientation.Horizontal;
        var overscrollValue = VirtualView.IsBounceEnabled ? "auto" : "none";

        PlatformView.SetStyle("overscrollBehaviorX", isHorizontal ? overscrollValue : "none");
        PlatformView.SetStyle("overscrollBehaviorY", isHorizontal ? "none" : overscrollValue);
    }

    private bool ScrollLogicalIndexIntoView(int logicalIndex, bool animate)
    {
        if (logicalIndex < 0 || logicalIndex >= ItemRealizations.Count
            || Coordinator is null
            || string.IsNullOrWhiteSpace(PlatformView.NodeId))
        {
            return false;
        }

        var targetDisplayedIndex = ResolveProgrammaticTargetDisplayedIndex(logicalIndex, animate);
        var displayedItems = GetDisplayedRealizations();
        if (targetDisplayedIndex < 0 || targetDisplayedIndex >= displayedItems.Count)
        {
            return false;
        }

        var targetNodeId = displayedItems[targetDisplayedIndex].Node.NodeId;
        if (string.IsNullOrWhiteSpace(targetNodeId))
        {
            return false;
        }

        if (!Coordinator.TryScrollNodeChildIntoView(
                PlatformView.NodeId,
                targetNodeId,
                (int)ScrollToPosition.Center,
                ResolveItemsLayoutOrientation() == ItemsLayoutOrientation.Horizontal,
                animate,
                out var actualLeft,
                out var actualTop))
        {
            return false;
        }

        var usingWrapClone = animate && targetDisplayedIndex != MapLogicalIndexToDisplayedIndex(logicalIndex);
        _pendingAnimatedWrapLogicalIndex = usingWrapClone ? logicalIndex : null;
        var (targetLeft, targetTop) = ResolveTargetScrollPositionForDisplayedIndex(targetDisplayedIndex);
        var targetMainAxisOffset = GetMainAxisOffset(targetLeft, targetTop);
        _pendingAnimatedTargetMainAxisOffset = animate ? targetMainAxisOffset : null;

        if (!animate)
        {
            if (Coordinator.TrySetNodeScrollPosition(PlatformView.NodeId, targetLeft, targetTop, out var alignedLeft, out var alignedTop))
            {
                actualLeft = alignedLeft;
                actualTop = alignedTop;
            }
            else
            {
                actualLeft = targetLeft;
                actualTop = targetTop;
            }
        }

        var currentMainAxisOffset = GetCurrentMainAxisOffset();
        _awaitingAnimatedProgrammaticScroll = animate && Math.Abs(targetMainAxisOffset - currentMainAxisOffset) > 0.5;
        if (!animate)
        {
            SendProgrammaticScrolled(actualLeft, actualTop);
        }
        else if (!_awaitingAnimatedProgrammaticScroll && _pendingAnimatedWrapLogicalIndex is { } immediateWrapIndex)
        {
            _pendingAnimatedWrapLogicalIndex = null;
            _pendingAnimatedTargetMainAxisOffset = null;
            JumpToLoopWrappedIndex(immediateWrapIndex);
        }
        else if (!_awaitingAnimatedProgrammaticScroll)
        {
            _awaitingProgrammaticScrollRequest = false;
            _pendingAnimatedTargetMainAxisOffset = null;
        }
        else if (_awaitingAnimatedProgrammaticScroll)
        {
            ScheduleAnimatedProgrammaticScrollWatchdog(currentMainAxisOffset);
        }

        return true;
    }

    private void ScheduleAnimatedProgrammaticScrollWatchdog(double observedMainAxisOffset)
    {
        if (Coordinator is null)
        {
            CompleteAnimatedProgrammaticScroll(ResolvePreferredLogicalIndex());
            return;
        }

        var version = ++_scrollSettleVersion;
        Coordinator.DispatchDelayed(ScrollSettleDelay, () =>
        {
            if (version != _scrollSettleVersion)
            {
                return;
            }

            if (!_awaitingAnimatedProgrammaticScroll)
            {
                return;
            }

            var currentMainAxisOffset = GetCurrentMainAxisOffset();
            if (TryCompleteAnimatedProgrammaticScroll(
                    currentMainAxisOffset,
                    ResolveLogicalIndexForOffset(currentMainAxisOffset, out _)))
            {
                return;
            }

            if (Math.Abs(currentMainAxisOffset - observedMainAxisOffset) > 0.5)
            {
                ScheduleAnimatedProgrammaticScrollWatchdog(currentMainAxisOffset);
                return;
            }

            CompleteAnimatedProgrammaticScroll(ResolveLogicalIndexForOffset(currentMainAxisOffset, out _));
        });
    }

    private void ScheduleEnsureLogicalIndexVisible(int logicalIndex, bool animate)
    {
        if (logicalIndex < 0 || logicalIndex >= ItemRealizations.Count || Coordinator is null)
        {
            return;
        }

        if (ScrollLogicalIndexIntoView(logicalIndex, animate))
        {
            return;
        }

        var version = ++_alignmentRetryVersion;
        ScheduleEnsureLogicalIndexVisibleRetry(version, logicalIndex, animate, attempt: 0);
    }

    private void ScheduleEnsureLogicalIndexVisibleRetry(int version, int logicalIndex, bool animate, int attempt)
    {
        if (Coordinator is null || attempt >= 6)
        {
            return;
        }

        Coordinator.DispatchDelayed(AlignmentRetryDelay, () =>
        {
            if (version != _alignmentRetryVersion || logicalIndex < 0 || logicalIndex >= ItemRealizations.Count)
            {
                return;
            }

            if (ScrollLogicalIndexIntoView(logicalIndex, animate))
            {
                return;
            }

            ScheduleEnsureLogicalIndexVisibleRetry(version, logicalIndex, animate, attempt + 1);
        });
    }

    private void ScheduleScrollSettle(int displayedIndex)
    {
        _ = displayedIndex;
        if (Coordinator is null)
        {
            return;
        }

        var version = ++_scrollSettleVersion;
        Coordinator.DispatchDelayed(ScrollSettleDelay, () =>
        {
            if (version != _scrollSettleVersion)
            {
                return;
            }

            VirtualView.SetIsDragging(false);
            _awaitingAnimatedProgrammaticScroll = false;

            var currentDisplayedIndex = ResolveDisplayedIndexForOffset(GetCurrentMainAxisOffset());
            if (ShouldUseLoopClones() && currentDisplayedIndex == 0)
            {
                JumpToLoopWrappedIndex(ItemRealizations.Count - 1);
                return;
            }

            if (ShouldUseLoopClones() && currentDisplayedIndex == GetDisplayedRealizationCount() - 1)
            {
                JumpToLoopWrappedIndex(0);
                return;
            }

            var currentLogicalIndex = ResolveLogicalIndexForOffset(GetCurrentMainAxisOffset(), out _);
            UpdateVisibleViewsAndVisualStates(currentLogicalIndex);
        });
    }

    private void JumpToLoopWrappedIndex(int logicalIndex)
    {
        if (logicalIndex < 0 || logicalIndex >= ItemRealizations.Count)
        {
            return;
        }

        UpdateVirtualStateFromLogicalIndex(logicalIndex);
        ScrollLogicalIndexIntoView(logicalIndex, animate: false);
        UpdateVisibleViewsAndVisualStates(logicalIndex);
    }

    private bool TryCompleteAnimatedProgrammaticScroll(double currentMainAxisOffset, int logicalIndex)
    {
        if (!_awaitingAnimatedProgrammaticScroll || !HasReachedAnimatedProgrammaticTarget(currentMainAxisOffset))
        {
            return false;
        }

        CompleteAnimatedProgrammaticScroll(logicalIndex);
        return true;
    }

    private bool HasReachedAnimatedProgrammaticTarget(double currentMainAxisOffset)
    {
        return _pendingAnimatedTargetMainAxisOffset is not { } targetOffset
            || Math.Abs(currentMainAxisOffset - targetOffset) <= 0.5;
    }

    private void CompleteAnimatedProgrammaticScroll(int logicalIndex)
    {
        _awaitingProgrammaticScrollRequest = false;
        _awaitingAnimatedProgrammaticScroll = false;
        _pendingAnimatedTargetMainAxisOffset = null;
        _scrollSettleVersion++;
        VirtualView.SetIsDragging(false);

        if (_pendingAnimatedWrapLogicalIndex is { } wrapLogicalIndex)
        {
            _pendingAnimatedWrapLogicalIndex = null;
            JumpToLoopWrappedIndex(wrapLogicalIndex);
            return;
        }

        if (logicalIndex < 0)
        {
            logicalIndex = ResolveLogicalIndexForOffset(GetCurrentMainAxisOffset(), out _);
        }

        UpdateVisibleViewsAndVisualStates(logicalIndex >= 0 ? logicalIndex : ResolvePreferredLogicalIndex());
    }

    private void UpdateVisibleViewsAndVisualStates(int logicalIndex)
    {
        UpdateVisibleViews();
        ApplyVisualStates(logicalIndex);
    }

    private void UpdateVisibleViews()
    {
        var visibleViews = ResolveVisibleViews();
        var currentViews = VirtualView.VisibleViews;

        if (currentViews.Count == visibleViews.Count)
        {
            var matches = true;
            for (var index = 0; index < currentViews.Count; index++)
            {
                if (!ReferenceEquals(currentViews[index], visibleViews[index]))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                return;
            }
        }

        currentViews.Clear();
        foreach (var view in visibleViews)
        {
            currentViews.Add(view);
        }
    }

    private void ClearVisibleViews()
    {
        if (VirtualView.VisibleViews.Count == 0)
        {
            return;
        }

        VirtualView.VisibleViews.Clear();
    }

    private IReadOnlyList<View> ResolveVisibleViews()
    {
        var displayedItems = GetDisplayedRealizations();
        if (displayedItems.Count == 0)
        {
            return Array.Empty<View>();
        }

        var viewportExtent = ResolveViewportExtent();
        var itemExtent = ResolveMainAxisItemExtent();
        var itemStride = ResolveItemStride(itemExtent);
        if (viewportExtent <= 0 || itemExtent <= 0 || itemStride <= 0)
        {
            return Array.Empty<View>();
        }

        var viewportStart = GetCurrentMainAxisOffset();
        var viewportEnd = viewportStart + viewportExtent;
        var visibleViews = new List<View>();
        var seenViews = new HashSet<View>(ReferenceEqualityComparer.Instance);
        for (var displayIndex = 0; displayIndex < displayedItems.Count; displayIndex++)
        {
            var itemStart = ResolveDisplayItemStart(displayIndex, itemStride);
            var itemEnd = itemStart + itemExtent;
            if (itemEnd <= viewportStart || itemStart >= viewportEnd)
            {
                continue;
            }

            var logicalIndex = MapDisplayedIndexToLogicalIndex(displayIndex);
            if (logicalIndex < 0 || logicalIndex >= ItemRealizations.Count)
            {
                continue;
            }

            var logicalView = ItemRealizations[logicalIndex].View;
            if (seenViews.Add(logicalView))
            {
                visibleViews.Add(logicalView);
            }
        }

        return visibleViews;
    }

    private void ApplyVisualStates(int logicalIndex)
    {
        if (logicalIndex < 0 || ItemRealizations.Count == 0)
        {
            return;
        }

        var logicalCount = ItemRealizations.Count;
        var previousIndex = logicalCount == 0
            ? -1
            : (logicalIndex == 0 ? (VirtualView.Loop ? logicalCount - 1 : -1) : logicalIndex - 1);
        var nextIndex = logicalCount == 0
            ? -1
            : (logicalIndex == logicalCount - 1 ? (VirtualView.Loop ? 0 : -1) : logicalIndex + 1);

        for (var itemIndex = 0; itemIndex < logicalCount; itemIndex++)
        {
            var state = ResolveVisualState(itemIndex, logicalIndex, previousIndex, nextIndex);
            VisualStateManager.GoToState(ItemRealizations[itemIndex].View, state);
            ItemRealizations[itemIndex].Node.SetProperty("CarouselVisualState", state);
            ItemRealizations[itemIndex].Node.SetProperty("CarouselLogicalIndex", itemIndex.ToString());
        }

        foreach (var clone in EnumerateLoopClones())
        {
            var mappedLogicalIndex = ReferenceEquals(clone, _leadingLoopClone)
                ? logicalCount - 1
                : 0;
            var state = ResolveVisualState(mappedLogicalIndex, logicalIndex, previousIndex, nextIndex);
            VisualStateManager.GoToState(clone.View, state);
            clone.Node.SetProperty("CarouselVisualState", state);
            clone.Node.SetProperty("CarouselLogicalIndex", mappedLogicalIndex.ToString());
        }
    }

    private IEnumerable<CollectionItemRealization> EnumerateLoopClones()
    {
        if (_leadingLoopClone is not null)
        {
            yield return _leadingLoopClone;
        }

        if (_trailingLoopClone is not null)
        {
            yield return _trailingLoopClone;
        }
    }

    private static string ResolveVisualState(int candidateIndex, int currentIndex, int previousIndex, int nextIndex)
    {
        if (candidateIndex == currentIndex)
        {
            return CarouselView.CurrentItemVisualState;
        }

        if (candidateIndex == previousIndex)
        {
            return CarouselView.PreviousItemVisualState;
        }

        if (candidateIndex == nextIndex)
        {
            return CarouselView.NextItemVisualState;
        }

        return CarouselView.DefaultItemVisualState;
    }

    private void RebuildLoopClones()
    {
        ClearLoopClones();
        if (!ShouldUseLoopClones() || ItemsHost is null)
        {
            return;
        }

        var leadingClone = CreateItemRealization(ItemsSnapshot[^1]);
        var trailingClone = CreateItemRealization(ItemsSnapshot[0]);
        if (leadingClone is null || trailingClone is null)
        {
            if (leadingClone is not null)
            {
                DisconnectItem(leadingClone, removeFromDom: false);
            }

            if (trailingClone is not null)
            {
                DisconnectItem(trailingClone, removeFromDom: false);
            }

            return;
        }

        leadingClone.Node.SetProperty("CarouselCloneRole", "leading");
        trailingClone.Node.SetProperty("CarouselCloneRole", "trailing");

        _leadingLoopClone = leadingClone;
        _trailingLoopClone = trailingClone;
    }

    private void ClearLoopClones()
    {
        if (_leadingLoopClone is not null)
        {
            DisconnectItem(_leadingLoopClone, removeFromDom: true);
            _leadingLoopClone = null;
        }

        if (_trailingLoopClone is not null)
        {
            DisconnectItem(_trailingLoopClone, removeFromDom: true);
            _trailingLoopClone = null;
        }
    }

    private bool ShouldUseLoopClones() => VirtualView.Loop && ItemRealizations.Count > 1;

    private int GetDisplayedRealizationCount()
    {
        var count = ItemRealizations.Count;
        if (ShouldUseLoopClones())
        {
            count += 2;
        }

        return count;
    }

    private List<CollectionItemRealization> GetDisplayedRealizations()
    {
        var displayedItems = new List<CollectionItemRealization>(GetDisplayedRealizationCount());
        if (_leadingLoopClone is not null)
        {
            displayedItems.Add(_leadingLoopClone);
        }

        displayedItems.AddRange(ItemRealizations);

        if (_trailingLoopClone is not null)
        {
            displayedItems.Add(_trailingLoopClone);
        }

        return displayedItems;
    }

    private void RebuildDisplayedDomOrder()
    {
        if (ItemsHost is null)
        {
            return;
        }

        ItemsHost.ClearChildren();
        foreach (var realization in GetDisplayedRealizations())
        {
            ItemsHost.AddChild(realization.Node);
        }
    }

    private int ResolveLogicalIndexForOffset(double offset, out int displayedIndex)
    {
        displayedIndex = ResolveDisplayedIndexForOffset(offset);
        return MapDisplayedIndexToLogicalIndex(displayedIndex);
    }

    private int ResolveDisplayedIndexForOffset(double offset)
    {
        var itemExtent = ResolveMainAxisItemExtent();
        var itemStride = ResolveItemStride(itemExtent);
        var displayedCount = GetDisplayedRealizationCount();
        if (itemStride <= 0 || displayedCount <= 0)
        {
            return -1;
        }

        var snappedIndex = (int)Math.Round(offset / itemStride, MidpointRounding.AwayFromZero);
        return Math.Clamp(snappedIndex, 0, displayedCount - 1);
    }

    private int MapDisplayedIndexToLogicalIndex(int displayedIndex)
    {
        if (displayedIndex < 0 || ItemRealizations.Count == 0)
        {
            return -1;
        }

        if (!ShouldUseLoopClones())
        {
            return Math.Clamp(displayedIndex, 0, ItemRealizations.Count - 1);
        }

        if (displayedIndex == 0)
        {
            return ItemRealizations.Count - 1;
        }

        if (displayedIndex == GetDisplayedRealizationCount() - 1)
        {
            return 0;
        }

        return Math.Clamp(displayedIndex - 1, 0, ItemRealizations.Count - 1);
    }

    private int MapLogicalIndexToDisplayedIndex(int logicalIndex)
    {
        if (logicalIndex < 0 || ItemRealizations.Count == 0)
        {
            return -1;
        }

        if (!ShouldUseLoopClones())
        {
            return Math.Clamp(logicalIndex, 0, ItemRealizations.Count - 1);
        }

        return Math.Clamp(logicalIndex + 1, 1, ItemRealizations.Count);
    }

    private int ResolveProgrammaticTargetDisplayedIndex(int logicalIndex, bool animate)
    {
        var logicalDisplayedIndex = MapLogicalIndexToDisplayedIndex(logicalIndex);
        if (!animate || !ShouldUseLoopClones())
        {
            return logicalDisplayedIndex;
        }

        var currentLogicalIndex = ResolveLogicalIndexForOffset(GetCurrentMainAxisOffset(), out _);
        if (currentLogicalIndex < 0)
        {
            currentLogicalIndex = ResolvePreferredLogicalIndex();
        }

        if (currentLogicalIndex == 0 && logicalIndex == ItemRealizations.Count - 1)
        {
            return 0;
        }

        if (currentLogicalIndex == ItemRealizations.Count - 1 && logicalIndex == 0)
        {
            return GetDisplayedRealizationCount() - 1;
        }

        return logicalDisplayedIndex;
    }

    private double ResolveViewportExtent()
    {
        var arrangedExtent = ResolveItemsLayoutOrientation() == ItemsLayoutOrientation.Horizontal
            ? _lastArrangedWidth
            : _lastArrangedHeight;
        var explicitExtent = ResolveExplicitViewportExtent();
        if (explicitExtent > 0)
        {
            return Math.Max(arrangedExtent, explicitExtent);
        }

        return arrangedExtent;
    }

    private double ResolveMainAxisPeekSpacing()
    {
        return ResolveLeadingMainAxisPeekInset() + ResolveTrailingMainAxisPeekInset();
    }

    private double ResolveLeadingMainAxisPeekInset()
    {
        var insets = VirtualView.PeekAreaInsets;
        return ResolveItemsLayoutOrientation() == ItemsLayoutOrientation.Horizontal
            ? Math.Max(0, insets.Left)
            : Math.Max(0, insets.Top);
    }

    private double ResolveTrailingMainAxisPeekInset()
    {
        var insets = VirtualView.PeekAreaInsets;
        return ResolveItemsLayoutOrientation() == ItemsLayoutOrientation.Horizontal
            ? Math.Max(0, insets.Right)
            : Math.Max(0, insets.Bottom);
    }

    private double ResolveMainAxisItemExtent()
    {
        var viewportExtent = ResolveViewportExtent();
        if (viewportExtent <= 0)
        {
            return 0;
        }

        return Math.Max(0, viewportExtent - ResolveMainAxisPeekSpacing());
    }

    private double ResolveItemStride(double itemExtent)
    {
        var layout = ResolveLinearItemsLayout();
        return itemExtent + ResolveItemSpacing(layout?.ItemSpacing ?? 0);
    }

    private double ResolveDisplayItemStart(int displayIndex, double itemStride)
    {
        if (displayIndex <= 0)
        {
            return ShouldUseLoopClones() ? 0 : ResolveLeadingMainAxisPeekInset();
        }

        return (ShouldUseLoopClones() ? 0 : ResolveLeadingMainAxisPeekInset()) + (displayIndex * itemStride);
    }

    private (double Left, double Top) ResolveTargetScrollPositionForDisplayedIndex(int displayedIndex)
    {
        if (displayedIndex < 0)
        {
            return (_lastScrollLeft, _lastScrollTop);
        }

        var itemExtent = ResolveMainAxisItemExtent();
        var itemStride = ResolveItemStride(itemExtent);
        if (itemStride <= 0)
        {
            return (_lastScrollLeft, _lastScrollTop);
        }

        var mainAxisOffset = displayedIndex * itemStride;
        if (ShouldUseLoopClones())
        {
            mainAxisOffset = Math.Max(0, mainAxisOffset - ResolveLeadingMainAxisPeekInset());
        }

        return ResolveItemsLayoutOrientation() == ItemsLayoutOrientation.Horizontal
            ? (mainAxisOffset, 0)
            : (0, mainAxisOffset);
    }

    private double ResolveExplicitViewportExtent()
    {
        var requestedExtent = ResolveItemsLayoutOrientation() == ItemsLayoutOrientation.Horizontal
            ? VirtualView.WidthRequest
            : VirtualView.HeightRequest;
        return double.IsFinite(requestedExtent) && requestedExtent > 0
            ? requestedExtent
            : 0;
    }

    private double GetCurrentMainAxisOffset()
    {
        return ResolveItemsLayoutOrientation() == ItemsLayoutOrientation.Horizontal
            ? _lastScrollLeft
            : _lastScrollTop;
    }

    private double GetMainAxisOffset(double left, double top)
    {
        return ResolveItemsLayoutOrientation() == ItemsLayoutOrientation.Horizontal ? left : top;
    }

    private void OnCarouselPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = sender;
        if (!string.Equals(e.PropertyName, CarouselView.LoopProperty.PropertyName, StringComparison.Ordinal)
            && !string.Equals(e.PropertyName, nameof(CarouselView.Loop), StringComparison.Ordinal))
        {
            return;
        }

        RebuildLoopClones();
        RebuildDisplayedDomOrder();
        ApplyDisplayedItemProjection();
        UpdateVisibleViewsAndVisualStates(ResolvePreferredLogicalIndex());
        ScheduleEnsureLogicalIndexVisible(ResolvePreferredLogicalIndex(), animate: false);
        RequestRender();
    }
}
