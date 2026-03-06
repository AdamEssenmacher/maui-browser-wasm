using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Graphics;

namespace Maui.Browser.Sample.Demo;

public partial class ParityContainersLayoutPage : ContentPage
{
    private readonly ParityContainersLayoutState _state = new();

    public ParityContainersLayoutPage()
    {
        LoadXamlFromResource();
        BindingContext = _state;

        ApplyClipMode(ClipProbeMode.None);
        ApplyShadowMode(ShadowProbeMode.Off);
        ApplyBackgroundMode(BackgroundProbeMode.Solid);
    }

    private Grid? Probe => FindByName("CoreSubstrateProbe") as Grid;

    private async void OnOpenConsolidatedClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new XamlDemoPage(), false);
    }

    private void OnClipNoneClicked(object? sender, EventArgs e)
    {
        ApplyClipMode(ClipProbeMode.None);
    }

    private void OnClipRectangleClicked(object? sender, EventArgs e)
    {
        ApplyClipMode(ClipProbeMode.Rectangle);
    }

    private void OnClipRoundRectangleClicked(object? sender, EventArgs e)
    {
        ApplyClipMode(ClipProbeMode.RoundRectangle);
    }

    private void OnClipUnsupportedClicked(object? sender, EventArgs e)
    {
        ApplyClipMode(ClipProbeMode.UnsupportedEllipse);
    }

    private void OnShadowOffClicked(object? sender, EventArgs e)
    {
        ApplyShadowMode(ShadowProbeMode.Off);
    }

    private void OnShadowOnClicked(object? sender, EventArgs e)
    {
        ApplyShadowMode(ShadowProbeMode.Solid);
    }

    private void OnShadowUnsupportedClicked(object? sender, EventArgs e)
    {
        ApplyShadowMode(ShadowProbeMode.UnsupportedGradient);
    }

    private void OnBackgroundSolidClicked(object? sender, EventArgs e)
    {
        ApplyBackgroundMode(BackgroundProbeMode.Solid);
    }

    private void OnBackgroundLinearClicked(object? sender, EventArgs e)
    {
        ApplyBackgroundMode(BackgroundProbeMode.LinearGradient);
    }

    private void OnBackgroundUnsupportedClicked(object? sender, EventArgs e)
    {
        ApplyBackgroundMode(BackgroundProbeMode.UnsupportedRadial);
    }

    private void ApplyClipMode(ClipProbeMode mode)
    {
        var probe = Probe;
        if (probe is null)
        {
            return;
        }

        switch (mode)
        {
            case ClipProbeMode.None:
                probe.Clip = null;
                break;
            case ClipProbeMode.Rectangle:
                probe.Clip = new RectangleGeometry(new Rect(18, 12, 155, 88));
                break;
            case ClipProbeMode.RoundRectangle:
                probe.Clip = new RoundRectangleGeometry(new CornerRadius(18, 10, 24, 6), new Rect(12, 8, 176, 98));
                break;
            case ClipProbeMode.UnsupportedEllipse:
                probe.Clip = new EllipseGeometry(new Point(120, 64), 68, 40);
                break;
        }

        _state.SetClipMode(mode);
    }

    private void ApplyShadowMode(ShadowProbeMode mode)
    {
        var probe = Probe;
        if (probe is null)
        {
            return;
        }

        switch (mode)
        {
            case ShadowProbeMode.Off:
                probe.ClearValue(VisualElement.ShadowProperty);
                break;
            case ShadowProbeMode.Solid:
                probe.Shadow = new Shadow
                {
                    Brush = new SolidColorBrush(Colors.Black),
                    Opacity = 0.55f,
                    Radius = 12,
                    Offset = new Point(8, 6)
                };
                break;
            case ShadowProbeMode.UnsupportedGradient:
                probe.Shadow = new Shadow
                {
                    Brush = new LinearGradientBrush(
                    [
                        new GradientStop(Colors.MidnightBlue, 0),
                        new GradientStop(Colors.SkyBlue, 1)
                    ], new Point(0, 0), new Point(1, 1)),
                    Opacity = 0.8f,
                    Radius = 12,
                    Offset = new Point(8, 6)
                };
                break;
        }

        _state.SetShadowMode(mode);
    }

    private void ApplyBackgroundMode(BackgroundProbeMode mode)
    {
        var probe = Probe;
        if (probe is null)
        {
            return;
        }

        switch (mode)
        {
            case BackgroundProbeMode.Solid:
                probe.Background = new SolidColorBrush(Colors.LightSkyBlue);
                break;
            case BackgroundProbeMode.LinearGradient:
                probe.Background = new LinearGradientBrush(
                [
                    new GradientStop(Colors.CornflowerBlue, 0),
                    new GradientStop(Colors.LightGoldenrodYellow, 0.5f),
                    new GradientStop(Colors.PaleGreen, 1)
                ], new Point(0, 0), new Point(1, 1));
                break;
            case BackgroundProbeMode.UnsupportedRadial:
                probe.Background = new RadialGradientBrush(
                [
                    new GradientStop(Colors.MediumPurple, 0),
                    new GradientStop(Colors.LightPink, 1)
                ], new Point(0.5, 0.5), 0.7f);
                break;
        }

        _state.SetBackgroundMode(mode);
    }

    private void LoadXamlFromResource()
    {
#pragma warning disable IL2026
        this.LoadFromXaml(typeof(ParityContainersLayoutPage));
#pragma warning restore IL2026
    }
}
