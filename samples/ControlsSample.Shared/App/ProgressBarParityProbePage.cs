using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace ControlsSample.Shared.App;

public sealed class ProgressBarParityProbePage : ContentPage
{
    private readonly ProgressBar _progressBar;
    private readonly Label _progressStateLabel;
    private readonly Label _styleStateLabel;
    private bool _useAlternateStyle;

    public ProgressBarParityProbePage()
    {
        Title = "ProgressBar Parity Probe";
        AutomationId = "controls-sample-page-progressbarparitypage";

        _progressBar = new ProgressBar
        {
            AutomationId = "controls-sample-progressbar-parity-host",
            WidthRequest = 220,
            Progress = 0.25,
            ProgressColor = Color.FromArgb("#2563EB"),
            Background = new SolidColorBrush(Color.FromArgb("#E2E8F0"))
        };

        _progressStateLabel = new Label
        {
            AutomationId = "controls-sample-progressbar-parity-progress-state",
            FontAttributes = FontAttributes.Bold
        };
        _styleStateLabel = new Label
        {
            AutomationId = "controls-sample-progressbar-parity-style-state"
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 12,
                Children =
                {
                    new Label
                    {
                        AutomationId = "controls-sample-progressbar-parity-description",
                        Text = "ProgressBar parity probe for determinate value clamping, native progress-host color projection, and browser-limited track styling updates."
                    },
                    _progressBar,
                    _progressStateLabel,
                    _styleStateLabel,
                    BuildControls()
                }
            }
        };

        UpdateStateLabels();
    }

    private View BuildControls()
    {
        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Increment", "controls-sample-progressbar-parity-increment", () =>
                        {
                            _progressBar.Progress += 0.2;
                            UpdateStateLabels();
                        }),
                        CreateButton("Overshoot", "controls-sample-progressbar-parity-overshoot", () =>
                        {
                            _progressBar.Progress = 1.4;
                            UpdateStateLabels();
                        }),
                        CreateButton("Reset", "controls-sample-progressbar-parity-reset", () =>
                        {
                            _progressBar.Progress = 0.25;
                            UpdateStateLabels();
                        })
                    }
                },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Toggle Style", "controls-sample-progressbar-parity-toggle-style", ToggleStyle)
                    }
                }
            }
        };
    }

    private static Button CreateButton(string text, string automationId, Action action)
    {
        return new Button
        {
            Text = text,
            AutomationId = automationId,
            Command = new Command(action)
        };
    }

    private void ToggleStyle()
    {
        _useAlternateStyle = !_useAlternateStyle;
        if (_useAlternateStyle)
        {
            _progressBar.ProgressColor = Color.FromArgb("#F97316");
            _progressBar.Background = new SolidColorBrush(Color.FromArgb("#FEF3C7"));
        }
        else
        {
            _progressBar.ProgressColor = Color.FromArgb("#2563EB");
            _progressBar.Background = new SolidColorBrush(Color.FromArgb("#E2E8F0"));
        }

        UpdateStateLabels();
    }

    private void UpdateStateLabels()
    {
        var rawProgress = _progressBar.Progress;
        var clampedProgress = !double.IsFinite(rawProgress)
            ? 0
            : Math.Clamp(rawProgress, 0, 1);

        _progressStateLabel.Text = $"progress={clampedProgress:0.###}";
        _styleStateLabel.Text = $"style={(_useAlternateStyle ? "alt" : "default")}";
    }
}
