using Maui.Browser.Dom;
using Maui.Browser.Utils;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Maui.Browser.Handlers;

public sealed class BrowserRefreshViewHandler : BrowserViewHandler<RefreshView>, IRefreshViewHandler
{
    private const double DefaultWidth = 260;
    private const double DefaultContentHeight = 220;
    private const double IndicatorRowHeight = 44;
    private const string DefaultIndicatorColorCss = "rgba(51,65,85,1)";
    private const string DefaultTriggerColorCss = "rgba(15,23,42,1)";
    private const string SpinnerAnimationName = "maui-rv-spin";

    public static IPropertyMapper<RefreshView, BrowserRefreshViewHandler> Mapper =
        new PropertyMapper<RefreshView, BrowserRefreshViewHandler>(BrowserViewMapper.CommonViewMapper)
        {
            [nameof(IRefreshView.Content)] = MapContent,
            [nameof(IRefreshView.IsRefreshing)] = MapIsRefreshing,
            [nameof(IRefreshView.IsRefreshEnabled)] = MapIsRefreshEnabled,
            [nameof(IRefreshView.RefreshColor)] = MapRefreshColor,
            [nameof(RefreshView.Command)] = MapCommand,
            [nameof(RefreshView.CommandParameter)] = MapCommandParameter
        };

    private DomElement? _indicatorHost;
    private DomElement? _stateHost;
    private DomElement? _stateElement;
    private DomElement? _spinnerElement;
    private DomElement? _triggerButton;
    private DomElement? _contentHost;
    private DomElement? _spinnerStyleElement;
    private IView? _currentContent;
    private IElementHandler? _currentContentHandler;
    private Action? _triggerClickCallback;
    private int _triggerClickSubscriptionId;
    private Rect _lastArrangedFrame;

    public BrowserRefreshViewHandler()
        : base(Mapper)
    {
    }

    IRefreshView IRefreshViewHandler.VirtualView => VirtualView;

    object IRefreshViewHandler.PlatformView => PlatformView;

    protected override DomElement CreatePlatformView() => DomElement.Create("div");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Attr("data-maui-rv-root", "true");
        platformView.Style("boxSizing", "border-box");
        platformView.Style("overflow", "hidden");

        _indicatorHost = DomElement.Create("div");
        _indicatorHost.Attr("data-maui-rv-indicator-host", "true");
        _indicatorHost.Style("display", "flex");
        _indicatorHost.Style("alignItems", "center");
        _indicatorHost.Style("justifyContent", "space-between");
        _indicatorHost.Style("gap", "8px");
        _indicatorHost.Style("padding", "6px 10px");
        _indicatorHost.Style("boxSizing", "border-box");
        _indicatorHost.Style("borderBottom", "1px solid rgba(148,163,184,0.45)");

        _stateHost = DomElement.Create("div");
        _stateHost.Attr("data-maui-rv-state-host", "true");
        _stateHost.Style("display", "inline-flex");
        _stateHost.Style("alignItems", "center");
        _stateHost.Style("gap", "6px");
        _stateHost.Style("minHeight", "16px");

        _spinnerElement = DomElement.Create("span");
        _spinnerElement.Attr("data-maui-rv-spinner", "true");
        _spinnerElement.Attr("data-maui-rv-spinner-active", "false");
        _spinnerElement.Attr("aria-hidden", "true");
        _spinnerElement.Style("display", "inline-block");
        _spinnerElement.Style("width", "13px");
        _spinnerElement.Style("height", "13px");
        _spinnerElement.Style("borderWidth", "2px");
        _spinnerElement.Style("borderStyle", "solid");
        _spinnerElement.Style("borderRadius", "999px");
        _spinnerElement.Style("boxSizing", "border-box");
        _spinnerElement.Style("borderRightColor", "transparent");
        _spinnerElement.Style("color", DefaultIndicatorColorCss);
        _spinnerElement.Style("visibility", "hidden");
        _spinnerElement.Style("animation", $"{SpinnerAnimationName} 0.9s linear infinite");
        _spinnerElement.Style("animationPlayState", "paused");

        _stateElement = DomElement.Create("span");
        _stateElement.Attr("data-maui-rv-state", "idle");
        _stateElement.Style("fontSize", "12px");
        _stateElement.Style("color", DefaultIndicatorColorCss);

        _triggerButton = DomElement.Create("button");
        _triggerButton.Attr("type", "button");
        _triggerButton.Attr("data-maui-rv-trigger", "true");
        _triggerButton.Style("padding", "5px 10px");
        _triggerButton.Style("borderRadius", "8px");
        _triggerButton.Style("border", "1px solid rgba(100,116,139,0.65)");
        _triggerButton.Style("backgroundColor", "rgba(248,250,252,1)");
        _triggerButton.Style("fontSize", "12px");
        _triggerButton.Style("lineHeight", "1.2");
        _triggerButton.Style("cursor", "pointer");
        _triggerButton.Style("color", DefaultTriggerColorCss);
        _triggerButton.Text("Refresh");

        _contentHost = DomElement.Create("div");
        _contentHost.Attr("data-maui-rv-content-host", "true");
        _contentHost.Style("boxSizing", "border-box");
        _contentHost.Style("overflow", "hidden");

        _spinnerStyleElement = DomElement.Create("style");
        _spinnerStyleElement.Attr("data-maui-rv-style", "spinner");
        _spinnerStyleElement.Text($"@keyframes {SpinnerAnimationName}{{from{{transform:rotate(0deg);}}to{{transform:rotate(360deg);}}}}");

        _stateHost.Append(_spinnerElement);
        _stateHost.Append(_stateElement);
        _indicatorHost.Append(_stateHost);
        _indicatorHost.Append(_triggerButton);
        platformView.Append(_spinnerStyleElement);
        platformView.Append(_indicatorHost);
        platformView.Append(_contentHost);

        _triggerClickCallback = OnTriggerClicked;
        _triggerClickSubscriptionId = _triggerButton.On("click", _triggerClickCallback);

        SetContent(VirtualView.Content);
        ApplyRefreshColor(VirtualView);
        ApplyRefreshState(VirtualView);

        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(DomElement platformView)
    {
        if (_triggerClickSubscriptionId != 0)
        {
            DomElement.Off(_triggerClickSubscriptionId);
            _triggerClickSubscriptionId = 0;
        }

        _triggerClickCallback = null;
        DisconnectCurrentContent();
        _indicatorHost = null;
        _stateHost = null;
        _stateElement = null;
        _spinnerElement = null;
        _triggerButton = null;
        _contentHost = null;
        _spinnerStyleElement = null;
        platformView.ClearChildren();

        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        var content = VirtualView.Content;
        if (content is null)
        {
            return new Size(
                ResolveDesiredDimension(widthConstraint, DefaultWidth),
                ResolveDesiredDimension(heightConstraint, DefaultContentHeight + IndicatorRowHeight));
        }

        var indicatorHeight = ResolveIndicatorHeight(VirtualView);
        var contentHeightConstraint = double.IsFinite(heightConstraint)
            ? Math.Max(0, heightConstraint - indicatorHeight)
            : heightConstraint;
        var contentSize = content.Measure(widthConstraint, contentHeightConstraint);

        return new Size(
            ResolveDesiredDimension(widthConstraint, contentSize.Width),
            ResolveDesiredDimension(heightConstraint, contentSize.Height + indicatorHeight));
    }

    public override void PlatformArrange(Rect frame)
    {
        _lastArrangedFrame = frame;
        ArrangeForFrame(frame);
    }

    public static void MapContent(BrowserRefreshViewHandler handler, RefreshView refreshView)
    {
        handler.SetContent(refreshView.Content);
        handler.ArrangeUsingLastFrame();
    }

    public static void MapIsRefreshing(BrowserRefreshViewHandler handler, RefreshView refreshView)
    {
        handler.ApplyRefreshState(refreshView);
    }

    public static void MapIsRefreshEnabled(BrowserRefreshViewHandler handler, RefreshView refreshView)
    {
        handler.ApplyRefreshState(refreshView);
    }

    public static void MapRefreshColor(BrowserRefreshViewHandler handler, RefreshView refreshView)
    {
        handler.ApplyRefreshColor(refreshView);
    }

    public static void MapCommand(BrowserRefreshViewHandler handler, RefreshView refreshView)
    {
        handler.ApplyRefreshState(refreshView);
    }

    public static void MapCommandParameter(BrowserRefreshViewHandler handler, RefreshView refreshView)
    {
        handler.ApplyRefreshState(refreshView);
    }

    private void OnTriggerClicked()
    {
        if (!VirtualView.IsRefreshEnabled || VirtualView.IsRefreshing)
        {
            return;
        }

        // Mirror upstream handler behavior: user interaction sets IsRefreshing=true.
        // RefreshView itself raises Refreshing and executes Command.
        VirtualView.IsRefreshing = true;
    }

    private void SetContent(IView? content)
    {
        if (_currentContentHandler is not null && !ReferenceEquals(_currentContent, content))
        {
            DisconnectCurrentContent();
        }

        _contentHost?.ClearChildren();

        if (content is null || MauiContext is null || _contentHost is null)
        {
            return;
        }

        var contentHandler = content.ToHandler(MauiContext);
        if (contentHandler.PlatformView is not DomElement contentElement)
        {
            contentHandler.DisconnectHandler();
            return;
        }

        _contentHost.Append(contentElement);
        _currentContent = content;
        _currentContentHandler = contentHandler;
    }

    private void DisconnectCurrentContent()
    {
        if (_currentContentHandler is null)
        {
            _currentContent = null;
            return;
        }

        _currentContentHandler.DisconnectHandler();
        _currentContentHandler = null;
        _currentContent = null;
    }

    private void ArrangeUsingLastFrame()
    {
        if (double.IsNaN(_lastArrangedFrame.Width) || double.IsNaN(_lastArrangedFrame.Height))
        {
            return;
        }

        if (Math.Abs(_lastArrangedFrame.Width) < 0.001 && Math.Abs(_lastArrangedFrame.Height) < 0.001)
        {
            return;
        }

        ArrangeForFrame(_lastArrangedFrame);
    }

    private void ArrangeForFrame(Rect frame)
    {
        base.PlatformArrange(frame);

        var width = Math.Max(0, frame.Width);
        var height = Math.Max(0, frame.Height);
        var indicatorHeight = ResolveIndicatorHeight(VirtualView);
        var contentHeight = Math.Max(0, height - indicatorHeight);

        if (_indicatorHost is not null)
        {
            BrowserViewMapper.ApplyFrame(_indicatorHost, new Rect(0, 0, width, indicatorHeight));
        }

        if (_contentHost is not null)
        {
            BrowserViewMapper.ApplyFrame(_contentHost, new Rect(0, indicatorHeight, width, contentHeight));
        }

        if (VirtualView.Content is IView content)
        {
            content.Arrange(new Rect(0, 0, width, contentHeight));
        }
    }

    private void ApplyRefreshState(IRefreshView refreshView)
    {
        var refreshing = refreshView.IsRefreshing;
        var refreshEnabled = refreshView.IsRefreshEnabled;
        var triggerInteractive = refreshEnabled && !refreshing;

        PlatformView.Attr("data-maui-rv-refreshing", refreshing ? "true" : "false");
        PlatformView.Attr("data-maui-rv-enabled", refreshEnabled ? "true" : "false");
        PlatformView.Attr("aria-busy", refreshing ? "true" : "false");

        if (_indicatorHost is not null)
        {
            _indicatorHost.Style("display", "flex");
        }

        if (_stateElement is not null)
        {
            var state = refreshing ? "refreshing" : refreshEnabled ? "idle" : "disabled";
            _stateElement.Attr("data-maui-rv-state", state);
            _stateElement.Text(state switch
            {
                "refreshing" => "Refreshing",
                "disabled" => "Refresh disabled",
                _ => "Refresh idle"
            });
        }

        if (_spinnerElement is not null)
        {
            _spinnerElement.Attr("data-maui-rv-spinner-active", refreshing ? "true" : "false");
            _spinnerElement.Style("visibility", refreshing ? "visible" : "hidden");
            _spinnerElement.Style("animationPlayState", refreshing ? "running" : "paused");
        }

        if (_triggerButton is null)
        {
            return;
        }

        _triggerButton.Attr("disabled", triggerInteractive ? null : "disabled");
        _triggerButton.Attr("aria-disabled", triggerInteractive ? "false" : "true");
        _triggerButton.Style("cursor", triggerInteractive ? "pointer" : "default");
        _triggerButton.Style("opacity", triggerInteractive ? "1" : "0.65");
        _triggerButton.Text(refreshing ? "Refreshing..." : "Refresh");
    }

    private void ApplyRefreshColor(IRefreshView refreshView)
    {
        var refreshColorCss = refreshView.RefreshColor is SolidPaint solidPaint && solidPaint.Color is not null
            ? CssUtil.ToCssColor(solidPaint.Color)
            : null;

        _stateElement?.Style("color", refreshColorCss ?? DefaultIndicatorColorCss);
        _spinnerElement?.Style("color", refreshColorCss ?? DefaultIndicatorColorCss);
        _triggerButton?.Style("borderColor", refreshColorCss);
        _triggerButton?.Style("color", refreshColorCss ?? DefaultTriggerColorCss);
    }

    private static double ResolveIndicatorHeight(IRefreshView refreshView)
    {
        return IndicatorRowHeight;
    }

    private static double ResolveDesiredDimension(double constraint, double contentDimension)
    {
        if (double.IsNaN(constraint) || double.IsInfinity(constraint))
        {
            return Math.Max(0, contentDimension);
        }

        return Math.Max(0, Math.Min(constraint, contentDimension));
    }
}
