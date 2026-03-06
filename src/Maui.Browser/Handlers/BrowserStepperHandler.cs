using System.Globalization;
using Maui.Browser.Dom;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Maui.Browser.Handlers;

public sealed class BrowserStepperHandler : BrowserViewHandler<IStepper>
{
    public static IPropertyMapper<IStepper, BrowserStepperHandler> Mapper = new PropertyMapper<IStepper, BrowserStepperHandler>(BrowserViewMapper.CommonViewMapper)
    {
        [nameof(IView.AutomationId)] = MapAutomationId,
        [nameof(IRange.Minimum)] = MapMinimum,
        [nameof(IRange.Maximum)] = MapMaximum,
        [nameof(IRange.Value)] = MapValue,
        [nameof(IStepper.Interval)] = MapInterval,
        [nameof(IView.IsEnabled)] = MapIsEnabled
    };

    private DomElement? _decrementButton;
    private DomElement? _incrementButton;
    private DomElement? _valueLabel;

    private Action? _decrementClickCallback;
    private Action? _incrementClickCallback;
    private int _decrementClickSubscriptionId;
    private int _incrementClickSubscriptionId;

    public BrowserStepperHandler()
        : base(Mapper)
    {
    }

    protected override DomElement CreatePlatformView() => DomElement.Create("div");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Style("display", "inline-flex");
        platformView.Style("alignItems", "center");
        platformView.Style("gap", "8px");
        platformView.Style("boxSizing", "border-box");

        _decrementButton = DomElement.Create("button");
        _decrementButton.Attr("type", "button");
        _decrementButton.Text("-");
        _decrementButton.Style("width", "30px");
        _decrementButton.Style("height", "30px");
        _decrementButton.Style("lineHeight", "1");

        _valueLabel = DomElement.Create("span");
        _valueLabel.Style("minWidth", "64px");
        _valueLabel.Style("textAlign", "center");
        _valueLabel.Style("fontVariantNumeric", "tabular-nums");

        _incrementButton = DomElement.Create("button");
        _incrementButton.Attr("type", "button");
        _incrementButton.Text("+");
        _incrementButton.Style("width", "30px");
        _incrementButton.Style("height", "30px");
        _incrementButton.Style("lineHeight", "1");

        platformView.Append(_decrementButton);
        platformView.Append(_valueLabel);
        platformView.Append(_incrementButton);

        _decrementClickCallback = OnDecrementClicked;
        _incrementClickCallback = OnIncrementClicked;
        _decrementClickSubscriptionId = _decrementButton.On("click", _decrementClickCallback);
        _incrementClickSubscriptionId = _incrementButton.On("click", _incrementClickCallback);

        UpdateVisualState(VirtualView);
        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(DomElement platformView)
    {
        if (_decrementClickSubscriptionId != 0)
        {
            DomElement.Off(_decrementClickSubscriptionId);
            _decrementClickSubscriptionId = 0;
        }

        if (_incrementClickSubscriptionId != 0)
        {
            DomElement.Off(_incrementClickSubscriptionId);
            _incrementClickSubscriptionId = 0;
        }

        _decrementClickCallback = null;
        _incrementClickCallback = null;

        _decrementButton = null;
        _incrementButton = null;
        _valueLabel = null;

        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        const double defaultWidth = 170;
        const double defaultHeight = 38;

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

    private void OnDecrementClicked()
    {
        if (!VirtualView.IsEnabled)
        {
            return;
        }

        var minimum = Math.Min(VirtualView.Minimum, VirtualView.Maximum);
        var maximum = Math.Max(VirtualView.Minimum, VirtualView.Maximum);
        var interval = NormalizeInterval(VirtualView.Interval);
        var nextValue = Math.Clamp(VirtualView.Value - interval, minimum, maximum);

        if (Math.Abs(nextValue - VirtualView.Value) > 1e-9)
        {
            VirtualView.Value = nextValue;
        }
    }

    private void OnIncrementClicked()
    {
        if (!VirtualView.IsEnabled)
        {
            return;
        }

        var minimum = Math.Min(VirtualView.Minimum, VirtualView.Maximum);
        var maximum = Math.Max(VirtualView.Minimum, VirtualView.Maximum);
        var interval = NormalizeInterval(VirtualView.Interval);
        var nextValue = Math.Clamp(VirtualView.Value + interval, minimum, maximum);

        if (Math.Abs(nextValue - VirtualView.Value) > 1e-9)
        {
            VirtualView.Value = nextValue;
        }
    }

    public static void MapAutomationId(BrowserStepperHandler handler, IStepper stepper)
    {
        BrowserViewMapper.MapAutomationId(handler, stepper);
        handler.UpdateChildAutomationIds(stepper);
    }

    public static void MapMinimum(BrowserStepperHandler handler, IStepper stepper)
    {
        handler.UpdateVisualState(stepper);
    }

    public static void MapMaximum(BrowserStepperHandler handler, IStepper stepper)
    {
        handler.UpdateVisualState(stepper);
    }

    public static void MapValue(BrowserStepperHandler handler, IStepper stepper)
    {
        handler.UpdateVisualState(stepper);
    }

    public static void MapInterval(BrowserStepperHandler handler, IStepper stepper)
    {
        handler.UpdateVisualState(stepper);
    }

    public static void MapIsEnabled(BrowserStepperHandler handler, IStepper stepper)
    {
        BrowserViewMapper.MapEnabled(stepper, handler.PlatformView);
        handler.UpdateVisualState(stepper);
    }

    private void UpdateChildAutomationIds(IStepper stepper)
    {
        var automationId = string.IsNullOrWhiteSpace(stepper.AutomationId) ? null : stepper.AutomationId;
        _decrementButton?.Attr("data-automation-id", automationId is null ? null : $"{automationId}-decrement-button");
        _incrementButton?.Attr("data-automation-id", automationId is null ? null : $"{automationId}-increment-button");
        _valueLabel?.Attr("data-automation-id", automationId is null ? null : $"{automationId}-value-label");
    }

    private void UpdateVisualState(IStepper stepper)
    {
        if (_decrementButton is null || _incrementButton is null || _valueLabel is null)
        {
            return;
        }

        var minimum = Math.Min(stepper.Minimum, stepper.Maximum);
        var maximum = Math.Max(stepper.Minimum, stepper.Maximum);
        var clampedValue = Math.Clamp(stepper.Value, minimum, maximum);

        _valueLabel.Text(clampedValue.ToString("0.###", CultureInfo.InvariantCulture));
        _valueLabel.Attr("aria-valuenow", clampedValue.ToString("0.###", CultureInfo.InvariantCulture));

        var disableDecrement = !stepper.IsEnabled || clampedValue <= minimum + 1e-9;
        var disableIncrement = !stepper.IsEnabled || clampedValue >= maximum - 1e-9;

        _decrementButton.Attr("disabled", disableDecrement ? "disabled" : null);
        _incrementButton.Attr("disabled", disableIncrement ? "disabled" : null);
    }

    private static double NormalizeInterval(double interval)
    {
        if (double.IsNaN(interval) || double.IsInfinity(interval) || interval <= 0)
        {
            return 1;
        }

        return interval;
    }
}
