using Maui.Browser.Dom;
using Maui.Browser.Measurement;
using Maui.Browser.Utils;
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
        [nameof(IButton.Padding)] = MapPadding,
        [nameof(IButtonStroke.StrokeColor)] = MapStrokeColor,
        [nameof(IButtonStroke.StrokeThickness)] = MapStrokeThickness,
        [nameof(IButtonStroke.CornerRadius)] = MapCornerRadius,
        ["BorderColor"] = MapStrokeColor,
        ["BorderWidth"] = MapStrokeThickness
    };

    private Action? _pointerClickCallback;
    private Action? _keyboardClickCallback;
    private Action? _pressCallback;
    private Action? _releaseCallback;
    private Action? _cancelCallback;
    private Action? _windowReleaseCallback;
    private int _clickSubscriptionId;
    private int _pressSubscriptionId;
    private int _releaseSubscriptionId;
    private int _cancelSubscriptionId;
    private int _windowReleaseSubscriptionId;
    private bool _isPressed;

    public BrowserButtonHandler()
        : base(Mapper)
    {
    }

    protected override DomElement CreatePlatformView() => DomElement.Create("button");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Attr("type", "button");
        platformView.Style("cursor", "pointer");
        // Keep button text on one line, but do not force clipping/ellipsis; MAUI arrange
        // should decide button bounds and we want full glyph visibility.
        platformView.Style("whiteSpace", "nowrap");
        platformView.Style("display", "flex");
        platformView.Style("alignItems", "center");
        platformView.Style("justifyContent", "center");
        platformView.Style("lineHeight", "1.3");
        platformView.Style("boxSizing", "border-box");

        _pointerClickCallback = OnPointerClick;
        _keyboardClickCallback = OnKeyboardClick;
        _pressCallback = OnPressed;
        _releaseCallback = OnReleased;
        _cancelCallback = OnReleased;
        _windowReleaseCallback = OnReleased;

        _clickSubscriptionId = platformView.OnClickActivation(_pointerClickCallback, _keyboardClickCallback);
        _pressSubscriptionId = platformView.On("pointerdown", _pressCallback);
        _releaseSubscriptionId = platformView.On("pointerup", _releaseCallback);
        _cancelSubscriptionId = platformView.On("pointercancel", _cancelCallback);

        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(DomElement platformView)
    {
        if (_clickSubscriptionId != 0)
        {
            DomElement.Off(_clickSubscriptionId);
            _clickSubscriptionId = 0;
        }

        if (_pressSubscriptionId != 0)
        {
            DomElement.Off(_pressSubscriptionId);
            _pressSubscriptionId = 0;
        }

        if (_releaseSubscriptionId != 0)
        {
            DomElement.Off(_releaseSubscriptionId);
            _releaseSubscriptionId = 0;
        }

        if (_cancelSubscriptionId != 0)
        {
            DomElement.Off(_cancelSubscriptionId);
            _cancelSubscriptionId = 0;
        }

        ReleaseWindowPointerSubscription();

        _pointerClickCallback = null;
        _keyboardClickCallback = null;
        _pressCallback = null;
        _releaseCallback = null;
        _cancelCallback = null;
        _windowReleaseCallback = null;
        _isPressed = false;
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
            minHeight: 40);
    }

    private void OnPointerClick()
    {
        if (VirtualView.IsEnabled)
        {
            VirtualView.Clicked();
        }
    }

    private void OnKeyboardClick()
    {
        if (!VirtualView.IsEnabled)
        {
            return;
        }

        if (VirtualView is IButton keyboardButton)
        {
            keyboardButton.Pressed();
            keyboardButton.Released();
        }

        VirtualView.Clicked();
    }

    private void OnPressed()
    {
        if (!VirtualView.IsEnabled || _isPressed)
        {
            return;
        }

        _isPressed = true;
        PlatformView.Attr("data-maui-button-state", "pressed");
        EnsureWindowPointerSubscription();

        if (VirtualView is IButton button)
        {
            button.Pressed();
        }
    }

    private void OnReleased()
    {
        if (!_isPressed)
        {
            return;
        }

        _isPressed = false;
        PlatformView.Attr("data-maui-button-state", null);
        ReleaseWindowPointerSubscription();

        if (!VirtualView.IsEnabled)
        {
            return;
        }

        if (VirtualView is IButton button)
        {
            button.Released();
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

    public static void MapStrokeColor(BrowserButtonHandler handler, ITextButton button)
    {
        handler.UpdateStrokeStyles(button);
    }

    public static void MapStrokeThickness(BrowserButtonHandler handler, ITextButton button)
    {
        handler.UpdateStrokeStyles(button);
    }

    public static void MapCornerRadius(BrowserButtonHandler handler, ITextButton button)
    {
        if (button is not IButtonStroke strokedButton)
        {
            handler.PlatformView.Style("borderRadius", null);
            return;
        }

        var cornerRadius = strokedButton.CornerRadius;
        if (!double.IsFinite(cornerRadius) || cornerRadius < 0)
        {
            handler.PlatformView.Style("borderRadius", null);
            return;
        }

        handler.PlatformView.Style("borderRadius", CssUtil.Px(cornerRadius));
    }

    private void UpdateStrokeStyles(ITextButton button)
    {
        if (button is not IButtonStroke strokedButton)
        {
            PlatformView.Style("borderStyle", null);
            PlatformView.Style("borderWidth", null);
            PlatformView.Style("borderColor", null);
            return;
        }

        var thickness = strokedButton.StrokeThickness;
        var normalizedThickness = double.IsFinite(thickness) && thickness > 0
            ? thickness
            : 0;

        if (normalizedThickness <= 0)
        {
            PlatformView.Style("borderStyle", null);
            PlatformView.Style("borderWidth", null);
            PlatformView.Style("borderColor", null);
            return;
        }

        var cssColor = CssUtil.ToCssColor(strokedButton.StrokeColor);
        PlatformView.Style("borderStyle", "solid");
        PlatformView.Style("borderWidth", CssUtil.Px(normalizedThickness));
        PlatformView.Style("borderColor", cssColor);
    }

    private void EnsureWindowPointerSubscription()
    {
        if (_windowReleaseSubscriptionId != 0 || _windowReleaseCallback is null)
        {
            return;
        }

        _windowReleaseSubscriptionId = DomElement.SubscribeWindowPointerRelease(_windowReleaseCallback);
    }

    private void ReleaseWindowPointerSubscription()
    {
        if (_windowReleaseSubscriptionId == 0)
        {
            return;
        }

        DomElement.UnsubscribeWindowPointerRelease(_windowReleaseSubscriptionId);
        _windowReleaseSubscriptionId = 0;
    }

}
