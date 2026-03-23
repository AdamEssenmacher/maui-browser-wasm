using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using Mirage.Core.Diagnostics;
using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Mirage.Maui.Handlers;

internal abstract class MirageItemsViewHandler<TItemsView> : MirageViewHandler<TItemsView>, IMirageRuntimeEventTarget
    where TItemsView : ItemsView
{
    private const string ScrollEventName = "scroll";
    private static long s_itemNodeIdCounter;

    private readonly List<object?> _itemsSnapshot = [];
    private readonly List<CollectionItemRealization> _itemRealizations = [];
    private readonly HashSet<string> _reportedUnsupportedKeys = new(StringComparer.Ordinal);

    private NotifyCollectionChangedEventHandler? _collectionChangedHandler;
    private INotifyCollectionChanged? _collectionChangedSource;
    private EventHandler<ScrollToRequestEventArgs>? _scrollToRequestedHandler;
    private EventHandler<SelectionChangedEventArgs>? _selectionChangedHandler;

    private MiragePlatformView? _contentHost;
    private MiragePlatformView? _headerHost;
    private MiragePlatformView? _itemsHost;
    private MiragePlatformView? _footerHost;
    private MiragePlatformView? _emptyHost;

    private HostedViewRealization? _headerRealization;
    private HostedViewRealization? _footerRealization;
    private HostedViewRealization? _emptyViewRealization;

    private bool _updatingSelectionFromDom;
    private bool _updatingSelectionFromHandler;

    internal static readonly string[] ParityPropertyKeys =
    {
        nameof(ItemsView.ItemsSource),
        nameof(ItemsView.HorizontalScrollBarVisibility),
        nameof(ItemsView.VerticalScrollBarVisibility),
        nameof(ItemsView.ItemTemplate),
        nameof(ItemsView.EmptyView),
        nameof(ItemsView.EmptyViewTemplate),
        nameof(IView.FlowDirection),
        "IsVisible",
        nameof(ItemsView.ItemsUpdatingScrollMode)
    };

    internal static readonly string[] ParityCommandKeys = [];

    internal static readonly string[] ParityExtensionPropertyKeys =
    {
        nameof(IView.IsEnabled)
    };

    public static IPropertyMapper<TItemsView, MirageItemsViewHandler<TItemsView>> ItemsViewMapper =
        new PropertyMapper<TItemsView, MirageItemsViewHandler<TItemsView>>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(ItemsView.ItemsSource)] = MapItemsSource,
            [nameof(ItemsView.HorizontalScrollBarVisibility)] = MapHorizontalScrollBarVisibility,
            [nameof(ItemsView.VerticalScrollBarVisibility)] = MapVerticalScrollBarVisibility,
            [nameof(ItemsView.ItemTemplate)] = MapItemTemplate,
            [nameof(ItemsView.EmptyView)] = MapEmptyView,
            [nameof(ItemsView.EmptyViewTemplate)] = MapEmptyViewTemplate,
            [nameof(IView.FlowDirection)] = MapFlowDirection,
            ["IsVisible"] = MapIsVisible,
            [nameof(ItemsView.ItemsUpdatingScrollMode)] = MapItemsUpdatingScrollMode,
            [nameof(IView.IsEnabled)] = MapIsEnabled,
            ["IsEnabled"] = MapIsEnabled
        };

    public static CommandMapper<TItemsView, MirageItemsViewHandler<TItemsView>> ItemsViewCommandMapper =
        new(MirageViewMapper.CommonViewCommandMapper);

    protected MirageItemsViewHandler(
        IPropertyMapper mapper,
        CommandMapper? commandMapper = null)
        : base(ComponentKind.ContentContainer, mapper, commandMapper ?? ItemsViewCommandMapper)
    {
        DefaultDisplayStyle = "block";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        EnsureHostTopology(platformView);
        ApplyRootStyles(platformView);

        _collectionChangedHandler ??= OnItemsSourceCollectionChanged;
        _scrollToRequestedHandler ??= OnScrollToRequested;

        SubscribeToItemsSource(VirtualView.ItemsSource);
        VirtualView.ScrollToRequested += _scrollToRequestedHandler;

        OnConnectVirtualView();

        ApplyFlowDirection();
        ApplyItemsLayout();
        ApplyScrollStyles();
        RebuildAllContent();

        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        UnsubscribeFromItemsSource();

        if (_scrollToRequestedHandler is not null)
        {
            VirtualView.ScrollToRequested -= _scrollToRequestedHandler;
        }

        OnDisconnectVirtualView();
        DisconnectAllItems();
        DisconnectHostedContent(ref _headerRealization, _headerHost);
        DisconnectHostedContent(ref _footerRealization, _footerHost);
        DisconnectHostedContent(ref _emptyViewRealization, _emptyHost);

        platformView.SetEvent(ScrollEventName, enabled: false);
        platformView.ClearChildren();

        _itemsSnapshot.Clear();
        _contentHost = null;
        _headerHost = null;
        _itemsHost = null;
        _footerHost = null;
        _emptyHost = null;

        base.DisconnectHandler(platformView);
    }

    protected virtual void OnConnectVirtualView()
    {
    }

    protected virtual void OnDisconnectVirtualView()
    {
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        _ = payload;
        if (!string.Equals(eventName, ScrollEventName, StringComparison.Ordinal))
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

        VirtualView.SendScrolled(new ItemsViewScrolledEventArgs
        {
            HorizontalOffset = left,
            VerticalOffset = top
        });
    }

    public static void MapItemsSource(MirageItemsViewHandler<TItemsView> handler, TItemsView itemsView)
    {
        handler.SubscribeToItemsSource(itemsView.ItemsSource);
        handler.RebuildAllContent();
    }

    public static void MapHorizontalScrollBarVisibility(MirageItemsViewHandler<TItemsView> handler, TItemsView itemsView)
    {
        _ = itemsView;
        handler.ApplyScrollStyles();
        handler.RequestRender();
    }

    public static void MapVerticalScrollBarVisibility(MirageItemsViewHandler<TItemsView> handler, TItemsView itemsView)
    {
        _ = itemsView;
        handler.ApplyScrollStyles();
        handler.RequestRender();
    }

    public static void MapItemTemplate(MirageItemsViewHandler<TItemsView> handler, TItemsView itemsView)
    {
        _ = itemsView;
        handler.RebuildAllContent();
    }

    public static void MapEmptyView(MirageItemsViewHandler<TItemsView> handler, TItemsView itemsView)
    {
        _ = itemsView;
        handler.DisconnectHostedContent(ref handler._emptyViewRealization, handler._emptyHost);
        handler.UpdateEmptyViewState();
        handler.RequestRender();
    }

    public static void MapEmptyViewTemplate(MirageItemsViewHandler<TItemsView> handler, TItemsView itemsView)
    {
        _ = itemsView;
        handler.DisconnectHostedContent(ref handler._emptyViewRealization, handler._emptyHost);
        handler.UpdateEmptyViewState();
        handler.RequestRender();
    }

    public static void MapFlowDirection(MirageItemsViewHandler<TItemsView> handler, TItemsView itemsView)
    {
        _ = itemsView;
        handler.ApplyFlowDirection();
        handler.RequestRender();
    }

    public static void MapIsVisible(MirageItemsViewHandler<TItemsView> handler, TItemsView itemsView)
    {
        MirageViewMapper.MapVisibility(handler, itemsView);
    }

    public static void MapItemsUpdatingScrollMode(MirageItemsViewHandler<TItemsView> handler, TItemsView itemsView)
    {
        handler.ReportUnsupportedItemsUpdatingScrollMode(itemsView.ItemsUpdatingScrollMode);
        handler.ApplyItemsUpdatingScrollModeAfterMutation();
    }

    public static void MapIsEnabled(MirageItemsViewHandler<TItemsView> handler, TItemsView itemsView)
    {
        _ = itemsView;
        handler.ApplySelectionInteractivity();
        handler.RequestRender();
    }

    protected void RebuildHeaderContent()
    {
        if (_headerHost is null)
        {
            return;
        }

        var structuredItemsView = GetStructuredItemsView();
        DisconnectHostedContent(ref _headerRealization, _headerHost);

        var headerView = CreateStructuredView(
            structuredItemsView?.Header,
            structuredItemsView?.HeaderTemplate,
            nameof(StructuredItemsView.HeaderTemplate),
            fallbackText: "Header");

        if (headerView is null)
        {
            _headerHost.SetStyle("display", "none");
            return;
        }

        _headerHost.SetStyle("display", "block");
        _headerRealization = RealizeHostedView(_headerHost, headerView, "header");
    }

    protected void RebuildFooterContent()
    {
        if (_footerHost is null)
        {
            return;
        }

        var structuredItemsView = GetStructuredItemsView();
        DisconnectHostedContent(ref _footerRealization, _footerHost);

        var footerView = CreateStructuredView(
            structuredItemsView?.Footer,
            structuredItemsView?.FooterTemplate,
            nameof(StructuredItemsView.FooterTemplate),
            fallbackText: "Footer");

        if (footerView is null)
        {
            _footerHost.SetStyle("display", "none");
            return;
        }

        _footerHost.SetStyle("display", "block");
        _footerRealization = RealizeHostedView(_footerHost, footerView, "footer");
    }

    protected void ApplyItemsLayout()
    {
        if (_itemsHost is null)
        {
            return;
        }

        var structuredItemsView = GetStructuredItemsView();
        var layout = structuredItemsView?.ItemsLayout;

        _itemsHost.SetStyle("display", "flex");
        _itemsHost.SetStyle("flexDirection", "column");
        _itemsHost.SetStyle("alignItems", "stretch");
        _itemsHost.SetStyle("rowGap", null);
        _itemsHost.SetStyle("columnGap", null);

        if (layout is LinearItemsLayout linearLayout)
        {
            var spacing = ResolveItemSpacing(linearLayout.ItemSpacing);
            var isHorizontal = linearLayout.Orientation == ItemsLayoutOrientation.Horizontal;
            _itemsHost.SetStyle("flexDirection", isHorizontal ? "row" : "column");
            if (isHorizontal)
            {
                _itemsHost.SetStyle("columnGap", MirageViewMapper.ToPx(spacing));
            }
            else
            {
                _itemsHost.SetStyle("rowGap", MirageViewMapper.ToPx(spacing));
            }

            return;
        }

        if (layout is not null)
        {
            ReportUnsupported(nameof(StructuredItemsView.ItemsLayout), layout, "fallback=LinearItemsLayout(vertical)");
        }
    }

    protected void ApplyItemSizingStrategy()
    {
        var structuredItemsView = GetStructuredItemsView();
        if (structuredItemsView is null)
        {
            return;
        }

        ReportUnsupported(
            nameof(StructuredItemsView.ItemSizingStrategy),
            structuredItemsView.ItemSizingStrategy,
            "fallback=MeasureFirstItem");
    }

    protected void ApplyGroupingProjection()
    {
        var groupableItemsView = GetGroupableItemsView();
        if (groupableItemsView is null)
        {
            return;
        }

        if (groupableItemsView.IsGrouped)
        {
            ReportUnsupported(nameof(GroupableItemsView.IsGrouped), groupableItemsView.IsGrouped, "fallback=flat-items");
        }

        if (groupableItemsView.GroupHeaderTemplate is not null)
        {
            ReportUnsupported(
                nameof(GroupableItemsView.GroupHeaderTemplate),
                groupableItemsView.GroupHeaderTemplate,
                "fallback=ignore-group-templates");
        }

        if (groupableItemsView.GroupFooterTemplate is not null)
        {
            ReportUnsupported(
                nameof(GroupableItemsView.GroupFooterTemplate),
                groupableItemsView.GroupFooterTemplate,
                "fallback=ignore-group-templates");
        }
    }

    protected void ApplyReorderProjection()
    {
        var reorderableItemsView = GetReorderableItemsView();
        if (reorderableItemsView is null)
        {
            return;
        }

        if (reorderableItemsView.CanReorderItems)
        {
            ReportUnsupported(nameof(ReorderableItemsView.CanReorderItems), reorderableItemsView.CanReorderItems, "fallback=no-reorder");
        }
    }

    protected void OnSelectionMapperChanged()
    {
        if (IsUpdatingSelection)
        {
            return;
        }

        EnsureSelectionModeCoherence();
        NormalizeSelectionForCurrentItems();
        ApplySelectionInteractivity();
        ApplySelectionFromVirtualView();
        RequestRender();
    }

    protected void SubscribeSelectionChanged(SelectableItemsView selectableItemsView)
    {
        _selectionChangedHandler ??= OnSelectionChanged;
        selectableItemsView.SelectionChanged += _selectionChangedHandler;
    }

    protected void UnsubscribeSelectionChanged(SelectableItemsView selectableItemsView)
    {
        if (_selectionChangedHandler is null)
        {
            return;
        }

        selectableItemsView.SelectionChanged -= _selectionChangedHandler;
    }

    protected SelectableItemsView? GetSelectableItemsView()
    {
        return VirtualView as SelectableItemsView;
    }

    protected StructuredItemsView? GetStructuredItemsView()
    {
        return VirtualView as StructuredItemsView;
    }

    protected GroupableItemsView? GetGroupableItemsView()
    {
        return VirtualView as GroupableItemsView;
    }

    protected ReorderableItemsView? GetReorderableItemsView()
    {
        return VirtualView as ReorderableItemsView;
    }

    private void EnsureHostTopology(MiragePlatformView platformView)
    {
        platformView.ClearChildren();

        _contentHost = CreateHostNode("content");
        _contentHost.SetStyle("display", "flex");
        _contentHost.SetStyle("flexDirection", "column");
        _contentHost.SetStyle("alignItems", "stretch");
        _contentHost.SetStyle("minHeight", "100%");

        _headerHost = CreateHostNode("header");
        _headerHost.SetStyle("display", "none");

        _itemsHost = CreateHostNode("items");
        _itemsHost.SetStyle("display", "flex");
        _itemsHost.SetStyle("flexDirection", "column");
        _itemsHost.SetStyle("alignItems", "stretch");

        _footerHost = CreateHostNode("footer");
        _footerHost.SetStyle("display", "none");

        _emptyHost = CreateHostNode("empty");
        _emptyHost.SetStyle("display", "none");

        _contentHost.AddChild(_headerHost);
        _contentHost.AddChild(_itemsHost);
        _contentHost.AddChild(_footerHost);

        platformView.AddChild(_contentHost);
        platformView.AddChild(_emptyHost);
    }

    private static MiragePlatformView CreateHostNode(string role)
    {
        var node = MiragePlatformView.CreateControl(ComponentKind.ContentContainer);
        EnsureNodeId(node, $"host-{role}");
        node.SetProperty("CollectionViewHostRole", role);
        node.SetStyle("boxSizing", "border-box");
        node.SetStyle("position", "relative");
        node.SetStyle("minWidth", "0");
        node.SetStyle("minHeight", "0");
        return node;
    }

    private void ApplyRootStyles(MiragePlatformView platformView)
    {
        platformView.SetStyle("display", "block");
        platformView.SetStyle("boxSizing", "border-box");
        platformView.SetStyle("minWidth", "0");
        platformView.SetStyle("minHeight", "0");
        platformView.SetStyle("overflowX", "hidden");
        platformView.SetStyle("overflowY", "auto");
        platformView.SetEvent(ScrollEventName, enabled: true);
        platformView.SetProperty("CollectionViewHostRole", "scroll");
    }

    private void RebuildAllContent()
    {
        using var _ = BeginRenderBatch();

        DisconnectAllItems();
        DisconnectHostedContent(ref _emptyViewRealization, _emptyHost);

        _itemsSnapshot.Clear();
        _itemsSnapshot.AddRange(SnapshotItems(VirtualView.ItemsSource));

        RebuildHeaderContent();
        RebuildFooterContent();

        var realizations = CreateItemRealizations(_itemsSnapshot);
        if (realizations is not null)
        {
            foreach (var realization in realizations)
            {
                _itemsHost?.AddChild(realization.Node);
                _itemRealizations.Add(realization);
            }
        }

        ApplyItemIndexAttributes();
        UpdateEmptyViewState();
        EnsureSelectionModeCoherence();
        NormalizeSelectionForCurrentItems();
        ApplySelectionInteractivity();
        ApplySelectionFromVirtualView();
        RequestRender();
    }

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _ = sender;
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (!TryApplyAdd(e))
                {
                    RebuildAllContent();
                }

                break;
            case NotifyCollectionChangedAction.Remove:
                if (!TryApplyRemove(e))
                {
                    RebuildAllContent();
                }

                break;
            case NotifyCollectionChangedAction.Move:
                if (!TryApplyMove(e))
                {
                    RebuildAllContent();
                }

                break;
            case NotifyCollectionChangedAction.Replace:
                if (!TryApplyReplace(e))
                {
                    RebuildAllContent();
                }

                break;
            case NotifyCollectionChangedAction.Reset:
            default:
                RebuildAllContent();
                break;
        }
    }

    private bool TryApplyAdd(NotifyCollectionChangedEventArgs e)
    {
        if (_itemsHost is null || e.NewItems is null || e.NewItems.Count == 0)
        {
            return false;
        }

        var insertionIndex = e.NewStartingIndex;
        if (insertionIndex < 0 || insertionIndex > _itemRealizations.Count)
        {
            return false;
        }

        var newItems = ToObjectList(e.NewItems);
        var newRealizations = CreateItemRealizations(newItems);
        if (newRealizations is null)
        {
            return false;
        }

        for (var i = 0; i < newItems.Count; i++)
        {
            var targetIndex = insertionIndex + i;
            _itemsSnapshot.Insert(targetIndex, newItems[i]);

            var realization = newRealizations[i];
            if (targetIndex >= _itemRealizations.Count)
            {
                _itemsHost.AddChild(realization.Node);
                _itemRealizations.Add(realization);
                continue;
            }

            _itemsHost.InsertChild(targetIndex, realization.Node);
            _itemRealizations.Insert(targetIndex, realization);
        }

        FinalizeCollectionMutation();
        return true;
    }

    private bool TryApplyRemove(NotifyCollectionChangedEventArgs e)
    {
        var removalIndex = e.OldStartingIndex;
        if (removalIndex < 0 || removalIndex >= _itemRealizations.Count)
        {
            return false;
        }

        var countToRemove = e.OldItems?.Count ?? 1;
        if (countToRemove <= 0 || removalIndex + countToRemove > _itemRealizations.Count)
        {
            return false;
        }

        for (var i = 0; i < countToRemove; i++)
        {
            var realization = _itemRealizations[removalIndex];
            DisconnectItem(realization, removeFromDom: true);
            _itemRealizations.RemoveAt(removalIndex);
            _itemsSnapshot.RemoveAt(removalIndex);
        }

        FinalizeCollectionMutation();
        return true;
    }

    private bool TryApplyMove(NotifyCollectionChangedEventArgs e)
    {
        if (_itemsHost is null)
        {
            return false;
        }

        var oldStartingIndex = e.OldStartingIndex;
        var newStartingIndex = e.NewStartingIndex;
        var moveCount = e.OldItems?.Count ?? 1;

        if (oldStartingIndex < 0 || newStartingIndex < 0 || moveCount <= 0)
        {
            return false;
        }

        if (oldStartingIndex + moveCount > _itemRealizations.Count || newStartingIndex > _itemRealizations.Count)
        {
            return false;
        }

        if (oldStartingIndex == newStartingIndex)
        {
            return true;
        }

        var movingSnapshots = _itemsSnapshot.GetRange(oldStartingIndex, moveCount);
        var movingRealizations = _itemRealizations.GetRange(oldStartingIndex, moveCount);

        _itemsSnapshot.RemoveRange(oldStartingIndex, moveCount);
        _itemRealizations.RemoveRange(oldStartingIndex, moveCount);

        var insertionIndex = Math.Min(Math.Max(0, newStartingIndex), _itemRealizations.Count);
        _itemsSnapshot.InsertRange(insertionIndex, movingSnapshots);
        _itemRealizations.InsertRange(insertionIndex, movingRealizations);

        ReorderItemsDomFromRealizations();
        FinalizeCollectionMutation();
        return true;
    }

    private bool TryApplyReplace(NotifyCollectionChangedEventArgs e)
    {
        if (_itemsHost is null)
        {
            return false;
        }

        var oldStartingIndex = e.OldStartingIndex >= 0 ? e.OldStartingIndex : e.NewStartingIndex;
        if (oldStartingIndex < 0)
        {
            return false;
        }

        var oldCount = e.OldItems?.Count ?? 0;
        var newItems = e.NewItems is null ? [] : ToObjectList(e.NewItems);
        var newRealizations = CreateItemRealizations(newItems);
        if (newRealizations is null)
        {
            return false;
        }

        if (oldStartingIndex + oldCount > _itemRealizations.Count)
        {
            DisconnectPendingRealizations(newRealizations);
            return false;
        }

        for (var i = 0; i < oldCount; i++)
        {
            var realization = _itemRealizations[oldStartingIndex];
            DisconnectItem(realization, removeFromDom: true);
            _itemRealizations.RemoveAt(oldStartingIndex);
            _itemsSnapshot.RemoveAt(oldStartingIndex);
        }

        for (var i = 0; i < newItems.Count; i++)
        {
            var insertionIndex = oldStartingIndex + i;
            var realization = newRealizations[i];

            _itemsSnapshot.Insert(insertionIndex, newItems[i]);
            _itemRealizations.Insert(insertionIndex, realization);
            _itemsHost.InsertChild(insertionIndex, realization.Node);
        }

        FinalizeCollectionMutation();
        return true;
    }

    private void FinalizeCollectionMutation()
    {
        ApplyItemIndexAttributes();
        UpdateEmptyViewState();
        EnsureSelectionModeCoherence();
        NormalizeSelectionForCurrentItems();
        ApplySelectionInteractivity();
        ApplySelectionFromVirtualView();
        ApplyItemsUpdatingScrollModeAfterMutation();
        RequestRender();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        if (IsUpdatingSelection)
        {
            return;
        }

        EnsureSelectionModeCoherence();
        NormalizeSelectionForCurrentItems();
        ApplySelectionFromVirtualView();
        RequestRender();
    }

    private void OnScrollToRequested(object? sender, ScrollToRequestEventArgs e)
    {
        _ = sender;
        ApplyScrollToRequest(e);
    }

    private void ApplyScrollToRequest(ScrollToRequestEventArgs request)
    {
        if (Coordinator is null || _itemRealizations.Count == 0 || string.IsNullOrWhiteSpace(PlatformView.NodeId))
        {
            return;
        }

        var targetIndex = ResolveScrollToTargetIndex(request);
        if (targetIndex < 0 || targetIndex >= _itemRealizations.Count)
        {
            return;
        }

        var targetNodeId = _itemRealizations[targetIndex].Node.NodeId;
        if (string.IsNullOrWhiteSpace(targetNodeId))
        {
            return;
        }

        var isHorizontalOrientation = ResolveItemsLayoutOrientation() == ItemsLayoutOrientation.Horizontal;

        if (!Coordinator.TryScrollNodeChildIntoView(
                PlatformView.NodeId,
                targetNodeId,
                (int)request.ScrollToPosition,
                isHorizontalOrientation,
                request.IsAnimated,
                out var actualLeft,
                out var actualTop))
        {
            return;
        }

        if (request.IsAnimated)
        {
            return;
        }

        VirtualView.SendScrolled(new ItemsViewScrolledEventArgs
        {
            HorizontalOffset = actualLeft,
            VerticalOffset = actualTop
        });
    }

    private int ResolveScrollToTargetIndex(ScrollToRequestEventArgs request)
    {
        if (GetGroupableItemsView()?.IsGrouped == true && (request.Group is not null || request.GroupIndex >= 0))
        {
            ReportUnsupported(nameof(GroupableItemsView.IsGrouped), request.Group, "fallback=ignore-group-scroll-target");
        }

        if (request.Mode == ScrollToMode.Position)
        {
            return request.Index;
        }

        return FindFirstIndexForItem(request.Item);
    }

    private void ApplyItemsUpdatingScrollModeAfterMutation()
    {
        switch (VirtualView.ItemsUpdatingScrollMode)
        {
            case ItemsUpdatingScrollMode.KeepScrollOffset:
                return;
            case ItemsUpdatingScrollMode.KeepLastItemInView:
                ScrollLastItemIntoView();
                return;
            case ItemsUpdatingScrollMode.KeepItemsInView:
                ReportUnsupportedItemsUpdatingScrollMode(ItemsUpdatingScrollMode.KeepItemsInView);
                return;
        }
    }

    private void ScrollLastItemIntoView()
    {
        if (_itemRealizations.Count == 0 || Coordinator is null || string.IsNullOrWhiteSpace(PlatformView.NodeId))
        {
            return;
        }

        var lastNodeId = _itemRealizations[^1].Node.NodeId;
        if (string.IsNullOrWhiteSpace(lastNodeId))
        {
            return;
        }

        var isHorizontalOrientation = ResolveItemsLayoutOrientation() == ItemsLayoutOrientation.Horizontal;
        if (!Coordinator.TryScrollNodeChildIntoView(
                PlatformView.NodeId,
                lastNodeId,
                (int)ScrollToPosition.End,
                isHorizontalOrientation,
                isAnimated: false,
                out var actualLeft,
                out var actualTop))
        {
            return;
        }

        VirtualView.SendScrolled(new ItemsViewScrolledEventArgs
        {
            HorizontalOffset = actualLeft,
            VerticalOffset = actualTop
        });
    }

    private List<CollectionItemRealization>? CreateItemRealizations(IReadOnlyList<object?> items)
    {
        var realizations = new List<CollectionItemRealization>(items.Count);
        foreach (var item in items)
        {
            var realization = CreateItemRealization(item);
            if (realization is null)
            {
                DisconnectPendingRealizations(realizations);
                return null;
            }

            realizations.Add(realization);
        }

        return realizations;
    }

    private CollectionItemRealization? CreateItemRealization(object? item)
    {
        var itemView = CreateItemView(item);
        if (itemView is BindableObject bindableView)
        {
            bindableView.BindingContext = item;
        }

        var itemNode = CreateHostNode("item");
        itemNode.SetStyle("display", "block");
        itemNode.SetStyle("minWidth", "0");
        itemNode.SetStyle("position", "relative");

        var itemTarget = new ItemEventTarget(eventName =>
        {
            if (string.Equals(eventName, "click", StringComparison.Ordinal))
            {
                OnItemClicked(itemNode);
            }
        });

        RegisterItemNode(itemNode, itemTarget);

        var platformView = TryResolveViewPlatform(itemView, item, out var itemHandler);
        platformView.SetStyle("position", "relative");
        platformView.SetStyle("left", null);
        platformView.SetStyle("top", null);
        platformView.SetStyle("right", null);
        platformView.SetStyle("bottom", null);
        itemNode.AddChild(platformView);

        return new CollectionItemRealization(item, itemView, itemHandler, itemNode);
    }

    private static void DisconnectPendingRealizations(IEnumerable<CollectionItemRealization> realizations)
    {
        foreach (var realization in realizations)
        {
            MirageHandlerDisconnect.SafeDisconnect(realization.Handler);
        }
    }

    private MiragePlatformView TryResolveViewPlatform(View view, object? fallbackItem, out IElementHandler? handler)
    {
        handler = null;
        if (MauiContext is not null)
        {
            try
            {
                handler = view.ToHandler(MauiContext);
                if (handler.PlatformView is MiragePlatformView platformView)
                {
                    return platformView;
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("IMauiHandlersFactory", StringComparison.Ordinal))
            {
                // Integration-test fallback when a synthetic MauiContext does not provide a handler factory.
            }
        }

        MirageHandlerDisconnect.SafeDisconnect(handler);
        handler = null;
        return CreateFallbackItemPlatformView(fallbackItem);
    }

    private static MiragePlatformView CreateFallbackItemPlatformView(object? item)
    {
        var labelNode = MiragePlatformView.CreateControl(ComponentKind.Label);
        EnsureNodeId(labelNode, "fallback-item");
        labelNode.SetProperty("Text", ResolveFallbackText(item));
        return labelNode;
    }

    private static string ResolveFallbackText(object? item)
    {
        if (item is null)
        {
            return string.Empty;
        }

        var titleProperty = item.GetType().GetProperty("Title");
        if (titleProperty?.PropertyType == typeof(string)
            && titleProperty.GetValue(item) is string title
            && !string.IsNullOrWhiteSpace(title))
        {
            return title;
        }

        return item.ToString() ?? string.Empty;
    }

    private static void EnsureNodeId(MiragePlatformView node, string prefix)
    {
        if (!string.IsNullOrWhiteSpace(node.NodeId))
        {
            return;
        }

        var nextId = Interlocked.Increment(ref s_itemNodeIdCounter);
        node.AssignNodeId($"collection-{prefix}-{nextId:D6}");
    }

    private void RegisterItemNode(MiragePlatformView itemNode, IMirageRuntimeEventTarget itemTarget)
    {
        if (Coordinator is { } coordinator)
        {
            coordinator.Register(itemNode, itemTarget);
            return;
        }

        if (!string.IsNullOrWhiteSpace(itemNode.NodeId))
        {
            return;
        }

        var localId = Interlocked.Increment(ref s_itemNodeIdCounter);
        itemNode.AssignNodeId($"collection-item-{localId:D4}");
    }

    private View CreateItemView(object? item)
    {
        var template = ResolveTemplateForItem(item);
        if (template is not null)
        {
            var templateContent = template.CreateContent();
            if (TryExtractView(templateContent, out var templatedView))
            {
                return templatedView;
            }

            ReportUnsupported(nameof(ItemsView.ItemTemplate), templateContent, "fallback=label");
        }

        return new Label
        {
            Text = item?.ToString() ?? string.Empty
        };
    }

    private DataTemplate? ResolveTemplateForItem(object? item)
    {
        var template = VirtualView.ItemTemplate;
        if (template is null)
        {
            return null;
        }

        if (template is DataTemplateSelector selector)
        {
            return selector.SelectTemplate(item, VirtualView);
        }

        return template;
    }

    private void OnItemClicked(MiragePlatformView itemNode)
    {
        var selectableItemsView = GetSelectableItemsView();
        if (selectableItemsView is null
            || !selectableItemsView.IsEnabled
            || selectableItemsView.SelectionMode == SelectionMode.None)
        {
            return;
        }

        var itemIndex = _itemRealizations.FindIndex(realization => ReferenceEquals(realization.Node, itemNode));
        if (itemIndex < 0)
        {
            return;
        }

        _updatingSelectionFromDom = true;
        try
        {
            if (selectableItemsView.SelectionMode == SelectionMode.Single)
            {
                SetSelectedItemFromHandler(_itemsSnapshot[itemIndex]);
            }
            else
            {
                var selectedIndexes = BuildSelectedIndexesFromVirtualView();
                if (!selectedIndexes.Add(itemIndex))
                {
                    selectedIndexes.Remove(itemIndex);
                }

                var selectedItems = selectedIndexes
                    .OrderBy(index => index)
                    .Select(index => _itemsSnapshot[index])
                    .ToList();
                UpdateSelectedItemsFromHandler(selectedItems);
            }
        }
        finally
        {
            _updatingSelectionFromDom = false;
        }

        EnsureSelectionModeCoherence();
        NormalizeSelectionForCurrentItems();
        ApplySelectionFromVirtualView();
        RequestRender();
    }

    private void ApplyFlowDirection()
    {
        var direction = VirtualView.FlowDirection switch
        {
            FlowDirection.LeftToRight => "ltr",
            FlowDirection.RightToLeft => "rtl",
            _ => null
        };

        PlatformView.SetStyle("direction", direction);
    }

    private void ApplyScrollStyles()
    {
        var orientation = ResolveItemsLayoutOrientation();
        var horizontalOverflow = ResolveScrollOverflowStyle(VirtualView.HorizontalScrollBarVisibility);
        var verticalOverflow = ResolveScrollOverflowStyle(VirtualView.VerticalScrollBarVisibility);

        PlatformView.SetStyle("overflowX", orientation == ItemsLayoutOrientation.Horizontal ? horizontalOverflow : "hidden");
        PlatformView.SetStyle("overflowY", orientation == ItemsLayoutOrientation.Horizontal ? "hidden" : verticalOverflow);
    }

    private static string ResolveScrollOverflowStyle(ScrollBarVisibility visibility)
    {
        return visibility switch
        {
            ScrollBarVisibility.Never => "hidden",
            ScrollBarVisibility.Always => "scroll",
            _ => "auto"
        };
    }

    private ItemsLayoutOrientation ResolveItemsLayoutOrientation()
    {
        if (GetStructuredItemsView()?.ItemsLayout is LinearItemsLayout linearLayout)
        {
            return linearLayout.Orientation;
        }

        return ItemsLayoutOrientation.Vertical;
    }

    private static double ResolveItemSpacing(double spacing)
    {
        if (!double.IsFinite(spacing))
        {
            return 0;
        }

        return Math.Max(0, spacing);
    }

    private void ApplySelectionInteractivity()
    {
        var selectableItemsView = GetSelectableItemsView();
        if (selectableItemsView is null)
        {
            return;
        }

        var selectionEnabled = selectableItemsView.IsEnabled && selectableItemsView.SelectionMode != SelectionMode.None;
        foreach (var realization in _itemRealizations)
        {
            realization.Node.SetEvent("click", selectionEnabled);
            realization.Node.SetStyle("cursor", selectionEnabled ? "pointer" : "default");
            realization.Node.SetStyle("pointerEvents", selectableItemsView.IsEnabled ? "auto" : "none");
        }
    }

    private void EnsureSelectionModeCoherence()
    {
        var selectableItemsView = GetSelectableItemsView();
        if (selectableItemsView is null)
        {
            return;
        }

        switch (selectableItemsView.SelectionMode)
        {
            case SelectionMode.None:
                if (selectableItemsView.SelectedItem is not null)
                {
                    SetSelectedItemFromHandler(null);
                }

                if (selectableItemsView.SelectedItems is { Count: > 0 })
                {
                    UpdateSelectedItemsFromHandler(Array.Empty<object?>());
                }

                break;
            case SelectionMode.Single:
                if (selectableItemsView.SelectedItem is null && selectableItemsView.SelectedItems is { Count: > 0 })
                {
                    SetSelectedItemFromHandler(selectableItemsView.SelectedItems[0]);
                }

                break;
            case SelectionMode.Multiple:
                if (selectableItemsView.SelectedItems is null)
                {
                    SetSelectedItemsCollectionFromHandler(new ObservableCollection<object?>());
                }

                break;
        }
    }

    private void NormalizeSelectionForCurrentItems()
    {
        var selectableItemsView = GetSelectableItemsView();
        if (selectableItemsView is null)
        {
            return;
        }

        switch (selectableItemsView.SelectionMode)
        {
            case SelectionMode.None:
                return;
            case SelectionMode.Single:
                NormalizeSingleSelectionForCurrentItems(selectableItemsView);
                return;
            case SelectionMode.Multiple:
                NormalizeMultipleSelectionForCurrentItems(selectableItemsView);
                return;
        }
    }

    private void NormalizeSingleSelectionForCurrentItems(SelectableItemsView selectableItemsView)
    {
        if (selectableItemsView.SelectedItem is not null && FindFirstIndexForItem(selectableItemsView.SelectedItem) < 0)
        {
            SetSelectedItemFromHandler(null);
        }

        if (selectableItemsView.SelectedItems is not { Count: > 0 })
        {
            return;
        }

        var usedIndexes = new bool[_itemsSnapshot.Count];
        if (TryFindMatchingIndex(selectableItemsView.SelectedItems[0], usedIndexes, out var selectedIndex))
        {
            SetSelectedItemFromHandler(_itemsSnapshot[selectedIndex]);
            return;
        }

        SetSelectedItemFromHandler(null);
    }

    private void NormalizeMultipleSelectionForCurrentItems(SelectableItemsView selectableItemsView)
    {
        if (selectableItemsView.SelectedItems is null)
        {
            SetSelectedItemsCollectionFromHandler(new ObservableCollection<object?>());
            return;
        }

        var normalizedSelection = new List<object?>();
        var usedIndexes = new bool[_itemsSnapshot.Count];
        foreach (var selectedItem in selectableItemsView.SelectedItems)
        {
            if (!TryFindMatchingIndex(selectedItem, usedIndexes, out var selectedIndex))
            {
                continue;
            }

            usedIndexes[selectedIndex] = true;
            normalizedSelection.Add(_itemsSnapshot[selectedIndex]);
        }

        if (!SelectionListsReferenceEquivalent(selectableItemsView.SelectedItems, normalizedSelection))
        {
            UpdateSelectedItemsFromHandler(normalizedSelection);
        }

        if (selectableItemsView.SelectedItem is not null && FindFirstIndexForItem(selectableItemsView.SelectedItem) < 0)
        {
            SetSelectedItemFromHandler(null);
        }
    }

    private void ApplySelectionFromVirtualView()
    {
        var selectableItemsView = GetSelectableItemsView();
        if (selectableItemsView is null)
        {
            return;
        }

        var selectedIndexes = BuildSelectedIndexesFromVirtualView();
        for (var index = 0; index < _itemRealizations.Count; index++)
        {
            var selected = selectedIndexes.Contains(index);
            var node = _itemRealizations[index].Node;
            node.SetStyle("outline", selected ? "2px solid rgba(86,57,176,0.35)" : null);
            node.SetStyle("outlineOffset", selected ? "1px" : null);
            node.SetProperty("CollectionViewSelected", selected ? "true" : "false");
        }
    }

    private HashSet<int> BuildSelectedIndexesFromVirtualView()
    {
        var selectableItemsView = GetSelectableItemsView();
        var selectedIndexes = new HashSet<int>();
        if (selectableItemsView is null || _itemsSnapshot.Count == 0 || selectableItemsView.SelectionMode == SelectionMode.None)
        {
            return selectedIndexes;
        }

        var usedIndexes = new bool[_itemsSnapshot.Count];
        if (selectableItemsView.SelectionMode == SelectionMode.Single)
        {
            if (TryFindMatchingIndex(selectableItemsView.SelectedItem, usedIndexes, out var singleIndex))
            {
                selectedIndexes.Add(singleIndex);
            }

            return selectedIndexes;
        }

        if (selectableItemsView.SelectedItems is null)
        {
            return selectedIndexes;
        }

        foreach (var selectedItem in selectableItemsView.SelectedItems)
        {
            if (!TryFindMatchingIndex(selectedItem, usedIndexes, out var selectedIndex))
            {
                continue;
            }

            usedIndexes[selectedIndex] = true;
            selectedIndexes.Add(selectedIndex);
        }

        return selectedIndexes;
    }

    private int FindFirstIndexForItem(object? selectedItem)
    {
        for (var i = 0; i < _itemsSnapshot.Count; i++)
        {
            if (ReferenceEquals(_itemsSnapshot[i], selectedItem))
            {
                return i;
            }
        }

        for (var i = 0; i < _itemsSnapshot.Count; i++)
        {
            if (AreItemsEquivalent(_itemsSnapshot[i], selectedItem))
            {
                return i;
            }
        }

        return -1;
    }

    private bool TryFindMatchingIndex(object? selectedItem, bool[] usedIndexes, out int index)
    {
        if (TryFindMatchingIndex(selectedItem, usedIndexes, preferReferenceMatch: true, out index))
        {
            return true;
        }

        if (TryFindMatchingIndex(selectedItem, usedIndexes, preferReferenceMatch: false, out index))
        {
            return true;
        }

        index = -1;
        return false;
    }

    private bool TryFindMatchingIndex(object? selectedItem, bool[] usedIndexes, bool preferReferenceMatch, out int index)
    {
        for (var i = 0; i < _itemsSnapshot.Count; i++)
        {
            if (usedIndexes[i])
            {
                continue;
            }

            if (preferReferenceMatch)
            {
                if (!ReferenceEquals(_itemsSnapshot[i], selectedItem))
                {
                    continue;
                }
            }
            else if (!AreItemsEquivalent(_itemsSnapshot[i], selectedItem))
            {
                continue;
            }

            index = i;
            return true;
        }

        index = -1;
        return false;
    }

    private static bool AreItemsEquivalent(object? left, object? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        if (left.GetType() != right.GetType())
        {
            return false;
        }

        return Equals(left, right);
    }

    private void SetSelectedItemFromHandler(object? selectedItem)
    {
        var selectableItemsView = GetSelectableItemsView();
        if (selectableItemsView is null)
        {
            return;
        }

        if (ReferenceEquals(selectableItemsView.SelectedItem, selectedItem)
            || (selectableItemsView.SelectedItem is null && selectedItem is null))
        {
            return;
        }

        _updatingSelectionFromHandler = true;
        try
        {
            selectableItemsView.SelectedItem = selectedItem;
        }
        finally
        {
            _updatingSelectionFromHandler = false;
        }
    }

    private void SetSelectedItemsCollectionFromHandler(IList<object?> selectedItems)
    {
        var selectableItemsView = GetSelectableItemsView();
        if (selectableItemsView is null)
        {
            return;
        }

        _updatingSelectionFromHandler = true;
        try
        {
            selectableItemsView.SelectedItems = ToObjectListPreservingNulls(selectedItems);
        }
        finally
        {
            _updatingSelectionFromHandler = false;
        }
    }

    private void UpdateSelectedItemsFromHandler(IList<object?> selectedItems)
    {
        var selectableItemsView = GetSelectableItemsView();
        if (selectableItemsView is null)
        {
            return;
        }

        _updatingSelectionFromHandler = true;
        try
        {
            selectableItemsView.UpdateSelectedItems(ToObjectListPreservingNulls(selectedItems));
        }
        finally
        {
            _updatingSelectionFromHandler = false;
        }
    }

    private static IList<object> ToObjectListPreservingNulls(IList<object?> source)
    {
        var selectedItems = new List<object>(source.Count);
        foreach (var item in source)
        {
            selectedItems.Add(item!);
        }

        return selectedItems;
    }

    private static bool SelectionListsReferenceEquivalent(IList<object>? current, IReadOnlyList<object?> normalized)
    {
        if (current is null)
        {
            return normalized.Count == 0;
        }

        if (current.Count != normalized.Count)
        {
            return false;
        }

        for (var i = 0; i < current.Count; i++)
        {
            if (!ReferenceEquals(current[i], normalized[i]))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsUpdatingSelection => _updatingSelectionFromDom || _updatingSelectionFromHandler;

    private void UpdateEmptyViewState()
    {
        var hasItems = _itemRealizations.Count > 0;

        _contentHost?.SetStyle("display", hasItems ? "flex" : "none");
        _emptyHost?.SetStyle("display", hasItems ? "none" : "block");

        if (hasItems)
        {
            DisconnectHostedContent(ref _emptyViewRealization, _emptyHost);
            return;
        }

        EnsureEmptyView();
    }

    private void EnsureEmptyView()
    {
        if (_emptyViewRealization is not null || _emptyHost is null)
        {
            return;
        }

        var emptyView = CreateEmptyView();
        _emptyViewRealization = RealizeHostedView(_emptyHost, emptyView, "empty");
    }

    private View CreateEmptyView()
    {
        if (VirtualView.EmptyViewTemplate is not null)
        {
            var templateContent = VirtualView.EmptyViewTemplate.CreateContent();
            if (TryExtractView(templateContent, out var templateView))
            {
                templateView.BindingContext = ResolveEmptyTemplateBindingContext();
                return templateView;
            }

            ReportUnsupported(nameof(ItemsView.EmptyViewTemplate), templateContent, "fallback=label");
        }

        if (VirtualView.EmptyView is View configuredView)
        {
            return configuredView;
        }

        var emptyText = VirtualView.EmptyView?.ToString();
        return new Label
        {
            Text = string.IsNullOrWhiteSpace(emptyText) ? "No items" : emptyText
        };
    }

    private object? ResolveEmptyTemplateBindingContext()
    {
        if (VirtualView.EmptyView is not null and not View)
        {
            return VirtualView.EmptyView;
        }

        if (VirtualView.EmptyView is null)
        {
            return VirtualView.BindingContext;
        }

        return null;
    }

    private View? CreateStructuredView(
        object? source,
        DataTemplate? template,
        string templateFeatureName,
        string fallbackText)
    {
        if (template is not null)
        {
            var templateContent = template.CreateContent();
            if (TryExtractView(templateContent, out var templateView))
            {
                templateView.BindingContext = ResolveStructuredTemplateBindingContext(source);
                return templateView;
            }

            ReportUnsupported(templateFeatureName, templateContent, "fallback=label");
        }

        if (source is View sourceView)
        {
            return sourceView;
        }

        if (source is null)
        {
            return null;
        }

        return new Label
        {
            Text = source.ToString() ?? fallbackText
        };
    }

    private object? ResolveStructuredTemplateBindingContext(object? source)
    {
        if (source is null)
        {
            return VirtualView.BindingContext;
        }

        return source is View ? VirtualView.BindingContext : source;
    }

    private HostedViewRealization? RealizeHostedView(MiragePlatformView host, View view, string role)
    {
        if (view is BindableObject bindableView && bindableView.BindingContext is null)
        {
            bindableView.BindingContext = VirtualView.BindingContext;
        }

        var platformView = TryResolveViewPlatform(view, view, out var handler);
        platformView.SetProperty("CollectionViewHostedRole", role);

        host.ClearChildren();
        host.AddChild(platformView);
        return new HostedViewRealization(view, handler, platformView);
    }

    private void DisconnectAllItems()
    {
        foreach (var realization in _itemRealizations)
        {
            DisconnectItem(realization, removeFromDom: false);
        }

        _itemRealizations.Clear();
        _itemsHost?.ClearChildren();
    }

    private void DisconnectItem(CollectionItemRealization realization, bool removeFromDom)
    {
        if (removeFromDom && _itemsHost is not null)
        {
            _itemsHost.RemoveChild(realization.Node);
        }

        Coordinator?.Unregister(realization.Node);
        MirageHandlerDisconnect.SafeDisconnect(realization.Handler);
    }

    private void DisconnectHostedContent(ref HostedViewRealization? realization, MiragePlatformView? host)
    {
        if (realization is null)
        {
            host?.ClearChildren();
            return;
        }

        MirageHandlerDisconnect.SafeDisconnect(realization.Handler);
        realization = null;
        host?.ClearChildren();
    }

    private void SubscribeToItemsSource(IEnumerable? itemsSource)
    {
        UnsubscribeFromItemsSource();
        if (itemsSource is not INotifyCollectionChanged collection || _collectionChangedHandler is null)
        {
            return;
        }

        _collectionChangedSource = collection;
        _collectionChangedSource.CollectionChanged += _collectionChangedHandler;
    }

    private void UnsubscribeFromItemsSource()
    {
        if (_collectionChangedSource is null || _collectionChangedHandler is null)
        {
            _collectionChangedSource = null;
            return;
        }

        _collectionChangedSource.CollectionChanged -= _collectionChangedHandler;
        _collectionChangedSource = null;
    }

    private void ApplyItemIndexAttributes()
    {
        for (var i = 0; i < _itemRealizations.Count; i++)
        {
            _itemRealizations[i].Node.SetProperty("CollectionViewItemIndex", i.ToString(CultureInfo.InvariantCulture));
        }
    }

    private void ReorderItemsDomFromRealizations()
    {
        if (_itemsHost is null)
        {
            return;
        }

        _itemsHost.ClearChildren();
        foreach (var realization in _itemRealizations)
        {
            _itemsHost.AddChild(realization.Node);
        }
    }

    private static IReadOnlyList<object?> SnapshotItems(IEnumerable? itemsSource)
    {
        if (itemsSource is null)
        {
            return Array.Empty<object?>();
        }

        var snapshot = new List<object?>();
        foreach (var item in itemsSource)
        {
            snapshot.Add(item);
        }

        return snapshot;
    }

    private static List<object?> ToObjectList(IList source)
    {
        var values = new List<object?>(source.Count);
        foreach (var value in source)
        {
            values.Add(value);
        }

        return values;
    }

    private void ReportUnsupportedItemsUpdatingScrollMode(ItemsUpdatingScrollMode mode)
    {
        if (mode != ItemsUpdatingScrollMode.KeepItemsInView)
        {
            return;
        }

        ReportUnsupported(
            nameof(ItemsView.ItemsUpdatingScrollMode),
            mode,
            "fallback=KeepScrollOffset");
    }

    protected void ReportUnsupported(string featureName, object? value, string fallback)
    {
        var key = $"{featureName}|{value?.GetType().FullName ?? "<null>"}|{fallback}";
        if (!_reportedUnsupportedKeys.Add(key))
        {
            return;
        }

        MirageDiagnostics.Log(
            "collectionview.feature",
            $"feature={featureName} valueType={value?.GetType().Name ?? "<null>"} fallback={fallback}");
    }

    private static bool TryExtractView(object? templateContent, out View view)
    {
#pragma warning disable CS0618
        switch (templateContent)
        {
            case View directView:
                view = directView;
                return true;
            case ViewCell { View: not null } viewCell:
                view = viewCell.View;
                return true;
            default:
                view = null!;
                return false;
        }
#pragma warning restore CS0618
    }

    private sealed record CollectionItemRealization(
        object? Item,
        View View,
        IElementHandler? Handler,
        MiragePlatformView Node);

    private sealed record HostedViewRealization(
        View View,
        IElementHandler? Handler,
        MiragePlatformView PlatformView);

    private sealed class ItemEventTarget : IMirageRuntimeEventTarget
    {
        private readonly Action<string> _onEvent;

        public ItemEventTarget(Action<string> onEvent)
        {
            _onEvent = onEvent;
        }

        public void OnRuntimeEvent(string eventName, string? payload)
        {
            _ = payload;
            _onEvent(eventName);
        }
    }
}
