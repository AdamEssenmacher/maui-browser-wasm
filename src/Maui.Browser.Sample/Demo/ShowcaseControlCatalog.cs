using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;

namespace Maui.Browser.Sample.Demo;

internal enum ShowcaseControlStatus
{
    Complete,
    Partial,
    Missing,
    GapDocumented
}

internal sealed class ShowcaseControlSpec
{
    public ShowcaseControlSpec(
        string typeName,
        string category,
        ShowcaseControlStatus status,
        string summary,
        Func<View>? probeFactory = null,
        Func<Page>? scenarioPageFactory = null,
        string? scenarioButtonText = null)
    {
        TypeName = typeName;
        Category = category;
        Status = status;
        Summary = summary;
        ProbeFactory = probeFactory;
        ScenarioPageFactory = scenarioPageFactory;
        ScenarioButtonText = scenarioButtonText;

        Slug = CreateSlug(typeName);
        OpenButtonAutomationId = $"showcase-open-control-{Slug}";
        DemoPageAutomationId = $"showcase-page-control-{Slug}";
    }

    public string TypeName { get; }

    public string Category { get; }

    public ShowcaseControlStatus Status { get; }

    public string Summary { get; }

    public Func<View>? ProbeFactory { get; }

    public Func<Page>? ScenarioPageFactory { get; }

    public string? ScenarioButtonText { get; }

    public string Slug { get; }

    public string OpenButtonAutomationId { get; }

    public string DemoPageAutomationId { get; }

    public bool IsImplemented => Status != ShowcaseControlStatus.Missing;

    public string StatusText => Status.ToString().ToLowerInvariant().Replace("gapdocumented", "gap-documented");

    private static string CreateSlug(string text)
    {
        var buffer = new char[text.Length];
        var index = 0;

        foreach (var character in text)
        {
            if (!char.IsLetterOrDigit(character))
            {
                continue;
            }

            buffer[index++] = char.ToLowerInvariant(character);
        }

        return new string(buffer, 0, index);
    }
}

internal static class ShowcaseControlCatalog
{
    public static IReadOnlyList<ShowcaseControlSpec> All { get; } =
    [
        new("Window", "Host", ShowcaseControlStatus.Complete, "Top-level MAUI window lifecycle is mapped by BrowserWindowHandler.", scenarioPageFactory: static () => new ParityNavigationWindowPage(), scenarioButtonText: "Open navigation/window depth"),
        new("NavigationPage", "Host", ShowcaseControlStatus.Partial, "Navigation bar + push/pop flow is implemented for single active stack.", scenarioPageFactory: static () => new ParityNavigationWindowPage(), scenarioButtonText: "Open navigation/window depth"),
        new("ContentPage", "Host", ShowcaseControlStatus.Complete, "ContentPage hosting is implemented with browser page-level content projection.", scenarioPageFactory: static () => new XamlDemoPage(), scenarioButtonText: "Open control lab"),
        new("ContentView", "Containers", ShowcaseControlStatus.Complete, "Single-child content hosting is implemented.", probeFactory: CreateContentViewProbe),
        new("ScrollView", "Containers", ShowcaseControlStatus.Partial, "Native overflow scrolling + programmatic scroll command mapping.", probeFactory: CreateScrollViewProbe),
        new("VerticalStackLayout", "Layouts", ShowcaseControlStatus.Complete, "Semantic CSS flex-column projection.", probeFactory: CreateVerticalStackProbe),
        new("HorizontalStackLayout", "Layouts", ShowcaseControlStatus.Complete, "Semantic CSS flex-row projection.", probeFactory: CreateHorizontalStackProbe),
        new("Grid", "Layouts", ShowcaseControlStatus.Complete, "Semantic CSS grid row/column/span projection.", probeFactory: CreateGridProbe),
        new("AbsoluteLayout", "Layouts", ShowcaseControlStatus.Complete, "Absolute frame projection path remains supported.", probeFactory: CreateAbsoluteLayoutProbe),
        new("FlexLayout", "Layouts", ShowcaseControlStatus.Partial, "Flex container + attached item property mapping is implemented.", probeFactory: CreateFlexLayoutProbe),
        new("Label", "Text/Input", ShowcaseControlStatus.Partial, "Interactive label depth lab with live property editing and explicit gap probes.", probeFactory: CreateLabelProbe, scenarioPageFactory: static () => new ParityLabelPage(), scenarioButtonText: "Open label depth"),
        new("Button", "Text/Input", ShowcaseControlStatus.Complete, "Text, click, style-state, border/corner mapping.", probeFactory: CreateButtonProbe),
        new("Entry", "Text/Input", ShowcaseControlStatus.Partial, "Two-way text, placeholder, keyboard, and completion mapping.", probeFactory: CreateEntryProbe),
        new("Editor", "Text/Input", ShowcaseControlStatus.Partial, "Multiline text edit + change flow mapping.", probeFactory: CreateEditorProbe),
        new("SearchBar", "Text/Input", ShowcaseControlStatus.Partial, "Search submit and text flow mapping.", probeFactory: CreateSearchBarProbe),
        new("CheckBox", "Form", ShowcaseControlStatus.Complete, "Boolean checked-state projection + change flow.", probeFactory: CreateCheckBoxProbe),
        new("Switch", "Form", ShowcaseControlStatus.Partial, "Dedicated toggle rendering with intrinsic thumb/track behavior.", probeFactory: CreateSwitchProbe),
        new("Slider", "Form", ShowcaseControlStatus.Partial, "Range value mapping with live input/change updates.", probeFactory: CreateSliderProbe),
        new("Stepper", "Form", ShowcaseControlStatus.Partial, "Increment/decrement and bounded numeric value mapping.", probeFactory: CreateStepperProbe),
        new("Picker", "Form", ShowcaseControlStatus.Partial, "Item + selected-index two-way mapping and title prompt behavior.", probeFactory: CreatePickerProbe),
        new("DatePicker", "Form", ShowcaseControlStatus.Partial, "Date value mapping via browser date input semantics.", probeFactory: CreateDatePickerProbe),
        new("TimePicker", "Form", ShowcaseControlStatus.Partial, "Time value mapping via browser time input semantics.", probeFactory: CreateTimePickerProbe),
        new("ProgressBar", "Form", ShowcaseControlStatus.Partial, "Progress value style projection.", probeFactory: CreateProgressBarProbe),
        new("ActivityIndicator", "Form", ShowcaseControlStatus.Partial, "IsRunning + visibility animation mapping.", probeFactory: CreateActivityIndicatorProbe),
        new("Image", "Media/Visual", ShowcaseControlStatus.Partial, "URI/file/font-image source projection + aspect mapping.", probeFactory: CreateImageProbe),
        new("ImageButton", "Media/Visual", ShowcaseControlStatus.Partial, "Image source projection with click + press/release flow.", probeFactory: CreateImageButtonProbe),
        new("Border", "Media/Visual", ShowcaseControlStatus.Partial, "Stroke/thickness/radius + child-host mapping.", probeFactory: CreateBorderProbe),
        new("GraphicsView", "Media/Visual", ShowcaseControlStatus.Missing, "GraphicsView handler has not been implemented yet."),
        new("CollectionView", "Collection", ShowcaseControlStatus.Partial, "Non-virtualized realization with selection, mutations, header/footer, and scroll-to.", probeFactory: CreateCollectionViewProbe),
        new("CarouselView", "Collection", ShowcaseControlStatus.Missing, "CarouselView handler has not been implemented yet."),
        new("IndicatorView", "Collection", ShowcaseControlStatus.Missing, "IndicatorView handler has not been implemented yet."),
        new("RefreshView", "Collection", ShowcaseControlStatus.Partial, "Command/state-first refresh flow plus pull-threshold activation.", probeFactory: CreateRefreshViewProbe),
        new("SwipeView", "Interaction", ShowcaseControlStatus.Missing, "SwipeView handler has not been implemented yet."),
        new("WebView", "Interaction", ShowcaseControlStatus.Missing, "WebView handler has not been implemented yet.")
    ];

    private static View CreateLabelProbe()
    {
        return new Label
        {
            Text = "Label probe: dynamic text rendering in the browser handler.",
            FontSize = 16
        };
    }

    private static View CreateButtonProbe()
    {
        var clicks = 0;
        var summary = new Label
        {
            Text = "Button clicks: 0",
            FontSize = 13
        };

        var button = new Button
        {
            Text = "Click button probe"
        };

        button.Clicked += (_, _) =>
        {
            clicks++;
            summary.Text = $"Button clicks: {clicks}";
        };

        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                button,
                summary
            }
        };
    }

    private static View CreateEntryProbe()
    {
        var summary = new Label
        {
            Text = "Entry text: <empty>",
            FontSize = 13
        };

        var entry = new Entry
        {
            Placeholder = "Type entry text"
        };

        entry.TextChanged += (_, _) =>
        {
            summary.Text = $"Entry text: {entry.Text ?? "<empty>"}";
        };

        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                entry,
                summary
            }
        };
    }

    private static View CreateEditorProbe()
    {
        var summary = new Label
        {
            Text = "Editor chars: 0",
            FontSize = 13
        };

        var editor = new Editor
        {
            Placeholder = "Type multiline text",
            HeightRequest = 96
        };

        editor.TextChanged += (_, _) =>
        {
            summary.Text = $"Editor chars: {(editor.Text ?? string.Empty).Length}";
        };

        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                editor,
                summary
            }
        };
    }

    private static View CreateSearchBarProbe()
    {
        var summary = new Label
        {
            Text = "Search submit count: 0",
            FontSize = 13
        };

        var count = 0;
        var searchBar = new SearchBar
        {
            Placeholder = "Search query"
        };

        searchBar.SearchButtonPressed += (_, _) =>
        {
            count++;
            summary.Text = $"Search submit count: {count} ({searchBar.Text ?? "<empty>"})";
        };

        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                searchBar,
                summary
            }
        };
    }

    private static View CreateCheckBoxProbe()
    {
        var summary = new Label
        {
            Text = "Checked: false",
            FontSize = 13
        };

        var checkBox = new CheckBox();
        checkBox.CheckedChanged += (_, args) =>
        {
            summary.Text = $"Checked: {args.Value.ToString().ToLowerInvariant()}";
        };

        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                checkBox,
                summary
            }
        };
    }

    private static View CreateSwitchProbe()
    {
        var summary = new Label
        {
            Text = "IsOn: false",
            FontSize = 13
        };

        var @switch = new Switch();
        @switch.Toggled += (_, args) =>
        {
            summary.Text = $"IsOn: {args.Value.ToString().ToLowerInvariant()}";
        };

        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                @switch,
                summary
            }
        };
    }

    private static View CreateSliderProbe()
    {
        var summary = new Label
        {
            Text = "Slider value: 35.0",
            FontSize = 13
        };

        var slider = new Slider
        {
            Minimum = 0,
            Maximum = 100,
            Value = 35
        };

        slider.ValueChanged += (_, args) =>
        {
            summary.Text = $"Slider value: {args.NewValue:0.0}";
        };

        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                slider,
                summary
            }
        };
    }

    private static View CreateStepperProbe()
    {
        var summary = new Label
        {
            Text = "Stepper value: 2.0",
            FontSize = 13
        };

        var stepper = new Stepper
        {
            Minimum = 0,
            Maximum = 10,
            Increment = 0.5,
            Value = 2
        };

        stepper.ValueChanged += (_, args) =>
        {
            summary.Text = $"Stepper value: {args.NewValue:0.0}";
        };

        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                stepper,
                summary
            }
        };
    }

    private static View CreatePickerProbe()
    {
        var items = new[] { "Alpha", "Beta", "Gamma" };
        var summary = new Label
        {
            Text = "Selected: 0 (Alpha)",
            FontSize = 13
        };

        var picker = new Picker
        {
            Title = "Choose item",
            ItemsSource = items,
            SelectedIndex = 0
        };

        picker.SelectedIndexChanged += (_, _) =>
        {
            var index = picker.SelectedIndex;
            var text = index >= 0 && index < items.Length ? items[index] : "<none>";
            summary.Text = $"Selected: {index} ({text})";
        };

        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                picker,
                summary
            }
        };
    }

    private static View CreateDatePickerProbe()
    {
        var summary = new Label
        {
            Text = "Date: 2026-03-07",
            FontSize = 13
        };

        var picker = new DatePicker
        {
            Date = new DateTime(2026, 3, 7),
            MinimumDate = new DateTime(2020, 1, 1),
            MaximumDate = new DateTime(2030, 12, 31),
            Format = "yyyy-MM-dd"
        };

        picker.DateSelected += (_, args) =>
        {
            summary.Text = $"Date: {args.NewDate:yyyy-MM-dd}";
        };

        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                picker,
                summary
            }
        };
    }

    private static View CreateTimePickerProbe()
    {
        var summary = new Label
        {
            Text = "Time: 09:30",
            FontSize = 13
        };

        var picker = new TimePicker
        {
            Time = new TimeSpan(9, 30, 0),
            Format = "HH:mm"
        };

        picker.PropertyChanged += (_, args) =>
        {
            if (!string.Equals(args.PropertyName, nameof(TimePicker.Time), StringComparison.Ordinal))
            {
                return;
            }

            summary.Text = $"Time: {picker.Time:hh\\:mm}";
        };

        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                picker,
                summary
            }
        };
    }

    private static View CreateProgressBarProbe()
    {
        var progressBar = new ProgressBar
        {
            Progress = 0.4
        };

        var summary = new Label
        {
            Text = "Progress: 0.40",
            FontSize = 13
        };

        var slider = new Slider
        {
            Minimum = 0,
            Maximum = 1,
            Value = 0.4
        };

        slider.ValueChanged += (_, args) =>
        {
            progressBar.Progress = args.NewValue;
            summary.Text = $"Progress: {args.NewValue:0.00}";
        };

        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                progressBar,
                slider,
                summary
            }
        };
    }

    private static View CreateActivityIndicatorProbe()
    {
        var indicator = new ActivityIndicator
        {
            IsRunning = true
        };

        var summary = new Label
        {
            Text = "IsRunning: true",
            FontSize = 13
        };

        var button = new Button
        {
            Text = "Toggle running"
        };

        button.Clicked += (_, _) =>
        {
            indicator.IsRunning = !indicator.IsRunning;
            summary.Text = $"IsRunning: {indicator.IsRunning.ToString().ToLowerInvariant()}";
        };

        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                indicator,
                button,
                summary
            }
        };
    }

    private static View CreateImageProbe()
    {
        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Image
                {
                    HeightRequest = 120,
                    WidthRequest = 180,
                    Aspect = Aspect.AspectFit,
                    Source = "assets/showcase/hero-grid.svg"
                },
                new Label
                {
                    Text = "Source: assets/showcase/hero-grid.svg",
                    FontSize = 13
                }
            }
        };
    }

    private static View CreateImageButtonProbe()
    {
        var clicks = 0;
        var summary = new Label
        {
            Text = "Clicks: 0",
            FontSize = 13
        };

        var button = new ImageButton
        {
            Source = "assets/showcase/hero-pulse.svg",
            HeightRequest = 72,
            WidthRequest = 72,
            Aspect = Aspect.AspectFit
        };

        button.Clicked += (_, _) =>
        {
            clicks++;
            summary.Text = $"Clicks: {clicks}";
        };

        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                button,
                summary
            }
        };
    }

    private static View CreateBorderProbe()
    {
        return new Border
        {
            Stroke = new SolidColorBrush(Color.FromArgb("#7C9CFF")),
            StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Padding = new Thickness(12),
            Content = new Label
            {
                Text = "Border child content",
                HorizontalTextAlignment = TextAlignment.Center
            }
        };
    }

    private static View CreateCollectionViewProbe()
    {
        var items = new ObservableCollection<string> { "Alpha", "Beta", "Gamma" };
        var summary = new Label
        {
            Text = $"Items: {items.Count}",
            FontSize = 13
        };

        var collectionView = new CollectionView
        {
            HeightRequest = 170,
            ItemsSource = items,
            ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label();
                var host = new Border
                {
                    StrokeThickness = 1,
                    Stroke = new SolidColorBrush(Color.FromArgb("#C3CDF2")),
                    StrokeShape = new RoundRectangle { CornerRadius = 10 },
                    Padding = new Thickness(8, 6),
                    Margin = new Thickness(0, 0, 0, 6),
                    Content = label
                };
                host.BindingContextChanged += (_, _) => label.Text = host.BindingContext?.ToString() ?? string.Empty;
                return host;
            })
        };

        var addButton = new Button { Text = "Add item" };
        addButton.Clicked += (_, _) =>
        {
            items.Add($"Item {items.Count + 1}");
            summary.Text = $"Items: {items.Count}";
        };

        var removeButton = new Button { Text = "Remove item" };
        removeButton.Clicked += (_, _) =>
        {
            if (items.Count > 0)
            {
                items.RemoveAt(items.Count - 1);
                summary.Text = $"Items: {items.Count}";
            }
        };

        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                collectionView,
                new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        addButton,
                        removeButton
                    }
                },
                summary
            }
        };
    }

    private static View CreateRefreshViewProbe()
    {
        var items = new ObservableCollection<string> { "North", "South", "West" };
        var summary = new Label
        {
            Text = "Refreshing: false, command count: 0",
            FontSize = 13
        };

        var commandCount = 0;
        var collectionView = new CollectionView
        {
            HeightRequest = 150,
            ItemsSource = items,
            ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label();
                label.BindingContextChanged += (_, _) => label.Text = label.BindingContext?.ToString() ?? string.Empty;
                return label;
            })
        };

        var refreshView = new RefreshView
        {
            Content = collectionView,
            IsRefreshEnabled = true,
            IsRefreshing = false
        };

        refreshView.Command = new Command(() =>
        {
            commandCount++;
            refreshView.IsRefreshing = true;
            summary.Text = $"Refreshing: true, command count: {commandCount}";
        });

        var completeButton = new Button { Text = "Complete refresh" };
        completeButton.Clicked += (_, _) =>
        {
            refreshView.IsRefreshing = false;
            summary.Text = $"Refreshing: false, command count: {commandCount}";
        };

        return new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                refreshView,
                completeButton,
                summary
            }
        };
    }

    private static View CreateContentViewProbe()
    {
        return new ContentView
        {
            Padding = new Thickness(12),
            Content = new Label
            {
                Text = "ContentView probe child"
            }
        };
    }

    private static View CreateScrollViewProbe()
    {
        return new ScrollView
        {
            HeightRequest = 150,
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label { Text = "Scroll line 1" },
                    new Label { Text = "Scroll line 2" },
                    new Label { Text = "Scroll line 3" },
                    new Label { Text = "Scroll line 4" },
                    new Label { Text = "Scroll line 5" },
                    new Label { Text = "Scroll line 6" },
                    new Label { Text = "Scroll line 7" },
                    new Label { Text = "Scroll line 8" }
                }
            }
        };
    }

    private static View CreateVerticalStackProbe()
    {
        return new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Label { Text = "Row A" },
                new Label { Text = "Row B" },
                new Label { Text = "Row C" }
            }
        };
    }

    private static View CreateHorizontalStackProbe()
    {
        return new HorizontalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label { Text = "Col A" },
                new Label { Text = "Col B" },
                new Label { Text = "Col C" }
            }
        };
    }

    private static View CreateGridProbe()
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            },
            RowSpacing = 6,
            ColumnSpacing = 8
        };

        var r0c0 = new Label { Text = "r0c0" };
        var r0c1 = new Label { Text = "r0c1" };
        var span = new Label { Text = "r1 span", HorizontalOptions = LayoutOptions.Center };

        grid.Children.Add(r0c0);
        Grid.SetColumn(r0c0, 0);
        Grid.SetRow(r0c0, 0);

        grid.Children.Add(r0c1);
        Grid.SetColumn(r0c1, 1);
        Grid.SetRow(r0c1, 0);

        grid.Children.Add(span);
        Grid.SetColumn(span, 0);
        Grid.SetRow(span, 1);
        Grid.SetColumnSpan(span, 2);

        return grid;
    }

    private static View CreateAbsoluteLayoutProbe()
    {
        var layout = new AbsoluteLayout
        {
            HeightRequest = 110
        };

        var one = new Label { Text = "Top-left" };
        var two = new Label { Text = "Bottom-right" };
        layout.Children.Add(one);
        layout.Children.Add(two);
        AbsoluteLayout.SetLayoutBounds(one, new Rect(4, 4, 90, 24));
        AbsoluteLayout.SetLayoutBounds(two, new Rect(120, 76, 120, 24));
        AbsoluteLayout.SetLayoutFlags(one, AbsoluteLayoutFlags.None);
        AbsoluteLayout.SetLayoutFlags(two, AbsoluteLayoutFlags.None);
        return layout;
    }

    private static View CreateFlexLayoutProbe()
    {
        return new FlexLayout
        {
            Direction = FlexDirection.Row,
            Wrap = FlexWrap.Wrap,
            JustifyContent = FlexJustify.Start,
            AlignItems = FlexAlignItems.Stretch,
            Children =
            {
                CreateFlexItem("Flex A"),
                CreateFlexItem("Flex B"),
                CreateFlexItem("Flex C")
            }
        };
    }

    private static Border CreateFlexItem(string text)
    {
        return new Border
        {
            StrokeThickness = 0,
            Margin = new Thickness(0, 0, 8, 8),
            Padding = new Thickness(10, 8),
            BackgroundColor = Color.FromArgb("#DFE6FF"),
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Content = new Label
            {
                Text = text
            }
        };
    }
}
