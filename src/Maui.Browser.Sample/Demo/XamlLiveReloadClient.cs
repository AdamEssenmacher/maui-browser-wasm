using System.Text.Json;
using Microsoft.Maui.Controls;

namespace Maui.Browser.Sample.Demo;

internal sealed class XamlLiveReloadClient : IAsyncDisposable
{
    private const string TargetXamlPath = "Demo/XamlDemoPage.xaml";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly CancellationTokenSource _shutdown = new();
    private Task? _eventLoop;
    private long _lastAppliedVersion;

    private XamlLiveReloadClient(Uri baseAddress)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = baseAddress,
            Timeout = TimeSpan.FromMinutes(2)
        };
    }

    public static async Task<XamlLiveReloadClient?> TryStartAsync()
    {
        try
        {
            await BrowserPageInterop.EnsureModuleLoadedAsync();
            var origin = BrowserPageInterop.GetLocationOrigin();
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var baseAddress))
            {
                Console.WriteLine($"[hot-reload] Unable to parse window origin '{origin}'.");
                return null;
            }

            var client = new XamlLiveReloadClient(baseAddress);
            if (!await client.ProbeDevHostAsync())
            {
                await client.DisposeAsync();
                return null;
            }

            client._eventLoop = Task.Run(() => client.RunLoopAsync(client._shutdown.Token));
            Console.WriteLine("[hot-reload] Live XAML SSE client started.");
            return client;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[hot-reload] Live XAML client failed to start: {ex.Message}");
            return null;
        }
    }

    private async Task<bool> ProbeDevHostAsync()
    {
        try
        {
            using var response = await _httpClient.GetAsync("/_dev/hot-reload", _shutdown.Token);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (!mediaType.Contains("json", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var payload = await response.Content.ReadAsStringAsync(_shutdown.Token);
            return payload.Contains("\"enabled\":true", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndListenAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[hot-reload] SSE loop error: {ex.Message}");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ConnectAndListenAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            "/_dev/events",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? eventName = null;
        string? eventData = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync().WaitAsync(cancellationToken);
            if (line is null)
            {
                return;
            }

            if (line.Length == 0)
            {
                if (string.Equals(eventName, "xaml-changed", StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(eventData))
                {
                    await HandleXamlChangedEventAsync(eventData, cancellationToken);
                }

                eventName = null;
                eventData = null;
                continue;
            }

            if (line.StartsWith("event:", StringComparison.Ordinal))
            {
                eventName = line[6..].Trim();
                continue;
            }

            if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                var payload = line[5..].TrimStart();
                eventData = eventData is null
                    ? payload
                    : $"{eventData}\n{payload}";
            }
        }
    }

    private async Task HandleXamlChangedEventAsync(string payloadJson, CancellationToken cancellationToken)
    {
        XamlChangeEventPayload? payload;
#pragma warning disable IL2026
        payload = JsonSerializer.Deserialize<XamlChangeEventPayload>(payloadJson, JsonOptions);
#pragma warning restore IL2026
        if (payload is null ||
            string.IsNullOrWhiteSpace(payload.RelativePath) ||
            !string.Equals(payload.RelativePath, TargetXamlPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (payload.Version <= Interlocked.Read(ref _lastAppliedVersion))
        {
            return;
        }

        var requestedPath = Uri.EscapeDataString(payload.RelativePath);
        string xaml;
        try
        {
            xaml = await _httpClient.GetStringAsync($"/_dev/xaml?path={requestedPath}", cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[hot-reload] Failed to fetch XAML source: {ex.Message}");
            return;
        }

        var wasApplied = await ApplyXamlOnUiThreadAsync(xaml);
        if (!wasApplied)
        {
            return;
        }

        Interlocked.Exchange(ref _lastAppliedVersion, payload.Version);
    }

    private static Task<bool> ApplyXamlOnUiThreadAsync(string xaml)
    {
        var application = Application.Current;
        var dispatcher = application?.Dispatcher;

        if (dispatcher is null)
        {
            return Task.FromResult(false);
        }

        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        dispatcher.Dispatch(() =>
        {
            try
            {
                var page = XamlDemoPage.Current;
                if (page is null)
                {
                    completion.TrySetResult(false);
                    return;
                }

                var applied = page.TryApplyLiveXaml(xaml, out var error);
                if (!applied && !string.IsNullOrWhiteSpace(error))
                {
                    Console.WriteLine($"[hot-reload] Live XAML apply failed: {error}");
                }

                completion.TrySetResult(applied);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        });

        return completion.Task;
    }

    public async ValueTask DisposeAsync()
    {
        _shutdown.Cancel();

        if (_eventLoop is not null)
        {
            try
            {
                await _eventLoop;
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
        }

        _httpClient.Dispose();
        _shutdown.Dispose();
    }

    private sealed class XamlChangeEventPayload
    {
        public long Version { get; init; }

        public string? RelativePath { get; init; }

        public DateTimeOffset Utc { get; init; }
    }
}
