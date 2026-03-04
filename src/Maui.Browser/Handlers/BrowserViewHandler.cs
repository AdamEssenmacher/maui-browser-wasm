using Maui.Browser.Dom;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Maui.Browser.Handlers;

public abstract class BrowserViewHandler<TVirtualView> : ViewHandler<TVirtualView, DomElement>
    where TVirtualView : class, IView
{
    protected BrowserViewHandler(IPropertyMapper mapper, CommandMapper? commandMapper = null)
        : base(mapper, commandMapper)
    {
    }

    public override void PlatformArrange(Rect frame)
    {
        BrowserViewMapper.ApplyFrame(PlatformView, frame);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        var width = double.IsPositiveInfinity(widthConstraint) || double.IsNaN(widthConstraint)
            ? 0
            : Math.Max(0, widthConstraint);

        var height = double.IsPositiveInfinity(heightConstraint) || double.IsNaN(heightConstraint)
            ? 0
            : Math.Max(0, heightConstraint);

        return new Size(width, height);
    }

    protected override void DisconnectHandler(DomElement platformView)
    {
        base.DisconnectHandler(platformView);
        platformView.Release();
    }
}
