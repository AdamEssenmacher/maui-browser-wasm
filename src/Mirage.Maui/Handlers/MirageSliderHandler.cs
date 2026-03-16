using System.Globalization;
using Mirage.Core.Semantics;
using Mirage.Maui.Images;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageSliderHandler : MirageViewHandler<ISlider>, IMirageRuntimeEventTarget
{
    public static IPropertyMapper<ISlider, MirageSliderHandler> Mapper =
        new PropertyMapper<ISlider, MirageSliderHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(IRange.Minimum)] = MapMinimum,
            [nameof(IRange.Maximum)] = MapMaximum,
            [nameof(IRange.Value)] = MapValue,
            [nameof(ISlider.MinimumTrackColor)] = MapMinimumTrackColor,
            [nameof(ISlider.MaximumTrackColor)] = MapMaximumTrackColor,
            [nameof(ISlider.ThumbColor)] = MapThumbColor,
            [nameof(IView.IsEnabled)] = MapIsEnabled,
            ["IsEnabled"] = MapIsEnabled,
            ["ThumbImageSource"] = MapThumbImageSource
        };

    private bool _isDragging;
    private IMirageImageSourceResolver? _imageSourceResolver;

    public MirageSliderHandler()
        : base(ComponentKind.Slider, Mapper)
    {
        DefaultDisplayStyle = "block";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        PlatformView.SetStyle("display", "block");
        PlatformView.SetStyle("width", "100%");
        PlatformView.SetStyle("minHeight", "24px");
        PlatformView.SetStyle("boxSizing", "border-box");
        PlatformView.SetEvent("input", enabled: true);
        PlatformView.SetEvent("change", enabled: true);
        PlatformView.SetProperty("Step", "any");
        UpdateEnabledProjection(VirtualView);
        UpdateThumbImageProjection(VirtualView);
        base.ConnectHandler(platformView);
        RequestRender();
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        if (_isDragging)
        {
            _isDragging = false;
            VirtualView.DragCompleted();
        }

        base.DisconnectHandler(platformView);
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        if (!string.Equals(eventName, "input", StringComparison.Ordinal)
            && !string.Equals(eventName, "change", StringComparison.Ordinal))
        {
            return;
        }

        if (!VirtualView.IsEnabled)
        {
            return;
        }

        if (!double.TryParse(payload, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
        {
            return;
        }

        if (!_isDragging && string.Equals(eventName, "input", StringComparison.Ordinal))
        {
            _isDragging = true;
            VirtualView.DragStarted();
        }

        var minimum = Math.Min(VirtualView.Minimum, VirtualView.Maximum);
        var maximum = Math.Max(VirtualView.Minimum, VirtualView.Maximum);
        var clamped = Math.Max(minimum, Math.Min(maximum, parsedValue));
        if (Math.Abs(VirtualView.Value - clamped) > 1e-9)
        {
            VirtualView.Value = clamped;
        }

        if (string.Equals(eventName, "change", StringComparison.Ordinal) && _isDragging)
        {
            _isDragging = false;
            VirtualView.DragCompleted();
        }
    }

    public static void MapMinimum(MirageSliderHandler handler, ISlider slider)
    {
        if (!handler.SetPropertyIfChanged("Minimum", ToInputNumber(slider.Minimum)))
        {
            return;
        }

        handler.UpdateTrackStyles(slider);
        handler.RequestRender();
    }

    public static void MapMaximum(MirageSliderHandler handler, ISlider slider)
    {
        if (!handler.SetPropertyIfChanged("Maximum", ToInputNumber(slider.Maximum)))
        {
            return;
        }

        handler.UpdateTrackStyles(slider);
        handler.RequestRender();
    }

    public static void MapValue(MirageSliderHandler handler, ISlider slider)
    {
        var changed = handler.SetPropertyIfChanged("Value", ToInputNumber(slider.Value));
        changed |= handler.UpdateTrackStyles(slider);
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapMinimumTrackColor(MirageSliderHandler handler, ISlider slider)
    {
        if (!handler.UpdateTrackStyles(slider))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapMaximumTrackColor(MirageSliderHandler handler, ISlider slider)
    {
        if (!handler.UpdateTrackStyles(slider))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapThumbColor(MirageSliderHandler handler, ISlider slider)
    {
        if (!handler.UpdateTrackStyles(slider))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapThumbImageSource(MirageSliderHandler handler, ISlider slider)
    {
        if (!handler.UpdateThumbImageProjection(slider))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapIsEnabled(MirageSliderHandler handler, ISlider slider)
    {
        if (!handler.UpdateEnabledProjection(slider))
        {
            return;
        }

        handler.RequestRender();
    }

    private bool UpdateTrackStyles(ISlider slider)
    {
        var minTrackColor = MirageCommonViewProjection.ToCssColor(slider.MinimumTrackColor) ?? "rgba(59,130,246,1)";
        var maxTrackColor = MirageCommonViewProjection.ToCssColor(slider.MaximumTrackColor) ?? "rgba(209,213,219,1)";
        var thumbColor = MirageCommonViewProjection.ToCssColor(slider.ThumbColor);
        var resolvedThumbColor = thumbColor ?? "rgba(255,255,255,1)";

        var minimum = Math.Min(slider.Minimum, slider.Maximum);
        var maximum = Math.Max(slider.Minimum, slider.Maximum);
        var range = maximum - minimum;
        var ratio = range <= 0 ? 0 : (slider.Value - minimum) / range;
        ratio = Math.Clamp(ratio, 0, 1);

        var stop = (ratio * 100).ToString("0.###", CultureInfo.InvariantCulture);
        var trackGradient = $"linear-gradient(to right, {minTrackColor} 0%, {minTrackColor} {stop}%, {maxTrackColor} {stop}%, {maxTrackColor} 100%)";

        var changed = SetPropertyIfChanged("TrackGradient", trackGradient);
        changed |= SetPropertyIfChanged("ThumbAccent", resolvedThumbColor);

        return changed;
    }

    private bool UpdateThumbImageProjection(ISlider slider)
    {
        var thumbImageSource = slider is Slider controlsSlider
            ? controlsSlider.ThumbImageSource
            : null;

        var projection = MirageImageSourceProjection.BuildProjection(thumbImageSource, GetImageSourceResolver(), slider as IView);
        var hasResolvedThumbImage = !string.IsNullOrWhiteSpace(projection.SourceValue);
        var thumbImageCss = hasResolvedThumbImage
            ? $"url(\"{EscapeCssUrl(projection.SourceValue!)}\")"
            : null;

        var changed = SetPropertyIfChanged("ThumbImage", thumbImageCss);
        changed |= SetPropertyIfChanged("ThumbImageState", hasResolvedThumbImage ? "resolved" : projection.InitialState);
        return changed;
    }

    private bool UpdateEnabledProjection(ISlider slider)
    {
        var isEnabled = slider.IsEnabled;
        var changed = SetStyleIfChanged("cursor", isEnabled ? "pointer" : "default");
        changed |= SetStyleIfChanged("pointerEvents", isEnabled ? "auto" : "none");
        changed |= SetStyleIfChanged("opacity", null);
        changed |= SetPropertyIfChanged("Disabled", isEnabled ? null : "disabled");
        return changed;
    }

    private IMirageImageSourceResolver? GetImageSourceResolver()
    {
        if (_imageSourceResolver is not null)
        {
            return _imageSourceResolver;
        }

        var services = MauiContext?.Services;
        if (services is null)
        {
            return null;
        }

        _imageSourceResolver = services.GetService(typeof(IMirageImageSourceResolver)) as IMirageImageSourceResolver;
        return _imageSourceResolver;
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

    private static string EscapeCssUrl(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}
