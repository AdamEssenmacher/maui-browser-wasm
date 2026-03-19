using Microsoft.Maui;
using Microsoft.Maui.Controls;
using MauiLayout = Microsoft.Maui.ILayout;

namespace Mirage.Maui.Handlers;

internal static class MirageLayoutHandlerOrdering
{
    public static IReadOnlyList<IView> OrderByZIndex(MauiLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        return layout.OrderBy(static child => ResolveZIndex(child)).ToArray();
    }

    public static int GetLayoutHandlerIndex(MauiLayout layout, IView view)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(view);

        var ordered = OrderByZIndex(layout);
        for (var index = 0; index < ordered.Count; index++)
        {
            if (ReferenceEquals(ordered[index], view))
            {
                return index;
            }
        }

        return ordered.Count;
    }

    private static int ResolveZIndex(IView view)
    {
        return view is VisualElement visualElement
            ? visualElement.ZIndex
            : view.ZIndex;
    }
}
