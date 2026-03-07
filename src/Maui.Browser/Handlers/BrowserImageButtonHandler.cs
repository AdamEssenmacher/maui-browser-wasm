using Maui.Browser.Dom;
using Maui.Browser.Utils;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Maui.Browser.Handlers;

public sealed class BrowserImageButtonHandler : BrowserViewHandler<ImageButton>
{
    public static IPropertyMapper<ImageButton, BrowserImageButtonHandler> Mapper = new PropertyMapper<ImageButton, BrowserImageButtonHandler>(BrowserViewMapper.CommonViewMapper)
    {
        [nameof(IView.AutomationId)] = MapAutomationId,
        [nameof(IView.Visibility)] = MapVisibility,
        [nameof(ImageButton.Source)] = MapSource,
        [nameof(ImageButton.Aspect)] = MapAspect,
        [nameof(ImageButton.Padding)] = MapPadding,
        [nameof(ImageButton.BorderColor)] = MapBorderColor,
        [nameof(ImageButton.BorderWidth)] = MapBorderWidth,
        [nameof(ImageButton.CornerRadius)] = MapCornerRadius,
        [nameof(ImageButton.IsOpaque)] = MapIsOpaque
    };

    private DomElement? _imageElement;
    private Action? _clickCallback;
    private Action? _pressCallback;
    private Action? _releaseCallback;
    private Action? _cancelCallback;
    private Action? _loadCallback;
    private Action? _errorCallback;
    private int _clickSubscriptionId;
    private int _pressSubscriptionId;
    private int _releaseSubscriptionId;
    private int _cancelSubscriptionId;
    private int _loadSubscriptionId;
    private int _errorSubscriptionId;

    public BrowserImageButtonHandler()
        : base(Mapper)
    {
    }

    protected override DomElement CreatePlatformView() => DomElement.Create("button");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Attr("type", "button");
        platformView.Style("display", "inline-flex");
        platformView.Style("alignItems", "center");
        platformView.Style("justifyContent", "center");
        platformView.Style("overflow", "hidden");
        platformView.Style("boxSizing", "border-box");

        _imageElement = DomElement.Create("img");
        _imageElement.Attr("alt", string.Empty);
        _imageElement.Style("display", "block");
        _imageElement.Style("width", "100%");
        _imageElement.Style("height", "100%");
        _imageElement.Style("objectPosition", "center center");
        _imageElement.Style("pointerEvents", "none");
        platformView.Append(_imageElement);

        _clickCallback = OnClick;
        _pressCallback = OnPressed;
        _releaseCallback = OnReleased;
        _cancelCallback = OnReleased;
        _loadCallback = OnImageLoaded;
        _errorCallback = OnImageFailed;

        _clickSubscriptionId = platformView.On("click", _clickCallback);
        _pressSubscriptionId = platformView.On("pointerdown", _pressCallback);
        _releaseSubscriptionId = platformView.On("pointerup", _releaseCallback);
        _cancelSubscriptionId = platformView.On("pointercancel", _cancelCallback);
        _loadSubscriptionId = _imageElement.On("load", _loadCallback);
        _errorSubscriptionId = _imageElement.On("error", _errorCallback);

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

        if (_loadSubscriptionId != 0)
        {
            DomElement.Off(_loadSubscriptionId);
            _loadSubscriptionId = 0;
        }

        if (_errorSubscriptionId != 0)
        {
            DomElement.Off(_errorSubscriptionId);
            _errorSubscriptionId = 0;
        }

        _clickCallback = null;
        _pressCallback = null;
        _releaseCallback = null;
        _cancelCallback = null;
        _loadCallback = null;
        _errorCallback = null;
        _imageElement = null;
        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        const double defaultWidth = 52;
        const double defaultHeight = 52;

        return new Size(
            ResolveDesiredDimension(widthConstraint, defaultWidth),
            ResolveDesiredDimension(heightConstraint, defaultHeight));
    }

    private static double ResolveDesiredDimension(double constraint, double fallback)
    {
        if (double.IsNaN(constraint) || double.IsInfinity(constraint))
        {
            return fallback;
        }

        return Math.Max(0, Math.Min(fallback, constraint));
    }

    private void OnClick()
    {
        if (!VirtualView.IsEnabled)
        {
            return;
        }

        ((IButton)VirtualView).Clicked();
    }

    private void OnPressed()
    {
        if (!VirtualView.IsEnabled)
        {
            return;
        }

        ((IButton)VirtualView).Pressed();
    }

    private void OnReleased()
    {
        if (!VirtualView.IsEnabled)
        {
            return;
        }

        ((IButton)VirtualView).Released();
    }

    private void OnImageLoaded()
    {
        _imageElement?.Attr("data-maui-image-state", "loaded");
        PlatformView.Attr("data-maui-image-state", "loaded");
    }

    private void OnImageFailed()
    {
        _imageElement?.Attr("data-maui-image-state", "error");
        PlatformView.Attr("data-maui-image-state", "error");
    }

    private void UpdateSourceProjection(ImageButton imageButton)
    {
        if (_imageElement is null)
        {
            return;
        }

        var projection = BrowserImageSourceMapper.BuildProjection(imageButton.Source, imageButton);
        BrowserImageSourceMapper.ApplyProjection(_imageElement, projection);
        PlatformView.Attr("data-maui-image-source-kind", projection.SourceKind);
        PlatformView.Attr("data-maui-image-state", projection.InitialState);
    }

    private void UpdateImageAutomationId(ImageButton imageButton)
    {
        var automationId = string.IsNullOrWhiteSpace(imageButton.AutomationId)
            ? null
            : $"{imageButton.AutomationId}-image";
        _imageElement?.Attr("data-automation-id", automationId);
    }

    public static void MapAutomationId(BrowserImageButtonHandler handler, ImageButton imageButton)
    {
        BrowserViewMapper.MapAutomationId(handler, imageButton);
        handler.UpdateImageAutomationId(imageButton);
    }

    public static void MapVisibility(BrowserImageButtonHandler handler, ImageButton imageButton)
    {
        BrowserViewMapper.MapVisibility(imageButton, handler.PlatformView);
        if (((IView)imageButton).Visibility != Visibility.Collapsed)
        {
            handler.PlatformView.Style("display", "inline-flex");
        }
    }

    public static void MapSource(BrowserImageButtonHandler handler, ImageButton imageButton)
    {
        handler.UpdateSourceProjection(imageButton);
    }

    public static void MapAspect(BrowserImageButtonHandler handler, ImageButton imageButton)
    {
        if (handler._imageElement is null)
        {
            return;
        }

        BrowserImageSourceMapper.ApplyAspect(handler._imageElement, imageButton.Aspect);
    }

    public static void MapPadding(BrowserImageButtonHandler handler, ImageButton imageButton)
    {
        BrowserViewMapper.MapPadding(imageButton, handler.PlatformView);
    }

    public static void MapBorderColor(BrowserImageButtonHandler handler, ImageButton imageButton)
    {
        handler.PlatformView.Style("borderColor", CssUtil.ToCssColor(imageButton.BorderColor));
        handler.UpdateBorderStyle(imageButton);
    }

    public static void MapBorderWidth(BrowserImageButtonHandler handler, ImageButton imageButton)
    {
        var width = imageButton.BorderWidth;
        var normalizedWidth = double.IsFinite(width) ? Math.Max(0, width) : 0;
        handler.PlatformView.Style("borderWidth", CssUtil.Px(normalizedWidth));
        handler.UpdateBorderStyle(imageButton);
    }

    public static void MapCornerRadius(BrowserImageButtonHandler handler, ImageButton imageButton)
    {
        var cornerRadius = imageButton.CornerRadius;
        if (cornerRadius <= 0)
        {
            handler.PlatformView.Style("borderRadius", null);
            return;
        }

        handler.PlatformView.Style("borderRadius", CssUtil.Px(cornerRadius));
    }

    public static void MapIsOpaque(BrowserImageButtonHandler handler, ImageButton imageButton)
    {
        handler.PlatformView.Attr("data-maui-image-opaque", imageButton.IsOpaque ? "true" : null);
    }

    private void UpdateBorderStyle(ImageButton imageButton)
    {
        var width = imageButton.BorderWidth;
        var hasBorder = double.IsFinite(width) && width > 0;
        PlatformView.Style("borderStyle", hasBorder ? "solid" : "none");
    }
}
