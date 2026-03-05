using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

#if DEBUG
var buildConfiguration = "Debug";
#else
var buildConfiguration = "Release";
#endif

var sampleAppBundlePath = ResolveSampleAppBundlePath(builder.Environment.ContentRootPath, buildConfiguration);
var sampleProjectPath = ResolveSampleProjectPath(builder.Environment.ContentRootPath);
if (!Directory.Exists(sampleAppBundlePath))
{
    throw new DirectoryNotFoundException(
        $"Sample AppBundle was not found at '{sampleAppBundlePath}'. Build Maui.Browser.Sample first.");
}

var devEndpointsEnabled = ResolveDevEndpointState(app.Environment);
var xamlStore = new XamlDocumentStore(sampleProjectPath, ["Demo/XamlDemoPage.xaml"]);
var xamlChangeStream = new XamlChangeStream();
var xamlFileWatcher = new XamlFileWatcher(sampleProjectPath, xamlStore.AllowedPaths);
xamlFileWatcher.XamlChanged += relativePath =>
{
    xamlStore.ClearOverride(relativePath);
    var change = xamlChangeStream.Publish(relativePath);
    Console.WriteLine($"[devhost] XAML changed: {change.RelativePath} (v{change.Version})");
};

app.Lifetime.ApplicationStarted.Register(xamlFileWatcher.Start);
app.Lifetime.ApplicationStopping.Register(() =>
{
    xamlFileWatcher.Dispose();
    xamlChangeStream.Dispose();
});

var sampleFiles = new PhysicalFileProvider(sampleAppBundlePath);
var indexPath = Path.Combine(sampleAppBundlePath, "index.html");
var contentTypeProvider = new FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".pdb"] = "application/octet-stream";
contentTypeProvider.Mappings[".dat"] = "application/octet-stream";
contentTypeProvider.Mappings[".symbols"] = "application/octet-stream";

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = sampleFiles,
    ContentTypeProvider = contentTypeProvider,
    ServeUnknownFileTypes = true
});

app.MapGet("/", async context =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    var html = await File.ReadAllTextAsync(indexPath, context.RequestAborted);
    await context.Response.WriteAsync(html, context.RequestAborted);
});

if (devEndpointsEnabled)
{
    app.MapGet("/_dev/hot-reload", () => Results.Json(new
    {
        enabled = true,
        mode = "sse-live-xaml",
        appBundle = sampleAppBundlePath,
        currentVersion = xamlChangeStream.CurrentVersion,
        allowedXamlPaths = xamlStore.AllowedPaths
    }));

    app.MapGet("/_dev/sample-version", () => Results.Json(new
    {
        version = xamlChangeStream.CurrentVersion,
        state = "watching"
    }));

    app.MapGet("/_dev/xaml", (HttpContext context) =>
    {
        var rawPath = context.Request.Query["path"].ToString();
        var relativePath = DevPathUtil.NormalizeRelativePath(rawPath);

        if (!xamlStore.IsAllowedPath(relativePath))
        {
            return Results.BadRequest(new { error = $"Unsupported XAML path '{rawPath}'." });
        }

        if (!xamlStore.TryGetXaml(relativePath, out var xaml, out var error))
        {
            return Results.NotFound(new { error });
        }

        return Results.Text(xaml, "application/xml; charset=utf-8");
    });

    app.MapGet("/_dev/events", async context =>
    {
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";
        context.Response.Headers.Append("X-Accel-Buffering", "no");
        context.Response.ContentType = "text/event-stream";

        var (subscriptionId, reader) = xamlChangeStream.Subscribe();

        try
        {
            await context.Response.WriteAsync(": connected\n\n", context.RequestAborted);
            await context.Response.Body.FlushAsync(context.RequestAborted);

            while (!context.RequestAborted.IsCancellationRequested)
            {
                XamlChangeEvent change;
                try
                {
                    change = await reader.ReadAsync(context.RequestAborted);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                var payload = JsonSerializer.Serialize(change);
                await context.Response.WriteAsync("event: xaml-changed\n", context.RequestAborted);
                await context.Response.WriteAsync($"data: {payload}\n\n", context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);
            }
        }
        finally
        {
            xamlChangeStream.Unsubscribe(subscriptionId);
        }
    });

    app.MapPost("/_dev/emit-xaml-change", async (HttpContext context) =>
    {
        SyntheticXamlChangeRequest? request;
        try
        {
            request = await JsonSerializer.DeserializeAsync<SyntheticXamlChangeRequest>(
                context.Request.Body,
                cancellationToken: context.RequestAborted);
        }
        catch (JsonException ex)
        {
            return (IResult)Results.BadRequest(new { error = ex.Message });
        }

        var relativePath = DevPathUtil.NormalizeRelativePath(request?.RelativePath);
        if (!xamlStore.IsAllowedPath(relativePath))
        {
            return (IResult)Results.BadRequest(new { error = $"Unsupported XAML path '{request?.RelativePath}'." });
        }

        if (request?.XamlOverride is { Length: > 0 })
        {
            xamlStore.SetOverride(relativePath, request.XamlOverride);
        }
        else
        {
            xamlStore.ClearOverride(relativePath);
        }

        var change = xamlChangeStream.Publish(relativePath);
        return (IResult)Results.Json(change);
    });
}
else
{
    app.Map("/_dev/{**path}", static context =>
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return Task.CompletedTask;
    });
}

app.MapFallback(async context =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    var html = await File.ReadAllTextAsync(indexPath, context.RequestAborted);
    await context.Response.WriteAsync(html, context.RequestAborted);
});

app.Run();

static string ResolveSampleAppBundlePath(string devHostContentRoot, string configuration)
{
    var configName = string.Equals(configuration, "Release", StringComparison.OrdinalIgnoreCase)
        ? "Release"
        : "Debug";

    return Path.GetFullPath(Path.Combine(
        devHostContentRoot,
        "..",
        "Maui.Browser.Sample",
        "bin",
        configName,
        "net10.0-browser",
        "browser-wasm",
        "AppBundle"));
}

static string ResolveSampleProjectPath(string devHostContentRoot)
{
    return Path.GetFullPath(Path.Combine(
        devHostContentRoot,
        "..",
        "Maui.Browser.Sample",
        "Maui.Browser.Sample.csproj"));
}

static bool ResolveDevEndpointState(IHostEnvironment environment)
{
#if DEBUG
    return true;
#else
    return environment.IsDevelopment() || string.Equals(Environment.GetEnvironmentVariable("DOTNET_WATCH"), "1", StringComparison.Ordinal);
#endif
}

internal sealed record XamlChangeEvent(long Version, string RelativePath, DateTimeOffset Utc);

internal sealed class SyntheticXamlChangeRequest
{
    public string? RelativePath { get; init; }

    public string? XamlOverride { get; init; }
}

internal sealed class XamlChangeStream : IDisposable
{
    private readonly ConcurrentDictionary<Guid, Channel<XamlChangeEvent>> _subscribers = new();
    private long _currentVersion = 1;

    public long CurrentVersion => Volatile.Read(ref _currentVersion);

    public XamlChangeEvent Publish(string relativePath)
    {
        var change = new XamlChangeEvent(
            Version: Interlocked.Increment(ref _currentVersion),
            RelativePath: relativePath,
            Utc: DateTimeOffset.UtcNow);

        foreach (var subscriber in _subscribers.Values)
        {
            subscriber.Writer.TryWrite(change);
        }

        return change;
    }

    public (Guid SubscriptionId, ChannelReader<XamlChangeEvent> Reader) Subscribe()
    {
        var subscriptionId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<XamlChangeEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _subscribers[subscriptionId] = channel;
        return (subscriptionId, channel.Reader);
    }

    public void Unsubscribe(Guid subscriptionId)
    {
        if (_subscribers.TryRemove(subscriptionId, out var channel))
        {
            channel.Writer.TryComplete();
        }
    }

    public void Dispose()
    {
        foreach (var entry in _subscribers)
        {
            entry.Value.Writer.TryComplete();
        }

        _subscribers.Clear();
    }
}

internal sealed class XamlDocumentStore
{
    private readonly string _sampleProjectDirectory;
    private readonly HashSet<string> _allowedPaths;
    private readonly ConcurrentDictionary<string, string> _overrides = new(StringComparer.OrdinalIgnoreCase);

    public XamlDocumentStore(string sampleProjectPath, IEnumerable<string> allowedPaths)
    {
        _sampleProjectDirectory = Path.GetDirectoryName(sampleProjectPath)
            ?? throw new DirectoryNotFoundException($"Unable to resolve project directory from '{sampleProjectPath}'.");

        _allowedPaths = new HashSet<string>(allowedPaths.Select(DevPathUtil.NormalizeRelativePath), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<string> AllowedPaths => _allowedPaths;

    public bool IsAllowedPath(string relativePath)
    {
        return _allowedPaths.Contains(DevPathUtil.NormalizeRelativePath(relativePath));
    }

    public void SetOverride(string relativePath, string xaml)
    {
        _overrides[DevPathUtil.NormalizeRelativePath(relativePath)] = xaml;
    }

    public void ClearOverride(string relativePath)
    {
        _overrides.TryRemove(DevPathUtil.NormalizeRelativePath(relativePath), out _);
    }

    public bool TryGetXaml(string relativePath, out string xaml, out string? error)
    {
        var normalized = DevPathUtil.NormalizeRelativePath(relativePath);
        if (_overrides.TryGetValue(normalized, out xaml!))
        {
            error = null;
            return true;
        }

        var fullPath = Path.GetFullPath(Path.Combine(_sampleProjectDirectory, normalized));
        if (!fullPath.StartsWith(_sampleProjectDirectory, StringComparison.Ordinal))
        {
            xaml = string.Empty;
            error = "XAML path escapes sample directory.";
            return false;
        }

        if (!File.Exists(fullPath))
        {
            xaml = string.Empty;
            error = $"XAML file '{normalized}' was not found.";
            return false;
        }

        try
        {
            xaml = File.ReadAllText(fullPath);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            xaml = string.Empty;
            error = ex.Message;
            return false;
        }
    }
}

internal sealed class XamlFileWatcher : IDisposable
{
    private readonly string _sampleProjectDirectory;
    private readonly HashSet<string> _allowedPaths;
    private readonly ConcurrentDictionary<string, long> _lastEventTicks = new(StringComparer.OrdinalIgnoreCase);
    private FileSystemWatcher? _watcher;

    public XamlFileWatcher(string sampleProjectPath, IEnumerable<string> allowedPaths)
    {
        _sampleProjectDirectory = Path.GetDirectoryName(sampleProjectPath)
            ?? throw new DirectoryNotFoundException($"Unable to resolve project directory from '{sampleProjectPath}'.");
        _allowedPaths = new HashSet<string>(allowedPaths.Select(DevPathUtil.NormalizeRelativePath), StringComparer.OrdinalIgnoreCase);
    }

    public event Action<string>? XamlChanged;

    public void Start()
    {
        _watcher = new FileSystemWatcher(_sampleProjectDirectory, "*.xaml")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
        };

        _watcher.Changed += (_, args) => OnXamlChanged(args.FullPath);
        _watcher.Created += (_, args) => OnXamlChanged(args.FullPath);
        _watcher.Renamed += (_, args) => OnXamlChanged(args.FullPath);
        _watcher.Deleted += (_, args) => OnXamlChanged(args.FullPath);
        _watcher.EnableRaisingEvents = true;

        Console.WriteLine("[devhost] XAML file watcher started.");
    }

    private void OnXamlChanged(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            return;
        }

        var fileName = Path.GetFileName(fullPath);
        if (fileName.EndsWith("~", StringComparison.Ordinal) || fileName.StartsWith(".", StringComparison.Ordinal))
        {
            return;
        }

        var relativePath = DevPathUtil.NormalizeRelativePath(Path.GetRelativePath(_sampleProjectDirectory, fullPath));
        if (relativePath.StartsWith("../", StringComparison.Ordinal) || !_allowedPaths.Contains(relativePath))
        {
            return;
        }

        if (IsDuplicateBurst(relativePath))
        {
            return;
        }

        XamlChanged?.Invoke(relativePath);
    }

    private bool IsDuplicateBurst(string relativePath)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (_lastEventTicks.TryGetValue(relativePath, out var previous) && (now - previous) < 200)
        {
            _lastEventTicks[relativePath] = now;
            return true;
        }

        _lastEventTicks[relativePath] = now;
        return false;
    }

    public void Dispose()
    {
        _watcher?.Dispose();
    }
}

internal static class DevPathUtil
{
    public static string NormalizeRelativePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var normalized = path.Replace('\\', '/').Trim();
        while (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        return normalized;
    }
}
