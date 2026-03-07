using Maui.Browser.Dom;
using Maui.Browser.Utils;
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
        platformView.Style("boxSizing", "border-box");
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
            if (content is ScrollView)
            {
                PlatformView.Style("overflow", "hidden");
                content.Measure(frame.Width, frame.Height);
                content.Arrange(new Rect(0, 0, frame.Width, frame.Height));
                return;
            }

            PlatformView.Style("overflow", "auto");
            var desiredSize = content.Measure(frame.Width, double.PositiveInfinity);
            var arrangedHeight = Math.Max(frame.Height, desiredSize.Height);
            content.Arrange(new Rect(0, 0, frame.Width, arrangedHeight));

            if (_currentContentHandler?.PlatformView is DomElement contentPlatformView)
            {
                NormalizeContentPosition(contentPlatformView, frame.Width, arrangedHeight);
            }
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
        PlatformView.Style("overflow", content is ScrollView ? "hidden" : "auto");
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

    private static void NormalizeContentPosition(DomElement contentPlatformView, double arrangedWidth, double arrangedHeight)
    {
        contentPlatformView.Style("position", "relative");
        contentPlatformView.Style("left", null);
        contentPlatformView.Style("top", null);
        contentPlatformView.Style("right", null);
        contentPlatformView.Style("bottom", null);
        contentPlatformView.Style("width", CssUtil.Px(Math.Max(0, arrangedWidth)));
        contentPlatformView.Style("height", CssUtil.Px(Math.Max(0, arrangedHeight)));
    }
}
