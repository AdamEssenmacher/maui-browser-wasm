using Mirage.Core.Diagnostics;
using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Mirage.Maui.Handlers;

internal abstract class MirageSingleContentHostHandler<TVirtualView> : MirageViewHandler<TVirtualView>
    where TVirtualView : class, IView, IContentView
{
    private IView? _currentPresentedContent;
    private IElementHandler? _currentContentHandler;

    protected MirageSingleContentHostHandler(
        ComponentKind kind,
        IPropertyMapper mapper,
        CommandMapper? commandMapper = null)
        : base(kind, mapper, commandMapper)
    {
    }

    protected MirageSingleContentHostHandler(
        Func<MiragePlatformView> platformViewFactory,
        IPropertyMapper mapper,
        CommandMapper? commandMapper = null)
        : base(platformViewFactory, mapper, commandMapper)
    {
    }

    protected IView? CurrentPresentedContent => _currentPresentedContent;

    protected bool TryGetPresentedContentPlatformView(out MiragePlatformView contentPlatformView)
    {
        if (_currentContentHandler?.PlatformView is MiragePlatformView childPlatformView)
        {
            contentPlatformView = childPlatformView;
            return true;
        }

        contentPlatformView = null!;
        return false;
    }

    protected void SetPresentedContent(IView? content)
    {
        if (_currentContentHandler is not null && !ReferenceEquals(_currentPresentedContent, content))
        {
            DisconnectCurrentPresentedContent();
        }

        _currentPresentedContent = content;
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
        IElementHandler contentHandler;
        try
        {
            contentHandler = content.ToHandler(MauiContext);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to create handler for presented content type '{content.GetType().FullName}' in host handler '{GetType().FullName}'.",
                ex);
        }

        if (diagnosticsEnabled)
        {
            var elapsedMs = MirageDiagnostics.GetElapsedMilliseconds(toHandlerStartTimestamp, MirageDiagnostics.GetTimestamp());
            MirageDiagnostics.Log(
                "handler.content",
                $"handler={GetType().Name} content={content.GetType().Name} childHandler={contentHandler.GetType().Name} toHandlerMs={elapsedMs:0.###}");
        }

        if (contentHandler.PlatformView is not MiragePlatformView contentPlatformView)
        {
            throw new InvalidOperationException($"{GetType().Name} requires child handlers to expose MiragePlatformView.");
        }

        platformView.AddChild(contentPlatformView);
        if (contentHandler is IViewHandler viewHandler)
        {
            MirageViewMapper.MapLayoutAlignment(viewHandler, content);
        }

        _currentContentHandler = contentHandler;
        OnPresentedContentConnected(content, contentHandler, contentPlatformView);
        RequestRender();
    }

    protected void DisconnectCurrentPresentedContent()
    {
        MirageHandlerDisconnect.SafeDisconnect(_currentContentHandler);

        _currentContentHandler = null;
        _currentPresentedContent = null;
        if (TryGetPlatformView(out var platformView))
        {
            platformView.ClearChildren();
        }

        OnPresentedContentDisconnected();
        RequestRender();
    }

    protected static void NormalizeHostedContentFlowStyles(MiragePlatformView contentPlatformView)
    {
        // Ensure flow-hosted content does not carry stale absolute offsets from prior parents.
        contentPlatformView.SetStyle("position", "relative");
        contentPlatformView.SetStyle("left", null);
        contentPlatformView.SetStyle("top", null);
        contentPlatformView.SetStyle("right", null);
        contentPlatformView.SetStyle("bottom", null);
    }

    protected virtual void OnPresentedContentConnected(IView content, IElementHandler contentHandler, MiragePlatformView contentPlatformView)
    {
        _ = content;
        _ = contentHandler;
        _ = contentPlatformView;
    }

    protected virtual void OnPresentedContentDisconnected()
    {
    }
}
