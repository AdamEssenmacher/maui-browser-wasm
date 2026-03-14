using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageContentPageHandler : MirageContentViewHandlerBase<ContentPage>
{
    public static IPropertyMapper<ContentPage, MirageContentPageHandler> Mapper =
        new PropertyMapper<ContentPage, MirageContentPageHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(ContentPage.Content)] = MapContent,
            [nameof(ContentPage.Padding)] = MapPadding
        };

    public MirageContentPageHandler()
        : base(Mapper)
    {
    }

    protected override IPadding PaddingSource => VirtualView;

    protected override bool IsBoundedPageSurface => true;

    public static void MapContent(MirageContentPageHandler handler, ContentPage page)
    {
        handler.SetPresentedContent(((IContentView)page).PresentedContent);
    }

    public static void MapPadding(MirageContentPageHandler handler, ContentPage page)
    {
        MirageViewMapper.MapPadding(handler, page);
    }
}
