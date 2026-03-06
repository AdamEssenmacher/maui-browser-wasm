using System.ComponentModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Maui.Browser.Sample.Demo;

internal sealed class NavigationScrollDetailPage : ContentPage
{
    private readonly DemoState _state;
    private readonly Entry _detailEntry;
    private readonly Label _summaryLabel;

    public NavigationScrollDetailPage(DemoState state)
    {
        _state = state;
        BindingContext = _state;
        Title = "Navigation Detail";
        AutomationId = "nav-detail-page";

        var heading = new Label
        {
            AutomationId = "nav-detail-heading",
            Text = "Navigation + Scroll Detail",
            FontSize = 20,
            TextColor = Colors.Black
        };

        var popButton = new Button
        {
            AutomationId = "nav-pop-button",
            Text = "Pop detail page",
            TextColor = Colors.Black,
            Padding = new Thickness(10, 8)
        };
        popButton.Clicked += async (_, _) => await Navigation.PopAsync(false);

        _detailEntry = new Entry
        {
            AutomationId = "nav-detail-entry",
            Placeholder = "Detail text",
            FontSize = 14,
            Text = _state.NavigationDetailText
        };
        _detailEntry.TextChanged += (_, args) =>
        {
            _state.NavigationDetailText = args.NewTextValue ?? string.Empty;
        };

        var counterButton = new Button
        {
            AutomationId = "nav-detail-counter-button",
            Text = "Increment detail counter",
            TextColor = Colors.Black,
            Padding = new Thickness(10, 8)
        };
        counterButton.Clicked += (_, _) => _state.IncrementNavigationDetailCounter();

        _summaryLabel = new Label
        {
            AutomationId = "nav-detail-summary-label",
            FontSize = 12,
            TextColor = Colors.DarkSlateGray,
            Text = _state.NavigationSummary
        };

        var stack = new VerticalStackLayout
        {
            HeightRequest = 1800,
            Spacing = 10,
            Padding = new Thickness(16)
        };

        stack.Children.Add(heading);
        stack.Children.Add(popButton);
        stack.Children.Add(_detailEntry);
        stack.Children.Add(counterButton);
        stack.Children.Add(_summaryLabel);

        for (var i = 1; i <= 36; i++)
        {
            stack.Children.Add(new Label
            {
                Text = $"Scroll probe row {i:00}",
                FontSize = 12,
                TextColor = Colors.DarkSlateGray
            });
        }

        stack.Children.Add(new Label
        {
            AutomationId = "nav-scroll-target",
            Text = "Reached scroll target",
            FontSize = 13,
            TextColor = Colors.DarkBlue
        });

        Content = new ScrollView
        {
            AutomationId = "nav-scroll-view",
            Orientation = ScrollOrientation.Vertical,
            Content = stack
        };

        _state.PropertyChanged += OnStatePropertyChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _state.PropertyChanged -= OnStatePropertyChanged;
        _state.RecordNavigationPop();
    }

    private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName is nameof(DemoState.NavigationSummary))
        {
            _summaryLabel.Text = _state.NavigationSummary;
        }

        if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName is nameof(DemoState.NavigationDetailText))
        {
            var updatedText = _state.NavigationDetailText;
            if (!string.Equals(_detailEntry.Text, updatedText, StringComparison.Ordinal))
            {
                _detailEntry.Text = updatedText;
            }
        }
    }
}
