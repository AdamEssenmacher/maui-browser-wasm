using Maui.Controls.Sample.Pages;
using Maui.Controls.Sample.Pages.CollectionViewGalleries;
using Microsoft.Maui.Controls;
using Xunit;

namespace Mirage.Integration.Tests;

public sealed class ControlsSampleGalleryBuilderTests
{
    [Fact]
    public void NavButton_CollectionViewDeferredButtons_AreHiddenAndDisabled()
    {
        var navigation = new NavigationPage(new CollectionViewGallery()).Navigation;

        var button = GalleryBuilder.NavButton("Filter Items", static () => new ContentPage(), navigation);

        Assert.Equal("controls-sample-collectionviewgallery-open-filter-items", button.AutomationId);
        Assert.False(button.IsVisible);
        Assert.False(button.IsEnabled);
    }

    [Fact]
    public void NavButton_CollectionViewSupportedButtons_UseCollectionPrefix()
    {
        var navigation = new NavigationPage(new CollectionViewGallery()).Navigation;

        var button = GalleryBuilder.NavButton("Default Text Galleries", static () => new ContentPage(), navigation);

        Assert.Equal("controls-sample-collectionviewgallery-open-default-text-galleries", button.AutomationId);
        Assert.True(button.IsVisible);
        Assert.True(button.IsEnabled);
    }

    [Fact]
    public void NavButton_BorderAndMapsPrefixesRemainStable()
    {
        var navigation = new NavigationPage(new ContentPage()).Navigation;

        var borderButton = GalleryBuilder.NavButton("Border Playground", static () => new ContentPage(), navigation);
        var mapButton = GalleryBuilder.NavButton("Map Type", static () => new ContentPage(), navigation);

        Assert.Equal("controls-sample-borderpage-open-border-playground", borderButton.AutomationId);
        Assert.Equal("controls-sample-mapsgallery-open-map-type", mapButton.AutomationId);
        Assert.True(borderButton.IsVisible);
        Assert.True(mapButton.IsVisible);
    }

    [Fact]
    public void NavButton_NonCollectionContext_UsesGenericPrefix()
    {
        var navigation = new NavigationPage(new ContentPage()).Navigation;

        var button = GalleryBuilder.NavButton("Custom Gallery", static () => new ContentPage(), navigation);

        Assert.Equal("controls-sample-gallery-open-custom-gallery", button.AutomationId);
        Assert.True(button.IsVisible);
        Assert.True(button.IsEnabled);
    }
}
