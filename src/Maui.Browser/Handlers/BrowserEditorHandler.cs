using Maui.Browser.Dom;
using Maui.Browser.Measurement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Maui.Browser.Handlers;

public sealed class BrowserEditorHandler : BrowserViewHandler<IEditor>
{
    public static IPropertyMapper<IEditor, BrowserEditorHandler> Mapper = new PropertyMapper<IEditor, BrowserEditorHandler>(BrowserViewMapper.CommonViewMapper)
    {
        [nameof(IEditor.Text)] = MapText,
        [nameof(IEditor.Placeholder)] = MapPlaceholder,
        [nameof(IEditor.PlaceholderColor)] = MapPlaceholderColor,
        [nameof(ITextStyle.TextColor)] = MapTextColor,
        [nameof(ITextStyle.Font)] = MapFont,
        [nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
        [nameof(IEditor.IsReadOnly)] = MapIsReadOnly,
        [nameof(IEditor.Keyboard)] = MapKeyboard,
        [nameof(IEditor.MaxLength)] = MapMaxLength,
        [nameof(IEditor.IsTextPredictionEnabled)] = MapIsTextPredictionEnabled,
        [nameof(IEditor.IsSpellCheckEnabled)] = MapIsSpellCheckEnabled,
        [nameof(ITextAlignment.HorizontalTextAlignment)] = MapHorizontalTextAlignment,
        [nameof(ITextAlignment.VerticalTextAlignment)] = MapVerticalTextAlignment
    };

    private Action? _inputCallback;
    private Action? _enterCallback;
    private int _inputSubscriptionId;
    private int _enterSubscriptionId;

    public BrowserEditorHandler()
        : base(Mapper)
    {
    }

    protected override DomElement CreatePlatformView() => DomElement.Create("textarea");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Style("boxSizing", "border-box");
        platformView.Style("resize", "none");

        _inputCallback = OnInput;
        _enterCallback = OnEnter;
        _inputSubscriptionId = platformView.On("input", _inputCallback);
        _enterSubscriptionId = platformView.OnEnter(_enterCallback);

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

        _inputCallback = null;
        _enterCallback = null;
        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        var measurer = MauiContext?.Services.GetService<IBrowserTextMeasurer>();
        if (measurer is null)
        {
            return base.GetDesiredSize(widthConstraint, heightConstraint);
        }

        return measurer.MeasureText(
            string.IsNullOrWhiteSpace(VirtualView.Text) ? VirtualView.Placeholder : VirtualView.Text,
            VirtualView.Font,
            widthConstraint,
            heightConstraint,
            new Thickness(10, 10),
            minWidth: 180,
            minHeight: 96);
    }

    private void OnInput()
    {
        VirtualView.UpdateText(PlatformView.GetInputValue());
    }

    private void OnEnter()
    {
        if (!VirtualView.IsEnabled)
        {
            return;
        }

        VirtualView.Completed();
    }

    public static void MapText(BrowserEditorHandler handler, IEditor editor)
    {
        handler.PlatformView.SetInputValue(editor.Text);
    }

    public static void MapPlaceholder(BrowserEditorHandler handler, IEditor editor)
    {
        BrowserTextInputMappings.MapPlaceholder(editor, handler.PlatformView);
    }

    public static void MapPlaceholderColor(BrowserEditorHandler handler, IEditor editor)
    {
        BrowserTextInputMappings.MapPlaceholderColor(editor, handler.PlatformView);
    }

    public static void MapTextColor(BrowserEditorHandler handler, IEditor editor)
    {
        BrowserViewMapper.MapTextColor(editor, handler.PlatformView);
    }

    public static void MapFont(BrowserEditorHandler handler, IEditor editor)
    {
        BrowserViewMapper.MapFont(editor, handler.PlatformView);
    }

    public static void MapCharacterSpacing(BrowserEditorHandler handler, IEditor editor)
    {
        BrowserViewMapper.MapCharacterSpacing(editor, handler.PlatformView);
    }

    public static void MapIsReadOnly(BrowserEditorHandler handler, IEditor editor)
    {
        BrowserTextInputMappings.MapReadOnly(editor, handler.PlatformView);
    }

    public static void MapKeyboard(BrowserEditorHandler handler, IEditor editor)
    {
        BrowserTextInputMappings.MapKeyboard(editor, handler.PlatformView, multiline: true, forcePassword: false);
    }

    public static void MapMaxLength(BrowserEditorHandler handler, IEditor editor)
    {
        BrowserTextInputMappings.MapMaxLength(editor, handler.PlatformView);
    }

    public static void MapIsTextPredictionEnabled(BrowserEditorHandler handler, IEditor editor)
    {
        BrowserTextInputMappings.MapPrediction(editor, handler.PlatformView);
    }

    public static void MapIsSpellCheckEnabled(BrowserEditorHandler handler, IEditor editor)
    {
        BrowserTextInputMappings.MapSpellCheck(editor, handler.PlatformView);
    }

    public static void MapHorizontalTextAlignment(BrowserEditorHandler handler, IEditor editor)
    {
        BrowserTextInputMappings.MapHorizontalTextAlignment(editor, handler.PlatformView);
    }

    public static void MapVerticalTextAlignment(BrowserEditorHandler handler, IEditor editor)
    {
        BrowserTextInputMappings.MapVerticalTextAlignment(editor, handler.PlatformView);
    }
}
