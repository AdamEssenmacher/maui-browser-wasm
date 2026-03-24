using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace ControlsSample.Shared.App;

public sealed class IndicatorViewParityProbePage : ContentPage
{
    private readonly IndicatorView _indicator;
    private readonly Label _positionLabel;
    private readonly Label _visibleCountLabel;
    private readonly Label _modeLabel;
    private readonly DataTemplate _template;
    private int _count = 5;
    private int _maximumVisible = 5;
    private bool _hideSingle;
    private bool _useSquareShape;
    private bool _useTemplate;

    public IndicatorViewParityProbePage()
    {
        Title = "IndicatorView Parity Probe";
        AutomationId = "controls-sample-page-indicatorviewparitypage";

        _template = new DataTemplate(() =>
            new Border
            {
                WidthRequest = 12,
                HeightRequest = 12,
                StrokeThickness = 0,
                Background = Brush.Gold,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 3
                }
            });

        _indicator = new IndicatorView
        {
            AutomationId = "controls-sample-indicatorview-parity-host",
            Count = _count,
            Position = 0,
            MaximumVisible = _maximumVisible,
            HideSingle = _hideSingle,
            IndicatorSize = 12,
            IndicatorColor = Color.FromArgb("#94A3B8"),
            SelectedIndicatorColor = Color.FromArgb("#1D4ED8"),
            HorizontalOptions = LayoutOptions.Center
        };

        _positionLabel = new Label
        {
            AutomationId = "controls-sample-indicatorview-parity-position",
            FontAttributes = FontAttributes.Bold
        };

        _visibleCountLabel = new Label
        {
            AutomationId = "controls-sample-indicatorview-parity-visible-count"
        };

        _modeLabel = new Label
        {
            AutomationId = "controls-sample-indicatorview-parity-mode"
        };

        _indicator.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(IndicatorView.Position)
                or nameof(IndicatorView.Count)
                or nameof(IndicatorView.MaximumVisible)
                or nameof(IndicatorView.HideSingle)
                or nameof(IndicatorView.IsEnabled)
                or nameof(IndicatorView.IndicatorTemplate))
            {
                UpdateStateText();
            }
        };

        ApplyProbeState();

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
                        AutomationId = "controls-sample-indicatorview-parity-description",
                        Text = "IndicatorView parity probe for count, position, max-visible, hide-single, shape, and template override behavior."
                    },
                    _indicator,
                    _positionLabel,
                    _visibleCountLabel,
                    _modeLabel,
                    BuildControls()
                }
            }
        };
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
                        CreateButton("Set Count 1", "controls-sample-indicatorview-parity-set-count-one", () =>
                        {
                            _count = 1;
                            ApplyProbeState();
                        }),
                        CreateButton("Set Count 5", "controls-sample-indicatorview-parity-set-count-five", () =>
                        {
                            _count = 5;
                            ApplyProbeState();
                        }),
                        CreateButton("Next Position", "controls-sample-indicatorview-parity-next-position", MoveToNextPosition)
                    }
                },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Toggle MaxVisible", "controls-sample-indicatorview-parity-toggle-max-visible", () =>
                        {
                            _maximumVisible = _maximumVisible == 5 ? 3 : 5;
                            ApplyProbeState();
                        }),
                        CreateButton("Toggle HideSingle", "controls-sample-indicatorview-parity-toggle-hide-single", () =>
                        {
                            _hideSingle = !_hideSingle;
                            ApplyProbeState();
                        }),
                        CreateButton("Toggle Shape", "controls-sample-indicatorview-parity-toggle-shape", () =>
                        {
                            _useSquareShape = !_useSquareShape;
                            ApplyProbeState();
                        })
                    }
                },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Toggle Template", "controls-sample-indicatorview-parity-toggle-template", () =>
                        {
                            _useTemplate = !_useTemplate;
                            ApplyProbeState();
                        }),
                        CreateButton("Toggle Enabled", "controls-sample-indicatorview-parity-toggle-enabled", () =>
                        {
                            _indicator.IsEnabled = !_indicator.IsEnabled;
                            UpdateStateText();
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

    private void MoveToNextPosition()
    {
        if (_count <= 0)
        {
            _indicator.Position = 0;
            UpdateStateText();
            return;
        }

        var current = Math.Clamp(_indicator.Position, 0, Math.Max(0, _count - 1));
        _indicator.Position = (current + 1) % _count;
        UpdateStateText();
    }

    private void ApplyProbeState()
    {
        _indicator.Count = Math.Max(0, _count);
        _indicator.MaximumVisible = Math.Max(0, _maximumVisible);
        _indicator.HideSingle = _hideSingle;
        _indicator.IndicatorsShape = _useSquareShape ? IndicatorShape.Square : IndicatorShape.Circle;
        _indicator.IndicatorTemplate = _useTemplate ? _template : null;

        if (_indicator.Count <= 0)
        {
            _indicator.Position = 0;
        }
        else
        {
            _indicator.Position = Math.Clamp(_indicator.Position, 0, _indicator.Count - 1);
        }

        UpdateStateText();
    }

    private int GetVisibleCount()
    {
        var visible = Math.Max(0, Math.Min(_indicator.Count, _indicator.MaximumVisible));
        if (visible == 1 && _indicator.HideSingle)
        {
            return 0;
        }

        return visible;
    }

    private void UpdateStateText()
    {
        _positionLabel.Text = $"position={_indicator.Position}";
        _visibleCountLabel.Text = $"visible={GetVisibleCount()}";
        _modeLabel.Text = $"shape={_indicator.IndicatorsShape} template={(_indicator.IndicatorTemplate is null ? "off" : "on")} enabled={_indicator.IsEnabled}";
    }
}
