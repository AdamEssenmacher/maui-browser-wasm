using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal abstract class MirageSelectableItemsViewHandler<TItemsView> : MirageStructuredItemsViewHandler<TItemsView>
    where TItemsView : SelectableItemsView
{
    internal new static readonly string[] ParityPropertyKeys =
    {
        nameof(SelectableItemsView.SelectionMode),
        nameof(SelectableItemsView.SelectedItem),
        nameof(SelectableItemsView.SelectedItems)
    };

    internal new static readonly string[] ParityCommandKeys = [];

    public static IPropertyMapper<TItemsView, MirageSelectableItemsViewHandler<TItemsView>> SelectableItemsViewMapper =
        new PropertyMapper<TItemsView, MirageSelectableItemsViewHandler<TItemsView>>(StructuredItemsViewMapper)
        {
            [nameof(SelectableItemsView.SelectionMode)] = MapSelectionMode,
            [nameof(SelectableItemsView.SelectedItem)] = MapSelectedItem,
            [nameof(SelectableItemsView.SelectedItems)] = MapSelectedItems
        };

    protected MirageSelectableItemsViewHandler(
        IPropertyMapper mapper,
        CommandMapper? commandMapper = null)
        : base(mapper, commandMapper)
    {
    }

    protected override void OnConnectVirtualView()
    {
        base.OnConnectVirtualView();
        SubscribeSelectionChanged(VirtualView);
    }

    protected override void OnDisconnectVirtualView()
    {
        UnsubscribeSelectionChanged(VirtualView);
        base.OnDisconnectVirtualView();
    }

    public static void MapSelectionMode(MirageSelectableItemsViewHandler<TItemsView> handler, TItemsView itemsView)
    {
        _ = itemsView;
        handler.OnSelectionMapperChanged();
    }

    public static void MapSelectedItem(MirageSelectableItemsViewHandler<TItemsView> handler, TItemsView itemsView)
    {
        _ = itemsView;
        handler.OnSelectionMapperChanged();
    }

    public static void MapSelectedItems(MirageSelectableItemsViewHandler<TItemsView> handler, TItemsView itemsView)
    {
        _ = itemsView;
        handler.OnSelectionMapperChanged();
    }
}
