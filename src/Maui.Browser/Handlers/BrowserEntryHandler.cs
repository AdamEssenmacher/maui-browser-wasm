using Maui.Browser.Dom;
using Maui.Browser.Measurement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Maui.Browser.Handlers;

public sealed class BrowserEntryHandler : BrowserViewHandler<IEntry>
{
    public static IPropertyMapper<IEntry, BrowserEntryHandler> Mapper = new PropertyMapper<IEntry, BrowserEntryHandler>(ViewHandler.ViewMapper)
    {
        [nameof(IEntry.Text)] = MapText,
        [nameof(IEntry.Placeholder)] = MapPlaceholder,
        [nameof(ITextStyle.TextColor)] = MapTextColor,
        [nameof(ITextStyle.Font)] = MapFont,
        [nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
        [nameof(IView.Visibility)] = MapVisibility,
        [nameof(IView.Opacity)] = MapOpacity,
        [nameof(IView.IsEnabled)] = MapIsEnabled,
        [nameof(IEntry.IsReadOnly)] = MapIsReadOnly
    };

    private Action? _inputCallback;
    private int _inputSubscriptionId;

    public BrowserEntryHandler()
        : base(Mapper)
    {
    }

    protected override DomElement CreatePlatformView() => DomElement.Create("input");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Attr("type", "text");
        platformView.Style("boxSizing", "border-box");

        _inputCallback = OnInput;
        _inputSubscriptionId = platformView.On("input", _inputCallback);

        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(DomElement platformView)
    {
        if (_inputSubscriptionId != 0)
        {
            DomElement.Off(_inputSubscriptionId);
            _inputSubscriptionId = 0;
        }

        _inputCallback = null;
        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        var measurer = MauiContext?.Services.GetService<IBrowserTextMeasurer>();
        if (measurer is null)
        {
            return base.GetDesiredSize(widthConstraint, heightConstraint);
        }

        var textForMeasure = string.IsNullOrWhiteSpace(VirtualView.Text)
            ? VirtualView.Placeholder
            : VirtualView.Text;

        return measurer.MeasureText(
            textForMeasure,
            VirtualView.Font,
            widthConstraint,
            heightConstraint,
            new Thickness(10, 8),
            minWidth: 140,
            minHeight: 36);
    }

    private void OnInput()
    {
        var inputValue = PlatformView.GetInputValue();
        VirtualView.UpdateText(inputValue);
    }

    public static void MapText(BrowserEntryHandler handler, IEntry entry)
    {
        handler.PlatformView.SetInputValue(entry.Text);
    }

    public static void MapPlaceholder(BrowserEntryHandler handler, IEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Placeholder))
        {
            handler.PlatformView.Attr("placeholder", null);
            return;
        }

        handler.PlatformView.Attr("placeholder", entry.Placeholder);
    }

    public static void MapTextColor(BrowserEntryHandler handler, IEntry entry)
    {
        BrowserViewMapper.MapTextColor(entry, handler.PlatformView);
    }

    public static void MapFont(BrowserEntryHandler handler, IEntry entry)
    {
        BrowserViewMapper.MapFont(entry, handler.PlatformView);
    }

    public static void MapCharacterSpacing(BrowserEntryHandler handler, IEntry entry)
    {
        BrowserViewMapper.MapCharacterSpacing(entry, handler.PlatformView);
    }

    public static void MapVisibility(BrowserEntryHandler handler, IEntry entry)
    {
        BrowserViewMapper.MapVisibility(entry, handler.PlatformView);
    }

    public static void MapOpacity(BrowserEntryHandler handler, IEntry entry)
    {
        BrowserViewMapper.MapOpacity(entry, handler.PlatformView);
    }

    public static void MapIsEnabled(BrowserEntryHandler handler, IEntry entry)
    {
        BrowserViewMapper.MapEnabled(entry, handler.PlatformView);
    }

    public static void MapIsReadOnly(BrowserEntryHandler handler, IEntry entry)
    {
        handler.PlatformView.Attr("readonly", entry.IsReadOnly ? "readonly" : null);
    }
}
