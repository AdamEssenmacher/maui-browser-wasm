using Maui.Browser.Dom;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Maui.Browser.Handlers;

public sealed class BrowserImageHandler : BrowserViewHandler<Image>
{
    public static IPropertyMapper<Image, BrowserImageHandler> Mapper = new PropertyMapper<Image, BrowserImageHandler>(BrowserViewMapper.CommonViewMapper)
    {
        [nameof(Image.Source)] = MapSource,
        [nameof(Image.Aspect)] = MapAspect,
        [nameof(Image.IsOpaque)] = MapIsOpaque
    };

    private Action? _loadCallback;
    private Action? _errorCallback;
    private int _loadSubscriptionId;
    private int _errorSubscriptionId;

    public BrowserImageHandler()
        : base(Mapper)
    {
    }

    protected override DomElement CreatePlatformView() => DomElement.Create("img");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Attr("alt", string.Empty);
        platformView.Style("display", "block");
        platformView.Style("width", "100%");
        platformView.Style("height", "100%");
        platformView.Style("boxSizing", "border-box");
        platformView.Style("objectPosition", "center center");

        _loadCallback = OnImageLoaded;
        _errorCallback = OnImageFailed;
        _loadSubscriptionId = platformView.On("load", _loadCallback);
        _errorSubscriptionId = platformView.On("error", _errorCallback);

        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(DomElement platformView)
    {
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

        _loadCallback = null;
        _errorCallback = null;
        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        const double defaultWidth = 120;
        const double defaultHeight = 90;

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

    private void OnImageLoaded()
    {
        PlatformView.Attr("data-maui-image-state", "loaded");
    }

    private void OnImageFailed()
    {
        PlatformView.Attr("data-maui-image-state", "error");
    }

    public static void MapSource(BrowserImageHandler handler, Image image)
    {
        var projection = BrowserImageSourceMapper.BuildProjection(image.Source, image);
        BrowserImageSourceMapper.ApplyProjection(handler.PlatformView, projection);
    }

    public static void MapAspect(BrowserImageHandler handler, Image image)
    {
        BrowserImageSourceMapper.ApplyAspect(handler.PlatformView, image.Aspect);
    }

    public static void MapIsOpaque(BrowserImageHandler handler, Image image)
    {
        handler.PlatformView.Attr("data-maui-image-opaque", image.IsOpaque ? "true" : null);
    }
}
