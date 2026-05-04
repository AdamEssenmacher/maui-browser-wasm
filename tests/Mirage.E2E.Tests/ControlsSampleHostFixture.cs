using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Microsoft.Playwright;
using Xunit;

namespace Mirage.E2E.Tests;

public sealed class ControlsSampleHostFixture : IAsyncLifetime
{
    private readonly ConcurrentQueue<string> _hostLogs = new();
    private Process? _sampleProcess;

    public IPlaywright? Playwright { get; private set; }

    public IBrowser? Browser { get; private set; }

    public Uri? HttpAppUrl { get; private set; }

    public async Task InitializeAsync()
    {
        HttpAppUrl = await StartSampleHostAsync();

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = ["--ignore-certificate-errors"]
        });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.CloseAsync();
            Browser = null;
        }

        Playwright?.Dispose();
        Playwright = null;

        if (_sampleProcess is not null && !_sampleProcess.HasExited)
        {
            _sampleProcess.Kill(entireProcessTree: true);
            await _sampleProcess.WaitForExitAsync();
        }

        _sampleProcess?.Dispose();
        _sampleProcess = null;
    }

    public async Task<IBrowserContext> NewContextAsync(int width = 1280, int height = 900, bool hasTouch = false)
    {
        if (Browser is null)
        {
            throw new InvalidOperationException("Playwright browser is not initialized.");
        }

        return await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            HasTouch = hasTouch,
            ViewportSize = new ViewportSize
            {
                Width = width,
                Height = height
            }
        });
    }

    public string GetHostLogsSnapshot() => RenderHostLogs();

    private async Task<Uri> StartSampleHostAsync()
    {
        await SampleHostStartupGate.WaitAsync();
        try
        {
            var repoRoot = FindRepoRoot();
            var sampleProjectPath = Path.Combine(repoRoot, "samples", "ControlsSample.Fluent", "ControlsSample.Fluent.csproj");
            var urlSource = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{sampleProjectPath}\" --configuration Release --no-build --no-launch-profile",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = repoRoot
            };

            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += (_, args) => ProcessHostLine(args.Data, urlSource);
            process.ErrorDataReceived += (_, args) => ProcessHostLine(args.Data, urlSource);
            process.Exited += (_, _) =>
            {
                if (urlSource.Task.IsCompleted)
                {
                    return;
                }

                urlSource.TrySetException(new InvalidOperationException(
                    $"ControlsSample.Fluent host exited before publishing app URL. ExitCode={process.ExitCode}.\n{RenderHostLogs()}"));
            };

            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start ControlsSample.Fluent host process.");
            }

            _sampleProcess = process;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            using var _ = timeout.Token.Register(() =>
                urlSource.TrySetException(new TimeoutException($"Timed out waiting for ControlsSample.Fluent URL.\n{RenderHostLogs()}")));

            return await urlSource.Task;
        }
        finally
        {
            SampleHostStartupGate.Release();
        }
    }

    private void ProcessHostLine(string? line, TaskCompletionSource<Uri> urlSource)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        _hostLogs.Enqueue(line);

        const string appUrlPrefix = "App url:";
        if (!line.StartsWith(appUrlPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var urlText = line[appUrlPrefix.Length..].Trim();
        if (Uri.TryCreate(urlText, UriKind.Absolute, out var uri))
        {
            urlSource.TrySetResult(uri);
        }
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var solutionPath = Path.Combine(current.FullName, "Mirage.sln");
            if (File.Exists(solutionPath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate repository root containing Mirage.sln.");
    }

    private string RenderHostLogs()
    {
        var sb = new StringBuilder();
        foreach (var line in _hostLogs.TakeLast(160))
        {
            sb.AppendLine(line);
        }

        return sb.ToString();
    }
}
