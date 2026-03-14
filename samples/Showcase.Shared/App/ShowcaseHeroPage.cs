using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Showcase.Shared.App;

public partial class ShowcaseHeroPage : ContentPage
{
    public ShowcaseHeroPage()
    {
        Title = "Mirage Showcase";
        LoadXamlFromResource();
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

    private void LoadXamlFromResource()
    {
#pragma warning disable IL2026
        this.LoadFromXaml(typeof(ShowcaseHeroPage));
#pragma warning restore IL2026
    }
}
