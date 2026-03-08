using Counter.Fast.App;
using Microsoft.Maui.Controls;

namespace Counter.Fast.App;

internal sealed class CounterPage : ContentPage
{
    public CounterPage()
    {
        AutomationId = "counter-page-root";

        var state = new CounterState();
        BindingContext = state;

        var header = new Label
        {
            AutomationId = "counter-title",
            Text = "Mirage Counter (FAST)",
            FontSize = 26,
            Margin = new Thickness(0, 0, 0, 4)
        };

        var subtitle = new Label
        {
            AutomationId = "counter-subtitle",
            Text = "Type into Entry and click Increment.",
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var entry = new Entry
        {
            AutomationId = "counter-entry",
            Placeholder = "Type here",
            Margin = new Thickness(0, 0, 0, 10)
        };
        entry.SetBinding(Entry.TextProperty, static (CounterState state) => state.InputText, mode: BindingMode.TwoWay);

        var liveLabel = new Label
        {
            AutomationId = "counter-live-label",
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 10)
        };
        liveLabel.SetBinding(Label.TextProperty, static (CounterState state) => state.LiveText);

        var button = new Button
        {
            AutomationId = "counter-button",
            Text = "Increment",
            Margin = new Thickness(0, 0, 0, 10)
        };
        button.SetBinding(Button.CommandProperty, static (CounterState state) => state.IncrementCommand);

        var countLabel = new Label
        {
            AutomationId = "counter-count-label",
            FontSize = 18
        };
        countLabel.SetBinding(Label.TextProperty, static (CounterState state) => state.CountText);

        Content = new VerticalStackLayout
        {
            AutomationId = "counter-stack",
            Padding = new Thickness(24),
            Spacing = 8,
            Children =
            {
                header,
                subtitle,
                entry,
                liveLabel,
                button,
                countLabel
            }
        };
    }
}
