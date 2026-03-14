using Microsoft.Maui.Controls;

namespace Maui.Browser.Sample.Demo;

public partial class ShowcaseLayoutCollectionPage : ContentPage
{
    private readonly ShowcaseLayoutCollectionState _state = new();

    public ShowcaseLayoutCollectionPage()
    {
        InitializeComponent();
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

    private void OnToggleFlexDirectionClicked(object? sender, EventArgs e)
    {
        _state.ToggleFlexDirection();
    }

    private void OnToggleFlexWrapClicked(object? sender, EventArgs e)
    {
        _state.ToggleFlexWrap();
    }

    private void OnAddItemClicked(object? sender, EventArgs e)
    {
        _state.AddItem();
    }

    private void OnRemoveItemClicked(object? sender, EventArgs e)
    {
        _state.RemoveItem();
    }

    private void OnStartRefreshClicked(object? sender, EventArgs e)
    {
        _state.BeginRefresh("button");
    }

    private void OnCompleteRefreshClicked(object? sender, EventArgs e)
    {
        _state.CompleteRefresh();
    }

    private void OnToggleRefreshEnabledClicked(object? sender, EventArgs e)
    {
        _state.ToggleRefreshEnabled();
    }
}
