using Mirage.Core.Diagnostics;
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

        var diagnosticsEnabled = MirageDiagnostics.IsEnabled;
        var toHandlerStartTimestamp = diagnosticsEnabled ? MirageDiagnostics.GetTimestamp() : 0;
        var contentHandler = content.ToHandler(MauiContext);
        if (diagnosticsEnabled)
        {
            var elapsedMs = MirageDiagnostics.GetElapsedMilliseconds(toHandlerStartTimestamp, MirageDiagnostics.GetTimestamp());
            MirageDiagnostics.Log(
                "handler.content",
                $"handler={GetType().Name} content={content.GetType().Name} childHandler={contentHandler.GetType().Name} toHandlerMs={elapsedMs:0.###}");
        }

        if (contentHandler.PlatformView is not MiragePlatformView contentPlatformView)
        {
            throw new InvalidOperationException("MirageContentPageHandler requires child handlers to expose MiragePlatformView.");
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
        MirageHandlerDisconnect.SafeDisconnect(_currentContentHandler);

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
        platformView.SetStyle("width", "100%");
        platformView.SetStyle("height", "100%");
        platformView.SetStyle("minHeight", null);
        platformView.SetStyle("overflow", "hidden");
        platformView.SetStyle("overflowY", "hidden");
        platformView.SetStyle("overflowX", "hidden");
        MirageViewMapper.MapPadding(this, VirtualView);
        RequestRender();
    }
}
