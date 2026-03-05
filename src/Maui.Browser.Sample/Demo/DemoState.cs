using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;

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

    private bool _coreInputTransparent;
    private int _coreProbeClickCount;
    private FlowDirection _coreFlowDirection = FlowDirection.LeftToRight;
    private bool _coreTransformApplied;
    private double _coreTranslationX;
    private double _coreTranslationY;
    private double _coreScale = 1;
    private double _coreRotation;
    private double _coreRotationX;
    private double _coreRotationY;
    private double _coreAnchorX = 0.5;
    private double _coreAnchorY = 0.5;
    private Color _coreProbeBackground = Colors.LightSkyBlue;
    private bool _coreBackgroundAlternate;
    private int _coreSemanticVersion = 1;
    private string _coreSemanticDescription = "Core semantics description v1";
    private string _coreSemanticHint = "Core semantics hint v1";

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

    public bool CoreInputTransparent
    {
        get => _coreInputTransparent;
        set
        {
            if (SetProperty(ref _coreInputTransparent, value))
            {
                OnPropertyChanged(nameof(CoreInputTransparentSummary));
            }
        }
    }

    public int CoreProbeClickCount
    {
        get => _coreProbeClickCount;
        private set
        {
            if (SetProperty(ref _coreProbeClickCount, value))
            {
                OnPropertyChanged(nameof(CoreProbeClickSummary));
            }
        }
    }

    public FlowDirection CoreFlowDirection
    {
        get => _coreFlowDirection;
        set
        {
            if (SetProperty(ref _coreFlowDirection, value))
            {
                OnPropertyChanged(nameof(CoreFlowDirectionSummary));
            }
        }
    }

    public double CoreTranslationX
    {
        get => _coreTranslationX;
        set
        {
            if (SetProperty(ref _coreTranslationX, value))
            {
                OnPropertyChanged(nameof(CoreTransformSummary));
            }
        }
    }

    public double CoreTranslationY
    {
        get => _coreTranslationY;
        set
        {
            if (SetProperty(ref _coreTranslationY, value))
            {
                OnPropertyChanged(nameof(CoreTransformSummary));
            }
        }
    }

    public double CoreScale
    {
        get => _coreScale;
        set
        {
            if (SetProperty(ref _coreScale, value))
            {
                OnPropertyChanged(nameof(CoreTransformSummary));
            }
        }
    }

    public double CoreRotation
    {
        get => _coreRotation;
        set
        {
            if (SetProperty(ref _coreRotation, value))
            {
                OnPropertyChanged(nameof(CoreTransformSummary));
            }
        }
    }

    public double CoreRotationX
    {
        get => _coreRotationX;
        set
        {
            if (SetProperty(ref _coreRotationX, value))
            {
                OnPropertyChanged(nameof(CoreTransformSummary));
            }
        }
    }

    public double CoreRotationY
    {
        get => _coreRotationY;
        set
        {
            if (SetProperty(ref _coreRotationY, value))
            {
                OnPropertyChanged(nameof(CoreTransformSummary));
            }
        }
    }

    public double CoreAnchorX
    {
        get => _coreAnchorX;
        set
        {
            if (SetProperty(ref _coreAnchorX, value))
            {
                OnPropertyChanged(nameof(CoreTransformSummary));
            }
        }
    }

    public double CoreAnchorY
    {
        get => _coreAnchorY;
        set
        {
            if (SetProperty(ref _coreAnchorY, value))
            {
                OnPropertyChanged(nameof(CoreTransformSummary));
            }
        }
    }

    public Color CoreProbeBackground
    {
        get => _coreProbeBackground;
        set
        {
            if (SetProperty(ref _coreProbeBackground, value))
            {
                OnPropertyChanged(nameof(CoreBackgroundSummary));
            }
        }
    }

    public string CoreSemanticDescription
    {
        get => _coreSemanticDescription;
        set
        {
            if (SetProperty(ref _coreSemanticDescription, value))
            {
                OnPropertyChanged(nameof(CoreSemanticsSummary));
            }
        }
    }

    public string CoreSemanticHint
    {
        get => _coreSemanticHint;
        set
        {
            if (SetProperty(ref _coreSemanticHint, value))
            {
                OnPropertyChanged(nameof(CoreSemanticsSummary));
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

    public string CoreInputTransparentSummary => $"Core input transparent: {(CoreInputTransparent ? "on" : "off")}";

    public string CoreProbeClickSummary => $"Core probe clicks: {CoreProbeClickCount}";

    public string CoreFlowDirectionSummary => $"Core flow direction: {(CoreFlowDirection == FlowDirection.RightToLeft ? "rtl" : "ltr")}";

    public string CoreTransformSummary =>
        $"Core transform: tx={CoreTranslationX:0.##}, ty={CoreTranslationY:0.##}, scale={CoreScale:0.##}, rot={CoreRotation:0.##}";

    public string CoreBackgroundSummary => $"Core background: {(_coreBackgroundAlternate ? "palegreen" : "lightskyblue")}";

    public string CoreSemanticsSummary => $"Core semantics version: {_coreSemanticVersion}";

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

    public void ToggleCoreInputTransparent()
    {
        CoreInputTransparent = !CoreInputTransparent;
    }

    public void RecordCoreProbeClick()
    {
        CoreProbeClickCount++;
    }

    public void ToggleCoreFlowDirection()
    {
        CoreFlowDirection = CoreFlowDirection == FlowDirection.RightToLeft
            ? FlowDirection.LeftToRight
            : FlowDirection.RightToLeft;
    }

    public void ToggleCoreTransformPreset()
    {
        _coreTransformApplied = !_coreTransformApplied;
        if (_coreTransformApplied)
        {
            CoreTranslationX = 18;
            CoreTranslationY = 12;
            CoreScale = 1.15;
            CoreRotation = 14;
            CoreRotationX = 5;
            CoreRotationY = -4;
            CoreAnchorX = 0.2;
            CoreAnchorY = 0.75;
            return;
        }

        CoreTranslationX = 0;
        CoreTranslationY = 0;
        CoreScale = 1;
        CoreRotation = 0;
        CoreRotationX = 0;
        CoreRotationY = 0;
        CoreAnchorX = 0.5;
        CoreAnchorY = 0.5;
    }

    public void ToggleCoreProbeBackground()
    {
        _coreBackgroundAlternate = !_coreBackgroundAlternate;
        CoreProbeBackground = _coreBackgroundAlternate ? Colors.PaleGreen : Colors.LightSkyBlue;
    }

    public void AdvanceCoreSemantics()
    {
        _coreSemanticVersion++;
        CoreSemanticDescription = $"Core semantics description v{_coreSemanticVersion}";
        CoreSemanticHint = $"Core semantics hint v{_coreSemanticVersion}";
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
