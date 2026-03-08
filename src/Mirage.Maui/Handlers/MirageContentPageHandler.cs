using Mirage.Core.Semantics;
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
        DefaultDisplayStyle = "flex";
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
        PlatformView.SetStyle("display", "flex");
        PlatformView.SetStyle("flexDirection", "column");
        PlatformView.SetStyle("alignItems", "stretch");
        PlatformView.SetStyle("width", "100%");
        PlatformView.SetStyle("minHeight", "100%");
        MirageViewMapper.MapPadding(this, VirtualView);
        RequestRender();
    }
}
