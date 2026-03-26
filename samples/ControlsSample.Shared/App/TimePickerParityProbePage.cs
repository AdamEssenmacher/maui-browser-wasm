using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace ControlsSample.Shared.App;

public sealed class TimePickerParityProbePage : ContentPage
{
    private readonly TimePicker _timePicker;
    private readonly Label _timeStateLabel;
    private readonly Label _formatStateLabel;
    private readonly Label _openStateLabel;

    public TimePickerParityProbePage()
    {
        Title = "TimePicker Parity Probe";
        AutomationId = "controls-sample-page-timepickerparitypage";

        _timePicker = new TimePicker
        {
            AutomationId = "controls-sample-timepicker-parity-host",
            Time = new TimeSpan(9, 30, 0),
            Format = "HH:mm",
            TextColor = Color.FromArgb("#1E293B"),
            CharacterSpacing = 0.5,
            WidthRequest = 180
        };

        _timeStateLabel = new Label
        {
            AutomationId = "controls-sample-timepicker-parity-time-state",
            FontAttributes = FontAttributes.Bold
        };
        _formatStateLabel = new Label
        {
            AutomationId = "controls-sample-timepicker-parity-format-state"
        };
        _openStateLabel = new Label
        {
            AutomationId = "controls-sample-timepicker-parity-open-state"
        };

        _timePicker.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(TimePicker.Time)
                or nameof(TimePicker.Format)
                or nameof(TimePicker.IsOpen)
                or nameof(TimePicker.IsEnabled))
            {
                UpdateStateLabels();
            }
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
                        AutomationId = "controls-sample-timepicker-parity-description",
                        Text = "TimePicker parity probe for time updates, format/step depth, open-state roundtrip, and enabled-state interaction gating."
                    },
                    _timePicker,
                    _timeStateLabel,
                    _formatStateLabel,
                    _openStateLabel,
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
                        CreateButton("Set 09:30", "controls-sample-timepicker-parity-set-0930", () =>
                        {
                            _timePicker.Time = new TimeSpan(9, 30, 0);
                        }),
                        CreateButton("Set 14:45:20", "controls-sample-timepicker-parity-set-144520", () =>
                        {
                            _timePicker.Time = new TimeSpan(14, 45, 20);
                        }),
                        CreateButton("Toggle Seconds", "controls-sample-timepicker-parity-toggle-seconds", ToggleSecondsFormat)
                    }
                },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Open", "controls-sample-timepicker-parity-open", () =>
                        {
                            _timePicker.IsOpen = true;
                        }),
                        CreateButton("Close", "controls-sample-timepicker-parity-close", () =>
                        {
                            _timePicker.IsOpen = false;
                        }),
                        CreateButton("Toggle Enabled", "controls-sample-timepicker-parity-toggle-enabled", () =>
                        {
                            _timePicker.IsEnabled = !_timePicker.IsEnabled;
                        })
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

    private void ToggleSecondsFormat()
    {
        _timePicker.Format = string.Equals(_timePicker.Format, "HH:mm:ss", StringComparison.Ordinal)
            ? "HH:mm"
            : "HH:mm:ss";
    }

    private void UpdateStateLabels()
    {
        var timeText = _timePicker.Time?.ToString(@"hh\:mm\:ss") ?? "null";
        var step = string.Equals(_timePicker.Format, "HH:mm:ss", StringComparison.Ordinal) ? "1" : "60";
        _timeStateLabel.Text = $"time={timeText}";
        _formatStateLabel.Text = $"format={_timePicker.Format} step={step}";
        _openStateLabel.Text = $"isOpen={_timePicker.IsOpen} enabled={_timePicker.IsEnabled}";
    }
}
