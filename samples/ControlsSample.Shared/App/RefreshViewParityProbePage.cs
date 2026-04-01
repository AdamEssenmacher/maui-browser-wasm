using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace ControlsSample.Shared.App;

public sealed class RefreshViewParityProbePage : ContentPage
{
    private readonly RefreshView _refreshView;
    private readonly ObservableCollection<string> _collectionItems =
    [
        "Alpha",
        "Beta",
        "Gamma",
        "Delta",
        "Epsilon",
        "Zeta",
        "Eta",
        "Theta",
        "Iota",
        "Kappa",
        "Lambda",
        "Mu"
    ];

    private readonly Label _refreshStateLabel;
    private readonly Label _enabledStateLabel;
    private readonly Label _modeStateLabel;
    private readonly Label _styleStateLabel;

    private RefreshContentMode _contentMode = RefreshContentMode.Collection;
    private bool _useAlternateColor;
    private int _commandCount;
    private string _lastCommandParameter = "<none>";

    public RefreshViewParityProbePage()
    {
        Title = "RefreshView Parity Probe";
        AutomationId = "controls-sample-page-refreshviewparitypage";

        _refreshView = new RefreshView
        {
            AutomationId = "controls-sample-refreshview-parity-host",
            WidthRequest = 280,
            HeightRequest = 264,
            IsRefreshEnabled = true,
            IsRefreshing = false,
            RefreshColor = Color.FromArgb("#0EA5E9"),
            Command = new Command<object?>(OnRefreshCommandExecuted),
            CommandParameter = "collection"
        };

        _refreshStateLabel = new Label
        {
            AutomationId = "controls-sample-refreshview-parity-refresh-state",
            FontAttributes = FontAttributes.Bold
        };
        _enabledStateLabel = new Label
        {
            AutomationId = "controls-sample-refreshview-parity-enabled-state"
        };
        _modeStateLabel = new Label
        {
            AutomationId = "controls-sample-refreshview-parity-mode-state"
        };
        _styleStateLabel = new Label
        {
            AutomationId = "controls-sample-refreshview-parity-style-state"
        };

        _refreshView.PropertyChanged += (_, _) => UpdateStateLabels();
        _refreshView.Content = CreateCollectionContent();

        Content = new VerticalStackLayout
        {
            Padding = new Thickness(16),
            Spacing = 12,
            Children =
            {
                new Label
                {
                    AutomationId = "controls-sample-refreshview-parity-description",
                    Text = "RefreshView parity probe for trigger-button refresh, pull-threshold activation across collection and plain content, enabled gating, and deterministic browser diagnostics."
                },
                _refreshView,
                _refreshStateLabel,
                _enabledStateLabel,
                _modeStateLabel,
                _styleStateLabel,
                BuildControls(),
                new Label
                {
                    AutomationId = "controls-sample-refreshview-parity-flow-sentinel",
                    FontSize = 12,
                    Text = "Flow sentinel: this label should stay below the RefreshView host."
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
                        CreateButton("Complete", "controls-sample-refreshview-parity-complete-refresh", CompleteRefresh),
                        CreateButton("Toggle Enabled", "controls-sample-refreshview-parity-toggle-enabled", ToggleEnabled),
                        CreateButton("Toggle Refresh", "controls-sample-refreshview-parity-toggle-refresh-enabled", ToggleRefreshEnabled)
                    }
                },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Toggle Mode", "controls-sample-refreshview-parity-toggle-mode", ToggleContentMode),
                        CreateButton("Toggle Color", "controls-sample-refreshview-parity-toggle-color", ToggleColor)
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

    private void OnRefreshCommandExecuted(object? parameter)
    {
        _commandCount++;
        _lastCommandParameter = parameter?.ToString() ?? "<null>";
        UpdateStateLabels();
    }

    private void CompleteRefresh()
    {
        _refreshView.IsRefreshing = false;
        UpdateStateLabels();
    }

    private void ToggleEnabled()
    {
        _refreshView.IsEnabled = !_refreshView.IsEnabled;
        UpdateStateLabels();
    }

    private void ToggleRefreshEnabled()
    {
        _refreshView.IsRefreshEnabled = !_refreshView.IsRefreshEnabled;
        if (!_refreshView.IsRefreshEnabled)
        {
            _refreshView.IsRefreshing = false;
        }

        UpdateStateLabels();
    }

    private void ToggleContentMode()
    {
        _contentMode = _contentMode == RefreshContentMode.Collection
            ? RefreshContentMode.Plain
            : RefreshContentMode.Collection;

        _refreshView.CommandParameter = _contentMode == RefreshContentMode.Collection
            ? "collection"
            : "plain";
        _refreshView.Content = _contentMode == RefreshContentMode.Collection
            ? CreateCollectionContent()
            : CreatePlainContent();
        UpdateStateLabels();
    }

    private void ToggleColor()
    {
        _useAlternateColor = !_useAlternateColor;
        _refreshView.RefreshColor = _useAlternateColor
            ? Color.FromArgb("#F97316")
            : Color.FromArgb("#0EA5E9");
        UpdateStateLabels();
    }

    private View CreateCollectionContent()
    {
        return new CollectionView
        {
            AutomationId = "controls-sample-refreshview-parity-collection",
            HeightRequest = 220,
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical) { ItemSpacing = 6 },
            ItemsSource = _collectionItems,
            ItemTemplate = new DataTemplate(() =>
            {
                var title = new Label
                {
                    FontSize = 13,
                    TextColor = Colors.Black
                };
                title.SetBinding(Label.TextProperty, ".");

                return new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb("#E2E8F0")),
                    Stroke = Colors.Transparent,
                    Padding = new Thickness(10, 8),
                    Content = title
                };
            })
        };
    }

    private View CreatePlainContent()
    {
        var layout = new VerticalStackLayout
        {
            AutomationId = "controls-sample-refreshview-parity-plain",
            Spacing = 8,
            Padding = new Thickness(10, 8)
        };

        for (var i = 0; i < 16; i++)
        {
            layout.Children.Add(new Border
            {
                Background = new SolidColorBrush(i % 2 == 0 ? Color.FromArgb("#DBEAFE") : Color.FromArgb("#F8FAFC")),
                Stroke = Colors.Transparent,
                Padding = new Thickness(10, 8),
                Content = new Label
                {
                    FontSize = 13,
                    Text = $"Plain row {i + 1}",
                    TextColor = Colors.Black
                }
            });
        }

        return layout;
    }

    private void UpdateStateLabels()
    {
        _refreshStateLabel.Text = $"isRefreshing={_refreshView.IsRefreshing} commands={_commandCount} lastParameter={_lastCommandParameter}";
        _enabledStateLabel.Text = $"enabled={_refreshView.IsEnabled} refreshEnabled={_refreshView.IsRefreshEnabled}";
        _modeStateLabel.Text = $"mode={(_contentMode == RefreshContentMode.Collection ? "collection" : "plain")}";
        _styleStateLabel.Text = $"color={(_useAlternateColor ? "alt" : "default")}";
    }

    private enum RefreshContentMode
    {
        Collection,
        Plain
    }
}
