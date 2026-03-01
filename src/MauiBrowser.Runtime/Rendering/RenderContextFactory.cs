using Microsoft.Maui.Controls;

namespace MauiBrowser.Runtime.Rendering;

internal static class RenderContextFactory
{
    public static RenderContext CreateFor(object virtualView)
    {
        var semanticsLabel = virtualView switch
        {
            Label label => label.Text,
            Button button => button.Text,
            _ => null
        };
        var semanticsDisabled = virtualView is VisualElement visualElement && !visualElement.IsEnabled;
        var semanticsRole = virtualView is Button ? "button" : null;

        return new RenderContext
        {
            StyleTokens = StyleTokens.Empty,
            Semantics = new Semantics
            {
                Label = semanticsLabel,
                Disabled = semanticsDisabled,
                Role = semanticsRole
            },
            Events = EventDispatcher.None
        };
    }
}
