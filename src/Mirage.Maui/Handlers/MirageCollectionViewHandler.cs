using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageCollectionViewHandler : MirageReorderableItemsViewHandler<CollectionView>
{
    internal new static readonly string[] ParityPropertyKeys =
    [
        .. MirageItemsViewHandler<CollectionView>.ParityPropertyKeys,
        .. MirageStructuredItemsViewHandler<CollectionView>.ParityPropertyKeys,
        .. MirageSelectableItemsViewHandler<CollectionView>.ParityPropertyKeys,
        .. MirageGroupableItemsViewHandler<CollectionView>.ParityPropertyKeys,
        .. MirageReorderableItemsViewHandler<CollectionView>.ParityPropertyKeys
    ];

    internal new static readonly string[] ParityCommandKeys = [];

    internal new static readonly string[] ParityExtensionPropertyKeys = MirageItemsViewHandler<CollectionView>.ParityExtensionPropertyKeys;

    public static IPropertyMapper<CollectionView, MirageCollectionViewHandler> Mapper =
        new PropertyMapper<CollectionView, MirageCollectionViewHandler>(ReorderableItemsViewMapper);

    public static CommandMapper<CollectionView, MirageCollectionViewHandler> CommandMapper =
        new(MirageItemsViewHandler<CollectionView>.ItemsViewCommandMapper);

    public MirageCollectionViewHandler()
        : base(Mapper, CommandMapper)
    {
    }
}
