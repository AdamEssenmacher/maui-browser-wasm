using Microsoft.Maui.Controls;

namespace MauiBrowser.Runtime.Rendering;

internal static class RenderContextFactory
{
    public static RenderContext CreateFor(object virtualView)
    {
        var semanticsLabel = virtualView is Label label ? label.Text : null;

        return new RenderContext
        {
            StyleTokens = StyleTokens.Empty,
            Semantics = new Semantics
            {
                Label = semanticsLabel
            },
            Events = EventDispatcher.None
        };
    }
}
