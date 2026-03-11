using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Showcase.Shared.App;

public partial class ShowcaseNavigationLabPage : ContentPage
{
    public ShowcaseNavigationLabPage()
    {
        LoadXamlFromResource();
    }

    private async void OnPushProject101Clicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        await NavigateToProjectAsync("101");
    }

    private async void OnPushProject202Clicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        await NavigateToProjectAsync("202");
    }

    private async void OnPopClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        if (Navigation.NavigationStack.Count <= 1)
        {
            return;
        }

        await Navigation.PopAsync(animated: false);
    }

    private async void OnPopToRootClicked(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        await Navigation.PopToRootAsync(animated: false);
    }

    private async Task NavigateToProjectAsync(string projectId)
    {
        await Navigation.PushAsync(new ShowcaseProjectPage
        {
            ProjectId = projectId
        }, animated: false);
    }

    private void LoadXamlFromResource()
    {
#pragma warning disable IL2026
        this.LoadFromXaml(typeof(ShowcaseNavigationLabPage));
#pragma warning restore IL2026
    }
}
