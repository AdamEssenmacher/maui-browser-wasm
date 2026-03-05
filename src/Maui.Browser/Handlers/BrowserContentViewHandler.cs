using Maui.Browser.Dom;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Maui.Browser.Handlers;

public sealed class BrowserContentViewHandler : BrowserViewHandler<IContentView>, IContentViewHandler
{
    public static IPropertyMapper<IContentView, BrowserContentViewHandler> Mapper =
        new PropertyMapper<IContentView, BrowserContentViewHandler>(BrowserViewMapper.CommonViewMapper)
        {
            [nameof(IContentView.Content)] = MapContent
        };

    public BrowserContentViewHandler()
        : base(Mapper)
    {
    }

    private IView? _currentContent;
    private IElementHandler? _currentContentHandler;

    IContentView IContentViewHandler.VirtualView => VirtualView;

    object IContentViewHandler.PlatformView => PlatformView;

    protected override DomElement CreatePlatformView() => DomElement.Create("div");

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        return VirtualView.CrossPlatformMeasure(widthConstraint, heightConstraint);
    }

    public override void PlatformArrange(Rect frame)
    {
        base.PlatformArrange(frame);
        VirtualView.CrossPlatformArrange(new Rect(0, 0, frame.Width, frame.Height));
    }

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Style("overflow", "visible");
        SetContent(VirtualView.PresentedContent);
        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(DomElement platformView)
    {
        DisconnectCurrentContent();
        base.DisconnectHandler(platformView);
    }

    public static void MapContent(BrowserContentViewHandler handler, IContentView contentView)
    {
        handler.SetContent(contentView.PresentedContent);
    }

    private void SetContent(IView? content)
    {
        if (_currentContentHandler is not null && !ReferenceEquals(_currentContent, content))
        {
            DisconnectCurrentContent();
        }

        PlatformView.ClearChildren();

        if (content is null || MauiContext is null)
        {
            return;
        }

        var contentHandler = content.ToHandler(MauiContext);
        if (contentHandler.PlatformView is DomElement childElement)
        {
            PlatformView.Append(childElement);
            _currentContent = content;
            _currentContentHandler = contentHandler;
        }
    }

    private void DisconnectCurrentContent()
    {
        if (_currentContentHandler is null)
        {
            _currentContent = null;
            return;
        }

        _currentContentHandler.DisconnectHandler();
        _currentContentHandler = null;
        _currentContent = null;
    }
}
