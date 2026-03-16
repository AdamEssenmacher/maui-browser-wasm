using Maui.Controls.Sample.Pages;
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
            id: "checkboxpage",
            title: "CheckBoxPage",
            upstreamPath: "Pages/Controls/CheckBoxPage.xaml",
            status: ControlsSampleStatus.Runnable,
            pageType: typeof(CheckBoxPage),
            description: "Runnable in Mirage today: exercises CheckBox checked state, color aliases, and disabled variants.",
            pageFactory: CreateCheckBoxPage),
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

        var disabledCheckBox = checkBoxes.FirstOrDefault(checkBox => !checkBox.IsEnabled);
        if (disabledCheckBox is not null)
        {
            disabledCheckBox.AutomationId = "controls-sample-checkboxpage-disabled";
        }

        return page;
    }

    private static void SetNamedElementAutomationId(Page page, string fieldName, string automationId)
    {
        var field = page.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field?.GetValue(page) is VisualElement visualElement)
        {
            visualElement.AutomationId = automationId;
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
