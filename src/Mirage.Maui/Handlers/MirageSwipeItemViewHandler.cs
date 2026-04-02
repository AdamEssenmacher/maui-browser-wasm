using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageSwipeItemViewHandler : MirageSingleContentHostHandler<SwipeItemView>
{
    private const double DefaultWidth = 88;
    private const double DefaultHeight = 56;
    private IView? _pendingPresentedContent;
    private bool _contentAttachmentEnabled;

    internal static readonly string[] ParityPropertyKeys =
    [
        "Content",
        "Visibility"
    ];

    internal static readonly string[] ParityCommandKeys = [];

    public static IPropertyMapper<SwipeItemView, MirageSwipeItemViewHandler> Mapper =
        new PropertyMapper<SwipeItemView, MirageSwipeItemViewHandler>(MirageViewMapper.CommonViewMapper)
        {
            ["Content"] = MapContent,
            ["Visibility"] = MapVisibility
        };

    public static CommandMapper<SwipeItemView, MirageSwipeItemViewHandler> CommandMapper =
        new(MirageViewMapper.CommonViewCommandMapper);

    public MirageSwipeItemViewHandler()
        : base(() => MiragePlatformView.CreateWeb("div"), Mapper, CommandMapper)
    {
        DefaultDisplayStyle = "flex";
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        if (CurrentPresentedContent is not IView content)
        {
            return new Size(DefaultWidth, DefaultHeight);
        }

        var measured = content.Measure(widthConstraint, heightConstraint);
        var width = measured.Width > 0 ? measured.Width : DefaultWidth;
        var height = measured.Height > 0 ? measured.Height : DefaultHeight;
        return new Size(width, height);
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        base.ConnectHandler(platformView);
        _pendingPresentedContent = ResolvePresentedContent(VirtualView);
        _contentAttachmentEnabled = false;

        if (ApplyRootProjection(VirtualView))
        {
            RequestRender();
        }
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        DisconnectCurrentPresentedContent();
        _pendingPresentedContent = null;
        _contentAttachmentEnabled = false;
        base.DisconnectHandler(platformView);
    }

    public override void PlatformArrange(Rect frame)
    {
        EnsurePresentedContentConnected();
        base.PlatformArrange(frame);

        if (CurrentPresentedContent is not IView content)
        {
            return;
        }

        content.Arrange(new Rect(0, 0, Math.Max(0, frame.Width), Math.Max(0, frame.Height)));
    }

    public static void MapContent(MirageSwipeItemViewHandler handler, SwipeItemView itemView)
    {
        handler._pendingPresentedContent = ResolvePresentedContent(itemView);

        if (!handler._contentAttachmentEnabled)
        {
            handler.RequestRender();
            return;
        }

        handler.SetPresentedContent(handler._pendingPresentedContent);
    }

    public static void MapVisibility(MirageSwipeItemViewHandler handler, SwipeItemView itemView)
    {
        if (!handler.ApplyRootProjection(itemView))
        {
            return;
        }

        handler.RequestRender();
    }

    protected override void OnPresentedContentConnected(IView content, IElementHandler contentHandler, MiragePlatformView contentPlatformView)
    {
        NormalizeHostedContentFlowStyles(contentPlatformView);
        contentPlatformView.SetStyle("display", "flex");
        contentPlatformView.SetStyle("flex", "1 1 auto");
        contentPlatformView.SetStyle("minWidth", "0");
        contentPlatformView.SetStyle("minHeight", "0");
        base.OnPresentedContentConnected(content, contentHandler, contentPlatformView);
    }

    private bool ApplyRootProjection(SwipeItemView itemView)
    {
        if (!TryGetPlatformView(out var platformView))
        {
            return false;
        }

        var changed = SetStyleIfChanged(platformView, "display", itemView.IsVisible ? DefaultDisplayStyle : "none");
        changed |= SetStyleIfChanged(platformView, "alignItems", "stretch");
        changed |= SetStyleIfChanged(platformView, "justifyContent", "stretch");
        changed |= SetStyleIfChanged(platformView, "boxSizing", "border-box");
        changed |= SetStyleIfChanged(platformView, "width", "100%");
        changed |= SetStyleIfChanged(platformView, "height", "100%");
        changed |= SetStyleIfChanged(platformView, "minWidth", "0");
        changed |= SetStyleIfChanged(platformView, "minHeight", "0");
        return changed;
    }

    private static bool SetStyleIfChanged(MiragePlatformView node, string styleName, string? value)
    {
        if (string.Equals(node.GetStyleValue(styleName), value, StringComparison.Ordinal))
        {
            return false;
        }

        node.SetStyle(styleName, value);
        return true;
    }

    private void EnsurePresentedContentConnected()
    {
        if (_contentAttachmentEnabled)
        {
            return;
        }

        _contentAttachmentEnabled = true;
        SetPresentedContent(_pendingPresentedContent);
    }

    private static IView? ResolvePresentedContent(SwipeItemView itemView)
    {
        return ((IContentView)itemView).PresentedContent;
    }
}
