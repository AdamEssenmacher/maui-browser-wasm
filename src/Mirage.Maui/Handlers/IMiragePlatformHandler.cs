namespace Mirage.Maui.Handlers;

public interface IMiragePlatformHandler
{
    MiragePlatformView MiragePlatformView { get; }

    string? DefaultDisplayStyle { get; }

    void RequestRender();
}
