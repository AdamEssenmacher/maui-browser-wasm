using Microsoft.Maui.Controls;
namespace Maui.Browser.Sample.Demo;

internal abstract class ParityFamilyPage : ContentPage
{
    protected ParityFamilyPage(string title, string automationId, params string[] probeItems)
    {
        Title = title;
        AutomationId = automationId;
        SetDynamicResource(BackgroundColorProperty, ShowcaseThemeManager.PageBackgroundColorKey);
        Resources = new ResourceDictionary
        {
            MergedDictionaries =
            {
                new ShowcaseStyles()
            }
        };

        var list = new VerticalStackLayout
        {
            Padding = new Thickness(24, 20, 24, 28),
            Spacing = 12
        };

        list.Children.Add(new Label
        {
            AutomationId = $"{automationId}-heading",
            Text = title,
            Style = GetStyle("ShowcasePageHeadingStyle")
        });

        foreach (var probe in probeItems)
        {
            list.Children.Add(new Label
            {
                Text = $"• {probe}",
                Style = GetStyle("ShowcaseMutedLabelStyle")
            });
        }

        var openConsolidated = new Button
        {
            AutomationId = $"{automationId}-open-consolidated",
            Text = "Open consolidated demo probes",
            Style = GetStyle("ShowcaseSecondaryButtonStyle")
        };
        openConsolidated.Clicked += async (_, _) => await Navigation.PushAsync(new XamlDemoPage(), false);
        list.Children.Add(openConsolidated);

        Content = new ScrollView { Content = list };
    }

    private Style GetStyle(string key)
    {
        return (Style)Resources[key];
    }
}
