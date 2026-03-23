using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal abstract class MirageGroupableItemsViewHandler<TItemsView> : MirageSelectableItemsViewHandler<TItemsView>
    where TItemsView : GroupableItemsView
{
    internal new static readonly string[] ParityPropertyKeys =
    {
        nameof(GroupableItemsView.IsGrouped),
        nameof(GroupableItemsView.GroupHeaderTemplate),
        nameof(GroupableItemsView.GroupFooterTemplate)
    };

    internal new static readonly string[] ParityCommandKeys = [];

    public static IPropertyMapper<TItemsView, MirageGroupableItemsViewHandler<TItemsView>> GroupableItemsViewMapper =
        new PropertyMapper<TItemsView, MirageGroupableItemsViewHandler<TItemsView>>(SelectableItemsViewMapper)
        {
            [nameof(GroupableItemsView.IsGrouped)] = MapIsGrouped,
            [nameof(GroupableItemsView.GroupHeaderTemplate)] = MapGroupHeaderTemplate,
            [nameof(GroupableItemsView.GroupFooterTemplate)] = MapGroupFooterTemplate
        };

    protected MirageGroupableItemsViewHandler(
        IPropertyMapper mapper,
        CommandMapper? commandMapper = null)
        : base(mapper, commandMapper)
    {
    }

    public static void MapIsGrouped(MirageGroupableItemsViewHandler<TItemsView> handler, TItemsView itemsView)
    {
        _ = itemsView;
        handler.ApplyGroupingProjection();
    }

    public static void MapGroupHeaderTemplate(MirageGroupableItemsViewHandler<TItemsView> handler, TItemsView itemsView)
    {
        _ = itemsView;
        handler.ApplyGroupingProjection();
    }

    public static void MapGroupFooterTemplate(MirageGroupableItemsViewHandler<TItemsView> handler, TItemsView itemsView)
    {
        _ = itemsView;
        handler.ApplyGroupingProjection();
    }
}
