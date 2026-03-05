using Maui.Browser.Dom;
using Maui.Browser.Measurement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Maui.Browser.Handlers;

public sealed class BrowserLabelHandler : BrowserViewHandler<ILabel>
{
    public static IPropertyMapper<ILabel, BrowserLabelHandler> Mapper = new PropertyMapper<ILabel, BrowserLabelHandler>(BrowserViewMapper.CommonViewMapper)
    {
        [nameof(ILabel.Text)] = MapText,
        [nameof(ITextStyle.TextColor)] = MapTextColor,
        [nameof(ITextStyle.Font)] = MapFont,
        [nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
        [nameof(ITextAlignment.HorizontalTextAlignment)] = MapTextAlignment
    };

    public BrowserLabelHandler()
        : base(Mapper)
    {
    }

    protected override DomElement CreatePlatformView() => DomElement.Create("span");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Style("whiteSpace", "pre-wrap");
        platformView.Style("lineHeight", "1.2");
        platformView.Style("overflow", "visible");
        base.ConnectHandler(platformView);
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
            new Thickness(0),
            minWidth: 1,
            minHeight: Math.Max(1, VirtualView.Font.Size > 0 ? VirtualView.Font.Size : 14));
    }

    public static void MapText(BrowserLabelHandler handler, ILabel label)
    {
        handler.PlatformView.Text(label.Text ?? string.Empty);
    }

    public static void MapTextColor(BrowserLabelHandler handler, ILabel label)
    {
        BrowserViewMapper.MapTextColor(label, handler.PlatformView);
    }

    public static void MapFont(BrowserLabelHandler handler, ILabel label)
    {
        BrowserViewMapper.MapFont(label, handler.PlatformView);
    }

    public static void MapCharacterSpacing(BrowserLabelHandler handler, ILabel label)
    {
        BrowserViewMapper.MapCharacterSpacing(label, handler.PlatformView);
    }

    public static void MapTextAlignment(BrowserLabelHandler handler, ILabel label)
    {
        BrowserViewMapper.MapTextAlignment(label, handler.PlatformView);
    }

}
