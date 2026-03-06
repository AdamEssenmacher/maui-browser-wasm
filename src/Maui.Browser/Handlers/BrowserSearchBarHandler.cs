using Maui.Browser.Dom;
using Maui.Browser.Measurement;
using Maui.Browser.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Maui.Browser.Handlers;

public sealed class BrowserSearchBarHandler : BrowserViewHandler<ISearchBar>
{
    public static IPropertyMapper<ISearchBar, BrowserSearchBarHandler> Mapper = new PropertyMapper<ISearchBar, BrowserSearchBarHandler>(BrowserViewMapper.CommonViewMapper)
    {
        [nameof(IView.AutomationId)] = MapAutomationId,
        [nameof(ISearchBar.Text)] = MapText,
        [nameof(ISearchBar.Placeholder)] = MapPlaceholder,
        [nameof(ISearchBar.PlaceholderColor)] = MapPlaceholderColor,
        [nameof(ITextStyle.TextColor)] = MapTextColor,
        [nameof(ITextStyle.Font)] = MapFont,
        [nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
        [nameof(ISearchBar.IsReadOnly)] = MapIsReadOnly,
        [nameof(ISearchBar.Keyboard)] = MapKeyboard,
        [nameof(ISearchBar.MaxLength)] = MapMaxLength,
        [nameof(ISearchBar.IsTextPredictionEnabled)] = MapIsTextPredictionEnabled,
        [nameof(ISearchBar.IsSpellCheckEnabled)] = MapIsSpellCheckEnabled,
        [nameof(ITextAlignment.HorizontalTextAlignment)] = MapHorizontalTextAlignment,
        [nameof(ITextAlignment.VerticalTextAlignment)] = MapVerticalTextAlignment,
        [nameof(ISearchBar.ReturnType)] = MapReturnType,
        [nameof(ISearchBar.CancelButtonColor)] = MapCancelButtonColor,
        [nameof(ISearchBar.SearchIconColor)] = MapSearchIconColor,
        [nameof(IView.IsEnabled)] = MapIsEnabled
    };

    public static CommandMapper<ISearchBar, BrowserSearchBarHandler> CommandMapper = new(BrowserViewMapper.CommonViewCommandMapper)
    {
        [nameof(IView.Focus)] = MapFocus,
        [nameof(IView.Unfocus)] = MapUnfocus
    };

    private DomElement? _inputElement;
    private DomElement? _searchButtonElement;
    private DomElement? _clearButtonElement;

    private Action? _inputCallback;
    private Action? _enterCallback;
    private Action? _searchClickCallback;
    private Action? _clearClickCallback;

    private int _inputSubscriptionId;
    private int _enterSubscriptionId;
    private int _searchClickSubscriptionId;
    private int _clearClickSubscriptionId;

    public BrowserSearchBarHandler()
        : base(Mapper, CommandMapper)
    {
    }

    protected override DomElement CreatePlatformView() => DomElement.Create("div");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Style("display", "flex");
        platformView.Style("flexDirection", "row");
        platformView.Style("alignItems", "center");
        platformView.Style("gap", "8px");
        platformView.Style("boxSizing", "border-box");

        _inputElement = DomElement.Create("input");
        _inputElement.Attr("type", "search");
        _inputElement.Style("flex", "1 1 auto");
        _inputElement.Style("minWidth", "0");
        _inputElement.Style("boxSizing", "border-box");

        _clearButtonElement = DomElement.Create("button");
        _clearButtonElement.Attr("type", "button");
        _clearButtonElement.Text("Clear");
        _clearButtonElement.Style("padding", "4px 8px");
        _clearButtonElement.Style("whiteSpace", "nowrap");

        _searchButtonElement = DomElement.Create("button");
        _searchButtonElement.Attr("type", "button");
        _searchButtonElement.Text("Search");
        _searchButtonElement.Style("padding", "4px 10px");
        _searchButtonElement.Style("whiteSpace", "nowrap");

        platformView.Append(_inputElement);
        platformView.Append(_clearButtonElement);
        platformView.Append(_searchButtonElement);

        _inputCallback = OnInput;
        _enterCallback = OnSearchPressed;
        _searchClickCallback = OnSearchPressed;
        _clearClickCallback = OnClearPressed;

        _inputSubscriptionId = _inputElement.On("input", _inputCallback);
        _enterSubscriptionId = _inputElement.OnEnter(_enterCallback);
        _searchClickSubscriptionId = _searchButtonElement.On("click", _searchClickCallback);
        _clearClickSubscriptionId = _clearButtonElement.On("click", _clearClickCallback);

        UpdateClearButtonState(VirtualView.Text);
        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(DomElement platformView)
    {
        if (_inputSubscriptionId != 0)
        {
            DomElement.Off(_inputSubscriptionId);
            _inputSubscriptionId = 0;
        }

        if (_enterSubscriptionId != 0)
        {
            DomElement.Off(_enterSubscriptionId);
            _enterSubscriptionId = 0;
        }

        if (_searchClickSubscriptionId != 0)
        {
            DomElement.Off(_searchClickSubscriptionId);
            _searchClickSubscriptionId = 0;
        }

        if (_clearClickSubscriptionId != 0)
        {
            DomElement.Off(_clearClickSubscriptionId);
            _clearClickSubscriptionId = 0;
        }

        _inputCallback = null;
        _enterCallback = null;
        _searchClickCallback = null;
        _clearClickCallback = null;

        _inputElement = null;
        _searchButtonElement = null;
        _clearButtonElement = null;

        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        var measurer = MauiContext?.Services.GetService<IBrowserTextMeasurer>();
        if (measurer is null)
        {
            return base.GetDesiredSize(widthConstraint, heightConstraint);
        }

        var measured = measurer.MeasureText(
            string.IsNullOrWhiteSpace(VirtualView.Text) ? VirtualView.Placeholder : VirtualView.Text,
            VirtualView.Font,
            widthConstraint,
            heightConstraint,
            new Thickness(12, 8),
            minWidth: 240,
            minHeight: 40);

        return measured;
    }

    private void OnInput()
    {
        var value = _inputElement?.GetInputValue() ?? string.Empty;
        VirtualView.UpdateText(value);
        UpdateClearButtonState(value);
    }

    private void OnSearchPressed()
    {
        if (!VirtualView.IsEnabled)
        {
            return;
        }

        VirtualView.SearchButtonPressed();
    }

    private void OnClearPressed()
    {
        if (!VirtualView.IsEnabled)
        {
            return;
        }

        if (string.IsNullOrEmpty(VirtualView.Text))
        {
            return;
        }

        VirtualView.UpdateText(string.Empty);
        _inputElement?.SetInputValue(string.Empty);
        UpdateClearButtonState(string.Empty);
    }

    private void UpdateClearButtonState(string? currentText)
    {
        if (_clearButtonElement is null)
        {
            return;
        }

        var hasText = !string.IsNullOrEmpty(currentText);
        _clearButtonElement.Style("visibility", hasText ? "visible" : "hidden");
        _clearButtonElement.Style("pointerEvents", hasText ? null : "none");
    }

    private void UpdateChildAutomationIds(ISearchBar searchBar)
    {
        var automationId = string.IsNullOrWhiteSpace(searchBar.AutomationId) ? null : searchBar.AutomationId;
        var inputAutomationId = automationId is null ? null : $"{automationId}-input";
        var clearAutomationId = automationId is null ? null : $"{automationId}-clear-button";
        var searchAutomationId = automationId is null ? null : $"{automationId}-search-button";

        _inputElement?.Attr("data-automation-id", inputAutomationId);
        _clearButtonElement?.Attr("data-automation-id", clearAutomationId);
        _searchButtonElement?.Attr("data-automation-id", searchAutomationId);
    }

    public static void MapAutomationId(BrowserSearchBarHandler handler, ISearchBar searchBar)
    {
        BrowserViewMapper.MapAutomationId(handler, searchBar);
        handler.UpdateChildAutomationIds(searchBar);
    }

    public static void MapText(BrowserSearchBarHandler handler, ISearchBar searchBar)
    {
        if (handler._inputElement is null)
        {
            return;
        }

        handler._inputElement.SetInputValue(searchBar.Text);
        handler.UpdateClearButtonState(searchBar.Text);
    }

    public static void MapPlaceholder(BrowserSearchBarHandler handler, ISearchBar searchBar)
    {
        if (handler._inputElement is null)
        {
            return;
        }

        BrowserTextInputMappings.MapPlaceholder(searchBar, handler._inputElement);
    }

    public static void MapPlaceholderColor(BrowserSearchBarHandler handler, ISearchBar searchBar)
    {
        if (handler._inputElement is null)
        {
            return;
        }

        BrowserTextInputMappings.MapPlaceholderColor(searchBar, handler._inputElement);
    }

    public static void MapTextColor(BrowserSearchBarHandler handler, ISearchBar searchBar)
    {
        if (handler._inputElement is null)
        {
            return;
        }

        BrowserViewMapper.MapTextColor(searchBar, handler._inputElement);
    }

    public static void MapFont(BrowserSearchBarHandler handler, ISearchBar searchBar)
    {
        if (handler._inputElement is null)
        {
            return;
        }

        BrowserViewMapper.MapFont(searchBar, handler._inputElement);
    }

    public static void MapCharacterSpacing(BrowserSearchBarHandler handler, ISearchBar searchBar)
    {
        if (handler._inputElement is null)
        {
            return;
        }

        BrowserViewMapper.MapCharacterSpacing(searchBar, handler._inputElement);
    }

    public static void MapIsReadOnly(BrowserSearchBarHandler handler, ISearchBar searchBar)
    {
        if (handler._inputElement is null)
        {
            return;
        }

        BrowserTextInputMappings.MapReadOnly(searchBar, handler._inputElement);
    }

    public static void MapKeyboard(BrowserSearchBarHandler handler, ISearchBar searchBar)
    {
        if (handler._inputElement is null)
        {
            return;
        }

        BrowserTextInputMappings.MapKeyboard(searchBar, handler._inputElement, multiline: false, forcePassword: false, defaultInputType: "search");
    }

    public static void MapMaxLength(BrowserSearchBarHandler handler, ISearchBar searchBar)
    {
        if (handler._inputElement is null)
        {
            return;
        }

        BrowserTextInputMappings.MapMaxLength(searchBar, handler._inputElement);
    }

    public static void MapIsTextPredictionEnabled(BrowserSearchBarHandler handler, ISearchBar searchBar)
    {
        if (handler._inputElement is null)
        {
            return;
        }

        BrowserTextInputMappings.MapPrediction(searchBar, handler._inputElement);
    }

    public static void MapIsSpellCheckEnabled(BrowserSearchBarHandler handler, ISearchBar searchBar)
    {
        if (handler._inputElement is null)
        {
            return;
        }

        BrowserTextInputMappings.MapSpellCheck(searchBar, handler._inputElement);
    }

    public static void MapHorizontalTextAlignment(BrowserSearchBarHandler handler, ISearchBar searchBar)
    {
        if (handler._inputElement is null)
        {
            return;
        }

        BrowserTextInputMappings.MapHorizontalTextAlignment(searchBar, handler._inputElement);
    }

    public static void MapVerticalTextAlignment(BrowserSearchBarHandler handler, ISearchBar searchBar)
    {
        if (handler._inputElement is null)
        {
            return;
        }

        BrowserTextInputMappings.MapVerticalTextAlignment(searchBar, handler._inputElement);
    }

    public static void MapReturnType(BrowserSearchBarHandler handler, ISearchBar searchBar)
    {
        if (handler._inputElement is null)
        {
            return;
        }

        BrowserTextInputMappings.MapReturnType(searchBar.ReturnType, handler._inputElement);
    }

    public static void MapCancelButtonColor(BrowserSearchBarHandler handler, ISearchBar searchBar)
    {
        if (handler._clearButtonElement is null)
        {
            return;
        }

        handler._clearButtonElement.Style("color", CssUtil.ToCssColor(searchBar.CancelButtonColor));
    }

    public static void MapSearchIconColor(BrowserSearchBarHandler handler, ISearchBar searchBar)
    {
        if (handler._searchButtonElement is null)
        {
            return;
        }

        handler._searchButtonElement.Style("color", CssUtil.ToCssColor(searchBar.SearchIconColor));
    }

    public static void MapIsEnabled(BrowserSearchBarHandler handler, ISearchBar searchBar)
    {
        BrowserViewMapper.MapEnabled(searchBar, handler.PlatformView);

        var disabled = !searchBar.IsEnabled;
        handler._inputElement?.Attr("disabled", disabled ? "disabled" : null);
        handler._searchButtonElement?.Attr("disabled", disabled ? "disabled" : null);
        handler._clearButtonElement?.Attr("disabled", disabled ? "disabled" : null);
    }

    public static void MapFocus(BrowserSearchBarHandler handler, ISearchBar searchBar, object? args)
    {
        var focusRequest = args as FocusRequest;

        if (!searchBar.IsEnabled || handler._inputElement is null)
        {
            focusRequest?.TrySetResult(false);
            return;
        }

        try
        {
            var focused = handler._inputElement.Focus();
            focusRequest?.TrySetResult(focused);
        }
        catch
        {
            focusRequest?.TrySetResult(false);
        }
    }

    public static void MapUnfocus(BrowserSearchBarHandler handler, ISearchBar searchBar, object? args)
    {
        handler._inputElement?.Blur();
    }
}
