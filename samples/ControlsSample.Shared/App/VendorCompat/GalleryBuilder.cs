using System.Reflection;
using System.Text;
using Maui.Controls.Sample.Pages.MapsGalleries;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;

namespace Maui.Controls.Sample.Pages;

public static class GalleryBuilder
{
    public static Button NavButton(string galleryName, Func<Page> gallery, INavigation nav)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(galleryName);
        ArgumentNullException.ThrowIfNull(gallery);
        ArgumentNullException.ThrowIfNull(nav);

        var slug = ToSlug(galleryName);
        var automationId = BuildNavButtonAutomationId(gallery, slug);
        var button = new Button
        {
            Text = galleryName,
            AutomationId = automationId,
            FontSize = 10,
            HeightRequest = 36,
            HorizontalOptions = LayoutOptions.Fill,
            Margin = 5,
            Padding = 5
        };

        button.Clicked += async (_, _) =>
        {
            var page = gallery();
            var pageId = page.GetType().Name.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(page.AutomationId))
            {
                page.AutomationId = $"controls-sample-page-{pageId}";
            }

            InstrumentGalleryPage(page);
            await nav.PushAsync(page, animated: false);
        };

        return button;
    }

    private static string BuildNavButtonAutomationId(Func<Page> gallery, string slug)
    {
        var returnType = gallery.Method.ReturnType;
        if (returnType == typeof(Page))
        {
            try
            {
                returnType = gallery().GetType();
            }
            catch
            {
                // Keep fallback behavior deterministic if factory inspection fails.
            }
        }

        var @namespace = returnType.Namespace ?? string.Empty;

        if (@namespace.Contains(".MapsGalleries", StringComparison.Ordinal))
        {
            return $"controls-sample-mapsgallery-open-{slug}";
        }

        return $"controls-sample-borderpage-open-{slug}";
    }

    private static void InstrumentGalleryPage(Page page)
    {
        switch (page)
        {
            case BorderPlayground borderPlayground:
                SetNamedElementAutomationId(borderPlayground, "BorderView", "controls-sample-borderplayground-border");
                SetNamedElementAutomationId(borderPlayground, "BorderContentPicker", "controls-sample-borderplayground-content-picker");
                SetNamedElementAutomationId(borderPlayground, "BorderShapePicker", "controls-sample-borderplayground-shape-picker");
                SetNamedElementAutomationId(borderPlayground, "BorderWidthSlider", "controls-sample-borderplayground-width-slider");
                SetNamedElementAutomationId(borderPlayground, "BorderDashArrayEntry", "controls-sample-borderplayground-dash-entry");
                SetNamedElementAutomationId(borderPlayground, "BorderDashOffsetSlider", "controls-sample-borderplayground-dash-offset-slider");
                SetNamedElementAutomationId(borderPlayground, "BorderLineJoinPicker", "controls-sample-borderplayground-linejoin-picker");
                SetNamedElementAutomationId(borderPlayground, "BorderLineCapPicker", "controls-sample-borderplayground-linecap-picker");
                SetNamedElementAutomationId(borderPlayground, "TopLeftCornerSlider", "controls-sample-borderplayground-corner-top-left");
                SetNamedElementAutomationId(borderPlayground, "BottomLeftCornerSlider", "controls-sample-borderplayground-corner-bottom-left");
                SetNamedElementAutomationId(borderPlayground, "BottomRightCornerSlider", "controls-sample-borderplayground-corner-bottom-right");
                break;
            case BorderClipPlayground borderClipPlayground:
                SetNamedElementAutomationId(borderClipPlayground, "BorderView", "controls-sample-borderclipplayground-border");
                SetNamedElementAutomationId(borderClipPlayground, "BorderShapePicker", "controls-sample-borderclipplayground-shape-picker");
                SetNamedElementAutomationId(borderClipPlayground, "BorderWidthSlider", "controls-sample-borderclipplayground-width-slider");
                SetNamedElementAutomationId(borderClipPlayground, "TopLeftCornerSlider", "controls-sample-borderclipplayground-corner-top-left");
                break;
            case BorderStyles borderStyles:
                SetNamedElementAutomationId(borderStyles, "UpdateStrokeShapeBorder", "controls-sample-borderstyles-update-border");
                SetNamedElementAutomationId(borderStyles, "UpdateStrokeShapeInfo", "controls-sample-borderstyles-update-info");
                SetButtonAutomationIdByText(borderStyles, "Increase CornerRadius", "controls-sample-borderstyles-increase-corner");
                SetButtonAutomationIdByText(borderStyles, "Decrease CornerRadius", "controls-sample-borderstyles-decrease-corner");
                break;
            case BorderLayout borderLayout:
                SetNamedElementAutomationId(borderLayout, "BorderWidthSlider", "controls-sample-borderlayout-width-slider");
                break;
            case BorderStroke borderStroke:
                SetNamedElementAutomationId(borderStroke, "ContentHeightSlider", "controls-sample-borderstroke-height-slider");
                SetNamedElementAutomationId(borderStroke, "RectangleHeight01", "controls-sample-borderstroke-rectangle-1");
                SetNamedElementAutomationId(borderStroke, "RectangleHeight03", "controls-sample-borderstroke-rectangle-3");
                break;
            case BorderResizeContent borderResizeContent:
                SetNamedElementAutomationId(borderResizeContent, "TextEntry", "controls-sample-borderresizecontent-text-entry");
                SetNamedElementAutomationId(borderResizeContent, "FontSizeSlider", "controls-sample-borderresizecontent-font-slider");
                SetNamedElementAutomationId(borderResizeContent, "ImageScaleSlider", "controls-sample-borderresizecontent-image-scale-slider");
                SetNamedElementAutomationId(borderResizeContent, "BorderViewCircleImage", "controls-sample-borderresizecontent-circle-image");
                SetNamedElementAutomationId(borderResizeContent, "BorderViewTriangleImage", "controls-sample-borderresizecontent-triangle-image");
                break;
            case BorderAlignment borderAlignment:
                SetBorderAutomationIdsByOrder(borderAlignment, "controls-sample-borderalignment-border");
                break;
            case BasicMapGallery basicMapGallery:
                SetNamedElementAutomationId(basicMapGallery, "ShowUserLocationSwitch", "controls-sample-basicmapgallery-show-user-switch");
                SetNamedElementAutomationId(basicMapGallery, "ShowTrafficSwitch", "controls-sample-basicmapgallery-show-traffic-switch");
                SetNamedElementAutomationId(basicMapGallery, "ZoomEnabledSwitch", "controls-sample-basicmapgallery-zoom-enabled-switch");
                SetNamedElementAutomationId(basicMapGallery, "ScrollEnabledSwitch", "controls-sample-basicmapgallery-scroll-enabled-switch");
                SetNamedElementAutomationId(basicMapGallery, "basicMap", "controls-sample-basicmapgallery-map");
                break;
            case MapTypeGallery mapTypeGallery:
                SetNamedElementAutomationId(mapTypeGallery, "MapTypePicker", "controls-sample-maptypegallery-type-picker");
                SetNamedElementAutomationId(mapTypeGallery, "mapTypeMap", "controls-sample-maptypegallery-map");
                SetSliderAutomationIdByOrder(mapTypeGallery, "controls-sample-maptypegallery-zoom-slider");
                break;
            case MapPinsGallery mapPinsGallery:
                SetNamedElementAutomationId(mapPinsGallery, "pinsMap", "controls-sample-mappinsgallery-map");
                SetButtonAutomationIdByText(mapPinsGallery, "Add Pin", "controls-sample-mappinsgallery-add-pin");
                SetButtonAutomationIdByText(mapPinsGallery, "Remove Pin", "controls-sample-mappinsgallery-remove-pin");
                SetButtonAutomationIdByText(mapPinsGallery, "Add 10 Pins", "controls-sample-mappinsgallery-add-ten-pins");
                break;
            case PinItemsSourceGallery pinItemsSourceGallery:
                SetNamedElementAutomationId(pinItemsSourceGallery, "map", "controls-sample-pinitemssourcegallery-map");
                SetButtonAutomationIdByText(pinItemsSourceGallery, "Add", "controls-sample-pinitemssourcegallery-add");
                SetButtonAutomationIdByText(pinItemsSourceGallery, "Remove", "controls-sample-pinitemssourcegallery-remove");
                SetButtonAutomationIdByText(pinItemsSourceGallery, "Clear", "controls-sample-pinitemssourcegallery-clear");
                SetButtonAutomationIdByText(pinItemsSourceGallery, "Update", "controls-sample-pinitemssourcegallery-update");
                SetButtonAutomationIdByText(pinItemsSourceGallery, "Replace", "controls-sample-pinitemssourcegallery-replace");
                break;
            case CircleGallery circleGallery:
                SetMapAutomationIdByOrder(circleGallery, "controls-sample-circlegallery-map");
                break;
            case PolygonsGallery polygonsGallery:
                SetNamedElementAutomationId(polygonsGallery, "MapElementsMap", "controls-sample-polygonsgallery-map");
                SetButtonAutomationIdByText(polygonsGallery, "Clear MapElements", "controls-sample-polygonsgallery-clear-map-elements");
                break;
        }
    }

    private static void SetNamedElementAutomationId(Page page, string fieldName, string automationId)
    {
        var field = page.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var visualElement = field?.GetValue(page) as VisualElement
            ?? page.FindByName<VisualElement>(fieldName);
        if (visualElement is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(visualElement.AutomationId))
        {
            return;
        }

        visualElement.AutomationId = automationId;
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

    private static void SetBorderAutomationIdsByOrder(Page page, string prefix)
    {
        var borders = FindDescendants<Border>(page).ToArray();
        for (var index = 0; index < borders.Length; index++)
        {
            if (string.IsNullOrWhiteSpace(borders[index].AutomationId))
            {
                borders[index].AutomationId = $"{prefix}-{index + 1}";
            }
        }
    }

    private static void SetSliderAutomationIdByOrder(Page page, string automationId)
    {
        var slider = FindDescendants<Slider>(page).FirstOrDefault();
        if (slider is null || !string.IsNullOrWhiteSpace(slider.AutomationId))
        {
            return;
        }

        slider.AutomationId = automationId;
    }

    private static void SetMapAutomationIdByOrder(Page page, string automationId)
    {
        var map = FindDescendants<Microsoft.Maui.Controls.Maps.Map>(page).FirstOrDefault();
        if (map is null || !string.IsNullOrWhiteSpace(map.AutomationId))
        {
            return;
        }

        map.AutomationId = automationId;
    }

    private static IEnumerable<TElement> FindDescendants<TElement>(Element root)
        where TElement : Element
    {
        if (root is TElement match)
        {
            yield return match;
        }

        if (root is not IElementController controller)
        {
            yield break;
        }

        foreach (var child in controller.LogicalChildren)
        {
            foreach (var descendant in FindDescendants<TElement>(child))
            {
                yield return descendant;
            }
        }
    }

    private static string ToSlug(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToLowerInvariant(c));
            }
            else if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        var slug = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug)
            ? "page"
            : slug;
    }
}
