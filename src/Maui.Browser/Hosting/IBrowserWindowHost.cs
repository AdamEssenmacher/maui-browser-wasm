using Maui.Browser.Dom;
using Microsoft.Maui;

namespace Maui.Browser.Hosting;

internal interface IBrowserWindowHost
{
    IWindow? ActiveWindow { get; }

    void AttachWindow(IWindow window, IMauiContext mauiContext, DomElement rootHost);

    void Arrange(double width, double height);

    void DetachWindow();
}
