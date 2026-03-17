using Maui.Controls.Sample.Pages;
using ControlsSample.Shared.App.VendorCompat;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using System.Reflection;

namespace ControlsSample.Shared.App;

public static class ControlsSampleCatalog
{
    public static IReadOnlyList<ControlsSampleEntry> All { get; } =
    [
        new(
            id: "absolutelayoutpage",
            title: "AbsoluteLayoutPage",
            upstreamPath: "Pages/Layouts/AbsoluteLayoutPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(AbsoluteLayoutPage),
            description: "Runnable in Mirage today: exercises AbsoluteLayout with proportional positioning and BoxView primitives."),
        new(
            id: "gridpage",
            title: "GridPage",
            upstreamPath: "Pages/Layouts/GridPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(GridPage),
            description: "Runnable in Mirage today: exercises Grid row/column definitions, spans, and centered label overlays."),
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
            id: "scrollviewpage",
            title: "ScrollViewPage",
            upstreamPath: "Pages/Layouts/ScrollViewPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(ScrollViewPage),
            description: "Runnable in Mirage today: exercises ScrollView navigation scenarios across constructor scroll, scroll-to-end, template, and orientation variants.",
            pageFactory: CreateScrollViewPage),
        new(
            id: "transformationspage",
            title: "TransformationsPage",
            upstreamPath: "Pages/Core/TransformationsPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(TransformationsPage),
            description: "Runnable in Mirage today: exercises MAUI view transform properties with slider/stepper-driven updates.",
            pageFactory: CreateTransformationsPage),
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
            id: "layoutisenabledpage",
            title: "LayoutIsEnabledPage",
            upstreamPath: "Pages/Layouts/LayoutIsEnabledPage.xaml",
            status: ControlsSampleStatus.HiddenUnsupported,
            pageType: typeof(LayoutIsEnabledPage),
            description: "Imported layout demo page. Uses controls/layout features that are not fully mapped in Mirage yet."),
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
            upstreamPath: "Pages/Controls/MapsGalleries",
            status: ControlsSampleStatus.HiddenPlatformSpecific,
            pageType: typeof(ControlsSamplePlaceholderPage),
            description: "Platform-specific dependencies are treated as blocked in browser host.",
            pageFactory: _ => new ControlsSamplePlaceholderPage(
                title: "MapsGallery",
                message: "Maps and other platform-specific galleries are imported but hidden in this wrapper unless host dependencies are wired."))
    ];

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
