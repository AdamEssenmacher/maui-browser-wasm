using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace ControlsSample.Shared.App;

public sealed class DatePickerParityProbePage : ContentPage
{
    private readonly DatePicker _datePicker;
    private readonly Label _dateStateLabel;
    private readonly Label _boundsStateLabel;
    private readonly Label _formatStateLabel;
    private readonly Label _openStateLabel;
    private bool _boundsEnabled;

    public DatePickerParityProbePage()
    {
        Title = "DatePicker Parity Probe";
        AutomationId = "controls-sample-page-datepickerparitypage";

        _datePicker = new DatePicker
        {
            AutomationId = "controls-sample-datepicker-parity-host",
            Date = new DateTime(2026, 4, 2),
            Format = "yyyy-MM-dd",
            TextColor = Color.FromArgb("#1E293B"),
            CharacterSpacing = 0.5,
            WidthRequest = 180
        };

        _dateStateLabel = new Label
        {
            AutomationId = "controls-sample-datepicker-parity-date-state",
            FontAttributes = FontAttributes.Bold
        };
        _boundsStateLabel = new Label
        {
            AutomationId = "controls-sample-datepicker-parity-bounds-state"
        };
        _formatStateLabel = new Label
        {
            AutomationId = "controls-sample-datepicker-parity-format-state"
        };
        _openStateLabel = new Label
        {
            AutomationId = "controls-sample-datepicker-parity-open-state"
        };

        _datePicker.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(DatePicker.Date)
                or nameof(DatePicker.MinimumDate)
                or nameof(DatePicker.MaximumDate)
                or nameof(DatePicker.Format)
                or nameof(DatePicker.IsOpen)
                or nameof(DatePicker.IsEnabled))
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
                        AutomationId = "controls-sample-datepicker-parity-description",
                        Text = "DatePicker parity probe for nullable date projection, bounds, format metadata, open-state roundtrip, and enabled-state interaction gating."
                    },
                    _datePicker,
                    _dateStateLabel,
                    _boundsStateLabel,
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
                        CreateButton("Set 2026-04-02", "controls-sample-datepicker-parity-set-20260402", () =>
                        {
                            _datePicker.Date = new DateTime(2026, 4, 2);
                        }),
                        CreateButton("Set 2026-04-12", "controls-sample-datepicker-parity-set-20260412", () =>
                        {
                            _datePicker.Date = new DateTime(2026, 4, 12);
                        }),
                        CreateButton("Clear Date", "controls-sample-datepicker-parity-clear-date", () =>
                        {
                            _datePicker.Date = null;
                        })
                    }
                },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Toggle Bounds", "controls-sample-datepicker-parity-toggle-bounds", ToggleBounds),
                        CreateButton("Toggle Format", "controls-sample-datepicker-parity-toggle-format", ToggleFormat),
                        CreateButton("Toggle Enabled", "controls-sample-datepicker-parity-toggle-enabled", () =>
                        {
                            _datePicker.IsEnabled = !_datePicker.IsEnabled;
                        })
                    }
                },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Open", "controls-sample-datepicker-parity-open", () =>
                        {
                            _datePicker.IsOpen = true;
                        }),
                        CreateButton("Close", "controls-sample-datepicker-parity-close", () =>
                        {
                            _datePicker.IsOpen = false;
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

    private void ToggleBounds()
    {
        _boundsEnabled = !_boundsEnabled;
        if (_boundsEnabled)
        {
            _datePicker.MinimumDate = new DateTime(2026, 4, 5);
            _datePicker.MaximumDate = new DateTime(2026, 4, 20);
        }
        else
        {
            _datePicker.MinimumDate = null;
            _datePicker.MaximumDate = null;
        }

        UpdateStateLabels();
    }

    private void ToggleFormat()
    {
        _datePicker.Format = string.Equals(_datePicker.Format, "D", StringComparison.Ordinal)
            ? "yyyy-MM-dd"
            : "D";
    }

    private void UpdateStateLabels()
    {
        var dateText = _datePicker.Date?.ToString("yyyy-MM-dd") ?? "null";
        var minText = _datePicker.MinimumDate?.ToString("yyyy-MM-dd") ?? "null";
        var maxText = _datePicker.MaximumDate?.ToString("yyyy-MM-dd") ?? "null";
        _dateStateLabel.Text = $"date={dateText}";
        _boundsStateLabel.Text = $"min={minText} max={maxText}";
        _formatStateLabel.Text = $"format={_datePicker.Format}";
        _openStateLabel.Text = $"isOpen={_datePicker.IsOpen} enabled={_datePicker.IsEnabled}";
    }
}
