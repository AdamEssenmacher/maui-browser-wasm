using Mirage.Core.Diagnostics;
using Mirage.Core.Semantics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Handlers;
using System.Globalization;
using System.Net;
using System.Text;

namespace Mirage.Maui.Handlers;

internal sealed class MirageLabelHandler : MirageViewHandler<ILabel>
{
    private const string DefaultLineHeight = "1.3";
    private const double DefaultFontSize = 14;
    private const string DefaultSystemLabelColor = MirageCommonViewProjection.DefaultSystemTextColor;
    private const string DefaultSystemFontFamily = MirageCommonViewProjection.DefaultSystemFontFamily;
    private static readonly HashSet<string> HtmlSanitizerWarnings = [];
    private static readonly HashSet<string> AllowedHtmlTags =
    [
        "b",
        "strong",
        "i",
        "em",
        "u",
        "s",
        "strike",
        "br",
        "p",
        "div",
        "span",
        "small",
        "big",
        "sub",
        "sup",
        "code",
        "h1",
        "h2",
        "h3",
        "h4",
        "h5",
        "h6"
    ];
    private static readonly HashSet<string> VoidHtmlTags =
    [
        "br"
    ];
    private string? _lineClampValue;

    private bool UsesLineClamp => !string.IsNullOrEmpty(_lineClampValue);

    internal static readonly string[] ParityPropertyKeys =
    {
        nameof(ILabel.Text),
        nameof(ITextStyle.TextColor),
        nameof(ITextStyle.Font),
        nameof(ITextStyle.CharacterSpacing),
        nameof(ITextAlignment.HorizontalTextAlignment),
        nameof(ITextAlignment.VerticalTextAlignment),
        nameof(ILabel.TextDecorations),
        nameof(ILabel.LineHeight),
        nameof(ILabel.Padding)
    };

    internal static readonly string[] ParityControlsRemapPropertyKeys =
    {
        nameof(Label.TextType),
        nameof(Label.TextTransform),
        nameof(Label.Text),
        nameof(Label.FormattedText),
        nameof(Label.LineBreakMode),
        nameof(Label.MaxLines)
    };

    internal static readonly string[] ParityDivergenceControlsRemapPropertyKeys =
    {
        nameof(Label.TextType),
        nameof(Label.FormattedText)
    };

    internal static readonly string[] ParityCommandKeys = [];

    public static IPropertyMapper<ILabel, MirageLabelHandler> Mapper =
        new PropertyMapper<ILabel, MirageLabelHandler>(MirageViewMapper.CommonViewMapper)
        {
            [nameof(ILabel.Text)] = MapText,
            [nameof(ITextStyle.TextColor)] = MapTextColor,
            ["TextColor"] = MapTextColor,
            [nameof(ILabel.Font)] = MapFont,
            [nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
            ["FontFamily"] = MapFont,
            ["FontSize"] = MapFont,
            ["FontAttributes"] = MapFont,
            ["FontAutoScalingEnabled"] = MapFont,
            [nameof(ITextAlignment.HorizontalTextAlignment)] = MapHorizontalTextAlignment,
            [nameof(ITextAlignment.VerticalTextAlignment)] = MapVerticalTextAlignment,
            [nameof(ILabel.TextDecorations)] = MapTextDecorations,
            [nameof(ILabel.LineHeight)] = MapLineHeight,
            [nameof(ILabel.Padding)] = MapPadding,
            [nameof(IView.Visibility)] = MapVisibility,
            ["IsVisible"] = MapVisibility,
            [nameof(Label.LineBreakMode)] = MapLineBreakMode,
            [nameof(Label.MaxLines)] = MapMaxLines,
            [nameof(Label.TextType)] = MapTextType,
            [nameof(Label.TextTransform)] = MapTextTransform,
            [nameof(Label.FormattedText)] = MapFormattedText
        };

    public static CommandMapper<ILabel, MirageLabelHandler> CommandMapper = new(MirageViewMapper.CommonViewCommandMapper);

    public MirageLabelHandler()
        : base(ComponentKind.Label, Mapper, CommandMapper)
    {
        DefaultDisplayStyle = "flex";
    }

    protected override void ConnectHandler(MiragePlatformView platformView)
    {
        var changed = SetStyleIfChanged("display", "flex");
        changed |= SetStyleIfChanged("flexDirection", "column");
        changed |= SetStyleIfChanged("justifyContent", "flex-start");
        changed |= SetStyleIfChanged("lineHeight", DefaultLineHeight);
        changed |= MirageCommonViewProjection.MapTextColor(this, VirtualView, DefaultSystemLabelColor);
        changed |= MirageCommonViewProjection.MapFont(this, VirtualView, DefaultFontSize, DefaultSystemFontFamily);
        changed |= MirageCommonViewProjection.MapCharacterSpacing(this, VirtualView);
        changed |= MirageCommonViewProjection.MapPadding(this, VirtualView);
        changed |= UpdateTextDecorations(VirtualView);
        changed |= UpdateLineHeight(VirtualView);
        changed |= ApplyLineBreakModeProjection(VirtualView);
        changed |= UpdateHorizontalTextAlignment(VirtualView);
        changed |= UpdateVerticalTextAlignment(VirtualView);
        changed |= UpdateVisibilityStyles(VirtualView.Visibility);
        base.ConnectHandler(platformView);
        if (changed)
        {
            RequestRender();
        }
    }

    public static void MapText(MirageLabelHandler handler, ILabel label)
    {
        var projection = ResolveProjectedTextProjection(label);
        var changed = handler.SetPropertyIfChanged("Text", projection.PlainText);
        changed |= handler.SetPropertyIfChanged("TextHtml", projection.HtmlText);
        if (!changed)
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapCharacterSpacing(MirageLabelHandler handler, ILabel label)
    {
        if (!MirageCommonViewProjection.MapCharacterSpacing(handler, label))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapTextColor(MirageLabelHandler handler, ILabel label)
    {
        if (MirageCommonViewProjection.MapTextColor(handler, label, DefaultSystemLabelColor))
        {
            handler.RequestRender();
        }
    }

    public static void MapFont(MirageLabelHandler handler, ILabel label)
    {
        if (MirageCommonViewProjection.MapFont(handler, label, DefaultFontSize, DefaultSystemFontFamily))
        {
            handler.RequestRender();
        }
    }

    public static void MapHorizontalTextAlignment(MirageLabelHandler handler, ILabel label)
    {
        if (!handler.UpdateHorizontalTextAlignment(label))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapVerticalTextAlignment(MirageLabelHandler handler, ILabel label)
    {
        if (!handler.UpdateVerticalTextAlignment(label))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapTextDecorations(MirageLabelHandler handler, ILabel label)
    {
        if (!handler.UpdateTextDecorations(label))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapLineHeight(MirageLabelHandler handler, ILabel label)
    {
        if (!handler.UpdateLineHeight(label))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapPadding(MirageLabelHandler handler, ILabel label)
    {
        if (!MirageCommonViewProjection.MapPadding(handler, label))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapVisibility(MirageLabelHandler handler, ILabel label)
    {
        if (!handler.UpdateVisibilityStyles(label.Visibility))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapLineBreakMode(MirageLabelHandler handler, ILabel label)
    {
        if (!handler.ApplyLineBreakModeProjection(label))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapMaxLines(MirageLabelHandler handler, ILabel label)
    {
        if (!handler.ApplyLineBreakModeProjection(label))
        {
            return;
        }

        handler.RequestRender();
    }

    public static void MapTextType(MirageLabelHandler handler, ILabel label)
    {
        if (label is not Label controlsLabel)
        {
            MapText(handler, label);
            return;
        }

        MapTextOrFormattedText(handler, controlsLabel);
    }

    public static void MapTextTransform(MirageLabelHandler handler, ILabel label)
    {
        if (label is not Label controlsLabel)
        {
            MapText(handler, label);
            return;
        }

        MapTextOrFormattedText(handler, controlsLabel);
    }

    public static void MapFormattedText(MirageLabelHandler handler, ILabel label)
    {
        MapText(handler, label);
    }

    private static void MapTextOrFormattedText(MirageLabelHandler handler, Label label)
    {
        if (HasFormattedTextSpans(label))
        {
            handler.UpdateValue(nameof(Label.FormattedText));
            return;
        }

        handler.UpdateValue(nameof(Label.Text));
    }

    private bool UpdateHorizontalTextAlignment(ILabel label)
    {
        var textAlign = label.HorizontalTextAlignment switch
        {
            TextAlignment.Center => "center",
            TextAlignment.End => "right",
            TextAlignment.Justify => "justify",
            _ => "left"
        };

        return SetStyleIfChanged("textAlign", textAlign);
    }

    private bool UpdateVerticalTextAlignment(ILabel label)
    {
        var justifyContent = label.VerticalTextAlignment switch
        {
            TextAlignment.Center => "center",
            TextAlignment.End => "flex-end",
            _ => "flex-start"
        };

        return SetStyleIfChanged("justifyContent", justifyContent);
    }

    private bool UpdateTextDecorations(ILabel label)
    {
        var decorations = label.TextDecorations;
        if (decorations == TextDecorations.None)
        {
            return SetStyleIfChanged("textDecoration", null);
        }

        var values = new List<string>(2);
        if (decorations.HasFlag(TextDecorations.Underline))
        {
            values.Add("underline");
        }

        if (decorations.HasFlag(TextDecorations.Strikethrough))
        {
            values.Add("line-through");
        }

        return SetStyleIfChanged("textDecoration", values.Count > 0 ? string.Join(' ', values) : null);
    }

    private bool UpdateLineHeight(ILabel label)
    {
        if (double.IsNaN(label.LineHeight) || label.LineHeight <= 0)
        {
            return SetStyleIfChanged("lineHeight", DefaultLineHeight);
        }

        var lineHeight = label.LineHeight.ToString("0.###", CultureInfo.InvariantCulture);
        return SetStyleIfChanged("lineHeight", lineHeight);
    }

    private bool ApplyLineBreakModeProjection(ILabel label)
    {
        var mode = ResolveLineBreakMode(label);
        var maxLines = ResolveMaxLines(label);
        return ApplyLineBreakMode(mode, maxLines);
    }

    private bool ApplyLineBreakMode(LineBreakMode mode, int maxLines)
    {
        var changed = false;
        if (maxLines > 1)
        {
            changed |= SetStyleIfChanged("webkitBoxOrient", "vertical");
            _lineClampValue = maxLines.ToString(CultureInfo.InvariantCulture);
            changed |= SetStyleIfChanged("webkitLineClamp", _lineClampValue);
            changed |= SetStyleIfChanged("overflow", "hidden");
            changed |= SetStyleIfChanged("textOverflow", "ellipsis");
            changed |= SetStyleIfChanged("whiteSpace", "normal");
            changed |= SetStyleIfChanged("wordBreak", ResolveTextWordBreak(mode));
            changed |= SetStyleIfChanged("overflowWrap", ResolveTextOverflowWrap(mode));
            changed |= UpdateVisibilityStyles(VirtualView.Visibility);
            return changed;
        }

        _lineClampValue = null;
        changed |= SetStyleIfChanged("webkitBoxOrient", null);
        changed |= SetStyleIfChanged("webkitLineClamp", null);
        changed |= SetStyleIfChanged("whiteSpace", ResolveTextWhiteSpace(mode));
        changed |= SetStyleIfChanged("overflow", ResolveTextOverflow(mode));
        changed |= SetStyleIfChanged("textOverflow", ResolveTextOverflowStyle(mode));
        changed |= SetStyleIfChanged("wordBreak", ResolveTextWordBreak(mode));
        changed |= SetStyleIfChanged("overflowWrap", ResolveTextOverflowWrap(mode));

        if (maxLines == 1)
        {
            changed |= SetStyleIfChanged("whiteSpace", "nowrap");
            changed |= SetStyleIfChanged("overflow", "hidden");
            changed |= SetStyleIfChanged("textOverflow", mode is LineBreakMode.NoWrap ? "clip" : "ellipsis");
            changed |= SetStyleIfChanged("wordBreak", "normal");
            changed |= SetStyleIfChanged("overflowWrap", "normal");
        }

        changed |= UpdateVisibilityStyles(VirtualView.Visibility);
        return changed;
    }

    private bool UpdateVisibilityStyles(Visibility visibility)
    {
        var visibleDisplay = UsesLineClamp ? "-webkit-box" : "flex";
        string? display;
        string? visibilityStyle;

        switch (visibility)
        {
            case Visibility.Visible:
                display = visibleDisplay;
                visibilityStyle = "visible";
                break;
            case Visibility.Hidden:
                display = visibleDisplay;
                visibilityStyle = "hidden";
                break;
            default:
                display = "none";
                visibilityStyle = "hidden";
                break;
        }

        var changed = SetStyleIfChanged("display", display);
        changed |= SetStyleIfChanged("visibility", visibilityStyle);
        return changed;
    }

    private static LineBreakMode ResolveLineBreakMode(ILabel label)
    {
        return label is Label controlsLabel
            ? controlsLabel.LineBreakMode
            : LineBreakMode.WordWrap;
    }

    private static int ResolveMaxLines(ILabel label)
    {
        return label is Label controlsLabel
            ? controlsLabel.MaxLines
            : -1;
    }

    private static string ResolveTextWhiteSpace(LineBreakMode mode)
    {
        return mode switch
        {
            LineBreakMode.WordWrap or LineBreakMode.CharacterWrap => "normal",
            _ => "nowrap"
        };
    }

    private static string ResolveTextOverflow(LineBreakMode mode)
    {
        return mode switch
        {
            LineBreakMode.WordWrap or LineBreakMode.CharacterWrap => "visible",
            _ => "hidden"
        };
    }

    private static string ResolveTextOverflowStyle(LineBreakMode mode)
    {
        return mode switch
        {
            LineBreakMode.HeadTruncation or LineBreakMode.MiddleTruncation or LineBreakMode.TailTruncation => "ellipsis",
            _ => "clip"
        };
    }

    private static string ResolveTextWordBreak(LineBreakMode mode)
    {
        return mode == LineBreakMode.CharacterWrap ? "break-all" : "normal";
    }

    private static string ResolveTextOverflowWrap(LineBreakMode mode)
    {
        return mode switch
        {
            LineBreakMode.CharacterWrap => "anywhere",
            LineBreakMode.WordWrap => "break-word",
            _ => "normal"
        };
    }

    private readonly record struct LabelTextProjection(string? PlainText, string? HtmlText);

    private static LabelTextProjection ResolveProjectedTextProjection(ILabel label)
    {
        if (label is not Label controlsLabel)
        {
            return new(label.Text, null);
        }

        if (controlsLabel.TextType == TextType.Html)
        {
            var projectedHtml = ResolveHtmlTextProjection(controlsLabel);
            return new(null, projectedHtml);
        }

        if (HasFormattedTextSpans(controlsLabel))
        {
            return new(null, BuildFormattedTextHtml(controlsLabel));
        }

        return new(ResolvePlainTextProjection(controlsLabel), null);
    }

    private static bool HasFormattedTextSpans(Label label) =>
        label.FormattedText is FormattedString { Spans.Count: > 0 };

    private static string ResolvePlainTextProjection(Label label) =>
        ApplyTextTransform(label.Text, label.TextTransform);

    private static string ResolveHtmlTextProjection(Label label)
    {
        var transformedText = ApplyTextTransform(label.Text, label.TextTransform);
        var sanitizedHtml = SanitizeHtml(transformedText, out var strippedMarkup);
        if (strippedMarkup)
        {
            ReportHtmlSanitized();
        }

        return sanitizedHtml;
    }

    private static string BuildFormattedTextHtml(Label label)
    {
        if (label.FormattedText is not FormattedString { Spans.Count: > 0 } formattedText)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var span in formattedText.Spans)
        {
            var effectiveTransform = span.TextTransform != TextTransform.Default
                ? span.TextTransform
                : label.TextTransform;
            var transformedText = ApplyTextTransform(span.Text, effectiveTransform);
            if (string.IsNullOrEmpty(transformedText))
            {
                continue;
            }

            var encodedTextBuilder = new StringBuilder();
            AppendEncodedTextPreservingLineBreaks(encodedTextBuilder, transformedText);

            var style = BuildSpanStyle(span);
            if (style is null)
            {
                builder.Append("<span>")
                    .Append(encodedTextBuilder)
                    .Append("</span>");
                continue;
            }

            builder.Append("<span style=\"")
                .Append(WebUtility.HtmlEncode(style))
                .Append("\">")
                .Append(encodedTextBuilder)
                .Append("</span>");
        }

        return builder.ToString();
    }

    private static string? BuildSpanStyle(Span span)
    {
        var declarations = new List<string>(8);

        if (span.IsSet(Span.TextColorProperty)
            && MirageCommonViewProjection.ToCssColor(span.TextColor) is { } textColorCss)
        {
            declarations.Add($"color:{textColorCss}");
        }

        if (span.IsSet(Span.BackgroundColorProperty)
            && MirageCommonViewProjection.ToCssColor(span.BackgroundColor) is { } backgroundColorCss)
        {
            declarations.Add($"background-color:{backgroundColorCss}");
        }

        if (span.IsSet(Span.FontFamilyProperty))
        {
            var fontFamilyCss = ResolveSpanFontFamily(span.FontFamily);
            if (!string.IsNullOrWhiteSpace(fontFamilyCss))
            {
                declarations.Add($"font-family:{fontFamilyCss}");
            }
        }

        if (span.IsSet(Span.FontSizeProperty) && double.IsFinite(span.FontSize) && span.FontSize > 0)
        {
            declarations.Add($"font-size:{MirageCommonViewProjection.ToPx(span.FontSize)}");
        }

        if (span.IsSet(Span.FontAttributesProperty))
        {
            declarations.Add($"font-weight:{ResolveFontWeightCss(span.FontAttributes)}");
            declarations.Add($"font-style:{ResolveFontStyleCss(span.FontAttributes)}");
        }

        if (span.IsSet(Span.TextDecorationsProperty))
        {
            declarations.Add($"text-decoration:{ResolveTextDecorationsCss(span.TextDecorations)}");
        }

        if (span.IsSet(Span.CharacterSpacingProperty) && double.IsFinite(span.CharacterSpacing))
        {
            declarations.Add($"letter-spacing:{MirageCommonViewProjection.ToPxNoClamp(span.CharacterSpacing)}");
        }

        if (span.IsSet(Span.LineHeightProperty) && double.IsFinite(span.LineHeight) && span.LineHeight > 0)
        {
            declarations.Add($"line-height:{span.LineHeight.ToString("0.###", CultureInfo.InvariantCulture)}");
        }

        return declarations.Count == 0 ? null : string.Join(';', declarations);
    }

    private static string ResolveTextDecorationsCss(TextDecorations decorations)
    {
        if (decorations == TextDecorations.None)
        {
            return "none";
        }

        var values = new List<string>(2);
        if (decorations.HasFlag(TextDecorations.Underline))
        {
            values.Add("underline");
        }

        if (decorations.HasFlag(TextDecorations.Strikethrough))
        {
            values.Add("line-through");
        }

        return values.Count == 0 ? "none" : string.Join(' ', values);
    }

    private static string ResolveFontWeightCss(FontAttributes attributes) =>
        attributes.HasFlag(FontAttributes.Bold) ? "700" : "400";

    private static string ResolveFontStyleCss(FontAttributes attributes) =>
        attributes.HasFlag(FontAttributes.Italic) ? "italic" : "normal";

    private static string? ResolveSpanFontFamily(string? fontFamily)
    {
        if (string.IsNullOrWhiteSpace(fontFamily))
        {
            return null;
        }

        var escapedFamily = fontFamily.Trim().Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
        return $"\"{escapedFamily}\", system-ui, sans-serif";
    }

    private static string ApplyTextTransform(string? source, TextTransform transform)
    {
        var normalizedTransform = transform == TextTransform.Default
            ? TextTransform.None
            : transform;
        return TextTransformUtilities.GetTransformedText(source, normalizedTransform);
    }

    private static void AppendEncodedTextPreservingLineBreaks(StringBuilder builder, string text)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var startIndex = 0;

        while (startIndex < normalized.Length)
        {
            var lineBreakIndex = normalized.IndexOf('\n', startIndex);
            if (lineBreakIndex < 0)
            {
                builder.Append(WebUtility.HtmlEncode(normalized.Substring(startIndex)));
                return;
            }

            if (lineBreakIndex > startIndex)
            {
                builder.Append(WebUtility.HtmlEncode(normalized.Substring(startIndex, lineBreakIndex - startIndex)));
            }

            builder.Append("<br>");
            startIndex = lineBreakIndex + 1;
        }
    }

    private enum HtmlTokenDisposition
    {
        PreserveLiteral,
        DropMarkup,
        EmitSanitizedTag
    }

    private static string SanitizeHtml(string html, out bool strippedMarkup)
    {
        strippedMarkup = false;
        if (string.IsNullOrEmpty(html))
        {
            return string.Empty;
        }

        var sanitized = new StringBuilder(html.Length);
        var cursor = 0;

        while (cursor < html.Length)
        {
            var openIndex = html.IndexOf('<', cursor);
            if (openIndex < 0)
            {
                sanitized.Append(html, cursor, html.Length - cursor);
                break;
            }

            if (openIndex > cursor)
            {
                sanitized.Append(html, cursor, openIndex - cursor);
            }

            var closeIndex = html.IndexOf('>', openIndex + 1);
            if (closeIndex < 0)
            {
                sanitized.Append(WebUtility.HtmlEncode(html.Substring(openIndex)));
                strippedMarkup = true;
                break;
            }

            // If another '<' appears before the next '>', the current '<' is literal text.
            var nestedOpenIndex = html.IndexOf('<', openIndex + 1);
            if (nestedOpenIndex >= 0 && nestedOpenIndex < closeIndex)
            {
                sanitized.Append("&lt;");
                cursor = openIndex + 1;
                continue;
            }

            var token = html.Substring(openIndex + 1, closeIndex - openIndex - 1);
            var disposition = TrySanitizeTagToken(token, sanitized, ref strippedMarkup);
            if (disposition == HtmlTokenDisposition.PreserveLiteral)
            {
                sanitized.Append(WebUtility.HtmlEncode(html.Substring(openIndex, closeIndex - openIndex + 1)));
            }

            cursor = closeIndex + 1;
        }

        return sanitized.ToString();
    }

    private static HtmlTokenDisposition TrySanitizeTagToken(string token, StringBuilder output, ref bool strippedMarkup)
    {
        var trimmed = token.Trim();
        if (trimmed.Length == 0)
        {
            return HtmlTokenDisposition.PreserveLiteral;
        }

        if (trimmed.StartsWith("!--", StringComparison.Ordinal)
            || trimmed[0] == '!'
            || trimmed[0] == '?')
        {
            strippedMarkup = true;
            return HtmlTokenDisposition.DropMarkup;
        }

        var isClosingTag = false;
        if (trimmed[0] == '/')
        {
            isClosingTag = true;
            trimmed = trimmed[1..].TrimStart();
        }

        if (trimmed.Length == 0 || !char.IsLetter(trimmed[0]))
        {
            return HtmlTokenDisposition.PreserveLiteral;
        }

        var isSelfClosingTag = false;
        if (!isClosingTag && trimmed.EndsWith("/", StringComparison.Ordinal))
        {
            isSelfClosingTag = true;
            trimmed = trimmed[..^1].TrimEnd();
        }

        if (trimmed.Length == 0)
        {
            strippedMarkup = true;
            return HtmlTokenDisposition.DropMarkup;
        }

        var nameLength = 0;
        while (nameLength < trimmed.Length && char.IsLetterOrDigit(trimmed[nameLength]))
        {
            nameLength++;
        }

        if (nameLength == 0)
        {
            return HtmlTokenDisposition.PreserveLiteral;
        }

        var tagName = trimmed[..nameLength].ToLowerInvariant();
        var hasAttributes = trimmed[nameLength..].Trim().Length > 0;
        if (!AllowedHtmlTags.Contains(tagName))
        {
            strippedMarkup = true;
            return HtmlTokenDisposition.DropMarkup;
        }

        if (hasAttributes || (isSelfClosingTag && !VoidHtmlTags.Contains(tagName)))
        {
            strippedMarkup = true;
        }

        if (isClosingTag)
        {
            if (VoidHtmlTags.Contains(tagName))
            {
                strippedMarkup = true;
                return HtmlTokenDisposition.DropMarkup;
            }

            output.Append("</")
                .Append(tagName)
                .Append('>');
            return HtmlTokenDisposition.EmitSanitizedTag;
        }

        output.Append('<')
            .Append(tagName)
            .Append('>');
        return HtmlTokenDisposition.EmitSanitizedTag;
    }

    private static void ReportHtmlSanitized()
    {
        if (!HtmlSanitizerWarnings.Add("label-texttype-html-sanitized"))
        {
            return;
        }

        MirageDiagnostics.Log(
            "label.map",
            "sanitized property=TextType value=TextType.Html policy=safe-subset");
    }

    private bool SetPropertyIfChanged(string name, string? value)
    {
        if (string.Equals(PlatformView.GetPropertyString(name), value, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetProperty(name, value);
        return true;
    }

    private bool SetStyleIfChanged(string name, string? value)
    {
        if (string.Equals(PlatformView.GetStyleValue(name), value, StringComparison.Ordinal))
        {
            return false;
        }

        PlatformView.SetStyle(name, value);
        return true;
    }
}
