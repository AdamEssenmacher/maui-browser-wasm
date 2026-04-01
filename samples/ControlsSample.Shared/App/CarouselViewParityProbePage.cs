using System.Collections.Specialized;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace ControlsSample.Shared.App;

public sealed class CarouselViewParityProbePage : ContentPage
{
    private readonly ObservableCollection<CarouselProbeItem> _items =
    [
        new("Alpha", "Current"),
        new("Beta", "Next"),
        new("Gamma", "Explore"),
        new("Delta", "Wrap")
    ];

    private readonly CarouselView _carousel;
    private readonly IndicatorView _indicator;
    private readonly Label _positionLabel;
    private readonly Label _modeLabel;
    private readonly Label _stateLabel;
    private bool _peekEnabled;

    public CarouselViewParityProbePage()
    {
        Title = "CarouselView Parity Probe";
        AutomationId = "controls-sample-page-carouselviewparitypage";

        _indicator = new IndicatorView
        {
            AutomationId = "controls-sample-carouselview-parity-indicator",
            HorizontalOptions = LayoutOptions.Center,
            IndicatorColor = Color.FromArgb("#94A3B8"),
            SelectedIndicatorColor = Color.FromArgb("#1D4ED8")
        };

        _carousel = new CarouselView
        {
            AutomationId = "controls-sample-carouselview-parity-host",
            HeightRequest = 220,
            ItemsSource = _items,
            ItemsLayout = CreateLinearLayout(ItemsLayoutOrientation.Horizontal),
            ItemTemplate = BuildItemTemplate(),
            IndicatorView = _indicator,
            CurrentItem = _items[0],
            Position = 0,
            IsSwipeEnabled = true,
            IsBounceEnabled = true,
            Loop = false
        };

        _positionLabel = new Label
        {
            AutomationId = "controls-sample-carouselview-parity-position",
            FontAttributes = FontAttributes.Bold
        };

        _modeLabel = new Label
        {
            AutomationId = "controls-sample-carouselview-parity-mode"
        };

        _stateLabel = new Label
        {
            AutomationId = "controls-sample-carouselview-parity-state"
        };

        _carousel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(CarouselView.Position)
                or nameof(CarouselView.CurrentItem)
                or nameof(CarouselView.ItemsLayout)
                or nameof(CarouselView.IsSwipeEnabled)
                or nameof(CarouselView.PeekAreaInsets)
                or nameof(CarouselView.IsDragging)
                or nameof(CarouselView.Loop))
            {
                UpdateStateLabels();
            }
        };

        _indicator.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(IndicatorView.Position) or nameof(IndicatorView.Count))
            {
                UpdateStateLabels();
            }
        };

        if (_carousel.VisibleViews is INotifyCollectionChanged visibleViewsChanged)
        {
            visibleViewsChanged.CollectionChanged += (_, _) => UpdateStateLabels();
        }

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
                        AutomationId = "controls-sample-carouselview-parity-description",
                        Text = "CarouselView parity probe for snapped position/current-item updates, orientation/peek changes, swipe gating, loop behavior, and linked IndicatorView roundtrip."
                    },
                    _carousel,
                    _indicator,
                    _positionLabel,
                    _modeLabel,
                    _stateLabel,
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
                        CreateButton("Next Position", "controls-sample-carouselview-parity-next-position", MoveNext),
                        CreateButton("Set Current Gamma", "controls-sample-carouselview-parity-set-current-gamma", () =>
                        {
                            _carousel.CurrentItem = _items[2];
                        }),
                        CreateButton("Animated ScrollTo Gamma", "controls-sample-carouselview-parity-animate-gamma", () =>
                        {
                            _carousel.ScrollTo(2, position: ScrollToPosition.Center, animate: true);
                        }),
                        CreateButton("ScrollTo Delta", "controls-sample-carouselview-parity-scrollto-delta", () =>
                        {
                            _carousel.ScrollTo(3, position: ScrollToPosition.Center, animate: false);
                        })
                    }
                },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Toggle Orientation", "controls-sample-carouselview-parity-toggle-orientation", ToggleOrientation),
                        CreateButton("Toggle Peek", "controls-sample-carouselview-parity-toggle-peek", TogglePeek),
                        CreateButton("Toggle Swipe", "controls-sample-carouselview-parity-toggle-swipe", () =>
                        {
                            _carousel.IsSwipeEnabled = !_carousel.IsSwipeEnabled;
                        })
                    }
                },
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        CreateButton("Toggle Loop", "controls-sample-carouselview-parity-toggle-loop", () =>
                        {
                            _carousel.Loop = !_carousel.Loop;
                        }),
                        CreateButton("Reset Alpha", "controls-sample-carouselview-parity-reset-alpha", () =>
                        {
                            _carousel.CurrentItem = _items[0];
                            _carousel.Position = 0;
                        })
                    }
                }
            }
        };
    }

    private DataTemplate BuildItemTemplate()
    {
        return new DataTemplate(() =>
        {
            var title = new Label
            {
                FontAttributes = FontAttributes.Bold,
                FontSize = 20,
                TextColor = Color.FromArgb("#0F172A")
            };
            title.SetBinding(Label.TextProperty, nameof(CarouselProbeItem.Title));

            var subtitle = new Label
            {
                FontSize = 14,
                TextColor = Color.FromArgb("#475569")
            };
            subtitle.SetBinding(Label.TextProperty, nameof(CarouselProbeItem.Subtitle));

            var content = new VerticalStackLayout
            {
                Padding = new Thickness(20),
                Spacing = 8,
                Children =
                {
                    title,
                    subtitle
                }
            };

            var card = new Border
            {
                Margin = new Thickness(0),
                Padding = new Thickness(0),
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(Color.FromArgb("#CBD5E1")),
                Background = new SolidColorBrush(Color.FromArgb("#FFFFFF")),
                Content = content
            };

            VisualStateManager.SetVisualStateGroups(card, new VisualStateGroupList
            {
                new VisualStateGroup
                {
                    Name = "CarouselStates",
                    States =
                    {
                        new VisualState
                        {
                            Name = CarouselView.DefaultItemVisualState,
                            Setters =
                            {
                                new Setter { Property = Border.BackgroundProperty, Value = new SolidColorBrush(Color.FromArgb("#FFFFFF")) },
                                new Setter { Property = Border.StrokeProperty, Value = new SolidColorBrush(Color.FromArgb("#CBD5E1")) },
                                new Setter { Property = ScaleProperty, Value = 0.96 }
                            }
                        },
                        new VisualState
                        {
                            Name = CarouselView.CurrentItemVisualState,
                            Setters =
                            {
                                new Setter { Property = Border.BackgroundProperty, Value = new SolidColorBrush(Color.FromArgb("#DBEAFE")) },
                                new Setter { Property = Border.StrokeProperty, Value = new SolidColorBrush(Color.FromArgb("#2563EB")) },
                                new Setter { Property = ScaleProperty, Value = 1.0 }
                            }
                        },
                        new VisualState
                        {
                            Name = CarouselView.NextItemVisualState,
                            Setters =
                            {
                                new Setter { Property = Border.BackgroundProperty, Value = new SolidColorBrush(Color.FromArgb("#F8FAFC")) },
                                new Setter { Property = Border.StrokeProperty, Value = new SolidColorBrush(Color.FromArgb("#93C5FD")) },
                                new Setter { Property = ScaleProperty, Value = 0.98 }
                            }
                        },
                        new VisualState
                        {
                            Name = CarouselView.PreviousItemVisualState,
                            Setters =
                            {
                                new Setter { Property = Border.BackgroundProperty, Value = new SolidColorBrush(Color.FromArgb("#EFF6FF")) },
                                new Setter { Property = Border.StrokeProperty, Value = new SolidColorBrush(Color.FromArgb("#60A5FA")) },
                                new Setter { Property = ScaleProperty, Value = 0.98 }
                            }
                        }
                    }
                }
            });

            return card;
        });
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

    private void MoveNext()
    {
        if (_items.Count == 0)
        {
            _carousel.Position = 0;
            return;
        }

        var current = Math.Clamp(_carousel.Position, 0, _items.Count - 1);
        _carousel.Position = (current + 1) % _items.Count;
    }

    private void ToggleOrientation()
    {
        var current = (_carousel.ItemsLayout as LinearItemsLayout)?.Orientation ?? ItemsLayoutOrientation.Horizontal;
        _carousel.ItemsLayout = CreateLinearLayout(
            current == ItemsLayoutOrientation.Horizontal
                ? ItemsLayoutOrientation.Vertical
                : ItemsLayoutOrientation.Horizontal);
    }

    private void TogglePeek()
    {
        _peekEnabled = !_peekEnabled;
        _carousel.PeekAreaInsets = _peekEnabled ? new Thickness(12, 0, 36, 0) : new Thickness(0);
    }

    private void UpdateStateLabels()
    {
        var current = (_carousel.CurrentItem as CarouselProbeItem)?.Title ?? "null";
        var orientation = (_carousel.ItemsLayout as LinearItemsLayout)?.Orientation ?? ItemsLayoutOrientation.Horizontal;
        _positionLabel.Text = $"position={_carousel.Position} current={current}";
        _modeLabel.Text = $"orientation={orientation} peek={FormatThickness(_carousel.PeekAreaInsets)} swipe={_carousel.IsSwipeEnabled} loop={_carousel.Loop}";
        _stateLabel.Text = $"dragging={_carousel.IsDragging} visible={_carousel.VisibleViews.Count} indicator={_indicator.Position}/{_indicator.Count}";
    }

    private static LinearItemsLayout CreateLinearLayout(ItemsLayoutOrientation orientation)
    {
        return new LinearItemsLayout(orientation)
        {
            ItemSpacing = 0
        };
    }

    private static string FormatThickness(Thickness thickness)
    {
        return $"{thickness.Left:0},{thickness.Top:0},{thickness.Right:0},{thickness.Bottom:0}";
    }

    private sealed record CarouselProbeItem(string Title, string Subtitle);
}
