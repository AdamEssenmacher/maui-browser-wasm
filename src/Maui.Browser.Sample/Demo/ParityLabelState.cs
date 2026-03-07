using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Maui.Browser.Sample.Demo;

internal sealed class ParityLabelState : INotifyPropertyChanged
{
    private static readonly Color[] TextColorValues =
    [
        Colors.Black,
        Colors.DarkSlateGray,
        Colors.Crimson,
        Colors.DodgerBlue,
        Colors.MediumSeaGreen
    ];

    private static readonly string[] TextColorLabels =
    [
        "Black",
        "DarkSlateGray",
        "Crimson",
        "DodgerBlue",
        "MediumSeaGreen"
    ];

    private static readonly FontAttributes[] FontAttributeValues =
    [
        FontAttributes.None,
        FontAttributes.Bold,
        FontAttributes.Italic,
        FontAttributes.Bold | FontAttributes.Italic
    ];

    private static readonly string[] FontAttributeLabels =
    [
        "None",
        "Bold",
        "Italic",
        "BoldItalic"
    ];

    private static readonly TextDecorations[] TextDecorationValues =
    [
        TextDecorations.None,
        TextDecorations.Underline,
        TextDecorations.Strikethrough,
        TextDecorations.Underline | TextDecorations.Strikethrough
    ];

    private static readonly string[] TextDecorationLabels =
    [
        "None",
        "Underline",
        "Strikethrough",
        "Underline+Strikethrough"
    ];

    private static readonly TextAlignment[] HorizontalAlignmentValues =
    [
        TextAlignment.Start,
        TextAlignment.Center,
        TextAlignment.End,
        TextAlignment.Justify
    ];

    private static readonly TextAlignment[] VerticalAlignmentValues =
    [
        TextAlignment.Start,
        TextAlignment.Center,
        TextAlignment.End
    ];

    private static readonly LineBreakMode[] LineBreakModeValues =
    [
        LineBreakMode.NoWrap,
        LineBreakMode.WordWrap,
        LineBreakMode.CharacterWrap,
        LineBreakMode.HeadTruncation,
        LineBreakMode.MiddleTruncation,
        LineBreakMode.TailTruncation
    ];

    private static readonly string[] LineBreakModeLabels =
    [
        "NoWrap",
        "WordWrap",
        "CharacterWrap",
        "HeadTruncation",
        "MiddleTruncation",
        "TailTruncation"
    ];

    private static readonly FlowDirection[] FlowDirectionValues =
    [
        FlowDirection.LeftToRight,
        FlowDirection.RightToLeft
    ];

    private static readonly Visibility[] VisibilityValues =
    [
        Visibility.Visible,
        Visibility.Hidden,
        Visibility.Collapsed
    ];

    private string _previewText =
        "Interactive label preview. Change any property in the editor and this text updates instantly.";
    private int _textColorIndex;
    private string _fontFamily = string.Empty;
    private double _fontSize = 18;
    private int _fontAttributesIndex;
    private bool _fontAutoScalingEnabled = true;
    private double _characterSpacing;
    private int _textDecorationsIndex;
    private int _horizontalAlignmentIndex;
    private int _verticalAlignmentIndex;
    private double _paddingValue = 8;
    private double _lineHeight = 1.2;
    private int _lineBreakModeIndex = 1;
    private double _maxLinesEditor = 3;
    private int _flowDirectionIndex;
    private int _visibilityIndex;
    private double _opacity = 1;
    private string _semanticDescription = "Parity label semantics description";
    private string _semanticHint = "Parity label semantics hint";
    private bool _headingEnabled = true;
    private double _headingLevelEditor = 2;
    private double _previewHostWidth = 280;
    private double _previewHostHeight = 128;

    private bool _formattedTextProbeEnabled;
    private bool _htmlProbeEnabled;
    private int _textTransformProbeIndex;

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<string> TextColorOptions => TextColorLabels;
    public IReadOnlyList<string> FontAttributeOptions => FontAttributeLabels;
    public IReadOnlyList<string> TextDecorationOptions => TextDecorationLabels;
    public IReadOnlyList<string> HorizontalAlignmentOptions => ["Start", "Center", "End", "Justify"];
    public IReadOnlyList<string> VerticalAlignmentOptions => ["Start", "Center", "End"];
    public IReadOnlyList<string> LineBreakModeOptions => LineBreakModeLabels;
    public IReadOnlyList<string> FlowDirectionOptions => ["LeftToRight", "RightToLeft"];
    public IReadOnlyList<string> VisibilityOptions => ["Visible", "Hidden", "Collapsed"];
    public IReadOnlyList<string> TextTransformProbeOptions => ["None", "Uppercase", "Lowercase"];

    public string PreviewText
    {
        get => _previewText;
        set => SetPreviewProperty(nameof(PreviewText), ref _previewText, value, nameof(PreviewSummary));
    }

    public int TextColorIndex
    {
        get => _textColorIndex;
        set
        {
            var clamped = ClampIndex(value, TextColorValues.Length);
            SetPreviewProperty(nameof(TextColorIndex), ref _textColorIndex, clamped, nameof(PreviewTextColor), nameof(PreviewSummary));
        }
    }

    public Color PreviewTextColor => TextColorValues[_textColorIndex];

    public string FontFamily
    {
        get => _fontFamily;
        set => SetPreviewProperty(nameof(FontFamily), ref _fontFamily, value, nameof(PreviewSummary));
    }

    public double FontSize
    {
        get => _fontSize;
        set
        {
            var normalized = Math.Clamp(value, 10, 46);
            SetPreviewProperty(nameof(FontSize), ref _fontSize, normalized, nameof(PreviewSummary));
        }
    }

    public int FontAttributesIndex
    {
        get => _fontAttributesIndex;
        set
        {
            var clamped = ClampIndex(value, FontAttributeValues.Length);
            SetPreviewProperty(nameof(FontAttributesIndex), ref _fontAttributesIndex, clamped, nameof(PreviewFontAttributes), nameof(PreviewSummary));
        }
    }

    public FontAttributes PreviewFontAttributes => FontAttributeValues[_fontAttributesIndex];

    public bool FontAutoScalingEnabled
    {
        get => _fontAutoScalingEnabled;
        set => SetPreviewProperty(nameof(FontAutoScalingEnabled), ref _fontAutoScalingEnabled, value, nameof(PreviewSummary));
    }

    public double CharacterSpacing
    {
        get => _characterSpacing;
        set
        {
            var normalized = Math.Clamp(value, 0, 10);
            SetPreviewProperty(nameof(CharacterSpacing), ref _characterSpacing, normalized, nameof(PreviewSummary));
        }
    }

    public int TextDecorationsIndex
    {
        get => _textDecorationsIndex;
        set
        {
            var clamped = ClampIndex(value, TextDecorationValues.Length);
            SetPreviewProperty(nameof(TextDecorationsIndex), ref _textDecorationsIndex, clamped, nameof(PreviewTextDecorations), nameof(PreviewSummary));
        }
    }

    public TextDecorations PreviewTextDecorations => TextDecorationValues[_textDecorationsIndex];

    public int HorizontalAlignmentIndex
    {
        get => _horizontalAlignmentIndex;
        set
        {
            var clamped = ClampIndex(value, HorizontalAlignmentValues.Length);
            SetPreviewProperty(nameof(HorizontalAlignmentIndex), ref _horizontalAlignmentIndex, clamped, nameof(PreviewHorizontalTextAlignment), nameof(PreviewSummary));
        }
    }

    public TextAlignment PreviewHorizontalTextAlignment => HorizontalAlignmentValues[_horizontalAlignmentIndex];

    public int VerticalAlignmentIndex
    {
        get => _verticalAlignmentIndex;
        set
        {
            var clamped = ClampIndex(value, VerticalAlignmentValues.Length);
            SetPreviewProperty(nameof(VerticalAlignmentIndex), ref _verticalAlignmentIndex, clamped, nameof(PreviewVerticalTextAlignment), nameof(PreviewSummary));
        }
    }

    public TextAlignment PreviewVerticalTextAlignment => VerticalAlignmentValues[_verticalAlignmentIndex];

    public double PaddingValue
    {
        get => _paddingValue;
        set
        {
            var normalized = Math.Clamp(value, 0, 28);
            SetPreviewProperty(nameof(PaddingValue), ref _paddingValue, normalized, nameof(PreviewPadding), nameof(PreviewSummary));
        }
    }

    public Thickness PreviewPadding => new(Math.Round(_paddingValue, 0));

    public double LineHeight
    {
        get => _lineHeight;
        set
        {
            var normalized = Math.Clamp(value, 0.8, 2.5);
            SetPreviewProperty(nameof(LineHeight), ref _lineHeight, normalized, nameof(PreviewSummary));
        }
    }

    public int LineBreakModeIndex
    {
        get => _lineBreakModeIndex;
        set
        {
            var clamped = ClampIndex(value, LineBreakModeValues.Length);
            SetPreviewProperty(nameof(LineBreakModeIndex), ref _lineBreakModeIndex, clamped, nameof(PreviewLineBreakMode), nameof(PreviewSummary));
        }
    }

    public LineBreakMode PreviewLineBreakMode => LineBreakModeValues[_lineBreakModeIndex];

    public double MaxLinesEditor
    {
        get => _maxLinesEditor;
        set
        {
            var rounded = Math.Round(value, 0, MidpointRounding.AwayFromZero);
            var normalized = Math.Clamp(rounded, -1, 5);
            SetPreviewProperty(nameof(MaxLinesEditor), ref _maxLinesEditor, normalized, nameof(PreviewMaxLines), nameof(PreviewSummary));
        }
    }

    public int PreviewMaxLines => (int)_maxLinesEditor;

    public int FlowDirectionIndex
    {
        get => _flowDirectionIndex;
        set
        {
            var clamped = ClampIndex(value, FlowDirectionValues.Length);
            SetPreviewProperty(nameof(FlowDirectionIndex), ref _flowDirectionIndex, clamped, nameof(PreviewFlowDirection), nameof(PreviewSummary));
        }
    }

    public FlowDirection PreviewFlowDirection => FlowDirectionValues[_flowDirectionIndex];

    public int VisibilityIndex
    {
        get => _visibilityIndex;
        set
        {
            var clamped = ClampIndex(value, VisibilityValues.Length);
            SetPreviewProperty(nameof(VisibilityIndex), ref _visibilityIndex, clamped, nameof(PreviewVisibility), nameof(PreviewSummary));
        }
    }

    public Visibility PreviewVisibility => VisibilityValues[_visibilityIndex];

    public double Opacity
    {
        get => _opacity;
        set
        {
            var normalized = Math.Clamp(value, 0, 1);
            SetPreviewProperty(nameof(Opacity), ref _opacity, normalized, nameof(PreviewSummary));
        }
    }

    public string SemanticDescription
    {
        get => _semanticDescription;
        set => SetPreviewProperty(nameof(SemanticDescription), ref _semanticDescription, value, nameof(PreviewSummary));
    }

    public string SemanticHint
    {
        get => _semanticHint;
        set => SetPreviewProperty(nameof(SemanticHint), ref _semanticHint, value, nameof(PreviewSummary));
    }

    public bool HeadingEnabled
    {
        get => _headingEnabled;
        set => SetPreviewProperty(nameof(HeadingEnabled), ref _headingEnabled, value, nameof(PreviewHeadingLevel), nameof(PreviewSummary));
    }

    public double HeadingLevelEditor
    {
        get => _headingLevelEditor;
        set
        {
            var rounded = Math.Round(value, 0, MidpointRounding.AwayFromZero);
            var normalized = Math.Clamp(rounded, 1, 6);
            SetPreviewProperty(nameof(HeadingLevelEditor), ref _headingLevelEditor, normalized, nameof(PreviewHeadingLevel), nameof(PreviewSummary));
        }
    }

    public SemanticHeadingLevel PreviewHeadingLevel =>
        HeadingEnabled
            ? _headingLevelEditor switch
            {
                1 => SemanticHeadingLevel.Level1,
                2 => SemanticHeadingLevel.Level2,
                3 => SemanticHeadingLevel.Level3,
                4 => SemanticHeadingLevel.Level4,
                5 => SemanticHeadingLevel.Level5,
                _ => SemanticHeadingLevel.Level6
            }
            : SemanticHeadingLevel.None;

    public double PreviewHostWidth
    {
        get => _previewHostWidth;
        set
        {
            var normalized = Math.Clamp(value, 160, 420);
            SetPreviewProperty(nameof(PreviewHostWidth), ref _previewHostWidth, normalized, nameof(PreviewSummary));
        }
    }

    public double PreviewHostHeight
    {
        get => _previewHostHeight;
        set
        {
            var normalized = Math.Clamp(value, 80, 240);
            SetPreviewProperty(nameof(PreviewHostHeight), ref _previewHostHeight, normalized, nameof(PreviewSummary));
        }
    }

    public string PreviewSummary =>
        $"color={TextColorOptions[TextColorIndex]}, font={FontSize.ToString("0.#", CultureInfo.InvariantCulture)}px/{FontAttributeOptions[FontAttributesIndex]}, " +
        $"lineBreak={LineBreakModeOptions[LineBreakModeIndex]}, maxLines={PreviewMaxLines}, visibility={VisibilityOptions[VisibilityIndex]}, opacity={Opacity.ToString("0.##", CultureInfo.InvariantCulture)}";

    public bool FormattedTextProbeEnabled
    {
        get => _formattedTextProbeEnabled;
        set
        {
            if (!SetProperty(ref _formattedTextProbeEnabled, value))
            {
                return;
            }

            OnPropertyChanged(nameof(FormattedTextProbeSummary));
            OnPropertyChanged(nameof(FormattedTextProbeStatus));
        }
    }

    public bool HtmlProbeEnabled
    {
        get => _htmlProbeEnabled;
        set
        {
            if (!SetProperty(ref _htmlProbeEnabled, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HtmlProbeSummary));
            OnPropertyChanged(nameof(HtmlProbeStatus));
        }
    }

    public int TextTransformProbeIndex
    {
        get => _textTransformProbeIndex;
        set
        {
            var clamped = ClampIndex(value, 3);
            if (!SetProperty(ref _textTransformProbeIndex, clamped))
            {
                return;
            }

            OnPropertyChanged(nameof(SelectedTextTransformProbe));
            OnPropertyChanged(nameof(TextTransformProbeSummary));
            OnPropertyChanged(nameof(TextTransformProbeStatus));
        }
    }

    public TextTransform SelectedTextTransformProbe =>
        _textTransformProbeIndex switch
        {
            1 => TextTransform.Uppercase,
            2 => TextTransform.Lowercase,
            _ => TextTransform.None
        };

    public string FormattedTextProbeSummary =>
        FormattedTextProbeEnabled
            ? "Actual: FormattedText spans are set, but browser handler maps plain ILabel.Text only. Status: gap-documented."
            : "Actual: Plain text path active.";

    public string FormattedTextProbeStatus =>
        FormattedTextProbeEnabled ? "gap-documented" : "plain-text-baseline";

    public string HtmlProbeSummary =>
        HtmlProbeEnabled
            ? "Actual: TextType=Html is not projected by BrowserLabelHandler; raw markup text remains visible. Status: gap-documented."
            : "Actual: TextType=Text baseline.";

    public string HtmlProbeStatus =>
        HtmlProbeEnabled ? "gap-documented" : "text-baseline";

    public string TextTransformProbeSummary =>
        SelectedTextTransformProbe == TextTransform.None
            ? "Actual: TextTransform=None baseline."
            : $"Actual: TextTransform={SelectedTextTransformProbe} does not change browser output in this backend path. Status: gap-documented.";

    public string TextTransformProbeStatus =>
        SelectedTextTransformProbe == TextTransform.None ? "none-baseline" : "gap-documented";

    public void ApplyTruncationPreset()
    {
        PreviewText =
            "Tail truncation preset with a long sentence to make sure single-line clipping and ellipsis behavior are easy to see in the live preview host.";
        FontSize = 17;
        LineBreakModeIndex = 5;
        MaxLinesEditor = 1;
        PaddingValue = 6;
        PreviewHostWidth = 220;
        PreviewHostHeight = 84;
        Opacity = 0.92;
    }

    public void ResetDefaults()
    {
        PreviewText =
            "Interactive label preview. Change any property in the editor and this text updates instantly.";
        TextColorIndex = 0;
        FontFamily = string.Empty;
        FontSize = 18;
        FontAttributesIndex = 0;
        FontAutoScalingEnabled = true;
        CharacterSpacing = 0;
        TextDecorationsIndex = 0;
        HorizontalAlignmentIndex = 0;
        VerticalAlignmentIndex = 0;
        PaddingValue = 8;
        LineHeight = 1.2;
        LineBreakModeIndex = 1;
        MaxLinesEditor = 3;
        FlowDirectionIndex = 0;
        VisibilityIndex = 0;
        Opacity = 1;
        SemanticDescription = "Parity label semantics description";
        SemanticHint = "Parity label semantics hint";
        HeadingEnabled = true;
        HeadingLevelEditor = 2;
        PreviewHostWidth = 280;
        PreviewHostHeight = 128;

        FormattedTextProbeEnabled = false;
        HtmlProbeEnabled = false;
        TextTransformProbeIndex = 0;
    }

    private static int ClampIndex(int index, int length)
    {
        if (length <= 0)
        {
            return 0;
        }

        if (index < 0)
        {
            return 0;
        }

        if (index >= length)
        {
            return length - 1;
        }

        return index;
    }

    private bool SetPreviewProperty<T>(string propertyName, ref T storage, T value, params string[] dependentProperties)
    {
        if (!SetProperty(ref storage, value, propertyName))
        {
            return false;
        }

        foreach (var dependentProperty in dependentProperties)
        {
            OnPropertyChanged(dependentProperty);
        }

        return true;
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
