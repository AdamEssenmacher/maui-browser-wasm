using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Graphics;

namespace Maui.Browser.Sample.Demo;

public partial class XamlDemoPage : ContentPage
{
    private readonly DemoState _state = new();

    public XamlDemoPage()
    {
        LoadXamlFromResource();

        BindingContext = _state;

        var rootLayout = FindByName("RootLayout") as VerticalStackLayout;
        if (rootLayout is not null)
        {
            rootLayout.SizeChanged += (_, _) =>
            {
                _state.ViewportText = $"Viewport/layout size: {rootLayout.Width:0} x {rootLayout.Height:0}";
            };
        }
    }

    private void OnIncrementClicked(object? sender, EventArgs e)
    {
        _state.ClickCount++;
    }

    private void OnToggleVisibilityClicked(object? sender, EventArgs e)
    {
        _state.ToggleProbeVisibility();
    }

    private void OnCycleOpacityClicked(object? sender, EventArgs e)
    {
        _state.CycleProbeOpacity();
    }

    private void OnStressClicked(object? sender, EventArgs e)
    {
        var stressHost = FindByName("StressHost") as VerticalStackLayout;
        if (stressHost is null)
        {
            return;
        }

        RunLeakStress(stressHost);
        _state.RecordStressRun();
    }

    private void LoadXamlFromResource()
    {
#pragma warning disable IL2026
        this.LoadFromXaml(typeof(XamlDemoPage));
#pragma warning restore IL2026
    }

    private static void RunLeakStress(VerticalStackLayout stressHost)
    {
        const int cycleCount = 12;
        const int controlsPerCycle = 6;

        for (var cycle = 0; cycle < cycleCount; cycle++)
        {
            var cycleLayout = new VerticalStackLayout
            {
                Spacing = 2
            };

            for (var i = 0; i < controlsPerCycle; i++)
            {
                var row = new HorizontalStackLayout
                {
                    Spacing = 4
                };

                var probeLabel = new Label
                {
                    Text = $"Transient {cycle}-{i}",
                    FontSize = 11,
                    TextColor = Colors.DarkGray
                };

                var probeButton = new Button
                {
                    Text = $"Probe {cycle}-{i}",
                    Padding = new Thickness(8, 4),
                    FontSize = 11,
                    TextColor = Colors.Black
                };
                probeButton.Clicked += static (_, _) => { };

                row.Children.Add(probeLabel);
                row.Children.Add(probeButton);
                cycleLayout.Children.Add(row);
            }

            stressHost.Children.Add(cycleLayout);
            stressHost.Children.Remove(cycleLayout);
        }

        stressHost.Clear();
    }
}
