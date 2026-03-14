using Microsoft.Maui;

namespace Mirage.Maui.Handlers;

internal static class MirageHandlerDisconnect
{
    private const string PlatformViewNullMessage = "PlatformView cannot be null here";

    public static void SafeDisconnect(IElementHandler? handler)
    {
        if (handler is null)
        {
            return;
        }

        try
        {
            handler.DisconnectHandler();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains(PlatformViewNullMessage, StringComparison.Ordinal))
        {
            // The handler has already been detached from a platform view.
        }
    }
}
