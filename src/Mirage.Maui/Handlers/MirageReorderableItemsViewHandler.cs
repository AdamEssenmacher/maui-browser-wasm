using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal abstract class MirageReorderableItemsViewHandler<TItemsView> : MirageGroupableItemsViewHandler<TItemsView>
    where TItemsView : ReorderableItemsView
{
    internal new static readonly string[] ParityPropertyKeys =
    {
        nameof(ReorderableItemsView.CanReorderItems)
    };

    internal new static readonly string[] ParityCommandKeys = [];

    public static IPropertyMapper<TItemsView, MirageReorderableItemsViewHandler<TItemsView>> ReorderableItemsViewMapper =
        new PropertyMapper<TItemsView, MirageReorderableItemsViewHandler<TItemsView>>(GroupableItemsViewMapper)
        {
            [nameof(ReorderableItemsView.CanReorderItems)] = MapCanReorderItems
        };

    protected MirageReorderableItemsViewHandler(
        IPropertyMapper mapper,
        CommandMapper? commandMapper = null)
        : base(mapper, commandMapper)
    {
    }

    public static void MapCanReorderItems(MirageReorderableItemsViewHandler<TItemsView> handler, TItemsView itemsView)
    {
        _ = itemsView;
        handler.ApplyReorderProjection();
    }
}
