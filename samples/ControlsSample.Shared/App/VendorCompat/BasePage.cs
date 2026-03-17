using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Maui.Controls.Sample.Models;

namespace Maui.Controls.Sample.Pages.Base;

// Minimal upstream-compatible base page used by selectively imported vendor pages.
// We intentionally avoid the full upstream dependency surface (navigation models/settings shell)
// while preserving page type identity expected by XAML roots.
public class BasePage : ContentPage
{
    private SectionModel? _selectedItem;

    public BasePage()
    {
        NavigateCommand = new Command(async () => await PreparePageAsync());

        if (Application.Current?.Resources is null)
        {
            return;
        }

        var resources = Application.Current.Resources;
        if (resources.TryGetValue("LightBackgroundColor", out var light)
            && resources.TryGetValue("DarkBackgroundColor", out var dark)
            && light is Color lightColor
            && dark is Color darkColor)
        {
            this.SetAppThemeColor(BackgroundColorProperty, lightColor, darkColor);
        }
    }

    public Command NavigateCommand { get; }

    public SectionModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (ReferenceEquals(_selectedItem, value))
            {
                return;
            }

            _selectedItem = value;
            OnPropertyChanged(nameof(SelectedItem));
        }
    }

    private async Task PreparePageAsync()
    {
        if (SelectedItem is not { } section)
        {
            return;
        }

        var nextPage = CreatePage(section);
        SelectedItem = null;
        if (nextPage is null)
        {
            return;
        }

        await Navigation.PushAsync(nextPage, animated: false);
    }

    private static Page? CreatePage(SectionModel section)
    {
        if (!typeof(Page).IsAssignableFrom(section.Type))
        {
            return null;
        }

        if (Activator.CreateInstance(section.Type) is not Page page)
        {
            return null;
        }

        if (section.ViewModel is not null)
        {
            page.BindingContext = section.ViewModel;
        }

        page.Title = section.Title;
        page.AutomationId = $"controls-sample-page-{section.Type.Name.ToLowerInvariant()}";
        return page;
    }
}
