using Maui.Controls.Sample.Pages;
using Maui.Controls.Sample.Pages.CollectionViewGalleries;
using Maui.Controls.Sample.Pages.MapsGalleries;
using ControlsSample.Shared.App.VendorCompat;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using System.Reflection;

namespace ControlsSample.Shared.App;

public static class ControlsSampleCatalog
{
    public static IReadOnlyList<ControlsSampleEntry> All => _all.Value;

    private static readonly Lazy<IReadOnlyList<ControlsSampleEntry>> _all = new(CreateCategorizedEntries);

    private static IReadOnlyList<ControlsSampleEntry> AllEntries { get; } =
    [
        new(
            id: "absolutelayoutpage",
            title: "AbsoluteLayoutPage",
            upstreamPath: "Pages/Layouts/AbsoluteLayoutPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(AbsoluteLayoutPage),
            description: "Runnable in Mirage today: exercises AbsoluteLayout with proportional positioning and BoxView primitives."),
        new(
            id: "absolutelayoutparitypage",
            title: "AbsoluteLayoutParityProbePage",
            upstreamPath: "App/AbsoluteLayoutParityProbePage.cs",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(AbsoluteLayoutParityProbePage),
            description: "Mirage parity probe: mutates AbsoluteLayout bounds, flags, and z-order at runtime to validate dynamic projection behavior."),
        new(
            id: "layoutspage",
            title: "LayoutsPage",
            upstreamPath: "Pages/LayoutsPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(LayoutsPage),
            description: "Runnable in Mirage today: exercises upstream layout gallery navigation across foundational layout scenarios.",
            pageFactory: CreateLayoutsPage),
        new(
            id: "gridpage",
            title: "GridPage",
            upstreamPath: "Pages/Layouts/GridPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(GridPage),
            description: "Runnable in Mirage today: exercises Grid row/column definitions, spans, and centered label overlays."),
        new(
            id: "layoutupdatespage",
            title: "LayoutUpdatesPage",
            upstreamPath: "Pages/Layouts/LayoutUpdatesPage.cs",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(LayoutUpdatesPage),
            description: "Runnable in Mirage today: exercises runtime add/remove/insert/replace updates in a layout with overlapping child content.",
            pageFactory: CreateLayoutUpdatesPage),
        new(
            id: "relativelayoutpage",
            title: "RelativeLayoutPage",
            upstreamPath: "Pages/Layouts/RelativeLayoutPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(RelativeLayoutPage),
            description: "Runnable in Mirage today: exercises compatibility RelativeLayout constraints against parent and sibling geometry."),
        new(
            id: "horizontalstacklayoutpage",
            title: "HorizontalStackLayoutPage",
            upstreamPath: "Pages/Layouts/HorizontalStackLayoutPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(HorizontalStackLayoutPage),
            description: "Runnable in Mirage today: exercises HorizontalStackLayout row flow with Label and BoxView children."),
        new(
            id: "stacklayoutpage",
            title: "StackLayoutPage",
            upstreamPath: "Pages/Layouts/StackLayoutPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(StackLayoutPage),
            description: "Runnable in Mirage today: exercises StackLayout orientation variants with Label and BoxView children."),
        new(
            id: "verticalstacklayoutpage",
            title: "VerticalStackLayoutPage",
            upstreamPath: "Pages/Layouts/VerticalStackLayoutPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(VerticalStackLayoutPage),
            description: "Runnable in Mirage today: exercises VerticalStackLayout column flow with Label and BoxView children."),
        new(
            id: "templatedviewpage",
            title: "TemplatedViewPage",
            upstreamPath: "Pages/Layouts/TemplatedViewPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(TemplatedViewPage),
            description: "Runnable in Mirage today: exercises templated content composition with CardView and custom templated controls."),
        new(
            id: "zindexpage",
            title: "ZIndexPage",
            upstreamPath: "Pages/Layouts/ZIndexPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(ZIndexPage),
            description: "Runnable in Mirage today: exercises runtime z-index mutations and overlap ordering.",
            pageFactory: CreateZIndexPage),
        new(
            id: "scrollviewpage",
            title: "ScrollViewPage",
            upstreamPath: "Pages/Layouts/ScrollViewPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(ScrollViewPage),
            description: "Runnable in Mirage today: exercises ScrollView navigation scenarios across constructor scroll, scroll-to-end, template, and orientation variants.",
            pageFactory: CreateScrollViewPage),
        new(
            id: "clippage",
            title: "ClipPage",
            upstreamPath: "Pages/Core/ClipPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(ClipPage),
            description: "Runnable in Mirage today: exercises Image clipping with RectangleGeometry, EllipseGeometry, GeometryGroup, and PathGeometry scenarios.",
            pageFactory: CreateClipPage),
        new(
            id: "brushespage",
            title: "BrushesPage",
            upstreamPath: "Pages/Core/BrushesPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(BrushesPage),
            description: "Runnable in Mirage today: exercises solid/gradient brush scenarios and live brush updates across content surfaces."),
        new(
            id: "contentpagegallery",
            title: "ContentPageGallery",
            upstreamPath: "Pages/Core/ContentPageGalleries/ContentPageGallery.cs",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(ContentPageGallery),
            description: "Runnable in Mirage today: exercises ContentPage gallery navigation across Background and BackgroundImageSource scenarios.",
            pageFactory: CreateContentPageGallery),
        new(
            id: "transformationspage",
            title: "TransformationsPage",
            upstreamPath: "Pages/Core/TransformationsPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(TransformationsPage),
            description: "Runnable in Mirage today: exercises MAUI view transform properties with slider/stepper-driven updates.",
            pageFactory: CreateTransformationsPage),
        new(
            id: "borderpage",
            title: "BorderPage",
            upstreamPath: "Pages/Core/BorderGalleries/BorderPage.cs",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(BorderPage),
            description: "Runnable in Mirage today: exercises Border gallery navigation including stroke, clipping, layout, and alignment scenarios.",
            pageFactory: CreateBorderPage),
        new(
            id: "clippingpage",
            title: "ClippingPage",
            upstreamPath: "Pages/Layouts/ClippingPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(ClippingPage),
            description: "Runnable in Mirage today: exercises clipping toggles on stack layouts, plus image clipping behavior."),
        new(
            id: "contentviewpage",
            title: "ContentViewPage",
            upstreamPath: "Pages/Layouts/ContentViewPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(ContentViewPage),
            description: "Runnable in Mirage today: exercises ContentView composition with Border + Image content."),
        new(
            id: "customlayoutpage",
            title: "CustomLayoutPage",
            upstreamPath: "Pages/Layouts/CustomLayoutPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(CustomLayoutPage),
            description: "Runnable in Mirage today: exercises custom DockLayout arrangement with top/bottom/left/right button docking."),
        new(
            id: "flexlayoutpage",
            title: "FlexLayoutPage",
            upstreamPath: "Pages/Layouts/FlexLayoutPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(FlexLayoutPage),
            description: "Runnable in Mirage today: exercises FlexLayout direction plus order/grow/basis child projection in nested layout scenarios."),
        new(
            id: "activityindicatorpage",
            title: "ActivityIndicatorPage",
            upstreamPath: "Pages/Controls/ActivityIndicatorPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(ActivityIndicatorPage),
            description: "Runnable in Mirage today: exercises ActivityIndicator running state, theme color, background, and requested sizing scenarios.",
            pageFactory: CreateActivityIndicatorPage),
        new(
            id: "boxviewpage",
            title: "BoxViewPage",
            upstreamPath: "Pages/Controls/BoxViewPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(BoxViewPage),
            description: "Runnable in Mirage today: exercises BoxView color/brush, corner radius, opacity, clip, and shadow scenarios.",
            pageFactory: CreateBoxViewPage),
        new(
            id: "buttonpage",
            title: "ButtonPage",
            upstreamPath: "Pages/Controls/ButtonPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(ButtonPage),
            description: "Runnable in Mirage today: exercises Button text/image/content layout, line-break modes, bindings, and stroke/chrome variants.",
            pageFactory: CreateButtonPage),
        new(
            id: "checkboxpage",
            title: "CheckBoxPage",
            upstreamPath: "Pages/Controls/CheckBoxPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(CheckBoxPage),
            description: "Runnable in Mirage today: exercises CheckBox checked state, accent color aliases, and disabled variants.",
            pageFactory: CreateCheckBoxPage),
        new(
            id: "imagepage",
            title: "ImagePage",
            upstreamPath: "Pages/Controls/ImagePage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(ImagePage),
            description: "Runnable in Mirage today: exercises URI/file/font/stream image sources plus GIF playback toggles and opacity scenarios.",
            pageFactory: CreateImagePage),
        new(
            id: "imagebuttonparitypage",
            title: "ImageButtonParityProbePage",
            upstreamPath: "App/ImageButtonParityProbePage.cs",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(ImageButtonParityProbePage),
            description: "Mirage parity probe: exercises ImageButton source/aspect projection, interaction counters, chrome updates, and enabled-state gating."),
        new(
            id: "sliderpage",
            title: "SliderPage",
            upstreamPath: "Pages/Controls/SliderPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(SliderPage),
            description: "Runnable in Mirage today: exercises Slider range updates, color projections, thumb image sources, and runtime min/max changes.",
            pageFactory: CreateSliderPage),
        new(
            id: "stepperpage",
            title: "StepperPage",
            upstreamPath: "Pages/Controls/StepperPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(StepperPage),
            description: "Runnable in Mirage today: exercises Stepper range/increment/value scenarios with enabled-state and value-changed flows.",
            pageFactory: CreateStepperPage),
        new(
            id: "tabbedpagegallerymainpage",
            title: "TabbedPageGalleryMainPage",
            upstreamPath: "Pages/Compatibility/TabbedPageGalleryMainPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(TabbedPageGalleryMainPage),
            description: "Runnable in Mirage today: uses ScrollView + VerticalStackLayout + Button."),
        new(
            id: "indicatorviewparitypage",
            title: "IndicatorViewParityProbePage",
            upstreamPath: "App/IndicatorViewParityProbePage.cs",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(IndicatorViewParityProbePage),
            description: "Mirage parity probe: exercises IndicatorView count/position/max-visible/hide-single behavior, shape/color updates, and template override hosting."),
        new(
            id: "shapesparitypage",
            title: "ShapesParityProbePage",
            upstreamPath: "App/ShapesParityProbePage.cs",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(ShapesParityProbePage),
            description: "Mirage parity probe: exercises all Shapes rows with runtime shape switching, aspect/paint updates, stroke options, and geometry mutations."),
        new(
            id: "editorparitypage",
            title: "EditorParityProbePage",
            upstreamPath: "App/EditorParityProbePage.cs",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(EditorParityProbePage),
            description: "Mirage parity probe: exercises Editor text/transform, selection, max-length, read-only/enabled gating, and completed-on-blur behavior."),
        new(
            id: "radiobuttonparitypage",
            title: "RadioButtonParityProbePage",
            upstreamPath: "App/RadioButtonParityProbePage.cs",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(RadioButtonParityProbePage),
            description: "Mirage parity probe: exercises RadioButton named/parent-scope group selection, SelectedValue roundtrip, content/style updates, and enabled-state interaction gating."),
        new(
            id: "timepickerparitypage",
            title: "TimePickerParityProbePage",
            upstreamPath: "App/TimePickerParityProbePage.cs",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(TimePickerParityProbePage),
            description: "Mirage parity probe: exercises TimePicker time/format-step projection, open-state roundtrip, and enabled-state interaction gating."),
        new(
            id: "layoutisenabledpage",
            title: "LayoutIsEnabledPage",
            upstreamPath: "Pages/Layouts/LayoutIsEnabledPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(LayoutIsEnabledPage),
            description: "Runnable in Mirage today: exercises layout-level IsEnabled propagation and command enablement behavior.",
            pageFactory: CreateLayoutIsEnabledPage),
        new(
            id: "inputtransparentpage",
            title: "InputTransparentPage",
            upstreamPath: "Pages/Core/InputTransparentPage.xaml",
            status: ControlsSampleStatus.HiddenUnsupported,
            pageType: typeof(InputTransparentPage),
            description: "Imported core interaction page. Kept for native runs while browser support catches up."),
        new(
            id: "blazorpage",
            title: "BlazorPage",
            upstreamPath: "Pages/Blazor/BlazorPage.xaml",
            status: ControlsSampleStatus.HiddenRazor,
            pageType: typeof(ControlsSamplePlaceholderPage),
            description: "Razor/Blazor sample retained in source but excluded from run surface.",
            pageFactory: _ => new ControlsSamplePlaceholderPage(
                title: "BlazorPage",
                message: "This upstream sample depends on Razor components that are intentionally excluded from wrapper runtime surfaces.")),
        new(
            id: "mapsgallery",
            title: "MapsGallery",
            upstreamPath: "Pages/Controls/MapsGalleries/MapsGallery.cs",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(MapsGallery),
            description: "Runnable in Mirage today: exercises map gallery navigation with map options, pins, item source, and shape overlays.",
            pageFactory: CreateMapsGallery),
        new(
            id: "collectionviewgallery",
            title: "CollectionViewGallery",
            upstreamPath: "Pages/Controls/CollectionViewGalleries/CollectionViewGallery.cs",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(CollectionViewGallery),
            description: "Runnable in Mirage today: exercises CollectionView gallery navigation across templates, grouping, selection, scrolling, and layout variations (excluding deferred SearchBar/Carousel/Indicator dependencies).",
            pageFactory: CreateCollectionViewGallery)
    ];

    private static IReadOnlyDictionary<string, ControlsSampleCategory> CategoryByEntryId { get; } =
        new Dictionary<string, ControlsSampleCategory>(StringComparer.Ordinal)
        {
            ["absolutelayoutpage"] = ControlsSampleCategory.Layouts,
            ["layoutspage"] = ControlsSampleCategory.Layouts,
            ["gridpage"] = ControlsSampleCategory.Layouts,
            ["layoutupdatespage"] = ControlsSampleCategory.Layouts,
            ["relativelayoutpage"] = ControlsSampleCategory.Layouts,
            ["horizontalstacklayoutpage"] = ControlsSampleCategory.Layouts,
            ["stacklayoutpage"] = ControlsSampleCategory.Layouts,
            ["verticalstacklayoutpage"] = ControlsSampleCategory.Layouts,
            ["templatedviewpage"] = ControlsSampleCategory.Layouts,
            ["zindexpage"] = ControlsSampleCategory.Layouts,
            ["scrollviewpage"] = ControlsSampleCategory.Layouts,
            ["clippingpage"] = ControlsSampleCategory.Layouts,
            ["contentviewpage"] = ControlsSampleCategory.Layouts,
            ["customlayoutpage"] = ControlsSampleCategory.Layouts,
            ["flexlayoutpage"] = ControlsSampleCategory.Layouts,
            ["layoutisenabledpage"] = ControlsSampleCategory.Layouts,

            ["activityindicatorpage"] = ControlsSampleCategory.Controls,
            ["boxviewpage"] = ControlsSampleCategory.Controls,
            ["buttonpage"] = ControlsSampleCategory.Controls,
            ["checkboxpage"] = ControlsSampleCategory.Controls,
            ["imagepage"] = ControlsSampleCategory.Controls,
            ["sliderpage"] = ControlsSampleCategory.Controls,
            ["stepperpage"] = ControlsSampleCategory.Controls,
            ["mapsgallery"] = ControlsSampleCategory.Controls,
            ["collectionviewgallery"] = ControlsSampleCategory.Controls,

            ["clippage"] = ControlsSampleCategory.Core,
            ["brushespage"] = ControlsSampleCategory.Core,
            ["contentpagegallery"] = ControlsSampleCategory.Core,
            ["transformationspage"] = ControlsSampleCategory.Core,
            ["borderpage"] = ControlsSampleCategory.Core,
            ["inputtransparentpage"] = ControlsSampleCategory.Core,
            ["blazorpage"] = ControlsSampleCategory.Core,

            ["absolutelayoutparitypage"] = ControlsSampleCategory.ParityProbes,
            ["imagebuttonparitypage"] = ControlsSampleCategory.ParityProbes,
            ["indicatorviewparitypage"] = ControlsSampleCategory.ParityProbes,
            ["shapesparitypage"] = ControlsSampleCategory.ParityProbes,
            ["editorparitypage"] = ControlsSampleCategory.ParityProbes,
            ["radiobuttonparitypage"] = ControlsSampleCategory.ParityProbes,
            ["timepickerparitypage"] = ControlsSampleCategory.ParityProbes,

            ["tabbedpagegallerymainpage"] = ControlsSampleCategory.Compatibility
        };

    private static IReadOnlyList<ControlsSampleEntry> CreateCategorizedEntries()
    {
        return AllEntries.Select(entry =>
        {
            if (!CategoryByEntryId.TryGetValue(entry.Id, out var category))
            {
                throw new InvalidOperationException(
                    $"Controls sample entry '{entry.Id}' is missing an explicit category assignment.");
            }

            return entry.WithCategory(category);
        }).ToArray();
    }

    private static Page CreateLayoutsPage(IServiceProvider services)
    {
        _ = services;
        var page = new LayoutsPage();

        SetNamedElementAutomationId(page, "LayoutSections", "controls-sample-layoutspage-sections");
        var sections = FindDescendants<CollectionView>(page).FirstOrDefault();
        if (sections is not null && string.IsNullOrWhiteSpace(sections.AutomationId))
        {
            sections.AutomationId = "controls-sample-layoutspage-sections";
        }

        return page;
    }

    private static Page CreateLayoutUpdatesPage(IServiceProvider services)
    {
        _ = services;
        var page = new LayoutUpdatesPage();

        SetButtonAutomationIdByText(page, "Add", "controls-sample-layoutupdatespage-add");
        SetButtonAutomationIdByText(page, "Remove", "controls-sample-layoutupdatespage-remove");
        SetButtonAutomationIdByText(page, "Insert", "controls-sample-layoutupdatespage-insert");
        SetButtonAutomationIdByText(page, "Clear", "controls-sample-layoutupdatespage-clear");
        SetButtonAutomationIdByText(page, "Update", "controls-sample-layoutupdatespage-update");

        return page;
    }

    private static Page CreateZIndexPage(IServiceProvider services)
    {
        _ = services;
        var page = new ZIndexPage();

        SetNamedElementAutomationId(page, "CurrentZIndex", "controls-sample-zindexpage-current-label");
        SetNamedElementAutomationId(page, "ZIndexStepper", "controls-sample-zindexpage-stepper");

        return page;
    }

    private static Page CreateLayoutIsEnabledPage(IServiceProvider services)
    {
        _ = services;
        var page = new LayoutIsEnabledPage();

        SetNamedElementAutomationId(page, "MainLayout", "controls-sample-layoutisenabledpage-main-layout");
        SetNamedElementAutomationId(page, "DisabledButton", "controls-sample-layoutisenabledpage-disabled-button");
        SetNamedElementAutomationId(page, "DisabledCommandButton", "controls-sample-layoutisenabledpage-disabled-command-button");
        SetButtonAutomationIdByText(page, "Disable Layout", "controls-sample-layoutisenabledpage-toggle-layout");
        SetButtonAutomationIdByText(page, "Disable Button", "controls-sample-layoutisenabledpage-toggle-button");

        var checkBoxes = FindDescendants<CheckBox>(page).ToArray();
        if (checkBoxes.Length > 0 && string.IsNullOrWhiteSpace(checkBoxes[0].AutomationId))
        {
            checkBoxes[0].AutomationId = "controls-sample-layoutisenabledpage-bind-layout-enabled";
        }

        if (checkBoxes.Length > 1 && string.IsNullOrWhiteSpace(checkBoxes[1].AutomationId))
        {
            checkBoxes[1].AutomationId = "controls-sample-layoutisenabledpage-bind-button-enabled";
        }

        if (checkBoxes.Length > 2 && string.IsNullOrWhiteSpace(checkBoxes[2].AutomationId))
        {
            checkBoxes[2].AutomationId = "controls-sample-layoutisenabledpage-bind-command-enabled";
        }

        return page;
    }

    private static Page CreateClipPage(IServiceProvider services)
    {
        _ = services;
        var page = new ClipPage();

        var images = FindDescendants<Image>(page).ToArray();
        var automationIds = new[]
        {
            "controls-sample-clippage-image-default",
            "controls-sample-clippage-image-rectangle",
            "controls-sample-clippage-image-ellipse",
            "controls-sample-clippage-image-geometry-group",
            "controls-sample-clippage-image-path"
        };

        for (var i = 0; i < images.Length && i < automationIds.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(images[i].AutomationId))
            {
                images[i].AutomationId = automationIds[i];
            }
        }

        return page;
    }

    private static Page CreateButtonPage(IServiceProvider services)
    {
        _ = services;
        var page = new ButtonPage();

        SetNamedElementAutomationId(page, "ImageSourceButton", "controls-sample-buttonpage-image-source-toggle");
        SetNamedElementAutomationId(page, "positionChange", "controls-sample-buttonpage-position-toggle");
        SetNamedElementAutomationId(page, "decreaseSpacing", "controls-sample-buttonpage-decrease-spacing");
        SetNamedElementAutomationId(page, "increaseSpacing", "controls-sample-buttonpage-increase-spacing");
        SetNamedElementAutomationId(page, "LineBreakModeButton", "controls-sample-buttonpage-linebreak-target");
        SetNamedElementAutomationId(page, "BorderWidthSlider", "controls-sample-buttonpage-border-slider");

        var checkBox = FindDescendants<CheckBox>(page).FirstOrDefault();
        if (checkBox is not null)
        {
            checkBox.AutomationId = "controls-sample-buttonpage-color-checkbox";
        }

        var borderWidthButton = FindDescendants<Button>(page)
            .FirstOrDefault(button => string.Equals(button.Text, "BorderWidth Changing", StringComparison.Ordinal));
        if (borderWidthButton is not null)
        {
            borderWidthButton.AutomationId = "controls-sample-buttonpage-border-button";
        }

        var lineBreakUpdateButton = FindDescendants<Button>(page)
            .FirstOrDefault(button => string.Equals(button.Text, "Update LineBreakMode", StringComparison.Ordinal));
        if (lineBreakUpdateButton is not null)
        {
            lineBreakUpdateButton.AutomationId = "controls-sample-buttonpage-linebreak-update";
        }

        return page;
    }

    private static Page CreateActivityIndicatorPage(IServiceProvider services)
    {
        _ = services;
        var page = new ActivityIndicatorPage();

        var indicators = FindDescendants<ActivityIndicator>(page).ToArray();
        var activityAutomationIds = new[]
        {
            "controls-sample-activityindicatorpage-default",
            "controls-sample-activityindicatorpage-themed",
            "controls-sample-activityindicatorpage-background",
            "controls-sample-activityindicatorpage-large",
            "controls-sample-activityindicatorpage-small",
            "controls-sample-activityindicatorpage-stopped"
        };

        for (var i = 0; i < indicators.Length && i < activityAutomationIds.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(indicators[i].AutomationId))
            {
                indicators[i].AutomationId = activityAutomationIds[i];
            }
        }

        return page;
    }

    private static Page CreateBoxViewPage(IServiceProvider services)
    {
        _ = services;
        var page = new BoxViewPage();

        var boxViews = FindDescendants<BoxView>(page).ToArray();
        var boxViewAutomationIds = new[]
        {
            "controls-sample-boxviewpage-default",
            "controls-sample-boxviewpage-color",
            "controls-sample-boxviewpage-background",
            "controls-sample-boxviewpage-corner-radius",
            "controls-sample-boxviewpage-complex-corner-radius",
            "controls-sample-boxviewpage-opacity",
            "controls-sample-boxviewpage-clip",
            "controls-sample-boxviewpage-shadow"
        };

        for (var i = 0; i < boxViews.Length && i < boxViewAutomationIds.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(boxViews[i].AutomationId))
            {
                boxViews[i].AutomationId = boxViewAutomationIds[i];
            }
        }

        return page;
    }

    private static Page CreateCheckBoxPage(IServiceProvider services)
    {
        _ = services;
        var page = new CheckBoxPage();

        SetNamedElementAutomationId(page, "ChangeIsCheckedButton", "controls-sample-checkboxpage-change-button");
        SetNamedElementAutomationId(page, "IsCheckedCheckBox", "controls-sample-checkboxpage-change-checkbox");

        var checkBoxes = FindDescendants<CheckBox>(page).ToArray();
        if (checkBoxes.Length > 0)
        {
            checkBoxes[0].AutomationId = "controls-sample-checkboxpage-default";
        }

        if (checkBoxes.Length > 1)
        {
            checkBoxes[1].AutomationId = "controls-sample-checkboxpage-colored";
        }

        if (checkBoxes.Length > 2)
        {
            checkBoxes[2].AutomationId = "controls-sample-checkboxpage-disabled";
        }

        if (checkBoxes.Length > 3)
        {
            checkBoxes[3].AutomationId = "controls-sample-checkboxpage-disabled-colored";
        }

        return page;
    }

    private static Page CreateSliderPage(IServiceProvider services)
    {
        _ = services;
        var page = new SliderPage();

        var sliders = FindDescendants<Slider>(page).ToArray();
        var sliderAutomationIds = new[]
        {
            "controls-sample-sliderpage-default-slider",
            "controls-sample-sliderpage-background-color-slider",
            "controls-sample-sliderpage-background-brush-slider",
            "controls-sample-sliderpage-minmax-slider",
            "controls-sample-sliderpage-disabled-slider",
            "controls-sample-sliderpage-minimum-track-slider",
            "controls-sample-sliderpage-maximum-track-slider",
            "controls-sample-sliderpage-thumb-color-slider",
            "controls-sample-sliderpage-image-slider",
            "controls-sample-sliderpage-custom-slider",
            "controls-sample-sliderpage-dynamic-slider",
            "controls-sample-sliderpage-edge-case-slider"
        };

        for (var i = 0; i < sliders.Length && i < sliderAutomationIds.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(sliders[i].AutomationId))
            {
                sliders[i].AutomationId = sliderAutomationIds[i];
            }
        }

        SetNamedElementAutomationId(page, "MinMaxSlider", "controls-sample-sliderpage-minmax-slider");
        SetNamedElementAutomationId(page, "ImageSlider", "controls-sample-sliderpage-image-slider");
        SetNamedElementAutomationId(page, "DynamicSlider", "controls-sample-sliderpage-dynamic-slider");
        SetNamedElementAutomationId(page, "DynamicInfoLabel", "controls-sample-sliderpage-dynamic-info");

        var toggleImageButton = FindDescendants<Button>(page)
            .FirstOrDefault(button => string.Equals(button.Text, "Toggle Image", StringComparison.Ordinal));
        if (toggleImageButton is not null)
        {
            toggleImageButton.AutomationId = "controls-sample-sliderpage-toggle-image";
        }

        var updateMinimumButton = FindDescendants<Button>(page)
            .FirstOrDefault(button => string.Equals(button.Text, "Update Minimum", StringComparison.Ordinal));
        if (updateMinimumButton is not null)
        {
            updateMinimumButton.AutomationId = "controls-sample-sliderpage-update-minimum";
        }

        var updateMaximumButton = FindDescendants<Button>(page)
            .FirstOrDefault(button => string.Equals(button.Text, "Update Maximum", StringComparison.Ordinal));
        if (updateMaximumButton is not null)
        {
            updateMaximumButton.AutomationId = "controls-sample-sliderpage-update-maximum";
        }

        var disabledSlider = sliders.FirstOrDefault(slider => !slider.IsEnabled);
        if (disabledSlider is not null && string.IsNullOrWhiteSpace(disabledSlider.AutomationId))
        {
            disabledSlider.AutomationId = "controls-sample-sliderpage-disabled-slider";
        }

        return page;
    }

    private static Page CreateImagePage(IServiceProvider services)
    {
        _ = services;
        var page = new ImagePage();

        SetNamedElementAutomationId(page, "IsAnimationPlayingSwitch", "controls-sample-imagepage-animation-switch");
        SetNamedElementAutomationId(page, "StreamSourceImage", "controls-sample-imagepage-stream");
        SetNamedElementAutomationId(page, "AnimatedGifImage", "controls-sample-imagepage-animated-gif");

        var images = FindDescendants<Image>(page).ToArray();
        var imageAutomationIds = new[]
        {
            "controls-sample-imagepage-uri",
            "controls-sample-imagepage-file",
            "controls-sample-imagepage-font",
            "controls-sample-imagepage-font-scaled",
            "controls-sample-imagepage-gif-switch-bound",
            "controls-sample-imagepage-stream",
            "controls-sample-imagepage-opacity",
            "controls-sample-imagepage-animated-gif"
        };

        for (var i = 0; i < images.Length && i < imageAutomationIds.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(images[i].AutomationId))
            {
                images[i].AutomationId = imageAutomationIds[i];
            }
        }

        var startStopButton = FindDescendants<Button>(page)
            .FirstOrDefault(button => string.Equals(button.Text, "Start/Stop", StringComparison.Ordinal));
        if (startStopButton is not null && string.IsNullOrWhiteSpace(startStopButton.AutomationId))
        {
            startStopButton.AutomationId = "controls-sample-imagepage-start-stop";
        }

        var onlineSourceButton = FindDescendants<Button>(page)
            .FirstOrDefault(button => string.Equals(button.Text, "Use Online Source", StringComparison.Ordinal));
        if (onlineSourceButton is not null && string.IsNullOrWhiteSpace(onlineSourceButton.AutomationId))
        {
            onlineSourceButton.AutomationId = "controls-sample-imagepage-use-online-source";
        }

        return page;
    }

    private static Page CreateStepperPage(IServiceProvider services)
    {
        _ = services;
        var page = new StepperPage();

        SetNamedElementAutomationId(page, "EnableStepper", "controls-sample-stepperpage-enable-stepper");
        SetNamedElementAutomationId(page, "EnableButton", "controls-sample-stepperpage-enable-button");
        SetNamedElementAutomationId(page, "ValueChangedStepper", "controls-sample-stepperpage-valuechanged-stepper");

        var steppers = FindDescendants<Stepper>(page).ToArray();
        var stepperAutomationIds = new[]
        {
            "controls-sample-stepperpage-default",
            "controls-sample-stepperpage-enable-stepper",
            "controls-sample-stepperpage-background-color",
            "controls-sample-stepperpage-background-brush",
            "controls-sample-stepperpage-minmax",
            "controls-sample-stepperpage-increment",
            "controls-sample-stepperpage-valuechanged-stepper"
        };

        for (var i = 0; i < steppers.Length && i < stepperAutomationIds.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(steppers[i].AutomationId))
            {
                steppers[i].AutomationId = stepperAutomationIds[i];
            }
        }

        var valueLabel = FindDescendants<Label>(page).LastOrDefault();
        if (valueLabel is not null && string.IsNullOrWhiteSpace(valueLabel.AutomationId))
        {
            valueLabel.AutomationId = "controls-sample-stepperpage-value-label";
        }

        return page;
    }

    private static Page CreateScrollViewPage(IServiceProvider services)
    {
        _ = services;
        var page = new ScrollViewPage();
        page.BindingContext = new MirageScrollViewOptionsModel();

        SetNamedElementAutomationId(page, "LayoutSections", "controls-sample-scrollviewpage-sections");

        var firstCollectionView = FindDescendants<CollectionView>(page).FirstOrDefault();
        if (firstCollectionView is not null && string.IsNullOrWhiteSpace(firstCollectionView.AutomationId))
        {
            firstCollectionView.AutomationId = "controls-sample-scrollviewpage-sections";
        }

        return page;
    }

    private static Page CreateContentPageGallery(IServiceProvider services)
    {
        _ = services;
        var page = new ContentPageGallery();

        SetButtonAutomationIdByText(
            page,
            "ContentPage BackgroundImage",
            "controls-sample-gallery-open-contentpage-backgroundimage");
        SetButtonAutomationIdByText(
            page,
            "ContentPage Background",
            "controls-sample-gallery-open-contentpage-background");

        return page;
    }

    private static Page CreateTransformationsPage(IServiceProvider services)
    {
        _ = services;
        var page = new TransformationsPage();

        SetNamedElementAutomationId(page, "_label", "controls-sample-transformationspage-target");
        SetNamedElementAutomationId(page, "_sliderScale", "controls-sample-transformationspage-scale-slider");
        SetNamedElementAutomationId(page, "_sliderScaleX", "controls-sample-transformationspage-scalex-slider");
        SetNamedElementAutomationId(page, "_sliderScaleY", "controls-sample-transformationspage-scaley-slider");
        SetNamedElementAutomationId(page, "_sliderRotation", "controls-sample-transformationspage-rotation-slider");
        SetNamedElementAutomationId(page, "_sliderRotationX", "controls-sample-transformationspage-rotationx-slider");
        SetNamedElementAutomationId(page, "_sliderRotationY", "controls-sample-transformationspage-rotationy-slider");
        SetNamedElementAutomationId(page, "_stepperAnchorX", "controls-sample-transformationspage-anchorx-stepper");
        SetNamedElementAutomationId(page, "_stepperAnchorY", "controls-sample-transformationspage-anchory-stepper");

        return page;
    }

    private static Page CreateBorderPage(IServiceProvider services)
    {
        _ = services;
        return new BorderPage();
    }

    private static Page CreateMapsGallery(IServiceProvider services)
    {
        _ = services;
        var page = new MapsGallery();

        SetButtonAutomationIdByText(page, "Basic Map", "controls-sample-mapsgallery-open-basic-map");
        SetButtonAutomationIdByText(page, "Map Type", "controls-sample-mapsgallery-open-map-type");
        SetButtonAutomationIdByText(page, "Map Pins", "controls-sample-mapsgallery-open-map-pins");
        SetButtonAutomationIdByText(page, "Pins ItemsSource", "controls-sample-mapsgallery-open-pins-itemssource");
        SetButtonAutomationIdByText(page, "Circle", "controls-sample-mapsgallery-open-circle");
        SetButtonAutomationIdByText(page, "Polygon", "controls-sample-mapsgallery-open-polygon");

        return page;
    }

    private static Page CreateCollectionViewGallery(IServiceProvider services)
    {
        _ = services;
        var page = new CollectionViewGallery();

        SetButtonAutomationIdByText(page, "Default Text Galleries", "controls-sample-collectionviewgallery-open-default-text-galleries");
        SetButtonAutomationIdByText(page, "DataTemplate Galleries", "controls-sample-collectionviewgallery-open-datatemplate-galleries");
        SetButtonAutomationIdByText(page, "Observable Collection Galleries", "controls-sample-collectionviewgallery-open-observable-collection-galleries");
        SetButtonAutomationIdByText(page, "Snap Points Galleries", "controls-sample-collectionviewgallery-open-snap-points-galleries");
        SetButtonAutomationIdByText(page, "ScrollTo Galleries", "controls-sample-collectionviewgallery-open-scrollto-galleries");
        SetButtonAutomationIdByText(page, "EmptyView Galleries", "controls-sample-collectionviewgallery-open-emptyview-galleries");
        SetButtonAutomationIdByText(page, "Selection Galleries", "controls-sample-collectionviewgallery-open-selection-galleries");
        SetButtonAutomationIdByText(page, "Propagation Galleries", "controls-sample-collectionviewgallery-open-propagation-galleries");
        SetButtonAutomationIdByText(page, "Grouping Galleries", "controls-sample-collectionviewgallery-open-grouping-galleries");
        SetButtonAutomationIdByText(page, "Reordering Galleries", "controls-sample-collectionviewgallery-open-reordering-galleries");
        SetButtonAutomationIdByText(page, "Item Spacing Galleries", "controls-sample-collectionviewgallery-open-item-spacing-galleries");
        SetButtonAutomationIdByText(page, "Item Size Galleries", "controls-sample-collectionviewgallery-open-item-size-galleries");
        SetButtonAutomationIdByText(page, "Scroll Mode Galleries", "controls-sample-collectionviewgallery-open-scroll-mode-galleries");
        SetButtonAutomationIdByText(page, "Alternate Layout Galleries", "controls-sample-collectionviewgallery-open-alternate-layout-galleries");
        SetButtonAutomationIdByText(page, "Header/Footer Galleries", "controls-sample-collectionviewgallery-open-header-footer-galleries");
        SetButtonAutomationIdByText(page, "Nested CollectionViews", "controls-sample-collectionviewgallery-open-nested-collectionviews");
        SetButtonAutomationIdByText(page, "Online images", "controls-sample-collectionviewgallery-open-online-images");
        SetButtonAutomationIdByText(page, "Adaptive CollectionView Gallery", "controls-sample-collectionviewgallery-open-adaptive-collectionview-gallery");

        return page;
    }

    private static void SetNamedElementAutomationId(Page page, string fieldName, string automationId)
    {
        var field = page.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field?.GetValue(page) is VisualElement visualElement)
        {
            if (string.IsNullOrWhiteSpace(visualElement.AutomationId))
            {
                visualElement.AutomationId = automationId;
            }
        }
    }

    private static void SetButtonAutomationIdByText(Page page, string buttonText, string automationId)
    {
        var button = FindDescendants<Button>(page)
            .FirstOrDefault(candidate => string.Equals(candidate.Text, buttonText, StringComparison.Ordinal));
        if (button is null || !string.IsNullOrWhiteSpace(button.AutomationId))
        {
            return;
        }

        button.AutomationId = automationId;
    }

    private static IEnumerable<TView> FindDescendants<TView>(Element root)
        where TView : Element
    {
        if (root is TView match)
        {
            yield return match;
        }

        if (root is not IElementController controller)
        {
            yield break;
        }

        foreach (var child in controller.LogicalChildren)
        {
            foreach (var descendant in FindDescendants<TView>(child))
            {
                yield return descendant;
            }
        }
    }
}
