using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
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
            [nameof(SelectableItemsView.SelectionMode)] = MapSelectionMode
        };

    private readonly List<object?> _itemsSnapshot = [];
    private readonly List<CollectionItemRealization> _itemRealizations = [];

    private NotifyCollectionChangedEventHandler? _collectionChangedHandler;
    private INotifyCollectionChanged? _collectionChangedSource;

    private DomElement? _scrollHost;
    private DomElement? _itemsHost;
    private DomElement? _emptyHost;
    private EmptyViewRealization? _emptyViewRealization;

#if DEBUG
    private static readonly HashSet<string> UnsupportedWarnings = [];
#endif

    public BrowserCollectionViewHandler()
        : base(Mapper)
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

        _itemsHost = DomElement.Create("div");
        _itemsHost.Attr("data-maui-cv-items-host", "true");
        _itemsHost.Style("display", "flex");
        _itemsHost.Style("flexDirection", "column");
        _itemsHost.Style("alignItems", "stretch");
        _itemsHost.Style("boxSizing", "border-box");

        _emptyHost = DomElement.Create("div");
        _emptyHost.Attr("data-maui-cv-empty-host", "true");
        _emptyHost.Style("boxSizing", "border-box");
        _emptyHost.Style("overflow", "auto");
        _emptyHost.Style("display", "none");

        _scrollHost.Append(_itemsHost);
        platformView.Append(_scrollHost);
        platformView.Append(_emptyHost);

        _collectionChangedHandler ??= OnItemsSourceCollectionChanged;
        SubscribeToItemsSource(VirtualView.ItemsSource);
        ApplyItemsLayout(VirtualView);
        RebuildAllContent();
        WarnUnsupportedSelectionMode(VirtualView.SelectionMode);

        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(DomElement platformView)
    {
        UnsubscribeFromItemsSource();
        DisconnectAllItems();
        DisconnectEmptyView();

        _itemsSnapshot.Clear();
        platformView.ClearChildren();
        _scrollHost = null;
        _itemsHost = null;
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
        handler.RebuildAllContent();
    }

    public static void MapEmptyViewTemplate(BrowserCollectionViewHandler handler, CollectionView collectionView)
    {
        handler.RebuildAllContent();
    }

    public static void MapSelectionMode(BrowserCollectionViewHandler handler, CollectionView collectionView)
    {
        handler.WarnUnsupportedSelectionMode(collectionView.SelectionMode);
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
            case NotifyCollectionChangedAction.Reset:
                RebuildAllContent();
                break;
            case NotifyCollectionChangedAction.Move:
            case NotifyCollectionChangedAction.Replace:
            default:
                RebuildAllContent();
                break;
        }
    }

    private bool TryApplyAdd(NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is null || e.NewItems.Count == 0 || MauiContext is null || _itemsHost is null)
        {
            return false;
        }

        var insertionIndex = e.NewStartingIndex;
        if (insertionIndex < 0 || insertionIndex > _itemRealizations.Count)
        {
            return false;
        }

        for (var i = 0; i < e.NewItems.Count; i++)
        {
            var snapshotIndex = insertionIndex + i;
            var item = e.NewItems[i];
            _itemsSnapshot.Insert(snapshotIndex, item);

            var realization = CreateItemRealization(item);
            if (realization is null)
            {
                return false;
            }

            if (snapshotIndex >= _itemRealizations.Count)
            {
                _itemsHost.Append(realization.PlatformView);
                _itemRealizations.Add(realization);
            }
            else
            {
                _itemsHost.Insert(snapshotIndex, realization.PlatformView);
                _itemRealizations.Insert(snapshotIndex, realization);
            }
        }

        UpdateEmptyViewState();
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

        UpdateEmptyViewState();
        return true;
    }

    private void RebuildAllContent()
    {
        DisconnectAllItems();
        DisconnectEmptyView();

        _itemsSnapshot.Clear();
        _itemsSnapshot.AddRange(SnapshotItems(VirtualView.ItemsSource));

        foreach (var item in _itemsSnapshot)
        {
            var realization = CreateItemRealization(item);
            if (realization is null)
            {
                continue;
            }

            _itemsHost?.Append(realization.PlatformView);
            _itemRealizations.Add(realization);
        }

        UpdateEmptyViewState();
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
        return new CollectionItemRealization(item, itemView, itemHandler, itemPlatformView);
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
            DisconnectEmptyView();
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
        var emptyHandler = emptyView.ToHandler(MauiContext);
        if (emptyHandler.PlatformView is not DomElement emptyPlatformView)
        {
            emptyHandler.DisconnectHandler();
            return;
        }

        _emptyHost.ClearChildren();
        _emptyHost.Append(emptyPlatformView);
        _emptyViewRealization = new EmptyViewRealization(emptyView, emptyHandler, emptyPlatformView);
    }

    private View CreateEmptyView()
    {
        var emptyTemplate = VirtualView.EmptyViewTemplate;
        if (emptyTemplate is not null)
        {
            var content = emptyTemplate.CreateContent();
            if (TryExtractView(content, out var templatedView))
            {
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

    private void DisconnectEmptyView()
    {
        if (_emptyViewRealization is null)
        {
            _emptyHost?.ClearChildren();
            return;
        }

        _emptyViewRealization.Handler.DisconnectHandler();
        _emptyViewRealization = null;
        _emptyHost?.ClearChildren();
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
            _itemsHost.Style("rowGap", linearLayout.ItemSpacing.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "px");
            return;
        }

        _itemsHost.Style("rowGap", null);
        ReportUnsupported(nameof(StructuredItemsView.ItemsLayout), collectionView.ItemsLayout);
    }

    private void WarnUnsupportedSelectionMode(SelectionMode selectionMode)
    {
        if (selectionMode == SelectionMode.None)
        {
            return;
        }

        ReportUnsupported(nameof(SelectableItemsView.SelectionMode), selectionMode);
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
        DomElement PlatformView);

    private sealed record EmptyViewRealization(
        View View,
        IElementHandler Handler,
        DomElement PlatformView);
}
