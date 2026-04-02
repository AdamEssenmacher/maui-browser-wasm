using System.Collections.ObjectModel;
using AndroidSwipeViewConfiguration = Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.SwipeView;
using IOSSwipeViewConfiguration = Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.SwipeView;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace ControlsSample.Shared.App;

public sealed class SwipeViewParityProbePage : ContentPage
{
    private readonly SwipeView _standaloneSwipeView;
    private readonly CollectionView _collectionView;
    private readonly ObservableCollection<string> _collectionItems =
    [
        "Mercury",
        "Venus",
        "Earth",
        "Mars"
    ];

    private readonly Label _openStateLabel;
    private readonly Label _eventStateLabel;
    private readonly Label _invokeStateLabel;
    private readonly Label _modeStateLabel;
    private readonly Label _collectionStateLabel;

    private ProbeSwipeSide _activeSide = ProbeSwipeSide.Left;
    private bool _useSwipeItemView;
    private bool _isOpen;
    private int _thresholdIndex;
    private string _openSideState = "none";
    private string _lastCommandParameter = "<none>";
    private string _lastInvokedKind = "<none>";
    private SwipeMode _currentMode = SwipeMode.Reveal;
    private SwipeBehaviorOnInvoked _currentBehavior = SwipeBehaviorOnInvoked.Auto;
    private SwipeTransitionMode _currentTransitionMode = SwipeTransitionMode.Reveal;
    private int _startCount;
    private int _changeCount;
    private int _endCount;
    private int _invokeCount;
    private int _actionViewInteractionCount;
    private int _collectionInvokeCount;

    public SwipeViewParityProbePage()
    {
        Title = "SwipeView Parity Probe";
        AutomationId = "controls-sample-page-swipeviewparitypage";

        _standaloneSwipeView = new SwipeView
        {
            AutomationId = "controls-sample-swipeview-parity-host",
            WidthRequest = 280,
            HeightRequest = 76,
            Threshold = 72,
            LeftItems = new SwipeItems(),
            RightItems = new SwipeItems(),
            TopItems = new SwipeItems(),
            BottomItems = new SwipeItems(),
            Content = BuildStandaloneContent(),
            IsEnabled = true
        };
        ApplySwipeTransitionMode(_standaloneSwipeView, SwipeTransitionMode.Reveal);
        _standaloneSwipeView.SwipeStarted += OnSwipeStarted;
        _standaloneSwipeView.SwipeChanging += OnSwipeChanging;
        _standaloneSwipeView.SwipeEnded += OnSwipeEnded;

        _openStateLabel = new Label
        {
            AutomationId = "controls-sample-swipeview-parity-open-state",
            FontAttributes = FontAttributes.Bold
        };
        _eventStateLabel = new Label
        {
            AutomationId = "controls-sample-swipeview-parity-event-state"
        };
        _invokeStateLabel = new Label
        {
            AutomationId = "controls-sample-swipeview-parity-invoke-state"
        };
        _modeStateLabel = new Label
        {
            AutomationId = "controls-sample-swipeview-parity-mode-state"
        };
        _collectionStateLabel = new Label
        {
            AutomationId = "controls-sample-swipeview-parity-collection-state"
        };

        _collectionView = new CollectionView
        {
            AutomationId = "controls-sample-swipeview-parity-collection",
            HeightRequest = 220,
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical) { ItemSpacing = 8 },
            ItemsSource = _collectionItems,
            ItemTemplate = new DataTemplate(CreateCollectionRowTemplate)
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
                        AutomationId = "controls-sample-swipeview-parity-description",
                        Text = "SwipeView parity probe for four-direction touch swipes, reveal/execute modes, SwipeBehaviorOnInvoked, SwipeItemView action content, programmatic open/close, and CollectionView row embedding."
                    },
                    _standaloneSwipeView,
                    _openStateLabel,
                    _eventStateLabel,
                    _invokeStateLabel,
                    _modeStateLabel,
                    BuildControls(),
                    new Border
                    {
                        Padding = new Thickness(12),
                        StrokeThickness = 0,
                        Background = new SolidColorBrush(Color.FromArgb("#F8FAFC")),
                        Content = new VerticalStackLayout
                        {
                            Spacing = 8,
                            Children =
                            {
                                new Label
                                {
                                    FontAttributes = FontAttributes.Bold,
                                    Text = "CollectionView row SwipeView"
                                },
                                _collectionStateLabel,
                                _collectionView
                            }
                        }
                    }
                }
            }
        };

        ApplyStandaloneConfiguration();
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
                        CreateButton("Toggle Direction", "controls-sample-swipeview-parity-toggle-direction", ToggleDirection),
                        CreateButton("Toggle Mode", "controls-sample-swipeview-parity-toggle-mode", ToggleMode),
                        CreateButton("Toggle Behavior", "controls-sample-swipeview-parity-toggle-behavior", ToggleBehavior)
                    }
                },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Toggle Item Kind", "controls-sample-swipeview-parity-toggle-item-kind", ToggleItemKind),
                        CreateButton("Toggle Threshold", "controls-sample-swipeview-parity-toggle-threshold", ToggleThreshold),
                        CreateButton("Toggle Transition", "controls-sample-swipeview-parity-toggle-transition", ToggleTransition)
                    }
                },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Open", "controls-sample-swipeview-parity-open", OpenCurrentSide),
                        CreateButton("Close", "controls-sample-swipeview-parity-close", CloseCurrentSide),
                        CreateButton("Toggle Enabled", "controls-sample-swipeview-parity-toggle-enabled", ToggleEnabled)
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

    private View BuildStandaloneContent()
    {
        return new Border
        {
            Background = new SolidColorBrush(Color.FromArgb("#E2E8F0")),
            StrokeThickness = 0,
            Padding = new Thickness(14, 10),
            Content = new Label
            {
                AutomationId = "controls-sample-swipeview-parity-content-label",
                FontAttributes = FontAttributes.Bold,
                Text = "Standalone SwipeView"
            }
        };
    }

    private View CreateCollectionRowTemplate()
    {
        var label = new Label
        {
            FontAttributes = FontAttributes.Bold
        };
        label.SetBinding(Label.TextProperty, ".");

        var swipeItem = new SwipeItem
        {
            Text = "Flag",
            BackgroundColor = Color.FromArgb("#0F766E"),
            Command = new Command(() =>
            {
                _collectionInvokeCount++;
                UpdateStateLabels();
            })
        };

        var swipeView = new SwipeView
        {
            AutomationId = "controls-sample-swipeview-parity-row-swipe",
            WidthRequest = 280,
            HeightRequest = 68,
            LeftItems = new SwipeItems(),
            RightItems = new SwipeItems
            {
                swipeItem
            },
            TopItems = new SwipeItems(),
            BottomItems = new SwipeItems(),
            Content = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb("#F8FAFC")),
                StrokeThickness = 0,
                Padding = new Thickness(14, 10),
                Content = label
            }
        };
        return swipeView;
    }

    private void ToggleDirection()
    {
        _activeSide = _activeSide switch
        {
            ProbeSwipeSide.Left => ProbeSwipeSide.Right,
            ProbeSwipeSide.Right => ProbeSwipeSide.Top,
            ProbeSwipeSide.Top => ProbeSwipeSide.Bottom,
            _ => ProbeSwipeSide.Left
        };

        ApplyStandaloneConfiguration();
    }

    private void ToggleMode()
    {
        _currentMode = _currentMode == SwipeMode.Reveal ? SwipeMode.Execute : SwipeMode.Reveal;
        ApplyStandaloneConfiguration();
    }

    private void ToggleBehavior()
    {
        _currentBehavior = _currentBehavior switch
        {
            SwipeBehaviorOnInvoked.Auto => SwipeBehaviorOnInvoked.Close,
            SwipeBehaviorOnInvoked.Close => SwipeBehaviorOnInvoked.RemainOpen,
            _ => SwipeBehaviorOnInvoked.Auto
        };

        ApplyStandaloneConfiguration();
    }

    private void ToggleItemKind()
    {
        _useSwipeItemView = !_useSwipeItemView;
        ApplyStandaloneConfiguration();
    }

    private void ToggleThreshold()
    {
        _thresholdIndex = (_thresholdIndex + 1) % 3;
        _standaloneSwipeView.Threshold = _thresholdIndex switch
        {
            1 => 104,
            2 => 6,
            _ => 72
        };
        UpdateStateLabels();
    }

    private void ToggleTransition()
    {
        _currentTransitionMode = _currentTransitionMode == SwipeTransitionMode.Reveal
            ? SwipeTransitionMode.Drag
            : SwipeTransitionMode.Reveal;
        ApplySwipeTransitionMode(_standaloneSwipeView, _currentTransitionMode);
        UpdateStateLabels();
    }

    private void OpenCurrentSide()
    {
        _standaloneSwipeView.Open(MapSideToOpenSwipeItem(_activeSide), animated: true);
    }

    private void CloseCurrentSide()
    {
        _standaloneSwipeView.Close(animated: true);
        _isOpen = false;
        _openSideState = "none";
        UpdateStateLabels();
    }

    private void ToggleEnabled()
    {
        _standaloneSwipeView.IsEnabled = !_standaloneSwipeView.IsEnabled;
        UpdateStateLabels();
    }

    private void ApplyStandaloneConfiguration()
    {
        _standaloneSwipeView.Close(animated: false);
        _isOpen = false;
        _openSideState = "none";
        _lastCommandParameter = "<none>";
        _lastInvokedKind = "<none>";
        _actionViewInteractionCount = 0;

        _standaloneSwipeView.LeftItems = CreateSideItemsContainer();
        _standaloneSwipeView.RightItems = CreateSideItemsContainer();
        _standaloneSwipeView.TopItems = CreateSideItemsContainer();
        _standaloneSwipeView.BottomItems = CreateSideItemsContainer();
        ApplySwipeTransitionMode(_standaloneSwipeView, _currentTransitionMode);

        var sideItems = CreateCurrentSideItems();
        switch (_activeSide)
        {
            case ProbeSwipeSide.Left:
                _standaloneSwipeView.LeftItems = sideItems;
                break;
            case ProbeSwipeSide.Right:
                _standaloneSwipeView.RightItems = sideItems;
                break;
            case ProbeSwipeSide.Top:
                _standaloneSwipeView.TopItems = sideItems;
                break;
            case ProbeSwipeSide.Bottom:
                _standaloneSwipeView.BottomItems = sideItems;
                break;
        }

        UpdateStateLabels();
    }

    private SwipeItems CreateCurrentSideItems()
    {
        var swipeItems = new SwipeItems
        {
            Mode = _currentMode,
            SwipeBehaviorOnInvoked = _currentBehavior
        };

        if (_useSwipeItemView)
        {
            var actionView = new SwipeItemView
            {
                Content = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb("#DBEAFE")),
                    StrokeThickness = 0,
                    Padding = new Thickness(10, 8),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 6,
                        Children =
                        {
                            new Label
                            {
                                AutomationId = "controls-sample-swipeview-parity-action-view-label",
                                FontAttributes = FontAttributes.Bold,
                                Text = $"{_activeSide} view"
                            },
                            new Button
                            {
                                AutomationId = "controls-sample-swipeview-parity-action-view-button",
                                FontSize = 12,
                                Padding = new Thickness(8, 4),
                                Text = "Inner action",
                                Command = new Command(() =>
                                {
                                    _actionViewInteractionCount++;
                                    UpdateStateLabels();
                                })
                            }
                        }
                    }
                },
                Command = new Command<object?>(parameter =>
                {
                    _invokeCount++;
                    _lastCommandParameter = parameter?.ToString() ?? "<null>";
                    UpdateStateLabels();
                }),
                CommandParameter = $"{_activeSide.ToString().ToLowerInvariant()}-view"
            };
            actionView.Invoked += (_, _) =>
            {
                _lastInvokedKind = "view";
                UpdateStateLabels();
            };
            swipeItems.Add(actionView);
            return swipeItems;
        }

        var swipeItem = new SwipeItem
        {
            Text = $"{_activeSide} action",
            BackgroundColor = ResolveSideColor(_activeSide),
            Command = new Command<object?>(parameter =>
            {
                _invokeCount++;
                _lastCommandParameter = parameter?.ToString() ?? "<null>";
                UpdateStateLabels();
            }),
            CommandParameter = $"{_activeSide.ToString().ToLowerInvariant()}-menu"
        };
        swipeItem.Invoked += (_, _) =>
        {
            _lastInvokedKind = "menu";
            UpdateStateLabels();
        };
        swipeItems.Add(swipeItem);
        return swipeItems;
    }

    private SwipeItems CreateSideItemsContainer()
    {
        return new SwipeItems
        {
            Mode = _currentMode,
            SwipeBehaviorOnInvoked = _currentBehavior
        };
    }

    private void OnSwipeStarted(object? sender, SwipeStartedEventArgs e)
    {
        _ = sender;
        _startCount++;
        UpdateStateLabels();
    }

    private void OnSwipeChanging(object? sender, SwipeChangingEventArgs e)
    {
        _ = sender;
        _changeCount++;
        UpdateStateLabels();
    }

    private void OnSwipeEnded(object? sender, SwipeEndedEventArgs e)
    {
        _ = sender;
        _endCount++;
        _isOpen = e.IsOpen;
        _openSideState = e.IsOpen ? MapDirectionToOpenSide(e.SwipeDirection) : "none";
        UpdateStateLabels();
    }

    private void UpdateStateLabels()
    {
        _openStateLabel.Text = $"open={_isOpen} openSide={_openSideState} enabled={_standaloneSwipeView.IsEnabled}";
        _eventStateLabel.Text = $"events start={_startCount} change={_changeCount} end={_endCount}";
        _invokeStateLabel.Text = $"invokes={_invokeCount} lastParameter={_lastCommandParameter} lastKind={_lastInvokedKind} innerTaps={_actionViewInteractionCount}";
        _modeStateLabel.Text = $"side={_activeSide.ToString().ToLowerInvariant()} mode={_currentMode} behavior={_currentBehavior} itemKind={(_useSwipeItemView ? "view" : "menu")} threshold={_standaloneSwipeView.Threshold} transition={_currentTransitionMode}";
        _collectionStateLabel.Text = $"collectionInvokes={_collectionInvokeCount}";
    }

    private static OpenSwipeItem MapSideToOpenSwipeItem(ProbeSwipeSide side)
    {
        return side switch
        {
            ProbeSwipeSide.Left => OpenSwipeItem.LeftItems,
            ProbeSwipeSide.Right => OpenSwipeItem.RightItems,
            ProbeSwipeSide.Top => OpenSwipeItem.TopItems,
            _ => OpenSwipeItem.BottomItems
        };
    }

    private static string MapDirectionToOpenSide(SwipeDirection direction)
    {
        return direction switch
        {
            SwipeDirection.Right => "left",
            SwipeDirection.Left => "right",
            SwipeDirection.Down => "top",
            SwipeDirection.Up => "bottom",
            _ => "none"
        };
    }

    private static Color ResolveSideColor(ProbeSwipeSide side)
    {
        return side switch
        {
            ProbeSwipeSide.Left => Color.FromArgb("#0F766E"),
            ProbeSwipeSide.Right => Color.FromArgb("#B91C1C"),
            ProbeSwipeSide.Top => Color.FromArgb("#1D4ED8"),
            _ => Color.FromArgb("#7C3AED")
        };
    }

    private static void ApplySwipeTransitionMode(SwipeView swipeView, SwipeTransitionMode mode)
    {
        AndroidSwipeViewConfiguration.SetSwipeTransitionMode(swipeView, mode);
        IOSSwipeViewConfiguration.SetSwipeTransitionMode(swipeView, mode);
    }

    private enum ProbeSwipeSide
    {
        Left,
        Right,
        Top,
        Bottom
    }
}
