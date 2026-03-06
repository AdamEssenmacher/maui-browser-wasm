using System.Globalization;
using Maui.Browser.Dom;
using Maui.Browser.Utils;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Maui.Browser.Handlers;

public sealed class BrowserProgressBarHandler : BrowserViewHandler<IProgress>
{
    public static IPropertyMapper<IProgress, BrowserProgressBarHandler> Mapper = new PropertyMapper<IProgress, BrowserProgressBarHandler>(BrowserViewMapper.CommonViewMapper)
    {
        [nameof(IProgress.Progress)] = MapProgress,
        [nameof(IProgress.ProgressColor)] = MapProgressColor
    };

    public BrowserProgressBarHandler()
        : base(Mapper)
    {
    }

    protected override DomElement CreatePlatformView() => DomElement.Create("progress");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Attr("max", "1");
        platformView.Attr("value", "0");
        platformView.Style("boxSizing", "border-box");
        base.ConnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        const double defaultWidth = 220;
        const double defaultHeight = 18;

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

    public static void MapProgress(BrowserProgressBarHandler handler, IProgress progressBar)
    {
        var clamped = Math.Clamp(progressBar.Progress, 0, 1);
        handler.PlatformView.Attr("value", clamped.ToString("0.###", CultureInfo.InvariantCulture));
    }

    public static void MapProgressColor(BrowserProgressBarHandler handler, IProgress progressBar)
    {
        handler.PlatformView.Style("accentColor", CssUtil.ToCssColor(progressBar.ProgressColor));
    }
}
