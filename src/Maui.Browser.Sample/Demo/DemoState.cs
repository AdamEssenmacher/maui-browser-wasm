using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Maui.Browser.Sample.Demo;

internal sealed class DemoState : INotifyPropertyChanged
{
    private string _inputText = "Type in the entry to update this text.";
    private int _clickCount;
    private string _viewportText = "Viewport: waiting for initial arrange";
    private bool _probeIsVisible = true;
    private double _probeOpacity = 1.0;
    private int _stressRunCount;

    private bool _formSwitchOn = true;
    private bool _formCheckBoxChecked;
    private double _formSliderValue = 35;
    private int _formPickerSelectedIndex = 1;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string InputText
    {
        get => _inputText;
        set
        {
            if (SetProperty(ref _inputText, value))
            {
                OnPropertyChanged(nameof(LiveText));
            }
        }
    }

    public int ClickCount
    {
        get => _clickCount;
        set
        {
            if (SetProperty(ref _clickCount, value))
            {
                OnPropertyChanged(nameof(ClickSummary));
            }
        }
    }

    public string ViewportText
    {
        get => _viewportText;
        set => SetProperty(ref _viewportText, value);
    }

    public bool ProbeIsVisible
    {
        get => _probeIsVisible;
        set
        {
            if (SetProperty(ref _probeIsVisible, value))
            {
                OnPropertyChanged(nameof(ProbeVisibilitySummary));
            }
        }
    }

    public double ProbeOpacity
    {
        get => _probeOpacity;
        set
        {
            if (SetProperty(ref _probeOpacity, value))
            {
                OnPropertyChanged(nameof(ProbeOpacitySummary));
            }
        }
    }

    public int StressRunCount
    {
        get => _stressRunCount;
        set
        {
            if (SetProperty(ref _stressRunCount, value))
            {
                OnPropertyChanged(nameof(StressSummary));
            }
        }
    }

    public bool FormSwitchOn
    {
        get => _formSwitchOn;
        set
        {
            if (SetProperty(ref _formSwitchOn, value))
            {
                OnPropertyChanged(nameof(FormSwitchSummary));
            }
        }
    }

    public bool FormCheckBoxChecked
    {
        get => _formCheckBoxChecked;
        set
        {
            if (SetProperty(ref _formCheckBoxChecked, value))
            {
                OnPropertyChanged(nameof(FormCheckBoxSummary));
            }
        }
    }

    public double FormSliderValue
    {
        get => _formSliderValue;
        set
        {
            var normalizedValue = Math.Max(0, Math.Min(100, value));
            if (SetProperty(ref _formSliderValue, normalizedValue))
            {
                OnPropertyChanged(nameof(FormSliderSummary));
            }
        }
    }

    public int FormPickerSelectedIndex
    {
        get => _formPickerSelectedIndex;
        set
        {
            var maxIndex = FormPickerItems.Count - 1;
            var normalizedIndex = value < -1
                ? -1
                : value > maxIndex
                    ? maxIndex
                    : value;

            if (SetProperty(ref _formPickerSelectedIndex, normalizedIndex))
            {
                OnPropertyChanged(nameof(FormPickerSummary));
            }
        }
    }

    public IReadOnlyList<string> FormPickerItems { get; } = ["Alpha", "Beta", "Gamma", "Delta"];

    public string LiveText => $"Live text: {InputText}";

    public string ClickSummary => $"Button clicks: {ClickCount}";

    public string ProbeVisibilitySummary => ProbeIsVisible
        ? "Probe visibility: visible"
        : "Probe visibility: collapsed";

    public string ProbeOpacitySummary => $"Probe opacity: {ProbeOpacity:0.00}";

    public string StressSummary => StressRunCount == 0
        ? "Stress runs: not started"
        : $"Stress runs: {StressRunCount}";

    public string FormSwitchSummary => FormSwitchOn
        ? "Switch is on"
        : "Switch is off";

    public string FormCheckBoxSummary => FormCheckBoxChecked
        ? "Checkbox is checked"
        : "Checkbox is unchecked";

    public string FormSliderSummary => $"Slider value: {FormSliderValue:0.##}";

    public string FormPickerSummary
    {
        get
        {
            var selectedText = FormPickerSelectedIndex >= 0 && FormPickerSelectedIndex < FormPickerItems.Count
                ? FormPickerItems[FormPickerSelectedIndex]
                : "<none>";

            return $"Picker selection: {FormPickerSelectedIndex} ({selectedText})";
        }
    }

    public void ToggleProbeVisibility()
    {
        ProbeIsVisible = !ProbeIsVisible;
    }

    public void CycleProbeOpacity()
    {
        ProbeOpacity = ProbeOpacity switch
        {
            > 0.8 => 0.6,
            > 0.4 => 0.25,
            _ => 1.0
        };
    }

    public void ApplyProgrammaticFormUpdate()
    {
        FormSwitchOn = false;
        FormCheckBoxChecked = true;
        FormSliderValue = 82;
        FormPickerSelectedIndex = 2;
    }

    public void RecordStressRun()
    {
        StressRunCount++;
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
