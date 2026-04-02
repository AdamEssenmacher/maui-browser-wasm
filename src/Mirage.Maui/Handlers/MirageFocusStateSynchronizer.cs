using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;

namespace Mirage.Maui.Handlers;

internal static class MirageFocusStateSynchronizer
{
    public static bool TrySetFocusedStateSilently(VisualElement visualElement, bool isFocused)
    {
        ArgumentNullException.ThrowIfNull(visualElement);

        if (visualElement is not IElementController controller)
        {
            return false;
        }

        controller.SetValueFromRenderer(VisualElement.IsFocusedProperty, isFocused);
        return true;
    }
}
