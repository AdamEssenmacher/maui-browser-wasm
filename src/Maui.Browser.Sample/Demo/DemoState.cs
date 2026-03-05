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

    public DemoState()
    {
        HotReloadSummary = HotReloadStatus.BuildSummary();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string HotReloadSummary { get; }

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

    public string LiveText => $"Live text: {InputText}";

    public string ClickSummary => $"Button clicks: {ClickCount}";

    public string ProbeVisibilitySummary => ProbeIsVisible
        ? "Probe visibility: visible"
        : "Probe visibility: collapsed";

    public string ProbeOpacitySummary => $"Probe opacity: {ProbeOpacity:0.00}";

    public string StressSummary => StressRunCount == 0
        ? "Stress runs: not started"
        : $"Stress runs: {StressRunCount}";

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
