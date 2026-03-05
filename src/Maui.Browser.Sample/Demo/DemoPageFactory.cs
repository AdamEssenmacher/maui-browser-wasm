using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Maui.Browser.Sample.Demo;

internal static class DemoPageFactory
{
    public static ContentPage CreateMainPage()
    {
        var state = new DemoState();

        var liveTextLabel = new Label
        {
            FontSize = 22,
            TextColor = Colors.Black,
            AutomationId = "live-text-label"
        };
        liveTextLabel.SetBinding(Label.TextProperty, static (DemoState vm) => vm.LiveText);

        var hotReloadLabel = new Label
        {
            FontSize = 12,
            TextColor = Colors.DarkOliveGreen,
            AutomationId = "hot-reload-label"
        };
        hotReloadLabel.SetBinding(Label.TextProperty, static (DemoState vm) => vm.HotReloadSummary);

        var entry = new Entry
        {
            Placeholder = "Type here...",
            FontSize = 16,
            AutomationId = "live-input-entry"
        };
        entry.SetBinding(Entry.TextProperty, static (DemoState vm) => vm.InputText, mode: BindingMode.TwoWay);

        var clickButton = new Button
        {
            Text = "Click to increment count",
            Padding = new Thickness(12, 8),
            TextColor = Colors.Black,
            AutomationId = "click-button"
        };
        clickButton.Clicked += (_, _) => state.ClickCount++;

        var clickSummaryLabel = new Label
        {
            FontSize = 14,
            TextColor = Colors.DarkSlateGray,
            AutomationId = "click-summary-label"
        };
        clickSummaryLabel.SetBinding(Label.TextProperty, static (DemoState vm) => vm.ClickSummary);

        var viewportLabel = new Label
        {
            FontSize = 12,
            TextColor = Colors.DarkSlateBlue,
            AutomationId = "viewport-label"
        };
        viewportLabel.SetBinding(Label.TextProperty, static (DemoState vm) => vm.ViewportText);

        var visibilityButton = new Button
        {
            Text = "Toggle probe visibility",
            Padding = new Thickness(12, 8),
            TextColor = Colors.Black,
            AutomationId = "visibility-button"
        };
        visibilityButton.Clicked += (_, _) => state.ToggleProbeVisibility();

        var opacityButton = new Button
        {
            Text = "Cycle probe opacity",
            Padding = new Thickness(12, 8),
            TextColor = Colors.Black,
            AutomationId = "opacity-button"
        };
        opacityButton.Clicked += (_, _) => state.CycleProbeOpacity();

        var probeLabel = new Label
        {
            Text = "Visibility and opacity probe",
            FontSize = 15,
            TextColor = Colors.DarkBlue,
            AutomationId = "probe-label"
        };
        probeLabel.SetBinding(VisualElement.IsVisibleProperty, static (DemoState vm) => vm.ProbeIsVisible);
        probeLabel.SetBinding(VisualElement.OpacityProperty, static (DemoState vm) => vm.ProbeOpacity);

        var probeVisibilityLabel = new Label
        {
            FontSize = 12,
            TextColor = Colors.DarkSlateGray,
            AutomationId = "probe-visibility-label"
        };
        probeVisibilityLabel.SetBinding(Label.TextProperty, static (DemoState vm) => vm.ProbeVisibilitySummary);

        var probeOpacityLabel = new Label
        {
            FontSize = 12,
            TextColor = Colors.DarkSlateGray,
            AutomationId = "probe-opacity-label"
        };
        probeOpacityLabel.SetBinding(Label.TextProperty, static (DemoState vm) => vm.ProbeOpacitySummary);

        var stressSummaryLabel = new Label
        {
            FontSize = 12,
            TextColor = Colors.DarkSlateGray,
            AutomationId = "stress-summary-label"
        };
        stressSummaryLabel.SetBinding(Label.TextProperty, static (DemoState vm) => vm.StressSummary);

        var stressHost = new VerticalStackLayout
        {
            Spacing = 2,
            AutomationId = "stress-host"
        };

        var stressButton = new Button
        {
            Text = "Run leak stress",
            Padding = new Thickness(12, 8),
            TextColor = Colors.Black,
            AutomationId = "stress-button"
        };
        stressButton.Clicked += (_, _) =>
        {
            RunLeakStress(stressHost);
            state.RecordStressRun();
        };

        var layout = new VerticalStackLayout
        {
            Spacing = 10,
            Padding = new Thickness(16),
            Children =
            {
                liveTextLabel,
                hotReloadLabel,
                entry,
                clickButton,
                clickSummaryLabel,
                viewportLabel,
                visibilityButton,
                opacityButton,
                probeLabel,
                probeVisibilityLabel,
                probeOpacityLabel,
                stressButton,
                stressSummaryLabel,
                stressHost
            }
        };

        layout.SizeChanged += (_, _) =>
        {
            state.ViewportText = $"Viewport/layout size: {layout.Width:0} x {layout.Height:0}";
        };

        return new ContentPage
        {
            Title = "MAUI Browser DOM Demo",
            BindingContext = state,
            Content = layout
        };
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
