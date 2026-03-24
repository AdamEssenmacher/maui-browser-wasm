using Microsoft.Maui.Controls;

namespace Maui.Controls.Sample.Pages.CollectionViewGalleries.CarouselViewGalleries;

internal sealed class CarouselViewGallery : ContentPage
{
    public CarouselViewGallery()
    {
        Title = "CarouselView Galleries (Deferred)";
        Content = new Label
        {
            Text = "CarouselView/IndicatorView gallery support is deferred in Mirage.",
            Margin = new Thickness(12),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
    }
}

public partial class ExampleTemplateCarousel : Grid
{
}
