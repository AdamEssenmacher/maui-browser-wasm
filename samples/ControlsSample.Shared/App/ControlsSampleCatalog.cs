using Maui.Controls.Sample.Pages;
using Microsoft.Maui.Controls;

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
}
