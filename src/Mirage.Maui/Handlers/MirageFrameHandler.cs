using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageFrameHandler : MirageContentViewHandlerBase<Frame>
{
    private const string DefaultFrameShadow = "0 2px 6px rgba(0,0,0,0.24)";

    public static IPropertyMapper<Frame, MirageFrameHandler> Mapper =
        new PropertyMapper<Frame, MirageFrameHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(Frame.Content)] = MapContent,
            [nameof(Frame.Padding)] = MapPadding,
            [nameof(Frame.BorderColor)] = MapBorderColor,
            [nameof(Frame.CornerRadius)] = MapCornerRadius,
            [nameof(Frame.HasShadow)] = MapHasShadow
        };

    public MirageFrameHandler()
        : base(Mapper)
    {
    }

    protected override IPadding PaddingSource => VirtualView;

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        base.ConnectHandler(platformView);
        if (UpdateFrameStyles(VirtualView))
        {
            RequestRender();
        }
    }

    public static void MapContent(MirageFrameHandler handler, Frame frame)
    {
        handler.SetPresentedContent(((IContentView)frame).PresentedContent);
    }

    public static void MapPadding(MirageFrameHandler handler, Frame frame)
    {
        MirageViewMapper.MapPadding(handler, frame);
    }

    public static void MapBorderColor(MirageFrameHandler handler, Frame frame)
    {
        if (!handler.UpdateFrameStyles(frame))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapCornerRadius(MirageFrameHandler handler, Frame frame)
    {
        if (!handler.UpdateFrameStyles(frame))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapHasShadow(MirageFrameHandler handler, Frame frame)
    {
        if (!handler.UpdateFrameStyles(frame))
        {
            return;
        }

        handler.RequestRender();
    }

    private bool UpdateFrameStyles(Frame frame)
    {
        var changed = false;
        changed |= SetStyleIfChanged("boxSizing", "border-box");
        changed |= SetStyleIfChanged("overflow", "hidden");
        changed |= SetStyleIfChanged("overflowX", "hidden");
        changed |= SetStyleIfChanged("overflowY", "hidden");

        var cornerRadius = Math.Max(0, NormalizeFinite(frame.CornerRadius));
        changed |= SetStyleIfChanged("borderRadius", MirageViewMapper.ToPx(cornerRadius));

        if (frame.BorderColor is { Alpha: > 0 } borderColor)
        {
            changed |= SetStyleIfChanged("borderStyle", "solid");
            changed |= SetStyleIfChanged("borderWidth", "1px");
            changed |= SetStyleIfChanged("borderColor", MirageCommonViewProjection.ToCssColor(borderColor));
        }
        else
        {
            changed |= SetStyleIfChanged("borderStyle", "none");
            changed |= SetStyleIfChanged("borderWidth", null);
            changed |= SetStyleIfChanged("borderColor", null);
        }

        changed |= SetStyleIfChanged("boxShadow", frame.HasShadow ? DefaultFrameShadow : null);
        return changed;
    }

    private static float NormalizeFinite(float value)
    {
        return float.IsFinite(value) ? value : 0f;
    }

    private bool SetStyleIfChanged(string styleName, string? styleValue)
    {
        if (string.Equals(PlatformView.GetStyleValue(styleName), styleValue, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetStyle(styleName, styleValue);
        return true;
    }
}
