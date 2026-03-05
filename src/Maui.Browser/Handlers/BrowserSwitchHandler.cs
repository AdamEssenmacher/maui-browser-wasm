using Maui.Browser.Dom;
using Maui.Browser.Utils;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Maui.Browser.Handlers;

public sealed class BrowserSwitchHandler : BrowserViewHandler<ISwitch>
{
    private const double IntrinsicWidth = 52;
    private const double IntrinsicHeight = 32;
    private const double ThumbSize = 26;
    private const double ThumbInset = 3;

    public static IPropertyMapper<ISwitch, BrowserSwitchHandler> Mapper = new PropertyMapper<ISwitch, BrowserSwitchHandler>(ViewHandler.ViewMapper)
    {
        [nameof(ISwitch.IsOn)] = MapIsOn,
        [nameof(ISwitch.TrackColor)] = MapTrackColor,
        [nameof(ISwitch.ThumbColor)] = MapThumbColor,
        [nameof(IView.Visibility)] = MapVisibility,
        [nameof(IView.Opacity)] = MapOpacity,
        [nameof(IView.IsEnabled)] = MapIsEnabled
    };

    private Action? _clickCallback;
    private int _clickSubscriptionId;
    private DomElement? _trackElement;
    private DomElement? _thumbElement;
    private double _arrangedWidth = IntrinsicWidth;
    private double _arrangedHeight = IntrinsicHeight;

    public BrowserSwitchHandler()
        : base(Mapper)
    {
    }

    protected override DomElement CreatePlatformView() => DomElement.Create("button");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Attr("type", "button");
        platformView.Attr("role", "switch");
        platformView.Style("appearance", "none");
        platformView.Style("webkitAppearance", "none");
        platformView.Style("padding", "0");
        platformView.Style("margin", "0");
        platformView.Style("border", "none");
        platformView.Style("background", "transparent");
        platformView.Style("outline", "auto");
        platformView.Style("outlineOffset", "2px");
        platformView.Style("boxSizing", "border-box");
        platformView.Style("overflow", "visible");

        _trackElement = DomElement.Create("span");
        _thumbElement = DomElement.Create("span");

        _trackElement.Style("position", "absolute");
        _trackElement.Style("left", "0");
        _trackElement.Style("top", "0");
        _trackElement.Style("width", "100%");
        _trackElement.Style("height", "100%");
        _trackElement.Style("borderRadius", "999px");
        _trackElement.Style("transition", "background-color 120ms ease, border-color 120ms ease");
        _trackElement.Style("boxSizing", "border-box");

        _thumbElement.Style("position", "absolute");
        _thumbElement.Style("top", CssUtil.Px(ThumbInset));
        _thumbElement.Style("width", CssUtil.Px(ThumbSize));
        _thumbElement.Style("height", CssUtil.Px(ThumbSize));
        _thumbElement.Style("borderRadius", "50%");
        _thumbElement.Style("transition", "left 120ms ease, background-color 120ms ease");
        _thumbElement.Style("boxShadow", "0 1px 2px rgba(15,23,42,0.35)");
        _thumbElement.Style("boxSizing", "border-box");

        platformView.Append(_trackElement);
        platformView.Append(_thumbElement);

        _clickCallback = OnClick;
        _clickSubscriptionId = platformView.On("click", _clickCallback);

        UpdateSwitchVisual(this, VirtualView);
        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(DomElement platformView)
    {
        if (_clickSubscriptionId != 0)
        {
            DomElement.Off(_clickSubscriptionId);
            _clickSubscriptionId = 0;
        }

        _clickCallback = null;
        _trackElement = null;
        _thumbElement = null;
        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        return new Size(
            ResolveDesiredDimension(widthConstraint, IntrinsicWidth),
            ResolveDesiredDimension(heightConstraint, IntrinsicHeight));
    }

    public override void PlatformArrange(Rect frame)
    {
        var arrangedWidth = Math.Max(0, frame.Width);
        var arrangedHeight = Math.Max(0, frame.Height);

        var width = Math.Min(IntrinsicWidth, arrangedWidth);
        var height = Math.Min(IntrinsicHeight, arrangedHeight);

        var x = frame.X + ResolveAlignedOffset(arrangedWidth, width, VirtualView.HorizontalLayoutAlignment);
        var y = frame.Y + ResolveAlignedOffset(arrangedHeight, height, VirtualView.VerticalLayoutAlignment);

        _arrangedWidth = width;
        _arrangedHeight = height;
        BrowserViewMapper.ApplyFrame(PlatformView, new Rect(x, y, width, height));
        UpdateSwitchVisual(this, VirtualView);
    }

    private static double ResolveDesiredDimension(double constraint, double fallback)
    {
        if (double.IsNaN(constraint) || double.IsInfinity(constraint))
        {
            return fallback;
        }

        return Math.Max(0, Math.Min(fallback, constraint));
    }

    private static double ResolveAlignedOffset(
        double arrangedSize,
        double contentSize,
        Microsoft.Maui.Primitives.LayoutAlignment alignment)
    {
        if (arrangedSize <= contentSize)
        {
            return 0;
        }

        return alignment switch
        {
            Microsoft.Maui.Primitives.LayoutAlignment.Center => (arrangedSize - contentSize) / 2,
            Microsoft.Maui.Primitives.LayoutAlignment.End => arrangedSize - contentSize,
            _ => 0
        };
    }

    private void OnClick()
    {
        if (!VirtualView.IsEnabled)
        {
            return;
        }

        VirtualView.IsOn = !VirtualView.IsOn;
        UpdateSwitchVisual(this, VirtualView);
    }

    public static void MapIsOn(BrowserSwitchHandler handler, ISwitch view)
    {
        UpdateSwitchVisual(handler, view);
    }

    public static void MapTrackColor(BrowserSwitchHandler handler, ISwitch view)
    {
        UpdateSwitchVisual(handler, view);
    }

    public static void MapThumbColor(BrowserSwitchHandler handler, ISwitch view)
    {
        UpdateSwitchVisual(handler, view);
    }

    public static void MapVisibility(BrowserSwitchHandler handler, ISwitch view)
    {
        BrowserViewMapper.MapVisibility(view, handler.PlatformView);
    }

    public static void MapOpacity(BrowserSwitchHandler handler, ISwitch view)
    {
        BrowserViewMapper.MapOpacity(view, handler.PlatformView);
    }

    public static void MapIsEnabled(BrowserSwitchHandler handler, ISwitch view)
    {
        BrowserViewMapper.MapEnabled(view, handler.PlatformView);
        UpdateSwitchVisual(handler, view);
    }

    private static void UpdateSwitchVisual(BrowserSwitchHandler handler, ISwitch view)
    {
        var trackColorOn = CssUtil.ToCssColor(view.TrackColor) ?? "rgba(16,185,129,1)";
        var trackColorOff = "rgba(148,163,184,1)";
        var thumbColor = CssUtil.ToCssColor(view.ThumbColor) ?? "rgba(255,255,255,1)";

        var trackColor = view.IsOn ? trackColorOn : trackColorOff;

        var switchWidth = handler._arrangedWidth > 0 ? handler._arrangedWidth : IntrinsicWidth;
        var switchHeight = handler._arrangedHeight > 0 ? handler._arrangedHeight : IntrinsicHeight;
        var thumbInset = Math.Min(ThumbInset, Math.Max(1, switchHeight * 0.15));
        var thumbSize = Math.Max(0, Math.Min(ThumbSize, Math.Min(switchHeight - (thumbInset * 2), switchWidth - (thumbInset * 2))));
        var thumbTop = Math.Max(0, (switchHeight - thumbSize) / 2);
        var thumbLeftOff = Math.Max(0, thumbInset);
        var thumbLeftOn = Math.Max(thumbLeftOff, switchWidth - thumbSize - thumbInset);
        var thumbLeft = view.IsOn ? thumbLeftOn : thumbLeftOff;

        handler.PlatformView.Attr("aria-checked", view.IsOn ? "true" : "false");
        handler.PlatformView.Style("cursor", view.IsEnabled ? "pointer" : "default");

        if (!view.IsEnabled)
        {
            handler.PlatformView.Style("filter", "saturate(0.45)");
        }
        else
        {
            handler.PlatformView.Style("filter", string.Empty);
        }

        if (handler._trackElement is not null)
        {
            handler._trackElement.Style("backgroundColor", trackColor);
            handler._trackElement.Style("border", "1px solid rgba(15,23,42,0.22)");
        }

        if (handler._thumbElement is not null)
        {
            handler._thumbElement.Style("top", CssUtil.Px(thumbTop));
            handler._thumbElement.Style("width", CssUtil.Px(thumbSize));
            handler._thumbElement.Style("height", CssUtil.Px(thumbSize));
            handler._thumbElement.Style("left", CssUtil.Px(thumbLeft));
            handler._thumbElement.Style("backgroundColor", thumbColor);
        }
    }
}
