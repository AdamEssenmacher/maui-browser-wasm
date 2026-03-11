using Mirage.Core.Semantics;
using Microsoft.Maui;
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
        using var renderBatch = BeginRenderBatch();

        if (_currentContentHandler is not null && !ReferenceEquals(_currentContent, content))
        {
            DisconnectCurrentContent();
        }

        _currentContent = content;
        if (!TryGetPlatformView(out var platformView))
        {
            RequestRender();
            return;
        }

        platformView.ClearChildren();

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

        platformView.AddChild(contentPlatformView);
        if (contentHandler is IViewHandler viewHandler && content is IView childView)
        {
            MirageViewMapper.MapLayoutAlignment(viewHandler, childView);
        }
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
        if (TryGetPlatformView(out var platformView))
        {
            platformView.ClearChildren();
        }

        RequestRender();
    }

    private void ApplyBaseStyles()
    {
        if (!TryGetPlatformView(out var platformView))
        {
            return;
        }

        platformView.SetStyle("display", "grid");
        platformView.SetStyle("gridTemplateColumns", "minmax(0, 1fr)");
        platformView.SetStyle("gridTemplateRows", "minmax(0, 1fr)");
        platformView.SetStyle("justifyItems", "stretch");
        platformView.SetStyle("alignItems", "stretch");
        platformView.SetStyle("flexDirection", null);
        platformView.SetStyle("flexWrap", null);
        platformView.SetStyle("justifyContent", null);
        platformView.SetStyle("rowGap", null);
        platformView.SetStyle("columnGap", null);
        platformView.SetStyle("gap", null);
        MirageViewMapper.MapPadding(this, VirtualView);
        RequestRender();
    }
}
