using Microsoft.Maui.Controls;
using Mirage.Maui.Handlers;

namespace Mirage.Maui.Dialogs;

internal interface IMirageBrowserDialogService
{
    void AttachWindow(Window window, MiragePlatformView host, IMirageRenderCoordinator coordinator);

    void CancelDialogs(Window window);

    void DetachWindow(Window window);
}
