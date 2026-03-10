using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace Showcase.Shared.App;

public sealed class ShowcaseState : INotifyPropertyChanged
{
    private int _clickCount;
    private string _foundationsLabelInput = "Mirage";
    private bool _foundationsGridSpacingExpanded;
    private bool _foundationsHorizontalSpacingExpanded;
    private bool _foundationsMarginExpanded;
    private bool _foundationsSpacingExpanded;
    private bool _foundationsVisibilityTargetVisible = true;
    private int _genericWidgetClickCount;
    private int _layoutLabScrollCommandAttemptCount;
    private string _name = "Adam";

    public ShowcaseState()
    {
        ClickCommand = new Command(OnClick);
        ToggleFoundationsVisibilityCommand = new Command(ToggleFoundationsVisibility);
        ToggleFoundationsMarginCommand = new Command(ToggleFoundationsMargin);
        ToggleFoundationsStackSpacingCommand = new Command(ToggleFoundationsSpacing);
        ToggleFoundationsHorizontalStackSpacingCommand = new Command(ToggleFoundationsHorizontalSpacing);
        ToggleFoundationsGridSpacingCommand = new Command(ToggleFoundationsGridSpacing);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ClickCommand { get; }

    public ICommand ToggleFoundationsVisibilityCommand { get; }

    public ICommand ToggleFoundationsMarginCommand { get; }

    public ICommand ToggleFoundationsStackSpacingCommand { get; }

    public ICommand ToggleFoundationsHorizontalStackSpacingCommand { get; }

    public ICommand ToggleFoundationsGridSpacingCommand { get; }

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

    public string LayoutLabScrollCommandCountText => $"LayoutLab RequestScrollTo attempts: {_layoutLabScrollCommandAttemptCount}";

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

    public void RegisterLayoutLabScrollCommandAttempt()
    {
        _layoutLabScrollCommandAttemptCount++;
        OnPropertyChanged(nameof(LayoutLabScrollCommandCountText));
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
