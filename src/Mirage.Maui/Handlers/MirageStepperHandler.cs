using System.Globalization;
using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageStepperHandler : MirageViewHandler<IStepper>, IMirageRuntimeEventTarget
{
    private const double NumericEpsilon = 1e-9;

    public static IPropertyMapper<IStepper, MirageStepperHandler> Mapper =
        new PropertyMapper<IStepper, MirageStepperHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(IRange.Minimum)] = MapRange,
            [nameof(IRange.Maximum)] = MapRange,
            [nameof(IRange.Value)] = MapRange,
            [nameof(IStepper.Interval)] = MapRange,
            [nameof(IView.IsEnabled)] = MapIsEnabled,
            ["IsEnabled"] = MapIsEnabled,
            [nameof(VisualElement.WidthRequest)] = MapSizeRequestsAndLayoutAlignment,
            [nameof(VisualElement.HeightRequest)] = MapSizeRequestsAndLayoutAlignment,
            [nameof(VisualElement.MinimumWidthRequest)] = MapSizeRequests,
            [nameof(VisualElement.MinimumHeightRequest)] = MapSizeRequests,
            [nameof(VisualElement.MaximumWidthRequest)] = MapSizeRequests,
            [nameof(VisualElement.MaximumHeightRequest)] = MapSizeRequests
        };

    public MirageStepperHandler()
        : base(ComponentKind.Stepper, Mapper)
    {
        DefaultDisplayStyle = "flex";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        PlatformView.SetStyle("display", "flex");
        PlatformView.SetEvent("decrement", enabled: true);
        PlatformView.SetEvent("increment", enabled: true);
        UpdateRangeProjection(VirtualView);
        UpdateEnabledProjection(VirtualView);
        base.ConnectHandler(platformView);
        RequestRender();
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        _ = payload;

        var direction = eventName switch
        {
            "decrement" => -1d,
            "increment" => 1d,
            _ => 0d
        };

        if (Math.Abs(direction) < NumericEpsilon || !VirtualView.IsEnabled)
        {
            return;
        }

        var minimum = Math.Min(VirtualView.Minimum, VirtualView.Maximum);
        var maximum = Math.Max(VirtualView.Minimum, VirtualView.Maximum);
        var interval = NormalizeInterval(VirtualView.Interval);
        var next = Math.Clamp(VirtualView.Value + (direction * interval), minimum, maximum);
        if (Math.Abs(VirtualView.Value - next) > NumericEpsilon)
        {
            VirtualView.Value = next;
        }
    }

    public static void MapRange(MirageStepperHandler handler, IStepper stepper)
    {
        if (!handler.UpdateRangeProjection(stepper))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapIsEnabled(MirageStepperHandler handler, IStepper stepper)
    {
        if (!handler.UpdateEnabledProjection(stepper))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapSizeRequests(MirageStepperHandler handler, IStepper stepper)
    {
        if (!MirageCommonViewProjection.MapSizeRequests(handler, stepper))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapSizeRequestsAndLayoutAlignment(MirageStepperHandler handler, IStepper stepper)
    {
        var changed = MirageCommonViewProjection.MapSizeRequests(handler, stepper);
        changed |= MirageCommonViewProjection.MapLayoutAlignment(handler, stepper);
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
    }

    private bool UpdateRangeProjection(IStepper stepper)
    {
        var minimum = Math.Min(stepper.Minimum, stepper.Maximum);
        var maximum = Math.Max(stepper.Minimum, stepper.Maximum);
        var interval = NormalizeInterval(stepper.Interval);
        var value = Math.Clamp(stepper.Value, minimum, maximum);

        var changed = SetPropertyIfChanged("Minimum", ToInputNumber(minimum));
        changed |= SetPropertyIfChanged("Maximum", ToInputNumber(maximum));
        changed |= SetPropertyIfChanged("Step", ToInputNumber(interval));
        changed |= SetPropertyIfChanged("Value", ToInputNumber(value));
        changed |= SetPropertyIfChanged("DecrementText", "-");
        changed |= SetPropertyIfChanged("IncrementText", "+");
        changed |= UpdateButtonAvailability(stepper, value, minimum, maximum);
        return changed;
    }

    private bool UpdateEnabledProjection(IStepper stepper)
    {
        var isEnabled = stepper.IsEnabled;
        var minimum = Math.Min(stepper.Minimum, stepper.Maximum);
        var maximum = Math.Max(stepper.Minimum, stepper.Maximum);
        var value = Math.Clamp(stepper.Value, minimum, maximum);

        var changed = SetStyleIfChanged("cursor", isEnabled ? "default" : "not-allowed");
        changed |= SetPropertyIfChanged("Disabled", isEnabled ? null : "disabled");
        changed |= UpdateButtonAvailability(stepper, value, minimum, maximum);
        return changed;
    }

    private bool UpdateButtonAvailability(IStepper stepper, double value, double minimum, double maximum)
    {
        var canDecrement = stepper.IsEnabled && (value - minimum) > NumericEpsilon;
        var canIncrement = stepper.IsEnabled && (maximum - value) > NumericEpsilon;

        var changed = SetPropertyIfChanged("DecrementDisabled", canDecrement ? null : "disabled");
        changed |= SetPropertyIfChanged("IncrementDisabled", canIncrement ? null : "disabled");
        return changed;
    }

    private bool SetPropertyIfChanged(string propertyName, string? value)
    {
        if (string.Equals(PlatformView.GetPropertyString(propertyName), value, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetProperty(propertyName, value);
        return true;
    }

    private bool SetStyleIfChanged(string styleName, string? value)
    {
        if (string.Equals(PlatformView.GetStyleValue(styleName), value, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetStyle(styleName, value);
        return true;
    }

    private static string ToInputNumber(double value)
    {
        return value.ToString("G17", CultureInfo.InvariantCulture);
    }

    private static double NormalizeInterval(double value)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            return 1;
        }

        return value;
    }
}
