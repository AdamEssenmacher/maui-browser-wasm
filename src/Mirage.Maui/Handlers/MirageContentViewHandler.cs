using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageContentViewHandler : MirageContentViewHandlerBase<ContentView>
{
    public static IPropertyMapper<ContentView, MirageContentViewHandler> Mapper =
        new PropertyMapper<ContentView, MirageContentViewHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(ContentView.Content)] = MapContent,
            [nameof(ContentView.Padding)] = MapPadding
        };

    public MirageContentViewHandler()
        : base(Mapper)
    {
    }

    protected override IPadding PaddingSource => VirtualView;

    public static void MapContent(MirageContentViewHandler handler, ContentView contentView)
    {
        handler.SetPresentedContent(((IContentView)contentView).PresentedContent);
    }

    public static void MapPadding(MirageContentViewHandler handler, ContentView contentView)
    {
        MirageViewMapper.MapPadding(handler, contentView);
    }
}
