using Maui.Browser.Dom;
using Maui.Browser.Utils;
using Microsoft.Maui;

namespace Maui.Browser.Handlers;

internal static class BrowserTextInputMappings
{
    public static void MapPlaceholder(IPlaceholder view, DomElement platformView)
    {
        if (string.IsNullOrWhiteSpace(view.Placeholder))
        {
            platformView.Attr("placeholder", null);
            return;
        }

        platformView.Attr("placeholder", view.Placeholder);
    }

    public static void MapPlaceholderColor(IPlaceholder view, DomElement platformView)
    {
        platformView.SetPlaceholderColor(CssUtil.ToCssColor(view.PlaceholderColor));
    }

    public static void MapReadOnly(ITextInput view, DomElement platformView)
    {
        platformView.Attr("readonly", view.IsReadOnly ? "readonly" : null);
    }

    public static void MapPrediction(ITextInput view, DomElement platformView)
    {
        var enabled = view.IsTextPredictionEnabled;
        platformView.Attr("autocorrect", enabled ? "on" : "off");
        platformView.Attr("autocomplete", enabled ? "on" : "off");
    }

    public static void MapSpellCheck(ITextInput view, DomElement platformView)
    {
        platformView.Attr("spellcheck", view.IsSpellCheckEnabled ? "true" : "false");
    }

    public static void MapMaxLength(ITextInput view, DomElement platformView)
    {
        platformView.Attr("maxlength", view.MaxLength > 0 ? view.MaxLength.ToString(System.Globalization.CultureInfo.InvariantCulture) : null);
    }

    public static void MapKeyboard(ITextInput view, DomElement platformView, bool multiline, bool forcePassword, string defaultInputType = "text")
    {
        var keyboard = view.Keyboard;

        var inputMode = "text";
        var inputType = defaultInputType;

        if (forcePassword || ReferenceEquals(keyboard, Keyboard.Password))
        {
            inputType = "password";
            inputMode = "text";
        }
        else if (ReferenceEquals(keyboard, Keyboard.Numeric))
        {
            inputType = multiline ? "text" : "number";
            inputMode = "decimal";
        }
        else if (ReferenceEquals(keyboard, Keyboard.Email))
        {
            inputType = "email";
            inputMode = "email";
        }
        else if (ReferenceEquals(keyboard, Keyboard.Telephone))
        {
            inputType = "tel";
            inputMode = "tel";
        }
        else if (ReferenceEquals(keyboard, Keyboard.Url))
        {
            inputType = "url";
            inputMode = "url";
        }
        else if (ReferenceEquals(keyboard, Keyboard.Date))
        {
            inputType = multiline ? "text" : "date";
            inputMode = "numeric";
        }
        else if (ReferenceEquals(keyboard, Keyboard.Time))
        {
            inputType = multiline ? "text" : "time";
            inputMode = "numeric";
        }

        platformView.Attr("inputmode", inputMode);
        if (!multiline)
        {
            platformView.Attr("type", inputType);
        }
    }

    public static void MapReturnType(ReturnType returnType, DomElement platformView)
    {
        var hint = returnType switch
        {
            ReturnType.Done => "done",
            ReturnType.Go => "go",
            ReturnType.Next => "next",
            ReturnType.Search => "search",
            ReturnType.Send => "send",
            _ => "enter"
        };

        platformView.Attr("enterkeyhint", hint);
    }

    public static void MapHorizontalTextAlignment(ITextAlignment view, DomElement platformView)
    {
        var horizontal = view.HorizontalTextAlignment switch
        {
            TextAlignment.Center => "center",
            TextAlignment.End => "right",
            TextAlignment.Justify => "justify",
            _ => "left"
        };

        platformView.Style("textAlign", horizontal);
    }

    public static void MapVerticalTextAlignment(ITextAlignment view, DomElement platformView)
    {
        var vertical = view.VerticalTextAlignment switch
        {
            TextAlignment.Center => "middle",
            TextAlignment.End => "bottom",
            _ => "top"
        };

        platformView.Style("verticalAlign", vertical);
    }
}
