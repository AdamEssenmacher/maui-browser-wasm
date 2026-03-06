using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Maui.Browser.Sample.Demo;

internal sealed class ParityCatalogPage : ContentPage
{
    public ParityCatalogPage()
    {
        Title = "MAUI Browser Parity Catalog";
        AutomationId = "parity-page-catalog";

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 10,
                Children =
                {
                    new Label
                    {
                        AutomationId = "parity-catalog-heading",
                        Text = "Parity Catalog",
                        FontSize = 24,
                        TextColor = Colors.Black
                    },
                    new Label
                    {
                        AutomationId = "parity-catalog-summary",
                        Text = "Use these pages to validate control/layout parity waves. The consolidated demo remains available for current smoke tests.",
                        FontSize = 12,
                        TextColor = Colors.DarkSlateGray
                    },
                    CreateNavigationButton(
                        "Open Consolidated Demo",
                        "parity-open-consolidated-demo",
                        static () => new XamlDemoPage()),
                    CreateNavigationButton(
                        "Text/Input Family",
                        "parity-open-text-input",
                        static () => new ParityTextInputPage()),
                    CreateNavigationButton(
                        "Pickers/Date-Time Family",
                        "parity-open-pickers-datetime",
                        static () => new ParityPickersDateTimePage()),
                    CreateNavigationButton(
                        "Media/Visual Family",
                        "parity-open-media-visual",
                        static () => new ParityMediaVisualPage()),
                    CreateNavigationButton(
                        "Collection/Templated Family",
                        "parity-open-collection-templated",
                        static () => new ParityCollectionTemplatedPage()),
                    CreateNavigationButton(
                        "Containers/Layout Family",
                        "parity-open-containers-layout",
                        static () => new ParityContainersLayoutPage()),
                    CreateNavigationButton(
                        "Navigation/Window Family",
                        "parity-open-navigation-window",
                        static () => new ParityNavigationWindowPage())
                }
            }
        };
    }

    private Button CreateNavigationButton(string text, string automationId, Func<Page> createPage)
    {
        var button = new Button
        {
            Text = text,
            AutomationId = automationId,
            Padding = new Thickness(12, 8),
            TextColor = Colors.Black
        };

        button.Clicked += async (_, _) => await Navigation.PushAsync(createPage(), false);
        return button;
    }
}
