using System.Globalization;
using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageDatePickerHandler : MirageViewHandler<IDatePicker>, IMirageRuntimeEventTarget
{
    private static readonly TimeSpan OpenRetryDelay = TimeSpan.FromMilliseconds(16);
    private const int MaxOpenRetryAttempts = 2;

    internal static readonly string[] ParityPropertyKeys =
    [
        nameof(IDatePicker.Background),
        nameof(IView.FlowDirection),
        nameof(ITextStyle.CharacterSpacing),
        nameof(ITextStyle.Font),
        nameof(IDatePicker.Format),
        nameof(ITextStyle.TextColor),
        nameof(IDatePicker.Date),
        nameof(IDatePicker.MinimumDate),
        nameof(IDatePicker.MaximumDate),
        nameof(IDatePicker.IsOpen)
    ];

    internal static readonly string[] ParityCommandKeys = [];

    internal static readonly string[] ParityExtensionPropertyKeys =
    [
        nameof(IView.IsEnabled)
    ];

    public static IPropertyMapper<IDatePicker, MirageDatePickerHandler> Mapper =
        new PropertyMapper<IDatePicker, MirageDatePickerHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(IDatePicker.Background)] = MapBackground,
            ["BackgroundColor"] = MapBackground,
            [nameof(IView.FlowDirection)] = MapFlowDirection,
            [nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
            [nameof(ITextStyle.Font)] = MapFont,
            ["FontFamily"] = MapFont,
            ["FontSize"] = MapFont,
            ["FontAttributes"] = MapFont,
            ["FontAutoScalingEnabled"] = MapFont,
            [nameof(IDatePicker.Format)] = MapFormat,
            [nameof(ITextStyle.TextColor)] = MapTextColor,
            ["TextColor"] = MapTextColor,
            [nameof(IDatePicker.Date)] = MapDate,
            [nameof(IDatePicker.MinimumDate)] = MapMinimumDate,
            [nameof(IDatePicker.MaximumDate)] = MapMaximumDate,
            [nameof(IDatePicker.IsOpen)] = MapIsOpen,
            [nameof(IView.IsEnabled)] = MapIsEnabled,
            ["IsEnabled"] = MapIsEnabled
        };

    public static CommandMapper<IDatePicker, MirageDatePickerHandler> CommandMapper =
        new(MirageViewMapper.CommonViewCommandMapper);

    private bool _isApplyingRuntimeIsOpen;
    private bool _isMappingIsOpen;
    private bool _isOpenRetryScheduled;
    private int _remainingOpenRetryAttempts;
    private long _openAttemptVersion;

    public MirageDatePickerHandler()
        : base(ComponentKind.DatePicker, Mapper, CommandMapper)
    {
        DefaultDisplayStyle = "inline-flex";
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        const double defaultWidth = 180;
        const double defaultHeight = 36;

        return ResolveMeasuredSizeWithConstraints(VirtualView, widthConstraint, heightConstraint, defaultWidth, defaultHeight);
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        platformView.SetStyle("display", "inline-flex");
        platformView.SetStyle("boxSizing", "border-box");
        platformView.SetStyle("minHeight", "36px");
        platformView.SetEvent("change", enabled: true);
        platformView.SetEvent("focus", enabled: true);
        platformView.SetEvent("blur", enabled: true);

        var changed = ApplyDatePickerProjectionCore(VirtualView);
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
            OnRuntimeDateChanged(payload);
            return;
        }

        if (string.Equals(eventName, "blur", StringComparison.Ordinal))
        {
            OnRuntimeIsOpenChanged(isOpen: false);
        }
    }

    public static void MapBackground(MirageDatePickerHandler handler, IDatePicker datePicker) => handler.MapDatePicker(datePicker);

    public static void MapFlowDirection(MirageDatePickerHandler handler, IDatePicker datePicker) => handler.MapDatePicker(datePicker);

    public static void MapCharacterSpacing(MirageDatePickerHandler handler, IDatePicker datePicker) => handler.MapDatePicker(datePicker);

    public static void MapFont(MirageDatePickerHandler handler, IDatePicker datePicker) => handler.MapDatePicker(datePicker);

    public static void MapFormat(MirageDatePickerHandler handler, IDatePicker datePicker) => handler.MapDatePicker(datePicker);

    public static void MapTextColor(MirageDatePickerHandler handler, IDatePicker datePicker) => handler.MapDatePicker(datePicker);

    public static void MapDate(MirageDatePickerHandler handler, IDatePicker datePicker) => handler.MapDatePicker(datePicker);

    public static void MapMinimumDate(MirageDatePickerHandler handler, IDatePicker datePicker) => handler.MapDatePicker(datePicker);

    public static void MapMaximumDate(MirageDatePickerHandler handler, IDatePicker datePicker) => handler.MapDatePicker(datePicker);

    public static void MapIsEnabled(MirageDatePickerHandler handler, IDatePicker datePicker) => handler.MapDatePicker(datePicker);

    public static void MapIsOpen(MirageDatePickerHandler handler, IDatePicker datePicker)
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
            if (datePicker.IsOpen)
            {
                handler.ResetOpenRetryState();
                var attemptVersion = handler.BeginOpenAttemptVersion();

                if (!datePicker.IsEnabled)
                {
                    handler.OnRuntimeIsOpenChanged(isOpen: false);
                    return;
                }

                // Browsers do not expose a definitive picker-visible callback.
                // A successful open request is the strongest signal available,
                // and close reconciliation still comes from change/blur.
                if (coordinator.TryRequestNodePickerOpen(handler.PlatformView.NodeId))
                {
                    handler.ResetOpenRetryState();
                    return;
                }

                handler._remainingOpenRetryAttempts = MaxOpenRetryAttempts;
                handler.ScheduleOpenRetryOrRollback(attemptVersion);
                return;
            }

            handler.InvalidateOpenAttemptVersion();
            _ = coordinator.TrySetNodeFocus(handler.PlatformView.NodeId, isFocused: false);
            handler.ResetOpenRetryState();
        }
        finally
        {
            handler._isMappingIsOpen = false;
        }
    }

    private void MapDatePicker(IDatePicker datePicker)
    {
        if (!ApplyDatePickerProjectionCore(datePicker))
        {
            return;
        }

        RequestRender();
    }

    private bool ApplyDatePickerProjectionCore(IDatePicker datePicker)
    {
        using var _ = BeginRenderBatch();

        var changed = UpdateBackgroundProjection(datePicker);
        changed |= UpdateFlowDirectionProjection(datePicker);
        changed |= UpdateCharacterSpacingProjection(datePicker);
        changed |= UpdateFontProjection(datePicker);
        changed |= UpdateFormatProjection(datePicker);
        changed |= UpdateTextColorProjection(datePicker);
        changed |= UpdateDateProjection(datePicker);
        changed |= UpdateMinimumDateProjection(datePicker);
        changed |= UpdateMaximumDateProjection(datePicker);
        changed |= UpdateEnabledProjection(datePicker);
        return changed;
    }

    private bool UpdateBackgroundProjection(IDatePicker datePicker)
    {
        return MirageCommonViewProjection.MapBackground(this, datePicker);
    }

    private bool UpdateFlowDirectionProjection(IDatePicker datePicker)
    {
        var direction = datePicker.FlowDirection switch
        {
            FlowDirection.LeftToRight => "ltr",
            FlowDirection.RightToLeft => "rtl",
            _ => null
        };

        return SetStyleIfChanged("direction", direction);
    }

    private bool UpdateCharacterSpacingProjection(IDatePicker datePicker)
    {
        return MirageCommonViewProjection.MapCharacterSpacing(this, datePicker);
    }

    private bool UpdateFontProjection(IDatePicker datePicker)
    {
        return MirageCommonViewProjection.MapFont(this, datePicker);
    }

    private bool UpdateFormatProjection(IDatePicker datePicker)
    {
        var format = string.IsNullOrWhiteSpace(datePicker.Format)
            ? null
            : datePicker.Format;

        return SetPropertyIfChanged("Format", format);
    }

    private bool UpdateTextColorProjection(IDatePicker datePicker)
    {
        return MirageCommonViewProjection.MapTextColor(this, datePicker);
    }

    private bool UpdateDateProjection(IDatePicker datePicker)
    {
        return SetPropertyIfChanged("Value", FormatDate(datePicker.Date));
    }

    private bool UpdateMinimumDateProjection(IDatePicker datePicker)
    {
        return SetPropertyIfChanged("Min", FormatDate(datePicker.MinimumDate));
    }

    private bool UpdateMaximumDateProjection(IDatePicker datePicker)
    {
        return SetPropertyIfChanged("Max", FormatDate(datePicker.MaximumDate));
    }

    private bool UpdateEnabledProjection(IDatePicker datePicker)
    {
        var isEnabled = datePicker.IsEnabled;
        var changed = SetStyleIfChanged("cursor", isEnabled ? "pointer" : "default");
        changed |= SetPropertyIfChanged("Disabled", isEnabled ? null : "disabled");
        return changed;
    }

    private void OnRuntimeDateChanged(string? payload)
    {
        if (!VirtualView.IsEnabled)
        {
            return;
        }

        if (!TryParseDatePayload(payload, out var parsedDate))
        {
            return;
        }

        if (!Nullable.Equals(VirtualView.Date, parsedDate))
        {
            VirtualView.Date = parsedDate;
        }

        OnRuntimeIsOpenChanged(isOpen: false);
    }

    private void OnRuntimeIsOpenChanged(bool isOpen)
    {
        if (VirtualView.IsOpen == isOpen)
        {
            return;
        }

        if (!isOpen)
        {
            InvalidateOpenAttemptVersion();
            ResetOpenRetryState();
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

    private void ScheduleOpenRetryOrRollback(long attemptVersion)
    {
        if (_isOpenRetryScheduled || Coordinator is not { } coordinator)
        {
            return;
        }

        _isOpenRetryScheduled = true;
        if (coordinator.DispatchDelayed(OpenRetryDelay, () => RetryOpenAfterDelay(attemptVersion)))
        {
            return;
        }

        _isOpenRetryScheduled = false;
        _remainingOpenRetryAttempts = 0;
        if (IsCurrentAttemptVersion(attemptVersion))
        {
            OnRuntimeIsOpenChanged(isOpen: false);
        }
    }

    private void RetryOpenAfterDelay(long attemptVersion)
    {
        if (!IsCurrentAttemptVersion(attemptVersion))
        {
            return;
        }

        _isOpenRetryScheduled = false;

        if (!VirtualView.IsOpen)
        {
            ResetOpenRetryState();
            return;
        }

        if (!VirtualView.IsEnabled)
        {
            ResetOpenRetryState();
            OnRuntimeIsOpenChanged(isOpen: false);
            return;
        }

        if (Coordinator is not { } coordinator || string.IsNullOrWhiteSpace(PlatformView.NodeId))
        {
            ResetOpenRetryState();
            OnRuntimeIsOpenChanged(isOpen: false);
            return;
        }

        // Browsers do not expose a definitive picker-visible callback.
        // A successful open request is the strongest signal available,
        // and close reconciliation still comes from change/blur.
        if (coordinator.TryRequestNodePickerOpen(PlatformView.NodeId))
        {
            ResetOpenRetryState();
            return;
        }

        if (_remainingOpenRetryAttempts > 1)
        {
            _remainingOpenRetryAttempts--;
            ScheduleOpenRetryOrRollback(attemptVersion);
            return;
        }

        ResetOpenRetryState();
        OnRuntimeIsOpenChanged(isOpen: false);
    }

    private void ResetOpenRetryState()
    {
        _isOpenRetryScheduled = false;
        _remainingOpenRetryAttempts = 0;
    }

    private long BeginOpenAttemptVersion()
    {
        return Interlocked.Increment(ref _openAttemptVersion);
    }

    private void InvalidateOpenAttemptVersion()
    {
        _ = Interlocked.Increment(ref _openAttemptVersion);
    }

    private bool IsCurrentAttemptVersion(long attemptVersion)
    {
        return Interlocked.Read(ref _openAttemptVersion) == attemptVersion;
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

    private static bool TryParseDatePayload(string? rawValue, out DateTime? parsed)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            parsed = null;
            return true;
        }

        if (DateTime.TryParseExact(rawValue, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
        {
            parsed = exact.Date;
            return true;
        }

        parsed = null;
        return false;
    }

    private static string? FormatDate(DateTime? value)
    {
        return value?.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static Size ResolveMeasuredSizeWithConstraints(
        IDatePicker datePicker,
        double widthConstraint,
        double heightConstraint,
        double measuredWidth,
        double measuredHeight)
    {
        var clampedWidth = ResolveDesiredDimension(widthConstraint, measuredWidth);
        var clampedHeight = ResolveDesiredDimension(heightConstraint, measuredHeight);
        var (requestedWidth, requestedMinimumWidth, requestedMaximumWidth) = ResolveRequestedDimensionRequests(datePicker, isWidth: true);
        var (requestedHeight, requestedMinimumHeight, requestedMaximumHeight) = ResolveRequestedDimensionRequests(datePicker, isWidth: false);
        return new Size(
            ResolveConstraints(clampedWidth, requestedWidth, requestedMinimumWidth, requestedMaximumWidth),
            ResolveConstraints(clampedHeight, requestedHeight, requestedMinimumHeight, requestedMaximumHeight));
    }

    private static double ResolveDesiredDimension(double constraint, double fallback)
    {
        if (double.IsNaN(constraint) || double.IsInfinity(constraint))
        {
            return Math.Max(0, fallback);
        }

        return Math.Max(0, Math.Min(constraint, fallback));
    }

    private static double ResolveConstraints(double measured, double exact, double min, double max)
    {
        var resolved = measured;
        var resolvedMin = ResolveMinimum(min);

        if (IsExplicitLengthSet(exact))
        {
            resolved = exact;
        }

        if (IsExplicitLengthSet(max) && resolved > max)
        {
            resolved = max;
        }

        if (resolved < resolvedMin)
        {
            resolved = resolvedMin;
        }

        return resolved;
    }

    private static double ResolveMinimum(double value)
    {
        return IsExplicitLengthSet(value)
            ? value
            : 0;
    }

    private static bool IsExplicitLengthSet(double value)
    {
        return double.IsFinite(value) && value >= 0;
    }

    private static (double Exact, double Minimum, double Maximum) ResolveRequestedDimensionRequests(IDatePicker datePicker, bool isWidth)
    {
        if (datePicker is not Microsoft.Maui.Controls.VisualElement element)
        {
            return (-1, -1, -1);
        }

        return isWidth
            ? (element.WidthRequest, element.MinimumWidthRequest, element.MaximumWidthRequest)
            : (element.HeightRequest, element.MinimumHeightRequest, element.MaximumHeightRequest);
    }
}
