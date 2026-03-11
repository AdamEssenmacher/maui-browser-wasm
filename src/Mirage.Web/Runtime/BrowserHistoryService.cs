using Mirage.Core.Diagnostics;
using Mirage.Web.Dom;

namespace Mirage.Web.Runtime;

public sealed class BrowserHistoryService : IMirageBrowserHistory
{
    public string GetCurrentLocation()
    {
        if (!MirageDiagnostics.IsEnabled)
        {
            return DomElement.GetLocationPathAndQueryAndHash();
        }

        var startTimestamp = MirageDiagnostics.GetTimestamp();
        var location = DomElement.GetLocationPathAndQueryAndHash();
        var elapsedMs = MirageDiagnostics.GetElapsedMilliseconds(startTimestamp, MirageDiagnostics.GetTimestamp());
        MirageDiagnostics.Log("history.interop", $"op=getLocation elapsedMs={elapsedMs:0.###} location={location}");
        return location;
    }

    public void Push(string path)
    {
        if (!MirageDiagnostics.IsEnabled)
        {
            DomElement.PushHistoryState(path);
            return;
        }

        var startTimestamp = MirageDiagnostics.GetTimestamp();
        DomElement.PushHistoryState(path);
        var elapsedMs = MirageDiagnostics.GetElapsedMilliseconds(startTimestamp, MirageDiagnostics.GetTimestamp());
        MirageDiagnostics.Log("history.interop", $"op=push elapsedMs={elapsedMs:0.###} path={path}");
    }

    public void Replace(string path)
    {
        if (!MirageDiagnostics.IsEnabled)
        {
            DomElement.ReplaceHistoryState(path);
            return;
        }

        var startTimestamp = MirageDiagnostics.GetTimestamp();
        DomElement.ReplaceHistoryState(path);
        var elapsedMs = MirageDiagnostics.GetElapsedMilliseconds(startTimestamp, MirageDiagnostics.GetTimestamp());
        MirageDiagnostics.Log("history.interop", $"op=replace elapsedMs={elapsedMs:0.###} path={path}");
    }

    public string GetDocumentTitle()
    {
        return DomElement.GetDocumentTitle();
    }

    public void SetDocumentTitle(string? title)
    {
        DomElement.SetDocumentTitle(title);
    }

    public int SubscribePopState(Action<string> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        return DomElement.SubscribePopState(() =>
        {
            var startTimestamp = MirageDiagnostics.IsEnabled ? MirageDiagnostics.GetTimestamp() : 0;
            var location = DomElement.GetLocationPathAndQueryAndHash();
            if (MirageDiagnostics.IsEnabled)
            {
                var elapsedMs = MirageDiagnostics.GetElapsedMilliseconds(startTimestamp, MirageDiagnostics.GetTimestamp());
                MirageDiagnostics.Log("history.interop", $"op=popstate-callback elapsedMs={elapsedMs:0.###} location={location}");
            }

            callback(string.IsNullOrWhiteSpace(location) ? "/" : location);
        });
    }

    public void UnsubscribePopState(int subscriptionId)
    {
        DomElement.UnsubscribePopState(subscriptionId);
    }
}
