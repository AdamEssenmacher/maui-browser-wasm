using Mirage.Core.Semantics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageEditorHandler : MirageViewHandler<IEditor>, IMirageRuntimeEventTarget
{
    private const string InputNodeClass = "mirage-editor-input";
    private const string ControlMinHeightExpression = "calc(var(--base-height-multiplier, 11) * 8px)";
    private static readonly TimeSpan InitialSelectionRetryDelay = TimeSpan.FromMilliseconds(2);
    private const int MaxInitialSelectionRetryCount = 6;
    private static int s_childNodeIdCounter;

    internal static readonly string[] ParityPropertyKeys =
    [
        nameof(IEditor.Background),
        nameof(IEditor.CharacterSpacing),
        nameof(IEditor.Font),
        nameof(IEditor.IsReadOnly),
        nameof(IEditor.IsTextPredictionEnabled),
        nameof(IEditor.IsSpellCheckEnabled),
        nameof(IEditor.MaxLength),
        nameof(IEditor.Placeholder),
        nameof(IEditor.PlaceholderColor),
        nameof(IEditor.Text),
        nameof(IEditor.TextColor),
        nameof(IEditor.HorizontalTextAlignment),
        nameof(IEditor.VerticalTextAlignment),
        nameof(IEditor.Keyboard),
        nameof(IEditor.CursorPosition),
        nameof(IEditor.SelectionLength)
    ];

    internal static readonly string[] ParityCommandKeys =
    [
        nameof(IView.Focus)
    ];

    internal static readonly string[] ParityControlsRemapPropertyKeys =
    [
        nameof(Editor.Text),
        nameof(InputView.TextTransform)
    ];

    internal static readonly string[] ParityExtensionPropertyKeys =
    [
        nameof(IView.IsEnabled)
    ];

    internal static readonly string[] ParityExtensionCommandKeys =
    [
        nameof(IView.Unfocus)
    ];

    public static IPropertyMapper<IEditor, MirageEditorHandler> Mapper =
        new PropertyMapper<IEditor, MirageEditorHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(IEditor.Background)] = MapBackground,
            [nameof(IEditor.CharacterSpacing)] = MapCharacterSpacing,
            [nameof(IEditor.Font)] = MapFont,
            ["FontFamily"] = MapFont,
            ["FontSize"] = MapFont,
            ["FontAttributes"] = MapFont,
            ["FontAutoScalingEnabled"] = MapFont,
            [nameof(IEditor.IsReadOnly)] = MapIsReadOnly,
            [nameof(IEditor.IsTextPredictionEnabled)] = MapIsTextPredictionEnabled,
            [nameof(IEditor.IsSpellCheckEnabled)] = MapIsSpellCheckEnabled,
            [nameof(IEditor.MaxLength)] = MapMaxLength,
            [nameof(IEditor.Placeholder)] = MapPlaceholder,
            [nameof(IEditor.PlaceholderColor)] = MapPlaceholderColor,
            [nameof(IEditor.Text)] = MapText,
            [nameof(IEditor.TextColor)] = MapTextColor,
            [nameof(IEditor.HorizontalTextAlignment)] = MapHorizontalTextAlignment,
            [nameof(IEditor.VerticalTextAlignment)] = MapVerticalTextAlignment,
            [nameof(IEditor.Keyboard)] = MapKeyboard,
            [nameof(IEditor.CursorPosition)] = MapCursorPosition,
            [nameof(IEditor.SelectionLength)] = MapSelectionLength,
            [nameof(InputView.TextTransform)] = MapTextTransform,
            ["TextTransform"] = MapTextTransform,
            [nameof(IView.IsEnabled)] = MapIsEnabled,
            ["IsEnabled"] = MapIsEnabled
        };

    public static CommandMapper<IEditor, MirageEditorHandler> CommandMapper =
        new(MirageViewMapper.CommonViewCommandMapper)
        {
            [nameof(IView.Focus)] = MapFocus,
            [nameof(IView.Unfocus)] = MapUnfocus
        };

    private MiragePlatformView? _inputNode;
    private bool _isApplyingRuntimeInput;
    private bool _isApplyingRuntimeSelection;
    private bool _isConnected;
    private bool _initialSelectionRetryScheduled;
    private int _initialSelectionRetryCount;

    public MirageEditorHandler()
        : base(ComponentKind.Editor, Mapper, CommandMapper)
    {
        DefaultDisplayStyle = "flex";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        _isConnected = true;
        _initialSelectionRetryCount = 0;
        _initialSelectionRetryScheduled = false;
        platformView.SetStyle("display", DefaultDisplayStyle);
        var changed = EnsureStructure();
        changed |= ApplyEditorProjectionCore(VirtualView);
        base.ConnectHandler(platformView);
        if (changed)
        {
            RequestRender();
        }

        EnsureInitialSelectionProjectionAfterConnect();
    }

    protected override void DisconnectHandler(MiragePlatformView platformView)
    {
        _isConnected = false;
        _initialSelectionRetryScheduled = false;
        _initialSelectionRetryCount = 0;
        DisconnectStructure();
        base.DisconnectHandler(platformView);
    }

    public void OnRuntimeEvent(string eventName, string? payload)
    {
        OnInputEvent(eventName, payload);
    }

    public static void MapBackground(MirageEditorHandler handler, IEditor editor) => handler.MapEditor(editor);
    public static void MapCharacterSpacing(MirageEditorHandler handler, IEditor editor) => handler.MapEditor(editor);
    public static void MapFont(MirageEditorHandler handler, IEditor editor) => handler.MapEditor(editor);
    public static void MapIsReadOnly(MirageEditorHandler handler, IEditor editor) => handler.MapEditor(editor);
    public static void MapIsTextPredictionEnabled(MirageEditorHandler handler, IEditor editor) => handler.MapEditor(editor);
    public static void MapIsSpellCheckEnabled(MirageEditorHandler handler, IEditor editor) => handler.MapEditor(editor);
    public static void MapMaxLength(MirageEditorHandler handler, IEditor editor) => handler.MapEditor(editor);
    public static void MapPlaceholder(MirageEditorHandler handler, IEditor editor) => handler.MapEditor(editor);
    public static void MapPlaceholderColor(MirageEditorHandler handler, IEditor editor) => handler.MapEditor(editor);
    public static void MapText(MirageEditorHandler handler, IEditor editor) => handler.MapEditor(editor);
    public static void MapTextColor(MirageEditorHandler handler, IEditor editor) => handler.MapEditor(editor);
    public static void MapHorizontalTextAlignment(MirageEditorHandler handler, IEditor editor) => handler.MapEditor(editor);
    public static void MapVerticalTextAlignment(MirageEditorHandler handler, IEditor editor) => handler.MapEditor(editor);
    public static void MapKeyboard(MirageEditorHandler handler, IEditor editor) => handler.MapEditor(editor);
    public static void MapTextTransform(MirageEditorHandler handler, IEditor editor) => handler.MapEditor(editor);
    public static void MapIsEnabled(MirageEditorHandler handler, IEditor editor) => handler.MapEditor(editor);

    public static void MapCursorPosition(MirageEditorHandler handler, IEditor editor)
    {
        handler.ApplySelectionFromVirtualView(editor);
    }

    public static void MapSelectionLength(MirageEditorHandler handler, IEditor editor)
    {
        handler.ApplySelectionFromVirtualView(editor);
    }

    public static void MapFocus(MirageEditorHandler handler, IEditor editor, object? args)
    {
        var focusRequest = args as FocusRequest;

        if (!editor.IsEnabled
            || handler.Coordinator is not { } coordinator
            || handler._inputNode is null
            || string.IsNullOrWhiteSpace(handler._inputNode.NodeId))
        {
            focusRequest?.TrySetResult(false);
            return;
        }

        var focused = coordinator.TrySetNodeFocus(handler._inputNode.NodeId, isFocused: true);
        focusRequest?.TrySetResult(focused);
    }

    public static void MapUnfocus(MirageEditorHandler handler, IEditor editor, object? args)
    {
        _ = editor;
        _ = args;

        if (handler.Coordinator is not { } coordinator
            || handler._inputNode is null
            || string.IsNullOrWhiteSpace(handler._inputNode.NodeId))
        {
            return;
        }

        _ = coordinator.TrySetNodeFocus(handler._inputNode.NodeId, isFocused: false);
    }

    private void MapEditor(IEditor editor)
    {
        if (!ApplyEditorProjectionCore(editor))
        {
            return;
        }

        RequestRender();
    }

    private bool ApplyEditorProjectionCore(IEditor editor)
    {
        var changed = EnsureStructure();

        using var _ = BeginRenderBatch();

        changed |= ApplyContainerStyles();
        changed |= UpdateBackgroundProjection(editor);
        changed |= UpdateInputValueProjection(editor);
        changed |= UpdatePlaceholderProjection(editor);
        changed |= UpdatePlaceholderColorProjection(editor);
        changed |= UpdateTextColorProjection(editor);
        changed |= UpdateFontProjection(editor);
        changed |= UpdateCharacterSpacingProjection(editor);
        changed |= UpdateReadOnlyProjection(editor);
        changed |= UpdateKeyboardProjection(editor);
        changed |= UpdateMaxLengthProjection(editor);
        changed |= UpdatePredictionProjection(editor);
        changed |= UpdateSpellCheckProjection(editor);
        changed |= UpdateHorizontalTextAlignmentProjection(editor);
        changed |= UpdateVerticalTextAlignmentProjection(editor);
        changed |= UpdateEnabledProjection(editor);
        changed |= UpdateFocusProjection(editor);

        ApplySelectionFromVirtualView(editor);
        return changed;
    }

    private bool EnsureStructure()
    {
        var changed = false;

        if (_inputNode is null)
        {
            _inputNode = MiragePlatformView.CreateWeb("textarea");
            _inputNode.AddClass(InputNodeClass);
            _inputNode.SetEvent("input", enabled: true);
            _inputNode.SetEvent("change", enabled: true);
            _inputNode.SetEvent("focus", enabled: true);
            _inputNode.SetEvent("blur", enabled: true);
            _inputNode.SetEvent("select", enabled: true);
            _inputNode.SetEvent("click", enabled: true);
            _inputNode.SetEvent("keydown", enabled: true);
            _inputNode.SetEvent("keyup", enabled: true);
            RegisterChildNode(_inputNode);
            changed = true;
        }

        if (PlatformView.Children.Count != 1 || !ReferenceEquals(PlatformView.Children[0], _inputNode))
        {
            PlatformView.ClearChildren();
            PlatformView.AddChild(_inputNode);
            changed = true;
        }

        return changed;
    }

    private void DisconnectStructure()
    {
        UnregisterChildNode(_inputNode);
        _inputNode = null;
        _isApplyingRuntimeInput = false;
        _isApplyingRuntimeSelection = false;
    }

    private bool ApplyContainerStyles()
    {
        var changed = SetStyleIfChanged(PlatformView, "alignItems", "stretch");
        changed |= SetStyleIfChanged(PlatformView, "boxSizing", "border-box");
        changed |= SetStyleIfChanged(PlatformView, "minHeight", ControlMinHeightExpression);
        changed |= SetStyleIfChanged(PlatformView, "cursor", VirtualView.IsEnabled ? "text" : "not-allowed");

        if (_inputNode is not null)
        {
            changed |= SetStyleIfChanged(_inputNode, "flex", "1 1 auto");
            changed |= SetStyleIfChanged(_inputNode, "minWidth", "0");
            changed |= SetStyleIfChanged(_inputNode, "width", "100%");
            changed |= SetStyleIfChanged(_inputNode, "height", "100%");
            changed |= SetStyleIfChanged(_inputNode, "boxSizing", "border-box");
            changed |= SetStyleIfChanged(_inputNode, "resize", "none");
            changed |= SetStyleIfChanged(_inputNode, "background", "transparent");
            changed |= SetStyleIfChanged(_inputNode, "cursor", VirtualView.IsEnabled ? "text" : "not-allowed");
            changed |= SetStyleIfChanged(_inputNode, "textTransform", null);
        }

        return changed;
    }

    private bool UpdateBackgroundProjection(IEditor editor)
    {
        return MirageCommonViewProjection.MapBackground(this, editor);
    }

    private bool UpdateInputValueProjection(IEditor editor)
    {
        if (_inputNode is null)
        {
            return false;
        }

        var transformedText = GetTransformedText(editor, editor.Text);
        var changed = false;
        if (!(_isApplyingRuntimeInput && string.Equals(_inputNode.GetPropertyString("value"), transformedText, StringComparison.Ordinal)))
        {
            changed |= SetPropertyIfChanged(_inputNode, "value", transformedText);
        }

        changed |= SetAttributeIfChanged(_inputNode, "aria-label", string.IsNullOrWhiteSpace(editor.Placeholder) ? "Editor" : editor.Placeholder);
        return changed;
    }

    private bool UpdatePlaceholderProjection(IEditor editor)
    {
        return SetAttributeIfChanged(_inputNode, "placeholder", string.IsNullOrWhiteSpace(editor.Placeholder) ? null : editor.Placeholder);
    }

    private bool UpdatePlaceholderColorProjection(IEditor editor)
    {
        var cssColor = MirageCommonViewProjection.ToCssColor(editor.PlaceholderColor);
        return SetStyleIfChanged(_inputNode, "--mirage-editor-placeholder-color", cssColor);
    }

    private bool UpdateTextColorProjection(IEditor editor)
    {
        var cssColor = MirageCommonViewProjection.ToCssColor(editor.TextColor);
        return SetStyleIfChanged(_inputNode, "color", cssColor);
    }

    private bool UpdateFontProjection(IEditor editor)
    {
        var changed = MirageCommonViewProjection.MapFont(this, editor);
        changed |= SetStyleIfChanged(_inputNode, "fontSize", PlatformView.GetStyleValue("fontSize"));
        changed |= SetStyleIfChanged(_inputNode, "fontWeight", PlatformView.GetStyleValue("fontWeight"));
        changed |= SetStyleIfChanged(_inputNode, "fontStyle", PlatformView.GetStyleValue("fontStyle"));
        changed |= SetStyleIfChanged(_inputNode, "fontFamily", PlatformView.GetStyleValue("fontFamily"));
        return changed;
    }

    private bool UpdateCharacterSpacingProjection(IEditor editor)
    {
        string? spacingStyle = null;
        if (double.IsFinite(editor.CharacterSpacing))
        {
            spacingStyle = MirageCommonViewProjection.ToPxNoClamp(editor.CharacterSpacing);
        }

        return SetStyleIfChanged(_inputNode, "letterSpacing", spacingStyle);
    }

    private bool UpdateReadOnlyProjection(IEditor editor)
    {
        return SetAttributeIfChanged(_inputNode, "readonly", editor.IsReadOnly ? "readonly" : null);
    }

    private bool UpdateKeyboardProjection(IEditor editor)
    {
        var inputMode = ResolveInputMode(editor.Keyboard);
        return SetAttributeIfChanged(_inputNode, "inputmode", inputMode);
    }

    private bool UpdateMaxLengthProjection(IEditor editor)
    {
        var maxLength = editor.MaxLength >= 0
            ? editor.MaxLength.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : null;
        return SetAttributeIfChanged(_inputNode, "maxlength", maxLength);
    }

    private bool UpdatePredictionProjection(IEditor editor)
    {
        var changed = SetAttributeIfChanged(_inputNode, "autocorrect", editor.IsTextPredictionEnabled ? "on" : "off");
        changed |= SetAttributeIfChanged(_inputNode, "autocomplete", editor.IsTextPredictionEnabled ? "on" : "off");
        return changed;
    }

    private bool UpdateSpellCheckProjection(IEditor editor)
    {
        return SetAttributeIfChanged(_inputNode, "spellcheck", editor.IsSpellCheckEnabled ? "true" : "false");
    }

    private bool UpdateHorizontalTextAlignmentProjection(IEditor editor)
    {
        var textAlign = editor.HorizontalTextAlignment switch
        {
            TextAlignment.Center => "center",
            TextAlignment.End => "right",
            TextAlignment.Justify => "justify",
            _ => "left"
        };

        return SetStyleIfChanged(_inputNode, "textAlign", textAlign);
    }

    private bool UpdateVerticalTextAlignmentProjection(IEditor editor)
    {
        var (paddingTop, paddingBottom) = editor.VerticalTextAlignment switch
        {
            TextAlignment.Start => ("4px", "8px"),
            TextAlignment.Center => ("6px", "6px"),
            TextAlignment.End => ("8px", "4px"),
            _ => ("6px", "6px")
        };

        var changed = SetStyleIfChanged(_inputNode, "verticalAlign", null);
        changed |= SetStyleIfChanged(_inputNode, "paddingTop", paddingTop);
        changed |= SetStyleIfChanged(_inputNode, "paddingBottom", paddingBottom);
        return changed;
    }

    private bool UpdateEnabledProjection(IEditor editor)
    {
        var disabled = editor.IsEnabled ? null : "disabled";
        var changed = SetAttributeIfChanged(_inputNode, "disabled", disabled);
        changed |= SetStyleIfChanged(_inputNode, "cursor", editor.IsEnabled ? "text" : "not-allowed");
        changed |= SetStyleIfChanged(PlatformView, "cursor", editor.IsEnabled ? "text" : "not-allowed");
        return changed;
    }

    private bool UpdateFocusProjection(IEditor editor)
    {
        return SetAttributeIfChanged(_inputNode, "data-maui-editor-focused", editor.IsFocused ? "true" : "false");
    }

    private void ApplySelectionFromVirtualView(IEditor editor)
    {
        _ = TryApplySelectionFromVirtualView(editor);
    }

    private bool TryApplySelectionFromVirtualView(IEditor editor)
    {
        if (_isApplyingRuntimeSelection
            || Coordinator is not { } coordinator
            || _inputNode is null
            || string.IsNullOrWhiteSpace(_inputNode.NodeId))
        {
            return false;
        }

        var sourceValue = editor.Text ?? string.Empty;
        var normalizedStart = Math.Clamp(editor.CursorPosition, 0, sourceValue.Length);
        var normalizedLength = Math.Max(0, editor.SelectionLength);
        var normalizedEnd = Math.Clamp(normalizedStart + normalizedLength, normalizedStart, sourceValue.Length);
        var mappedStart = MapTransformedSelectionIndex(editor, sourceValue, normalizedStart);
        var mappedEnd = MapTransformedSelectionIndex(editor, sourceValue, normalizedEnd);
        var renderedTextLength = (_inputNode.GetPropertyString("value") ?? string.Empty).Length;
        mappedStart = Math.Clamp(mappedStart, 0, renderedTextLength);
        mappedEnd = Math.Clamp(mappedEnd, mappedStart, renderedTextLength);
        return coordinator.TrySetNodeTextSelection(_inputNode.NodeId, mappedStart, mappedEnd);
    }

    private void EnsureInitialSelectionProjectionAfterConnect()
    {
        if (TryApplySelectionFromVirtualView(VirtualView))
        {
            _initialSelectionRetryCount = 0;
            _initialSelectionRetryScheduled = false;
            return;
        }

        ScheduleInitialSelectionRetry();
    }

    private void ScheduleInitialSelectionRetry()
    {
        if (!_isConnected
            || _initialSelectionRetryScheduled
            || _initialSelectionRetryCount >= MaxInitialSelectionRetryCount
            || Coordinator is not { } coordinator)
        {
            return;
        }

        _initialSelectionRetryScheduled = true;
        _initialSelectionRetryCount++;
        if (!coordinator.DispatchDelayed(InitialSelectionRetryDelay, RetryInitialSelectionProjection))
        {
            _initialSelectionRetryScheduled = false;
            RetryInitialSelectionProjection();
        }
    }

    private void RetryInitialSelectionProjection()
    {
        _initialSelectionRetryScheduled = false;
        if (!_isConnected)
        {
            return;
        }

        if (TryApplySelectionFromVirtualView(VirtualView))
        {
            _initialSelectionRetryCount = 0;
            return;
        }

        ScheduleInitialSelectionRetry();
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

        var renderedTextLength = (_inputNode.GetPropertyString("value") ?? string.Empty).Length;
        var runtimeStart = Math.Clamp(Math.Min(selectionStart, selectionEnd), 0, renderedTextLength);
        var runtimeEnd = Math.Clamp(Math.Max(selectionStart, selectionEnd), runtimeStart, renderedTextLength);
        var sourceValue = VirtualView.Text ?? string.Empty;
        var normalizedStart = MapSourceSelectionIndex(VirtualView, sourceValue, runtimeStart);
        var normalizedEnd = MapSourceSelectionIndex(VirtualView, sourceValue, runtimeEnd);
        normalizedStart = Math.Clamp(normalizedStart, 0, sourceValue.Length);
        normalizedEnd = Math.Clamp(normalizedEnd, normalizedStart, sourceValue.Length);
        var normalizedLength = normalizedEnd - normalizedStart;
        if (VirtualView.CursorPosition == normalizedStart
            && VirtualView.SelectionLength == normalizedLength)
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
            case "change":
                if (!VirtualView.IsEnabled || VirtualView.IsReadOnly)
                {
                    return;
                }

                OnRuntimeInputValue(payload ?? string.Empty);
                SyncSelectionFromRuntime();
                return;
            case "focus":
                if (!VirtualView.IsFocused)
                {
                    VirtualView.IsFocused = true;
                }

                return;
            case "blur":
                var wasFocused = VirtualView.IsFocused;
                if (wasFocused)
                {
                    VirtualView.IsFocused = false;
                    VirtualView.Completed();
                }

                return;
            case "keydown":
            case "keyup":
            case "select":
            case "click":
                SyncSelectionFromRuntime();
                return;
        }
    }

    private void OnRuntimeInputValue(string value)
    {
        var transformedValue = GetTransformedText(VirtualView, value);
        var runtimeSelection = CaptureRuntimeSelectionSnapshot(value.Length);
        if (_inputNode is not null)
        {
            var didUpdateValue = SetPropertyIfChanged(_inputNode, "value", transformedValue);
            if (didUpdateValue && runtimeSelection is { } selectionSnapshot)
            {
                PreserveRuntimeSelectionAfterTransformedInput(
                    value,
                    selectionSnapshot.SelectionStart,
                    selectionSnapshot.SelectionEnd);
            }
        }

        if (string.Equals(VirtualView.Text, transformedValue, StringComparison.Ordinal))
        {
            return;
        }

        _isApplyingRuntimeInput = true;
        _isApplyingRuntimeSelection = true;
        try
        {
            VirtualView.UpdateText(transformedValue);
        }
        finally
        {
            _isApplyingRuntimeSelection = false;
            _isApplyingRuntimeInput = false;
        }
    }

    private (int SelectionStart, int SelectionEnd)? CaptureRuntimeSelectionSnapshot(int rawValueLength)
    {
        if (Coordinator is not { } coordinator
            || _inputNode is null
            || string.IsNullOrWhiteSpace(_inputNode.NodeId))
        {
            return null;
        }

        if (!coordinator.TryGetNodeTextSelection(_inputNode.NodeId, out var selectionStart, out var selectionEnd))
        {
            return null;
        }

        var normalizedStart = Math.Clamp(Math.Min(selectionStart, selectionEnd), 0, rawValueLength);
        var normalizedEnd = Math.Clamp(Math.Max(selectionStart, selectionEnd), normalizedStart, rawValueLength);
        return (normalizedStart, normalizedEnd);
    }

    private void PreserveRuntimeSelectionAfterTransformedInput(string sourceValue, int selectionStart, int selectionEnd)
    {
        if (Coordinator is not { } coordinator
            || _inputNode is null
            || string.IsNullOrWhiteSpace(_inputNode.NodeId))
        {
            return;
        }

        var mappedStart = MapTransformedSelectionIndex(VirtualView, sourceValue, selectionStart);
        var mappedEnd = MapTransformedSelectionIndex(VirtualView, sourceValue, selectionEnd);
        _ = coordinator.TrySetNodeTextSelection(_inputNode.NodeId, mappedStart, mappedEnd);
    }

    private static int MapTransformedSelectionIndex(IEditor editor, string sourceValue, int sourceIndex)
    {
        var normalizedIndex = Math.Clamp(sourceIndex, 0, sourceValue.Length);
        var transformedPrefix = GetTransformedText(editor, sourceValue[..normalizedIndex]);
        return transformedPrefix.Length;
    }

    private static int MapSourceSelectionIndex(IEditor editor, string sourceValue, int transformedIndex)
    {
        if (sourceValue.Length == 0)
        {
            return 0;
        }

        var transformedLength = GetTransformedText(editor, sourceValue).Length;
        var normalizedIndex = Math.Clamp(transformedIndex, 0, transformedLength);
        if (normalizedIndex == 0)
        {
            return 0;
        }

        var previousMappedLength = 0;
        for (var sourceIndex = 1; sourceIndex <= sourceValue.Length; sourceIndex++)
        {
            var mappedLength = MapTransformedSelectionIndex(editor, sourceValue, sourceIndex);
            if (normalizedIndex == mappedLength)
            {
                return sourceIndex;
            }

            if (normalizedIndex < mappedLength)
            {
                var distanceToPrevious = normalizedIndex - previousMappedLength;
                var distanceToCurrent = mappedLength - normalizedIndex;
                return distanceToCurrent < distanceToPrevious ? sourceIndex : sourceIndex - 1;
            }

            previousMappedLength = mappedLength;
        }

        return sourceValue.Length;
    }

    private static string ResolveInputMode(Keyboard keyboard)
    {
        if (keyboard == Keyboard.Numeric || keyboard.Equals(Keyboard.Numeric))
        {
            return "decimal";
        }

        if (keyboard == Keyboard.Email || keyboard.Equals(Keyboard.Email))
        {
            return "email";
        }

        if (keyboard == Keyboard.Telephone || keyboard.Equals(Keyboard.Telephone))
        {
            return "tel";
        }

        if (keyboard == Keyboard.Url || keyboard.Equals(Keyboard.Url))
        {
            return "url";
        }

        if (keyboard == Keyboard.Date || keyboard.Equals(Keyboard.Date))
        {
            return "numeric";
        }

        if (keyboard == Keyboard.Time || keyboard.Equals(Keyboard.Time))
        {
            return "numeric";
        }

        return "text";
    }

    private static string GetTransformedText(IEditor editor, string? sourceText)
    {
        var source = sourceText ?? string.Empty;
        if (editor is not InputView inputView)
        {
            return source;
        }

        var textTransform = inputView.TextTransform == TextTransform.Default
            ? TextTransform.None
            : inputView.TextTransform;
        return TextTransformUtilities.GetTransformedText(source, textTransform);
    }

    private void RegisterChildNode(MiragePlatformView childNode)
    {
        if (Coordinator is { } coordinator)
        {
            coordinator.Register(childNode, this);
            return;
        }

        if (!string.IsNullOrWhiteSpace(childNode.NodeId))
        {
            return;
        }

        var localId = Interlocked.Increment(ref s_childNodeIdCounter);
        childNode.AssignNodeId($"editor-node-{localId:D4}");
    }

    private void UnregisterChildNode(MiragePlatformView? childNode)
    {
        if (childNode is null || Coordinator is not { } coordinator)
        {
            return;
        }

        coordinator.Unregister(childNode);
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

        var current = node.Attributes.TryGetValue(attributeName, out var existing)
            ? existing
            : null;
        if (string.Equals(current, value, StringComparison.Ordinal))
        {
            return false;
        }

        node.SetAttribute(attributeName, value);
        return true;
    }
}
