using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace Showcase.Fluent.App;

internal sealed class ShowcaseState : INotifyPropertyChanged
{
    private static readonly Brush[] ContentViewBackgroundSequence =
    [
        new SolidColorBrush(Color.FromArgb("#DBEAFE")),
        new SolidColorBrush(Color.FromArgb("#CCFBF1")),
        new LinearGradientBrush(
            new GradientStopCollection
            {
                new GradientStop(Color.FromArgb("#BFDBFE"), 0),
                new GradientStop(Color.FromArgb("#DDD6FE"), 1)
            },
            new Point(0, 0),
            new Point(1, 1)),
        new SolidColorBrush(Color.FromArgb("#FDE68A"))
    ];

    private static readonly string[] ContentViewBackgroundNames =
    [
        "Blue Solid",
        "Mint Solid",
        "Linear Gradient",
        "Amber Solid"
    ];

    private int _clickCount;
    private int _contentViewBackgroundIndex;
    private bool _contentViewHeightClipEnabled;
    private bool _contentViewHeightOverflow;
    private int _contentViewMarginIndex;
    private int _contentViewOpacityIndex;
    private int _contentViewVerticalOptionIndex;
    private bool _contentViewVisible = true;
    private bool _contentViewWidthClipEnabled;
    private bool _contentViewWidthOverflow;
    private int _contentViewHorizontalOptionIndex;
    private string _foundationsLabelInput = "Mirage";
    private bool _foundationsGridSpacingExpanded;
    private bool _foundationsHorizontalSpacingExpanded;
    private bool _foundationsMarginExpanded;
    private bool _foundationsSpacingExpanded;
    private bool _foundationsVisibilityTargetVisible = true;
    private int _genericWidgetClickCount;
    private string _name = "Adam";

    public ShowcaseState()
    {
        ClickCommand = new Command(OnClick);
        ToggleFoundationsVisibilityCommand = new Command(ToggleFoundationsVisibility);
        ToggleFoundationsMarginCommand = new Command(ToggleFoundationsMargin);
        ToggleFoundationsStackSpacingCommand = new Command(ToggleFoundationsSpacing);
        ToggleFoundationsHorizontalStackSpacingCommand = new Command(ToggleFoundationsHorizontalSpacing);
        ToggleFoundationsGridSpacingCommand = new Command(ToggleFoundationsGridSpacing);
        ToggleContentViewBackgroundCommand = new Command(ToggleContentViewBackground);
        ToggleContentViewOpacityCommand = new Command(ToggleContentViewOpacity);
        ToggleContentViewVisibilityCommand = new Command(ToggleContentViewVisibility);
        ToggleContentViewMarginCommand = new Command(ToggleContentViewMargin);
        ToggleContentViewWidthModeCommand = new Command(ToggleContentViewWidthMode);
        CycleContentViewHorizontalOptionCommand = new Command(CycleContentViewHorizontalOption);
        ToggleContentViewWidthClipCommand = new Command(ToggleContentViewWidthClip);
        ToggleContentViewHeightModeCommand = new Command(ToggleContentViewHeightMode);
        CycleContentViewVerticalOptionCommand = new Command(CycleContentViewVerticalOption);
        ToggleContentViewHeightClipCommand = new Command(ToggleContentViewHeightClip);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ClickCommand { get; }

    public ICommand ToggleFoundationsVisibilityCommand { get; }

    public ICommand ToggleFoundationsMarginCommand { get; }

    public ICommand ToggleFoundationsStackSpacingCommand { get; }

    public ICommand ToggleFoundationsHorizontalStackSpacingCommand { get; }

    public ICommand ToggleFoundationsGridSpacingCommand { get; }

    public ICommand ToggleContentViewBackgroundCommand { get; }

    public ICommand ToggleContentViewOpacityCommand { get; }

    public ICommand ToggleContentViewVisibilityCommand { get; }

    public ICommand ToggleContentViewMarginCommand { get; }

    public ICommand ToggleContentViewWidthModeCommand { get; }

    public ICommand CycleContentViewHorizontalOptionCommand { get; }

    public ICommand ToggleContentViewWidthClipCommand { get; }

    public ICommand ToggleContentViewHeightModeCommand { get; }

    public ICommand CycleContentViewVerticalOptionCommand { get; }

    public ICommand ToggleContentViewHeightClipCommand { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (string.Equals(_name, value, StringComparison.Ordinal))
            {
                return;
            }

            _name = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Greeting));
            OnPropertyChanged(nameof(GenericWidgetText));
        }
    }

    public string Greeting => string.IsNullOrWhiteSpace(_name)
        ? "Hello, developer"
        : $"Hello, {_name}";

    public string ClickCountText => $"Click count: {_clickCount}";

    public string GenericWidgetText => string.IsNullOrWhiteSpace(_name)
        ? "vendor-pill element: Hello, developer"
        : $"vendor-pill element: Hello, {_name}";

    public string GenericWidgetClicksText => $"Generic widget clicks: {_genericWidgetClickCount}";

    public string FoundationsLabelInput
    {
        get => _foundationsLabelInput;
        set
        {
            if (string.Equals(_foundationsLabelInput, value, StringComparison.Ordinal))
            {
                return;
            }

            _foundationsLabelInput = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FoundationsLabelOutput));
        }
    }

    public string FoundationsLabelOutput => string.IsNullOrWhiteSpace(_foundationsLabelInput)
        ? "Live label: (empty)"
        : $"Live label: {_foundationsLabelInput}";

    public bool FoundationsVisibilityTargetVisible => _foundationsVisibilityTargetVisible;

    public string FoundationsVisibilityButtonText => _foundationsVisibilityTargetVisible
        ? "Hide Visibility Target"
        : "Show Visibility Target";

    public Thickness FoundationsMarginTargetMargin => _foundationsMarginExpanded
        ? new Thickness(10, 20, 30, 40)
        : new Thickness(2, 4, 6, 8);

    public string FoundationsMarginButtonText => _foundationsMarginExpanded
        ? "Set Compact Margin"
        : "Set Expanded Margin";

    public double FoundationsStackSpacing => _foundationsSpacingExpanded ? 24 : 8;

    public string FoundationsStackSpacingButtonText => _foundationsSpacingExpanded
        ? "Set Compact Spacing"
        : "Set Expanded Spacing";

    public double FoundationsHorizontalStackSpacing => _foundationsHorizontalSpacingExpanded ? 30 : 10;

    public string FoundationsHorizontalStackSpacingButtonText => _foundationsHorizontalSpacingExpanded
        ? "Set Compact Horizontal Spacing"
        : "Set Expanded Horizontal Spacing";

    public double FoundationsGridColumnSpacing => _foundationsGridSpacingExpanded ? 24 : 10;

    public double FoundationsGridRowSpacing => _foundationsGridSpacingExpanded ? 18 : 8;

    public string FoundationsGridSpacingButtonText => _foundationsGridSpacingExpanded
        ? "Set Compact Grid Spacing"
        : "Set Expanded Grid Spacing";

    public Brush ContentViewBackground => ContentViewBackgroundSequence[_contentViewBackgroundIndex];

    public double ContentViewOpacity => _contentViewOpacityIndex switch
    {
        1 => 0.65,
        2 => 0.35,
        _ => 1.0
    };

    public bool ContentViewVisible => _contentViewVisible;

    public Thickness ContentViewMargin => _contentViewMarginIndex == 0
        ? new Thickness(0)
        : new Thickness(8, 16, 12, 10);

    public Thickness ContentViewPadding => _contentViewMarginIndex == 0
        ? new Thickness(12, 10, 12, 10)
        : new Thickness(18, 14, 18, 14);

    public string ContentViewBackgroundButtonText => $"Background: {ContentViewBackgroundNames[_contentViewBackgroundIndex]}";

    public string ContentViewOpacityButtonText => $"Opacity: {ContentViewOpacity:0.##}";

    public string ContentViewVisibilityButtonText => _contentViewVisible
        ? "Hide Target"
        : "Show Target";

    public string ContentViewMarginButtonText => _contentViewMarginIndex == 0
        ? "Set Expanded Margin/Padding"
        : "Set Compact Margin/Padding";

    public string ContentViewDiagnosticsText =>
        $"Background={ContentViewBackgroundNames[_contentViewBackgroundIndex]} | Opacity={ContentViewOpacity:0.##} | IsVisible={_contentViewVisible} | Margin={FormatThickness(ContentViewMargin)}";

    public double ContentViewInteractiveWidthRequest => _contentViewWidthOverflow ? 360 : 180;

    public LayoutOptions ContentViewInteractiveHorizontalOptions => _contentViewHorizontalOptionIndex switch
    {
        1 => LayoutOptions.Center,
        2 => LayoutOptions.End,
        _ => LayoutOptions.Start
    };

    public bool ContentViewInteractiveWidthClipEnabled => _contentViewWidthClipEnabled;

    public Geometry? ContentViewInteractiveWidthClip => _contentViewWidthClipEnabled
        ? new RectangleGeometry(new Rect(0, 0, 240, 78))
        : null;

    public double ContentViewInteractiveHeightRequest => _contentViewHeightOverflow ? 176 : 72;

    public LayoutOptions ContentViewInteractiveVerticalOptions => _contentViewVerticalOptionIndex switch
    {
        1 => LayoutOptions.Center,
        2 => LayoutOptions.End,
        _ => LayoutOptions.Start
    };

    public bool ContentViewInteractiveHeightClipEnabled => _contentViewHeightClipEnabled;

    public Geometry? ContentViewInteractiveHeightClip => _contentViewHeightClipEnabled
        ? new RectangleGeometry(new Rect(0, 0, 220, 90))
        : null;

    public string ContentViewWidthModeButtonText => _contentViewWidthOverflow
        ? "Width mode: overflow"
        : "Width mode: fit";

    public string ContentViewHorizontalOptionButtonText => $"Horizontal option: {LayoutOptionLabel(ContentViewInteractiveHorizontalOptions)}";

    public string ContentViewWidthClipButtonText => _contentViewWidthClipEnabled
        ? "Width clip: on"
        : "Width clip: off";

    public string ContentViewHeightModeButtonText => _contentViewHeightOverflow
        ? "Height mode: overflow"
        : "Height mode: fit";

    public string ContentViewVerticalOptionButtonText => $"Vertical option: {LayoutOptionLabel(ContentViewInteractiveVerticalOptions)}";

    public string ContentViewHeightClipButtonText => _contentViewHeightClipEnabled
        ? "Height clip: on"
        : "Height clip: off";

    public string ContentViewSizeOverflowDiagnosticsText =>
        $"Width={ContentViewInteractiveWidthRequest:0}px/{LayoutOptionLabel(ContentViewInteractiveHorizontalOptions)}/{(_contentViewWidthClipEnabled ? "clip" : "no-clip")} | Height={ContentViewInteractiveHeightRequest:0}px/{LayoutOptionLabel(ContentViewInteractiveVerticalOptions)}/{(_contentViewHeightClipEnabled ? "clip" : "no-clip")}";

    private void OnClick()
    {
        _clickCount++;
        OnPropertyChanged(nameof(ClickCountText));
    }

    public void RegisterGenericWidgetClick()
    {
        _genericWidgetClickCount++;
        OnPropertyChanged(nameof(GenericWidgetClicksText));
    }

    private void ToggleFoundationsVisibility()
    {
        _foundationsVisibilityTargetVisible = !_foundationsVisibilityTargetVisible;
        OnPropertyChanged(nameof(FoundationsVisibilityTargetVisible));
        OnPropertyChanged(nameof(FoundationsVisibilityButtonText));
    }

    private void ToggleFoundationsMargin()
    {
        _foundationsMarginExpanded = !_foundationsMarginExpanded;
        OnPropertyChanged(nameof(FoundationsMarginTargetMargin));
        OnPropertyChanged(nameof(FoundationsMarginButtonText));
    }

    private void ToggleFoundationsSpacing()
    {
        _foundationsSpacingExpanded = !_foundationsSpacingExpanded;
        OnPropertyChanged(nameof(FoundationsStackSpacing));
        OnPropertyChanged(nameof(FoundationsStackSpacingButtonText));
    }

    private void ToggleFoundationsHorizontalSpacing()
    {
        _foundationsHorizontalSpacingExpanded = !_foundationsHorizontalSpacingExpanded;
        OnPropertyChanged(nameof(FoundationsHorizontalStackSpacing));
        OnPropertyChanged(nameof(FoundationsHorizontalStackSpacingButtonText));
    }

    private void ToggleFoundationsGridSpacing()
    {
        _foundationsGridSpacingExpanded = !_foundationsGridSpacingExpanded;
        OnPropertyChanged(nameof(FoundationsGridColumnSpacing));
        OnPropertyChanged(nameof(FoundationsGridRowSpacing));
        OnPropertyChanged(nameof(FoundationsGridSpacingButtonText));
    }

    private static string FormatThickness(Thickness thickness)
    {
        return $"{thickness.Left:0.###},{thickness.Top:0.###},{thickness.Right:0.###},{thickness.Bottom:0.###}";
    }

    private void ToggleContentViewBackground()
    {
        _contentViewBackgroundIndex = (_contentViewBackgroundIndex + 1) % ContentViewBackgroundSequence.Length;
        OnPropertyChanged(nameof(ContentViewBackground));
        OnPropertyChanged(nameof(ContentViewBackgroundButtonText));
        OnPropertyChanged(nameof(ContentViewDiagnosticsText));
    }

    private void ToggleContentViewOpacity()
    {
        _contentViewOpacityIndex = (_contentViewOpacityIndex + 1) % 3;
        OnPropertyChanged(nameof(ContentViewOpacity));
        OnPropertyChanged(nameof(ContentViewOpacityButtonText));
        OnPropertyChanged(nameof(ContentViewDiagnosticsText));
    }

    private void ToggleContentViewVisibility()
    {
        _contentViewVisible = !_contentViewVisible;
        OnPropertyChanged(nameof(ContentViewVisible));
        OnPropertyChanged(nameof(ContentViewVisibilityButtonText));
        OnPropertyChanged(nameof(ContentViewDiagnosticsText));
    }

    private void ToggleContentViewMargin()
    {
        _contentViewMarginIndex = (_contentViewMarginIndex + 1) % 2;
        OnPropertyChanged(nameof(ContentViewMargin));
        OnPropertyChanged(nameof(ContentViewPadding));
        OnPropertyChanged(nameof(ContentViewMarginButtonText));
        OnPropertyChanged(nameof(ContentViewDiagnosticsText));
    }

    private void ToggleContentViewWidthMode()
    {
        _contentViewWidthOverflow = !_contentViewWidthOverflow;
        OnPropertyChanged(nameof(ContentViewInteractiveWidthRequest));
        OnPropertyChanged(nameof(ContentViewWidthModeButtonText));
        OnPropertyChanged(nameof(ContentViewSizeOverflowDiagnosticsText));
    }

    private void CycleContentViewHorizontalOption()
    {
        _contentViewHorizontalOptionIndex = (_contentViewHorizontalOptionIndex + 1) % 3;
        OnPropertyChanged(nameof(ContentViewInteractiveHorizontalOptions));
        OnPropertyChanged(nameof(ContentViewHorizontalOptionButtonText));
        OnPropertyChanged(nameof(ContentViewSizeOverflowDiagnosticsText));
    }

    private void ToggleContentViewWidthClip()
    {
        _contentViewWidthClipEnabled = !_contentViewWidthClipEnabled;
        OnPropertyChanged(nameof(ContentViewInteractiveWidthClipEnabled));
        OnPropertyChanged(nameof(ContentViewInteractiveWidthClip));
        OnPropertyChanged(nameof(ContentViewWidthClipButtonText));
        OnPropertyChanged(nameof(ContentViewSizeOverflowDiagnosticsText));
    }

    private void ToggleContentViewHeightMode()
    {
        _contentViewHeightOverflow = !_contentViewHeightOverflow;
        OnPropertyChanged(nameof(ContentViewInteractiveHeightRequest));
        OnPropertyChanged(nameof(ContentViewHeightModeButtonText));
        OnPropertyChanged(nameof(ContentViewSizeOverflowDiagnosticsText));
    }

    private void CycleContentViewVerticalOption()
    {
        _contentViewVerticalOptionIndex = (_contentViewVerticalOptionIndex + 1) % 3;
        OnPropertyChanged(nameof(ContentViewInteractiveVerticalOptions));
        OnPropertyChanged(nameof(ContentViewVerticalOptionButtonText));
        OnPropertyChanged(nameof(ContentViewSizeOverflowDiagnosticsText));
    }

    private void ToggleContentViewHeightClip()
    {
        _contentViewHeightClipEnabled = !_contentViewHeightClipEnabled;
        OnPropertyChanged(nameof(ContentViewInteractiveHeightClipEnabled));
        OnPropertyChanged(nameof(ContentViewInteractiveHeightClip));
        OnPropertyChanged(nameof(ContentViewHeightClipButtonText));
        OnPropertyChanged(nameof(ContentViewSizeOverflowDiagnosticsText));
    }

    private static string LayoutOptionLabel(LayoutOptions options)
    {
        var alignment = options.Alignment.ToString();
        if (string.Equals(alignment, "Center", StringComparison.Ordinal))
        {
            return "Center";
        }

        if (string.Equals(alignment, "End", StringComparison.Ordinal))
        {
            return "End";
        }

        return "Start";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
