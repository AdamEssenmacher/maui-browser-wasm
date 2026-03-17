using System.Collections;
using System.Collections.Specialized;
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
    private INotifyCollectionChanged? _observedCollection;

    public static IPropertyMapper<CollectionView, MirageCollectionViewHandler> Mapper =
        new PropertyMapper<CollectionView, MirageCollectionViewHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(ItemsView.ItemsSource)] = MapItemsSource,
            [nameof(ItemsView.ItemTemplate)] = MapItemTemplate,
            [nameof(StructuredItemsView.ItemsLayout)] = MapItemsLayout,
            [nameof(SelectableItemsView.SelectionMode)] = MapSelectionMode,
            [nameof(SelectableItemsView.SelectedItem)] = MapSelectedItem,
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
        handler.UpdateSelectionStyles();
        handler.RequestRender();
    }

    public static void MapSelectedItem(MirageCollectionViewHandler handler, CollectionView collectionView)
    {
        _ = collectionView;
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

        DetachObservedCollection();
        ClearItems();

        if (!TryGetPlatformView(out var platformView))
        {
            return;
        }

        var realizedItems = new List<object>();
        if (VirtualView.ItemsSource is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is not null)
                {
                    realizedItems.Add(item);
                }
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
        UpdateSelectionStyles();
        RequestRender();
    }

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        RebuildItems();
    }

    private bool TryCreateItem(object item, out RealizedItem realizedItem)
    {
        realizedItem = null!;

        if (MauiContext is null)
        {
            return false;
        }

        var itemRoot = CreateItemRootNode(item);
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

    private MiragePlatformView CreateItemRootNode(object item)
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

            OnItemClicked(item);
        });

        RegisterItemNode(itemNode, itemTarget);
        return itemNode;
    }

    private IView? CreateItemView(object item)
    {
        var template = VirtualView.ItemTemplate;
        if (template is null)
        {
            return new Label { Text = item.ToString() ?? string.Empty };
        }

        var templateContent = template.CreateContent();
        return templateContent switch
        {
            View view => view,
            ViewCell viewCell => viewCell.View,
            _ => null
        };
    }

    private static MiragePlatformView CreateFallbackItemPlatformView(object item)
    {
        var labelNode = MiragePlatformView.CreateControl(ComponentKind.Label);
        labelNode.SetProperty("Text", ResolveFallbackText(item));
        return labelNode;
    }

    private static string ResolveFallbackText(object item)
    {
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

    private void OnItemClicked(object item)
    {
        if (!VirtualView.IsEnabled || VirtualView.SelectionMode == SelectionMode.None)
        {
            return;
        }

        if (!ReferenceEquals(VirtualView.SelectedItem, item))
        {
            VirtualView.SelectedItem = item;
        }

        UpdateSelectionStyles();
        RequestRender();
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
        foreach (var realizedItem in _realizedItems)
        {
            var isSelected = ReferenceEquals(VirtualView.SelectedItem, realizedItem.Item);
            realizedItem.Node.SetStyle("outline", isSelected ? "2px solid rgba(86,57,176,0.35)" : null);
            realizedItem.Node.SetStyle("outlineOffset", isSelected ? "1px" : null);
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
        public RealizedItem(object item, MiragePlatformView node, IElementHandler? contentHandler)
        {
            Item = item;
            Node = node;
            ContentHandler = contentHandler;
        }

        public object Item { get; }

        public MiragePlatformView Node { get; }

        public IElementHandler? ContentHandler { get; }
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
