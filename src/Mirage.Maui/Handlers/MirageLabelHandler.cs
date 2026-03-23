using Mirage.Core.Diagnostics;
using Mirage.Core.Semantics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using System.Globalization;
using System.Text;

namespace Mirage.Maui.Handlers;

internal sealed class MirageLabelHandler : MirageViewHandler<ILabel>
{
    private const string DefaultLineHeight = "1.3";
    private const double DefaultFontSize = 14;
    private const string DefaultSystemLabelColor = MirageCommonViewProjection.DefaultSystemTextColor;
    private const string DefaultSystemFontFamily = MirageCommonViewProjection.DefaultSystemFontFamily;
    private static readonly HashSet<string> UnsupportedWarnings = [];
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

    internal static readonly string[] ParityDeferredControlsRemapPropertyKeys =
    {
        nameof(Label.TextType),
        nameof(Label.TextTransform),
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
        var projectedText = ResolveProjectedText(label);
        if (string.Equals(handler.PlatformView.GetPropertyString("Text"), projectedText, StringComparison.Ordinal))
        {
            return;
        }

        handler.PlatformView.SetProperty("Text", projectedText);
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
            return;
        }

        if (controlsLabel.TextType != TextType.Text)
        {
            ReportUnsupported(nameof(Label.TextType), controlsLabel.TextType, "fallback=plain-text");
        }

        MapText(handler, label);
    }

    public static void MapTextTransform(MirageLabelHandler handler, ILabel label)
    {
        if (label is not Label controlsLabel)
        {
            return;
        }

        if (controlsLabel.TextTransform != TextTransform.None)
        {
            ReportUnsupported(nameof(Label.TextTransform), controlsLabel.TextTransform, "fallback=plain-text");
        }

        MapText(handler, label);
    }

    public static void MapFormattedText(MirageLabelHandler handler, ILabel label)
    {
        if (label is not Label controlsLabel)
        {
            return;
        }

        if (controlsLabel.FormattedText is not null)
        {
            ReportUnsupported(nameof(Label.FormattedText), controlsLabel.FormattedText, "fallback=plain-text");
        }

        MapText(handler, label);
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

    private static void ReportUnsupported(string propertyName, object? value, string fallback)
    {
        var valueDescriptor = value switch
        {
            null => "<null>",
            Enum e => $"{e.GetType().Name}.{e}",
            _ => value.GetType().Name
        };

        var key = $"{propertyName}:{valueDescriptor}:{fallback}";
        if (!UnsupportedWarnings.Add(key))
        {
            return;
        }

        MirageDiagnostics.Log(
            "label.map",
            $"unsupported property={propertyName} value={valueDescriptor} {fallback}");
    }

    private static string? ResolveProjectedText(ILabel label)
    {
        if (!string.IsNullOrEmpty(label.Text))
        {
            return label.Text;
        }

        if (label is not Label controlsLabel
            || controlsLabel.FormattedText is not FormattedString formattedText
            || formattedText.Spans.Count == 0)
        {
            return label.Text;
        }

        var builder = new StringBuilder();
        foreach (var span in formattedText.Spans)
        {
            if (!string.IsNullOrEmpty(span.Text))
            {
                builder.Append(span.Text);
            }
        }

        return builder.ToString();
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
