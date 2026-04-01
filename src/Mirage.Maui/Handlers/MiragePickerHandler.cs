using System.Globalization;
using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MiragePickerHandler : MirageViewHandler<IPicker>, IMirageRuntimeEventTarget
{
    private static readonly TimeSpan OpenRetryDelay = TimeSpan.FromMilliseconds(16);
    private const int MaxOpenRetryAttempts = 2;
    private const double DefaultWidth = 180;
    private const double DefaultHeight = 36;
    private const string CenterVerticalInset = "3px";
    private const string EdgeVerticalInset = "6px";
    private static int s_optionNodeIdCounter;

    internal static readonly string[] ParityPropertyKeys =
    [
        nameof(IPicker.Background),
        nameof(IPicker.CharacterSpacing),
        nameof(IPicker.Font),
        nameof(IPicker.SelectedIndex),
        nameof(IPicker.TextColor),
        nameof(IPicker.Title),
        nameof(IPicker.TitleColor),
        nameof(ITextAlignment.HorizontalTextAlignment),
        nameof(ITextAlignment.VerticalTextAlignment),
        nameof(IPicker.Items),
        nameof(IPicker.IsOpen)
    ];

    internal static readonly string[] ParityCommandKeys =
    [
        nameof(IPicker.Focus),
        nameof(IPicker.Unfocus)
    ];

    internal static readonly string[] ParityControlsRemapPropertyKeys =
    [
        nameof(Picker.ItemsSource)
    ];

    internal static readonly string[] ParityExtensionPropertyKeys =
    [
        nameof(IView.IsEnabled)
    ];

    public static IPropertyMapper<IPicker, MiragePickerHandler> Mapper =
        new PropertyMapper<IPicker, MiragePickerHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(IPicker.Background)] = MapBackground,
            ["BackgroundColor"] = MapBackground,
            [nameof(IPicker.CharacterSpacing)] = MapCharacterSpacing,
            [nameof(IPicker.Font)] = MapFont,
            ["FontFamily"] = MapFont,
            ["FontSize"] = MapFont,
            ["FontAttributes"] = MapFont,
            ["FontAutoScalingEnabled"] = MapFont,
            [nameof(IPicker.SelectedIndex)] = MapSelectedIndex,
            [nameof(IPicker.TextColor)] = MapTextColor,
            ["TextColor"] = MapTextColor,
            [nameof(IPicker.Title)] = MapTitle,
            [nameof(IPicker.TitleColor)] = MapTitleColor,
            [nameof(IView.Semantics)] = MapSemantics,
            [nameof(ITextAlignment.HorizontalTextAlignment)] = MapHorizontalTextAlignment,
            [nameof(ITextAlignment.VerticalTextAlignment)] = MapVerticalTextAlignment,
            [nameof(IPicker.Items)] = MapItems,
            [nameof(IPicker.IsOpen)] = MapIsOpen,
            [nameof(IView.IsEnabled)] = MapIsEnabled,
            ["IsEnabled"] = MapIsEnabled
        };

    public static CommandMapper<IPicker, MiragePickerHandler> CommandMapper =
        new(MirageViewMapper.CommonViewCommandMapper)
        {
            [nameof(IPicker.Focus)] = MapFocus,
            [nameof(IPicker.Unfocus)] = MapUnfocus
        };

    private readonly List<MiragePlatformView> _optionNodes = [];
    private bool _isApplyingRuntimeIsOpen;
    private bool _isMappingIsOpen;
    private bool _isOpenRetryScheduled;
    private int _remainingOpenRetryAttempts;
    private long _openAttemptVersion;

    public MiragePickerHandler()
        : base(ComponentKind.Picker, Mapper, CommandMapper)
    {
        DefaultDisplayStyle = "inline-block";
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        return ResolveMeasuredSizeWithConstraints(VirtualView, widthConstraint, heightConstraint, DefaultWidth, DefaultHeight);
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        platformView.SetStyle("display", DefaultDisplayStyle);
        platformView.SetStyle("boxSizing", "border-box");
        platformView.SetStyle("minHeight", "36px");
        platformView.SetEvent("change", enabled: true);
        platformView.SetEvent("focus", enabled: true);
        platformView.SetEvent("blur", enabled: true);

        var changed = RebuildOptions(VirtualView);
        changed |= ApplyPickerProjectionCore(VirtualView);

        base.ConnectHandler(platformView);

        if (changed)
        {
            RequestRender();
        }
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        InvalidateOpenAttemptVersion();
        ResetOpenRetryState();
        ClearOptions();
        base.DisconnectHandler(platformView);
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        if (string.Equals(eventName, "change", StringComparison.Ordinal))
        {
            OnRuntimeSelectionChanged(payload);
            return;
        }

        if (string.Equals(eventName, "blur", StringComparison.Ordinal))
        {
            OnRuntimeIsOpenChanged(isOpen: false);
        }
    }

    public static void MapBackground(MiragePickerHandler handler, IPicker picker) => handler.MapPicker(picker);

    public static void MapCharacterSpacing(MiragePickerHandler handler, IPicker picker) => handler.MapPicker(picker);

    public static void MapFont(MiragePickerHandler handler, IPicker picker) => handler.MapPicker(picker);

    public static void MapSelectedIndex(MiragePickerHandler handler, IPicker picker)
    {
        var changed = handler.UpdateSelectedValueProjection(picker);
        changed |= handler.UpdateSelectedOptionProjection(picker);
        changed |= handler.UpdateDisplayedTextColor(picker);
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapTextColor(MiragePickerHandler handler, IPicker picker)
    {
        if (!handler.UpdateDisplayedTextColor(picker))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapTitle(MiragePickerHandler handler, IPicker picker)
    {
        var changed = handler.UpdateTitleProjection(picker);
        changed |= handler.RebuildOptions(picker);
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapTitleColor(MiragePickerHandler handler, IPicker picker)
    {
        if (!handler.UpdateDisplayedTextColor(picker))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapSemantics(MiragePickerHandler handler, IPicker picker)
    {
        var changed = MirageCommonViewProjection.MapSemantics(handler, picker);
        changed |= handler.UpdateTitleProjection(picker);
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapHorizontalTextAlignment(MiragePickerHandler handler, IPicker picker)
    {
        if (!handler.UpdateHorizontalTextAlignmentProjection(picker))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapVerticalTextAlignment(MiragePickerHandler handler, IPicker picker)
    {
        if (!handler.UpdateVerticalTextAlignmentProjection(picker))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapItems(MiragePickerHandler handler, IPicker picker)
    {
        if (!handler.RebuildOptions(picker))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapIsOpen(MiragePickerHandler handler, IPicker picker)
    {
        if (handler._isApplyingRuntimeIsOpen
            || handler._isMappingIsOpen
            || handler.Coordinator is null
            || string.IsNullOrWhiteSpace(handler.PlatformView.NodeId))
        {
            return;
        }

        handler._isMappingIsOpen = true;
        try
        {
            if (picker.IsOpen)
            {
                handler.BeginProgrammaticOpen(picker);
                return;
            }

            handler.BeginProgrammaticClose();
        }
        finally
        {
            handler._isMappingIsOpen = false;
        }
    }

    public static void MapIsEnabled(MiragePickerHandler handler, IPicker picker)
    {
        if (!handler.UpdateEnabledProjection(picker))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapFocus(MiragePickerHandler handler, IPicker picker, object? args)
    {
        var focusRequest = args as FocusRequest;

        if (!picker.IsEnabled
            || handler.Coordinator is not { } coordinator
            || string.IsNullOrWhiteSpace(handler.PlatformView.NodeId))
        {
            focusRequest?.TrySetResult(false);
            return;
        }

        var focused = coordinator.TrySetNodeFocus(handler.PlatformView.NodeId, isFocused: true);
        if (!focused)
        {
            focusRequest?.TrySetResult(false);
            return;
        }

        if (picker.IsOpen)
        {
            handler.ReissueProgrammaticOpen(picker);
        }
        else
        {
            picker.IsOpen = true;
        }

        focusRequest?.TrySetResult(focused);
    }

    public static void MapUnfocus(MiragePickerHandler handler, IPicker picker, object? args)
    {
        _ = picker;
        _ = args;

        if (handler.Coordinator is null
            || string.IsNullOrWhiteSpace(handler.PlatformView.NodeId))
        {
            return;
        }

        handler.BeginProgrammaticClose();
        handler.OnRuntimeIsOpenChanged(isOpen: false);
    }

    private void MapPicker(IPicker picker)
    {
        if (!ApplyPickerProjectionCore(picker))
        {
            return;
        }

        RequestRender();
    }

    private bool ApplyPickerProjectionCore(IPicker picker)
    {
        using var _ = BeginRenderBatch();

        var changed = UpdateBackgroundProjection(picker);
        changed |= UpdateCharacterSpacingProjection(picker);
        changed |= UpdateFontProjection(picker);
        changed |= UpdateTitleProjection(picker);
        changed |= UpdateSelectedValueProjection(picker);
        changed |= UpdateSelectedOptionProjection(picker);
        changed |= UpdateDisplayedTextColor(picker);
        changed |= UpdateHorizontalTextAlignmentProjection(picker);
        changed |= UpdateVerticalTextAlignmentProjection(picker);
        changed |= UpdateEnabledProjection(picker);
        return changed;
    }

    private void ReissueProgrammaticOpen(IPicker picker)
    {
        if (_isApplyingRuntimeIsOpen
            || _isMappingIsOpen
            || Coordinator is not { }
            || string.IsNullOrWhiteSpace(PlatformView.NodeId))
        {
            return;
        }

        _isMappingIsOpen = true;
        try
        {
            BeginProgrammaticOpen(picker);
        }
        finally
        {
            _isMappingIsOpen = false;
        }
    }

    private bool RebuildOptions(IPicker picker)
    {
        var changed = ClearOptions();

        if (HasPlaceholder(picker))
        {
            var placeholderNode = CreateOptionNode(string.Empty, picker.Title ?? string.Empty, disabled: true);
            PlatformView.AddChild(placeholderNode);
            _optionNodes.Add(placeholderNode);
            changed = true;
        }

        for (var index = 0; index < picker.Items.Count; index++)
        {
            var optionNode = CreateOptionNode(
                index.ToString(CultureInfo.InvariantCulture),
                picker.Items[index] ?? string.Empty,
                disabled: false);
            PlatformView.AddChild(optionNode);
            _optionNodes.Add(optionNode);
            changed = true;
        }

        changed |= UpdateSelectedValueProjection(picker);
        changed |= UpdateSelectedOptionProjection(picker);
        changed |= UpdateDisplayedTextColor(picker);
        return changed;
    }

    private MiragePlatformView CreateOptionNode(string value, string text, bool disabled)
    {
        var optionNode = MiragePlatformView.CreateWeb("option");
        RegisterOptionNode(optionNode);
        optionNode.SetAttribute("value", value);
        optionNode.SetProperty("textContent", text);
        optionNode.SetAttribute("disabled", disabled ? "disabled" : null);
        return optionNode;
    }

    private void RegisterOptionNode(MiragePlatformView optionNode)
    {
        if (Coordinator is { } coordinator)
        {
            coordinator.Register(optionNode, eventTarget: null);
            return;
        }

        if (!string.IsNullOrWhiteSpace(optionNode.NodeId))
        {
            return;
        }

        var localId = Interlocked.Increment(ref s_optionNodeIdCounter);
        optionNode.AssignNodeId($"picker-option-{localId:D4}");
    }

    private bool ClearOptions()
    {
        var changed = _optionNodes.Count > 0 || PlatformView.Children.Count > 0;
        if (changed)
        {
            PlatformView.ClearChildren();
        }

        if (Coordinator is { } coordinator)
        {
            foreach (var optionNode in _optionNodes)
            {
                coordinator.Unregister(optionNode);
            }
        }

        _optionNodes.Clear();
        return changed;
    }

    private bool UpdateBackgroundProjection(IPicker picker)
    {
        return MirageCommonViewProjection.MapBackground(this, picker);
    }

    private bool UpdateCharacterSpacingProjection(IPicker picker)
    {
        return MirageCommonViewProjection.MapCharacterSpacing(this, picker);
    }

    private bool UpdateFontProjection(IPicker picker)
    {
        return MirageCommonViewProjection.MapFont(this, picker);
    }

    private bool UpdateTitleProjection(IPicker picker)
    {
        var semanticLabel = picker.Semantics?.Description;
        var title = string.IsNullOrWhiteSpace(semanticLabel) && !string.IsNullOrWhiteSpace(picker.Title)
            ? picker.Title
            : null;
        return SetPropertyIfChanged("Title", title);
    }

    private bool UpdateSelectedValueProjection(IPicker picker)
    {
        var selectedIndex = NormalizeSelectedIndex(picker, picker.SelectedIndex);
        var selectedValue = selectedIndex >= 0
            ? selectedIndex.ToString(CultureInfo.InvariantCulture)
            : string.Empty;

        return SetPropertyIfChanged("SelectedValue", selectedValue);
    }

    private bool UpdateSelectedOptionProjection(IPicker picker)
    {
        if (_optionNodes.Count == 0)
        {
            return false;
        }

        var selectedIndex = NormalizeSelectedIndex(picker, picker.SelectedIndex);
        var hasPlaceholder = HasPlaceholder(picker);
        var changed = false;

        for (var optionIndex = 0; optionIndex < _optionNodes.Count; optionIndex++)
        {
            var selected = hasPlaceholder
                ? (selectedIndex < 0 ? optionIndex == 0 : optionIndex == selectedIndex + 1)
                : optionIndex == selectedIndex;

            var selectedAttribute = selected ? "selected" : null;
            var optionNode = _optionNodes[optionIndex];
            optionNode.Attributes.TryGetValue("selected", out var currentAttribute);
            if (string.Equals(currentAttribute, selectedAttribute, StringComparison.Ordinal))
            {
                continue;
            }

            optionNode.SetAttribute("selected", selectedAttribute);
            changed = true;
        }

        return changed;
    }

    private bool UpdateDisplayedTextColor(IPicker picker)
    {
        var selectedIndex = NormalizeSelectedIndex(picker, picker.SelectedIndex);
        var useTitleColor = HasPlaceholder(picker) && selectedIndex < 0;

        var cssColor = useTitleColor
            ? MirageCommonViewProjection.ToCssColor(picker.TitleColor)
            : ResolvePickerTextColor(picker);

        if (cssColor is null && useTitleColor)
        {
            cssColor = ResolvePickerTextColor(picker);
        }

        return SetStyleIfChanged("color", cssColor);
    }

    private bool UpdateHorizontalTextAlignmentProjection(IPicker picker)
    {
        var alignment = picker.HorizontalTextAlignment switch
        {
            TextAlignment.Center => "center",
            TextAlignment.End => "end",
            _ => "start"
        };

        return SetStyleIfChanged("textAlign", alignment);
    }

    private bool UpdateVerticalTextAlignmentProjection(IPicker picker)
    {
        string paddingTop;
        string paddingBottom;

        switch (picker.VerticalTextAlignment)
        {
            case TextAlignment.Start:
                paddingTop = "0px";
                paddingBottom = EdgeVerticalInset;
                break;
            case TextAlignment.End:
                paddingTop = EdgeVerticalInset;
                paddingBottom = "0px";
                break;
            default:
                paddingTop = CenterVerticalInset;
                paddingBottom = CenterVerticalInset;
                break;
        }

        var changed = SetStyleIfChanged("paddingTop", paddingTop);
        changed |= SetStyleIfChanged("paddingBottom", paddingBottom);
        return changed;
    }

    private bool UpdateEnabledProjection(IPicker picker)
    {
        var isEnabled = picker.IsEnabled;
        var changed = SetPropertyIfChanged("Disabled", isEnabled ? null : "disabled");
        changed |= SetStyleIfChanged("cursor", isEnabled ? "pointer" : "default");
        return changed;
    }

    private void OnRuntimeSelectionChanged(string? payload)
    {
        if (!VirtualView.IsEnabled)
        {
            return;
        }

        var selectedIndex = ParseSelectedIndex(VirtualView, payload);
        if (VirtualView.SelectedIndex != selectedIndex)
        {
            VirtualView.SelectedIndex = selectedIndex;
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

    private void BeginProgrammaticOpen(IPicker picker)
    {
        ResetOpenRetryState();
        var attemptVersion = BeginOpenAttemptVersion();

        if (!picker.IsEnabled)
        {
            OnRuntimeIsOpenChanged(isOpen: false);
            return;
        }

        if (Coordinator is not { } coordinator || string.IsNullOrWhiteSpace(PlatformView.NodeId))
        {
            OnRuntimeIsOpenChanged(isOpen: false);
            return;
        }

        if (coordinator.TryRequestNodePickerOpen(PlatformView.NodeId))
        {
            ResetOpenRetryState();
            return;
        }

        _remainingOpenRetryAttempts = MaxOpenRetryAttempts;
        ScheduleOpenRetryOrRollback(attemptVersion);
    }

    private void BeginProgrammaticClose()
    {
        InvalidateOpenAttemptVersion();

        if (Coordinator is { } coordinator && !string.IsNullOrWhiteSpace(PlatformView.NodeId))
        {
            _ = coordinator.TrySetNodeFocus(PlatformView.NodeId, isFocused: false);
        }

        ResetOpenRetryState();
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

    private static string? ResolvePickerTextColor(IPicker picker)
    {
        return picker is ITextStyle textStyle
            ? MirageCommonViewProjection.ToCssColor(textStyle.TextColor)
            : null;
    }

    private static int NormalizeSelectedIndex(IPicker picker, int selectedIndex)
    {
        return selectedIndex >= 0 && selectedIndex < picker.Items.Count
            ? selectedIndex
            : -1;
    }

    private static int ParseSelectedIndex(IPicker picker, string? payload)
    {
        if (HasPlaceholder(picker) && string.IsNullOrWhiteSpace(payload))
        {
            return -1;
        }

        return int.TryParse(payload, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? NormalizeSelectedIndex(picker, parsed)
            : -1;
    }

    private static bool HasPlaceholder(IPicker picker)
    {
        return !string.IsNullOrWhiteSpace(picker.Title);
    }

    private static Size ResolveMeasuredSizeWithConstraints(
        IPicker picker,
        double widthConstraint,
        double heightConstraint,
        double measuredWidth,
        double measuredHeight)
    {
        var clampedWidth = ResolveDesiredDimension(widthConstraint, measuredWidth);
        var clampedHeight = ResolveDesiredDimension(heightConstraint, measuredHeight);
        var (requestedWidth, requestedMinimumWidth, requestedMaximumWidth) = ResolveRequestedDimensionRequests(picker, isWidth: true);
        var (requestedHeight, requestedMinimumHeight, requestedMaximumHeight) = ResolveRequestedDimensionRequests(picker, isWidth: false);
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

    private static (double Exact, double Minimum, double Maximum) ResolveRequestedDimensionRequests(IPicker picker, bool isWidth)
    {
        if (picker is VisualElement visualElement)
        {
            return isWidth
                ? (visualElement.WidthRequest, visualElement.MinimumWidthRequest, visualElement.MaximumWidthRequest)
                : (visualElement.HeightRequest, visualElement.MinimumHeightRequest, visualElement.MaximumHeightRequest);
        }

        return isWidth
            ? (double.NaN, picker.MinimumWidth, picker.MaximumWidth)
            : (double.NaN, picker.MinimumHeight, picker.MaximumHeight);
    }
}
