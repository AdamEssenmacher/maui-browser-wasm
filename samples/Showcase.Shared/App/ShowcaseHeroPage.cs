using Microsoft.Maui.Controls;

namespace Showcase.Shared.App;

public partial class ShowcaseHeroPage : ContentPage
{
    public ShowcaseHeroPage()
    {
        Title = "Mirage Showcase";
        InitializeComponent();
    }

    private async void OnOpenEssentialsLabClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        await Navigation.PushAsync(new ShowcaseEssentialsLabPage(), animated: false);
    }

    private async void OnCommandScrollToLayoutLabClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        await Navigation.PushAsync(new ShowcaseLayoutLabPage(), animated: false);
    }

    private async void OnOpenNavigationLabPageClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        await Navigation.PushAsync(new ShowcaseNavigationLabPage(), animated: false);
    }

    private async void OnOpenProjectRoutePageClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        await Navigation.PushAsync(new ShowcaseProjectPage
        {
            ProjectId = "42"
        }, animated: false);
    }

    private async void OnOpenXamlLabClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        await Navigation.PushAsync(new ShowcaseXamlLabPage(), animated: false);
    }
}
