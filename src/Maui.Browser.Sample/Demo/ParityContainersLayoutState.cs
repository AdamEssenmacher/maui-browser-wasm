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

internal enum FlexDirectionProbeMode
{
    Row,
    Column
}

internal enum FlexWrapProbeMode
{
    NoWrap,
    Wrap
}

internal enum FlexJustifyProbeMode
{
    Start,
    SpaceBetween
}

internal enum FlexAlignProbeMode
{
    StretchStart,
    CenterAround
}

internal enum FlexItemPresetMode
{
    Default,
    Emphasis
}

internal sealed class ParityContainersLayoutState : INotifyPropertyChanged
{
    private string _clipSummary = "Clip mode: none";
    private string _shadowSummary = "Shadow mode: off";
    private string _backgroundSummary = "Background mode: solid";
    private string _flexDirectionSummary = "Flex direction: row";
    private string _flexWrapSummary = "Flex wrap: no-wrap";
    private string _flexJustifySummary = "Flex justify: start";
    private string _flexAlignSummary = "Flex align: items=stretch, content=start";
    private string _flexItemPresetSummary = "Flex item preset: default";

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

    public string FlexDirectionSummary
    {
        get => _flexDirectionSummary;
        private set => SetProperty(ref _flexDirectionSummary, value);
    }

    public string FlexWrapSummary
    {
        get => _flexWrapSummary;
        private set => SetProperty(ref _flexWrapSummary, value);
    }

    public string FlexJustifySummary
    {
        get => _flexJustifySummary;
        private set => SetProperty(ref _flexJustifySummary, value);
    }

    public string FlexAlignSummary
    {
        get => _flexAlignSummary;
        private set => SetProperty(ref _flexAlignSummary, value);
    }

    public string FlexItemPresetSummary
    {
        get => _flexItemPresetSummary;
        private set => SetProperty(ref _flexItemPresetSummary, value);
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

    public void SetFlexDirectionMode(FlexDirectionProbeMode mode)
    {
        FlexDirectionSummary = mode switch
        {
            FlexDirectionProbeMode.Column => "Flex direction: column",
            _ => "Flex direction: row"
        };
    }

    public void SetFlexWrapMode(FlexWrapProbeMode mode)
    {
        FlexWrapSummary = mode switch
        {
            FlexWrapProbeMode.Wrap => "Flex wrap: wrap",
            _ => "Flex wrap: no-wrap"
        };
    }

    public void SetFlexJustifyMode(FlexJustifyProbeMode mode)
    {
        FlexJustifySummary = mode switch
        {
            FlexJustifyProbeMode.SpaceBetween => "Flex justify: space-between",
            _ => "Flex justify: start"
        };
    }

    public void SetFlexAlignMode(FlexAlignProbeMode mode)
    {
        FlexAlignSummary = mode switch
        {
            FlexAlignProbeMode.CenterAround => "Flex align: items=center, content=space-around",
            _ => "Flex align: items=stretch, content=start"
        };
    }

    public void SetFlexItemPresetMode(FlexItemPresetMode mode)
    {
        FlexItemPresetSummary = mode switch
        {
            FlexItemPresetMode.Emphasis => "Flex item preset: emphasis (grow/shrink/order/basis/align-self)",
            _ => "Flex item preset: default"
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
