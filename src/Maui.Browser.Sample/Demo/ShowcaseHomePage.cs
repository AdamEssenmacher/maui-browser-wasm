using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Graphics;

namespace Maui.Browser.Sample.Demo;

public partial class ShowcaseHomePage : ContentPage
{
    private readonly ShowcaseHomeState _state = new();
    private VerticalStackLayout? ControlCatalogRowsHostElement => FindByName("ControlCatalogRowsHost") as VerticalStackLayout;

    public ShowcaseHomePage()
    {
        LoadXamlFromResource();
        BindingContext = _state;
        _state.ApplyTheme(ShowcaseThemeManager.CurrentMode, incrementToggle: false);
        BuildControlCatalogRows();
    }

    private void OnToggleThemeClicked(object? sender, EventArgs e)
    {
        if (Application.Current is null)
        {
            return;
        }

        ShowcaseThemeManager.Toggle(Application.Current);
        _state.ApplyTheme(ShowcaseThemeManager.CurrentMode, incrementToggle: true);
    }

    private async void OnOpenCatalogClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new ParityCatalogPage(), false);
    }

    private async void OnOpenTextInputClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new ShowcaseTextInputPage(), false);
    }

    private async void OnOpenFormMediaClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new ShowcaseFormMediaPage(), false);
    }

    private async void OnOpenLayoutCollectionClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new ShowcaseLayoutCollectionPage(), false);
    }

    private void OnPrimaryActionClicked(object? sender, EventArgs e)
    {
        _state.RecordPrimaryAction();
    }

    private void OnTogglePrimaryEnabledClicked(object? sender, EventArgs e)
    {
        _state.TogglePrimaryActionEnabled();
    }

    private void LoadXamlFromResource()
    {
#pragma warning disable IL2026
        this.LoadFromXaml(typeof(ShowcaseHomePage));
#pragma warning restore IL2026
    }

    private void BuildControlCatalogRows()
    {
        var host = ControlCatalogRowsHostElement;
        if (host is null)
        {
            return;
        }

        host.Children.Clear();

        foreach (var category in ShowcaseControlCatalog.All.GroupBy(static spec => spec.Category, StringComparer.Ordinal))
        {
            host.Children.Add(new Label
            {
                Text = category.Key,
                Style = (Style)Resources["ShowcaseSectionHeadingStyle"],
                FontSize = 16
            });

            foreach (var spec in category)
            {
                var rowGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    ColumnSpacing = 12,
                    Margin = new Thickness(0, 0, 0, 4)
                };

                var summaryLabel = new Label
                {
                    AutomationId = $"showcase-control-list-row-{spec.Slug}",
                    Text = $"{spec.TypeName} — {spec.StatusText} — {(spec.IsImplemented ? "implemented" : "not implemented yet")}",
                    Style = (Style)Resources["ShowcaseMutedLabelStyle"],
                    TextColor = ResolveStatusColor(spec.Status)
                };

                var openButton = new Button
                {
                    AutomationId = spec.OpenButtonAutomationId,
                    Text = "Open",
                    Style = (Style)Resources["ShowcaseSecondaryButtonStyle"],
                    HorizontalOptions = LayoutOptions.End
                };

                var capturedSpec = spec;
                openButton.Clicked += async (_, _) =>
                {
                    if (capturedSpec.Slug == "label")
                    {
                        await Navigation.PushAsync(new ParityLabelPage(), false);
                        return;
                    }

                    await Navigation.PushAsync(new ShowcaseControlDemoPage(capturedSpec), false);
                };

                rowGrid.Children.Add(summaryLabel);
                Grid.SetColumn(summaryLabel, 0);

                rowGrid.Children.Add(openButton);
                Grid.SetColumn(openButton, 1);

                host.Children.Add(rowGrid);
            }
        }
    }

    private static Color ResolveStatusColor(ShowcaseControlStatus status)
    {
        return status switch
        {
            ShowcaseControlStatus.Complete => Color.FromArgb("#166534"),
            ShowcaseControlStatus.Partial => Color.FromArgb("#6D28D9"),
            ShowcaseControlStatus.GapDocumented => Color.FromArgb("#B45309"),
            _ => Color.FromArgb("#B91C1C")
        };
    }
}
