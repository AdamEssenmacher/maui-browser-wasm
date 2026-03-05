using Microsoft.AspNetCore.StaticFiles;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.FileProviders;

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

var xamlRebuildWatcher = new XamlRebuildWatcher(sampleProjectPath, buildConfiguration);
app.Lifetime.ApplicationStarted.Register(xamlRebuildWatcher.Start);
app.Lifetime.ApplicationStopping.Register(xamlRebuildWatcher.Dispose);

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

app.MapGet("/_dev/hot-reload", () => Results.Json(new
{
    appBundle = sampleAppBundlePath
}));

app.MapGet("/_dev/sample-version", () => Results.Json(new
{
    version = xamlRebuildWatcher.Version,
    state = xamlRebuildWatcher.State,
    lastBuildUtc = xamlRebuildWatcher.LastBuildUtc
}));

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

internal sealed class XamlRebuildWatcher : IDisposable
{
    private readonly string _sampleProjectPath;
    private readonly string _sampleProjectDirectory;
    private readonly string _configuration;
    private readonly object _debounceLock = new();

    private FileSystemWatcher? _watcher;
    private CancellationTokenSource? _debounceCts;
    private int _buildLoopRunning;
    private int _pendingBuild;
    private long _version = 1;
    private string _state = "idle";
    private DateTimeOffset? _lastBuildUtc;

    public XamlRebuildWatcher(string sampleProjectPath, string configuration)
    {
        _sampleProjectPath = sampleProjectPath;
        _sampleProjectDirectory = Path.GetDirectoryName(sampleProjectPath)
            ?? throw new DirectoryNotFoundException($"Unable to resolve project directory from '{sampleProjectPath}'.");
        _configuration = configuration;
    }

    public long Version => Interlocked.Read(ref _version);

    public string State => Volatile.Read(ref _state);

    public DateTimeOffset? LastBuildUtc => _lastBuildUtc;

    public void Start()
    {
        if (!File.Exists(_sampleProjectPath))
        {
            Console.Error.WriteLine($"[devhost] Sample project not found at '{_sampleProjectPath}'.");
            return;
        }

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

        Console.WriteLine("[devhost] XAML rebuild watcher started.");
    }

    private void OnXamlChanged(string fullPath)
    {
        if (IgnorePath(fullPath))
        {
            return;
        }

        lock (_debounceLock)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();
            _ = DebounceThenQueueBuildAsync(_debounceCts.Token);
        }
    }

    private async Task DebounceThenQueueBuildAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(350, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        Interlocked.Exchange(ref _pendingBuild, 1);
        StartBuildLoopIfNeeded();
    }

    private void StartBuildLoopIfNeeded()
    {
        if (Interlocked.CompareExchange(ref _buildLoopRunning, 1, 0) != 0)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                while (Interlocked.Exchange(ref _pendingBuild, 0) == 1)
                {
                    await BuildSampleAsync();
                }
            }
            finally
            {
                Interlocked.Exchange(ref _buildLoopRunning, 0);
                if (Interlocked.Exchange(ref _pendingBuild, 0) == 1)
                {
                    StartBuildLoopIfNeeded();
                }
            }
        });
    }

    private async Task BuildSampleAsync()
    {
        Volatile.Write(ref _state, "building");
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{_sampleProjectPath}\" -c {_configuration} --nologo -v minimal",
            WorkingDirectory = _sampleProjectDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process
        {
            StartInfo = startInfo
        };

        try
        {
            if (!process.Start())
            {
                Volatile.Write(ref _state, "build-start-failed");
                Console.Error.WriteLine("[devhost] Failed to start XAML rebuild process.");
                return;
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode == 0)
            {
                Interlocked.Increment(ref _version);
                _lastBuildUtc = DateTimeOffset.UtcNow;
                Volatile.Write(ref _state, "build-ok");
                Console.WriteLine($"[devhost] XAML rebuild succeeded. Version={Version}");
                return;
            }

            Volatile.Write(ref _state, $"build-failed({process.ExitCode})");
            Console.Error.WriteLine($"[devhost] XAML rebuild failed with exit code {process.ExitCode}.");
            if (!string.IsNullOrWhiteSpace(stderr))
            {
                Console.Error.WriteLine(stderr);
            }
            else if (!string.IsNullOrWhiteSpace(stdout))
            {
                Console.Error.WriteLine(stdout);
            }
        }
        catch (Exception ex)
        {
            Volatile.Write(ref _state, "build-exception");
            Console.Error.WriteLine($"[devhost] XAML rebuild exception: {ex.Message}");
        }
    }

    private static bool IgnorePath(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            return true;
        }

        var fileName = Path.GetFileName(fullPath);
        return fileName.EndsWith("~", StringComparison.Ordinal) || fileName.StartsWith(".", StringComparison.Ordinal);
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        lock (_debounceLock)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = null;
        }
    }
}
