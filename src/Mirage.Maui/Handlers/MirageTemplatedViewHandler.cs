using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageTemplatedViewHandler : MirageContentViewHandlerBase<TemplatedView>
{
    private static readonly IPadding ZeroPadding = new ZeroPaddingSource();

    public static IPropertyMapper<TemplatedView, MirageTemplatedViewHandler> Mapper =
        new PropertyMapper<TemplatedView, MirageTemplatedViewHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(IContentView.Content)] = MapContent,
            [nameof(TemplatedView.ControlTemplate)] = MapContent
        };

    public MirageTemplatedViewHandler()
        : base(Mapper)
    {
    }

    protected override IPadding PaddingSource => VirtualView as IPadding ?? ZeroPadding;

    public static void MapContent(MirageTemplatedViewHandler handler, TemplatedView templatedView)
    {
        handler.SetPresentedContent(((IContentView)templatedView).PresentedContent);
    }

    private sealed class ZeroPaddingSource : IPadding
    {
        public Thickness Padding => Thickness.Zero;
    }
}
