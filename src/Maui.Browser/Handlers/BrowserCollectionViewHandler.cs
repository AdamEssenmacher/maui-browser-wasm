using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using Maui.Browser.Dom;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Maui.Browser.Handlers;

public sealed class BrowserCollectionViewHandler : BrowserViewHandler<CollectionView>
{
    public static IPropertyMapper<CollectionView, BrowserCollectionViewHandler> Mapper =
        new PropertyMapper<CollectionView, BrowserCollectionViewHandler>(BrowserViewMapper.CommonViewMapper)
        {
            [nameof(ItemsView.ItemsSource)] = MapItemsSource,
            [nameof(ItemsView.ItemTemplate)] = MapItemTemplate,
            [nameof(StructuredItemsView.ItemsLayout)] = MapItemsLayout,
            [nameof(ItemsView.EmptyView)] = MapEmptyView,
            [nameof(ItemsView.EmptyViewTemplate)] = MapEmptyViewTemplate,
            [nameof(StructuredItemsView.Header)] = MapHeader,
            [nameof(StructuredItemsView.HeaderTemplate)] = MapHeaderTemplate,
            [nameof(StructuredItemsView.Footer)] = MapFooter,
            [nameof(StructuredItemsView.FooterTemplate)] = MapFooterTemplate,
            [nameof(SelectableItemsView.SelectionMode)] = MapSelectionMode,
            [nameof(SelectableItemsView.SelectedItem)] = MapSelectedItem,
            [nameof(SelectableItemsView.SelectedItems)] = MapSelectedItems
        };

    public static CommandMapper<CollectionView, BrowserCollectionViewHandler> CommandMapper =
        new(BrowserViewMapper.CommonViewCommandMapper)
        {
            [nameof(ItemsView.ScrollTo)] = MapScrollTo
        };

    private readonly List<object?> _itemsSnapshot = [];
    private readonly List<CollectionItemRealization> _itemRealizations = [];

    private NotifyCollectionChangedEventHandler? _collectionChangedHandler;
    private INotifyCollectionChanged? _collectionChangedSource;
    private EventHandler<SelectionChangedEventArgs>? _selectionChangedHandler;
    private EventHandler<ScrollToRequestEventArgs>? _scrollToRequestedHandler;

    private DomElement? _scrollHost;
    private DomElement? _contentHost;
    private DomElement? _headerHost;
    private DomElement? _itemsHost;
    private DomElement? _footerHost;
    private DomElement? _emptyHost;
    private HostedViewRealization? _headerRealization;
    private HostedViewRealization? _footerRealization;
    private HostedViewRealization? _emptyViewRealization;

    private bool _updatingSelectionFromDom;
    private bool _updatingSelectionFromHandler;

#if DEBUG
    private static readonly HashSet<string> UnsupportedWarnings = [];
#endif

    public BrowserCollectionViewHandler()
        : base(Mapper, CommandMapper)
    {
    }

    protected override DomElement CreatePlatformView() => DomElement.Create("div");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Style("overflow", "hidden");
        platformView.Style("boxSizing", "border-box");

        _scrollHost = DomElement.Create("div");
        _scrollHost.Attr("data-maui-cv-scroll-host", "true");
        _scrollHost.Style("boxSizing", "border-box");
        _scrollHost.Style("overflowY", "auto");
        _scrollHost.Style("overflowX", "hidden");

        _contentHost = DomElement.Create("div");
        _contentHost.Attr("data-maui-cv-content-host", "true");
        _contentHost.Style("display", "flex");
        _contentHost.Style("flexDirection", "column");
        _contentHost.Style("alignItems", "stretch");
        _contentHost.Style("boxSizing", "border-box");

        _headerHost = DomElement.Create("div");
        _headerHost.Attr("data-maui-cv-header-host", "true");
        _headerHost.Style("boxSizing", "border-box");

        _itemsHost = DomElement.Create("div");
        _itemsHost.Attr("data-maui-cv-items-host", "true");
        _itemsHost.Style("display", "flex");
        _itemsHost.Style("flexDirection", "column");
        _itemsHost.Style("alignItems", "stretch");
        _itemsHost.Style("boxSizing", "border-box");

        _footerHost = DomElement.Create("div");
        _footerHost.Attr("data-maui-cv-footer-host", "true");
        _footerHost.Style("boxSizing", "border-box");

        _emptyHost = DomElement.Create("div");
        _emptyHost.Attr("data-maui-cv-empty-host", "true");
        _emptyHost.Style("boxSizing", "border-box");
        _emptyHost.Style("overflow", "auto");
        _emptyHost.Style("display", "none");

        _contentHost.Append(_headerHost);
        _contentHost.Append(_itemsHost);
        _contentHost.Append(_footerHost);
        _scrollHost.Append(_contentHost);
        platformView.Append(_scrollHost);
        platformView.Append(_emptyHost);

        _collectionChangedHandler ??= OnItemsSourceCollectionChanged;
        _selectionChangedHandler ??= OnSelectionChanged;
        _scrollToRequestedHandler ??= OnScrollToRequested;

        SubscribeToItemsSource(VirtualView.ItemsSource);
        VirtualView.SelectionChanged += _selectionChangedHandler;
        VirtualView.ScrollToRequested += _scrollToRequestedHandler;

        ApplyItemsLayout(VirtualView);
        RebuildAllContent();
        EnsureSelectionModeCoherence();

        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(DomElement platformView)
    {
        UnsubscribeFromItemsSource();

        if (_selectionChangedHandler is not null)
        {
            VirtualView.SelectionChanged -= _selectionChangedHandler;
        }

        if (_scrollToRequestedHandler is not null)
        {
            VirtualView.ScrollToRequested -= _scrollToRequestedHandler;
        }

        DisconnectAllItems();
        DisconnectHostedContent(ref _headerRealization, _headerHost);
        DisconnectHostedContent(ref _footerRealization, _footerHost);
        DisconnectHostedContent(ref _emptyViewRealization, _emptyHost);

        _itemsSnapshot.Clear();
        platformView.ClearChildren();
        _scrollHost = null;
        _contentHost = null;
        _headerHost = null;
        _itemsHost = null;
        _footerHost = null;
        _emptyHost = null;

        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        const double defaultWidth = 260;
        const double defaultHeight = 220;

        return new Size(
            ResolveDesiredDimension(widthConstraint, defaultWidth),
            ResolveDesiredDimension(heightConstraint, defaultHeight));
    }

    public override void PlatformArrange(Rect frame)
    {
        base.PlatformArrange(frame);

        var width = Math.Max(0, frame.Width);
        var height = Math.Max(0, frame.Height);

        if (_scrollHost is not null)
        {
            BrowserViewMapper.ApplyFrame(_scrollHost, new Rect(0, 0, width, height));
        }

        if (_emptyHost is not null)
        {
            BrowserViewMapper.ApplyFrame(_emptyHost, new Rect(0, 0, width, height));
        }

        if (_contentHost is not null)
        {
            _contentHost.Style("position", "relative");
            _contentHost.Style("left", null);
            _contentHost.Style("top", null);
            _contentHost.Style("width", null);
            _contentHost.Style("height", null);
        }

        if (_itemsHost is not null)
        {
            _itemsHost.Style("position", "relative");
            _itemsHost.Style("left", null);
            _itemsHost.Style("top", null);
            _itemsHost.Style("width", null);
            _itemsHost.Style("height", null);
        }
    }

    public static void MapItemsSource(BrowserCollectionViewHandler handler, CollectionView collectionView)
    {
        handler.SubscribeToItemsSource(collectionView.ItemsSource);
        handler.RebuildAllContent();
    }

    public static void MapItemTemplate(BrowserCollectionViewHandler handler, CollectionView collectionView)
    {
        handler.RebuildAllContent();
    }

    public static void MapItemsLayout(BrowserCollectionViewHandler handler, CollectionView collectionView)
    {
        handler.ApplyItemsLayout(collectionView);
        handler.RebuildAllContent();
    }

    public static void MapEmptyView(BrowserCollectionViewHandler handler, CollectionView collectionView)
    {
        handler.DisconnectHostedContent(ref handler._emptyViewRealization, handler._emptyHost);
        handler.UpdateEmptyViewState();
    }

    public static void MapEmptyViewTemplate(BrowserCollectionViewHandler handler, CollectionView collectionView)
    {
        handler.DisconnectHostedContent(ref handler._emptyViewRealization, handler._emptyHost);
        handler.UpdateEmptyViewState();
    }

    public static void MapHeader(BrowserCollectionViewHandler handler, CollectionView collectionView)
    {
        handler.RebuildHeaderContent();
    }

    public static void MapHeaderTemplate(BrowserCollectionViewHandler handler, CollectionView collectionView)
    {
        handler.RebuildHeaderContent();
    }

    public static void MapFooter(BrowserCollectionViewHandler handler, CollectionView collectionView)
    {
        handler.RebuildFooterContent();
    }

    public static void MapFooterTemplate(BrowserCollectionViewHandler handler, CollectionView collectionView)
    {
        handler.RebuildFooterContent();
    }

    public static void MapSelectionMode(BrowserCollectionViewHandler handler, CollectionView collectionView)
    {
        if (handler.IsUpdatingSelection)
        {
            return;
        }

        handler.EnsureSelectionModeCoherence();
        handler.NormalizeSelectionForCurrentItems();
        handler.ApplySelectionFromVirtualView();
    }

    public static void MapSelectedItem(BrowserCollectionViewHandler handler, CollectionView collectionView)
    {
        if (handler.IsUpdatingSelection)
        {
            return;
        }

        handler.EnsureSelectionModeCoherence();
        handler.NormalizeSelectionForCurrentItems();
        handler.ApplySelectionFromVirtualView();
    }

    public static void MapSelectedItems(BrowserCollectionViewHandler handler, CollectionView collectionView)
    {
        if (handler.IsUpdatingSelection)
        {
            return;
        }

        handler.EnsureSelectionModeCoherence();
        handler.NormalizeSelectionForCurrentItems();
        handler.ApplySelectionFromVirtualView();
    }

    public static void MapScrollTo(BrowserCollectionViewHandler handler, CollectionView collectionView, object? args)
    {
        if (args is ScrollToRequestEventArgs request)
        {
            handler.ApplyScrollToRequest(request);
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ApplySelectionFromVirtualView();
    }

    private void OnScrollToRequested(object? sender, ScrollToRequestEventArgs e)
    {
        ApplyScrollToRequest(e);
    }

    private void ApplyScrollToRequest(ScrollToRequestEventArgs request)
    {
        if (_scrollHost is null)
        {
            return;
        }

        var targetIndex = ResolveScrollToTargetIndex(request);
        if (targetIndex < 0 || targetIndex >= _itemRealizations.Count)
        {
            return;
        }

        var targetRealization = _itemRealizations[targetIndex];
        _scrollHost.ScrollChildIntoView(targetRealization.PlatformView, (int)request.ScrollToPosition);

        var (left, top) = _scrollHost.GetScrollPosition();
        VirtualView.SendScrolled(new ItemsViewScrolledEventArgs
        {
            HorizontalOffset = left,
            VerticalOffset = top
        });
    }

    private int ResolveScrollToTargetIndex(ScrollToRequestEventArgs request)
    {
        if (request.Mode == ScrollToMode.Position)
        {
            return request.Index;
        }

        return FindFirstIndexForItem(request.Item);
    }

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
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
                _itemsHost.Append(realization.PlatformView);
                _itemRealizations.Add(realization);
                continue;
            }

            _itemsHost.Insert(targetIndex, realization.PlatformView);
            _itemRealizations.Insert(targetIndex, realization);
        }

        ApplyItemIndexAttributes();
        UpdateEmptyViewState();
        NormalizeSelectionForCurrentItems();
        ApplySelectionFromVirtualView();
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

            if (removalIndex < _itemsSnapshot.Count)
            {
                _itemsSnapshot.RemoveAt(removalIndex);
            }
        }

        ApplyItemIndexAttributes();
        UpdateEmptyViewState();
        NormalizeSelectionForCurrentItems();
        ApplySelectionFromVirtualView();
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
        ApplyItemIndexAttributes();
        UpdateEmptyViewState();
        NormalizeSelectionForCurrentItems();
        ApplySelectionFromVirtualView();
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
            _itemsHost.Insert(insertionIndex, realization.PlatformView);
        }

        ApplyItemIndexAttributes();
        UpdateEmptyViewState();
        NormalizeSelectionForCurrentItems();
        ApplySelectionFromVirtualView();
        return true;
    }

    private void RebuildAllContent()
    {
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
                _itemsHost?.Append(realization.PlatformView);
                _itemRealizations.Add(realization);
            }
        }

        ApplyItemIndexAttributes();
        UpdateEmptyViewState();
        NormalizeSelectionForCurrentItems();
        ApplySelectionFromVirtualView();
    }

    private void RebuildHeaderContent()
    {
        var host = _headerHost;
        if (host is null)
        {
            return;
        }

        DisconnectHostedContent(ref _headerRealization, host);
        var headerView = CreateStructuredView(
            VirtualView.Header,
            VirtualView.HeaderTemplate,
            nameof(StructuredItemsView.HeaderTemplate),
            fallbackText: "Header");

        if (headerView is null)
        {
            host.Style("display", "none");
            return;
        }

        host.Style("display", "block");
        _headerRealization = RealizeHostedView(host, headerView, "data-maui-cv-header");
    }

    private void RebuildFooterContent()
    {
        var host = _footerHost;
        if (host is null)
        {
            return;
        }

        DisconnectHostedContent(ref _footerRealization, host);
        var footerView = CreateStructuredView(
            VirtualView.Footer,
            VirtualView.FooterTemplate,
            nameof(StructuredItemsView.FooterTemplate),
            fallbackText: "Footer");

        if (footerView is null)
        {
            host.Style("display", "none");
            return;
        }

        host.Style("display", "block");
        _footerRealization = RealizeHostedView(host, footerView, "data-maui-cv-footer");
    }

    private View? CreateStructuredView(object? source, DataTemplate? template, string templateFeatureName, string fallbackText)
    {
        if (template is not null)
        {
            var templateContent = template.CreateContent();
            if (TryExtractView(templateContent, out var templatedView))
            {
                templatedView.BindingContext = ResolveStructuredTemplateBindingContext(source);
                return templatedView;
            }

            ReportUnsupported(templateFeatureName, templateContent);
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
            FontSize = 12,
            Padding = new Thickness(8, 6),
            Text = source.ToString() ?? fallbackText,
            TextColor = Colors.DarkSlateGray
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

    private HostedViewRealization? RealizeHostedView(DomElement host, View view, string markerAttribute)
    {
        if (MauiContext is null)
        {
            return null;
        }

        var handler = view.ToHandler(MauiContext);
        if (handler.PlatformView is not DomElement platformView)
        {
            handler.DisconnectHandler();
            return null;
        }

        platformView.Attr(markerAttribute, "true");
        host.ClearChildren();
        host.Append(platformView);
        return new HostedViewRealization(view, handler, platformView);
    }

    private List<CollectionItemRealization>? CreateItemRealizations(IReadOnlyList<object?> items)
    {
        if (MauiContext is null)
        {
            return null;
        }

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

    private static void DisconnectPendingRealizations(IEnumerable<CollectionItemRealization> realizations)
    {
        foreach (var realization in realizations)
        {
            if (realization.ActivationSubscriptionId != 0)
            {
                DomElement.Off(realization.ActivationSubscriptionId);
            }

            realization.Handler.DisconnectHandler();
        }
    }

    private CollectionItemRealization? CreateItemRealization(object? item)
    {
        if (MauiContext is null)
        {
            return null;
        }

        var itemView = CreateItemView(item);
        itemView.BindingContext = item;

        var itemHandler = itemView.ToHandler(MauiContext);
        if (itemHandler.PlatformView is not DomElement itemPlatformView)
        {
            itemHandler.DisconnectHandler();
            return null;
        }

        itemPlatformView.Attr("data-maui-cv-item", "true");
        var activationSubscriptionId = itemPlatformView.On("click", () => OnItemClicked(itemPlatformView));
        return new CollectionItemRealization(item, itemView, itemHandler, itemPlatformView, activationSubscriptionId);
    }

    private void OnItemClicked(DomElement itemPlatformView)
    {
        var itemIndex = _itemRealizations.FindIndex(realization => realization.PlatformView.Handle == itemPlatformView.Handle);
        if (itemIndex < 0)
        {
            return;
        }

        var selectionMode = VirtualView.SelectionMode;
        if (selectionMode == SelectionMode.None)
        {
            return;
        }

        _updatingSelectionFromDom = true;
        try
        {
            if (selectionMode == SelectionMode.Single)
            {
                var selectedItem = _itemsSnapshot[itemIndex];
                if (!AreItemsEquivalent(VirtualView.SelectedItem, selectedItem))
                {
                    VirtualView.SelectedItem = selectedItem;
                }
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
                    .Where(static item => item is not null)
                    .Cast<object>()
                    .ToList();

                VirtualView.SelectedItems = selectedItems;
            }
        }
        finally
        {
            _updatingSelectionFromDom = false;
        }

        ApplySelectionFromVirtualView();
    }

    private View CreateItemView(object? item)
    {
        var itemTemplate = VirtualView.ItemTemplate;
        if (itemTemplate is not null)
        {
            var content = itemTemplate.CreateContent();
            if (TryExtractView(content, out var templatedView))
            {
                return templatedView;
            }

            ReportUnsupported(nameof(ItemsView.ItemTemplate), content);
        }
        else
        {
            ReportUnsupported(nameof(ItemsView.ItemTemplate), null);
        }

        return new Label
        {
            FontSize = 12,
            Text = item?.ToString() ?? string.Empty,
            TextColor = Colors.DarkSlateGray
        };
    }

    private void UpdateEmptyViewState()
    {
        var hasItems = _itemRealizations.Count > 0;

        _scrollHost?.Style("display", hasItems ? "block" : "none");
        _emptyHost?.Style("display", hasItems ? "none" : "block");

        if (hasItems)
        {
            DisconnectHostedContent(ref _emptyViewRealization, _emptyHost);
            return;
        }

        EnsureEmptyView();
    }

    private void EnsureEmptyView()
    {
        if (_emptyViewRealization is not null || _emptyHost is null || MauiContext is null)
        {
            return;
        }

        var emptyView = CreateEmptyView();
        _emptyViewRealization = RealizeHostedView(_emptyHost, emptyView, "data-maui-cv-empty-view");
    }

    private View CreateEmptyView()
    {
        var emptyTemplate = VirtualView.EmptyViewTemplate;
        if (emptyTemplate is not null)
        {
            var content = emptyTemplate.CreateContent();
            if (TryExtractView(content, out var templatedView))
            {
                templatedView.BindingContext = ResolveEmptyTemplateBindingContext();
                return templatedView;
            }

            ReportUnsupported(nameof(ItemsView.EmptyViewTemplate), content);
        }

        if (VirtualView.EmptyView is View configuredView)
        {
            return configuredView;
        }

        var emptyText = VirtualView.EmptyView?.ToString();
        return new Label
        {
            FontSize = 12,
            Text = string.IsNullOrWhiteSpace(emptyText) ? "No items" : emptyText,
            TextColor = Colors.DarkSlateGray
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

    private void EnsureSelectionModeCoherence()
    {
        switch (VirtualView.SelectionMode)
        {
            case SelectionMode.None:
                if (VirtualView.SelectedItem is not null)
                {
                    SetSelectedItemFromHandler(null);
                }

                if (VirtualView.SelectedItems is { Count: > 0 })
                {
                    UpdateSelectedItemsFromHandler(Array.Empty<object>());
                }

                break;
            case SelectionMode.Single:
                if (VirtualView.SelectedItem is null && VirtualView.SelectedItems is { Count: > 0 })
                {
                    SetSelectedItemFromHandler(VirtualView.SelectedItems[0]);
                }

                break;
            case SelectionMode.Multiple:
                if (VirtualView.SelectedItems is null)
                {
                    SetSelectedItemsCollectionFromHandler(new ObservableCollection<object>());
                }

                break;
        }
    }

    private void NormalizeSelectionForCurrentItems()
    {
        switch (VirtualView.SelectionMode)
        {
            case SelectionMode.None:
                return;
            case SelectionMode.Single:
                NormalizeSingleSelectionForCurrentItems();
                return;
            case SelectionMode.Multiple:
                NormalizeMultipleSelectionForCurrentItems();
                return;
            default:
                return;
        }
    }

    private void NormalizeSingleSelectionForCurrentItems()
    {
        if (VirtualView.SelectedItem is not null &&
            FindFirstIndexForItem(VirtualView.SelectedItem) < 0)
        {
            SetSelectedItemFromHandler(null);
        }

        if (VirtualView.SelectedItems is not { Count: > 0 })
        {
            return;
        }

        var usedIndexes = new bool[_itemsSnapshot.Count];
        if (TryFindMatchingIndex(VirtualView.SelectedItems[0], usedIndexes, out var firstSelectedIndex))
        {
            SetSelectedItemFromHandler(_itemsSnapshot[firstSelectedIndex]);
            return;
        }

        SetSelectedItemFromHandler(null);
    }

    private void NormalizeMultipleSelectionForCurrentItems()
    {
        if (VirtualView.SelectedItems is null)
        {
            SetSelectedItemsCollectionFromHandler(new ObservableCollection<object>());
            return;
        }

        var normalizedSelection = new List<object>();
        var usedIndexes = new bool[_itemsSnapshot.Count];
        foreach (var selectedItem in VirtualView.SelectedItems)
        {
            if (!TryFindMatchingIndex(selectedItem, usedIndexes, out var selectedIndex))
            {
                continue;
            }

            usedIndexes[selectedIndex] = true;
            if (_itemsSnapshot[selectedIndex] is { } selectedSnapshotItem)
            {
                normalizedSelection.Add(selectedSnapshotItem);
            }
        }

        if (!SelectionListsEquivalent(VirtualView.SelectedItems, normalizedSelection))
        {
            UpdateSelectedItemsFromHandler(normalizedSelection);
        }

        if (VirtualView.SelectedItem is not null &&
            FindFirstIndexForItem(VirtualView.SelectedItem) < 0)
        {
            SetSelectedItemFromHandler(null);
        }
    }

    private void ApplySelectionFromVirtualView()
    {
        var selectionMode = VirtualView.SelectionMode;
        var selectedIndexes = BuildSelectedIndexesFromVirtualView();

        _itemsHost?.Attr("data-maui-cv-selection-mode", selectionMode.ToString().ToLowerInvariant());
        _itemsHost?.Attr("role", selectionMode == SelectionMode.None ? "list" : "listbox");
        _itemsHost?.Attr("aria-multiselectable", selectionMode == SelectionMode.Multiple ? "true" : null);

        for (var index = 0; index < _itemRealizations.Count; index++)
        {
            var realization = _itemRealizations[index];
            var selected = selectedIndexes.Contains(index);

            realization.PlatformView.Attr("role", selectionMode == SelectionMode.None ? "listitem" : "option");
            realization.PlatformView.Attr("data-maui-cv-selected", selected ? "true" : "false");
            realization.PlatformView.Attr("aria-selected", selectionMode == SelectionMode.None ? null : (selected ? "true" : "false"));
            realization.PlatformView.Style("cursor", selectionMode == SelectionMode.None ? null : "pointer");
            realization.PlatformView.Style("outline", selected ? "2px solid #2563EB" : null);
            realization.PlatformView.Style("outlineOffset", selected ? "-2px" : null);
        }
    }

    private HashSet<int> BuildSelectedIndexesFromVirtualView()
    {
        var selectedIndexes = new HashSet<int>();
        if (_itemsSnapshot.Count == 0)
        {
            return selectedIndexes;
        }

        if (VirtualView.SelectionMode == SelectionMode.None)
        {
            return selectedIndexes;
        }

        var usedIndexes = new bool[_itemsSnapshot.Count];

        if (VirtualView.SelectionMode == SelectionMode.Single)
        {
            if (TryFindMatchingIndex(VirtualView.SelectedItem, usedIndexes, out var singleIndex))
            {
                selectedIndexes.Add(singleIndex);
            }

            return selectedIndexes;
        }

        if (VirtualView.SelectedItems is null)
        {
            return selectedIndexes;
        }

        foreach (var selectedItem in VirtualView.SelectedItems)
        {
            if (TryFindMatchingIndex(selectedItem, usedIndexes, out var index))
            {
                usedIndexes[index] = true;
                selectedIndexes.Add(index);
            }
        }

        return selectedIndexes;
    }

    private bool TryFindMatchingIndex(object? selectedItem, bool[] usedIndexes, out int index)
    {
        if (TryFindMatchingIndex(selectedItem, usedIndexes, preferReferenceMatch: true, out index))
        {
            return true;
        }

        if (SupportsValueFallback(selectedItem) &&
            TryFindMatchingIndex(selectedItem, usedIndexes, preferReferenceMatch: false, out index))
        {
            return true;
        }

        index = -1;
        return false;
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

        if (!SupportsValueFallback(selectedItem))
        {
            return -1;
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

        return SupportsValueFallback(left) && Equals(left, right);
    }

    private static bool SupportsValueFallback(object? item)
    {
        return item is string || item?.GetType().IsValueType == true;
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
            else
            {
                if (!AreItemsEquivalent(_itemsSnapshot[i], selectedItem))
                {
                    continue;
                }
            }

            index = i;
            return true;
        }

        index = -1;
        return false;
    }

    private void SetSelectedItemFromHandler(object? selectedItem)
    {
        if (AreItemsEquivalent(VirtualView.SelectedItem, selectedItem))
        {
            return;
        }

        _updatingSelectionFromHandler = true;
        try
        {
            VirtualView.SelectedItem = selectedItem;
        }
        finally
        {
            _updatingSelectionFromHandler = false;
        }
    }

    private void SetSelectedItemsCollectionFromHandler(IList<object> selectedItems)
    {
        _updatingSelectionFromHandler = true;
        try
        {
            VirtualView.SelectedItems = selectedItems;
        }
        finally
        {
            _updatingSelectionFromHandler = false;
        }
    }

    private void UpdateSelectedItemsFromHandler(IList<object> selectedItems)
    {
        _updatingSelectionFromHandler = true;
        try
        {
            VirtualView.UpdateSelectedItems(selectedItems);
        }
        finally
        {
            _updatingSelectionFromHandler = false;
        }
    }

    private static bool SelectionListsEquivalent(IList<object> current, IReadOnlyList<object> normalized)
    {
        if (current.Count != normalized.Count)
        {
            return false;
        }

        for (var i = 0; i < current.Count; i++)
        {
            if (!AreItemsEquivalent(current[i], normalized[i]))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsUpdatingSelection => _updatingSelectionFromDom || _updatingSelectionFromHandler;

    private void ApplyItemIndexAttributes()
    {
        for (var i = 0; i < _itemRealizations.Count; i++)
        {
            _itemRealizations[i].PlatformView.Attr("data-maui-cv-index", i.ToString(CultureInfo.InvariantCulture));
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
            _itemsHost.Append(realization.PlatformView);
        }
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
        if (realization.ActivationSubscriptionId != 0)
        {
            DomElement.Off(realization.ActivationSubscriptionId);
        }

        if (removeFromDom && _itemsHost is not null)
        {
            try
            {
                _itemsHost.Remove(realization.PlatformView);
            }
            catch
            {
                // Item may already be detached; continue disconnecting handler.
            }
        }

        realization.Handler.DisconnectHandler();
    }

    private void DisconnectHostedContent(ref HostedViewRealization? realization, DomElement? host)
    {
        if (realization is null)
        {
            host?.ClearChildren();
            return;
        }

        realization.Handler.DisconnectHandler();
        realization = null;
        host?.ClearChildren();
    }

    private void SubscribeToItemsSource(IEnumerable? itemsSource)
    {
        UnsubscribeFromItemsSource();

        if (itemsSource is not INotifyCollectionChanged observable || _collectionChangedHandler is null)
        {
            return;
        }

        _collectionChangedSource = observable;
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

    private void ApplyItemsLayout(CollectionView collectionView)
    {
        if (_itemsHost is null)
        {
            return;
        }

        _itemsHost.Style("display", "flex");
        _itemsHost.Style("flexDirection", "column");
        _itemsHost.Style("alignItems", "stretch");

        if (collectionView.ItemsLayout is LinearItemsLayout linearLayout &&
            linearLayout.Orientation == ItemsLayoutOrientation.Vertical)
        {
            _itemsHost.Style("rowGap", linearLayout.ItemSpacing.ToString("0.###", CultureInfo.InvariantCulture) + "px");
            return;
        }

        _itemsHost.Style("rowGap", null);
        ReportUnsupported(nameof(StructuredItemsView.ItemsLayout), collectionView.ItemsLayout);
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

    private static double ResolveDesiredDimension(double constraint, double fallback)
    {
        if (double.IsNaN(constraint) || double.IsInfinity(constraint))
        {
            return fallback;
        }

        return Math.Max(0, Math.Min(fallback, constraint));
    }

    private void ReportUnsupported(string featureName, object? value)
    {
#if DEBUG
        var key = $"{featureName}|{VirtualView.GetType().Name}|{value?.GetType().Name ?? "<null>"}";
        if (!UnsupportedWarnings.Add(key))
        {
            return;
        }

        Debug.WriteLine(
            $"[Maui.Browser] CollectionView feature '{featureName}' is not fully supported in this slice. Value type: {value?.GetType().Name ?? "<null>"}.");
#endif
    }

    private sealed record CollectionItemRealization(
        object? Item,
        View View,
        IElementHandler Handler,
        DomElement PlatformView,
        int ActivationSubscriptionId);

    private sealed record HostedViewRealization(
        View View,
        IElementHandler Handler,
        DomElement PlatformView);
}
