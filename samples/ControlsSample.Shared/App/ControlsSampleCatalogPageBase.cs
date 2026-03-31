using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace ControlsSample.Shared.App;

public abstract class ControlsSampleCatalogPageBase : ContentPage
{
    protected static readonly Color PageBackgroundLight = Color.FromArgb("#F5F7FB");
    protected static readonly Color PageBackgroundDark = Color.FromArgb("#14171D");
    protected static readonly Color CardBackgroundLight = Color.FromArgb("#FFFFFF");
    protected static readonly Color CardBackgroundDark = Color.FromArgb("#20242D");
    protected static readonly Color CardStrokeLight = Color.FromArgb("#D7DEEA");
    protected static readonly Color CardStrokeDark = Color.FromArgb("#333A49");
    protected static readonly Color PrimaryTextLight = Color.FromArgb("#10131A");
    protected static readonly Color PrimaryTextDark = Color.FromArgb("#E8EDF8");
    protected static readonly Color SecondaryTextLight = Color.FromArgb("#4A5568");
    protected static readonly Color SecondaryTextDark = Color.FromArgb("#B9C3D8");
    protected static readonly Color AccentLight = Color.FromArgb("#2563EB");
    protected static readonly Color AccentDark = Color.FromArgb("#3B82F6");

    protected ControlsSampleCatalogPageBase()
    {
        this.SetAppTheme(BackgroundColorProperty, PageBackgroundLight, PageBackgroundDark);
    }

    protected static VerticalStackLayout CreateRootStack()
    {
        return new VerticalStackLayout
        {
            Padding = new Thickness(20, 16),
            Spacing = 12
        };
    }

    protected View BuildCard(VerticalStackLayout content)
    {
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

        return new ContentView
        {
            Padding = new Thickness(0),
            Margin = new Thickness(0, 4),
            Content = card
        };
    }

    protected Label CreatePrimaryLabel(string text, double fontSize, string automationId)
    {
        var label = new Label
        {
            Text = text,
            FontSize = fontSize,
            AutomationId = automationId
        };
        SetThemeValue(label, Label.TextColorProperty, PrimaryTextLight, PrimaryTextDark);
        return label;
    }

    protected Label CreateSecondaryLabel(string text, double fontSize, string automationId)
    {
        var label = new Label
        {
            Text = text,
            FontSize = fontSize,
            AutomationId = automationId
        };
        SetThemeValue(label, Label.TextColorProperty, SecondaryTextLight, SecondaryTextDark);
        return label;
    }

    protected Button CreateAccentButton(string text, string automationId, Func<Task> action)
    {
        var button = new Button
        {
            Text = text,
            AutomationId = automationId,
            Command = new Command(async () => await action())
        };
        SetThemeValue(button, Button.BackgroundColorProperty, AccentLight, AccentDark);
        SetThemeValue(button, Button.TextColorProperty, Colors.White, Colors.White);
        return button;
    }

    protected static void SetThemeValue<TBindable, TValue>(
        TBindable bindable,
        BindableProperty property,
        TValue light,
        TValue dark)
        where TBindable : BindableObject
        where TValue : notnull
    {
        bindable.SetAppTheme(property, light, dark);
    }
}
