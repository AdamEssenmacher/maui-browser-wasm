using Maui.Browser.Dom;
using Maui.Browser.Utils;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Maui.Browser.Handlers;

public sealed class BrowserActivityIndicatorHandler : BrowserViewHandler<IActivityIndicator>
{
    public static IPropertyMapper<IActivityIndicator, BrowserActivityIndicatorHandler> Mapper = new PropertyMapper<IActivityIndicator, BrowserActivityIndicatorHandler>(BrowserViewMapper.CommonViewMapper)
    {
        [nameof(IActivityIndicator.Color)] = MapColor,
        [nameof(IView.Visibility)] = MapVisibility,
        [nameof(IActivityIndicator.IsRunning)] = MapIsRunning
    };

    public BrowserActivityIndicatorHandler()
        : base(Mapper)
    {
    }

    protected override DomElement CreatePlatformView() => DomElement.Create("progress");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Attr("max", "1");
        platformView.Attr("data-maui-activity-indicator", "true");
        platformView.Style("boxSizing", "border-box");
        base.ConnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        const double defaultWidth = 36;
        const double defaultHeight = 36;

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

    public static void MapColor(BrowserActivityIndicatorHandler handler, IActivityIndicator activityIndicator)
    {
        handler.PlatformView.Style("accentColor", CssUtil.ToCssColor(activityIndicator.Color));
    }

    public static void MapIsRunning(BrowserActivityIndicatorHandler handler, IActivityIndicator activityIndicator)
    {
        ApplyRunningVisibility(handler, activityIndicator);
    }

    public static void MapVisibility(BrowserActivityIndicatorHandler handler, IActivityIndicator activityIndicator)
    {
        ApplyRunningVisibility(handler, activityIndicator);
    }

    private static void ApplyRunningVisibility(BrowserActivityIndicatorHandler handler, IActivityIndicator activityIndicator)
    {
        if (activityIndicator.IsRunning)
        {
            handler.PlatformView.Attr("aria-busy", "true");
            handler.PlatformView.Attr("value", null);
            BrowserViewMapper.MapVisibility(activityIndicator, handler.PlatformView);
            return;
        }

        handler.PlatformView.Attr("aria-busy", "false");
        handler.PlatformView.Attr("value", "0");
        handler.PlatformView.Style("display", "none");
        handler.PlatformView.Style("visibility", "hidden");
    }
}
