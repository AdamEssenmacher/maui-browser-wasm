using Microsoft.Maui.Controls;

namespace Maui.Browser.Sample.Demo;

internal sealed class ShowcaseControlDemoPage : ContentPage
{
    public ShowcaseControlDemoPage(ShowcaseControlSpec spec)
    {
        Title = spec.TypeName;
        AutomationId = spec.DemoPageAutomationId;
        SetDynamicResource(BackgroundColorProperty, ShowcaseThemeManager.PageBackgroundColorKey);
        Resources = new ResourceDictionary
        {
            MergedDictionaries =
            {
                new ShowcaseStyles()
            }
        };

        var root = new VerticalStackLayout
        {
            Padding = new Thickness(30, 26, 30, 36),
            Spacing = 16
        };

        var backButton = new Button
        {
            AutomationId = "showcase-control-back",
            Text = "Back",
            Style = GetStyle("ShowcaseGhostButtonStyle")
        };
        backButton.Clicked += async (_, _) => await Navigation.PopAsync(false);

        root.Children.Add(backButton);
        root.Children.Add(new Label
        {
            AutomationId = $"showcase-control-title-{spec.Slug}",
            Text = spec.TypeName,
            Style = GetStyle("ShowcasePageHeadingStyle")
        });

        root.Children.Add(new Label
        {
            AutomationId = $"showcase-control-status-{spec.Slug}",
            Text = $"Category: {spec.Category} | Status: {spec.StatusText}",
            Style = GetStyle("ShowcaseMutedLabelStyle")
        });

        root.Children.Add(new Label
        {
            AutomationId = $"showcase-control-summary-{spec.Slug}",
            Text = spec.Summary,
            Style = GetStyle("ShowcaseBaseLabelStyle")
        });

        if (!spec.IsImplemented)
        {
            root.Children.Add(new Label
            {
                AutomationId = "showcase-control-missing-notice",
                Text = "This control is not implemented yet on the browser backend.",
                Style = GetStyle("ShowcaseCalloutLabelStyle")
            });
        }
        else if (spec.ProbeFactory is null)
        {
            root.Children.Add(new Label
            {
                AutomationId = "showcase-control-probe-missing",
                Text = "No standalone probe is defined yet for this control.",
                Style = GetStyle("ShowcaseMutedLabelStyle")
            });
        }
        else
        {
            var probeHost = new Border
            {
                Style = GetStyle("ShowcaseCardStyle")
            };

            try
            {
                var probe = spec.ProbeFactory();
                probe.AutomationId = $"showcase-control-probe-{spec.Slug}";
                probeHost.Content = probe;
            }
            catch (Exception ex)
            {
                probeHost.Content = new Label
                {
                    AutomationId = "showcase-control-probe-error",
                    Text = $"Probe failed to build: {ex.GetType().Name}",
                    Style = GetStyle("ShowcaseCalloutLabelStyle")
                };
            }

            root.Children.Add(probeHost);
        }

        if (spec.ScenarioPageFactory is not null)
        {
            var scenarioButton = new Button
            {
                AutomationId = $"showcase-open-control-scenario-{spec.Slug}",
                Text = spec.ScenarioButtonText ?? "Open scenario",
                Style = GetStyle("ShowcaseSecondaryButtonStyle")
            };

            scenarioButton.Clicked += async (_, _) => await Navigation.PushAsync(spec.ScenarioPageFactory(), false);
            root.Children.Add(scenarioButton);
        }

        Content = new ScrollView
        {
            Content = root
        };
    }

    private Style GetStyle(string key)
    {
        return (Style)Resources[key];
    }
}
