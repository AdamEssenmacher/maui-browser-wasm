using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Maui.Browser.Sample.Demo;

public partial class ShowcaseTextInputPage : ContentPage
{
    private readonly ShowcaseTextInputState _state = new();
    private SearchBar? SearchProbeElement => FindByName("SearchProbe") as SearchBar;

    public ShowcaseTextInputPage()
    {
        LoadXamlFromResource();
        BindingContext = _state;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync(false);
    }

    private void OnToggleThemeClicked(object? sender, EventArgs e)
    {
        if (Application.Current is null)
        {
            return;
        }

        ShowcaseThemeManager.Toggle(Application.Current);
    }

    private void OnEntryCompleted(object? sender, EventArgs e)
    {
        _state.RecordEntryCompleted();
    }

    private void OnSearchPressed(object? sender, EventArgs e)
    {
        _state.RecordSearch(SearchProbeElement?.Text);
    }

    private void OnApplyProgrammaticClicked(object? sender, EventArgs e)
    {
        _state.ApplyProgrammaticPreset();
        ReapplySearchVisibility();
    }

    private async void OnOpenFormMediaClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new ShowcaseFormMediaPage(), false);
    }

    private async void OnOpenLayoutCollectionClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new ShowcaseLayoutCollectionPage(), false);
    }

    private void ReapplySearchVisibility()
    {
        var searchBar = SearchProbeElement;
        if (searchBar is null || !searchBar.IsVisible)
        {
            return;
        }

        searchBar.IsVisible = false;
        searchBar.IsVisible = true;
    }

    private void LoadXamlFromResource()
    {
#pragma warning disable IL2026
        this.LoadFromXaml(typeof(ShowcaseTextInputPage));
#pragma warning restore IL2026
    }
}
