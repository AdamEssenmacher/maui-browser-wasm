using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Maui.Browser.Sample.Demo;

internal enum ClipProbeMode
{
    None,
    Rectangle,
    RoundRectangle,
    UnsupportedEllipse
}

internal enum ShadowProbeMode
{
    Off,
    Solid,
    UnsupportedGradient
}

internal enum BackgroundProbeMode
{
    Solid,
    LinearGradient,
    UnsupportedRadial
}

internal sealed class ParityContainersLayoutState : INotifyPropertyChanged
{
    private string _clipSummary = "Clip mode: none";
    private string _shadowSummary = "Shadow mode: off";
    private string _backgroundSummary = "Background mode: solid";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ClipSummary
    {
        get => _clipSummary;
        private set => SetProperty(ref _clipSummary, value);
    }

    public string ShadowSummary
    {
        get => _shadowSummary;
        private set => SetProperty(ref _shadowSummary, value);
    }

    public string BackgroundSummary
    {
        get => _backgroundSummary;
        private set => SetProperty(ref _backgroundSummary, value);
    }

    public void SetClipMode(ClipProbeMode mode)
    {
        ClipSummary = mode switch
        {
            ClipProbeMode.Rectangle => "Clip mode: rectangle",
            ClipProbeMode.RoundRectangle => "Clip mode: round-rectangle",
            ClipProbeMode.UnsupportedEllipse => "Clip mode: unsupported ellipse (fallback expected)",
            _ => "Clip mode: none"
        };
    }

    public void SetShadowMode(ShadowProbeMode mode)
    {
        ShadowSummary = mode switch
        {
            ShadowProbeMode.Solid => "Shadow mode: solid",
            ShadowProbeMode.UnsupportedGradient => "Shadow mode: unsupported gradient (fallback expected)",
            _ => "Shadow mode: off"
        };
    }

    public void SetBackgroundMode(BackgroundProbeMode mode)
    {
        BackgroundSummary = mode switch
        {
            BackgroundProbeMode.LinearGradient => "Background mode: linear-gradient",
            BackgroundProbeMode.UnsupportedRadial => "Background mode: unsupported radial (fallback expected)",
            _ => "Background mode: solid"
        };
    }

    private void SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
