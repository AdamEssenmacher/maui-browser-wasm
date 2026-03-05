using Maui.Browser.Dom;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Maui.Browser.Handlers;

public sealed class BrowserContentPageHandler : BrowserViewHandler<ContentPage>
{
    public static IPropertyMapper<ContentPage, BrowserContentPageHandler> Mapper =
        new PropertyMapper<ContentPage, BrowserContentPageHandler>(BrowserViewMapper.CommonViewMapper)
        {
            [nameof(ContentPage.Content)] = MapContent
        };

    private View? _currentContent;
    private IElementHandler? _currentContentHandler;

    public BrowserContentPageHandler()
        : base(Mapper)
    {
    }

    protected override DomElement CreatePlatformView() => DomElement.Create("div");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Style("overflow", "visible");
        SetContent(VirtualView.Content);
        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(DomElement platformView)
    {
        DisconnectCurrentContent();
        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        if (VirtualView.Content is IView content)
        {
            return content.Measure(widthConstraint, heightConstraint);
        }

        return base.GetDesiredSize(widthConstraint, heightConstraint);
    }

    public override void PlatformArrange(Rect frame)
    {
        base.PlatformArrange(frame);

        if (VirtualView.Content is IView content)
        {
            content.Measure(frame.Width, frame.Height);
            content.Arrange(new Rect(0, 0, frame.Width, frame.Height));
        }
    }

    public static void MapContent(BrowserContentPageHandler handler, ContentPage page)
    {
        handler.SetContent(page.Content);
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
            return;
        }

        var contentHandler = content.ToHandler(MauiContext);
        if (contentHandler.PlatformView is not DomElement childElement)
        {
            return;
        }

        PlatformView.Append(childElement);
        _currentContent = content;
        _currentContentHandler = contentHandler;
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
