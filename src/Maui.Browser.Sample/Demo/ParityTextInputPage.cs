using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Maui.Browser.Sample.Demo;

public partial class ParityTextInputPage : ContentPage
{
    private readonly ParityTextInputState _state = new();
    private Label? LabelProbeElement => FindByName("LabelProbe") as Label;
    private SearchBar? SearchProbeElement => FindByName("SearchProbe") as SearchBar;

    public ParityTextInputPage()
    {
        LoadXamlFromResource();
        BindingContext = _state;
        ApplyLabelMode();
    }

    private async void OnOpenConsolidatedClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new XamlDemoPage(), false);
    }

    private void OnToggleLabelModeClicked(object? sender, EventArgs e)
    {
        _state.ToggleLabelMode();
        ApplyLabelMode();
    }

    private void OnEntryCompleted(object? sender, EventArgs e)
    {
        _state.RecordEntryCompleted();
    }

    private void OnSearchPressed(object? sender, EventArgs e)
    {
        _state.RecordSearchPressed(SearchProbeElement?.Text);
    }

    private void OnApplyProgrammaticUpdateClicked(object? sender, EventArgs e)
    {
        _state.ApplyProgrammaticUpdate();
        ApplyLabelMode();
        ReapplySearchBarVisibility();
    }

    private void ApplyLabelMode()
    {
        var labelProbe = LabelProbeElement;
        if (labelProbe is null)
        {
            return;
        }

        if (_state.LabelTruncationEnabled)
        {
            labelProbe.LineBreakMode = LineBreakMode.TailTruncation;
            labelProbe.MaxLines = 1;
            return;
        }

        labelProbe.LineBreakMode = LineBreakMode.WordWrap;
        labelProbe.MaxLines = 3;
    }

    private void LoadXamlFromResource()
    {
#pragma warning disable IL2026
        this.LoadFromXaml(typeof(ParityTextInputPage));
#pragma warning restore IL2026
    }

    private void ReapplySearchBarVisibility()
    {
        var searchBar = SearchProbeElement;
        if (searchBar is null || !searchBar.IsVisible)
        {
            return;
        }

        var originalVisibility = searchBar.IsVisible;
        searchBar.IsVisible = false;
        searchBar.IsVisible = originalVisibility;
    }
}
