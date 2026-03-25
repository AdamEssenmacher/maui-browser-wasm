using Mirage.Core.Diagnostics;
using Mirage.Core.Semantics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using System.Globalization;

namespace Mirage.Maui.Handlers;

internal sealed class MirageEntryHandler : MirageViewHandler<IEntry>, IMirageRuntimeEventTarget
{
    private const string InputNodeClass = "mirage-entry-input";
    private const string ClearButtonNodeClass = "mirage-entry-clear";
    private const string ControlHeightExpression = "calc(var(--base-height-multiplier, 11) * 4px)";
    private static readonly TimeSpan InputFlushDelay = TimeSpan.FromMilliseconds(1);
    private static int s_childNodeIdCounter;

    internal static readonly string[] ParityPropertyKeys =
    [
        nameof(IEntry.Background),
        nameof(IEntry.CharacterSpacing),
        nameof(IEntry.ClearButtonVisibility),
        nameof(IEntry.Font),
        nameof(IEntry.IsPassword),
        nameof(IEntry.HorizontalTextAlignment),
        nameof(IEntry.VerticalTextAlignment),
        nameof(IEntry.IsReadOnly),
        nameof(IEntry.IsTextPredictionEnabled),
        nameof(IEntry.IsSpellCheckEnabled),
        nameof(IEntry.Keyboard),
        nameof(IEntry.MaxLength),
        nameof(IEntry.Placeholder),
        nameof(IEntry.PlaceholderColor),
        nameof(IEntry.ReturnType),
        nameof(IEntry.Text),
        nameof(IEntry.TextColor),
        nameof(IEntry.CursorPosition),
        nameof(IEntry.SelectionLength)
    ];

    internal static readonly string[] ParityCommandKeys =
    [
        nameof(IView.Focus),
        nameof(IView.Unfocus)
    ];

    internal static readonly string[] ParityExtensionPropertyKeys =
    [
        nameof(IView.IsEnabled)
    ];

    public static IPropertyMapper<IEntry, MirageEntryHandler> Mapper =
        new PropertyMapper<IEntry, MirageEntryHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(IEntry.Background)] = MapBackground,
            [nameof(IEntry.CharacterSpacing)] = MapCharacterSpacing,
            [nameof(IEntry.ClearButtonVisibility)] = MapClearButtonVisibility,
            [nameof(IEntry.Font)] = MapFont,
            ["FontFamily"] = MapFont,
            ["FontSize"] = MapFont,
            ["FontAttributes"] = MapFont,
            ["FontAutoScalingEnabled"] = MapFont,
            [nameof(IEntry.IsPassword)] = MapIsPassword,
            [nameof(IEntry.HorizontalTextAlignment)] = MapHorizontalTextAlignment,
            [nameof(IEntry.VerticalTextAlignment)] = MapVerticalTextAlignment,
            [nameof(IEntry.IsReadOnly)] = MapIsReadOnly,
            [nameof(IEntry.IsTextPredictionEnabled)] = MapIsTextPredictionEnabled,
            [nameof(IEntry.IsSpellCheckEnabled)] = MapIsSpellCheckEnabled,
            [nameof(IEntry.Keyboard)] = MapKeyboard,
            [nameof(IEntry.MaxLength)] = MapMaxLength,
            [nameof(IEntry.Placeholder)] = MapPlaceholder,
            [nameof(IEntry.PlaceholderColor)] = MapPlaceholderColor,
            [nameof(IEntry.ReturnType)] = MapReturnType,
            [nameof(IEntry.Text)] = MapText,
            [nameof(IEntry.TextColor)] = MapTextColor,
            [nameof(IEntry.CursorPosition)] = MapCursorPosition,
            [nameof(IEntry.SelectionLength)] = MapSelectionLength,
            [nameof(IView.IsEnabled)] = MapIsEnabled,
            ["IsEnabled"] = MapIsEnabled,
            [nameof(VisualElement.WidthRequest)] = MapSizeRequestsAndLayoutAlignment,
            [nameof(VisualElement.HeightRequest)] = MapSizeRequestsAndLayoutAlignment,
            [nameof(VisualElement.MinimumWidthRequest)] = MapSizeRequests,
            [nameof(VisualElement.MinimumHeightRequest)] = MapSizeRequests,
            [nameof(VisualElement.MaximumWidthRequest)] = MapSizeRequests,
            [nameof(VisualElement.MaximumHeightRequest)] = MapSizeRequests
        };

    public static CommandMapper<IEntry, MirageEntryHandler> CommandMapper =
        new(MirageViewMapper.CommonViewCommandMapper)
        {
            [nameof(IView.Focus)] = MapFocus,
            [nameof(IView.Unfocus)] = MapUnfocus
        };

    private MiragePlatformView? _inputNode;
    private MiragePlatformView? _clearButtonNode;
    private EntryInputRuntimeEventTarget? _inputEventTarget;
    private EntryClearButtonRuntimeEventTarget? _clearButtonEventTarget;

    private bool _isApplyingRuntimeInput;
    private bool _isApplyingRuntimeSelection;
    private bool _isFocused;
    private bool _isComposingText;

    private int _nextInputEventId;
    private readonly object _inputSync = new();
    private string? _pendingInputValue;
    private bool _inputFlushScheduled;
    private int _pendingInputEventCount;
    private int _pendingInputEventId;
    private long _pendingInputReceivedTimestamp;
    private long _pendingBaselineRenderSequence;
    private long _pendingInputTextVersion;
    private int _inputEventReceivedCount;
    private int _inputFlushApplyCount;
    private int _inputCoalescedEventCount;
    private long _textVersion;

    public MirageEntryHandler()
        : base(ComponentKind.Entry, Mapper, CommandMapper)
    {
        DefaultDisplayStyle = "flex";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        platformView.SetStyle("display", DefaultDisplayStyle);
        var changed = EnsureStructure();
        changed |= ApplyEntryProjectionCore(VirtualView);
        base.ConnectHandler(platformView);
        if (changed)
        {
            RequestRender();
        }
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        DisconnectStructure();
        base.DisconnectHandler(platformView);
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        _ = eventName;
        _ = payload;
    }

    public static void MapBackground(MirageEntryHandler handler, IEntry entry) => handler.MapEntry(entry);
    public static void MapCharacterSpacing(MirageEntryHandler handler, IEntry entry) => handler.MapEntry(entry);
    public static void MapClearButtonVisibility(MirageEntryHandler handler, IEntry entry) => handler.MapEntry(entry);
    public static void MapFont(MirageEntryHandler handler, IEntry entry) => handler.MapEntry(entry);
    public static void MapIsPassword(MirageEntryHandler handler, IEntry entry) => handler.MapEntry(entry);
    public static void MapHorizontalTextAlignment(MirageEntryHandler handler, IEntry entry) => handler.MapEntry(entry);
    public static void MapVerticalTextAlignment(MirageEntryHandler handler, IEntry entry) => handler.MapEntry(entry);
    public static void MapIsReadOnly(MirageEntryHandler handler, IEntry entry) => handler.MapEntry(entry);
    public static void MapIsTextPredictionEnabled(MirageEntryHandler handler, IEntry entry) => handler.MapEntry(entry);
    public static void MapIsSpellCheckEnabled(MirageEntryHandler handler, IEntry entry) => handler.MapEntry(entry);
    public static void MapKeyboard(MirageEntryHandler handler, IEntry entry) => handler.MapEntry(entry);
    public static void MapMaxLength(MirageEntryHandler handler, IEntry entry) => handler.MapEntry(entry);
    public static void MapPlaceholder(MirageEntryHandler handler, IEntry entry) => handler.MapEntry(entry);
    public static void MapPlaceholderColor(MirageEntryHandler handler, IEntry entry) => handler.MapEntry(entry);
    public static void MapReturnType(MirageEntryHandler handler, IEntry entry) => handler.MapEntry(entry);
    public static void MapText(MirageEntryHandler handler, IEntry entry) => handler.MapEntry(entry);
    public static void MapTextColor(MirageEntryHandler handler, IEntry entry) => handler.MapEntry(entry);
    public static void MapIsEnabled(MirageEntryHandler handler, IEntry entry) => handler.MapEntry(entry);

    public static void MapCursorPosition(MirageEntryHandler handler, IEntry entry)
    {
        handler.ApplySelectionFromVirtualView(entry);
    }

    public static void MapSelectionLength(MirageEntryHandler handler, IEntry entry)
    {
        handler.ApplySelectionFromVirtualView(entry);
    }

    public static void MapFocus(MirageEntryHandler handler, IEntry entry, object? args)
    {
        var focusRequest = args as FocusRequest;

        if (!entry.IsEnabled
            || handler.Coordinator is not { } coordinator
            || handler._inputNode is null
            || string.IsNullOrWhiteSpace(handler._inputNode.NodeId))
        {
            focusRequest?.TrySetResult(false);
            return;
        }

        var focused = coordinator.TrySetNodeFocus(handler._inputNode.NodeId, isFocused: true);
        if (focused)
        {
            handler._isFocused = true;
            if (handler.UpdateClearButtonProjection(entry))
            {
                handler.RequestRender();
            }
        }

        focusRequest?.TrySetResult(focused);
    }

    public static void MapUnfocus(MirageEntryHandler handler, IEntry entry, object? args)
    {
        _ = entry;
        _ = args;

        if (handler.Coordinator is not { } coordinator
            || handler._inputNode is null
            || string.IsNullOrWhiteSpace(handler._inputNode.NodeId))
        {
            return;
        }

        _ = coordinator.TrySetNodeFocus(handler._inputNode.NodeId, isFocused: false);
        handler._isFocused = false;
        if (handler.UpdateClearButtonProjection(handler.VirtualView))
        {
            handler.RequestRender();
        }
    }

    public static void MapSizeRequests(MirageEntryHandler handler, IEntry entry)
    {
        var changed = MirageCommonViewProjection.MapSizeRequests(handler, entry);
        changed |= handler.UpdateControlHeightToken(entry);
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapSizeRequestsAndLayoutAlignment(MirageEntryHandler handler, IEntry entry)
    {
        var changed = MirageCommonViewProjection.MapSizeRequests(handler, entry);
        changed |= MirageCommonViewProjection.MapLayoutAlignment(handler, entry);
        changed |= handler.UpdateControlHeightToken(entry);
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
    }

    private void MapEntry(IEntry entry)
    {
        if (!ApplyEntryProjectionCore(entry))
        {
            return;
        }

        RequestRender();
    }

    private bool ApplyEntryProjectionCore(IEntry entry)
    {
        var changed = EnsureStructure();

        using var _ = BeginRenderBatch();

        changed |= ApplyContainerStyles(entry);
        changed |= UpdateBackgroundProjection(entry);
        changed |= UpdateInputValueProjection(entry);
        changed |= UpdatePlaceholderProjection(entry);
        changed |= UpdatePlaceholderColorProjection(entry);
        changed |= UpdateTextColorProjection(entry);
        changed |= UpdateFontProjection(entry);
        changed |= UpdateCharacterSpacingProjection(entry);
        changed |= UpdateReadOnlyProjection(entry);
        changed |= UpdateKeyboardProjection(entry);
        changed |= UpdateMaxLengthProjection(entry);
        changed |= UpdatePredictionProjection(entry);
        changed |= UpdateSpellCheckProjection(entry);
        changed |= UpdateHorizontalTextAlignmentProjection(entry);
        changed |= UpdateVerticalTextAlignmentProjection(entry);
        changed |= UpdateReturnTypeProjection(entry);
        changed |= UpdateEnabledProjection(entry);
        changed |= UpdateClearButtonProjection(entry);
        ApplySelectionFromVirtualView(entry);
        return changed;
    }

    private bool EnsureStructure()
    {
        var changed = false;
        if (_inputNode is null)
        {
            _inputNode = MiragePlatformView.CreateWeb("input");
            _inputNode.AddClass(InputNodeClass);
            _inputNode.SetAttribute("type", "text");
            _inputNode.SetEvent("input", enabled: true);
            _inputNode.SetEvent("keydown", enabled: true);
            _inputNode.SetEvent("select", enabled: true);
            _inputNode.SetEvent("focus", enabled: true);
            _inputNode.SetEvent("blur", enabled: true);
            _inputNode.SetEvent("click", enabled: true);
            _inputNode.SetEvent("keyup", enabled: true);
            _inputNode.SetEvent("compositionstart", enabled: true);
            _inputNode.SetEvent("compositionend", enabled: true);
            _inputEventTarget = new EntryInputRuntimeEventTarget(this);
            RegisterChildNode(_inputNode, _inputEventTarget);
            changed = true;
        }

        if (_clearButtonNode is null)
        {
            _clearButtonNode = MiragePlatformView.CreateWeb("button");
            _clearButtonNode.AddClass(ClearButtonNodeClass);
            _clearButtonNode.SetAttribute("type", "button");
            _clearButtonNode.SetProperty("textContent", "Clear");
            _clearButtonNode.SetEvent("click", enabled: true);
            _clearButtonEventTarget = new EntryClearButtonRuntimeEventTarget(this);
            RegisterChildNode(_clearButtonNode, _clearButtonEventTarget);
            changed = true;
        }

        if (PlatformView.Children.Count != 2
            || !ReferenceEquals(PlatformView.Children[0], _inputNode)
            || !ReferenceEquals(PlatformView.Children[1], _clearButtonNode))
        {
            PlatformView.ClearChildren();
            PlatformView.AddChild(_inputNode);
            PlatformView.AddChild(_clearButtonNode);
            changed = true;
        }

        return changed;
    }

    private void DisconnectStructure()
    {
        PlatformView.ClearChildren();
        UnregisterChildNode(_inputNode);
        UnregisterChildNode(_clearButtonNode);
        _inputNode = null;
        _clearButtonNode = null;
        _inputEventTarget = null;
        _clearButtonEventTarget = null;
        _isFocused = false;
        _isComposingText = false;
    }

    private bool ApplyContainerStyles(IEntry entry)
    {
        _ = entry;
        var changed = SetStyleIfChanged(PlatformView, "alignItems", "center");
        changed |= SetStyleIfChanged(PlatformView, "gap", "8px");
        changed |= SetStyleIfChanged(PlatformView, "boxSizing", "border-box");
        changed |= SetStyleIfChanged(PlatformView, "minHeight", ControlHeightExpression);

        if (_inputNode is not null)
        {
            changed |= SetStyleIfChanged(_inputNode, "flex", "1 1 auto");
            changed |= SetStyleIfChanged(_inputNode, "minWidth", "0");
            changed |= SetStyleIfChanged(_inputNode, "boxSizing", "border-box");
            changed |= SetStyleIfChanged(_inputNode, "minHeight", ControlHeightExpression);
        }

        if (_clearButtonNode is not null)
        {
            changed |= SetStyleIfChanged(_clearButtonNode, "padding", "4px 8px");
            changed |= SetStyleIfChanged(_clearButtonNode, "whiteSpace", "nowrap");
            changed |= SetStyleIfChanged(_clearButtonNode, "border", "0");
            changed |= SetStyleIfChanged(_clearButtonNode, "background", "transparent");
        }

        return changed;
    }

    private bool UpdateBackgroundProjection(IEntry entry)
    {
        return MirageCommonViewProjection.MapBackground(this, entry);
    }

    private bool UpdateInputValueProjection(IEntry entry)
    {
        if (_inputNode is null)
        {
            return false;
        }

        var nextText = entry.Text ?? string.Empty;
        var changed = false;
        if (!(_isApplyingRuntimeInput
            && string.Equals(_inputNode.GetPropertyString("value"), nextText, StringComparison.Ordinal)))
        {
            changed |= SetPropertyIfChanged(_inputNode, "value", nextText);
        }

        if (changed && !_isApplyingRuntimeInput)
        {
            Interlocked.Increment(ref _textVersion);
        }

        changed |= SetAttributeIfChanged(_inputNode, "aria-label", string.IsNullOrWhiteSpace(entry.Placeholder) ? "Entry" : entry.Placeholder);
        return changed;
    }

    private bool UpdatePlaceholderProjection(IEntry entry)
    {
        return SetAttributeIfChanged(_inputNode, "placeholder", string.IsNullOrWhiteSpace(entry.Placeholder) ? null : entry.Placeholder);
    }

    private bool UpdatePlaceholderColorProjection(IEntry entry)
    {
        var cssColor = MirageCommonViewProjection.ToCssColor(entry.PlaceholderColor);
        return SetStyleIfChanged(_inputNode, "--mirage-entry-placeholder-color", cssColor);
    }

    private bool UpdateTextColorProjection(IEntry entry)
    {
        var cssColor = MirageCommonViewProjection.ToCssColor(entry.TextColor);
        return SetStyleIfChanged(_inputNode, "color", cssColor);
    }

    private bool UpdateFontProjection(IEntry entry)
    {
        var changed = MirageCommonViewProjection.MapFont(this, entry);
        changed |= SetStyleIfChanged(_inputNode, "fontSize", PlatformView.GetStyleValue("fontSize"));
        changed |= SetStyleIfChanged(_inputNode, "fontWeight", PlatformView.GetStyleValue("fontWeight"));
        changed |= SetStyleIfChanged(_inputNode, "fontStyle", PlatformView.GetStyleValue("fontStyle"));
        changed |= SetStyleIfChanged(_inputNode, "fontFamily", PlatformView.GetStyleValue("fontFamily"));
        return changed;
    }

    private bool UpdateCharacterSpacingProjection(IEntry entry)
    {
        string? spacingStyle = null;
        if (double.IsFinite(entry.CharacterSpacing))
        {
            spacingStyle = MirageCommonViewProjection.ToPxNoClamp(entry.CharacterSpacing);
        }

        return SetStyleIfChanged(_inputNode, "letterSpacing", spacingStyle);
    }

    private bool UpdateReadOnlyProjection(IEntry entry)
    {
        return SetAttributeIfChanged(_inputNode, "readonly", entry.IsReadOnly ? "readonly" : null);
    }

    private bool UpdateKeyboardProjection(IEntry entry)
    {
        if (_inputNode is null)
        {
            return false;
        }

        var (inputType, inputMode) = ResolveKeyboard(entry.Keyboard, entry.IsPassword);
        var changed = SetAttributeIfChanged(_inputNode, "type", inputType);
        changed |= SetAttributeIfChanged(_inputNode, "inputmode", inputMode);
        return changed;
    }

    private bool UpdateMaxLengthProjection(IEntry entry)
    {
        var maxLength = entry.MaxLength > 0
            ? entry.MaxLength.ToString(CultureInfo.InvariantCulture)
            : null;
        return SetAttributeIfChanged(_inputNode, "maxlength", maxLength);
    }

    private bool UpdatePredictionProjection(IEntry entry)
    {
        var changed = SetAttributeIfChanged(_inputNode, "autocorrect", entry.IsTextPredictionEnabled ? "on" : "off");
        changed |= SetAttributeIfChanged(_inputNode, "autocomplete", entry.IsTextPredictionEnabled ? "on" : "off");
        return changed;
    }

    private bool UpdateSpellCheckProjection(IEntry entry)
    {
        return SetAttributeIfChanged(_inputNode, "spellcheck", entry.IsSpellCheckEnabled ? "true" : "false");
    }

    private bool UpdateHorizontalTextAlignmentProjection(IEntry entry)
    {
        var textAlign = entry.HorizontalTextAlignment switch
        {
            TextAlignment.Center => "center",
            TextAlignment.End => "right",
            TextAlignment.Justify => "justify",
            _ => "left"
        };

        return SetStyleIfChanged(_inputNode, "textAlign", textAlign);
    }

    private bool UpdateVerticalTextAlignmentProjection(IEntry entry)
    {
        var (paddingTop, paddingBottom) = entry.VerticalTextAlignment switch
        {
            TextAlignment.Start => ("2px", "6px"),
            TextAlignment.Center => ("4px", "4px"),
            TextAlignment.End => ("6px", "2px"),
            _ => ("4px", "4px")
        };

        var changed = SetStyleIfChanged(_inputNode, "verticalAlign", null);
        changed |= SetStyleIfChanged(_inputNode, "paddingTop", paddingTop);
        changed |= SetStyleIfChanged(_inputNode, "paddingBottom", paddingBottom);
        return changed;
    }

    private bool UpdateReturnTypeProjection(IEntry entry)
    {
        var hint = entry.ReturnType switch
        {
            ReturnType.Done => "done",
            ReturnType.Go => "go",
            ReturnType.Next => "next",
            ReturnType.Search => "search",
            ReturnType.Send => "send",
            _ => "enter"
        };

        return SetAttributeIfChanged(_inputNode, "enterkeyhint", hint);
    }

    private bool UpdateEnabledProjection(IEntry entry)
    {
        var disabled = entry.IsEnabled ? null : "disabled";
        var changed = SetAttributeIfChanged(_inputNode, "disabled", disabled);
        changed |= SetStyleIfChanged(_inputNode, "cursor", entry.IsEnabled ? "text" : "not-allowed");
        changed |= SetStyleIfChanged(PlatformView, "cursor", entry.IsEnabled ? "text" : "not-allowed");
        return changed;
    }

    private bool UpdateClearButtonProjection(IEntry entry)
    {
        var shouldShow = ShouldShowClearButton(entry);
        var changed = SetStyleIfChanged(_clearButtonNode, "visibility", shouldShow ? "visible" : "hidden");
        changed |= SetStyleIfChanged(_clearButtonNode, "pointerEvents", shouldShow ? "auto" : "none");
        changed |= SetStyleIfChanged(_clearButtonNode, "cursor", shouldShow ? "pointer" : "default");
        changed |= SetAttributeIfChanged(_clearButtonNode, "disabled", shouldShow ? null : "disabled");
        return changed;
    }

    private bool ShouldShowClearButton(IEntry entry)
    {
        if (!entry.IsEnabled || entry.IsReadOnly || string.IsNullOrEmpty(entry.Text))
        {
            return false;
        }

        return entry.ClearButtonVisibility switch
        {
            ClearButtonVisibility.WhileEditing => _isFocused,
            _ => false
        };
    }

    private void ApplySelectionFromVirtualView(IEntry entry)
    {
        if (_isApplyingRuntimeSelection
            || Coordinator is not { } coordinator
            || _inputNode is null
            || string.IsNullOrWhiteSpace(_inputNode.NodeId))
        {
            return;
        }

        var textLength = (entry.Text ?? string.Empty).Length;
        var cursorPosition = Math.Clamp(entry.CursorPosition, 0, textLength);
        var selectionLength = Math.Max(0, entry.SelectionLength);
        var selectionEnd = Math.Clamp(cursorPosition + selectionLength, 0, textLength);
        _ = coordinator.TrySetNodeTextSelection(_inputNode.NodeId, cursorPosition, selectionEnd);
    }

    private void SyncSelectionFromRuntime()
    {
        if (_isApplyingRuntimeSelection
            || Coordinator is not { } coordinator
            || _inputNode is null
            || string.IsNullOrWhiteSpace(_inputNode.NodeId))
        {
            return;
        }

        if (!coordinator.TryGetNodeTextSelection(_inputNode.NodeId, out var selectionStart, out var selectionEnd))
        {
            return;
        }

        var textLength = (VirtualView.Text ?? string.Empty).Length;
        var normalizedStart = Math.Clamp(Math.Min(selectionStart, selectionEnd), 0, textLength);
        var normalizedEnd = Math.Clamp(Math.Max(selectionStart, selectionEnd), normalizedStart, textLength);
        var normalizedLength = normalizedEnd - normalizedStart;
        if (VirtualView.CursorPosition == normalizedStart && VirtualView.SelectionLength == normalizedLength)
        {
            return;
        }

        _isApplyingRuntimeSelection = true;
        try
        {
            VirtualView.CursorPosition = normalizedStart;
            VirtualView.SelectionLength = normalizedLength;
        }
        finally
        {
            _isApplyingRuntimeSelection = false;
        }
    }

    private void OnInputEvent(string eventName, string? payload)
    {
        switch (eventName)
        {
            case "input":
                OnRuntimeInputValue(payload ?? string.Empty);
                SyncSelectionFromRuntime();
                return;
            case "keydown":
                if (!_isComposingText && string.Equals(payload, "Enter", StringComparison.Ordinal))
                {
                    OnCompletedInvoked();
                }

                SyncSelectionFromRuntime();
                return;
            case "focus":
                _isFocused = true;
                if (!VirtualView.IsFocused)
                {
                    VirtualView.IsFocused = true;
                }

                if (UpdateClearButtonProjection(VirtualView))
                {
                    RequestRender();
                }

                return;
            case "blur":
                _isFocused = false;
                _isComposingText = false;
                if (VirtualView.IsFocused)
                {
                    VirtualView.IsFocused = false;
                }

                if (UpdateClearButtonProjection(VirtualView))
                {
                    RequestRender();
                }

                return;
            case "select":
            case "click":
            case "keyup":
                SyncSelectionFromRuntime();
                return;
            case "compositionstart":
                _isComposingText = true;
                return;
            case "compositionend":
                _isComposingText = false;
                SyncSelectionFromRuntime();
                return;
        }
    }

    private void OnRuntimeInputValue(string value)
    {
        var diagnosticsEnabled = MirageDiagnostics.IsEnabled;
        var inputEventId = diagnosticsEnabled ? Interlocked.Increment(ref _nextInputEventId) : 0;
        var inputReceivedTimestamp = diagnosticsEnabled ? MirageDiagnostics.GetTimestamp() : 0;
        var baselineRenderSequence = diagnosticsEnabled ? MirageDiagnostics.GetLastRenderCompletedSequence() : 0;

        var shouldScheduleFlush = false;
        var queueDepth = 0;
        var textVersion = Volatile.Read(ref _textVersion);
        lock (_inputSync)
        {
            if (!_inputFlushScheduled
                && _pendingInputEventCount == 0
                && string.Equals(VirtualView.Text, value, StringComparison.Ordinal))
            {
                return;
            }

            _pendingInputValue = value;
            _pendingInputEventId = inputEventId;
            _pendingInputReceivedTimestamp = inputReceivedTimestamp;
            _pendingBaselineRenderSequence = baselineRenderSequence;
            _pendingInputTextVersion = textVersion;
            _pendingInputEventCount++;
            queueDepth = _pendingInputEventCount;
            if (_pendingInputEventCount > 1)
            {
                _inputCoalescedEventCount++;
            }

            if (!_inputFlushScheduled)
            {
                _inputFlushScheduled = true;
                shouldScheduleFlush = true;
            }
        }

        _inputEventReceivedCount++;

        if (diagnosticsEnabled)
        {
            MirageDiagnostics.Log(
                "entry.input",
                $"eventId={inputEventId} phase=received nodeId={PlatformView.NodeId} valueLength={value.Length} baselineRenderSequence={baselineRenderSequence} queueDepth={queueDepth} inputEventsReceived={_inputEventReceivedCount} inputEventsCoalesced={_inputCoalescedEventCount}");
        }

        if (shouldScheduleFlush)
        {
            SchedulePendingInputFlush();
        }
    }

    private void OnClearButtonInvoked()
    {
        if (!VirtualView.IsEnabled || VirtualView.IsReadOnly || string.IsNullOrEmpty(VirtualView.Text))
        {
            return;
        }

        if (_inputNode is not null)
        {
            _ = SetPropertyIfChanged(_inputNode, "value", string.Empty);
        }

        _isApplyingRuntimeInput = true;
        try
        {
            VirtualView.UpdateText(string.Empty);
            VirtualView.CursorPosition = 0;
            VirtualView.SelectionLength = 0;
        }
        finally
        {
            _isApplyingRuntimeInput = false;
        }

        if (_inputNode is not null
            && Coordinator is { } coordinator
            && !string.IsNullOrWhiteSpace(_inputNode.NodeId))
        {
            _ = coordinator.TrySetNodeTextSelection(_inputNode.NodeId, 0, 0);
        }

        if (UpdateClearButtonProjection(VirtualView))
        {
            RequestRender();
        }
    }

    private void OnCompletedInvoked()
    {
        if (!VirtualView.IsEnabled)
        {
            return;
        }

        VirtualView.Completed();
    }

    private void SchedulePendingInputFlush()
    {
        var dispatcher = ResolveDispatcher();
        if (dispatcher is null)
        {
            FlushPendingInput();
            return;
        }

        if (!dispatcher.DispatchDelayed(InputFlushDelay, FlushPendingInput))
        {
            if (!dispatcher.Dispatch(FlushPendingInput))
            {
                FlushPendingInput();
            }
        }
    }

    private void FlushPendingInput()
    {
        string? value;
        var eventId = 0;
        var eventCount = 0;
        var inputReceivedTimestamp = 0L;
        var baselineRenderSequence = 0L;
        var inputTextVersion = 0L;
        lock (_inputSync)
        {
            value = _pendingInputValue;
            eventId = _pendingInputEventId;
            eventCount = _pendingInputEventCount;
            inputReceivedTimestamp = _pendingInputReceivedTimestamp;
            baselineRenderSequence = _pendingBaselineRenderSequence;
            inputTextVersion = _pendingInputTextVersion;
            _pendingInputValue = null;
            _pendingInputEventCount = 0;
            _pendingInputEventId = 0;
            _pendingInputReceivedTimestamp = 0;
            _pendingBaselineRenderSequence = 0;
            _pendingInputTextVersion = 0;
            _inputFlushScheduled = false;
        }

        if (eventCount == 0)
        {
            return;
        }

        _inputFlushApplyCount++;
        var diagnosticsEnabled = MirageDiagnostics.IsEnabled;
        var valueToApply = value ?? string.Empty;
        var currentTextVersion = Volatile.Read(ref _textVersion);
        if (currentTextVersion != inputTextVersion
            && !string.Equals(VirtualView.Text, valueToApply, StringComparison.Ordinal))
        {
            if (diagnosticsEnabled)
            {
                MirageDiagnostics.Log(
                    "entry.input",
                    $"eventId={eventId} phase=flush-skip-stale eventCount={eventCount} staleTextVersion={inputTextVersion} currentTextVersion={currentTextVersion} inputFlushApplies={_inputFlushApplyCount} inputEventsCoalesced={_inputCoalescedEventCount}");
            }

            return;
        }

        if (string.Equals(VirtualView.Text, valueToApply, StringComparison.Ordinal))
        {
            if (diagnosticsEnabled)
            {
                MirageDiagnostics.Log(
                    "entry.input",
                    $"eventId={eventId} phase=flush-skip eventCount={eventCount} inputFlushApplies={_inputFlushApplyCount} inputEventsCoalesced={_inputCoalescedEventCount}");
            }

            return;
        }

        if (_inputNode is not null)
        {
            _ = SetPropertyIfChanged(_inputNode, "value", valueToApply);
        }

        var scheduledRenderSequenceBeforeSet = diagnosticsEnabled
            ? MirageDiagnostics.GetLastRenderScheduledSequence()
            : 0;

        _isApplyingRuntimeInput = true;
        try
        {
            var setStartTimestamp = diagnosticsEnabled ? MirageDiagnostics.GetTimestamp() : 0;
            VirtualView.UpdateText(valueToApply);
            if (diagnosticsEnabled)
            {
                var setElapsedMs = MirageDiagnostics.GetElapsedMilliseconds(setStartTimestamp, MirageDiagnostics.GetTimestamp());
                MirageDiagnostics.Log(
                    "entry.input",
                    $"eventId={eventId} phase=virtual-text-set setMs={setElapsedMs:0.###} eventCount={eventCount} inputFlushApplies={_inputFlushApplyCount} inputEventsCoalesced={_inputCoalescedEventCount}");
            }
        }
        finally
        {
            _isApplyingRuntimeInput = false;
        }

        SyncSelectionFromRuntime();
        if (UpdateClearButtonProjection(VirtualView))
        {
            RequestRender();
        }

        if (diagnosticsEnabled)
        {
            var scheduledRenderSequenceAfterSet = MirageDiagnostics.GetLastRenderScheduledSequence();
            if (scheduledRenderSequenceAfterSet > scheduledRenderSequenceBeforeSet)
            {
                _ = TrackRenderCompletionForInputAsync(
                    eventId,
                    baselineRenderSequence,
                    scheduledRenderSequenceAfterSet,
                    inputReceivedTimestamp);
            }
            else
            {
                MirageDiagnostics.Log(
                    "entry.input",
                    $"eventId={eventId} phase=render-tracking-skipped reason=no-new-render-request scheduledBefore={scheduledRenderSequenceBeforeSet} scheduledAfter={scheduledRenderSequenceAfterSet}");
            }
        }
    }

    private IDispatcher? ResolveDispatcher()
    {
        return MauiContext?.Services.GetService<IDispatcher>();
    }

    private static async Task TrackRenderCompletionForInputAsync(
        int inputEventId,
        long baselineRenderSequence,
        long expectedRenderSequence,
        long inputReceivedTimestamp)
    {
        const int pollIntervalMs = 10;
        const int maxWaitMs = 5000;

        var waitedMs = 0;
        while (waitedMs < maxWaitMs)
        {
            var completedSequence = MirageDiagnostics.GetLastRenderCompletedSequence();
            if (completedSequence >= expectedRenderSequence)
            {
                var completedTimestamp = MirageDiagnostics.GetLastRenderCompletedTimestamp();
                var latencyMs = MirageDiagnostics.GetElapsedMilliseconds(inputReceivedTimestamp, completedTimestamp);
                MirageDiagnostics.Log(
                    "entry.input",
                    $"eventId={inputEventId} phase=render-complete renderSequence={completedSequence} expectedRenderSequence={expectedRenderSequence} endToEndMs={latencyMs:0.###}");
                return;
            }

            await Task.Delay(pollIntervalMs);
            waitedMs += pollIntervalMs;
        }

        MirageDiagnostics.Log(
            "entry.input",
            $"eventId={inputEventId} phase=render-timeout baselineRenderSequence={baselineRenderSequence} expectedRenderSequence={expectedRenderSequence} waitedMs={maxWaitMs}");
    }

    private void RegisterChildNode(MiragePlatformView childNode, IMirageRuntimeEventTarget eventTarget)
    {
        if (Coordinator is { } coordinator)
        {
            coordinator.Register(childNode, eventTarget);
            return;
        }

        if (!string.IsNullOrWhiteSpace(childNode.NodeId))
        {
            return;
        }

        var localId = Interlocked.Increment(ref s_childNodeIdCounter);
        childNode.AssignNodeId($"entry-node-{localId:D4}");
    }

    private void UnregisterChildNode(MiragePlatformView? childNode)
    {
        if (childNode is null || Coordinator is not { } coordinator)
        {
            return;
        }

        coordinator.Unregister(childNode);
    }

    private static (string InputType, string InputMode) ResolveKeyboard(Keyboard keyboard, bool isPassword)
    {
        var inputMode = "text";
        var inputType = "text";

        if (isPassword || keyboard == Keyboard.Password || keyboard.Equals(Keyboard.Password))
        {
            inputType = "password";
            inputMode = "text";
        }
        else if (keyboard == Keyboard.Numeric || keyboard.Equals(Keyboard.Numeric))
        {
            inputType = "text";
            inputMode = "decimal";
        }
        else if (keyboard == Keyboard.Email || keyboard.Equals(Keyboard.Email))
        {
            inputType = "email";
            inputMode = "email";
        }
        else if (keyboard == Keyboard.Telephone || keyboard.Equals(Keyboard.Telephone))
        {
            inputType = "tel";
            inputMode = "tel";
        }
        else if (keyboard == Keyboard.Url || keyboard.Equals(Keyboard.Url))
        {
            inputType = "url";
            inputMode = "url";
        }
        else if (keyboard == Keyboard.Date || keyboard.Equals(Keyboard.Date))
        {
            inputType = "text";
            inputMode = "numeric";
        }
        else if (keyboard == Keyboard.Time || keyboard.Equals(Keyboard.Time))
        {
            inputType = "text";
            inputMode = "numeric";
        }

        return (inputType, inputMode);
    }

    private static bool SetStyleIfChanged(MiragePlatformView? node, string styleName, string? value)
    {
        if (node is null)
        {
            return false;
        }

        if (string.Equals(node.GetStyleValue(styleName), value, StringComparison.Ordinal))
        {
            return false;
        }

        node.SetStyle(styleName, value);
        return true;
    }

    private static bool SetPropertyIfChanged(MiragePlatformView? node, string propertyName, string? value)
    {
        if (node is null)
        {
            return false;
        }

        if (string.Equals(node.GetPropertyString(propertyName), value, StringComparison.Ordinal))
        {
            return false;
        }

        node.SetProperty(propertyName, value);
        return true;
    }

    private static bool SetAttributeIfChanged(MiragePlatformView? node, string attributeName, string? value)
    {
        if (node is null)
        {
            return false;
        }

        if (node.Attributes.TryGetValue(attributeName, out var existing)
            && string.Equals(existing, value, StringComparison.Ordinal))
        {
            return false;
        }

        if (value is null && !node.Attributes.ContainsKey(attributeName))
        {
            return false;
        }

        node.SetAttribute(attributeName, value);
        return true;
    }

    private bool UpdateControlHeightToken(IEntry entry)
    {
        if (entry is not VisualElement visualElement)
        {
            return SetStyleIfChanged(PlatformView, "--base-height-multiplier", null);
        }

        var preferredHeight = ResolvePreferredControlHeight(visualElement);
        if (preferredHeight is null)
        {
            return SetStyleIfChanged(PlatformView, "--base-height-multiplier", null);
        }

        const double fluentDesignUnit = 4d;
        var multiplier = Math.Max(1, preferredHeight.Value / fluentDesignUnit);
        var multiplierValue = $"{multiplier:0.###}";
        return SetStyleIfChanged(PlatformView, "--base-height-multiplier", multiplierValue);
    }

    private static double? ResolvePreferredControlHeight(VisualElement visualElement)
    {
        double? preferredHeight = null;
        if (IsFinitePositive(visualElement.HeightRequest))
        {
            preferredHeight = visualElement.HeightRequest;
        }
        else if (IsFinitePositive(visualElement.MinimumHeightRequest))
        {
            preferredHeight = visualElement.MinimumHeightRequest;
        }

        if (preferredHeight is null)
        {
            return null;
        }

        if (IsFinitePositive(visualElement.MaximumHeightRequest))
        {
            preferredHeight = Math.Min(preferredHeight.Value, visualElement.MaximumHeightRequest);
        }

        return preferredHeight.Value;
    }

    private static bool IsFinitePositive(double value)
    {
        return double.IsFinite(value) && value > 0;
    }

    private sealed class EntryInputRuntimeEventTarget(MirageEntryHandler owner) : IMirageRuntimeEventTarget
    {
        public void OnRuntimeEvent(string eventName, string? payload)
        {
            owner.OnInputEvent(eventName, payload);
        }
    }

    private sealed class EntryClearButtonRuntimeEventTarget(MirageEntryHandler owner) : IMirageRuntimeEventTarget
    {
        public void OnRuntimeEvent(string eventName, string? payload)
        {
            _ = eventName;
            _ = payload;
            owner.OnClearButtonInvoked();
        }
    }
}
