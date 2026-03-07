using Microsoft.Maui.Controls;

namespace Maui.Browser.Sample.Demo;

internal sealed class ParityCatalogPage : ContentPage
{
    public ParityCatalogPage()
    {
        Title = "MAUI Browser Showcase";
        AutomationId = "showcase-page-catalog";
        SetDynamicResource(BackgroundColorProperty, ShowcaseThemeManager.PageBackgroundColorKey);
        Resources = new ResourceDictionary
        {
            MergedDictionaries =
            {
                new ShowcaseStyles()
            }
        };

        var content = new VerticalStackLayout
        {
            Padding = new Thickness(30, 26, 30, 36),
            Spacing = 16
        };

        content.Children.Add(new Label
        {
            AutomationId = "parity-catalog-heading",
            Text = "MAUI Browser Showcase",
            Style = GetStyle("ShowcasePageHeadingStyle")
        });

        content.Children.Add(new Label
        {
            AutomationId = "parity-catalog-summary",
            Text = "Showcase is now the single track for product polish and parity-depth validation.",
            Style = GetStyle("ShowcaseMutedLabelStyle")
        });

        content.Children.Add(CreateSectionHeading("Control Catalog"));
        content.Children.Add(new Label
        {
            AutomationId = "showcase-catalog-control-list-notice",
            Text = "The full MAUI control/layout list now lives on Showcase Home.",
            Style = GetStyle("ShowcaseMutedLabelStyle")
        });

        content.Children.Add(CreateSectionHeading("Showcase Core"));
        content.Children.Add(CreateNavigationButton(
            "Showcase Home",
            "showcase-open-home",
            static () => new ShowcaseHomePage()));
        content.Children.Add(CreateNavigationButton(
            "Showcase Text/Input",
            "showcase-open-text-input",
            static () => new ShowcaseTextInputPage()));
        content.Children.Add(CreateNavigationButton(
            "Showcase Form/Media",
            "showcase-open-form-media",
            static () => new ShowcaseFormMediaPage()));
        content.Children.Add(CreateNavigationButton(
            "Showcase Layout/Collection",
            "showcase-open-layout-collection",
            static () => new ShowcaseLayoutCollectionPage()));

        content.Children.Add(CreateSectionHeading("Showcase Deep Dives"));
        content.Children.Add(CreateNavigationButton(
            "Control Lab",
            "showcase-open-control-lab",
            static () => new XamlDemoPage()));
        content.Children.Add(CreateNavigationButton(
            "Text/Input Depth",
            "showcase-open-text-input-depth",
            static () => new ParityTextInputPage()));
        content.Children.Add(CreateNavigationButton(
            "Pickers/Date-Time Depth",
            "showcase-open-pickers-datetime-depth",
            static () => new ParityPickersDateTimePage()));
        content.Children.Add(CreateNavigationButton(
            "Media/Visual Depth",
            "showcase-open-media-visual-depth",
            static () => new ParityMediaVisualPage()));
        content.Children.Add(CreateNavigationButton(
            "Collection/Templated Depth",
            "showcase-open-collection-templated-depth",
            static () => new ParityCollectionTemplatedPage()));
        content.Children.Add(CreateNavigationButton(
            "Containers/Layout Depth",
            "showcase-open-containers-layout-depth",
            static () => new ParityContainersLayoutPage()));
        content.Children.Add(CreateNavigationButton(
            "Navigation/Window Depth",
            "showcase-open-navigation-window-depth",
            static () => new ParityNavigationWindowPage()));

        Content = new ScrollView
        {
            Content = content
        };
    }

    private Label CreateSectionHeading(string text)
    {
        return new Label
        {
            Text = text,
            Style = GetStyle("ShowcaseSectionHeadingStyle")
        };
    }

    private Button CreateNavigationButton(string text, string automationId, Func<Page> createPage)
    {
        var button = new Button
        {
            Text = text,
            AutomationId = automationId,
            HorizontalOptions = LayoutOptions.Fill,
            Style = GetStyle("ShowcaseSecondaryButtonStyle")
        };

        button.Clicked += async (_, _) => await Navigation.PushAsync(createPage(), false);
        return button;
    }

    private Style GetStyle(string key)
    {
        return (Style)Resources[key];
    }
}
