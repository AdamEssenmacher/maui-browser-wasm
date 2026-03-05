using Maui.Browser.Dom;
using Maui.Browser.Measurement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Maui.Browser.Handlers;

public sealed class BrowserButtonHandler : BrowserViewHandler<ITextButton>
{
    public static IPropertyMapper<ITextButton, BrowserButtonHandler> Mapper = new PropertyMapper<ITextButton, BrowserButtonHandler>(BrowserViewMapper.CommonViewMapper)
    {
        [nameof(IText.Text)] = MapText,
        [nameof(ITextStyle.TextColor)] = MapTextColor,
        [nameof(ITextStyle.Font)] = MapFont,
        [nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
        [nameof(IButton.Padding)] = MapPadding
    };

    private Action? _clickCallback;
    private int _clickSubscriptionId;

    public BrowserButtonHandler()
        : base(Mapper)
    {
    }

    protected override DomElement CreatePlatformView() => DomElement.Create("button");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Attr("type", "button");
        platformView.Style("cursor", "pointer");

        _clickCallback = OnClick;
        _clickSubscriptionId = platformView.On("click", _clickCallback);

        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(DomElement platformView)
    {
        if (_clickSubscriptionId != 0)
        {
            DomElement.Off(_clickSubscriptionId);
            _clickSubscriptionId = 0;
        }

        _clickCallback = null;
        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        var measurer = MauiContext?.Services.GetService<IBrowserTextMeasurer>();
        if (measurer is null)
        {
            return base.GetDesiredSize(widthConstraint, heightConstraint);
        }

        return measurer.MeasureText(
            VirtualView.Text,
            VirtualView.Font,
            widthConstraint,
            heightConstraint,
            VirtualView.Padding,
            minWidth: 88,
            minHeight: 36);
    }

    private void OnClick()
    {
        if (VirtualView.IsEnabled)
        {
            VirtualView.Clicked();
        }
    }

    public static void MapText(BrowserButtonHandler handler, ITextButton button)
    {
        handler.PlatformView.Text(button.Text ?? string.Empty);
    }

    public static void MapTextColor(BrowserButtonHandler handler, ITextButton button)
    {
        BrowserViewMapper.MapTextColor(button, handler.PlatformView);
    }

    public static void MapFont(BrowserButtonHandler handler, ITextButton button)
    {
        BrowserViewMapper.MapFont(button, handler.PlatformView);
    }

    public static void MapCharacterSpacing(BrowserButtonHandler handler, ITextButton button)
    {
        BrowserViewMapper.MapCharacterSpacing(button, handler.PlatformView);
    }

    public static void MapPadding(BrowserButtonHandler handler, ITextButton button)
    {
        BrowserViewMapper.MapPadding(button, handler.PlatformView);
    }

}
