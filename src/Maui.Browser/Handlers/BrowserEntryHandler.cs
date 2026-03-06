using Maui.Browser.Dom;
using Maui.Browser.Measurement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Maui.Browser.Handlers;

public sealed class BrowserEntryHandler : BrowserViewHandler<IEntry>
{
    public static IPropertyMapper<IEntry, BrowserEntryHandler> Mapper = new PropertyMapper<IEntry, BrowserEntryHandler>(BrowserViewMapper.CommonViewMapper)
    {
        [nameof(IEntry.Text)] = MapText,
        [nameof(IEntry.Placeholder)] = MapPlaceholder,
        [nameof(IEntry.PlaceholderColor)] = MapPlaceholderColor,
        [nameof(ITextStyle.TextColor)] = MapTextColor,
        [nameof(ITextStyle.Font)] = MapFont,
        [nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
        [nameof(IEntry.IsReadOnly)] = MapIsReadOnly,
        [nameof(IEntry.Keyboard)] = MapKeyboard,
        [nameof(IEntry.IsPassword)] = MapIsPassword,
        [nameof(IEntry.ReturnType)] = MapReturnType,
        [nameof(IEntry.MaxLength)] = MapMaxLength,
        [nameof(IEntry.IsTextPredictionEnabled)] = MapIsTextPredictionEnabled,
        [nameof(IEntry.IsSpellCheckEnabled)] = MapIsSpellCheckEnabled,
        [nameof(ITextAlignment.HorizontalTextAlignment)] = MapHorizontalTextAlignment,
        [nameof(ITextAlignment.VerticalTextAlignment)] = MapVerticalTextAlignment
    };

    private Action? _inputCallback;
    private Action? _enterCallback;
    private int _inputSubscriptionId;
    private int _enterSubscriptionId;

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
        _enterCallback = OnEnter;
        _inputSubscriptionId = platformView.On("input", _inputCallback);
        _enterSubscriptionId = platformView.OnEnter(_enterCallback);

        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(DomElement platformView)
    {
        if (_inputSubscriptionId != 0)
        {
            DomElement.Off(_inputSubscriptionId);
            _inputSubscriptionId = 0;
        }

        if (_enterSubscriptionId != 0)
        {
            DomElement.Off(_enterSubscriptionId);
            _enterSubscriptionId = 0;
        }

        _inputCallback = null;
        _enterCallback = null;
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
        VirtualView.UpdateText(PlatformView.GetInputValue());
    }

    private void OnEnter()
    {
        if (!VirtualView.IsEnabled)
        {
            return;
        }

        VirtualView.Completed();
    }

    public static void MapText(BrowserEntryHandler handler, IEntry entry)
    {
        handler.PlatformView.SetInputValue(entry.Text);
    }

    public static void MapPlaceholder(BrowserEntryHandler handler, IEntry entry)
    {
        BrowserTextInputMappings.MapPlaceholder(entry, handler.PlatformView);
    }

    public static void MapPlaceholderColor(BrowserEntryHandler handler, IEntry entry)
    {
        BrowserTextInputMappings.MapPlaceholderColor(entry, handler.PlatformView);
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

    public static void MapIsReadOnly(BrowserEntryHandler handler, IEntry entry)
    {
        BrowserTextInputMappings.MapReadOnly(entry, handler.PlatformView);
    }

    public static void MapKeyboard(BrowserEntryHandler handler, IEntry entry)
    {
        BrowserTextInputMappings.MapKeyboard(entry, handler.PlatformView, multiline: false, forcePassword: entry.IsPassword);
    }

    public static void MapIsPassword(BrowserEntryHandler handler, IEntry entry)
    {
        BrowserTextInputMappings.MapKeyboard(entry, handler.PlatformView, multiline: false, forcePassword: entry.IsPassword);
    }

    public static void MapReturnType(BrowserEntryHandler handler, IEntry entry)
    {
        BrowserTextInputMappings.MapReturnType(entry.ReturnType, handler.PlatformView);
    }

    public static void MapMaxLength(BrowserEntryHandler handler, IEntry entry)
    {
        BrowserTextInputMappings.MapMaxLength(entry, handler.PlatformView);
    }

    public static void MapIsTextPredictionEnabled(BrowserEntryHandler handler, IEntry entry)
    {
        BrowserTextInputMappings.MapPrediction(entry, handler.PlatformView);
    }

    public static void MapIsSpellCheckEnabled(BrowserEntryHandler handler, IEntry entry)
    {
        BrowserTextInputMappings.MapSpellCheck(entry, handler.PlatformView);
    }

    public static void MapHorizontalTextAlignment(BrowserEntryHandler handler, IEntry entry)
    {
        BrowserTextInputMappings.MapHorizontalTextAlignment(entry, handler.PlatformView);
    }

    public static void MapVerticalTextAlignment(BrowserEntryHandler handler, IEntry entry)
    {
        BrowserTextInputMappings.MapVerticalTextAlignment(entry, handler.PlatformView);
    }
}
