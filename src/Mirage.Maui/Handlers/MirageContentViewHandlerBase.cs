using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal abstract class MirageContentViewHandlerBase<TVirtualView> : MirageSingleContentHostHandler<TVirtualView>
    where TVirtualView : class, IView, IContentView
{
    protected MirageContentViewHandlerBase(
        IPropertyMapper mapper,
        CommandMapper? commandMapper = null)
        : base(ComponentKind.ContentContainer, mapper, commandMapper)
    {
        DefaultDisplayStyle = "grid";
    }

    protected abstract IPadding PaddingSource { get; }

    protected virtual bool IsBoundedPageSurface => false;

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        ApplyBaseStyles();
        SetPresentedContent(ResolvePresentedContent());
        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        DisconnectCurrentPresentedContent();
        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        return VirtualView.CrossPlatformMeasure(widthConstraint, heightConstraint);
    }

    public override void PlatformArrange(Rect frame)
    {
        base.PlatformArrange(frame);

        var width = Math.Max(0, frame.Width);
        var height = Math.Max(0, frame.Height);
        VirtualView.CrossPlatformArrange(new Rect(0, 0, width, height));

        if (TryGetPresentedContentPlatformView(out var contentPlatformView))
        {
            NormalizeHostedContentFlowStyles(contentPlatformView);
        }
    }

    protected virtual IView? ResolvePresentedContent() => VirtualView.PresentedContent;

    private void ApplyBaseStyles()
    {
        if (!TryGetPlatformView(out var platformView))
        {
            return;
        }

        platformView.SetStyle("display", "grid");
        platformView.SetStyle("gridTemplateColumns", "minmax(0, 1fr)");
        platformView.SetStyle("gridTemplateRows", "minmax(0, 1fr)");
        platformView.SetStyle("justifyItems", "stretch");
        platformView.SetStyle("alignItems", "stretch");
        platformView.SetStyle("flexDirection", null);
        platformView.SetStyle("flexWrap", null);
        platformView.SetStyle("justifyContent", null);
        platformView.SetStyle("rowGap", null);
        platformView.SetStyle("columnGap", null);
        platformView.SetStyle("gap", null);

        if (IsBoundedPageSurface)
        {
            platformView.SetStyle("width", "100%");
            platformView.SetStyle("height", "100%");
            platformView.SetStyle("minHeight", null);
            platformView.SetStyle("overflow", "hidden");
            platformView.SetStyle("overflowY", "hidden");
            platformView.SetStyle("overflowX", "hidden");
        }
        else
        {
            platformView.SetStyle("width", null);
            platformView.SetStyle("height", null);
            platformView.SetStyle("minHeight", null);
            platformView.SetStyle("overflow", null);
            platformView.SetStyle("overflowY", null);
            platformView.SetStyle("overflowX", null);
        }

        MirageViewMapper.MapPadding(this, PaddingSource);
        RequestRender();
    }
}
