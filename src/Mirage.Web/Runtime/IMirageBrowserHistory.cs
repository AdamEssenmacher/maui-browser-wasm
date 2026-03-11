namespace Mirage.Web.Runtime;

public interface IMirageBrowserHistory
{
    string GetCurrentLocation();

    void Push(string path);

    void Replace(string path);

    string GetDocumentTitle();

    void SetDocumentTitle(string? title);

    int SubscribePopState(Action<string> callback);

    void UnsubscribePopState(int subscriptionId);
}
