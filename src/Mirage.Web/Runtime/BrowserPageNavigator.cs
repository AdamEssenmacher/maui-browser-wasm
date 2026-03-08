using Mirage.Web.Dom;

namespace Mirage.Web.Runtime;

public sealed class BrowserPageNavigator : IMiragePageNavigator
{
    private DomElement? _rootElement;

    public bool ScrollToAutomationId(string automationId, bool smooth = true)
    {
        if (_rootElement is null)
        {
            return false;
        }

        return _rootElement.ScrollToAutomationId(automationId, smooth);
    }

    public void AttachRoot(DomElement rootElement)
    {
        ArgumentNullException.ThrowIfNull(rootElement);
        _rootElement = rootElement;
    }

    public void DetachRoot()
    {
        _rootElement = null;
    }
}
