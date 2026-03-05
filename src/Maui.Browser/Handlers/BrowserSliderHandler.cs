using System.Globalization;
using Maui.Browser.Dom;
using Maui.Browser.Utils;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Maui.Browser.Handlers;

public sealed class BrowserSliderHandler : BrowserViewHandler<ISlider>
{
    public static IPropertyMapper<ISlider, BrowserSliderHandler> Mapper = new PropertyMapper<ISlider, BrowserSliderHandler>(ViewHandler.ViewMapper)
    {
        [nameof(IRange.Minimum)] = MapMinimum,
        [nameof(IRange.Maximum)] = MapMaximum,
        [nameof(IRange.Value)] = MapValue,
        [nameof(ISlider.MinimumTrackColor)] = MapMinimumTrackColor,
        [nameof(ISlider.MaximumTrackColor)] = MapMaximumTrackColor,
        [nameof(ISlider.ThumbColor)] = MapThumbColor,
        [nameof(IView.Visibility)] = MapVisibility,
        [nameof(IView.Opacity)] = MapOpacity,
        [nameof(IView.IsEnabled)] = MapIsEnabled
    };

    private Action? _inputCallback;
    private Action? _changeCallback;
    private int _inputSubscriptionId;
    private int _changeSubscriptionId;
    private bool _isDragging;

    public BrowserSliderHandler()
        : base(Mapper)
    {
    }

    protected override DomElement CreatePlatformView() => DomElement.Create("input");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Attr("type", "range");
        platformView.Style("cursor", "pointer");
        platformView.Style("boxSizing", "border-box");

        _inputCallback = OnInput;
        _changeCallback = OnChange;
        _inputSubscriptionId = platformView.On("input", _inputCallback);
        _changeSubscriptionId = platformView.On("change", _changeCallback);

        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(DomElement platformView)
    {
        if (_inputSubscriptionId != 0)
        {
            DomElement.Off(_inputSubscriptionId);
            _inputSubscriptionId = 0;
        }

        if (_changeSubscriptionId != 0)
        {
            DomElement.Off(_changeSubscriptionId);
            _changeSubscriptionId = 0;
        }

        if (_isDragging)
        {
            _isDragging = false;
            VirtualView.DragCompleted();
        }

        _inputCallback = null;
        _changeCallback = null;
        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        const double defaultWidth = 220;
        const double defaultHeight = 32;

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

    private void OnInput()
    {
        if (!_isDragging)
        {
            _isDragging = true;
            VirtualView.DragStarted();
        }

        UpdateValueFromPlatform();
    }

    private void OnChange()
    {
        UpdateValueFromPlatform();

        if (_isDragging)
        {
            _isDragging = false;
            VirtualView.DragCompleted();
        }
    }

    private void UpdateValueFromPlatform()
    {
        var rawValue = PlatformView.GetInputValue();
        if (!double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
        {
            return;
        }

        var minimum = Math.Min(VirtualView.Minimum, VirtualView.Maximum);
        var maximum = Math.Max(VirtualView.Minimum, VirtualView.Maximum);
        var clamped = Math.Max(minimum, Math.Min(maximum, parsedValue));

        if (Math.Abs(VirtualView.Value - clamped) > 0.0001)
        {
            VirtualView.Value = clamped;
        }

        UpdateTrackStyles(this, VirtualView);
    }

    public static void MapMinimum(BrowserSliderHandler handler, ISlider view)
    {
        handler.PlatformView.Attr("min", ToInputNumber(view.Minimum));
        UpdateTrackStyles(handler, view);
    }

    public static void MapMaximum(BrowserSliderHandler handler, ISlider view)
    {
        handler.PlatformView.Attr("max", ToInputNumber(view.Maximum));
        UpdateTrackStyles(handler, view);
    }

    public static void MapValue(BrowserSliderHandler handler, ISlider view)
    {
        handler.PlatformView.SetInputValue(ToInputNumber(view.Value));
        UpdateTrackStyles(handler, view);
    }

    public static void MapMinimumTrackColor(BrowserSliderHandler handler, ISlider view)
    {
        UpdateTrackStyles(handler, view);
    }

    public static void MapMaximumTrackColor(BrowserSliderHandler handler, ISlider view)
    {
        UpdateTrackStyles(handler, view);
    }

    public static void MapThumbColor(BrowserSliderHandler handler, ISlider view)
    {
        UpdateTrackStyles(handler, view);
    }

    public static void MapVisibility(BrowserSliderHandler handler, ISlider view)
    {
        BrowserViewMapper.MapVisibility(view, handler.PlatformView);
    }

    public static void MapOpacity(BrowserSliderHandler handler, ISlider view)
    {
        BrowserViewMapper.MapOpacity(view, handler.PlatformView);
    }

    public static void MapIsEnabled(BrowserSliderHandler handler, ISlider view)
    {
        BrowserViewMapper.MapEnabled(view, handler.PlatformView);
    }

    private static void UpdateTrackStyles(BrowserSliderHandler handler, ISlider view)
    {
        var minTrackColor = CssUtil.ToCssColor(view.MinimumTrackColor) ?? "rgba(59,130,246,1)";
        var maxTrackColor = CssUtil.ToCssColor(view.MaximumTrackColor) ?? "rgba(209,213,219,1)";
        var thumbColor = CssUtil.ToCssColor(view.ThumbColor);
        var accentColor = thumbColor ?? minTrackColor;

        var minimum = Math.Min(view.Minimum, view.Maximum);
        var maximum = Math.Max(view.Minimum, view.Maximum);
        var range = maximum - minimum;
        var ratio = range <= 0 ? 0 : (view.Value - minimum) / range;
        ratio = Math.Clamp(ratio, 0, 1);

        var stop = (ratio * 100).ToString("0.###", CultureInfo.InvariantCulture);

        handler.PlatformView.Style("accentColor", accentColor);
        handler.PlatformView.Style(
            "background",
            $"linear-gradient(to right, {minTrackColor} 0%, {minTrackColor} {stop}%, {maxTrackColor} {stop}%, {maxTrackColor} 100%)");
    }

    private static string ToInputNumber(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
