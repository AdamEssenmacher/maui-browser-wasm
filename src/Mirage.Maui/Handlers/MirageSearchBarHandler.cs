using Mirage.Core.Diagnostics;
using Mirage.Core.Semantics;
using Mirage.Web.Runtime;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Mirage.Maui.Handlers;

internal sealed class MirageSearchBarHandler : MirageViewHandler<ISearchBar>
{
    private const string InputNodeClass = "mirage-searchbar-input";
    private const string ClearButtonNodeClass = "mirage-searchbar-clear";
    private const string SubmitButtonNodeClass = "mirage-searchbar-submit";
    private static readonly TimeSpan ClearLikeSearchSuppressionWindow = TimeSpan.FromMilliseconds(250);
    private static int s_childNodeIdCounter;
    private static readonly HashSet<string> UnsupportedWarnings = [];

    private MiragePlatformView? _inputNode;
    private MiragePlatformView? _clearButtonNode;
    private MiragePlatformView? _submitButtonNode;
    private SearchBarInputRuntimeEventTarget? _inputEventTarget;
    private SearchBarButtonRuntimeEventTarget? _clearButtonEventTarget;
    private SearchBarButtonRuntimeEventTarget? _submitButtonEventTarget;
    private bool _isApplyingRuntimeInput;
    private long? _lastClearLikeInputTimestamp;

    internal static readonly string[] ParityPropertyKeys =
    {
        nameof(ISearchBar.Background),
        nameof(ISearchBar.CharacterSpacing),
        nameof(ISearchBar.Font),
        nameof(ITextAlignment.HorizontalTextAlignment),
        nameof(ITextAlignment.VerticalTextAlignment),
        nameof(ISearchBar.IsReadOnly),
        nameof(ISearchBar.IsTextPredictionEnabled),
        nameof(ISearchBar.IsSpellCheckEnabled),
        nameof(ISearchBar.MaxLength),
        nameof(ISearchBar.Placeholder),
        nameof(ISearchBar.PlaceholderColor),
        nameof(ISearchBar.Text),
        nameof(ISearchBar.TextColor),
        nameof(ISearchBar.CancelButtonColor),
        nameof(ISearchBar.SearchIconColor),
        nameof(ISearchBar.Keyboard),
        nameof(ISearchBar.ReturnType)
    };

    internal static readonly string[] ParityCommandKeys =
    {
        nameof(IView.Focus),
        nameof(IView.Unfocus)
    };

    internal static readonly string[] ParityExtensionPropertyKeys =
    {
        nameof(IView.IsEnabled)
    };

    public static IPropertyMapper<ISearchBar, MirageSearchBarHandler> Mapper =
        new PropertyMapper<ISearchBar, MirageSearchBarHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(IView.AutomationId)] = MapAutomationId,
            [nameof(ISearchBar.Background)] = MapBackground,
            [nameof(ISearchBar.CharacterSpacing)] = MapCharacterSpacing,
            [nameof(ISearchBar.Font)] = MapFont,
            ["FontFamily"] = MapFont,
            ["FontSize"] = MapFont,
            ["FontAttributes"] = MapFont,
            ["FontAutoScalingEnabled"] = MapFont,
            [nameof(ITextAlignment.HorizontalTextAlignment)] = MapHorizontalTextAlignment,
            [nameof(ITextAlignment.VerticalTextAlignment)] = MapVerticalTextAlignment,
            [nameof(ISearchBar.IsReadOnly)] = MapIsReadOnly,
            [nameof(ISearchBar.IsTextPredictionEnabled)] = MapIsTextPredictionEnabled,
            [nameof(ISearchBar.IsSpellCheckEnabled)] = MapIsSpellCheckEnabled,
            [nameof(ISearchBar.MaxLength)] = MapMaxLength,
            [nameof(ISearchBar.Placeholder)] = MapPlaceholder,
            [nameof(ISearchBar.PlaceholderColor)] = MapPlaceholderColor,
            [nameof(ISearchBar.Text)] = MapText,
            [nameof(ISearchBar.TextColor)] = MapTextColor,
            [nameof(ISearchBar.CancelButtonColor)] = MapCancelButtonColor,
            [nameof(ISearchBar.SearchIconColor)] = MapSearchIconColor,
            [nameof(ISearchBar.Keyboard)] = MapKeyboard,
            [nameof(ISearchBar.ReturnType)] = MapReturnType,
            [nameof(IView.IsEnabled)] = MapIsEnabled,
            ["IsEnabled"] = MapIsEnabled
        };

    public static CommandMapper<ISearchBar, MirageSearchBarHandler> CommandMapper =
        new(MirageViewMapper.CommonViewCommandMapper)
        {
            [nameof(IView.Focus)] = MapFocus,
            [nameof(IView.Unfocus)] = MapUnfocus
        };

    public MirageSearchBarHandler()
        : base(ComponentKind.SearchBar, Mapper, CommandMapper)
    {
        DefaultDisplayStyle = "flex";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        platformView.SetStyle("display", DefaultDisplayStyle);
        var changed = EnsureStructure();
        changed |= ApplySearchBarProjectionCore(VirtualView);
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

    public static void MapBackground(MirageSearchBarHandler handler, ISearchBar searchBar) => handler.MapSearchBar(searchBar);
    public static void MapAutomationId(MirageSearchBarHandler handler, ISearchBar searchBar)
    {
        MirageViewMapper.MapAutomationId(handler, searchBar);
        handler.MapSearchBar(searchBar);
    }
    public static void MapCharacterSpacing(MirageSearchBarHandler handler, ISearchBar searchBar) => handler.MapSearchBar(searchBar);
    public static void MapFont(MirageSearchBarHandler handler, ISearchBar searchBar) => handler.MapSearchBar(searchBar);
    public static void MapHorizontalTextAlignment(MirageSearchBarHandler handler, ISearchBar searchBar) => handler.MapSearchBar(searchBar);
    public static void MapVerticalTextAlignment(MirageSearchBarHandler handler, ISearchBar searchBar) => handler.MapSearchBar(searchBar);
    public static void MapIsReadOnly(MirageSearchBarHandler handler, ISearchBar searchBar) => handler.MapSearchBar(searchBar);
    public static void MapIsTextPredictionEnabled(MirageSearchBarHandler handler, ISearchBar searchBar) => handler.MapSearchBar(searchBar);
    public static void MapIsSpellCheckEnabled(MirageSearchBarHandler handler, ISearchBar searchBar) => handler.MapSearchBar(searchBar);
    public static void MapMaxLength(MirageSearchBarHandler handler, ISearchBar searchBar) => handler.MapSearchBar(searchBar);
    public static void MapPlaceholder(MirageSearchBarHandler handler, ISearchBar searchBar) => handler.MapSearchBar(searchBar);
    public static void MapPlaceholderColor(MirageSearchBarHandler handler, ISearchBar searchBar) => handler.MapSearchBar(searchBar);
    public static void MapText(MirageSearchBarHandler handler, ISearchBar searchBar) => handler.MapSearchBar(searchBar);
    public static void MapTextColor(MirageSearchBarHandler handler, ISearchBar searchBar) => handler.MapSearchBar(searchBar);
    public static void MapCancelButtonColor(MirageSearchBarHandler handler, ISearchBar searchBar) => handler.MapSearchBar(searchBar);
    public static void MapSearchIconColor(MirageSearchBarHandler handler, ISearchBar searchBar) => handler.MapSearchBar(searchBar);
    public static void MapKeyboard(MirageSearchBarHandler handler, ISearchBar searchBar) => handler.MapSearchBar(searchBar);
    public static void MapReturnType(MirageSearchBarHandler handler, ISearchBar searchBar) => handler.MapSearchBar(searchBar);
    public static void MapIsEnabled(MirageSearchBarHandler handler, ISearchBar searchBar) => handler.MapSearchBar(searchBar);
    public static void MapFocus(MirageSearchBarHandler handler, ISearchBar searchBar, object? args)
    {
        var focusRequest = args as FocusRequest;

        if (!searchBar.IsEnabled
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

    public static void MapUnfocus(MirageSearchBarHandler handler, ISearchBar searchBar, object? args)
    {
        _ = searchBar;
        _ = args;

        if (handler.Coordinator is not { } coordinator
            || handler._inputNode is null
            || string.IsNullOrWhiteSpace(handler._inputNode.NodeId))
        {
            return;
        }

        _ = coordinator.TrySetNodeFocus(handler._inputNode.NodeId, isFocused: false);
    }

    private void MapSearchBar(ISearchBar searchBar)
    {
        if (!ApplySearchBarProjectionCore(searchBar))
        {
            return;
        }

        RequestRender();
    }

    private bool ApplySearchBarProjectionCore(ISearchBar searchBar)
    {
        var changed = EnsureStructure();

        using var _ = BeginRenderBatch();

        changed |= ApplyContainerStyles(searchBar);
        changed |= UpdateAutomationIdProjection(searchBar);
        changed |= UpdateBackgroundProjection(searchBar);
        changed |= UpdateInputValueProjection(searchBar);
        changed |= UpdatePlaceholderProjection(searchBar);
        changed |= UpdatePlaceholderColorProjection(searchBar);
        changed |= UpdateTextColorProjection(searchBar);
        changed |= UpdateFontProjection(searchBar);
        changed |= UpdateCharacterSpacingProjection(searchBar);
        changed |= UpdateReadOnlyProjection(searchBar);
        changed |= UpdateKeyboardProjection(searchBar);
        changed |= UpdateMaxLengthProjection(searchBar);
        changed |= UpdatePredictionProjection(searchBar);
        changed |= UpdateSpellCheckProjection(searchBar);
        changed |= UpdateHorizontalTextAlignmentProjection(searchBar);
        changed |= UpdateVerticalTextAlignmentProjection(searchBar);
        changed |= UpdateReturnTypeProjection(searchBar);
        changed |= UpdateCancelButtonColorProjection(searchBar);
        changed |= UpdateSearchButtonColorProjection(searchBar);
        changed |= UpdateEnabledProjection(searchBar);
        changed |= UpdateClearButtonProjection(searchBar);
        return changed;
    }

    private bool EnsureStructure()
    {
        var changed = false;
        if (_inputNode is null)
        {
            _inputNode = MiragePlatformView.CreateWeb("input");
            _inputNode.AddClass(InputNodeClass);
            _inputNode.SetAttribute("type", "search");
            _inputNode.SetEvent("input", enabled: true);
            _inputNode.SetEvent("search", enabled: true);
            _inputNode.SetEvent("keydown", enabled: true);
            _inputNode.SetEvent("focus", enabled: true);
            _inputNode.SetEvent("blur", enabled: true);
            _inputEventTarget = new SearchBarInputRuntimeEventTarget(this);
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
            _clearButtonEventTarget = new SearchBarButtonRuntimeEventTarget(this, isClearButton: true);
            RegisterChildNode(_clearButtonNode, _clearButtonEventTarget);
            changed = true;
        }

        if (_submitButtonNode is null)
        {
            _submitButtonNode = MiragePlatformView.CreateWeb("button");
            _submitButtonNode.AddClass(SubmitButtonNodeClass);
            _submitButtonNode.SetAttribute("type", "button");
            _submitButtonNode.SetProperty("textContent", "Search");
            _submitButtonNode.SetEvent("click", enabled: true);
            _submitButtonEventTarget = new SearchBarButtonRuntimeEventTarget(this, isClearButton: false);
            RegisterChildNode(_submitButtonNode, _submitButtonEventTarget);
            changed = true;
        }

        if (PlatformView.Children.Count != 3
            || !ReferenceEquals(PlatformView.Children[0], _inputNode)
            || !ReferenceEquals(PlatformView.Children[1], _clearButtonNode)
            || !ReferenceEquals(PlatformView.Children[2], _submitButtonNode))
        {
            PlatformView.ClearChildren();
            PlatformView.AddChild(_inputNode);
            PlatformView.AddChild(_clearButtonNode);
            PlatformView.AddChild(_submitButtonNode);
            changed = true;
        }

        return changed;
    }

    private void DisconnectStructure()
    {
        PlatformView.ClearChildren();
        UnregisterChildNode(_inputNode);
        UnregisterChildNode(_clearButtonNode);
        UnregisterChildNode(_submitButtonNode);
        _inputNode = null;
        _clearButtonNode = null;
        _submitButtonNode = null;
        _inputEventTarget = null;
        _clearButtonEventTarget = null;
        _submitButtonEventTarget = null;
    }

    private bool ApplyContainerStyles(ISearchBar searchBar)
    {
        _ = searchBar;
        var changed = SetStyleIfChanged(PlatformView, "alignItems", "center");
        changed |= SetStyleIfChanged(PlatformView, "gap", "8px");
        changed |= SetStyleIfChanged(PlatformView, "boxSizing", "border-box");

        if (_inputNode is not null)
        {
            changed |= SetStyleIfChanged(_inputNode, "flex", "1 1 auto");
            changed |= SetStyleIfChanged(_inputNode, "minWidth", "0");
            changed |= SetStyleIfChanged(_inputNode, "boxSizing", "border-box");
        }

        if (_clearButtonNode is not null)
        {
            changed |= SetStyleIfChanged(_clearButtonNode, "padding", "4px 8px");
            changed |= SetStyleIfChanged(_clearButtonNode, "whiteSpace", "nowrap");
        }

        if (_submitButtonNode is not null)
        {
            changed |= SetStyleIfChanged(_submitButtonNode, "padding", "4px 10px");
            changed |= SetStyleIfChanged(_submitButtonNode, "whiteSpace", "nowrap");
        }

        return changed;
    }

    private bool UpdateAutomationIdProjection(ISearchBar searchBar)
    {
        var baseAutomationId = string.IsNullOrWhiteSpace(searchBar.AutomationId)
            ? null
            : searchBar.AutomationId;
        var changed = false;
        changed |= SetAttributeIfChanged(_inputNode, "data-automation-id", baseAutomationId is null ? null : $"{baseAutomationId}-input");
        changed |= SetAttributeIfChanged(_clearButtonNode, "data-automation-id", baseAutomationId is null ? null : $"{baseAutomationId}-clear-button");
        changed |= SetAttributeIfChanged(_submitButtonNode, "data-automation-id", baseAutomationId is null ? null : $"{baseAutomationId}-search-button");
        return changed;
    }

    private bool UpdateBackgroundProjection(ISearchBar searchBar)
    {
        return MirageCommonViewProjection.MapBackground(this, searchBar);
    }

    private bool UpdateInputValueProjection(ISearchBar searchBar)
    {
        if (_inputNode is null)
        {
            return false;
        }

        var nextText = searchBar.Text ?? string.Empty;
        var changed = false;
        if (!(_isApplyingRuntimeInput
              && string.Equals(_inputNode.GetPropertyString("value"), nextText, StringComparison.Ordinal)))
        {
            changed |= SetPropertyIfChanged(_inputNode, "value", nextText);
        }

        changed |= SetAttributeIfChanged(_inputNode, "aria-label", searchBar.Placeholder);
        return changed;
    }

    private bool UpdatePlaceholderProjection(ISearchBar searchBar)
    {
        return SetAttributeIfChanged(_inputNode, "placeholder", string.IsNullOrWhiteSpace(searchBar.Placeholder) ? null : searchBar.Placeholder);
    }

    private bool UpdatePlaceholderColorProjection(ISearchBar searchBar)
    {
        var cssColor = MirageCommonViewProjection.ToCssColor(searchBar.PlaceholderColor);
        return SetStyleIfChanged(_inputNode, "--mirage-searchbar-placeholder-color", cssColor);
    }

    private bool UpdateTextColorProjection(ISearchBar searchBar)
    {
        var cssColor = MirageCommonViewProjection.ToCssColor(searchBar.TextColor);
        return SetStyleIfChanged(_inputNode, "color", cssColor);
    }

    private bool UpdateFontProjection(ISearchBar searchBar)
    {
        var changed = MirageCommonViewProjection.MapFont(this, searchBar);
        changed |= SetStyleIfChanged(_inputNode, "fontSize", PlatformView.GetStyleValue("fontSize"));
        changed |= SetStyleIfChanged(_inputNode, "fontWeight", PlatformView.GetStyleValue("fontWeight"));
        changed |= SetStyleIfChanged(_inputNode, "fontStyle", PlatformView.GetStyleValue("fontStyle"));
        changed |= SetStyleIfChanged(_inputNode, "fontFamily", PlatformView.GetStyleValue("fontFamily"));
        return changed;
    }

    private bool UpdateCharacterSpacingProjection(ISearchBar searchBar)
    {
        string? spacingStyle = null;
        if (double.IsFinite(searchBar.CharacterSpacing))
        {
            spacingStyle = MirageCommonViewProjection.ToPxNoClamp(searchBar.CharacterSpacing);
        }

        return SetStyleIfChanged(_inputNode, "letterSpacing", spacingStyle);
    }

    private bool UpdateReadOnlyProjection(ISearchBar searchBar)
    {
        return SetAttributeIfChanged(_inputNode, "readonly", searchBar.IsReadOnly ? "readonly" : null);
    }

    private bool UpdateKeyboardProjection(ISearchBar searchBar)
    {
        if (_inputNode is null)
        {
            return false;
        }

        var (inputType, inputMode) = ResolveKeyboard(searchBar.Keyboard);
        var changed = SetAttributeIfChanged(_inputNode, "type", inputType);
        changed |= SetAttributeIfChanged(_inputNode, "inputmode", inputMode);
        return changed;
    }

    private bool UpdateMaxLengthProjection(ISearchBar searchBar)
    {
        var maxLength = searchBar.MaxLength > 0
            ? searchBar.MaxLength.ToString(CultureInfo.InvariantCulture)
            : null;
        return SetAttributeIfChanged(_inputNode, "maxlength", maxLength);
    }

    private bool UpdatePredictionProjection(ISearchBar searchBar)
    {
        var changed = SetAttributeIfChanged(_inputNode, "autocorrect", searchBar.IsTextPredictionEnabled ? "on" : "off");
        changed |= SetAttributeIfChanged(_inputNode, "autocomplete", searchBar.IsTextPredictionEnabled ? "on" : "off");
        return changed;
    }

    private bool UpdateSpellCheckProjection(ISearchBar searchBar)
    {
        return SetAttributeIfChanged(_inputNode, "spellcheck", searchBar.IsSpellCheckEnabled ? "true" : "false");
    }

    private bool UpdateHorizontalTextAlignmentProjection(ISearchBar searchBar)
    {
        var textAlign = searchBar.HorizontalTextAlignment switch
        {
            TextAlignment.Center => "center",
            TextAlignment.End => "right",
            TextAlignment.Justify => "justify",
            _ => "left"
        };

        return SetStyleIfChanged(_inputNode, "textAlign", textAlign);
    }

    private bool UpdateVerticalTextAlignmentProjection(ISearchBar searchBar)
    {
        var (paddingTop, paddingBottom) = searchBar.VerticalTextAlignment switch
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

    private bool UpdateReturnTypeProjection(ISearchBar searchBar)
    {
        var hint = searchBar.ReturnType switch
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

    private bool UpdateCancelButtonColorProjection(ISearchBar searchBar)
    {
        var cssColor = MirageCommonViewProjection.ToCssColor(searchBar.CancelButtonColor);
        return SetStyleIfChanged(_clearButtonNode, "color", cssColor);
    }

    private bool UpdateSearchButtonColorProjection(ISearchBar searchBar)
    {
        var cssColor = MirageCommonViewProjection.ToCssColor(searchBar.SearchIconColor);
        if (cssColor is null && searchBar.SearchIconColor is not null)
        {
            LogUnsupportedSearchColor(searchBar.SearchIconColor.GetType().Name);
        }

        return SetStyleIfChanged(_submitButtonNode, "color", cssColor);
    }

    private bool UpdateEnabledProjection(ISearchBar searchBar)
    {
        var disabled = searchBar.IsEnabled ? null : "disabled";
        var changed = SetAttributeIfChanged(_inputNode, "disabled", disabled);
        changed |= SetAttributeIfChanged(_submitButtonNode, "disabled", disabled);
        changed |= SetStyleIfChanged(_inputNode, "cursor", searchBar.IsEnabled ? "text" : "not-allowed");
        changed |= SetStyleIfChanged(_submitButtonNode, "cursor", searchBar.IsEnabled ? "pointer" : "not-allowed");
        changed |= SetStyleIfChanged(PlatformView, "cursor", searchBar.IsEnabled ? "text" : "not-allowed");
        return changed;
    }

    private bool UpdateClearButtonProjection(ISearchBar searchBar)
    {
        var hasText = !string.IsNullOrEmpty(searchBar.Text);
        var shouldShow = hasText && searchBar.IsEnabled && !searchBar.IsReadOnly;
        var changed = SetStyleIfChanged(_clearButtonNode, "visibility", shouldShow ? "visible" : "hidden");
        changed |= SetStyleIfChanged(_clearButtonNode, "pointerEvents", shouldShow ? "auto" : "none");
        changed |= SetStyleIfChanged(_clearButtonNode, "cursor", shouldShow ? "pointer" : "default");
        changed |= SetAttributeIfChanged(_clearButtonNode, "disabled", shouldShow ? null : "disabled");
        return changed;
    }

    private void OnInputEvent(string eventName, string? payload)
    {
        switch (eventName)
        {
            case MirageRuntimeEventNames.SuppressedFocusStateSync:
                SyncSuppressedFocusState(isFocused: true);
                return;
            case "input":
            {
                var runtimeValue = payload ?? string.Empty;
                var hadTextBeforeEvent = !string.IsNullOrEmpty(VirtualView.Text);
                var valueChanged = !string.Equals(VirtualView.Text, runtimeValue, StringComparison.Ordinal);

                OnTextChangedFromRuntime(runtimeValue);

                if (valueChanged && hadTextBeforeEvent && runtimeValue.Length == 0)
                {
                    _lastClearLikeInputTimestamp = Stopwatch.GetTimestamp();
                }
                else
                {
                    _lastClearLikeInputTimestamp = null;
                }

                return;
            }
            case "search":
            {
                var runtimeValue = payload ?? string.Empty;
                var hadTextBeforeEvent = !string.IsNullOrEmpty(VirtualView.Text);
                var valueChanged = !string.Equals(VirtualView.Text, runtimeValue, StringComparison.Ordinal);

                OnTextChangedFromRuntime(runtimeValue);

                // Browsers can fire `search` when the native clear affordance is clicked.
                // Treat "value changed from non-empty to empty" as clear, not submit.
                var clearLikeSearchEvent = (valueChanged && hadTextBeforeEvent && runtimeValue.Length == 0)
                    || (runtimeValue.Length == 0 && IsWithinClearSuppressionWindow());

                if (!clearLikeSearchEvent)
                {
                    OnSearchInvoked();
                }

                _lastClearLikeInputTimestamp = null;
                return;
            }
            case "keydown":
                // Any keyboard interaction after an emptying input transition indicates user intent.
                // Keep clear-affordance suppression for native clear events, but do not suppress
                // a subsequent explicit keyboard submit (Enter) for empty queries.
                _lastClearLikeInputTimestamp = null;
                return;
            case "focus":
                if (!VirtualView.IsFocused)
                {
                    VirtualView.IsFocused = true;
                }

                return;
            case "blur":
                if (VirtualView.IsFocused)
                {
                    VirtualView.IsFocused = false;
                }

                return;
        }
    }

    private void SyncSuppressedFocusState(bool isFocused)
    {
        if (VirtualView is VisualElement visualElement)
        {
            _ = MirageFocusStateSynchronizer.TrySetFocusedStateSilently(visualElement, isFocused);
        }
    }

    private void OnButtonEvent(bool isClearButton)
    {
        if (isClearButton)
        {
            OnClearInvoked();
            return;
        }

        OnSearchInvoked();
    }

    private void OnTextChangedFromRuntime(string value)
    {
        if (_inputNode is null)
        {
            return;
        }

        _ = SetPropertyIfChanged(_inputNode, "value", value);
        if (string.Equals(VirtualView.Text, value, StringComparison.Ordinal))
        {
            if (UpdateClearButtonProjection(VirtualView))
            {
                RequestRender();
            }

            return;
        }

        _isApplyingRuntimeInput = true;
        try
        {
            VirtualView.UpdateText(value);
        }
        finally
        {
            _isApplyingRuntimeInput = false;
        }

        if (UpdateClearButtonProjection(VirtualView))
        {
            RequestRender();
        }
    }

    private void OnSearchInvoked()
    {
        if (!VirtualView.IsEnabled)
        {
            return;
        }

        VirtualView.SearchButtonPressed();
    }

    private void OnClearInvoked()
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
        }
        finally
        {
            _isApplyingRuntimeInput = false;
        }

        if (UpdateClearButtonProjection(VirtualView))
        {
            RequestRender();
        }
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
        childNode.AssignNodeId($"searchbar-node-{localId:D4}");
    }

    private void UnregisterChildNode(MiragePlatformView? childNode)
    {
        if (childNode is null || Coordinator is not { } coordinator)
        {
            return;
        }

        coordinator.Unregister(childNode);
    }

    private static (string InputType, string InputMode) ResolveKeyboard(Keyboard keyboard)
    {
        // Keep SearchBar as type=search so browser submit semantics remain stable and
        // the runtime "search" event still fires when users submit from the keyboard.
        var inputType = "search";
        var inputMode = "text";

        if (keyboard == Keyboard.Numeric || keyboard.Equals(Keyboard.Numeric))
        {
            inputMode = "decimal";
        }
        else if (keyboard == Keyboard.Email || keyboard.Equals(Keyboard.Email))
        {
            inputMode = "email";
        }
        else if (keyboard == Keyboard.Telephone || keyboard.Equals(Keyboard.Telephone))
        {
            inputMode = "tel";
        }
        else if (keyboard == Keyboard.Url || keyboard.Equals(Keyboard.Url))
        {
            inputMode = "url";
        }
        else if (keyboard == Keyboard.Date || keyboard.Equals(Keyboard.Date))
        {
            inputMode = "numeric";
        }
        else if (keyboard == Keyboard.Time || keyboard.Equals(Keyboard.Time))
        {
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

    private bool IsWithinClearSuppressionWindow()
    {
        if (_lastClearLikeInputTimestamp is not { } clearLikeInputTimestamp)
        {
            return false;
        }

        var elapsed = Stopwatch.GetElapsedTime(clearLikeInputTimestamp, Stopwatch.GetTimestamp());
        return elapsed <= ClearLikeSearchSuppressionWindow;
    }

    private static void LogUnsupportedSearchColor(string colorTypeName)
    {
        var key = $"SearchIconColor:{colorTypeName}";
        lock (UnsupportedWarnings)
        {
            if (!UnsupportedWarnings.Add(key))
            {
                return;
            }
        }

        MirageDiagnostics.Log(
            "searchbar.color",
            $"property=SearchIconColor colorType={colorTypeName} unsupported=true fallback=default-button-color");
    }

    private sealed class SearchBarInputRuntimeEventTarget(MirageSearchBarHandler owner) : IMirageRuntimeEventTarget
    {
        public void OnRuntimeEvent(string eventName, string? payload)
        {
            owner.OnInputEvent(eventName, payload);
        }
    }

    private sealed class SearchBarButtonRuntimeEventTarget(MirageSearchBarHandler owner, bool isClearButton) : IMirageRuntimeEventTarget
    {
        public void OnRuntimeEvent(string eventName, string? payload)
        {
            _ = eventName;
            _ = payload;
            owner.OnButtonEvent(isClearButton);
        }
    }
}
