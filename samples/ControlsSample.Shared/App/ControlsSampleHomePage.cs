using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace ControlsSample.Shared.App;

public sealed class ControlsSampleHomePage : ContentPage
{
    private static readonly Color PageBackgroundLight = Color.FromArgb("#F5F7FB");
    private static readonly Color PageBackgroundDark = Color.FromArgb("#14171D");
    private static readonly Color CardBackgroundLight = Color.FromArgb("#FFFFFF");
    private static readonly Color CardBackgroundDark = Color.FromArgb("#20242D");
    private static readonly Color CardStrokeLight = Color.FromArgb("#D7DEEA");
    private static readonly Color CardStrokeDark = Color.FromArgb("#333A49");
    private static readonly Color PrimaryTextLight = Color.FromArgb("#10131A");
    private static readonly Color PrimaryTextDark = Color.FromArgb("#E8EDF8");
    private static readonly Color SecondaryTextLight = Color.FromArgb("#4A5568");
    private static readonly Color SecondaryTextDark = Color.FromArgb("#B9C3D8");
    private static readonly Color AccentLight = Color.FromArgb("#2563EB");
    private static readonly Color AccentDark = Color.FromArgb("#3B82F6");

    private readonly IServiceProvider _services;
    private readonly IReadOnlyList<ControlsSampleEntry> _entries;

    public ControlsSampleHomePage(IServiceProvider services)
    {
        _services = services;

        var policy = (services.GetService(typeof(IControlsSampleVisibilityPolicy)) as IControlsSampleVisibilityPolicy)
                     ?? new NativeControlsSampleVisibilityPolicy();

        _entries = ControlsSampleCatalog.All.Where(policy.IsVisible).ToArray();

        Title = "Controls.Sample (Mirage Wrapper)";
        AutomationId = "controls-sample-home";
        this.SetAppTheme(BackgroundColorProperty, PageBackgroundLight, PageBackgroundDark);

        Content = BuildContent();
    }

    private View BuildContent()
    {
        var stack = new VerticalStackLayout
        {
            Padding = new Thickness(20, 16),
            Spacing = 12
        };

        var title = new Label
        {
            Text = "Upstream Controls.Sample wrapper",
            FontSize = 24,
            AutomationId = "controls-sample-home-title"
        };
        SetThemeValue(title, Label.TextColorProperty, PrimaryTextLight, PrimaryTextDark);
        stack.Children.Add(title);

        var subtitle = new Label
        {
            Text = "Browser host shows Mirage-runnable pages only. Native host includes Mirage-unsupported pages unless externally blocked.",
            FontSize = 14,
            AutomationId = "controls-sample-home-subtitle"
        };
        SetThemeValue(subtitle, Label.TextColorProperty, SecondaryTextLight, SecondaryTextDark);
        stack.Children.Add(subtitle);

        if (_entries.Count == 0)
        {
            var empty = new Label
            {
                Text = "No pages are visible for the active host policy.",
                AutomationId = "controls-sample-empty-state"
            };
            SetThemeValue(empty, Label.TextColorProperty, SecondaryTextLight, SecondaryTextDark);
            stack.Children.Add(empty);

            return new ScrollView { Content = stack };
        }

        foreach (var entry in _entries)
        {
            stack.Children.Add(BuildEntryCard(entry));
        }

        return new ScrollView { Content = stack };
    }

    private View BuildEntryCard(ControlsSampleEntry entry)
    {
        var content = new VerticalStackLayout
        {
            Spacing = 6
        };

        var cardBody = new ContentView
        {
            Padding = new Thickness(12),
            Content = content
        };
        SetThemeValue(cardBody, VisualElement.BackgroundColorProperty, CardBackgroundLight, CardBackgroundDark);

        var card = new ContentView
        {
            Padding = new Thickness(1),
            Content = cardBody
        };
        SetThemeValue(card, VisualElement.BackgroundColorProperty, CardStrokeLight, CardStrokeDark);

        var cardContainer = new ContentView
        {
            Padding = new Thickness(0),
            Margin = new Thickness(0, 4)
        };
        cardContainer.Content = card;

        var title = new Label
        {
            Text = entry.Title,
            FontSize = 18,
            AutomationId = $"controls-sample-title-{entry.Id}"
        };
        SetThemeValue(title, Label.TextColorProperty, PrimaryTextLight, PrimaryTextDark);
        content.Children.Add(title);

        var status = new Label
        {
            Text = $"Status: {entry.Status}",
            FontSize = 13,
            AutomationId = $"controls-sample-status-{entry.Id}"
        };
        SetThemeValue(status, Label.TextColorProperty, SecondaryTextLight, SecondaryTextDark);
        content.Children.Add(status);

        var description = new Label
        {
            Text = entry.Description,
            FontSize = 13,
            AutomationId = $"controls-sample-description-{entry.Id}"
        };
        SetThemeValue(description, Label.TextColorProperty, SecondaryTextLight, SecondaryTextDark);
        content.Children.Add(description);

        var openButton = new Button
        {
            Text = "Open page",
            AutomationId = $"controls-sample-open-{entry.Id}",
            Command = new Command(async () => await OpenEntryAsync(entry))
        };
        SetThemeValue(openButton, Button.BackgroundColorProperty, AccentLight, AccentDark);
        SetThemeValue(openButton, Button.TextColorProperty, Colors.White, Colors.White);
        content.Children.Add(openButton);

        return cardContainer;
    }

    private async Task OpenEntryAsync(ControlsSampleEntry entry)
    {
        var page = entry.CreatePage(_services);
        page.Title = entry.Title;
        if (string.IsNullOrWhiteSpace(page.AutomationId))
        {
            page.AutomationId = $"controls-sample-page-{entry.Id}";
        }

        await Navigation.PushAsync(page, animated: false);
    }

    private static void SetThemeValue<TBindable, TValue>(
        TBindable bindable,
        BindableProperty property,
        TValue light,
        TValue dark)
        where TBindable : BindableObject
        where TValue : notnull
    {
        bindable.SetAppTheme<TValue>(property, light, dark);
    }
}
