using Maui.Browser.Dom;
using Maui.Browser.Measurement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Maui.Browser.Handlers;

public sealed class BrowserLabelHandler : BrowserViewHandler<ILabel>
{
    public static IPropertyMapper<ILabel, BrowserLabelHandler> Mapper = new PropertyMapper<ILabel, BrowserLabelHandler>(BrowserViewMapper.CommonViewMapper)
    {
        [nameof(ILabel.Text)] = MapText,
        [nameof(ITextStyle.TextColor)] = MapTextColor,
        [nameof(ITextStyle.Font)] = MapFont,
        [nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
        [nameof(ITextAlignment.HorizontalTextAlignment)] = MapHorizontalTextAlignment,
        [nameof(ITextAlignment.VerticalTextAlignment)] = MapVerticalTextAlignment,
        [nameof(ILabel.TextDecorations)] = MapTextDecorations,
        [nameof(ILabel.LineHeight)] = MapLineHeight,
        [nameof(ILabel.Padding)] = MapPadding,
        [nameof(IView.Visibility)] = MapVisibility,
        [nameof(Microsoft.Maui.Controls.Label.LineBreakMode)] = MapLineBreakMode,
        [nameof(Microsoft.Maui.Controls.Label.MaxLines)] = MapMaxLines
    };

    public BrowserLabelHandler()
        : base(Mapper)
    {
    }

    private bool UsesLineClamp => !string.IsNullOrEmpty(_lineClampValue);
    private string? _lineClampValue;

    protected override DomElement CreatePlatformView() => DomElement.Create("span");

    protected override void ConnectHandler(DomElement platformView)
    {
        platformView.Style("display", "flex");
        platformView.Style("flexDirection", "column");
        platformView.Style("justifyContent", "flex-start");
        platformView.Style("whiteSpace", "pre-wrap");
        platformView.Style("lineHeight", "1.2");
        platformView.Style("overflow", "visible");
        base.ConnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        var measurer = MauiContext?.Services.GetService<IBrowserTextMeasurer>();
        if (measurer is null)
        {
            return base.GetDesiredSize(widthConstraint, heightConstraint);
        }

        return measurer.MeasureText(
            VirtualView.Text,
            VirtualView.Font,
            widthConstraint,
            heightConstraint,
            VirtualView.Padding,
            minWidth: 1,
            minHeight: Math.Max(1, VirtualView.Font.Size > 0 ? VirtualView.Font.Size : 14));
    }

    public static void MapText(BrowserLabelHandler handler, ILabel label)
    {
        handler.PlatformView.Text(label.Text ?? string.Empty);
    }

    public static void MapTextColor(BrowserLabelHandler handler, ILabel label)
    {
        BrowserViewMapper.MapTextColor(label, handler.PlatformView);
    }

    public static void MapFont(BrowserLabelHandler handler, ILabel label)
    {
        BrowserViewMapper.MapFont(label, handler.PlatformView);
    }

    public static void MapCharacterSpacing(BrowserLabelHandler handler, ILabel label)
    {
        BrowserViewMapper.MapCharacterSpacing(label, handler.PlatformView);
    }

    public static void MapHorizontalTextAlignment(BrowserLabelHandler handler, ILabel label)
    {
        BrowserViewMapper.MapTextAlignment(label, handler.PlatformView);
    }

    public static void MapVerticalTextAlignment(BrowserLabelHandler handler, ILabel label)
    {
        var justifyContent = label.VerticalTextAlignment switch
        {
            TextAlignment.Center => "center",
            TextAlignment.End => "flex-end",
            _ => "flex-start"
        };

        handler.PlatformView.Style("justifyContent", justifyContent);
    }

    public static void MapTextDecorations(BrowserLabelHandler handler, ILabel label)
    {
        var decorations = label.TextDecorations;
        if (decorations == TextDecorations.None)
        {
            handler.PlatformView.Style("textDecoration", null);
            return;
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

        handler.PlatformView.Style("textDecoration", values.Count > 0 ? string.Join(' ', values) : null);
    }

    public static void MapLineHeight(BrowserLabelHandler handler, ILabel label)
    {
        if (double.IsNaN(label.LineHeight) || label.LineHeight <= 0)
        {
            handler.PlatformView.Style("lineHeight", "1.2");
            return;
        }

        handler.PlatformView.Style("lineHeight", label.LineHeight.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
    }

    public static void MapPadding(BrowserLabelHandler handler, ILabel label)
    {
        BrowserViewMapper.MapPadding(label, handler.PlatformView);
    }

    public static void MapVisibility(BrowserLabelHandler handler, ILabel label)
    {
        var visibleDisplay = handler.UsesLineClamp ? "-webkit-box" : "flex";

        switch (label.Visibility)
        {
            case Visibility.Visible:
                handler.PlatformView.Style("display", visibleDisplay);
                handler.PlatformView.Style("visibility", "visible");
                break;
            case Visibility.Hidden:
                handler.PlatformView.Style("display", visibleDisplay);
                handler.PlatformView.Style("visibility", "hidden");
                break;
            default:
                handler.PlatformView.Style("display", "none");
                handler.PlatformView.Style("visibility", "hidden");
                break;
        }
    }

    public static void MapLineBreakMode(BrowserLabelHandler handler, ILabel label)
    {
        var mode = (label as Microsoft.Maui.Controls.Label)?.LineBreakMode ?? Microsoft.Maui.LineBreakMode.WordWrap;
        var maxLines = (label as Microsoft.Maui.Controls.Label)?.MaxLines ?? -1;

        handler.ApplyLineBreakMode(mode, maxLines);
    }

    public static void MapMaxLines(BrowserLabelHandler handler, ILabel label)
    {
        var mode = (label as Microsoft.Maui.Controls.Label)?.LineBreakMode ?? Microsoft.Maui.LineBreakMode.WordWrap;
        var maxLines = (label as Microsoft.Maui.Controls.Label)?.MaxLines ?? -1;

        handler.ApplyLineBreakMode(mode, maxLines);
    }

    private void ApplyLineBreakMode(Microsoft.Maui.LineBreakMode mode, int maxLines)
    {
        if (maxLines > 1)
        {
            PlatformView.Style("webkitBoxOrient", "vertical");
            _lineClampValue = maxLines.ToString(System.Globalization.CultureInfo.InvariantCulture);
            PlatformView.Style("webkitLineClamp", _lineClampValue);
            PlatformView.Style("overflow", "hidden");
            PlatformView.Style("textOverflow", "ellipsis");
            PlatformView.Style("whiteSpace", "normal");
            MapVisibility(this, VirtualView);
            return;
        }

        _lineClampValue = null;
        PlatformView.Style("webkitBoxOrient", null);
        PlatformView.Style("webkitLineClamp", null);

        switch (mode)
        {
            case Microsoft.Maui.LineBreakMode.NoWrap:
                PlatformView.Style("whiteSpace", "nowrap");
                PlatformView.Style("overflow", "hidden");
                PlatformView.Style("textOverflow", "clip");
                break;
            case Microsoft.Maui.LineBreakMode.HeadTruncation:
            case Microsoft.Maui.LineBreakMode.MiddleTruncation:
            case Microsoft.Maui.LineBreakMode.TailTruncation:
                PlatformView.Style("whiteSpace", "nowrap");
                PlatformView.Style("overflow", "hidden");
                PlatformView.Style("textOverflow", "ellipsis");
                break;
            case Microsoft.Maui.LineBreakMode.CharacterWrap:
            case Microsoft.Maui.LineBreakMode.WordWrap:
            default:
                PlatformView.Style("whiteSpace", "normal");
                PlatformView.Style("overflow", "visible");
                PlatformView.Style("textOverflow", "clip");
                break;
        }

        if (maxLines == 1)
        {
            PlatformView.Style("whiteSpace", "nowrap");
            PlatformView.Style("overflow", "hidden");
            PlatformView.Style("textOverflow", mode is Microsoft.Maui.LineBreakMode.NoWrap ? "clip" : "ellipsis");
        }

        MapVisibility(this, VirtualView);
    }
}
