using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace ControlsSample.Shared.App;

public sealed class RadioButtonParityProbePage : ContentPage
{
    private readonly VerticalStackLayout _namedGroupHost;
    private readonly RadioButton _namedMercury;
    private readonly RadioButton _namedVenus;
    private readonly RadioButton _namedEarth;
    private readonly VerticalStackLayout _parentScopeHost;
    private readonly RadioButton _parentAlpha;
    private readonly RadioButton _parentBeta;
    private readonly Label _namedSelectionLabel;
    private readonly Label _parentSelectionLabel;
    private readonly Label _styleStateLabel;

    private bool _contentAsView;
    private bool _alternateStyle;
    private bool _isEnabled = true;

    public RadioButtonParityProbePage()
    {
        Title = "RadioButton Parity Probe";
        AutomationId = "controls-sample-page-radiobuttonparitypage";

        _namedSelectionLabel = new Label
        {
            AutomationId = "controls-sample-radiobutton-parity-selected-value",
            FontAttributes = FontAttributes.Bold
        };

        _parentSelectionLabel = new Label
        {
            AutomationId = "controls-sample-radiobutton-parity-parent-selected"
        };

        _styleStateLabel = new Label
        {
            AutomationId = "controls-sample-radiobutton-parity-style-state"
        };

        _namedGroupHost = new VerticalStackLayout
        {
            Spacing = 6,
            AutomationId = "controls-sample-radiobutton-parity-named-group"
        };
        RadioButtonGroup.SetGroupName(_namedGroupHost, "planet-group");

        _namedMercury = CreateNamedRadioButton(
            text: "Mercury",
            value: "mercury",
            automationId: "controls-sample-radiobutton-parity-named-mercury");
        _namedVenus = CreateNamedRadioButton(
            text: "Venus",
            value: "venus",
            automationId: "controls-sample-radiobutton-parity-named-venus");
        _namedEarth = CreateNamedRadioButton(
            text: "Earth",
            value: "earth",
            automationId: "controls-sample-radiobutton-parity-named-earth");
        _namedEarth.BorderWidth = 2;
        _namedEarth.CornerRadius = 8;
        _namedEarth.Background = new SolidColorBrush(Color.FromArgb("#F8FAFC"));

        _namedGroupHost.Children.Add(_namedMercury);
        _namedGroupHost.Children.Add(_namedVenus);
        _namedGroupHost.Children.Add(_namedEarth);
        RadioButtonGroup.SetSelectedValue(_namedGroupHost, "venus");

        _parentScopeHost = new VerticalStackLayout
        {
            Spacing = 6,
            AutomationId = "controls-sample-radiobutton-parity-parent-group"
        };
        _parentAlpha = CreateParentScopeRadioButton(
            text: "Alpha",
            value: "alpha",
            automationId: "controls-sample-radiobutton-parity-parent-alpha");
        _parentBeta = CreateParentScopeRadioButton(
            text: "Beta",
            value: "beta",
            automationId: "controls-sample-radiobutton-parity-parent-beta");
        _parentAlpha.IsChecked = true;

        _parentScopeHost.Children.Add(_parentAlpha);
        _parentScopeHost.Children.Add(_parentBeta);

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
                        AutomationId = "controls-sample-radiobutton-parity-description",
                        Text = "RadioButton parity probe for group exclusivity, selected-value roundtrip, content/style changes, and enabled gating."
                    },
                    new Label
                    {
                        Text = "Named Group (RadioButtonGroup.GroupName + SelectedValue)",
                        FontAttributes = FontAttributes.Bold
                    },
                    _namedGroupHost,
                    _namedSelectionLabel,
                    new Label
                    {
                        Text = "Parent-Scope Group (unnamed)",
                        FontAttributes = FontAttributes.Bold
                    },
                    _parentScopeHost,
                    _parentSelectionLabel,
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
                        CreateButton("Select Mercury", "controls-sample-radiobutton-parity-select-mercury", () =>
                        {
                            RadioButtonGroup.SetSelectedValue(_namedGroupHost, "mercury");
                            UpdateStateLabels();
                        }),
                        CreateButton("Select Venus", "controls-sample-radiobutton-parity-select-venus", () =>
                        {
                            RadioButtonGroup.SetSelectedValue(_namedGroupHost, "venus");
                            UpdateStateLabels();
                        }),
                        CreateButton("Select Earth", "controls-sample-radiobutton-parity-select-earth", () =>
                        {
                            RadioButtonGroup.SetSelectedValue(_namedGroupHost, "earth");
                            UpdateStateLabels();
                        })
                    }
                },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Toggle Parent", "controls-sample-radiobutton-parity-toggle-parent", () =>
                        {
                            _parentBeta.IsChecked = !_parentBeta.IsChecked;
                            if (_parentBeta.IsChecked)
                            {
                                _parentAlpha.IsChecked = false;
                            }

                            UpdateStateLabels();
                        }),
                        CreateButton("Toggle Content", "controls-sample-radiobutton-parity-toggle-content", ToggleContentMode),
                        CreateButton("Toggle Style", "controls-sample-radiobutton-parity-toggle-style", ToggleStyleMode),
                        CreateButton("Toggle Enabled", "controls-sample-radiobutton-parity-toggle-enabled", ToggleEnabledMode)
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

    private RadioButton CreateNamedRadioButton(string text, string value, string automationId)
    {
        var radioButton = new RadioButton
        {
            Content = text,
            Value = value,
            AutomationId = automationId
        };
        radioButton.CheckedChanged += OnProbeRadioCheckedChanged;
        return radioButton;
    }

    private RadioButton CreateParentScopeRadioButton(string text, string value, string automationId)
    {
        var radioButton = new RadioButton
        {
            Content = text,
            Value = value,
            AutomationId = automationId
        };
        radioButton.CheckedChanged += OnProbeRadioCheckedChanged;
        return radioButton;
    }

    private void ToggleContentMode()
    {
        _contentAsView = !_contentAsView;
        if (_contentAsView)
        {
            _namedEarth.Content = new HorizontalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new BoxView
                    {
                        WidthRequest = 10,
                        HeightRequest = 10,
                        Color = Color.FromArgb("#334155"),
                        CornerRadius = 5
                    },
                    new Label
                    {
                        Text = "Earth (View Content)"
                    }
                }
            };
        }
        else
        {
            _namedEarth.Content = "Earth";
        }

        UpdateStateLabels();
    }

    private void ToggleStyleMode()
    {
        _alternateStyle = !_alternateStyle;
        _namedEarth.BorderColor = _alternateStyle
            ? Color.FromArgb("#7C3AED")
            : Color.FromArgb("#475569");
        _namedEarth.BorderWidth = _alternateStyle ? 3 : 2;
        _namedEarth.CornerRadius = _alternateStyle ? 14 : 8;
        _namedEarth.TextColor = _alternateStyle
            ? Color.FromArgb("#312E81")
            : Color.FromArgb("#0F172A");
        _namedEarth.CharacterSpacing = _alternateStyle ? 1.2 : 0;
        _namedEarth.Background = _alternateStyle
            ? new SolidColorBrush(Color.FromArgb("#EEF2FF"))
            : new SolidColorBrush(Color.FromArgb("#F8FAFC"));
        UpdateStateLabels();
    }

    private void ToggleEnabledMode()
    {
        _isEnabled = !_isEnabled;
        _namedMercury.IsEnabled = _isEnabled;
        _namedVenus.IsEnabled = _isEnabled;
        _namedEarth.IsEnabled = _isEnabled;
        _parentAlpha.IsEnabled = _isEnabled;
        _parentBeta.IsEnabled = _isEnabled;
        UpdateStateLabels();
    }

    private void OnProbeRadioCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        UpdateStateLabels();
    }

    private void UpdateStateLabels()
    {
        var namedSelected = _namedGroupHost is null
            ? "none"
            : RadioButtonGroup.GetSelectedValue(_namedGroupHost)?.ToString() ?? "none";
        var parentSelected = _parentAlpha is null || _parentBeta is null
            ? "none"
            : _parentAlpha.IsChecked
                ? "alpha"
                : _parentBeta.IsChecked ? "beta" : "none";
        _namedSelectionLabel.Text = $"namedSelected={namedSelected}";
        _parentSelectionLabel.Text = $"parentSelected={parentSelected}";
        _styleStateLabel.Text = $"contentMode={(_contentAsView ? "view" : "text")} style={(_alternateStyle ? "alt" : "base")} enabled={_isEnabled}";
    }
}
