using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Maui.Browser.Sample.Demo;

internal static class DemoPageFactory
{
    public static View CreateRootView()
    {
        var state = new DemoState();

        var liveTextLabel = new Label
        {
            FontSize = 22,
            TextColor = Colors.Black
        };
        liveTextLabel.SetBinding(Label.TextProperty, static (DemoState vm) => vm.LiveText);

        var entry = new Entry
        {
            Placeholder = "Type here...",
            FontSize = 16
        };
        entry.SetBinding(Entry.TextProperty, static (DemoState vm) => vm.InputText, mode: BindingMode.TwoWay);

        var clickButton = new Button
        {
            Text = "Click to increment count",
            Padding = new Thickness(12, 8),
            TextColor = Colors.Black
        };
        clickButton.Clicked += (_, _) => state.ClickCount++;

        var clickSummaryLabel = new Label
        {
            FontSize = 14,
            TextColor = Colors.DarkSlateGray
        };
        clickSummaryLabel.SetBinding(Label.TextProperty, static (DemoState vm) => vm.ClickSummary);

        var viewportLabel = new Label
        {
            FontSize = 12,
            TextColor = Colors.DarkSlateBlue
        };
        viewportLabel.SetBinding(Label.TextProperty, static (DemoState vm) => vm.ViewportText);

        var visibilityButton = new Button
        {
            Text = "Toggle probe visibility",
            Padding = new Thickness(12, 8),
            TextColor = Colors.Black
        };
        visibilityButton.Clicked += (_, _) => state.ToggleProbeVisibility();

        var opacityButton = new Button
        {
            Text = "Cycle probe opacity",
            Padding = new Thickness(12, 8),
            TextColor = Colors.Black
        };
        opacityButton.Clicked += (_, _) => state.CycleProbeOpacity();

        var probeLabel = new Label
        {
            Text = "Visibility and opacity probe",
            FontSize = 15,
            TextColor = Colors.DarkBlue
        };
        probeLabel.SetBinding(VisualElement.IsVisibleProperty, static (DemoState vm) => vm.ProbeIsVisible);
        probeLabel.SetBinding(VisualElement.OpacityProperty, static (DemoState vm) => vm.ProbeOpacity);

        var probeVisibilityLabel = new Label
        {
            FontSize = 12,
            TextColor = Colors.DarkSlateGray
        };
        probeVisibilityLabel.SetBinding(Label.TextProperty, static (DemoState vm) => vm.ProbeVisibilitySummary);

        var probeOpacityLabel = new Label
        {
            FontSize = 12,
            TextColor = Colors.DarkSlateGray
        };
        probeOpacityLabel.SetBinding(Label.TextProperty, static (DemoState vm) => vm.ProbeOpacitySummary);

        var layout = new VerticalStackLayout
        {
            Spacing = 10,
            Padding = new Thickness(16),
            Children =
            {
                liveTextLabel,
                entry,
                clickButton,
                clickSummaryLabel,
                viewportLabel,
                visibilityButton,
                opacityButton,
                probeLabel,
                probeVisibilityLabel,
                probeOpacityLabel
            }
        };

        layout.SizeChanged += (_, _) =>
        {
            state.ViewportText = $"Viewport/layout size: {layout.Width:0} x {layout.Height:0}";
        };

        var root = new ContentView
        {
            BindingContext = state,
            Content = layout
        };

        return root;
    }
}
