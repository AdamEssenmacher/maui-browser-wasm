using System.Globalization;
using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageTimePickerHandler : MirageViewHandler<ITimePicker>, IMirageRuntimeEventTarget
{
    internal static readonly string[] ParityPropertyKeys =
    [
        nameof(ITimePicker.Background),
        nameof(IView.FlowDirection),
        nameof(ITextStyle.CharacterSpacing),
        nameof(ITextStyle.Font),
        nameof(ITimePicker.Format),
        nameof(ITextStyle.TextColor),
        nameof(ITimePicker.Time),
        nameof(ITimePicker.IsOpen)
    ];

    internal static readonly string[] ParityCommandKeys = [];

    internal static readonly string[] ParityExtensionPropertyKeys =
    [
        nameof(IView.IsEnabled)
    ];

    public static IPropertyMapper<ITimePicker, MirageTimePickerHandler> Mapper =
        new PropertyMapper<ITimePicker, MirageTimePickerHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(ITimePicker.Background)] = MapBackground,
            ["BackgroundColor"] = MapBackground,
            [nameof(IView.FlowDirection)] = MapFlowDirection,
            [nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
            [nameof(ITextStyle.Font)] = MapFont,
            ["FontFamily"] = MapFont,
            ["FontSize"] = MapFont,
            ["FontAttributes"] = MapFont,
            ["FontAutoScalingEnabled"] = MapFont,
            [nameof(ITimePicker.Format)] = MapFormat,
            [nameof(ITextStyle.TextColor)] = MapTextColor,
            ["TextColor"] = MapTextColor,
            [nameof(ITimePicker.Time)] = MapTime,
            [nameof(ITimePicker.IsOpen)] = MapIsOpen,
            [nameof(IView.IsEnabled)] = MapIsEnabled,
            ["IsEnabled"] = MapIsEnabled
        };

    public static CommandMapper<ITimePicker, MirageTimePickerHandler> CommandMapper =
        new(MirageViewMapper.CommonViewCommandMapper);

    private bool _isApplyingRuntimeIsOpen;
    private bool _isMappingIsOpen;

    public MirageTimePickerHandler()
        : base(ComponentKind.TimePicker, Mapper, CommandMapper)
    {
        DefaultDisplayStyle = "inline-flex";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        platformView.SetStyle("display", "inline-flex");
        platformView.SetStyle("boxSizing", "border-box");
        platformView.SetStyle("minHeight", "36px");
        platformView.SetEvent("change", enabled: true);
        platformView.SetEvent("focus", enabled: true);
        platformView.SetEvent("blur", enabled: true);

        var changed = ApplyTimePickerProjectionCore(VirtualView);
        base.ConnectHandler(platformView);
        if (changed)
        {
            RequestRender();
        }
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        if (string.Equals(eventName, "change", StringComparison.Ordinal))
        {
            OnRuntimeTimeChanged(payload);
            return;
        }

        if (string.Equals(eventName, "blur", StringComparison.Ordinal))
        {
            OnRuntimeIsOpenChanged(isOpen: false);
        }
    }

    public static void MapBackground(MirageTimePickerHandler handler, ITimePicker timePicker) => handler.MapTimePicker(timePicker);

    public static void MapFlowDirection(MirageTimePickerHandler handler, ITimePicker timePicker) => handler.MapTimePicker(timePicker);

    public static void MapCharacterSpacing(MirageTimePickerHandler handler, ITimePicker timePicker) => handler.MapTimePicker(timePicker);

    public static void MapFont(MirageTimePickerHandler handler, ITimePicker timePicker) => handler.MapTimePicker(timePicker);

    public static void MapFormat(MirageTimePickerHandler handler, ITimePicker timePicker) => handler.MapTimePicker(timePicker);

    public static void MapTextColor(MirageTimePickerHandler handler, ITimePicker timePicker) => handler.MapTimePicker(timePicker);

    public static void MapTime(MirageTimePickerHandler handler, ITimePicker timePicker) => handler.MapTimePicker(timePicker);

    public static void MapIsEnabled(MirageTimePickerHandler handler, ITimePicker timePicker) => handler.MapTimePicker(timePicker);

    public static void MapIsOpen(MirageTimePickerHandler handler, ITimePicker timePicker)
    {
        if (handler._isApplyingRuntimeIsOpen
            || handler._isMappingIsOpen
            || handler.Coordinator is not { } coordinator
            || string.IsNullOrWhiteSpace(handler.PlatformView.NodeId))
        {
            return;
        }

        handler._isMappingIsOpen = true;
        try
        {
            if (timePicker.IsOpen)
            {
                if (!coordinator.TryOpenNodePicker(handler.PlatformView.NodeId))
                {
                    // Keep IsOpen authoritative to actual runtime state: failed open requests roll back.
                    handler.OnRuntimeIsOpenChanged(isOpen: false);
                }

                return;
            }

            _ = coordinator.TrySetNodeFocus(handler.PlatformView.NodeId, isFocused: false);
        }
        finally
        {
            handler._isMappingIsOpen = false;
        }
    }

    private void MapTimePicker(ITimePicker timePicker)
    {
        if (!ApplyTimePickerProjectionCore(timePicker))
        {
            return;
        }

        RequestRender();
    }

    private bool ApplyTimePickerProjectionCore(ITimePicker timePicker)
    {
        using var _ = BeginRenderBatch();

        var changed = UpdateBackgroundProjection(timePicker);
        changed |= UpdateFlowDirectionProjection(timePicker);
        changed |= UpdateCharacterSpacingProjection(timePicker);
        changed |= UpdateFontProjection(timePicker);
        changed |= UpdateFormatProjection(timePicker);
        changed |= UpdateTextColorProjection(timePicker);
        changed |= UpdateTimeProjection(timePicker);
        changed |= UpdateEnabledProjection(timePicker);
        return changed;
    }

    private bool UpdateBackgroundProjection(ITimePicker timePicker)
    {
        return MirageCommonViewProjection.MapBackground(this, timePicker);
    }

    private bool UpdateFlowDirectionProjection(ITimePicker timePicker)
    {
        var direction = timePicker.FlowDirection switch
        {
            FlowDirection.LeftToRight => "ltr",
            FlowDirection.RightToLeft => "rtl",
            _ => null
        };

        return SetStyleIfChanged("direction", direction);
    }

    private bool UpdateCharacterSpacingProjection(ITimePicker timePicker)
    {
        return MirageCommonViewProjection.MapCharacterSpacing(this, timePicker);
    }

    private bool UpdateFontProjection(ITimePicker timePicker)
    {
        return MirageCommonViewProjection.MapFont(this, timePicker);
    }

    private bool UpdateFormatProjection(ITimePicker timePicker)
    {
        var format = string.IsNullOrWhiteSpace(timePicker.Format)
            ? null
            : timePicker.Format;
        var includeSeconds = IncludesSeconds(format);

        var changed = SetPropertyIfChanged("Format", format);
        changed |= SetPropertyIfChanged("Step", includeSeconds ? "1" : "60");
        return changed;
    }

    private bool UpdateTextColorProjection(ITimePicker timePicker)
    {
        return MirageCommonViewProjection.MapTextColor(this, timePicker);
    }

    private bool UpdateTimeProjection(ITimePicker timePicker)
    {
        var formatted = FormatTime(timePicker.Time, timePicker.Format);
        return SetPropertyIfChanged("Value", formatted);
    }

    private bool UpdateEnabledProjection(ITimePicker timePicker)
    {
        var isEnabled = timePicker.IsEnabled;
        var changed = SetStyleIfChanged("cursor", isEnabled ? "pointer" : "default");
        changed |= SetStyleIfChanged("pointerEvents", isEnabled ? "auto" : "none");
        changed |= SetPropertyIfChanged("Disabled", isEnabled ? null : "disabled");
        return changed;
    }

    private void OnRuntimeTimeChanged(string? payload)
    {
        if (!VirtualView.IsEnabled)
        {
            return;
        }

        if (!TryParseTimePayload(payload, out var parsedTime))
        {
            return;
        }

        if (Nullable.Equals(VirtualView.Time, parsedTime))
        {
            return;
        }

        VirtualView.Time = parsedTime;
    }

    private void OnRuntimeIsOpenChanged(bool isOpen)
    {
        if (VirtualView.IsOpen == isOpen)
        {
            return;
        }

        _isApplyingRuntimeIsOpen = true;
        try
        {
            VirtualView.IsOpen = isOpen;
        }
        finally
        {
            _isApplyingRuntimeIsOpen = false;
        }
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

    private static bool TryParseTimePayload(string? rawValue, out TimeSpan? parsed)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            parsed = null;
            return true;
        }

        if (TimeSpan.TryParseExact(rawValue, ["hh\\:mm", "hh\\:mm\\:ss"], CultureInfo.InvariantCulture, out var exact))
        {
            parsed = exact;
            return true;
        }

        parsed = null;
        return false;
    }

    private static string? FormatTime(TimeSpan? value, string? format)
    {
        if (value is null)
        {
            return null;
        }

        return IncludesSeconds(format)
            ? value.Value.ToString("hh\\:mm\\:ss", CultureInfo.InvariantCulture)
            : value.Value.ToString("hh\\:mm", CultureInfo.InvariantCulture);
    }

    private static bool IncludesSeconds(string? format)
    {
        return !string.IsNullOrWhiteSpace(format)
            && format.IndexOf('s', StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
