using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal abstract class MirageStructuredItemsViewHandler<TItemsView> : MirageItemsViewHandler<TItemsView>
    where TItemsView : StructuredItemsView
{
    internal new static readonly string[] ParityPropertyKeys =
    {
        nameof(StructuredItemsView.Header),
        nameof(StructuredItemsView.HeaderTemplate),
        nameof(StructuredItemsView.Footer),
        nameof(StructuredItemsView.FooterTemplate),
        nameof(StructuredItemsView.ItemsLayout),
        nameof(StructuredItemsView.ItemSizingStrategy)
    };

    internal new static readonly string[] ParityCommandKeys = [];

    public static IPropertyMapper<TItemsView, MirageStructuredItemsViewHandler<TItemsView>> StructuredItemsViewMapper =
        new PropertyMapper<TItemsView, MirageStructuredItemsViewHandler<TItemsView>>(ItemsViewMapper)
        {
            [nameof(StructuredItemsView.Header)] = MapHeader,
            [nameof(StructuredItemsView.HeaderTemplate)] = MapHeaderTemplate,
            [nameof(StructuredItemsView.Footer)] = MapFooter,
            [nameof(StructuredItemsView.FooterTemplate)] = MapFooterTemplate,
            [nameof(StructuredItemsView.ItemsLayout)] = MapItemsLayout,
            [nameof(StructuredItemsView.ItemSizingStrategy)] = MapItemSizingStrategy
        };

    protected MirageStructuredItemsViewHandler(
        IPropertyMapper mapper,
        CommandMapper? commandMapper = null)
        : base(mapper, commandMapper)
    {
    }

    public static void MapHeader(MirageStructuredItemsViewHandler<TItemsView> handler, TItemsView itemsView)
    {
        _ = itemsView;
        handler.RebuildHeaderContent();
        handler.RequestRender();
    }

    public static void MapHeaderTemplate(MirageStructuredItemsViewHandler<TItemsView> handler, TItemsView itemsView)
    {
        _ = itemsView;
        handler.RebuildHeaderContent();
        handler.RequestRender();
    }

    public static void MapFooter(MirageStructuredItemsViewHandler<TItemsView> handler, TItemsView itemsView)
    {
        _ = itemsView;
        handler.RebuildFooterContent();
        handler.RequestRender();
    }

    public static void MapFooterTemplate(MirageStructuredItemsViewHandler<TItemsView> handler, TItemsView itemsView)
    {
        _ = itemsView;
        handler.RebuildFooterContent();
        handler.RequestRender();
    }

    public static void MapItemsLayout(MirageStructuredItemsViewHandler<TItemsView> handler, TItemsView itemsView)
    {
        _ = itemsView;
        handler.ApplyItemsLayout();
        handler.RequestRender();
    }

    public static void MapItemSizingStrategy(MirageStructuredItemsViewHandler<TItemsView> handler, TItemsView itemsView)
    {
        _ = itemsView;
        handler.ApplyItemSizingStrategy();
    }
}
