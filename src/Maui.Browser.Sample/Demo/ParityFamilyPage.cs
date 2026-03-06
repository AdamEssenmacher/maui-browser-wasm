using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Maui.Browser.Sample.Demo;

internal abstract class ParityFamilyPage : ContentPage
{
    protected ParityFamilyPage(string title, string automationId, params string[] probeItems)
    {
        Title = title;
        AutomationId = automationId;

        var list = new VerticalStackLayout
        {
            Padding = new Thickness(16),
            Spacing = 8
        };

        list.Children.Add(new Label
        {
            AutomationId = $"{automationId}-heading",
            Text = title,
            FontSize = 20,
            TextColor = Colors.Black
        });

        foreach (var probe in probeItems)
        {
            list.Children.Add(new Label
            {
                Text = $"• {probe}",
                FontSize = 12,
                TextColor = Colors.DarkSlateGray
            });
        }

        var openConsolidated = new Button
        {
            AutomationId = $"{automationId}-open-consolidated",
            Text = "Open consolidated demo probes",
            Padding = new Thickness(12, 8),
            TextColor = Colors.Black
        };
        openConsolidated.Clicked += async (_, _) => await Navigation.PushAsync(new XamlDemoPage(), false);
        list.Children.Add(openConsolidated);

        Content = new ScrollView { Content = list };
    }
}
