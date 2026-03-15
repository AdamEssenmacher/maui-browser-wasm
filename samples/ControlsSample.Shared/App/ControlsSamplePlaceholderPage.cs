using Microsoft.Maui.Controls;

namespace ControlsSample.Shared.App;

public sealed class ControlsSamplePlaceholderPage : ContentPage
{
    public ControlsSamplePlaceholderPage(string title, string message)
    {
        Title = title;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20, 16),
                Spacing = 12,
                Children =
                {
                    new Label
                    {
                        Text = title,
                        FontSize = 22,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = message,
                        FontSize = 14
                    }
                }
            }
        };
    }
}
