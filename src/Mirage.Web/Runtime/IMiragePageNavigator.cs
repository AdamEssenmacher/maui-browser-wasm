namespace Mirage.Web.Runtime;

public interface IMiragePageNavigator
{
    bool ScrollToAutomationId(string automationId, bool smooth = true);
}
