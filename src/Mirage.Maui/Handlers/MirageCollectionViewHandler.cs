using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Mirage.Maui.Handlers;

internal sealed class MirageCollectionViewHandler : MirageViewHandler<CollectionView>, IMirageRuntimeEventTarget
{
    private static long s_itemNodeIdCounter;
    private readonly List<RealizedItem> _realizedItems = [];
    private readonly HashSet<int> _selectedIndexes = [];
    private INotifyCollectionChanged? _observedCollection;
    private bool _updatingSelectionFromHandler;
    private int? _selectedSingleIndex;

    public static IPropertyMapper<CollectionView, MirageCollectionViewHandler> Mapper =
        new PropertyMapper<CollectionView, MirageCollectionViewHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(ItemsView.ItemsSource)] = MapItemsSource,
            [nameof(ItemsView.ItemTemplate)] = MapItemTemplate,
            [nameof(StructuredItemsView.ItemsLayout)] = MapItemsLayout,
            [nameof(SelectableItemsView.SelectionMode)] = MapSelectionMode,
            [nameof(SelectableItemsView.SelectedItem)] = MapSelectedItem,
            [nameof(SelectableItemsView.SelectedItems)] = MapSelectedItems,
            [nameof(IView.IsEnabled)] = MapIsEnabled,
            ["IsEnabled"] = MapIsEnabled
        };

    public MirageCollectionViewHandler()
        : base(ComponentKind.ContentContainer, Mapper)
    {
        DefaultDisplayStyle = "flex";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        base.ConnectHandler(platformView);
        ApplyContainerStyles();
        RebuildItems();
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        DetachObservedCollection();
        ClearItems();
        base.DisconnectHandler(platformView);
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        _ = eventName;
        _ = payload;
    }

    public static void MapItemsSource(MirageCollectionViewHandler handler, CollectionView collectionView)
    {
        _ = collectionView;
        handler.RebuildItems();
    }

    public static void MapItemTemplate(MirageCollectionViewHandler handler, CollectionView collectionView)
    {
        _ = collectionView;
        handler.RebuildItems();
    }

    public static void MapItemsLayout(MirageCollectionViewHandler handler, CollectionView collectionView)
    {
        _ = collectionView;
        handler.ApplyContainerStyles();
        handler.RequestRender();
    }

    public static void MapSelectionMode(MirageCollectionViewHandler handler, CollectionView collectionView)
    {
        _ = collectionView;
        handler.ApplySelectionInteractivity();
        handler.SyncSelectionStateFromVirtualView();
        handler.UpdateSelectionStyles();
        handler.RequestRender();
    }

    public static void MapSelectedItem(MirageCollectionViewHandler handler, CollectionView collectionView)
    {
        _ = collectionView;
        if (!handler._updatingSelectionFromHandler)
        {
            handler.SyncSelectionStateFromVirtualView();
        }

        handler.UpdateSelectionStyles();
        handler.RequestRender();
    }

    public static void MapSelectedItems(MirageCollectionViewHandler handler, CollectionView collectionView)
    {
        _ = collectionView;
        if (!handler._updatingSelectionFromHandler)
        {
            handler.SyncSelectionStateFromVirtualView();
        }

        handler.UpdateSelectionStyles();
        handler.RequestRender();
    }

    public static void MapIsEnabled(MirageCollectionViewHandler handler, CollectionView collectionView)
    {
        _ = collectionView;
        handler.ApplySelectionInteractivity();
        handler.RequestRender();
    }

    private void RebuildItems()
    {
        using var _ = BeginRenderBatch();
        var selectionSnapshot = CaptureSelectionSnapshot();

        DetachObservedCollection();
        ClearItems();

        if (!TryGetPlatformView(out var platformView))
        {
            return;
        }

        var realizedItems = new List<object?>();
        if (VirtualView.ItemsSource is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                realizedItems.Add(item);
            }
        }

        foreach (var item in realizedItems)
        {
            if (!TryCreateItem(item, out var realizedItem))
            {
                continue;
            }

            _realizedItems.Add(realizedItem);
            platformView.AddChild(realizedItem.Node);
        }

        if (VirtualView.ItemsSource is INotifyCollectionChanged collectionChanged)
        {
            _observedCollection = collectionChanged;
            _observedCollection.CollectionChanged += OnItemsSourceCollectionChanged;
        }

        ApplyContainerStyles();
        ApplySelectionInteractivity();
        if (!TryRestoreSelectionSnapshot(selectionSnapshot))
        {
            SyncSelectionStateFromVirtualView();
        }

        UpdateSelectionStyles();
        RequestRender();
    }

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        RebuildItems();
    }

    private bool TryCreateItem(object? item, out RealizedItem realizedItem)
    {
        realizedItem = null!;

        if (MauiContext is null)
        {
            return false;
        }

        var itemRoot = CreateItemRootNode();
        var contentView = CreateItemView(item);
        if (contentView is null)
        {
            return false;
        }

        if (contentView is BindableObject bindableObject)
        {
            bindableObject.BindingContext = item;
        }

        IElementHandler? contentHandler = null;
        MiragePlatformView? contentPlatformView = null;
        try
        {
            contentHandler = contentView.ToHandler(MauiContext);
            if (contentHandler.PlatformView is MiragePlatformView realizedPlatformView)
            {
                contentPlatformView = realizedPlatformView;
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("IMauiHandlersFactory", StringComparison.Ordinal))
        {
            // Integration-test fallback when a synthetic MauiContext does not provide a handler factory.
        }

        if (contentPlatformView is null)
        {
            contentPlatformView = CreateFallbackItemPlatformView(item);
        }

        contentPlatformView.SetStyle("position", "relative");
        contentPlatformView.SetStyle("left", null);
        contentPlatformView.SetStyle("top", null);
        contentPlatformView.SetStyle("right", null);
        contentPlatformView.SetStyle("bottom", null);
        itemRoot.AddChild(contentPlatformView);

        realizedItem = new RealizedItem(item, itemRoot, contentHandler);
        return true;
    }

    private MiragePlatformView CreateItemRootNode()
    {
        var itemNode = MiragePlatformView.CreateControl(ComponentKind.ContentContainer);
        itemNode.SetStyle("display", "block");
        itemNode.SetStyle("boxSizing", "border-box");
        itemNode.SetStyle("minWidth", "0");
        itemNode.SetStyle("position", "relative");

        var itemTarget = new ItemEventTarget((eventName, payload) =>
        {
            _ = payload;
            if (!string.Equals(eventName, "click", StringComparison.Ordinal))
            {
                return;
            }

            OnItemClicked(itemNode);
        });

        RegisterItemNode(itemNode, itemTarget);
        return itemNode;
    }

    private IView? CreateItemView(object? item)
    {
        var template = VirtualView.ItemTemplate;
        if (template is null)
        {
            return new Label { Text = item?.ToString() ?? string.Empty };
        }

        var templateContent = template.CreateContent();
        return templateContent switch
        {
            View view => view,
            ViewCell viewCell => viewCell.View,
            _ => null
        };
    }

    private static MiragePlatformView CreateFallbackItemPlatformView(object? item)
    {
        var labelNode = MiragePlatformView.CreateControl(ComponentKind.Label);
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

    private void OnItemClicked(MiragePlatformView itemNode)
    {
        if (!VirtualView.IsEnabled || VirtualView.SelectionMode == SelectionMode.None)
        {
            return;
        }

        if (!TryFindItemIndex(itemNode, out var itemIndex))
        {
            return;
        }

        if (VirtualView.SelectionMode == SelectionMode.Multiple)
        {
            ToggleMultipleSelection(itemIndex);
        }
        else
        {
            _selectedIndexes.Clear();
            _selectedSingleIndex = itemIndex;

            var clickedItem = _realizedItems[itemIndex].Item;
            SetSelectedItemFromHandler(clickedItem);
        }

        UpdateSelectionStyles();
        RequestRender();
    }

    private void ToggleMultipleSelection(int itemIndex)
    {
        _selectedSingleIndex = null;
        if (!_selectedIndexes.Add(itemIndex))
        {
            _selectedIndexes.Remove(itemIndex);
        }

        var normalizedSelection = _selectedIndexes
            .OrderBy(index => index)
            .Select(index => _realizedItems[index].Item)
            .OfType<object>()
            .ToList();

        SetSelectedItemsFromHandler(normalizedSelection);
    }

    private void ApplyContainerStyles()
    {
        if (!TryGetPlatformView(out var platformView))
        {
            return;
        }

        var isHorizontal = VirtualView.ItemsLayout is LinearItemsLayout { Orientation: ItemsLayoutOrientation.Horizontal };
        var spacing = ResolveItemSpacing(VirtualView.ItemsLayout);

        platformView.SetStyle("display", "flex");
        platformView.SetStyle("boxSizing", "border-box");
        platformView.SetStyle("alignItems", "stretch");
        platformView.SetStyle("justifyContent", "flex-start");
        platformView.SetStyle("flexDirection", isHorizontal ? "row" : "column");
        platformView.SetStyle("gap", MirageViewMapper.ToPx(spacing));
        platformView.SetStyle("overflowX", isHorizontal ? "auto" : "hidden");
        platformView.SetStyle("overflowY", isHorizontal ? "hidden" : "auto");
        platformView.SetStyle("minWidth", "0");
        platformView.SetStyle("minHeight", "0");
    }

    private void ApplySelectionInteractivity()
    {
        var selectionEnabled = VirtualView.SelectionMode != SelectionMode.None && VirtualView.IsEnabled;
        foreach (var realizedItem in _realizedItems)
        {
            realizedItem.Node.SetEvent("click", selectionEnabled);
            realizedItem.Node.SetStyle("cursor", selectionEnabled ? "pointer" : "default");
            realizedItem.Node.SetStyle("pointerEvents", VirtualView.IsEnabled ? "auto" : "none");
        }
    }

    private void UpdateSelectionStyles()
    {
        for (var index = 0; index < _realizedItems.Count; index++)
        {
            var isSelected = VirtualView.SelectionMode switch
            {
                SelectionMode.Multiple => _selectedIndexes.Contains(index),
                SelectionMode.Single => _selectedSingleIndex == index,
                _ => false
            };

            var node = _realizedItems[index].Node;
            node.SetStyle("outline", isSelected ? "2px solid rgba(86,57,176,0.35)" : null);
            node.SetStyle("outlineOffset", isSelected ? "1px" : null);
        }
    }

    private bool TryFindItemIndex(MiragePlatformView node, out int index)
    {
        for (var i = 0; i < _realizedItems.Count; i++)
        {
            if (ReferenceEquals(_realizedItems[i].Node, node))
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }

    private bool TryFindNextMatchingItemIndex(object? item, HashSet<int> usedIndexes, out int index)
    {
        for (var i = 0; i < _realizedItems.Count; i++)
        {
            if (usedIndexes.Contains(i))
            {
                continue;
            }

            if (AreItemsEquivalent(_realizedItems[i].Item, item))
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }

    private SelectionSnapshot CaptureSelectionSnapshot()
    {
        if (_realizedItems.Count == 0)
        {
            return SelectionSnapshot.Empty;
        }

        return VirtualView.SelectionMode switch
        {
            SelectionMode.Single => CaptureSingleSelectionSnapshot(),
            SelectionMode.Multiple => CaptureMultipleSelectionSnapshot(),
            _ => SelectionSnapshot.Empty
        };
    }

    private SelectionSnapshot CaptureSingleSelectionSnapshot()
    {
        if (!_selectedSingleIndex.HasValue
            || _selectedSingleIndex.Value < 0
            || _selectedSingleIndex.Value >= _realizedItems.Count)
        {
            return SelectionSnapshot.Empty;
        }

        return SelectionSnapshot.FromSingleToken(CreateSelectionToken(_selectedSingleIndex.Value));
    }

    private SelectionSnapshot CaptureMultipleSelectionSnapshot()
    {
        if (_selectedIndexes.Count == 0)
        {
            return SelectionSnapshot.Empty;
        }

        var selectedTokens = _selectedIndexes
            .Where(index => index >= 0 && index < _realizedItems.Count)
            .OrderBy(index => index)
            .Select(CreateSelectionToken)
            .ToList();

        return selectedTokens.Count == 0
            ? SelectionSnapshot.Empty
            : SelectionSnapshot.FromMultipleTokens(selectedTokens);
    }

    private SelectionToken CreateSelectionToken(int itemIndex)
    {
        var selectedItem = _realizedItems[itemIndex].Item;
        var occurrence = 0;
        for (var i = 0; i <= itemIndex; i++)
        {
            if (AreItemsEquivalent(_realizedItems[i].Item, selectedItem))
            {
                occurrence++;
            }
        }

        return new SelectionToken(selectedItem, occurrence);
    }

    private bool TryRestoreSelectionSnapshot(SelectionSnapshot snapshot)
    {
        if (snapshot.IsEmpty || snapshot.Mode != VirtualView.SelectionMode)
        {
            return false;
        }

        return snapshot.Mode switch
        {
            SelectionMode.Single => TryRestoreSingleSelectionSnapshot(snapshot),
            SelectionMode.Multiple => TryRestoreMultipleSelectionSnapshot(snapshot),
            _ => false
        };
    }

    private bool TryRestoreSingleSelectionSnapshot(SelectionSnapshot snapshot)
    {
        if (!snapshot.SingleToken.HasValue
            || !TryFindIndexBySelectionToken(snapshot.SingleToken.Value, usedIndexes: null, out var selectedIndex))
        {
            return false;
        }

        _selectedIndexes.Clear();
        _selectedSingleIndex = selectedIndex;
        SetSelectedItemFromHandler(_realizedItems[selectedIndex].Item);
        return true;
    }

    private bool TryRestoreMultipleSelectionSnapshot(SelectionSnapshot snapshot)
    {
        if (snapshot.MultipleTokens.Count == 0)
        {
            return false;
        }

        _selectedSingleIndex = null;
        _selectedIndexes.Clear();

        var usedIndexes = new HashSet<int>();
        var selectedItems = new List<object>();
        foreach (var token in snapshot.MultipleTokens)
        {
            if (!TryFindIndexBySelectionToken(token, usedIndexes, out var selectedIndex))
            {
                continue;
            }

            usedIndexes.Add(selectedIndex);
            _selectedIndexes.Add(selectedIndex);
            if (_realizedItems[selectedIndex].Item is { } selectedItem)
            {
                selectedItems.Add(selectedItem);
            }
        }

        if (selectedItems.Count == 0)
        {
            return false;
        }

        SetSelectedItemsFromHandler(selectedItems);
        return true;
    }

    private bool TryFindIndexBySelectionToken(SelectionToken token, HashSet<int>? usedIndexes, out int index)
    {
        var matchesSeen = 0;
        for (var i = 0; i < _realizedItems.Count; i++)
        {
            if (usedIndexes?.Contains(i) == true)
            {
                continue;
            }

            if (!AreItemsEquivalent(_realizedItems[i].Item, token.Item))
            {
                continue;
            }

            matchesSeen++;
            if (matchesSeen == token.Occurrence)
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }

    private void SyncSelectionStateFromVirtualView()
    {
        _selectedIndexes.Clear();
        _selectedSingleIndex = null;

        if (VirtualView.SelectionMode == SelectionMode.None)
        {
            return;
        }

        if (VirtualView.SelectionMode == SelectionMode.Single)
        {
            if (VirtualView.SelectedItem is null)
            {
                return;
            }

            var usedIndexes = new HashSet<int>();
            if (TryFindNextMatchingItemIndex(VirtualView.SelectedItem, usedIndexes, out var selectedIndex))
            {
                _selectedSingleIndex = selectedIndex;
            }
            else
            {
                SetSelectedItemFromHandler(null);
            }

            return;
        }

        if (VirtualView.SelectedItems is null)
        {
            return;
        }

        var matchedIndexes = new HashSet<int>();
        var normalizedSelection = new List<object>();
        foreach (var selectedItem in VirtualView.SelectedItems)
        {
            if (selectedItem is null
                || !TryFindNextMatchingItemIndex(selectedItem, matchedIndexes, out var index))
            {
                continue;
            }

            matchedIndexes.Add(index);
            _selectedIndexes.Add(index);
            if (_realizedItems[index].Item is { } normalizedItem)
            {
                normalizedSelection.Add(normalizedItem);
            }
        }

        if (!SelectionListsEquivalent(VirtualView.SelectedItems, normalizedSelection))
        {
            SetSelectedItemsFromHandler(normalizedSelection);
        }
    }

    private static bool SelectionListsEquivalent(IList<object>? left, IList<object>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        var leftCount = left?.Count ?? 0;
        var rightCount = right?.Count ?? 0;
        if (leftCount != rightCount)
        {
            return false;
        }

        if (leftCount == 0)
        {
            return true;
        }

        for (var i = 0; i < leftCount; i++)
        {
            if (!AreItemsEquivalent(left![i], right![i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AreItemsEquivalent(object? left, object? right)
    {
        return ReferenceEquals(left, right);
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

    private void SetSelectedItemsFromHandler(IList<object> selectedItems)
    {
        if (SelectionListsEquivalent(VirtualView.SelectedItems, selectedItems))
        {
            return;
        }

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

    private void ClearItems()
    {
        if (TryGetPlatformView(out var platformView))
        {
            platformView.ClearChildren();
        }

        if (Coordinator is { } coordinator)
        {
            foreach (var realizedItem in _realizedItems)
            {
                coordinator.Unregister(realizedItem.Node);
            }
        }

        foreach (var realizedItem in _realizedItems)
        {
            MirageHandlerDisconnect.SafeDisconnect(realizedItem.ContentHandler);
        }

        _realizedItems.Clear();
        _selectedIndexes.Clear();
        _selectedSingleIndex = null;
    }

    private void DetachObservedCollection()
    {
        if (_observedCollection is null)
        {
            return;
        }

        _observedCollection.CollectionChanged -= OnItemsSourceCollectionChanged;
        _observedCollection = null;
    }

    private static double ResolveItemSpacing(IItemsLayout? layout)
    {
        if (layout is not LinearItemsLayout linearItemsLayout || !double.IsFinite(linearItemsLayout.ItemSpacing))
        {
            return 0;
        }

        return Math.Max(0, linearItemsLayout.ItemSpacing);
    }

    private sealed class RealizedItem
    {
        public RealizedItem(object? item, MiragePlatformView node, IElementHandler? contentHandler)
        {
            Item = item;
            Node = node;
            ContentHandler = contentHandler;
        }

        public object? Item { get; }

        public MiragePlatformView Node { get; }

        public IElementHandler? ContentHandler { get; }
    }

    private readonly record struct SelectionToken(object? Item, int Occurrence);

    private sealed class SelectionSnapshot
    {
        public static SelectionSnapshot Empty { get; } = new();

        private SelectionSnapshot()
        {
            Mode = SelectionMode.None;
            MultipleTokens = [];
        }

        private SelectionSnapshot(SelectionMode mode, SelectionToken? singleToken, List<SelectionToken> multipleTokens)
        {
            Mode = mode;
            SingleToken = singleToken;
            MultipleTokens = multipleTokens;
        }

        public SelectionMode Mode { get; }

        public SelectionToken? SingleToken { get; }

        public IReadOnlyList<SelectionToken> MultipleTokens { get; }

        public bool IsEmpty => Mode == SelectionMode.None;

        public static SelectionSnapshot FromSingleToken(SelectionToken token)
        {
            return new SelectionSnapshot(SelectionMode.Single, token, []);
        }

        public static SelectionSnapshot FromMultipleTokens(List<SelectionToken> tokens)
        {
            return new SelectionSnapshot(SelectionMode.Multiple, null, tokens);
        }
    }

    private sealed class ItemEventTarget : IMirageRuntimeEventTarget
    {
        private readonly Action<string, string?> _onEvent;

        public ItemEventTarget(Action<string, string?> onEvent)
        {
            _onEvent = onEvent;
        }

        public void OnRuntimeEvent(string eventName, string? payload)
        {
            _onEvent(eventName, payload);
        }
    }
}
