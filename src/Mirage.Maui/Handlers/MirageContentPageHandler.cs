using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Mirage.Maui.Handlers;

internal sealed class MirageContentPageHandler : MirageViewHandler<ContentPage>
{
    public static IPropertyMapper<ContentPage, MirageContentPageHandler> Mapper =
        new PropertyMapper<ContentPage, MirageContentPageHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(ContentPage.Content)] = MapContent,
            [nameof(ContentPage.Padding)] = MapPadding
        };

    private View? _currentContent;
    private IElementHandler? _currentContentHandler;

    public MirageContentPageHandler()
        : base(ComponentKind.ContentContainer, Mapper)
    {
        DefaultDisplayStyle = "grid";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        ApplyBaseStyles();
        SetContent(VirtualView.Content);
        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        DisconnectCurrentContent();
        base.DisconnectHandler(platformView);
    }

    public static void MapContent(MirageContentPageHandler handler, ContentPage page)
    {
        handler.SetContent(page.Content);
    }

    public static void MapPadding(MirageContentPageHandler handler, ContentPage page)
    {
        MirageViewMapper.MapPadding(handler, page);
    }

    private void SetContent(View? content)
    {
        if (_currentContentHandler is not null && !ReferenceEquals(_currentContent, content))
        {
            DisconnectCurrentContent();
        }

        PlatformView.ClearChildren();

        if (content is null || MauiContext is null)
        {
            RequestRender();
            return;
        }

        var contentHandler = content.ToHandler(MauiContext);
        if (contentHandler.PlatformView is not MiragePlatformView contentPlatformView)
        {
            throw new InvalidOperationException("MirageContentPageHandler requires child handlers to expose MiragePlatformView.");
        }

        PlatformView.AddChild(contentPlatformView);
        if (contentHandler is IViewHandler viewHandler && content is IView childView)
        {
            MirageViewMapper.MapLayoutAlignment(viewHandler, childView);
        }

        _currentContent = content;
        _currentContentHandler = contentHandler;
        RequestRender();
    }

    private void DisconnectCurrentContent()
    {
        if (_currentContentHandler is not null)
        {
            _currentContentHandler.DisconnectHandler();
        }

        _currentContentHandler = null;
        _currentContent = null;
        PlatformView.ClearChildren();
        RequestRender();
    }

    private void ApplyBaseStyles()
    {
        PlatformView.SetStyle("display", "grid");
        PlatformView.SetStyle("gridTemplateColumns", "minmax(0, 1fr)");
        PlatformView.SetStyle("gridTemplateRows", "minmax(0, 1fr)");
        PlatformView.SetStyle("justifyItems", "stretch");
        PlatformView.SetStyle("alignItems", "stretch");
        PlatformView.SetStyle("flexDirection", null);
        PlatformView.SetStyle("flexWrap", null);
        PlatformView.SetStyle("justifyContent", null);
        PlatformView.SetStyle("rowGap", null);
        PlatformView.SetStyle("columnGap", null);
        PlatformView.SetStyle("gap", null);
        PlatformView.SetStyle("width", "100%");
        PlatformView.SetStyle("height", "100%");
        PlatformView.SetStyle("minHeight", null);
        PlatformView.SetStyle("overflow", "hidden");
        PlatformView.SetStyle("overflowY", "hidden");
        PlatformView.SetStyle("overflowX", "hidden");
        MirageViewMapper.MapPadding(this, VirtualView);
        RequestRender();
    }
}
