using Maui.Browser.Dom;
using Maui.Browser.Handlers;
using Microsoft.Maui;
using Microsoft.Maui.Platform;

namespace Maui.Browser.Hosting;

internal sealed class BrowserWindowHost : IBrowserWindowHost
{
    private IWindow? _activeWindow;
    private IElementHandler? _windowHandler;
    private DomElement? _rootHost;

    public IWindow? ActiveWindow => _activeWindow;

    public void AttachWindow(IWindow window, IMauiContext mauiContext, DomElement rootHost)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(mauiContext);
        ArgumentNullException.ThrowIfNull(rootHost);

        if (!ReferenceEquals(_activeWindow, window))
        {
            DetachWindow();
        }

        _rootHost = rootHost;

        _windowHandler = window.ToHandler(mauiContext);
        if (_windowHandler.PlatformView is not DomElement platformView)
        {
            throw new InvalidOperationException(
                $"Window handler platform view must be {nameof(DomElement)}. Actual: {_windowHandler.PlatformView?.GetType().FullName ?? "<null>"}");
        }

        rootHost.ClearChildren();
        rootHost.Append(platformView);

        _activeWindow = window;
    }

    public void Arrange(double width, double height)
    {
        if (_windowHandler is BrowserWindowHandler windowHandler)
        {
            windowHandler.ArrangeWindow(width, height);
        }
    }

    public void DetachWindow()
    {
        _windowHandler?.DisconnectHandler();
        _windowHandler = null;

        _rootHost?.ClearChildren();
        _rootHost = null;
        _activeWindow = null;
    }
}
