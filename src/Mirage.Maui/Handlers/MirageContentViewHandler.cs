using Mirage.Core.Semantics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Mirage.Maui.Handlers;

internal sealed class MirageContentViewHandler : MirageViewHandler<ContentView>
{
    public static IPropertyMapper<ContentView, MirageContentViewHandler> Mapper =
        new PropertyMapper<ContentView, MirageContentViewHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(ContentView.Content)] = MapContent,
            [nameof(ContentView.Padding)] = MapPadding
        };

    private View? _currentContent;
    private IElementHandler? _currentContentHandler;

    public MirageContentViewHandler()
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

    public static void MapContent(MirageContentViewHandler handler, ContentView contentView)
    {
        handler.SetContent(contentView.Content);
    }

    public static void MapPadding(MirageContentViewHandler handler, ContentView contentView)
    {
        MirageViewMapper.MapPadding(handler, contentView);
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
            throw new InvalidOperationException("MirageContentViewHandler requires child handlers to expose MiragePlatformView.");
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
        MirageViewMapper.MapPadding(this, VirtualView);
        RequestRender();
    }
}
