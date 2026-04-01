using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace ControlsSample.Shared.App;

public sealed class PickerParityProbePage : ContentPage
{
    private readonly Picker _stringPicker;
    private readonly Picker _itemsPicker;
    private readonly Label _selectionStateLabel;
    private readonly Label _openStateLabel;
    private readonly Label _styleStateLabel;
    private readonly Label _itemsStateLabel;
    private ObservableCollection<PickerPlanet> _itemsSource;
    private bool _useAlternateStyle;
    private bool _useNameBinding;
    private bool _useAlternateSource;

    public PickerParityProbePage()
    {
        Title = "Picker Parity Probe";
        AutomationId = "controls-sample-page-pickerparitypage";

        _stringPicker = new Picker
        {
            AutomationId = "controls-sample-picker-parity-host",
            Title = "Choose planet",
            TitleColor = Colors.Silver,
            TextColor = Color.FromArgb("#111827"),
            Background = new SolidColorBrush(Color.FromArgb("#F8FAFC")),
            WidthRequest = 180
        };
        _stringPicker.Items.Add("Mercury");
        _stringPicker.Items.Add("Venus");
        _stringPicker.Items.Add("Earth");
        _stringPicker.SelectedIndex = -1;

        _itemsSource = CreatePlanetSource(useAlternateSource: false);
        _itemsPicker = new Picker
        {
            AutomationId = "controls-sample-picker-parity-items-host",
            Title = "Choose object planet",
            TitleColor = Colors.DarkSlateGray,
            WidthRequest = 220,
            ItemsSource = _itemsSource,
            ItemDisplayBinding = new Binding(nameof(PickerPlanet.Display))
        };
        _itemsPicker.SelectedItem = _itemsSource[1];

        _selectionStateLabel = new Label
        {
            AutomationId = "controls-sample-picker-parity-selection-state",
            FontAttributes = FontAttributes.Bold
        };
        _openStateLabel = new Label
        {
            AutomationId = "controls-sample-picker-parity-open-state"
        };
        _styleStateLabel = new Label
        {
            AutomationId = "controls-sample-picker-parity-style-state"
        };
        _itemsStateLabel = new Label
        {
            AutomationId = "controls-sample-picker-parity-items-state"
        };

        _stringPicker.PropertyChanged += OnPickerPropertyChanged;
        _itemsPicker.PropertyChanged += OnPickerPropertyChanged;

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
                        AutomationId = "controls-sample-picker-parity-description",
                        Text = "Picker parity probe for placeholder/title color behavior, runtime selection sync, ItemsSource/SelectedItem updates, open-state requests, and style/alignment projection."
                    },
                    new Label
                    {
                        Text = "String items + title placeholder"
                    },
                    _stringPicker,
                    _selectionStateLabel,
                    _openStateLabel,
                    _styleStateLabel,
                    BuildStringPickerControls(),
                    new BoxView
                    {
                        HeightRequest = 1,
                        Color = Color.FromArgb("#CBD5E1")
                    },
                    new Label
                    {
                        Text = "ItemsSource + SelectedItem + ItemDisplayBinding"
                    },
                    _itemsPicker,
                    _itemsStateLabel,
                    BuildItemsPickerControls()
                }
            }
        };

        UpdateStateLabels();
    }

    private View BuildStringPickerControls()
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
                        CreateButton("Placeholder", "controls-sample-picker-parity-set-placeholder", () =>
                        {
                            _stringPicker.SelectedIndex = -1;
                        }),
                        CreateButton("Select Earth", "controls-sample-picker-parity-select-earth", () =>
                        {
                            _stringPicker.SelectedIndex = 2;
                        }),
                        CreateButton("Toggle Enabled", "controls-sample-picker-parity-toggle-enabled", () =>
                        {
                            _stringPicker.IsEnabled = !_stringPicker.IsEnabled;
                        })
                    }
                },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Open", "controls-sample-picker-parity-open", () =>
                        {
                            _stringPicker.IsOpen = true;
                        }),
                        CreateButton("Close", "controls-sample-picker-parity-close", () =>
                        {
                            _stringPicker.IsOpen = false;
                        }),
                        CreateButton("Toggle Style", "controls-sample-picker-parity-toggle-style", ToggleStringPickerStyle)
                    }
                }
            }
        };
    }

    private View BuildItemsPickerControls()
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
                        CreateButton("Toggle Binding", "controls-sample-picker-parity-toggle-binding", ToggleItemsBinding),
                        CreateButton("Replace Source", "controls-sample-picker-parity-replace-source", ReplaceItemsSource),
                        CreateButton("Select Last", "controls-sample-picker-parity-select-last-item", () =>
                        {
                            if (_itemsSource.Count > 0)
                            {
                                _itemsPicker.SelectedItem = _itemsSource[^1];
                            }
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

    private void OnPickerPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        _ = sender;
        _ = args;
        UpdateStateLabels();
    }

    private void ToggleStringPickerStyle()
    {
        _useAlternateStyle = !_useAlternateStyle;
        if (_useAlternateStyle)
        {
            _stringPicker.Background = new SolidColorBrush(Color.FromArgb("#DBEAFE"));
            _stringPicker.HorizontalTextAlignment = TextAlignment.End;
            _stringPicker.VerticalTextAlignment = TextAlignment.End;
        }
        else
        {
            _stringPicker.Background = new SolidColorBrush(Color.FromArgb("#F8FAFC"));
            _stringPicker.HorizontalTextAlignment = TextAlignment.Start;
            _stringPicker.VerticalTextAlignment = TextAlignment.Center;
        }

        UpdateStateLabels();
    }

    private void ToggleItemsBinding()
    {
        _useNameBinding = !_useNameBinding;
        _itemsPicker.ItemDisplayBinding = new Binding(_useNameBinding ? nameof(PickerPlanet.Name) : nameof(PickerPlanet.Display));
        UpdateStateLabels();
    }

    private void ReplaceItemsSource()
    {
        _useAlternateSource = !_useAlternateSource;
        _itemsSource = CreatePlanetSource(_useAlternateSource);
        _itemsPicker.ItemsSource = _itemsSource;
        _itemsPicker.SelectedItem = _itemsSource[Math.Min(1, _itemsSource.Count - 1)];
        UpdateStateLabels();
    }

    private void UpdateStateLabels()
    {
        var stringSelectedText = _stringPicker.SelectedIndex >= 0 && _stringPicker.SelectedIndex < _stringPicker.Items.Count
            ? _stringPicker.Items[_stringPicker.SelectedIndex]
            : "null";
        _selectionStateLabel.Text = $"selectedIndex={_stringPicker.SelectedIndex} selectedItem={stringSelectedText}";
        _openStateLabel.Text = $"isOpen={_stringPicker.IsOpen} enabled={_stringPicker.IsEnabled}";
        _styleStateLabel.Text = $"style={(_useAlternateStyle ? "alt" : "default")} hAlign={_stringPicker.HorizontalTextAlignment} vAlign={_stringPicker.VerticalTextAlignment}";

        var selectedPlanet = _itemsPicker.SelectedItem as PickerPlanet;
        var bindingMode = _useNameBinding ? "Name" : "Display";
        var sourceMode = _useAlternateSource ? "alt" : "default";
        _itemsStateLabel.Text = $"binding={bindingMode} source={sourceMode} selectedItem={selectedPlanet?.Name ?? "null"} count={_itemsSource.Count}";
    }

    private static ObservableCollection<PickerPlanet> CreatePlanetSource(bool useAlternateSource)
    {
        return useAlternateSource
            ? new ObservableCollection<PickerPlanet>
            {
                new("Mars", "Planet Mars"),
                new("Jupiter", "Planet Jupiter"),
                new("Saturn", "Planet Saturn")
            }
            : new ObservableCollection<PickerPlanet>
            {
                new("Mercury", "Planet Mercury"),
                new("Venus", "Planet Venus"),
                new("Earth", "Planet Earth")
            };
    }

    private sealed record PickerPlanet(string Name, string Display);
}
