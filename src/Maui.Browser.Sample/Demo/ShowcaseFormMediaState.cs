using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui;

namespace Maui.Browser.Sample.Demo;

internal sealed class ShowcaseFormMediaState : INotifyPropertyChanged
{
    internal enum MediaSourceMode
    {
        Uri,
        File,
        Font
    }

    private static readonly string[] Items = ["Alpha", "Beta", "Gamma", "Delta"];

    private bool _switchOn = true;
    private bool _checkBoxChecked;
    private double _sliderValue = 35;
    private int _pickerIndex = 1;
    private MediaSourceMode _sourceMode = MediaSourceMode.Uri;
    private Aspect _aspect = Aspect.AspectFit;
    private int _imageButtonClicks;

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<string> PickerItems => Items;

    public bool SwitchOn
    {
        get => _switchOn;
        set
        {
            if (!SetProperty(ref _switchOn, value))
            {
                return;
            }

            OnPropertyChanged(nameof(FormSummary));
        }
    }

    public bool CheckBoxChecked
    {
        get => _checkBoxChecked;
        set
        {
            if (!SetProperty(ref _checkBoxChecked, value))
            {
                return;
            }

            OnPropertyChanged(nameof(FormSummary));
        }
    }

    public double SliderValue
    {
        get => _sliderValue;
        set
        {
            var normalized = Math.Clamp(value, 0, 100);
            if (!SetProperty(ref _sliderValue, normalized))
            {
                return;
            }

            OnPropertyChanged(nameof(FormSummary));
        }
    }

    public int PickerIndex
    {
        get => _pickerIndex;
        set
        {
            var normalized = value;
            if (normalized < -1 || normalized >= Items.Length)
            {
                normalized = -1;
            }

            if (!SetProperty(ref _pickerIndex, normalized))
            {
                return;
            }

            OnPropertyChanged(nameof(FormSummary));
        }
    }

    public MediaSourceMode SourceMode
    {
        get => _sourceMode;
        private set
        {
            if (!SetProperty(ref _sourceMode, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SourceSummary));
        }
    }

    public Aspect CurrentAspect
    {
        get => _aspect;
        private set
        {
            if (!SetProperty(ref _aspect, value))
            {
                return;
            }

            OnPropertyChanged(nameof(AspectSummary));
        }
    }

    public int ImageButtonClicks
    {
        get => _imageButtonClicks;
        private set
        {
            if (!SetProperty(ref _imageButtonClicks, value))
            {
                return;
            }

            OnPropertyChanged(nameof(ImageButtonSummary));
        }
    }

    public string FormSummary =>
        $"Switch: {(SwitchOn ? "on" : "off")}, CheckBox: {(CheckBoxChecked ? "checked" : "unchecked")}, Slider: {SliderValue:0.#}, Picker: {PickerSelectionText}";

    public string SourceSummary => $"Image source: {SourceMode.ToString().ToLowerInvariant()}";

    public string AspectSummary => $"Image aspect: {CurrentAspect.ToString().ToLowerInvariant()}";

    public string ImageButtonSummary => $"ImageButton clicks: {ImageButtonClicks}";

    public string PickerSelectionText => PickerIndex >= 0 && PickerIndex < Items.Length
        ? $"{PickerIndex}:{Items[PickerIndex]}"
        : "none";

    public void CycleSourceMode()
    {
        SourceMode = SourceMode switch
        {
            MediaSourceMode.Uri => MediaSourceMode.File,
            MediaSourceMode.File => MediaSourceMode.Font,
            _ => MediaSourceMode.Uri
        };
    }

    public void ToggleAspect()
    {
        CurrentAspect = CurrentAspect switch
        {
            Aspect.AspectFit => Aspect.AspectFill,
            Aspect.AspectFill => Aspect.Fill,
            _ => Aspect.AspectFit
        };
    }

    public void RecordImageButtonClick()
    {
        ImageButtonClicks += 1;
    }

    public void ApplyProgrammaticPreset()
    {
        SwitchOn = !SwitchOn;
        CheckBoxChecked = !CheckBoxChecked;
        SliderValue = SliderValue >= 70 ? 28.5 : 74.2;
        PickerIndex = PickerIndex <= 1 ? 3 : 0;
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
