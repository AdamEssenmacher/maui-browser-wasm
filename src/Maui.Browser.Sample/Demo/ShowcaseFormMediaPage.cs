using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Graphics;

namespace Maui.Browser.Sample.Demo;

public partial class ShowcaseFormMediaPage : ContentPage
{
    private readonly ShowcaseFormMediaState _state = new();
    private Image? MediaImageElement => FindByName("MediaImage") as Image;
    private ImageButton? MediaImageButtonElement => FindByName("MediaImageButton") as ImageButton;

    public ShowcaseFormMediaPage()
    {
        LoadXamlFromResource();
        BindingContext = _state;
        ApplyCurrentImageSource();
        ApplyCurrentAspect();
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

    private void OnCycleSourceClicked(object? sender, EventArgs e)
    {
        _state.CycleSourceMode();
        ApplyCurrentImageSource();
    }

    private void OnToggleAspectClicked(object? sender, EventArgs e)
    {
        _state.ToggleAspect();
        ApplyCurrentAspect();
    }

    private void OnImageButtonClicked(object? sender, EventArgs e)
    {
        _state.RecordImageButtonClick();
    }

    private void OnApplyProgrammaticClicked(object? sender, EventArgs e)
    {
        _state.ApplyProgrammaticPreset();
    }

    private async void OnOpenLayoutCollectionClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new ShowcaseLayoutCollectionPage(), false);
    }

    private void ApplyCurrentImageSource()
    {
        ImageSource source = _state.SourceMode switch
        {
            ShowcaseFormMediaState.MediaSourceMode.Uri => new UriImageSource
            {
                Uri = new Uri(
                    "data:image/svg+xml;utf8," + Uri.EscapeDataString(
                        "<svg xmlns='http://www.w3.org/2000/svg' width='220' height='150' viewBox='0 0 220 150'><defs><linearGradient id='g' x1='0' y1='0' x2='1' y2='1'><stop offset='0%' stop-color='%230F766E'/><stop offset='100%' stop-color='%23EA580C'/></linearGradient></defs><rect width='220' height='150' fill='url(%23g)'/><text x='110' y='82' text-anchor='middle' fill='white' font-size='24' font-family='Trebuchet MS'>URI SOURCE</text></svg>"),
                    UriKind.Absolute)
            },
            ShowcaseFormMediaState.MediaSourceMode.File => new FileImageSource
            {
                File = "assets/showcase/hero-grid.svg"
            },
            _ => new FontImageSource
            {
                Glyph = "★",
                FontFamily = "serif",
                Color = Color.FromArgb("#EA580C"),
                Size = 34
            }
        };

        if (MediaImageElement is not null)
        {
            MediaImageElement.Source = source;
        }

        if (MediaImageButtonElement is not null)
        {
            MediaImageButtonElement.Source = source;
        }
    }

    private void ApplyCurrentAspect()
    {
        if (MediaImageElement is not null)
        {
            MediaImageElement.Aspect = _state.CurrentAspect;
        }

        if (MediaImageButtonElement is not null)
        {
            MediaImageButtonElement.Aspect = _state.CurrentAspect;
        }
    }

    private void LoadXamlFromResource()
    {
#pragma warning disable IL2026
        this.LoadFromXaml(typeof(ShowcaseFormMediaPage));
#pragma warning restore IL2026
    }
}
