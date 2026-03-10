namespace Showcase.Shared.App;

internal enum ShowcaseBacklogStatus
{
    Complete,
    Started,
    NotStarted
}

internal sealed record ShowcaseBacklogRow(
    string Key,
    string Category,
    string MauiType,
    ShowcaseBacklogStatus Status,
    string Notes);

internal static class ShowcaseBacklogCatalog
{
    public static IReadOnlyList<ShowcaseBacklogRow> Rows { get; } =
    [
        new("window", "Host", "Window", ShowcaseBacklogStatus.Complete, "Primary MAUI window lifecycle is mapped."),
        new("navigation-page", "Host", "NavigationPage", ShowcaseBacklogStatus.Started, "Single-stack browser navigation flow is implemented."),
        new("content-page", "Host", "ContentPage", ShowcaseBacklogStatus.Complete, "Content page hosting is implemented."),
        new("content-view", "Container", "ContentView", ShowcaseBacklogStatus.Complete, "Single-child content hosting is implemented."),
        new("scroll-view", "Container", "ScrollView", ShowcaseBacklogStatus.Started, "Explicit scroll-container behavior with orientation, scrollbar visibility, and command scrolling is implemented."),
        new("vertical-stack-layout", "Layout", "VerticalStackLayout", ShowcaseBacklogStatus.Complete, "Semantic flex-column realization is implemented."),
        new("horizontal-stack-layout", "Layout", "HorizontalStackLayout", ShowcaseBacklogStatus.Complete, "Semantic flex-row realization is implemented."),
        new("grid", "Layout", "Grid", ShowcaseBacklogStatus.Complete, "Typed grid realization with row/column/span is implemented."),
        new("absolute-layout", "Layout", "AbsoluteLayout", ShowcaseBacklogStatus.Complete, "Absolute projection path remains supported."),
        new("flex-layout", "Layout", "FlexLayout", ShowcaseBacklogStatus.Started, "Core flex container/item mapping is implemented."),
        new("label", "Text/Input", "Label", ShowcaseBacklogStatus.Started, "Core text mapping is implemented; deep parity remains partial."),
        new("button", "Text/Input", "Button", ShowcaseBacklogStatus.Complete, "Text + click command/event flow is implemented."),
        new("entry", "Text/Input", "Entry", ShowcaseBacklogStatus.Started, "Two-way text + placeholder mapping is implemented."),
        new("editor", "Text/Input", "Editor", ShowcaseBacklogStatus.Started, "Multiline text edit flow is implemented."),
        new("search-bar", "Text/Input", "SearchBar", ShowcaseBacklogStatus.Started, "Search text/submit flow is implemented."),
        new("check-box", "Form", "CheckBox", ShowcaseBacklogStatus.Complete, "Boolean checked-state flow is implemented."),
        new("switch", "Form", "Switch", ShowcaseBacklogStatus.Started, "Toggle semantics are implemented with partial parity."),
        new("slider", "Form", "Slider", ShowcaseBacklogStatus.Started, "Range value mapping is implemented."),
        new("stepper", "Form", "Stepper", ShowcaseBacklogStatus.Started, "Increment/decrement numeric flow is implemented."),
        new("picker", "Form", "Picker", ShowcaseBacklogStatus.Started, "Items + selected-index mapping is implemented."),
        new("date-picker", "Form", "DatePicker", ShowcaseBacklogStatus.Started, "Date value mapping is implemented."),
        new("time-picker", "Form", "TimePicker", ShowcaseBacklogStatus.Started, "Time value mapping is implemented."),
        new("progress-bar", "Form", "ProgressBar", ShowcaseBacklogStatus.Started, "Progress value mapping is implemented."),
        new("activity-indicator", "Form", "ActivityIndicator", ShowcaseBacklogStatus.Started, "IsRunning + visibility flow is implemented."),
        new("image", "Media/Visual", "Image", ShowcaseBacklogStatus.Started, "Source + aspect mapping is implemented."),
        new("image-button", "Media/Visual", "ImageButton", ShowcaseBacklogStatus.Started, "Image + button interaction mapping is implemented."),
        new("border", "Media/Visual", "Border", ShowcaseBacklogStatus.Started, "Core stroke/shape child-host mapping is implemented."),
        new("graphics-view", "Media/Visual", "GraphicsView", ShowcaseBacklogStatus.NotStarted, "Handler is not implemented."),
        new("collection-view", "Collection", "CollectionView", ShowcaseBacklogStatus.Started, "Non-virtualized templated list behavior is implemented."),
        new("carousel-view", "Collection", "CarouselView", ShowcaseBacklogStatus.NotStarted, "Handler is not implemented."),
        new("indicator-view", "Collection", "IndicatorView", ShowcaseBacklogStatus.NotStarted, "Handler is not implemented."),
        new("refresh-view", "Collection", "RefreshView", ShowcaseBacklogStatus.Started, "Refresh command/state and pull-threshold flow are implemented."),
        new("swipe-view", "Interaction", "SwipeView", ShowcaseBacklogStatus.NotStarted, "Handler is not implemented."),
        new("web-view", "Interaction", "WebView", ShowcaseBacklogStatus.NotStarted, "Handler is not implemented."),
        new("element", "Shared", "Element", ShowcaseBacklogStatus.Complete, "Generic WebNode realization path is implemented."),
        new("visual-element", "Shared", "VisualElement", ShowcaseBacklogStatus.Started, "Common visual mapping substrate is partially implemented."),
        new("iview", "Shared", "IView", ShowcaseBacklogStatus.Started, "Common IView mapping path is implemented incrementally."),
        new("layout", "Shared", "Layout", ShowcaseBacklogStatus.Started, "Shared layout infrastructure is implemented for core layout kinds."),
        new("input-view", "Shared", "InputView", ShowcaseBacklogStatus.Started, "Input-related shared behavior is implemented incrementally."),
        new("image-element", "Shared", "ImageElement", ShowcaseBacklogStatus.Started, "Shared image-element behavior is partially covered via Image/ImageButton.")
    ];
}
