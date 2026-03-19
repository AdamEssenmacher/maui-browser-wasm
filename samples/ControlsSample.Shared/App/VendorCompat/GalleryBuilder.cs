using System.Reflection;
using System.Text;
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
        var button = new Button
        {
            Text = galleryName,
            AutomationId = $"controls-sample-borderpage-open-{slug}",
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

            InstrumentBorderGalleryPage(page);
            await nav.PushAsync(page, animated: false);
        };

        return button;
    }

    private static void InstrumentBorderGalleryPage(Page page)
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
