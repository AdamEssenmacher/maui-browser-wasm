using Microsoft.Maui.Controls;

namespace Maui.Controls.Sample.Pages;

public sealed class NavigationGallery : ContentPage
{
    public NavigationGallery()
    {
        Title = "Navigation Gallery (Stub)";

        Content = new VerticalStackLayout
        {
            Padding = new Thickness(20, 16),
            Spacing = 10,
            Children =
            {
                new Label
                {
                    Text = "NavigationGallery is not wired in this wrapper yet.",
                    FontSize = 14
                }
            }
        };
    }
}
